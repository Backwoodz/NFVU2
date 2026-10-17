"""Prepare the user-selected sustained core reactor as one crossfaded local loop.

Requires numpy and FFmpeg. Sources and generated audio stay ignored. The crop
is based on waveform energy only; listening and in-game review remain pending.
"""
import argparse
import json
from pathlib import Path
import subprocess

import numpy as np

from prepare_transformer_audio import RATE, ROOT, find_ffmpeg, sha256, write_wav


PARAMETERS = {
    'crop_start_seconds': 8.0,
    'crop_end_seconds': 16.8,
    'crossfade_seconds': 0.8,
    'crossfade_shape': 'equal_power_cosine_sine_tail_into_head',
    'normalization_window_seconds': 0.08,
    'target_max_window_rms': 0.055,
    'peak_ceiling': 0.5,
}


def decode_reactor(ffmpeg, source):
    # The chosen recording is about 73 s: the short one-shot helper's 60 s
    # bound does not apply. Bound decoder output and processing time explicitly.
    raw = subprocess.check_output([
        str(ffmpeg), '-v', 'error', '-i', str(source), '-t', '120',
        '-f', 'f32le', '-ac', '1', '-ar', str(RATE), 'pipe:1',
    ], timeout=60)
    samples = np.frombuffer(raw, dtype='<f4').astype(np.float64)
    if not np.isfinite(samples).all() or not RATE <= len(samples) <= RATE * 120:
        raise ValueError('Expected finite, bounded reactor recording')
    return samples


def window_rms(samples):
    window = round(PARAMETERS['normalization_window_seconds'] * RATE)
    if len(samples) < window:
        raise ValueError('Reactor selection too short')
    energy = np.concatenate(([0.0], np.cumsum(samples * samples)))
    return np.sqrt(np.maximum(0, energy[window:] - energy[:-window]) / window)


def prepare_reactor(samples):
    start = round(PARAMETERS['crop_start_seconds'] * RATE)
    end = round(PARAMETERS['crop_end_seconds'] * RATE)
    overlap = round(PARAMETERS['crossfade_seconds'] * RATE)
    if not np.isfinite(samples).all() or not 0 <= start < end <= len(samples):
        raise ValueError('Missing or invalid configured reactor crop')
    selection = samples[start:end].copy()
    selection -= np.mean(selection)
    # Retain the continuous middle, then overlap the crop's tail into its head.
    # The loop seam follows the next sample of the head, with no fade to zero.
    theta = np.linspace(0, np.pi / 2, overlap)
    join = selection[-overlap:] * np.cos(theta) + selection[:overlap] * np.sin(theta)
    result = np.concatenate([selection[overlap:-overlap], join])
    peak = float(np.max(np.abs(result)))
    rms = float(np.max(window_rms(result)))
    if peak < .001 or rms < .001:
        raise ValueError('Silent or damaged reactor crop')
    gain = min(PARAMETERS['target_max_window_rms'] / rms,
               PARAMETERS['peak_ceiling'] / peak)
    result *= gain
    # Include a whole repeated join in energy checks, rather than report only
    # the interior of a loop that might hide a silent boundary.
    repeated_rms = window_rms(np.tile(result, 2))
    metadata = {
        'crop_start_sample': start,
        'crop_end_sample_exclusive': end,
        'crop_start_seconds': start / RATE,
        'crop_end_seconds': end / RATE,
        'crossfade_samples': overlap,
        'first_output_source_seconds': (start + overlap) / RATE,
        'gain_db': float(20 * np.log10(gain)),
        'peak': float(np.max(np.abs(result))),
        'max_window_rms': float(np.max(repeated_rms)),
        'min_window_rms': float(np.min(repeated_rms)),
        'rms': float(np.sqrt(np.mean(result * result))),
        'loop_boundary_sample_jump': float(abs(result[-1] - result[0])),
    }
    return result, metadata


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-file', required=True, type=Path)
    parser.add_argument('--ffmpeg', type=Path)
    parser.add_argument('--output-dir', type=Path, default=ROOT / '.local/audio-transformer')
    parser.add_argument('--preview', type=Path, help='Optional three consecutive loops, without gaps')
    args = parser.parse_args()
    if not args.source_file.is_file():
        raise ValueError('Missing reactor source recording')
    targets = [args.output_dir / 'reactor.wav', args.output_dir / 'reactor_manifest.json']
    if any(p.is_symlink() or p.resolve() == args.source_file.resolve() for p in targets):
        raise ValueError('Refusing symlink output or source replacement')
    if args.preview and (args.preview.is_symlink() or
                         args.preview.resolve() == args.source_file.resolve() or
                         args.preview.resolve().parent == args.output_dir.resolve()):
        raise ValueError('Preview must be separate from the source and managed output folder')
    ffmpeg = find_ffmpeg(args.ffmpeg)
    decoded = decode_reactor(ffmpeg, args.source_file)
    result, metadata = prepare_reactor(decoded)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_wav(targets[0], result)
    manifest = {
        'schema_version': 1,
        'files': [{
            'file': targets[0].name, 'role': 'reactor', 'source': args.source_file.name,
            'source_sha256': sha256(args.source_file), 'source_duration_seconds': len(decoded) / RATE,
            'sample_rate': RATE, 'channels': 1, 'bits_per_sample': 16,
            'samples': len(result), 'duration_seconds': len(result) / RATE,
            'loop': True, **metadata, 'sha256': sha256(targets[0]),
        }],
        'processing': PARAMETERS,
        'decoder_version': subprocess.check_output([str(ffmpeg), '-version'], text=True).splitlines()[0],
        'provenance': 'User-selected local kernreaktor.mp3 recording. Source filename and SHA-256 identify the input; licensing and redistribution rights have not been independently verified.',
        'review_status': 'Waveform-based sustained-region selection; native listening and in-game review pending.',
        'notes': 'Stable source energy at 8-16.8 s; crop excludes the initial build-up and later stronger passages. DC removal, 800 ms equal-power circular overlap and gain only. No pitch shift, time stretch or silent fade at the loop boundary. Runtime fades by visible core deployment, then loops continuously in robot form, including saved parked robots.',
    }
    targets[1].write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    if args.preview:
        write_wav(args.preview, np.tile(result, 3))
    print(f'Prepared {len(result) / RATE:.2f} s mono PCM16 reactor loop; listening review pending.')
    print(json.dumps(metadata, indent=2))


if __name__ == '__main__':
    main()
