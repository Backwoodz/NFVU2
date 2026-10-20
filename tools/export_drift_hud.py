"""Prepare the retail drift-score backing locally; game fonts are not copied."""
import hashlib
import json
from project import LOCAL
from nfs_formats import TexturePack
from export_hud import rgba


def export():
    source=LOCAL/'nfs-assets/GLOBAL/InGameDrift.bun'
    name,image,_=TexturePack(source).decode(0x51052532)
    if name!='DRIFT_STYLE_POINTS_BACK' or image.size!=(256,32):
        raise ValueError('Unexpected drift score artwork')
    target=LOCAL/'hud';target.mkdir(exist_ok=True)
    (target/'drift-score.json').write_text(json.dumps(rgba(name,image),separators=(',',':')))
    (target/'drift-score-provenance.json').write_text(json.dumps({
        'source':'GLOBAL/InGameDrift.bun','sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
        'texture_key':'0x51052532','texture_name':name,
        'adaptation':'Original score backing; NFVU2 authors scoring, layout and labels. Font comes from the running Valheim HUD.'},indent=2))
    print('Prepared original drift score backing in .local/hud/')


if __name__=='__main__':export()
