"""Reproducible V18 clearance adjustments on the saved animation prototype."""
import json
import hashlib
import sys
import bisect
import math
from pathlib import Path
import bpy
from mathutils import Matrix, Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'artifacts/transformer-study-v18'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'350z-transformer-cinematic-base.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['RIG - 350Z cinematic V18']
audit=json.loads((ROOT/'artifacts/transformer-study-v17/rig-audit.json').read_text())
fit_path=ROOT/'tools/transformer_cinematic_clearance.json'
fitted=json.loads(fit_path.read_text()) if fit_path.exists() and '--no-fit' not in sys.argv else {}
fitted={float(k):v for k,v in fitted.items()};knots=sorted(fitted)
def fit_shift(name,t):
    if not knots:return Vector()
    position=1+120*t
    index=max(1,min(len(knots)-1,bisect.bisect_right(knots,position)))
    lo,hi=knots[index-1:index+1]
    a=Vector(fitted[lo].get(name,(0,0,0)));b=Vector(fitted[hi].get(name,(0,0,0)))
    return a.lerp(b,(position-lo)/(hi-lo))

def ease(t):
    t=max(0,min(1,t));return t*t*(3-2*t)

def pulse(t,a,b,c,d):return ease((t-a)/(b-a))*(1-ease((t-c)/(d-c)))

# Car and robot endpoints remain exact. Outward detours are on the same live
# panel supports, with mirrored movements on the two sides.
def correction(name,t):
    sign=1 if name.endswith('_L') or 'L_fold' in name or name.endswith(' L') else -1
    v=Vector((0,0,0))
    if name.startswith('panel.floor1_'):
        v.x=sign*.65*pulse(t,0,.07,.23,.38)
    elif name.startswith('panel.apron_front_'):
        v.x=sign*.55*pulse(t,.16,.28,.80,.96)
    elif name.startswith('panel.apron_back_'):
        p=pulse(t,.18,.30,.94,1)
        v=Vector((sign*.50,-.20,0))*p
    elif name.startswith('neon.underbody side'):
        v.x=sign*.65*pulse(t,.40,.58,.87,1)
    elif name in ('neon.trunk L','neon.trunk R'):
        v.x=sign*.80*pulse(t,.50,.64,.84,1)
    elif name=='neon.underbody rear':
        v=Vector((0,-.30,-.35))*pulse(t,.72,.85,.94,1)
    return v

steel=bpy.data.materials['V9 - brushed titanium']
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.name.startswith('V18 connected sleeve'):
        obj.data.materials.clear();obj.data.materials.append(steel)

mounts=[o for o in bpy.data.objects if o.name.startswith('V9 - neon chassis mount')]
scene.frame_set(121)
mount_rest={o.name:rig.pose.bones[audit['mesh_bones'][o.name]].matrix.inverted()@o.matrix_world for o in mounts}
ride_offset=rig.pose.bones['thorax'].matrix.inverted()@rig.pose.bones['rider.anchor'].matrix.translation
frames=[1+i*.5 for i in range(529)]
source_poses={}
for frame in frames:
    scene.frame_set(int(frame),subframe=frame%1)
    source_poses[frame]={p.name:p.matrix.copy() for p in rig.pose.bones if p.name.startswith(('panel.','neon.','rider.'))}
for frame in frames:
    scene.frame_set(int(frame),subframe=frame%1)
    t=min(1,(frame-1)/120) if frame<=145 else (265-frame)/120
    for p in rig.pose.bones:
        shift=correction(p.name,t)+fit_shift(p.name,t)
        if shift.length or p.name.startswith(('panel.floor1_','panel.apron_','neon.')):
            world=source_poses[frame][p.name].copy();world.translation+=shift;p.matrix=world
            p.keyframe_insert('location',frame=frame,group=p.name)
    bpy.context.view_layer.update()
    # Once the cabin has opened, follow the actual neck support instead of the
    # obsolete V8 seat lift. Moving one ancestor retains all rider limb lengths.
    anchor=rig.pose.bones['rider.anchor']
    desired=rig.pose.bones['thorax'].matrix@ride_offset
    world=source_poses[frame][anchor.name].copy()
    world.translation=world.translation.lerp(desired,ease((t-.16)/.34))
    anchor.matrix=world;anchor.keyframe_insert('location',frame=frame,group=anchor.name)
    bpy.context.view_layer.update()
    # The full-size mannequin needs its feet above the cabin floor. V8's old
    # smaller rider could use a lower ankle target; do not inherit that contact.
    if t<.15:
        transfer=world.translation-source_poses[frame][anchor.name].translation
        lift=.16*(1-ease((t-.03)/.12))
        for side in ('L','R'):
            thigh=rig.pose.bones['rider.thigh.'+side];calf=rig.pose.bones['rider.calf.'+side]
            hip=source_poses[frame][thigh.name].translation+transfer
            ankle=source_poses[frame][calf.name]@Vector((0,calf.bone.length,0))+transfer+Vector((0,0,lift))
            delta=ankle-hip;direction=delta.normalized()
            upper,lower=thigh.bone.length,calf.bone.length
            along=(upper*upper-lower*lower+delta.length_squared)/(2*delta.length)
            bend=Vector((0,0,1));bend-=direction*bend.dot(direction);bend.normalize()
            knee=hip+direction*along+bend*math.sqrt(max(0,upper*upper-along*along))
            for part,a,b in ((thigh,hip,knee),(calf,knee,ankle)):
                turn=(part.bone.tail_local-part.bone.head_local).rotation_difference(b-a)
                rotation=turn@part.bone.matrix_local.to_quaternion()
                part.matrix=Matrix.Translation(a)@rotation.to_matrix().to_4x4()
                bpy.context.view_layer.update()
                for prop in ('location','rotation_quaternion','scale'):
                    part.keyframe_insert(prop,frame=frame,group=part.name)
    for o in mounts:
        s=.025+.975*ease((t-.27)/.26)
        o.matrix_world=rig.pose.bones[audit['mesh_bones'][o.name]].matrix@Matrix.Scale(s,4)@mount_rest[o.name]
        o.rotation_mode='QUATERNION'
        for prop in ('location','rotation_quaternion','scale'):o.keyframe_insert(prop,frame=frame)
    if frame%60==1:print('REFINE',frame,flush=True)
for data in [rig]+mounts:
    for curve in data.animation_data.action.fcurves:
        for key in curve.keyframe_points:key.interpolation='LINEAR'
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if not obj.name.startswith('V18 connected sleeve'):continue
    for frame,hidden in [(1,True),(12,True),(13,False),(253,False),(254,True),(265,True)]:
        obj.hide_render=hidden;obj.hide_viewport=hidden
        obj.keyframe_insert('hide_render',frame=frame);obj.keyframe_insert('hide_viewport',frame=frame)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if not obj.animation_data or not obj.animation_data.action:continue
    channels=[c for c in obj.animation_data.action.fcurves if c.data_path in ('hide_render','hide_viewport')]
    if not channels:continue
    for curve in channels:obj.animation_data.action.fcurves.remove(curve)
    obj['v18_concealed']=True
    # Blender 2.80 can evaluate two driven visibility flags inconsistently
    # when the viewport flag removes the object from the dependency graph.
    # Render visibility is authoritative; viewport keeps internals inspectable.
    obj.hide_viewport=False
    obj.driver_add('hide_render').driver.expression='frame < 12.5 or frame > 253.5'
scene.frame_set(121)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'350z-transformer-cinematic.blend'))
if '--no-fit' in sys.argv:
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'350z-transformer-cinematic-unfitted.blend'))
report=json.loads((OUT/'cinematic-build.json').read_text())
report['blend_sha256']=hashlib.sha256((OUT/'350z-transformer-cinematic.blend').read_bytes()).hexdigest()
report['clearance_revision']='Symmetric exterior detours, retracting authored neon mounts, nape-attached rider, feet clear cabin floor'
report['clearance_validated']=False
(OUT/'cinematic-build.json').write_text(json.dumps(report,indent=2))
print('REFINED',flush=True)
