from pathlib import Path
import zipfile
from project import BASE,OUTPUT

target=OUTPUT/'NFVU2-v0.1.5.zip'
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as archive:
    for name in ('Rachel350Z.dll','body.json'):
        archive.write(OUTPUT/'mod'/name,'BepInEx/plugins/Rachel350Z/'+name)
    for path in (BASE/'README.md',BASE/'docs/Spielanleitung.txt',BASE/'docs/Audioquellen.txt'):
        archive.write(path,path.name)
with zipfile.ZipFile(target) as archive:assert archive.testzip() is None
print('Local package:',target)
