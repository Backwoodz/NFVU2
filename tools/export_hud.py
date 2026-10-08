"""Import eleven original NFSU2 gauge designs and NOS artwork; local output only.

Texture names are truncated in the archive, so keys identify the exact variant.
Layout, colours and live values are adapted by the Valheim HUD, not NFS scripts.
"""
import base64,json
from project import LOCAL
from nfs_formats import TexturePack,bin_hash

def rgba(name,image):
    image=image.convert('RGBA')
    return dict(name=name,width=image.width,height=image.height,rgba=base64.b64encode(image.tobytes()).decode())

def export():
    base=LOCAL/'nfs-assets/GLOBAL';designs=[]
    for i in range(11):
        suffix=f'{i:02d}';pack=TexturePack(base/f'HUD_CustomTextures_{suffix}.bin')
        def read(prefix):return pack.decode(bin_hash(prefix+suffix))[1].convert('RGBA')
        # The original UI puts the artwork in the top-left 140px (70px for 10)
        # of a padded texture. Preserve that authored square, not the empty pad.
        size=70 if i==10 else 140
        fill=read('TACH_FILL_CUSTOM_').crop((0,0,size,size))
        lines=read('3RDPERSON_7500LINES_CUSTOM_')
        needle=read('3RDPERSON_RPMDIAL_CUSTOM_').crop((0,56,16,128))
        designs.append(dict(name=f'NFSU2 {i+1:02d}',fill=rgba('fill',fill),lines=rgba('lines',lines),needle=rgba('needle',needle)))
    race=TexturePack(base/'InGameRace.bun')
    data=dict(schemaVersion=1,designs=designs,
              nitro=rgba('TACH_NOS_ALPHA',race.decode(bin_hash('TACH_NOS_ALPHA'))[1]),
              nitroBacking=rgba('TACH_NOS_BACKING',race.decode(bin_hash('TACH_NOS_BACKING'))[1]))
    out=LOCAL/'hud';out.mkdir(exist_ok=True)
    (out/'hud.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
    (out/'provenance.json').write_text(json.dumps({'source':'GLOBAL/HUD_CustomTextures_00..10.bin and GLOBAL/InGameRace.bun',
        'art':'Original TACH_FILL_CUSTOM, 7500LINES, RPMDIAL, TACH_NOS_ALPHA and TACH_NOS_BACKING',
        'adaptation':'Padded canvas cropped; Unity layout, colours, NOS fill and telemetry authored for NFVU2. RPM/gear follows the acoustic gearbox.'},indent=2),encoding='utf-8')
    print('Exported 11 original gauge designs and NOS textures to .local/hud/hud.json')
    return data

if __name__=='__main__':export()
