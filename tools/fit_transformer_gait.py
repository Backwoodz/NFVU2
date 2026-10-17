"""Fit small symmetric, connected panel releases for the flat-ground gait.

Only rigid translation of named armor modules. Never edits source surfaces,
glazing, lights, wheel shapes or the accepted transformation. Review and run
the independent 65-phase checker after fitting. Blender: one process, 2 threads.
"""
from pathlib import Path
import json,itertools,sys
amplitude=float(sys.argv[sys.argv.index('--amount')+1]) if '--amount' in sys.argv else 1.
from collections import defaultdict
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
source=Path(__file__).with_name('preview_transformer_locomotion.py').read_text()
sys.argv.append('--no-clearance')
exec(source[:source.index('\nvisible=')])
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
groups=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:groups[obj['module']].append(obj)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.get('continuous_unsegmented'):groups[obj.name].append(obj)
groups['RIDER']=[o for o in bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects if o.type=='MESH']
pairs=list(itertools.combinations(sorted(groups),2))
gait(0,0)
reference={k:(geometry(v),np.linalg.inv(np.array(v[0].matrix_world))) for k,v in groups.items()}
initial={k:placed(data[0],np.zeros(3)) for k,data in reference.items()}
baseline={p:(set() if 'RIDER' in p else hits(initial[p[0]],initial[p[1]])) for p in pairs}
poses=[]
for reverse,i in itertools.product((False,True),range(33)):
    gait(i/32,amplitude,reverse)
    frame={}
    for name,obs in groups.items():
        data,inverse=reference[name];points,faces,tris,normals=data
        d=np.array(obs[0].matrix_world)@inverse;r=d[:3,:3];t=d[:3,3]
        frame[name]=(points@r.T+t,faces,tris@r.T+t,normals@r.T)
    poses.append(frame)
families={}
for prefix in ('toe_','hipguard_','rear_corner_','sill1_','sill2_','floor0_L_fold2','floor0_R_fold2','floor1_','shinface_','rear_arch_'):
    # Both knee sides form one symmetric family.
    key='floor0_knee' if prefix.startswith('floor0_') else prefix
    families.setdefault(key,[]).extend(n for n in groups if n.startswith(prefix))
for fold in range(3):families['floor2_fold'+str(fold)]=[n for n in groups if n.startswith('floor2_') and '_fold%d_'%fold in n]
seed_path=ROOT/'tools/transformer_gait_clearance.json'
seed=json.loads(seed_path.read_text()) if seed_path.exists() else {}
profile_path=ROOT/'tools/transformer_gait_profiles.json'
profiles=json.loads(profile_path.read_text()) if profile_path.exists() else {}
if amplitude<1:
    smaller=[float(k) for k in profiles if float(k)<=amplitude]
    seed=profiles[str(max(smaller))] if smaller else {}
shifts={n:np.array(seed.get(n,[0,0,0]),dtype=float) for n in groups}
cache=[{n:placed(data,shifts[n]) for n,data in frame.items()} for frame in poses]
def pair_count(p):return sum(len(hits(c[p[0]],c[p[1]])-baseline[p]) for c in cache)
counts={p:pair_count(p) for p in pairs}
print('GAIT FIT before',sum(counts.values()),flush=True)
for iteration in range(12):
    changed=0
    ordered=sorted(families,key=lambda f:-sum(n for p,n in counts.items() if any(g in p for g in families[f])))
    for family in ordered:
        names=families[family];relevant=[p for p in pairs if p[0] in names or p[1] in names]
        if not sum(counts[p] for p in relevant):continue
        old={n:shifts[n].copy() for n in names}
        saved=[{n:c[n] for n in names} for c in cache]
        def cost(values,offsets):
            return sum((n**.5+4)*(30 if 'RIDER' in p or any(x.startswith('NEON') for x in p) else 1) for p,n in values.items() if n)+.2*sum(np.linalg.norm(s) for s in offsets.values())
        best=cost({p:counts[p] for p in relevant},old);best_offsets=None;best_counts=None;best_cache=None
        steps=[]
        for axis,amount in itertools.product(range(3),(-.18,-.10,-.05,-.02,-.006,.006,.02,.05,.10,.18)):
            v=np.zeros(3);v[axis]=amount;steps.append(v)
        steps += [np.array(v) for v in itertools.product((-.08,0,.08),repeat=3) if sum(x!=0 for x in v)>1]
        if family=='toe_':
            origin=old['toe_L']
            steps += [np.array(v)-origin for v in itertools.product((-.35,-.2,0,.2,.35),(-.25,0,.25,.4,.45),(0,.06,.15))]
        for step in steps:
            candidate={}
            for name in names:
                shift=old[name]+step*np.array([-1 if '_R' in name else 1,1,1])
                if np.linalg.norm(shift)>(.48 if family=='toe_' else .42):break
                candidate[name]=shift
            else:
                valid=True
                for i,frame in enumerate(poses):
                    for name in names:
                        c=placed(frame[name],candidate[name]);cache[i][name]=c
                        if c[3][2]<-.0005:valid=False;break
                    if not valid:break
                if valid:
                    values={p:pair_count(p) for p in relevant};score=cost(values,candidate)
                    if score<best-1e-7:
                        best=score;best_offsets=candidate;best_counts=values;best_cache=[{n:c[n] for n in names} for c in cache]
        for i in range(len(cache)):cache[i].update(saved[i])
        if best_offsets is not None:
            shifts.update(best_offsets);counts.update(best_counts)
            for i in range(len(cache)):cache[i].update(best_cache[i])
            changed+=1
    print('GAIT FIT',iteration,'remaining',sum(counts.values()),'changed',changed,flush=True)
    if not changed or not sum(counts.values()):break
result={n:list(map(float,s)) for n,s in shifts.items() if np.linalg.norm(s)>1e-6}
profiles[str(amplitude)]=result
profile_path.write_text(json.dumps(profiles,indent=2))
(OUT/('gait-fit-%s.json'%amplitude)).write_text(json.dumps(dict(sampledPhases=len(poses),remaining={a+' / '+b:n for (a,b),n in counts.items() if n},offsets=result),indent=2))
print('GAIT FIT SAVED',flush=True)
