"""Render the reviewed runtime walk: deploy, walk, pivot, settle, retract.

Run one Blender process with --threads 2. Uses the same pose function as the
geometric audit; the mannequin remains a reference, not the Valheim Player.
"""
from pathlib import Path
import sys,math,json
sys.argv.append('--runtime')
source=Path(__file__).with_name('preview_transformer_locomotion.py').read_text()
exec(source[:source.index('\nvisible=')])
visible=[o for c in ('01 Original 350Z surfaces','02 Authored mechanism - BLOCKOUT','05 Rider - scale reference, NOT game avatar') for o in bpy.data.collections[c].objects if o.type=='MESH' and not o.hide_render]
for o in visible:
    local=o.matrix_basis.copy()
    if o.animation_data:
        if o.animation_data.action and not o.name.startswith('V18 connected'):o.animation_data.action=None
        for d in list(o.animation_data.drivers):
            if d.data_path in ('hide_render','hide_viewport'):o.driver_remove(d.data_path)
    o.matrix_basis=local;o.hide_render=False
for mat in bpy.data.materials:
    if mat.name=='V9 - V13 reactor plasma' and mat.use_nodes:
        bsdf=mat.node_tree.nodes.get('Principled BSDF')
        if bsdf:
            e=bsdf.inputs['Emission'].default_value
            bsdf.inputs['Emission'].default_value=(*[x*.15 for x in e[:3]],1)
scene.render.fps=24
trace=json.loads((ROOT/'artifacts/tests/transformer/runtime-gait-preview.json').read_text())
# Controller trace is60Hz. Keep the34second trajectory at24fps instead of
# stretching its2041samples to an85second slow-motion preview.
frames=[trace[min(len(trace)-1,round(f/24*60))] for f in range(round(trace[-1]['time']*24)+1)]
for f,data in enumerate(frames,1):
    scene.frame_set(f)
    gait(data['phase'],data['amount'],posture=data)
    position=Vector((data['x'],data['z'],0))
    rig.location=position;rig.rotation_euler.z=-math.radians(data['yaw'])
    rig.keyframe_insert('location',frame=f);rig.keyframe_insert('rotation_euler',frame=f)
    for p in rig.pose.bones:
        for prop in ('location','rotation_quaternion','scale'):p.keyframe_insert(prop,frame=f)
    scene.camera.location=position+Vector((7,11,5))
    scene.camera.rotation_euler=(position+Vector((0,0,2.35))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
    scene.camera.keyframe_insert('location',frame=f);scene.camera.keyframe_insert('rotation_euler',frame=f)
for obj in (rig,scene.camera):
    if obj.animation_data and obj.animation_data.action:
        for curve in obj.animation_data.action.fcurves:
            for k in curve.keyframe_points:k.interpolation='LINEAR'
scene.frame_start=1;scene.frame_end=len(frames);scene.frame_set(109)
scene.camera.data.ortho_scale=7.2;scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=16
scene.render.resolution_x=scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.render.threads_mode='FIXED';scene.render.threads=2
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'350z-locomotion-v3.blend'))
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'locomotion-v3.png');bpy.ops.render.render(write_still=True)
if '--animation' in sys.argv:
    if '--preview-seconds' in sys.argv:
        seconds=float(sys.argv[sys.argv.index('--preview-seconds')+1])
        if not math.isfinite(seconds) or seconds<=0:raise ValueError('Preview duration must be positive and finite')
        # Keep the saved Blender scene complete; only shorten the video render.
        scene.frame_end=min(len(frames),round(seconds*scene.render.fps)+1)
    scene.frame_step=1;scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='MEDIUM'
    scene.render.filepath=str(OUT/'350z-locomotion-v3.mp4');bpy.ops.render.render(animation=True)
print('LOCOMOTION PREVIEW',OUT,flush=True)
