"""Bake the approved V18 into a local, versioned Unity runtime payload.

blender --background --threads 2 --python tools/export_transformer_runtime.py
No Unity editor, FBX import, player clone, installed-mod mutation, or source edit.
Rigid source panels use the game's material table. Wheels/exhaust are supplied
by the live vehicle so its tuning is preserved. Mannequin becomes IK targets.
"""
import hashlib
import json
import struct
import sys
from collections import defaultdict
from pathlib import Path
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT/'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'
OUT = ROOT/'.local/transformer'
OUT.mkdir(parents=True, exist_ok=True)
body_path = ROOT/'.local/model/body.json'
body = json.loads(body_path.read_text())
audit = json.loads((SOURCE.parent/'cinematic-check.json').read_text())
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
assert audit['passed'], 'V18 clearance audit must pass before export'
assert audit['blend_sha256'] == sha(SOURCE), 'V18 changed since validation'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.frame_set(121)
rig = bpy.data.objects['RIG - 350Z cinematic V18']
# Blender X/right,Y/forward,Z/up -> Unity X/right,Y/up,Z/forward (reflection).
C = Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)))
parts = {p['name']:p for p in body['parts']}
material_source = {m['name']:i for i,m in enumerate(body['materials'])}
nodes = []
getters = []
indices = {}
def node(name, getter, parent=-1):
    if name in indices: return indices[name]
    index = len(nodes); indices[name] = index
    nodes.append(dict(name=name,parent=parent));getters.append(getter)
    return index
for bone in rig.data.bones:
    name = bone.name
    node(name, lambda n=name:rig.matrix_world @ rig.pose.bones[n].matrix,
         indices[bone.parent.name] if bone.parent else -1)

def ancestor(obj):
    while obj:
        if obj.parent_type == 'BONE': return obj.parent_bone
        obj = obj.parent
    return 'thorax'

materials = []
mat_ids = {}
def material(mat, source_part='', neon_group=0):
    key=(mat.name,neon_group)
    if key in mat_ids:return mat_ids[key]
    bsdf = mat.node_tree.nodes.get('Principled BSDF') if mat.use_nodes else None
    color = list(mat.diffuse_color)
    emission = [0,0,0,1]
    metallic,roughness = 0.,.5
    if bsdf:
        color = list(bsdf.inputs['Base Color'].default_value)
        emission = list(bsdf.inputs['Emission'].default_value)
        metallic = bsdf.inputs['Metallic'].default_value
        roughness = bsdf.inputs['Roughness'].default_value
    source=material_source.get(mat.name,-1)
    live_part=source_part if mat.name.startswith('V10 active ') else ''
    if live_part:source=parts[live_part]['material']
    result = dict(name=mat.name,source=source,sourcePart=live_part,neonGroup=neon_group,color=color,
                  emission=emission,metallic=metallic,smoothness=1-roughness)
    index=len(materials);mat_ids[key]=index;materials.append(result);return index

meshes=[];represented=set();skipped=[]
for collection_name in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT'):
    for obj in bpy.data.collections[collection_name].objects:
        if obj.type != 'MESH':continue
        source = obj.get('source_part','')
        part = parts.get(source,{})
        # Runtime supplies the actual four wheels/brakes and chosen exhaust tips.
        if (part.get('group') in ('wheel','brake') or part.get('optionGroup') in ('exhaust','engine','trunk')
                or source in ('350Z_BASE_A_slice3_3','350Z_BASE_A_slice5_5','350Z_BASE_A_slice6_6')):
            skipped.append(obj.name);continue
        if source:represented.add(source)
        binding=ancestor(obj)
        animated = bool(obj.constraints) or bool(obj.animation_data and (
            obj.animation_data.drivers or obj.animation_data.action))
        # Visibility alone doesn't need a separate motion node.
        paths = []
        if obj.animation_data:
            paths += [f.data_path for f in obj.animation_data.drivers]
            if obj.animation_data.action:paths += [f.data_path for f in obj.animation_data.action.fcurves]
        animated = bool(obj.constraints) or any(p not in ('hide_render','hide_viewport') for p in paths)
        idx=node('object:'+obj.name,lambda o=obj:o.matrix_world.copy(),indices[binding]) if animated else indices[binding]
        meshes.append((obj,idx,bool(obj.get('v18_concealed'))))

# Mannequin endpoints drive the EXISTING Valheim skeleton, without its meshes.
for name in ('NEW - REFERENCE pelvis','NEW - REFERENCE boot L','NEW - REFERENCE boot R',
             'NEW - REFERENCE hand L','NEW - REFERENCE hand R'):
    obj=bpy.data.objects[name]
    node('target:'+name,lambda o=obj:o.matrix_world.copy(),indices[ancestor(obj)])

# Sample every validated half-frame. Reverse uses this same track backwards.
samples=[];relative={o.name:getters[i]().inverted()@o.matrix_world for o,i,_ in meshes}
max_error=0.;max_shear=0.
for f in range(241):
    frame=1+f*.5;scene.frame_set(int(frame),subframe=frame%1)
    mats=[g() for g in getters];row=[]
    for matrix in mats:
        converted=C@matrix@C
        pos,rot,scale=converted.decompose()
        rebuilt=Matrix.Translation(pos)@rot.to_matrix().to_4x4()@Matrix.Diagonal(Vector((*scale,1)))
        max_shear=max(max_shear,max(abs(converted[r][c]-rebuilt[r][c]) for r in range(4) for c in range(4)))
        row.extend((*pos,rot.x,rot.y,rot.z,rot.w,*scale))
    for obj,i,_ in meshes:
        predicted=mats[i]@relative[obj.name]
        max_error=max(max_error,max(abs(predicted[r][c]-obj.matrix_world[r][c]) for r in range(4) for c in range(4)))
    samples.append(row)
    if f%40==0:print('Runtime bake',f,'/ 240',flush=True)
assert max_error<.0002, ('Non-rigid group needs its own track',max_error)
assert max_shear<.0002, ('Animation contains unsupported shear',max_shear)
scene.frame_set(121)
neutral=[C@g()@C for g in getters]
for i,n in enumerate(nodes):n['neutral']=[v for row in neutral[i] for v in row]

# Merge by material/visibility, at most 48 bones and 60k vertices per draw.
# Every vertex has exactly one bone: no deformation of glass or source panels.
batches=[];current={}
for obj,idx,concealed in meshes:
    deps=bpy.context.evaluated_depsgraph_get();evaluated=obj.evaluated_get(deps)
    mesh=evaluated.to_mesh();mesh.calc_loop_triangles();mesh.calc_normals_split()
    transform=C@obj.matrix_world
    normal_matrix=transform.to_3x3().inverted().transposed()
    for tri in mesh.loop_triangles:
        mat=obj.data.materials[tri.material_index]
        neon=2 if 'trunk' in obj.name else 1
        mid=material(mat,obj.get('source_part',''),neon if mat.name=='NEON - continuous green tubes' else 0);key=(mid,concealed)
        b=current.get(key)
        if b is None or len(b['vertices'])>=59994 or idx not in b['bones'] and len(b['bones'])>=48:
            b=dict(material=mid,concealed=concealed,bones=[],vertices=[],indices=[],lookup={})
            current[key]=b;batches.append(b)
        if idx not in b['bones']:b['bones'].append(idx)
        weight=b['bones'].index(idx)
        # Reflection changes handedness. Reverse triangle winding explicitly.
        corners=[(loop,1) for loop in (tri.loops[0],tri.loops[2],tri.loops[1])]
        # Match VehicleMesh.AddBackfaces for thin source shells. Authored solid
        # machinery already has volume; do not double its interior triangles.
        if obj.get('source_part'):
            corners += [(loop,-1) for loop in (tri.loops[0],tri.loops[1],tri.loops[2])]
        for loop,normal_sign in corners:
            pos=transform@mesh.vertices[mesh.loops[loop].vertex_index].co
            normal=(normal_matrix@mesh.loops[loop].normal).normalized()*normal_sign
            uv=mesh.uv_layers.active.data[loop].uv if mesh.uv_layers.active else (0,0)
            vertex=(*pos,*normal,*uv,weight)
            if vertex not in b['lookup']:
                b['lookup'][vertex]=len(b['vertices']);b['vertices'].append(vertex)
            b['indices'].append(b['lookup'][vertex])
    evaluated.to_mesh_clear()

payload=OUT/'transformer.bin'
with payload.open('wb') as f:
    f.write(b'NFVT0001')
    def integer(n):f.write(struct.pack('<i',n))
    integer(len(nodes));integer(len(samples));integer(len(batches))
    for row in samples:f.write(struct.pack('<%df'%len(row),*row))
    for b in batches:
        integer(b['material']);integer(int(b['concealed']));integer(len(b['bones']))
        for n in b['bones']:integer(n)
        integer(len(b['vertices']));integer(len(b['indices']))
        for v in b['vertices']:f.write(struct.pack('<8fi',*v))
        f.write(struct.pack('<%di'%len(b['indices']),*b['indices']))
clearance_path=ROOT/'tools/transformer_gait_clearance.json'
clearance=json.loads(clearance_path.read_text())
releases=[dict(node=indices['panel.'+name],offset=[v[0],v[2],v[1]]) for name,v in sorted(clearance.items())]
manifest=dict(schema=1,bodySha256=sha(body_path),blendSha256=sha(SOURCE),payloadSha256=sha(payload),
              walkingReady=False,
              gaitReleases=releases,gaitClearanceSha256=sha(clearance_path),
              fps=48,duration=5,nodes=nodes,materials=materials,representedParts=sorted(represented),
              frames=len(samples),batches=len(batches),vertices=sum(len(b['vertices']) for b in batches),
              maxGroupingError=max_error,maxTrsError=max_shear,
              excludedReferenceActor=True,runtimeWheelsAndExhaust=True,
              locomotion='LOCKED: candidate gait has new armor crossings. Transformation/standing only until a validated locomotion asset is available.')
sys.path.insert(0,str(ROOT/'tools'))
from transformer_locomotion_review import configure
manifest=configure(manifest)
(OUT/'transformer.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('EXPORTED',len(nodes),'nodes,',len(batches),'draw batches,',manifest['vertices'],'vertices',flush=True)
