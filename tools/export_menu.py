"""Import selected original NFSU2 menu icons, German labels and navigation clips.

NFVU2 authors the layout and actions. Frontend bank roles are configurable;
their exact original event-to-substream mapping awaits an in-game comparison.
"""
import argparse
import hashlib
import json
import struct
from pathlib import Path

from project import LOCAL, vgmstream_path
from nfs_formats import TexturePack, jdlz
from export_hud import rgba
from prepare_effects_audio import extract_missing, prepare_clip


def language_table(data):
    if len(data) < 24 or struct.unpack_from('<I', data)[0] != 0x39000:
        raise ValueError('Invalid NFS language header')
    count, table, strings = struct.unpack_from('<3I', data, 12)
    table += 8
    strings += 8
    if count > 20000 or table + count * 8 > strings or strings > len(data):
        raise ValueError('Invalid language offsets')
    result = {}
    for index in range(count):
        key, offset = struct.unpack_from('<II', data, table + index * 8)
        start = strings + offset
        end = data.find(b'\0', start)
        if start >= len(data) or end < start or key in result:
            raise ValueError('Invalid language record')
        result[key] = data[start:end].decode('cp1252', errors='replace')
    return result


def export(navigate=1, accept=3, back=2):
    names = ['FRONTEND/FrontB.lzc', 'LANGUAGES/Labels.bin', 'LANGUAGES/German.bin',
             'SOUND/GLOBAL/FE_COMMON_MB.abk']
    extract_missing(names)
    root = LOCAL / 'nfs-assets'
    out = LOCAL / 'menu'
    out.mkdir(exist_ok=True)
    decoded = LOCAL / 'frontend-decoded.bin'
    decoded.write_bytes(jdlz((root / names[0]).read_bytes()))
    pack = TexturePack(decoded)
    # Exact resource keys; long names in the retail texture records are truncated.
    keys = [0xb0c46023, 0xc52ccbf6, 0xf69ac384, 0x02929058, 0xbded289a,
            0x0294d2a3, 0xb2798bb4, 0xe803e1cd, 0xc591c4b6, None, 0xa964095b]
    icons = []
    for key in keys:
        if key is None:
            icons.append(None)
        else:
            name, image, _ = pack.decode(key)
            icons.append(rgba(name, image))
    labels = language_table((root / names[1]).read_bytes())
    german = language_table((root / names[2]).read_bytes())
    localized = {label: german[key] for key, label in labels.items() if key in german}
    label_keys = ['VISUAL_PART_BODY_KIT', 'OPT_AUDIO', 'VISUAL_PART_HEAD_LIGHT', None,
                  'PERF_CATEGORY_NOS', 'VISUAL_PART_RIMS', None, 'TRUNK_AUDIO_TITLE', None, None, 'THE_CRIB_TITLE']
    titles = [localized[key] if key else None for key in label_keys]
    (out / 'menu.json').write_text(json.dumps(dict(schemaVersion=1, icons=icons, titles=titles),
                                             separators=(',', ':')), encoding='utf-8')
    bank = root / names[3]
    files = [prepare_clip(vgmstream_path(), bank, index, out / (role + '.wav'), False)
             for role, index in [('navigate', navigate), ('accept', accept), ('back', back)]]
    manifest = dict(schema_version=1, files=files, texture_keys=keys, label_keys=label_keys,
                    sources=[dict(file=name, sha256=hashlib.sha256((root/name).read_bytes()).hexdigest()) for name in names],
                    adaptation='Original local artwork, labels and audio. NFVU2 layout/actions; configurable sound roles are not a verified reconstruction of native NFS events.')
    (out / 'provenance.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print('Imported 10 original menu icons, 7 original labels and 3 frontend clips into .local/menu.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for role, default in [('navigate', 1), ('accept', 3), ('back', 2)]:
        parser.add_argument('--' + role + '-stream', type=int, default=default)
    args = parser.parse_args()
    export(args.navigate_stream, args.accept_stream, args.back_stream)
