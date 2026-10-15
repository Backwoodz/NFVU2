"""Diagnostic surface audit of authored hardware in the packed car.

Intentional mounting contacts are reported too; this does not certify volumes.
"""
import sys,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=(root/'tools/fit_transformer_paths.py').read_text()
exec(source[:source.index('\nscene.frame_set(1)')])
scene.frame_set(1)
panels={k:placed(geometry(v),np.zeros(3)) for k,v in groups.items()}
findings={}
rider_owners=[]
for obj in groups['RIDER_REFERENCE']:
    for face in obj.data.polygons:rider_owners.extend([obj.name]*(len(face.vertices)-2))
rider_contacts={}
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.type!='MESH':continue
    body=placed(geometry([obj]),np.zeros(3))
    collisions={name:len(hits(body,data)) for name,data in panels.items()}
    collisions={name:n for name,n in collisions.items() if n}
    if collisions:findings[obj.name]=collisions
    if 'RIDER_REFERENCE' in collisions:
        rider_contacts[obj.name]=sorted({rider_owners[j] for i,j in hits(body,panels['RIDER_REFERENCE'])})
out=Path(bpy.data.filepath).parent/'packing-surfaces.json'
out.write_text(json.dumps(dict(scope='Packed car: authored hardware against original surfaces and rider; mounting contacts included',
                              collision_free=False,findings=findings,rider_contact_parts=rider_contacts),indent=2))
print('PACKING OBJECTS',len(findings),flush=True)
print('PACKING RIDER',{k:v['RIDER_REFERENCE'] for k,v in findings.items() if 'RIDER_REFERENCE' in v},flush=True)
print('PACKING RIDER PARTS',rider_contacts,flush=True)
print('PACKING MOST', sorted([(sum(v.values()),k,v) for k,v in findings.items()],reverse=True)[:25],flush=True)
