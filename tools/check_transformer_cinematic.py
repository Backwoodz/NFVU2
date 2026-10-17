"""Audit V18 exterior rigidity, return, ground and priority surface crossings.

Run on the saved blend. --quick uses 13 forward samples; otherwise half frames
through both directions. This does not certify all internal self-collisions.
"""
import hashlib
import json
import sys
from collections import defaultdict
from pathlib import Path
import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
OUT=Path(bpy.data.filepath).parent
scene=bpy.context.scene
build=json.loads((OUT/'cinematic-build.json').read_text())
mapping=json.loads((ROOT/'artifacts/transformer-study-v17/car-module-targets.json').read_text())
old=json.loads((ROOT/'artifacts/transformer-study-v17/rig-audit.json').read_text())
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
groups=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:groups[obj['module']].append(obj)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.get('continuous_unsegmented'):groups[obj.name].append(obj)
groups['RIDER']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
meshes=[o for col in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT','05 Rider - scale reference, NOT game avatar')
        for o in bpy.data.collections[col].objects if o.type=='MESH']
vertices={o.name:np.array([tuple(v.co) for v in o.data.vertices]) for o in meshes}

def hash_shape(obj):
    d=obj.data
    return hashlib.sha256(repr(([tuple(v.co) for v in d.vertices],
        [tuple(p.vertices) for p in d.polygons],
        [tuple(v.uv) for v in d.uv_layers.active.data] if d.uv_layers.active else [])).encode()).hexdigest()

protected=[o for obs in groups.values() for o in obs]
shape_errors=[o.name for o in meshes if o.name in old['baseline'] and hash_shape(o)!=old['baseline'][o.name]['shape']]
scene.frame_set(1)
start={o.name:np.array(o.matrix_world) for o in meshes}
scales={o.name:np.linalg.norm(np.array(o.matrix_world)[:3,:3],axis=0) for o in protected}
car_error=0.
for role,item in mapping.items():
    for name in item['objects']:
        if role.startswith('neon.'):
            expected=Matrix(item['car_world'])@bpy.data.objects[name].matrix_local
        else:
            expected=Matrix(item['car_world'])@Matrix(item['robot_world']).inverted()@Matrix(old['baseline'][name]['world'])
        car_error=max(car_error,float(np.max(np.abs(start[name]-np.array(expected)))))

pairs=[]
for a in groups:
    for b in groups:
        if a>=b:continue
        if ('RIDER' in (a,b) or a.startswith('NEON TUBE') or b.startswith('NEON TUBE') or
            (a.startswith('hood_') and b.startswith('lamp_')) or
            (b.startswith('hood_') and a.startswith('lamp_'))):pairs.append((a,b))
scene.frame_set(1)
reference={k:(geometry(v),np.linalg.inv(np.array(v[0].matrix_world))) for k,v in groups.items() if k!='RIDER'}
def surface_cache():
    result={}
    for name,obs in groups.items():
        if name=='RIDER':
            result[name]=placed(geometry(obs),np.zeros(3));continue
        (points,faces,tris,normals),inverse=reference[name]
        delta=np.array(obs[0].matrix_world)@inverse
        rotation=delta[:3,:3];translation=delta[:3,3]
        data=(points@rotation.T+translation,faces,tris@rotation.T+translation,normals@rotation.T)
        result[name]=placed(data,np.zeros(3))
    return result
baseline={}
for frame in (1,121):
    scene.frame_set(frame)
    data=surface_cache()
    for pair in pairs:
        baseline.setdefault(pair,set())
        if 'RIDER' not in pair:baseline[pair].update(hits(data[pair[0]],data[pair[1]]))

frames=list(range(1,122,5))+[145,205,265] if '--quick' in sys.argv else np.arange(1,265.01,.5)
findings={}; ground=[];scale_error=0.;visibility_errors=[];joint_error=0.
minimum=100.; minimum_object=None; carrier_error=0.
rig=bpy.data.objects['RIG - 350Z cinematic V18']
forward_worlds={};forward_visibility={};forward_surfaces={};reverse_error=0.
for frame in frames:
    scene.frame_set(int(frame),subframe=float(frame%1))
    world_matrices=np.array([np.array(o.matrix_world) for o in meshes])
    visibility=tuple(o.hide_render for o in meshes)
    matching=frame if frame<=121 else 121 if frame<=145 else 266-frame
    if frame<=121:
        forward_worlds[frame]=world_matrices;forward_visibility[frame]=visibility
    elif matching in forward_worlds:
        reverse_error=max(reverse_error,float(np.max(np.abs(world_matrices-forward_worlds[matching]))))
        if visibility!=forward_visibility[matching]:visibility_errors.append((float(frame),'Reverse visibility differs'))
    for obj in meshes:
        if obj.hide_render:continue
        w=np.array(obj.matrix_world)
        z=float(np.min(vertices[obj.name]@w[2,:3]+w[2,3]))
        if z<minimum:minimum=z;minimum_object=(float(frame),obj.name)
        if z<-.001:ground.append(dict(frame=float(frame),object=obj.name,z=z))
    for obj in protected:
        scale_error=max(scale_error,float(np.max(np.abs(np.linalg.norm(np.array(obj.matrix_world)[:3,:3],axis=0)-scales[obj.name]))))
        if obj.hide_render or obj.hide_viewport:visibility_errors.append((float(frame),obj.name))
    for side in ('L','R'):
        for first,last in [('upperarm','forearm'),('forearm','hand'),('thigh','shin'),('shin','foot'),
                           ('rider.thigh','rider.calf'),('rider.upperarm','rider.forearm')]:
            joint_error=max(joint_error,(rig.pose.bones[first+'.'+side].tail-rig.pose.bones[last+'.'+side].head).length)
    for connection in build['connections']:
        first=bpy.data.objects['V18 connected sleeve 0 - '+connection['role']]
        last=bpy.data.objects['V18 connected sleeve 1 - '+connection['role']]
        a=bpy.data.objects[connection['start']].matrix_world.translation
        b=bpy.data.objects[connection['end']].matrix_world.translation
        carrier_error=max(carrier_error,(first.matrix_world.translation-a).length,
                          (last.matrix_world@Vector((0,0,1))-b).length)
    if frame>121 and matching in forward_surfaces and reverse_error<2e-5:
        # The complete mesh matrices and visibility above prove this geometry
        # is identical to the already checked forward pose, including subframes.
        crosses=forward_surfaces[matching]
    else:
        data=surface_cache()
        crosses={a+' / '+b:len(hits(data[a],data[b])-baseline[(a,b)]) for a,b in pairs}
        crosses={k:v for k,v in crosses.items() if v}
    if frame<=121:forward_surfaces[frame]=crosses
    if crosses:findings[str(float(frame))]=crosses
    if frame%20==1:print('AUDIT',frame,'priority',len(crosses),'ground',len(ground),flush=True)
scene.frame_set(265)
return_error=max(float(np.max(np.abs(np.array(o.matrix_world)-start[o.name]))) for o in meshes)
result=dict(blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
    scope='Source shape/UV, rigid exterior, rider, seven neon tubes, ground, live carrier endpoints; priority transverse surfaces at 1.5mm, endpoint contacts excluded. Not all internal contacts or occlusion.',
    samples=len(frames),source_shape_errors=shape_errors,car_matrix_error=car_error,return_error=return_error,
    reverse_matrix_error=reverse_error,
    protected_scale_error=scale_error,protected_visibility_errors=visibility_errors,
    joint_endpoint_error=joint_error,carrier_endpoint_error=carrier_error,
    minimum_visible_z=minimum,minimum_object=minimum_object,ground_crossings=ground,
    priority_surface_crossings=findings,baseline_contacts={a+' / '+b:len(v) for (a,b),v in baseline.items() if v},
    physically_packable=False,internal_compaction_authorized=True)
result['passed']=not(shape_errors or visibility_errors or ground or findings or car_error>2e-5 or return_error>2e-5 or reverse_error>2e-5 or scale_error>2e-5 or carrier_error>2e-5 or joint_error>2e-5)
name='cinematic-check-quick.json' if '--quick' in sys.argv else 'cinematic-check.json'
(OUT/name).write_text(json.dumps(result,indent=2))
if '--quick' not in sys.argv:
    build['clearance_validated']=result['passed']
    build['validation_file']=name
    build['validation_scope']=result['scope']
    (OUT/'cinematic-build.json').write_text(json.dumps(build,indent=2))
print('AUDIT RESULT',json.dumps({k:v for k,v in result.items() if k not in ('ground_crossings','priority_surface_crossings','baseline_contacts')}),flush=True)
