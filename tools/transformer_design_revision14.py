"""Closed green/black helmet and red compound optics from the user's reference.

Only replaces meshes attached to the existing head control; V13 body and neck
remain intact. This is authored 3D geometry, not an image projection.
"""
import math
import bpy
from mathutils import Vector


def revise_head(c):
    mesh,rod,ring,loft,material=[c[k] for k in ('mesh','rod','ring','loft','material')]
    head=bpy.data.objects['DESIGN - V11 interlocking cranial assembly']
    for o in list(c['hardware'].objects):
        if o.parent==head:bpy.data.objects.remove(o,do_unlink=True)
    head['design_revision']=14
    black=material('V14 satin black helmet',(.006,.009,.010),.35,.43)
    graphite=material('V14 graphite edges',(.025,.032,.036),.60,.43)
    silver=material('V14 machined mask',(.12,.15,.16),.78,.38)
    green=material('V14 emerald helmet inlay',(.009,.15,.030),.48,.36)
    recess=material('V14 deep optical and vent cavities',(.0015,.002,.002),.15,.65)
    red=material('V14 crimson optic glass',(.018,.0003,.0002),.15,.39,(.055,.0002,.0001))
    emitter=material('V14 red optical emitter',(.50,.002,.001),.2,.22,(8,.012,.004))
    for mat in (black,graphite,silver,green):
        tree=mat.node_tree;bsdf=tree.nodes.get('Principled BSDF')
        noise=tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=210
        noise.inputs['Detail'].default_value=2
        bump=tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.09;bump.inputs['Distance'].default_value=.0007
        tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);tree.links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])

    def panel(name,outline,mat,depth=.018,rise=.006):
        # Closed beveled plate with a restrained center ridge, not thin triangles.
        outline=list(outline);n=len(outline)
        normal=sum((Vector(outline[i]).cross(Vector(outline[(i+1)%n])) for i in range(n)),Vector())
        if normal.y<0:outline.reverse()
        center=sum((Vector(p) for p in outline),Vector())/n;center.y+=rise
        vs=outline+[tuple(center)]+[(x,y-depth,z) for x,y,z in outline]
        fs=[(i,(i+1)%n,n) for i in range(n)]+[tuple(range(2*n,n,-1))]
        fs.extend((i,n+1+i,n+1+(i+1)%n,(i+1)%n) for i in range(n))
        return mesh('V14 '+name,vs,fs,mat,head,.002)

    loft('V14 fitted cranial shell',[(.055,.075,.085),(.18,.145,.115),(.40,.225,.14),
        (.58,.22,.145),(.73,.13,.12),(.79,.055,.08)],black,head,.006)
    panel('central crown keel',[(-.043,.14,.79),(.043,.14,.79),(.061,.23,.65),
        (.036,.31,.46),(0,.337,.397),(-.036,.31,.46),(-.061,.23,.65)],black,.025,.016)
    for z in (.64,.663,.686):
        panel('forehead recessed vent',[(-.023,.255,z),(.023,.255,z),(.021,.257,z-.009),(-.021,.257,z-.009)],recess,.004,0)

    for s in (-1,1):
        def p(name,points,mat,depth=.018,rise=.006):
            return panel(name,[(s*x,y,z) for x,y,z in points],mat,depth,rise)

        # Broad crown armor and tapered green stripes follow the reference.
        p('crown side armor',[(.061,.16,.76),(.133,.09,.75),(.218,.075,.62),
            (.224,.175,.54),(.096,.27,.445),(.051,.27,.53)],black,.025,.011)
        p('long green crown inlay',[(.090,.184,.766),(.122,.145,.733),(.164,.164,.62),
            (.112,.25,.493),(.071,.294,.455),(.092,.235,.614)],green,.009,.003)
        p('outer temple armor',[(.221,.055,.65),(.258,-.005,.54),(.257,.10,.37),
            (.207,.23,.28),(.196,.24,.41),(.230,.17,.53)],graphite,.026,.008)
        p('temple green inlay',[(.238,.099,.575),(.251,.078,.52),(.233,.173,.418),
            (.212,.232,.378),(.215,.218,.465)],green,.010,.004)
        # Two integrated fins per side, rather than the old many-bladed crown.
        p('tall swept temple fin',[(.204,.05,.57),(.227,.008,.72),(.226,-.05,.86),
            (.185,.015,.68),(.171,.084,.62)],graphite,.021,.010)
        p('outer short fin',[(.246,.015,.38),(.275,-.035,.57),(.270,-.09,.67),
            (.236,-.03,.52)],black,.022,.008)

        # Each eye is a recessed swept triangle with five visible red lenses.
        p('deep triangular eye socket',[(.032,.323,.407),(.218,.246,.492),
            (.203,.302,.379),(.074,.345,.350)],recess,.025,.002)
        p('crimson eye backing',[(.052,.329,.407),(.202,.269,.475),
            (.187,.310,.387),(.081,.347,.367)],red,.006,.001)
        for x,z in ((.083,.387),(.112,.397),(.143,.409),(.171,.426),(.184,.451)):
            y=.363-(x-.08)*.45
            rod('V14 optical lens barrel',(s*x,y-.012,z),(s*x,y,z),.012,recess,head,20)
            ring('V14 red lens ring',(s*x,y+.001,z),.0085,.0026,(0,1,0),emitter,head)
            rod('V14 lens center',(s*x,y,z),(s*x,y+.002,z),.0035,emitter,head,16)
        p('heavy sloping brow',[(.018,.352,.43),(.207,.287,.541),(.242,.188,.575),
            (.221,.278,.492),(.062,.363,.405)],black,.025,.010)
        p('lower orbital silver edge',[(.061,.360,.346),(.197,.324,.379),(.218,.282,.412),
            (.190,.337,.346),(.095,.377,.307)],silver,.015,.005)

        # Cheek plate covers the mouth sides; vent and pistons remain recessed.
        p('cheek mechanical recess',[(.120,.285,.304),(.223,.215,.335),(.204,.239,.155),
            (.132,.296,.089)],recess,.019,.002)
        for row in range(5):
            z=.17+row*.023;x=.16+(z-.17)*.24
            rod('V14 cheek vent louver',(s*(x-.015),.300,z),(s*(x+.023),.284,z+.015),.0045,graphite,head,6)
        p('green jaw rail',[(.222,.26,.345),(.236,.197,.28),(.192,.263,.123),
            (.101,.329,.031),(.123,.343,.107),(.171,.304,.194)],green,.020,.005)
        p('angular cheek blade',[(.061,.383,.307),(.193,.332,.347),(.153,.349,.262),
            (.098,.386,.209),(.077,.399,.104),(.038,.403,.137)],graphite,.023,.010)
        p('silver mandible edge',[(.151,.352,.264),(.127,.369,.198),(.086,.399,.103),
            (.045,.402,.055),(.069,.405,.177),(.092,.388,.223)],silver,.014,.005)
        rod('V14 jaw pivot',(s*.204,.15,.205),(s*.232,.15,.205),.033,graphite,head,16)
        ring('V14 jaw pivot rim',(s*.232,.15,.205),.025,.005,(1,0,0),silver,head)
        rod('V14 rear jaw piston',(s*.21,.04,.30),(s*.13,.18,.10),.016,graphite,head)
        for row in range(3):
            z=.27+row*.115
            p('rear overlapping helmet armor',[(.09,-.13,z-.07),(.21,-.09,z),(.20,-.04,z+.13),(.08,-.13,z+.10)],black,.025,.004)
        for x,y,z in ((.215,.19,.51),(.195,.275,.31),(.120,.343,.12)):
            rod('V14 recessed face fastener',(s*x,y,z),(s*x,y+.006,z),.005,graphite,head,6)

    panel('nose bridge',[(-.025,.355,.431),(0,.383,.452),(.025,.355,.431),(.049,.380,.318),
        (0,.422,.279),(-.049,.380,.318)],graphite,.023,.010)
    panel('silver nasal tip',[(-.039,.399,.321),(0,.431,.297),(.039,.399,.321),
        (.025,.415,.267),(0,.435,.251),(-.025,.415,.267)],silver,.012,.005)
    panel('closed respirator backing',[(-.068,.381,.25),(0,.414,.278),(.068,.381,.25),
        (.061,.399,.111),(0,.410,.071),(-.061,.399,.111)],recess,.027,.002)
    for z in (.13,.154,.178,.202,.226):
        for s in (-1,1):
            rod('V14 respirator diagonal vane',(s*.014,.424,z),(s*.050,.407,z+.017),.005,graphite,head,6)
    rod('V14 respirator central spine',(0,.423,.116),(0,.428,.238),.010,graphite,head,6)
    panel('pointed silver chin',[(-.055,.408,.117),(0,.433,.136),(.055,.408,.117),
        (.038,.401,.040),(0,.371,-.015),(-.038,.401,.040)],silver,.023,.012)
    panel('chin center seam',[(-.004,.447,.12),(.004,.447,.12),(.003,.412,.042),(0,.381,-.002),(-.003,.412,.042)],graphite,.004,0)
    return dict(head_design='Closed black/green angular helmet, swept brow, ten red optical lenses, vented respirator and pointed silver mask',
        head_reference_applied=True,head_revision=14,head_hand_animation_validated=False)
