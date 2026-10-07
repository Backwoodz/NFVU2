from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import io
import json
import zipfile
from pathlib import Path
from inspect_discs import Disc

class DiscFile(io.RawIOBase):
    def __init__(self, disc, rec):
        self.disc, self.rec, self.pos = disc, rec, 0
    def seekable(self): return True
    def readable(self): return True
    def tell(self): return self.pos
    def seek(self, offset, whence=0):
        self.pos = offset if whence == 0 else self.pos + offset if whence == 1 else self.rec['size'] + offset
        if self.pos < 0: raise ValueError('negative seek')
        return self.pos
    def read(self, n=-1):
        n = self.rec['size'] - self.pos if n < 0 else min(n, self.rec['size'] - self.pos)
        if n <= 0: return b''
        data = self.disc.read(self.rec['lba'] * 2048 + self.pos, n)
        self.pos += len(data)
        return data

def main():
    base = LOCAL/'nfs-assets'
    base.mkdir(parents=True, exist_ok=True)
    inventory = []
    for image in disc_root().glob('CD*/*.bin'):
        disc = Disc(image)
        for rec in disc.walk():
            if rec['name'] != 'COMPRESS.ZIP': continue
            with zipfile.ZipFile(DiscFile(disc, rec)) as archive:
                for item in archive.infolist():
                    name = item.filename.replace('\\', '/')
                    inventory.append({'disc': disc.label, 'name': name, 'size': item.file_size})
                    upper = name.upper()
                    wanted = upper.startswith(('CARS/350Z/', 'CARS/WHEELS/', 'CARS/SPOILER/', 'CARS/BRAKES/', 'CARS/EXHAUST/', 'GLOBAL/'))
                    if not wanted or item.is_dir() or upper.endswith(('.EXE', '.DLL')): continue
                    target = (base / name).resolve()
                    if not target.is_relative_to(base): raise ValueError('unsafe archive path')
                    target.parent.mkdir(parents=True, exist_ok=True)
                    data = archive.read(item)
                    target.write_bytes(data)
                    print('Extracted', name, len(data))
    (LOCAL/'zip_inventory.json').write_text(json.dumps(inventory, indent=2), encoding='utf-8')
    print('Archive entries:', len(inventory))

if __name__ == '__main__':
    main()
