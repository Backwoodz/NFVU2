"""A stale animation audit must never unlock a different runtime implementation."""
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
import transformer_locomotion_review as review


class TransformerReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.addCleanup(patch.stopall)
        patch.object(review, 'BASE', self.root).start()
        patch.object(review, 'OUTPUT', self.root/'artifacts').start()
        self.sources = {'src/TransformerGait.cs', 'src/TransformerVisual.cs',
                        'src/VehicleTransformer.cs', 'tests/transformer/Program.cs',
                        'tests/transformer/GaitPreview.cs'}
        for name in review.DOMAIN_BINDINGS | review.TRACE_BINDINGS | self.sources:
            self.write(name, name.encode())
        behavior = dict(passed=True, gaitVersion=3, checks=41, maximumPlantedError=.001,
                        sourceHashes={n: review.digest(self.root/n) for n in self.sources},
                        traceSha256=review.digest(self.root/'artifacts/tests/transformer/runtime-gait-preview.json'),
                        domainSha256=review.digest(self.root/'artifacts/tests/transformer/runtime-gait-domain.json'))
        self.behavior = 'artifacts/tests/transformer/runtime-gait-behavior.json'
        self.write_json(self.behavior, behavior)
        self.domain = 'artifacts/transformer-runtime-review/locomotion-v3-check.json'
        self.trace = 'artifacts/transformer-runtime-review/locomotion-v3-trace.json'
        metrics = {side+'/'+key: 0 for side in ('L', 'R')
                   for key in ('reachError', 'upperLengthError', 'lowerLengthError', 'ankleError')}
        for name, bindings, samples in ((self.domain, review.DOMAIN_BINDINGS, 4485),
                                        (self.trace, review.TRACE_BINDINGS, 2041)):
            self.write_json(name, dict(passed=True, quick=False, samples=samples,
                newContacts={}, belowGround={}, ikErrors={}, maximumIkErrors=metrics, neutralPoseMaxError=0,
                hashes={n: review.digest(self.root/n) for n in bindings}))

    def write(self, name, data):
        path = self.root/name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)

    def write_json(self, name, data):
        self.write(name, json.dumps(data).encode())

    def change_report(self, name, **changes):
        data = json.loads((self.root/name).read_text())
        data.update(changes)
        self.write_json(name, data)

    def test_current_complete_behavior_and_geometry_unlock(self):
        self.assertIsNotNone(review.reviewed())

    def test_changed_visual_or_controller_invalidates_behavior(self):
        self.write('src/TransformerVisual.cs', b'changed carrier geometry')
        self.assertIsNone(review.reviewed())

    def test_new_controller_trace_invalidates_completed_audits(self):
        self.write('artifacts/tests/transformer/runtime-gait-preview.json', b'new motion')
        self.assertIsNone(review.reviewed())

    def test_diagnostic_trace_without_behavior_cannot_unlock(self):
        (self.root/self.behavior).unlink()
        self.assertIsNone(review.reviewed())

    def test_quick_geometry_is_not_a_complete_clearance_review(self):
        self.change_report(self.domain, quick=True)
        self.assertIsNone(review.reviewed())

    def test_incomplete_coverage_stays_locked(self):
        self.change_report(self.trace, samples=100)
        self.assertIsNone(review.reviewed())

    def test_foot_penetration_cannot_be_hidden_by_passed_flag(self):
        self.change_report(self.domain, belowGround={'toe': -.02})
        self.assertIsNone(review.reviewed())

    def test_panel_collision_cannot_be_hidden_by_passed_flag(self):
        self.change_report(self.trace, newContacts={'frame/1': {'toe / calf': 4}})
        self.assertIsNone(review.reviewed())

    def test_missing_or_excessive_ik_evidence_stays_locked(self):
        self.change_report(self.domain, maximumIkErrors={'L/reachError': .1})
        self.assertIsNone(review.reviewed())

    def test_shifted_standing_pose_cannot_define_its_own_collision_baseline(self):
        self.change_report(self.domain, neutralPoseMaxError=.15)
        self.assertIsNone(review.reviewed())


if __name__ == '__main__':
    unittest.main()
