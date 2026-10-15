"""Render a saved study sequentially, without rebuilding it.

Blender --background STUDY.blend --threads 2 --python this_file.py -- --mode still
The close-up covers the final hood closure, hold and reversed opening.
"""
import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Vector

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--mode',choices=['still','full','hood'],default='still')
parser.add_argument('--engine',choices=['cycles','eevee'],default='cycles',
                    help='CPU Cycles by default; EEVEE uses the graphics device')
parser.add_argument('--frame',type=int,help='Render only this frame in still mode')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=Path(bpy.data.filepath).parent
scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=16 if args.mode=='still' else 4
scene.render.threads_mode='FIXED'
scene.render.threads=2
scene.render.resolution_percentage=100
scene.render.resolution_x=scene.render.resolution_y=640
if hasattr(scene.view_layers[0].cycles,'use_denoising'):
    scene.view_layers[0].cycles.use_denoising=True
if args.engine=='eevee':
    scene.render.engine='BLENDER_EEVEE'
    scene.eevee.taa_render_samples=16
scene.render.image_settings.file_format='PNG'
camera=scene.camera
if args.mode in ('still','hood'):
    camera.location=(6,10,4.7)
    camera.rotation_euler=(Vector((0,.5,3.65))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=3.7
if args.mode=='still':
    for frame in ([args.frame] if args.frame is not None else (109,116,119,121)):
        scene.frame_set(frame)
        scene.render.filepath=str(out/('hood-%03d.png'%frame))
        bpy.ops.render.render(write_still=True)
else:
    scene.frame_start,scene.frame_end,scene.frame_step=(1,265,2) if args.mode=='full' else (101,165,1)
    scene.render.fps=12
    scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4'
    scene.render.ffmpeg.codec='H264'
    scene.render.ffmpeg.constant_rate_factor='MEDIUM'
    scene.render.filepath=str(out/('350z-transformer-motion.mp4' if args.mode=='full' else '350z-hood-clearance-closeup.mp4'))
    bpy.ops.render.render(animation=True)
