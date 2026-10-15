"""Verify V13 neutral palms-in rest pose, thumb side and finger curl."""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
bpy.context.scene.frame_set(1)
result={}
for side,sign in [('L',1),('R',-1)]:
 hand=bpy.data.objects['DESIGN - V11 articulated hand '+side]
 palm=hand.matrix_world.to_quaternion() @ Vector((0,-1,0))
 thumb=bpy.data.objects['DESIGN - V11 '+side+' thumb link0']
 index=bpy.data.objects['DESIGN - V11 '+side+' digit0 link0']
 little=bpy.data.objects['DESIGN - V11 '+side+' digit3 link0']
 assert palm.dot(Vector((-sign,0,0)))>.999
 assert thumb.matrix_world.translation.y>hand.matrix_world.translation.y
 assert index.location.x*sign<little.location.x*sign
 for digit in range(4):
  a=bpy.data.objects['DESIGN - V11 '+side+' digit%d link0'%digit]
  b=bpy.data.objects['DESIGN - V11 '+side+' digit%d link2'%digit]
  assert b.location.y<a.location.y
  for link in range(3):
   frame=bpy.data.objects['DESIGN - V11 '+side+' digit%d link%d'%(digit,link)]
   assert (frame.rotation_quaternion @ Vector((0,1,0))).y>0
 assert hand.matrix_world.determinant()>0
 result[side]=dict(palm_world=list(palm),thumb_forward=True,index_adjacent_to_thumb=True,curl_toward_palm=True)
path=Path(bpy.data.filepath)
(path.parent/'hand-anatomy-review.json').write_text(json.dumps(dict(blend_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),hands=result,scope='Static hand orientation, digit ordering and finger curl'),indent=2))
print('HAND ANATOMY',result)

