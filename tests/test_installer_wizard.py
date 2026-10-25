"""Exercise the actual C# filesystem engine in disposable fake game directories."""
from pathlib import Path
import json
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
from build_installer import compile_installer
import build_installer
from project import BASE, OUTPUT


class WizardTests(unittest.TestCase):
    def test_local_update_then_real_wizard_uninstall_and_reinstall(self):
        import install
        executable = OUTPUT/'tests/installer/LocalHandoffTests.exe'
        compile_installer(executable, [BASE/'installer/InstallerCore.cs', BASE/'tests/installer/Program.cs'])
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);game=root/'game';game.mkdir()
            (game/'valheim.exe').write_bytes(b'fake game');(game/'valheim_Data').mkdir()
            subprocess.run([str(executable),'handoff-install',str(game)],check=True)
            (game/'BepInEx/config').mkdir(exist_ok=True)
            (game/'BepInEx/config/personal.cfg').write_text('keep settings')
            (game/'BepInEx/plugins/personal.dll').write_text('keep other mod')
            source=root/'mod';source.mkdir()
            for name in install.REQUIRED:(source/name).write_bytes(b'local developer update')
            install.install_payload(source,game/'BepInEx/plugins/Rachel350Z',root/'backup',lambda:None)
            subprocess.run([str(executable),'handoff-uninstall-reinstall',str(game)],check=True)
            state=json.loads((game/'.nfvu2/install.json').read_text())
            self.assertTrue(all(install.checksum(game/name)==digest for name,digest in state['Mod'].items()))

    def test_packaging_allowlist_includes_music(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            mod = root/'mod'
            (mod/'radio').mkdir(parents=True)
            for name in ('Rachel350Z.dll', 'NFVU2.DriftPhysics.dll', 'body.json', 'private-plugin.dll', 'config.local.json', 'radio/song.wav'):
                (mod/name).write_bytes(b'fixture')
            loader = root/'loader.zip'
            with zipfile.ZipFile(loader, 'w') as archive:
                for name in ('winhttp.dll', 'doorstop_config.ini', 'BepInEx/core/BepInEx.dll',
                             'BepInEx/plugins/private.dll', 'start_game_bepinex.sh'):
                    archive.writestr('BepInExPack_Valheim/'+name, b'fixture')
            target = root/'payload.zip'
            hashes = build_installer.make_payload(target, mod, loader)
            with zipfile.ZipFile(target) as archive:
                names = set(archive.namelist())
                self.assertIn(build_installer.MOD_ROOT+'radio/song.wav', names)
                self.assertIn(build_installer.MOD_ROOT+'NFVU2.DriftPhysics.dll', names)
                self.assertNotIn(build_installer.MOD_ROOT+'private-plugin.dll', names)
                self.assertNotIn(build_installer.MOD_ROOT+'config.local.json', names)
                self.assertNotIn('BepInEx/plugins/private.dll', names)
                self.assertNotIn('start_game_bepinex.sh', names)
                self.assertEqual(json.loads(archive.read('payload.json')), hashes)

    def test_missing_music_prevents_player_package(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            mod = root/'mod'
            mod.mkdir()
            for name in ('Rachel350Z.dll', 'NFVU2.DriftPhysics.dll', 'body.json'):
                (mod/name).write_bytes(b'fixture')
            with self.assertRaisesRegex(ValueError, 'Radio music is required'):
                build_installer.make_payload(root/'payload.zip', mod, root/'loader.zip')
            self.assertFalse((root/'payload.zip').exists())

    def test_missing_own_physics_prevents_incomplete_payload(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            mod = root/'mod'
            (mod/'radio').mkdir(parents=True)
            for name in ('Rachel350Z.dll', 'body.json', 'radio/song.wav'):
                (mod/name).write_bytes(b'fixture')
            with self.assertRaisesRegex(FileNotFoundError, 'NFVU2.DriftPhysics.dll'):
                build_installer.make_payload(root/'payload.zip', mod, root/'loader.zip')
            self.assertFalse((root/'payload.zip').exists())

    def test_cached_loader_hash_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root/'installer').mkdir()
            (root/'installer'/f'BepInExPack_Valheim-{build_installer.LOADER_VERSION}.zip').write_bytes(b'corrupt')
            with patch.object(build_installer, 'LOCAL', root):
                with self.assertRaisesRegex(ValueError, 'checksum mismatch'):
                    build_installer.loader_archive()

    def test_windows_installer_engine(self):
        executable = OUTPUT/'tests/installer/InstallerTests.exe'
        compile_installer(executable, [BASE/'installer/InstallerCore.cs', BASE/'tests/installer/Program.cs'])
        subprocess.run([str(executable)], check=True)

    def test_windows_update_client(self):
        executable = OUTPUT/'tests/installer/UpdateTests.exe'
        compile_installer(executable, [BASE/'installer/InstallerCore.cs', BASE/'installer/ReleaseUpdates.cs',
                                     BASE/'tests/installer/UpdateTests.cs'])
        subprocess.run([str(executable)], check=True)


if __name__ == '__main__':
    unittest.main()
