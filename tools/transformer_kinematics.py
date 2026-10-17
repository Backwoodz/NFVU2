"""Connected carrier rig for the Blender 350Z study (Blender Python).

All exterior modules descend from one chassis. Hatch/spoiler and wheel/fender
relationships are structural, not independently interpolated world poses.
The roof uses a folding hinge on a longitudinal carriage and a telescopic lift.
Door carriers use equal-length two-link arms. This is an authored mechanism,
not a recovered car mechanism or a rigid-body simulation.
"""
import math
import bpy
from mathutils import Matrix, Quaternion, Vector


def ease(t):
    t = max(0., min(1., t))
    return t*t*(3-2*t)


def phase(t, a, b):
    return ease((t-a)/(b-a))


def q(axis, angle):
    return Quaternion(axis, math.radians(angle))


def matrix(position, rotation=None):
    return Matrix.Translation(Vector(position)) @ (rotation or Quaternion()).to_matrix().to_4x4()


def rebuild(scene, nodes, specs, originals, mechanism, controls, box, materials):
    dark, metal, green, eye = materials
    # Remove the disconnected v1 skeleton and its animation controllers.
    for obj in list(mechanism.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for obj in list(controls.objects):
        if obj not in nodes.values():
            bpy.data.objects.remove(obj, do_unlink=True)
    for obj in nodes.values():
        obj.animation_data_clear()
        obj.location = specs[obj['role']]['source']
        obj.rotation_quaternion = Quaternion()
        obj['prototype'] = 'Attached carrier hierarchy; see clearance report for unresolved intersections'

    def control(name, parent=None, loc=(0, 0, 0)):
        obj = bpy.data.objects.new('RIG - '+name, None)
        controls.objects.link(obj)
        obj.empty_display_type = 'PLAIN_AXES'
        obj.empty_display_size = .10
        obj.rotation_mode = 'QUATERNION'
        obj.parent = parent
        obj.location = loc
        return obj

    core = control('Chassis pitch about rear support')
    car = control('Car coordinates', core, (0, 0, -.4))
    for obj in nodes.values():
        obj.parent = car

    def attach(module, parent, pivot):
        obj = nodes[module]
        obj.parent = parent
        obj.location = specs[module]['source']-Vector(pivot)
        obj.rotation_quaternion = Quaternion()

    roof_pivot = Vector((0, .60, .82))
    hatch_pivot = Vector((0, -.748856, 1.140004))
    roof = control('Roof lift and fold hinge', car, roof_pivot)
    hatch = control('Hatch hinge - spoiler stays bolted on', roof, hatch_pivot-roof_pivot)
    attach('roof', roof, roof_pivot)
    attach('hatch', hatch, hatch_pivot)
    attach('spoiler', hatch, hatch_pivot)
    hood_pivot = Vector((0, .700543, .812943))
    hood = control('Original hood hinge', car, hood_pivot)
    attach('chest', hood, hood_pivot)
    shoulder, legs, cabin, cargo, shin = {}, {}, {}, {}, {}
    door_hinges = {}
    for s, sign in [('L', 1), ('R', -1)]:
        p = Vector((sign*.60, 1.30, .40))
        shoulder[s] = control('Shoulder rail '+s, car, p)
        for name in ('shoulder_', 'front_wheel_', 'bumper_'):
            attach(name+s, shoulder[s], p)
        p = Vector((sign*.60, -.65, .30))
        legs[s] = control('Hip rail and knee '+s, car, p)
        for name in ('calf_', 'rear_wheel_', 'foot_'):
            attach(name+s, legs[s], p)
        shin[s] = control('Sill / thigh frame '+s, car)
        attach('shin_'+s, shin[s], (0, 0, 0))
        p = Vector((sign*.78, -.12, .25))
        cabin[s] = control('Cabin frame '+s, car, p)
        attach('cabin_'+s, cabin[s], p)
        p = Vector((sign*.38, -.65, .25))
        cargo[s] = control('Cargo frame '+s, car, p)
        attach('cargo_'+s, cargo[s], p)
        door_hinges[s] = Vector((sign*.82695, .704878, .37956))
        attach('window_'+s, nodes['arm_'+s], specs['arm_'+s]['source'])

    animated = [core, roof, hatch, hood, *shoulder.values(), *legs.values(),
                *cabin.values(), *cargo.values(), *shin.values(), nodes['arm_L'], nodes['arm_R'],
                nodes['window_L'], nodes['window_R']]
    # Folding order is deliberate: do not run all modules towards their endpoint.
    for f in range(1, 266):
        t = min(1., (f-1)/120.) if f <= 145 else (265-f)/120.
        stand = phase(t, .38, .88)
        angle = math.pi*.5*stand
        # Study 03: preserve the driving heading in the final robot pose.
        # The provisional one-piece chassis requires a half-turn during rise.
        # Rotate about the rear support, not around the car's centre in space.
        yaw = math.pi*phase(t, .38, .86)
        core.location = (-2.1*math.cos(angle)*math.sin(yaw),
            -2.1+2.1*math.cos(angle)*math.cos(yaw), max(
            .54, 1.29*math.sin(angle)+.19*math.cos(angle)+.35,
            2.16*math.sin(angle)+.40*math.cos(angle)))
        core.rotation_quaternion = Quaternion((0, 0, 1), yaw) @ Quaternion((1, 0, 0), angle)
        hatch.rotation_quaternion = q((1, 0, 0), -170*phase(t, .07, .25))
        # Raise and fold above the hood; travel ahead of the engine envelope,
        # lower outside it, then slide the folded stack onto the spine.
        lift = 1.15*phase(t, .18, .30)-2.17*phase(t, .62, .76)
        retreat = 2.0*phase(t, .48, .59)-2.0*phase(t, .78, .92)
        roof.location = roof_pivot+Vector((0, retreat, lift))
        roof.rotation_quaternion = q((1, 0, 0), -180*phase(t, .31, .47))
        hood.rotation_quaternion = Quaternion()
        for s, sign in [('L', 1), ('R', -1)]:
            lift_window = phase(t, .12, .16)
            fold_window = phase(t, .16, .23)
            wp = Vector((sign*.56, 0, 1.12))-specs['arm_'+s]['source']
            wr = q((0, 1, 0), -sign*180*fold_window)
            shift = Vector((-sign*.08, .06, .30))*lift_window
            slide_window = .25*phase(t, .23, .30)*(1-phase(t, .50, .56))
            nodes['window_'+s].location = wp-wr@wp+wr@shift+Vector((0, .2*fold_window+slide_window, 0))
            nodes['window_'+s].rotation_quaternion = wr
            shoulder[s].location = (sign*(.60+.32*phase(t, .17, .27)), 1.30, .40)
            legs[s].location = (sign*(.60+.18*phase(t, .11, .29)), -.65, .30)
            shin[s].location = (0, 0, 0)
            cabin[s].rotation_quaternion = Quaternion()
            cargo[s].rotation_quaternion = Quaternion()
            # Start at the real door hinge, then use the now-exposed arm carrier.
            source = specs['arm_'+s]['source']
            door_q = Quaternion().slerp(Quaternion((.949964, -.01084, sign*.033174, sign*.310405)).normalized(), phase(t, .08, .16))
            opened = door_hinges[s]+door_q @ (source-door_hinges[s])+Vector((sign*.28*phase(t, .005, .03), -.20*phase(t, .04, .07), 0))
            deploy = phase(t, .23, .43)
            nodes['arm_'+s].location = opened.lerp(Vector((sign*1.56, -.35, .36)), deploy)+Vector((0, .20*phase(t, .44, .50), 0))
            nodes['arm_'+s].rotation_quaternion = door_q.slerp(q((0, 1, 0), -sign*80), deploy)
        for obj in animated:
            obj.keyframe_insert('location', frame=f)
            obj.keyframe_insert('rotation_quaternion', frame=f)

    def point(name, parent, pos):
        return control(name, parent, pos)

    connections = []

    def bearing(name, parent, pos, radius=.075):
        obj = point(name, parent, pos)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=radius)
        ball = bpy.context.object
        for collection in list(ball.users_collection):
            collection.objects.unlink(ball)
        mechanism.objects.link(ball)
        ball.name = 'NEW - '+name+' swivel'
        ball.parent = obj
        ball.location = (0, 0, 0)
        ball.data.materials.append(metal)
        return obj

    def rod(name, a, b, radius=.05, mat=None):
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=1)
        obj = bpy.context.object
        for collection in list(obj.users_collection):
            collection.objects.unlink(obj)
        mechanism.objects.link(obj)
        obj.name = 'LINK - '+name
        obj.data.materials.append(mat or dark)
        obj.rotation_mode = 'QUATERNION'
        # Model the shaft from Z=0 to Z=1 and constrain BOTH endpoints live.
        # Changing a controller in Blender therefore cannot leave its rod behind.
        for vertex in obj.data.vertices:
            vertex.co.z += .5
        obj.parent = a
        obj.location = (0, 0, 0)
        track = obj.constraints.new('DAMPED_TRACK')
        track.target = b
        track.track_axis = 'TRACK_Z'
        driver = obj.driver_add('scale', 2).driver
        variable = driver.variables.new()
        variable.name = 'distance'
        variable.type = 'LOC_DIFF'
        variable.targets[0].id = a
        variable.targets[1].id = b
        driver.expression = 'max(0.002, distance)'
        connections.append((obj, a, b))
        return obj

    # A continuous spine and crossmembers stay inside the source car volume.
    spine_a = point('Rear spine', car, (0, -.65, .30))
    spine_b = point('Front spine', car, (0, 1.98, .30))
    rod('main spine', spine_a, spine_b, .11)
    for s, sign in [('L', 1), ('R', -1)]:
        root = bearing('shoulder anchor '+s, car, (sign*.30, 1.30, .4))
        end = bearing('shoulder slide '+s, shoulder[s], (0, 0, 0))
        rod('shoulder outrigger '+s, root, end, .075, metal)
        hip = bearing('hip anchor '+s, car, (sign*.25, -.65, .30))
        knee = bearing('knee '+s, legs[s], (0, 0, 0), .11)
        ankle = bearing('ankle '+s, legs[s], (0, -1.40, .10), .09)
        rod('hip crossmember '+s, hip, knee, .09)
        rod('lower leg '+s, knee, ankle, .095)
        sole = control('Sole folding hinge '+s, legs[s])
        for f in range(1, 266):
            t = min(1., (f-1)/120.) if f <= 145 else (265-f)/120.
            opening = phase(t, .24, .38)
            sole.location = Vector((0, -1.18, -.375)).lerp(Vector((0, -1.45, 0)), opening)
            sole.rotation_quaternion = q((1, 0, 0), 90*(1-opening))
            sole.keyframe_insert('location', frame=f)
            sole.keyframe_insert('rotation_quaternion', frame=f)
        box('sole '+s, (0, 0, 0), (.55, .12, .60), dark, sole)
        rod('ankle sole carrier '+s, ankle, sole, .045, metal)
        sill_start = point('sill rail fixed '+s, legs[s], (sign*.18, .65, .04))
        sill_end = point('sill carriage '+s, shin[s], (sign*.18, .65, .04))
        rod('sill guide '+s, sill_start, sill_end, .045, metal)
        # Constant-length links: the elbow solution drives the two real links.
        upper = bearing('arm root '+s, shoulder[s], (sign*.08, -.55, .05))
        lower = bearing('door bracket '+s, nodes['arm_'+s], (0, .40, -.05))
        elbow = bearing('elbow '+s, car, (0, 0, 0), .085)
        length = .68
        for f in range(1, 266):
            scene.frame_set(f)
            bpy.context.view_layer.update()
            inv = car.matrix_world.inverted()
            a, b = inv @ upper.matrix_world.translation, inv @ lower.matrix_world.translation
            delta = b-a
            if delta.length > length*2-1e-4:
                raise ValueError('Arm reach exceeded at frame {}: {}'.format(f, delta.length))
            t = min(1., (f-1)/120.) if f <= 145 else (265-f)/120.
            unfold = phase(t, .09, .43)
            bend = Vector((-sign*math.cos(math.pi*unfold), 0, math.sin(math.pi*unfold)))
            direction = delta.normalized()
            bend -= direction*bend.dot(direction)
            bend.normalize()
            elbow.location = (a+b)*.5+bend*math.sqrt(length*length-delta.length_squared*.25)
            elbow.keyframe_insert('location', frame=f)
        rod('upper arm '+s, upper, elbow, .07)
        rod('forearm '+s, elbow, lower, .06, metal)
        # Both sides of the window's swivel/slide carrier remain visibly held.
        for y in (-.30, .10):
            pivot = bearing('window swivel {} {}'.format(s, y), nodes['arm_'+s],
                            (sign*.56-specs['arm_'+s]['source'].x, y, .52), .035)
            panel = point('window clamp {} {}'.format(s, y), nodes['window_'+s],
                          (sign*.73-specs['window_'+s]['source'].x, y, .27))
            rod('window guide {} {}'.format(s, y), pivot, panel, .018, metal)
        # Hands are bolted to the door carrier, never on independent world paths.
        hand = control('Wrist fold '+s, nodes['arm_'+s])
        for f in range(1, 266):
            t = min(1., (f-1)/120.) if f <= 145 else (265-f)/120.
            opening = phase(t, .23, .43)
            hand.location = Vector((0, -.55, -.20)).lerp(Vector((0, -.79, -.02)), opening)
            hand.rotation_quaternion = q((1, 0, 0), 90*(1-opening))
            hand.keyframe_insert('location', frame=f)
            hand.keyframe_insert('rotation_quaternion', frame=f)
        box('palm '+s, (0, 0, 0), (.25, .25, .13), dark, hand)
        for finger in range(4):
            box('finger {} {}'.format(s, finger), ((finger-1.5)*.062, -.20, 0),
                (.048, .17, .075), metal, hand, .01)
        # Visible mounting axles for both interior fold groups.
        for label, hinge in [('cabin', cabin[s]), ('cargo', cargo[s])]:
            a = point(label+' axle A '+s, hinge, (-.12, 0, 0))
            b = point(label+' axle B '+s, hinge, (.12, 0, 0))
            rod(label+' hinge '+s, a, b, .065, metal)

    rail_start = point('roof rail front', car, (0, .60, .24))
    rail_end = point('roof rail rear', car, (0, 2.60, .24))
    rod('roof longitudinal guide', rail_start, rail_end, .08)
    slider = bearing('roof carriage', car, (0, .60, .24), .09)
    roof_a = point('roof hinge A', roof, (-.45, 0, 0))
    roof_b = point('roof hinge B', roof, (.45, 0, 0))
    rod('roof transverse hinge', roof_a, roof_b, .06, metal)
    hatch_a = point('hatch hinge A', hatch, (-.43, 0, 0))
    hatch_b = point('hatch hinge B', hatch, (.43, 0, 0))
    rod('hatch transverse hinge', hatch_a, hatch_b, .05, metal)
    roof_center = bearing('roof lift head', roof, (0, 0, 0))
    rod('roof lift piston', slider, roof_center, .06, metal)
    for f in range(1, 266):
        scene.frame_set(f)
        slider.location = (0, roof.location.y, .24)
        slider.keyframe_insert('location', frame=f)
        rail_end.location = slider.location
        rail_end.keyframe_insert('location', frame=f)

    neck = control('Head extraction track', car)
    for f in range(1, 266):
        t = min(1., (f-1)/120.) if f <= 145 else (265-f)/120.
        extend = phase(t, .83, .94)
        neck.location = (0, 1.05+1.19*extend, .33)
        neck.rotation_quaternion = q((1, 0, 0), 90).slerp(
            q((1, 0, 0), -90) @ q((0, 0, 1), 180), phase(t, .94, 1.))
        neck.keyframe_insert('location', frame=f)
        neck.keyframe_insert('rotation_quaternion', frame=f)
    neck_start = point('head rail root', car, (0, 1.05, .33))
    rod('head extraction rail', neck_start, neck, .055, metal)
    box('helmet', (0, 0, .18), (.40, .34, .45), dark, neck, .075)
    box('faceplate', (0, .18, .10), (.24, .08, .21), metal, neck, .035)
    for sign in (-1, 1):
        box('brow', (sign*.105, .18, .285), (.18, .09, .07), green, neck, .012)
        box('optic', (sign*.09, .215, .23), (.13, .025, .035), eye, neck, .006)
        fin = box('head fin', (sign*.19, -.015, .37), (.065, .19, .30), green, neck, .018)
        fin.rotation_euler.y = sign*.20

    max_link_gap = max_arm_length_error = 0.
    for f in range(1, 266):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        for obj, a, b in connections:
            av, bv = a.matrix_world.translation, b.matrix_world.translation
            distance = (bv-av).length
            if distance > .002:
                max_link_gap = max(max_link_gap, (obj.matrix_world.translation-av).length,
                    (obj.matrix_world @ Vector((0, 0, 1))-bv).length)
            if obj.name.startswith(('LINK - upper arm', 'LINK - forearm')):
                max_arm_length_error = max(max_arm_length_error, abs(distance-.68))
    assert max_link_gap < 1e-4, max_link_gap
    assert max_arm_length_error < 1e-4, max_arm_length_error

    # Explicit hierarchy and reach invariants. These guard the user's primary
    # complaint: panels cannot become unparented flying islands again.
    assert nodes['spoiler'].parent == nodes['hatch'].parent == hatch
    for s in ('L', 'R'):
        assert nodes['front_wheel_'+s].parent == nodes['shoulder_'+s].parent
        assert nodes['rear_wheel_'+s].parent == nodes['calf_'+s].parent
    for obj in originals.objects:
        parent = obj
        while parent.parent:
            parent = parent.parent
        assert parent == core, obj.name
    scene.frame_set(1)
    bpy.context.view_layer.update()
    driving_forward = (car.matrix_world.to_quaternion() @ Vector((0, 1, 0))).normalized()
    scene.frame_set(121)
    bpy.context.view_layer.update()
    face_forward = (neck.matrix_world.to_quaternion() @ Vector((0, 1, 0))).normalized()
    hood_outward = (nodes['chest'].matrix_world.to_quaternion() @ Vector((0, 0, 1))).normalized()
    assert face_forward.dot(driving_forward) > .9999, tuple(face_forward)
    assert hood_outward.dot(driving_forward) > .9999, tuple(hood_outward)
    return dict(rig_version=3, connected_original_objects=len(originals.objects),
                driving_forward=list(driving_forward), robot_face_forward=list(face_forward),
                robot_hood_outward=list(hood_outward), rise_yaw_degrees=180,
                physical_links=len(connections), fixed_arm_link_length=.68,
                max_link_endpoint_gap_m=max_link_gap,
                max_arm_link_length_error_m=max_arm_length_error,
                roof_hatch_spoiler_hierarchy=True, wheels_share_fender_carriers=True)
