"""Offline geometric review of the runtime gait, on the approved V18 scene.

This mirrors TransformerVisual.Walk for authoring inspection. It is NOT a
Valheim physics/Player test. Run sequentially with Blender --threads 2.
"""
import json,math,sys,hashlib,itertools
from pathlib import Path
import bpy
import bmesh
import numpy as np
from collections import defaultdict
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'artifacts/transformer-runtime-review';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'))
scene=bpy.context.scene;scene.frame_set(121)
rig=bpy.data.objects['RIG - 350Z cinematic V18']
neutral={p.name:p.matrix.copy() for p in rig.pose.bones}
foot_support={}
for side in ('L','R'):
    points=[]
    for col in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT'):
        for o in bpy.data.collections[col].objects:
            if o.type!='MESH':continue
            ancestor=o
            while ancestor and ancestor.parent_type!='BONE':ancestor=ancestor.parent
            if not ancestor:continue
            bone=rig.data.bones[ancestor.parent_bone]
            attachment=bone
            while attachment and not attachment.name.startswith(('panel.','neon.')) and attachment.name!='foot.'+side:attachment=attachment.parent
            follows_foot=bool(attachment and attachment.name=='foot.'+side)
            if attachment and attachment.name.startswith(('panel.','neon.')) and not attachment.name.startswith(('panel.hipguard_','panel.sill1_')):
                leg=attachment.parent
                while leg and not leg.name.startswith(('thigh.','shin.','foot.')):leg=leg.parent
                follows_foot=bool(leg and leg.name.startswith(('shin.','foot.')) and leg.name.endswith('.'+side))
            if attachment and attachment.name.startswith('panel.floor0_') and ('_fold1_' in attachment.name or '_fold2_' in attachment.name):follows_foot=('_'+side+'_') in attachment.name
            if attachment and attachment.name.startswith('neon.underbody side'):follows_foot=False
            if follows_foot:points.extend(neutral['foot.'+side].inverted()@o.matrix_world@v.co for v in o.data.vertices)
    mesh=bmesh.new()
    for p in points:mesh.verts.new(p)
    hull=bmesh.ops.convex_hull(mesh,input=list(mesh.verts),use_existing_faces=False)
    foot_support[side]=[v.co.copy() for v in hull['geom'] if isinstance(v,bmesh.types.BMVert)]
    mesh.free()
def terrain_hinge(name):return (name.startswith('panel.floor1_') and '_fold0_' in name) or (name.startswith('panel.floor0_') and '_fold1_' in name)
terrain_rows={};terrain_hinges={}
for obj in [o for col in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT') for o in bpy.data.collections[col].objects if o.type=='MESH']:
    ancestor=obj
    while ancestor and ancestor.parent_type!='BONE':ancestor=ancestor.parent
    if ancestor and terrain_hinge(ancestor.parent_bone):
        name=ancestor.parent_bone;row=name[:-2]
        terrain_rows.setdefault(row,[]).extend((name,(obj.matrix_world@v.co).z) for v in obj.data.vertices)
for row,points in terrain_rows.items():
    lower=min(z for _,z in points);names=sorted(set(name for name,_ in points))
    pivot=Vector((0,neutral[names[0]].translation.y,lower))
    for name in names:terrain_hinges[name]=pivot
clearance_file=ROOT/(sys.argv[sys.argv.index('--clearance')+1] if '--clearance' in sys.argv else 'tools/transformer_gait_clearance.json')
clearance=json.loads(clearance_file.read_text()) if clearance_file.exists() and '--no-clearance' not in sys.argv else {}
source_action=rig.animation_data.action if rig.animation_data else None
rig.animation_data_clear()
levels={}
rest_inverse={b.name:b.matrix_local.inverted() for b in rig.data.bones}
for b in rig.data.bones:
    depth=0;p=b.parent
    while p:depth+=1;p=p.parent
    levels.setdefault(depth,[]).append(b.name)
def trs(p,q):return Matrix.Translation(p)@q.to_matrix().to_4x4()
def parameter(name,default):return float(sys.argv[sys.argv.index(name)+1]) if name in sys.argv else default
sys.path.insert(0,str(ROOT/'tools'))
from transformer_locomotion_review import GAIT_VERSION,DOMAIN_FILE,TRACE_FILE
runtime_definition=json.loads((ROOT/'tools/transformer_gait_v3.json').read_text())
trace_path=ROOT/(sys.argv[sys.argv.index('--pose-trace')+1] if '--pose-trace' in sys.argv else DOMAIN_FILE)
runtime_poses=json.loads(trace_path.read_text()) if '--runtime' in sys.argv else []
assert '--runtime' not in sys.argv or runtime_poses,'Runtime gait requires actual managed pose samples'

last_gait_diagnostics={}
ground_provider=None
last_gait_families={}

def ground_at(point):
    if ground_provider is None:return Vector((0,0,1)),0.
    normal,height=ground_provider(point)
    normal=Vector(normal)
    if not all(math.isfinite(v) for v in normal) or normal.length_squared<.001 or normal.z<=.5:normal=Vector((0,0,1))
    if not math.isfinite(height):height=0.
    return normal.normalized(),height

def around(p,q):return Matrix.Translation(p)@q.to_matrix().to_4x4()@Matrix.Translation(-p)
def rotation(axis,degrees):return Quaternion(axis,math.radians(degrees))
def apply_world(world):
    # Original deployment bones disable inherited scale. Blender's matrix setter
    # applies that rule; the fast unit-scale gait basis formula cannot be used
    # on compacted V18 source poses without distorting their legs and glass.
    for depth in sorted(levels):
        for name in levels[depth]:rig.pose.bones[name].matrix=world[name]
        bpy.context.view_layer.update()
    error=max(abs(rig.pose.bones[n].matrix[r][c]-world[n][r][c]) for n in world for r in range(4) for c in range(4))
    assert error<.0002,('Original hierarchy/scale reconstruction mismatch',error)

def source_pose(progress):
    """Authoritative original transformation, before any terrain correction."""
    global last_gait_diagnostics,last_gait_families
    rig.animation_data_create();rig.animation_data.action=source_action
    frame=1+120*progress;scene.frame_set(int(frame),subframe=frame-int(frame))
    bpy.context.view_layer.update()
    world={p.name:p.matrix.copy() for p in rig.pose.bones}
    rig.animation_data_clear();apply_world(world)
    last_gait_diagnostics={};last_gait_families={}
    return world

def gait(phase,amount,reverse=False,left_direction=None,right_direction=None,release=None,posture=None):
    """Mirror the runtime solver; V3 state is supplied by managed trace, never guessed."""
    global last_gait_diagnostics,last_gait_families
    runtime='--runtime' in sys.argv
    if scene.frame_current!=121 or scene.frame_subframe!=0:scene.frame_set(121)
    if runtime and posture is None:
        # Authoring selection only. Geometry loops below consume every actual
        # controller sample directly; they never reconstruct state from phase.
        candidates=[p for p in runtime_poses if (p['left']+p['right']<0)==reverse] or runtime_poses
        posture=min(candidates,key=lambda p:abs(p['amount']-amount)*4+min(abs(p['phase']-phase),1-abs(p['phase']-phase)))
    if posture is not None:
        phase=posture['phase'];amount=posture['amount'];release=posture['release']
    if runtime:
        release=1 if release is None else release
        release=release*release*(3-2*release)
    overrides={};ankle_offsets={};targets={};foot_rotations={};ground_align={};support_heights={};terrain_exposure=0.
    body=posture or dict(pelvisSide=0,pelvisHeight=0,pelvisYaw=0,torsoYaw=0,torsoLean=0,torsoRoll=0)
    pelvis_yaw=around(neutral['pelvis'].translation,rotation((0,0,1),-body['pelvisYaw']))
    pelvis_height=body['pelvisHeight']
    feet={}
    for side in ('L','R'):
        thigh,shin,foot=['%s.%s'%(x,side) for x in ('thigh','shin','foot')]
        cycle=(phase+(.5 if side=='R' else 0))%1
        if posture is not None:
            f=posture['rightFoot' if side=='R' else 'leftFoot']
        else:
            u=(cycle-.5)*2;u=u*u*(3-2*u)
            stride=parameter('--stride',.32);height=parameter('--lift',.20)
            direction=(left_direction if side=='L' else right_direction)
            if direction is None:direction=-1 if reverse else 1
            f=dict(forward=stride*(1-4*cycle if cycle<.5 else -1+2*u)*amount*direction,
                   lift=(0 if cycle<.5 else math.sin((cycle-.5)*math.pi*2)*height)*amount,
                   swing=math.sin(cycle*math.pi*2)*9*amount*direction,elbow=0,side=0,pitch=0,yaw=0)
        feet[side]=f
        hip,knee,ankle=[neutral[n].translation for n in (thigh,shin,foot)]
        target=ankle+Vector((f['side'],f['forward'],0))
        rest_min=min((neutral[foot]@v).z for v in foot_support[side])
        normal,height=ground_at(Vector((target.x,target.y,rest_min)));plane_point=Vector((target.x,target.y,height))
        ground_align[side]=Vector((0,0,1)).rotation_difference(normal)
        support_heights[side]=height-(normal.x*(ankle.x-target.x)+normal.y*(ankle.y-target.y))/normal.z
        terrain_exposure=max(terrain_exposure,math.degrees(ground_align[side].angle)/15)
        flat_rotation=ground_align[side]@rotation((0,0,1),-f['yaw'])@neutral[foot].to_quaternion()
        foot_rotations[side]=ground_align[side]@rotation((0,0,1),-f['yaw'])@rotation((1,0,0),f['pitch'])@neutral[foot].to_quaternion()
        rolled=trs(target,foot_rotations[side])
        pivot=min(foot_support[side],key=lambda v:normal.dot(rolled@v-plane_point))
        lowest=normal.dot(rolled@pivot-plane_point)
        contact_offset=flat_rotation@pivot-foot_rotations[side]@pivot
        tangent_shift=Vector((contact_offset.x,contact_offset.y,0));target+=tangent_shift
        target.z+=(rest_min+f['lift']*normal.z-lowest-normal.dot(tangent_shift))/normal.z
        targets[side]=target;ankle_offsets[side]=target-ankle
        shifted_hip=pelvis_yaw@hip+Vector((body['pelvisSide'],0,0))
        reach=(knee-hip).length+(ankle-knee).length-.001
        horizontal=(target.x-shifted_hip.x)**2+(target.y-shifted_hip.y)**2
        pelvis_height=min(pelvis_height,target.z+math.sqrt(max(.001,reach*reach-horizontal))-shifted_hip.z)
    terrain_exposure=min(1,max(terrain_exposure,abs(support_heights['L']-support_heights['R'])/.2))
    pelvis_delta=Matrix.Translation(Vector((body['pelvisSide'],0,pelvis_height)))@pelvis_yaw
    overrides['pelvis']=pelvis_delta@neutral['pelvis']
    torso_rotation=rotation((0,0,1),-body['torsoYaw'])@rotation((1,0,0),-body['torsoLean'])@rotation((0,1,0),body['torsoRoll'])
    torso_delta=pelvis_delta@around(neutral['thorax'].translation,torso_rotation)
    overrides['thorax']=torso_delta@neutral['thorax'];last_gait_diagnostics={}
    for side in ('L','R'):
        thigh,shin,foot=['%s.%s'%(x,side) for x in ('thigh','shin','foot')]
        rest_hip,knee,ankle=[neutral[n].translation for n in (thigh,shin,foot)]
        hip=pelvis_delta@rest_hip;target=targets[side];f=feet[side]
        l1,l2=(knee-rest_hip).length,(ankle-knee).length
        d=target-hip;distance=max(abs(l1-l2)+.001,min(l1+l2-.001,d.length));direction=d.normalized()
        along=(l1*l1-l2*l2+distance*distance)/(2*distance)
        rest_direction=(ankle-rest_hip).normalized();authored=knee-rest_hip
        authored=authored-rest_direction*authored.dot(rest_direction)
        authored=(authored-direction*authored.dot(direction)).normalized()
        forward_pole=Vector((0,1,0));forward_pole=(forward_pole-direction*forward_pole.dot(direction)).normalized()
        blend=amount*amount*(3-2*amount);bend=authored.slerp(forward_pole,blend).normalized()
        target_knee=hip+direction*along+bend*math.sqrt(max(0,l1*l1-along*along))
        overrides[thigh]=trs(hip,(knee-rest_hip).rotation_difference(target_knee-hip)@neutral[thigh].to_quaternion())
        overrides[shin]=trs(target_knee,(ankle-knee).rotation_difference(target-target_knee)@neutral[shin].to_quaternion())
        overrides[foot]=trs(target,foot_rotations[side])
        solved_end=target_knee+(target-target_knee).normalized()*l2
        last_gait_diagnostics[side]=dict(reachError=max(0,d.length-(l1+l2-.001)),upperLengthError=abs((target_knee-hip).length-l1),
            lowerLengthError=abs((target-target_knee).length-l2),ankleError=(target-solved_end).length,pelvisHeight=pelvis_height)
        arm='upperarm.'+side;forearm='forearm.'+side;hand='hand.'+side
        clear=max(0,min(1,release/.2)) if runtime else 0;run=(posture or {}).get('runBlend',0);sign=-1 if side=='R' else 1
        axis=(neutral[forearm].translation-neutral[arm].translation).normalized()
        abduction=rotation((0,1,0),-sign*(9+3*run)*clear)
        external=rotation(axis,sign*(12+8*run)*clear)
        backswing_bias=amount*(8+10*run)
        overrides[arm]=trs(torso_delta@neutral[arm].translation,torso_delta.to_quaternion()@abduction@rotation((1,0,0),-f['swing']-backswing_bias)@external@neutral[arm].to_quaternion())
        arm_delta=overrides[arm]@neutral[arm].inverted();flex=f['elbow']+16*clear*(1-run)
        overrides[forearm]=trs((arm_delta@neutral[forearm]).translation,arm_delta.to_quaternion()@rotation((1,0,0),flex)@neutral[forearm].to_quaternion())
        forearm_delta=overrides[forearm]@neutral[forearm].inverted();wrist=forearm_delta@neutral[hand]
        roll_amount=max(0,min(1,run/.6));roll_amount=roll_amount*roll_amount*(3-2*roll_amount)
        inward=Vector((-sign,0,0));rest_wrist=neutral[hand].translation
        palm=(neutral['digit00.'+side].translation-rest_wrist).cross(neutral['digit30.'+side].translation-rest_wrist).normalized()
        if palm.dot(inward)<0:palm=-palm
        wrist_axis=(wrist.translation-overrides[forearm].translation).normalized()
        current=forearm_delta.to_3x3()@palm;target=torso_delta.to_3x3()@inward
        current-=wrist_axis*current.dot(wrist_axis);target-=wrist_axis*target.dot(wrist_axis)
        pronation=math.degrees(math.atan2(wrist_axis.dot(current.cross(target)),current.dot(target))) if current.length_squared>.0001 and target.length_squared>.0001 else 0
        roll=rotation(wrist_axis,pronation*roll_amount*clear)
        overrides[forearm]=trs(overrides[forearm].translation,roll@overrides[forearm].to_quaternion())
        forearm_delta=overrides[forearm]@neutral[forearm].inverted();wrist=forearm_delta@neutral[hand]
        overrides[hand]=trs(wrist.translation,forearm_delta.to_quaternion()@neutral[hand].to_quaternion())
    delta={};world={}
    for b in rig.data.bones:
        world[b.name]=overrides.get(b.name,delta[b.parent.name]@neutral[b.name] if b.parent else neutral[b.name])
        if '--articulated-armor' in sys.argv or runtime:
            if b.name.startswith('panel.door_rear_scale'):world[b.name]=delta['clavicle.'+b.name[-1]]@neutral[b.name]
            if b.name.startswith('panel.belt_') and 'hipfold1' in b.name:world[b.name]=delta['thorax']@neutral[b.name]
            if b.name.startswith('panel.floor0_') and ('_fold1_' in b.name or '_fold2_' in b.name):world[b.name]=delta['foot.'+('L' if '_L_' in b.name else 'R')]@neutral[b.name]
            if b.name in ('panel.pelvis_rear','neon.trunk rear') or b.name.startswith('panel.exhaust_'):world[b.name]=delta['thorax']@neutral[b.name]
            if b.name.startswith(('panel.hipguard_','panel.sill1_')):
                world[b.name]=delta['thigh.'+b.name[-1]]@neutral[b.name]
            elif b.name.startswith(('panel.','neon.')) and not b.name.startswith('panel.toe_'):
                ancestor=b.parent
                while ancestor and not ancestor.name.startswith(('thigh.','shin.','foot.')):ancestor=ancestor.parent
                if ancestor and ancestor.name.startswith(('shin.','foot.')):
                    world[b.name]=(Matrix.Translation(ankle_offsets[ancestor.name[-1]]) if b.name.startswith('neon.underbody side') else delta['foot.'+ancestor.name[-1]])@neutral[b.name]
                    world[b.name].translation.x+=parameter('--calf-spread',0)*amount*(1 if ancestor.name[-1]=='L' else -1)
            if b.name=='neon.trunk rear':world[b.name].translation.z+=parameter('--trunk-lift',0)*parameter('--release',amount)
        if runtime and b.name.startswith('neon.underbody side'):
            side=b.name[-1];foot='foot.'+side;yaw=rotation((0,0,1),-feet[side]['yaw'])
            world[b.name]=trs(targets[side],yaw@neutral[foot].to_quaternion())@neutral[foot].inverted()@neutral[b.name]
            world[b.name].translation+=yaw@Vector((0,-.1*release,-.45*release))
        if '--ankle-roll' in sys.argv:
            if b.name.startswith(('panel.hipguard_','panel.sill1_')):
                world[b.name]=delta['thigh.'+b.name[-1]]@neutral[b.name]
        if '--rail-armor' in sys.argv and b.name.startswith(('panel.','neon.')):
            ancestor=b.parent;side=None
            while ancestor:
                if ancestor.name.startswith(('thigh.','shin.','foot.')):side=ancestor.name[-1];break
                ancestor=ancestor.parent
            if b.name.startswith('panel.hipguard_'):side=b.name[-1]
            if b.name.startswith('panel.belt_') and 'hipfold1' in b.name:side=b.name.split('_')[1]
            if side:world[b.name]=Matrix.Translation(ankle_offsets[side])@neutral[b.name]
        if b.name.startswith('panel.') and b.name[6:] in clearance:
            if not runtime:world[b.name].translation+=Vector(clearance[b.name[6:]])*parameter('--release',amount)
        if runtime and b.name in runtime_definition['releases'] and not (b.name.startswith('panel.floor0_') and '_fold2_' in b.name):
            deployment=min(1,release/.25) if b.name.startswith('panel.sill1_') else release
            carrier=delta['thorax'] if b.name=='neon.trunk rear' else delta['thigh.'+b.name[-1]] if b.name.startswith('panel.sill1_') else delta[b.parent.name]
            world[b.name].translation+=carrier.to_3x3()@Vector(runtime_definition['releases'][b.name])*deployment
            if b.name.startswith('panel.sill1_'):
                world[b.name].translation.y+=.04*max(0,min(1,deployment/.1,(.8-deployment)/.1))
                slope=min(1,abs(ground_align[b.name[-1]].y)/.1305262)
                world[b.name].translation+=carrier.to_3x3()@Vector(((.08 if b.name.endswith('L') else -.08)*slope*deployment,0,0))
        if runtime and b.name.startswith('panel.belt_') and 'hipfold1' in b.name:
            fold=min(1,release/.7);angle=90 if '_L_' in b.name else -90
            world[b.name]=delta['thorax']@around(neutral[b.name].translation,rotation((0,1,0),-angle*fold))@neutral[b.name]
        if runtime:
            offset=Vector((0,0,0))
            if b.name.startswith('panel.floor2_') and ('_fold1_' in b.name or '_fold2_' in b.name):
                slide=max(0,min(1,(release-.2)/.5))
                offset+=Vector((0,.32*slide,-.06*max(0,min(1,release/.2,(1-release)/.3))))
            if b.name.startswith('panel.floor0_') and '_fold0_' in b.name:
                run=max(0,min(1,(release-.55)/.4));run=run*run*(3-2*run)
                def smooth(lo,hi):
                    u=max(0,min(1,(run-lo)/(hi-lo)));return u*u*(3-2*u)
                park=smooth(.06,.3)*release;down=.12*smooth(0,.06)*(1-smooth(.3,.55))*release
                parent='thigh.'+('L' if '_L_' in b.name else 'R');origin=delta[parent];target=delta['thorax']@Matrix.Translation(Vector((0,-.95,0)))
                pivot=neutral[parent].translation;p=(origin@pivot).lerp(target@pivot,park)+delta['thorax'].to_3x3()@Vector((0,0,-down))
                carrier=trs(p,origin.to_quaternion().slerp(target.to_quaternion(),park))@Matrix.Translation(-pivot)
                world[b.name]=carrier@neutral[b.name]
            if b.name.startswith('panel.floor0_') and ('_fold1_' in b.name or '_fold2_' in b.name):offset+=Vector((0,-.45*min(1,release/.5),-.6*max(0,min(1,(release-.5)/.5))))
            if b.name.startswith(('panel.hipguard_','panel.rear_corner_','panel.floor2_','panel.sill1_')):
                stow=max(0,min(1,(release-.25)/.75))
                offset+=Vector((0,.25*max(0,min(1,(stow-.4)/.6)),-.15*min(1,stow/.4)))
            if b.name in terrain_hinges:
                side='L' if '_L_' in b.name else 'R'
                world[b.name]=delta['foot.'+side]@around(terrain_hinges[b.name],rotation((1,0,0),-25*terrain_exposure*release))@neutral[b.name]
            if b.name.startswith('panel.door_front_scale0'):
                side=b.name[-1];axis=(neutral['hand.'+side].translation-neutral['forearm.'+side].translation).normalized()
                unlock=min(1,release/.2);fold=max(0,min(1,(release-.2)/.8))
                world[b.name]=delta['forearm.'+side]@Matrix.Translation(-axis*.18*unlock)@around(neutral['forearm.'+side].translation,rotation(axis,(1 if side=='L' else -1)*90*fold))@neutral[b.name]
            if b.name=='neon.trunk rear':world[b.name].translation+=delta['thorax'].to_3x3()@Vector((0,.45*min(1,release/.4),.32*max(0,min(1,(release-.4)/.6))))
            if b.name.startswith('panel.belt_') and 'hipfold1' in b.name:world[b.name].translation+=delta['thorax'].to_3x3()@Vector((0,0,.1*min(1,release/.7)))
            if offset.length_squared:
                side='L' if '_L' in b.name else 'R'
                carrier='foot.' if b.name.startswith('panel.floor0_') and ('_fold1_' in b.name or '_fold2_' in b.name) else 'thigh.'
                world[b.name].translation+=delta[carrier+side].to_3x3()@offset
        delta[b.name]=world[b.name]@neutral[b.name].inverted()
    # A provenance tag denotes an exact common T in world=T@neutral. Unique
    # mutable carriers retain their own tag; unchanged descendants inherit it.
    # This avoids treating a common rigid rotation as a new surface collision.
    last_gait_families={}
    if runtime and not any(flag in sys.argv for flag in ('--calf-spread','--trunk-lift','--rail-armor','--ankle-roll')):
        for b in rig.data.bones:
            parent=b.parent.name if b.parent else None
            if b.name in ('panel.pelvis_rear','neon.trunk rear') or b.name.startswith('panel.exhaust_') or b.name.startswith('panel.belt_') and 'hipfold1' in b.name:parent='thorax'
            elif b.name.startswith('panel.door_rear_scale'):parent='clavicle.'+b.name[-1]
            elif b.name.startswith('panel.floor0_') and ('_fold1_' in b.name or '_fold2_' in b.name):parent='foot.'+('L' if '_L_' in b.name else 'R')
            elif b.name.startswith(('panel.hipguard_','panel.sill1_')):parent='thigh.'+b.name[-1]
            elif b.name.startswith(('panel.','neon.')) and not b.name.startswith('panel.toe_'):
                ancestor=b.parent
                while ancestor and not ancestor.name.startswith(('thigh.','shin.','foot.')):ancestor=ancestor.parent
                if ancestor and ancestor.name.startswith(('shin.','foot.')):parent='foot.'+ancestor.name[-1]
            family=b.name if b.name in overrides else last_gait_families.get(parent,b.name)
            mutable=b.name.startswith(('panel.hipguard_','panel.sill1_','panel.floor2_','panel.door_front_scale0','neon.underbody side')) or b.name=='neon.trunk rear'
            mutable|=b.name.startswith('panel.floor0_')
            mutable|=b.name.startswith('panel.belt_') and 'hipfold1' in b.name
            if mutable or b.name in terrain_hinges:family=b.name
            last_gait_families[b.name]=family
    if runtime:
        for b in rig.data.bones:
            rig.pose.bones[b.name].matrix_basis=rest_inverse[b.name]@(b.parent.matrix_local@world[b.parent.name].inverted()@world[b.name] if b.parent else world[b.name])
        bpy.context.view_layer.update()
        error=max(abs(rig.pose.bones[n].matrix[r][c]-world[n][r][c]) for n in world for r in range(4) for c in range(4))
        assert error<.0001,('Gait pose hierarchy mismatch',error)

    else:
        for depth in sorted(levels):
            for name in levels[depth]:rig.pose.bones[name].matrix=world[name]
            bpy.context.view_layer.update()

visible=[o for c in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT','05 Rider - scale reference, NOT game avatar') for o in bpy.data.collections[c].objects if o.type=='MESH' and not o.hide_render]
mins={};worst=0
runtime='--runtime' in sys.argv
samples=[] if '--skip-ground' in sys.argv else runtime_poses if runtime else list(itertools.product((False,True),range(129)))
for sample in samples:
    if runtime:gait(sample['phase'],sample['amount'],posture=sample)
    else:
        reverse,i=sample;gait(i/128,1,reverse)
    for o in visible:
        # Convex local bounding-box corners conservatively cover raw geometry.
        lower=min((o.matrix_world@Vector(v)).z for v in o.bound_box)
        if lower<-.002:
            actual=min((o.matrix_world@v.co).z for v in o.data.vertices)
            if actual<-.002:mins[o.name]=min(mins.get(o.name,0),actual)
print('GAIT GROUND',len(mins),'penetrating objects',flush=True)
(OUT/'gait-ground.json').write_text(json.dumps(dict(samples=len(samples),belowGround=mins,scope='Forward/reverse flat-ground gait raw vertices; no live game verification'),indent=2))
if '--check-surfaces' in sys.argv:
    helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
    exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
    groups=defaultdict(list)
    for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:groups[obj['module']].append(obj)
    for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
        if obj.get('continuous_unsegmented'):groups[obj.name].append(obj)
    groups['RIDER']=[o for o in bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects if o.type=='MESH']
    fixed_release=None
    if '--release' in sys.argv:
        fixed_release=sys.argv[sys.argv.index('--release')+1];sys.argv[sys.argv.index('--release')+1]='0'
    gait(0,0)
    reference={k:(geometry(v),np.linalg.inv(np.array(v[0].matrix_world))) for k,v in groups.items()}
    def surfaces():
        result={}
        for name,obs in groups.items():
            data,inverse=reference[name];points,faces,tris,normals=data
            d=np.array(obs[0].matrix_world)@inverse;r=d[:3,:3];t=d[:3,3]
            result[name]=placed((points@r.T+t,faces,tris@r.T+t,normals@r.T),np.zeros(3))
        return result
    pairs=[(a,b) for a in groups for b in groups if a<b]
    cache=surfaces();baseline={p:(set() if 'RIDER' in p else hits(cache[p[0]],cache[p[1]])) for p in pairs}
    (OUT/'gait-baseline.json').write_text(json.dumps({a+' / '+b:len(v) for (a,b),v in baseline.items() if v},indent=2))
    if fixed_release is not None:sys.argv[sys.argv.index('--release')+1]=fixed_release
    findings={}
    check_count=17 if '--quick' in sys.argv else 65
    check_amounts=(1,) if '--quick' in sys.argv else tuple(map(float,sys.argv[sys.argv.index('--amounts')+1].split(','))) if '--amounts' in sys.argv else (.1,.25,.5,1)
    cases=list(enumerate(runtime_poses[::16] if '--quick' in sys.argv else runtime_poses)) if runtime else list(itertools.product((False,True),check_amounts,range(check_count)))
    for case in cases:
        if runtime:
            i,sample=case;gait(sample['phase'],sample['amount'],posture=sample);label=str(i)
        else:
            reverse,amount,i=case
            if '--deploy-only' in sys.argv:
                sys.argv[sys.argv.index('--release')+1]=str(i/(check_count-1));amount=0
            gait(i/(check_count-1),amount,reverse);label='%s/%s/%s'%(reverse,amount,i)
        cache=surfaces()
        found={a+' / '+b:len(hits(cache[a],cache[b])-baseline[(a,b)]) for a,b in pairs}
        found={k:v for k,v in found.items() if v}
        if found:findings[label]=found
        if i%100==0:print('GAIT SURFACES',i,'cumulative failing poses',len(findings),flush=True)
    (OUT/'gait-surfaces.json').write_text(json.dumps(dict(samples=len(cases),quick='--quick' in sys.argv,newContacts=findings,
        gaitClearanceSha256=hashlib.sha256(clearance_file.read_bytes()).hexdigest(),
        scope='All exterior-module pairs, seven neon tubes and rigid reference rider. 1.5mm transverse triangle crossings; neutral endpoint pairs excluded except rider. Not all inner mechanism.'),indent=2))
    if '--check-only' in sys.argv:sys.exit(0)
# Hold non-rig local transforms at the approved endpoint, before changing time.
for o in visible:
    local=o.matrix_basis.copy()
    if o.animation_data:
        if o.animation_data.action and not o.name.startswith('V18 connected'):o.animation_data.action=None
        for d in list(o.animation_data.drivers):
            if d.data_path in ('hide_render','hide_viewport'):o.driver_remove(d.data_path)
    o.matrix_basis=local;o.hide_render=False
render_path=ROOT/TRACE_FILE
render_poses=json.loads(render_path.read_text()) if runtime and render_path.exists() and '--pose-trace' not in sys.argv else runtime_poses
for f in range(1,98):
    scene.frame_set(f)
    amount=min(1,(f-1)/12,(97-f)/12)
    if runtime:
        sample=render_poses[min(len(render_poses)-1,round((f-1)*60/24))];gait(sample['phase'],sample['amount'],posture=sample)
    else:gait((f-1)/48,amount)
    for p in rig.pose.bones:
        for prop in ('location','rotation_quaternion','scale'):p.keyframe_insert(prop,frame=f)
scene.frame_start=1;scene.frame_end=97;scene.render.fps=24
scene.frame_set(37)
scene.camera.location=(7,11,5);scene.camera.rotation_euler=(Vector((0,0,2.35))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=7;scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=16
scene.render.resolution_x=scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.render.threads_mode='FIXED';scene.render.threads=2
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'350z-walk-review.blend'))
if '--render' in sys.argv:
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'walk.png');bpy.ops.render.render(write_still=True)
if '--animation' in sys.argv:
    scene.frame_step=2;scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='MEDIUM'
    scene.render.filepath=str(OUT/'350z-walk-review.mp4');bpy.ops.render.render(animation=True)
print('WALK REVIEW',OUT,flush=True)
