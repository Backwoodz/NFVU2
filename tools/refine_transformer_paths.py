"""Smooth carrier slides and resolve collisions with time-distributed corrections.

Run on the fitted study. Corrections span neighbouring frames, avoiding isolated
one-frame evasions. Output remains a candidate until the scene is rebuilt/tested.
"""
import sys,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=(root/'tools/fit_transformer_paths.py').read_text()
exec(source[:source.index('\nscene.frame_set(1)')])
raw=json.loads((root/'tools/transformer_carrier_paths.json').read_text())
names=sorted(groups)
original={n:np.array([raw.get(str(f),{}).get(n,[0,0,0]) for f in range(1,122)]) for n in names}
kernel=np.exp(-np.arange(-4,5,dtype=float)**2/(2*1.6**2));kernel/=kernel.sum()
offsets={n:np.array([np.convolve(np.pad(original[n][:,a],(4,4),mode='edge'),kernel,mode='valid') for a in range(3)]).T-original[n] for n in names}
for n in names:offsets[n][0]=offsets[n][-1]=0
data=[];cache=[]
for f in range(1,122):
    scene.frame_set(f)
    frame={k:geometry(v) for k,v in groups.items()};data.append(frame)
    cache.append({k:placed(v,offsets[k][f-1]) for k,v in frame.items()})
    if f%30==0:print('CACHE',f,flush=True)
baseline={(a,b):hits(cache[0][a],cache[0][b]) if 'RIDER_REFERENCE' not in (a,b) else set() for a,b in pairs}
def count(f,pair):
    a,b=pair
    return len(hits(cache[f][a],cache[f][b])-baseline[pair])
counts=[{pair:count(f,pair) for pair in pairs} for f in range(121)]
def metric(n):return math.sqrt(n)+4 if n else 0
blocked=set()
def save():
    result={str(f+1):{n:[round(float(v),7) for v in original[n][f]+offsets[n][f]] for n in names
                     if np.linalg.norm(original[n][f]+offsets[n][f])>1e-8} for f in range(121)}
    Path(args.output).write_text(json.dumps(result,indent=2))
for iteration in range(args.passes):
    choices=[]
    for f in range(1,120):
        for family,members in families.items():
            if (f,family) in blocked:continue
            score=sum(metric(n) for p,n in counts[f].items() if p[0] in members or p[1] in members)
            if score:choices.append((score,f,family))
    if not choices:break
    _,f,family=max(choices);members=families[family]
    relevant=[p for p in pairs if p[0] in members or p[1] in members]
    best_change=None;best_counts=None;best_delta=None;best_score=0
    for width in (1.5,3.0):
        frames=list(range(max(1,f-6),min(120,f+7)))
        weights={g:math.exp(-(g-f)**2/(2*width*width)) for g in frames}
        old={g:{n:cache[g][n] for n in members} for g in frames}
        old_score=sum(metric(counts[g][p]) for g in frames for p in relevant)
        for axis in range(3):
            if axis==0 and not any(n.endswith(('_L','_R')) for n in members):continue
            for amount in (-.15,-.06,-.02,-.006,.006,.02,.06,.15):
                shifts={};valid=True
                for g in frames:
                    for n in members:
                        delta=np.zeros(3);delta[axis]=amount*weights[g]*(-1 if axis==0 and n.endswith('_R') else 1)
                        d=offsets[n][g]+delta
                        if np.linalg.norm(original[n][g]+d)>.90:valid=False;break
                        candidate=placed(data[g][n],d)
                        if candidate[3][2]<-.0001:valid=False;break
                        cache[g][n]=candidate;shifts[g,n]=delta
                    if not valid:break
                if valid:
                    trials={g:{p:count(g,p) for p in relevant} for g in frames}
                    score=sum(metric(n) for v in trials.values() for n in v.values())-old_score+.05*abs(amount)
                    if score<best_score-1e-6:
                        best_score=score;best_delta=shifts;best_counts=trials
                        best_change={g:{n:cache[g][n] for n in members} for g in frames}
                for g in frames:cache[g].update(old[g])
    if best_change:
        for (g,n),d in best_delta.items():offsets[n][g]+=d
        for g,changes in best_change.items():cache[g].update(changes);counts[g].update(best_counts[g])
        blocked.clear()
    else:blocked.add((f,family))
    remaining=sum(n>0 for c in counts for n in c.values())
    print('TEMPORAL',iteration,'frame',f+1,family,'remaining',remaining,'gain',round(best_score,2),flush=True)
    if iteration%5==0 or not remaining:save()
    if not remaining:break
save()
print('TEMPORAL REMAINING',{str(f+1):{str(p):n for p,n in c.items() if n} for f,c in enumerate(counts) if any(c.values())},flush=True)
