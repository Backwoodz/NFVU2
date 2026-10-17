"""Athletic torso, routed hoses, exposed reactor and exhaust rear for study 13."""
import math
from collections import defaultdict
import bpy
from mathutils import Matrix,Vector
from transformer_parts import split,area


def revise_body(c):
    node,rod,ring,box,loft,mesh,plate,material,pose,quat=[c[k] for k in ('node','rod','ring','box','loft','mesh','plate','material','pose','quat')]
    root,hardware,originals,modules,groups=[c[k] for k in ('root','hardware','originals','modules','groups')]
    dark,steel,edge,rubber,copper,paint=[c[k] for k in ('dark','steel','edge','rubber','copper','paint')]
    newparts=node('V13 body mechanism');newparts.parent=root
    glow=material('V13 reactor plasma',(.01,.35,.04),.1,.23,(.10,5,.22))
    dim=material('V13 coolant glow',(.015,.19,.035),.3,.3,(.03,1.5,.09))
    hosemat=material('V13 braided hose',(.025,.032,.028),.3,.54)
    reactor_metal=material('V13 reactor gunmetal',(.065,.085,.095),.80,.37)
    reactor_edge=material('V13 reactor machined edge',(.21,.25,.27),.86,.28)

    # Forearms in a relaxed neutral pose: dorsal +Y -> lateral +/-X,
    # palm -Y -> medial -/+X; V13 digit ordering puts the thumbs forward.
    for side,sign in [('L',1),('R',-1)]:
        hand=bpy.data.objects['DESIGN - V11 articulated hand '+side]
        hand.rotation_quaternion=quat((0,0,1),-sign*90)
        hand.location.x=sign*1.30
        hand.location.z=2.14
        rod('V13 wrist alignment bridge',(sign*1.30,.39,2.24),(sign*1.30,.39,2.10),.045,steel,newparts)

    # Remove the authored broad abdominal plates; vehicle skins are folded below.
    names=('rib cage','pelvic casting','abdominal chevron','lateral rib','lumbar cylinder','lumbar polished shaft',
           'sternum keel','sternum interlock','sternal rib blade','sternal rib actuator','pectoral seam')
    for o in list(hardware.objects):
        if any(o.name.startswith('V9 - '+n) for n in names):bpy.data.objects.remove(o,do_unlink=True)
    loft('V13 tapered thoracic frame',[(2.43,.27,.20),(2.69,.29,.22),(2.99,.43,.25),
        (3.30,.67,.29),(3.55,.70,.25),(3.73,.34,.19)],dark,newparts,.018)
    loft('V13 compact pelvic saddle',[(2.0,.33,.19),(2.18,.43,.23),(2.33,.34,.21),(2.47,.25,.19)],dark,newparts,.015)
    for side,sign in [('L',1),('R',-1)]:
        pose('hood_1_'+side,(sign*.64,.10,3.20),quat((0,0,1),-sign*69) @ quat((0,1,0),-sign*17) @ quat((1,0,0),-72))
        pose('hood_2_'+side,(sign*.48,-.09,2.81),quat((0,0,1),-sign*84) @ quat((0,1,0),-sign*9) @ quat((1,0,0),-72))
        # Oblique load struts trace the V from chest down to the narrow pelvis.
        rod('V13 oblique outer cylinder',(sign*.65,.05,3.29),(sign*.36,.08,2.64),.065,steel,newparts)
        rod('V13 oblique polished ram',(sign*.40,.08,2.75),(sign*.27,.10,2.40),.031,edge,newparts)
        for row in range(3):
            z=3.42-row*.14;w=.65-row*.055
            plate('V13 swept rib',[(sign*.30,.34,z+.08),(sign*w,.27,z+.03),
                (sign*(w+.08),.12,z-.08),(sign*.38,.32,z-.02)],.045,steel,newparts)
        plate('V13 iliac crest',[(sign*.17,.25,2.40),(sign*.37,.27,2.43),(sign*.52,.17,2.23),(sign*.29,.32,2.15)],.055,steel,newparts)
        # Existing authored leg cores become leaner; source car armor is unscaled.
        for label in ('thigh inner core','shin inner core'):
            o=bpy.data.objects.get('V9 - '+label+' '+side)
            if o:
                for v in o.data.vertices:v.co.x*=.88;v.co.y*=.88

    # Lossless articulation of the two flat 1.1m rear strips, keeping UVs.
    before=after=0.
    for side,sign in [('L',1),('R',-1)]:
        role='belt_'+side;fragments=defaultdict(list);mats={}
        for o in groups[role]:
            d=o.data;d.calc_normals_split();mats[o.name]=d.materials[0]
            for p in d.polygons:
                loops=list(p.loop_indices)
                for j in range(1,len(loops)-1):
                    tri=[tuple(d.vertices[d.loops[i].vertex_index].co)+tuple(d.loops[i].normal)+tuple(d.uv_layers.active.data[i].uv) for i in (loops[0],loops[j],loops[j+1])]
                    before+=area(tri)
                    for poly in split(tri,1,0):
                        for k in range(1,len(poly)-1):
                            t=[poly[0],poly[k],poly[k+1]]
                            if area(t)<1e-12:continue
                            band=int(sum(v[1] for v in t)>0);fragments[band,o.name].append(t);after+=area(t)
        for band in (0,1):
            points=[v for (b,name),tris in fragments.items() if b==band for t in tris for v in t]
            center=Vector([(min(v[a] for v in points)+max(v[a] for v in points))/2 for a in range(3)])
            role2=role+'_hipfold'+str(band);n=node(role2,(sign*(.54 if band==0 else .46),-.33,2.86 if band==0 else 2.32),
                quat((0,0,1),sign*(72 if band==0 else 53)) @ c['RX']);n.parent=root;modules[role2]=n
            for (b,name),tris in fragments.items():
                if b!=band:continue
                points=[v for t in tris for v in t];d=bpy.data.meshes.new(name+' hip fold')
                d.from_pydata([tuple(Vector(v[:3])-center) for v in points],[],[tuple(range(i,i+3)) for i in range(0,len(points),3)])
                d.update();uv=d.uv_layers.new(name='Original UV')
                for loop in d.loops:uv.data[loop.index].uv=points[loop.vertex_index][6:8]
                for p in d.polygons:p.use_smooth=True
                d.use_auto_smooth=True;d.normals_split_custom_set_from_vertices([tuple(Vector(v[3:6]).normalized()) for v in points]);d.materials.append(mats[name])
                o=bpy.data.objects.new(name+'__hipfold'+str(band),d);originals.objects.link(o);o.parent=n
                o['module']=role2;o['source_part']=name.split('__')[0];o['source_module']=role;o['source_local_pivot']=list(center)
                groups[role2].append(o)
            rod('V13 folded floor mount',Vector(n.location)+Vector((-sign*.1,.13,0)),n.location,.03,steel,newparts)
        for o in list(groups.pop(role)):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.objects.remove(modules.pop(role),do_unlink=True)
    assert abs(before-after)<1e-6
    pose('pelvis_rear',(0,-.27,2.08),quat((0,0,1),180) @ c['RX'])
    for side,sign in [('L',1),('R',-1)]:pose('exhaust_'+side,(sign*.43,-.98,2.40),quat((1,0,0),20))
    spoiler=modules['spoiler'];spoiler.location.z+=.80;spoiler.location.y-=.05
    spoiler.rotation_quaternion=quat((1,0,0),-65) @ spoiler.rotation_quaternion
    for sign in (-1,1):
        rod('V13 spoiler folding support',(sign*.42,-.96,3.43),Vector(spoiler.location)+Vector((sign*.42,0,0)),.032,steel,newparts)

    # Move the complete lower rear neon assembly, including its channel/fasteners.
    neon=bpy.data.objects['DESIGN - protected neon underbody rear']
    old=neon.location.copy();neon.location=(0,-1.40,2.12)
    delta=neon.location-old
    for o in list(hardware.objects):
        if o.name.startswith('V9 - neon chassis mount underbody rear'):
            bpy.data.objects.remove(o,do_unlink=True);continue
        if o.type=='LIGHT' and o.name=='V9 neon spill underbody rear':o.location+=delta
    for x in (-.39,.39):rod('V13 rear neon bracket',(x,-.98,2.19),(x,-1.375,2.12),.023,steel,newparts)

    # Smooth sampled Bezier-style paths, modeled as mesh tubes for render/audits.
    hose_records=[]
    def conduit(name,knots,radius,mat,corrugated=False,connection=None):
        previous=set(hardware.objects)
        ps=[Vector(k) for k in knots];points=[]
        for i in range(len(ps)-1):
            a=ps[max(0,i-1)];b=ps[i];cc=ps[i+1];d=ps[min(len(ps)-1,i+2)]
            for j in range(12):
                t=j/12
                points.append(.5*((2*b)+(-a+cc)*t+(2*a-5*b+4*cc-d)*t*t+(-a+3*b-3*cc+d)*t*t*t))
        points.append(ps[-1]);vs=[];faces=[];basis=[]
        for i,p in enumerate(points):
            tangent=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
            ref=Vector((1,0,0)) if abs(tangent.x)<.85 else Vector((0,1,0))
            u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized();basis.append((u,v,tangent))
            for j in range(12):
                a=2*math.pi*j/12;vs.append(tuple(p+radius*(u*math.cos(a)+v*math.sin(a))))
        for i in range(len(points)-1):
            for j in range(12):faces.append((i*12+j,i*12+(j+1)%12,(i+1)*12+(j+1)%12,(i+1)*12+j))
        faces.extend([tuple(range(11,-1,-1)),tuple(range((len(points)-1)*12,len(points)*12))])
        o=mesh('V13 '+name,vs,faces,mat,newparts)
        for p in o.data.polygons:p.use_smooth=True
        if corrugated:
            for i in range(2,len(points)-2,3):ring('V13 hose rib',points[i],radius*1.04,radius*.17,basis[i][2],dark,newparts)
        for i in (0,len(points)-1):
            ring('V13 hose compression fitting',points[i],radius*1.15,radius*.24,basis[i][2],edge,newparts)
            p=points[i];t=basis[i][2]
            rod('V13 hose end socket',p-t*.035,p+t*.035,radius*1.13,steel,newparts,vertices=12)
        for i in range(12,len(points)-10,20):ring('V13 hose retaining band',points[i],radius*1.07,radius*.18,basis[i][2],steel,newparts)
        hose_records.append(dict(name=name,knots=[list(k) for k in ps],radius=radius))
        for created in set(hardware.objects)-previous:
            created['v13_route']=o.name
            if connection:
                created['route_connection']=connection;created['route_terminal']=list(ps[-1]);created['terminal_tolerance']=radius+.06
        return o

    # Reactor, with nested containment rings, shutter blades and radial fasteners.
    previous=set(hardware.objects)
    center=Vector((0,.56,2.98));axis=(0,1,0)
    rod('V13 reactor rear casing',center-Vector((0,.15,0)),center+Vector((0,.025,0)),.31,dark,newparts,vertices=48)
    for r,y,t,mat in [(.315,.03,.037,reactor_metal),(.264,.075,.021,copper),(.213,.10,.025,reactor_edge),(.153,.13,.015,dark)]:
        ring('V13 reactor containment ring',center+Vector((0,y,0)),r,t,axis,mat,newparts)
    ring('V13 reactor energized channel',center+Vector((0,.10,0)),.24,.009,axis,dim,newparts)
    # A luminous spherical core, rather than an opaque glowing disk.
    vs=[];fs=[]
    for row in range(13):
        lat=-math.pi/2+math.pi*row/12
        for j in range(24):
            lon=2*math.pi*j/24
            vs.append(tuple(center+Vector((.115*math.cos(lat)*math.cos(lon),.12+.115*math.cos(lat)*math.sin(lon),.115*math.sin(lat)))))
    for row in range(12):
        for j in range(24):fs.append((row*24+j,row*24+(j+1)%24,(row+1)*24+(j+1)%24,(row+1)*24+j))
    core=mesh('V13 reactor plasma core',vs,fs,glow,newparts)
    for p in core.data.polygons:p.use_smooth=True
    for i in range(12):
        a=2*math.pi*i/12;rad=Vector((math.cos(a),0,math.sin(a)));t=Vector((-math.sin(a),0,math.cos(a)))
        plate('V13 reactor radial shutter',[tuple(center+rad*.146+t*.025+Vector((0,.145,0))),
            tuple(center+rad*.205+t*.052+Vector((0,.125,0))),tuple(center+rad*.291+t*.026+Vector((0,.078,0))),
            tuple(center+rad*.289-t*.018+Vector((0,.078,0))),tuple(center+rad*.18-t*.014+Vector((0,.14,0)))],.024,reactor_metal,newparts)
        p=center+rad*.317+Vector((0,.073,0))
        rod('V13 containment bolt',p,p+Vector((0,.022,0)),.018,dark,newparts,vertices=6)
    light=bpy.data.lights.new('V13 reactor spill','POINT');light.energy=24;light.color=(.055,1,.12);light.shadow_soft_size=.16
    o=bpy.data.objects.new(light.name,light);hardware.objects.link(o);o.parent=newparts;o.location=center+Vector((0,.26,0))
    for created in set(hardware.objects)-previous:created['v13_reactor']=True
    for x in (-.12,0,.12):
        rod('V13 sternum drive casing',(x,.35,3.34),(x,.35,3.61),.027,reactor_metal,newparts)
        for z in (3.37,3.43,3.49,3.55):ring('V13 sternum drive collar',(x,.35,z),.030,.007,(0,0,1),reactor_edge,newparts)
        rod('V13 lumbar driveshaft',(x*.7,.32,2.25),(x*.7,.34,2.64),.027,reactor_metal,newparts)
        for z in (2.31,2.38,2.45,2.52,2.59):ring('V13 lumbar flex coupling',(x*.7,.33,z),.030,.008,(0,0,1),reactor_edge,newparts)
    for z in (3.38,3.51,3.64):
        plate('V13 central sternum bridge',[(-.20,.36,z+.035),(0,.40,z+.005),(.20,.36,z+.035),(.17,.37,z-.025),(0,.41,z-.045),(-.17,.37,z-.025)],.025,dark,newparts)
    for sign in (-1,1):
        rod('V13 reactor upper support',(sign*.23,.39,3.52),(sign*.25,.46,3.17),.037,steel,newparts)
        rod('V13 reactor lower support',(sign*.19,.29,2.48),(sign*.23,.46,2.78),.035,steel,newparts)
        conduit('front coolant loop',[(sign*.25,.48,3.19),(sign*.48,.50,3.12),(sign*.49,.51,2.88),(sign*.31,.45,2.58)],.039,hosemat,True)
        conduit('thoracic power conduit',[(sign*.21,.37,3.58),(sign*.33,.42,3.43),(sign*.23,.48,3.27)],.026,steel)
        conduit('lumbar coolant feed',[(sign*.20,.52,2.59),(sign*.15,.53,2.42),(sign*.04,.50,2.30),(sign*.04,.35,2.25)],.022,hosemat,True)
        conduit('upper arm hydraulic hose',[(sign*1.15,-.85,3.35),(sign*1.37,-.72,3.17),(sign*1.44,-.38,3.02),(sign*1.42,-.10,2.93)],.026,hosemat,True)
        conduit('thigh hydraulic hose',[(sign*.40,-.49,2.0),(sign*.54,-.60,1.85),(sign*.59,-.58,1.60),(sign*.64,-.33,1.30)],.032,hosemat,True)
        conduit('calf hydraulic return',[(sign*.64,-.56,1.10),(sign*.63,-.58,.90),(sign*.72,-.59,.62),(sign*.81,-.40,.37)],.024,hosemat,True)
        rod('V13 knee hydraulic port',(sign*.64,-.12,1.30),(sign*.64,-.33,1.30),.036,steel,newparts)
        rod('V13 ankle hydraulic port',(sign*.81,-.14,.37),(sign*.81,-.40,.37),.028,steel,newparts)

    # Exposed rear heat exchanger and manifold feed the two original exhausts.
    rear=Vector((0,-1.25,2.48))
    rod('V13 rear heat exchanger',rear+Vector((0,.09,0)),rear,.21,dark,newparts,vertices=32)
    ring('V13 rear turbine rim',rear-Vector((0,.027,0)),.208,.025,(0,1,0),steel,newparts)
    ring('V13 rear turbine bearing',rear-Vector((0,.065,0)),.07,.018,(0,1,0),copper,newparts)
    for i in range(14):
        a=i*math.tau/14;p=rear+Vector((math.cos(a)*.137,-.04,math.sin(a)*.137))
        vane=box('V13 turbine cooling vane',p,(.022,.06,.13),steel,newparts,.004);vane.rotation_euler.y=-a+.35
    for sign in (-1,1):
        connection='exhaust_'+('L' if sign==1 else 'R')
        conduit('rear exhaust manifold',[(sign*.18,-1.28,2.57),(sign*.34,-1.46,2.66),(sign*.51,-1.36,2.50),(sign*.43,-1.13,2.43)],.060,steel,connection=connection)
        conduit('rear return pipe',[(sign*.15,-1.25,2.46),(sign*.21,-1.20,2.35),(sign*.295,-.99,2.43)],.038,dark,connection=connection)
        for z in (2.78,2.85,2.92):
            box('V13 rear heat vent',(sign*.32,-1.27,z-.10),(.12,.045,.027),dim,newparts,.005)
        rod('V13 rear manifold mounting strut',(sign*.25,-.20,2.43),(sign*.40,-.98,2.40),.035,steel,newparts)
    # Rebuild the hood hinges after the new side folds, keeping actual supports.
    for o in list(hardware.objects):
        if any(o.name.startswith('V9 - '+prefix) for prefix in ('panel hinge hood_1','panel hinge hood_2','panel hinge collar hood_1','panel hinge collar hood_2')):
            bpy.data.objects.remove(o,do_unlink=True)
    for side in ('L','R'):
        for band in (1,2):
            p=modules['hood_%d_%s'%(band,side)].location
            rod('V13 hood fold hinge',Vector((p.x*.7,0,p.z)),p,.032,steel,newparts)
    return dict(body_design='V-tapered torso, exposed green reactor, routed coolant and hydraulic hoses, folded rear panels and original exhaust manifold',
        hand_rest_pose='Palms medially toward legs; thumbs forward',reactor_emissive=True,
        rear_strip_segments=4,rear_strip_area_before=before,rear_strip_area_after=after,
        routed_conduits=hose_records,hose_deformation_validated=False)
