from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import zipfile
import argparse
from pathlib import Path
from inspect_discs import Disc
from extract_assets import DiscFile

parser=argparse.ArgumentParser(description='Extract local NFS vehicle audio; original data stays in .local.')
parser.add_argument('--turbo-only',action='store_true')
args=parser.parse_args()
base=LOCAL/'nfs-assets'
extracted=0
for image in disc_root().glob('CD*/*.bin'):
    disc=Disc(image)
    try:
        for rec in disc.walk():
            if rec['name']!='COMPRESS.ZIP': continue
            with zipfile.ZipFile(DiscFile(disc,rec)) as archive:
                for item in archive.infolist():
                    name=item.filename.replace('\\','/'); upper=name.upper()
                    turbo=upper.startswith('SOUND/TURBO/TURBO_MED_') and upper.endswith('.ABK') or upper=='SOUND/EVT_SYS/UG2_TURBO.CSI'
                    wanted=turbo or (not args.turbo_only and ((upper.startswith('SOUND/ENGINE/') and '350Z' in upper and upper.endswith('.GIN')) or upper.startswith('SOUND/SKIDS/') or upper in ('SOUND/IG_GLOBAL/ROADNOISE_00_MB.ABK','SOUND/IG_GLOBAL/FX_MAIN_MEM_MB.ABK')))
                    if not wanted or item.is_dir(): continue
                    target=(base/name).resolve()
                    if not target.is_relative_to(base): raise ValueError('unsafe archive path')
                    target.parent.mkdir(parents=True,exist_ok=True)
                    target.write_bytes(archive.read(item))
                    extracted+=1
                    print(name,item.file_size)
    finally:
        disc.f.close()
if not extracted:raise SystemExit('No audio sources found. Check the local CD image configuration.')
