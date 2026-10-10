"""Convert Facepunch's official logo archive to an optional, local menu texture.

Run with --download or pass the locally downloaded rust-logos.zip. Original
artwork and generated pixels remain in ignored .local, never in source control.
"""
import argparse
import base64
import hashlib
import io
import json
import urllib.request
import zipfile
from PIL import Image
from project import LOCAL

SOURCE = 'https://facepunch.com/s/rust-logos.zip'
MEMBER = 'rust-logos/Rust_Branding-1080.png'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', nargs='?', type=str)
    parser.add_argument('--download', action='store_true')
    args = parser.parse_args()
    if not args.archive and not args.download:
        parser.error('Supply the official archive or --download')
    folder = LOCAL / 'branding'
    folder.mkdir(parents=True, exist_ok=True)
    archive = folder / 'rust-logos.zip'
    if args.download:
        request = urllib.request.Request(SOURCE, headers={'User-Agent': 'NFVU2-local-asset-import'})
        with urllib.request.urlopen(request, timeout=30) as response:
            data = response.read(16 * 1024 * 1024 + 1)
        if len(data) > 16 * 1024 * 1024:
            raise ValueError('Oversized logo archive')
        archive.write_bytes(data)
    if args.archive:
        from pathlib import Path
        archive = Path(args.archive)
    with zipfile.ZipFile(archive) as package:
        if package.getinfo(MEMBER).file_size > 16 * 1024 * 1024:
            raise ValueError('Oversized logo image')
        original = package.read(MEMBER)
    picture = Image.open(io.BytesIO(original)).convert('RGBA')
    # Discard only transparent outer padding, retaining the entire original mark.
    bounds = picture.getchannel('A').getbbox()
    if bounds is None:
        raise ValueError('Empty logo')
    picture = picture.crop(bounds)
    picture.thumbnail((512, 128), Image.Resampling.LANCZOS)
    output = LOCAL / 'menu'
    output.mkdir(parents=True, exist_ok=True)
    (output / 'rust-logo.json').write_text(json.dumps(dict(
        name='Official_Rust_Branding', width=picture.width, height=picture.height,
        rgba=base64.b64encode(picture.tobytes()).decode()), separators=(',', ':')), encoding='utf-8')
    (output / 'rust-logo-source.json').write_text(json.dumps(dict(
        source=SOURCE, member=MEMBER, copyright='Facepunch Studios Limited',
        sha256=hashlib.sha256(original).hexdigest(),
        conversion='Transparent padding removed; proportional downsample; original colours and artwork.'
    ), indent=2), encoding='utf-8')
    print('Imported official Rust logo:', picture.size)


if __name__ == '__main__':
    main()
