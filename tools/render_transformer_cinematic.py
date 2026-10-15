"""Sequential V18 preview renderer. Never run alongside another Blender job."""
import argparse
import sys
from pathlib import Path
import bpy
from mathutils import Vector

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--animation',action='store_true')
p.add_argument('--frames',default='1,25,49,73,97,121')
p.add_argument('--cinematic',action='store_true')
args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
scene=bpy.context.scene;out=Path(bpy.data.filepath).parent
scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=16
scene.eevee.use_gtao=True;scene.eevee.gtao_distance=1.5
scene.eevee.use_bloom=True;scene.eevee.bloom_intensity=.025
scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=960 if args.cinematic else 640
scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.camera.animation_data_clear();scene.camera.data.animation_data_clear()
scene.camera.location=(7,11,5)
scene.camera.rotation_euler=(Vector((0,0,2.35))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=9 if args.cinematic else 7
if args.cinematic:
    # Continuous camera travel; no edits conceal the mechanics in the review.
    for frame,loc,target,scale in [
        (1,(6,10,3.5),(0,0,1.2),6.5),
        (25,(6.5,10,4.5),(0,0,1.8),7.7),
        (73,(7,11,4.6),(0,0,2.2),8.5),
        (121,(6,11,3.7),(0,0,2.6),8.4),
        (145,(5.5,11,3.7),(0,0,2.6),8.4),
        (193,(7,11,4.6),(0,0,2.2),8.5),
        (241,(6.5,10,4.5),(0,0,1.8),7.7),
        (265,(6,10,3.5),(0,0,1.2),6.5)]:
        scene.camera.location=loc
        scene.camera.rotation_euler=(Vector(target)-scene.camera.location).to_track_quat('-Z','Y').to_euler()
        scene.camera.data.ortho_scale=scale
        scene.camera.keyframe_insert('location',frame=frame)
        scene.camera.keyframe_insert('rotation_euler',frame=frame)
        scene.camera.data.keyframe_insert('ortho_scale',frame=frame)
    for datablock in (scene.camera,scene.camera.data):
        for curve in datablock.animation_data.action.fcurves:
            for key in curve.keyframe_points:key.interpolation='BEZIER';key.handle_left_type=key.handle_right_type='AUTO_CLAMPED'
if args.animation:
    if args.cinematic:
        # Give both closed-car endpoints one second to read. This render-only
        # timing edit leaves the audited Blender animation at frames 1..265.
        actions={data.animation_data.action for data in list(bpy.data.objects)+list(bpy.data.cameras)
                 if data.animation_data and data.animation_data.action}
        for action in actions:
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.co.x+=24;key.handle_left.x+=24;key.handle_right.x+=24
        # Quicker central unfolding conceals internal compaction; slow release,
        # head deployment and endpoint holds still make the original parts read.
        timing=[(1,1),(25,25),(49,49),(105,81),(145,121),(169,145),
                (209,185),(265,217),(289,241),(313,265)]
        def retime(x):
            for (a,u),(b,v) in zip(timing,timing[1:]):
                if x<=b:return u+(x-a)*(v-u)/(b-a)
            return x-48
        for action in actions:
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.co.x=retime(key.co.x)
                    key.handle_left.x=retime(key.handle_left.x)
                    key.handle_right.x=retime(key.handle_right.x)
                curve.update()
        for obj in bpy.data.objects:
            if not obj.get('v18_concealed'):continue
            for driver in obj.animation_data.drivers:
                if driver.data_path in ('hide_render','hide_viewport'):
                    driver.driver.expression='frame < %.6f or frame > %.6f'%(retime(36.5),retime(277.5))
    scene.frame_start=1;scene.frame_end=265;scene.frame_step=2;scene.render.fps=24
    scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
    scene.render.ffmpeg.constant_rate_factor='MEDIUM'
    scene.render.filepath=str(out/('350z-transformation-v18.mp4' if args.cinematic else '350z-transformation-review-v18.mp4'))
    bpy.ops.render.render(animation=True)
else:
    scene.render.image_settings.file_format='PNG'
    for frame in map(int,args.frames.split(',')):
        scene.frame_set(frame);scene.render.filepath=str(out/('pose-%03d.png'%frame))
        bpy.ops.render.render(write_still=True)
