import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
from prepare_radio import compressed_source, digest, encode_radio
from mod_files import asset_files, sync_optional_assets, payload_files, REQUIRED


class RadioCompressionTests(unittest.TestCase):
    def fixture(self, root):
        source, encoded = root/'radio', root/'radio-ogg'
        source.mkdir(); encoded.mkdir()
        (source/'song.wav').write_bytes(b'original WAV fixture')
        original = dict(file='song.wav', title='Artist - Song', source='Music.mpf',
                        subsong=1, sha256=digest(source/'song.wav'))
        (source/'tracks.json').write_text(json.dumps([original]))
        (encoded/'song.ogg').write_bytes(b'encoded Ogg fixture')
        compressed = dict(original, file='song.ogg', sha256=digest(encoded/'song.ogg'),
                          source_sha256=original['sha256'])
        (encoded/'tracks.json').write_text(json.dumps([compressed]))
        (encoded/'compression.json').write_text(json.dumps(dict(source_manifest_sha256=digest(source/'tracks.json'))))
        return source, encoded

    def test_build_selects_ogg_preserves_masters_and_removes_stale_output_wav(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); source, encoded = self.fixture(root)
            (encoded/'unlisted.ogg').write_bytes(b'not a song')
            out = root/'build'; (out/'radio').mkdir(parents=True)
            (out/'radio/song.wav').write_bytes(b'old generated WAV')
            sync_optional_assets(root, out)
            self.assertEqual({p.name for p in (out/'radio').iterdir()}, {'song.ogg', 'tracks.json'})
            self.assertEqual((source/'song.wav').read_bytes(), b'original WAV fixture')
            self.assertEqual(json.loads((out/'radio/tracks.json').read_text())[0]['title'], 'Artist - Song')
            for name in REQUIRED:
                (out/name).write_bytes(b'fixture')
            names = {p.as_posix() for p in payload_files(out)}
            self.assertIn('radio/song.ogg', names)
            self.assertNotIn('radio/song.wav', names)
            self.assertNotIn('radio/compression.json', names)

    def test_no_compressed_cache_keeps_wav_workflow(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertEqual(compressed_source(Path(tmp)), Path(tmp)/'radio')

    def test_stale_or_corrupt_cache_is_rejected(self):
        for change in ('source_audio', 'source_title', 'encoded_audio', 'missing_audio', 'missing_metadata'):
            with self.subTest(change=change), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp); source, encoded = self.fixture(root)
                if change == 'source_audio': (source/'song.wav').write_bytes(b'changed')
                if change == 'source_title': (source/'tracks.json').write_text('[]')
                if change == 'encoded_audio': (encoded/'song.ogg').write_bytes(b'truncated')
                if change == 'missing_audio': (encoded/'song.ogg').unlink()
                if change == 'missing_metadata': (encoded/'compression.json').unlink()
                with self.assertRaisesRegex(ValueError, 'rerun tools/prepare_radio.py'):
                    compressed_source(root)

    def test_unsafe_or_duplicate_names_are_rejected(self):
        for names in (['../song.ogg'], ['song.ogg', 'song.ogg']):
            with tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp); _, encoded = self.fixture(root)
                (encoded/'tracks.json').write_text(json.dumps([dict(file=n) for n in names]))
                with self.assertRaises(ValueError):
                    asset_files(encoded, {'.ogg', '.json'})

    def test_encoder_failure_preserves_previous_playlist_and_source(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); source, encoded = self.fixture(root)
            before = {p: p.read_bytes() for p in [*source.iterdir(), *encoded.iterdir()]}
            with patch('prepare_radio.subprocess.check_output', return_value='test encoder'), \
                 patch('prepare_radio.subprocess.run', side_effect=subprocess.CalledProcessError(1, 'ffmpeg')):
                with self.assertRaises(subprocess.CalledProcessError):
                    encode_radio(source, encoded, 'ffmpeg')
            self.assertTrue(all(p.read_bytes() == data for p, data in before.items()))
            self.assertFalse(list(root.glob('radio-encode-*')))


if __name__ == '__main__':
    unittest.main()
