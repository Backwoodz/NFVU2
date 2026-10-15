"""Audit locomotion V2, including deployment, partial steps and pivot turns.

One Blender process, --threads 2. Surface tests use 1.5 mm transverse triangle
crossings. Existing closed-pose overlaps are reported, never called collision
free: during withdrawal only those same pairs may separate, without increasing
their maximum transverse crossing depth. Walking uses triangle-level baseline
exclusions. This does not validate Unity physics, equipment or inner machinery.
"""
from pathlib import Path
import sys,json,hashlib,itertools
sys.argv.append('--runtime')
source=Path(__file__).with_name('preview_transformer_locomotion.py').read_text()
exec(source[:source.index('\nvisible=')])
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
groups=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:groups[obj['module']].append(obj)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.get('continuous_unsegmented'):groups[obj.name].append(obj)
groups['RIDER']=[o for o in bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects if o.type=='MESH']
gait(0,0,release=0)
reference={k:(geometry(v),np.linalg.inv(np.array(v[0].matrix_world))) for k,v in groups.items()}
last={};shapes={}
def surfaces():
    changed=set()
    for name,obs in groups.items():
        matrix=np.array(obs[0].matrix_world)
        if name in last and np.max(np.abs(last[name]-matrix))<1e-7:continue
        changed.add(name);last[name]=matrix
        data,inverse=reference[name];points,faces,tris,normals=data
        d=matrix@inverse;r=d[:3,:3];t=d[:3,3]
        shapes[name]=placed((points@r.T+t,faces,tris@r.T+t,normals@r.T),np.zeros(3))
    return changed
def depth(a,b,contacts):
    if not contacts:return 0.
    ia,ib=np.array(list(contacts)).T
    da=np.einsum('ijk,ik->ij',a[1][ia]-b[1][ib,0,None],b[2][ib])
    db=np.einsum('ijk,ik->ij',b[1][ib]-a[1][ia,0,None],a[2][ia])
    return float(np.minimum.reduce([-da.min(axis=1),da.max(axis=1),-db.min(axis=1),db.max(axis=1)]).max())
surfaces();pairs=list(itertools.combinations(sorted(groups),2))
baseline={p:set() if 'RIDER' in p else hits(shapes[p[0]],shapes[p[1]]) for p in pairs}
baseline_depth={p:depth(shapes[p[0]],shapes[p[1]],v) for p,v in baseline.items()}
visible=[o for c in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT','05 Rider - scale reference, NOT game avatar') for o in bpy.data.collections[c].objects if o.type=='MESH' and not o.hide_render]
bounds=np.array([[tuple(v)+(1,) for v in o.bound_box] for o in visible])
ground={};findings={};deployment_contacts={};tested=0
def check(key,deployment=False):
    global tested
    tested+=1;changed=surfaces();bad={}
    for a,b in pairs:
        if a not in changed and b not in changed:continue
        contact=hits(shapes[a],shapes[b])
        new=contact-baseline[a,b]
        if not new:continue
        if deployment and baseline[a,b] and depth(shapes[a],shapes[b],contact)<=baseline_depth[a,b]+.0015:
            deployment_contacts[a+' / '+b]=max(deployment_contacts.get(a+' / '+b,0),depth(shapes[a],shapes[b],contact))
        else:bad[a+' / '+b]=len(new)
    if bad:
        findings[key]=bad
        if len(findings)<=8:print('V2 CONTACT',key,bad,flush=True)
    rows=np.array([list(o.matrix_world[2]) for o in visible])
    lower=np.einsum('nij,nj->ni',bounds,rows).min(axis=1)
    for i in np.flatnonzero(lower<-.002):
        o=visible[i];actual=min((o.matrix_world@v.co).z for v in o.data.vertices)
        if actual<-.002:ground[o.name]=min(ground.get(o.name,0),actual)

for i in range(129):
    gait(0,0,release=i/128);check('deployment/'+str(i),True)
print('V2 DEPLOY',tested,'bad',len(findings),'ground',len(ground),flush=True)
count=17 if '--quick' in sys.argv else 65
amounts=(.25,1) if '--quick' in sys.argv else (.1,.25,.5,.75,1)
directions=((-1,-1),(1,1),(-1,1),(1,-1),(0,1),(1,0),(0,-1),(-1,0),(.5,1),(1,.5))
for left,right in directions:
    for amount in amounts:
        for i in range(count):
            gait(i/(count-1),amount,left_direction=left,right_direction=right,release=1)
            check('walk/%s/%s/%s/%s'%(left,right,amount,i))
    print('V2 WALK',left,right,'poses',tested,'bad',len(findings),'ground',len(ground),flush=True)
    if findings and '--fail-fast' in sys.argv:break
hashes={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in (
    'tools/transformer_gait_v2.json','tools/preview_transformer_locomotion.py',
    'tools/check_transformer_locomotion.py','src/TransformerVisual.cs','src/TransformerGait.cs',
    'artifacts/transformer-study-v18/350z-transformer-cinematic.blend')}
report=dict(passed=not findings and not ground,quick='--quick' in sys.argv,samples=tested,
    newContacts=findings,belowGround=ground,existingClosedPosePairs={a+' / '+b:dict(triangles=len(v),depth=baseline_depth[a,b]) for (a,b),v in baseline.items() if v},
    separatingMountContacts=deployment_contacts,hashes=hashes,
    scope=__doc__)
(OUT/'locomotion-v2-check.json').write_text(json.dumps(report,indent=2))
print('V2 RESULT',report['passed'],flush=True)
