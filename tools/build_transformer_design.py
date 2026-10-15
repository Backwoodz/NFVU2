"""Build study 09/10: static robot design from the actual study-08 meshes.

Run with Blender --background --threads 2 --python this_file.py -- --render.
Add --revision 10 for the exposed lights, shoulder wheels and revised head/legs.
This is deliberately a new end-pose study. No old transformation is presented
as valid for this substantially different body. Original model files and v8
remain untouched; new mechanism packing and animation are separate next work.
"""
import argparse
import hashlib
import json
import math
import sys
from collections import defaultdict
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from transformer_parts import split, area

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--render',action='store_true')
parser.add_argument('--turntable',action='store_true',help='Render a camera orbit, not a transformation')
parser.add_argument('--revision',type=int,choices=(9,10,11,12,13,14,15,16),default=9)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
SOURCE=ROOT/'artifacts/transformer-study-v8/350z-transformer-study.blend'
OUT=ROOT/('artifacts/transformer-study-v%d'%args.revision)
OUT.mkdir(parents=True,exist_ok=True)
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene
scene.frame_set(121)
bpy.context.view_layer.update()
originals=bpy.data.collections['01 Original 350Z surfaces']
hardware=bpy.data.collections['02 Authored mechanism - BLOCKOUT']
controls=bpy.data.collections['03 Module controls']
rider=bpy.data.collections['05 Rider - scale reference, NOT game avatar']
groups=defaultdict(list)
old_pose={}
for obj in originals.objects:
    groups[obj['module']].append(obj)
    old_pose[obj['module']]=obj.parent.matrix_world.copy()
original_fingerprints={o.name:hashlib.sha256(repr((list(map(tuple,(v.co for v in o.data.vertices))),
    [tuple(p.vertices) for p in o.data.polygons],
    [tuple(u.uv) for u in o.data.uv_layers.active.data])).encode()).hexdigest() for o in originals.objects}
tubes=[o for o in hardware.objects if o.get('continuous_unsegmented')]
tube_geometry={o.name:[tuple(v.co) for v in o.data.vertices] for o in tubes}
rider_world={o:o.matrix_world.copy() for o in rider.objects}
# Separate source surfaces from the old rig before replacing its authored parts.
for obj in list(originals.objects)+tubes:
    local=obj.matrix_local.copy()
    obj.parent=None;obj.animation_data_clear();obj.constraints.clear();obj.matrix_world=local
for obj,world in rider_world.items():
    obj.parent=None;obj.animation_data_clear();obj.constraints.clear();obj.matrix_world=world
for obj in list(hardware.objects):
    if obj not in tubes:bpy.data.objects.remove(obj,do_unlink=True)
for obj in list(controls.objects):bpy.data.objects.remove(obj,do_unlink=True)
for marker in list(scene.timeline_markers):scene.timeline_markers.remove(marker)
scene.frame_start=scene.frame_end=1;scene.frame_set(1)
for action in list(bpy.data.actions):
    if action.users==0:bpy.data.actions.remove(action)

def material(name,color,metallic=.75,roughness=.3,emission=None):
    mat=bpy.data.materials.new('V9 - '+name);mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Metallic'].default_value=metallic
    shader.inputs['Roughness'].default_value=roughness
    if emission:shader.inputs['Emission'].default_value=(*emission,1)
    mat.diffuse_color=(*color,1)
    return mat

dark=material('recessed frame',(.026,.035,.044),.72,.38)
steel=material('brushed titanium',(.18,.22,.26),.78,.34)
edge=material('machined edges',(.44,.49,.51),.85,.24)
paint=material('green casing',(.035,.30,.075),.65,.30)
rubber=material('joint boots',(.012,.017,.020),.10,.68)
copper=material('bearing alloy',(.28,.15,.055),.8,.3)
optic=material('dragon optics',(.035,.6,.10),.15,.24,(.12,3,.30))
for mat in (dark,steel,edge,copper):
    shader=mat.node_tree.nodes.get('Principled BSDF')
    noise=mat.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=125
    noise.inputs['Detail'].default_value=2
    bump=mat.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.10;bump.inputs['Distance'].default_value=.005
    mat.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height'])
    mat.node_tree.links.new(bump.outputs['Normal'],shader.inputs['Normal'])

def quat(axis,angle):return Quaternion(axis,math.radians(angle))
RX=quat((1,0,0),-90)
def node(name,position=(0,0,0),rotation=None):
    obj=bpy.data.objects.new('DESIGN - '+name,None);controls.objects.link(obj)
    obj.location=position;obj.rotation_mode='QUATERNION';obj.rotation_quaternion=rotation or Quaternion()
    return obj
root=node('robot end pose - no transformation yet')

def mesh(name,vertices,faces,mat,parent=None,bevel=0):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    obj=bpy.data.objects.new('V9 - '+name,data);hardware.objects.link(obj)
    data.materials.append(mat);obj.parent=parent or root
    obj['provenance']='Authored robot mechanism; car-mode packing not yet established'
    if bevel:
        mod=obj.modifiers.new('machined bevel','BEVEL');mod.width=bevel;mod.segments=2
    return obj

def box(name,pos,size,mat,parent=None,bevel=.012):
    verts=[(x*size[0]/2,y*size[1]/2,z*size[2]/2) for x,y,z in
           [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    obj=mesh(name,verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat,parent,bevel)
    obj.location=pos;return obj

def rod(name,a,b,radius,mat,parent=None,vertices=12):
    a,b=Vector(a),Vector(b);length=(b-a).length
    points=[(radius*math.cos(i*2*math.pi/vertices),radius*math.sin(i*2*math.pi/vertices),z)
            for z in (0,length) for i in range(vertices)]
    faces=[tuple(range(vertices-1,-1,-1)),tuple(range(vertices,vertices*2))]
    faces += [(i,(i+1)%vertices,(i+1)%vertices+vertices,i+vertices) for i in range(vertices)]
    obj=mesh(name,points,faces,mat,parent)
    obj.location=a;obj.rotation_mode='QUATERNION';obj.rotation_quaternion=(b-a).to_track_quat('Z','Y')
    return obj

def ring(name,center,radius,thickness,axis,mat,parent=None):
    points=[];faces=[]
    for i in range(24):
        a=i*2*math.pi/24
        for j in range(6):
            b=j*2*math.pi/6;r=radius+thickness*math.cos(b)
            points.append((r*math.cos(a),r*math.sin(a),thickness*math.sin(b)))
    for i in range(24):
        for j in range(6):faces.append((i*6+j,((i+1)%24)*6+j,((i+1)%24)*6+(j+1)%6,i*6+(j+1)%6))
    obj=mesh(name,points,faces,mat,parent);obj.location=center
    obj.rotation_mode='QUATERNION';obj.rotation_quaternion=Vector(axis).to_track_quat('Z','Y')
    return obj

def loft(name,sections,mat,parent=None,bevel=.012):
    # Chamfered cross-sections give each body member an actual changing profile.
    points=[]
    for z,w,d in sections:
        points.extend([(x*w,y*d,z) for x,y in [(-.65,-1),(.65,-1),(1,-.6),(1,.5),(.55,1),(-.55,1),(-1,.5),(-1,-.6)]])
    faces=[tuple(range(7,-1,-1)),tuple(range(len(points)-8,len(points)))]
    for r in range(len(sections)-1):
        for i in range(8):faces.append((r*8+i,r*8+(i+1)%8,(r+1)*8+(i+1)%8,(r+1)*8+i))
    return mesh(name,points,faces,mat,parent,bevel)

def plate(name,outline,depth,mat,parent=None):
    # Outline in XYZ, with thickness behind its front surface along local Y.
    n=len(outline);vertices=list(outline)+[(x,y-depth,z) for x,y,z in outline]
    faces=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vertices,faces,mat,parent,.008)

modules={}
for role,objects in groups.items():
    n=node(role);n.parent=root;n.matrix_world=old_pose[role];modules[role]=n
    for obj in objects:obj.parent=n

def pose(role,position,rotation=None):
    obj=modules[role];obj.location=position;obj.rotation_quaternion=rotation or Quaternion()

# Split only the carbon hood surfaces. Normals and UVs are interpolated by the
# same lossless polygon cutter as the existing study, never by reprojecting them.
hood_area_before=hood_area_after=0.
hood_fragments=defaultdict(list)
for side in ('L','R'):
    for obj in list(groups['chest_'+side]):
        data=obj.data;data.calc_normals_split()
        for poly in data.polygons:
            loops=list(poly.loop_indices)
            for j in range(1,len(loops)-1):
                tri=[]
                for i in (loops[0],loops[j],loops[j+1]):
                    loop=data.loops[i]
                    tri.append(tuple(data.vertices[loop.vertex_index].co)+tuple(loop.normal)+tuple(data.uv_layers.active.data[i].uv))
                hood_area_before+=area(tri);polys=[tri]
                for cut in (-.224,.224):polys=[part for polygon in polys for part in split(polygon,1,cut)]
                for polygon in polys:
                    for k in range(1,len(polygon)-1):
                        fragment=[polygon[0],polygon[k],polygon[k+1]]
                        if area(fragment)<1e-12:continue
                        band=sum(sum(v[1] for v in fragment)/3>cut for cut in (-.224,.224))
                        hood_fragments[side,band,obj.name].append(fragment)
                        hood_area_after+=area(fragment)
    for obj in list(groups['chest_'+side]):bpy.data.objects.remove(obj,do_unlink=True)
    groups.pop('chest_'+side)
    bpy.data.objects.remove(modules.pop('chest_'+side),do_unlink=True)
assert abs(hood_area_before-hood_area_after)<1e-6
for side in ('L','R'):
    sign=1 if side=='L' else -1
    for band in range(3):
        role='hood_%d_%s'%(band,side)
        verts=[v for (s,b,name),tris in hood_fragments.items() if s==side and b==band for tri in tris for v in tri]
        center=Vector(tuple((min(v[a] for v in verts)+max(v[a] for v in verts))/2 for a in range(3)))
        n=node(role);n.parent=root;modules[role]=n
        pose(role,(sign*[.46,.54,.47][band],[.56,.40,.24][band],[3.56,3.19,2.86][band]),
             quat((0,0,1),-sign*[30,53,68][band]) @ quat((0,1,0),-sign*[22,14,10][band]) @ quat((1,0,0),-72))
        for (s,b,name),tris in hood_fragments.items():
            if s!=side or b!=band:continue
            points=[v for tri in tris for v in tri]
            data=bpy.data.meshes.new(name+' segment '+str(band))
            data.from_pydata([tuple(Vector(v[:3])-center) for v in points],[],[tuple(range(i,i+3)) for i in range(0,len(points),3)])
            data.update();uv=data.uv_layers.new(name='Original UV')
            for loop in data.loops:uv.data[loop.index].uv=points[loop.vertex_index][6:8]
            for p in data.polygons:p.use_smooth=True
            data.use_auto_smooth=True;data.normals_split_custom_set_from_vertices([tuple(Vector(v[3:6]).normalized()) for v in points])
            old_data=bpy.data.meshes.get(name)
            if old_data and old_data.materials:data.materials.append(old_data.materials[0])
            obj=bpy.data.objects.new(name+'__band'+str(band),data);originals.objects.link(obj);obj.parent=n
            obj['module']=role;obj['source_part']=name.split('__')[0]
            obj['source_module']='chest_'+side;obj['source_local_pivot']=list(center)
            obj['provenance']='Study-08 hood split into three rigid sections per side; area and UV preserved'
            groups[role].append(obj)

# Anatomical pose. Original exterior meshes keep unit scale and source shapes.
for side,sign in [('L',1),('R',-1)]:
    shoulder=Vector((sign*1.07,0,3.53));elbow=Vector((sign*1.37,.05,2.88));wrist=Vector((sign*1.30,.39,2.24))
    hip=Vector((sign*.40,-.03,2.16));knee=Vector((sign*.68,.28,1.22));ankle=Vector((sign*.88,-.02,.34))
    door=quat((0,0,1),sign*64) @ RX
    pose('door_rear_'+side,(sign*1.33,.24,3.17),door)
    pose('door_front_'+side,(sign*1.40,.45,2.56),quat((0,1,0),sign*8) @ door)
    pose('lamp_'+side,(sign*.99,.54,3.66),quat((0,1,0),-sign*26) @ quat((1,0,0),-70))
    pose('front_wheel_'+side,(sign*1.22,-.32,3.60),RX)
    pose('rear_wheel_'+side,(sign*.99,-.16,.88),RX)
    bpy.context.view_layer.update()
    for role,base in [('fender_front_','front_wheel_'),('fender_back_','front_wheel_'),('rear_arch_','rear_wheel_'),('mirror_','door_front_')]:
        name=role+side;anchor=base+side
        # Preserve bolted fender/wheel and mirror/door relationships where used.
        relative=old_pose[anchor].inverted() @ old_pose[name]
        if role=='rear_arch_':relative.translation.x+=sign*.30
        modules[name].matrix_world=modules[anchor].matrix_world @ relative
    # The upper fender folds over the wheel rather than remaining an upright flag.
    pose('fender_back_'+side,(sign*1.32,-.22,3.78),quat((0,1,0),sign*48) @ RX)
    rod('folded fender hinge', (sign*1.22,-.32,3.60),(sign*1.32,-.22,3.78),.038,steel)
    pose('rear_corner_'+side,(sign*.58,.10,1.95),quat((0,1,0),sign*13) @ quat((1,0,0),82))
    pose('hipguard_'+side,(sign*.44,.23,2.23),quat((0,1,0),sign*18) @ RX)
    for index,z in [(0,2.0),(1,1.54),(2,.87),(3,2.42)]:
        role='sill%d_%s'%(index,side)
        if role in modules:pose(role,(sign*(.54 if index in (0,3) else .74),.10,z),door)
    for index in range(3):pose('floor%d_%s'%(index,side),(sign*[.57,.87,.58][index],[-.19,-.37,-.36][index],[1.76,.93,1.85][index]),quat((0,1,0),-sign*15) @ RX)
    pose('toe_'+side,(sign*.88,.39,.27),quat((0,0,1),-sign*8))
    pose('shinface_'+side,(sign*.88,.28,.74),quat((0,1,0),sign*10))
    pose('taillamp_'+side,(sign*1.04,-.27,1.12),quat((0,0,1),180))
    pose('exhaust_'+side,(sign*.58,-.62,2.68),quat((1,0,0),10))
    pose('window_'+side,(sign*.79,-.63,3.18),quat((0,0,1),sign*7) @ RX)
    pose('quarterglass_'+side,(sign*.59,-.58,2.54),quat((1,0,0),90))
    pose('roof_'+side,(sign*.39,-.66,3.20),quat((0,0,1),sign*14) @ RX)
    pose('apron_front_'+side,(sign*.52,-.32,2.63),quat((0,0,1),sign*50) @ RX)
    pose('apron_back_'+side,(sign*.56,-.33,3.38),quat((0,0,1),sign*50) @ RX)
    pose('belt_'+side,(sign*.48,-.61,2.23),quat((0,1,0),90) @ RX)

    # Sculpted limb volumes, articulated bearings and visible paired actuators.
    for name,a,b,width,depth in [('upperarm',shoulder,elbow,.27,.23),('forearm',elbow,wrist,.29,.24),
                                ('thigh',hip,knee,.33,.28),('shin',knee,ankle,.27,.25)]:
        length=(b-a).length
        frame=node(name+' '+side,a,(b-a).to_track_quat('Z','Y'));frame.parent=root
        loft(name+' inner core '+side,[(0,width*.70,depth*.70),(.20*length,width,depth),(.70*length,width*.86,depth*.85),(length,width*.62,depth*.65)],dark,frame)
        for direction in (-1,1):
            rod(name+' piston casing', (direction*width*.66,-depth*.45,.12),(direction*width*.66,-depth*.45,length*.63),.045,steel,frame)
            rod(name+' chrome ram', (direction*width*.66,-depth*.45,length*.52),(direction*width*.66,-depth*.45,length-.03),.022,edge,frame)
        for row in range(3):
            z=length*(.22+row*.20);w=width*(1-row*.10)
            plate(name+' overlapping armor', [(-w,depth*.62,z-.10),(-w*.80,depth*.93,z+.12),(w*.80,depth*.93,z+.12),(w,depth*.62,z-.10),(0,depth*1.10,z-.16)],.07,steel if row%2==0 else dark,frame)
        ring(name+' end bearing',b,width*.65,.036,(1,0,0),edge)
        ring(name+' bearing recess',b,width*.48,.025,(1,0,0),copper)
        for i in range(6):
            angle=i*math.pi/3
            rod('bearing stud',(b.x+sign*.12,b.y+math.sin(angle)*width*.54,b.z+math.cos(angle)*width*.54),
                (b.x+sign*.17,b.y+math.sin(angle)*width*.54,b.z+math.cos(angle)*width*.54),.021,dark)

    loft('shoulder crown '+side,[(0,.29,.30),(.16,.40,.34),(.37,.32,.27),(.46,.19,.20)],steel).location=(sign*1.08,-.02,3.42)
    shoulder_blade=plate('shoulder green prow '+side,[(sign*.78,.24,3.72),(sign*.91,.23,4.00),(sign*1.29,.12,3.91),(sign*1.43,.15,3.60),(sign*1.16,.33,3.49)],.07,paint)
    rod('clavicle actuator',(sign*.27,-.02,3.54),shoulder,.11,steel)
    ring('shoulder gimbal',shoulder,.24,.045,(1,0,0),dark)
    # Tapered knee protection and independently modelled mechanical knuckles.
    plate('knee cap '+side,[(knee.x-.19,knee.y+.18,knee.z+.17),(knee.x,knee.y+.28,knee.z+.26),(knee.x+.19,knee.y+.18,knee.z+.17),(knee.x+.14,knee.y+.30,knee.z-.16),(knee.x,knee.y+.34,knee.z-.22),(knee.x-.14,knee.y+.30,knee.z-.16)],.10,paint)
    rod('knee front hub',knee+Vector((0,.24,0)),knee+Vector((0,.35,0)),.105,dark)
    ring('knee front hub rim',knee+Vector((0,.36,0)),.082,.018,(0,1,0),edge)
    for vent in range(5):
        z=3.47+vent*.055
        box('shoulder ventilation',(sign*1.12,.29,z),(.21,.035,.022),dark,bevel=.003)
    palm=box('hand palm '+side,wrist+Vector((0,.03,-.14)),(.27,.20,.31),dark,bevel=.045)
    for finger in range(4):
        x=wrist.x+(finger-1.5)*.068
        a=Vector((x,wrist.y+.08,wrist.z-.24));b=a+Vector((0,.08,-.11));c=b+Vector((0,-.01,-.10))
        rod('finger proximal',a,b,.035,steel);rod('finger distal',b,c,.031,dark)
        ring('finger joint',b,.032,.008,(1,0,0),edge)
    rod('opposing thumb',wrist+Vector((-sign*.16,.04,-.09)),wrist+Vector((-sign*.19,.16,-.25)),.055,steel)
    # Broad shaped feet instead of rectangular display bases.
    foot=node('foot structure '+side,(sign*.88,.04,0));foot.parent=root
    box('compact heel '+side,(0,-.18,.10),(.46,.32,.19),dark,foot,.035)
    for digit in (-1,1):
        footprint=[(digit*.17-.12,-.03),(digit*.17+.12,-.03),(digit*.17+.13,.48),(digit*.17+.08,.73),(digit*.17-.08,.73),(digit*.17-.13,.48)]
        verts=[(x,y,.006) for x,y in footprint]+[(x,y,z) for (x,y),z in zip(footprint,[.19,.19,.14,.075,.075,.14])]
        mesh('separate load bearing toe '+side,verts,[(5,4,3,2,1,0),(6,7,8,9,10,11)]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)],dark,foot,.010)
        box('toe crown '+side,(digit*.17,.48,.15),(.20,.28,.045),steel,foot,.018)
        for y in (.40,.49,.58):box('toe tread seam',(digit*.17,y,.179),(.18,.014,.015),rubber,foot,.003)
    for toe in (-1,1):
        plate('toe armor '+side,[(toe*.16-.12,.40,.10),(toe*.16-.10,.68,.08),(toe*.16+.10,.68,.08),(toe*.16+.12,.38,.23)],.06,steel,foot)
    rod('ankle load strut',(sign*.88,-.05,.17),ankle,.13,dark)

pose('hatch',(0,-1.02,3.18),quat((1,0,0),75))
pose('windscreen',(0,-.89,3.40),quat((1,0,0),112))
bpy.context.view_layer.update()
modules['spoiler'].matrix_world=modules['hatch'].matrix_world @ old_pose['hatch'].inverted() @ old_pose['spoiler']
pose('pelvis_rear',(0,-.50,2.12),quat((0,0,1),180) @ RX)
pose('aerial',(.64,-.68,3.66),Quaternion())

# Continuous, substantial torso: broad rib cage tapers to a compact waist.
loft('rib cage',[(2.66,.38,.24),(2.95,.57,.33),(3.30,.74,.36),(3.57,.64,.30),(3.72,.35,.22)],dark)
loft('pelvic casting',[(1.99,.37,.23),(2.13,.54,.29),(2.37,.42,.27),(2.47,.30,.22)],dark)
for row in range(4):
    z=2.43+row*.155;w=.27+row*.065
    plate('abdominal chevron',[(-w,.31,z+.10),(0,.43,z+.02),(w,.31,z+.10),(w*.82,.32,z-.025),(0,.44,z-.09),(-w*.82,.32,z-.025)],.055,steel if row%2==0 else dark)
    for sign in (-1,1):
        rod('lateral rib',(sign*(w+.04),-.05,z-.04),(sign*(w+.10),.22,z+.08),.045,edge)
for sign in (-1,1):
    rod('lumbar cylinder',(sign*.26,-.18,2.15),(sign*.47,-.14,2.86),.067,steel)
    rod('lumbar polished shaft',(sign*.40,-.14,2.75),(sign*.55,-.10,3.08),.036,edge)
    plate('hip vane',[(sign*.22,.29,2.27),(sign*.47,.30,2.35),(sign*.64,.18,2.06),(sign*.39,.37,1.97)],.065,steel)
    for row in range(4):
        rib=box('pectoral seam',(sign*(.45+row*.065),.36,3.40-row*.095),(.27,.07,.035),edge)
        rib.rotation_euler.y=sign*.30
    for row in range(3):
        z=3.47-row*.16
        plate('sternal rib blade',[(sign*.05,.47,z+.08),(sign*.34,.42,z+.13),(sign*.43,.39,z+.015),(sign*.10,.51,z-.06)],.05,steel)
        rod('sternal rib actuator',(sign*.10,.44,z-.03),(sign*.34,.36,z+.10),.022,copper)
plate('sternum keel',[(-.075,.49,3.57),(0,.58,3.65),(.075,.49,3.57),(.055,.56,3.03),(0,.57,2.94),(-.055,.56,3.03)],.09,dark)
for z in (3.10,3.25,3.40,3.55):
    box('sternum interlock',(0,.578,z),(.10,.035,.055),edge,bevel=.008)

# Defined helmet and face, clear of the new smaller chest segments.
head=node('dragon helmet',(0,.10,3.92));head.parent=root;head.scale=(.88,.88,.88)
loft('helmet core',[(0,.16,.15),(.12,.24,.19),(.40,.30,.23),(.61,.23,.18),(.72,.08,.12)],dark,head)
for sign in (-1,1):
    plate('cheek blade',[(sign*.07,.27,.13),(sign*.20,.30,.20),(sign*.28,.24,.42),(sign*.17,.32,.36)],.075,steel,head)
    plate('brow ridge',[(sign*.035,.32,.47),(sign*.26,.27,.54),(sign*.29,.20,.59),(sign*.09,.24,.54)],.08,dark,head)
    plate('eye socket',[(sign*.045,.342,.457),(sign*.26,.286,.505),(sign*.20,.318,.396),(sign*.075,.353,.408)],.022,rubber,head)
    plate('eye aperture',[(sign*.065,.360,.444),(sign*.235,.314,.481),(sign*.14,.350,.420)],.012,optic,head)
    plate('temple fin',[(sign*.23,.04,.39),(sign*.35,-.06,.53),(sign*.33,-.18,.74),(sign*.24,.02,.66)],.08,steel,head)
    ring('temple bearing',(sign*.275,-.005,.37),.10,.028,(1,0,0),copper,head)
    for j in range(3):
        vent=box('jaw vent',(sign*(.10+j*.035),.264,.22+j*.055),(.023,.03,.085),dark,head,.003)
        vent.rotation_euler.y=sign*-.25
    rod('cheek jaw ram',(sign*.24,.19,.32),(sign*.13,.28,.12),.026,copper,head)
    rod('jaw ram piston',(sign*.16,.26,.19),(sign*.085,.33,.09),.014,edge,head)
    plate('orbital cheek plate',[(sign*.13,.34,.39),(sign*.24,.28,.42),(sign*.20,.34,.29),(sign*.08,.37,.28)],.026,steel,head)
    plate('mandible segment',[(sign*.05,.36,.18),(sign*.17,.31,.20),(sign*.20,.26,.13),(sign*.095,.31,.035)],.045,steel,head)
    for z in (.48,.54,.60):
        box('temple vent',(sign*.27,-.015,z),(.045,.16,.025),edge,head,.005)
plate('nasal bridge',[(-.033,.36,.45),(0,.405,.49),(.033,.36,.45),(.042,.40,.30),(0,.43,.265),(-.042,.40,.30)],.05,steel,head)
plate('mouth surround',[(-.10,.35,.255),(.10,.35,.255),(.115,.34,.13),(0,.38,.065),(-.115,.34,.13)],.05,dark,head)
for row in range(3):box('mouth segmented grille',(0,.382,.165+row*.027),(.15,.018,.012),steel,head,.003)
for x in (-.045,0,.045):box('mouth grille channel',(x,.394,.195),(.013,.012,.08),rubber,head,.002)
plate('chin plate',[(-.065,.36,.12),(0,.402,.145),(.065,.36,.12),(.045,.36,.035),(0,.38,.015),(-.045,.36,.035)],.035,steel,head)
plate('helmet crest',[(-.045,.21,.60),(0,.16,.75),(.045,.21,.60),(0,.31,.52)],.08,paint,head)
rod('neck spine',(0,.04,3.73),(0,.10,4.08),.12,steel)
for z in (3.79,3.84,3.89):ring('neck vertebra',(0,.08,z),.14,.028,(0,0,1),dark)

# Keep the same reference figure, move it behind the robot's neck, and give its
# seat a supported shoulder bridge. The actual game Player is not in this file.
seat=node('retained rider and shoulder saddle',(0,-.94,4.35));seat.parent=root
old_hips=Vector((0,-.62,4.46))
for obj,world in rider_world.items():
    obj.parent=seat;obj.matrix_parent_inverse=Matrix.Identity(4)
    obj.matrix_basis=Matrix.Translation(-old_hips) @ world
cushion=box('shoulder saddle cushion',(0,-.975,4.205),(.40,.30,.12),rubber,bevel=.04)
cushion.rotation_euler.x=-.20
box('saddle back',(0,-1.13,4.43),(.43,.08,.34),dark,bevel=.025)
for sign in (-1,1):
    rod('saddle side frame',(sign*.30,-.45,3.50),(sign*.25,-1.06,4.25),.047,steel)
    rod('saddle cross brace',(sign*.65,-.38,3.66),(sign*.25,-1.06,4.25),.035,dark)
    rod('rider handle outrigger',(sign*.25,-1.06,4.25),(sign*.55,-1.04,4.27),.027,steel)
    rod('rider handle support',(sign*.55,-1.04,4.27),(sign*.46,-.60,4.425),.022,steel)
    rod('rider grip crossbar',(sign*.46,-.60,4.425),(sign*.34,-.60,4.49),.020,steel)
    box('rider hand grip',(sign*.34,-.60,4.49),(.055,.14,.05),rubber)

# Reuse every original continuous tube mesh exactly once in protected channels.
neon_layout={
 'underbody side L':((1.12,-.61,1.34),(.14,-.05,1)),
 'underbody side R':((-1.12,-.61,1.34),(-.14,-.05,1)),
 'underbody front':((0,-.67,3.63),(1,0,0)),
 'underbody rear':((0,-.69,2.26),(1,0,0)),
 'trunk L':((.84,.19,3.94),(1,-.05,-.22)),
 'trunk R':((-.84,.19,3.94),(-1,-.05,-.22)),
 'trunk rear':((0,.44,2.48),(1,0,0))}
for tube in tubes:
    key=tube.name.split('NEON TUBE - ')[1];position,axis=neon_layout[key]
    z_axis=Vector(axis).normalized()
    opening=Vector((0,-1 if 'underbody' in key else 1,0))
    y_axis=(opening-z_axis*opening.dot(z_axis)).normalized()
    x_axis=y_axis.cross(z_axis).normalized()
    orientation=Matrix((x_axis,y_axis,z_axis)).transposed().to_quaternion()
    base=node('protected neon '+key,position,orientation);base.parent=root
    tube.parent=base;tube.matrix_basis=Matrix.Identity(4)
    length=max(v.co.z for v in tube.data.vertices)-min(v.co.z for v in tube.data.vertices)
    box('neon channel '+key,(0,-.027,0),(.068,.042,length+.07),dark,base,.005)
    for sign in (-1,1):
        box('neon channel edge',(sign*.032,0,0),(.014,.031,length+.065),steel,base,.003)
        box('neon end cap',(0,0,sign*(length/2+.018)),(.066,.045,.033),steel,base,.004)
        box('neon clamp',(0,.001,sign*length*.36),(.069,.035,.025),dark,base,.004)
        endpoint=Vector(position)+base.rotation_quaternion @ Vector((0,-.025,sign*length*.36))
        landing=endpoint.copy()
        if 'side ' in key:landing.x*=.80;landing.y+=.20
        elif 'underbody' in key:landing.y+=.22
        else:landing.z-=.10;landing.y-=.10
        rod('neon chassis mount '+key,landing,endpoint,.022,steel)
    light=bpy.data.lights.new('V9 neon spill '+key,'POINT')
    light.energy=8;light.color=(.06,1,.14);light.shadow_soft_size=.22
    glow=bpy.data.objects.new(light.name,light);hardware.objects.link(glow)
    glow.parent=root;glow.location=Vector(position)+y_axis*.05
    assert [tuple(v.co) for v in tube.data.vertices]==tube_geometry[tube.name]

# Door skins form overlapping upper-arm and gauntlet scales. Their original
# dragon artwork is carried by the source UVs through the cuts.
door_area_before=door_area_after=0.
for side,sign in [('L',1),('R',-1)]:
    for prefix in ('door_front_','door_rear_'):
        role=prefix+side;old=modules[role]
        bpy.context.view_layer.update();old_world=old.matrix_world.copy()
        pieces=defaultdict(list);source_material={}
        for obj in groups[role]:
            data=obj.data;data.calc_normals_split();source_material[obj.name]=data.materials[0]
            for poly in data.polygons:
                loops=list(poly.loop_indices)
                for j in range(1,len(loops)-1):
                    tri=[tuple(data.vertices[data.loops[i].vertex_index].co)+tuple(data.loops[i].normal)+tuple(data.uv_layers.active.data[i].uv) for i in (loops[0],loops[j],loops[j+1])]
                    door_area_before+=area(tri)
                    for polygon in split(tri,1,0):
                        for k in range(1,len(polygon)-1):
                            fragment=[polygon[0],polygon[k],polygon[k+1]]
                            if area(fragment)<1e-12:continue
                            band=int(sum(v[1] for v in fragment)>0)
                            pieces[band,obj.name].append(fragment);door_area_after+=area(fragment)
        for band in range(2):
            points=[v for (b,name),tris in pieces.items() if b==band for tri in tris for v in tri]
            center=Vector(tuple((min(v[a] for v in points)+max(v[a] for v in points))/2 for a in range(3)))
            new_role=prefix+'scale%d_'%band+side
            n=node(new_role);n.parent=root;modules[new_role]=n
            n.location=old_world @ center+Vector((0,0,.028 if band==0 else -.028))
            n.rotation_quaternion=quat((0,0,1),sign*(-8 if band==0 else 8)) @ old_world.to_quaternion()
            for (b,name),tris in pieces.items():
                if b!=band:continue
                verts=[v for tri in tris for v in tri]
                data=bpy.data.meshes.new(name+' articulated scale '+str(band))
                data.from_pydata([tuple(Vector(v[:3])-center) for v in verts],[],[tuple(range(i,i+3)) for i in range(0,len(verts),3)])
                data.update();uv=data.uv_layers.new(name='Original UV')
                for loop in data.loops:uv.data[loop.index].uv=verts[loop.vertex_index][6:8]
                for polygon in data.polygons:polygon.use_smooth=True
                data.use_auto_smooth=True;data.normals_split_custom_set_from_vertices([tuple(Vector(v[3:6]).normalized()) for v in verts])
                data.materials.append(source_material[name])
                obj=bpy.data.objects.new(name+'__scale'+str(band),data);originals.objects.link(obj);obj.parent=n
                obj['module']=new_role;obj['source_part']=name.split('__')[0]
                obj['source_module']=role;obj['source_local_pivot']=list(center)
                obj['provenance']='Original door surface split into rigid scales; source UV and area retained'
                groups[new_role].append(obj)
        for obj in list(groups.pop(role)):bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.objects.remove(modules.pop(role),do_unlink=True)
assert abs(door_area_before-door_area_after)<1e-6

# Short visible mount lugs bind the body panels to the new anatomical frame.
bpy.context.view_layer.update()
for role,n in modules.items():
    p=n.matrix_world.translation
    if role.startswith(('hood_','lamp_')):
        anchor=Vector((p.x*.82,.18,p.z))
    elif role.startswith(('door_','mirror_')):
        anchor=Vector((p.x*.94,p.y-.18,p.z))
    else:continue
    rod('panel hinge '+role,anchor,p,.031,steel)
    ring('panel hinge collar '+role,p,.055,.016,(1,0,0),dark)

revision_report={}
if args.revision>=10:
    from transformer_design_revision10 import revise
    revision_report=revise(globals())
if args.revision>=11:
    from transformer_design_revision11 import revise_head_hands
    revision_report.update(revise_head_hands(globals()))
if args.revision>=13:
    from transformer_design_revision13 import revise_body
    revision_report.update(revise_body(globals()))
if args.revision==14:
    from transformer_design_revision14 import revise_head
    revision_report.update(revise_head(globals()))
if args.revision>=15:
    from transformer_design_revision15 import revise_head
    revision_report.update(revise_head(globals()))
if args.revision>=16:
    from transformer_design_revision16 import revise_back_and_rider
    revision_report.update(revise_back_and_rider(globals()))

# No original glass/lamp/wheel surface may be lost or silently reshaped.
preserved=0
for obj in originals.objects:
    if obj.name not in original_fingerprints:continue
    digest=hashlib.sha256(repr((list(map(tuple,(v.co for v in obj.data.vertices))),
        [tuple(p.vertices) for p in obj.data.polygons],
        [tuple(u.uv) for u in obj.data.uv_layers.active.data])).encode()).hexdigest()
    assert digest==original_fingerprints[obj.name],obj.name
    preserved+=1
assert len(tubes)==7
assert len([name for name in modules if '_wheel_' in name])==4

# Floor check on the actual pose. Whole foot/bumper panels are raised together
# if their source curvature dips below the contact surface.
bpy.context.view_layer.update()
for role in ['toe_L','toe_R']:
    low=min((o.matrix_world@v.co).z for o in groups[role] for v in o.data.vertices)
    if low<.025:modules[role].location.z+=.025-low
bpy.context.view_layer.update()
floor_findings={}
for obj in list(originals.objects)+list(hardware.objects)+list(rider.objects):
    if obj.type!='MESH':continue
    low=min((obj.matrix_world@v.co).z for v in obj.data.vertices)
    if low<-.0001:floor_findings[obj.name]=low

notes='''350Z TRANSFORMER STUDY 09 - ROBOT BODY DESIGN

Static end-pose and silhouette study. The v8 transformation is retained in its
own folder and is NOT a valid animation for this redesigned robot.
Six hood segments preserve the original surface area, UVs and interpolated normals.
Eight door scales carry the original dragon graphics through lossless cuts.
All remaining original surfaces retain their mesh coordinates and UVs, including
whole glazing, mirrors, original light shapes and exactly four wheels.
Seven continuous neon tube meshes are reused without segmentation or resizing.
The same reference mannequin is seated behind the new head on a supported saddle.
Authored: tapered limbs, rib cage, pelvis, joint housings, layered armor, head,
feet, hands, panel mounts and protective neon channels.
OPEN: final-pose intersections, car-mode packing, carrier routes and full
transformation. This is an art-direction milestone, not a game-ready rig.
'''
for text in list(bpy.data.texts):bpy.data.texts.remove(text)
if args.revision>=10:
    notes=notes.replace('STUDY 09','STUDY 10')+'''\nRevision 10: original headlights exposed ahead of the shoulders; original rear
lamps face rearward on shoulder mounts. Four unchanged wheels, front pair tilted
out/up as shoulder pads. New recessed narrow optics and a layered mechanical mask.
Original floor panels are losslessly cut and folded around the legs; rear knee
bearings, calf actuators and heel linkages replace the broad flat rear silhouette.
Still a static design study; transformation and car-mode packing remain open.
'''
if args.revision>=11:
    notes=notes.replace('STUDY 10','STUDY 11')+'''\nRevision 11 replaces only the authored head, neck and hands: layered cranial
blades, deep optical sockets, small green pupils, articulated jaw mechanisms,
five mechanical digits per hand with three finger links, opposing thumb,
tendon rods, knuckle bearings and tapered gripping tips. Body/car panels,
lighting, glazing, shoulder wheels, neon and reference rider retain v10 poses.
This is a model and pose study; finger/face animation and car packing are open.
'''
if args.revision>=12:
    notes=notes.replace('STUDY 11','STUDY 12')+'''\nRevision 12 corrects hand chirality: with dorsal armor facing forward, thumbs
are on the lateral sides and fingers curl rearward toward the palm. Index and
little-finger ordering, thumb opposition and curved tips are corrected together.
No negative object scale; other vehicle/robot geometry retains the v11 design.
'''
if args.revision>=13:
    notes=notes.replace('STUDY 12','STUDY 13')+'''\nRevision 13: palms face medially toward the legs; thumbs and index edges forward.
Tapered waist with an exposed
green reactor assembly, routed coolant hoses and conduits. The two wide rear
floor strips become four rigid hip folds; original exhaust assemblies move to
the lower rear cooling manifold. All original glazing, lights and four wheels
remain whole. Seven continuous neon tubes retained; one rear tube relocated.
Static pose only. New hoses have no simulated deformation or car packing yet.
'''
if args.revision>=14:
    notes=notes.replace('STUDY 13','STUDY 14')+'''\nRevision 14 replaces only the head meshes with a closed black/green helmet,
red compound optics, swept brows, vented respirator and pointed silver mask.
Body, neck, hands, original car parts, neon and rider keep the V13 geometry/pose.
Static reference-guided 3D study; facial animation and car packing remain open.
'''
if args.revision>=15:
    notes=notes.replace('STUDY 14','STUDY 15')+'''\nRevision 15 rebuilds the head plate surfaces with curved quad rings, corner
fillets and narrow metal rims. Optics sit inside deep black housings; cheek
vents have inset honeycomb cells. Separate painted/metal finishes include
roughness variation and fine machining grain. Body retains the V13/V14 pose.
Static geometry study; internal self-contact and car packing remain open.
'''
if args.revision>=16:
    notes=notes.replace('STUDY 15','STUDY 16')+'''\nRevision 16 folds the original spoiler onto visible hinge cradles and back-frame
arms. The complete rear neon tube/channel moves vertically onto the back above
the exhaust. Chair, backrest and handrails are removed. The same reference
figure rides directly astride the neck with fixed-length articulated limbs.
Only a static end pose: rider transfer, folding paths and car packing are open.
'''
bpy.data.texts.new('READ ME - design scope').write(notes)
(OUT/'READ-ME.txt').write_text(notes)
report=dict(version=args.revision,static_design_only=True,source_blend_sha256=source_hash,
            hood_segments=6,hood_area_before=hood_area_before,hood_area_after=hood_area_after,
            door_segments=8,door_area_before=door_area_before,door_area_after=door_area_after,
            original_meshes_unchanged=preserved,wheels=4,neon_tubes=7,neon_meshes_unchanged=True,
            rigid_glass_preserved=True,rider_reference_only=True,transformation_validated=False,
            car_packing_verified=False,floor_crossings=floor_findings)
report.update(revision_report)
(OUT/'design-audit.json').write_text(json.dumps(report,indent=2))
scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=32
scene.eevee.use_gtao=True;scene.eevee.gtao_distance=2
scene.eevee.use_bloom=True;scene.eevee.bloom_intensity=.035
scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=scene.render.resolution_y=1000
scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
camera=scene.camera;camera.data.type='ORTHO';camera.data.ortho_scale=5.9
camera.location=(7,11,3.8);camera.rotation_euler=(Vector((0,0,2.5))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'350z-transformer-design.blend'))
print('DESIGN AUDIT',json.dumps(report),flush=True)
if args.render:
    for name,location,target,scale in [
        ('01-hero',(7,11,3.8),(0,0,2.5),5.9),
        ('02-front',(0,13,2.5),(0,0,2.5),5.9),
        ('03-rear',(-7,-11,4.5),(0,0,2.5),5.9),
        ('04-head-chest',(4,10,4.1),(0,.10,3.78),2.65),
        ('05-rear-legs',(-4,-10,2.0),(0,-.1,1.2),2.7),
        ('06-head',(3,10,4.45),(0,.10,4.30),1.22),
        ('07-hand',(4,10,2.8),(1.40,.45,1.96),1.15),
        ('08-head-profile',(10,3,4.5),(0,.1,4.30),1.22),
        ('09-other-hand',(-4,10,2.8),(-1.40,.45,1.96),1.15),
        ('10-hand-palm',(4,-10,2.8),(1.40,.39,1.96),1.15),
        ('11-reactor',(3,10,3.35),(0,.30,2.98),1.75),
        ('12-rear-mechanism',(-4,-10,2.9),(0,-.5,2.45),2.0),
        ('13-head-front',(0,10,4.32),(0,.10,4.32),1.05),
        ('16-neck-rider',(5,10,5.6),(0,-.1,4.15),2.6),
        ('17-back-attachments',(-3,-10,4.8),(0,-.65,3.65),2.5),
        ('18-rider-profile',(10,0,4.7),(0,-.05,4.20),2.3)]:
        camera.location=location;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.ortho_scale=scale;scene.render.filepath=str(OUT/(name+'.png'))
        bpy.ops.render.render(write_still=True)
if args.turntable:
    camera.data.ortho_scale=5.9
    scene.frame_start=1;scene.frame_end=145;scene.frame_step=2;scene.render.fps=12
    scene.render.resolution_x=scene.render.resolution_y=800
    for frame in range(1,146):
        angle=math.atan2(11,7)+2*math.pi*(frame-1)/144
        camera.location=(13*math.cos(angle),13*math.sin(angle),3.8)
        camera.rotation_euler=(Vector((0,0,2.5))-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.keyframe_insert('location',frame=frame);camera.keyframe_insert('rotation_euler',frame=frame)
    scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
    scene.render.ffmpeg.constant_rate_factor='HIGH'
    scene.render.filepath=str(OUT/'350z-design-turntable.mp4')
    bpy.ops.render.render(animation=True)
