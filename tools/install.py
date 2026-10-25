"""Install the local payload while Valheim is closed; back up and verify every file."""
import csv
import hashlib
import io
import json
import os
import re
import shutil
import subprocess
from datetime import datetime
from project import OUTPUT,valheim_dir
from pathlib import Path
from mod_files import payload_files, OPTIONAL, REQUIRED


def require_closed():
    result=subprocess.check_output(['tasklist','/FI','IMAGENAME eq valheim.exe','/FO','CSV','/NH'],text=True)
    if any(row and row[0].lower()=='valheim.exe' for row in csv.reader(io.StringIO(result))):
        raise SystemExit('Valheim is running. Close it before installing the mod.')


def checksum(path):return hashlib.sha256(path.read_bytes()).hexdigest()


MANIFEST='nfvu2-installed-files.json'

def wizard_state(destination):
    """Keep an existing setup's ownership/checksum record in the same transaction."""
    destination=Path(destination)
    if tuple(p.lower() for p in destination.parts[-3:])!=('bepinex','plugins','rachel350z'):return None,None
    game=destination.parents[2]
    state=contained(game,'.nfvu2/install.json')
    if not state.exists():return None,None
    if contained(game,'.nfvu2/pending.json').exists():raise ValueError('Complete the pending setup transaction before installing locally')
    data=json.loads(state.read_text(encoding='utf-8-sig'))
    if data.get('Schema')!=1 or not isinstance(data.get('Mod'),dict) or not isinstance(data.get('Loader'),dict):
        raise ValueError('Invalid wizard installation state')
    prefix='BepInEx/plugins/Rachel350Z/'
    for name,digest in data['Mod'].items():
        if not name.startswith(prefix) or not allowed_name(name[len(prefix):]) or not isinstance(digest,str) or not re.fullmatch('[a-f0-9]{64}',digest):
            raise ValueError('Invalid wizard mod ownership entry')
    return state,data

def allowed_name(value):
    path=Path(value)
    if path.is_absolute() or '..' in path.parts:return False
    if path.as_posix() in REQUIRED:return True
    return any(path.parent.as_posix()==folder and path.suffix.lower() in extensions for _,folder,extensions in OPTIONAL)

def contained(root,name):
    target=(root/name).resolve()
    if not target.is_relative_to(root.resolve()):raise ValueError('Install path escapes its directory')
    return target


def prune_backups(folder):
    """Retain two dated snapshots; leave separately named backups untouched."""
    def linked(path):
        return path.is_symlink() or getattr(os.path,'isjunction',lambda p:False)(path)
    folder=Path(folder)
    if linked(folder):raise ValueError('Backup directory is a link')
    root=folder.resolve()
    candidates=sorted((p for p in folder.iterdir()
                       if re.fullmatch(r'\d{8}-\d{6}-\d{6}',p.name)
                       and not linked(p) and p.is_dir()),key=lambda p:p.name,reverse=True)
    stale=candidates[2:]
    # Validate all deletion targets before removing anything. Do not traverse
    # links even when they point to another directory inside the backup root.
    for backup in stale:
        if not backup.resolve().is_relative_to(root) or backup.resolve()==root:
            raise ValueError('Backup cleanup path escapes its directory')
        for current,dirs,files in os.walk(backup,followlinks=False):
            for name in dirs+files:
                path=Path(current)/name
                if linked(path) or not path.resolve().is_relative_to(backup.resolve()):
                    raise ValueError('Backup cleanup refuses linked contents')
    for backup in stale:shutil.rmtree(backup)
    return len(stale)

def install_payload(source,destination,backup,guard=require_closed):
    guard()
    source,destination,backup=Path(source),Path(destination),Path(backup)
    names=list(payload_files(source))
    state,state_data=wizard_state(destination)
    state_backup=contained(backup,'wizard-install-state.json')
    old=[]
    manifest=contained(destination,MANIFEST)
    if manifest.exists():
        saved=json.loads(manifest.read_text(encoding='utf-8'))
        if not isinstance(saved,list) or not all(isinstance(n,str) and allowed_name(n) for n in saved):
            raise ValueError('Invalid installed file manifest')
        old=[Path(n) for n in saved]
    if state_data is not None:
        old=list(set(old)|{Path(n.removeprefix('BepInEx/plugins/Rachel350Z/')) for n in state_data['Mod']})
    touched=set(names)|set(old)|{Path(MANIFEST)}
    for name in touched:
        contained(destination,name);contained(backup,name)
    backup.mkdir(parents=True,exist_ok=False)
    if state is not None:shutil.copy2(state,state_backup)
    destination.mkdir(parents=True,exist_ok=True)
    for name in touched:
        installed=destination/name
        if installed.exists():
            saved=backup/name;saved.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(installed,saved)
    try:
        for name in names:
            guard()
            original=source/name;installed=destination/name
            installed.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(original,installed)
            if checksum(original)!=checksum(installed):raise RuntimeError('Checksum mismatch: '+str(name))
        for name in set(old)-set(names):
            guard()
            path=destination/name
            if path.exists():path.unlink()
        guard()
        manifest.write_text(json.dumps([p.as_posix() for p in names],indent=2),encoding='utf-8')
        if state is not None:
            guard()
            state_data['Mod']={'BepInEx/plugins/Rachel350Z/'+p.as_posix():checksum(destination/p) for p in names}
            state.write_text(json.dumps(state_data,indent=2),encoding='utf-8')
            if json.loads(state.read_text(encoding='utf-8'))!=state_data:raise RuntimeError('Wizard state verification failed')
    except Exception:
        guard()
        for name in touched:
            installed=destination/name
            if (backup/name).exists():shutil.copy2(backup/name,installed)
            elif installed.exists():installed.unlink()
        if state is not None:shutil.copy2(state_backup,state)
        raise
    if re.fullmatch(r'\d{8}-\d{6}-\d{6}',backup.name):prune_backups(backup.parent)
    return len(names)

def main():
    backup=OUTPUT/'backups'/datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    count=install_payload(OUTPUT/'mod',valheim_dir()/'BepInEx/plugins/Rachel350Z',backup)
    print('Installed and SHA-256 verified',count,'files. Backup:',backup)


if __name__=='__main__':main()
