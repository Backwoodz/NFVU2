"""Sequential V17 joint-test preview; CPU rendering avoids GPU load spikes."""
import argparse
import sys
from pathlib import Path
import bpy
from mathutils import Vector

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--animation', action='store_true')
parser.add_argument('--frame', type=int, default=49)
parser.add_argument('--view', choices=['hero','rear','hand'], default='hero')
parser.add_argument('--engine', choices=['cycles','eevee'], default='cycles')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
scene=bpy.context.scene
out=Path(bpy.data.filepath).parent
scene.render.engine='CYCLES';scene.cycles.device='CPU'
scene.cycles.samples=4 if args.animation else 16
scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=scene.render.resolution_y=640
scene.render.resolution_percentage=100
if hasattr(scene.view_layers[0].cycles,'use_denoising'):
    scene.view_layers[0].cycles.use_denoising=True
if args.engine=='eevee':
    scene.render.engine='BLENDER_EEVEE'
    scene.eevee.taa_render_samples=16
location,target,scale={
 'hero':((7,11,4.8),(0,0,2.5),6.1),
 'rear':((-7,-11,4.8),(0,0,2.5),6.1),
 'hand':((7,10,4),(1.4,1.0,2.8),2.7),
}[args.view]
scene.camera.location=location
scene.camera.rotation_euler=(Vector(target)-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=scale
if args.animation:
    # Blender timestamps by scene-frame number: keep 24fps when rendering every
    # second frame, yielding a 12fps preview at the original animation speed.
    scene.frame_start=1;scene.frame_step=2;scene.render.fps=24
    scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
    scene.render.ffmpeg.constant_rate_factor='MEDIUM'
    scene.render.filepath=str(out/('350z-rig-joint-test-'+args.view+'.mp4'))
    bpy.ops.render.render(animation=True)
else:
    scene.frame_set(args.frame)
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(out/('rig-%s-%03d.png'%(args.view,args.frame)))
    bpy.ops.render.render(write_still=True)
