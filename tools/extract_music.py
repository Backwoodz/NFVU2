"""Import the user's NFSU2 soundtrack as streamed local stereo WAV files.

BIG4 directory layout: github.com/RadeonChan/XtraTrax/blob/main/patcher.py
MPF/MUS decoding: github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_map_mpf_mus.c
No game files or music are downloaded. Source files are never modified.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import struct
import subprocess
import wave

from project import LOCAL, disc_root, vgmstream_path


def title_catalog():
    """Exact decoded-audio identities; filenames alone never identify a song."""
    data = json.loads(Path(__file__).with_name('music_titles.json').read_text(encoding='utf-8'))
    return {(data['source'].casefold(), track['subsong'], track['sha256']): track
            for track in data['tracks']}


def excluded_catalog():
    data = json.loads(Path(__file__).with_name('music_titles.json').read_text(encoding='utf-8'))
    return {(data['source'].casefold(), track['subsong'], track['sha256'])
            for track in data['excluded_streams']}


def assign_title(entry, catalog, overrides=None):
    """Resolve only verified audio or an explicit edition-specific user override."""
    key = f"{entry['source']}:{entry['subsong']}"
    if overrides and key in overrides:
        title = overrides[key]
        if not isinstance(title, str) or not title.strip():
            raise ValueError(f'Invalid title override: {key}')
        entry['title'] = title
        entry.pop('artist', None)
        entry.pop('song', None)
        entry['title_origin'] = 'user override'
        return True
    match = catalog.get((entry['source'].casefold(), entry['subsong'], entry['sha256']))
    if match is None:
        return False
    entry.update(title=f"{match['artist']} – {match['song']}", artist=match['artist'],
                 song=match['song'], title_origin='verified EA Trax event/sample mapping')
    return True


def refresh_titles(output, overrides=None, include_unmapped=False):
    """Update a local playlist in place without decoding or changing audio files."""
    manifest = output / 'tracks.json'
    entries = json.loads(manifest.read_text(encoding='utf-8-sig'))
    catalog = title_catalog()
    excluded = excluded_catalog()
    updated = 0
    playlist = []
    for entry in entries:
        name = entry['file']
        if Path(name).name != name or any(c in name for c in ('/', '\\', ':')):
            raise ValueError('Unsafe playlist filename')
        target = output / name
        actual = hashlib.sha256(target.read_bytes()).hexdigest()
        if actual != entry['sha256']:
            raise ValueError(f'Audio changed since import: {name}; reimport or supply a verified playlist')
        matched = assign_title(entry, catalog, overrides)
        if matched:
            updated += 1
        elif (entry['source'].casefold(), entry['subsong'], actual) in excluded:
            if not include_unmapped:
                continue
        elif not include_unmapped:
            raise ValueError(f'Unverified music identity: {name}; pass --titles for verified custom titles or --include-unmapped for a research playlist')
        playlist.append(entry)
    pending = output / 'tracks.pending.json'
    pending.write_text(json.dumps(playlist, ensure_ascii=False, indent=2), encoding='utf-8')
    pending.replace(manifest)
    return updated, len(playlist)


def big_entries(path):
    """Read bounded BIG4 entries; reject traversal and malformed ranges."""
    size = path.stat().st_size
    with path.open('rb') as stream:
        header = stream.read(16)
        if len(header) != 16 or header[:4] != b'BIG4':
            raise ValueError('Not a BIG4 archive')
        total, count, end = struct.unpack('>III', header[4:])
        if total != size or not 16 <= end <= size or count > 100000:
            raise ValueError('Invalid BIG4 header')
        directory = stream.read(end - 16)
    offset = 0
    entries = []
    seen = set()
    for _ in range(count):
        if offset + 9 > len(directory):
            raise ValueError('Truncated BIG4 directory')
        start, length = struct.unpack_from('>II', directory, offset)
        offset += 8
        terminator = directory.find(b'\0', offset)
        if terminator < 0:
            raise ValueError('Unterminated BIG4 filename')
        name = directory[offset:terminator].decode('ascii').replace('\\', '/')
        offset = terminator + 1
        parts = name.split('/')
        if not name or any(p in ('', '.', '..') for p in parts) or ':' in name:
            raise ValueError('Unsafe BIG4 filename')
        if name.lower() in seen or start < end or start + length > size:
            raise ValueError('Invalid BIG4 entry')
        seen.add(name.lower())
        entries.append((name, start, length))
    return entries


def extract_archive(path, output):
    output.mkdir(parents=True, exist_ok=True)
    entries = [e for e in big_entries(path) if Path(e[0]).suffix.lower() in ('.mpf', '.mus')]
    if not entries:
        raise ValueError('No MPF/MUS music in archive')
    with path.open('rb') as stream:
        for name, offset, size in entries:
            target = output / name
            target.parent.mkdir(parents=True, exist_ok=True)
            stream.seek(offset)
            with target.open('wb') as dest:
                remaining = size
                while remaining:
                    data = stream.read(min(1024 * 1024, remaining))
                    if not data:
                        raise ValueError('Truncated BIG4 audio')
                    dest.write(data)
                    remaining -= len(data)
    return output


def extract_discs():
    from inspect_discs import Disc
    from extract_assets import DiscFile
    target = LOCAL / 'nfs-assets/SDATA/SDAT.VIV'
    if target.is_file():
        return target
    for image in sorted(disc_root().glob('CD*/*.bin')):
        disc = Disc(image)
        try:
            for rec in disc.walk():
                if rec['path'].upper() != 'SDATA/SDAT.VIV':
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                with target.open('wb') as dest:
                    shutil.copyfileobj(DiscFile(disc, rec), dest, 1024 * 1024)
                if target.stat().st_size != rec['size']:
                    raise ValueError('Incomplete soundtrack archive')
                return target
        finally:
            disc.f.close()
    raise FileNotFoundError('No SDATA/SDAT.VIV on configured CD images; pass --source for an extracted archive or MPF/MUS folder')


def music_stream(info, min_seconds=60):
    """Exclude short stingers/mono SFX; duration does not prove song boundaries."""
    rate = info.get('sampleRate', 0)
    samples = info.get('numberOfSamples', 0)
    return rate > 0 and info.get('channels') == 2 and samples / rate >= min_seconds


def source_maps(source):
    paths = [source] if source.is_file() else sorted(p for p in source.rglob('*') if p.suffix.lower() == '.mpf')
    if not paths:
        raise FileNotFoundError('No .mpf files found; matching .mus files must be adjacent')
    stems = [p.stem.casefold() for p in paths]
    if len(set(stems)) != len(stems):
        raise ValueError('Ambiguous MPF filenames in source; import one soundtrack folder at a time')
    return paths


def decode(source, decoder, output, min_seconds=60, titles=None, include_unmapped=False):
    output.mkdir(parents=True, exist_ok=True)
    manifests = []
    hashes = set()
    titles = titles or {}
    catalog = title_catalog()
    identity = json.loads(Path(__file__).with_name('music_titles.json').read_text(encoding='utf-8'))
    paths = source_maps(source)
    def metadata(path, index):
        return json.loads(subprocess.check_output([str(decoder), '-m', '-I', '-s', str(index), str(path)], text=True))
    for path in paths:
        known_map = hashlib.sha256(path.read_bytes()).hexdigest() == identity['map_sha256']
        if not known_map and not titles and not include_unmapped:
            raise ValueError('Unverified MPF edition; pass --titles for a verified custom mapping or --include-unmapped for research output')
        song_ids = {t['subsong'] for t in identity['tracks']}
        first = metadata(path, 1)
        total = max(1, first.get('streamInfo', {}).get('total', 1))
        if total > 4096:
            raise ValueError('Unexpected soundtrack stream count')
        for index in range(1, total + 1):
            key = f'{path.name}:{index}'
            if not include_unmapped and key not in titles and (not known_map or index not in song_ids):
                continue
            info = first if index == 1 else metadata(path, index)
            if not music_stream(info, min_seconds):
                continue
            name = f'{path.stem}_{index:02d}.wav'
            target = output / name
            temporary = target.with_suffix('.pending.wav')
            try:
                subprocess.run([str(decoder), '-i', '-s', str(index), '-o', str(temporary), str(path)], check=True, stdout=subprocess.DEVNULL)
                with wave.open(str(temporary), 'rb') as wav:
                    if wav.getnchannels() != 2 or wav.getsampwidth() != 2 or wav.getnframes() != info['numberOfSamples']:
                        raise ValueError('Unexpected decoded music format or truncated track')
                digest = hashlib.sha256(temporary.read_bytes()).hexdigest()
                if digest in hashes:
                    continue
                hashes.add(digest)
                entry = dict(file=name, title=f'NFS Underground 2 – Stream {index:02d}', source=path.name, subsong=index,
                             seconds=round(info['numberOfSamples']/info['sampleRate'], 3), sha256=digest)
                matched = assign_title(entry, catalog, titles)
                if not matched and not include_unmapped:
                    raise ValueError(f'Unverified audio for {key}; the source audio differs from the verified soundtrack')
                temporary.replace(target)
            finally:
                temporary.unlink(missing_ok=True)
            manifests.append(entry)
            print(f'Imported {key}: {manifests[-1]["seconds"]:.1f}s stereo', flush=True)
    if not manifests:
        raise ValueError('No eligible stereo music streams found; source may only contain short SFX/segments')
    manifest = output / 'tracks.json'
    pending = output / 'tracks.pending.json'
    pending.write_text(json.dumps(manifests, ensure_ascii=False, indent=2), encoding='utf-8')
    pending.replace(manifest)
    return manifests


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, help='SDAT.VIV archive, MPF file, or extracted MPF/MUS folder; otherwise read configured CDs')
    parser.add_argument('--decoder', type=Path, help='vgmstream-cli executable (otherwise config.local.json)')
    parser.add_argument('--output', type=Path, default=LOCAL/'radio')
    parser.add_argument('--titles', type=Path, help='Optional JSON map {"MusicSFx.mpf:1": "Artist – Song"}, verified against your source edition')
    parser.add_argument('--min-seconds', type=float, default=60)
    parser.add_argument('--refresh-titles', action='store_true', help='Verify existing WAV hashes and update playlist titles without decoding audio again')
    parser.add_argument('--include-unmapped', action='store_true', help='Research only: include streams without verified song names, including non-EA-Trax audio')
    args = parser.parse_args()
    titles = json.loads(args.titles.read_text(encoding='utf-8-sig')) if args.titles else None
    if args.refresh_titles:
        updated, total = refresh_titles(args.output.resolve(), titles, args.include_unmapped)
        print(f'Updated verified titles for {updated}/{total} radio entries; audio files unchanged.')
        return
    source = (args.source or extract_discs()).resolve()
    decoder = (args.decoder or vgmstream_path()).resolve()
    if not decoder.is_file():
        raise SystemExit('vgmstream decoder missing; configure its local path or pass --decoder')
    if source.suffix.lower() == '.viv':
        source = extract_archive(source, LOCAL/'music-source')
    tracks = decode(source, decoder, args.output.resolve(), args.min_seconds, titles, args.include_unmapped)
    print(f'Imported {len(tracks)} local stereo radio entries. Rebuild/package to include radio files.')


if __name__ == '__main__':
    main()
