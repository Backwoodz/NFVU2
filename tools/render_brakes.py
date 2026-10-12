"""Blender brake geometry preview (outside, inside, and installed wheel)."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
BASE=Path(__file__).resolve().parents[1];LOCAL=BASE/'.local';OUTPUT=BASE/'artifacts'
model=json.loads((LOCAL/'model/body.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials=[]
for m in model['materials']:
    mat=bpy.data.materials.new(m['name']);mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=m['color']
    shader.inputs['Metallic'].default_value=m['metallic'];shader.inputs['Roughness'].default_value=1-m['smoothness']
    if m['texture']>=0:
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(LOCAL/'model'/(model['textures'][m['texture']]['name']+'.png')),check_existing=True)
        mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=m['color']
        mat.node_tree.links.new(node.outputs['Color'],mix.inputs[1]);mat.node_tree.links.new(mix.outputs[0],shader.inputs['Base Color'])
    materials.append(mat)

objects=[]
for part in model['parts']:
    if part['group'] not in ('brake','wheel'):continue
    mesh=bpy.data.meshes.new(part['name'])
    mesh.from_pydata([(v['x'],v['z'],v['y']) for v in part['vertices']],[],[part['indices'][i:i+3][::-1] for i in range(0,len(part['indices']),3)]);mesh.update()
    uv=mesh.uv_layers.new()
    for loop in mesh.loops:
        t=part['uv'][loop.vertex_index];uv.data[loop.index].uv=(t['u'],t['v'])
    for poly in mesh.polygons:poly.use_smooth=True
    mesh.normals_split_custom_set_from_vertices([(n['x'],n['z'],n['y']) for n in part['normals']])
    mesh.materials.append(materials[part['material']])
    obj=bpy.data.objects.new(part['name'],mesh);bpy.context.collection.objects.link(obj);objects.append((obj,part['group']))
for pos,energy in (((2,-1,3),220),((-2,1,2),160),((1,2,0),80)):
    bpy.ops.object.light_add(type='AREA',location=pos);bpy.context.object.data.energy=energy;bpy.context.object.data.size=2
bpy.ops.object.camera_add();camera=bpy.context.object;s=bpy.context.scene;s.camera=camera;s.render.engine='BLENDER_EEVEE';s.eevee.use_gtao=True;s.eevee.gtao_distance=.08
s.render.resolution_x=900;s.render.resolution_y=900;s.render.resolution_percentage=100;s.world.color=(.2,.2,.2)
s.view_settings.view_transform='Filmic';camera.data.type='ORTHO'
tag=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'current'
for name,pos,scale in [('outer',(1,0,0),.48),('oblique',(1,-.65,.4),.48),('inner',(-1,0,0),.48),('wheel',(1,-.18,.08),.78)]:
    for obj,group in objects:obj.hide_render=group=='wheel' and name!='wheel'
    camera.data.ortho_scale=scale;camera.location=pos;camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
    s.render.filepath=str(OUTPUT/f'brake-{tag}-{name}.png');bpy.ops.render.render(write_still=True)
