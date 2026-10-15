"""Audit V3 actual-controller locomotion poses on the unchanged V18 scene.

One Blender process, --threads 2. Surface tests use 1.5 mm transverse triangle
crossings. Closed-pose overlaps remain reported, never called collision free.
Withdrawal may separate the same pairs without increasing their transverse
crossing depth. Walking uses triangle-level baseline exclusions. Ground and
IK reach/length checks run on every sample. Unity physics and equipment are
not exercised; all moving inner machinery is not covered by the surface test.
"""
from pathlib import Path
import sys,json,hashlib,itertools,copy
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from transformer_locomotion_review import DOMAIN_BINDINGS,TRACE_BINDINGS
def input_hash(name):
    path=ROOT/name
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
initial_hashes={name:input_hash(name) for name in DOMAIN_BINDINGS|TRACE_BINDINGS}
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
def neutral_pose():
    p=copy.deepcopy(runtime_poses[0])
    for key in ('phase','amount','release','left','right','pelvisHeight','pelvisSide','pelvisYaw','torsoLean','torsoRoll','torsoYaw'):
        p[key]=0
    for side in ('leftFoot','rightFoot'):
        for key in p[side]:p[side][key]=True if key=='grounded' else 0
    return p
def apply_pose(p):
    gait(p['phase'],p['amount'],posture=p)
apply_pose(neutral_pose())
# Capture continuity evidence BEFORE building collision exclusions. Otherwise
# an unintended idle mount shift could silently become the new baseline.
neutral_errors={name:float(np.max(np.abs(np.array(rig.pose.bones[name].matrix)-np.array(matrix)))) for name,matrix in neutral.items()}
neutral_valid=all(math.isfinite(value) and value<=2e-5 for value in neutral_errors.values())
neutral_max=max(neutral_errors.values()) if all(math.isfinite(v) for v in neutral_errors.values()) else None
neutral_worst=max(neutral_errors,key=lambda name:neutral_errors[name] if math.isfinite(neutral_errors[name]) else float('inf'))
print('V3 NEUTRAL',neutral_max,'worst',neutral_worst,'passed',neutral_valid,flush=True)
reference={k:(geometry(v),np.linalg.inv(np.array(v[0].matrix_world))) for k,v in groups.items()}
last={};last_inverse={};shapes={}
def surfaces():
    changed=set()
    for name,obs in groups.items():
        matrix=np.array(obs[0].matrix_world)
        if name in last and np.max(np.abs(last[name]-matrix))<1e-7:continue
        changed.add(name);last[name]=matrix;last_inverse[name]=np.linalg.inv(matrix)
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
baseline_relative={p:reference[p[0]][1]@last[p[1]] for p in pairs}
visible=[o for c in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT','05 Rider - scale reference, NOT game avatar') for o in bpy.data.collections[c].objects if o.type=='MESH' and not o.hide_render]
bounds=np.array([[tuple(v)+(1,) for v in o.bound_box] for o in visible])
ground={};findings={};deployment_contacts={};ik_errors={};ik_max={};rigid_invariant={};tested=0
def check(key,deployment=False):
    global tested
    tested+=1;changed=surfaces();bad={}
    for a,b in pairs:
        if a not in changed and b not in changed:continue
        # A shared rigid transform cannot create a new physical intersection.
        # Reusing its exact baseline avoids BVH edge-roundoff on the stock
        # overlapping roof halves; the1.5mm crossing gate is unchanged.
        relative_error=float(np.max(np.abs(last_inverse[a]@last[b]-baseline_relative[a,b])))
        if relative_error<1e-6:
            rigid_invariant[a+' / '+b]=max(rigid_invariant.get(a+' / '+b,0),relative_error)
            continue
        contact=hits(shapes[a],shapes[b]);new=contact-baseline[a,b]
        if not new:continue
        if deployment and baseline[a,b] and depth(shapes[a],shapes[b],contact)<=baseline_depth[a,b]+.0015:
            deployment_contacts[a+' / '+b]=max(deployment_contacts.get(a+' / '+b,0),depth(shapes[a],shapes[b],contact))
        else:bad[a+' / '+b]=len(new)
    if bad:
        findings[key]=bad
        if len(findings)<=8:print('V3 CONTACT',key,bad,flush=True)
    rows=np.array([list(o.matrix_world[2]) for o in visible])
    lower=np.einsum('nij,nj->ni',bounds,rows).min(axis=1)
    for i in np.flatnonzero(lower<-.002):
        o=visible[i];actual=min((o.matrix_world@v.co).z for v in o.data.vertices)
        if actual<-.002:ground[o.name]=min(ground.get(o.name,0),actual)
    for side,diagnostics in last_gait_diagnostics.items():
        for metric in ('reachError','upperLengthError','lowerLengthError','ankleError'):
            value=diagnostics[metric];name=side+'/'+metric
            ik_max[name]=max(ik_max.get(name,0),value)
            if not math.isfinite(value) or value>.002:ik_errors.setdefault(key,{})[name]=value
def report(bindings):
    missing=[name for name in bindings if not (ROOT/name).is_file()]
    altered=[name for name in bindings if input_hash(name)!=initial_hashes[name]]
    diagnostic=any(flag in sys.argv for flag in ('--start-domain','--end-domain'))
    return dict(passed=neutral_valid and not findings and not ground and not ik_errors and not missing and not altered and not diagnostic,
        geometryPassed=neutral_valid and not findings and not ground and not ik_errors,missingBindings=missing,
        neutralPoseMaxError=neutral_max,neutralPoseWorstBone=neutral_worst,
        neutralPoseErrors={name:value for name,value in neutral_errors.items() if not math.isfinite(value) or value>2e-5},
        quick='--quick' in sys.argv or diagnostic,diagnosticRange=diagnostic,samples=tested,
        inputsChangedDuringAudit=altered,
        newContacts=findings,belowGround=ground,ikErrors=ik_errors,maximumIkErrors=ik_max,
        existingClosedPosePairs={a+' / '+b:dict(triangles=len(v),depth=baseline_depth[a,b]) for (a,b),v in baseline.items() if v},
        separatingMountContacts=deployment_contacts,rigidInvariantPairs=rigid_invariant,
        hashes={name:initial_hashes[name] for name in bindings},scope=__doc__)

# BEGIN_DOMAIN_REVIEW
domain=json.loads((ROOT/'artifacts/tests/transformer/runtime-gait-domain.json').read_text())
def domain_index(flag,default):
    if flag not in sys.argv:return default
    try:return int(sys.argv[sys.argv.index(flag)+1])
    except (ValueError,IndexError):raise ValueError(flag+' requires an integer pose index')
start=domain_index('--start-domain',0);end=domain_index('--end-domain',len(domain))
if not 0<=start<end<=len(domain):raise ValueError('Diagnostic domain range must satisfy 0 <= start < end <= '+str(len(domain)))
# Range selection is only for repairing an already identified late pose. It
# never qualifies as a complete review, even if its selected range is large.
for i in (range(129) if start==0 else ()):
    p=neutral_pose();p['release']=i/128;apply_pose(p);check('deployment/'+str(i),True)
deployment_samples=tested
print('V3 DEPLOY',tested,'bad',len(findings),'ground',len(ground),'IK',len(ik_errors),flush=True)
previous=None
for i in range(start,end):
    p=domain[i]
    if '--quick' in sys.argv and (i%6!=0):continue
    apply_pose(p);check('domain/'+str(i),p['amount']==0)
    if previous!=p['scenario']:
        print('V3 DOMAIN',p['scenario'],'poses',tested,'bad',len(findings),'ground',len(ground),'IK',len(ik_errors),flush=True)
        previous=p['scenario']
    if (findings or ground or ik_errors) and '--fail-fast' in sys.argv:break
result=report(DOMAIN_BINDINGS)
result.update(domainRange=[start,end],deploymentSamples=deployment_samples,domainSamples=tested-deployment_samples)
(OUT/'locomotion-v3-check.json').write_text(json.dumps(result,indent=2))
print('V3 RESULT',result['passed'],flush=True)
