"""Additional local transformation textures, chosen from signal activity only.

Separate source intervals distinguish unfolding and folding. These movement
textures are not assertions of physical hydraulic recordings or exact locks.
Auditory judgement and final balance belong to the in-game listening test.
"""
import hashlib
import io
import re
import subprocess
import wave

import numpy as np

from prepare_transformer_audio import RATE


MOVING = 'freesound_community-moving-tools-3-40372.mp3'
INSIDE = 'freesound_community-029974_inside-the-robot-70923.mp3'
BASS = 'imgmidi-moving-bass-sfx-set-336212.mp3'
BASS_DECODE_START = 75.0
BASS_DECODE_SECONDS = 3.0


def texture(name, source, crop, onset, target=0.105):
    return dict(name=name, source=source, crop=crop, onset=onset,
                target_rms=target, fade_in=0.025, fade_out=0.12,
                stage='active movement texture; waveform-selected, auditory review pending',
                bus='new_movement_textures')


def inside_impulse(name, crop, onset, target):
    """Short attack-centred excerpt, not a sustained fragment of robot hum."""
    cue = texture(name, INSIDE, crop, onset, target)
    cue.update(fade_in=0.006, fade_out=0.065,
               stage='isolated 5ms-envelope attack impulse; short tail, auditory review pending')
    return cue


DEPLOY_TEXTURES = [
    texture('tools_unfold_initial', MOVING, (0.42, 1.07), 0.08, 0.11),
    inside_impulse('inside_unfold_chassis', (1.536, 1.736), 0.78, 0.10),
    texture('tools_unfold_extension', MOVING, (1.55, 2.30), 1.70, 0.12),
    inside_impulse('inside_unfold_upper', (7.528, 7.728), 2.80, 0.11),
    texture('tools_unfold_final_motion', MOVING, (3.05, 3.75), 3.85, 0.10),
]

RETURN_TEXTURES = [
    inside_impulse('inside_fold_release', (11.794, 11.994), 0.05, 0.10),
    texture('tools_fold_retraction', MOVING, (4.43, 5.05), 0.82, 0.12),
    inside_impulse('inside_fold_lower', (15.545, 15.745), 1.67, 0.11),
    texture('tools_fold_upper', MOVING, (5.55, 6.19), 2.68, 0.12),
    inside_impulse('inside_fold_body', (26.300, 26.520), 3.78, 0.11),
]

DEPLOY_BASS = dict(
    name='moving_bass_75s', source=BASS, crop=(75.78, 77.68), onset=0.08,
    source_origin_seconds=BASS_DECODE_START,
    target_rms=0.16, fade_in=0.025, fade_out=0.18,
    stage='active bass effect after requested 75s point; natural duration, auditory review pending',
    bus='additional_bass',
)


def decode_bass(ffmpeg, source):
    """Decode only 75..78s; do not relax the shared short-recording guard."""
    probe = subprocess.run([str(ffmpeg), '-hide_banner', '-i', str(source)],
                           capture_output=True, text=True, check=False)
    match = re.search(r'Duration: (\d+):(\d+):(\d+(?:\.\d+)?)', probe.stderr)
    if match is None:
        raise ValueError('Cannot determine original bass recording duration')
    hours, minutes, seconds = map(float, match.groups())
    duration = hours * 3600 + minutes * 60 + seconds
    if duration < DEPLOY_BASS['crop'][1]:
        raise ValueError('Bass source is shorter than the requested 75s excerpt')
    raw = subprocess.check_output([
        str(ffmpeg), '-v', 'error', '-i', str(source),
        '-ss', str(BASS_DECODE_START), '-t', str(BASS_DECODE_SECONDS),
        '-f', 'f32le', '-ac', '1', '-ar', str(RATE), 'pipe:1',
    ])
    samples = np.frombuffer(raw, dtype='<f4').astype(np.float64)
    if (not np.isfinite(samples).all() or len(samples) > round(BASS_DECODE_SECONDS * RATE)
            or len(samples) < round((DEPLOY_BASS['crop'][1] - BASS_DECODE_START) * RATE)):
        raise ValueError('Invalid bounded bass decode')
    return samples, {
        'duration_seconds': duration,
        'duration_precision_seconds': 0.01,
        'duration_basis': 'original recording container duration reported by FFmpeg',
        'decoded_source_start_seconds': BASS_DECODE_START,
        'decoded_duration_seconds': len(samples) / RATE,
    }


def wav_sha256(samples):
    """Hash the exact PCM16 WAV representation used by write_wav."""
    buffer = io.BytesIO()
    pcm = np.rint(np.clip(samples, -1, 1) * 32767).astype('<i2')
    with wave.open(buffer, 'wb') as stream:
        stream.setparams((1, 2, RATE, len(pcm), 'NONE', 'not compressed'))
        stream.writeframes(pcm.tobytes())
    return hashlib.sha256(buffer.getvalue()).hexdigest()


def place_layers(decoded, cues, length, prepare):
    bus = np.zeros(length)
    layers = []
    for cue in cues:
        samples, metadata = prepare(decoded[cue['source']], cue)
        start = metadata['onset_sample']
        end = start + len(samples)
        if start < 0 or end > length:
            raise ValueError(f"Cue exceeds soundtrack: {cue['name']}")
        bus[start:end] += samples
        metadata['bus'] = cue.get('bus', 'mechanical_locks')
        layers.append(metadata)
    return bus, layers
