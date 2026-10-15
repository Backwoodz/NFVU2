"""Art-direction corrections to the v9 static study; no transformation claims."""
import json
from collections import defaultdict
import bpy
from mathutils import Vector
from transformer_parts import split, area


def revise(ctx):
    node,pose,quat,rod,ring,box,plate,loft=[ctx[k] for k in ('node','pose','quat','rod','ring','box','plate','loft')]
    root,hardware,originals,modules,groups=[ctx[k] for k in ('root','hardware','originals','modules','groups')]
    dark,steel,edge,paint,rubber,copper,optic=[ctx[k] for k in ('dark','steel','edge','paint','rubber','copper','optic')]
    RX=ctx['RX']
    # The static import did not apply VehicleLighting's per-part emission.
    # Reuse its explicit source metadata; never paint whole lamp housings red.
    model=json.loads((ctx['ROOT']/'.local/model/body.json').read_text())
    light_roles={p['name']:p.get('lightRole','') for p in model['parts']}
    emitting=[]
    for o in originals.objects:
        source=o.name.split('__')[0];role=light_roles.get(source)
        if role not in ('headlight','taillight'):continue
        original=o.data.materials[0];mat=original.copy();mat.name='V10 active '+role+' '+source
        shader=mat.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Emission'].default_value=(2.5,2.35,2.05,1) if role=='headlight' else (.8,.012,.004,1)
        o.data.materials[0]=mat;emitting.append(source)
    assert len(emitting)==4, 'Expected precisely two front and two rear emitting surfaces'
    # Remove the closed shoulder shell and toy-like round ear/crest helmet.
    old_head=ctx['head']
    for o in list(hardware.objects):
        if o.parent==old_head or any(s in o.name for s in ('shoulder crown','shoulder green prow','shoulder ventilation','folded fender hinge','panel hinge lamp_','panel hinge collar lamp_')):
            bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.objects.remove(old_head,do_unlink=True)

    # Complete lamp assemblies retain their original meshes, lens contours and UVs.
    # Face forward at the front collar; wheels sit clear above/outboard of them.
    for side,sign in [('L',1),('R',-1)]:
        pose('lamp_'+side,(sign*1.03,.78,3.75),quat((0,0,1),-sign*12) @ quat((1,0,0),-38))
        pose('front_wheel_'+side,(sign*1.43,-.04,3.98),quat((0,0,1),sign*20) @ quat((0,1,0),-sign*48) @ RX)
        pose('fender_front_'+side,(sign*1.52,-.22,3.42),quat((0,1,0),sign*28) @ RX)
        pose('fender_back_'+side,(sign*1.29,-.38,3.45),quat((0,0,1),sign*27) @ quat((0,1,0),sign*63) @ RX)
        # Original rear lamps: local +Z lens normal tips toward world -Y.
        pose('taillamp_'+side,(sign*1.35,-.99,3.77),quat((0,0,1),sign*17) @ quat((1,0,0),48))
        rod('V10 shoulder load arm '+side,(sign*.87,-.02,3.54),(sign*1.43,-.04,3.98),.105,dark)
        rod('V10 shoulder exposed ram '+side,(sign*.76,.01,3.68),(sign*1.28,.01,3.96),.042,edge)
        ring('V10 shoulder hinge '+side,(sign*1.14,-.04,3.75),.15,.035,(0,1,0),steel)
        for p in ((sign*1.03,.78,3.75),(sign*1.35,-.99,3.77),(sign*1.52,-.22,3.42),(sign*1.29,-.38,3.45)):
            rod('V10 original assembly carrier '+side,(sign*1.10,-.02,3.57),p,.035,steel)
        box('V10 headlight mounting spine '+side,(sign*1.03,.62,3.72),(.12,.10,.27),dark)

    # Compact head, small sunken optical slits and a broken-up metal face.
    # No broad cartoon eyes, gold ear rings or pointed crown.
    head=node('V10 layered mechanical head',(0,.10,3.94));head.parent=root
    loft('V10 cranial frame',[(0,.105,.11),(.11,.18,.14),(.34,.245,.19),(.51,.22,.17),(.61,.13,.12)],dark,head,.006)
    for sign in (-1,1):
        plate('V10 rear helmet shell',[(sign*.10,-.14,.08),(sign*.23,-.12,.32),(sign*.23,-.09,.48),(sign*.12,-.06,.63),(sign*.06,-.15,.49)],.055,steel,head)
        plate('V10 forehead armor',[(sign*.018,.207,.47),(sign*.18,.19,.53),(sign*.145,.11,.615),(sign*.018,.155,.625)],.045,steel,head)
        plate('V10 orbital recess',[(sign*.025,.253,.417),(sign*.207,.221,.455),(sign*.19,.232,.369),(sign*.035,.268,.379)],.018,rubber,head)
        plate('V10 narrow optical slit',[(sign*.042,.270,.406),(sign*.186,.243,.432),(sign*.164,.250,.416),(sign*.051,.273,.393)],.010,optic,head)
        plate('V10 heavy brow',[(sign*.018,.285,.432),(sign*.22,.232,.482),(sign*.225,.225,.457),(sign*.035,.282,.416)],.045,dark,head)
        plate('V10 orbital rim',[(sign*.05,.287,.377),(sign*.19,.247,.402),(sign*.232,.199,.335),(sign*.14,.275,.315)],.022,steel,head)
        plate('V10 swept cheek',[(sign*.232,.18,.36),(sign*.172,.285,.29),(sign*.105,.29,.14),(sign*.205,.16,.20)],.036,dark,head)
        for row in range(3):
            z=.18+row*.056
            rod('V10 exposed cheek linkage',(sign*.208,.165,z+.035),(sign*.117,.277,z-.022),.013,edge,head,8)
        plate('V10 jaw armor',[(sign*.055,.278,.155),(sign*.161,.245,.19),(sign*.173,.176,.10),(sign*.085,.245,.022)],.035,steel,head)
        # Small recessed temple bearing surrounded by rails, rather than a large ear.
        rod('V10 temple hub',(sign*.233,-.012,.32),(sign*.258,-.012,.32),.054,dark,head)
        for i in range(4):
            plate('V10 temporal cooling scale',[(sign*.235,.025,.35+i*.038),(sign*.25,-.075,.37+i*.038),(sign*.22,-.16,.32+i*.038)],.017,steel,head)
        for y,z in ((.13,.50),(.12,.22),(-.10,.43)):
            rod('V10 helmet fastener',(sign*.225,y,z),(sign*.239,y,z),.013,edge,head,8)
    plate('V10 nose bridge',[(-.029,.290,.43),(0,.33,.445),(.029,.290,.43),(.037,.32,.30),(0,.35,.28),(-.037,.32,.30)],.035,dark,head)
    # Angled louver plates instead of a smiling mouth or a flat grille.
    for row in range(4):
        z=.265-row*.036;w=.077-row*.008
        plate('V10 respirator louver',[(-w,.294,z),(0,.337,z-.015),(w,.294,z),(w*.84,.304,z-.020),(0,.345,z-.035),(-w*.84,.304,z-.020)],.018,steel,head)
    plate('V10 chin keel',[(-.056,.28,.112),(0,.321,.127),(.056,.28,.112),(.043,.264,.025),(0,.281,.004),(-.043,.264,.025)],.028,dark,head)
    rod('V10 exposed crown rail',(0,-.13,.52),(0,.10,.616),.022,edge,head,8)

    # Cut the broad original floor sheets into hinged lamellae. No deletion,
    # scaling, glass cutting or texture reprojection: preserve every triangle's area.
    floor_before=floor_after=0.;count=0
    for side,sign in [('L',1),('R',-1)]:
        for index in range(3):
            role='floor%d_%s'%(index,side);objects=groups[role]
            extent=max(abs(v.co.y) for o in objects for v in o.data.vertices)
            cuts=[(1,-extent/3),(1,extent/3)]
            if index<2:cuts.append((0,0.))
            fragments=defaultdict(list);mats={}
            for o in objects:
                d=o.data;d.calc_normals_split();mats[o.name]=d.materials[0]
                for p in d.polygons:
                    loops=list(p.loop_indices)
                    for j in range(1,len(loops)-1):
                        tri=[tuple(d.vertices[d.loops[i].vertex_index].co)+tuple(d.loops[i].normal)+tuple(d.uv_layers.active.data[i].uv) for i in (loops[0],loops[j],loops[j+1])]
                        floor_before+=area(tri);polygons=[tri]
                        for axis,cut in cuts:polygons=[p2 for poly in polygons for p2 in split(poly,axis,cut)]
                        for poly in polygons:
                            for k in range(1,len(poly)-1):
                                t=[poly[0],poly[k],poly[k+1]]
                                if area(t)<1e-12:continue
                                band=sum(sum(v[1] for v in t)/3>c for c in (-extent/3,extent/3))
                                half=int(sum(v[0] for v in t)>0) if index<2 else 0
                                fragments[band,half,o.name].append(t);floor_after+=area(t)
            for band,half in sorted({(b,h) for b,h,n in fragments}):
                points=[v for (b,h,n),tris in fragments.items() if (b,h)==(band,half) for tri in tris for v in tri]
                center=Vector([(min(v[a] for v in points)+max(v[a] for v in points))/2 for a in range(3)])
                newrole=role+'_fold%d_%d'%(band,half);n=node(newrole);n.parent=root;modules[newrole]=n;count+=1
                if index<2:
                    side_of_leg=-1 if half==0 else 1
                    z=([1.96,1.73,1.50] if index==0 else [1.13,.87,.61])[band]
                    legx=sign*((.40+(2.16-z)*.30) if index==0 else (.68+(1.22-z)*.23))
                    n.location=(legx+side_of_leg*.17,(-.32 if index==0 else -.36),z)
                    n.rotation_quaternion=quat((0,0,1),side_of_leg*60) @ quat((1,0,0),-82+(band-1)*12)
                else:
                    n.location=(sign*(.43+band*.065),.14,1.98-band*.32)
                    n.rotation_quaternion=quat((0,0,1),sign*88) @ quat((1,0,0),-77+band*8)
                for (b,h,name),tris in fragments.items():
                    if (b,h)!=(band,half):continue
                    points=[v for tri in tris for v in tri]
                    d=bpy.data.meshes.new(name+' hinged fold')
                    d.from_pydata([tuple(Vector(v[:3])-center) for v in points],[],[tuple(range(i,i+3)) for i in range(0,len(points),3)])
                    d.update();uv=d.uv_layers.new(name='Original UV')
                    for loop in d.loops:uv.data[loop.index].uv=points[loop.vertex_index][6:8]
                    for p in d.polygons:p.use_smooth=True
                    d.use_auto_smooth=True;d.normals_split_custom_set_from_vertices([tuple(Vector(v[3:6]).normalized()) for v in points]);d.materials.append(mats[name])
                    o=bpy.data.objects.new(name+'__fold%d_%d'%(band,half),d);originals.objects.link(o);o.parent=n
                    o['module']=newrole;o['source_part']=name.split('__')[0];o['source_module']=role;o['source_local_pivot']=list(center)
                    o['provenance']='Original floor, losslessly cut into rigid folding lamellae';groups[newrole].append(o)
                ring('V10 floor hinge',n.location,.043,.012,(1,0,0),steel)
                if index<2:
                    # Rear-facing floor shells wrap outward around the leg, with
                    # a ribbed inner carrier and a bright narrow structural rim.
                    for x in (-.125,.125):
                        rod('V10 floor edge rib',(x,-.082,.018),(x,.082,.018),.009,steel,n,8)
                    box('V10 floor stiffener',(0,0,.019),(.26,.027,.021),dark,n,.004)
                anchor=Vector(n.location);anchor.y+=.16 if index<2 else -.16
                rod('V10 floor carrier '+newrole,anchor,n.location,.024,steel)
            for o in list(groups.pop(role)):bpy.data.objects.remove(o,do_unlink=True)
            bpy.data.objects.remove(modules.pop(role),do_unlink=True)
        # Rear-facing exposed mechanism follows the bent thigh/knee/calf chain.
        for a,b,label in [((sign*.49,-.28,1.98),(sign*.65,-.10,1.43),'hamstring'),((sign*.72,-.27,1.09),(sign*.88,-.38,.45),'calf')]:
            a,b=Vector(a),Vector(b)
            for offset in (-.09,.09):
                offset=Vector((offset,0,0));delta=b-a
                rod('V10 '+label+' cylinder',a+offset,a+offset+delta*.61,.051,steel)
                rod('V10 '+label+' polished ram',a+offset+delta*.5,b+offset,.027,edge)
                for t in (.1,.20,.30):ring('V10 actuator cooling collar',a+offset+delta*t,.054,.009,delta,dark)
            ring('V10 '+label+' yoke',b,.105,.024,(1,0,0),dark)
        rod('V10 rear knee axle',(sign*.49,.02,1.23),(sign*.85,.02,1.23),.12,dark)
        for z in (1.97,1.72,1.47):
            x=sign*(.40+(2.16-z)*.30)
            plate('V10 rear thigh chevron',[(x-.14,-.335,z+.06),(x,-.385,z+.025),(x+.14,-.335,z+.06),(x+.11,-.35,z-.015),(x,-.39,z-.04),(x-.11,-.35,z-.015)],.023,steel)
        rod('V10 Achilles clevis',(sign*.88,-.34,.43),(sign*.88,-.26,.15),.045,edge)
        box('V10 heel anchor',(sign*.88,-.245,.18),(.20,.16,.13),steel)
    assert abs(floor_before-floor_after)<1e-6
    return dict(floor_segments=count,floor_area_before=floor_before,floor_area_after=floor_after,
                head_design='Layered helmet, recessed narrow optics, mechanical respirator',
                front_wheel_mount='Outward/upward shoulder pads',
                lamp_layout='Original front lamps on front collar; original rear lamps on rear shoulders',
                active_light_surfaces=emitting)
