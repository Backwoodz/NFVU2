"""Explicit allowlist for locally generated mod payloads (never source archives)."""
from pathlib import Path
import shutil
import json

REQUIRED=('Rachel350Z.dll','body.json')
OPTIONAL=(('radio', 'radio', {'.wav','.ogg','.json'}),
          ('menu', 'menu', {'.wav','.json'}),
          ('audio-turbo', 'audio/turbo', {'.wav','.json'}),
          ('audio-effects', 'audio/effects', {'.wav','.json'}),
          ('hud', 'hud', {'.json'}),
          ('hummer', 'hummer', {'.json'}),
          ('audio-hummer', 'audio/hummer', {'.wav','.json'}),
          ('turret', 'turret', {'.json','.wav'}))

def asset_files(folder,extensions):
    folder=Path(folder)
    manifest=folder/'tracks.json'
    if folder.name=='radio' and manifest.is_file():
        entries=json.loads(manifest.read_text(encoding='utf-8-sig'))
        if not isinstance(entries,list):raise ValueError('Radio manifest must be a track list')
        selected=[manifest];seen=set()
        for entry in entries:
            name=entry.get('file') if isinstance(entry,dict) else None
            if not isinstance(name,str) or not name or Path(name).name!=name or '/' in name or '\\' in name or ':' in name or Path(name).suffix.lower() not in ('.wav','.ogg'):
                raise ValueError('Invalid radio filename in manifest')
            path=folder/name
            if name.lower() in seen or path.is_symlink() or not path.is_file():
                raise ValueError('Duplicate or missing radio file: '+name)
            selected.append(path);seen.add(name.lower())
        return selected
    return sorted(p for p in folder.glob('*') if p.is_file() and not p.is_symlink() and p.suffix.lower() in extensions)

def payload_files(root):
    root=Path(root)
    for name in REQUIRED:
        path=root/name
        if not path.is_file():raise FileNotFoundError('Build the mod first: '+name)
        yield Path(name)
    for _,folder,extensions in OPTIONAL:
        for path in asset_files(root/folder,extensions):
            yield path.relative_to(root)

def sync_optional_assets(local,output):
    output=Path(output).resolve()
    for source_name,folder,extensions in OPTIONAL:
        source=Path(local)/source_name
        target=(output/folder).resolve()
        if not target.is_relative_to(output):raise ValueError('Asset target escaped build directory')
        # Remove only this generator's allowlisted stale files in its fixed folder.
        target.mkdir(parents=True,exist_ok=True)
        wanted={p.name:p for p in asset_files(source,extensions)}
        for path in target.iterdir():
            if path.is_file() and path.suffix.lower() in extensions and path.name not in wanted:path.unlink()
        for name,path in wanted.items():shutil.copy2(path,target/name)
