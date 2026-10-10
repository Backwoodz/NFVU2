"""Local-source regression for preserving relative turbo sample loudness."""
import hashlib
import json
from pathlib import Path
import sys
import unittest
import wave
import numpy as np

sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL


def samples(path):
    with wave.open(str(path),'rb') as stream:
        return np.frombuffer(stream.readframes(stream.getnframes()),dtype='<i2').astype(float)/32768


class TurboAudioTests(unittest.TestCase):
    @unittest.skipUnless((LOCAL/'audio-turbo/manifest.json').is_file() and
                         (LOCAL/'audio-decoded/TURBO_MED_2_MB_2.wav').is_file(), 'local turbo source absent')
    def test_original_relative_loudness_survives_import(self):
        manifest=json.loads((LOCAL/'audio-turbo/manifest.json').read_text())
        originals=[];prepared=[]
        for name,index in [('blowoff_low.wav',2),('blowoff_high.wav',3)]:
            source=samples(LOCAL/f'audio-decoded/TURBO_MED_2_MB_{index}.wav')
            output=LOCAL/'audio-turbo'/name;result=samples(output)
            originals.append(np.sqrt(np.mean(source*source)));prepared.append(np.sqrt(np.mean(result*result)))
            self.assertGreater(np.max(np.abs(result)),.1)
            self.assertLess(np.max(np.abs(result)),.83)
            entry=next(e for e in manifest['files'] if e['file']==name)
            self.assertEqual(hashlib.sha256(output.read_bytes()).hexdigest(),entry['sha256'])
        # Independent RMS normalisation makes this ratio 1 and fails the test.
        self.assertAlmostEqual(prepared[0]/prepared[1],originals[0]/originals[1],delta=.02)


if __name__=='__main__':unittest.main()
