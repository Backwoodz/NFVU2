import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from export_menu import language_table
from mod_files import sync_optional_assets, payload_files, REQUIRED
from project import LOCAL


class MenuTests(unittest.TestCase):
    def test_language_hash_join(self):
        data = bytearray(60)
        struct.pack_into('<I', data, 0, 0x39000)
        struct.pack_into('<3I', data, 12, 2, 16, 32)
        struct.pack_into('<4I', data, 24, 123, 0, 456, 7)
        data[40:53] = b'Garage\0Felgen\0'
        self.assertEqual(language_table(data), {123: 'Garage', 456: 'Felgen'})
        struct.pack_into('<I', data, 28, 9999)
        with self.assertRaises(ValueError): language_table(data)

    def test_reject_invalid_tables(self):
        for data in (b'', bytes(30), struct.pack('<6I', 0x39000, 0, 0, 100000, 16, 32)):
            with self.assertRaises(ValueError): language_table(data)

    def test_menu_payload_excludes_original_banks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); local = root/'local'; menu = local/'menu'; menu.mkdir(parents=True)
            for name in ('menu.json', 'navigate.wav', 'accept.wav', 'back.wav', 'provenance.json', 'source.abk'):
                (menu/name).write_bytes(b'fixture')
            output=root/'build';sync_optional_assets(local, output)
            for name in REQUIRED: (output/name).write_bytes(b'fixture')
            files={str(p).replace('\\', '/') for p in payload_files(output)}
            self.assertIn('menu/menu.json', files);self.assertIn('menu/accept.wav', files)
            self.assertNotIn('menu/source.abk', files)

    @unittest.skipUnless((LOCAL/'menu/menu.json').exists(), 'optional local menu export absent')
    def test_local_original_catalog(self):
        import base64
        import hashlib
        import wave
        data=json.loads((LOCAL/'menu/menu.json').read_text())
        self.assertEqual(len(data['icons']), 11)
        self.assertEqual(sum(icon is not None for icon in data['icons']), 10)
        self.assertEqual(data['titles'][5], 'Felgen')
        self.assertEqual(data['titles'][10], 'Garage')
        for icon in filter(None, data['icons']):
            self.assertEqual(len(base64.b64decode(icon['rgba'])), icon['width']*icon['height']*4)
        for entry in json.loads((LOCAL/'menu/provenance.json').read_text())['files']:
            path=LOCAL/'menu'/entry['file']
            self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), entry['sha256'])
            with wave.open(str(path), 'rb') as wav:
                self.assertEqual(wav.getnchannels(), 1)
                self.assertLess(wav.getnframes()/wav.getframerate(), 1)
