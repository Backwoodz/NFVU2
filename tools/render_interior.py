"""Local cabin fit review: Blender --background --python tools/render_interior.py."""
import base64
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector

BASE = Path(__file__).resolve().parents[1]
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def add_model(path, cabin=False):
    model = json.loads(path.read_text()); materials = []
    for m in model['materials']:
        mat = bpy.data.materials.new(m['name']); mat.use_nodes = True
        shader = mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = m['color']
        shader.inputs['Roughness'].default_value = 1-m['smoothness']
        shader.inputs['Metallic'].default_value = m['metallic']
        if m['texture'] >= 0:
            texture = model['textures'][m['texture']]
            node = mat.node_tree.nodes.new('ShaderNodeTexImage')
            node.image = bpy.data.images.load(str(path.parent/(texture['name']+'.png')), check_existing=True)
            node.extension = 'REPEAT' if cabin else 'EXTEND'
            mix = mat.node_tree.nodes.new('ShaderNodeMixRGB'); mix.blend_type='MULTIPLY'; mix.inputs[0].default_value=1; mix.inputs[2].default_value=m['color']
            mat.node_tree.links.new(node.outputs['Color'], mix.inputs[1]); mat.node_tree.links.new(mix.outputs[0], shader.inputs['Base Color'])
        if m['name'].startswith('471a1dca_'):
            mat.blend_method='BLEND'; shader.inputs['Alpha'].default_value=.15
        materials.append(mat)
    for p in model['parts']:
        if p['group'] != 'body': continue
        if not cabin:
            if 'DOOR_PANEL_' in p['name'] or p['name'] in ('350Z_BASE_A_slice3_3','350Z_BASE_A_slice5_5','350Z_BASE_A_slice6_6'): continue
            if p.get('optionGroup') and p['option'] != ('2' if p['optionGroup']=='trunk' else '0'): continue
        mesh=bpy.data.meshes.new(p['name']); mesh.from_pydata([(v['x'],v['z'],v['y']) for v in p['vertices']], [], [p['indices'][i:i+3][::-1] for i in range(0,len(p['indices']),3)]); mesh.update()
        uv=mesh.uv_layers.new()
        for loop in mesh.loops:
            v=p['uv'][loop.vertex_index]; uv.data[loop.index].uv=(v['u'],v['v'])
        for face in mesh.polygons: face.use_smooth=True
        mesh.normals_split_custom_set_from_vertices([(v['x'],v['z'],v['y']) for v in p['normals']])
        mesh.materials.append(materials[p['material']])
        obj=bpy.data.objects.new(p['name'],mesh); bpy.context.collection.objects.link(obj)
        obj['cabin']=cabin

add_model(BASE/'.local/model/body.json'); add_model(BASE/'.local/interior/interior.json',True)
for location,energy,size in (((-2,1,5),1000,4),((3,-1,4),800,3),((0,3,2),500,2)):
    bpy.ops.object.light_add(type='AREA',location=location); bpy.context.object.data.energy=energy; bpy.context.object.data.size=size
bpy.ops.object.camera_add(); camera=bpy.context.object
scene=bpy.context.scene; scene.camera=camera; scene.render.engine='BLENDER_EEVEE'
scene.eevee.use_gtao=True; scene.eevee.gtao_distance=.25; scene.eevee.taa_render_samples=48
scene.render.resolution_x=1400; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.world.color=(.25,.25,.25); scene.view_settings.view_transform='Standard'
def render(name,position,target,lens=45):
    camera.location=position; camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.lens=lens
    scene.render.filepath=str(BASE/'artifacts'/name); bpy.ops.render.render(write_still=True)
# Hide only the roof/glass and physical left door for a section through the cabin.
for obj in bpy.context.scene.objects:
    if obj.type!='MESH' or obj.get('cabin'): continue
    if 'ROOF_' in obj.name or 'DOOR_RIGHT_' in obj.name or 'SIDE_MIRROR' in obj.name or obj.name in ('350Z_BASE_A_slice7_7','350Z_BASE_A_slice8_8'):
        obj.hide_render=True
render('interior-fit.png',(-2.8,2.2,2.3),(0,0,.6))
for obj in bpy.context.scene.objects:
    if obj.type=='MESH' and not obj.get('cabin'): obj.hide_render=True
render('interior-only.png',(-2.2,1.7,1.8),(0,.05,.58))
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'.local/interior-preview.blend'))
