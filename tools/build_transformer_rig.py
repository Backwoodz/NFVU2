"""Bind the approved V16 design to a persistent articulated V17 armature.

Blender --background --threads 2 --python tools/build_transformer_rig.py

This is the rigging milestone, not a validated car transformation. The active
action exercises guided elbow plates, elbows and fingers with rigid geometry.
Car-space targets are reconstructed from the actual V8 source pivots, including
all subsequent lossless cuts. No mesh is hidden, resized or replaced to pack it.
"""
import hashlib
import json
import math
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'artifacts/transformer-study-v17'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT/'artifacts/transformer-study-v16/350z-transformer-design.blend'
CAR = ROOT/'artifacts/transformer-study-v8/350z-transformer-study.blend'


def serial(matrix):
    return [list(row) for row in matrix]


def shape_hash(obj):
    data = obj.data
    return hashlib.sha256(repr((
        [tuple(v.co) for v in data.vertices],
        [tuple(p.vertices) for p in data.polygons],
        [tuple(v.uv) for v in data.uv_layers.active.data] if data.uv_layers.active else []
    )).encode()).hexdigest()


def center(obj):
    return sum((obj.matrix_world @ Vector(p) for p in obj.bound_box), Vector())/8


bpy.ops.wm.open_mainfile(filepath=str(CAR))
bpy.context.scene.frame_set(1)
car_frames = {o.name[9:]: o.matrix_world.copy() for o in bpy.context.scene.objects
              if o.name.startswith('MODULE - ')}
car_neon = {o.name: o.matrix_world.copy() for o in bpy.context.scene.objects
            if o.get('continuous_unsegmented')}
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.frame_set(1)
originals = bpy.data.collections['01 Original 350Z surfaces']
hardware = bpy.data.collections['02 Authored mechanism - BLOCKOUT']
controls = bpy.data.collections['03 Module controls']
rider = bpy.data.collections['05 Rider - scale reference, NOT game avatar']
meshes = [o for col in (originals, hardware, rider) for o in col.objects if o.type == 'MESH']
baseline = {o.name: dict(shape=shape_hash(o), world=serial(o.matrix_world)) for o in meshes}
old_world = {o.name: o.matrix_world.copy() for o in scene.objects}
old_parent = {o.name: o.parent.name if o.parent else None for o in scene.objects}
groups = defaultdict(list)
for obj in originals.objects:
    groups[obj['module']].append(obj)
modules = {role: objects[0].parent for role, objects in groups.items()}

# All bones are authored in the approved world-space rest pose. Geometry binds
# rigidly through bone parenting; a car pane can never acquire skin weights.
bpy.ops.object.select_all(action='DESELECT')
data = bpy.data.armatures.new('350Z articulated mechanism')
rig = bpy.data.objects.new('RIG - 350Z V17', data)
controls.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
rig.show_in_front = True
data.display_type = 'STICK'
bpy.ops.object.mode_set(mode='EDIT')
parents = {}


def bone(name, head, tail=None, parent=None, layer=0):
    b = data.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail) if tail is not None else b.head+Vector((0, .12, 0))
    assert (b.tail-b.head).length > 1e-5, name
    if parent:
        b.parent = data.edit_bones[parent]
    b.use_connect = False
    b.layers = [i == layer for i in range(32)]
    parents[name] = parent
    return name


bone('root', (0, 0, 0), (0, 0, .30))
bone('pelvis', (0, -.03, 2.16), (0, -.03, 2.43), 'root')
bone('thorax', (0, -.03, 2.43), (0, 0, 3.73), 'pelvis')
bone('head', (0, .10, 3.92), (0, .10, 4.43), 'thorax')
for side, sign in [('L', 1), ('R', -1)]:
    shoulder, elbow, wrist = [(sign*x, y, z) for x, y, z in
                              [(1.07, 0, 3.53), (1.37, .05, 2.88), (1.30, .39, 2.24)]]
    hip, knee, ankle = [(sign*x, y, z) for x, y, z in
                        [(.40, -.03, 2.16), (.68, .28, 1.22), (.88, -.02, .34)]]
    bone('clavicle.'+side, (sign*.27, -.02, 3.54), shoulder, 'thorax')
    bone('upperarm.'+side, shoulder, elbow, 'clavicle.'+side)
    bone('forearm.'+side, elbow, wrist, 'upperarm.'+side)
    bone('hand.'+side, wrist, (sign*1.30, .39, 1.87), 'forearm.'+side)
    bone('thigh.'+side, hip, knee, 'pelvis')
    bone('shin.'+side, knee, ankle, 'thigh.'+side)
    bone('foot.'+side, ankle, (sign*.88, .50, .12), 'shin.'+side)

    for digit in ('digit0', 'digit1', 'digit2', 'digit3', 'thumb'):
        for index in range(3):
            obj = bpy.data.objects['DESIGN - V11 %s %s link%d' % (side, digit, index)]
            head = obj.matrix_world.translation
            if index < 2:
                tail = bpy.data.objects['DESIGN - V11 %s %s link%d' % (side, digit, index+1)].matrix_world.translation
            else:
                tail = obj.matrix_world @ Vector((0, 0, .10))
            bone('%s%d.%s' % (digit, index, side), head, tail,
                 'hand.'+side if index == 0 else '%s%d.%s' % (digit, index-1, side), layer=1)


def panel_parent(role):
    side = 'L' if '_L' in role else 'R'
    if role.startswith(('front_wheel', 'lamp_', 'taillamp_', 'fender_')):
        return 'clavicle.'+side
    if role.startswith('mirror_'):
        return 'panel.door_front_scale0_'+side
    if role.startswith('door_front'):
        return 'forearm.'+side
    if role.startswith('door_rear'):
        return 'upperarm.'+side
    if role.startswith(('rear_wheel', 'rear_arch', 'shinface', 'floor1_', 'sill1_', 'sill2_')):
        return 'shin.'+side
    if role.startswith(('rear_corner', 'floor0_', 'floor2_', 'sill0_', 'sill3_')):
        return 'thigh.'+side
    if role.startswith('toe_'):
        return 'foot.'+side
    if role.startswith(('hipguard_', 'pelvis_', 'exhaust_')) or 'hipfold1' in role:
        return 'pelvis'
    return 'thorax'


for role, control in sorted(modules.items(),key=lambda item:item[0].startswith('mirror_')):
    pivot=old_world[control.name].translation.copy()
    bone('panel.'+role, pivot, parent=panel_parent(role), layer=2)

neon_controls = [o for o in controls.objects if o.name.startswith('DESIGN - protected neon ')]
for o in neon_controls:
    label = o.name[len('DESIGN - protected neon '):]
    side = label[-1]
    parent = ('shin.'+side if label.startswith('underbody side') else
              'pelvis' if label == 'trunk rear' else 'thorax')
    bone('neon.'+label, old_world[o.name].translation, parent=parent, layer=2)

seat = bpy.data.objects['DESIGN - retained rider and shoulder saddle']
bone('rider.anchor', old_world[seat.name].translation, parent='thorax', layer=3)
for side in ('L', 'R'):
    parent = 'rider.anchor'
    for part in ('thigh', 'calf'):
        obj = bpy.data.objects['LINK - REFERENCE '+side+' '+part]
        bone('rider.'+part+'.'+side, obj.matrix_world.translation,
             obj.matrix_world @ Vector((0, 0, 1)), parent, layer=3)
        parent = 'rider.'+part+'.'+side
    parent = 'rider.anchor'
    for part in ('upper arm', 'forearm'):
        obj = bpy.data.objects['LINK - REFERENCE '+side+' '+part]
        name = 'rider.'+part.replace(' ', '')+'.'+side
        bone(name, obj.matrix_world.translation, obj.matrix_world @ Vector((0, 0, 1)), parent, layer=3)
        parent = name
bpy.ops.object.mode_set(mode='OBJECT')
data.layers = [i in (0, 1) for i in range(32)]
bpy.context.view_layer.update()
bindings = {}


def bind(obj, name):
    # Blender's bone-object parent uses the tail frame, unlike pose matrices.
    # Cancelling that frame explicitly also avoids a dependency-graph lag.
    world = old_world[obj.name]
    b = data.bones[name]
    tail_frame = b.matrix_local @ Matrix.Translation((0, b.length, 0))
    obj.parent = rig
    obj.parent_type = 'BONE'
    obj.parent_bone = name
    obj.matrix_parent_inverse = tail_frame.inverted()
    obj.matrix_basis = world
    obj['rig_binding'] = name
    bindings[obj.name] = name


for role, obj in modules.items():
    bind(obj, 'panel.'+role)
for obj in neon_controls:
    bind(obj, 'neon.'+obj.name[len('DESIGN - protected neon '):])
bind(seat, 'rider.anchor')
bind(bpy.data.objects['DESIGN - V11 interlocking cranial assembly'], 'head')
for side in ('L', 'R'):
    for label in ('upperarm', 'forearm', 'thigh', 'shin'):
        bind(bpy.data.objects['DESIGN - '+label+' '+side], label+'.'+side)
    bind(bpy.data.objects['DESIGN - foot structure '+side], 'foot.'+side)
    bind(bpy.data.objects['DESIGN - V11 articulated hand '+side], 'hand.'+side)
    for digit in ('digit0', 'digit1', 'digit2', 'digit3', 'thumb'):
        for index in range(3):
            bind(bpy.data.objects['DESIGN - V11 %s %s link%d' % (side, digit, index)],
                 '%s%d.%s' % (digit, index, side))
    for part in ('thigh', 'calf', 'upper arm', 'forearm'):
        bind(bpy.data.objects['LINK - REFERENCE '+side+' '+part],
             'rider.'+part.replace(' ', '')+'.'+side)
    bind(bpy.data.objects['NEW - REFERENCE boot '+side], 'rider.calf.'+side)
    bind(bpy.data.objects['NEW - REFERENCE hand '+side], 'rider.forearm.'+side)

bpy.context.view_layer.update()


def nearest(point, names, at_head=False):
    def distance(name):
        b = data.bones[name]
        if at_head:
            return (point-b.head_local).length_squared
        a, delta = b.head_local, b.tail_local-b.head_local
        u = max(0, min(1, (point-a).dot(delta)/delta.length_squared))
        return (point-a-u*delta).length_squared
    return min(names, key=distance)


# Resolve each route as an assembly so rib rings/fittings cannot select a
# different side or anatomical joint from the hose they belong to.
route_bindings = {}
for obj in hardware.objects:
    route = obj.get('v13_route')
    if not route or route in route_bindings:
        continue
    c = center(bpy.data.objects[route]); side = 'L' if c.x > 0 else 'R'
    route_bindings[route] = (
        'upperarm.'+side if 'upper arm' in route else
        'thigh.'+side if 'thigh' in route else
        'shin.'+side if 'calf' in route else
        'pelvis' if any(s in route for s in ('lumbar', 'rear exhaust', 'rear return')) else 'thorax')


def loose_binding(obj):
    name = obj.name
    p = center(obj) if obj.type == 'MESH' else obj.matrix_world.translation
    side = 'L' if p.x >= 0 else 'R'
    if obj.get('v13_route'):
        return route_bindings[obj['v13_route']]
    for role in sorted(modules, key=len, reverse=True):
        if name.endswith(role) and any(s in name for s in ('panel hinge', 'floor carrier')):
            return 'panel.'+role
    if 'V13 hood fold hinge' in name:
        return 'panel.'+min(('hood_1_'+side, 'hood_2_'+side),
                            key=lambda r: (old_world[modules[r].name].translation-p).length)
    if 'V16 spoiler pedestal' in name or 'V16 spoiler hinge' in name:
        return 'panel.spoiler'
    if 'V16 dorsal neon' in name:
        return 'neon.underbody rear'
    if 'neon chassis mount' in name or name.startswith('V9 neon spill '):
        return 'neon.'+min((o.name[len('DESIGN - protected neon '):] for o in neon_controls),
                          key=lambda n: (data.bones['neon.'+n].head_local-p).length)
    if any(s in name for s in ('V16 spoiler', 'V16 neck collar', 'V11 cervical', 'neck actuator')):
        return 'thorax'
    if 'neck ram' in name or 'neck main bearing' in name:
        return 'head'
    if 'V10 shoulder' in name or 'original assembly carrier' in name or 'headlight mounting spine' in name or 'clavicle' in name or 'shoulder gimbal' in name:
        return 'clavicle.'+side
    if 'wrist alignment' in name:
        return 'hand.'+side
    if any(s in name for s in ('V13 rear', 'V13 turbine', 'iliac', 'pelvic', 'hip vane')):
        return 'pelvis'
    if 'hamstring' in name or 'thigh chevron' in name:
        return 'thigh.'+side
    if any(s in name for s in ('calf ', 'rear knee', 'knee cap', 'knee front', 'knee hydraulic')):
        return 'shin.'+side
    if any(s in name for s in ('Achilles', 'heel anchor', 'ankle load', 'ankle hydraulic')):
        return 'foot.'+side
    if 'bearing stud' in name or 'end bearing' in name or 'bearing recess' in name:
        return nearest(p, [s+'.'+side for s in ('forearm', 'hand', 'shin', 'foot')], at_head=True)
    if p.z < 2.16 and abs(p.x) > .22:
        return nearest(p, [s+'.'+side for s in ('thigh', 'shin', 'foot')])
    return 'thorax' if p.z > 2.47 else 'pelvis'


for obj in list(hardware.objects):
    parent = old_parent[obj.name]
    if parent in ('DESIGN - robot end pose - no transformation yet', 'DESIGN - V13 body mechanism'):
        bind(obj, loose_binding(obj))
    elif parent in ('DESIGN - V11 articulated hand L', 'DESIGN - V11 articulated hand R'):
        # Knuckles, tendons and claws were originally siblings of finger links.
        # Bind them to their actual phalanges instead of leaving them on the palm.
        if any(s in obj.name for s in ('finger ', 'thumb ', 'talon', 'gripping tip')):
            side = parent[-1]
            digits = ['thumb'] if 'thumb' in obj.name else ['digit'+str(i) for i in range(4)]
            names = ['%s%d.%s' % (d, i, side) for d in digits for i in range(3)]
            bind(obj, nearest(center(obj), names,
                              at_head=any(s in obj.name for s in ('bearing', 'knuckle', 'swivel'))))

bpy.context.view_layer.update()
rest_error = max(max(abs(o.matrix_world[r][c]-baseline[o.name]['world'][r][c])
                     for r in range(4) for c in range(4)) for o in meshes)
assert rest_error < 2e-5, rest_error
assert all(shape_hash(o) == baseline[o.name]['shape'] for o in meshes)


def bound_bone(obj):
    while obj and obj != rig:
        if obj.parent == rig and obj.parent_type == 'BONE':
            return obj.parent_bone
        obj = obj.parent
    return None


unbound = [o.name for o in meshes if not bound_bone(o)]
assert not unbound, unbound
# V16's hidden arm castings intersect their car-shell armor even at rest. Make
# room inside the approved exterior by narrowing the authored arm mechanisms
# around their longitudinal bone axes. Original surfaces, head, hands, rider
# and all seven neon tubes are explicitly excluded.
reworked_inner_mechanism=[]
for obj in hardware.objects:
    if obj.type!='MESH':continue
    name=bound_bone(obj)
    if not name or not name.startswith(('upperarm.','forearm.')):continue
    b=data.bones[name]
    to_bone=b.matrix_local.inverted()@obj.matrix_world
    to_object=to_bone.inverted()
    for vertex in obj.data.vertices:
        p=to_bone@vertex.co
        p.x*=.65;p.z*=.65
        vertex.co=to_object@p
    obj.data.update()
    reworked_inner_mechanism.append(obj.name)
    baseline[obj.name]['shape']=shape_hash(obj)
mapping = {}
for role, objects in groups.items():
    obj = objects[0]
    source_role = obj.get('source_module', role)
    pivot = Vector(obj.get('source_local_pivot', (0, 0, 0)))
    assert source_role in car_frames, (role, source_role)
    car_matrix = car_frames[source_role] @ Matrix.Translation(pivot)
    mapping[role] = dict(bone='panel.'+role, body_parent=panel_parent(role),
                         source_module=source_role, source_local_pivot=list(pivot),
                         car_world=serial(car_matrix), robot_world=serial(old_world[modules[role].name]),
                         objects=[o.name for o in objects],
                         folding_path_validated=False)
for obj in neon_controls:
    tube = next(o for o in obj.children if o.get('continuous_unsegmented'))
    assert tube.name in car_neon
    mapping['neon.'+tube.name[12:]] = dict(
        bone=obj.parent_bone, car_world=serial(car_neon[tube.name] @ tube.matrix_local.inverted()),
        robot_world=serial(old_world[obj.name]), objects=[tube.name], folding_path_validated=False)
(OUT/'car-module-targets.json').write_text(json.dumps(mapping, indent=2))

# Functional cycle: unlock plates -> flex -> return -> relock, for each arm.
# Bone chains retain their physical length. No scale/visibility keys exist.
for p in rig.pose.bones:
    p.rotation_mode = 'QUATERNION'
frames = (1, 13, 25, 49, 65, 81, 93, 105, 117, 129, 153, 169, 185, 197, 209)
for frame in frames:
    left = {49: 1, 65: 1}.get(frame, 0)
    right = {153: 1, 169: 1}.get(frame, 0)
    for p in rig.pose.bones:
        p.rotation_quaternion = Quaternion()
        p.location=(0,0,0)
        if p.name.startswith('panel.door_'):
            p.location=data.bones[p.name].matrix_local.to_quaternion().inverted()@Vector((0,.20,0))
    for side, sign, value in [('L', 1, left), ('R', -1, right)]:
        front = float((side=='L' and 13<=frame<=93) or (side=='R' and 117<=frame<=197))
        rear = float((side=='L' and 25<=frame<=81) or (side=='R' and 129<=frame<=185))
        for name, world_axis, angle in [
            ('forearm.'+side, (1, 0, 0), 48*value),
            ('panel.door_rear_scale1_'+side,(0,0,1),-sign*90*rear)]:
            axis = data.bones[name].matrix_local.to_quaternion().inverted() @ Vector(world_axis)
            rig.pose.bones[name].rotation_quaternion = Quaternion(axis, math.radians(angle))
        for name,translation in [('panel.door_front_scale0_'+side,(0,.54*front,0)),
                                  ('panel.door_rear_scale1_'+side,(sign*(.08*front+.57*rear),.28*front-.15*rear,0))]:
            rig.pose.bones[name].location+=data.bones[name].matrix_local.to_quaternion().inverted()@Vector(translation)
        for digit in range(4):
            for index in range(3):
                name = 'digit%d%d.%s' % (digit, index, side)
                axis = data.bones[name].matrix_local.to_quaternion().inverted() @ Vector((0, -sign, 0))
                rig.pose.bones[name].rotation_quaternion = Quaternion(axis, math.radians((8+index*4)*value))
    for p in rig.pose.bones:
        p.keyframe_insert('rotation_quaternion', frame=frame, group=p.name)
        if p.name.startswith('panel.door_'):
            p.keyframe_insert('location', frame=frame, group=p.name)
action = rig.animation_data.action
action.name = 'V17 - articulation check - NOT car transformation'
for fcurve in action.fcurves:
    for key in fcurve.keyframe_points:
        key.interpolation = 'BEZIER'
        key.handle_left_type = key.handle_right_type = 'AUTO_CLAMPED'
action.use_fake_user = True
for marker in list(scene.timeline_markers):
    scene.timeline_markers.remove(marker)
for name, frame in [('Approved V16 pose', 1), ('Left arm and fingers', 49),
                    ('Neutral', 105), ('Right arm and fingers', 153), ('Exact return', 209)]:
    scene.timeline_markers.new(name, frame=frame)
scene.frame_start, scene.frame_end, scene.render.fps = 1, 209, 24
scene.frame_set(1)

# The four sliding plates are carried by actual nested, fixed-length sleeves.
# Both ends are gimballed to body and panel bones. Replace only their old static
# authored rods; original vehicle surfaces and all other approved meshes stay.
retired=[]
carrier_report=[]
def anchor(name,bone_name,point):
    o=bpy.data.objects.new(name,None);controls.objects.link(o)
    b=data.bones[bone_name]
    o.parent=rig;o.parent_type='BONE';o.parent_bone=bone_name
    o.matrix_parent_inverse=(b.matrix_local@Matrix.Translation((0,b.length,0))).inverted()
    o.matrix_basis=Matrix.Translation(point)
    return o
for side in ('L','R'):
    for role in ('door_front_scale0_'+side,'door_front_scale1_'+side,
                 'door_rear_scale0_'+side,'door_rear_scale1_'+side):
        old=bpy.data.objects['V9 - panel hinge '+role]
        lateral=Vector(((.45 if side=='L' else -.45) if role.startswith('door_front_scale0') else 0,0,0))
        root_point=old_world[old.name].translation+Vector((0,-.15,0))
        a=anchor('CARRIER root '+role,panel_parent(role),root_point+lateral)
        b=anchor('CARRIER end '+role,'panel.'+role,old_world[modules[role].name].translation+Vector((0,-.22,0))+lateral)
        material=old.data.materials[0]
        retired.append(old.name);meshes.remove(old);baseline.pop(old.name)
        bindings.pop(old.name,None);bpy.data.objects.remove(old,do_unlink=True)
        spans=[]
        for frame in frames:
            scene.frame_set(frame)
            spans.append((b.matrix_world.translation-a.matrix_world.translation).length)
        low,high=min(spans),max(spans)
        count=max(2,math.ceil((high/low-1)/.70)+1)
        length=high/(1+.70*(count-1))
        assert length <= low+1e-5
        tracking=bpy.data.objects.new('CARRIER gimbal '+role,None);controls.objects.link(tracking)
        tracking.parent=a
        tracking.rotation_mode='QUATERNION'
        constraint=tracking.constraints.new('DAMPED_TRACK');constraint.target=b;constraint.track_axis='TRACK_Z'
        for index in range(count):
            radius=.012+(count-index-1)*.0023
            assert radius>.008
            verts=[];faces=[]
            for z in (0,length):
                for r in (radius,radius-.0015):
                    verts.extend((r*math.cos(i*2*math.pi/12),r*math.sin(i*2*math.pi/12),z) for i in range(12))
            for i in range(12):
                j=(i+1)%12
                faces.extend([(i,j,j+24,i+24),(i+12,i+36,j+36,j+12),
                              (i,i+12,j+12,j),(i+24,j+24,j+36,i+36)])
            mesh=bpy.data.meshes.new('Fixed hollow sleeve '+role)
            mesh.from_pydata(verts,[],faces);mesh.update();mesh.materials.append(material)
            obj=bpy.data.objects.new('V17 carrier sleeve %d - %s'%(index,role),mesh)
            hardware.objects.link(obj);obj.parent=tracking
            obj['v17_carrier']=role;obj['fixed_length_m']=length
            obj['rig_binding']=panel_parent(role)
            driver=obj.driver_add('location',2).driver
            variable=driver.variables.new();variable.name='span';variable.type='LOC_DIFF'
            variable.targets[0].id=a;variable.targets[1].id=b
            driver.expression='(span-%.12f)*%.12f'%(length,index/(count-1))
            meshes.append(obj)
        carrier_report.append(dict(role=role,sleeves=count,length_m=length,
                                   minimum_span_m=low,maximum_span_m=high,minimum_overlap_fraction=.30))
        scene.frame_set(1);bpy.context.view_layer.update()
        # Visible spherical bearings and a fixed terminal lug close the gap
        # between the last sleeve and the existing panel collar.
        for label,point,bone_name in [('root',a.matrix_basis.translation,panel_parent(role)),
                                      ('end',b.matrix_basis.translation,'panel.'+role)]:
            bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.035,location=point)
            bearing=bpy.context.object
            for col in list(bearing.users_collection):col.objects.unlink(bearing)
            hardware.objects.link(bearing);bearing.name='V17 carrier '+label+' bearing '+role
            bearing.data.materials.append(material)
            old_world[bearing.name]=bearing.matrix_world.copy();bind(bearing,bone_name)
            meshes.append(bearing)
        start=b.matrix_basis.translation.copy()
        end=old_world[modules[role].name].translation.copy()
        bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.019,depth=(end-start).length,
                                          location=(start+end)/2)
        lug=bpy.context.object
        for col in list(lug.users_collection):col.objects.unlink(lug)
        hardware.objects.link(lug);lug.name='V17 carrier terminal lug '+role
        lug.rotation_mode='QUATERNION';lug.rotation_quaternion=(end-start).to_track_quat('Z','Y')
        lug.data.materials.append(material);bpy.context.view_layer.update()
        old_world[lug.name]=lug.matrix_world.copy();bind(lug,'panel.'+role)
        meshes.append(lug)
        if lateral.length:
            bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.026,depth=lateral.length,
                                              location=root_point+lateral/2)
            bridge=bpy.context.object
            for col in list(bridge.users_collection):col.objects.unlink(bridge)
            hardware.objects.link(bridge);bridge.name='V17 carrier outer clevis '+role
            bridge.rotation_mode='QUATERNION';bridge.rotation_quaternion=lateral.to_track_quat('Z','Y')
            bridge.data.materials.append(material);bpy.context.view_layer.update()
            old_world[bridge.name]=bridge.matrix_world.copy();bind(bridge,panel_parent(role))
            meshes.append(bridge)
scene.frame_set(1);bpy.context.view_layer.update()
for obj in meshes:
    if obj.name not in baseline:
        baseline[obj.name]=dict(shape=shape_hash(obj),world=serial(obj.matrix_world))
rig['milestone'] = 'Articulated V16 design; car-space targets reconstructed; transformation not validated'
rig['rider_contract'] = 'One persistent live Player in game; mannequin here is only a scale/pose reference'
rig['glass_contract'] = 'Whole rigid source panes; never skinned or segmented'
rig['neon_contract'] = 'Seven continuous persistent tubes; no scaling or visibility animation'
rig['car_module_targets'] = 'car-module-targets.json'

notes = '''350Z TRANSFORMER V17 - ARTICULATED RIG

V16 remains the armature bind pose. The functional neutral pose at frames
1, 105 and 209 puts the eight door plates 20cm ahead of their former locations
to clear the inner arm mechanisms; mirrors follow their original front panels.
Space/play tests guided elbow plates, elbows and connected finger chains.
This action is a joint test, NOT the car-to-robot transformation.

Armature layers: 1 body, 2 fingers, 3 vehicle panels/neon, 4 rider.
All source surfaces are rigid bone attachments, including whole glazing.
Eight old static authored panel rods become fixed-length nested gimbal sleeves.
The two elbow plates clear their neighbours before the elbow flexes.
Authored inner arm mechanisms are narrowed radially inside the original shell
to resolve intersections already present in the static V16 design.
The same 17 mannequin meshes remain attached at the nape; no chair is added.
Car-module-targets.json reconstructs original car placements from V8 and all
subsequent lossless segment pivots. It is target data, NOT validated folding paths.

OPEN before a game integration: pack the authored inner mechanism, add folding
carriers and cross-joint hose deformation, validate swept clearances, transfer
the rider, then animate both transformation directions. Existing V16 panel
contacts are not erased by introducing a rig.
'''
for t in list(bpy.data.texts):
    bpy.data.texts.remove(t)
bpy.data.texts.new('READ ME - V17 rig scope').write(notes)
(OUT/'READ-ME.txt').write_text(notes)
scene.render.threads_mode = 'FIXED'; scene.render.threads = 2
scene.render.resolution_x = scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.eevee.taa_render_samples = 16
camera = scene.camera
camera.location = (7, 11, 4.8)
camera.rotation_euler = (Vector((0, 0, 2.5))-camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.ortho_scale = 6.1
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_perspective = 'CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'350z-transformer-rig.blend'))
report = dict(source_blend_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
              blend_sha256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
              bones=len(data.bones), panel_modules=len(groups), neon_tubes=7, wheels=4,
              rigid_meshes=len(meshes), direct_bindings=len(bindings), unbound_meshes=unbound,
              rest_matrix_max_error=rest_error, original_car_head_hands_rider_neon_geometry_preserved=True,
              reworked_inner_mechanism=reworked_inner_mechanism,
              neutral_door_plate_forward_clearance_m=.20,
              replaced_static_authored_rods=retired, telescopic_panel_carriers=carrier_report,
              bindings=bindings, mesh_bones={o.name: bound_bone(o) for o in meshes},
              transformation_validated=False, car_packing_verified=False,
              action=action.name, frames=[1,209], baseline=baseline)
(OUT/'rig-audit.json').write_text(json.dumps(report, indent=2))
print('V17 RIG', len(data.bones), 'bones;', len(meshes), 'meshes; rest error', rest_error, flush=True)
