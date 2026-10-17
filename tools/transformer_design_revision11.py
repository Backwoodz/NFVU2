"""Layered metal head and skeletal mechanical hands for static study 11.

Authored mesh detail, not a pasted reference image or a transformation rig.
Original car assemblies, neon and the supported reference rider are retained.
"""
import math
import bpy
from mathutils import Matrix, Vector


def revise_head_hands(ctx):
    node,rod,ring,box,loft,mesh,material=[ctx[k] for k in ('node','rod','ring','box','loft','mesh','material')]
    root,hardware=ctx['root'],ctx['hardware']
    old_head=bpy.data.objects['DESIGN - V10 layered mechanical head']
    removed=[]
    for o in list(hardware.objects):
        if o.parent==old_head or any(s in o.name for s in ('hand palm','finger proximal','finger distal','finger joint','opposing thumb','neck spine','neck vertebra')):
            removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.objects.remove(old_head,do_unlink=True)

    graphite=material('V11 deep mechanism',(.021,.029,.032),.8,.43)
    gunmetal=material('V11 worn gunmetal',(.038,.049,.056),.78,.40)
    titanium=material('V11 exposed machined metal',(.11,.137,.15),.83,.35)
    glint=material('V11 blade edges',(.28,.33,.35),.88,.27)
    bronze=material('V11 heat-darkened bearing',(.16,.105,.055),.83,.4)
    black=material('V11 optical cavities',(.003,.005,.005),.1,.72)
    pupil=material('V11 small green optical core',(.015,.4,.035),.25,.2,(.15,4,.38))
    for mat in (graphite,gunmetal,titanium):
        tree=mat.node_tree;bsdf=tree.nodes.get('Principled BSDF')
        noise=tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=180;noise.inputs['Detail'].default_value=3
        ramp=tree.nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position=.25;ramp.color_ramp.elements[0].color=(.22,.22,.22,1)
        ramp.color_ramp.elements[1].position=.75;ramp.color_ramp.elements[1].color=(.49,.49,.49,1)
        tree.links.new(noise.outputs['Fac'],ramp.inputs['Fac']);tree.links.new(ramp.outputs['Color'],bsdf.inputs['Roughness'])
        grain=tree.nodes.new('ShaderNodeTexNoise');grain.inputs['Scale'].default_value=37;grain.inputs['Detail'].default_value=4
        patina=tree.nodes.new('ShaderNodeValToRGB');base=tuple(bsdf.inputs['Base Color'].default_value)
        patina.color_ramp.elements[0].position=.30;patina.color_ramp.elements[0].color=tuple(c*.70 for c in base[:3])+(1,)
        patina.color_ramp.elements[1].position=.70;patina.color_ramp.elements[1].color=base
        tree.links.new(grain.outputs['Fac'],patina.inputs['Fac']);tree.links.new(patina.outputs['Color'],bsdf.inputs['Base Color'])
        bump=tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.0012
        tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);tree.links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])

    # Raised central ridges make sculpted metal facets, rather than flat polygons.
    def blade(name,outline,mat,parent,depth=.014,rise=.012):
        if name=='swept cranial blade':
            outline=[(x,y,.60+(z-.60)*.55 if z>.60 else z) for x,y,z in outline]
        n=len(outline);c=sum((Vector(p) for p in outline),Vector())/n;c.y+=rise
        vertices=list(outline)+[tuple(c)]+[(x,y-depth,z) for x,y,z in outline]
        faces=[(i,(i+1)%n,n) for i in range(n)]
        faces.append(tuple(range(2*n,n,-1)))
        faces.extend((i,n+1+i,n+1+(i+1)%n,(i+1)%n) for i in range(n))
        o=mesh('V11 '+name,vertices,faces,mat,parent,.0015)
        return o

    head=node('V11 interlocking cranial assembly',(0,.10,3.92));head.parent=root
    # A small recessed structural core supports the face but leaves deep gaps.
    loft('V11 cranial support',[(.07,.105,.105),(.20,.17,.13),(.40,.205,.145),(.58,.16,.12),(.68,.09,.08)],graphite,head,.004)
    rod('V11 neck main bearing',(0,.035,3.74),(0,.09,4.06),.090,graphite)
    for i in range(4):
        z=3.77+i*.040
        ring('V11 cervical bearing',(0,.075,z),.097,.011,(0,0,1),titanium)
    for sign in (-1,1):
        rod('V11 neck actuator',(sign*.15,.055,3.77),(sign*.125,.15,4.09),.023,gunmetal)
        rod('V11 neck ram',(sign*.133,.126,3.96),(sign*.115,.17,4.14),.012,glint)
        # Swept crown: long curved, irregular blades anchored at the temple.
        for row in range(3):
            x=.14+row*.05
            blade('swept cranial blade',[(sign*(x-.035),.09-row*.028,.43),
                (sign*(x+.025),.025-row*.034,.55),
                (sign*(x+.02),-.025-row*.035,.70+row*.035),
                (sign*(x-.005),-.065-row*.035,.80+row*.035),
                (sign*(x-.045),.055-row*.027,.66),
                (sign*(x-.053),.135-row*.029,.50)],gunmetal if row%2 else titanium,head,.023,.021)
        for row in range(3):
            z=.46+row*.078;x=.15-row*.035
            blade('forehead interlock',[(sign*.020,.232,z-.028),(sign*(x+.044),.177,z+.017),
                (sign*(x+.019),.126,z+.105),(sign*.026,.196,z+.072)],titanium if row==1 else gunmetal,head,.017,.024)
            rod('forehead seam edge',(sign*.036,.241,z+.013),(sign*(x+.025),.191,z+.051),.004,glint,head,6)
        # Deep almond-shaped orbital apertures, tiny bright pupils and dark optics.
        blade('orbital cavity',[(sign*.025,.253,.451),(sign*.135,.250,.490),(sign*.248,.162,.526),
            (sign*.224,.226,.440),(sign*.109,.287,.414)],black,head,.012,.003)
        eye=Vector((sign*.128,.275,.448))
        rod('optical barrel',eye-Vector((0,.022,0)),eye,.016,black,head,16)
        ring('optical retaining ring',eye,.012,.003,(0,1,0),graphite,head)
        blade('green optical pupil',[(eye.x-sign*.013,eye.y+.005,eye.z-.003),
            (eye.x+sign*.014,eye.y+.005,eye.z+.005),(eye.x+sign*.008,eye.y+.006,eye.z-.003),
            (eye.x-sign*.007,eye.y+.006,eye.z-.005)],pupil,head,.004,.001)
        for row in range(3):
            dz=row*.022
            blade('overlapping predatory brow',[(sign*(.025+row*.043),.285-row*.017,.474+dz),
                (sign*(.143+row*.036),.224-row*.025,.513+dz),
                (sign*(.206+row*.02),.168-row*.034,.574+dz),
                (sign*(.158+row*.025),.232-row*.024,.491+dz)],gunmetal if row==1 else titanium,head,.012,.015)
        blade('deep orbital overhang',[(sign*.030,.327,.464),(sign*.12,.312,.489),
            (sign*.241,.223,.529),(sign*.19,.284,.480),(sign*.073,.336,.444)],gunmetal,head,.022,.012)
        blade('lower orbital knife',[(sign*.04,.307,.409),(sign*.142,.279,.430),
            (sign*.227,.228,.450),(sign*.191,.238,.391),(sign*.098,.307,.365)],titanium,head,.019,.012)
        # Smaller diagonal cheek scales expose tendons and pivots between them.
        for row in range(4):
            z=.38-row*.051;x=[.17,.185,.178,.211][row]
            blade('cheek interlocking scale',[(sign*.087,.316,z+.014),(sign*x,.252,z+.029),
                (sign*(x+.047),.167,z+[.052,.038,.064,.046][row]),(sign*(x+.027),.204,z-.001),
                (sign*[.121,.108,.142,.126][row],.318,z-.036)],gunmetal if row%2 else titanium,head,.012,.014)
            rod('cheek recessed tendon',(sign*.224,.12,z+.025),(sign*.12,.287,z-.043),.012,graphite,head)
            rod('cheek tendon piston',(sign*.17,.22,z-.012),(sign*.095,.312,z-.052),.006,glint,head,8)
        # Open mandible frame with serrated outer armor and paired jaw hydraulics.
        rod('jaw pivot',(sign*.208,.024,.285),(sign*.253,.024,.285),.050,graphite,head,16)
        ring('jaw bearing race',(sign*.252,.024,.285),.039,.008,(1,0,0),titanium,head)
        rod('mandible jaw link',(sign*.227,.07,.26),(sign*.126,.255,.084),.026,gunmetal,head)
        rod('jaw cylinder',(sign*.237,.145,.33),(sign*.183,.217,.205),.021,titanium,head)
        rod('jaw polished ram',(sign*.19,.21,.218),(sign*.116,.283,.113),.009,glint,head)
        blade('mandible outer blade',[(sign*.205,.205,.235),(sign*.247,.086,.312),
            (sign*.235,.068,.172),(sign*.159,.216,.064),(sign*.048,.30,-.004),
            (sign*.096,.326,.105)],gunmetal,head,.027,.029)
        blade('mandible cheek edge',[(sign*.204,.243,.235),(sign*.185,.278,.161),
            (sign*.090,.329,.058),(sign*.142,.312,.157)],titanium,head,.012,.012)
        for row in range(4):
            z=.32+row*.04
            rod('temple service conduit',(sign*.245,-.045,z),(sign*.23,.097,z+.012),.009,bronze if row==0 else graphite,head,8)
            ring('temple conduit collar',(sign*.24,.003,z+.006),.011,.003,(0,1,0),gunmetal,head)
        for row in range(3):
            blade('rear cranial overlapping shell',[(sign*.13,-.125,.22+row*.13),(sign*.24,-.08,.32+row*.13),
                (sign*.20,-.065,.44+row*.13),(sign*.075,-.145,.40+row*.13)],gunmetal,head,.025,.015)
        for x,y,z in ((.201,.24,.364),(.168,.294,.18),(.207,.147,.57)):
            rod('small face fastener',(sign*x,y,z),(sign*x,y+.008,z),.006,graphite,head,8)
    # Central spear is part of the forehead/nose mechanism, not a broad flat mask.
    blade('central forehead spear',[(-.039,.192,.67),(0,.04,.76),(.039,.192,.67),
        (.028,.276,.49),(0,.319,.434),(-.028,.276,.49)],titanium,head,.022,.027)
    blade('nasal bridge',[(-.028,.316,.465),(0,.351,.49),(.028,.316,.465),
        (.043,.333,.32),(0,.377,.276),(-.043,.333,.32)],gunmetal,head,.020,.015)
    for sign in (-1,1):
        blade('nasal armor slip',[(sign*.006,.357,.402),(sign*.033,.339,.369),(sign*.025,.369,.303),(sign*.009,.380,.317)],titanium,head,.009,.005)
    blade('mouth mechanism recess',[(-.112,.284,.253),(0,.307,.270),(.112,.284,.253),
        (.101,.302,.157),(0,.326,.137),(-.101,.302,.157)],black,head,.022,.003)
    for sign in (-1,1):
        blade('upper mouth linkage',[(0,.353,.258),(sign*.107,.321,.267),(sign*.126,.287,.236),
            (sign*.033,.355,.236)],gunmetal,head,.014,.011)
    for i in range(7):
        x=(i-3)*.023
        # Small uneven metal engagement teeth in a recessed narrow mouth.
        z=.239-abs(i-3)*.002
        blade('upper jaw tooth',[(x-.008,.331,z),(x+.008,.331,z),(x+.004,.342,z-.025-(i%2)*.006),(x-.004,.342,z-.025)],titanium,head,.012,.004)
        z=.175+abs(i-3)*.002
        blade('lower jaw tooth',[(x-.008,.334,z),(x-.003,.344,z+.024),(x+.004,.344,z+.021),(x+.008,.334,z)],gunmetal,head,.012,.004)
    blade('chin prow',[(-.075,.318,.143),(0,.367,.159),(.075,.318,.143),
        (.054,.322,.069),(0,.312,-.013),(-.054,.322,.069)],titanium,head,.023,.024)
    blade('chin central recess',[(-.012,.37,.123),(0,.38,.143),(.012,.37,.123),(0,.337,.028)],graphite,head,.008,.003)

    # Hand frame: four individually posed three-link fingers and opposed thumb.
    hand_nodes=[];digit_links=[]
    for side,sign in [('L',1),('R',-1)]:
        hand=node('V11 articulated hand '+side,(sign*1.30,.39,2.24));hand.parent=root
        hand.rotation_quaternion=ctx['quat']((0,0,1),sign*12)
        corrected=ctx['args'].revision>=12
        # V13 neutral palms-in pose needs the thumb/index edge forward.
        # Keep V12 reproducible, but correct its lateral digit ordering here.
        hand_sign=sign if ctx['args'].revision>=13 else (-sign if corrected else sign)
        curl=-1 if corrected else 1
        def digit_orientation(delta):
            if not corrected:return delta.to_track_quat('Z','Y')
            # Explicit dorsal axis: generic tracking can roll a downward link
            # 180 degrees and put its armor on the palm side.
            z=delta.normalized()
            y=(Vector((0,1,0))-z*z.y).normalized()
            x=y.cross(z).normalized()
            return Matrix((x,y,z)).transposed().to_quaternion()
        hand_nodes.append(hand)
        rod('V11 wrist spindle',(0,0,-.014),(0,0,-.08),.040,graphite,hand)
        for z in (-.02,-.061):ring('V11 wrist bearing',(0,0,z),.049,.006,(0,0,1),titanium,hand)
        loft('V11 palm skeleton',[(-.06,.082,.06),(-.13,.137,.068),(-.26,.16,.075),(-.31,.139,.055)],graphite,hand,.006)
        for j in range(4):
            x=(j-1.5)*.088;length=[.36,.405,.38,.30][j]
            # Intentionally unequal curl and spread keep the resting hand alive.
            spread=(j-1.5)*.037
            base=Vector((hand_sign*x,.015,-.285+(.021 if j in (0,3) else 0)))
            p1=base+Vector((hand_sign*spread,-.025*curl,-length*.43))
            p2=p1+Vector((hand_sign*spread*.45,curl*(.058+(.017*j)),-length*.33))
            p3=p2+Vector((-hand_sign*spread*.10,curl*(.068+(.008*j)),-length*.21))
            points=[base,p1,p2,p3]
            rod('V11 metacarpal rail',(hand_sign*x*.48,-.009,-.07),base,.026,gunmetal,hand)
            rod('V11 dorsal palm tendon',(hand_sign*x*.5,.077,-.105),base+Vector((0,.064,.016)),.009,glint,hand,8)
            blade('dorsal metacarpal blade',[(hand_sign*(x-.031),.084,-.143),(hand_sign*(x+.028),.088,-.177),
                (hand_sign*(x+.036),.092,base.z-.015),(hand_sign*(x-.034),.092,base.z+.002)],titanium if j%2 else gunmetal,hand,.018,.009)
            for link,(a,b) in enumerate(zip(points,points[1:])):
                delta=b-a;frame=node('V11 '+side+' digit%d link%d'%(j,link),a,digit_orientation(delta));frame.parent=hand
                radius=.033-link*.005;l=delta.length
                loft('V11 finger phalanx',[(0,radius*.82,radius*.85),(.026,radius,radius),
                    (l*.65,radius*.82,radius*.82),(l,radius*.61,radius*.61)],gunmetal,frame,.003)
                for offset in (-1,1):
                    rod('V11 finger side tendon',(offset*radius*.77,-radius*.73,.017),
                        (offset*radius*.61,-radius*.73,l-.012),.005,glint,frame,8)
                blade('finger dorsal scute',[(-radius*.85,radius*.84,.015),(0,radius*1.18,.005),
                    (radius*.85,radius*.84,.015),(radius*.64,radius*.9,l*.81),(0,radius*1.1,l*.94),(-radius*.64,radius*.9,l*.81)],titanium if link!=1 else graphite,frame,.009,.004)
                rod('V11 finger knuckle axle',a+Vector((-.039,0,0)),a+Vector((.039,0,0)),.027-link*.003,graphite,hand,12)
                for direction in (-1,1):
                    ring('V11 finger bearing',a+Vector((direction*.039,0,0)),.023-link*.003,.005,(1,0,0),titanium,hand)
                digit_links.append(frame.name)
            tip=points[-1]
            blade('curved finger gripping tip',[(tip.x-.020,tip.y+curl*.01,tip.z+.023),
                (tip.x+.020,tip.y+curl*.01,tip.z+.023),(tip.x+.013,tip.y+curl*.040,tip.z-.023),
                (tip.x,tip.y+curl*.084,tip.z-.075),(tip.x-.012,tip.y+curl*.042,tip.z-.025)],titanium,hand,.017,.012)
        thumb=[Vector((-hand_sign*.115,-.01,-.12)),Vector((-hand_sign*.222,curl*.018,-.224)),
               Vector((-hand_sign*.235,curl*.131,-.33)),Vector((-hand_sign*.191,curl*.208,-.377))]
        for link,(a,b) in enumerate(zip(thumb,thumb[1:])):
            d=b-a;frame=node('V11 '+side+' thumb link%d'%link,a,digit_orientation(d));frame.parent=hand
            loft('V11 thumb phalanx',[(0,.042,.035),(d.length*.30,.043,.038),(d.length,.027,.026)],gunmetal,frame,.004)
            rod('V11 thumb tendon',a+Vector((0,.035,0)),b+Vector((0,.025,0)),.008,glint,hand)
            ring('V11 thumb swivel',a,.037,.008,(1,0,0),titanium,hand)
            digit_links.append(frame.name)
        p=thumb[-1]
        blade('opposing thumb talon',[(p.x-hand_sign*.025,p.y,p.z+.015),(p.x+hand_sign*.025,p.y,p.z+.015),
            (p.x+hand_sign*.052,p.y+curl*.048,p.z-.045),(p.x+hand_sign*.084,p.y+curl*.041,p.z-.055),
            (p.x-hand_sign*.01,p.y+curl*.033,p.z-.025)],titanium,hand,.022,.012)
        for sign2 in (-1,1):
            rod('V11 wrist flexor',(sign2*.072,-.045,.025),(sign2*.12,-.057,-.20),.017,gunmetal,hand)
            rod('V11 wrist flexor shaft',(sign2*.10,-.054,-.14),(sign2*.13,-.063,-.252),.008,glint,hand)
    return dict(head_design='Interlocking swept cranial blades, deep sockets with green pupils, cheek tendons and toothed mandible',
                hand_design='Four three-link fingers plus three-link opposed thumb per hand; unequal curl, exposed tendons and gripping tips',
                hand_count=2,digits_per_hand=5,hand_link_nodes=len(digit_links),
                corrected_hand_chirality=ctx['args'].revision>=12,
                head_hand_animation_validated=False)
