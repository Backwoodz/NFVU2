"""Nested fixed-length sleeves replacing the study's stretchable panel rods."""
import math
import bpy
from mathutils import Vector


def build_telescopes(connections,ranges,poses,mechanism,controls,materials):
    dark,metal,green,eye=materials
    replacements=[]; new_connections=[]; rejected=[]
    for old,a,b in list(connections):
        if not old.name.startswith('LINK - panel carrier '): continue
        low,high=ranges[old.name]
        if low<.06:
            rejected.append(dict(carrier=old.name,minimum_span_m=low));continue
        count=max(2,int(math.ceil((high/low-1)/.70))+1)
        if count>8:
            # Large contraction ratios need folding links, not dozens of
            # implausibly tiny nested tubes. Horizontal folding preserves the
            # underfloor height throughout the transfer.
            count=max(2,int(math.ceil(high/.65/2))*2)
            length=high/(count*.98)
            pivots=[a]
            for index in range(1,count):
                pivot=bpy.data.objects.new('CARRIAGE folding knuckle '+str(index)+' - '+b.name,None)
                controls.objects.link(pivot);pivot.parent=a;pivots.append(pivot)
            pivots.append(b)
            for frame,(parent_matrix,end) in enumerate(poses[old.name],1):
                start=parent_matrix.translation
                delta=end-start;span=delta.length;direction=delta.normalized()
                side=Vector((0,0,1)).cross(direction)
                if side.length<1e-5:side=Vector((1,0,0))
                side.normalize()
                sign=-1 if '_R' in b.name else 1
                if side.x*sign<0:side=-side
                height=math.sqrt(max(0,length*length-(span/count)**2))
                for index,pivot in enumerate(pivots[1:-1],1):
                    point=start+delta*(index/count)+side*(height if index%2 else 0)
                    pivot.location=parent_matrix.inverted() @ point
                    pivot.keyframe_insert('location',frame=frame)
            for index,(first,last) in enumerate(zip(pivots,pivots[1:])):
                bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.019,depth=1)
                obj=bpy.context.object
                for c in list(obj.users_collection):c.objects.unlink(obj)
                mechanism.objects.link(obj);obj.name='CARRIAGE fixed folding link - '+b.name
                for vertex in obj.data.vertices:vertex.co.z+=.5
                obj.parent=first;obj.location=(0,0,0);obj.scale.z=length
                obj.data.materials.append(metal if index%2 else dark)
                track=obj.constraints.new('DAMPED_TRACK');track.target=last;track.track_axis='TRACK_Z'
                obj['fixed_link_length_m']=length
                new_connections.append((obj,first,last))
            connections.remove((old,a,b));bpy.data.objects.remove(old,do_unlink=True)
            replacements.append(dict(carrier=b.name,folding_links=count,fixed_link_length_m=length,
                                      minimum_span_m=low,maximum_span_m=high))
            continue
        length=high/(1+.70*(count-1))
        root=bpy.data.objects.new('CARRIAGE - '+old.name[7:],None)
        controls.objects.link(root);root.parent=a
        track=root.constraints.new('DAMPED_TRACK');track.target=b;track.track_axis='TRACK_Z'
        segments=[]
        for index in range(count):
            radius=.012+.002*(count-index-1)
            vertices=[];faces=[]
            inner=radius-.0012 if index<count-1 else 0
            for z in (0.,1.):
                for r in (radius,inner):
                    vertices.extend((r*math.cos(i*math.tau/16),r*math.sin(i*math.tau/16),z) for i in range(16))
            for i in range(16):
                j=(i+1)%16
                faces.extend([(i,j,32+j,32+i),(16+j,16+i,48+i,48+j),
                              (j,i,16+i,16+j),(32+i,32+j,48+j,48+i)])
            mesh=bpy.data.meshes.new('Fixed length hollow telescope sleeve')
            mesh.from_pydata(vertices,[],faces);mesh.update()
            obj=bpy.data.objects.new('CARRIAGE sleeve '+str(index)+' - '+b.name,mesh)
            mechanism.objects.link(obj);obj.parent=root;obj.scale.z=length
            obj.data.materials.append(metal if index%2 else dark)
            for face in mesh.polygons:face.use_smooth=True
            driver=obj.driver_add('location',2).driver
            var=driver.variables.new();var.name='span';var.type='LOC_DIFF'
            var.targets[0].id=a;var.targets[1].id=b
            driver.expression='(span-%.9f)*%.9f' % (length,index/(count-1))
            obj['fixed_sleeve_length_m']=length
            segments.append(obj)
        connections.remove((old,a,b))
        bpy.data.objects.remove(old,do_unlink=True)
        new_connections.append((segments[-1],a,b))
        replacements.append(dict(carrier=b.name,sleeves=count,sleeve_length_m=length,
                                  minimum_span_m=low,maximum_span_m=high))
    connections.extend(new_connections)
    return dict(telescopic_carriers=replacements,short_pivot_links=rejected,
                carrier_sleeves_fixed_length=True)
