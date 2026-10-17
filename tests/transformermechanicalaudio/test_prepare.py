"""Signal and preservation checks; optional original recordings stay local."""
import os
import hashlib
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
from types import SimpleNamespace

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
import prepare_transformer_deploy_audio as deploy
import prepare_transformer_return_audio as returning
import prepare_transformer_mechanical_audio as mechanical
from prepare_transformer_audio import decode, find_ffmpeg


class MechanicalPreparationTests(unittest.TestCase):
    def test_subdrop_is_a_short_late_deploy_layer_only(self):
        cue = deploy.SUBDROP_CUE
        self.assertGreaterEqual(cue['onset'], 3.7)
        self.assertLessEqual(cue['onset'], 4.0)
        self.assertAlmostEqual(cue['onset'] + cue['crop'][1] - cue['crop'][0], 5.3)
        self.assertEqual(cue['fade_in'], .01)
        self.assertEqual(cue['fade_out'], .08)
        self.assertFalse(any(item['source'] == deploy.SUBDROP for item in
                             returning.RETURN_LOCKS + mechanical.RETURN_TEXTURES))

    def test_all_inside_cues_are_isolated_short_attacks_with_timeline_gaps(self):
        for cues in [deploy.CUES + mechanical.DEPLOY_TEXTURES, mechanical.RETURN_TEXTURES]:
            inside = sorted([cue for cue in cues if cue['source'] == mechanical.INSIDE],
                            key=lambda cue: cue['onset'])
            for cue in inside:
                self.assertGreaterEqual(cue['crop'][1] - cue['crop'][0], .12)
                self.assertLessEqual(cue['crop'][1] - cue['crop'][0], .28)
                self.assertLessEqual(cue['fade_in'], .01)
                self.assertLessEqual(cue['fade_out'], .08)
            for first, second in zip(inside, inside[1:]):
                self.assertGreaterEqual(second['onset'] - first['onset'] -
                                        (first['crop'][1] - first['crop'][0]), .10)

    def test_bass_decode_is_explicitly_bounded_and_reports_original_duration(self):
        audio = np.ones(round(3 * deploy.RATE), dtype='<f4') * .1
        with patch.object(mechanical.subprocess, 'run', return_value=SimpleNamespace(
                stderr='Duration: 00:02:21.22, start: 0')), patch.object(
                mechanical.subprocess, 'check_output', return_value=audio.tobytes()) as decoder:
            samples, metadata = mechanical.decode_bass(Path('ffmpeg'), Path('source.mp3'))
        command = decoder.call_args.args[0]
        self.assertEqual(command[command.index('-ss') + 1], '75.0')
        self.assertEqual(command[command.index('-t') + 1], '3.0')
        self.assertEqual(len(samples), 3 * deploy.RATE)
        self.assertEqual(metadata['duration_seconds'], 141.22)
        self.assertEqual(metadata['decoded_duration_seconds'], 3)

    def test_added_bus_preserves_foundation_without_clipping_or_global_gain(self):
        foundation = np.linspace(-.949, .949, 20001)
        original = foundation.copy()
        extra = .8 * np.sin(np.arange(len(foundation)) * .17)
        limited, report = deploy.constrain_lock_bus(extra, foundation)
        mixed = foundation + limited
        self.assertTrue(np.isfinite(mixed).all())
        self.assertLessEqual(float(np.max(np.abs(mixed))), .950000001)
        np.testing.assert_array_equal(foundation, original)
        np.testing.assert_allclose(mixed - limited, foundation, rtol=0, atol=2e-16)
        self.assertEqual(report['growl_gain_db'], 0)

    def test_both_directions_use_both_sources_and_different_crops(self):
        for cues in [mechanical.DEPLOY_TEXTURES, mechanical.RETURN_TEXTURES]:
            self.assertEqual({cue['source'] for cue in cues}, {mechanical.MOVING, mechanical.INSIDE})
            self.assertTrue(all(0 <= cue['onset'] < cue['onset'] + cue['crop'][1] - cue['crop'][0] <= 5
                                for cue in cues))
        for source in [mechanical.MOVING, mechanical.INSIDE]:
            forward = [cue['crop'] for cue in mechanical.DEPLOY_TEXTURES if cue['source'] == source]
            reverse = [cue['crop'] for cue in mechanical.RETURN_TEXTURES if cue['source'] == source]
            self.assertTrue(all(end1 <= begin2 or end2 <= begin1
                                for begin1, end1 in forward for begin2, end2 in reverse))

    def test_final_reverse_accents_follow_car_pose_not_forward_mirror(self):
        cues = {cue['name']: cue for cue in returning.RETURN_LOCKS}
        self.assertAlmostEqual(cues['floor_panel_car_seating']['contact_seconds'], 172 / 48)
        self.assertEqual(cues['floor_panel_car_seating']['component'], 'complete_world_car_pose')
        self.assertEqual(cues['limb_assembly_seating']['contact_seconds'], 4.5)
        self.assertAlmostEqual(cues['quarterglass_car_seating']['contact_seconds'], 224 / 48)
        self.assertEqual(cues['final_car_body_lock']['contact_seconds'], 5)
        self.assertTrue(all(cue['contact_seconds'] >= 2.125 for cue in returning.RETURN_LOCKS))

    def test_layer_rejects_silent_and_nonfinite_selection(self):
        cue = mechanical.DEPLOY_TEXTURES[0]
        for samples in [np.zeros(deploy.RATE * 8), np.full(deploy.RATE * 8, np.nan)]:
            with self.assertRaises(ValueError):
                deploy.prepare_layer(samples, cue)


@unittest.skipUnless(os.environ.get('NFVU2_AUDIO_SOURCE_DIR'),
                     'Set NFVU2_AUDIO_SOURCE_DIR for optional local-recording regression checks')
class LocalRecordingRegressionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = Path(os.environ['NFVU2_AUDIO_SOURCE_DIR'])
        sources = {cue['source'] for cue in deploy.CUES + mechanical.DEPLOY_TEXTURES +
                   returning.RETURN_LOCKS + mechanical.RETURN_TEXTURES}
        cls.decoded = {name: decode(find_ffmpeg(None), source / name) for name in sources}
        cls.decoded[mechanical.BASS], cls.bass_metadata = mechanical.decode_bass(
            find_ffmpeg(None), source / mechanical.BASS)
        cls.decoded[deploy.SUBDROP] = decode(find_ffmpeg(None), source / deploy.SUBDROP)

    def test_entire_previous_25_layer_foundation_and_no_early_subdrop_are_exact(self):
        foundation, old_layers, _ = deploy.compose_before_subdrop(self.decoded)
        self.assertEqual(mechanical.wav_sha256(foundation), deploy.PRIOR_25_LAYER_SHA256)
        self.assertEqual(len(old_layers), 25)
        self.assertEqual(hashlib.sha256(json.dumps(old_layers, sort_keys=True).encode()).hexdigest(),
                         '4cccfd713aff7fce8b6fc1884b84a85e90aba8e2b3ebfb2d02000b5664c918d2')
        mix, layers, processing = deploy.compose(self.decoded)
        self.assertEqual(layers[:-1], old_layers)
        self.assertEqual(layers[-1]['name'], 'late_subdrop')
        onset = round(3.75 * deploy.RATE)
        np.testing.assert_array_equal(mix[:onset], foundation[:onset])
        self.assertGreater(np.sqrt(np.mean((mix[onset:] - foundation[onset:]) ** 2)), .03)
        self.assertEqual(processing['prior_25_layer_foundation_sha256'], deploy.PRIOR_25_LAYER_SHA256)

    def test_subdrop_retains_descending_source_and_quiet_tail(self):
        prepared, metadata = deploy.prepare_layer(self.decoded[deploy.SUBDROP], deploy.SUBDROP_CUE)
        self.assertEqual(len(prepared), round(1.55 * deploy.RATE))
        self.assertEqual(metadata['onset_seconds'], 3.75)
        self.assertEqual(metadata['end_seconds'], 5.3)
        self.assertAlmostEqual(metadata['max_window_rms_before_mix_attenuation'], .18)
        first = prepared[:round(.1 * deploy.RATE)]
        last = prepared[-round(.1 * deploy.RATE):]
        self.assertLess(np.sqrt(np.mean(last ** 2)), .02 * np.sqrt(np.mean(first ** 2)))
        frequencies = []
        for start in [1.4, 2.65]:
            selection = self.decoded[deploy.SUBDROP][round(start * deploy.RATE):round((start + .2) * deploy.RATE)]
            fft = abs(np.fft.rfft(selection * np.hanning(len(selection)), n=deploy.RATE * 2))
            freq = np.fft.rfftfreq(deploy.RATE * 2, 1 / deploy.RATE)
            mask = (freq >= 15) & (freq <= 250)
            frequencies.append(float(freq[mask][np.argmax(fft[mask])]))
        self.assertLess(frequencies[-1], frequencies[0] - 3)

    def test_protected_growls_locks_laser_and_tools_are_byte_identical(self):
        # Frozen fingerprints from the prior assets: timing, cuts, gain/fades and
        # each isolated PCM clip. Inside is intentionally excluded/replaced.
        for cues, expected in [
                (deploy.CUES + mechanical.DEPLOY_TEXTURES,
                 'd1fd411139d2082f4d9fc084da220066b3a442e36bde5a7583b48ea6c0ef4be0'),
                (returning.RETURN_LOCKS + mechanical.RETURN_TEXTURES,
                 '5ae568443dde4f3a75faec80487a5d8dce5f54faa3e85293a0ebc491c4f23435')]:
            entries = []
            for cue in cues:
                if cue['source'] == mechanical.INSIDE:
                    continue
                samples, _ = deploy.prepare_layer(self.decoded[cue['source']], cue)
                entry = {key: cue.get(key) for key in [
                    'name', 'source', 'crop', 'onset', 'contact_seconds',
                    'target_rms', 'fade_in', 'fade_out']}
                entry['isolated_wav_sha256'] = mechanical.wav_sha256(samples)
                entries.append(entry)
            self.assertEqual(hashlib.sha256(json.dumps(entries, sort_keys=True).encode()).hexdigest(), expected)
        growl, _ = returning.prepare_return(self.decoded[deploy.GROWL])
        self.assertEqual(mechanical.wav_sha256(growl), returning.SELECTED_A_SHA256)

    def test_compositions_are_finite_bounded_and_preserve_timing(self):
        for compose in [deploy.compose, returning.compose]:
            mix, layers, processing = compose(self.decoded)
            self.assertEqual(len(mix), round(5.3 * deploy.RATE))
            self.assertTrue(np.isfinite(mix).all())
            self.assertLessEqual(float(np.max(np.abs(mix))), .95)
            self.assertTrue(all(0 <= layer['onset_seconds'] < layer['end_seconds'] <= 5.3
                                for layer in layers))
            if compose is returning.compose:
                self.assertEqual(processing['approved_growl_isolated_wav_sha256'], returning.SELECTED_A_SHA256)
            if compose is deploy.compose:
                self.assertNotIn('approved_foundation_sha256', processing)
                self.assertEqual(sum(layer['source'] == mechanical.BASS for layer in layers), 1)
            else:
                self.assertFalse(any(layer['source'] == mechanical.BASS for layer in layers))

    def test_new_source_crops_contain_actual_signal(self):
        for cue in mechanical.DEPLOY_TEXTURES + mechanical.RETURN_TEXTURES:
            start, end = [round(value * deploy.RATE) for value in cue['crop']]
            crop = self.decoded[cue['source']][start:end]
            self.assertGreater(float(np.sqrt(np.mean(crop * crop))), .01)

    def test_inside_crops_capture_attacks_not_steady_hum(self):
        for cue in deploy.CUES + mechanical.DEPLOY_TEXTURES + mechanical.RETURN_TEXTURES:
            if cue['source'] != mechanical.INSIDE:
                continue
            start = round(cue['crop'][0] * deploy.RATE)
            samples = self.decoded[mechanical.INSIDE]
            before = samples[start - round(.05 * deploy.RATE):start]
            attack = samples[start:start + round(.06 * deploy.RATE)]
            self.assertGreater(np.sqrt(np.mean(attack ** 2)),
                               1.15 * np.sqrt(np.mean(before ** 2)), cue['name'])

    def test_bass_metadata_and_crop_use_original_source_clock(self):
        samples, metadata = deploy.prepare_layer(self.decoded[mechanical.BASS], mechanical.DEPLOY_BASS)
        self.assertEqual(len(samples), round(1.9 * deploy.RATE))
        self.assertEqual(metadata['crop_start_seconds'], 75.78)
        self.assertEqual(metadata['crop_end_seconds'], 77.68)
        self.assertEqual(metadata['decoded_source_origin_seconds'], 75)
        self.assertGreater(self.bass_metadata['duration_seconds'], 140)
        self.assertLessEqual(len(self.decoded[mechanical.BASS]), 3 * deploy.RATE)


if __name__ == '__main__':
    unittest.main()
