"""Prepare the sustained core reactor with an optional additive spaceship layer.

Requires numpy and FFmpeg. Sources and generated audio stay ignored. The crop
is based on waveform energy only; listening and in-game review remain pending.
"""
import argparse
import json
from pathlib import Path
import subprocess

import numpy as np

from prepare_transformer_audio import RATE, ROOT, find_ffmpeg, sha256, write_wav
from prepare_transformer_mechanical_audio import wav_sha256


PARAMETERS = {
    'crop_start_seconds': 8.0,
    'crop_end_seconds': 16.8,
    'crossfade_seconds': 0.8,
    'crossfade_shape': 'equal_power_cosine_sine_tail_into_head',
    'normalization_window_seconds': 0.08,
    'target_max_window_rms': 0.055,
    'peak_ceiling': 0.5,
}
ADDITIONAL_PARAMETERS = {
    'crop_start_seconds': 0.8,
    'crop_end_seconds': 5.6,
    'crossfade_seconds': 0.8,
    'crossfade_shape': 'equal_power_cosine_sine_tail_into_head',
    'normalization_window_seconds': 0.08,
    'target_max_window_rms': 0.035,
    'peak_ceiling': 0.3,
    'loop_seconds': 4.0,
}
MIX_PEAK_CEILING = 0.5


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


def loop_statistics(samples):
    repeated_rms = window_rms(np.tile(samples, 2))
    return {
        'peak': float(np.max(np.abs(samples))),
        'max_window_rms': float(np.max(repeated_rms)),
        'min_window_rms': float(np.min(repeated_rms)),
        'rms': float(np.sqrt(np.mean(samples * samples))),
        'loop_boundary_sample_jump': float(abs(samples[-1] - samples[0])),
    }


def prepare_additional_reactor(samples):
    """Keep the source's natural pulses in a four-second circular loop."""
    parameters = ADDITIONAL_PARAMETERS
    start = round(parameters['crop_start_seconds'] * RATE)
    end = round(parameters['crop_end_seconds'] * RATE)
    overlap = round(parameters['crossfade_seconds'] * RATE)
    if not np.isfinite(samples).all() or not 0 <= start < end <= len(samples):
        raise ValueError('Missing or invalid additional reactor crop')
    selection = samples[start:end].copy()
    selection -= np.mean(selection)
    theta = np.linspace(0, np.pi / 2, overlap)
    join = selection[-overlap:] * np.cos(theta) + selection[:overlap] * np.sin(theta)
    result = np.concatenate([selection[overlap:-overlap], join])
    if len(result) != round(parameters['loop_seconds'] * RATE):
        raise ValueError('Additional reactor loop must retain its natural four-second length')
    peak = float(np.max(np.abs(result)))
    rms = float(np.max(window_rms(result)))
    if min(peak, rms) < .001:
        raise ValueError('Silent or damaged additional reactor crop')
    gain = min(parameters['target_max_window_rms'] / rms, parameters['peak_ceiling'] / peak)
    result *= gain
    metadata = {
        'crop_start_sample': start, 'crop_end_sample_exclusive': end,
        'crop_start_seconds': start / RATE, 'crop_end_seconds': end / RATE,
        'crossfade_samples': overlap, 'crossfade_seconds': overlap / RATE,
        'first_output_source_seconds': (start + overlap) / RATE,
        'duration_seconds': len(result) / RATE,
        'gain_db': float(20 * np.log10(gain)),
        'isolated_wav_sha256': wav_sha256(result),
        **loop_statistics(result),
    }
    return result, metadata


def mix_reactor_layers(foundation, additional):
    """Add whole unmodified loop cycles; only the new bus may lose headroom."""
    if (len(foundation) == 0 or len(additional) == 0 or
            len(foundation) % len(additional) != 0 or
            not np.isfinite(foundation).all() or not np.isfinite(additional).all()):
        raise ValueError('Reactor layers must be finite whole cycles with matching duration')
    if np.max(np.abs(foundation)) > MIX_PEAK_CEILING:
        raise ValueError('Existing reactor foundation exceeds the mix peak ceiling')
    repeats = len(foundation) // len(additional)
    added = np.tile(additional, repeats)
    active = np.abs(added) > 1e-12
    # A single fixed new-layer gain preserves its circular seam and pulse shape.
    # The foundation is never scaled, ducked, normalized or modified in place.
    headroom = MIX_PEAK_CEILING - np.sign(added[active]) * foundation[active]
    gain = min(1.0, float(np.min(headroom / np.abs(added[active])))) if np.any(active) else 1.0
    mixed = foundation + added * gain
    return mixed, {
        'mix_peak_ceiling': MIX_PEAK_CEILING,
        'foundation_gain_db': 0.0, 'mix_gain_db': 0.0,
        'additional_headroom_gain_db': float(20 * np.log10(gain)) if gain > 0 else None,
        'additional_headroom_gain_linear': gain,
        'additional_repetitions': repeats,
        'foundation_isolated_wav_sha256': wav_sha256(foundation),
        'additional_only_headroom_constraint': 'fixed gain only if the additive layer would exceed available peak headroom',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-file', required=True, type=Path)
    parser.add_argument('--additional-source-file', type=Path,
                        help='Optional user-selected evil-robot/spaceship recording')
    parser.add_argument('--ffmpeg', type=Path)
    parser.add_argument('--output-dir', type=Path, default=ROOT / '.local/audio-transformer')
    parser.add_argument('--preview', type=Path, help='Optional three consecutive loops, without gaps')
    args = parser.parse_args()
    if not args.source_file.is_file():
        raise ValueError('Missing reactor source recording')
    source_paths = [args.source_file] + ([args.additional_source_file] if args.additional_source_file else [])
    if any(not path.is_file() for path in source_paths):
        raise ValueError('Missing additional reactor source recording')
    resolved_sources = {path.resolve() for path in source_paths}
    targets = [args.output_dir / 'reactor.wav', args.output_dir / 'reactor_manifest.json']
    if any(p.is_symlink() or p.resolve() in resolved_sources for p in targets):
        raise ValueError('Refusing symlink output or source replacement')
    if args.preview and (args.preview.is_symlink() or
                         args.preview.resolve() in resolved_sources or
                         args.preview.resolve().parent == args.output_dir.resolve()):
        raise ValueError('Preview must be separate from the source and managed output folder')
    ffmpeg = find_ffmpeg(args.ffmpeg)
    decoded = decode_reactor(ffmpeg, args.source_file)
    result, metadata = prepare_reactor(decoded)
    base_hash = wav_sha256(result)
    layers = [{
        'name': 'original_core_reactor_foundation', 'source': args.source_file.name,
        'source_sha256': sha256(args.source_file),
        'source_duration_seconds': len(decoded) / RATE,
        'duration_seconds': len(result) / RATE,
        'bus': 'preserved_foundation', 'isolated_wav_sha256': base_hash,
        'mix_gain_db': 0.0, **metadata,
    }]
    sources = [{'file': args.source_file.name, 'sha256': sha256(args.source_file),
                'duration_seconds': len(decoded) / RATE}]
    processing = {**PARAMETERS, 'foundation_parameters': PARAMETERS,
                  'time_stretch': False, 'pitch_change': False,
                  'foundation_isolated_wav_sha256': base_hash,
                  'mix_gain_db': 0.0, 'foundation_gain_db': 0.0}
    if args.additional_source_file:
        extra_decoded = decode_reactor(ffmpeg, args.additional_source_file)
        extra, extra_metadata = prepare_additional_reactor(extra_decoded)
        result, mix_metadata = mix_reactor_layers(result, extra)
        metadata = loop_statistics(result)
        layers.append({
            'name': 'additional_spaceship_reactor', 'source': args.additional_source_file.name,
            'source_sha256': sha256(args.additional_source_file),
            'source_duration_seconds': len(extra_decoded) / RATE,
            'bus': 'additional_reactor_only',
            'repetitions_in_eight_second_output': mix_metadata['additional_repetitions'],
            'headroom_gain_db': mix_metadata['additional_headroom_gain_db'],
            **extra_metadata,
        })
        sources.append({'file': args.additional_source_file.name,
                        'sha256': sha256(args.additional_source_file),
                        'duration_seconds': len(extra_decoded) / RATE})
        processing.update({'additional_parameters': ADDITIONAL_PARAMETERS, **mix_metadata})
    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_wav(targets[0], result)
    manifest = {
        'schema_version': 2,
        'files': [{
            'file': targets[0].name, 'role': 'reactor', 'source': args.source_file.name,
            'source_sha256': sha256(args.source_file), 'source_duration_seconds': len(decoded) / RATE,
            'sample_rate': RATE, 'channels': 1, 'bits_per_sample': 16,
            'samples': len(result), 'duration_seconds': len(result) / RATE,
            'loop': True, **metadata, 'layers': layers, 'sha256': sha256(targets[0]),
        }],
        'sources': sources,
        'processing': processing,
        'decoder_version': subprocess.check_output([str(ffmpeg), '-version'], text=True).splitlines()[0],
        'provenance': 'User-selected local reactor recording(s). Per-source filenames and SHA-256 identify inputs; licensing and redistribution rights have not been independently verified.',
        'review_status': 'Original kernreaktor loop retained exactly as an isolated foundation. Optional added layer selected from waveform energy only; in-game listening and balance review pending.',
        'notes': 'Original kernreaktor.mp3 crop8-16.8s/800ms circular overlap and .055 maximum80msRMS unchanged. Optional spaceship crop.8-5.6s/800ms circular overlap gives4s at natural speed, tiled twice over8s, maximum80msRMS .035. Only the new layer receives fixed headroom attenuation if necessary; no global normalization, foundation ducking, time stretch or pitch change. Isolated hashes describe individual layers, not the composite WAV. Existing runtime phase, position, pause, parked restoration and .75 gain apply to the resulting8s loop without runtime changes. Preview contains three complete8s cycles.',
    }
    targets[1].write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    if args.preview:
        write_wav(args.preview, np.tile(result, 3))
    print(f'Prepared {len(result) / RATE:.2f} s mono PCM16 reactor loop; listening review pending.')
    print(json.dumps(metadata, indent=2))


if __name__ == '__main__':
    main()
