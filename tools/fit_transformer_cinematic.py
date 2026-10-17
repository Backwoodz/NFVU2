"""Time-distributed V18 neon/rider clearance fitting; run on unfitted blend.

Fits translations on already connected carriers, never mesh scale. Candidate
data must subsequently pass the independent half-frame scene audit.
"""
import json
import math
import bisect
from pathlib import Path
from collections import defaultdict
import bpy
import numpy as np
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'tools/transformer_cinematic_clearance.json'
scene=bpy.context.scene
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
groups=defaultdict(list)
for o in bpy.data.collections['01 Original 350Z surfaces'].objects:groups['panel.'+o['module']].append(o)
for o in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if o.get('continuous_unsegmented'):groups['neon.'+o.name[12:]].append(o)
groups['rider']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
families=defaultdict(list)
for name in groups:
    if name.startswith('neon.'):
        family=name[:-2] if name.endswith((' L',' R')) else name
        families[family].append(name)
    elif name.startswith('panel.floor1_'):
        families['floor1'].append(name)
    elif name.startswith('panel.apron_'):
        families[name[:-2]].append(name)
    elif name.startswith('panel.sill2_'):
        families['sill2'].append(name)
    elif name.startswith('panel.hood_'):
        families[name[:-2]].append(name)
    elif name=='panel.pelvis_rear':
        families[name].append(name)
pairs=[(a,b) for a in groups for b in groups if a<b and ('rider' in (a,b) or a.startswith('neon.') or b.startswith('neon.') or
       (a.startswith('panel.hood_') and b.startswith('panel.lamp_')) or (b.startswith('panel.hood_') and a.startswith('panel.lamp_')))]
scene.frame_set(1)
reference={k:(geometry(v),np.linalg.inv(np.array(v[0].matrix_world))) for k,v in groups.items() if k!='rider'}

def frame_data():
    result={}
    for name,obs in groups.items():
        if name=='rider':result[name]=geometry(obs);continue
        (points,faces,tris,normals),inverse=reference[name]
        delta=np.array(obs[0].matrix_world)@inverse;r=delta[:3,:3];p=delta[:3,3]
        result[name]=(points@r.T+p,faces,tris@r.T+p,normals@r.T)
    return result

frames=[1+i*.5 for i in range(241)]
seed=json.loads(OUT.read_text()) if OUT.exists() else {}
seed={float(k):v for k,v in seed.items()};knots=sorted(seed)
def initial(name,frame):
    if not knots:return np.zeros(3)
    index=max(1,min(len(knots)-1,bisect.bisect_right(knots,frame)))
    lo,hi=knots[index-1:index+1]
    a=np.array(seed[lo].get(name,(0,0,0)));b=np.array(seed[hi].get(name,(0,0,0)))
    return a+(b-a)*(frame-lo)/(hi-lo)
offsets={name:np.array([initial(name,frame) for frame in frames]) for name in groups}
data=[];cache=[]
for frame in frames:
    scene.frame_set(int(frame),subframe=frame%1);d=frame_data();data.append(d)
    cache.append({name:placed(g,offsets[name][len(data)-1]) for name,g in d.items()})
    if frame%20==1:print('FIT CACHE',frame,flush=True)
baseline={p:set() if 'rider' in p else hits(cache[0][p[0]],cache[0][p[1]])|hits(cache[-1][p[0]],cache[-1][p[1]]) for p in pairs}
def count(f,p):return len(hits(cache[f][p[0]],cache[f][p[1]])-baseline[p])
counts=[{p:count(f,p) for p in pairs} for f in range(len(frames))]
def metric(n):return math.sqrt(n)+4 if n else 0
blocked=set()
for iteration in range(180):
    choices=[]
    for f in range(1,len(frames)-1):
        for family,members in families.items():
            if (f,family) in blocked:continue
            score=sum(metric(n) for p,n in counts[f].items() if p[0] in members or p[1] in members)
            if score:choices.append((score,f,family))
    if not choices:break
    _,f,family=max(choices);members=families[family]
    relevant=[p for p in pairs if p[0] in members or p[1] in members]
    best_score=0;best=None
    for width in (.65,1.5,3.0):
        samples=list(range(max(1,f-12),min(len(frames)-1,f+13)))
        weights={g:math.exp(-(frames[g]-frames[f])**2/(2*width*width)) for g in samples}
        old={g:{n:cache[g][n] for n in members} for g in samples}
        old_score=sum(metric(counts[g][p]) for g in samples for p in relevant)
        for axis in range(3):
            for amount in (-.30,-.12,-.04,-.012,.012,.04,.12,.30):
                shifts={};valid=True
                for g in samples:
                    for name in members:
                        sign=-1 if axis==0 and (name.endswith((' R','_R')) or '_R_fold' in name) else 1
                        delta=np.zeros(3);delta[axis]=amount*weights[g]*sign
                        d=offsets[name][g]+delta
                        if np.linalg.norm(d)>1.1:valid=False;break
                        candidate=placed(data[g][name],d)
                        if candidate[3][2]<0:valid=False;break
                        cache[g][name]=candidate;shifts[g,name]=delta
                    if not valid:break
                if valid:
                    trials={g:{p:count(g,p) for p in relevant} for g in samples}
                    score=sum(metric(n) for values in trials.values() for n in values.values())-old_score+.1*abs(amount)
                    if score<best_score-1e-6:
                        best_score=score
                        best=(shifts,trials,{g:{n:cache[g][n] for n in members} for g in samples})
                for g in samples:cache[g].update(old[g])
    if best:
        shifts,trials,updated=best
        for (g,name),delta in shifts.items():offsets[name][g]+=delta
        for g,values in trials.items():counts[g].update(values);cache[g].update(updated[g])
        blocked.clear()
    else:blocked.add((f,family))
    remaining=sum(n>0 for row in counts for n in row.values())
    print('FIT',iteration,frames[f],family,'remaining',remaining,'improved',bool(best),flush=True)
    OUT.write_text(json.dumps({str(frame):{name:[round(float(x),8) for x in offsets[name][f]] for name in groups
        if np.linalg.norm(offsets[name][f])>1e-8} for f,frame in enumerate(frames)},indent=2))
    if not remaining:break
print('FIT REMAINING',json.dumps({str(frames[f]):{a+' / '+b:n for (a,b),n in row.items() if n}
                                for f,row in enumerate(counts) if any(row.values())}),flush=True)
