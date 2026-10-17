from pathlib import Path
import sys
import unittest

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
import prepare_transformer_reactor_audio as reactor


class ReactorPreparationTests(unittest.TestCase):
    def source(self):
        t = np.arange(reactor.RATE * 18) / reactor.RATE
        return .1 * np.sin(2 * np.pi * 83.37 * t) + .06 * np.sin(2 * np.pi * 127.123 * t)

    def test_sustained_loop_has_bounded_gain_and_no_silent_seam(self):
        result, metadata = reactor.prepare_reactor(self.source())
        self.assertEqual(len(result), reactor.RATE * 8)
        self.assertTrue(np.isfinite(result).all())
        self.assertLessEqual(np.max(np.abs(result)), .5)
        rms = reactor.window_rms(np.tile(result, 3))
        self.assertAlmostEqual(max(rms), .055, places=7)
        self.assertGreater(min(rms), .02)
        self.assertLess(metadata['loop_boundary_sample_jump'], .003)

    def test_crop_is_deterministic_and_rejects_missing_or_bad_input(self):
        first, metadata = reactor.prepare_reactor(self.source())
        second, _ = reactor.prepare_reactor(self.source())
        np.testing.assert_array_equal(first, second)
        self.assertEqual(metadata['crop_start_sample'], 8 * reactor.RATE)
        self.assertEqual(metadata['crossfade_samples'], round(.8 * reactor.RATE))
        with self.assertRaises(ValueError):
            reactor.prepare_reactor(np.zeros(reactor.RATE))
        with self.assertRaises(ValueError):
            reactor.prepare_reactor(np.zeros(reactor.RATE * 18))
        broken = self.source()
        broken[10] = float('nan')
        with self.assertRaises(ValueError):
            reactor.prepare_reactor(broken)


if __name__ == '__main__':
    unittest.main()
