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
WHEEL_WIDTH=.28
BRAKE_RADIUS=.19
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


def wheel_geometry(part):
    ids={i for s in part['slices'] if s['materialKey']==0x78743ca1 for i in s['indices']}
    axial=[part['vertices'][i][1] for i in ids]
    width=max(axial)-min(axial)
    if not .05 < width < .5:raise ValueError('Invalid original tyre width')
    radial_scale=WHEEL_RADIUS/wheel_source_radius(part)
    axial_scale=WHEEL_WIDTH/width
    outer_face=.165*radial_scale
    vertices=[{'x':outer_face-(v[1]-min(axial))*axial_scale,'y':v[2]*radial_scale,'z':v[0]*radial_scale} for v in part['vertices']]
    normals=[]
    for nx,ny,nz in part['normals']:
        n=(-ny/axial_scale,nz/radial_scale,nx/radial_scale)
        length=math.sqrt(sum(c*c for c in n))
        normals.append(dict(zip(('x','y','z'),(c/length for c in n))) if length else {'x':0,'y':1,'z':0})
    return vertices,normals


def wheel_finish_sections(part,section):
    if section['materialKey']!=0xfd102a92:return [(section,'')]
    groups={'wheel_chrome':[],'wheel_gold':[]}
    for j in range(0,len(section['indices']),3):
        tri=section['indices'][j:j+3]
        # The original material merges lip/barrel and spokes. Existing radial
        # loops divide the outer machined ring from the five-point star.
        outer=min(math.hypot(part['vertices'][i][0],part['vertices'][i][2]) for i in tri)>=.2319
        groups['wheel_chrome' if outer else 'wheel_gold'].extend(tri)
    return [({**section,'indices':ids},finish) for finish,ids in groups.items() if ids]


def exhaust_mouths(part):
    # Two eight-sided mouth loops occupy the minimum longitudinal plane.
    end=min(v[0] for v in part['vertices'])
    mouths=[]
    for side in (-1,1):
        ring={tuple(v) for v in part['vertices'] if abs(v[0]-end)<.00001 and v[1]*side>0}
        if len(ring)!=8:raise ValueError('Re-identify STYLE08 exhaust mouth loops')
        mouths.append(tuple(sum(v[a] for v in ring)/len(ring) for a in range(3)))
    return mouths


def sport_brake_geometry(part):
    # Original STYLE03 uses an alpha-masked square for the drilled rotor.
    # Rebuild only that square as a round, thin rotor so opaque Unity materials
    # cannot expose black square corners; keep its original atlas and caliper.
    part={**part,'vertices':part['vertices'][:],'normals':part['normals'][:],'uv':part['uv'][:],
          'slices':[s for s in part['slices'] if s['materialKey']!=0xb1aae6bb]}
    indices=[];count=64
    for back,axial in ((False,-.008),(True,.006)):
        center=len(part['vertices'])
        part['vertices'].append((0,axial,0));part['normals'].append((0,1 if back else -1,0));part['uv'].append((.5,.5))
        for i in range(count):
            a=2*math.pi*i/count;x=.18*math.cos(a);z=.18*math.sin(a)
            part['vertices'].append((x,axial,z));part['normals'].append((0,1 if back else -1,0));part['uv'].append((.5+x/.36,.5-z/.36))
        for i in range(count):
            tri=[center,center+1+i,center+1+(i+1)%count]
            indices.extend(tri[::-1] if back else tri)
    part['slices'].append({'materialKey':0xb1aae6bb,'textureKey':bin_hash('BRAKE_STYLE03'),'indices':indices})
    return part


def rear_lens_sections(part,family,section):
    """Restore only identified lower lenses, not entire clear headlamp covers."""
    if section['materialKey']!=0x33a26cb6:return [(section,'')]
    triangles=[section['indices'][i:i+3] for i in range(0,len(section['indices']),3)]
    if family=='350Z_STYLE04_BRAKELIGHT':
        ids=[i for tri in triangles if max(part['vertices'][j][2] for j in tri)<.62 for i in tri]
        return [({**section,'indices':ids},'rear_amber_trim')] if ids else []
    if family=='350Z_KITW02_BODY':
        result=[]
        for side in (-1,1):
            for role in ('reverse','rear_amber_trim'):
                ids=[]
                for tri in triangles:
                    vs=[part['vertices'][j] for j in tri]
                    if max(v[0] for v in vs)>-1.8 or side*sum(v[1] for v in vs)<=0:continue
                    # Inner versus outer subdivision follows the stock combined
                    # bumper-lamp arrangement; exact NFS atlas mapping unresolved.
                    inner=sum(abs(v[1]) for v in vs)/3<.59
                    if inner==(role=='reverse'):ids.extend(tri)
                if ids:result.append(({**section,'indices':ids},role))
        return result
    return []


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
    brakes=pick(parse(ASSETS/'CARS/BRAKES/GEOMETRY.BIN'),'BRAKE_STYLE03')
    selected.append((sport_brake_geometry(brakes),'brake',None,'BRAKE_STYLE03'))
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
    _,plate_image,_=packs[2].decode(bin_hash('LICENSE_PLATE'))
    plate_id=texture('original_license_plate',plate_image)
    _,neon_image,_=packs[0].decode(bin_hash('350Z_NEON'))
    neon_id=texture('350Z_NEON',neon_image)
    effects=TexturePack(ASSETS/'GLOBAL/InGameCommon.bun')
    _,flame_image,_=effects.decode(bin_hash('SFX_FIREB'))
    _,purge_image,_=effects.decode(bin_hash('SFX_SMOKE_B_CS'))
    flame_id=texture('SFX_FIREB',flame_image)
    purge_id=texture('SFX_SMOKE_B_CS',purge_image)
    def source_texture(key):
        for pack in packs:
            if key in pack.slots or key in pack.records:
                name,image,_=pack.decode(key)
                # Opaque mesh slices use atlas RGB; transparent covers are
                # omitted below so lens housings stay visible and correctly lit.
                image=image.convert('RGB').convert('RGBA')
                return texture(f'tex_{key:08x}',image)
        return -1
    def material(key,tex,group,hood,appearance=''):
        identity=(key,tex,group,hood,appearance)
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
        if group=='brake':
            if key==0xd6640dff:color=[.8,.025,.018,1.];metal=.45;smooth=.65;tex=-1
            elif key==0xb1aae6bb:color=[.9,.92,.95,1.];metal=.75;smooth=.6
            else:color=[.7,.73,.78,1.];metal=.75;smooth=.55
        if appearance=='reverse':color=[.88,.92,1.,1.];metal=.05;smooth=.8;tex=-1
        elif appearance=='rear_amber_trim' and tex<0:color=[1.,.3,.025,1.];metal=.05;smooth=.7
        elif appearance=='wheel_chrome':color=[.94,.96,1.,1.];metal=.28;smooth=.76
        elif appearance=='wheel_gold':color=[1.,.72,.28,1.];metal=.22;smooth=.58
        prefix=appearance+'_' if appearance.startswith('wheel_') else ''
        desc={'name':f'{prefix}{key:08x}_{len(materials)}','color':color,'metallic':metal,'smoothness':smooth,'texture':tex}
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
        sections=[entry for section in p['slices'] for entry in (wheel_finish_sections(p,section) if group=='wheel' else rear_lens_sections(p,family,section))]
        for number,(s,lens_role) in enumerate(sections):
            key=s['materialKey']
            if key in (0xa6348ee3,0xd79597d6):continue # full clear covers still omitted
            texture_key=s['textureKey']
            if family=='350Z_STYLE04_BRAKELIGHT' and texture_key in (bin_hash('BRAKELIGHT_LEFT'),bin_hash('BRAKELIGHT_RIGHT')):
                texture_key=bin_hash('350Z_STYLE04_BRAKELIGHT_OFF')
            tex=-1 if key==0xd6d6080a else source_texture(texture_key)
            if group in ('wheel','brake'):
                # Source wheel axis +Y becomes Unity +X; the disk extends
                # inward from its outer face. No WheelCollider changes.
                if group=='wheel':vertices,normals=wheel_geometry(p)
                else:
                    scale=BRAKE_RADIUS/.18
                    vertices=[{'x':(-v[1]+.04)*scale,'y':v[2]*scale,'z':v[0]*scale} for v in p['vertices']]
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
            role='reverse' if lens_role=='reverse' else ''
            if 'HEADLIGHT' in family and key==0x9c645529:role='headlight'
            if 'BRAKELIGHT' in family and key==0x05bc3a3c:role='taillight'
            if 'TRUNK' in family and key==0x05bc3a3c:role='brakelight'
            parts.append({'name':family+f'_A_slice{number}_{len(parts)}','group':group,'lightRole':role,
                          'rotatesWithWheel':group=='brake' and key==0xb1aae6bb,'vertices':vertices,'normals':normals,
                          'uv':[{'u':u,'v':1-v} for u,v in p['uv']], 'indices':ids,
                          'material':material(key,tex,group,'HOOD' in family,lens_role)})
    # Shared plate geometry was not identified. This small authored rectangle
    # uses the original fixed UNDRGND 2 texture at the rear-centre kit socket.
    # The socket's purpose is inferred from its position/orientation, not its hash.
    plate_socket=pick(car,'350Z_KITW02_BODY')['sockets'][0xc805f7e0]
    plate_vertices=[transform((v,u,.008),plate_socket) for u,v in [(-.16,-.08),(-.16,.08),(.16,.08),(.16,-.08)]]
    plate_normal=transform((0,0,1),plate_socket,normal=True)
    plate_material=len(materials)
    materials.append({'name':'original_license_plate','color':[1.,1.,1.,1.],'metallic':.05,'smoothness':.3,'texture':plate_id})
    parts.append({'name':'NFVU2_license_plate_rectangle','group':'body','lightRole':'','vertices':plate_vertices,
                  'normals':[plate_normal]*4,'uv':[{'u':0,'v':0},{'u':0,'v':1},{'u':1,'v':1},{'u':1,'v':0}],
                  'indices':[0,1,2,0,2,3],'material':plate_material})
    lights=[]
    def lamp(role,position,direction):lights.append({'role':role,'position':position,'direction':direction})
    # Original headlamp locator translations; projection direction is adapted for Valheim.
    head_sockets=pick(car,'350Z_STYLE02_HEADLIGHT_LEFT')['sockets']
    for key in (0x9db90133,0xd09091c6):
        lamp('headlight',transform((0,0,0),head_sockets[key]),{'x':0,'y':-.12,'z':1})
    for side in (-1,1):lamp('taillight',{'x':side*.62,'y':.71,'z':-1.97},{'x':0,'y':0,'z':-1})
    for side in (-1,1):lamp('reverse',{'x':side*.49,'y':.45,'z':-2.11},{'x':0,'y':-.12,'z':-1})
    lamp('plate',transform((.10,0,.045),plate_socket),transform((-.5,0,-1),plate_socket,normal=True))
    emitters=[]
    for key in (0xbcf8a18b,0xbd7cf15e):
        socket=pick(car,'350Z_KITW02_BODY')['sockets'][key]
        for mouth in exhaust_mouths(exhaust):
            point=transform((mouth[0]-.006,mouth[1],mouth[2]),socket,exhaust=True)
            emitters.append({'role':'exhaust','position':point,'direction':transform((-1,0,0),socket,exhaust=True,normal=True)})
    # No verified NFS purge sockets were found. Mod-authored bumper positions
    # and hood surface anchors provide paired jets without changing geometry.
    for side in (-1,1):
        emitters.append({'role':'purge','position':{'x':side*.62,'y':.53,'z':2.11},'direction':{'x':side*.3,'y':.65,'z':.7}})
        emitters.append({'role':'purge','position':{'x':side*.412741,'y':.833191,'z':.987397},'direction':{'x':side*.25,'y':1,'z':.12}})
    model={'schemaVersion':2,'preset':'DDAY_PLAYER_CAR','wheelVisualLift':WHEEL_LIFT,'lights':lights,'neonTexture':neon_id,
           'effectEmitters':emitters,'nitroFlameTexture':flame_id,'nitroPurgeTexture':purge_id,
           'frontWheelVisualOffset':FRONT_WHEEL_OFFSET,'rearWheelVisualOffset':REAR_WHEEL_OFFSET,
           'previewHubHeight':.20,'parts':parts,'materials':materials,'textures':textures}
    (out/'body.json').write_text(json.dumps(model,separators=(',',':')),encoding='utf-8')
    (out/'provenance.json').write_text(json.dumps({'preset':model['preset'],'verifiedFields':expected,'paint':'METAL_L2_COLOR10; calibrated Valheim RGB','vinyl':'350Z_ART_F_6 + mask; original embedded RGBA palettes','parts':families,
        'licensePlate':{'texture':'original LICENSE_PLATE / UNDRGND 2','geometry':'NFVU2 rectangle; shared original mesh not identified','placement':'rear-centre kit socket 0xc805f7e0; purpose inferred from geometry'},
        'neon':{'texture':'original 350Z_NEON','projectionAndPulse':'NFVU2 Unity implementation; not recovered NFS timing'},
        'rearLenses':{'upperStrip':'original STYLE04 amber textured trim; passive','bumper':'original lens triangles split into inner reverse and outer amber, split inferred from stock layout; not recovered NFS atlas mapping'},
        'sportBrakes':{'source':'BRAKE_STYLE03_A + original BRAKE_STYLE03 rotor atlas','adaptation':'38cm round opaque rotor replaces alpha-masked square; original caliper geometry painted red; visual only'},
        'wheelFinish':{'source':'original RACINGHART_STYLE07 geometry; its shared wheel material split at existing radial loops','adaptation':'chrome-coloured outer ring, gold-bronze star; reduced metallic plus subtle runtime ambient fill for Valheim'},
        'nitroEffects':{'textures':'original InGameCommon SFX_FIREB and SFX_SMOKE_B_CS; runtime tint/animation adapted','exhaust':'four original STYLE08 mouth-loop centres, offset 6mm outward','purge':'mod-authored bumper pair and measured hood surface pair; original nozzle sockets not identified'},
        'visualFit':{'wheelRadius':WHEEL_RADIUS,'wheelWidth':WHEEL_WIDTH,'wheelWidthFit':'outer face retained, widened inward; inverse-transpose normals','brakeRadius':BRAKE_RADIUS,'bodyLift':BODY_LIFT,'frontOffset':FRONT_WHEEL_OFFSET,'rearOffset':REAR_WHEEL_OFFSET,'preview':'approximate terrain-mode loaded suspension; not a Unity simulation'}},indent=2),encoding='utf-8')
    print(f'Rachel preset: {len(parts)} material slices, {sum(len(p["indices"])//3 for p in parts)} triangles, {len(textures)} textures')


if __name__=='__main__':main()
