"""Import six original local NFSU2 sponsor marks for optional venue signage.

Artwork and provenance stay in ignored .local/venue-branding. This does not
download assets and does not reconstruct the original game's sign placement.
"""
import hashlib
import json
from nfs_formats import TexturePack, jdlz
from project import LOCAL

MARKS = {'hks': 0xda35dbc5, 'apexi': 0x3e9fc376, 'injen': 0x3f2f8193,
         'falken': 0x1d362cf0, 'bbs': 0xda35c116, 'nfsu2': 0x3f85c14d}


def main():
    source = LOCAL / 'nfs-assets/FRONTEND/FrontB.lzc'
    decoded = LOCAL / 'venue-front-decoded.bin'
    decoded.write_bytes(jdlz(source.read_bytes()))
    pack = TexturePack(decoded)
    output = LOCAL / 'venue-branding'
    output.mkdir(exist_ok=True)
    records = []
    for role, key in MARKS.items():
        name, image, _ = pack.decode(key)
        image = image.convert('RGBA')
        image.save(output / (role + '.png'))
        records.append(dict(file=role + '.png', resource=name, key=hex(key),
                            width=image.width, height=image.height))
    (output / 'provenance.json').write_text(json.dumps(dict(
        source='FRONTEND/FrontB.lzc', sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
        marks=records, adaptation='Original local decal artwork; independent NFVU2 venue arrangement.'
    ), indent=2), encoding='utf-8')
    print('Imported six local sponsor marks into .local/venue-branding.')


if __name__ == '__main__':
    main()
