from pathlib import Path
import json

BASE=Path(__file__).resolve().parents[1]
LOCAL=BASE/'.local'
OUTPUT=BASE/'artifacts'
LOCAL.mkdir(exist_ok=True)
OUTPUT.mkdir(exist_ok=True)

def config():
    path=BASE/'config.local.json'
    if not path.exists():raise SystemExit('Copy config.example.json to config.local.json and set your local paths.')
    return json.loads(path.read_text(encoding='utf-8-sig'))

def configured_path(key):
    value=Path(config()[key]).expanduser()
    return (value if value.is_absolute() else BASE/value).resolve()

def valheim_dir():return configured_path('valheim_dir')
def disc_root():return configured_path('disc_root')
def vgmstream_path():return configured_path('vgmstream')
