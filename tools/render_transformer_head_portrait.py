"""Additional Cycles head portraits; does not save or mutate the design blend.

Uses two CPU threads. Reference rider is hidden only in these labeled studio
portraits; normal design views keep it visible with the original stage lights.
"""
from pathlib import Path
import bpy
from mathutils import Vector

scene=bpy.context.scene
out=Path(bpy.data.filepath).parent
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=48
scene.cycles.max_bounces=5;scene.cycles.diffuse_bounces=2;scene.cycles.glossy_bounces=3
scene.render.threads_mode='FIXED';scene.render.threads=2
bpy.context.view_layer.cycles.use_denoising=True
scene.render.resolution_x=scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.025,.025,.025,1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.25
for o in bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects:o.hide_render=True
for o in scene.objects:
    if o.type=='LIGHT':o.hide_render=True
for name,pos,power,size in [('key',(1.2,2.2,5.7),220,1.4),('fill',(-1.8,1.3,4.3),45,1.8),('rim',(-.9,-.8,5.2),320,1.1)]:
    data=bpy.data.lights.new('Head portrait '+name,'AREA');data.energy=power;data.size=size
    o=bpy.data.objects.new(data.name,data);scene.collection.objects.link(o);o.location=pos
    o.rotation_euler=(Vector((0,.1,4.35))-o.location).to_track_quat('-Z','Y').to_euler()
camera=scene.camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.08
for name,pos in [('14-head-studio-front',(0,6,4.32)),('15-head-studio-quarter',(1.8,6,4.4))]:
    camera.location=pos;camera.rotation_euler=(Vector((0,.12,4.32))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
