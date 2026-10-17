"""Visible seat-transfer study; the mannequin is NOT a copied Valheim Player.

Runtime integration contract (not implemented here): retain the live Player
already owned by VehicleCabinDriver; drive its existing pelvis/pose from one
moving seat anchor. Preserve inventory, equipment, health and network identity.
Return the visual scale/animator settings on exit as VehicleCabinDriver.Dispose
already does. Entry, death, despawn, emergency exit and camera require game tests.
Coordinates below are Blender X lateral / Y forward / Z up, in metres.
"""
import math
import bpy
from mathutils import Vector
from transformer_kinematics import phase


def build_rider(scene, bones, poses, controls, mechanism, box, rod, materials):
    dark, metal, green, eye = materials
    collection=bpy.data.collections.new('05 Rider - scale reference, NOT game avatar')
    scene.collection.children.link(collection)

    def material(name,color):
        m=bpy.data.materials.new(name); m.use_nodes=True; m.diffuse_color=(*color,1)
        shader=m.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value=(*color,1)
        shader.inputs['Roughness'].default_value=.7
        return m
    cloth=material('REFERENCE - ochre shirt',(.35,.19,.055))
    trousers=material('REFERENCE - slate trousers',(.06,.09,.12))
    skin=material('REFERENCE - mannequin clay',(.50,.31,.20))

    def reference(obj):
        for c in list(obj.users_collection): c.objects.unlink(obj)
        collection.objects.link(obj)
        obj['provenance']='Generic scale mannequin; NOT the player model'
        return obj
    def control(name,parent):
        obj=bpy.data.objects.new(name,None); controls.objects.link(obj)
        obj.parent=parent; obj.empty_display_size=.04
        return obj
    anchor=control('RIDER - persistent seat anchor',bones['thorax'])
    anchor['runtime_contract']='Same live Player; no clone, destruction, inventory or ownership change'
    anchor['car_seat_source']='VehicleInterior.Seat (-0.354, 0.35, -0.25) Unity; preview lift +0.14'
    actor=control('RIDER - one persistent reference actor',anchor)
    actor['identity']='one actor throughout forward, hold and reverse'
    actor_id=actor.as_pointer()
    endpoints={}
    for frame in range(1,266):
        t=min(1.,(frame-1)/120.) if frame<=145 else (265-frame)/120.
        lift=phase(t,.20,.66)
        anchor.location=Vector((-.354,-1.20,.15)).lerp(Vector((0,-.58,.83)),lift)
        anchor.keyframe_insert('location',frame=frame)
        scale=.85+.15*phase(t,.28,.72)
        actor.scale=(scale,scale,scale); actor.keyframe_insert('scale',frame=frame)
        if frame in (1,121,265):
            endpoints[frame]=poses[frame]['thorax'] @ anchor.location
    rail=control('RIDER - lift rail root',bones['thorax'])
    for frame in range(1,266):
        t=min(1.,(frame-1)/120.) if frame<=145 else (265-frame)/120.
        rail.location=Vector((-.354,-1.65,-.14)).lerp(Vector((0,-.45,-.10)),phase(t,.20,.66))
        rail.keyframe_insert('location',frame=frame)
    saddle_rear=control('RIDER - rear underside mount',anchor);saddle_rear.location=(0,-.20,-.12)
    rod('rider rail attachment',bones['pelvis'],rail,.035,metal)
    rod('rider telescopic support',rail,saddle_rear,.055,metal)
    cushion=box('rider seat cushion',(0,-.035,-.145),(.40,.30,.12),dark,anchor,.04)
    cushion.rotation_euler.x=-.20
    box('rider seat back',(0,-.20,.12),(.40,.08,.29),dark,anchor,.04)
    for sign in (-1,1):
        mount=control('RIDER - shoulder saddle bracket',bones['pelvis']); mount.location=(sign*.34,-.10,.08)
        rod('rider saddle brace',mount,saddle_rear,.035,dark)

    def elbow(a,b,l1,l2,bend):
        a,b=Vector(a),Vector(b); delta=b-a; length=delta.length
        assert abs(l1-l2)<length<l1+l2
        direction=delta/length; bend=Vector(bend)
        bend-=direction*bend.dot(direction); bend.normalize()
        along=(l1*l1-l2*l2+length*length)/(2*length)
        return a+direction*along+bend*math.sqrt(l1*l1-along*along)

    reference(box('REFERENCE pelvis',(0,0,.035),(.28,.20,.15),trousers,actor,.06))
    reference(box('REFERENCE torso',(0,-.025,.295),(.35,.22,.43),cloth,actor,.09))
    reference(box('REFERENCE neck',(0,-.01,.55),(.085,.085,.10),skin,actor,.025))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=.115)
    head=reference(bpy.context.object); head.name='REFERENCE human head'; head.parent=actor
    head.location=(0,-.01,.685); head.scale=(1,.91,1.10); head.data.materials.append(skin)
    for polygon in head.data.polygons: polygon.use_smooth=True
    reference(box('REFERENCE nose',(0,.092,.678),(.045,.052,.055),skin,actor,.02))

    for side,sign in [('L',1),('R',-1)]:
        points={name:control('RIDER '+side+' '+name,actor) for name in
                ('hip','knee','ankle','shoulder','elbow','wrist')}
        grip=control('RIDER - hand grip '+side,anchor)
        mount=control('RIDER - grip mount '+side,anchor); mount.location=(sign*.34,-.20,-.12)
        box('rider hand grip '+side,(0,0,0),(.06,.16,.045),metal,grip,.012)
        elbow_mount=control('RIDER - outboard grip bracket '+side,anchor)
        rod('rider grip support '+side,mount,elbow_mount,.018,metal)
        rod('rider grip crossbar '+side,elbow_mount,grip,.018,metal)
        for frame in range(1,266):
            t=min(1.,(frame-1)/120.) if frame<=145 else (265-frame)/120.
            u=phase(t,.30,.78)
            positions={
                'hip':(sign*.13,0,.015),
                'ankle':Vector((sign*.14,.60,-.30)).lerp(Vector((sign*.46,.68,-.46)),u),
                'shoulder':(sign*.19,0,.47),
                'wrist':Vector((sign*.18,.40,.35)).lerp(Vector((sign*.34,.34,.14)),u)}
            tuck=math.sin(math.pi*phase(t,0,.60))
            positions['ankle']+=Vector((0,.08*tuck,.55*tuck))
            positions['wrist']+=Vector((sign*.10*tuck,0,.32*tuck))
            positions['knee']=elbow(positions['hip'],positions['ankle'],.49,.43,(0,0,1))
            # Keep elbows tucked in while passing the door opening; spread only
            # after the seat has risen clear of the original cabin.
            positions['elbow']=elbow(positions['shoulder'],positions['wrist'],.30,.28,(sign*u,0,-1))
            grip.location=positions['wrist']*(.85+.15*phase(t,.28,.72))
            grip.keyframe_insert('location',frame=frame)
            elbow_mount.location=grip.location+Vector((sign*.12,0,-.065))
            elbow_mount.keyframe_insert('location',frame=frame)
            for name,obj in points.items():
                obj.location=positions[name]; obj.keyframe_insert('location',frame=frame)
        for name,a,b,radius,mat in [('thigh','hip','knee',.071,trousers),
                                   ('calf','knee','ankle',.055,trousers),
                                   ('upper arm','shoulder','elbow',.046,cloth),
                                   ('forearm','elbow','wrist',.035,skin)]:
            reference(rod('REFERENCE '+side+' '+name,points[a],points[b],radius,mat))
        reference(box('REFERENCE boot '+side,(0,.07,-.045),(.11,.25,.10),dark,points['ankle'],.035))
        reference(box('REFERENCE hand '+side,(0,0,0),(.075,.10,.045),skin,points['wrist'],.02))

    assert actor.as_pointer()==actor_id
    assert (endpoints[1]-endpoints[265]).length < 1e-6
    assert (endpoints[1]-Vector((-.354,-.25,.49))).length < 1e-6
    return dict(rider_reference_only=True, rider_runtime_integrated=False,
                rider_actor_count=1, rider_car_hips=list(endpoints[1]),
                rider_fixed_limb_lengths_m=dict(thigh=.49,calf=.43,upper_arm=.30,forearm=.28),
                rider_robot_hips=list(endpoints[121]), rider_return_error_m=(endpoints[1]-endpoints[265]).length,
                rider_ride_pose='behind robot head, legs astride shoulder bridge',
                rider_runtime_strategy='Retain live Player through existing VehicleCabinDriver; animate its seat anchor')
