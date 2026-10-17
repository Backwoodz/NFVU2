"""Build an editable, reversible exterior blockout from local 350Z assets.

Run: blender --background --python tools/build_transformer_study.py -- --render
Optional --animation renders a small preview movie with Blender's FFmpeg output.
Outputs stay in artifacts/transformer-study-v8; no Unity or installed mod changes.

The chassis, hinges and carriers form a connected kinematic study.
Original exterior panel scale and UVs are preserved; listed inner trims are adapted. New mechanism meshes are authored
placeholders. Cabin packing, collisions, cut-edge thickness and passenger space
remain design work, explicitly recorded in the scene and audit report.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE / 'tools'))
from transformer_parts import OPTIONS, prepare_study_part, selected as selected_original
from transformer_reassembly import selected, partition, sources as reassembly_sources, rigid_glazing_group

OUT = BASE / 'artifacts' / 'transformer-study-v8'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = BASE / '.local' / 'model' / 'body.json'
model = json.loads(SOURCE.read_text())
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--render', action='store_true')
parser.add_argument('--animation', action='store_true')
parser.add_argument('--quick', action='store_true', help='Fast EEVEE review renders')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.frame_start, scene.frame_end = 1, 265
scene.render.fps = 24
scene.unit_settings.system = 'METRIC'
originals = bpy.data.collections.new('01 Original 350Z surfaces')
mechanism = bpy.data.collections.new('02 Authored mechanism - BLOCKOUT')
controls = bpy.data.collections.new('03 Module controls')
studio = bpy.data.collections.new('04 Studio')
for collection in (originals, mechanism, controls, studio):
    scene.collection.children.link(collection)


def move_collection(obj, collection):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    collection.objects.link(obj)
    return obj


def material(name, color, metal=0., rough=.4):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metal
    shader.inputs['Roughness'].default_value = rough
    return mat


materials = []
for m in model['materials']:
    mat = material(m['name'], m['color'][:3], m['metallic'], max(.2, 1-m['smoothness']))
    shader = mat.node_tree.nodes.get('Principled BSDF')
    if m['texture'] >= 0:
        node = mat.node_tree.nodes.new('ShaderNodeTexImage')
        path = SOURCE.parent / (model['textures'][m['texture']]['name'] + '.png')
        node.image = bpy.data.images.load(str(path), check_existing=True)
        mix = mat.node_tree.nodes.new('ShaderNodeMixRGB')
        mix.blend_type = 'MULTIPLY'
        mix.inputs[0].default_value = 1
        mix.inputs[2].default_value = m['color']
        mat.node_tree.links.new(node.outputs['Color'], mix.inputs[1])
        mat.node_tree.links.new(mix.outputs[0], shader.inputs['Base Color'])
    if m['name'].startswith('471a1dca'):
        shader.inputs['Alpha'].default_value=.32
        shader.inputs['Metallic'].default_value=.15
        shader.inputs['Roughness'].default_value=.18
        mat.blend_method='BLEND'
        mat.use_screen_refraction=True
    materials.append(mat)
gunmetal = material('NEW - Graphite mechanism', (.075, .095, .11), .8, .28)
silver = material('NEW - Machined joint metal', (.38, .43, .47), .8, .24)
rubber = material('NEW - Joint bellows', (.022, .027, .03), .15, .65)
green = material('NEW - Head armor', (.025, .26, .07), .6, .3)
eye = material('NEW - Green optical core', (.10, .8, .16), .4)
eye.node_tree.nodes.get('Principled BSDF').inputs['Emission'].default_value = (.15, 1, .22, 1)


def empty(name, collection=controls):
    obj = bpy.data.objects.new(name, None)
    collection.objects.link(obj)
    obj.empty_display_type = 'PLAIN_AXES'
    obj.empty_display_size = .12
    return obj


# Segment pivots follow the actual cut geometry.
sources = reassembly_sources(model, prepare_study_part)
specs = {name: dict(source=Vector(position)) for name, position in sources.items()}
nodes = {}
for name, position in sources.items():
    obj = empty('MODULE - '+name)
    obj['role'] = name
    obj.rotation_mode = 'QUATERNION'
    obj.location = position
    nodes[name] = obj

ledger = []
rest_vertices = {}
max_area_error = 0.
source_triangles = output_triangles = 0
wheel_instances = set()
interior_adaptations = []
for part in model['parts']:
    if not selected(part):
        continue
    part, adaptation = prepare_study_part(part)
    if adaptation:
        interior_adaptations.append(adaptation)
    groups, audit = partition(part)
    max_area_error = max(max_area_error, max(abs(a-b) for a, b in audit))
    source_triangles += len(audit)
    instances = [(None, None)]
    if part['group'] in ('wheel', 'brake'):
        instances = [('front', 1), ('front', -1), ('rear', 1), ('rear', -1)]
    for axle, sign in instances:
        if axle:
            s = 'L' if sign > 0 else 'R'
            group = axle+'_wheel_'+s
            wheel_instances.add(group)
            hub = Vector((sign*.8, 1.375 if axle == 'front' else -1.29, .21))
            all_tris = [tri for tris in groups.values() for tri in tris]
            groups_to_add = {group: [[
                (v[0]*sign+hub.x, v[1]*sign+hub.y, v[2]+hub.z,
                 v[3]*sign, v[4]*sign, v[5], v[6], v[7]) for v in tri]
                for tri in all_tris]}
        else:
            groups_to_add = groups
        for group, tris in groups_to_add.items():
            if not tris:
                continue
            verts = [v for tri in tris for v in tri]
            mesh = bpy.data.meshes.new(part['name']+'__'+group)
            pivot = specs[group]['source']
            mesh.from_pydata([tuple(Vector(v[:3])-pivot) for v in verts], [],
                             [tuple(range(i, i+3)) for i in range(0, len(verts), 3)])
            mesh.update()
            uv = mesh.uv_layers.new(name='Original UV')
            for loop in mesh.loops:
                uv.data[loop.index].uv = verts[loop.vertex_index][6:8]
            for face in mesh.polygons:
                face.use_smooth = True
            mesh.use_auto_smooth = True
            mesh.normals_split_custom_set_from_vertices([
                tuple(Vector(v[3:6]).normalized()) for v in verts])
            mesh.materials.append(gunmetal if any(tag in part['name'] for tag in ('DOOR_PANEL_', 'TRUNK_UNDER_', 'HOOD_UNDER_')) else materials[part['material']])
            obj = bpy.data.objects.new(mesh.name, mesh)
            originals.objects.link(obj)
            obj.parent = nodes[group]
            obj['source_part'] = part['name']
            obj['module'] = group
            obj['provenance'] = 'Existing local 350Z export; cut surfaces preserve source UVs'
            rest_vertices[obj.name] = [Vector(v[:3])+Vector((0, 0, .14)) for v in verts]
            ledger.append(dict(source=part['name'], module=group, triangles=len(tris)))
            output_triangles += len(tris)


def box(name, center, size, mat, parent=None, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = move_collection(bpy.context.object, mechanism)
    obj.name = 'NEW - '+name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if parent:
        obj.parent = parent
    if bevel:
        mod = obj.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
    obj['provenance'] = 'NFVU2 authored blockout; not an original car part'
    return obj


from transformer_articulated_rig import rebuild
rig_report = rebuild(scene, nodes, specs, originals, mechanism, controls, box,
                     (gunmetal, silver, green, eye))

# Linear interpolation of densely sampled eased transforms avoids Bezier
# overshoot and makes the stored reverse animation match the forward samples.
for obj in list(nodes.values()) + list(controls.objects) + list(mechanism.objects):
    if obj.animation_data and obj.animation_data.action:
        for curve in obj.animation_data.action.fcurves:
            for key in curve.keyframe_points:
                key.interpolation = 'LINEAR'

notes = '''350Z TRANSFORMATION STUDY 08 - HOOD / HEADLIGHT CLEARANCE

Timeline: 1 car; 121 robot; 121-145 hold; 265 car restored.
Both car and robot face world +Y. The car does not rotate upright as a block.

The original exterior is cut into rigid armor segments with original UVs.
Hood halves form the chest; door halves form upper/forearm armor; split sills
form thigh and shin armor; roof and hatch fold into the backpack.
Every pane stays whole, including front/rear glass and side mirrors.
Whole glazing triangles, normals and UVs are preserved without clipping.
Headlights and tail lamps retain their imported shapes; exactly four wheels.
Final hood closure is staged: slide inwards in front of both headlights,
then retract onto the chest. Reverse uses the same path in reverse order.
Initial hood lift clears the lamp seam; a forward slide clears the rising lamps.
The final inward slide stays above the front neon rail before settling down.
The simplified original cockpit/boot/engine inserts are omitted in BOTH modes
and replaced by an authored articulated chassis, not hidden during animation.
Inner door/hatch linings use a graphite finish. Interior trim reliefs are in
audit.json. This does not certify a complete Rachel replica.

Four telescopic two-bone limbs unfold. Each armor module remains attached
by its carrier; pelvis, spine, neck and limb bearings form one hierarchy.
New geometry: mechanism, hands, soles, faceted head, and shoulder saddle.
The ochre mannequin is a scale reference, NOT an exported Valheim character.
One persistent seat anchor lifts it from the driver position onto the shoulder
bridge behind the robot head. Same actor on return; no visibility switching.
Runtime plan: keep the existing live Player and adapt VehicleCabinDriver.
Inventory/health/network identity must remain untouched. Not integrated yet.

Seven continuous neon rails stay visible and mounted throughout the motion.
Three trunk strips follow VehicleShowroom positions/dimensions. Four underbody
tubes are newly authored hardware based on VehicleNeon horizontal light positions (the current
runtime underbody glow has no physical tube mesh). No tube is cut or duplicated.

Knees fold upward during transfer; rear panels lift before rolling into armor.
Front bumper shin plates travel upright above the floor; toe plates sit higher.
check_transformer_ground.py verifies rendered meshes against the studio floor,
including half frames in both directions. See ground-clearance.json.

OPEN: swept-volume collisions, physically packed car interior, hinge travel,
load bearing and ground contacts. clearance.json documents vehicle-surface
crossings if run; it is not a certificate for the new mechanism. This is a
new design/kinematics concept, not a collision-free or game-ready animation.
'''

bpy.data.texts.new('READ ME - Blockout limits').write(notes)
(OUT/'READ-ME.txt').write_text(notes, encoding='utf-8')
for frame, label in [(1, 'CAR'), (31, 'UNLOCK'), (61, 'DEPLOY'), (91, 'RISE'),
                     (121, 'ROBOT'), (145, 'RETURN'), (265, 'CAR RESTORED')]:
    scene.timeline_markers.new(label, frame=frame)

# Verify the actual animated matrices, not merely the written keyframes.
scene.frame_set(1)
first = {o.name: o.matrix_world.copy() for o in originals.objects}
rest_error = max((o.matrix_world @ v.co - expected).length
                 for o in originals.objects for v, expected in zip(o.data.vertices, rest_vertices[o.name]))
scene.frame_set(265)
return_error = max(abs(o.matrix_world[r][c]-first[o.name][r][c])
                   for o in originals.objects for r in range(4) for c in range(4))
reverse_error = 0.
for forward in range(1, 122, 4):
    scene.frame_set(forward)
    pose = {o.name: o.matrix_world.copy() for o in originals.objects}
    scene.frame_set(266-forward)
    reverse_error = max(reverse_error, max(abs(o.matrix_world[r][c]-pose[o.name][r][c])
        for o in originals.objects for r in range(4) for c in range(4)))
assert max_area_error < 1e-8, max_area_error
assert rest_error < 1e-6, rest_error
assert return_error < 1e-6, return_error
assert reverse_error < 1e-6, reverse_error
assert len(wheel_instances) == 4
report = dict(source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
              selected_options=OPTIONS, source_triangles=sum(len(p['indices'])//3 for p in model['parts'] if selected(p)),
              prepared_triangles=source_triangles,
              output_triangles_including_wheel_instances=output_triangles,
              max_triangle_area_error=max_area_error,
              prepared_car_vertex_error=rest_error,
              car_return_matrix_error=return_error, reverse_matrix_error=reverse_error,
              wheels=sorted(wheel_instances), module_count=len(nodes),
              collision_free=False, cabin_packing_verified=False, parts=ledger)
report.update(rig_report)
report['interior_adaptations'] = interior_adaptations
report['rigid_glazing_sources'] = [p['name'] for p in model['parts'] if selected(p) and rigid_glazing_group(p)]
report['replaced_interior_parts'] = [p['name'] for p in model['parts'] if selected_original(p) and not selected(p)]
(OUT/'audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')

bpy.ops.mesh.primitive_plane_add(size=200)
ground = move_collection(bpy.context.object, studio)
ground.name = 'Studio floor'
ground.data.materials.append(material('Studio slate', (.075, .087, .10), .15, .5))
# A fixed direction reference makes the car/robot heading directly comparable.
arrow_mesh = bpy.data.meshes.new('Driving direction arrow')
arrow_mesh.from_pydata([(2.4+x, y, .008) for x, y in
    [(-.07, -1), (.07, -1), (.07, .55), (.25, .55), (0, 1), (-.25, .55), (-.07, .55)]],
    [], [(0, 1, 2, 3, 4, 5, 6)])
arrow_mesh.update()
arrow = bpy.data.objects.new('Fixed original driving direction +Y', arrow_mesh)
studio.objects.link(arrow)
arrow.data.materials.append(material('Direction marking', (.65, .75, .72), 0, .8))
for loc, energy, size in [((3, 5, 8), 1900, 5), ((-4, 2, 5), 1400, 4),
                           ((1, -4, 6), 2300, 3)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    lamp = move_collection(bpy.context.object, studio)
    lamp.data.energy, lamp.data.size = energy, size
    lamp.rotation_euler = (Vector((0, 0, 2))-lamp.location).to_track_quat('-Z', 'Y').to_euler()
bpy.ops.object.camera_add(location=(8, 11, 6))
camera = move_collection(bpy.context.object, studio)
camera.rotation_euler = (Vector((0, -.8, 2.2))-camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 7.0
scene.camera = camera
# Frame the complete sweep, including the roof carriage during the half-turn.
# Keep this camera fixed so heading comparisons remain meaningful.
camera_basis = camera.rotation_euler.to_quaternion()
camera_inverse = camera_basis.inverted()
limits = [[float('inf'), -float('inf')] for _ in range(2)]
framed_objects = [obj for obj in list(originals.objects)+list(mechanism.objects)+list(bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects)+[arrow] if obj.type=='MESH']
for frame in range(1, 122):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    for obj in framed_objects:
        for corner in obj.bound_box:
            projected = camera_inverse @ (obj.matrix_world @ Vector(corner))
            for axis in range(2):
                limits[axis][0] = min(limits[axis][0], projected[axis])
                limits[axis][1] = max(limits[axis][1], projected[axis])
camera_position = camera_inverse @ camera.location
camera.location += camera_basis @ Vector(tuple(
    sum(limits[axis])*.5-camera_position[axis] for axis in range(2))+(0,))
camera.data.ortho_scale = max(high-low for low, high in limits)*1.10
review_camera_location = camera.location.copy()
review_camera_rotation = camera.rotation_euler.copy()
review_camera_scale = camera.data.ortho_scale
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
if hasattr(scene.view_layers[0].cycles, 'use_denoising'):
    scene.view_layers[0].cycles.use_denoising = True
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.world.color = (.24, .24, .24)
scene.view_settings.view_transform = 'Filmic'
try:
    scene.view_settings.look = 'Medium High Contrast'
except TypeError:
    scene.view_settings.look = 'Filmic - Medium High Contrast'
scene.render.image_settings.file_format = 'PNG'
if args.quick:
    scene.render.engine = 'BLENDER_EEVEE'
    scene.eevee.taa_render_samples = 32
    scene.eevee.use_gtao = True
    scene.eevee.use_bloom = True
    scene.eevee.bloom_intensity = .025
    scene.eevee.bloom_threshold = 1.5
    scene.eevee.gtao_distance = 3
    scene.render.resolution_x = scene.render.resolution_y = 800
scene.frame_set(121)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.region_3d.view_perspective = 'CAMERA'
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'350z-transformer-study.blend'))
print('STUDY AUDIT', json.dumps({k: v for k, v in report.items() if k != 'parts'}))
if args.render:
    for frame, name in [(1, '01-car'), (19, '02-unlock'), (43, '03-fold'),
                         (67, '04-rise'), (91, '05-pack'), (121, '06-robot'), (265, '09-return')]:
        scene.frame_set(frame)
        scene.render.filepath = str(OUT/(name+'.png'))
        bpy.ops.render.render(write_still=True)
    scene.frame_set(121)
    for loc, name in [((0, 12, 3), '07-robot-front'), ((-7, -8, 5), '08-robot-rear'), ((7, 10, 5), '10-design')]:
        camera.location = loc
        camera.data.ortho_scale = 6.4
        camera.rotation_euler = (Vector((0, 0, 2.65))-camera.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(OUT/(name+'.png'))
        bpy.ops.render.render(write_still=True)
if args.animation:
    camera.data.ortho_scale = review_camera_scale
    camera.location = review_camera_location
    camera.rotation_euler = review_camera_rotation
    scene.render.resolution_x, scene.render.resolution_y = 640, 640
    scene.cycles.samples = 8
    if hasattr(scene.view_layers[0].cycles, 'use_denoising'):
        scene.view_layers[0].cycles.use_denoising = False
    # Sample at 12 fps, preserving the authored 24 fps timeline's duration.
    scene.frame_step = 2
    scene.render.fps = 12
    scene.render.image_settings.file_format = 'FFMPEG'
    scene.render.ffmpeg.format = 'MPEG4'
    scene.render.ffmpeg.codec = 'H264'
    scene.render.ffmpeg.constant_rate_factor = 'MEDIUM'
    scene.render.filepath = str(OUT/'350z-transformer-motion.mp4')
    bpy.ops.render.render(animation=True)
