from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import json
import struct
from pathlib import Path

class Disc:
    def __init__(self, path):
        self.f = open(path, 'rb')
        self.path = str(path)
        pvd = self.read(16 * 2048, 2048)
        if pvd[1:6] != b'CD001':
            raise ValueError('No ISO9660 primary volume descriptor')
        self.label = pvd[40:72].decode('ascii', 'replace').strip()
        self.root = self.record(pvd[156:])

    def read(self, offset, length):
        out = bytearray()
        while length:
            sector, inner = divmod(offset, 2048)
            n = min(length, 2048 - inner)
            self.f.seek(sector * 2352 + 16 + inner)
            data = self.f.read(n)
            if len(data) != n:
                raise EOFError()
            out.extend(data)
            offset += n
            length -= n
        return bytes(out)

    @staticmethod
    def record(data):
        n = data[32]
        return {'lba': struct.unpack_from('<I', data, 2)[0],
                'size': struct.unpack_from('<I', data, 10)[0],
                'directory': bool(data[25] & 2),
                'name': data[33:33+n].decode('ascii', 'replace').split(';')[0]}

    def walk(self, record=None, prefix=''):
        record = record or self.root
        data = self.read(record['lba'] * 2048, record['size'])
        offset = 0
        while offset < len(data):
            length = data[offset]
            if not length:
                offset = (offset // 2048 + 1) * 2048
                continue
            rec = self.record(data[offset:offset+length])
            offset += length
            if rec['name'] in ('\x00', '\x01'):
                continue
            rec['path'] = prefix + rec['name']
            yield rec
            if rec['directory']:
                yield from self.walk(rec, rec['path'] + '/')

if __name__ == '__main__':
    results = []
    for path in disc_root().glob('CD*/*.bin'):
        disc = Disc(path)
        entries = list(disc.walk())
        results.append({'image': str(path), 'label': disc.label, 'entries': entries})
        print(disc.label, len(entries), 'entries')
        for e in entries:
            if not e['directory'] and '/' not in e['path']:
                print(e['path'], e['size'])
    (LOCAL/'disc_inventory.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
