"""Formed hard-surface helmet plates, recessed optics and layered PBR finishes."""
import math
import bpy
import numpy as np
from mathutils import Vector
from transformer_design_revision14 import revise_head as build_layout


def revise_head(c):
    mesh,rod,ring,material=[c[k] for k in ('mesh','rod','ring','material')]
    edge=material('V15 dark exposed alloy edge',(.032,.038,.041),.85,.29)

    def formed_mesh(name,vertices,faces,mat,parent=None,bevel=0):
        if name=='V14 central crown keel':return None  # Rebuilt around a real vent opening below.
        n=(len(vertices)-1)//2
        is_panel=len(vertices)==2*n+1 and faces and tuple(faces[0])==(0,1,n)
        if not is_panel:return mesh(name,vertices,faces,mat,parent,bevel)
        outline=[Vector(v) for v in vertices[:n]]
        if 'pointed silver chin' in name:
            outline=[Vector(v) for v in [(-.052,.407,.135),(0,.429,.145),(.052,.407,.135),
                (.035,.402,.052),(.022,.397,.038),(-.022,.397,.038),(-.035,.402,.052)]]
            # Reuse the positive-Y winding of the source plate.
            normal=sum((outline[i].cross(outline[(i+1)%len(outline)]) for i in range(len(outline))),Vector())
            if normal.y<0:outline.reverse()
        if 'green jaw rail' in name:
            sign=1 if sum(v.x for v in outline)>0 else -1
            for v in outline:v.x=sign*.175+(v.x-sign*.175)*.74
        if 'nose bridge' in name:
            for v in outline:v.x*=.82;v.y=.35+(v.y-.35)*.72
        # The old outlines had inconsistent front depths. Interpolating those
        # depths toward a common center creates star-shaped pinching even with
        # smooth normals. Fit a coherent surface before forming its shallow crown.
        if not any(t in name for t in ('nose','nasal','fin','chin')):
            design=np.array([[v.x,v.z,1.] for v in outline])
            fit=np.linalg.lstsq(design,np.array([v.y for v in outline]),rcond=None)[0]
            for v in outline:v.y=fit[0]*v.x+fit[1]*v.z+fit[2]
        depth=max(.006,abs(vertices[0][1]-vertices[n+1][1]))
        # Quadratic corner fillets preserve sharp overall styling without razor
        # polygon corners; concentric quad loops replace the visible triangle fan.
        loop=[]
        for i,p in enumerate(outline):
            a=p.lerp(outline[i-1],.075);b=p.lerp(outline[(i+1)%len(outline)],.075)
            for j in range(5):
                t=j/4;loop.append((1-t)**2*a+2*t*(1-t)*p+t*t*b)
        center=sum(loop,Vector())/len(loop);count=len(loop);vs=[];fs=[]
        bulge=.007 if 'armor' in name or 'keel' in name else .003
        for t in (0,.028,.09,.28,.55,.80,.94):
            for p in loop:
                v=p.lerp(center,t);v.y+=.002*min(t/.028,1)+bulge*math.sin(math.pi*t)
                vs.append(tuple(v))
        for row in range(6):
            for j in range(count):fs.append((row*count+j,row*count+(j+1)%count,(row+1)*count+(j+1)%count,(row+1)*count+j))
        vs.append(tuple(center+Vector((0,.002,0))));idx=len(vs)-1
        for j in range(count):fs.append((6*count+j,6*count+(j+1)%count,idx))
        back=len(vs);vs.extend(tuple(p-Vector((0,depth,0))) for p in loop)
        for j in range(count):fs.append((j,back+j,back+(j+1)%count,(j+1)%count))
        fs.append(tuple(range(back+count-1,back-1,-1)))
        o=mesh(name.replace('V14','V15'),vs,fs,mat,parent)
        o.data.materials.append(edge)
        for polygon in o.data.polygons:
            polygon.use_smooth=True
            # Narrow edge catches; the broad surface retains its own finish.
            if polygon.index<count:polygon.material_index=1
        o.data.use_auto_smooth=True;o.data.auto_smooth_angle=math.radians(55)
        o['v15_formed_plate']=True
        return o

    context=dict(c);context['mesh']=formed_mesh
    result=build_layout(context)
    head=bpy.data.objects['DESIGN - V11 interlocking cranial assembly'];head['design_revision']=15
    mats={}
    for o in c['hardware'].objects:
        if o.parent==head and o.type=='MESH':
            for m in o.data.materials:mats[m.name]=m
    for name,m in mats.items():
        if 'V14' not in name:continue
        tree=m.node_tree;bsdf=tree.nodes.get('Principled BSDF')
        # Object-space patina, roughness variation and fine brushed machining.
        if 'black helmet' in name:color=(.007,.010,.012);metal=.88;rough=.32
        elif 'graphite' in name:color=(.027,.034,.038);metal=.88;rough=.35
        elif 'machined mask' in name:color=(.20,.23,.25);metal=.92;rough=.30
        elif 'emerald' in name:color=(.009,.115,.028);metal=.62;rough=.28
        else:continue
        bsdf.inputs['Metallic'].default_value=metal
        bsdf.inputs['Anisotropic'].default_value=.28 if 'machined' in name else .1
        bsdf.inputs['Clearcoat'].default_value=.25 if 'emerald' in name else .08
        bsdf.inputs['Clearcoat Roughness'].default_value=.24
        tex=tree.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=95;tex.inputs['Detail'].default_value=3
        ramp=tree.nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position=.2;ramp.color_ramp.elements[0].color=tuple(v*.55 for v in color)+(1,)
        ramp.color_ramp.elements[1].position=.8;ramp.color_ramp.elements[1].color=color+(1,)
        tree.links.new(tex.outputs['Fac'],ramp.inputs['Fac']);tree.links.new(ramp.outputs['Color'],bsdf.inputs['Base Color'])
        # ColorRamp works in Blender 2.80 as well as current Blender versions.
        rr=tree.nodes.new('ShaderNodeValToRGB');rr.color_ramp.elements[0].color=(rough-.10,)*3+(1,)
        rr.color_ramp.elements[1].color=(rough+.16,)*3+(1,)
        tree.links.new(tex.outputs['Fac'],rr.inputs['Fac']);tree.links.new(rr.outputs['Color'],bsdf.inputs['Roughness'])
        coord=tree.nodes.new('ShaderNodeTexCoord');split=tree.nodes.new('ShaderNodeSeparateXYZ');stretch=tree.nodes.new('ShaderNodeCombineXYZ')
        tree.links.new(coord.outputs['Object'],split.inputs[0])
        for axis,factor in zip('XYZ',(70,70,2400)):
            scale=tree.nodes.new('ShaderNodeMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=factor
            tree.links.new(split.outputs[axis],scale.inputs[0]);tree.links.new(scale.outputs[0],stretch.inputs[axis])
        grain=tree.nodes.new('ShaderNodeTexNoise');grain.inputs['Scale'].default_value=1;grain.inputs['Detail'].default_value=2
        tree.links.new(stretch.outputs['Vector'],grain.inputs['Vector'])
        bump=tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.00035
        tree.links.new(grain.outputs['Fac'],bump.inputs['Height']);tree.links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])

    # Remove the old flat optical screens and surface-mounted rings completely.
    remove=('eye socket','eye backing','optical lens barrel','red lens ring','lens center',
        'cheek vent louver','forehead recessed vent','chin center seam')
    for o in list(c['hardware'].objects):
        if o.parent==head and any(t in o.name for t in remove):bpy.data.objects.remove(o,do_unlink=True)
    black=material('V15 optical well black',(.001,.001,.001),.0,.8)
    black.node_tree.nodes.get('Principled BSDF').inputs['Specular'].default_value=.06
    optic=material('V15 deep red optical glass',(.018,.0003,.0001),.25,.19,(.025,.0001,0))
    red=material('V15 red lens emission',(.30,.001,0),.15,.22,(12,.012,.002))
    metal=material('V15 optical socket alloy',(.045,.052,.056),.82,.28)

    def opening(name,outline,depth):
        # Open beveled flange, black interior sidewalls and a recessed back.
        outer=[Vector(v) for v in outline];center=sum(outer,Vector())/len(outer)
        inner=[p.lerp(center,.07) for p in outer]
        back=[p-Vector((0,depth,0)) for p in inner];n=len(outer)
        vs=list(map(tuple,outer+inner+back));fs=[]
        for i in range(n):
            j=(i+1)%n;fs.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)])
        fs.append(tuple(range(2*n,3*n)))
        o=mesh('V15 '+name,vs,fs,metal,head,.0015);o.data.materials.append(black)
        for p in o.data.polygons:
            if p.index%2==1 or p.index==len(fs)-1:p.material_index=1
        return o

    for s in (-1,1):
        opening('recessed optical housing',[(s*.045,.346,.411),(s*.210,.277,.479),
            (s*.194,.334,.382),(s*.078,.371,.356)],.060)
        # Optics sit 20-30 mm behind the surround, with curved lens fronts.
        for x,z,r in ((.090,.390,.010),(.116,.399,.011),(.145,.412,.012),(.170,.430,.010),(.183,.452,.008)):
            y=.338-(x-.08)*.45
            rod('V15 recessed lens barrel',(s*x,y-.026,z),(s*x,y,z),r*1.18,black,head,32)
            ring('V15 red lens ring',(s*x,y+.002,z),r*.77,r*.14,(0,1,0),red,head)
            # Concentric curved glass dome, small dark pupil and hot inner glint.
            vertices=[];faces=[]
            for k in range(6):
                a=math.pi*k/12
                for j in range(24):
                    theta=j*math.tau/24
                    vertices.append((s*x+r*.64*math.cos(a)*math.cos(theta),y+.002+r*.40*math.sin(a),z+r*.64*math.cos(a)*math.sin(theta)))
            for k in range(5):
                for j in range(24):faces.append((k*24+j,k*24+(j+1)%24,(k+1)*24+(j+1)%24,(k+1)*24+j))
            lens=mesh('V15 curved optical lens',vertices,faces,optic,head)
            for p in lens.data.polygons:p.use_smooth=True
            rod('V15 optical pupil',(s*x,y+.007,z),(s*x,y+.008,z),r*.24,black,head,20)
            ring('V15 optical inner focus',(s*x,y+.007,z),r*.31,.0008,(0,1,0),red,head)
        # Compact true inset cheek vent with fine honeycomb cells.
        opening('cheek vent frame',[(s*.148,.319,.269),(s*.198,.289,.299),
            (s*.185,.310,.201),(s*.145,.337,.173)],.030)
        for row in range(5):
            for col in range(3):
                x=.151+col*.012+(row%2)*.005;z=.202+row*.014
                y=.313-(x-.15)*.5
                a=[]
                for j in range(7):
                    angle=math.tau*j/6;a.append((s*(x+.006*math.cos(angle)),y,z+.006*math.sin(angle)))
                for j in range(6):rod('V15 vent honeycomb wall',a[j],a[j+1],.0011,metal,head,5)
        # Short internal cylinders give the cheek/temple gaps functional depth.
        for row in range(3):
            z=.25+row*.055
            rod('V15 temple braided conduit',(s*.233,.065,z),(s*.241,.136,z+.032),.007,black,head,16)
            for t in range(5):ring('V15 temple conduit collar',(s*(.234+t*.0014),.076+t*.011,z+.005+t*.005),.008,.0013,(0,1,.45),metal,head)
    helmet=bpy.data.materials['V9 - V14 satin black helmet']
    def crown_piece(name,outline):
        pts=[Vector(v) for v in outline];n=len(pts)
        normal=sum((pts[i].cross(pts[(i+1)%n]) for i in range(n)),Vector())
        if normal.y<0:pts.reverse()
        center=sum(pts,Vector())/n
        vs=list(map(tuple,pts))+[tuple(center)]+[tuple(p-Vector((0,.022,0))) for p in pts]
        formed_mesh('V15 '+name,vs,[(i,(i+1)%n,n) for i in range(n)],helmet,head)
    crown_piece('upper crown keystone',[(-.043,.14,.79),(.043,.14,.79),(.054,.204,.716),(-.054,.204,.716)])
    crown_piece('lower crown keystone',[(-.060,.258,.641),(.060,.258,.641),(.036,.31,.46),
        (0,.337,.397),(-.036,.31,.46)])
    for s in (-1,1):
        crown_piece('crown vent side rail',[(s*.030,.208,.716),(s*.054,.204,.716),
            (s*.060,.258,.641),(s*.028,.261,.641)])
    opening('forehead vent pocket',[(-.029,.212,.709),(.029,.212,.709),(.027,.257,.648),(-.027,.257,.648)],.025)
    for z in (.661,.677,.693):
        y=.257-(z-.648)*.738-.009
        rod('V15 forehead internal louver',(-.022,y,z),(.022,y,z),.003,black,head,8)
    for o in c['hardware'].objects:
        if o.parent==head and o.type=='MESH' and not o.get('v15_formed_plate'):
            if any(t in o.name for t in ('barrel','ring','pivot','piston','conduit','collar','lens')):
                for p in o.data.polygons:p.use_smooth=True
                o.data.use_auto_smooth=True;o.data.auto_smooth_angle=.7
    result.update(head_design='Curved formed armor with filleted rims, machined PBR finishes, deep optical wells and recessed honeycomb ventilation',
        head_revision=15,head_reference_applied=True,head_hand_animation_validated=False)
    return result
