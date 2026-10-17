"""Compose the local Auto -> Robot soundtrack from user-selected recordings.

Requires numpy and FFmpeg. Only deploy.wav and deploy_manifest.json are managed
here; approved footsteps and turning sounds are untouched. This waveform-based
remix requires listening/in-game review; the approved growl excerpt and gain
are preserved, with the earlier onset requested by the user.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import subprocess

import numpy as np

from prepare_transformer_audio import RATE, ROOT, decode, find_ffmpeg, sha256, write_wav
from prepare_transformer_mechanical_audio import (
    BASS, DEPLOY_BASS, DEPLOY_TEXTURES, decode_bass, inside_impulse, place_layers, wav_sha256,
)


GROWL = 'freesound_community-transformers-growling-31509.mp3'
LOCK = 'audiopapkin-big-robot-footstep-020-500843.mp3'
GIANT = 'idoberg-giant-robot-in-space-352492.mp3'
HEAVY = 'daviddumaisaudio-robot-heavy-mechanical-footsteps-194039.mp3'
STEP_330678 = 'audiopapkin-big-robot-footstep-330678.mp3'
STEP_016 = 'audiopapkin-big-robot-footstep-016-445102.mp3'
INSIDE = 'freesound_community-029974_inside-the-robot-70923.mp3'
LASER = 'gregorquendel-laser-charge-175727.mp3'
ANIMATION_SECONDS = 5.0
TAIL_SECONDS = 0.3
PEAK_CEILING = 0.95
LAYER_PEAK_CEILING = 0.90
WINDOW_SECONDS = 0.08
LOCK_KNEE_FRACTION = 0.86
SUBDROP = 'brvhrtz-sub-drop-short-232033.mp3'
PRIOR_25_LAYER_SHA256 = '97d60be78204d9a2a50be2d2c6564fe94e20ed361b9f2879d74452157a1e3b40'
SUBDROP_CUE = dict(
    name='late_subdrop', source=SUBDROP, crop=(1.4, 2.95), onset=3.75,
    target_rms=0.18, fade_in=0.01, fade_out=0.08,
    stage='late natural descending bass tail; source ends quietly without time compression',
    bus='additional_late_subdrop',
)


def locking_cue(name, source, crop, contact, target_rms, stage, nodes,
                component, fade_out=0.09):
    """Align the crop's strongest 20 ms block with a baked-pose milestone."""
    return dict(name=name, source=source, crop=crop, contact_seconds=contact,
                target_rms=target_rms, fade_in=0.003, fade_out=fade_out,
                stage=stage, nodes=nodes, component=component)


# Authoritative animation time, unchanged five-second / 241-frame baked clip.
# Orientation/extension endpoints below are JOINT milestones: their carriers
# continue moving. Complete part/assembly seating only clusters near 4.85-5 s.
# Verified against world TRS / relative-parent matrices over all remaining
# frames: rotation <= .1 deg, scale delta <= .001, local matrix delta <= .002.
# Each paired left/right group receives one accent; no invented alternating
# footfalls or staggered animation events. Original bright 015 is excluded.
CUES = [
    dict(name='opening_mechanism', source=GIANT, crop=(0.0, 0.40),
         onset=0.08, target_rms=0.055, fade_in=0.008, fade_out=0.12,
         stage='opening/unlocking; original quiet lead-in retained'),
    # Same approved excerpt/gain; user requests an immediate rather than .85 s onset.
    dict(name='rising_growl', source=GROWL, crop=(0.75, 4.25),
         onset=0.0, target_rms=0.16, fade_in=0.04, fade_out=0.35,
         stage='body expands and rises; approved 3.5 s source excerpt, unchanged speed'),
    # Waveform-selected short textures while assemblies move, not invented locks.
    # End these before final seating so the twelve stronger cues remain distinct.
    dict(inside_impulse('inside_initial_panels', (0.773, 0.973), 0.22, 0.075), bus='support'),
    dict(inside_impulse('inside_body_extension', (10.441, 10.641), 1.10, 0.085), bus='support'),
    dict(inside_impulse('inside_joint_movement', (12.477, 12.677), 2.10, 0.085), bus='support'),
    dict(inside_impulse('inside_upper_assembly', (27.812, 28.032), 3.52, 0.075), bus='support'),
    dict(name='laser_charge_rise', source=LASER, crop=(1.45, 4.25),
         onset=0.35, target_rms=0.12, fade_in=0.09, fade_out=0.35,
         stage='test energy rise alongside growl; source growth retained without stretching',
         bus='support'),
    locking_cue('boot_joint_rotation', HEAVY, (0.08975056689342403, 0.31),
                116 / 48, 0.18,
                'boot orientation reaches its final angle; leg carrier still translates',
                [99, 125], 'world_rotation'),
    locking_cue('rear_arch_joint_rotation', STEP_016, (0.4688888888888889, 0.73),
                120 / 48, 0.18,
                'paired rear-arch orientation ends; full panel poses are still moving',
                [101, 127], 'world_rotation'),
    locking_cue('quarterglass_joint_rotation', HEAVY, (0.5548299319727891, 0.80),
                144 / 48, 0.22,
                'quarterglass joint rotation ends; final translation continues',
                [64, 65], 'world_rotation'),
    locking_cue('foot_extension', STEP_330678, (0.5836281179138322, 0.89),
                157 / 48, 0.32,
                'paired foot extension settles; carriers continue toward final position',
                [99, 125], 'world_scale'),
    locking_cue('core_hand_joint_alignment', LOCK, (0.24, 0.53),
                170 / 48, 0.36,
                'pelvis width and hand joint orientation settle; whole body still moves',
                [1, 7, 36], 'world_scale_and_rotation'),
    locking_cue('hand_extension', STEP_016, (0.4688888888888889, 0.76),
                181 / 48, 0.30,
                'paired hand extension ends; arms still translate',
                [7, 36], 'world_scale'),
    locking_cue('rear_wheel_joint_rotation', STEP_016, (0.4688888888888889, 0.74),
                210 / 48, 0.25,
                'rear wheel orientation and head extension end together; carriers still translate',
                [105, 131, 3], 'world_rotation_and_scale'),
    locking_cue('head_joint_alignment', GIANT, (0.24, 0.54),
                215 / 48, 0.34,
                'head orientation settles after extension; final translation remains',
                [3], 'world_rotation'),
    locking_cue('hatch_spoiler_joint_rotation', HEAVY, (1.5548299319727892, 1.80),
                228 / 48, 0.26,
                'hatch and spoiler rotation ends; final translation continues',
                [74, 75], 'world_rotation'),
    locking_cue('leg_assembly_seating', STEP_330678, (0.5836281179138322, 0.83),
                233 / 48, 0.23,
                'feet reach complete local pose; thighs already settled one frame earlier',
                [97, 123, 99, 125], 'complete_local_pose'),
    locking_cue('hand_assembly_seating', HEAVY, (0.08975056689342403, 0.28),
                235 / 48, 0.22,
                'hands reach complete local pose; forearms and doorfront panels follow next frame',
                [7, 36], 'complete_local_pose', fade_out=0.07),
    locking_cue('final_panel_lock', LOCK, (0.24, 0.57),
                5.0, 0.43,
                'final panel seating and complete deployed pose; floor rows finish one frame earlier',
                [2, 3, 4, 33, 32, 61, 105, 131, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81],
                'complete_deployed_pose', fade_out=0.12),
]


def max_window_rms(samples):
    size = min(round(WINDOW_SECONDS * RATE), len(samples))
    energy = np.concatenate(([0.0], np.cumsum(samples * samples)))
    return float(np.sqrt(np.max(energy[size:] - energy[:-size]) / size))


def prepare_layer(samples, cue):
    source_start, source_end = (round(value * RATE) for value in cue['crop'])
    origin = round(cue.get('source_origin_seconds', 0.0) * RATE)
    start, end = source_start - origin, source_end - origin
    if not 0 <= start < end <= len(samples):
        raise ValueError(f"Source does not contain selection: {cue['name']}")
    result = samples[start:end].copy()
    result -= np.mean(result)
    fade_in = min(round(cue['fade_in'] * RATE), len(result) // 2)
    fade_out = min(round(cue['fade_out'] * RATE), len(result) // 2)
    result[:fade_in] *= np.linspace(0, 1, fade_in)
    result[-fade_out:] *= np.linspace(1, 0, fade_out)
    raw_peak = float(np.max(np.abs(result)))
    raw_rms = max_window_rms(result)
    if not np.isfinite(result).all() or min(raw_peak, raw_rms) < 0.001:
        raise ValueError(f"Silent or damaged selection: {cue['name']}")
    # The approved growl never reaches this ceiling; its original gain is exact.
    gain = min(cue['target_rms'] / raw_rms, LAYER_PEAK_CEILING / raw_peak)
    result *= gain
    evidence_block = round(0.02 * RATE)
    complete_blocks = len(result) // evidence_block
    envelope = np.sqrt(np.mean(result[:complete_blocks * evidence_block].reshape(
        -1, evidence_block) ** 2, axis=1))
    emphasis_sample = int(np.argmax(envelope)) * evidence_block
    onset_sample = (round(cue['contact_seconds'] * RATE) - emphasis_sample
                    if 'contact_seconds' in cue else round(cue['onset'] * RATE))
    return result, {
        'name': cue['name'], 'source': cue['source'], 'stage': cue['stage'],
        'baked_node_indices': cue.get('nodes', []),
        'milestone_component': cue.get('component', 'sound_design_layer'),
        'contact_seconds': cue.get('contact_seconds'),
        'baked_frame_48fps': (round(cue['contact_seconds'] * 48)
                              if 'contact_seconds' in cue else None),
        'crop_start_sample': source_start, 'crop_end_sample_exclusive': source_end,
        'crop_start_seconds': source_start / RATE, 'crop_end_seconds': source_end / RATE,
        'decoded_source_origin_seconds': origin / RATE,
        'onset_sample': onset_sample, 'onset_seconds': onset_sample / RATE,
        'end_seconds': (onset_sample + len(result)) / RATE,
        'strongest_20ms_block_offset_seconds': emphasis_sample / RATE,
        'strongest_20ms_block_timeline_seconds': (onset_sample + emphasis_sample) / RATE,
        'fade_in_seconds': fade_in / RATE, 'fade_out_seconds': fade_out / RATE,
        'target_max_window_rms': cue['target_rms'],
        'gain_db_before_mix_attenuation': float(20 * np.log10(gain)),
        'peak_before_mix_attenuation': float(np.max(np.abs(result))),
        'max_window_rms_before_mix_attenuation': max_window_rms(result),
        'isolated_wav_sha256': wav_sha256(result),
    }


def constrain_lock_bus(lock_bus, growl):
    """Soft-knee an added bus into the headroom left by its preserved foundation.

    Values below 86% of available headroom are unchanged. The continuous,
    unit-slope knee smoothly approaches the peak ceiling; no complete-mix
    normalization, growl ducking or alteration of the animation clock occurs.
    """
    magnitude = np.abs(lock_bus)
    available = PEAK_CEILING - np.sign(lock_bus) * growl
    knee = LOCK_KNEE_FRACTION * available
    exceeds = magnitude > knee
    result = lock_bus.copy()
    room = available[exceeds] - knee[exceeds]
    result[exceeds] = np.sign(lock_bus[exceeds]) * (
        knee[exceeds] + room * np.tanh((magnitude[exceeds] - knee[exceeds]) / room))
    reduction = np.ones_like(result)
    active = magnitude > 1e-12
    reduction[active] = np.abs(result[active]) / magnitude[active]
    return result, {
        'type': 'lock_bus_only_continuous_soft_knee',
        'knee_fraction_of_available_headroom': LOCK_KNEE_FRACTION,
        'affected_samples': int(np.count_nonzero(exceeds)),
        'affected_fraction': float(np.mean(exceeds)),
        'maximum_attenuation_db': float(20 * np.log10(np.min(reduction))),
        'growl_gain_db': 0.0,
    }


def compose_foundation(decoded):
    growl = np.zeros(round((ANIMATION_SECONDS + TAIL_SECONDS) * RATE))
    lock_bus = np.zeros_like(growl)
    support_bus = np.zeros_like(growl)
    layers = []
    for cue in CUES:
        samples, metadata = prepare_layer(decoded[cue['source']], cue)
        start = metadata['onset_sample']
        end = start + len(samples)
        if start < 0 or end > len(growl):
            raise ValueError(f"Cue exceeds soundtrack: {cue['name']}")
        bus = (growl if cue['name'] == 'rising_growl' else
               support_bus if cue.get('bus') == 'support' else lock_bus)
        bus[start:end] += samples
        metadata['bus'] = ('preserved_growl' if bus is growl else
                           'support_textures' if bus is support_bus else 'mechanical_locks')
        metadata['effective_gain_db'] = metadata['gain_db_before_mix_attenuation']
        layers.append(metadata)
    constrained, processing = constrain_lock_bus(lock_bus, growl)
    foundation = growl + constrained
    support, support_processing = constrain_lock_bus(support_bus, foundation)
    support_processing['type'] = 'support_bus_only_continuous_soft_knee'
    support_processing['preserves_growl_and_lock_foundation'] = True
    mix = foundation + support
    processing.update({
        'lock_bus_peak_before_constraint': float(np.max(np.abs(lock_bus))),
        'lock_bus_peak_after_constraint': float(np.max(np.abs(constrained))),
        'lock_bus_max_window_rms_before_constraint': max_window_rms(lock_bus),
        'lock_bus_max_window_rms_after_constraint': max_window_rms(constrained),
        'growl_peak': float(np.max(np.abs(growl))),
        'growl_max_window_rms': max_window_rms(growl),
        'growl_first_100ms_rms': float(np.sqrt(np.mean(growl[:round(0.1 * RATE)] ** 2))),
        'support_bus_peak_before_constraint': float(np.max(np.abs(support_bus))),
        'support_bus_peak_after_constraint': float(np.max(np.abs(support))),
        'support_bus_constraint': support_processing,
        'mix_first_100ms_rms': float(np.sqrt(np.mean(mix[:round(0.1 * RATE)] ** 2))),
    })
    return mix, layers, processing


def compose_before_subdrop(decoded):
    """Short inside impulses replace sustained crops; add bass on its own bus."""
    foundation, layers, processing = compose_foundation(decoded)
    textures, metadata = place_layers(decoded, DEPLOY_TEXTURES, len(foundation), prepare_layer)
    added, constraint = constrain_lock_bus(textures, foundation)
    constraint['type'] = 'additional_movement_bus_only_continuous_soft_knee'
    constraint['preserves_current_growl_lock_laser_foundation'] = True
    processing['revised_short_inside_foundation_sha256'] = wav_sha256(foundation)
    processing['additional_movement_bus_constraint'] = constraint
    processing['additional_movement_bus_peak'] = float(np.max(np.abs(added)))
    base = foundation + added
    bass, bass_metadata = place_layers(decoded, [DEPLOY_BASS], len(base), prepare_layer)
    limited_bass, bass_constraint = constrain_lock_bus(bass, base)
    bass_constraint['type'] = 'additional_bass_bus_only_continuous_soft_knee'
    bass_constraint['preserves_growl_locks_laser_and_mechanics'] = True
    processing['additional_bass_bus_constraint'] = bass_constraint
    processing['inside_change'] = 'All six forward inside layers replaced by isolated .20-.22s attack impulses; previous full deploy mix is intentionally changed.'
    return base + limited_bass, layers + metadata + bass_metadata, processing


def compose(decoded):
    """Keep all 25 previous layers intact and add only the late drop bus."""
    foundation, layers, processing = compose_before_subdrop(decoded)
    prior_hash = wav_sha256(foundation)
    if prior_hash != PRIOR_25_LAYER_SHA256:
        raise ValueError('Previous 25-layer deploy foundation differs; check sources and decoder')
    subdrop, metadata = place_layers(decoded, [SUBDROP_CUE], len(foundation), prepare_layer)
    added, constraint = constrain_lock_bus(subdrop, foundation)
    constraint['type'] = 'late_subdrop_bus_only_continuous_soft_knee'
    constraint['preserves_complete_previous_25_layer_mix'] = True
    processing['prior_25_layer_foundation_sha256'] = prior_hash
    processing['additional_late_subdrop_bus_constraint'] = constraint
    processing['additional_late_subdrop_bus_peak'] = float(np.max(np.abs(added)))
    return foundation + added, layers + metadata, processing


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-dir', required=True, type=Path,
                        help='Folder with the user-selected mechanical/growl and approved step MP3s')
    parser.add_argument('--ffmpeg', type=Path)
    parser.add_argument('--output-dir', type=Path, default=ROOT / '.local/audio-transformer')
    parser.add_argument('--preview', type=Path,
                        help='Optional identical standalone mix WAV, outside managed output')
    args = parser.parse_args()
    sources = {cue['source'] for cue in CUES + DEPLOY_TEXTURES + [DEPLOY_BASS, SUBDROP_CUE]}
    missing = [name for name in sorted(sources) if not (args.source_dir / name).is_file()]
    if missing:
        raise ValueError('Missing source recordings: ' + ', '.join(missing))
    if args.preview and (args.preview.resolve().parent == args.output_dir.resolve() or
                         args.preview.resolve() in {(args.source_dir / name).resolve() for name in sources}):
        raise ValueError('Preview must be separate from sources and managed output folder')
    ffmpeg = find_ffmpeg(args.ffmpeg)
    decoded = {name: decode(ffmpeg, args.source_dir / name) for name in sorted(sources) if name != BASS}
    decoded[BASS], bass_source_metadata = decode_bass(ffmpeg, args.source_dir / BASS)
    mix, layers, constraint = compose(decoded)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    target = args.output_dir / 'deploy.wav'
    manifest_path = args.output_dir / 'deploy_manifest.json'
    if target.is_symlink() or manifest_path.is_symlink():
        raise ValueError('Refusing symlink soundtrack/manifest output')
    write_wav(target, mix)
    source_metadata = []
    for name in sorted(sources):
        path = args.source_dir / name
        source_metadata.append({
            'file': name, 'sha256': sha256(path),
            'duration_seconds': len(decoded[name]) / RATE,
            'source_file_modified_date_utc': datetime.fromtimestamp(
                path.stat().st_mtime, timezone.utc).date().isoformat(),
            **(bass_source_metadata if name == BASS else {}),
        })
    manifest = {
        'schema_version': 5,
        'generated_date_utc': datetime.now(timezone.utc).date().isoformat(),
        'files': [{
            'file': target.name, 'role': 'transform_deploy', 'sha256': sha256(target),
            'sample_rate': RATE, 'channels': 1, 'bits_per_sample': 16,
            'samples': len(mix), 'duration_seconds': len(mix) / RATE,
            'loop': False, 'peak': float(np.max(np.abs(mix))),
            'max_window_rms': max_window_rms(mix), 'layers': layers,
        }],
        'sources': source_metadata,
        'processing': {
            'animation_seconds': ANIMATION_SECONDS, 'tail_seconds': TAIL_SECONDS,
            'rms_window_seconds': WINDOW_SECONDS, 'peak_ceiling': PEAK_CEILING,
            'mix_gain_db': 0.0,
            'layer_peak_ceiling': LAYER_PEAK_CEILING,
            'lock_bus_constraint': constraint,
            'operations': ['mono float decode at 44100 Hz', 'source crop',
                           'per-layer DC removal', 'linear fades', 'per-layer gain',
                           'align lock emphasis to verified baked milestones',
                           'sum growl and mechanical buses separately',
                           'soft-knee mechanical bus only into remaining headroom',
                           'replace every sustained inside texture with an isolated short attack impulse',
                           'add separately constrained support textures without attenuating growl or lock foundation',
                           'decode bass source only at 75-78s and add its cropped effect on a separate final constrained bus',
                           'preserve the complete previous 25-layer mix and add a separately constrained late subdrop bus from 3.75 to 5.30s',
                           'round to PCM16'],
            'time_stretch': False, 'pitch_change': False,
        },
        'decoder_version': subprocess.check_output(
            [str(ffmpeg), '-version'], text=True).splitlines()[0],
        'provenance': 'Locally downloaded MP3s selected by the user. Filenames and SHA-256 identify sources; licensing and redistribution rights have not been independently verified.',
        'review_status': 'Complete previous 25-layer deploy mix retained. Additional late subdrop1.40-2.95s at3.75s prepared from descending source tail; in-game listening and balance review pending.',
        'waveform_evidence': 'All previous 25 cues and their bus processing retained. New subdrop source lasts3.31755s, with active signal until about2.9s. Last descending section1.40-2.95s preserves the natural quiet ending;200ms FFT windows fall from35.5Hz at1.4s to28.5Hz at2.65s.100ms source RMS drops from about.64 to.020 at2.8s and.0003 at2.9s. A10ms onset fade and80ms final fade avoid cut edges without time/pitch change. Added only at3.75s on its own final headroom-constrained bus; previous whole-mix PCM hash is recorded and verified. Numerical evidence does not establish perceived sound quality.',
        'timing_basis': 'tools/build_transformer_cinematic.py and baked 241-frame / 48 Hz TRS analysis. World orientation endpoint uses <=.1 degree remaining delta; world extension endpoint <=.001 scale-component delta; complete relative-parent pose <=.002 maximum matrix delta against all remaining frames. Node indices and component labels are recorded per cue. Whole panel seating clusters near 4.85-5.00 s; earlier cues denote real orientation/extension endpoints while carriers continue moving. No animation changes or invented alternating contacts.',
        'timing_evidence': {
            'baked_payload_sha256': '0868ec0ad0399566869a92552aed6b4f1316599ed6350de4fa4a40d20e38ffd9',
            'local_analysis_report': 'artifacts/audio-transformer/panel-lock-events.json',
            'sampled_frames': 241, 'frames_per_second': 48,
            'world_orientation_tolerance_degrees': 0.1,
            'world_scale_absolute_tolerance': 0.001,
            'relative_parent_matrix_max_entry_tolerance': 0.002,
            'criterion': 'All subsequent frames remain within tolerance of the final pose; a joint milestone does not imply a stopped carrier or a physical collision.',
        },
        'notes': 'Auto -> Robot only. Entire previous25-layer mix, including early1.9s bass, six short inside impulses, twelve locks, growl, laser and moving-tools, is retained exactly as the foundation. A late1.55s subdrop from source1.40-2.95s starts3.75s and finishes5.30s. Only that new bus is headroom-constrained; no global attenuation, pitch change or time stretching. Existing return, step, turn and layered reactor outputs untouched; .3-second tail and five-second animation unchanged.',
    }
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    if args.preview:
        if args.preview.is_symlink():
            raise ValueError('Refusing symlink preview output')
        write_wav(args.preview, mix)
    print(f'Prepared {len(mix) / RATE:.2f}s Auto -> Robot remix; immediate growl and new test textures, auditory review pending.')
    print('Cue onsets: ' + ', '.join(f"{layer['name']}={layer['onset_seconds']:.2f}s" for layer in layers))


if __name__ == '__main__':
    main()
