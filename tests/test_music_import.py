import struct
import hashlib
import json
import sys
import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from extract_music import big_entries, extract_archive, music_stream, source_maps, title_catalog, excluded_catalog, assign_title, refresh_titles


class MusicImportTests(unittest.TestCase):
    def archive(self, folder, name='pfdata/MusicSFx.mpf', payload=b'PFDx', truncate=False):
        directory = struct.pack('>II', 16 + 9 + len(name), len(payload)) + name.encode() + b'\0'
        data = b'BIG4' + struct.pack('>III', 16 + len(directory) + len(payload), 1, 16 + len(directory)) + directory + payload
        path = Path(folder) / 'test.viv'
        path.write_bytes(data[:-1] if truncate else data)
        return path

    def test_extract_original_names_and_bytes(self):
        with tempfile.TemporaryDirectory() as folder:
            path = self.archive(folder)
            output = extract_archive(path, Path(folder) / 'out')
            self.assertEqual((output / 'pfdata/MusicSFx.mpf').read_bytes(), b'PFDx')

    def test_reject_traversal_and_truncation(self):
        with tempfile.TemporaryDirectory() as folder:
            for name in ('../escape.mpf', '/root.mpf', 'C:/absolute.mpf', 'a/../b.mpf'):
                with self.assertRaises(ValueError):
                    big_entries(self.archive(folder, name))
            with self.assertRaises(ValueError):
                big_entries(self.archive(folder, truncate=True))

    def test_filter_stingers_and_mono_sfx(self):
        info = dict(sampleRate=44100, numberOfSamples=44100*120, channels=2)
        self.assertTrue(music_stream(info))
        self.assertFalse(music_stream(dict(info, channels=1)))
        self.assertFalse(music_stream(dict(info, numberOfSamples=44100*4)))
        self.assertFalse(music_stream(dict(info, sampleRate=0)))

    def test_reject_ambiguous_map_output_names(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for subdir in ('one', 'two'):
                (root / subdir).mkdir()
                (root / subdir / 'Music.mpf').write_bytes(b'')
            with self.assertRaisesRegex(ValueError, 'Ambiguous'):
                source_maps(root)

    def test_verified_catalog_uses_one_based_sample_ids(self):
        catalog = title_catalog()
        songs = {key[1]: track for key, track in catalog.items()}
        self.assertEqual(len(songs), 27)
        self.assertEqual(songs[39]['artist'], 'Snoop Dogg feat. The Doors')
        self.assertEqual(songs[39]['song'], 'Riders on the Storm (Fredwreck Remix)')
        self.assertEqual(songs[4]['artist'], 'Capone')
        self.assertEqual(songs[3]['artist'], 'The Bronx')
        self.assertEqual(songs[32]['artist'], 'Unwritten Law')
        excluded = {key[1] for key in excluded_catalog()}
        self.assertEqual(len(excluded), 16)
        self.assertFalse(excluded & set(songs))
        self.assertEqual(excluded | set(songs), set(range(1, 44)))

    def test_changed_audio_or_wrong_sample_never_gets_known_title(self):
        catalog = title_catalog()
        key = next(k for k in catalog if k[1] == 39)
        entry = dict(source=key[0], subsong=39, sha256=key[2], title='Unknown')
        self.assertTrue(assign_title(entry, catalog))
        self.assertIn('Riders on the Storm', entry['title'])
        self.assertFalse(assign_title(dict(entry, sha256='0'*64), catalog))
        self.assertFalse(assign_title(dict(entry, subsong=40), catalog))
        self.assertFalse(assign_title(dict(entry, source='AnotherEdition.mpf'), catalog))

    def test_explicit_custom_titles_override_without_stale_artist_metadata(self):
        entry = dict(source='Custom.mpf', subsong=1, sha256='0'*64, artist='Old artist', song='Old song')
        self.assertTrue(assign_title(entry, {}, {'Custom.mpf:1': 'My artist – My song'}))
        self.assertEqual(entry['title'], 'My artist – My song')
        self.assertNotIn('artist', entry)
        with self.assertRaisesRegex(ValueError, 'Invalid title override'):
            assign_title(entry, {}, {'Custom.mpf:1': []})

    def test_refresh_filters_non_songs_without_deleting_local_audio(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            tracks = []
            for index in (1, 2):
                path = root / f'track{index}.wav'
                path.write_bytes(bytes([index])*10)
                tracks.append(dict(file=path.name, source='Music.mpf', subsong=index,
                                   sha256=hashlib.sha256(path.read_bytes()).hexdigest(), title='Numbered'))
            manifest = root / 'tracks.json'
            manifest.write_text(json.dumps(tracks), encoding='utf-8')
            catalog = {('music.mpf', 1, tracks[0]['sha256']): dict(artist='Artist', song='Song')}
            excluded = {('music.mpf', 2, tracks[1]['sha256'])}
            with patch('extract_music.title_catalog', return_value=catalog), patch('extract_music.excluded_catalog', return_value=excluded):
                self.assertEqual(refresh_titles(root), (1, 1))
            result = json.loads(manifest.read_text(encoding='utf-8'))
            self.assertEqual(result[0]['title'], 'Artist – Song')
            self.assertTrue((root / 'track2.wav').is_file())
            self.assertEqual((root / 'track1.wav').read_bytes(), bytes([1])*10)

    def test_unknown_edition_requires_explicit_research_fallback(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            audio = root / 'unknown.wav'
            audio.write_bytes(b'unknown audio')
            manifest = root / 'tracks.json'
            manifest.write_text(json.dumps([dict(file=audio.name, source='Unknown.mpf', subsong=1,
                                                 sha256=hashlib.sha256(audio.read_bytes()).hexdigest(), title='Research stream')]), encoding='utf-8')
            original = manifest.read_bytes()
            with self.assertRaisesRegex(ValueError, 'Unverified music identity'):
                refresh_titles(root)
            self.assertEqual(manifest.read_bytes(), original)
            self.assertEqual(refresh_titles(root, include_unmapped=True), (0, 1))

    def test_modified_local_wav_keeps_existing_playlist_untouched(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'changed.wav').write_bytes(b'replaced audio')
            manifest = root / 'tracks.json'
            manifest.write_text(json.dumps([dict(file='changed.wav', source='MusicSFx.mpf', subsong=39,
                                                 sha256='0'*64, title='Previous title')]), encoding='utf-8')
            original = manifest.read_bytes()
            with self.assertRaisesRegex(ValueError, 'Audio changed'):
                refresh_titles(root)
            self.assertEqual(manifest.read_bytes(), original)


if __name__ == '__main__':
    unittest.main()
