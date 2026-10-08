import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
import install
from mod_files import sync_optional_assets

class InstallTests(unittest.TestCase):
    def fixture(self,root):
        source=root/'build';dest=root/'installed'
        source.mkdir();dest.mkdir()
        for name in install.REQUIRED:
            (source/name).write_bytes(b'new');(dest/name).write_bytes(b'old')
        (source/'radio').mkdir();(source/'radio/song.wav').write_bytes(b'song')
        return source,dest,root/'backup'
    def test_payload_checksums_backup_and_stale_owned_only(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            (dest/'radio').mkdir()
            (dest/'radio/old.wav').write_bytes(b'old song')
            (dest/'radio/personal.ogg').write_bytes(b'keep')
            (dest/install.MANIFEST).write_text(json.dumps(['radio/old.wav']))
            self.assertEqual(install.install_payload(source,dest,backup,lambda:None),3)
            self.assertEqual((backup/'radio/old.wav').read_bytes(),b'old song')
            self.assertFalse((dest/'radio/old.wav').exists())
            self.assertEqual((dest/'radio/personal.ogg').read_bytes(),b'keep')
            for name in install.payload_files(source):
                self.assertEqual(install.checksum(source/name),install.checksum(dest/name))
    def test_copy_failure_restores_old_and_removes_partial_new(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            original=install.checksum
            def corrupt(path):
                if Path(path)==dest/'radio/song.wav':return 'bad checksum'
                return original(path)
            with patch.object(install,'checksum',corrupt):
                with self.assertRaises(RuntimeError):install.install_payload(source,dest,backup,lambda:None)
            self.assertEqual((dest/'Rachel350Z.dll').read_bytes(),b'old')
            self.assertFalse((dest/'radio/song.wav').exists())
    def test_reject_manifest_escape_before_mutation(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            (dest/install.MANIFEST).write_text(json.dumps(['../outside.wav']))
            with self.assertRaises(ValueError):install.install_payload(source,dest,backup,lambda:None)
            self.assertFalse(backup.exists())
    def test_radio_payload_follows_playlist_and_retains_original_streams(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);local=root/'local';output=root/'build'
            radio=local/'radio';radio.mkdir(parents=True)
            (radio/'song.wav').write_bytes(b'music')
            (radio/'ambient.wav').write_bytes(b'not a song')
            (radio/'tracks.json').write_text(json.dumps([{'file':'song.wav','title':'Artist - Song'}]))
            (output/'radio').mkdir(parents=True);(output/'radio/ambient.wav').write_bytes(b'stale')
            sync_optional_assets(local,output)
            self.assertTrue((radio/'ambient.wav').exists())
            self.assertFalse((output/'radio/ambient.wav').exists())
            self.assertEqual((output/'radio/song.wav').read_bytes(),b'music')
    def test_radio_missing_manifest_song_fails_before_install(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            (source/'radio/tracks.json').write_text(json.dumps([{'file':'missing.wav'}]))
            with self.assertRaises(ValueError):install.install_payload(source,dest,backup,lambda:None)
            self.assertFalse(backup.exists())

if __name__=='__main__':unittest.main()
