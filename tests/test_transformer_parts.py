"""Surface/UV conservation for robot cuts, independent of Blender rendering."""
import json
import copy
import math
from collections import Counter
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'tools'))
from transformer_parts import split, area, partition, selected, prepare_study_part, vertex
import transformer_reassembly as reassembly


class TransformerPartitionTests(unittest.TestCase):
    def test_every_pane_keeps_exact_original_triangles_uvs_and_normals(self):
        path=ROOT/'.local/model/body.json'
        if not path.exists():
            self.skipTest('Local 350Z assets not available')
        model=json.loads(path.read_text())
        pane_groups=set()
        for part in model['parts']:
            if not selected(part):
                continue
            material=model['materials'][part['material']]['name']
            if not material.startswith(('471a1dca', '54949afd')):
                continue
            self.assertTrue(reassembly.selected(part),part['name'])
            prepared,_=prepare_study_part(part)
            self.assertEqual(prepared,part,part['name'])
            groups,_=reassembly.partition(prepared)
            expected=Counter(tuple(vertex(part,i) for i in part['indices'][offset:offset+3][::-1])
                             for offset in range(0,len(part['indices']),3))
            actual=Counter(tuple(tri) for tris in groups.values() for tri in tris)
            self.assertEqual(actual,expected,part['name'])
            self.assertNotIn('windscreen_L',groups)
            self.assertNotIn('hatch_L',groups)
            if 'BASE_A_slice10_' not in part['name']:
                self.assertEqual(len(groups),1,part['name'])
            pane_groups.update(groups)
        self.assertEqual(pane_groups,{'windscreen','hatch','quarterglass_L','quarterglass_R',
                                     'window_L','window_R','mirror_L','mirror_R','lamp_L','lamp_R'})

    def test_reassembly_preserves_exterior_area_and_splits_armor(self):
        path = ROOT/'.local/model/body.json'
        if not path.exists():
            self.skipTest('Local 350Z assets not available')
        model = json.loads(path.read_text())
        modules = set()
        for part in model['parts']:
            if not selected(part):
                continue
            if any(tag in part['name'] for tag in ('KITW02_BODY', 'HEADLIGHT', 'BRAKELIGHT', 'SPOILER', 'DOOR_LEFT', 'DOOR_RIGHT')):
                self.assertTrue(reassembly.selected(part), part['name'])
            if not reassembly.selected(part):
                continue
            prepared, _ = prepare_study_part(part)
            groups, audit = reassembly.partition(prepared)
            self.assertAlmostEqual(sum(a for a, _ in audit),
                sum(area(tri) for tris in groups.values() for tri in tris), places=8, msg=part['name'])
            modules.update(groups)
            if 'HEADLIGHT' in part['name']:
                expected = 'lamp_L' if '_LEFT_' in part['name'] else 'lamp_R'
                self.assertEqual(set(groups), {expected})
                self.assertEqual(len(groups[expected]), len(part['indices'])//3)
        for side in ('L', 'R'):
            for role in ('chest_', 'door_front_', 'door_rear_', 'roof_', 'rear_arch_', 'shinface_'):
                self.assertIn(role+side, modules)
        self.assertIn('windscreen',modules)
        self.assertIn('hatch',modules)

    def test_inner_clearance_preserves_source_and_exterior(self):
        path = ROOT/'.local/model/body.json'
        if not path.exists():
            self.skipTest('Local 350Z assets not available')
        model = json.loads(path.read_text())
        allowed = ('350Z_BASE_A_slice3_', '350Z_BASE_A_slice5_',
                   '350Z_KITW02_DOOR_PANEL_', '350Z_KIT00_TRUNK_UNDER_',
                   '350Z_KIT00_TRUNK_AUDIO_', 'NFVU2_350Z_RearInnerShell')
        changed = 0
        for part in model['parts']:
            if not selected(part):
                continue
            before = copy.deepcopy(part)
            prepared, note = prepare_study_part(part)
            self.assertEqual(part, before, part['name'])
            if not part['name'].startswith(allowed):
                self.assertIs(prepared, part)
                self.assertIsNone(note)
                continue
            changed += 1
            self.assertEqual(note['source_part'], part['name'])
            self.assertNotEqual(prepared['vertices'], part['vertices'])
            self.assertEqual(len(prepared['vertices']), len(prepared['normals']))
            self.assertEqual(len(prepared['vertices']), len(prepared['uv']))
            self.assertTrue(all(0 <= i < len(prepared['vertices']) for i in prepared['indices']))
            self.assertTrue(all(math.isfinite(x) for n in prepared['normals'] for x in n.values()))
            if 'DOOR_PANEL_' in part['name']:
                self.assertLessEqual(max(v['y'] for v in prepared['vertices']), .7400001)
            else:
                self.assertEqual(prepared['uv'], part['uv'])
                self.assertEqual(prepared['indices'], part['indices'])
        self.assertEqual(changed, 8)

    def test_cut_interpolates_uv_and_does_not_change_surface_area(self):
        # Position x/y maps exactly to u/v; cuts must retain that relationship.
        tri = [(0, 0, 0, 0, 0, 1, 0, 0),
               (2, 0, 0, 0, 0, 1, 2, 0),
               (0, 2, 0, 0, 0, 1, 0, 2)]
        polygons = split(tri, 0, .75)
        self.assertEqual(len(polygons), 2)
        total = 0
        for poly in polygons:
            for vertex in poly:
                self.assertEqual(vertex[:2], vertex[6:8])
            total += sum(area([poly[0], poly[i], poly[i+1]]) for i in range(1, len(poly)-1))
        self.assertAlmostEqual(total, area(tri), places=12)

    def test_coplanar_faces_are_not_duplicated(self):
        tri = [(0, 0, 0), (0, 1, 0), (0, 0, 1)]
        self.assertEqual(split(tri, 0, 0), [tri])

    def test_every_selected_source_triangle_preserves_area(self):
        path = ROOT/'.local/model/body.json'
        if not path.exists():
            self.skipTest('Local 350Z assets not available')
        model = json.loads(path.read_text())
        modules = set()
        for part in model['parts']:
            if not selected(part):
                continue
            groups, audit = partition(part)
            self.assertEqual(len(audit), len(part['indices'])//3)
            for before, after in audit:
                self.assertAlmostEqual(before, after, places=10, msg=part['name'])
            modules.update(groups)
        for name in ['chest', 'roof', 'hatch', 'spoiler', 'arm_L', 'arm_R',
                     'calf_L', 'calf_R', 'foot_L', 'foot_R']:
            self.assertIn(name, modules)

    def test_headlights_keep_their_original_triangles(self):
        path = ROOT/'.local/model/body.json'
        if not path.exists():
            self.skipTest('Local 350Z assets not available')
        model = json.loads(path.read_text())
        for part in model['parts']:
            if 'HEADLIGHT' not in part['name']:
                continue
            groups, audit = partition(part)
            expected = 'shoulder_L' if '_LEFT_' in part['name'] else 'shoulder_R'
            self.assertEqual(set(groups), {expected})
            self.assertEqual(len(groups[expected]), len(part['indices'])//3)


if __name__ == '__main__':
    unittest.main()
