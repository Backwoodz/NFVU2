"""Fit symmetric carrier clearances to an existing study, without changing meshes.

Run in Blender on the study. Outputs candidate world-space slide corrections;
these must be baked, checked at full sampling density, and visually reviewed.
"""
import argparse
import itertools
import json
import math
import sys
from collections import defaultdict
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--frames', default='13,31,55,79,103')
parser.add_argument('--passes', type=int, default=12)
parser.add_argument('--output', default='.local/transformer-slide-candidates.json')
parser.add_argument('--limit', type=float, default=.72)
parser.add_argument('--step-max', type=float, default=.36)
parser.add_argument('--free-end', action='store_true')
parser.add_argument('--neon-only', action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
scene=bpy.context.scene
groups=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:
    name=obj['module']
    if name.startswith('fender_'): name='front_wheel_'+name[-1]
    if name.startswith('mirror_'): name='door_front_'+name[-1]
    if name=='spoiler': name='hatch'
    groups[name].append(obj)
groups['RIDER_REFERENCE']=list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if not obj.get('continuous_unsegmented'):continue
    if args.neon_only:
        suffix=obj.name.split('NEON TUBE - ')[-1]
        name='neon_'+suffix.replace('underbody side ','side_').replace('underbody ','').replace('trunk ','trunk_')
        groups[name].append(obj)
        continue
    ancestor=obj.parent
    while ancestor and not ancestor.name.startswith('MODULE - '):ancestor=ancestor.parent
    if ancestor:
        name=ancestor.name[len('MODULE - '):]
        if name=='spoiler':name='hatch'
        groups[name].append(obj)
    else:
        groups['NEON '+obj.name].append(obj)
families=defaultdict(list)
for name in groups:
    if args.neon_only and not name.startswith('neon_'):continue
    if name!='RIDER_REFERENCE' and not name.startswith('NEON '):
        families[name[:-2] if name.endswith(('_L','_R')) else name].append(name)
pairs=list(itertools.combinations(sorted(groups),2))

def geometry(objects):
    vertices=[]; faces=[]
    for obj in objects:
        offset=len(vertices)
        vertices.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        for p in obj.data.polygons:
            for j in range(1,len(p.vertices)-1):
                faces.append((offset+p.vertices[0],offset+p.vertices[j],offset+p.vertices[j+1]))
    points=np.array(vertices,dtype=np.float64)
    triangles=points[np.array(faces)]
    normals=np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0])
    lengths=np.linalg.norm(normals,axis=1); normals/=np.maximum(lengths[:,None],1e-20)
    return points, faces, triangles, normals

def placed(data,shift):
    points,faces,tris,normals=data
    points=points+shift
    return (BVHTree.FromPolygons(points.tolist(),faces,all_triangles=True),
            tris+shift,normals,points.min(axis=0),points.max(axis=0))

def hits(a,b):
    ta,va,na,lo,hi=a; tb,vb,nb,lb,hb=b
    if np.any(hi<lb) or np.any(hb<lo): return set()
    overlaps=ta.overlap(tb)
    if not overlaps: return set()
    ia,ib=np.array(overlaps).T
    da=np.einsum('ijk,ik->ij',va[ia]-vb[ib,0,None],nb[ib])
    db=np.einsum('ijk,ik->ij',vb[ib]-va[ia,0,None],na[ia])
    good=(da.min(axis=1)<-.0015)&(da.max(axis=1)>.0015)&(db.min(axis=1)<-.0015)&(db.max(axis=1)>.0015)
    return {overlaps[i] for i in np.flatnonzero(good)}

scene.frame_set(1)
data={k:geometry(v) for k,v in groups.items()}
cache={k:placed(v,np.zeros(3)) for k,v in data.items()}
baseline={(a,b):hits(cache[a],cache[b]) if 'RIDER_REFERENCE' not in (a,b) else set() for a,b in pairs}
previous={name:np.zeros(3) for name in groups}
results={}
for frame in map(int,args.frames.split(',')):
    scene.frame_set(frame)
    data={k:geometry(v) for k,v in groups.items()}
    limit=args.limit*min(1.,max(0.,(frame-1)/12.),1. if args.free_end else max(0.,(121-frame)/12.))
    decay=.86 if frame>90 else .995
    offsets={name:previous[name]*decay*min(1.,limit/max(np.linalg.norm(previous[name]),1e-9)) for name in groups}
    cache={k:placed(v,offsets[k]) for k,v in data.items()}
    def count(pair):
        a,b=pair
        return len(hits(cache[a],cache[b])-baseline[pair])
    counts={pair:count(pair) for pair in pairs}
    before=sum(n>0 for n in counts.values())
    for iteration in range(args.passes):
        changed=0
        ordered=sorted(families.items(), key=lambda item: -sum(counts[p] for p in pairs if p[0] in item[1] or p[1] in item[1]))
        for family,names in ordered:
            relevant=[p for p in pairs if p[0] in names or p[1] in names]
            if not any(counts[p] for p in relevant): continue
            old={name:cache[name] for name in names}
            oldoffset={name:offsets[name].copy() for name in names}
            def cost(values, shifts):
                return sum((math.sqrt(n)+4)*(50 if 'RIDER_REFERENCE' in p else 10 if any(x.startswith('NEON ') for x in p) else 1)
                           for p,n in values.items() if n)+.2*sum(np.linalg.norm(v) for v in shifts.values())
            best=cost({p:counts[p] for p in relevant},oldoffset)
            best_offsets=None; best_counts=None; best_cache=None
            steps=(.006,.03,.08,.18,.36) if iteration<6 else (.004,.015,.05,.12,.25)
            steps=tuple(s for s in steps if s<=args.step_max)
            for axis in range(3):
                if axis==0 and not any(n.endswith(('_L','_R')) for n in names): continue
                for step in steps:
                    for sign in (-1,1):
                        shifts={}
                        for name in names:
                            d=oldoffset[name].copy()
                            d[axis]+=step*sign*(-1 if axis==0 and name.endswith('_R') else 1)
                            if np.linalg.norm(d)>limit+1e-9: break
                            candidate=placed(data[name],d)
                            if candidate[3][2]<-.0001: break
                            shifts[name]=d; cache[name]=candidate
                        else:
                            trial={p:count(p) for p in relevant}
                            score=cost(trial,shifts)
                            if score<best-1e-6:
                                best=score; best_offsets=shifts.copy(); best_counts=trial
                                best_cache={name:cache[name] for name in names}
                        cache.update(old)
            if best_offsets:
                offsets.update(best_offsets); counts.update(best_counts); cache.update(best_cache); changed+=1
        remaining=sum(n>0 for n in counts.values())
        print('FIT',frame,iteration,before,remaining,changed,flush=True)
        if not remaining or not changed: break
    results[str(frame)]={name:[round(float(v),6) for v in d] for name,d in offsets.items() if np.linalg.norm(d)>1e-6}
    previous=offsets
    results_path=Path(args.output)
    results_path.parent.mkdir(parents=True,exist_ok=True)
    results_path.write_text(json.dumps(results,indent=2))
    print('REMAINING',frame,{str(p):n for p,n in counts.items() if n},flush=True)
