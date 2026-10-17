"""Seven persistent light rails: four authored underbody tubes and three trunk strips.

VehicleNeon currently projects light without physical tubes. The four underbody
tubes below are newly authored hardware using its four horizontal light positions,
with their mounting height lowered to clear the bumpers and exhaust.
The three trunk strips preserve VehicleShowroom's dimensions and placement.
"""
import math
import json
from pathlib import Path
import bpy
from mathutils import Vector, Quaternion
from transformer_kinematics import phase, matrix


def build_neon(scene,nodes,specs,bones,poses,controls,mechanism,box,rod,materials):
    dark,metal,green,eye=materials
    material=bpy.data.materials.new('NEON - continuous green tubes');material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(.04,.9,.10,1)
    shader.inputs['Emission'].default_value=(.12,5,.30,1)
    shader.inputs['Roughness'].default_value=.25
    tubes=[];rails={}

    def control(name,parent):
        obj=bpy.data.objects.new('NEON - '+name,None);controls.objects.link(obj)
        obj.parent=parent;obj.rotation_mode='QUATERNION';return obj

    def rail(name,parent,position,axis,length,rectangular=False):
        base=control(name,parent);base.location=position
        key='neon_'+name.replace('underbody side ','side_').replace('underbody ','').replace('trunk ','trunk_')
        rails[key]=base
        base.rotation_quaternion=Vector(axis).to_track_quat('Z','Y')
        if rectangular:
            tube=box('NEON '+name,(0,0,0),(.012,.012,length),material,base,.003)
        else:
            bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.012,depth=length)
            tube=bpy.context.object
            for collection in list(tube.users_collection):collection.objects.unlink(tube)
            mechanism.objects.link(tube);tube.parent=base;tube.location=(0,0,0)
            tube.data.materials.append(material)
            for face in tube.data.polygons:face.use_smooth=True
        tube.name='NEON TUBE - '+name;tube['continuous_unsegmented']=True
        tube['provenance']='Authored hardware; trunk dimensions follow VehicleShowroom' if rectangular else 'Authored tube using VehicleNeon horizontal position; mounting height adjusted'
        tubes.append(tube)
        for sign in (-1,1):
            box('neon end cap',(0,0,sign*(length*.5+.01)),(.032,.032,.025),metal,base,.006)
            box('neon saddle clamp',(0,-.021,sign*length*.36),(.044,.042,.032),dark,base,.006)
            box('neon mounting tab',(0,-.045,sign*length*.36),(.07,.016,.05),metal,base,.004)
            clamp=control(name+' clamp',base);clamp.location=(0,-.045,sign*length*.36)
            target=Vector(position)+base.rotation_quaternion @ clamp.location
            surface=[child.matrix_local @ vertex.co for child in parent.children
                     if child.type=='MESH' and child.get('source_part') for vertex in child.data.vertices]
            mount=control(name+' fixed mounting point',parent)
            mount.location=min(surface,key=lambda p:(p-target).length_squared) if surface else Vector((0,0,0))
            rod('neon bracket '+name,mount,clamp,.014,metal)
        light=bpy.data.lights.new('Neon spill '+name,'POINT');light.energy=12;light.color=(.06,1,.14)
        light.shadow_soft_size=.22
        obj=bpy.data.objects.new(light.name,light);mechanism.objects.link(obj);obj.parent=base
        return base

    for side,sign in [('L',1),('R',-1)]:
        role='door_front_'+side
        position=Vector((sign*.75,0,.12))-(specs[role]['source']+Vector((0,0,.14)))
        base=rail('underbody side '+side,nodes[role],position,(0,1,0),1.50)
        for frame in range(1,266):
            t=min(1.,(frame-1)/120.) if frame<=145 else (265-frame)/120.
            scene.frame_set(frame)
            release=.16*phase(t,.04,.15)*(1-phase(t,.38,.55))
            base.location=position+nodes[role].matrix_world.to_quaternion().inverted() @ Vector((sign*release,0,0))
            base.keyframe_insert('location',frame=frame)
    for name,start,end,parent in [
            ('underbody front',(0,1.65,.10),(0,.72,3.0),'thorax'),
            ('underbody rear',(0,-1.65,.10),(0,-1.40,2.45),'pelvis')]:
        carrier=control(name+' folding carrier',bones[parent])
        for frame in range(1,266):
            t=min(1.,(frame-1)/120.) if frame<=145 else (265-frame)/120.
            p=Vector(start).lerp(Vector(end),phase(t,.20,.84))
            p.y+=(1 if 'front' in name else -1)*.45*math.sin(math.pi*phase(t,.12,.84))
            carrier.location=poses[frame][parent].inverted() @ p
            carrier.keyframe_insert('location',frame=frame)
        rod(name+' rail support',bones[parent],carrier,.022,metal)
        rail(name,carrier,(0,0,0),(1,0,0),1.10)
    hatch_source=specs['hatch']['source']+Vector((0,0,.14))
    for name,p,axis,length in [
            ('trunk L',(-.59,-1.43,.82),(0,1,0),.72),
            ('trunk R',(.59,-1.43,.82),(0,1,0),.72),
            ('trunk rear',(0,-1.89,.71),(1,0,0),1.03)]:
        rail(name,nodes['hatch'],Vector(p)-hatch_source,axis,length,True)
    path_file=Path(__file__).with_name('transformer_neon_paths.json')
    corrections=json.loads(path_file.read_text()) if path_file.exists() else {}
    if corrections:
        locations={}
        for frame in range(1,122):
            scene.frame_set(frame);locations[frame]={}
            for key,base in rails.items():
                delta=Vector(corrections.get(str(frame),{}).get(key,(0,0,0)))
                locations[frame][key]=base.location+base.parent.matrix_world.to_3x3().inverted() @ delta
        for frame in range(1,266):
            forward=min(frame,121) if frame<=145 else 266-frame
            for key,base in rails.items():
                base.location=locations[forward][key];base.keyframe_insert('location',frame=frame)
    return dict(neon_tube_count=len(tubes),neon_trunk_strips=3,neon_authored_underbody_tubes=4,
                neon_unsegmented=True,neon_runtime_integration=False,neon_clearance_samples=len(corrections))
