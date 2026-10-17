"""Find new transverse panel intersections in the saved Blender study.

Run with Blender --background STUDY.blend --python this_file.py.
Existing source intersections are recorded separately. A 1.5 mm plane tolerance
filters touching seams. This detects surface crossings, not enclosed volume,
coplanar overlap, minimum separation or rigid-body contact. No hidden exclusions
of interior panels: all imported original surfaces are included.
"""
import argparse
import hashlib
import itertools
import json
import sys
from collections import defaultdict
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

parser = argparse.ArgumentParser()
parser.add_argument('--step', type=float, default=2)
parser.add_argument('--pair', action='append', default=[], metavar='MODULE:MODULE',
                    help='Check only these explicit module pairs (repeatable)')
parser.add_argument('--focus', action='append', default=[], metavar='MODULE',
                    help='Check these modules against every other included group')
parser.add_argument('--roundtrip', action='store_true', help='Include hold and reverse animation')
parser.add_argument('--tolerance', type=float, default=.0015, help='Transverse plane tolerance in metres')
parser.add_argument('--output', help='Report filename within the study folder')
parser.add_argument('--fail-on-crossing', action='store_true')
parser.add_argument('--rider', action='store_true', help='Also check the reference rider against vehicle panels')
parser.add_argument('--neon', action='store_true', help='Also check continuous neon rails')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
if args.step <= 0 or args.tolerance < 0:
    parser.error('--step must be positive')
scene = bpy.context.scene
modules = defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:
    modules[obj['module']].append(obj)
if args.rider:
    modules['RIDER_REFERENCE'] = list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)
if args.neon:
    for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
        if obj.get('continuous_unsegmented'):
            modules['NEON::'+obj.name]=[obj]


def mesh_at(objects):
    verts, faces, owners = [], [], []
    for obj in objects:
        offset = len(verts)
        verts.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        for polygon in obj.data.polygons:
            ids=polygon.vertices
            for j in range(1,len(ids)-1):
                faces.append((offset+ids[0],offset+ids[j],offset+ids[j+1]))
                owners.append(obj.get('source_part',obj.name))
    tris = [[verts[i] for i in face] for face in faces]
    normals = [(t[1]-t[0]).cross(t[2]-t[0]).normalized() for t in tris]
    bounds = [(min(v[a] for v in verts), max(v[a] for v in verts)) for a in range(3)]
    return BVHTree.FromPolygons(verts, faces, all_triangles=True, epsilon=0), tris, normals, bounds, owners


def crossing(a, b, na, nb):
    da = [(v-b[0]).dot(nb) for v in a]
    db = [(v-a[0]).dot(na) for v in b]
    return min(da) < -args.tolerance and max(da) > args.tolerance and min(db) < -args.tolerance and max(db) > args.tolerance


def rigid_carrier(objects):
    node = objects[0].parent
    while node.parent:
        if node.animation_data and node.animation_data.action:
            for curve in node.animation_data.action.fcurves:
                values = [key.co.y for key in curve.keyframe_points]
                if values and max(values)-min(values) > 1e-5:
                    return node.name
        node = node.parent
    return node.name


pairs = [(a, b) for a, b in itertools.combinations(sorted(modules), 2)
         if rigid_carrier(modules[a]) != rigid_carrier(modules[b])]
if args.pair:
    requested=set()
    for pair in args.pair:
        names=pair.split(':')
        if len(names)!=2 or names[0]==names[1] or any(n not in modules for n in names):
            parser.error('Unknown or invalid module pair: '+pair)
        requested.add(tuple(sorted(names)))
    pairs=sorted(requested)
    used={name for pair in pairs for name in pair}
    modules={name:objects for name,objects in modules.items() if name in used}
if args.focus:
    if any(name not in modules for name in args.focus):
        parser.error('Unknown focus module')
    pairs=[(a,b) for a,b in pairs if a in args.focus or b in args.focus]
baseline = {}
findings = defaultdict(list)
end=265 if args.roundtrip else 121
samples=sorted(set([1.,121.,float(end)]+[round(1+i*args.step,8) for i in range(int((end-1)/args.step)+1)]))
for frame in samples:
    scene.frame_set(int(frame), subframe=frame%1)
    cache = {name: mesh_at(objects) for name, objects in modules.items()}
    for a, b in pairs:
        ta, va, na, ba, oa = cache[a]
        tb, vb, nb, bb, ob = cache[b]
        if any(ba[i][1] < bb[i][0] or bb[i][1] < ba[i][0] for i in range(3)):
            hits = set()
        else:
            hits = {(i, j) for i, j in ta.overlap(tb) if crossing(va[i], vb[j], na[i], nb[j])}
        if frame == 1:
            baseline[a, b] = hits
        new = hits if 'RIDER_REFERENCE' in (a,b) else hits-baseline[a, b]
        if new:
            i, j = next(iter(new))
            findings[a+' / '+b].append(dict(frame=frame, triangle_pairs=len(new),
                example=[i, j], source_parts=[oa[i], ob[j]], position=list(sum(va[i], Vector())/3)))
    if frame%10==1 or frame==end:print('CLEARANCE FRAME', frame, flush=True)
result = dict(blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
    sampled_frames=samples, plane_tolerance_m=args.tolerance,
    scope='Explicit module pairs' if args.pair else 'Focused modules against included groups' if args.focus else 'All original surface groups; rigid co-moving groups skipped; new transverse crossings only',
    checked_pairs=[list(pair) for pair in pairs],
    not_checked=['enclosed volumes', 'coplanar overlap', 'clearance distance', 'new mechanism against panels'],
    baseline_triangle_pairs=sum(len(x) for x in baseline.values()),
    reference_rider_checked=args.rider,
    neon_rails_checked=args.neon,
    new_crossing_pairs=dict(findings))
out = Path(bpy.data.filepath).parent/(args.output or ('clearance-with-neon.json' if args.neon else 'clearance-with-rider.json' if args.rider else 'clearance.json'))
out.write_text(json.dumps(result, indent=2))
print('CLEARANCE SUMMARY', json.dumps({pair:dict(samples=len(hits), maximum=max(h['triangle_pairs'] for h in hits),
    first=hits[0]['frame'], last=hits[-1]['frame']) for pair, hits in findings.items()}), flush=True)
if args.fail_on_crossing and findings:
    raise RuntimeError('New vehicle surface crossings found; see '+out.name)
