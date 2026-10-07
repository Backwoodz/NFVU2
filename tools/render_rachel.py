"""Run with Blender --background --python tools/render_rachel.py."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
BASE=Path(__file__).resolve().parents[1]
LOCAL=BASE/'.local';OUTPUT=BASE/'artifacts'
model=json.loads((LOCAL/'model/body.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials=[]
for m in model['materials']:
    mat=bpy.data.materials.new(m['name']);mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=m['color']
    shader.inputs['Metallic'].default_value=m['metallic'];shader.inputs['Roughness'].default_value=1-m['smoothness']
    if m['texture']>=0:
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(LOCAL/'model'/(model['textures'][m['texture']]['name']+'.png')),check_existing=True)
        if m['color'][:3]!=[1.,1.,1.]:
            mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=m['color']
            mat.node_tree.links.new(node.outputs['Color'],mix.inputs[1]);mat.node_tree.links.new(mix.outputs[0],shader.inputs['Base Color'])
        else:mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
    materials.append(mat)
for p in model['parts']:
    mesh=bpy.data.meshes.new(p['name']);mesh.from_pydata([(v['x'],v['z'],v['y']) for v in p['vertices']],[],[p['indices'][i:i+3][::-1] for i in range(0,len(p['indices']),3)]);mesh.update()
    uv=mesh.uv_layers.new()
    for loop in mesh.loops:
        v=p['uv'][loop.vertex_index];uv.data[loop.index].uv=(v['u'],v['v'])
    for poly in mesh.polygons:poly.use_smooth=True
    mesh.normals_split_custom_set_from_vertices([(v['x'],v['z'],v['y']) for v in p['normals']])
    mesh.materials.append(materials[p['material']])
    # A loaded suspension extends below the collider's .34 anchor. Rendering
    # at the anchor incorrectly hides the wheels inside the fenders.
    hub=model.get('previewHubHeight',.20)+model.get('wheelVisualLift',0)
    axles=(1.3+model.get('frontWheelVisualOffset',0),-1.35+model.get('rearWheelVisualOffset',0))
    positions=[(0,0,0)] if p['group']=='body' else [(x,y,hub) for x in (-.8,.8) for y in axles]
    for pos in positions:
        obj=bpy.data.objects.new(p['name'],mesh);bpy.context.collection.objects.link(obj);obj.location=pos
        if p['group']!='body' and pos[0]<0:obj.rotation_euler.z=math.pi
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.location.z=model.get('previewHubHeight',.20)-.34
ground=bpy.data.materials.new('Ground');ground.diffuse_color=(.17,.18,.2,1);bpy.context.object.data.materials.append(ground)
for location,energy,size in (((2,3,6),1700,5),((-4,-3,4),1200,4),((3,-4,3),850,3)):
    bpy.ops.object.light_add(type='AREA',location=location);bpy.context.object.data.energy=energy;bpy.context.object.data.size=size
# A low perspective view permits a like-for-like comparison with the supplied
# NFS image. Keep a true side elevation as a separate wheel-fit check.
bpy.ops.object.camera_add(location=(-5,6,2.15));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.50))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='PERSP';camera.data.lens=48
s=bpy.context.scene;s.camera=camera;s.render.engine='CYCLES';s.cycles.samples=32;s.render.resolution_x=1200;s.render.resolution_y=800;s.render.resolution_percentage=100;s.world.color=(.2,.2,.2)
s.view_settings.view_transform='AgX';s.render.filepath=str(OUTPUT/'rachel-vorschau.png')
bpy.ops.wm.save_as_mainfile(filepath=str(LOCAL/'rachel-preview.blend'));bpy.ops.render.render(write_still=True)
camera.location=(5,-6,2.15);camera.rotation_euler=(Vector((0,0,.50))-camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(OUTPUT/'rachel-hinten.png');bpy.ops.render.render(write_still=True)
camera.location=(-7,0,.55);camera.rotation_euler=(Vector((0,0,.55))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=5.1
s.render.filepath=str(OUTPUT/'rachel-seite.png');bpy.ops.render.render(write_still=True)
