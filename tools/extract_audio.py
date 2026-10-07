from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import zipfile
from pathlib import Path
from inspect_discs import Disc
from extract_assets import DiscFile

base=LOCAL/'nfs-assets'
for image in disc_root().glob('CD*/*.bin'):
    disc=Disc(image)
    try:
        for rec in disc.walk():
            if rec['name']!='COMPRESS.ZIP': continue
            with zipfile.ZipFile(DiscFile(disc,rec)) as archive:
                for item in archive.infolist():
                    name=item.filename.replace('\\','/'); upper=name.upper()
                    wanted=(upper.startswith('SOUND/ENGINE/') and '350Z' in upper and upper.endswith('.GIN')) or upper.startswith('SOUND/SKIDS/') or upper in ('SOUND/IG_GLOBAL/ROADNOISE_00_MB.ABK','SOUND/IG_GLOBAL/FX_MAIN_MEM_MB.ABK')
                    if not wanted or item.is_dir(): continue
                    target=(base/name).resolve()
                    if not target.is_relative_to(base): raise ValueError('unsafe archive path')
                    target.parent.mkdir(parents=True,exist_ok=True)
                    target.write_bytes(archive.read(item))
                    print(name,item.file_size)
    finally:
        disc.f.close()
