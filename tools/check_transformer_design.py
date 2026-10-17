"""Static surface review of body-design studies; includes co-parented panels."""
import hashlib
import itertools
import json
from collections import defaultdict
from pathlib import Path

import bpy
import numpy as np
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
bpy.context.scene.frame_set(1)
groups=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:groups[obj['module']].append(obj)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.get('continuous_unsegmented'):groups['NEON '+obj.name]=[obj]
groups['RIDER']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
cache={name:placed(geometry(objects),[0,0,0]) for name,objects in groups.items()}
findings={}
for a,b in itertools.combinations(sorted(groups),2):
    count=len(hits(cache[a],cache[b]))
    if count:findings[a+' / '+b]=count
result=dict(blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
            scope='Static original body, reference rider and seven neon tubes; 1.5 mm transverse surface tolerance',
            not_checked=['enclosed volumes','coplanar contact','authored mechanism versus panels','car-mode packing','transformation'],
            surface_crossings=findings,
            priority_crossings={k:v for k,v in findings.items() if 'NEON' in k or 'RIDER' in k or ('hood_' in k and 'lamp_' in k)})
out=Path(bpy.data.filepath).parent/'static-surface-review.json'
out.write_text(json.dumps(result,indent=2))
print('STATIC REVIEW',json.dumps(findings),flush=True)
if result['priority_crossings']:
    raise RuntimeError('Hood/lamp, rider or neon surface crossing in design pose; see '+out.name)
