"""Geometry regressions for the gaps reported with the optional detailed cabin."""
import json
import sys
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL
from cabin_fit import BASE_SHELL, REAR_SHELL, ROOF_CLEARANCE, split_rear_shell, roof_triangles, roof_heights, fit_headliner, fit_windscreen, windscreen_triangles, WINDSCREEN_CLEARANCE


class CabinFitTests(unittest.TestCase):
    def test_partition_preserves_original_faces_and_material(self):
        vertices = [dict(x=0, y=1, z=z) for z in (-1, -2, -1.5, 0, .5, 1)]
        original = dict(name=BASE_SHELL, vertices=vertices, indices=[0, 1, 2, 3, 4, 5], material=7)
        parts = [original]
        split_rear_shell(parts)
        self.assertEqual(original['indices'], [3, 4, 5])
        self.assertEqual(parts[1]['indices'], [0, 1, 2])
        self.assertEqual(parts[1]['material'], 7)
        self.assertIs(parts[1]['vertices'], vertices)

    def test_roof_fit_uses_sloped_surface_and_keeps_lower_cabin(self):
        roof = np.array([[[0, 1.2, 0], [1, 1.1, 0], [0, 1.2, 1]]])
        points = np.array([[.2, 1.22, .2], [.2, .8, .2], [3, 1.1, 3]])
        fitted = fit_headliner(points, roof)
        self.assertAlmostEqual(fitted[0, 1], 1.18-ROOF_CLEARANCE)
        np.testing.assert_array_equal(fitted[1:], points[1:])
        np.testing.assert_array_equal(fitted[:, [0, 2]], points[:, [0, 2]])
        self.assertEqual(points[0, 1], 1.22)

    def test_windscreen_fit_covers_border_triangles_without_moving_driver_controls(self):
        glass = np.array([[[-1, .8, .8], [1, .8, .8], [0, 1.2, .2]]])
        points = np.array([[0, 1, .6], [0, 1.21, .3], [0, .5, .3], [0, 1., -.3]])
        fitted = fit_windscreen(points, glass)
        self.assertAlmostEqual(fitted[0, 2], .5-WINDSCREEN_CLEARANCE)
        self.assertLess(fitted[1, 2], .2)  # Immediately above the glass rim.
        np.testing.assert_array_equal(fitted[2:], points[2:])
        np.testing.assert_array_equal(fitted[:, :2], points[:, :2])

    def test_actual_cabin_faces_stay_behind_windscreen(self):
        glass = windscreen_triangles(self.body)[:, :, [0, 2, 1]]
        checked = 0
        for part in self.cabin['parts']:
            vertices = np.array([[q[k] for k in 'xyz'] for q in part['vertices']])
            faces = vertices[np.array(part['indices']).reshape(-1, 3)]
            # Check the interiors of faces as well as endpoints along the seal.
            samples = np.concatenate([vertices, faces.mean(1), (faces[:, 0]+faces[:, 1])/2,
                                      (faces[:, 1]+faces[:, 2])/2, (faces[:, 2]+faces[:, 0])/2])
            front = roof_heights(samples[:, [0, 2, 1]], glass)
            covered = np.isfinite(front)
            checked += np.count_nonzero(covered)
            self.assertTrue(np.all(samples[covered, 2] <= front[covered]-.003), part['name'])
        self.assertGreater(checked, 1000)

    @classmethod
    def setUpClass(cls):
        cls.body = json.loads((LOCAL/'model/body.json').read_text())
        cls.cabin = json.loads((LOCAL/'interior/interior.json').read_text())

    def test_actual_rear_rim_blocks_views_into_the_body(self):
        part = next(p for p in self.body['parts'] if p['name'] == REAR_SHELL)
        vertices = np.array([[q[k] for k in 'xyz'] for q in part['vertices']])
        triangles = vertices[np.array(part['indices']).reshape(-1, 3)]
        # Rear sill and the two side returns that disappeared with the old cockpit.
        points = np.array([[0, 2, -1.99], [.62, 2, -1.7], [-.62, 2, -1.7], [.69, 2, -1.2], [-.69, 2, -1.2]])
        self.assertTrue(np.all(np.isfinite(roof_heights(points, triangles))))
        self.assertFalse(part.get('movingPart'))
        self.assertFalse(part.get('optionGroup'))  # Kept with all four audio enclosures.
        old = next(p for p in self.body['parts'] if p['name'] == BASE_SHELL)
        rear = {tuple(t) for t in np.array(part['indices']).reshape(-1, 3)}
        cabin = {tuple(t) for t in np.array(old['indices']).reshape(-1, 3)}
        self.assertFalse(rear & cabin)
        self.assertEqual(len(rear | cabin), 314)  # Complete original BASE material slice.

    def test_actual_door_returns_keep_their_hinges(self):
        panels = [p for p in self.body['parts'] if 'DOOR_PANEL_' in p['name']]
        self.assertEqual({p['movingPart'] for p in panels}, {'leftDoor', 'rightDoor'})
        self.assertTrue(all(len(p['indices']) == 150 for p in panels))
        for part in panels:
            # Edges extend behind the detailed trim to meet the widebody outer skin.
            xs = [abs(part['vertices'][j]['x']) for j in part['indices']]
            self.assertGreater(max(xs)-min(xs), .25)

    def test_actual_pillars_continue_above_previous_cut_and_lining_stays_below_roof(self):
        roof = roof_triangles(self.body)
        shell = [p for p in self.cabin['parts'] if p['name'].startswith('Cabin_Nis350Z_interior_')]
        points = np.array([[q[k] for k in 'xyz'] for p in shell for q in p['vertices']])
        for side in (-1, 1):
            pillar = points[(side*points[:, 0] > .5) & (points[:, 2] > .12)]
            self.assertGreater(pillar[:, 1].max(), 1.09)
        height = roof_heights(points, roof)
        covered = (points[:, 1] > .95) & np.isfinite(height)
        self.assertGreater(np.count_nonzero(covered), 500)
        self.assertTrue(np.all(points[covered, 1] <= height[covered]-ROOF_CLEARANCE+2e-6))


if __name__ == '__main__':
    unittest.main()
