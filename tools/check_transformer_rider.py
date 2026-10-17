"""Surface audit of every authored mechanism mesh against the reference rider."""
import sys,json,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=(root/'tools/fit_transformer_paths.py').read_text()
exec(source[:source.index('\nscene.frame_set(1)')])
scene.frame_set(1)
hardware=[]
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.type!='MESH':continue
    vertices=np.array([tuple(v.co) for v in obj.data.vertices])
    faces=[]
    for p in obj.data.polygons:
        faces.extend((p.vertices[0],p.vertices[j],p.vertices[j+1]) for j in range(1,len(p.vertices)-1))
    hardware.append((obj,vertices,faces,np.array(faces)))
owners=[]
for obj in groups['RIDER_REFERENCE']:
    for p in obj.data.polygons:owners.extend([obj.name]*(len(p.vertices)-2))
findings=defaultdict(list);grip_contacts=0
for frame in map(int,args.frames.split(',')):
    scene.frame_set(frame)
    rider=placed(geometry(groups['RIDER_REFERENCE']),np.zeros(3))
    for obj,local,faces,indices in hardware:
        m=np.array(obj.matrix_world)
        vertices=local @ m[:3,:3].T+m[:3,3]
        low=vertices.min(axis=0);high=vertices.max(axis=0)
        if np.any(high<rider[3]) or np.any(rider[4]<low):continue
        tris=vertices[indices]
        normals=np.cross(tris[:,1]-tris[:,0],tris[:,2]-tris[:,0])
        normals/=np.maximum(np.linalg.norm(normals,axis=1)[:,None],1e-20)
        body=(BVHTree.FromPolygons(vertices.tolist(),faces,all_triangles=True),tris,normals,low,high)
        collisions=hits(body,rider)
        touched=sorted({owners[j] for i,j in collisions})
        intentional=('rider hand grip' in obj.name or 'rider grip crossbar' in obj.name) and all('hand ' in n or 'forearm' in n for n in touched)
        if intentional:
            grip_contacts+=len(collisions)
        elif collisions:
            findings[obj.name].append(dict(frame=frame,parts=touched,triangle_pairs=len(collisions)))
    if frame%20==0:print('RIDER FRAME',frame,flush=True)
result=dict(blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
            scope='All authored rigid hardware meshes vs reference figure; raw surfaces, not enclosed volumes',
            sampled_frames=list(map(int,args.frames.split(','))),
            allowed_contact='Hand/grip and distal forearm/grip overlap of the simplified closed-hand mannequin',
            grip_contact_triangle_pairs=grip_contacts,findings=dict(findings))
(Path(bpy.data.filepath).parent/'rider-mechanism-clearance.json').write_text(json.dumps(result,indent=2))
print('RIDER FINDINGS',{k:len(v) for k,v in findings.items()},flush=True)
