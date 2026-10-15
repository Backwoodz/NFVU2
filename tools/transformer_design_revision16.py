"""Folded spoiler attachment, dorsal neon and direct shoulder-riding pose."""
import math
import bpy
from mathutils import Vector,Matrix


def revise_back_and_rider(c):
    hardware,modules,groups,root=[c[k] for k in ('hardware','modules','groups','root')]
    rod,ring,box,plate,quat,pose=[c[k] for k in ('rod','ring','box','plate','quat','pose')]
    dark,steel,paint,rubber=[c[k] for k in ('dark','steel','paint','rubber')]
    prefixes=('shoulder saddle cushion','saddle back','saddle side frame','saddle cross brace',
        'rider handle outrigger','rider handle support','rider grip crossbar','rider hand grip',
        'V13 spoiler folding support','V13 rear neon bracket')
    removed=[]
    for o in list(hardware.objects):
        if any(o.name.startswith('V9 - '+p) for p in prefixes):
            removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True)

    # The whole original spoiler folds flat; both original pedestals land on
    # explicit hinge cradles attached around the upper edge of the back pack.
    pose('spoiler',(0,-1.33,3.77),quat((1,0,0),75))
    bpy.context.view_layer.update()
    mount=groups['spoiler'][0]
    points=[v.co for v in mount.data.vertices]
    bottom=min(v.z for v in points)
    hinge_points=[]
    for sign in (-1,1):
        feet=[p for p in points if p.x*sign>0 and p.z<bottom+.018]
        assert feet
        foot=modules['spoiler'].matrix_world @ (sum(feet,Vector())/len(feet))
        hinge_points.append(list(foot))
        # Thick fork, transverse pin and a bolted root are visible from behind.
        box('V16 spoiler pedestal cradle',foot,(.14,.09,.10),dark,bevel=.012)
        rod('V16 spoiler hinge pin',foot-Vector((.10,0,0)),foot+Vector((.10,0,0)),.025,steel)
        for dx in (-.085,.085):ring('V16 spoiler hinge bearing',foot+Vector((dx,0,0)),.037,.010,(1,0,0),steel)
        shoulder=Vector((sign*.64,-.28,3.76));corner=Vector((sign*.81,-1.03,4.08))
        rod('V16 spoiler frame upright',shoulder,corner,.045,dark)
        rod('V16 spoiler folding arm',corner,foot+Vector((sign*.105,0,0)),.045,steel)
        box('V16 spoiler frame root',shoulder,(.14,.16,.12),dark,bevel=.012)
        rod('V16 spoiler actuator',corner+Vector((0,-.02,.02)),foot+Vector((0,.04,-.04)),.027,dark)

    # Reuse the complete tube/channel as a vertical dorsal light, above the
    # exhaust. No deletion, duplication, segmentation or rescaling of neon.
    neon=bpy.data.objects['DESIGN - protected neon underbody rear']
    neon.location=(0,-1.16,3.29);neon.rotation_quaternion=quat((0,0,1),180)
    light=bpy.data.objects['V9 neon spill underbody rear'];light.location=(0,-1.21,3.29)
    for z in (2.91,3.66):
        box('V16 dorsal neon mount',(0,-1.105,z),(.15,.045,.075),dark,bevel=.008)
        rod('V16 dorsal neon standoff',(0,-1.105,z),(0,-1.135,z),.024,steel)

    # Same mannequin and fixed limb lengths; only the articulated pose changes.
    seat=bpy.data.objects['DESIGN - retained rider and shoulder saddle']
    seat.location=(0,-.30,4.08)
    seat['ride_pose']='Directly astride the neck; no chair, backrest or handrails'
    seat['runtime_contract']='Retain the existing live Player, inventory and equipment; animate one pelvis anchor with IK'

    def elbow(a,b,l1,l2,bend):
        a,b=Vector(a),Vector(b);delta=b-a;length=delta.length
        assert abs(l1-l2)<length<l1+l2
        direction=delta/length;bend=Vector(bend);bend-=direction*bend.dot(direction);bend.normalize()
        along=(l1*l1-l2*l2+length*length)/(2*length)
        return a+direction*along+bend*math.sqrt(l1*l1-along*along)

    landmarks={}
    for side,sign in [('L',1),('R',-1)]:
        hip=Vector((sign*.13,0,.015));ankle=Vector((sign*.46,.60,-.38))
        knee=elbow(hip,ankle,.49,.43,(0,0,1))
        shoulder=Vector((sign*.19,0,.47));wrist=Vector((sign*.29,.19,.34))
        arm_elbow=elbow(shoulder,wrist,.30,.28,(sign,0,-.4))
        for label,a,b,length in [('thigh',hip,knee,.49),('calf',knee,ankle,.43),
                ('upper arm',shoulder,arm_elbow,.30),('forearm',arm_elbow,wrist,.28)]:
            o=bpy.data.objects['LINK - REFERENCE '+side+' '+label]
            delta=b-a;assert abs(delta.length-length)<1e-6
            o.location=a;o.rotation_mode='QUATERNION';o.rotation_quaternion=delta.to_track_quat('Z','Y');o.scale=(1,1,length)
        boot=bpy.data.objects['NEW - REFERENCE boot '+side];boot.location=ankle+Vector((0,.07,-.045))
        hand=bpy.data.objects['NEW - REFERENCE hand '+side];hand.location=wrist
        landmarks[side]={key:list(value+seat.location) for key,value in [('hip',hip),('knee',knee),('ankle',ankle),('shoulder',shoulder),('elbow',arm_elbow),('wrist',wrist)]}
        # Low collar armor supports the pelvis directly on the robot, with no
        # separate chair silhouette. Top surface meets the figure's underside.
        plate('V16 neck collar bearing',[(sign*.01,-.405,4.035),(sign*.055,-.405,4.035),
            (sign*.25,-.16,3.98),(sign*.10,-.12,3.98)],.035,dark)
        rod('V16 neck collar strut',(sign*.41,-.13,3.70),(sign*.22,-.14,3.96),.052,steel)
    bpy.context.view_layer.update()
    return dict(rider_ride_pose='Directly astride the neck, thighs around shoulders, legs forward; no chair',
        rider_reference_only=True,rider_runtime_integrated=False,rider_landmarks=landmarks,
        rider_fixed_limb_lengths_m=dict(thigh=.49,calf=.43,upper_arm=.30,forearm=.28),
        spoiler_mount='Original pedestals in two hinge cradles on back-frame folding arms',spoiler_hinge_points=hinge_points,
        rear_neon_location='Continuous vertical dorsal tube above exhaust',removed_seat_hardware=len(removed),
        transformation_validated=False)
