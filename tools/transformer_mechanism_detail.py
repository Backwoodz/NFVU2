"""Authored joint housings and ribbed limb sleeves for the mechanism study.

These travel with the articulated bones; no detail floats on world-space paths.
Packed volume and mechanism collision are still separate engineering checks.
"""
import math
import bpy


def add_details(bones, mechanism, controls, box, materials):
    dark,metal,green,eye=materials
    def move(obj,parent,name):
        for c in list(obj.users_collection): c.objects.unlink(obj)
        mechanism.objects.link(obj); obj.parent=parent; obj.name='DETAIL - '+name
        obj['provenance']='Authored robot mechanism'
        return obj
    def ring(parent,z,radius,mat):
        # Mesh construction also works in Blender 2.80 background mode, whose
        # torus operator expects a VIEW_3D context.
        vertices=[]; faces=[]
        for i in range(24):
            a=i*math.tau/24
            for j in range(6):
                b=j*math.tau/6; r=radius+.018*math.cos(b)
                vertices.append((r*math.cos(a),r*math.sin(a),.018*math.sin(b)))
        for i in range(24):
            for j in range(6):
                faces.append((i*6+j,((i+1)%24)*6+j,((i+1)%24)*6+(j+1)%6,i*6+(j+1)%6))
        mesh=bpy.data.meshes.new('Shaft collar'); mesh.from_pydata(vertices,[],faces); mesh.update()
        obj=bpy.data.objects.new('DETAIL - shaft collar',mesh); mechanism.objects.link(obj)
        obj.parent=parent; obj.location=(0,0,z)
        obj.data.materials.append(mat)
        for polygon in obj.data.polygons: polygon.use_smooth=True
    for side in ('L','R'):
        for role,length,width in [('upperarm_',.78,.26),('forearm_',.78,.23),('thigh_',1.0,.33),('shin_',1.12,.28)]:
            bone=bones[role+side]
            distal=bones[{'upperarm_':'forearm_','forearm_':'hand_','thigh_':'shin_','shin_':'foot_'}[role]+side]
            slider=bpy.data.objects.new('DETAIL - distal sliding sleeve '+role+side,None)
            controls.objects.link(slider);slider.parent=bone
            driver=slider.driver_add('location',2).driver
            var=driver.variables.new();var.name='span';var.type='LOC_DIFF'
            var.targets[0].id=bone;var.targets[1].id=distal
            driver.expression='span'
            for z in (.07,.16): ring(bone,z,width*.50,metal)
            ring(slider,-.12,width*.50,metal)
            # Separate outer sleeve plates leave a visible piston channel.
            for sign in (-1,1):
                plate=box(role+'segment cover',(sign*width*.35,0,length*.20),
                          (width*.33,width*.9,length*.28),dark,bone,.045)
                box(role+'wear strip',(sign*width*.52,0,length*.20),
                    (.018,width*.54,length*.22),metal,bone,.007)
            for row in range(4):
                box(role+'bellows rib',(0,0,-.19+row*.032),
                    (width*.72,width*.74,.018),dark,slider,.008)
            # Hinged axle and visible bolt heads on the outside of the joint.
            bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=width*.57,depth=width*1.12)
            axle=move(bpy.context.object,bone,'joint axle'); axle.location=(0,0,0)
            axle.rotation_euler.y=math.pi/2; axle.data.materials.append(dark)
            for sign in (-1,1):
                for i in range(6):
                    angle=i*math.tau/6
                    bolt=box('joint bolt',(sign*width*.58,math.cos(angle)*width*.40,math.sin(angle)*width*.40),
                             (.025,.035,.035),metal,bone,.007)
        # Thigh and knee fronts gain small integral armor facets.
        box('knee cap '+side,(0,.12,0),(.29,.17,.25),green,bones['shin_'+side],.055)
    # Spine collars and a narrow central keel visually tie the hood halves together.
    for z in (.18,.40,.62,.84,1.02): ring(bones['spine'],z,.17,dark)
    keel=box('sternum mechanical keel',(0,.22,-.03),(.10,.16,.73),dark,bones['thorax'],.025)
    from transformer_kinematics import phase
    for frame in range(1,266):
        t=min(1.,(frame-1)/120.) if frame<=145 else (265-frame)/120.
        keel.rotation_euler.x=math.pi/2*(1-phase(t,.30,.80))
        keel.keyframe_insert('rotation_euler',frame=frame)
    for z in (-.27,-.09,.09,.27):
        box('sternum bridge',(0,.27,z),(.28,.06,.045),metal,bones['thorax'],.008)
