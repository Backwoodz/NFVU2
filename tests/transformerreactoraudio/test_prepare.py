from pathlib import Path
import os
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

    def additional_source(self):
        t = np.arange(round(6.9 * reactor.RATE)) / reactor.RATE
        return (.08 * np.sin(2 * np.pi * 177.73 * t) +
                .04 * np.sin(2 * np.pi * 241.23 * t)) * (1 + .25 * np.cos(2 * np.pi * .8 * t))

    def test_added_loop_has_whole_cycles_continuity_and_no_silent_seam(self):
        added, metadata = reactor.prepare_additional_reactor(self.additional_source())
        self.assertEqual(len(added), 4 * reactor.RATE)
        self.assertTrue(np.isfinite(added).all())
        self.assertLessEqual(np.max(np.abs(added)), .3)
        repeated = reactor.window_rms(np.tile(added, 3))
        self.assertAlmostEqual(max(repeated), .035, places=7)
        self.assertGreater(min(repeated), .01)
        self.assertLess(metadata['loop_boundary_sample_jump'], .003)
        self.assertEqual(metadata['crop_start_seconds'], .8)
        self.assertEqual(metadata['crop_end_seconds'], 5.6)

    def test_additive_mix_preserves_foundation_and_avoids_global_gain(self):
        foundation, _ = reactor.prepare_reactor(self.source())
        original = foundation.copy()
        added, _ = reactor.prepare_additional_reactor(self.additional_source())
        result, metadata = reactor.mix_reactor_layers(foundation, added)
        np.testing.assert_array_equal(foundation, original)
        np.testing.assert_array_equal(result, foundation + np.tile(added, 2))
        self.assertEqual(metadata['foundation_gain_db'], 0)
        self.assertEqual(metadata['mix_gain_db'], 0)
        self.assertEqual(metadata['additional_headroom_gain_linear'], 1)
        self.assertEqual(metadata['additional_repetitions'], 2)
        self.assertEqual(metadata['foundation_isolated_wav_sha256'], reactor.wav_sha256(original))
        self.assertLessEqual(np.max(np.abs(result)), .5)
        self.assertGreater(min(reactor.window_rms(np.tile(result, 3))), .015)

    def test_peak_headroom_reduces_only_new_bus_with_one_constant_gain(self):
        foundation = np.full(reactor.RATE * 8, .45)
        added = np.full(reactor.RATE * 4, .3)
        original = foundation.copy()
        result, metadata = reactor.mix_reactor_layers(foundation, added)
        self.assertLess(metadata['additional_headroom_gain_linear'], 1)
        np.testing.assert_array_equal(foundation, original)
        np.testing.assert_allclose(result, .5, rtol=0, atol=1e-15)
        self.assertEqual(metadata['foundation_gain_db'], 0)

    def test_bad_additional_sources_and_incompatible_cycles_are_rejected(self):
        for source in [np.zeros(reactor.RATE), np.zeros(reactor.RATE * 7),
                       np.full(reactor.RATE * 7, np.nan)]:
            with self.assertRaises(ValueError):
                reactor.prepare_additional_reactor(source)
        base = np.zeros(reactor.RATE * 8)
        for added in [np.zeros(reactor.RATE * 3), np.array([]), np.full(reactor.RATE * 4, np.nan)]:
            with self.assertRaises(ValueError):
                reactor.mix_reactor_layers(base, added)


@unittest.skipUnless(os.environ.get('NFVU2_AUDIO_SOURCE_DIR'),
                     'Set NFVU2_AUDIO_SOURCE_DIR for optional original-recording regression checks')
class LocalReactorRegressionTests(unittest.TestCase):
    def test_original_foundation_is_exact_and_actual_added_mix_is_bounded(self):
        source_dir = Path(os.environ['NFVU2_AUDIO_SOURCE_DIR'])
        ffmpeg = reactor.find_ffmpeg(None)
        decoded = reactor.decode_reactor(ffmpeg, source_dir / 'kernreaktor.mp3')
        foundation, _ = reactor.prepare_reactor(decoded)
        original_hash = '4a097a649d509efac721d9dfe08899df3941cc6a3e9ac44f47b50162508f60fd'
        self.assertEqual(reactor.wav_sha256(foundation), original_hash)
        extra_decoded = reactor.decode_reactor(ffmpeg, source_dir /
            'freesound_community-evil-robot-or-spaceship-or-something-idk-94720.mp3')
        added, metadata = reactor.prepare_additional_reactor(extra_decoded)
        mix, processing = reactor.mix_reactor_layers(foundation, added)
        self.assertEqual(len(mix), reactor.RATE * 8)
        self.assertTrue(np.isfinite(mix).all())
        self.assertEqual(processing['foundation_isolated_wav_sha256'], original_hash)
        self.assertEqual(processing['additional_headroom_gain_linear'], 1)
        self.assertGreater(metadata['min_window_rms'], .005)
        self.assertLess(metadata['loop_boundary_sample_jump'], .001)
        self.assertLessEqual(np.max(np.abs(mix)), .5)
        repeated = reactor.window_rms(np.tile(mix, 3))
        self.assertGreater(min(repeated), .02)
        self.assertLess(abs(mix[-1] - mix[0]), .001)


if __name__ == '__main__':
    unittest.main()
