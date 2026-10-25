"""Build a directly extractable, offline Nexus ZIP, separate from Windows setup."""
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile

from mod_files import payload_files
from project import BASE, OUTPUT

MOD_ROOT = 'BepInEx/plugins/Rachel350Z/'
MANIFEST = 'NFVU2-PACKAGE.json'


def checksum(path):
    digest = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def project_version():
    version = re.search(r'<Version>([^<]+)</Version>', (BASE/'src/NFVU2.csproj').read_text())[1]
    if not re.fullmatch(r'(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)', version):
        raise ValueError('Expected Major.Minor.Patch mod version')
    plugin = (BASE/'src/Plugin.cs').read_text(encoding='utf-8')
    if not re.search(r'\[BepInPlugin\("local\.rachel350z\.prototype",\s*"[^"]+",\s*"'+re.escape(version)+r'"\)\]', plugin):
        raise ValueError('Project and plugin versions must match')
    return version


def regular_path(path):
    for part in (path, *path.parents):
        if part.is_symlink() or getattr(part.stat(), 'st_file_attributes', 0) & 0x400:
            raise ValueError('Linked package path: '+str(path))


def instructions(version):
    return f'''NFVU2 {version} - Nexus manual package / Manuelles Nexus-Paket

REQUIREMENTS / VORAUSSETZUNGEN
Valheim for Windows, single-player, and BepInEx 5 installed separately.
Tested loader: BepInExPack Valheim 5.4.2351.
Loader information: https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/
This package contains the mod and its assets, not BepInEx or a setup program.
Installation does not need GitHub or any download/update program.

INSTALL
1. Close Valheim. Install the required BepInEx loader separately if needed.
2. Extract this ZIP. Copy its BepInEx folder into the Valheim folder containing
   valheim.exe. Merge the folders. The final plugin path must be:
   BepInEx/plugins/Rachel350Z/Rachel350Z.dll
   Keep NFVU2.DriftPhysics.dll, body.json and all asset subfolders beside it.
3. Start Valheim and use F9 / L3 to open the mod menu.

INSTALLATION
1. Valheim schliessen. BepInEx bei Bedarf separat installieren.
2. ZIP entpacken. Den enthaltenen BepInEx-Ordner in den Valheim-Spielordner
   neben valheim.exe kopieren und Ordner zusammenfuehren. Der Pluginpfad lautet:
   BepInEx/plugins/Rachel350Z/Rachel350Z.dll
   NFVU2.DriftPhysics.dll, body.json und alle Asset-Unterordner mitkopieren.
3. Valheim starten und mit F9 / L3 das Modmenue oeffnen.

UPDATING / AKTUALISIEREN
Close Valheim. Back up and replace only BepInEx/plugins/Rachel350Z with the
folder from this package. Keep BepInEx/config and your saves.
Valheim schliessen. Nur BepInEx/plugins/Rachel350Z sichern und durch den Ordner
aus diesem Paket ersetzen. BepInEx/config und Spielstaende behalten.

SWITCHING FROM THE EXE INSTALLER / WECHSEL VOM EXE-INSTALLER
First uninstall NFVU2 with the existing installer. Leave "Also remove bundled
BepInEx" unchecked to keep the loader. Then install this manual package.
Uninstall preserves personal settings and saves. Replacing installer-owned
files manually can otherwise invalidate the installer's recorded checksums.
Zuerst NFVU2 mit dem bisherigen Installer entfernen. BepInEx dabei behalten
("Also remove bundled BepInEx" nicht ankreuzen). Danach dieses ZIP installieren.
Einstellungen und Spielstaende bleiben erhalten. Installer und manuelle
Dateiersetzung nicht vermischen, sonst stimmen dessen Pruefsummen nicht mehr.

UNINSTALL / ENTFERNEN
Close Valheim and remove only BepInEx/plugins/Rachel350Z.
Valheim schliessen und nur BepInEx/plugins/Rachel350Z entfernen.
Keep your saves, BepInEx/config, the loader and other mods.
Spielstaende, BepInEx/config, Loader und andere Mods behalten.

NFVU2-PACKAGE.json lists the package files and their SHA-256 checksums.
Copy only the BepInEx folder into the game. No EXE or script needs to be run.
Nur den BepInEx-Ordner ins Spiel kopieren. Keine EXE und kein Skript ausfuehren.
'''


def verify_archive(archive, expected):
    with zipfile.ZipFile(archive) as zipped:
        names = zipped.namelist()
        if len(names) != len(set(n.lower() for n in names)) or set(names) != set(expected) | {MANIFEST}:
            raise ValueError('Unexpected or duplicate ZIP entries')
        if zipped.testzip() is not None:
            raise ValueError('ZIP CRC verification failed')
        manifest = json.loads(zipped.read(MANIFEST))
        if manifest['files'] != expected:
            raise ValueError('Package manifest mismatch')
        for name, digest in expected.items():
            if hashlib.sha256(zipped.read(name)).hexdigest() != digest:
                raise ValueError('Package checksum mismatch: '+name)


def make_package(source, destination, version):
    if not re.fullmatch(r'(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)', version):
        raise ValueError('Expected Major.Minor.Patch mod version')
    source, destination = Path(source), Path(destination)
    names = list(payload_files(source))
    for name in names:
        regular_path(source/name)
        if name.suffix.lower() not in {'.dll', '.json', '.bin', '.png', '.wav', '.ogg'}:
            raise ValueError('Unsupported mod asset: '+str(name))
    playlist = source/'radio/tracks.json'
    if not playlist.is_file():
        raise ValueError('Complete radio playlist is required; build the mod first')
    tracks = json.loads(playlist.read_text(encoding='utf-8-sig'))
    if not tracks or any(not (source/'radio'/track['file']).stat().st_size for track in tracks):
        raise ValueError('Radio playlist must contain complete, nonempty tracks')
    destination.mkdir(parents=True, exist_ok=False)
    regular_path(destination)
    package = destination/'package'
    package.mkdir()
    hashes = {}
    for name in names:
        target = package/MOD_ROOT/name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source/name, target)
        original = checksum(source/name)
        if checksum(target) != original:
            raise ValueError('Copy verification failed: '+str(name))
        hashes[MOD_ROOT+name.as_posix()] = original
    guide = package/'INSTALL.txt'
    guide.write_text(instructions(version), encoding='utf-8')
    hashes[guide.name] = checksum(guide)
    manifest = {'schema': 1, 'version': version, 'distribution': 'nexus-manual',
                'requires': 'BepInEx 5 (separate installation)', 'files': hashes}
    (package/MANIFEST).write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    archive = destination/f'NFVU2-Nexus-{version}.zip'
    temporary = archive.with_suffix('.zip.partial')
    with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as zipped:
        for name in sorted([*hashes, MANIFEST]):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            zipped.writestr(info, (package/name).read_bytes())
    verify_archive(temporary, hashes)
    temporary.rename(archive)
    digest = checksum(archive)
    archive.with_suffix('.zip.sha256').write_text(f'{digest}  {archive.name}\n', encoding='utf-8')
    return archive


def main():
    version = project_version()
    stamp = datetime.now(timezone.utc).strftime('%Y%m%d-%H%M%S-%f')
    archive = make_package(OUTPUT/'mod', OUTPUT/'nexus'/stamp, version)
    print('Nexus ZIP:', archive)
    print('Extracted folder:', archive.parent/'package')
    print('SHA-256:', checksum(archive))


if __name__ == '__main__':
    main()
