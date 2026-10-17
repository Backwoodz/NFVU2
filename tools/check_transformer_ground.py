"""Check rendered rigid meshes against the studio floor, including subframes.

Run inside Blender with the study .blend loaded. No geometry is modified.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
import numpy as np

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--step', type=float, default=.5)
parser.add_argument('--end', type=int, default=265)
parser.add_argument('--fail-on-crossing', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
assert args.step > 0
scene = bpy.context.scene
scene.frame_set(1)
deps = bpy.context.evaluated_depsgraph_get()
meshes = []
for collection in bpy.data.collections:
    if not collection.name.startswith(('01 ', '02 ', '05 ')):
        continue
    for obj in collection.objects:
        if obj.type != 'MESH' or obj.hide_render:
            continue
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        used = sorted({i for p in mesh.polygons for i in p.vertices})
        vertices = np.array([tuple(mesh.vertices[i].co) for i in used], dtype=np.float64)
        evaluated.to_mesh_clear()
        meshes.append((obj, vertices))
worst = {}
minimum = float('inf')
samples = list(np.arange(1, args.end + .0001, args.step))
for frame in samples:
    scene.frame_set(int(frame), subframe=float(frame % 1))
    for obj, vertices in meshes:
        world = np.array(obj.matrix_world, dtype=np.float64)
        z = float(np.min(vertices @ world[2, :3] + world[2, 3]))
        minimum = min(minimum, z)
        if z < -.0001 and (obj.name not in worst or z < worst[obj.name]['min_z_m']):
            worst[obj.name] = dict(frame=float(frame), min_z_m=z,
                                   module=obj.get('module'), parent=obj.parent.name if obj.parent else None)
result = dict(blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
              floor_z_m=0, tolerance_m=.0001, minimum_z_m=minimum,
              sampled_frames=len(samples), step=args.step, mesh_count=len(meshes),
              scope='Rigid rendered meshes including static modifiers; integer and fractional frames',
              crossings=worst)
out = Path(bpy.data.filepath).parent/'ground-clearance.json'
out.write_text(json.dumps(result, indent=2))
print('GROUND', json.dumps(result), flush=True)
if args.fail_on_crossing and worst:
    raise RuntimeError('Geometry passes below floor; see ground-clearance.json')
