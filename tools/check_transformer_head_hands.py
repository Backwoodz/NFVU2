"""Check the redesigned head/hands against the retained car surfaces and rider.

Static transverse surface checks; excludes intended wrist/neck attachment to
the authored robot skeleton, and does not certify internal hand self-contact.
"""
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
groups=defaultdict(list)
for o in bpy.data.collections['01 Original 350Z surfaces'].objects:groups[o['module']].append(o)
groups['RIDER']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
subjects=defaultdict(list)
for o in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if o.type!='MESH':continue
    parent=o.parent
    while parent:
        if parent.name.startswith('DESIGN - V11 articulated hand') or parent.name=='DESIGN - V11 interlocking cranial assembly':
            subjects[parent.name].append(o);break
        parent=parent.parent
    if o.get('continuous_unsegmented'):groups[o.name]=[o]
cache={name:placed(geometry(objects),[0,0,0]) for name,objects in groups.items()}
findings={};object_findings={}
for name,objects in subjects.items():
    body=placed(geometry(objects),[0,0,0])
    for other,target in cache.items():
        count=len(hits(body,target))
        if count:
            findings[name+' / '+other]=count
            for o in objects:
                individual=placed(geometry([o]),[0,0,0])
                if hits(individual,target):object_findings[o.name+' / '+other]=[individual[3].tolist(),individual[4].tolist()]
result=dict(blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
            scope='New head and hands vs retained car surfaces, seven neon tubes and reference rider; static 1.5mm transverse surface tolerance',
            not_checked=['internal head/hand contacts','enclosed volumes','car-mode packing','transformation'],
            subject_mesh_counts={k:len(v) for k,v in subjects.items()},surface_crossings=findings,object_crossings=object_findings)
(Path(bpy.data.filepath).parent/'head-hand-clearance.json').write_text(json.dumps(result,indent=2))
print('HEAD HAND REVIEW',json.dumps(result),flush=True)
if findings:raise RuntimeError('Head/hand surface crossings: see head-hand-clearance.json')
