"""Footfall bass checks; original downloaded recordings remain optional/local."""
import json
import os
from pathlib import Path
import subprocess
import sys
from tempfile import TemporaryDirectory
import unittest

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
import prepare_transformer_audio as audio


ORIGINAL_HASHES = {
    'step_01.wav': '8c36c2f57feab1c3c87c27cab676538a2ff8ae81f1a0f5f312b3283821c7849e',
    'step_02.wav': 'a5862568953fbbed0c5ea9b5ce3df1bc7baeb44f0c507c20965d02ffa3c05e90',
    'step_03.wav': 'd35365aeabe047351f90c6a46a04a8dab1a70ab93e5740f0a3f50c07dd6af053',
    'step_04.wav': '7298f08d6113d516c9572d7317d1dd978bdc7b3dc56f87ed1d9573df692747a4',
    'step_05.wav': '438ed48a4148928862de23e1213a0cbbf8b73b3b919feec94c8221a0a52743d0',
    'step_07.wav': 'c961c6f78ccff7eabcf7da511f822121b6c17d289f5f67fd4448b0b3fd171cad',
}


def low_band_rms(samples):
    length = round(.3 * audio.RATE)
    selected = np.zeros(length)
    selected[:min(length, len(samples))] = samples[:length]
    spectrum = np.fft.rfft(selected)
    frequencies = np.fft.rfftfreq(length, 1 / audio.RATE)
    spectrum[(frequencies < 20) | (frequencies > 100)] = 0
    return float(np.sqrt(np.mean(np.fft.irfft(spectrum, n=length) ** 2)))


class StepBassPreparationTests(unittest.TestCase):
    def bass_source(self):
        t = np.arange(audio.RATE) / audio.RATE
        return .4 * np.sin(2 * np.pi * 49.1 * t) + .1 * np.sin(2 * np.pi * 61.7 * t)

    def test_punch_has_short_sprint_safe_duration_and_bounded_gain(self):
        bass, metadata = audio.prepare_step_bass(self.bass_source())
        self.assertEqual(len(bass), round(.28 * audio.RATE))
        self.assertLess(len(bass) / audio.RATE / .955, .385)
        self.assertAlmostEqual(audio.max_window_rms(bass), .20)
        self.assertLessEqual(np.max(np.abs(bass)), .65)
        self.assertEqual(bass[0], 0)
        self.assertEqual(bass[-1], 0)
        self.assertEqual(metadata['crop_start_seconds'], .2)
        self.assertEqual(metadata['crop_end_seconds'], .48)

    def test_addition_aligns_attack_and_keeps_mechanical_foundation_exact(self):
        bass, metadata = audio.prepare_step_bass(self.bass_source())
        base = .05 * np.sin(np.arange(round(.4 * audio.RATE)) * .31)
        original = base.copy()
        mix, processing = audio.add_step_bass(base, .015, bass, metadata)
        self.assertEqual(processing['bass_attack_timeline_seconds'], round(.015 * audio.RATE) / audio.RATE)
        onset = processing['bass_onset_sample']
        expected = base.copy()
        expected[onset:onset + len(bass)] += bass
        np.testing.assert_array_equal(mix, expected)
        np.testing.assert_array_equal(base, original)
        np.testing.assert_array_equal(mix[:onset], original[:onset])
        self.assertEqual(processing['foundation_gain_db'], 0)
        self.assertEqual(processing['mix_gain_db'], 0)
        self.assertEqual(processing['foundation_isolated_wav_sha256'], audio.pcm_wav_sha256(original))

    def test_headroom_constraint_changes_only_new_bass(self):
        base = np.full(round(.4 * audio.RATE), .85)
        bass = np.full(round(.28 * audio.RATE), .3)
        original = base.copy()
        mixed, metadata = audio.add_step_bass(base, .015, bass, {'attack_offset_seconds': .005})
        self.assertLess(metadata['bass_headroom_gain_linear'], 1)
        self.assertLessEqual(np.max(np.abs(mixed)), .9)
        np.testing.assert_array_equal(base, original)
        self.assertEqual(metadata['foundation_gain_db'], 0)

    def test_invalid_sources_and_alignment_are_rejected(self):
        for samples in [np.zeros(audio.RATE), np.zeros(100), np.full(audio.RATE, np.nan)]:
            with self.assertRaises(ValueError):
                audio.prepare_step_bass(samples)
        with self.assertRaises(ValueError):
            audio.add_step_bass(np.zeros(100), 0, np.ones(100) * .1,
                                {'attack_offset_seconds': .1})
        self.assertEqual({item[0] for item in audio.SELECTIONS}, {1, 2, 3, 4, 5, 7})


@unittest.skipUnless(os.environ.get('NFVU2_AUDIO_SOURCE_DIR'),
                     'Set NFVU2_AUDIO_SOURCE_DIR for optional original-recording regression checks')
class LocalStepBassRegressionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sources = Path(os.environ['NFVU2_AUDIO_SOURCE_DIR'])
        ffmpeg = audio.find_ffmpeg(None)
        cls.decoded = {name: audio.decode(ffmpeg, cls.sources / name)
                       for name in {item[1] for item in audio.SELECTIONS}}
        cls.bass, cls.bass_metadata = audio.prepare_step_bass(audio.decode(ffmpeg, cls.sources / 'bass.mp3'))

    def test_every_variant_preserves_base_and_adds_the_same_aligned_bass(self):
        for index, name, start, end in audio.SELECTIONS:
            base, metadata = audio.prepare_step(self.decoded[name], start, end)
            original = base.copy()
            expected_hash = ORIGINAL_HASHES[f'step_{index:02d}.wav']
            self.assertEqual(audio.pcm_wav_sha256(base), expected_hash)
            mix, processing = audio.add_step_bass(base, metadata['attack_offset_seconds'], self.bass, self.bass_metadata)
            self.assertEqual(processing['foundation_isolated_wav_sha256'], expected_hash)
            self.assertEqual(processing['bass_headroom_gain_linear'], 1)
            self.assertEqual(processing['bass_attack_timeline_seconds'], metadata['attack_offset_seconds'])
            onset = processing['bass_onset_sample']
            expected = np.zeros(len(mix))
            expected[:len(base)] = base
            expected[onset:onset + len(self.bass)] += self.bass
            np.testing.assert_array_equal(mix, expected)
            np.testing.assert_array_equal(base, original)
            self.assertTrue(np.isfinite(mix).all())
            self.assertLessEqual(np.max(np.abs(mix)), .9)
            self.assertLess(len(mix) / audio.RATE, 3)
            self.assertGreater(low_band_rms(mix), 1.5 * low_band_rms(base))

    def test_optional_cli_reproduces_original_and_writes_six_bass_variants(self):
        with TemporaryDirectory(prefix='nfvu2-step-bass-') as temporary:
            target = Path(temporary)
            command = [sys.executable, str(ROOT / 'tools/prepare_transformer_audio.py'),
                       '--source-dir', str(self.sources), '--output-dir', str(target)]
            subprocess.run(command, check=True, capture_output=True)
            self.assertEqual({p.name: audio.sha256(p) for p in target.glob('step_*.wav')}, ORIGINAL_HASHES)
            subprocess.run(command + ['--bass-file', str(self.sources / 'bass.mp3')],
                           check=True, capture_output=True)
            manifest = json.loads((target / 'manifest.json').read_text())
            self.assertEqual({p.name for p in target.glob('step_*.wav')}, set(ORIGINAL_HASHES))
            self.assertEqual(len(manifest['files']), 6)
            bass_hashes = set()
            for item in manifest['files']:
                self.assertEqual(item['role'], 'footstep')
                self.assertEqual(len(item['layers']), 2)
                self.assertEqual(item['layers'][0]['isolated_wav_sha256'], ORIGINAL_HASHES[item['file']])
                self.assertEqual(item['layers'][1]['source'], 'bass.mp3')
                bass_hashes.add(item['layers'][1]['isolated_wav_sha256'])
            self.assertEqual(len(bass_hashes), 1)


if __name__ == '__main__':
    unittest.main()
