import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
import build_nexus
from mod_files import REQUIRED


class NexusTests(unittest.TestCase):
    def fixture(self, root):
        source = root/'mod'
        source.mkdir()
        for name in REQUIRED:
            (source/name).write_bytes(('fresh '+name).encode())
        (source/'radio').mkdir()
        (source/'radio/song.ogg').write_bytes(b'full radio track')
        (source/'radio/tracks.json').write_text(json.dumps([{'file': 'song.ogg', 'title': 'A song'}]))
        return source

    def test_flat_manual_archive_matches_build_and_excludes_loader_setup_and_private_files(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self.fixture(root)
            for name in ('winhttp.dll', 'NFVU2-Setup.exe', 'payload.zip', 'private.dll', 'personal.cfg'):
                (source/name).write_bytes(b'not for Nexus')
            (source/'radio/unlisted.ogg').write_bytes(b'not in playlist')
            (source/'loading').mkdir()
            (source/'loading/background.png').write_bytes(b'loading image')
            archive = build_nexus.make_package(source, root/'output', '0.31.2')
            with zipfile.ZipFile(archive) as zipped:
                manifest = json.loads(zipped.read(build_nexus.MANIFEST))
                self.assertEqual(manifest['version'], '0.31.2')
                self.assertEqual(set(zipped.namelist()), set(manifest['files']) | {build_nexus.MANIFEST})
                for name, digest in manifest['files'].items():
                    data = zipped.read(name)
                    self.assertEqual(hashlib.sha256(data).hexdigest(), digest)
                    self.assertEqual((archive.parent/'package'/name).read_bytes(), data)
                for name in REQUIRED:
                    self.assertEqual(zipped.read(build_nexus.MOD_ROOT+name), (source/name).read_bytes())
                self.assertIn(build_nexus.MOD_ROOT+'loading/background.png', zipped.namelist())
                self.assertFalse(any(name.endswith(('.exe', '.zip', '.7z', '.cfg', '.ps1', '.py')) for name in zipped.namelist()))
                self.assertFalse(any('BepInEx/core/' in name or 'unlisted.ogg' in name or 'private.dll' in name for name in zipped.namelist()))
            expected, filename = archive.with_suffix('.zip.sha256').read_text().split()
            self.assertEqual(filename, archive.name)
            self.assertEqual(expected, build_nexus.checksum(archive))

    def test_no_radio_or_missing_song_fails_before_creating_output(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self.fixture(root)
            (source/'radio/song.ogg').unlink()
            with self.assertRaises(ValueError):
                build_nexus.make_package(source, root/'output', '0.31.2')
            self.assertFalse((root/'output').exists())

    def test_empty_radio_track_is_not_shipped(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self.fixture(root)
            (source/'radio/song.ogg').write_bytes(b'')
            with self.assertRaises(ValueError):
                build_nexus.make_package(source, root/'output', '0.31.2')

    def test_existing_output_is_never_replaced(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self.fixture(root)
            destination = root/'output'
            archive = build_nexus.make_package(source, destination, '0.31.2')
            digest = build_nexus.checksum(archive)
            with self.assertRaises(FileExistsError):
                build_nexus.make_package(source, destination, '0.31.2')
            self.assertEqual(build_nexus.checksum(archive), digest)

    def test_same_input_produces_same_zip_without_build_machine_paths(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self.fixture(root)
            first = build_nexus.make_package(source, root/'first', '0.31.2')
            second = build_nexus.make_package(source, root/'second', '0.31.2')
            self.assertEqual(build_nexus.checksum(first), build_nexus.checksum(second))
            with zipfile.ZipFile(first) as zipped:
                self.assertNotIn(str(root), zipped.read(build_nexus.MANIFEST).decode())

    def test_modified_zip_entry_fails_checksum_verification(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            archive = build_nexus.make_package(self.fixture(root), root/'output', '0.31.2')
            with zipfile.ZipFile(archive) as zipped:
                files = {name: zipped.read(name) for name in zipped.namelist()}
            expected = json.loads(files[build_nexus.MANIFEST])['files']
            files[build_nexus.MOD_ROOT+'Rachel350Z.dll'] = b'modified'
            with zipfile.ZipFile(archive, 'w') as zipped:
                for name, data in files.items():
                    zipped.writestr(name, data)
            with self.assertRaisesRegex(ValueError, 'checksum mismatch'):
                build_nexus.verify_archive(archive, expected)

    def test_reparse_point_is_rejected_before_packaging(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self.fixture(root)
            original = Path.stat
            class Linked:
                st_file_attributes = 0x400
                st_mode = 0o100644
            def stat(path, *args, **kwargs):
                if path == source/'Rachel350Z.dll':
                    return Linked()
                return original(path, *args, **kwargs)
            with patch.object(Path, 'stat', stat):
                with self.assertRaisesRegex(ValueError, 'Linked'):
                    build_nexus.make_package(source, root/'output', '0.31.2')
            self.assertFalse((root/'output').exists())


if __name__ == '__main__':
    unittest.main()
