"""Static routed-hose/reactor clearance review for body design studies."""
import hashlib,json,itertools
from collections import defaultdict
from pathlib import Path
import bpy
import numpy as np
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
bpy.context.scene.frame_set(1)
targets=defaultdict(list);subjects=defaultdict(list)
for o in bpy.data.collections['01 Original 350Z surfaces'].objects:targets[o['module']].append(o)
targets['RIDER']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
for o in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if o.type!='MESH':continue
    if o.get('continuous_unsegmented'):targets[o.name].append(o)
    if o.get('v13_route'):subjects[o['v13_route']].append(o)
    elif o.get('v13_reactor'):subjects['REACTOR'].append(o)
    elif o.parent and o.parent.name=='DESIGN - V13 body mechanism':subjects['OTHER_BODY'].append(o)
cache={k:placed(geometry(v),[0,0,0]) for k,v in targets.items()};findings={};terminal_contacts={}
for name,objects in subjects.items():
    body=placed(geometry(objects),[0,0,0])
    for target,shape in cache.items():
        collisions=hits(body,shape)
        connection=next((o for o in objects if o.get('route_connection')==target),None)
        if connection:
            endpoint=np.array(connection['route_terminal']);tolerance=connection['terminal_tolerance']
            allowed={(i,j) for i,j in collisions if np.max(np.linalg.norm(body[1][i]-endpoint,axis=1))<tolerance}
            if allowed:terminal_contacts[name+' / '+target]=len(allowed)
            collisions=set(collisions)-allowed
        count=len(collisions)
        if count:findings[name+' / '+target]=count
priority={k:v for k,v in findings.items() if not k.startswith('OTHER_BODY') or 'RIDER' in k or 'NEON' in k}
result=dict(blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
    scope='Static routed hoses including fittings, reactor and authored body vs source panels/neon/rider; 1.5mm transverse tolerance',
    not_checked=['internal mechanism self-contact','hose deformation','enclosed volumes','car-mode packing','transformation'],
    surface_crossings=findings,priority_crossings=priority,
    allowed_contact_scope='Only route triangles entirely inside the declared final exhaust socket radius; all other cuts retained',
    exhaust_terminal_contacts=terminal_contacts)
(Path(bpy.data.filepath).parent/'body-detail-clearance.json').write_text(json.dumps(result,indent=2))
print('BODY DETAIL',json.dumps(result),flush=True)
if priority:raise RuntimeError('Hose/reactor/neon/rider surface crossings remain')
