"""Import a yellow stock H2 and original 26-inch spinner meshes from local UG2 data."""
import base64
import json
import math
import struct
import zipfile
from project import LOCAL, disc_root
from inspect_discs import Disc
from extract_assets import DiscFile
from export_geometry import parse
from export_rachel import wheel_source_radius, sport_brake_geometry
from nfs_formats import bin_hash, TexturePack, jdlz

ASSETS=LOCAL/'nfs-assets'
LIFT=.30
RADIUS=.4364
WIDTH=.34
RIM='SPINNER_STYLE01_26_34'

def extract():
    wanted=('CARS/HUMMER/','CARS/MIRRORS_HUMMER/')
    sounds=('SOUND/ENGINE/GIN_HUMMER.GIN',)
    for image in disc_root().glob('CD*/*.bin'):
        disc=Disc(image)
        try:
            for rec in disc.walk():
                if rec['name']!='COMPRESS.ZIP':continue
                with zipfile.ZipFile(DiscFile(disc,rec)) as archive:
                    for item in archive.infolist():
                        name=item.filename.replace('\\','/');upper=name.upper()
                        if item.is_dir() or not (upper.startswith(wanted) or upper in sounds or upper=='CARS/WHEELS/GEOMETRY_SPINNER.BIN'):continue
                        target=(ASSETS/name).resolve()
                        if not target.is_relative_to(ASSETS.resolve()):raise ValueError('Unsafe source path')
                        target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(archive.read(item))
        finally:disc.f.close()

def pick(parts,name):
    matches=[p for p in parts if p['key']==bin_hash(name+'_A')]
    if len(matches)!=1:raise ValueError('Missing/ambiguous original mesh: '+name)
    return matches[0]

def main():
    extract()
    out=LOCAL/'hummer';out.mkdir(parents=True,exist_ok=True)
    car=parse(ASSETS/'CARS/HUMMER/GEOMETRY.BIN')
    families=['HUMMER_BASE']+['HUMMER_KIT00_'+n for n in ('BODY','FRONT_BUMPER','REAR_BUMPER','SKIRT','HOOD','ROOF','TRUNK','BRAKELIGHT','DOOR_LEFT','DOOR_RIGHT','DOOR_PANEL_LEFT','DOOR_PANEL_RIGHT','HEADLIGHT_LEFT','HEADLIGHT_RIGHT','LEFT_SIDE_MIRROR','RIGHT_SIDE_MIRROR')]
    selected=[(pick(car,n),'body',n) for n in families]
    rims=parse(ASSETS/'CARS/WHEELS/GEOMETRY_SPINNER.BIN');rim=pick(rims,RIM)
    selected.extend([(rim,'wheel',RIM),(pick(rims,RIM+'_SPIN'),'spinner',RIM+'_SPIN')])
    selected.append((sport_brake_geometry(pick(parse(ASSETS/'CARS/BRAKES/GEOMETRY.BIN'),'BRAKE_STYLE03')),'brake','BRAKE_STYLE03'))
    global_data=jdlz((ASSETS/'GLOBAL/GlobalB.lzc').read_bytes())
    gp=LOCAL/'global-decompressed.bin';gp.write_bytes(global_data)
    # The car's global record stores four wheel locators at 0x30-byte strides.
    record=global_data.index(b'HUMMER\0');locators=[struct.unpack_from('<3f',global_data,record+0x120+i*0x30) for i in range(4)]
    if not all(.8<abs(y)<1 and 1.4<abs(x)<1.7 and 0<z<.1 for x,y,z in locators):raise ValueError('Re-identify H2 wheel locators')
    packs=[TexturePack(ASSETS/'CARS/HUMMER/TEXTURES.BIN'),TexturePack(ASSETS/'CARS/WHEELS/TEXTURES.BIN'),TexturePack(ASSETS/'CARS/TEXTURES.BIN'),TexturePack(gp),TexturePack(ASSETS/'GLOBAL/InGameCommon.bun')]
    textures=[];materials=[];parts=[];cache={}
    def texture(key):
        if key in cache:return cache[key]
        for pack in packs:
            if key in pack.slots or key in pack.records:
                _,im,_=pack.decode(key)
                im=im.convert('RGBA') if key in (bin_hash('SFX_SMOKE_B_CS'),bin_hash('HUMMER_NEON')) else im.convert('RGB').convert('RGBA')
                name=f'h2_{key:08x}'
                cache[key]=len(textures);textures.append(dict(name=name,width=im.width,height=im.height,rgba=base64.b64encode(im.tobytes()).decode()))
                im.save(out/(name+'.png'));return cache[key]
        return -1
    source_radius=wheel_source_radius(rim)
    rubber={i for s in rim['slices'] if s['materialKey']==0x78743ca1 for i in s['indices']}
    inner=min(rim['vertices'][i][1] for i in rubber);axial=WIDTH/(max(rim['vertices'][i][1] for i in rubber)-inner)
    radial=RADIUS/source_radius
    for p,group,name in selected:
        for number,s in enumerate(p['slices']):
            key=s['materialKey']
            if key in (0xa6348ee3,0xd79597d6):continue
            texture_key=s['textureKey']
            if 'BRAKELIGHT' in name:texture_key=bin_hash('HUMMER_KIT00_BRAKELIGHT_OFF')
            if 'HEADLIGHT' in name and key==0xf7fc7674:texture_key=bin_hash('HUMMER_KIT00_HEADLIGHT_OFF')
            tex=texture(texture_key);color=[1,1,1,1];metal=.08;smooth=.45
            chrome=group=='spinner' or key in (0xfd102a92,0x54949afd,0x2e65e067)
            if key==0xd6d6080a:color=[1,.72,.015,1];tex=-1;metal=.25;smooth=.7
            elif chrome:color=[.94,.96,1,1];metal=.28;smooth=.78;tex=-1
            elif key in (0x471a1dca,0x1ff8c329):color=[.04,.08,.1,1];tex=-1;smooth=.9
            elif key==0x78743ca1:color=[.13,.13,.14,1];smooth=.12
            elif tex<0:color=[.055,.055,.06,1]
            if group=='brake' and key==0xd6640dff:color=[.65,.04,.025,1];tex=-1
            material=len(materials);materials.append(dict(name=('wheel_chrome_' if chrome else '78743ca1_' if key==0x78743ca1 else 'h2_')+name+str(number),color=color,metallic=metal,smoothness=smooth,texture=tex))
            def vertex(v,normal=False):
                x,y,z=v
                if group=='body':r=(y,z+(0 if normal else LIFT),x)
                elif group=='wheel':r=(-y/axial,z/radial,x/radial) if normal else (.17-(y-inner)*axial,z*radial,x*radial)
                elif group=='spinner':r=(-y,z,x) if normal else (.185-y*radial,z*radial,x*radial)
                else:r=(-y,z,x) if normal else ((-y+.04)*1.12,z*1.12,x*1.12)
                if normal:
                    length=math.sqrt(sum(c*c for c in r));r=tuple(c/length for c in r) if length else (0,1,0)
                return dict(zip(('x','y','z'),r))
            ids=s['indices'][:]
            if group=='wheel' and key==0x010cb64a:
                ids=[i for j in range(0,len(ids),3) if all(math.hypot(p['vertices'][k][0],p['vertices'][k][2])<=source_radius*1.01 for k in ids[j:j+3]) for i in ids[j:j+3]]
            if not ids:continue
            if group!='body':
                for i in range(0,len(ids),3):ids[i+1],ids[i+2]=ids[i+2],ids[i+1]
            role='headlight' if 'HEADLIGHT' in name and key==0xf7fc7674 else 'brakelight' if 'TRUNK' in name and key==0x05bc3a3c else ''
            uv=[dict(u=u,v=1-v) for u,v in p['uv']]
            if name=='HUMMER_KIT00_REAR_BUMPER' and texture_key==bin_hash('HUMMER_BADGING'):
                # HUM and MER occupy two different atlas rows and two body quads.
                # Mirror the complete word in geometry space, exchanging those rows too.
                letter_ids={i for i in ids if .30<p['vertices'][i][2]<.41 and abs(p['vertices'][i][1])<.42}
                if len(letter_ids)!=8:raise ValueError('Re-identify rear HUMMER lettering')
                upper={i for i in letter_ids if p['uv'][i][1]<.511}
                lower=letter_ids-upper
                if len(upper)!=4 or len(lower)!=4:raise ValueError('Re-identify split HUM/MER atlas islands')
                original=[dict(x) for x in uv]
                for i in letter_ids:
                    x,y,z=p['vertices'][i]
                    j=min(lower if i in upper else upper,key=lambda j:sum((a-b)**2 for a,b in zip(p['vertices'][j],(x,-y,z))))
                    uv[i]=original[j]
            groups={role:ids}
            if 'BRAKELIGHT' in name and key==0x05bc3a3c:
                groups={'taillight':[],'reverse':[],'':[]}
                for j in range(0,len(ids),3):
                    tri=ids[j:j+3];u=sum(p['uv'][i][0] for i in tri)/3
                    groups['taillight' if u>.55 else 'reverse' if u<.32 else ''].extend(tri)
            for lamp,triangles in groups.items():
                if not triangles:continue
                parts.append(dict(name=name+'_A_'+str(number)+'_'+lamp,group=group,lightRole=lamp,material=material,vertices=[vertex(v) for v in p['vertices']],normals=[vertex(v,True) for v in p['normals']],uv=uv,indices=triangles,rotatesWithWheel=group=='brake' and key==0xb1aae6bb))
    lights=[]
    for role,x,y,z,d in [('headlight',.65,1.0,2.24,1),('taillight',.81,1.16,-2.29,-1),('reverse',.81,.91,-2.3,-1)]:
        for side in (-1,1):lights.append(dict(role=role,position=dict(x=side*x,y=y,z=z),direction=dict(x=0,y=-.12,z=d)))
    model=dict(schemaVersion=2,preset='NFVU2_HUMMER_YELLOW_SPINNER26',parts=parts,materials=materials,textures=textures,lights=lights,neonTexture=texture(bin_hash('HUMMER_NEON')),nitroFlameTexture=-1,nitroPurgeTexture=texture(bin_hash('SFX_SMOKE_B_CS')),previewHubHeight=locators[0][2]+LIFT)
    (out/'h2.json').write_text(json.dumps(model,separators=(',',':')),encoding='utf-8')
    (out/'provenance.json').write_text(json.dumps(dict(body=families,spinner=RIM+'_SPIN_A',wheel=RIM+'_A',wheelLocators=locators,bodyLift=LIFT,tyreRadius=RADIUS,tyreWidth=WIDTH,paint='NFVU2 yellow; no vinyl',spinnerFit='Original separate spinner mesh; axial placement adapted to widened tyre. Runtime inertia is NFVU2, not recovered NFS animation.',excluded='No engine models, trunk upgrades or door animations.'),indent=2),encoding='utf-8')
    print('H2:',len(parts),'sections;',len(textures),'textures; original wheel locators',locators)

if __name__=='__main__':main()
