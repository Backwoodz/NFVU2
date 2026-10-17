"""Study 08: hood closure clears the lamps before retracting onto the chest.

The car chassis no longer pitches upright as one rigid body. Four telescopic
limb chains unfold while separate panel carriers reorient the original shell.
This remains authored concept kinematics, not a contact/packing simulation.
"""
import math
import json
from pathlib import Path
import bpy
from mathutils import Matrix, Quaternion, Vector
from transformer_kinematics import phase, q, matrix


# Small symmetric adjustments from the end-pose surface-clearance review.
FINAL_ADJUSTMENTS = {'aerial': [0.12, 0.0, 0.0],
 'apron_back_L': [0.12, -0.04, -0.33],
 'apron_back_R': [-0.12, -0.04, -0.33],
 'apron_front_L': [0.0, -0.12, 0.12],
 'apron_front_R': [0.0, -0.12, 0.12],
 'floor0_L': [-0.12, 0.0, 0.0],
 'floor0_R': [0.12, 0.0, 0.0],
 'floor2_L': [0.0, -0.12, 0.0],
 'floor2_R': [0.0, -0.12, 0.0],
 'hipguard_L': [0.0, 0.12, 0.0],
 'hipguard_R': [0.0, 0.12, 0.0],
 'pelvis_rear': [0.0, 0.12, 0.0],
 'rear_arch_L': [-0.3, 0.0, 0.0],
 'rear_arch_R': [0.3, 0.0, 0.0],
 'rear_corner_L': [0.12, 0.0, 0.0],
 'rear_corner_R': [-0.12, 0.0, 0.0],
 'roof_L': [0.0, 0.18, -0.12],
 'roof_R': [0.0, 0.18, -0.12],
 'shinface_L': [0.12, 0.12, 0.06],
 'shinface_R': [-0.12, 0.12, 0.06],
 'sill1_L': [0.06, 0.0, 0.12],
 'sill1_R': [-0.06, 0.0, 0.12],
 'sill2_L': [0.12, 0.03, 0.12],
 'sill2_R': [-0.12, 0.03, 0.12],
 'spoiler': [0.0, 0.0, 0.24],
 'toe_L': [0.3, 0.0, 0.02],
 'toe_R': [-0.3, 0.0, 0.02],
 'windscreen_L': [0.0, 0.06, 0.12],
 'windscreen_R': [0.0, 0.06, 0.12]}

def rebuild(scene, nodes, specs, originals, mechanism, controls, box, materials):
    dark, metal, green, eye = materials
    path_file=Path(__file__).with_name('transformer_carrier_paths.json')
    path_corrections=json.loads(path_file.read_text()) if path_file.exists() else {}
    def control(name, parent=None):
        obj = bpy.data.objects.new('RIG - '+name, None)
        controls.objects.link(obj)
        obj.parent = parent
        obj.rotation_mode = 'QUATERNION'
        obj.empty_display_size = .08
        return obj

    root = control('Pelvis - no whole car rotation')
    bones = {'pelvis': root}
    parents = {'pelvis': None}
    for name, parent in [('thorax', 'pelvis'), ('spine', 'pelvis'), ('neck', 'thorax')]:
        bones[name] = control(name, bones[parent]); parents[name] = parent
    for s in ('L', 'R'):
        for name, parent in [('upperarm_'+s, 'thorax'), ('forearm_'+s, 'upperarm_'+s),
                             ('hand_'+s, 'forearm_'+s), ('thigh_'+s, 'pelvis'),
                             ('shin_'+s, 'thigh_'+s), ('foot_'+s, 'shin_'+s)]:
            bones[name] = control(name, bones[parent]); parents[name] = parent

    def ik(a, b, l1, l2, bend):
        delta = b-a; length = delta.length
        assert abs(l1-l2)+1e-4 < length < l1+l2-1e-4, length
        direction = delta/length
        along = (l1*l1-l2*l2+length*length)/(2*length)
        bend = Vector(bend); bend -= direction*bend.dot(direction); bend.normalize()
        return a+direction*along+bend*math.sqrt(max(0, l1*l1-along*along))

    poses = {}
    limb_lengths = {}
    for f in range(1, 266):
        t = min(1., (f-1)/120.) if f <= 145 else (265-f)/120.
        rise = phase(t, .24, .87)
        pelvis = Vector((0, -.70, .32)).lerp(Vector((0, 0, 2.45)), rise)
        thorax = pelvis+Vector((0, 1.65, .02)).lerp(Vector((0, -.04, 1.18)), rise)
        neck = thorax+Vector((0, .30, .015)).lerp(Vector((0, 0, .53)), phase(t, .72, .94))
        world = {'pelvis': matrix(pelvis), 'thorax': matrix(thorax),
                 'spine': matrix(pelvis, (thorax-pelvis).to_track_quat('Z', 'Y')),
                 'neck': matrix(neck, q((1, 0, 0), -90*(1-phase(t, .72, .94))))}
        for s, sign in [('L', 1), ('R', -1)]:
            hip = pelvis+Vector((sign*.32, 0, 0))
            ankle = Vector((sign*.42, -1.65, .34)).lerp(Vector((sign*.60, .02, .36)), phase(t, .12, .72))
            leg_fold = phase(t, .12, .75)
            extend = phase(t,.22,.80)
            femur,tibia = .52+.48*extend, .58+.54*extend
            # Retracted telescopic leg members fit beside the seat, with knees
            # folded upward into the rear cabin instead of crossing the centre.
            bend = Vector((0,0,1)).lerp(Vector((0,1,.02)),leg_fold)
            bend.z += math.sin(math.pi*leg_fold)
            knee = ik(hip, ankle, femur, tibia, bend)
            limb_lengths[f] = (femur,tibia)
            shoulder = thorax+Vector((sign*.50, 0, 0)).lerp(Vector((sign*1.02, 0, .02)), phase(t, .16, .63))
            wrist = Vector((sign*.50, 1.38, .32)).lerp(Vector((sign*1.31, .14, 2.28)), phase(t, .23, .86))
            arm_extend=phase(t,.16,.72)
            upper,lower=.30+.48*arm_extend,.32+.46*arm_extend
            bend = Vector((0, 0, 1)).lerp(Vector((sign, .35, 0)), phase(t, .14, .80))
            # A short shoulder slide keeps the packed and deployed reach valid.
            reach = (wrist-shoulder).length
            if reach > upper+lower-.035:
                wrist = shoulder+(wrist-shoulder).normalized()*(upper+lower-.035)
            elbow = ik(shoulder, wrist, upper, lower, bend)
            limb_lengths[f]=(femur,tibia,upper,lower)
            for name, start, end in [('thigh_', hip, knee), ('shin_', knee, ankle),
                                     ('upperarm_', shoulder, elbow), ('forearm_', elbow, wrist)]:
                world[name+s] = matrix(start, (end-start).to_track_quat('Z', 'Y'))
            world['foot_'+s] = matrix(ankle)
            world['hand_'+s] = matrix(wrist, q((1, 0, 0), 90))
        poses[f] = world
        for name, obj in bones.items():
            local = world[parents[name]].inverted() @ world[name] if parents[name] else world[name]
            obj.location = local.to_translation(); obj.rotation_quaternion = local.to_quaternion()
            obj.keyframe_insert('location', frame=f); obj.keyframe_insert('rotation_quaternion', frame=f)

    def placement(role):
        s = role[-1]; sign = 1 if s == 'L' else -1
        rx = q((1, 0, 0), -90)
        door = q((0, 0, 1), sign*42) @ rx
        source = specs[role]['source']
        if role.startswith('chest_'):
            return 'thorax', (sign*.42, .33, 3.60), q((0, 0, 1), -sign*22) @ rx, (.12, .66)
        if role.startswith('apron_'):
            return 'thorax', (sign*.39, -.20, 2.73 if 'front' in role else 3.91), rx, (.17, .61)
        if role.startswith('lamp_'):
            return 'thorax', (sign*.93, .53, 3.78), q((1, 0, 0), -65), (.12, .63)
        if role.startswith(('fender_', 'front_wheel_')):
            hub = Vector((sign*1.04, -.12, 3.77))
            p = hub+rx @ (source-Vector((sign*.8, 1.375, .21)))
            return 'thorax', p, rx, (.13, .69)
        if role.startswith('door_front_'):
            return 'forearm_'+s, (sign*1.45, .12, 2.66), door, (.08, .78)
        if role.startswith('door_rear_'):
            return 'upperarm_'+s, (sign*1.38, .50, 3.35), door, (.09, .75)
        if role.startswith('window_'):
            return 'upperarm_'+s, (sign*1.18, -.64, 3.30), q((0, 1, 0), -sign*16) @ door, (.04, .52)
        if role.startswith(('rear_arch_', 'rear_wheel_')):
            p = Vector((sign*.60, -.08, .94))+rx @ (source-Vector((sign*.8, -1.29, .21)))
            return 'shin_'+s, p, rx, (.16, .76)
        if role.startswith('rear_corner_'):
            return 'thigh_'+s, (sign*.57, -.08, 1.93), q((1, 0, 0), 90), (.16, .76)
        if role.startswith('taillamp_'):
            return 'shin_'+s, (sign*.60, .37, 1.44), q((0, 0, 1), 180), (.20, .76)
        if role.startswith('exhaust_'):
            return 'shin_'+s, (sign*.76, -.66, 1.02), q((1, 0, 0), 75), (.16, .77)
        if role.startswith('floor'):
            index = int(role[5])
            return 'thigh_'+s, (sign*.47, .10-index*.14, 1.88+index*.13), rx, (.17, .74)
        if role.startswith('sill'):
            index = int(role[4]); z = [2.15, 1.25, .80, 2.42][index]
            bone = ('thigh_' if index in (0, 3) else 'shin_')+s
            return bone, (sign*(.50 if index in (0, 3) else .59), .24, z), door, (.11, .72)
        if role.startswith('toe_'):
            return 'foot_'+s, (sign*.60, .39, .24), Quaternion(), (.20, .70)
        if role.startswith('shinface_'):
            return 'shin_'+s, (sign*.60, .34, .69), Quaternion(), (.22, .73)
        if role.startswith('hipguard_'):
            return 'pelvis', (sign*.43, .22, 2.48), rx, (.18, .66)
        if role == 'pelvis_rear':
            return 'pelvis', (0, .27, 2.49), q((0, 0, 1), 180), (.17, .71)
        if role.startswith('belt_'):
            return 'pelvis', (sign*.18, -.12, 2.51), rx, (.17, .70)
        if role.startswith('roof_'):
            return 'thorax', (sign*.50, -.42, 3.51), q((0, 0, 1), sign*75) @ rx, (.02, .55)
        if role == 'hatch':
            return 'thorax', (0, -1.04, 3.28), q((1, 0, 0), 72), (.01, .82)
        if role == 'windscreen':
            return 'thorax', (0, -.76, 3.80), q((1, 0, 0), 120), (.01, .82)
        if role.startswith('mirror_'):
            return 'forearm_'+s, (sign*1.45, .12, 2.66), door, (.08, .78)
        if role.startswith('quarterglass_'):
            return 'pelvis', (sign*.43, -.80, 2.36), q((1, 0, 0), 90), (.07, .61)
        if role == 'spoiler':
            return 'thorax', (0, -1.04, 3.28), q((1, 0, 0), 72), (.01, .82)
        if role == 'aerial':
            return 'thorax', (.73, -.28, 4.08), Quaternion(), (.10, .65)
        raise ValueError('Missing articulated assignment: '+role)

    connections = []
    def rod(name, a, b, radius, mat):
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=1)
        obj = bpy.context.object
        for col in list(obj.users_collection): col.objects.unlink(obj)
        mechanism.objects.link(obj); obj.name = 'LINK - '+name
        for vertex in obj.data.vertices: vertex.co.z += .5
        obj.parent = a; obj.location = (0, 0, 0)
        track = obj.constraints.new('DAMPED_TRACK'); track.target = b; track.track_axis = 'TRACK_Z'
        driver = obj.driver_add('scale', 2).driver
        var = driver.variables.new(); var.name = 'distance'; var.type = 'LOC_DIFF'
        var.targets[0].id = a; var.targets[1].id = b
        inherited=driver.variables.new(); inherited.name='parent_scale'; inherited.type='TRANSFORMS'
        inherited.targets[0].id=a
        inherited.targets[0].transform_type='SCALE_Z'
        inherited.targets[0].transform_space='WORLD_SPACE'
        driver.expression = 'max(0.002, distance)/max(0.000001, parent_scale)'
        for polygon in obj.data.polygons: polygon.use_smooth = True
        obj.data.materials.append(mat); connections.append((obj, a, b))
        return obj

    def bearing(parent, radius=.12):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=radius)
        obj = bpy.context.object
        for col in list(obj.users_collection): col.objects.unlink(obj)
        mechanism.objects.link(obj); obj.parent = parent; obj.location = (0, 0, 0)
        obj.name = 'JOINT - '+parent.name; obj.data.materials.append(dark if radius < .05 else metal)

    def path_pose(t, waypoints):
        for i in range(1,len(waypoints)):
            a,p,qa=waypoints[i-1]; b,r,qb=waypoints[i]
            if t <= b:
                u=phase(t,a,b)
                return Vector(p).lerp(Vector(r),u),qa.slerp(qb,u)
        return Vector(waypoints[-1][1]),waypoints[-1][2]

    for role, obj in nodes.items():
        bone, pos, rotation, timing = placement(role)
        pos = Vector(pos)+Vector(FINAL_ADJUSTMENTS.get(role, (0,0,0)))
        obj.parent = bones[bone]
        start = poses[1][bone].inverted() @ matrix(specs[role]['source']+Vector((0, 0, .14)))
        end = poses[121][bone].inverted() @ matrix(pos, rotation)
        obj['carrier'] = bone
        obj['prototype'] = 'Rigid segmented exterior on folding carrier; path clearance remains under review'
        # Mirrors stay on their door segments; spoiler stays bolted to the
        # complete hatch. No second animation can tear these assemblies apart.
        assembly = ('hatch' if role == 'spoiler' else
                    'door_front_'+role[-1] if role.startswith('mirror_') else
                    'front_wheel_'+role[-1] if role.startswith('fender_') else None)
        if assembly:
            obj.parent = nodes[assembly]
            obj.location = specs[role]['source']-specs[assembly]['source']
            obj.rotation_quaternion = Quaternion()
            if role.startswith('fender_'):
                sign=1 if role.endswith('_L') else -1
                inner=control('wheel inboard bracket '+role,nodes[assembly]);inner.location=(-sign*.24,0,0)
                crown=control('wheel crown bracket '+role,nodes[assembly]);crown.location=(-sign*.24,0,.48)
                for a,b in zip([nodes[assembly],inner,crown],[inner,crown,obj]):
                    rod('bolted fender bracket '+role,a,b,.018,dark)
            else:
                rod('bolted attachment '+role, nodes[assembly], obj, .022, dark)
            continue
        for f in range(1, 266):
            t = min(1., (f-1)/120.) if f <= 145 else (265-f)/120.
            progress = phase(t, *timing)
            # A lateral release provides room before the shell rolls into armor.
            release = math.sin(math.pi*progress)*.16
            sign = 1 if specs[role]['source'].x >= 0 else -1
            obj.location = start.to_translation().lerp(end.to_translation(), progress)+Vector((sign*release, 0, 0))
            obj.rotation_quaternion = start.to_quaternion().slerp(end.to_quaternion(), progress)
            if role in ('windscreen', 'hatch') or role.startswith('roof_'):
                # Release above/outside the passenger cell BEFORE folding;
                # then bring the complete pane down behind the torso.
                source_world = specs[role]['source']+Vector((0,0,.14))
                sign = 1 if source_world.x >= 0 else -1
                if role == 'windscreen':
                    release_pos = source_world+Vector((0,.25,.65))
                    release_q = Quaternion()
                    corridor = Vector((0,-1.05,1.95))
                elif role == 'hatch':
                    release_pos = source_world+Vector((0,-.35,.35))
                    release_q = q((1,0,0),25)
                    corridor = Vector((0,-1.32,1.75))
                else:
                    release_pos = source_world+Vector((sign*.35,0,.55))
                    release_q = q((0,1,0),sign*65)
                    corridor = Vector((sign*.95,-.70,1.85))
                if t < .12:
                    u=phase(t,.005,.12)
                    world_pos=source_world.lerp(release_pos,u)
                    world_q=Quaternion().slerp(release_q,u)
                elif t < .30:
                    u=phase(t,.12,.30)
                    world_pos=release_pos.lerp(corridor,u)
                    world_q=release_q.slerp(rotation,u)
                else:
                    u=phase(t,.30,.84)
                    world_pos=corridor.lerp(Vector(pos),u)
                    world_q=rotation
                local=poses[f][bone].inverted() @ matrix(world_pos,world_q)
                obj.location=local.to_translation(); obj.rotation_quaternion=local.to_quaternion()
            elif role.startswith(('window_', 'sill', 'floor', 'apron_', 'chest_',
                                  'shinface_', 'rear_arch_', 'rear_corner_')):
                # Keep the occupied cell clear: release side panels outwards,
                # lift the rider, assemble the inner chest, THEN close the hood.
                source_world=specs[role]['source']+Vector((0,0,.14))
                sign=1 if source_world.x>=0 else -1
                points=None
                if role.startswith('window_'):
                    points=[(0,source_world,Quaternion()),
                            (.12,source_world+Vector((sign*.38,0,.08)),Quaternion()),
                            (.32,(sign*1.48,-.70,1.45),rotation),(.82,pos,rotation)]
                elif role.startswith('sill'):
                    points=[(0,source_world,Quaternion()),
                            (.18,source_world+Vector((sign*.48,0,.08)),Quaternion()),
                            (.48,(sign*1.35,.24,1.05),rotation),(.84,pos,rotation)]
                elif role.startswith('floor'):
                    points=[(0,source_world,Quaternion()),(.29,source_world,Quaternion()),
                            (.56,(sign*.85,-.25,1.25),rotation),(.85,pos,rotation)]
                elif role.startswith('apron_'):
                    points=[(0,source_world,Quaternion()),
                            (.22,source_world+Vector((0,.30,.25)),Quaternion()),
                            (.48,(pos.x,1.05,pos.z-1.70),rotation),
                            (.74,(pos.x,1.05,pos.z-.35),rotation),(.88,pos,rotation)]
                elif role.startswith('chest_'):
                    # First close laterally in FRONT of the lamp assemblies.
                    # Retract only once both halves are inboard of the lamps;
                    # a diagonal final slide cuts through both headlight lenses.
                    points=[(0,source_world,Quaternion()),
                            (.20,source_world+Vector((sign*.55,.20,.22)),q((0,1,0),sign*30)),
                            (.48,(sign*1.15,1.05,2.30),rotation),
                            (.90,(sign*1.15,1.05,pos.z),rotation),
                            (.965,(pos.x,1.05,pos.z),rotation),(1.,pos,rotation)]
                elif role.startswith('shinface_'):
                    # Front bumper plates stay upright during transfer instead
                    # of inheriting the shin's temporary fold below the ankle.
                    points=[(0,source_world,Quaternion()),
                            (.30,(sign*1.05,.90,.85),Quaternion()),
                            (.55,(sign*1.05,.65,.95),rotation),(.82,pos,rotation)]
                elif role.startswith(('rear_arch_', 'rear_corner_')):
                    # Lift complete rear panels outside the folded leg before
                    # rolling them into calf/thigh armor.
                    points=[(0,source_world,Quaternion()),
                            (.25,source_world+Vector((sign*.38,0,.50)),Quaternion()),
                            (.50,(sign*1.20,-1.05,1.30),rotation),(.82,pos,rotation)]
                world_pos,world_q=path_pose(t,points)
                local=poses[f][bone].inverted() @ matrix(world_pos,world_q)
                obj.location=local.to_translation(); obj.rotation_quaternion=local.to_quaternion()
            if role.startswith('rear_wheel_'):
                # Clear the tire's full radius while its calf carrier unfolds.
                # The slide retracts at both endpoints, preserving the car.
                lift = .08*math.sin(math.pi*phase(t,.08,.76))
                obj.location += poses[f][bone].to_quaternion().inverted() @ Vector((0,0,lift))
            forward=min(f,121) if f<=145 else 266-f
            correction=path_corrections.get(str(forward),{}).get(role)
            if correction:
                obj.location+=poses[f][bone].to_quaternion().inverted() @ Vector(correction)
            if role.startswith('chest_'):
                # Release above the seated lamp seam, then use a forward
                # corridor while the lamps rise past the lower hood edge.
                # Dense subframe checks are essential here: the old paths
                # swapped sides between frames 63 and 64 without an integer-
                # frame intersection. Both offsets vanish in car/robot poses.
                lift=.025*phase(t,0,.025)*(1-phase(t,.025,.04))
                # Pass above the transverse front neon rail during closure;
                # settle vertically only after the hood has retracted past it.
                lift+=.20*phase(t,.86,.90)*(1-phase(t,.985,1.))
                forward_slide=.38*phase(t,.38,.48)*(1-phase(t,.60,.78))
                obj.location+=poses[f][bone].to_quaternion().inverted() @ Vector((0,forward_slide,lift))
            obj.keyframe_insert('location', frame=f); obj.keyframe_insert('rotation_quaternion', frame=f)
        attachment=obj
        if role.startswith('floor'):
            attachment=control('floor edge hinge '+role,obj)
            surface=[v.co for child in obj.children if child.type=='MESH' and child.get('source_part') for v in child.data.vertices]
            sign=1 if role.endswith('_L') else -1
            target=Vector((sign*.64-specs[role]['source'].x,0,0))
            attachment.location=min(surface,key=lambda p:(p-target).length_squared)
        if role=='windscreen' or role.startswith('roof_'):
            sides=[('L',1),('R',-1)] if role=='windscreen' else [(role[-1],1 if role.endswith('_L') else -1)]
            for side,sign in sides:
                outrigger=control('glazing outboard bracket '+role+side,bones['thorax'])
                outrigger.location=(sign*.90,-.35,.10)
                tip=attachment
                if role=='windscreen':
                    tip=control('whole windscreen edge hinge '+side,obj)
                    surface=[v.co for child in obj.children if child.type=='MESH' and child.get('source_part') for v in child.data.vertices]
                    target=Vector((sign*.7-specs[role]['source'].x,0,0))
                    tip.location=min(surface,key=lambda p:(p-target).length_squared)
                rod('glazing bracket root '+role+side,bones['thorax'],outrigger,.020,dark)
                rod('panel carrier '+role+side,outrigger,tip,.018,dark)
        elif role.startswith(('toe_', 'shinface_', 'floor')):
            # Route long front-to-rear transfers underneath the occupied cabin.
            # The two folding knuckles retract to the limb when deployed.
            joints=[]
            for index in (0,1):
                joint=control('underfloor transfer '+role+' '+str(index),bones[bone])
                for f in range(1,266):
                    t=min(1.,(f-1)/120.) if f<=145 else (265-f)/120.
                    start_point=poses[1][bone].translation.copy() if index==0 else specs[role]['source'].copy()
                    if index==1 and attachment!=obj:start_point+=attachment.location
                    start_point.z=.10
                    deployed=poses[f][bone] @ Vector((0,0,.10+.09*index))
                    world_point=start_point.lerp(deployed,phase(t,.20,.78))
                    joint.location=poses[f][bone].inverted() @ world_point
                    joint.keyframe_insert('location',frame=f)
                joints.append(joint)
            for index,(a,b) in enumerate(zip([bones[bone]]+joints,joints+[attachment])):
                rod('panel carrier '+role+' folded section '+str(index),a,b,.018,dark)
                bearing(b,.025)
        else:
            rod('panel carrier '+role, bones[bone], obj, .022, dark)
        bearing(attachment, .025)

    for s in ('L', 'R'):
        for proximal, distal, radius in [('upperarm_', 'forearm_', .13), ('forearm_', 'hand_', .11),
                                         ('thigh_', 'shin_', .17), ('shin_', 'foot_', .14)]:
            a, b = bones[proximal+s], bones[distal+s]
            rod(proximal+s, a, b, radius, dark); bearing(a, radius*1.15)
            # Paired metal guide rods give the limb actual mechanical depth.
            for offset in (-.09, .09):
                pa = control('piston root', a); pa.location.x = offset
                pb = control('piston end', b); pb.location.x = offset
                rod(proximal+s+' piston', pa, pb, .028, metal)
        bearing(bones['foot_'+s], .12)
        sign = 1 if s == 'L' else -1
        sole = control('Folded sole '+s, bones['foot_'+s])
        for f in range(1,266):
            t = min(1.,(f-1)/120.) if f <= 145 else (265-f)/120.
            u=phase(t,.16,.72)
            sole.location = Vector((0, .50, .25)).lerp(Vector((sign*.18, .16, -.25)),u)
            sole.rotation_quaternion=q((0,1,0),sign*90*(1-u))
            sole.keyframe_insert('location',frame=f)
            sole.keyframe_insert('rotation_quaternion',frame=f)
        box('sole '+s, (0, 0, 0), (.66, .92, .17), dark, sole, .045)
        rod('sole ankle bracket '+s, bones['foot_'+s], sole, .06, metal)
        hand = bones['hand_'+s]
        box('palm '+s, (0, 0, .02), (.26, .13, .26), dark, hand, .035)
        for finger in range(4):
            for segment in range(2):
                box('finger', ((finger-1.5)*.064, 0, .20+segment*.105),
                    (.049, .075, .095), metal, hand, .015)
        box('thumb '+s, (.17, .01, .12), (.09, .09, .17), dark, hand, .025)
    rod('telescopic spine', root, bones['thorax'], .15, dark)
    rod('neck', bones['thorax'], bones['neck'], .10, metal)
    for s in ('L', 'R'):
        rod('clavicle '+s, bones['thorax'], bones['upperarm_'+s], .11, dark)
        rod('hip crossmember '+s, root, bones['thigh_'+s], .12, dark)
    box('pelvis core', (0, 0, 0), (.60, .38, .30), dark, root, .06)
    for row in range(4):
        for sign in (-1, 1):
            rib = box('abdominal rib', (sign*(.15+row*.025), .04, .27+row*.18),
                      (.29+row*.025, .31, .11), dark if row%2 else metal, bones['spine'], .028)
            rib.rotation_euler.y = sign*-.20
            for f in range(1,266):
                t=min(1.,(f-1)/120.) if f<=145 else (265-f)/120.
                u=phase(t,.30,.85)
                rib.location.x=sign*(.10*(1-u)+(.15+row*.025)*u)
                rib.rotation_euler.y=sign*((math.pi/2)*(1-u)-.20*u)
                rib.keyframe_insert('location',frame=f);rib.keyframe_insert('rotation_euler',frame=f)

    # Faceted helmet and cheek armor, rather than a square placeholder head.
    head = bones['neck']
    vertices = [(-.21,-.16,.07),(.21,-.16,.07),(.18,.17,.07),(-.18,.17,.07),
                (-.23,-.13,.44),(.23,-.13,.44),(.16,.12,.52),(-.16,.12,.52)]
    mesh = bpy.data.meshes.new('Faceted helmet')
    mesh.from_pydata(vertices, [], [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
    mesh.update(); helmet = bpy.data.objects.new('NEW - Faceted helmet', mesh)
    mechanism.objects.link(helmet); helmet.parent = head; mesh.materials.append(dark)
    box('jaw', (0,.16,.15), (.20,.13,.16), metal, head, .05)
    box('nose guard', (0,.225,.28), (.055,.10,.19), metal, head, .014)
    for sign in (-1,1):
        brow = box('brow', (sign*.12,.18,.36), (.20,.12,.07), green, head, .018)
        brow.rotation_euler.y = sign*.17
        optic = box('optic', (sign*.10,.232,.315), (.13,.025,.032), eye, head, .008)
        optic.rotation_euler.y = sign*.14
        fin = box('temple fin', (sign*.22,-.03,.43), (.07,.21,.19), green, head, .018)
        fin.rotation_euler.y = sign*.30

    from transformer_rider import build_rider
    rider_report = build_rider(scene, bones, poses, controls, mechanism, box, rod, materials)

    from transformer_mechanism_detail import add_details
    add_details(bones, mechanism, controls, box, materials)

    from transformer_neon import build_neon
    neon_report=build_neon(scene,nodes,specs,bones,poses,controls,mechanism,box,rod,materials)

    max_gap = max_length_error = 0.
    connection_ranges={obj.name:[float('inf'),0.] for obj,a,b in connections}
    connection_poses={obj.name:[] for obj,a,b in connections}
    for f in range(1,266):
        scene.frame_set(f); bpy.context.view_layer.update()
        for obj, a, b in connections:
            connection_poses[obj.name].append((a.matrix_world.copy(),b.matrix_world.translation.copy()))
            span=(a.matrix_world.translation-b.matrix_world.translation).length
            connection_ranges[obj.name][0]=min(connection_ranges[obj.name][0],span)
            connection_ranges[obj.name][1]=max(connection_ranges[obj.name][1],span)
            if (a.matrix_world.translation-b.matrix_world.translation).length > .002:
                max_gap = max(max_gap, (obj.matrix_world @ Vector((0,0,1))-b.matrix_world.translation).length)
        for s in ('L','R'):
            for a,b,length in [('thigh_','shin_',limb_lengths[f][0]),('shin_','foot_',limb_lengths[f][1]),
                               ('upperarm_','forearm_',limb_lengths[f][2]),('forearm_','hand_',limb_lengths[f][3])]:
                error = abs((bones[a+s].matrix_world.translation-bones[b+s].matrix_world.translation).length-length)
                max_length_error = max(max_length_error,error)
    assert max_gap < 1e-4, max_gap
    assert max_length_error < 1e-4, max_length_error
    from transformer_carriers import build_telescopes
    carriage_report=build_telescopes(connections,connection_ranges,connection_poses,mechanism,controls,materials)
    for f in range(1,266):
        scene.frame_set(f);bpy.context.view_layer.update()
        for obj,a,b in connections:
            if (a.matrix_world.translation-b.matrix_world.translation).length>.002:
                max_gap=max(max_gap,(obj.matrix_world @ Vector((0,0,1))-b.matrix_world.translation).length)
    assert max_gap<1e-4,max_gap
    scene.frame_set(121); bpy.context.view_layer.update()
    face = bones['neck'].matrix_world.to_quaternion() @ Vector((0,1,0))
    assert face.y > .9999
    for side in ('L','R'):
        assert nodes['fender_front_'+side].parent == nodes['front_wheel_'+side]
        assert nodes['fender_back_'+side].parent == nodes['front_wheel_'+side]
        assert nodes['mirror_'+side].parent == nodes['door_front_'+side]
    assert nodes['spoiler'].parent == nodes['hatch']
    for obj in originals.objects:
        ancestor = obj
        while ancestor.parent: ancestor = ancestor.parent
        assert ancestor == root
    result = dict(rig_version=8, connected_original_objects=len(originals.objects),
                physical_links=len(connections), max_link_endpoint_gap_m=max_gap,
                max_fixed_limb_length_error_m=max_length_error,
                robot_face_forward=list(face), whole_chassis_yaw_degrees=0,
                whole_chassis_pitch_degrees=0, original_interior_replaced=True,
                intact_windscreen=True, intact_rear_window=True,
                front_fenders_follow_wheel_carriers=True, mirrors_and_spoiler_bolted=True,
                telescopic_limb_lengths_m=dict(thigh=[.52,1.0],shin=[.58,1.12],upperarm=[.30,.78],forearm=[.32,.78]))
    result['carrier_clearance_samples']=len(path_corrections)
    result.update(rider_report)
    result.update(neon_report)
    result.update(carriage_report)
    return result
