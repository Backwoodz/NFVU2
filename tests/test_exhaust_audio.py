from pathlib import Path
import sys
import tempfile
import unittest
import wave
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from prepare_exhaust_audio import prepare


class ExhaustAudioTests(unittest.TestCase):
    def write(self, path, samples):
        with wave.open(str(path), 'wb') as wav:
            wav.setparams((2, 2, 44100, len(samples), 'NONE', 'not compressed'))
            wav.writeframes(samples.astype('<i2').tobytes())

    def test_trim_mono_tail_and_preserved_level(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            samples = np.zeros((44100, 2))
            samples[4410:22050] = (np.sin(np.arange(17640) * .11) * 12000)[:, None]
            self.write(root/'input.wav', samples)
            row = prepare(root/'input.wav', root/'output.wav', .36)
            with wave.open(str(root/'output.wav'), 'rb') as wav:
                x = np.frombuffer(wav.readframes(wav.getnframes()), '<i2')
                self.assertEqual((wav.getnchannels(), wav.getsampwidth(), wav.getframerate()), (1, 2, 44100))
            self.assertEqual(len(x), 15876)
            self.assertLess(abs(row['trim_start_samples']-4410), 100)
            self.assertLess(abs(x[0]), 2);self.assertLess(abs(x[-1]), 2)
            self.assertLessEqual(abs(x).max(), 12120)  # <=1% allowance for DC removal
            self.assertGreater(row['rms'], .1)

    def test_reject_silence_and_already_clipped_source(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for samples in (np.zeros((1000, 2)), np.full((1000, 2), 32767)):
                self.write(root/'input.wav', samples)
                with self.assertRaises(ValueError):prepare(root/'input.wav', root/'output.wav', .36)

    def test_sustained_texture_has_no_silent_loop_seam_and_bounded_working_level(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            t=np.arange(44100*4);mono=np.sin(t*.11)*3000+np.sin(t*.017)*1000
            self.write(root/'input.wav',np.column_stack((mono,mono)))
            row=prepare(root/'input.wav',root/'output.wav',3,loop=True)
            with wave.open(str(root/'output.wav'),'rb') as wav:
                x=np.frombuffer(wav.readframes(wav.getnframes()),'<i2').astype(float)/32768
            self.assertTrue(row['loop']);self.assertGreater(len(x),44100*2.9)
            self.assertGreater(row['rms'],.07);self.assertLessEqual(row['peak'],.751)
            self.assertLess(abs(x[0]-x[-1]),.05)
