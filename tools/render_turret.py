"""Blender --background --python tools/render_turret.py (local visual QA)."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
BASE=Path(__file__).resolve().parents[1];LOCAL=BASE/'.local';OUTPUT=BASE/'artifacts'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def vec(v):return Vector((v['x'],v['y'],v['z']))
def convert(v):return (v.x,v.z,v.y)
def add_model(folder,turret=False):
    data=json.loads((folder/('turret.json' if turret else 'body.json')).read_text());materials=[]
    for m in data['materials']:
        mat=bpy.data.materials.new(m['name']);mat.use_nodes=True;s=mat.node_tree.nodes.get('Principled BSDF')
        s.inputs['Base Color'].default_value=m['color'];s.inputs['Metallic'].default_value=m['metallic'];s.inputs['Roughness'].default_value=1-m['smoothness']
        if m['texture']>=0:
            node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(folder/(data['textures'][m['texture']]['name']+'.png')),check_existing=True)
            mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=m['color']
            mat.node_tree.links.new(node.outputs['Color'],mix.inputs[1]);mat.node_tree.links.new(mix.outputs[0],s.inputs['Base Color'])
        materials.append(mat)
    for p in data['parts']:
        if p['group']=='audio':continue
        if p.get('optionGroup') in ('engine','trunk') and p['option']!=('0' if p['optionGroup']=='engine' else '2'):continue
        points=[vec(v) for v in p['vertices']];normals=[vec(v) for v in p['normals']]
        if turret:
            role=p['name'].split('|')[0];rig=data['turretRig'];offset=Vector((0,0,0))
            if role!='base':offset+=vec(rig['yawPivot'])
            if role in ('pitch','gun','laser'):offset+=vec(rig['pitchOffset'])
            points=[(v+offset)*.65+Vector((0,1.15,-.32)) for v in points]
        mesh=bpy.data.meshes.new(p['name']);mesh.from_pydata([convert(v) for v in points],[],[p['indices'][i:i+3][::-1] for i in range(0,len(p['indices']),3)]);mesh.update()
        uv=mesh.uv_layers.new()
        for loop in mesh.loops:
            v=p['uv'][loop.vertex_index];uv.data[loop.index].uv=(v['u'],v['v'])
        for poly in mesh.polygons:poly.use_smooth=True
        mesh.normals_split_custom_set_from_vertices([convert(v) for v in normals]);mesh.materials.append(materials[p['material']])
        positions=[(0,0,0)] if p['group']=='body' else [(x,z,.21) for x in (-.8,.8) for z in (1.375,-1.29)]
        for pos in positions:
            obj=bpy.data.objects.new(p['name'],mesh);bpy.context.collection.objects.link(obj);obj.location=pos
            if p['group']!='body' and pos[0]<0:obj.rotation_euler.z=math.pi
add_model(LOCAL/'model');add_model(LOCAL/'turret',True)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.14))
ground=bpy.data.materials.new('Ground');ground.diffuse_color=(.16,.17,.2,1);bpy.context.object.data.materials.append(ground)
for pos,energy in (((2,3,6),1700),((-4,-3,4),1500),((3,-4,3),1000)):
    bpy.ops.object.light_add(type='AREA',location=pos);bpy.context.object.data.energy=energy;bpy.context.object.data.size=4
bpy.ops.object.camera_add();cam=bpy.context.object;s=bpy.context.scene;s.camera=cam;s.render.engine='CYCLES';s.cycles.samples=16
s.render.resolution_x=1100;s.render.resolution_y=800;s.render.resolution_percentage=100;s.world.color=(.2,.2,.2);s.view_settings.view_transform='Filmic';cam.data.lens=48
for name,pos,target in [('car',(-5,6,3.5),(0,0,1)),('detail',(-1.7,2,2.6),(0,0,1.65)),('laser',(-.6,.5,1.92),(0,-.24,1.72))]:
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    s.render.filepath=str(OUTPUT/('turret-'+name+'.png'));bpy.ops.render.render(write_still=True)
