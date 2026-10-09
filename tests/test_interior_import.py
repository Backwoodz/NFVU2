import sys
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from export_interior import geometry_parts, FIT, scene_nodes

class InteriorImportTests(unittest.TestCase):
    def test_reflected_coordinates_preserve_winding_and_uv_seams(self):
        # Collada has separate position, normal and UV indices. One position has two UVs.
        geometry=ET.fromstring('''<geometry xmlns="http://www.collada.org/2005/11/COLLADASchema"><mesh>
          <source id="pos"><float_array>0 0 0 1 0 0 0 1 0</float_array><technique_common><accessor stride="3"/></technique_common></source>
          <source id="normal"><float_array>0 0 1</float_array><technique_common><accessor stride="3"/></technique_common></source>
          <source id="uv"><float_array>0 0 1 0 0 1 1 1</float_array><technique_common><accessor stride="2"/></technique_common></source>
          <vertices id="verts"><input semantic="POSITION" source="#pos"/></vertices>
          <triangles material="test"><input semantic="VERTEX" source="#verts" offset="0"/><input semantic="NORMAL" source="#normal" offset="1"/><input semantic="TEXCOORD" source="#uv" offset="2"/>
          <p>0 0 0 1 0 1 2 0 2 0 0 3 1 0 1 2 0 2</p></triangles>
        </mesh></geometry>''')
        p=list(geometry_parts(geometry,FIT,'Nis350Z_doorpanel_L',lambda _:0))[0]
        self.assertEqual(len(p['vertices']),4)
        self.assertEqual(p['movingPart'],'rightDoor')
        v=np.array([[p['vertices'][i][k] for k in 'xyz'] for i in p['indices'][:3]])
        normal=np.array([p['normals'][p['indices'][0]][k] for k in 'xyz'])
        self.assertGreater(np.dot(np.cross(v[1]-v[0],v[2]-v[0]),normal),0)
        self.assertIn({'u':1.,'v':1.},p['uv'])
    def test_nested_transform_is_applied_once(self):
        scene=ET.fromstring('''<visual_scene xmlns="http://www.collada.org/2005/11/COLLADASchema"><node name="parent"><matrix>1 0 0 2 0 1 0 3 0 0 1 4 0 0 0 1</matrix><node name="child"><matrix>1 0 0 1 0 1 0 0 0 0 1 0 0 0 0 1</matrix></node></node></visual_scene>''')
        transforms=list(scene_nodes(scene))
        self.assertEqual(transforms[1][1][:3,3].tolist(),[3.,3.,4.])

if __name__=='__main__':unittest.main()
