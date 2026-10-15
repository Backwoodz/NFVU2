"""Static V16 attachment clearance; explicit original spoiler hinge contacts."""
import bpy,numpy as np,json,hashlib
from pathlib import Path
from mathutils.bvhtree import BVHTree
from collections import defaultdict
root=Path(__file__).resolve().parents[1]
src=(root/'tools/fit_transformer_paths.py').read_text();exec(src[src.index('def geometry('):src.index('\nscene.frame_set(1)')])
targets=defaultdict(list)
for o in bpy.data.collections['01 Original 350Z surfaces'].objects:targets[o['module']].append(o)
for o in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
 if o.get('continuous_unsegmented'):targets[o.name].append(o)
targets['RIDER']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
cache={k:placed(geometry(v),[0,0,0]) for k,v in targets.items()};findings={};intentional={}
for o in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
 if not o.name.startswith('V9 - V16') or o.type!='MESH':continue
 a=placed(geometry([o]),[0,0,0])
 for target,b in cache.items():
  n=len(hits(a,b))
  if n:
   key=o.name+' / '+target
   if target=='spoiler' and any(p in o.name for p in ('pedestal cradle','hinge pin','hinge bearing')):intentional[key]=n
   else:findings[key]=n
p=Path(bpy.data.filepath)
(p.parent/'attachment-clearance.json').write_text(json.dumps(dict(blend_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),scope='V16 brackets/collar vs original surfaces, neon and rider; static 1.5 mm tolerance',surface_crossings=findings,intentional_spoiler_hinge_attachment=intentional),indent=2))
print('ATTACHMENTS',json.dumps(findings),flush=True)
if findings:raise RuntimeError('Attachment crossings remain')
