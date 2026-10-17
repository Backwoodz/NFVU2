"""Prepare local, user-selected Transformer turning-mechanism samples.

Requires numpy and FFmpeg. This first waveform-based cut needs listening and
in-game review. Sources and generated WAVs remain outside version control.
Only turn_*.wav and turn_manifest.json are managed by this script.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess

import numpy as np

from prepare_transformer_audio import RATE, ROOT, decode, find_ffmpeg, sha256, write_wav


# Separate active regions of the user-selected recording. Avoid the opening
# silence and the isolated high transient at about 26 seconds. These are
# independent, short one-shots, not slices intended to form a seamless loop.
SELECTIONS = [
    (1, 1.56, 2.56),
    (2, 7.58, 8.48),
    (3, 12.43, 13.53),
    (4, 26.46, 27.66),
]
PARAMETERS = {
    'fade_in_seconds': 0.012,
    'fade_out_seconds': 0.10,
    'normalization_window_seconds': 0.08,
    'target_max_window_rms': 0.08,
    'peak_ceiling': 0.55,
}


def max_window_rms(samples):
    length = min(round(PARAMETERS['normalization_window_seconds'] * RATE), len(samples))
    energy = np.concatenate(([0.0], np.cumsum(samples * samples)))
    return float(np.sqrt(np.max(energy[length:] - energy[:-length]) / length))


def prepare_turn(samples, start_seconds, end_seconds):
    start = round(start_seconds * RATE)
    end = round(end_seconds * RATE)
    if not 0 <= start < end <= len(samples):
        raise ValueError('Source does not contain the configured turning selection')
    result = samples[start:end].copy()
    result -= np.mean(result)
    fade_in = min(round(PARAMETERS['fade_in_seconds'] * RATE), len(result) // 4)
    fade_out = min(round(PARAMETERS['fade_out_seconds'] * RATE), len(result) // 4)
    result[:fade_in] *= np.linspace(0, 1, fade_in)
    result[-fade_out:] *= np.linspace(1, 0, fade_out)
    raw_peak = float(np.max(np.abs(result)))
    raw_rms = max_window_rms(result)
    if raw_peak < 0.001 or raw_rms < 0.001:
        raise ValueError('Silent or damaged turning selection')
    gain = min(PARAMETERS['target_max_window_rms'] / raw_rms,
               PARAMETERS['peak_ceiling'] / raw_peak)
    result *= gain
    return result, {
        'selection_window_seconds': [start_seconds, end_seconds],
        'crop_start_sample': start,
        'crop_end_sample_exclusive': end,
        'crop_start_seconds': start / RATE,
        'crop_end_seconds': end / RATE,
        'gain_db': float(20 * np.log10(gain)),
        'peak': float(np.max(np.abs(result))),
        'max_window_rms': max_window_rms(result),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-file', required=True, type=Path,
                        help='User-selected dreh sounds.mp3 recording')
    parser.add_argument('--ffmpeg', type=Path)
    parser.add_argument('--output-dir', type=Path, default=ROOT / '.local/audio-transformer')
    parser.add_argument('--preview', type=Path,
                        help='Optional comparison WAV with one-second gaps')
    args = parser.parse_args()
    if not args.source_file.is_file():
        raise ValueError('Missing turning source recording')
    # Keep explicitly requested preview output away from the managed assets.
    if args.preview and (args.preview.resolve() == args.source_file.resolve() or
                         args.preview.resolve().parent == args.output_dir.resolve()):
        raise ValueError('Preview must be separate from the source and managed output folder')
    ffmpeg = find_ffmpeg(args.ffmpeg)
    decoded = decode(ffmpeg, args.source_file)
    source_hash = sha256(args.source_file)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    files = []
    previews = [np.zeros(RATE // 2)]
    for index, start, end in SELECTIONS:
        samples, metadata = prepare_turn(decoded, start, end)
        target = args.output_dir / f'turn_{index:02d}.wav'
        if target.is_symlink():
            raise ValueError(f'Refusing symlink output: {target.name}')
        write_wav(target, samples)
        files.append({
            'file': target.name, 'role': 'turn_mechanism',
            'source': args.source_file.name, 'source_sha256': source_hash,
            'source_duration_seconds': len(decoded) / RATE,
            'sample_rate': RATE, 'channels': 1, 'bits_per_sample': 16,
            'samples': len(samples), 'duration_seconds': len(samples) / RATE,
            'loop': False, **metadata, 'sha256': sha256(target),
        })
        previews.extend([samples, np.zeros(RATE)])
    manifest = {
        'schema_version': 1,
        'files': files,
        'processing': PARAMETERS,
        'decoder_version': subprocess.check_output(
            [str(ffmpeg), '-version'], text=True).splitlines()[0],
        'provenance': 'User-selected local MP3 recording. Source filename and SHA-256 identify the input; licensing and redistribution rights have not been independently verified.',
        'review_status': 'Waveform-based first cut; listening and in-game review pending.',
        'notes': 'Mono PCM16 one-shots from four separate active source regions. DC removal, short fade-in, soft 100 ms fade-out and gain only; no time stretch, pitch change or synthetic layers. Target 80 ms RMS is below the approved footstep target. Use as varied bursts during actual turning, with runtime fade-out when turning stops.',
    }
    manifest_path = args.output_dir / 'turn_manifest.json'
    if manifest_path.is_symlink():
        raise ValueError('Refusing symlink manifest output')
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    wanted = {item['file'] for item in files}
    for path in args.output_dir.iterdir():
        if (re.fullmatch(r'turn_\d{2}\.wav', path.name) and path.name not in wanted
                and path.is_file() and not path.is_symlink()):
            path.unlink()
    if args.preview:
        write_wav(args.preview, np.concatenate(previews))
        print('Comparison order: ' + ', '.join(item['file'] for item in files))
    print(f'Prepared {len(files)} mono PCM16 turns at {RATE} Hz; listening review pending.')


if __name__ == '__main__':
    main()
