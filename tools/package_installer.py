"""Wrap the verified Windows setup in a ZIP for GitHub releases."""
import hashlib
from pathlib import Path
import zipfile

from build_installer import version
from project import OUTPUT


def stream_sha256(stream):
    digest = hashlib.sha256()
    for chunk in iter(lambda: stream.read(1024 * 1024), b''):
        digest.update(chunk)
    return digest.hexdigest()


def sha256(path):
    with Path(path).open('rb') as stream:
        return stream_sha256(stream)


def package_installer(folder, release_version):
    folder = Path(folder)
    executable = folder/f'NFVU2-Setup-{release_version}.exe'
    checksum = executable.with_suffix('.sha256')
    target = executable.with_suffix('.zip')
    sidecar = target.with_suffix('.zip.sha256')
    if target.exists() or sidecar.exists():
        raise FileExistsError('Release package already exists: '+str(target))
    digest = sha256(executable)
    if checksum.read_text(encoding='utf-8').strip() != f'{digest}  {executable.name}':
        raise ValueError('Installer SHA-256 sidecar does not match the EXE.')
    instructions = (
        f'NFVU2 {release_version} - Windows 10/11 x64\n\n'
        '1. Extract all files from this ZIP to a folder.\n'
        '2. Close Valheim.\n'
        f'3. Run {executable.name} and select valheim.exe.\n'
        '4. Choose Install / Update. BepInEx and all 27 radio tracks are included.\n'
        '5. Launch Valheim, enter a single-player world and press F9 / L3.\n\n'
        'This ZIP contains the Windows setup EXE; it is not a manual-install mod ZIP.\n'
        'ZIP packaging does not change the installer or its security properties.\n'
        'Use Check for updates in this installer to download and verify ZIP updates\n'
        'automatically. Older installers use the compatibility EXE download.\n'
        'You can also download the latest ZIP from:\n'
        'https://github.com/Backwoodz/NFVU2/releases/latest\n'
        '\n'
        'To uninstall, run this setup again and select the same game.\n'
        'The included .sha256 file identifies the extracted setup EXE.\n'
    )
    # The embedded payload is already compressed; avoid recompressing 150+ MiB.
    with zipfile.ZipFile(target, 'x', zipfile.ZIP_STORED) as archive:
        archive.write(executable, executable.name)
        archive.write(checksum, checksum.name)
        archive.writestr('INSTALL.txt', instructions)
    with zipfile.ZipFile(target) as archive:
        if archive.testzip() is not None:
            raise ValueError('Release ZIP verification failed.')
        if set(archive.namelist()) != {executable.name, checksum.name, 'INSTALL.txt'}:
            raise ValueError('Unexpected release ZIP contents.')
        with archive.open(executable.name) as stream:
            if stream_sha256(stream) != digest:
                raise ValueError('Packaged installer differs from verified EXE.')
        if archive.read(checksum.name) != checksum.read_bytes():
            raise ValueError('Packaged installer checksum differs from sidecar.')
    sidecar.write_text(f'{sha256(target)}  {target.name}\n', encoding='utf-8')
    return target, sidecar


if __name__ == '__main__':
    for path in package_installer(OUTPUT/'installer', version()):
        print('Release asset:', path)
