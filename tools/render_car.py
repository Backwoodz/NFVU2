from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import bpy, json, math
from pathlib import Path
from mathutils import Vector
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
base=BASE
data=json.loads((LOCAL/'model/body.json').read_text())
green=bpy.data.materials.new('Provisional green');green.diffuse_color=(0.13,0.48,0.035,1)
green.use_nodes=True;bs=green.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(0.13,0.48,0.035,1);bs.inputs['Metallic'].default_value=.6;bs.inputs['Roughness'].default_value=.26
black=bpy.data.materials.new('Black');black.diffuse_color=(.025,.025,.028,1)
for p in data['parts']:
    vs=[(v['x'],v['z'],v['y']) for v in p['vertices']]
    faces=[p['indices'][i:i+3][::-1] for i in range(0,len(p['indices']),3)]
    mesh=bpy.data.meshes.new(p['name']);mesh.from_pydata(vs,[],faces);mesh.update()
    obj=bpy.data.objects.new(p['name'],mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(black if 'BASE' in p['name'] else green)
    for poly in mesh.polygons:poly.use_smooth=True
for x in (-.78,.78):
    for y in (-1.35,1.3):
        bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.32,depth=.22,location=(x,y,.34),rotation=(0,math.pi/2,0));bpy.context.object.data.materials.append(black)
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.location.z=-.01
mat=bpy.data.materials.new('Ground');mat.diffuse_color=(.13,.14,.17,1);bpy.context.object.data.materials.append(mat)
bpy.ops.object.light_add(type='AREA',location=(1,-3,7));bpy.context.object.data.energy=1800;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=6
bpy.ops.object.light_add(type='AREA',location=(-4,4,5));bpy.context.object.data.energy=1200;bpy.context.object.data.size=5
bpy.ops.object.camera_add(location=(5,6,3.6));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.6))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=6.4
s=bpy.context.scene;s.camera=camera;s.render.engine='CYCLES';s.cycles.samples=32;s.render.resolution_x=1100;s.render.resolution_y=700;s.render.resolution_percentage=100;s.world.color=(.18,.18,.18);s.render.filepath=str(OUTPUT/'350z-vorschau.png');bpy.ops.wm.save_as_mainfile(filepath=str(LOCAL/'350z-preview.blend'));bpy.ops.render.render(write_still=True)
