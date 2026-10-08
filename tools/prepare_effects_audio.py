"""Prepare original local nitrous/purge samples; never modify a game installation."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave
import zipfile

import numpy as np

from project import LOCAL, disc_root, vgmstream_path
from inspect_discs import Disc
from extract_assets import DiscFile


def extract_missing(names):
    root = LOCAL / 'nfs-assets'
    missing = {name.upper(): name for name in names if not (root / name).is_file()}
    for image in disc_root().glob('CD*/*.bin'):
        if not missing:
            break
        disc = Disc(image)
        try:
            for record in disc.walk():
                if record['name'] != 'COMPRESS.ZIP':
                    continue
                with zipfile.ZipFile(DiscFile(disc, record)) as archive:
                    for entry in archive.infolist():
                        key = entry.filename.replace('\\', '/').upper()
                        if key not in missing:
                            continue
                        target = root / missing.pop(key)
                        target.parent.mkdir(parents=True, exist_ok=True)
                        target.write_bytes(archive.read(entry))
        finally:
            disc.f.close()
    if missing:
        raise ValueError('Original effect sources unavailable: ' + ', '.join(missing.values()))


def prepare_samples(samples, rate, loop):
    """Bounded mono PCM with quiet seams; role selection remains in the manifest."""
    samples = np.asarray(samples, dtype=float).copy()
    if not 8000 <= rate <= 96000 or samples.ndim != 1 or not 32 <= len(samples) <= rate * 15:
        raise ValueError('Expected a short mono effect sample')
    if not np.isfinite(samples).all():
        raise ValueError('Non-finite effect audio')
    if loop is not None:
        start, end = loop['start'], loop['end']
        if not isinstance(start, int) or not isinstance(end, int) or not 0 <= start < end <= len(samples) or end-start < 32:
            raise ValueError('Invalid original loop boundaries')
        samples = samples[start:end]
    samples -= np.mean(samples)
    if loop is not None:
        n = min(round(rate * .012), len(samples) // 8)
        ramp = np.linspace(0, 1, n)
        samples = np.concatenate((samples[n:-n], samples[-n:] * (1-ramp) + samples[:n] * ramp))
    else:
        n = min(round(rate * .004), len(samples) // 8)
        samples[:n] *= np.linspace(0, 1, n)
        samples[-n:] *= np.linspace(1, 0, n)
    peak = float(np.max(np.abs(samples)))
    rms = float(np.sqrt(np.mean(samples * samples)))
    if rms < .0001:
        raise ValueError('Silent effect audio')
    samples *= min(.16 / rms, .8 / peak)
    return samples


def prepare_clip(decoder, source, index, target, looping):
    info = json.loads(subprocess.check_output([str(decoder), '-m', '-I', '-s', str(index), str(source)], text=True))
    if not 1 <= index <= info['streamInfo']['total']:
        raise ValueError('Invalid effect substream index')
    loop = info.get('loopingInfo')
    if looping != (loop is not None):
        raise ValueError('Selected substream loop metadata does not match the requested effect')
    decoded = LOCAL / 'audio-decoded' / f'{source.stem}_{index}.wav'
    decoded.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([str(decoder), '-i', '-s', str(index), '-o', str(decoded), str(source)], check=True, stdout=subprocess.DEVNULL)
    with wave.open(str(decoded), 'rb') as wav:
        if wav.getnchannels() != 1 or wav.getsampwidth() != 2 or wav.getnframes() != info['numberOfSamples']:
            raise ValueError('Decoded effect differs from original metadata')
        rate = wav.getframerate()
        samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype='<i2').astype(float) / 32768
    samples = prepare_samples(samples, rate, loop)
    with wave.open(str(target), 'wb') as wav:
        wav.setparams((1, 2, rate, len(samples), 'NONE', 'not compressed'))
        wav.writeframes(np.rint(samples * 32767).astype('<i2').tobytes())
    return {'file': target.name, 'source': source.relative_to(LOCAL / 'nfs-assets').as_posix(),
            'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'subsong': index,
            'decoder_version': info.get('version'), 'source_loop': loop,
            'sample_rate': rate, 'samples': len(samples), 'loop': looping,
            'peak': float(np.max(np.abs(samples))), 'rms': float(np.sqrt(np.mean(samples*samples))),
            'sha256': hashlib.sha256(target.read_bytes()).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--decoder', type=Path)
    parser.add_argument('--bank', choices=('00', '01'), default='00')
    parser.add_argument('--nitro-stream', type=int, default=1)
    parser.add_argument('--purge-stream', type=int, default=4)
    parser.add_argument('--backfire-stream', type=int, help='Opt in to a chosen 350Z sweetener one-shot; native trigger and substream role are not verified')
    args = parser.parse_args()
    decoder = args.decoder or vgmstream_path()
    if not decoder.is_file():
        raise SystemExit('Configure the official vgmstream decoder or pass --decoder.')
    bank = f'SOUND/NOS/Nitrous_{args.bank}_MB.abk'
    csi = 'SOUND/EVT_SYS/UG2_MAIN_AEMS.csi'
    names = [bank, csi]
    if args.backfire_stream is not None:
        names += ['SOUND/ENGINE/SWTN_CAR_27_MB.abk', 'SOUND/EVT_SYS/UG2_ENGINES_AEMS2.csi']
    extract_missing(names)
    root = LOCAL / 'nfs-assets'
    evidence = (root / csi).read_bytes()
    if b'FX_PURGE\0' not in evidence or b'FX_NITROUS\0' not in evidence:
        raise ValueError('Expected original nitrous and purge event declarations missing')
    out = LOCAL / 'audio-effects'
    out.mkdir(exist_ok=True)
    files = [prepare_clip(decoder, root / bank, args.nitro_stream, out / 'nitro.wav', True),
             prepare_clip(decoder, root / bank, args.purge_stream, out / 'purge.wav', True)]
    if args.backfire_stream is not None:
        files.append(prepare_clip(decoder, root / 'SOUND/ENGINE/SWTN_CAR_27_MB.abk', args.backfire_stream, out / 'backfire.wav', False))
    elif (out / 'backfire.wav').exists():
        # Only our generated optional output, never installed assets or sources.
        (out / 'backfire.wav').unlink()
    manifest = {'schema_version': 1, 'files': files,
        'evidence': [{'source': csi, 'sha256': hashlib.sha256(evidence).hexdigest(), 'events': ['FX_NITROUS', 'FX_PURGE']}],
        'provenance': 'Original local NFSU2 banks, decoded with official vgmstream. Loop/one-shot metadata is verified. Assignment of the two nitrous bank loops to boost and purge is configurable and awaits an original-game listening comparison.',
        'behaviour': 'NFVU2 drives these sounds from its own boost/purge states. Native RPM thresholds, upgrade restrictions and lift-off backfire triggers have not been reconstructed. Backfire export is opt-in only.',
        'references': ['https://github.com/nlgxzef/NFSU2Unlimiter/blob/master/CarEffect.h',
                       'https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/SweetnerDataSet/27.ini',
                       'https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_abk.c']}
    (out / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print(f'Prepared {len(files)} original effect clips in .local/audio-effects; native event timing is not reproduced.')


if __name__ == '__main__':
    main()
