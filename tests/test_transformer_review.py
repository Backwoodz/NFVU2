"""A stale animation audit must never unlock a different runtime implementation."""
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import itertools
import math
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
import transformer_locomotion_review as review


class HandDistanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source=(Path(__file__).resolve().parents[1]/'tools/check_transformer_locomotion.py').read_text()
        helper=source.split('# BEGIN_HAND_DISTANCE\n',1)[1].split('# END_HAND_DISTANCE',1)[0]
        scope=dict(np=np,itertools=itertools,math=math)
        exec(compile(helper,'actual-hand-clearance-helper','exec'),scope)
        cls.distance=staticmethod(scope['hand_gjk_distance'])

    def box(self,center=(0,0,0),half=(1,1,1)):
        return np.array(list(itertools.product((-1.,1.),repeat=3)))*half+center

    def test_axis_and_diagonal_separation(self):
        self.assertAlmostEqual(self.distance(self.box(),self.box((2.1,0,0))),.1,places=7)
        self.assertAlmostEqual(self.distance(self.box(),self.box((2.1,2.1,0))),math.sqrt(.02),places=7)

    def test_overlap_touch_and_nested_volumes_are_not_false_clearance(self):
        for other in (self.box((1.9,0,0)),self.box((2,0,0)),self.box(half=(.1,.1,.1)),self.box()):
            self.assertAlmostEqual(self.distance(self.box(),other),0,places=7)

    def test_distance_resolves_both_sides_of_the25mm_gate(self):
        self.assertAlmostEqual(self.distance(self.box(),self.box((2.024,0,0))),.024,places=7)
        self.assertAlmostEqual(self.distance(self.box(),self.box((2.026,0,0))),.026,places=7)

    def test_common_rotation_translation_swapping_and_vertex_order_preserve_distance(self):
        angle=.73;c,s=math.cos(angle),math.sin(angle);rotation=np.array([[c,-s,0],[s,c,0],[0,0,1.]])
        a=self.box();b=self.box((2.1,2.1,0));expected=math.sqrt(.02)
        a=a@rotation.T+(100,-70,2);b=b@rotation.T+(100,-70,2)
        self.assertAlmostEqual(self.distance(a,b),expected,places=6)
        self.assertAlmostEqual(self.distance(b[::-1],a[::-1]),expected,places=6)

    def test_deterministic_axis_aligned_cases_match_exact_box_distance(self):
        rng=np.random.default_rng(403)
        for _ in range(32):
            ca=rng.uniform(-3,3,3);cb=rng.uniform(-3,3,3);ha=rng.uniform(.001,1,3);hb=rng.uniform(.001,1,3)
            expected=np.linalg.norm(np.maximum(np.abs(ca-cb)-ha-hb,0))
            self.assertAlmostEqual(self.distance(self.box(ca,ha),self.box(cb,hb)),expected,places=6)


class TransformerReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.addCleanup(patch.stopall)
        patch.object(review, 'BASE', self.root).start()
        patch.object(review, 'OUTPUT', self.root/'artifacts').start()
        self.sources = review.BEHAVIOR_SOURCES
        for name in review.DOMAIN_BINDINGS | review.TRACE_BINDINGS | self.sources:
            self.write(name, name.encode())
        behavior = dict(passed=True, gaitVersion=4, checks=54, maximumPlantedError=.001,
                        sprintTested=True, sprintFlightSamples=30, sprintSupportSamples=100, sprintAirClearanceSamples=15, maximumAirTime=.1,
                        sourceHashes={n: review.digest(self.root/n) for n in self.sources},
                        traceSha256=review.digest(self.root/review.TRACE_FILE),
                        domainSha256=review.digest(self.root/review.DOMAIN_FILE))
        self.behavior = review.BEHAVIOR_FILE
        self.write_json(self.behavior, behavior)
        self.domain = 'artifacts/transformer-runtime-review/locomotion-v4-check.json'
        self.trace = 'artifacts/transformer-runtime-review/locomotion-v4-trace.json'
        metrics = {side+'/'+key: 0 for side in ('L', 'R')
                   for key in ('reachError', 'upperLengthError', 'lowerLengthError', 'ankleError')}
        hands = {side: ['hand.'+side]+['digit%d%d.%s'%(d,j,side) for d in range(4) for j in range(3)]+['thumb%d.%s'%(j,side) for j in range(3)] for side in ('L','R')}
        for name, bindings, samples in ((self.domain, review.DOMAIN_BINDINGS, 5930),
                                        (self.trace, review.TRACE_BINDINGS, 3661)):
            progress=sorted({.65+i*.025 for i in range(15)}|{.82})
            baseline_progress=[p for p in progress if p<1]
            baseline_samples=60 if name==self.domain else 0
            self.write_json(name, dict(passed=True, quick=False, samples=samples,
                newContacts={}, belowGround={}, ikErrors={}, maximumIkErrors=metrics, neutralPoseMaxError=0,
                handContacts={}, handCheckSides=['L','R'], handCheckSamples=samples, handCheckMeshes=254,
                handDistanceTests=100, handTriangleTests=100, handVolumeTests=100,
                handMinimumClearance=.03, handClearanceThreshold=.025, handFingerBoneCoverage=hands,
                handBaselineSamples=baseline_samples, handAbsoluteSamples=samples-baseline_samples, handBaselineExcessContacts={},
                terrainCases=sorted(review.TERRAIN_CASES), terrainSamples=356, terrainDeploymentSamples=64, terrainWalkSamples=256,
                terrainContacts={}, terrainBelowGround={}, terrainCaseSamples={case:57 if case[0] in 'xz' else 32 for case in review.TERRAIN_CASES},
                terrainPreparationSource='unchanged V18 support-parallel; existing129 flat release samples',
                terrainStandingRelease=1, terrainStandingSamples=4,
                terrainAlignmentSamples=36, terrainAlignmentFractions=[i/8 for i in range(9)],
                terrainAlignmentCaseSamples={case:9 for case in ('x+8','x-8','z+8','z-8')},
                terrainDeploymentProgress=progress,
                terrainHandBaselineSource='same-frame V18 flat-ground transformation', terrainHandBaselineSamples=60, terrainHandAbsoluteSamples=296,
                terrainHandBaselineProgress=baseline_progress, terrainHandBaselineExcessContacts={},
                terrainHandBaselineGapFloor=.0015, terrainHandBaselinePackedMinPositiveGap=.002,
                terrainHandBaselineMaximumGapReduction=.018, terrainHandBaselineMaximumGapShortfall=0,
                terrainHandBaselineOriginal={str(p):{'hand / shin':dict(clearance=0,depth=.03,triangles=12)} for p in baseline_progress},
                maximumTerrainIkErrors={key:0 for key in ('reachError','upperLengthError','lowerLengthError','ankleError')},terrainHandMinimumClearance=.03,
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

    def test_alignment_must_cover_every_slope_and_the_full_interval(self):
        for changes in (dict(terrainAlignmentSamples=35),dict(terrainAlignmentFractions=[0,1]),
                        dict(terrainAlignmentCaseSamples={'x+8':9,'x-8':9,'z+8':9})):
            with self.subTest(changes=changes):
                data=json.loads((self.root/self.domain).read_text())
                self.change_report(self.domain,**changes)
                self.assertIsNone(review.reviewed())
                self.write_json(self.domain,data)

    def test_preparation_requires_original_parallel_transform_and_open_standing(self):
        for changes in (dict(terrainPreparationSource='tail IK'),dict(terrainStandingRelease=0),dict(terrainStandingSamples=3)):
            with self.subTest(changes=changes):
                data=json.loads((self.root/self.domain).read_text())
                self.change_report(self.domain,**changes)
                self.assertIsNone(review.reviewed())
                self.write_json(self.domain,data)

    def test_both_moving_and_raised_step_frames_require_full_coverage(self):
        for case in ('stepL+0.1','stepR+0.1','stepL+0.1-raised','stepR+0.1-raised'):
            with self.subTest(case=case):
                data=json.loads((self.root/self.domain).read_text())
                counts=dict(data['terrainCaseSamples']);counts[case]=31
                self.change_report(self.domain,terrainCaseSamples=counts)
                self.assertIsNone(review.reviewed())
                self.write_json(self.domain,data)

    def test_changed_visual_or_controller_invalidates_behavior(self):
        self.write('src/TransformerVisual.cs', b'changed carrier geometry')
        self.assertIsNone(review.reviewed())

    def test_new_controller_trace_invalidates_completed_audits(self):
        self.write(review.TRACE_FILE, b'new motion')
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

    def test_v3_walk_only_evidence_cannot_unlock_sprint(self):
        self.change_report(self.behavior, gaitVersion=3, sprintTested=False)
        self.assertIsNone(review.reviewed())

    def test_no_flight_or_unbounded_airtime_cannot_claim_sprint(self):
        self.change_report(self.behavior, sprintFlightSamples=0, maximumAirTime=4)
        self.assertIsNone(review.reviewed())

    def test_airborne_flags_without_visible_foot_clearance_cannot_claim_flight(self):
        self.change_report(self.behavior, sprintAirClearanceSamples=0)
        self.assertIsNone(review.reviewed())

    def test_unchecked_fingers_cannot_hide_behind_body_clearance(self):
        self.change_report(self.domain, handFingerBoneCoverage={'L':['hand.L'],'R':['hand.R']})
        self.assertIsNone(review.reviewed())

    def test_missing_hand_distance_or_clearance_proof_stays_locked(self):
        self.change_report(self.trace, handDistanceTests=0, handMinimumClearance=.001)
        self.assertIsNone(review.reviewed())

    def test_real_hand_collision_cannot_hide_behind_passed_flag(self):
        self.change_report(self.domain, handContacts={'frame/17': {'digit00.L / knee': 3}})
        self.assertIsNone(review.reviewed())

    def test_flat_ground_only_proof_cannot_unlock_sloped_runtime(self):
        self.change_report(self.domain, terrainCases=[], terrainSamples=0)
        self.assertIsNone(review.reviewed())

    def test_incomplete_terrain_case_or_deployment_boundary_stays_locked(self):
        self.change_report(self.domain, terrainDeploymentProgress=[.65+i*.025 for i in range(15)])
        self.assertIsNone(review.reviewed())

    def test_slope_burial_or_hand_collision_cannot_hide_behind_passed_flag(self):
        self.change_report(self.domain, terrainBelowGround={'x+8/step': {'toe':-.02}}, terrainHandMinimumClearance=.001)
        self.assertIsNone(review.reviewed())

    def test_original_packed_hands_are_only_a_transformation_comparison_baseline(self):
        report=json.loads((self.root/self.domain).read_text())
        self.assertTrue(report['terrainHandBaselineOriginal'])
        self.assertIsNotNone(review.reviewed())

    def test_complete_robot_frame_cannot_be_added_to_packed_hand_baseline(self):
        report=json.loads((self.root/self.domain).read_text())
        self.change_report(self.domain,terrainHandBaselineProgress=report['terrainHandBaselineProgress']+[1])
        self.assertIsNone(review.reviewed())

    def test_deepened_original_hand_contact_cannot_hide_in_a_baseline(self):
        self.change_report(self.domain,terrainHandBaselineExcessContacts={'x+8/.8/hand-shin':dict(depthIncrease=.01)})
        self.assertIsNone(review.reviewed())

    def test_same_frame_hand_baseline_never_allows_ground_penetration(self):
        self.change_report(self.domain,terrainBelowGround={'x+8/.65/toe':-.038})
        self.assertIsNone(review.reviewed())

    def test_packed_original_positive_gaps_may_reduce_without_an_unchanged_gap_cap(self):
        self.change_report(self.domain,terrainHandBaselineMaximumGapReduction=.02,terrainHandBaselinePackedMinPositiveGap=.0015,terrainHandBaselineMaximumGapShortfall=0)
        self.assertIsNotNone(review.reviewed())

    def test_original_overlap_improving_to_a_tiny_positive_gap_is_valid(self):
        self.change_report(self.domain,terrainHandBaselinePackedMinPositiveGap=.0002,terrainHandBaselineMaximumGapShortfall=0)
        self.assertIsNotNone(review.reviewed())

    def test_per_pair_packed_gap_shortfall_stays_locked(self):
        self.change_report(self.domain,terrainHandBaselineMaximumGapShortfall=.0005)
        self.assertIsNone(review.reviewed())


if __name__ == '__main__':
    unittest.main()
