"""Assemble the original DDAY_PLAYER_CAR visual preset from local UG2 assets.

No handling parameters are changed. Colour response is calibrated for Valheim;
the mesh choices and ART_F_6 design come from the user's game files.
"""
import base64
import json
import math
import struct
from pathlib import Path
import numpy as np
from PIL import Image
from project import LOCAL
from export_geometry import parse
from nfs_formats import bin_hash, jdlz, TexturePack

ASSETS=LOCAL/'nfs-assets'
PAINT=(15,108,49)
# Visual fit to KITW02's wheel arches. Collider radius/axles remain untouched.
WHEEL_RADIUS=.35
WHEEL_LIFT=WHEEL_RADIUS-.34
BODY_LIFT=.04
FRONT_WHEEL_OFFSET=.075
REAR_WHEEL_OFFSET=.06


def wheel_source_radius(part):
    # The INTERIOR slice contains a square atlas backing plane. Its diagonal
    # is not a tyre radius. Measure only vertices used by RUBBER triangles.
    ids={i for section in part['slices'] if section['materialKey']==0x78743ca1 for i in section['indices']}
    if not ids: raise ValueError('Wheel has no rubber surface to measure')
    radius=max(math.hypot(part['vertices'][i][0],part['vertices'][i][2]) for i in ids)
    if not math.isfinite(radius) or not .1 < radius < 1: raise ValueError('Invalid source tyre radius')
    return radius


def main():
    out=LOCAL/'model';out.mkdir(parents=True,exist_ok=True)
    global_data=jdlz((ASSETS/'GLOBAL/GlobalB.lzc').read_bytes())
    global_path=LOCAL/'global-decompressed.bin';global_path.write_bytes(global_data)
    position=global_data.index(b'DDAY_PLAYER_CAR\0')-40
    if global_data[position+8:position+13]!=b'350Z\0': raise ValueError('Unexpected preset model')
    preset=lambda offset:struct.unpack_from('<I',global_data,position+offset)[0]
    expected={0x64:'350Z_KITW02_BODY',0x70:'350Z_STYLE07_HOOD_CF',0x7c:'SPOILER_STYLE02',0x84:'350Z_STYLE02_HEADLIGHT',0x88:'350Z_STYLE04_BRAKELIGHT',0x8c:'EXHAUST_STYLE08_LEVEL1',0xc0:'RACINGHART_STYLE07_20_26',0x148:'METAL_L2_COLOR10',0x14c:'ART_F_6'}
    for offset,name in expected.items():
        if preset(offset)!=bin_hash(name): raise ValueError(f'Unexpected DDAY preset field {offset:x}; re-identify its parts')
    car=parse(ASSETS/'CARS/350Z/GEOMETRY.BIN')
    def pick(parts,name):
        matches=[p for p in parts if p['key']==bin_hash(name+'_A')]
        if len(matches)!=1: raise ValueError('Missing/ambiguous LOD A: '+name)
        return matches[0]
    families=['350Z_BASE','350Z_KITW02_BODY','350Z_KITW02_DOOR_LEFT','350Z_KITW02_DOOR_RIGHT',
              '350Z_KITW02_DOOR_PANEL_LEFT','350Z_KITW02_DOOR_PANEL_RIGHT','350Z_KITW02_DOOR_SILL_LEFT','350Z_KITW02_DOOR_SILL_RIGHT',
              '350Z_STYLE07_HOOD','350Z_KIT00_ROOF','350Z_KIT00_TRUNK','350Z_KIT00_LEFT_SIDE_MIRROR','350Z_KIT00_RIGHT_SIDE_MIRROR',
              '350Z_STYLE02_HEADLIGHT_LEFT','350Z_STYLE02_HEADLIGHT_RIGHT','350Z_STYLE04_BRAKELIGHT']
    selected=[(pick(car,n),'body',None,n) for n in families]
    spoiler=pick(parse(ASSETS/'CARS/SPOILER/GEOMETRY.BIN'),'SPOILER_STYLE02')
    socket=pick(car,'350Z_BASE')['sockets'][0xc93b73fd]
    selected.append((spoiler,'body',socket,'SPOILER_STYLE02'))
    exhaust=pick(parse(ASSETS/'CARS/EXHAUST/GEOMETRY.BIN'),'EXHAUST_STYLE08_LEVEL1')
    for key in (0xbcf8a18b,0xbd7cf15e):
        selected.append((exhaust,'body',pick(car,'350Z_KITW02_BODY')['sockets'][key],'EXHAUST_STYLE08_LEVEL1'))
    rim=pick(parse(ASSETS/'CARS/WHEELS/GEOMETRY_RACINGHART.BIN'),'RACINGHART_STYLE07_20_26')
    source_radius=wheel_source_radius(rim)
    selected.append((rim,'wheel',None,'RACINGHART_STYLE07_20_26'))
    selected.append((pick(car,'350Z_KIT00_FRONT_BRAKE'),'brake',None,'350Z_KIT00_FRONT_BRAKE'))
    packs=[TexturePack(ASSETS/'CARS/350Z/TEXTURES.BIN'),TexturePack(ASSETS/'CARS/WHEELS/TEXTURES.BIN'),TexturePack(global_path)]
    vinyls=TexturePack(ASSETS/'CARS/350Z/VINYLS.BIN')
    _,vinyl,_=vinyls.decode(bin_hash('350Z_ART_F_6'))
    _,mask,_=vinyls.decode(bin_hash('350Z_ART_F_6_MASK'))
    # Keep the authored layout and colour palette, rather than generating decals.
    # Both design and coverage are palette indices, not raw grayscale values.
    # Use the original embedded RGBA palettes, including their antialiasing.
    coverage=np.array(mask.convert('RGBA'))[:,:,0].astype(float)/255
    rgb=np.array(vinyl.convert('RGBA'))[:,:,:3].astype(float)
    rgb=rgb*coverage[:,:,None]+np.array(PAINT)*(1-coverage[:,:,None])
    paint_image=Image.fromarray(np.uint8(np.clip(rgb,0,255)),'RGB').convert('RGBA')
    textures=[];texture_ids={};materials=[];material_ids={}
    def texture(name,image):
        if name not in texture_ids:
            image=image.convert('RGBA');texture_ids[name]=len(textures)
            textures.append({'name':name,'width':image.width,'height':image.height,'rgba':base64.b64encode(image.tobytes()).decode()})
            image.save(out/(name+'.png'))
        return texture_ids[name]
    vinyl_id=texture('rachel_paint',paint_image)
    def source_texture(key):
        for pack in packs:
            if key in pack.slots or key in pack.records:
                name,image,_=pack.decode(key)
                # Opaque mesh slices use atlas RGB; transparent covers are
                # omitted below so lens housings stay visible and correctly lit.
                image=image.convert('RGB').convert('RGBA')
                return texture(f'tex_{key:08x}',image)
        return -1
    def material(key,tex,group,hood):
        identity=(key,tex,group,hood)
        if identity in material_ids:return material_ids[identity]
        color=[1.,1.,1.,1.];metal=.05;smooth=.45
        if key==0xd6d6080a:
            metal=.55;smooth=.7
            if hood:tex=source_texture(0x721aff7c);color=[.22,.24,.25,1.]
            elif group=='body':tex=vinyl_id
        elif key in (0x22719fa9,0xfd102a92):color=[.66,.45,.16,1.];metal=.85;smooth=.8
        elif key in (0x54949afd,0x2e65e067):color=[.75,.77,.8,1.];metal=.85;smooth=.8
        elif key in (0x721aff7c,0xb57aef2e):color=[.22,.24,.25,1.];metal=.35;smooth=.65
        elif key==0x2dddad9:color=[.08,.08,.09,1.];metal=.2;smooth=.25
        elif key in (0x471a1dca,0x1ff8c329):color=[.08,.13,.16,1.];metal=.5;smooth=.95;tex=-1
        elif key in (0x78743ca1,0x12c9453c,0x0fedee40,0x010cb64a,0x2787edab,0x3ed70c43):
            color=[.16,.16,.18,1.] if tex>=0 else [.035,.035,.045,1.];smooth=.15
        elif tex<0:color=[.12,.12,.14,1.]
        desc={'name':f'{key:08x}_{len(materials)}','color':color,'metallic':metal,'smoothness':smooth,'texture':tex}
        material_ids[identity]=len(materials);materials.append(desc);return len(materials)-1
    parts=[]
    def transform(v,matrix,exhaust=False,normal=False):
        x,y,z=v
        if exhaust:x,y,z=z,y,-x
        if matrix:
            v=[x*matrix[a]+y*matrix[4+a]+z*matrix[8+a]+(0 if normal else matrix[12+a]) for a in range(3)]
            x,y,z=v
        return {'x':y,'y':z+(0 if normal else BODY_LIFT),'z':x}
    for p,group,matrix,family in selected:
        for number,s in enumerate(p['slices']):
            key=s['materialKey']
            if key in (0xa6348ee3,0xd79597d6,0x33a26cb6):continue # clear lens cover; render its detailed housing instead
            tex=-1 if key==0xd6d6080a else source_texture(s['textureKey'])
            if group in ('wheel','brake'):
                # Source wheel axis +Y becomes Unity +X; the disk extends
                # inward from its outer face. No WheelCollider changes.
                scale=WHEEL_RADIUS/source_radius if group=='wheel' else 1
                offset=.165 if group=='wheel' else -.035
                vertices=[{'x':(-v[1]+offset)*scale,'y':v[2]*scale,'z':v[0]*scale} for v in p['vertices']]
                normals=[{'x':-v[1],'y':v[2],'z':v[0]} for v in p['normals']]
            else:
                vertices=[transform(v,matrix,'EXHAUST' in family) for v in p['vertices']]
                normals=[transform(v,matrix,'EXHAUST' in family,True) for v in p['normals']]
            ids=s['indices'][:]
            if group=='wheel' and key==0x010cb64a:
                # The rear atlas quad needs the original game's alpha mask.
                # Omit only its out-of-circle triangles; keep the barrel.
                ids=[index for j in range(0,len(ids),3)
                     if all(math.hypot(p['vertices'][i][0],p['vertices'][i][2])<=source_radius*1.01 for i in ids[j:j+3])
                     for index in ids[j:j+3]]
                if not ids:continue
            # Wheel conversion includes a reflection. Socket reflection is
            # measured separately; preserve exterior winding for both.
            determinant=1
            if matrix:
                m=matrix;determinant=m[0]*(m[5]*m[10]-m[6]*m[9])-m[4]*(m[1]*m[10]-m[2]*m[9])+m[8]*(m[1]*m[6]-m[2]*m[5])
            if group in ('wheel','brake') or determinant<0:
                for i in range(0,len(ids),3):ids[i+1],ids[i+2]=ids[i+2],ids[i+1]
            parts.append({'name':family+f'_A_slice{number}_{len(parts)}','group':group,'vertices':vertices,'normals':normals,
                          'uv':[{'u':u,'v':1-v} for u,v in p['uv']], 'indices':ids,
                          'material':material(key,tex,group,'HOOD' in family)})
    model={'schemaVersion':2,'preset':'DDAY_PLAYER_CAR','wheelVisualLift':WHEEL_LIFT,
           'frontWheelVisualOffset':FRONT_WHEEL_OFFSET,'rearWheelVisualOffset':REAR_WHEEL_OFFSET,
           'previewHubHeight':.20,'parts':parts,'materials':materials,'textures':textures}
    (out/'body.json').write_text(json.dumps(model,separators=(',',':')),encoding='utf-8')
    (out/'provenance.json').write_text(json.dumps({'preset':model['preset'],'verifiedFields':expected,'paint':'METAL_L2_COLOR10; calibrated Valheim RGB','vinyl':'350Z_ART_F_6 + mask; original embedded RGBA palettes','parts':families,
        'visualFit':{'wheelRadius':WHEEL_RADIUS,'bodyLift':BODY_LIFT,'frontOffset':FRONT_WHEEL_OFFSET,'rearOffset':REAR_WHEEL_OFFSET,'preview':'approximate terrain-mode loaded suspension; not a Unity simulation'}},indent=2),encoding='utf-8')
    print(f'Rachel preset: {len(parts)} material slices, {sum(len(p["indices"])//3 for p in parts)} triangles, {len(textures)} textures')


if __name__=='__main__':main()
