import sys
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from prepare_effects_audio import prepare_samples


class EffectsAudioTests(unittest.TestCase):
    def test_preserves_non_silent_loop_without_clipping(self):
        source = np.sin(np.arange(12000) * .071) * .9 + .1
        prepared = prepare_samples(source, 12000, {'start': 100, 'end': 11900})
        self.assertLess(len(prepared), 11800)
        self.assertGreater(np.sqrt(np.mean(prepared**2)), .1)
        self.assertLessEqual(np.max(np.abs(prepared)), .800001)

    def test_one_shot_edges_are_quiet(self):
        source = np.sin(np.arange(3200) * .071) * .9 + .1
        prepared = prepare_samples(source, 16000, None)
        self.assertEqual(len(prepared), len(source))
        self.assertEqual(prepared[0], 0)
        self.assertEqual(prepared[-1], 0)

    def test_rejects_bad_samples_and_loop_bounds(self):
        for samples, loop in [(np.zeros(500), None), (np.full(500, np.nan), None),
                              (np.zeros((500, 2)), None), (np.ones(500), {'start': 400, 'end': 900}),
                              (np.ones(500), {'start': 300, 'end': 200})]:
            with self.subTest(loop=loop), self.assertRaises(ValueError):
                prepare_samples(samples, 22050, loop)

    def test_rejects_long_audio_and_invalid_rate(self):
        with self.assertRaises(ValueError):
            prepare_samples(np.ones(8000*16), 8000, None)
        with self.assertRaises(ValueError):
            prepare_samples(np.ones(1000), 1000, None)


if __name__ == '__main__':
    unittest.main()
