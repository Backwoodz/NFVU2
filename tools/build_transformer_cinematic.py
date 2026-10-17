"""V18: cinematic compaction of authored internals, rigid original exterior.

Run with Blender 2.80, --background --threads 2 --python this_file.py.
The user explicitly permits concealed compression/disappearance of the robot
internals. Original panels, glazing, lamps, wheels, neon and rider never vanish.
This is an animation prototype, not a physically packable mechanism.
"""
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'tools'))
from transformer_kinematics import phase, q, matrix

OUT = ROOT/'artifacts/transformer-study-v18'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT/'artifacts/transformer-study-v17/350z-transformer-rig.blend'
V8 = ROOT/'artifacts/transformer-study-v8/350z-transformer-study.blend'
mapping = json.loads((SOURCE.parent/'car-module-targets.json').read_text())
audit = json.loads((SOURCE.parent/'rig-audit.json').read_text())

# Preserve the earlier ordered release, including the corrected hood/lamp path.
# Its endpoint is retargeted to V17, so the result needs its OWN clearance audit.
bpy.ops.wm.open_mainfile(filepath=str(V8))
old_scene = bpy.context.scene
source_paths = []
for frame in range(1, 122):
    old_scene.frame_set(frame)
    source_paths.append({o.name: o.matrix_world.copy() for o in old_scene.objects
                         if o.name.startswith(('MODULE - ', 'RIG - ', 'NEON TUBE - ', 'RIDER - one persistent'))})

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.frame_set(1)
rig = bpy.data.objects['RIG - 350Z V17']
rig.name = 'RIG - 350Z cinematic V18'
hardware = bpy.data.collections['02 Authored mechanism - BLOCKOUT']
controls = bpy.data.collections['03 Module controls']
originals = bpy.data.collections['01 Original 350Z surfaces']
rider = bpy.data.collections['05 Rider - scale reference, NOT game avatar']
rest = {b.name: b.matrix_local.copy() for b in rig.data.bones}
neutral = {p.name: p.matrix.copy() for p in rig.pose.bones}
world0 = {o.name: o.matrix_world.copy() for o in scene.objects}
rig.animation_data_clear()
for p in rig.pose.bones:
    p.rotation_mode = 'QUATERNION'
    p.matrix_basis = Matrix.Identity(4)


def blend(a, b, u):
    return matrix(a.translation.lerp(b.translation, u), a.to_quaternion().slerp(b.to_quaternion(), u))


def retarget(current, old_end, new_end, u):
    rotation = Quaternion().slerp(new_end.to_quaternion() @ old_end.to_quaternion().inverted(), u)
    return matrix(current.translation + (new_end.translation-old_end.translation)*u,
                  rotation @ current.to_quaternion())


def source_bone(frame, name):
    label = 'Pelvis - no whole car rotation' if name == 'pelvis' else name
    return source_paths[frame]['RIG - '+label].translation.copy()


def joint(frame, old_name, target, u):
    return source_bone(frame, old_name)+(Vector(target)-source_bone(120, old_name))*u


def scaled_frame(name, head, rotation, scale):
    return matrix(head, rotation) @ Matrix.Diagonal(Vector((*scale, 1)))


def segment(name, a, b, width=1):
    original = rig.data.bones[name]
    delta = b-a
    turn = (original.tail_local-original.head_local).rotation_difference(delta)
    return scaled_frame(name, a, turn @ rest[name].to_quaternion(),
                        (width, delta.length/original.length, width))


def ik(a, b, upper, lower, bend):
    a, b = Vector(a), Vector(b)
    delta = b-a
    distance = delta.length
    assert abs(upper-lower)+1e-5 < distance < upper+lower-1e-5, (distance, upper, lower)
    direction = delta/distance
    along = (upper*upper-lower*lower+distance*distance)/(2*distance)
    normal = Vector(bend)-direction*Vector(bend).dot(direction)
    normal.normalize()
    return a+direction*along+normal*math.sqrt(max(0, upper*upper-along*along))


# Original exterior destinations are object-control matrices, not bone frames.
destinations = {}
for role, item in mapping.items():
    bone = item['bone']
    control_rest = Matrix(item['robot_world'])
    destinations[role] = neutral[bone] @ rest[bone].inverted() @ control_rest

# Remove static stand-ins only where an actual live attachment replaces them.
retired = []
for obj in list(hardware.objects):
    name = obj.name
    if (name.startswith('V17 carrier ') or
        any(word in name for word in ('panel hinge ', 'floor carrier ',
             'V10 original assembly carrier', 'V16 spoiler frame upright',
             'V16 spoiler folding arm', 'V16 spoiler actuator'))):
        retired.append(name)
        bpy.data.objects.remove(obj, do_unlink=True)
for obj in list(controls.objects):
    if obj.name.startswith('CARRIER '):
        bpy.data.objects.remove(obj, do_unlink=True)

poses = []
for index in range(121):
    t = index/120
    adapt = phase(t, .38, 1)
    expand = phase(t, .10, .72)
    width = .10+.90*expand
    world = {'root': rest['root'].copy()}
    pelvis = joint(index, 'pelvis', rig.data.bones['pelvis'].head_local, adapt)
    top = joint(index, 'thorax', (0, 0, 3.53), adapt)
    spine = top-pelvis
    tilt = Vector((0,0,1)).rotation_difference(spine)
    base = pelvis+spine*(.27/1.37)
    world['pelvis'] = scaled_frame('pelvis', pelvis, q((1,0,0), -75*(1-expand)) @ rest['pelvis'].to_quaternion(),
                                   (width, width, width))
    world['thorax'] = scaled_frame('thorax', base, tilt @ rest['thorax'].to_quaternion(),
                                  (width, spine.length/1.37, width))
    head = joint(index, 'neck', rig.data.bones['head'].head_local, adapt)
    head_scale = .025+.975*phase(t,.55,.88)
    world['head'] = scaled_frame('head', head, q((1,0,0), -95*(1-phase(t,.55,.9))) @ rest['head'].to_quaternion(),
                                 (head_scale,)*3)
    for side, sign in [('L',1), ('R',-1)]:
        names = {'shoulder':'upperarm_', 'elbow':'forearm_', 'wrist':'hand_',
                 'hip':'thigh_', 'knee':'shin_', 'ankle':'foot_'}
        targets = {'shoulder':'upperarm.', 'elbow':'forearm.', 'wrist':'hand.',
                   'hip':'thigh.', 'knee':'shin.', 'ankle':'foot.'}
        points = {key:joint(index, label+side, rig.data.bones[targets[key]+side].head_local,adapt)
                  for key,label in names.items()}
        for name,a,b in [('upperarm','shoulder','elbow'),('forearm','elbow','wrist'),
                         ('thigh','hip','knee'),('shin','knee','ankle')]:
            world[name+'.'+side] = segment(name+'.'+side, points[a], points[b], width)
        clavicle_root = base+tilt@Vector((sign*.27, .01, 1.11))*min(1,spine.length/1.37)
        # Match the exact approved root in the end pose.
        if index==120:
            clavicle_root=rig.data.bones['clavicle.'+side].head_local.copy()
        world['clavicle.'+side] = segment('clavicle.'+side, clavicle_root, points['shoulder'], width)
        hand_scale = .035+.965*phase(t,.27,.76)
        world['hand.'+side] = scaled_frame('hand.'+side, points['wrist'],
            q((1,0,0),-85*(1-expand)) @ rest['hand.'+side].to_quaternion(),(hand_scale,)*3)
        foot_scale = .15+.85*phase(t,.18,.66)
        world['foot.'+side] = scaled_frame('foot.'+side, points['ankle'],rest['foot.'+side].to_quaternion(),(foot_scale,)*3)
        for digit in ('digit0','digit1','digit2','digit3','thumb'):
            for link in range(3):
                name='%s%d.%s'%(digit,link,side)
                parent=rig.data.bones[name].parent.name
                world[name]=world[parent] @ rest[parent].inverted() @ rest[name]

    for role, item in mapping.items():
        name=item['bone']
        if role.startswith('neon.'):
            tube=bpy.data.objects[item['objects'][0]]
            old_current=source_paths[index][tube.name] @ tube.matrix_local.inverted()
            old_end=source_paths[120][tube.name] @ tube.matrix_local.inverted()
        else:
            pivot=Matrix.Translation(Vector(item['source_local_pivot']))
            old_current=source_paths[index]['MODULE - '+item['source_module']] @ pivot
            old_end=source_paths[120]['MODULE - '+item['source_module']] @ pivot
        u=phase(t,.54,1)
        target=retarget(old_current,old_end,destinations[role],u)
        world[name]=target @ Matrix(item['robot_world']).inverted() @ rest[name]

    # A single persistent mannequin, constant body and limb scale. The cabin
    # pickup precedes the head deployment; the end pose is the approved nape ride.
    old_actor=source_paths[index]['RIDER - one persistent reference actor'].translation
    old_end=source_paths[120]['RIDER - one persistent reference actor'].translation
    rider_end=rest['rider.anchor'].translation
    rider_position=old_actor+(rider_end-old_end)*phase(t,.48,1)
    rider_position.z-=.06*(1-phase(t,.1,.35))
    world['rider.anchor']=matrix(rider_position) @ rest['rider.anchor'].to_quaternion().to_matrix().to_4x4()
    ride=phase(t,.35,.88)
    tuck=math.sin(math.pi*phase(t,.08,.7))
    for side,sign in [('L',1),('R',-1)]:
        hip=Vector((sign*.13,0,.015))
        shoulder=Vector((sign*.19,0,.47))
        ankle=Vector((sign*.14,.60,-.29)).lerp(Vector((sign*.46,.60,-.38)),ride)
        ankle+=Vector((0,.05*tuck,.36*tuck))
        wrist=Vector((sign*.18,.40,.35)).lerp(Vector((sign*.29,.19,.34)),ride)
        knee=ik(hip,ankle,.49,.43,(0,0,1))
        elbow=ik(shoulder,wrist,.30,.28,(sign*ride,0,-.4))
        for name,a,b in [('thigh',hip,knee),('calf',knee,ankle),
                         ('upperarm',shoulder,elbow),('forearm',elbow,wrist)]:
            world['rider.'+name+'.'+side]=segment('rider.'+name+'.'+side,rider_position+a,rider_position+b)
    if index==120:
        # Eliminate floating point differences and preserve the approved pose.
        world.update({name:m.copy() for name,m in neutral.items()})
    poses.append(world)

# Bake local channels with parent-scale cancellation for the rigid exterior.
# Full matrix decomposition is shear-free because each desired bone frame has
# orthogonal axes; inheritance is disabled for cinematic anisotropic internals.
for bone in rig.data.bones:
    bone.use_inherit_scale=False

levels={}
for bone in rig.data.bones:
    depth=0;parent=bone.parent
    while parent:
        depth+=1;parent=parent.parent
    levels.setdefault(depth,[]).append(bone.name)

def apply(world):
    # Evaluate parents before children; siblings can be assigned together.
    for depth in sorted(levels):
        for name in levels[depth]:
            rig.pose.bones[name].matrix=world[name]
        bpy.context.view_layer.update()

for frame in range(1,266):
    index=min(120,frame-1) if frame<=145 else 265-frame
    world=poses[index]
    apply(world)
    for p in rig.pose.bones:
        p.keyframe_insert('location',frame=frame,group=p.name)
        p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name)
        p.keyframe_insert('scale',frame=frame,group=p.name)
    if frame%30==1:print('BAKE',frame,flush=True)
action=rig.animation_data.action
action.name='V18 - cinematic car robot car - internal compaction permitted'
for curve in action.fcurves:
    for key in curve.keyframe_points:key.interpolation='LINEAR'

# Always-visible connected supports. These are stylized telescopes: authored
# sleeves may compress, but their endpoints remain on body and exterior panel.
metal=bpy.data.materials['V9 - brushed titanium']

def anchor(label,bone_name,point):
    obj=bpy.data.objects.new(label,None);controls.objects.link(obj)
    b=rig.data.bones[bone_name]
    obj.parent=rig;obj.parent_type='BONE';obj.parent_bone=bone_name
    obj.matrix_parent_inverse=(b.matrix_local@Matrix.Translation((0,b.length,0))).inverted()
    obj.matrix_basis=Matrix.Translation(point)
    return obj

connections=[]
scene.frame_set(121)
for role,item in mapping.items():
    target_bone=item['bone']
    parent=rig.data.bones[target_bone].parent.name
    b=rig.data.bones[parent]
    start=b.head_local.lerp(b.tail_local,.45)
    end=rig.data.bones[target_bone].head_local.copy()
    a=anchor('V18 carrier root '+role,parent,start)
    z=anchor('V18 carrier end '+role,target_bone,end)
    track=bpy.data.objects.new('V18 carrier axis '+role,None);controls.objects.link(track)
    track.parent=a
    # Ignore parent's scale on the new carrier: all extension is explicit.
    follow=track.constraints.new('COPY_LOCATION');follow.target=a
    track.parent=None
    aim=track.constraints.new('DAMPED_TRACK');aim.target=z;aim.track_axis='TRACK_Z'
    for sleeve in range(2):
        bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=.017 if sleeve==0 else .012,depth=1)
        obj=bpy.context.object
        for col in list(obj.users_collection):col.objects.unlink(obj)
        hardware.objects.link(obj);obj.name='V18 connected sleeve %d - %s'%(sleeve,role)
        for vertex in obj.data.vertices:vertex.co.z+=.5
        obj.data.materials.append(metal);obj.parent=track;obj.location=(0,0,0)
        obj['cinematic_compressible']=True
        for property,index,expr in [('scale',2,'max(.00001,span*.60)'),('location',2,'span*%.2f'%(sleeve*.40))]:
            driver=obj.driver_add(property,index).driver
            var=driver.variables.new();var.name='span';var.type='LOC_DIFF'
            var.targets[0].id=a;var.targets[1].id=z
            driver.expression=expr
    connections.append(dict(role=role,start=a.name,end=z.name,axis=track.name))

# Hide only additional inner robot machinery at the fully closed end. The
# small emerging geometry is behind the opening shell at this changeover.
concealed=[]
for obj in hardware.objects:
    if obj.type!='MESH' or obj.get('continuous_unsegmented') or obj.name.startswith('V18 connected'):continue
    binding=audit['mesh_bones'].get(obj.name,'')
    if binding.startswith(('panel.','neon.')):continue
    concealed.append(obj.name)
    for frame,hidden in [(1,True),(12,True),(13,False),(253,False),(254,True),(265,True)]:
        obj.hide_render=hidden;obj.hide_viewport=hidden
        obj.keyframe_insert('hide_render',frame=frame)
        obj.keyframe_insert('hide_viewport',frame=frame)

scene.frame_start=1;scene.frame_end=265;scene.frame_step=1;scene.render.fps=24
for marker in list(scene.timeline_markers):scene.timeline_markers.remove(marker)
for frame,label in [(1,'350Z'),(13,'Release'),(43,'Unfold chassis'),(79,'Expand internal mechanism'),
                    (109,'Head and shoulder rider'),(121,'Approved robot'),(145,'Return'),(265,'350Z exact return')]:
    scene.timeline_markers.new(label,frame=frame)
rig['milestone']='Cinematic transformation prototype; concealed inner compression explicitly allowed'
rig['exterior_contract']='Rigid source parts, whole glazing, four wheels, seven neon tubes; no disappearance'
scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=16
scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.camera.location=(7,11,5)
scene.camera.rotation_euler=(Vector((0,0,2.35))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=7
scene.frame_set(121)
path=OUT/'350z-transformer-cinematic-base.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(path))
(OUT/'cinematic-build.json').write_text(json.dumps(dict(
    source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    blend_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
    original_parts=[o.name for o in originals.objects],
    rider_objects=[o.name for o in rider.objects],
    concealed_internal_objects=concealed,retired_static_supports=retired,
    connections=connections,physically_packable=False,clearance_validated=False,
    scope='Cinematic compaction permitted by user; visible exterior must stay rigid'),indent=2))
print('V18 SAVED',path,flush=True)
