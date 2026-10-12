"""Check the authored inner finish against the actual hood and engine variants."""
import copy
import json
import sys
import unittest
from pathlib import Path
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL
from cabin_fit import roof_heights
from hood_lining import dress_hood


def triangles(part):
    return np.array([[v[k] for k in 'xyz'] for v in part['vertices']])[np.array(part['indices']).reshape(-1, 3)]


class HoodLiningTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.model = json.loads((LOCAL/'model/body.json').read_text())
        cls.details = [p for p in cls.model['parts'] if p['name'].startswith('NFVU2_HoodInner')]
        cls.outer = next(p for p in cls.model['parts'] if p['name']=='350Z_STYLE07_HOOD_A_slice1_40')

    def test_dressing_preserves_carbon_skin_vents_and_original_hinge(self):
        model = copy.deepcopy(self.model)
        model['parts'] = [p for p in model['parts'] if not p['name'].startswith('NFVU2_HoodInner')]
        before = copy.deepcopy([p for p in model['parts'] if p['name'].startswith('350Z_STYLE07_HOOD_')])
        track = copy.deepcopy(next(a for a in model['animations'] if a['name']=='HOOD_REG'))
        dress_hood(model['parts'], model['materials'])
        self.assertEqual(before, [p for p in model['parts'] if p['name'].startswith('350Z_STYLE07_HOOD_')])
        self.assertEqual(track, next(a for a in model['animations'] if a['name']=='HOOD_REG'))
        hood = [p for p in model['parts'] if 'HOOD_UNDER_' in p['name'] or p['name'].startswith('NFVU2_HoodInner')]
        self.assertEqual(len(hood), 4)
        self.assertTrue(all(p['movingPart']=='hood' and not p.get('optionGroup') for p in hood))

    def test_details_stay_inside_skin_and_clear_all_three_engines(self):
        self.assertEqual(len(self.details), 3)
        roof = triangles(self.outer)
        samples = []
        for part in self.details:
            t = triangles(part)
            samples.extend([t.reshape(-1, 3), t.mean(1), (t[:, 0]+t[:, 1])/2,
                            (t[:, 1]+t[:, 2])/2, (t[:, 2]+t[:, 0])/2])
        samples = np.concatenate(samples)
        height = roof_heights(samples, roof)
        self.assertTrue(np.all(np.isfinite(height)))
        # Keep at least 2 mm below the exterior, including interpolation over
        # its sharp creases; the nominal profile base is 10 mm below the skin.
        self.assertTrue(np.all(samples[:, 1] < height-.002))
        for variant in ('0', '1', '2'):
            engine = np.concatenate([triangles(p) for p in self.model['parts']
                                     if p.get('optionGroup')=='engine' and p['option']==variant])
            height = roof_heights(samples, engine)
            covered = np.isfinite(height)
            self.assertTrue(np.all(samples[covered, 1] > height[covered]+.002), 'Engine '+variant)

    def test_normals_and_materials_support_non_emissive_relief(self):
        for part in self.details:
            t = triangles(part)
            geometric = np.cross(t[:, 1]-t[:, 0], t[:, 2]-t[:, 0])
            length = np.linalg.norm(geometric, axis=1)
            self.assertTrue(np.all(length > 1e-10))
            normals = np.array([[v[k] for k in 'xyz'] for v in part['normals']])[np.array(part['indices'])[::3]]
            self.assertTrue(np.all(np.sum(geometric/length[:, None]*normals, axis=1) > .99))
            self.assertFalse(part.get('lightRole'))
        pad = next(m for m in self.model['materials'] if m['name']=='hood_inner_insulation')
        self.assertEqual(pad['metallic'], 0)
        self.assertLess(pad['smoothness'], .1)


if __name__=='__main__': unittest.main()
