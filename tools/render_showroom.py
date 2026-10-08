"""Blender --background --python tools/render_showroom.py -- [REG|SUICIDE|SCISSOR].

Standalone visual check of imported geometry and the same pivot equation used
by VehicleShowroom. This does not simulate Unity shaders or physics.
"""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector,Quaternion
BASE=Path(__file__).resolve().parents[1];LOCAL=BASE/'.local';OUTPUT=BASE/'artifacts'
model=json.loads((LOCAL/'model/body.json').read_text())
style=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'SCISSOR'
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
layout=int(args[1]) if len(args)>1 else 2
engine=int(args[2]) if len(args)>2 else 0
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
def v(d):return Vector((d['x'],d['y'],d['z']))
def convert(vec):return (vec.x,vec.z,vec.y)
animations={a['name']:a for a in model['animations']}
tracks={'leftDoor':'LEFT_DOOR_'+style,'rightDoor':'RIGHT_DOOR_'+style,'hood':'HOOD_REG','trunk':'TRUNK_REG'}
def add(part,slot=None,hub=None):
    points=[v(p) for p in part['vertices']];normals=[v(p) for p in part['normals']]
    if slot:
        right,up,forward=[v(slot[n]) for n in ('right','up','forward')]
        def turn(p):return right*p.x+up*p.y+forward*p.z
        points=[v(slot['position'])+turn(p) for p in points];normals=[turn(p) for p in normals]
    if part.get('movingPart'):
        a=animations[tracks[part['movingPart']]];frame=a['frames'][-1];q=frame['rotation'];rotation=Quaternion((q['w'],q['x'],q['y'],q['z']));pivot=v(a['pivot'])
        points=[pivot+v(frame['offset'])+rotation@(p-pivot) for p in points];normals=[rotation@n for n in normals]
    if hub:
        points=[Vector((p.x if hub[0]>0 else -p.x,p.y,p.z if hub[0]>0 else -p.z))+Vector(hub) for p in points]
        normals=[Vector((p.x if hub[0]>0 else -p.x,p.y,p.z if hub[0]>0 else -p.z)) for p in normals]
    mesh=bpy.data.meshes.new(part['name']);mesh.from_pydata([convert(p) for p in points],[],[part['indices'][i:i+3][::-1] for i in range(0,len(part['indices']),3)]);mesh.update()
    uv=mesh.uv_layers.new()
    for loop in mesh.loops:
        tex=part['uv'][loop.vertex_index];uv.data[loop.index].uv=(tex['u'],tex['v'])
    for poly in mesh.polygons:poly.use_smooth=True
    mesh.normals_split_custom_set_from_vertices([convert(n) for n in normals]);mesh.materials.append(materials[part['material']])
    obj=bpy.data.objects.new(part['name'],mesh);bpy.context.collection.objects.link(obj)
for p in model['parts']:
    if p['group']=='audio':continue
    if p.get('optionGroup') in ('engine','trunk') and p['option']!=str(engine if p['optionGroup']=='engine' else layout):continue
    if p['group']=='body':add(p)
    else:
        for x in (-.8,.8):
            for z in (1.375,-1.29):add(p,hub=(x,.21,z))
for i,slot in enumerate(model['trunkLayouts'][layout]['sockets']):
    choices=[c for c in model['audioComponents'] if c['size']==slot['size']]
    # Show two default woofers and the selectable amp/LCD in this preview.
    component=choices[[1,2,5,9,3,4][i]]
    for p in model['parts']:
        if p['group']=='audio' and p['option']==component['id']:add(p,slot=slot)
neon=bpy.data.materials.new('TrunkGlow');neon.use_nodes=True
shader=neon.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(.1,1,.03,1);shader.inputs['Emission'].default_value=(.1,1,.03,1)
for x in (-.59,.59):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(x,-1.43,.68));bpy.context.object.scale=(.012,.72,.012);bpy.context.object.data.materials.append(neon)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.14))
ground=bpy.data.materials.new('Ground');ground.diffuse_color=(.15,.16,.18,1);bpy.context.object.data.materials.append(ground)
for location,energy,size in (((2,3,6),1700,5),((-4,-3,4),1600,4),((3,-4,3),1000,3)):
    bpy.ops.object.light_add(type='AREA',location=location);bpy.context.object.data.energy=energy;bpy.context.object.data.size=size
bpy.ops.object.camera_add();camera=bpy.context.object;s=bpy.context.scene;s.camera=camera;s.render.engine='CYCLES';s.cycles.samples=16
s.render.resolution_x=1100;s.render.resolution_y=800;s.render.resolution_percentage=100;s.world.color=(.2,.2,.2)
s.view_settings.view_transform='Filmic';camera.data.lens=45
for name,location,target in [('rear',(3.6,-5.3,3.1),(0,-.7,.8)),('engine',(-3,4.2,2.5),(0,.8,.6)),('trunk',(1.7,-3.2,2.6),(0,-1.4,.65))]:
    camera.location=location;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    s.render.filepath=str(OUTPUT/f'showroom-{style.lower()}-{name}.png');bpy.ops.render.render(write_still=True)
