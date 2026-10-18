"""Check the public ZIP contract and rejection of unverified setup files."""
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
from package_installer import package_installer, sha256


class PackageInstallerTests(unittest.TestCase):
    def fixture(self, folder):
        executable = folder/'NFVU2-Setup-1.2.3.exe'
        executable.write_bytes(b'fixture setup bytes')
        executable.with_suffix('.sha256').write_text(
            f'{sha256(executable)}  {executable.name}\n', encoding='utf-8')
        (folder/'private-plugin.dll').write_bytes(b'excluded')
        return executable

    def test_package_preserves_verified_installer_and_checksums(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            executable = self.fixture(folder)
            package, checksum = package_installer(folder, '1.2.3')
            with zipfile.ZipFile(package) as archive:
                self.assertEqual(set(archive.namelist()), {
                    executable.name, executable.with_suffix('.sha256').name, 'INSTALL.txt'})
                self.assertEqual(archive.read(executable.name), executable.read_bytes())
                self.assertIn('Extract all files', archive.read('INSTALL.txt').decode())
            self.assertEqual(checksum.read_text().strip(), f'{sha256(package)}  {package.name}')
            original = package.read_bytes()
            with self.assertRaises(FileExistsError):
                package_installer(folder, '1.2.3')
            self.assertEqual(package.read_bytes(), original)

    def test_changed_installer_is_rejected_before_packaging(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            executable = self.fixture(folder)
            executable.write_bytes(b'changed after verification')
            with self.assertRaisesRegex(ValueError, 'SHA-256'):
                package_installer(folder, '1.2.3')
            self.assertFalse(executable.with_suffix('.zip').exists())


if __name__ == '__main__':
    unittest.main()
