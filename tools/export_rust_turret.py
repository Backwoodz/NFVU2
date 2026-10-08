"""Import the user's local Rust AutoTurret and default AK into ignored assets.

Original meshes, diffuse textures, pivots and sounds; recoil/mount fit are
NFVU2 adaptations. No Rust assemblies/scripts or original bundles are copied.
"""
import base64,io,json,wave
from project import LOCAL
from rust_bundle import load_bundles
import numpy as np
from UnityPy.helpers.MeshHelper import MeshHandler

def mono_wav(data):
    """Keep source timing/rate, downmix for a positional Unity emitter."""
    with wave.open(io.BytesIO(data),'rb') as src:
        if src.getsampwidth()!=2:raise ValueError('Expected PCM16 Rust sample')
        samples=np.frombuffer(src.readframes(src.getnframes()),dtype='<i2').reshape(-1,src.getnchannels())
        pcm=np.rint(samples.astype(np.float64).mean(axis=1)).astype('<i2').tobytes();rate=src.getframerate()
    result=io.BytesIO()
    with wave.open(result,'wb') as dst:
        dst.setnchannels(1);dst.setsampwidth(2);dst.setframerate(rate);dst.writeframes(pcm)
    return result.getvalue()

def vec(d):return np.array([d[k] for k in ('x','y','z')],dtype=float)
def xyz(v):return dict(zip(('x','y','z'),map(float,v)))
def matrix(t):
    q=t['m_LocalRotation'];x,y,z,w=[q[k] for k in ('x','y','z','w')]
    r=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
                [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
                [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
    m=np.eye(4);m[:3,:3]=r@np.diag(vec(t['m_LocalScale']));m[:3,3]=vec(t['m_LocalPosition']);return m

def main():
    env=load_bundles(['assetscenes.bundle','content.bundle','audio.bundle',*[f'textures.{i}.bundle' for i in range(5)]])
    print('Loaded Rust object indexes (payloads read on demand)',flush=True)
    scene=env.files['BuildPlayer-AssetScene-prefabs']
    def resolve(file,ptr):
        if not ptr['m_PathID']:return None
        target=file if ptr['m_FileID']==0 else env.cabs[file.externals[ptr['m_FileID']-1].path.split('/')[-1].lower()]
        return target.objects[ptr['m_PathID']]
    wanted={'assets/prefabs/npc/autoturret/autoturret_deployed.prefab':None,'assets/prefabs/weapons/ak47u/ak47u.worldmodel.prefab':None,
        'assets/prefabs/weapon mods/lasersight/lasersight.entity.prefab':None}
    for o in scene.objects.values():
        if o.type.name=='GameObject':
            name=o.peek_name()
            if name in wanted:wanted[name]=o
        if all(wanted.values()):break
    if not all(wanted.values()):raise ValueError('Required original Rust prefabs missing')
    out=LOCAL/'turret';out.mkdir(exist_ok=True)
    parts=[];materials=[];textures=[];mat_ids={};tex_ids={};transforms={}
    def material(obj):
        key=(obj.assets_file.name,obj.path_id)
        if key in mat_ids:return mat_ids[key]
        data=obj.parse_as_dict();props=data['m_SavedProperties'];envs=dict(props['m_TexEnvs'])
        tex=resolve(obj.assets_file,envs['_MainTex']['m_Texture']) if '_MainTex' in envs else None
        tk=(tex.assets_file.name,tex.path_id) if tex else None
        if tex and tk not in tex_ids:
            image=tex.parse_as_object().image.convert('RGBA');limit=1024 if 'sentry' in data['m_Name'].lower() else 512
            image.thumbnail((limit,limit));image.putalpha(255);tid=len(textures);tex_ids[tk]=tid
            image.save(out/f'texture_{tid}.png')
            textures.append(dict(name=f'texture_{tid}',width=image.width,height=image.height,rgba=base64.b64encode(image.tobytes()).decode()))
        index=len(materials);mat_ids[key]=index
        materials.append(dict(name=data['m_Name'],color=[1,1,1,1],metallic=.15,smoothness=.4,texture=tex_ids[tk] if tex else -1));return index
    yaw=pitch=None
    def walk(go,parent,role,gun=False):
        nonlocal yaw,pitch
        d=go.parse_as_dict();name=d['m_Name'];components={}
        for c in d['m_Component']:
            o=resolve(go.assets_file,c['component']);components[o.type.name]=o
        trans=components['Transform'].parse_as_dict();world=parent@matrix(trans);transforms[name]=world
        if name=='sentry.yaw':role='yaw';yaw=world[:3,3].copy()
        if name=='sentry.pitch':role='pitch';pitch=world[:3,3].copy()
        use=(name=='lasersight_LOD0' if role=='laser' else ('ak47_' in name if gun else name in ('sentry.base_LOD0','sentry.yaw_LOD0','sentry.pitch_LOD0'))) and name.endswith('LOD0')
        if use and 'MeshFilter' in components:
            mf=components['MeshFilter'];mesh=resolve(mf.assets_file,mf.parse_as_dict()['m_Mesh']).parse_as_object();handler=MeshHandler(mesh);handler.process()
            rend=components['MeshRenderer'];mats=rend.parse_as_dict()['m_Materials']
            v=np.array(handler.m_Vertices);v=v@world[:3,:3].T+world[:3,3]
            offset=np.zeros(3) if role in ('base','gun','laser') else yaw if role=='yaw' else pitch
            v-=offset
            n=np.array(handler.m_Normals)[:,:3]@np.linalg.inv(world[:3,:3]);n/=np.maximum(np.linalg.norm(n,axis=1)[:,None],1e-12)
            for i,tri in enumerate(handler.get_triangles()):
                mid=material(resolve(rend.assets_file,mats[i]))
                parts.append(dict(name=role+'|'+name+'_'+str(i),group='body',lightRole='',vertices=[xyz(p) for p in v],normals=[xyz(p) for p in n],
                    uv=[dict(u=float(p[0]),v=float(p[1])) for p in handler.m_UV0],indices=[int(j) for t in tri for j in t],material=mid))
        for c in trans['m_Children']:
            child=resolve(go.assets_file,c);walk(resolve(child.assets_file,child.parse_as_dict()['m_GameObject']),world,role,gun)
    walk(next(iter(wanted.values())),np.eye(4),'base')
    yaw=transforms['sentry.yaw'][:3,3];pitch=transforms['sentry.pitch'][:3,3]
    # Dropped worldmodel lies on its side: barrel -X, magazine +Z.
    # Map barrel to +Z and magazine down (-Y), centre the bore on the cradle.
    align=np.array([[0,1,0,.0055],[0,0,-1,0],[-1,0,0,-.12],[0,0,0,1]],dtype=float)
    walk(list(wanted.values())[1],align,'gun',True)
    muzzle=transforms['barrel'][:3,3]
    laser_mount=np.eye(4);laser_mount[:3,:3]*=1.1;laser_mount[:3,3]=[0,.025,.13]
    walk(list(wanted.values())[2],laser_mount,'laser')
    laser=transforms['effectBone'][:3,3]
    model=dict(schemaVersion=2,preset='RUST_AUTOTURRET_AK',parts=parts,materials=materials,textures=textures,
        turretRig=dict(yawPivot=xyz(yaw),pitchOffset=xyz(pitch-yaw),muzzle=xyz(muzzle),laser=xyz(laser)))
    (out/'turret.json').write_text(json.dumps(model,separators=(',',':')),encoding='utf-8')
    # Resolve clips by original name rather than installation-specific path IDs.
    audio=next(f for f in env.files.values() if hasattr(f,'objects') and any(o.type.name=='AudioClip' for o in list(f.objects.values())[:20]))
    wanted_sounds={'ak47-gunshot-body-01':'shot1.wav','ak47-gunshot-body-02':'shot2.wav','ak47-gunshot-body-03':'shot3.wav',
        'ak47-gunshot-tail-outdoor-01':'tail.wav','autoturret-movement-loop':'servo.wav','autoturret-reload-full':'reload.wav','autoturret-turn-on':'online.wav',
        'explosive-ammo-close':'explosion.wav'}
    found=[]
    for obj in audio.objects.values():
        if obj.type.name!='AudioClip':continue
        name=obj.peek_name()
        if name not in wanted_sounds:continue
        samples=obj.parse_as_object().samples
        if len(samples)!=1:raise ValueError('Ambiguous audio clip '+name)
        (out/wanted_sounds[name]).write_bytes(mono_wav(next(iter(samples.values()))));found.append(name)
    if set(found)!=set(wanted_sounds):raise ValueError('Missing original Rust clips')
    (out/'provenance.json').write_text(json.dumps(dict(source=list(wanted),sounds=wanted_sounds,
        importTool='UnityPy 1.25.4; read-only UnityFS slices',animation='Original yaw/pitch pivots. AK worldmodel is static; recoil authored in NFVU2.',
        adaptation='Roof mount and scale, AK barrel -X to +Z and magazine +Z to -Y, centred bore and -12cm seating offset, diffuse-only materials, mono audio downmix; gameplay/effects authored for Valheim.'),indent=2),encoding='utf-8')
    print('Exported',len(parts),'mesh sections,',len(textures),'textures,',len(found),'sounds; muzzle',muzzle,flush=True)

if __name__=='__main__':main()
