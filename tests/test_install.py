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
    def test_optional_loading_image_is_packaged_and_checksum_verified(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,backup=self.fixture(root)
            local=root/'local';art=local/'loading-screen';art.mkdir(parents=True)
            (art/'background.png').write_bytes(b'approved image')
            (art/'manifest.json').write_text('{"schema_version":1}')
            (art/'original-source.zip').write_bytes(b'private source')
            sync_optional_assets(local,source)
            self.assertFalse((source/'loading/original-source.zip').exists())
            install.install_payload(source,dest,backup,lambda:None)
            self.assertEqual((dest/'loading/background.png').read_bytes(),b'approved image')
            self.assertTrue(install.allowed_name('loading/background.png'))
            self.assertIn('loading/background.png',json.loads((dest/install.MANIFEST).read_text()))

    def test_successful_install_retains_two_newest_backups_and_named_backup(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,_=self.fixture(root)
            backups=root/'backups';backups.mkdir()
            for name in ['20261007-000000-000001','20261008-000000-000001','flatgrass-20261007']:
                (backups/name).mkdir();(backups/name/'snapshot').write_bytes(b'old')
            newest=backups/'20261009-000000-000001'
            install.install_payload(source,dest,newest,lambda:None)
            self.assertEqual({p.name for p in backups.iterdir()},
                             {'20261008-000000-000001',newest.name,'flatgrass-20261007'})
            self.assertEqual((newest/'Rachel350Z.dll').read_bytes(),b'old')

    def test_failed_install_does_not_prune_backups(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,_=self.fixture(root);backups=root/'backups';backups.mkdir()
            old=['20261007-000000-000001','20261008-000000-000001']
            for name in old:(backups/name).mkdir()
            original=install.checksum
            def corrupt(path):return 'bad' if Path(path)==dest/'radio/song.wav' else original(path)
            with patch.object(install,'checksum',corrupt):
                with self.assertRaises(RuntimeError):
                    install.install_payload(source,dest,backups/'20261009-000000-000001',lambda:None)
            for name in old:self.assertTrue((backups/name).exists())

    def test_backup_cleanup_refuses_linked_contents(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);backups=root/'backups';backups.mkdir()
            for day in [7,8,9]:(backups/f'2026100{day}-000000-000001').mkdir()
            nested=backups/'20261007-000000-000001/nested';nested.mkdir()
            (nested/'keep').write_bytes(b'keep')
            with patch.object(install.os.path,'isjunction',lambda p:Path(p)==nested,create=True):
                with self.assertRaises(ValueError):install.prune_backups(backups)
            self.assertEqual((nested/'keep').read_bytes(),b'keep')

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
            self.assertEqual(install.install_payload(source,dest,backup,lambda:None),len(install.REQUIRED)+1)
            self.assertEqual((backup/'radio/old.wav').read_bytes(),b'old song')
            self.assertFalse((dest/'radio/old.wav').exists())
            self.assertEqual((dest/'radio/personal.ogg').read_bytes(),b'keep')
            for name in install.payload_files(source):
                self.assertEqual(install.checksum(source/name),install.checksum(dest/name))
            self.assertEqual((backup/'NFVU2.DriftPhysics.dll').read_bytes(),b'old')
            self.assertIn('NFVU2.DriftPhysics.dll',json.loads((dest/install.MANIFEST).read_text()))
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
    def test_effects_are_synced_installed_and_backed_up(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,backup=self.fixture(root)
            local=root/'local';effects=local/'audio-effects';effects.mkdir(parents=True)
            (effects/'nitro.wav').write_bytes(b'nitro');(effects/'purge.wav').write_bytes(b'purge')
            (effects/'source.abk').write_bytes(b'original bank stays local')
            (dest/'audio/effects').mkdir(parents=True)
            (dest/'audio/effects/nitro.wav').write_bytes(b'old effect')
            sync_optional_assets(local,source)
            install.install_payload(source,dest,backup,lambda:None)
            self.assertEqual((backup/'audio/effects/nitro.wav').read_bytes(),b'old effect')
            self.assertEqual((dest/'audio/effects/nitro.wav').read_bytes(),b'nitro')
            self.assertEqual((dest/'audio/effects/purge.wav').read_bytes(),b'purge')
            self.assertFalse((dest/'audio/effects/source.abk').exists())

    def test_exhaust_media_is_installed_without_source_bank_or_runtime(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,backup=self.fixture(root)
            local=root/'local';exhaust=local/'audio-exhaust';exhaust.mkdir(parents=True)
            (exhaust/'overrun_01.wav').write_bytes(b'pop');(exhaust/'backfire.wav').write_bytes(b'bang')
            (exhaust/'manifest.json').write_text('{}')
            (exhaust/'source.bank').write_bytes(b'private');(exhaust/'runtime.dll').write_bytes(b'private')
            (dest/'audio/exhaust').mkdir(parents=True);(dest/'audio/exhaust/backfire.wav').write_bytes(b'old')
            sync_optional_assets(local,source);install.install_payload(source,dest,backup,lambda:None)
            self.assertEqual((backup/'audio/exhaust/backfire.wav').read_bytes(),b'old')
            self.assertEqual((dest/'audio/exhaust/backfire.wav').read_bytes(),b'bang')
            self.assertEqual({p.name for p in (dest/'audio/exhaust').iterdir()},{'overrun_01.wav','backfire.wav','manifest.json'})
    def test_reject_manifest_escape_before_mutation(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            (dest/install.MANIFEST).write_text(json.dumps(['../outside.wav']))
            with self.assertRaises(ValueError):install.install_payload(source,dest,backup,lambda:None)
            self.assertFalse(backup.exists())
    def test_transformer_impacts_are_staged_installed_and_backed_up(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,backup=self.fixture(root)
            impacts=root/'local/audio-transformer';impacts.mkdir(parents=True)
            (impacts/'step_01.wav').write_bytes(b'prepared impact')
            (impacts/'manifest.json').write_text(json.dumps({'step_01.wav':'local cut'}))
            (impacts/'source.mp3').write_bytes(b'original recording stays local')
            (dest/'audio/transformer').mkdir(parents=True)
            (dest/'audio/transformer/step_01.wav').write_bytes(b'old impact')
            sync_optional_assets(root/'local',source)
            self.assertTrue((impacts/'source.mp3').exists())
            self.assertFalse((source/'audio/transformer/source.mp3').exists())
            install.install_payload(source,dest,backup,lambda:None)
            self.assertEqual((backup/'audio/transformer/step_01.wav').read_bytes(),b'old impact')
            self.assertEqual((dest/'audio/transformer/step_01.wav').read_bytes(),b'prepared impact')
            self.assertEqual(json.loads((dest/'audio/transformer/manifest.json').read_text()),{'step_01.wav':'local cut'})
            self.assertIn('audio/transformer/step_01.wav',json.loads((dest/install.MANIFEST).read_text()))
            self.assertFalse((dest/'audio/transformer/source.mp3').exists())
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
