"""Compose Robot -> Auto around the exact user-approved A growl.

Requires numpy and FFmpeg. Only return.wav and return_manifest.json are managed
in the output folder; all approved step, turn, deploy and reactor assets remain
untouched. Growl, reverse-pose accents and new movement textures use separate
buses. The five-second animation has a .3-second audio tail.
"""
import argparse
import json
from pathlib import Path
import subprocess
from tempfile import TemporaryDirectory

import numpy as np

from prepare_transformer_audio import (
    RATE, ROOT, decode, find_ffmpeg, max_window_rms, sha256, write_wav,
)
from prepare_transformer_deploy_audio import (
    GIANT, GROWL, HEAVY, LOCK, STEP_016, STEP_330678,
    constrain_lock_bus, locking_cue, prepare_layer,
)
from prepare_transformer_mechanical_audio import (
    RETURN_TEXTURES, place_layers, wav_sha256,
)


SOURCE_SHA256 = 'df901e4ac5c0b74c5db9ba19d66f14dc8a914c1adfc9aa6374a04e906cd647e5'
SELECTED_A_SHA256 = '86fd09bd9ab2180585af57cf3902bd985b849c9fe9a23bf035f032341ee7b8e4'
PARAMETERS = {
    'selection_label': 'A',
    'source_start_seconds': 6.0,
    'source_end_seconds': 10.2,
    'fade_in_seconds': 0.04,
    'fade_out_seconds': 0.25,
    'normalization_window_seconds': 0.08,
    'target_max_window_rms': 0.16,
    'peak_ceiling': 0.78,
    'animation_seconds': 5.0,
    'onset_seconds': 0.0,
    'time_stretch': False,
    'pitch_change': False,
}

# Measured against car frame 0 while traversing all 241 baked frames backwards.
# Early cues are component endpoints; only the final cue closes the whole car.
# Nearby paired events are grouped to avoid a separate impact for every panel.
RETURN_LOCKS = [
    locking_cue('mirror_car_seating', HEAVY, (0.08975056689342403, 0.31),
                102 / 48, 0.16, 'mirrors reach local car pose; restrained first component of a staggered mechanical accent',
                [24, 53], 'complete_local_car_pose'),
    locking_cue('head_compaction', GIANT, (0.24, 0.54),
                107 / 48, 0.22, 'head rotation and compaction end; moderate second component, carrier continues',
                [3], 'world_rotation_and_scale'),
    locking_cue('lower_joint_fold', STEP_016, (0.4688888888888889, 0.74),
                142 / 48, 0.25, 'feet toes and shin panels finish world rotation',
                [99, 125, 100, 126, 104, 130], 'world_rotation'),
    locking_cue('floor_panel_car_seating', HEAVY, (0.5548299319727891, 0.80),
                172 / 48, 0.30, 'floor0 panels reach complete world car pose; nearby hand scale settles at 3.625 s without a duplicate impact',
                [114, 115, 116, 117, 118, 119, 140, 141, 142, 143, 144, 145],
                'complete_world_car_pose'),
    locking_cue('foot_retraction', STEP_330678, (0.5836281179138322, 0.89),
                195 / 48, 0.30, 'feet finish retraction scale; carriers continue',
                [99, 125], 'world_scale'),
    locking_cue('limb_assembly_seating', LOCK, (0.24, 0.53),
                216 / 48, 0.34, 'arms legs and hip belt reach complete local car pose',
                [2, 4, 5, 6, 33, 34, 35, 97, 98, 99, 123, 124, 125, 82, 83, 151, 154, 155],
                'complete_local_car_pose'),
    locking_cue('quarterglass_car_seating', HEAVY, (1.5548299319727892, 1.80),
                224 / 48, 0.27, 'quarterglass reaches complete local car pose',
                [64, 65], 'complete_local_car_pose'),
    locking_cue('final_car_body_lock', LOCK, (0.24, 0.57),
                5.0, 0.43, 'large body panels wheels roof hood and hatch reach car endpose',
                [30, 59, 31, 60, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 105, 131],
                'complete_car_pose', fade_out=0.12),
]


def prepare_return(samples):
    """Apply the exact crop, fades and gain from the approved A comparison."""
    start = round(PARAMETERS['source_start_seconds'] * RATE)
    end = round(PARAMETERS['source_end_seconds'] * RATE)
    if not 0 <= start < end <= len(samples):
        raise ValueError('Source does not contain the selected A return growl')
    result = samples[start:end].copy()
    result -= np.mean(result)
    fade_in = round(PARAMETERS['fade_in_seconds'] * RATE)
    fade_out = round(PARAMETERS['fade_out_seconds'] * RATE)
    result[:fade_in] *= np.linspace(0, 1, fade_in)
    result[-fade_out:] *= np.linspace(1, 0, fade_out)
    raw_peak = float(np.max(np.abs(result)))
    raw_rms = max_window_rms(result)
    if not np.isfinite(result).all() or min(raw_peak, raw_rms) < 0.001:
        raise ValueError('Silent, damaged or nonfinite return growl selection')
    gain = min(PARAMETERS['target_max_window_rms'] / raw_rms,
               PARAMETERS['peak_ceiling'] / raw_peak)
    result *= gain
    return result, {
        'selection_label': 'A',
        'crop_start_sample': start,
        'crop_end_sample_exclusive': end,
        'crop_start_seconds': start / RATE,
        'crop_end_seconds': end / RATE,
        'fade_in_seconds': fade_in / RATE,
        'fade_out_seconds': fade_out / RATE,
        'gain_db': float(20 * np.log10(gain)),
        'peak': float(np.max(np.abs(result))),
        'max_80ms_window_rms': max_window_rms(result),
        'first_100ms_rms': float(np.sqrt(np.mean(result[:round(0.1 * RATE)] ** 2))),
    }


def compose(decoded):
    growl, growl_metadata = prepare_return(decoded[GROWL])
    if wav_sha256(growl) != SELECTED_A_SHA256:
        raise ValueError('Return growl differs from approved A; check source and decoder')
    foundation = np.zeros(round(5.3 * RATE))
    foundation[:len(growl)] = growl
    lock_bus, locks = place_layers(decoded, RETURN_LOCKS, len(foundation), prepare_layer)
    constrained, locks_processing = constrain_lock_bus(lock_bus, foundation)
    foundation += constrained
    textures, texture_metadata = place_layers(decoded, RETURN_TEXTURES, len(foundation), prepare_layer)
    added, texture_processing = constrain_lock_bus(textures, foundation)
    texture_processing['type'] = 'movement_bus_only_continuous_soft_knee'
    texture_processing['preserves_growl_and_lock_foundation'] = True
    growl_metadata.update({
        'name': 'approved_return_growl_A', 'source': GROWL,
        'bus': 'preserved_growl', 'onset_seconds': 0.0,
        'end_seconds': len(growl) / RATE, 'isolated_wav_sha256': wav_sha256(growl),
    })
    return foundation + added, [growl_metadata] + locks + texture_metadata, {
        'lock_bus_constraint': locks_processing,
        'movement_bus_constraint': texture_processing,
        'approved_growl_isolated_wav_sha256': wav_sha256(growl),
        'growl_gain_db': growl_metadata['gain_db'],
        'mix_gain_db': 0.0,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-file', type=Path,
                        help='User-selected transformers-growling-31509.mp3 recording')
    parser.add_argument('--source-dir', type=Path,
                        help='Folder with selected growl, approved lock and new movement recordings')
    parser.add_argument('--ffmpeg', type=Path)
    parser.add_argument('--output-dir', type=Path, default=ROOT / '.local/audio-transformer')
    parser.add_argument('--preview', type=Path,
                        help='Optional byte-identical standalone WAV outside managed output')
    args = parser.parse_args()
    if args.source_dir is None:
        if args.source_file is None:
            parser.error('Provide --source-dir or --source-file')
        args.source_dir = args.source_file.parent
    if args.source_file is None:
        args.source_file = args.source_dir / GROWL
    if not args.source_file.is_file():
        raise ValueError('Missing return growl source recording')
    source_hash = sha256(args.source_file)
    if source_hash != SOURCE_SHA256:
        raise ValueError('Source differs from the recording used for approved candidate A')
    target = args.output_dir / 'return.wav'
    manifest_path = args.output_dir / 'return_manifest.json'
    outputs = [target, manifest_path] + ([args.preview] if args.preview else [])
    if any(path.is_symlink() for path in outputs):
        raise ValueError('Refusing symlink output')
    if args.preview and args.preview.resolve().parent == args.output_dir.resolve():
        raise ValueError('Preview must be outside the managed output folder')
    sources = {GROWL} | {cue['source'] for cue in RETURN_LOCKS + RETURN_TEXTURES}
    source_paths = {name: args.source_file if name == GROWL else args.source_dir / name
                    for name in sources}
    resolved_sources = {path.resolve() for path in source_paths.values()}
    if any(path.resolve() in resolved_sources for path in outputs):
        raise ValueError('Outputs must be separate from all source recordings')
    if any(not path.is_file() for path in source_paths.values()):
        raise ValueError('Missing return soundtrack source recording')
    ffmpeg = find_ffmpeg(args.ffmpeg)
    decoded = {name: decode(ffmpeg, path) for name, path in source_paths.items()}
    samples, layers, constraint = compose(decoded)
    # Check reproduction before replacing any managed asset: a different decoder
    # must not silently change the excerpt the user selected in the comparison.
    with TemporaryDirectory(prefix='nfvu2-return-a-') as temporary:
        prepared = Path(temporary) / 'return.wav'
        write_wav(prepared, samples)
        output_hash = sha256(prepared)
        wav_bytes = prepared.read_bytes()
    manifest = {
        'schema_version': 2,
        'files': [{
            'file': target.name, 'role': 'transform_return',
            'source': args.source_file.name, 'source_sha256': source_hash,
            'source_duration_seconds': len(decoded[GROWL]) / RATE,
            'sample_rate': RATE, 'channels': 1, 'bits_per_sample': 16,
            'samples': len(samples), 'duration_seconds': len(samples) / RATE,
            'loop': False, 'onset_seconds': 0.0,
            'sha256': output_hash, 'peak': float(np.max(np.abs(samples))),
            'max_window_rms': max_window_rms(samples), 'layers': layers,
        }],
        'sources': [{'file': name, 'sha256': sha256(path),
                     'duration_seconds': len(decoded[name]) / RATE}
                    for name, path in sorted(source_paths.items())],
        'processing': {
            **PARAMETERS, 'tail_seconds': 0.3, 'mix_peak_ceiling': 0.95, **constraint,
            'operations': ['mono float decode at 44100 Hz', 'source crop',
                           'DC removal', 'linear fades', 'gain only',
                           'align accent emphasis to measured reverse car-pose milestones',
                           'add separately headroom-limited lock and movement buses',
                           'round to PCM16'],
        },
        'selected_comparison': {
            'label': 'A', 'filename': 'A_start6s.wav',
            'manifest': 'artifacts/audio-transformer/reverse-growl-candidates/manifest.json',
            'sha256': SELECTED_A_SHA256, 'byte_identical': True,
            'scope': 'isolated approved A growl foundation only; combined return.wav contains additional layers',
        },
        'decoder_version': subprocess.check_output(
            [str(ffmpeg), '-version'], text=True).splitlines()[0],
        'provenance': 'User-selected locally downloaded growl recording. Filename and SHA-256 identify the input; licensing and redistribution rights have not been independently verified.',
        'review_status': 'A growl, eight reverse accents and moving-tools retained; all three inside layers replaced by isolated .20-.22s attack impulses after user feedback. Listening/in-game balance review pending.',
        'timing_basis': 'Start at TransformerState.Returning, after braking, foot settling and terrain preparation. Existing five-second animation clock; .3-second audio tail. Reverse milestones measured against original frame0 car pose, not mirrored forward endpoints. Report artifacts/audio-transformer/reverse-lock-events.json; baked SHA256 0868ec0ad0399566869a92552aed6b4f1316599ed6350de4fa4a40d20e38ffd9.',
        'notes': 'Approved A growl crop/gain/fades retained exactly as isolated foundation. Eight existing grouped return accents and both moving-tools clips retain all cuts/gain/fades/timing. All three inside layers now use isolated .20-.22s attacks identified by 5ms-envelope rise, with 6ms fade-in and 65ms fade-out; sustained inside hum fragments removed. Source intervals differ from deploy. Bass is added only in deploy. No global normalization, Morph, time stretch or pitch changes. Existing engine/reactor transitions remain runtime controlled. Signal analysis is not a claim of having heard the new mix.',
    }
    args.output_dir.mkdir(parents=True, exist_ok=True)
    target.write_bytes(wav_bytes)
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    if args.preview:
        args.preview.parent.mkdir(parents=True, exist_ok=True)
        args.preview.write_bytes(wav_bytes)
    print(f'Prepared Robot -> Auto mix with preserved A: {len(samples) / RATE:.2f}s, SHA-256 {output_hash}')


if __name__ == '__main__':
    main()
