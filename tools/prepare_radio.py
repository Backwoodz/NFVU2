"""Create verified Ogg Vorbis radio copies; preserve imported WAV masters."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import wave

from project import LOCAL


def digest(path):
    result = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024*1024), b''):
            result.update(chunk)
    return result.hexdigest()


def checked_entries(folder):
    from mod_files import asset_files
    # Use the same filename/playlist validation as the packaging allowlist.
    files = asset_files(folder, {'.wav', '.ogg', '.json'})
    entries = json.loads((folder/'tracks.json').read_text(encoding='utf-8-sig'))
    if not entries or len(files) != len(entries)+1:
        raise ValueError('A non-empty radio playlist is required')
    for entry in entries:
        if digest(folder/entry['file']) != entry.get('sha256'):
            raise ValueError('Radio checksum mismatch: '+entry['file'])
    return entries


def compressed_source(local):
    source, target = Path(local)/'radio', Path(local)/'radio-ogg'
    if not target.exists():
        return source
    try:
        metadata = json.loads((target/'compression.json').read_text(encoding='utf-8'))
        if metadata['source_manifest_sha256'] != digest(source/'tracks.json'):
            raise ValueError('Source playlist changed')
        original, encoded = checked_entries(source), checked_entries(target)
        if len(original) != len(encoded):
            raise ValueError('Track count changed')
        for before, after in zip(original, encoded):
            if after.get('source_sha256') != before['sha256'] or after.get('title') != before.get('title'):
                raise ValueError('Track identity changed')
            if Path(after['file']).suffix.lower() != '.ogg':
                raise ValueError('Expected Ogg radio copy')
    except (OSError, ValueError, KeyError, TypeError) as error:
        raise ValueError('Compressed radio is stale or incomplete; rerun tools/prepare_radio.py: '+str(error)) from error
    return target


def wav_info(path):
    with wave.open(str(path), 'rb') as audio:
        return audio.getframerate(), audio.getnchannels(), audio.getnframes()


def encode_radio(source, target, ffmpeg, bitrate=192):
    source, target = Path(source).resolve(), Path(target).resolve()
    if source == target:
        raise ValueError('Keep compressed copies separate from source WAV files')
    entries = checked_entries(source)
    source_hash = digest(source/'tracks.json')
    if any(Path(e['file']).suffix.lower() != '.wav' for e in entries):
        raise ValueError('Encode from WAV masters, never recompress Ogg files')
    version = subprocess.check_output([str(ffmpeg), '-version'], text=True).splitlines()[0]
    target.parent.mkdir(parents=True, exist_ok=True)
    result = []
    with tempfile.TemporaryDirectory(prefix='radio-encode-', dir=target.parent) as temporary:
        stage = Path(temporary)
        for index, entry in enumerate(entries, 1):
            original = source/entry['file']
            output = stage/Path(entry['file']).with_suffix('.ogg').name
            run = [str(ffmpeg), '-hide_banner', '-loglevel', 'error', '-nostdin', '-y']
            subprocess.run([*run, '-i', str(original), '-map', '0:a:0', '-map_metadata', '-1',
                            '-c:a', 'libvorbis', '-b:a', f'{bitrate}k', str(output)], check=True)
            # Fully decode each result: header-only probes cannot detect broken songs.
            decoded = stage/'verification.wav'
            subprocess.run([*run, '-xerror', '-i', str(output), '-map', '0:a:0',
                            '-c:a', 'pcm_s16le', str(decoded)], check=True)
            rate, channels, frames = wav_info(original)
            new_rate, new_channels, new_frames = wav_info(decoded)
            if (rate, channels) != (new_rate, new_channels) or abs(frames-new_frames) > rate*.02:
                raise ValueError('Encoded track changed channels, sample rate or duration: '+entry['file'])
            decoded.unlink()
            result.append(dict(entry, file=output.name, sha256=digest(output),
                               source_sha256=entry['sha256'], codec='vorbis', target_kbps=bitrate))
            print(f'{index}/{len(entries)} verified: {output.name} ({output.stat().st_size/1e6:.1f} MB)', flush=True)
        # Detect source changes during the batch before publishing a new playlist.
        if source_hash != digest(source/'tracks.json') or entries != checked_entries(source):
            raise ValueError('Radio masters changed during encoding')
        (stage/'tracks.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
        (stage/'compression.json').write_text(json.dumps(dict(source_manifest_sha256=source_hash,
            encoder=version, codec='libvorbis', target_kbps=bitrate), indent=2), encoding='utf-8')
        target.mkdir(exist_ok=True)
        for entry in result:
            (stage/entry['file']).replace(target/entry['file'])
        (stage/'tracks.json').replace(target/'tracks.json')
        (stage/'compression.json').replace(target/'compression.json')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ffmpeg', type=Path, help='Local FFmpeg executable with libvorbis')
    parser.add_argument('--bitrate', type=int, choices=(160, 192, 256), default=192)
    args = parser.parse_args()
    encoder = args.ffmpeg or shutil.which('ffmpeg')
    if encoder is None:
        bundled = list((LOCAL/'tools/audio-encoder/imageio_ffmpeg/binaries').glob('ffmpeg*.exe'))
        encoder = bundled[0] if len(bundled) == 1 else None
    if encoder is None:
        raise SystemExit('Provide FFmpeg with --ffmpeg or put it on PATH. See CONTRIBUTING.md.')
    encode_radio(LOCAL/'radio', LOCAL/'radio-ogg', encoder, args.bitrate)
    compressed_source(LOCAL)
    print('Verified Ogg playlist ready; python tools/build.py will use it automatically.')


if __name__ == '__main__':
    main()
