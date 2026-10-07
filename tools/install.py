"""Install a built mod only while Valheim is closed; back up and verify both files."""
import csv
import hashlib
import io
import shutil
import subprocess
from datetime import datetime
from project import OUTPUT,valheim_dir


def require_closed():
    result=subprocess.check_output(['tasklist','/FI','IMAGENAME eq valheim.exe','/FO','CSV','/NH'],text=True)
    if any(row and row[0].lower()=='valheim.exe' for row in csv.reader(io.StringIO(result))):
        raise SystemExit('Valheim is running. Close it before installing the mod.')


def checksum(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    require_closed()
    destination=valheim_dir()/'BepInEx/plugins/Rachel350Z'
    names=('Rachel350Z.dll','body.json')
    for name in names:
        if not (OUTPUT/'mod'/name).is_file():raise SystemExit('Build the mod first: '+name)
    backup=OUTPUT/'backups'/datetime.now().strftime('%Y%m%d-%H%M%S-%f');backup.mkdir(parents=True)
    destination.mkdir(parents=True,exist_ok=True)
    for name in names:
        installed=destination/name
        if installed.exists():shutil.copy2(installed,backup/name)
    require_closed()
    try:
        for name in names:
            require_closed()
            source=OUTPUT/'mod'/name;installed=destination/name
            shutil.copy2(source,installed)
            if checksum(source)!=checksum(installed):raise RuntimeError('Checksum mismatch: '+name)
            print(name,checksum(installed))
    except Exception:
        require_closed()
        for name in names:
            if (backup/name).exists():shutil.copy2(backup/name,destination/name)
        raise
    print('Installed. Backup:',backup)


if __name__=='__main__':main()
