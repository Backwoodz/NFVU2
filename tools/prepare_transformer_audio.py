"""Prepare local, user-selected Transformer step samples; never install them.

Requires numpy and FFmpeg. Sources and generated WAVs stay outside version
control. The waveform-based first cut still needs a listening/in-game review.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path
import shutil
import subprocess
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
HEAVY = 'daviddumaisaudio-robot-heavy-mechanical-footsteps-194039.mp3'
# The first recording is a sequence: select complete, separate footfalls.
# Other recordings contain one impact with a long mechanical lead-in.
SELECTIONS = [
    (1, HEAVY, 0.0, 0.47),
    (2, HEAVY, 0.5, 0.97),
    (3, HEAVY, 1.0, 1.47),
    (4, HEAVY, 1.5, 1.97),
    (5, 'audiopapkin-big-robot-footstep-330678.mp3', 0.0, None),
    # Original variant 06 (015-445103) rejected after user listening: too bright.
    # Keep the remaining filenames stable for provenance and stale-file removal.
    (7, 'audiopapkin-big-robot-footstep-016-445102.mp3', 0.0, None),
]
PARAMETERS = {
    'envelope_window_seconds': 0.005,
    'attack_threshold_fraction': 0.4,
    'attack_preroll_seconds': 0.015,
    'tail_threshold_fraction': 0.012,
    'tail_extra_seconds': 0.05,
    'fade_in_seconds': 0.003,
    'fade_out_seconds': 0.03,
    'normalization_window_seconds': 0.08,
    'target_max_window_rms': 0.14,
    'peak_ceiling': 0.7,
}


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def find_ffmpeg(explicit):
    if explicit:
        path = explicit.resolve()
    else:
        binaries = ROOT / '.local/tools/audio-encoder/imageio_ffmpeg/binaries'
        choices = sorted(binaries.glob('ffmpeg-*.exe'))
        found = shutil.which('ffmpeg')
        path = choices[0] if choices else Path(found) if found else None
    if path is None or not path.is_file():
        raise ValueError('FFmpeg unavailable; pass --ffmpeg or add it to PATH.')
    return path


def decode(ffmpeg, source):
    # Float decode avoids clipping decoded MP3 overshoot before normalization.
    raw = subprocess.check_output([
        str(ffmpeg), '-v', 'error', '-i', str(source), '-f', 'f32le',
        '-ac', '1', '-ar', str(RATE), 'pipe:1',
    ])
    samples = np.frombuffer(raw, dtype='<f4').astype(np.float64)
    if not np.isfinite(samples).all() or not RATE // 4 <= len(samples) <= RATE * 60:
        raise ValueError(f'Expected finite, short audio: {source.name}')
    return samples


def max_window_rms(samples):
    length = min(round(PARAMETERS['normalization_window_seconds'] * RATE), len(samples))
    energy = np.concatenate(([0.0], np.cumsum(samples * samples)))
    return float(np.sqrt(np.max(energy[length:] - energy[:-length]) / length))


def prepare_step(samples, window_start, window_end):
    """Keep one impact, align its rise, fade the tail, and apply only gain."""
    start = round(window_start * RATE)
    end = len(samples) if window_end is None else round(window_end * RATE)
    if not 0 <= start < end <= len(samples):
        raise ValueError('Source does not contain the configured selection window')
    selected = samples[start:end]
    block = round(PARAMETERS['envelope_window_seconds'] * RATE)
    blocks = len(selected) // block
    envelope = np.sqrt(np.mean(selected[:blocks * block].reshape(-1, block) ** 2, axis=1))
    peak_index = int(np.argmax(envelope))
    peak_rms = float(envelope[peak_index])
    if peak_rms < 0.001:
        raise ValueError('Silent or damaged step source')
    attack = int(np.flatnonzero(envelope[:peak_index + 1] >=
                               peak_rms * PARAMETERS['attack_threshold_fraction'])[0]) * block
    begin = max(0, attack - round(PARAMETERS['attack_preroll_seconds'] * RATE))
    active = np.flatnonzero(envelope >= peak_rms * PARAMETERS['tail_threshold_fraction'])
    finish = min(len(selected), (int(active[-1]) + 1) * block +
                 round(PARAMETERS['tail_extra_seconds'] * RATE))
    result = selected[begin:finish].copy()
    result -= np.mean(result)
    fade_in = min(round(PARAMETERS['fade_in_seconds'] * RATE), len(result) // 4)
    fade_out = min(round(PARAMETERS['fade_out_seconds'] * RATE), len(result) // 4)
    result[:fade_in] *= np.linspace(0, 1, fade_in)
    result[-fade_out:] *= np.linspace(1, 0, fade_out)
    raw_peak = float(np.max(np.abs(result)))
    raw_rms = max_window_rms(result)
    gain = min(PARAMETERS['target_max_window_rms'] / raw_rms,
               PARAMETERS['peak_ceiling'] / raw_peak)
    result *= gain
    metadata = {
        'selection_window_seconds': [window_start, len(samples) / RATE if window_end is None else window_end],
        'crop_start_sample': start + begin,
        'crop_end_sample_exclusive': start + finish,
        'crop_start_seconds': (start + begin) / RATE,
        'crop_end_seconds': (start + finish) / RATE,
        'attack_offset_seconds': (attack - begin) / RATE,
        'gain_db': float(20 * np.log10(gain)),
        'peak': float(np.max(np.abs(result))),
        'max_window_rms': max_window_rms(result),
    }
    return result, metadata


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.rint(np.clip(samples, -1, 1) * 32767).astype('<i2')
    with wave.open(str(path), 'wb') as stream:
        stream.setparams((1, 2, RATE, len(pcm), 'NONE', 'not compressed'))
        stream.writeframes(pcm.tobytes())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-dir', required=True, type=Path,
                        help='Folder with the three retained user-selected MP3 recordings')
    parser.add_argument('--ffmpeg', type=Path)
    parser.add_argument('--output-dir', type=Path, default=ROOT / '.local/audio-transformer')
    parser.add_argument('--preview', type=Path,
                        help='Optional comparison WAV: each variant once, with one-second gaps')
    args = parser.parse_args()
    ffmpeg = find_ffmpeg(args.ffmpeg)
    names = sorted({name for _, name, _, _ in SELECTIONS})
    missing = [name for name in names if not (args.source_dir / name).is_file()]
    if missing:
        raise ValueError('Missing source recordings: ' + ', '.join(missing))
    decoded = {name: decode(ffmpeg, args.source_dir / name) for name in names}
    hashes = {name: sha256(args.source_dir / name) for name in names}
    args.output_dir.mkdir(parents=True, exist_ok=True)
    files = []
    previews = [np.zeros(RATE // 2)]
    for index, name, start, end in SELECTIONS:
        samples, metadata = prepare_step(decoded[name], start, end)
        target = args.output_dir / f'step_{index:02d}.wav'
        write_wav(target, samples)
        files.append({
            'file': target.name, 'role': 'footstep', 'source': name,
            'source_sha256': hashes[name], 'source_duration_seconds': len(decoded[name]) / RATE,
            'sample_rate': RATE, 'channels': 1, 'bits_per_sample': 16,
            'samples': len(samples), 'duration_seconds': len(samples) / RATE,
            'loop': False, **metadata, 'sha256': sha256(target),
        })
        previews.extend([samples, np.zeros(RATE)])
    version = subprocess.check_output([str(ffmpeg), '-version'], text=True).splitlines()[0]
    manifest = {
        'schema_version': 1, 'files': files, 'processing': PARAMETERS,
        'decoder_version': version,
        'provenance': 'Locally downloaded MP3 recordings selected by the user. Source names and hashes identify the inputs; licensing and redistribution rights have not been independently verified.',
        'review_status': 'Initial set reviewed by user; original variant 06 (015-445103) rejected as too bright. Six remaining variants retained. Revised in-game review pending; runtime gain increased by 5 percent separately.',
        'notes': 'Mono PCM16 one-shots. First four clips are separate impacts from a sequence. Lead-in is trimmed to 15 ms before the first 40-percent RMS rise. DC removal, short linear fades and gain only; no time stretch, pitch change or synthetic layers.',
    }
    (args.output_dir / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    # This output folder contains generated samples. Remove retired numbered
    # outputs so the worker, which discovers step_*.wav, cannot reload them.
    wanted = {item['file'] for item in files}
    for path in args.output_dir.iterdir():
        if re.fullmatch(r'step_\d{2}\.wav', path.name) and path.name not in wanted and path.is_file() and not path.is_symlink():
            path.unlink()
    if args.preview:
        write_wav(args.preview, np.concatenate(previews))
        print('Comparison order: ' + ', '.join(item['file'] for item in files))
    print(f'Prepared {len(files)} mono PCM16 steps at {RATE} Hz; listening review pending.')


if __name__ == '__main__':
    main()
