"""Local cabin fit review: Blender --background --python tools/render_interior.py -- [--exhaust-style 1..10]."""
import argparse
import base64
import sys
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector, Quaternion

BASE = Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description='Render original cabin fit.')
parser.add_argument('--exhaust-style',type=int,choices=range(1,11),default=8)
parser.add_argument('--shell-review',action='store_true',help='Review intact roof, door edges and rear shell with doors/hatch open.')
parser.add_argument('--front-review',action='store_true',help='Review windscreen fit and the open hood underside.')
parser.add_argument('--door-style',choices=('REG','SUICIDE','SCISSOR'),default='REG')
parser.add_argument('--layout',type=int,choices=range(4),default=2)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
exhaust_style=args.exhaust_style
suffix='' if exhaust_style==8 else f'-exhaust{exhaust_style:02}'
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
            if p['name'] in ('350Z_BASE_A_slice3_3','350Z_BASE_A_slice5_5','350Z_BASE_A_slice6_6'): continue
            if p.get('optionGroup') and p['option'] != {'engine':'0','trunk':str(args.layout),'exhaust':str(exhaust_style)}.get(p['optionGroup']): continue
        mesh=bpy.data.meshes.new(p['name']); mesh.from_pydata([(v['x'],v['z'],v['y']) for v in p['vertices']], [], [p['indices'][i:i+3][::-1] for i in range(0,len(p['indices']),3)]); mesh.update()
        uv=mesh.uv_layers.new()
        for loop in mesh.loops:
            v=p['uv'][loop.vertex_index]; uv.data[loop.index].uv=(v['u'],v['v'])
        for face in mesh.polygons: face.use_smooth=True
        mesh.normals_split_custom_set_from_vertices([(v['x'],v['z'],v['y']) for v in p['normals']])
        mesh.materials.append(materials[p['material']])
        obj=bpy.data.objects.new(p['name'],mesh); bpy.context.collection.objects.link(obj)
        obj['cabin']=cabin; obj['movingPart']=p.get('movingPart','')

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
if args.shell_review or args.front_review:
    if args.front_review:
        render('cabin-front-windscreen.png',(0,2.2,1.6),(0,.5,.97),52)
        render('cabin-front-oblique.png',(-1.5,2.5,1.9),(0,.4,.9),52)
        render('cabin-front-inside.png',(-.35,-.5,1.02),(0,.65,.96),32)
    # Same pivot equation as VehicleShowroom; includes restored original panels.
    model=json.loads((BASE/'.local/model/body.json').read_text())
    tracks={a['name']:a for a in model['animations']}
    for obj in scene.objects:
        role=obj.get('movingPart','')
        if role not in (('hood',) if args.front_review else ('leftDoor','rightDoor','trunk')):continue
        key={'hood':'HOOD_REG','leftDoor':'LEFT_DOOR_'+args.door_style,'rightDoor':'RIGHT_DOOR_'+args.door_style,'trunk':'TRUNK_REG'}[role]
        track=tracks[key];frame=track['frames'][-1];q=frame['rotation']
        rotation=Quaternion((q['w'],q['x'],q['y'],q['z']))
        pivot=Vector(tuple(track['pivot'][k] for k in 'xyz'));offset=Vector(tuple(frame['offset'][k] for k in 'xyz'))
        turned_normals=[]
        for vertex in obj.data.vertices:
            n=rotation@Vector((vertex.normal.x,vertex.normal.z,vertex.normal.y))
            turned_normals.append((n.x,n.z,n.y))
            p=Vector((vertex.co.x,vertex.co.z,vertex.co.y))
            p=pivot+offset+rotation@(p-pivot);vertex.co=(p.x,p.z,p.y)
        obj.data.normals_split_custom_set_from_vertices(turned_normals)
    if args.front_review:
        render('hood-inner-front.png',(0,3.1,1.3),(0,1.1,1.25),48)
        render('hood-inner-oblique.png',(-1.5,2.8,1.45),(0,1.1,1.15),48)
        render('hood-outer-open.png',(1.7,-.4,2.8),(0,1.2,1.25),48)
        sys.exit(0)
    prefix=f'cabin-shell-{args.door_style.lower()}-layout{args.layout}'
    for name,position,target in (
        ('door',(-2.4,-.7,1.5),(-.7,0,.6)),
        ('door-inside',(-1.0,-1.8,1.4),(-1.3,.1,.5)),
        ('trunk',(0,-3.8,2.5),(0,-1.4,.65)),
        ('pillar',(-1.6,-.6,1.5),(-.65,.48,1.0)),
        ('roof',(2.6,2.6,2.7),(0,0,.7)),
        ('headliner',(-.35,-.3,.78),(0,.12,1.18))):
        render(prefix+'-'+name+'.png',position,target,48)
    sys.exit(0)
# Hide only the roof/glass and physical left door for a section through the cabin.
for obj in bpy.context.scene.objects:
    if obj.type!='MESH' or obj.get('cabin'): continue
    if 'ROOF_' in obj.name or 'DOOR_RIGHT_' in obj.name or 'SIDE_MIRROR' in obj.name or obj.name in ('350Z_BASE_A_slice7_7','350Z_BASE_A_slice8_8'):
        obj.hide_render=True
render(f'interior-fit{suffix}.png',(-2.8,2.2,2.3),(0,0,.6))
for obj in bpy.context.scene.objects:
    if obj.type=='MESH' and not obj.get('cabin'): obj.hide_render=True
render(f'interior-only{suffix}.png',(-2.2,1.7,1.8),(0,.05,.58))
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/f'.local/interior-preview{suffix}.blend'))
