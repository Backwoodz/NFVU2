"""Validate V17 rigid bindings, joint continuity, source targets and motion clearance.

Surface tests cover sampled moving rig groups vs original car panels, neon and
rider. They are not a solid-volume or full mechanical self-collision certificate.
"""
import hashlib
import itertools
import json
import sys
from collections import defaultdict
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
PATH=Path(bpy.data.filepath)
OUT=PATH.parent
audit=json.loads((OUT/'rig-audit.json').read_text())
mapping=json.loads((OUT/'car-module-targets.json').read_text())
scene=bpy.context.scene
rig=bpy.data.objects['RIG - 350Z V17']
scene.frame_set(1)
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
objects={name:bpy.data.objects[name] for name in audit['mesh_bones']}
baseline={name:np.array(obj.matrix_world) for name,obj in objects.items()}
errors=[]
max_joint_gap=0.
minimum=100.
max_scale_error=0.
carrier_endpoint_error=0.
minimum_sleeve_overlap=1.
local_vertices={name:np.array([tuple(v.co) for v in obj.data.vertices]) for name,obj in objects.items()}
quick='--quick' in sys.argv
end=scene.frame_end
structure_frames=[1,13,25,49,65,81,93,105,117,129,153,169,185,197,209] if quick else np.arange(1,end+.01,.5)
for fcurve in rig.animation_data.action.fcurves:
    if not fcurve.data_path.endswith(('rotation_quaternion','location')):
        errors.append('Unexpected nonrotation animation: '+fcurve.data_path)
for frame in structure_frames:
    scene.frame_set(int(frame), subframe=float(frame%1))
    for side in ('L','R'):
        for first,last in [('upperarm','forearm'),('forearm','hand'),('thigh','shin'),('shin','foot')]:
            gap=(rig.pose.bones[first+'.'+side].tail-rig.pose.bones[last+'.'+side].head).length
            max_joint_gap=max(max_joint_gap,gap)
        for digit in ('digit0','digit1','digit2','digit3','thumb'):
            for i in (0,1):
                gap=(rig.pose.bones['%s%d.%s'%(digit,i,side)].tail-rig.pose.bones['%s%d.%s'%(digit,i+1,side)].head).length
                max_joint_gap=max(max_joint_gap,gap)
    for name,obj in objects.items():
        world=np.array(obj.matrix_world)
        z=float(np.min(local_vertices[name]@world[2,:3]+world[2,3]))
        minimum=min(minimum,z)
        max_scale_error=max(max_scale_error,float(np.max(np.abs(
            np.linalg.norm(world[:3,:3],axis=0)-np.linalg.norm(baseline[name][:3,:3],axis=0)))))
    for carrier in audit['telescopic_panel_carriers']:
        role=carrier['role'];length=carrier['length_m'];count=carrier['sleeves']
        start=bpy.data.objects['CARRIER root '+role].matrix_world.translation
        carrier_target=bpy.data.objects['CARRIER end '+role].matrix_world.translation
        first=bpy.data.objects['V17 carrier sleeve 0 - '+role]
        last=bpy.data.objects['V17 carrier sleeve %d - %s'%(count-1,role)]
        carrier_endpoint_error=max(carrier_endpoint_error,(first.matrix_world.translation-start).length,
                                   (last.matrix_world@Vector((0,0,length))-carrier_target).length)
        overlap=1-((carrier_target-start).length-length)/(count-1)/length
        minimum_sleeve_overlap=min(minimum_sleeve_overlap,overlap)
    if int(frame)%48==0 and frame%1==0:
        print('STRUCTURE frame',frame,flush=True)
scene.frame_set(end)
return_error=max(float(np.max(np.abs(np.array(obj.matrix_world)-baseline[name]))) for name,obj in objects.items())
if max_joint_gap>2e-5:errors.append('Joint gap exceeds tolerance')
if minimum<-.0001:errors.append('Geometry crosses ground')
if max_scale_error>2e-5:errors.append('Mesh scale changes')
if return_error>2e-5:errors.append('Animation does not return to approved pose')
if carrier_endpoint_error>2e-5:errors.append('Carrier detaches from endpoint')
if minimum_sleeve_overlap<.2999:errors.append('Carrier sleeves lose designed overlap')

# Group every rigid assembly by its attachment. Its surface can be transformed
# cheaply with one bone matrix, instead of rebuilding all 1723 objects each frame.
scene.frame_set(1)
targets=defaultdict(list);subjects=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:
    targets['panel.'+obj['module']].append(obj)
targets['rider']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.type!='MESH':continue
    if obj.get('continuous_unsegmented'):
        targets[obj.name].append(obj)
    else:
        subjects['carrier.'+obj.name if obj.get('v17_carrier') else 'mechanism.'+audit['mesh_bones'][obj.name]].append(obj)
allgroups=dict(targets);allgroups.update(subjects)
group_data={name:geometry(obs) for name,obs in allgroups.items()}
group_bones={name:audit['mesh_bones'][obs[0].name] for name,obs in allgroups.items()}
# Rider has multiple bones, all stationary during this action; assert that here.
assert all(np.max(np.abs(np.array(rig.pose.bones[audit['mesh_bones'][o.name]].matrix)-
                        np.array(rig.pose.bones[audit['mesh_bones'][o.name]].bone.matrix_local)))<1e-5 for o in targets['rider'])
rest={name:rig.pose.bones[name].matrix.copy() for name in rig.data.bones.keys()}
moving=set()
for frame in (49,153):
    scene.frame_set(frame)
    for name,b in group_bones.items():
        delta=np.array(rig.pose.bones[b].matrix@rest[b].inverted())
        if np.max(np.abs(delta-np.eye(4)))>1e-5 or name.startswith('carrier.'):moving.add(name)
pairs=[(a,b) for a,b in itertools.combinations(sorted(allgroups),2)
       if (a in moving or b in moving) and (a in targets or b in targets)]


def transformed(name):
    if name.startswith('carrier.'):
        return placed(geometry(allgroups[name]),[0,0,0])
    points,faces,tris,normals=group_data[name]
    b=group_bones[name]
    matrix=np.array(rig.pose.bones[b].matrix@rest[b].inverted())
    pts=points@matrix[:3,:3].T+matrix[:3,3]
    triangle=tris@matrix[:3,:3].T+matrix[:3,3]
    normal=normals@matrix[:3,:3].T
    return (BVHTree.FromPolygons(pts.tolist(),faces,all_triangles=True),triangle,normal,pts.min(0),pts.max(0))


scene.frame_set(1)
cache={name:transformed(name) for name in allgroups}
baseline_hits={pair:hits(cache[pair[0]],cache[pair[1]]) for pair in pairs}
new_crossings={}
sampled=(list(range(1,end+1,16))+[13,25,49,65,81,93,105,117,129,153,169,185,197,209]) if quick else list(np.arange(1,end+.01,.5))
for frame in sorted(set(sampled)):
    scene.frame_set(int(frame),subframe=float(frame%1))
    for name in moving:cache[name]=transformed(name)
    for pair in pairs:
        fresh=hits(cache[pair[0]],cache[pair[1]])-baseline_hits[pair]
        if fresh:
            key=' / '.join(pair)
            new_crossings.setdefault(key,dict(first_frame=frame,maximum_triangle_pairs=0))
            new_crossings[key]['maximum_triangle_pairs']=max(new_crossings[key]['maximum_triangle_pairs'],len(fresh))
    if frame%16==1 or frame==end:
        print('SURFACES frame',frame,'new pairs',len(new_crossings),flush=True)

# Reconstruct every original car group, including cut surfaces, and compare its
# bounds and total triangle area to V8. Nothing from the inner mechanism is
# declared packed merely because the exterior target positions reconstruct.
scene.frame_set(1)
reconstructed=defaultdict(list)
for role,item in mapping.items():
    if role.startswith('neon.'):continue
    control_world=Matrix(item['robot_world'])
    for name in item['objects']:
        obj=objects[name]
        local=control_world.inverted()@Matrix(audit['baseline'][name]['world'])
        carworld=Matrix(item['car_world'])@local
        points=np.array([tuple(carworld@v.co) for v in obj.data.vertices])
        tris=[]
        for p in obj.data.polygons:
            for j in range(1,len(p.vertices)-1):tris.append(points[[p.vertices[0],p.vertices[j],p.vertices[j+1]]])
        reconstructed[item['source_module']].extend(tris)
result=dict(blend_sha256=hashlib.sha256(PATH.read_bytes()).hexdigest(),
            sampled_structure_frames=len(structure_frames),minimum_z_m=minimum,maximum_joint_gap_m=max_joint_gap,
            maximum_scale_error=max_scale_error,return_matrix_error=return_error,
            maximum_carrier_endpoint_error_m=carrier_endpoint_error,
            minimum_carrier_sleeve_overlap_fraction=minimum_sleeve_overlap,
            sampled_surface_frames=sorted(set(sampled)),surface_tolerance_m=.0015,
            baseline_surface_pairs=sum(bool(v) for v in baseline_hits.values()),
            new_surface_crossings=new_crossings,
            scope='Rigid joint test: moving mechanism/panels against original panels, neon and rider; baseline triangle contacts excluded',
            not_checked=['all mechanical self contacts','solid containment','car packing','transformation paths'],
            structural_errors=errors)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'artifacts/transformer-study-v8/350z-transformer-study.blend'))
bpy.context.scene.frame_set(1)
source=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:
    source[obj['module']].append(obj)
car_differences={}
for role,tris in reconstructed.items():
    a=np.array(tris);b=geometry(source[role])[2]
    def area(t):return np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1).sum()/2
    bound_error=max(float(np.max(np.abs(a.min((0,1))-b.min((0,1))))),float(np.max(np.abs(a.max((0,1))-b.max((0,1))))))
    area_error=abs(float(area(a)-area(b)))
    if bound_error>2e-5 or area_error>2e-5:
        car_differences[role]=dict(bound_error_m=bound_error,area_error_m2=area_error)
result['car_source_group_count']=len(reconstructed)
result['car_reconstruction_differences']=car_differences
(OUT/'rig-validation.json').write_text(json.dumps(result,indent=2))
print('RIG VALIDATION',json.dumps(result),flush=True)
if errors or car_differences or new_crossings:
    raise RuntimeError('Rig review findings: see rig-validation.json')
