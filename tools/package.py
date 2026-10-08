from pathlib import Path
import zipfile
from project import BASE,OUTPUT
from mod_files import payload_files

target=OUTPUT/'NFVU2-v0.7.2.zip'
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as archive:
    for name in payload_files(OUTPUT/'mod'):
        archive.write(OUTPUT/'mod'/name,'BepInEx/plugins/Rachel350Z/'+name.as_posix())
    for path in (BASE/'README.md',BASE/'docs/Spielanleitung.txt',BASE/'docs/Audioquellen.txt'):
        archive.write(path,path.name)
with zipfile.ZipFile(target) as archive:assert archive.testzip() is None
print('Local package:',target)
