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
            while bone and bone.name!='foot.'+side:bone=bone.parent
            if bone:points.extend(neutral['foot.'+side].inverted()@o.matrix_world@v.co for v in o.data.vertices)
    mesh=bmesh.new()
    for p in points:mesh.verts.new(p)
    hull=bmesh.ops.convex_hull(mesh,input=list(mesh.verts),use_existing_faces=False)
    foot_support[side]=[v.co.copy() for v in hull['geom'] if isinstance(v,bmesh.types.BMVert)]
    mesh.free()
clearance_file=ROOT/(sys.argv[sys.argv.index('--clearance')+1] if '--clearance' in sys.argv else 'tools/transformer_gait_clearance.json')
clearance=json.loads(clearance_file.read_text()) if clearance_file.exists() and '--no-clearance' not in sys.argv else {}
rig.animation_data_clear()
levels={}
rest_inverse={b.name:b.matrix_local.inverted() for b in rig.data.bones}
for b in rig.data.bones:
    depth=0;p=b.parent
    while p:depth+=1;p=p.parent
    levels.setdefault(depth,[]).append(b.name)
def trs(p,q):return Matrix.Translation(p)@q.to_matrix().to_4x4()
def parameter(name,default):return float(sys.argv[sys.argv.index(name)+1]) if name in sys.argv else default
runtime_definition=json.loads((ROOT/'tools/transformer_gait_v2.json').read_text())
def gait(phase,amount,reverse=False,left_direction=None,right_direction=None,release=None):
    runtime='--runtime' in sys.argv
    if runtime:
        release=1 if release is None else release
        release=release*release*(3-2*release)
    overrides={};ankle_offsets={}
    for side in ('L','R'):
        thigh,shin,foot=['%s.%s'%(x,side) for x in ('thigh','shin','foot')]
        cycle=(phase+(.5 if side=='R' else 0))%1
        u=(cycle-.5)*2;u=u*u*(3-2*u)
        stride=runtime_definition['stride'] if runtime else parameter('--stride',.32);height=runtime_definition['lift'] if runtime else parameter('--lift',.20)
        forward=stride*(1-4*cycle) if cycle<.5 else stride*(-1+2*u)
        lift=0 if cycle<.5 else math.sin((cycle-.5)*math.pi*2)*height
        hip,knee,ankle=[neutral[n].translation for n in (thigh,shin,foot)]
        stride_direction=(left_direction if side=='L' else right_direction)
        if stride_direction is None:stride_direction=-1 if reverse else 1
        target=ankle+Vector((0,forward*stride_direction,lift))*amount
        ankle_offsets[side]=target-ankle
        l1,l2=(knee-hip).length,(ankle-knee).length
        for iteration in range(5 if '--ankle-roll' in sys.argv else 1):
            d=target-hip;distance=max(abs(l1-l2)+.001,min(l1+l2-.001,d.length));direction=d.normalized()
            along=(l1*l1-l2*l2+distance*distance)/(2*distance)
            bend=knee-hip;bend=(bend-direction*bend.dot(direction)).normalized()
            target_knee=hip+direction*along+bend*math.sqrt(max(0,l1*l1-along*along))
            shin_rotation=(ankle-knee).rotation_difference(target-target_knee)@neutral[shin].to_quaternion()
            foot_rotation=shin_rotation@neutral[shin].to_quaternion().inverted()@neutral[foot].to_quaternion()
            if '--ankle-roll' in sys.argv and iteration<4:
                foot_matrix=trs(target,foot_rotation)
                current_min=min((foot_matrix@v).z for v in foot_support[side])
                rest_min=min((neutral[foot]@v).z for v in foot_support[side])
                target.z+=rest_min+lift*amount-current_min
        overrides[thigh]=trs(hip,(knee-hip).rotation_difference(target_knee-hip)@neutral[thigh].to_quaternion())
        overrides[shin]=trs(target_knee,shin_rotation)
        overrides[foot]=trs(target,foot_rotation if '--ankle-roll' in sys.argv else neutral[foot].to_quaternion())
        arm='upperarm.'+side
        # Unity is reflected; positive Unity X rotation is negative Blender X.
        swing=math.cos(cycle*math.pi*2)*runtime_definition['armSwing']*amount*stride_direction if runtime else math.sin(cycle*math.pi*2)*9*amount*stride_direction
        overrides[arm]=trs(neutral[arm].translation,Quaternion((1,0,0),-math.radians(swing))@neutral[arm].to_quaternion())
        if runtime:
            forearm='forearm.'+side;arm_delta=overrides[arm]@neutral[arm].inverted()
            elbow=runtime_definition['elbowBase']*amount+runtime_definition['elbowForward']*max(0,-swing/runtime_definition['armSwing'])
            overrides[forearm]=trs((arm_delta@neutral[forearm]).translation,arm_delta.to_quaternion()@Quaternion((1,0,0),math.radians(elbow))@neutral[forearm].to_quaternion())
    delta={};world={}
    for b in rig.data.bones:
        world[b.name]=overrides.get(b.name,delta[b.parent.name]@neutral[b.name] if b.parent else neutral[b.name])
        if '--articulated-armor' in sys.argv or runtime:
            if b.name.startswith(('panel.hipguard_','panel.sill1_')):
                world[b.name]=delta['thigh.'+b.name[-1]]@neutral[b.name]
            elif b.name.startswith(('panel.','neon.')):
                ancestor=b.parent
                while ancestor and not ancestor.name.startswith(('thigh.','shin.','foot.')):ancestor=ancestor.parent
                if ancestor and ancestor.name.startswith(('shin.','foot.')):
                    world[b.name]=Matrix.Translation(ankle_offsets[ancestor.name[-1]])@neutral[b.name]
                    world[b.name].translation.x+=parameter('--calf-spread',0)*amount*(1 if ancestor.name[-1]=='L' else -1)
            if b.name=='neon.trunk rear':world[b.name].translation.z+=parameter('--trunk-lift',0)*parameter('--release',amount)
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
        if runtime and b.name in runtime_definition['releases']:
            world[b.name].translation+=Vector(runtime_definition['releases'][b.name])*release
            if b.name.startswith('panel.sill1_'):
                world[b.name].translation.y+=.04*max(0,min(1,release/.1,(.8-release)/.1))
        delta[b.name]=world[b.name]@neutral[b.name].inverted()
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
for reverse,i in ([] if '--skip-ground' in sys.argv else itertools.product((False,True),range(129))):
    gait(i/128,1,reverse)
    for o in visible:
        # Convex local bounding-box corners conservatively cover raw geometry.
        lower=min((o.matrix_world@Vector(v)).z for v in o.bound_box)
        if lower<-.002:
            actual=min((o.matrix_world@v.co).z for v in o.data.vertices)
            if actual<-.002:mins[o.name]=min(mins.get(o.name,0),actual)
print('GAIT GROUND',len(mins),'penetrating objects',flush=True)
(OUT/'gait-ground.json').write_text(json.dumps(dict(samples=258,belowGround=mins,scope='Forward/reverse flat-ground gait raw vertices; no live game verification'),indent=2))
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
    for reverse,amount,i in itertools.product((False,True),check_amounts,range(check_count)):
        if '--deploy-only' in sys.argv:
            sys.argv[sys.argv.index('--release')+1]=str(i/(check_count-1));amount=0
        gait(i/(check_count-1),amount,reverse);cache=surfaces()
        found={a+' / '+b:len(hits(cache[a],cache[b])-baseline[(a,b)]) for a,b in pairs}
        found={k:v for k,v in found.items() if v}
        if found:findings['%s/%s/%s'%(reverse,amount,i)]=found
        if i==check_count-1:print('GAIT SURFACES',reverse,amount,'cumulative failing poses',len(findings),flush=True)
    (OUT/'gait-surfaces.json').write_text(json.dumps(dict(samples=check_count*len(check_amounts)*2,newContacts=findings,
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
for f in range(1,98):
    scene.frame_set(f)
    amount=min(1,(f-1)/12,(97-f)/12)
    gait((f-1)/48,amount)
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
