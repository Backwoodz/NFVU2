"""Prepare short exhaust effects from offline-rendered local event recordings.

Input files and their accurate source description stay local. No audio library
from another game is loaded by the mod. Output is ignored, optional PCM media.
"""
import argparse
import hashlib
import json
from pathlib import Path
import wave

import numpy as np
from project import LOCAL


def prepare(source, target, duration, loop=False):
    with wave.open(str(source), 'rb') as wav:
        rate, channels = wav.getframerate(), wav.getnchannels()
        if wav.getsampwidth() != 2 or channels not in (1, 2) or not 8000 <= rate <= 96000:
            raise ValueError('Expected mono/stereo PCM16')
        if not 32 <= wav.getnframes() <= rate * 15:
            raise ValueError('Expected a short rendered effect')
        samples = np.frombuffer(wav.readframes(wav.getnframes()), '<i2').astype(float)
    # Reject already clipped renders rather than hiding clipping by attenuation.
    if np.max(np.abs(samples)) >= 32767:
        raise ValueError('Input clips; render the source event at a lower gain')
    samples = samples.reshape(-1, channels).mean(axis=1) / 32768
    audible = np.flatnonzero(np.abs(samples) > .001)
    if len(audible) == 0:
        raise ValueError('Silent exhaust recording')
    start = max(0, int(audible[0]) - round(rate * .002))
    if loop:
        start=max(start,round(rate*.25))  # skip the event's initial startup transient
    samples = samples[start:min(len(samples), start + round(rate * duration))].copy()
    samples -= np.mean(samples)
    if np.sqrt(np.mean(samples*samples)) < .0001:
        raise ValueError('Effect has no audible signal after DC removal')
    if loop:
        n=min(round(rate*.035),len(samples)//8);ramp=np.linspace(0,1,n)
        samples=np.concatenate((samples[n:-n],samples[-n:]*(1-ramp)+samples[:n]*ramp))
        # The continuous bed needs a stable working level beside engine loops.
        gain=min(3.5,.085/float(np.sqrt(np.mean(samples*samples))),.75/float(np.max(np.abs(samples))))
    else:
        fade_in, fade_out = min(round(rate * .001), len(samples)//8), min(round(rate * .06), len(samples)//4)
        samples[:fade_in] *= np.linspace(0, 1, fade_in)
        samples[-fade_out:] *= np.linspace(1, 0, fade_out)
        gain=min(1,.8/max(float(np.max(np.abs(samples))),1e-9))
    samples *= gain
    with wave.open(str(target), 'wb') as wav:
        wav.setparams((1, 2, rate, len(samples), 'NONE', 'not compressed'))
        wav.writeframes(np.rint(samples * 32767).astype('<i2').tobytes())
    return {'file': target.name, 'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
            'trim_start_samples': start, 'sample_rate': rate, 'samples': len(samples), 'loop': loop, 'preparation_gain': gain,
            'peak': float(np.max(np.abs(samples))), 'rms': float(np.sqrt(np.mean(samples*samples))),
            'sha256': hashlib.sha256(target.read_bytes()).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--overrun', type=Path, nargs=2, required=True)
    parser.add_argument('--backfire', type=Path, required=True)
    parser.add_argument('--source-description', required=True, help='Accurate original title/bank/event provenance, without personal paths')
    parser.add_argument('--loop', type=Path, help='Optional sustained exhaust texture, separate from one-shot pops')
    parser.add_argument('--loop-source-description', help='Accurate source event and render settings for the sustained layer')
    args = parser.parse_args()
    folder = LOCAL / 'audio-exhaust'
    folder.mkdir(parents=True, exist_ok=True)
    clips = [prepare(source, folder / f'overrun_{index+1:02}.wav', .36) for index, source in enumerate(args.overrun)]
    clips.append(prepare(args.backfire, folder / 'backfire.wav', .24))
    if args.loop:
        if not args.loop_source_description:parser.error('--loop requires --loop-source-description')
        row=prepare(args.loop,folder/'overrun_loop.wav',3,loop=True)
        row['source_description']=args.loop_source_description;clips.append(row)
    elif (folder/'manifest.json').exists() and (folder/'overrun_loop.wav').exists():
        # A short-sample reimport must not lose an independently imported loop's provenance.
        previous=json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
        clips.extend(row for row in previous.get('clips',[]) if row.get('file')=='overrun_loop.wav')
    manifest = {'source_description': args.source_description,
                'processing': 'Offline event render, mono downmix, leading silence trim, bounded tail and fades; own runtime trigger model',
                'vehicle_mapping': 'Shared backfire events; exact vehicle assignment unverified', 'clips': clips}
    (folder / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(manifest, indent=2))


if __name__ == '__main__':
    main()
