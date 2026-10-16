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
from transformer_locomotion_review import DOMAIN_BINDINGS,TRACE_BINDINGS,GAIT_VERSION,DOMAIN_FILE,TRACE_FILE
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
# BEGIN_HAND_DISTANCE
# Convex component distance, including nested volumes. This is intentionally
# conservative for concave/open armor shells: hands stay outside their rigid
# component envelope, rather than being allowed inside an apparent hollow leg.
def hand_closest_simplex(points):
    def segment(a,b):
        ab=b-a;den=float(ab@ab)
        t=max(0.,min(1.,-float(a@ab)/den)) if den>1e-30 else 0.
        return (a,[a]) if t<=1e-10 else (b,[b]) if t>=1-1e-10 else (a+t*ab,[a,b])
    def triangle(a,b,c):
        ab=b-a;ac=c-a;ap=-a
        cross=np.cross(ab,ac);area=float(cross@cross);scale=max(float(ab@ab),float(ac@ac),1e-30)
        if area<=scale*scale*1e-24:
            return min((segment(a,b),segment(a,c),segment(b,c)),key=lambda p:float(p[0]@p[0]))
        d1=float(ab@ap);d2=float(ac@ap)
        if d1<=0 and d2<=0:return a,[a]
        bp=-b;d3=float(ab@bp);d4=float(ac@bp)
        if d3>=0 and d4<=d3:return b,[b]
        vc=d1*d4-d3*d2
        if vc<=0 and d1>=0 and d3<=0:
            den=d1-d3
            if den>1e-30:return a+ab*(d1/den),[a,b]
        cp=-c;d5=float(ab@cp);d6=float(ac@cp)
        if d6>=0 and d5<=d6:return c,[c]
        vb=d5*d2-d1*d6
        if vb<=0 and d2>=0 and d6<=0:
            den=d2-d6
            if den>1e-30:return a+ac*(d2/den),[a,c]
        va=d3*d6-d5*d4
        if va<=0 and d4-d3>=0 and d5-d6>=0:
            den=(d4-d3)+(d5-d6)
            if den>1e-30:return b+(c-b)*((d4-d3)/den),[b,c]
        den=va+vb+vc
        if abs(den)>1e-28:return a+ab*(vb/den)+ac*(vc/den),[a,b,c]
        candidates=[segment(a,b),segment(a,c),segment(b,c)]
        return min(candidates,key=lambda p:float(p[0]@p[0]))
    if len(points)==1:return points[0],points
    if len(points)==2:return segment(*points)
    if len(points)==3:return triangle(*points)
    if len(points)!=4:raise ValueError('GJK simplex must have one to four vertices')
    a,b,c,d=points
    normal=np.cross(b-a,c-a);volume=float(normal@(d-a))
    scale=max(float(np.linalg.norm(b-a)),float(np.linalg.norm(c-a)),float(np.linalg.norm(d-a)),1e-12)
    faces=((a,b,c,d),(a,c,d,b),(a,d,b,c),(b,d,c,a))
    inside=abs(volume)>max(1e-30,scale**3*1e-12)
    if inside:
        for p,q,r,opposite in faces:
            n=np.cross(q-p,r-p)
            if float(n@(-p))*float(n@(opposite-p))<0:
                inside=False;break
    if inside:return np.zeros(3),points
    candidates=[triangle(p,q,r) for p,q,r,_ in faces]
    return min(candidates,key=lambda p:float(p[0]@p[0]))

def hand_gjk_distance(a,b):
    direction=b.mean(axis=0)-a.mean(axis=0)
    if float(direction@direction)<1e-16:direction=np.array([1.,0,0])
    def support(d):return a[np.argmax(a@d)]-b[np.argmax(b@(-d))]
    simplex=[support(direction)];v=simplex[0]
    for iteration in range(64):
        distance=float(v@v)
        if distance<1e-16:return 0.
        w=support(-v)
        if distance-float(v@w)<=1e-9:return math.sqrt(distance)
        if any(np.max(np.abs(w-p))<1e-10 for p in simplex):return math.sqrt(distance)
        v,simplex=hand_closest_simplex(simplex+[w])
    raise RuntimeError('Hand component distance did not converge')
# END_HAND_DISTANCE

def hand_bound_bone(obj):
    while obj and obj.parent_type!='BONE':obj=obj.parent
    return rig.data.bones.get(obj.parent_bone) if obj else None

def hand_ancestor(bone,prefix):
    while bone and not bone.name.startswith(prefix):bone=bone.parent
    return bone

hand_components=[];hand_armor=[];hand_coverage={side:set() for side in ('L','R')}
armor_prefix=('door_front_scale','door_rear_scale','toe_','rear_arch_','sill1_','sill2_','shinface_','rear_wheel_','rear_corner_','floor0_','floor1_','floor2_','hipguard_','pelvis_','belt_','exhaust_')
for collection in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT'):
    for obj in bpy.data.collections[collection].objects:
        if obj.type!='MESH' or obj.hide_render:continue
        bone=hand_bound_bone(obj);hand=hand_ancestor(bone,'hand.')
        if hand:
            hand_coverage[hand.name[-1]].add(bone.name)
            hand_components.append((obj,hand.name[-1]))
        elif collection=='01 Original 350Z surfaces' and obj.get('module','').startswith(armor_prefix):hand_armor.append(obj)
        elif collection=='02 Authored mechanism - BLOCKOUT' and (hand_ancestor(bone,'thigh.') or hand_ancestor(bone,'shin.') or hand_ancestor(bone,'foot.')):hand_armor.append(obj)

def hand_hull(obj):
    # Linear support over original vertices is exactly support over their
    # convex hull, including planar/open pieces and without approximation.
    points=np.unique(np.array([tuple(v.co) for v in obj.data.vertices]),axis=0)
    if not len(points):raise ValueError('Empty hand/armor component: '+obj.name)
    return points

hand_local={o.name:hand_hull(o) for o in [x[0] for x in hand_components]+hand_armor}
hand_contacts={};hand_minimum=float('inf');hand_samples=0;hand_distance_tests=0;hand_triangle_tests=0;hand_volume_tests=0
HAND_CLEARANCE=.025
hand_mount_baseline={};hand_mount_observed={}

def hand_points(obj):
    matrix=np.array(obj.matrix_world);return hand_local[obj.name]@matrix[:3,:3].T+matrix[:3,3]

hand_baseline_samples=0;hand_absolute_samples=0;hand_baseline_excess={};hand_baseline_gap_reduction=0.;hand_packed_min_positive=float('inf');hand_gap_shortfall=0.;HAND_PACKED_GAP_FLOOR=.0015
def check_hands(key,baseline=None,capture=False):
    global hand_minimum,hand_samples,hand_distance_tests,hand_triangle_tests,hand_volume_tests,hand_baseline_samples,hand_absolute_samples,hand_baseline_gap_reduction,hand_packed_min_positive,hand_gap_shortfall
    if not capture:
        hand_samples+=1
        if baseline is None:hand_absolute_samples+=1
        else:hand_baseline_samples+=1
    hp=[hand_points(o) for o,side in hand_components];ap=[hand_points(o) for o in hand_armor]
    hlo=np.array([p.min(axis=0) for p in hp]);hhi=np.array([p.max(axis=0) for p in hp])
    alo=np.array([p.min(axis=0) for p in ap]);ahi=np.array([p.max(axis=0) for p in ap])
    gap=np.maximum(0,np.maximum(hlo[:,None,:]-ahi[None,:,:],alo[None,:,:]-hhi[:,None,:]))
    distances=np.linalg.norm(gap,axis=2);hand_distance_tests+=int(distances.size)
    best=float('inf');bad={};raw_bvhs={}
    def raw_shape(obj):
        if obj.name not in raw_bvhs:raw_bvhs[obj.name]=placed(geometry([obj]),np.zeros(3))
        return raw_bvhs[obj.name]
    for flat in np.argsort(distances,axis=None):
        i,j=np.unravel_index(flat,distances.shape)
        if distances[i,j]>=max(HAND_CLEARANCE,best):break
        distance=hand_gjk_distance(hp[i],ap[j]);hand_volume_tests+=1
        hand_obj,side=hand_components[i];armor_obj=hand_armor[j]
        hs=raw_shape(hand_obj);ars=raw_shape(armor_obj)
        contacts=hs[0].overlap(ars[0]);crossings=len(contacts);hand_triangle_tests+=1
        current_depth=depth(hs,ars,contacts)
        pair=hand_obj.name+' / '+armor_obj.name
        if baseline is None and not capture:
            mount=hand_mount_baseline.get((hand_obj.name,armor_obj.name))
            if mount:
                hand_mount_observed[pair]=max(hand_mount_observed.get(pair,0.),current_depth)
                # Float32 reconstruction of two rigidly moving parts can differ
                # by a few micrometres. This only covers the four original wrist
                # mounts; triangle/volume tests and mechanical clearances remain.
                if distance+5e-6>=mount['clearance'] and current_depth<=mount['depth']+.0015:continue
        best=min(best,distance)
        if distance>=HAND_CLEARANCE-1e-6 and not crossings:continue
        relative=np.linalg.inv(np.array(hand_obj.matrix_world))@np.array(armor_obj.matrix_world)
        record=dict(side=side,clearance=distance,trianglePairs=crossings,envelopeOverlap=distance<1e-7,depth=current_depth,
                    triangleIndices=[list(p) for p in contacts],relativeMatrix=relative.tolist())
        if baseline is not None:
            old=baseline.get(pair)
            if distance>1e-7:hand_packed_min_positive=min(hand_packed_min_positive,distance)
            if old is None or old['clearance']>1e-7:
                required=min(old['clearance'],HAND_PACKED_GAP_FLOOR) if old else HAND_PACKED_GAP_FLOOR
                hand_gap_shortfall=max(hand_gap_shortfall,required-distance)
                if old:hand_baseline_gap_reduction=max(hand_baseline_gap_reduction,old['clearance']-distance)
                # Before the original transformation finishes, free mechanism
                # needs its physical air gap, not the deployed25mm buffer.
                # A new actual surface or volume contact is always rejected.
                if distance>1e-7 and distance+1e-6>=required and not crossings:continue
            else:
                invariant=float(np.max(np.abs(relative-np.array(old['relativeMatrix']))))<1e-6
                containment=old['envelopeOverlap'] and not old['trianglePairs']
                # Existing true source overlaps can withdraw without deeper
                # penetration. Containment lacking a surface-depth bound can
                # only inherit a mathematically unchanged relative pose.
                if invariant or (not containment and current_depth<=old['depth']+.0015):continue
        bad[pair]=record
    if capture:return best,bad
    if baseline is None:hand_minimum=min(hand_minimum,best)
    if bad:
        hand_contacts[key]=bad
        if baseline is not None:hand_baseline_excess[key]=bad
        if len(hand_contacts)<=8:print('HAND CLEARANCE',key,len(bad),'minimum',best,list(bad)[:3],flush=True)
    return best

def neutral_pose():
    p=copy.deepcopy(runtime_poses[0])
    for key in ('phase','amount','release','left','right','pelvisHeight','pelvisSide','pelvisYaw','torsoLean','torsoRoll','torsoYaw','runBlend'):
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
# The authored wrist bearing and its adjacent stud are mechanical mounts,
# already intersecting their own forearm shell at the approved V18 idle pose.
# This narrow joint baseline NEVER applies to fingers/palm, upperarm doors,
# opposite arms, or any leg/hip component. More penetration still fails.
for hand_obj,side in hand_components:
    if hand_bound_bone(hand_obj).name!='hand.'+side or not any(word in hand_obj.name for word in ('end bearing','bearing stud','bearing recess')):continue
    for armor_obj in hand_armor:
        if armor_obj.get('module')!='door_front_scale1_'+side:continue
        distance=hand_gjk_distance(hand_points(hand_obj),hand_points(armor_obj))
        if distance>=HAND_CLEARANCE:continue
        hs=placed(geometry([hand_obj]),np.zeros(3));ars=placed(geometry([armor_obj]),np.zeros(3))
        contact=hs[0].overlap(ars[0])
        hand_mount_baseline[hand_obj.name,armor_obj.name]=dict(clearance=distance,depth=depth(hs,ars,contact),triangles=len(contact),side=side)
print('HAND WRIST MOUNTS',len(hand_mount_baseline),flush=True)
surfaces();pairs=list(itertools.combinations(sorted(groups),2))
baseline={p:set() if 'RIDER' in p else hits(shapes[p[0]],shapes[p[1]]) for p in pairs}
baseline_depth={p:depth(shapes[p[0]],shapes[p[1]],v) for p,v in baseline.items()}
baseline_relative={p:reference[p[0]][1]@last[p[1]] for p in pairs}
group_bones={name:{hand_bound_bone(obj).name for obj in obs if hand_bound_bone(obj)} for name,obs in groups.items()}
visible=[o for c in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT','05 Rider - scale reference, NOT game avatar') for o in bpy.data.collections[c].objects if o.type=='MESH' and not o.hide_render]
bounds=np.array([[tuple(v)+(1,) for v in o.bound_box] for o in visible])
ground={};findings={};deployment_contacts={};ik_errors={};ik_max={};rigid_invariant={};rigid_construction={};tested=0
terrain_cases=[];terrain_counts={};terrain_deployment=0;terrain_walk=0;terrain_progress=[]
terrain_alignment=0;terrain_alignment_counts={};terrain_alignment_fractions=[i/8 for i in range(9)];terrain_standing=0
terrain_contacts={};terrain_ground={};terrain_ik={metric:0. for metric in ('reachError','upperLengthError','lowerLengthError','ankleError')};terrain_hand_min=float('inf');terrain_hand_original={};terrain_hand_baseline_samples=0

def check(key,deployment=False,terrain=None,hand_baseline=None):
    global tested
    tested+=1;changed=surfaces();bad={}
    sample_hand_min=check_hands(key,hand_baseline)
    families={}
    for name,bones in group_bones.items():
        fs={last_gait_families.get(bone) for bone in bones};families[name]=next(iter(fs)) if len(fs)==1 else None
    for a,b in pairs:
        if families.get(a) is not None and families.get(a)==families.get(b):
            # The gait FK constructed BOTH groups as the same exact T times
            # their original neutral transforms. This is symbolic provenance,
            # not a larger collision/float tolerance or an exclusion by name.
            rigid_construction[a+' / '+b]=families[a];continue
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
    if terrain is None:
        rows=np.array([list(o.matrix_world[2]) for o in visible])
        lower=np.einsum('nij,nj->ni',bounds,rows).min(axis=1)
        for i in np.flatnonzero(lower<-.002):
            o=visible[i];actual=min((o.matrix_world@v.co).z for v in o.data.vertices)
            if actual<-.002:ground[o.name]=min(ground.get(o.name,0),actual)
    else:
        normal,step=terrain[:2];ground_offset=terrain[2] if len(terrain)>2 else 0.
        matrices=np.array([np.array(o.matrix_world) for o in visible])
        corners=np.einsum('nij,nkj->nki',matrices,bounds)[:,:,:3]
        lower=(corners@normal).min(axis=1)-(abs(step) if step else 0.)-ground_offset
        for i in np.flatnonzero(lower<-.002):
            o=visible[i];points=np.array([tuple(v.co)+(1,) for v in o.data.vertices])@matrices[i].T
            heights=(np.where(points[:,0]>=0,step,-step) if step else 0.)+ground_offset
            actual=float(np.min(points[:,:3]@normal-heights))
            if actual<-.002:terrain_ground.setdefault(key,{})[o.name]=actual
        if bad:terrain_contacts[key]=bad
        for diagnostics in last_gait_diagnostics.values():
            for metric in terrain_ik:terrain_ik[metric]=max(terrain_ik[metric],diagnostics[metric])
    for side,diagnostics in last_gait_diagnostics.items():
        for metric in ('reachError','upperLengthError','lowerLengthError','ankleError'):
            value=diagnostics[metric];name=side+'/'+metric
            ik_max[name]=max(ik_max.get(name,0),value)
            if not math.isfinite(value) or value>.002:ik_errors.setdefault(key,{})[name]=value
    return sample_hand_min
def report(bindings):
    missing=[name for name in bindings if not (ROOT/name).is_file()]
    altered=[name for name in bindings if input_hash(name)!=initial_hashes[name]]
    diagnostic=any(flag in sys.argv for flag in ('--start-domain','--end-domain','--terrain-only'))
    return dict(passed=neutral_valid and not findings and not ground and not ik_errors and not missing and not altered and not diagnostic and not hand_contacts and not terrain_ground,
        geometryPassed=neutral_valid and not findings and not ground and not ik_errors and not hand_contacts and not terrain_ground,missingBindings=missing,
        neutralPoseMaxError=neutral_max,neutralPoseWorstBone=neutral_worst,
        neutralPoseErrors={name:value for name,value in neutral_errors.items() if not math.isfinite(value) or value>2e-5},
        quick='--quick' in sys.argv or diagnostic,diagnosticRange=diagnostic,samples=tested,
        inputsChangedDuringAudit=altered,
        handBaselineSamples=hand_baseline_samples,handAbsoluteSamples=hand_absolute_samples,handBaselineExcessContacts=hand_baseline_excess,
        handContacts=hand_contacts,handCheckSides=sorted(hand_coverage),handCheckMeshes=len(hand_components),handCheckSamples=hand_samples,
        handFingerBoneCoverage={side:sorted(bones) for side,bones in hand_coverage.items()},handMinimumClearance=hand_minimum if math.isfinite(hand_minimum) else None,
        handClearanceThreshold=HAND_CLEARANCE,handDistanceTests=hand_distance_tests,handTriangleTests=hand_triangle_tests,handVolumeTests=hand_volume_tests,
        handWristMountBaseline={a+' / '+b:record for (a,b),record in hand_mount_baseline.items()},handWristMountMaximumDepth=hand_mount_observed,
        handScope='Complete visible hand/finger rigid components versus visible leg/hip and forearm/upperarm door armor components; conservative convex component envelopes, GJK volume distance including containment, plus unfiltered actual triangle intersections. 25mm separation; no finger/palm or leg/hip baseline exclusions. Only the four original wrist-bearing/stud contacts against their own forearm scale1 retain their exact idle clearance and cannot deepen by more than1.5mm.',
        newContacts=findings,belowGround=ground,ikErrors=ik_errors,maximumIkErrors=ik_max,
        terrainCases=terrain_cases,terrainSamples=sum(terrain_counts.values()),terrainCaseSamples=terrain_counts,
        terrainDeploymentSamples=terrain_deployment,terrainWalkSamples=terrain_walk,terrainDeploymentProgress=terrain_progress,
        terrainAlignmentSamples=terrain_alignment,terrainAlignmentFractions=terrain_alignment_fractions,terrainAlignmentCaseSamples=terrain_alignment_counts,
        terrainPreparationSource='unchanged V18 support-parallel; existing129 flat release samples',terrainStandingRelease=1,terrainStandingSamples=terrain_standing,
        terrainContacts=terrain_contacts,terrainBelowGround=terrain_ground,maximumTerrainIkErrors=terrain_ik,
        terrainHandBaselineSource='same-frame V18 flat-ground transformation',terrainHandBaselineSamples=terrain_hand_baseline_samples,
        terrainHandBaselineProgress=[p for p in terrain_progress if p<1],terrainHandBaselineOriginal=terrain_hand_original,
        terrainHandAbsoluteSamples=sum(terrain_counts.values())-terrain_hand_baseline_samples,terrainHandBaselineExcessContacts=hand_baseline_excess,
        terrainHandBaselineGapFloor=HAND_PACKED_GAP_FLOOR,terrainHandBaselineMaximumGapReduction=hand_baseline_gap_reduction,
        terrainHandBaselinePackedMinPositiveGap=hand_packed_min_positive if math.isfinite(hand_packed_min_positive) else None,
        terrainHandBaselineMaximumGapShortfall=max(0.,hand_gap_shortfall),
        terrainHandMinimumClearance=terrain_hand_min if math.isfinite(terrain_hand_min) else None,
        terrainScope='Four +/-8degree planes: unchanged V18 deployment terrain-parallel, existing129 flat clearance preparations, then nine opened-pose root-alignment fractions per slope, plus32 actual walking/sprint poses per slope or opposite10cm interfoot step at both moving and raised preparation root heights. Fully opened standing/gait hands remain25mm clear; compacted transformation hands use same-frame original evidence only.',
        existingClosedPosePairs={a+' / '+b:dict(triangles=len(v),depth=baseline_depth[a,b]) for (a,b),v in baseline.items() if v},
        separatingMountContacts=deployment_contacts,rigidInvariantPairs=rigid_invariant,rigidConstructionFamilies=rigid_construction,
        hashes={name:initial_hashes[name] for name in bindings},scope=__doc__)

# BEGIN_DOMAIN_REVIEW
domain=json.loads((ROOT/DOMAIN_FILE).read_text())
def domain_index(flag,default):
    if flag not in sys.argv:return default
    try:return int(sys.argv[sys.argv.index(flag)+1])
    except (ValueError,IndexError):raise ValueError(flag+' requires an integer pose index')
start=domain_index('--start-domain',0);end=domain_index('--end-domain',len(domain))
if not 0<=start<end<=len(domain):raise ValueError('Diagnostic domain range must satisfy 0 <= start < end <= '+str(len(domain)))
# Range selection is only for repairing an already identified late pose. It
# never qualifies as a complete review, even if its selected range is large.
for i in (range(129) if start==0 and '--terrain-only' not in sys.argv else ()):
    p=neutral_pose();p['release']=i/128;apply_pose(p);check('deployment/'+str(i),True)
deployment_samples=tested
print('V3 DEPLOY',tested,'bad',len(findings),'ground',len(ground),'IK',len(ik_errors),flush=True)
previous=None
for i in (range(start,end) if '--terrain-only' not in sys.argv else ()):
    p=domain[i]
    if '--quick' in sys.argv and (i%6!=0):continue
    apply_pose(p);check('domain/'+str(i),p['amount']==0)
    if previous!=p['scenario']:
        print('V3 DOMAIN',p['scenario'],'poses',tested,'bad',len(findings),'ground',len(ground),'IK',len(ik_errors),flush=True)
        previous=p['scenario']
    if (findings or ground or ik_errors or hand_contacts) and '--fail-fast' in sys.argv:break
domain_samples=tested-deployment_samples
# Terrain is a separate complete domain. Diagnostic index ranges do not silently
# add it, while an explicit terrain-only repair run remains non-activating.
if (not any(flag in sys.argv for flag in ('--start-domain','--end-domain')) or '--terrain-only' in sys.argv) and not (findings or ground or ik_errors or hand_contacts):
    original_baseline=(reference,baseline,baseline_depth,baseline_relative)
    def capture_reference():
        global reference,baseline,baseline_depth,baseline_relative
        reference={k:(geometry(v),np.linalg.inv(np.array(v[0].matrix_world))) for k,v in groups.items()}
        last.clear();last_inverse.clear();shapes.clear();surfaces()
        baseline={p:hits(shapes[p[0]],shapes[p[1]]) for p in pairs}
        baseline_depth={p:depth(shapes[p[0]],shapes[p[1]],v) for p,v in baseline.items()}
        baseline_relative={p:reference[p[0]][1]@last[p[1]] for p in pairs}
    selections=[]
    # Evenly sample each locomotion scenario, preserving actual support/flight,
    # turns and speed transitions rather than synthesizing phase formulas.
    for scenario in ('travel/1/1/0','travel/1/1/-1','travel/1/-1/1','sprint/0,75/-1','sprint/1/0','sprint/1/1'):
        candidates=[p for p in domain if p['scenario']==scenario and p['time']>=1]
        if candidates:
            for index in np.linspace(0,len(candidates)-1,6,dtype=int):selections.append(candidates[index])
    if len(selections)<32:selections=[domain[int(i)] for i in np.linspace(0,len(domain)-1,32)]
    selections=selections[:32]
    terrain_progress=sorted(set(round(.65+.025*i,6) for i in range(15))|{.82})
    for case in ('x+8','x-8','z+8','z-8','stepL+0.1','stepR+0.1','stepL+0.1-raised','stepR+0.1-raised'):
        terrain_cases.append(case);terrain_counts[case]=0
        step=(.05 if case.startswith('stepL') else -.05) if case.startswith('step') else 0.
        ground_offset=-abs(step) if case.endswith('-raised') else 0.
        slope=math.tan(math.radians(8))*(1 if '+' in case else -1)
        world_normal=Vector((-slope,0,1) if case.startswith('x') else (0,-slope,1)) if not step else Vector((0,0,1))
        world_normal.normalize()
        if not step:
            for progress in terrain_progress:
                # Original V18 stays parallel to the fitted support plane.
                # No per-leg IK or source panel reparenting alters that clip.
                local_normal=Vector((0,0,1))
                ground_provider=lambda point:(local_normal,0.)
                source=source_pose(progress);capture_reference()
                hand_base=None
                if progress<1:
                    _,hand_base=check_hands('original/'+str(progress),capture=True)
                    terrain_hand_original[str(progress)]=hand_base
                    terrain_hand_baseline_samples+=1
                else:
                    opened=neutral_pose();opened['release']=1;apply_pose(opened)
                    terrain_standing+=1
                key='terrain/'+case+'/deploy/'+str(progress)
                minimum=check(key,True,(np.array(local_normal),0.),hand_base)
                if progress>=1:terrain_hand_min=min(terrain_hand_min,minimum)
                terrain_counts[case]+=1;terrain_deployment+=1
                if (findings or terrain_ground or ik_errors or hand_contacts) and '--fail-fast' in sys.argv:break
        reference,baseline,baseline_depth,baseline_relative=original_baseline
        last.clear();last_inverse.clear();shapes.clear()
        for i,pose in enumerate(selections):
            if (findings or terrain_ground or ik_errors or hand_contacts) and '--fail-fast' in sys.argv:break
            def provider(point,n=world_normal,step=step,offset=ground_offset):return n,((step if point.x>=0 else -step) if step else -(n.x*point.x+n.y*point.y)/n.z)+offset
            ground_provider=provider;apply_pose(pose)
            key='terrain/'+case+'/walk/'+str(i)
            minimum=check(key,False,(np.array(world_normal),step,ground_offset));terrain_hand_min=min(terrain_hand_min,minimum)
            terrain_counts[case]+=1;terrain_walk+=1
        if not step and not ((findings or terrain_ground or ik_errors or hand_contacts) and '--fail-fast' in sys.argv):
            # The opened robot, not the compacted source animation, aligns to
            # world-up. Uniform slopes need only this Root-inverse normal;
            # FitWORLD and ActualWORLD are identical, so no second alpha.
            terrain_alignment_counts[case]=0
            support=Vector((0,0,1)).rotation_difference(world_normal)
            for fraction in terrain_alignment_fractions:
                root=support.slerp(Quaternion(),fraction)
                normal=root.inverted()@world_normal
                ground_provider=lambda point,n=normal:(n,-(n.x*point.x+n.y*point.y)/n.z)
                opened=neutral_pose();opened['release']=1;apply_pose(opened)
                key='terrain/'+case+'/alignment/'+str(fraction)
                minimum=check(key,False,(np.array(normal),0.));terrain_hand_min=min(terrain_hand_min,minimum)
                terrain_counts[case]+=1;terrain_alignment+=1;terrain_alignment_counts[case]+=1
                if (findings or terrain_ground or ik_errors or hand_contacts) and '--fail-fast' in sys.argv:break
        print('V4 TERRAIN',case,'poses',terrain_counts[case],'contacts',len(terrain_contacts),'ground',len(terrain_ground),'hands',len(hand_contacts),flush=True)
        if (findings or terrain_ground or ik_errors or hand_contacts) and '--fail-fast' in sys.argv:break
    ground_provider=None
result=report(DOMAIN_BINDINGS)
result.update(domainRange=[start,end],deploymentSamples=deployment_samples,domainSamples=domain_samples)
(OUT/('locomotion-v%d-check.json'%GAIT_VERSION)).write_text(json.dumps(result,indent=2))
print('V4 RESULT',result['passed'],'terrain',sum(terrain_counts.values()),flush=True)
