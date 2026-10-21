"""Explicit own drift integration check using Valheim's real Unity/PhysX runtime.

Runs only in the menu, without loading a world. Requires Valheim closed;
never part of normal tests. Compiles the own physics sources into the probe.
"""
from datetime import datetime
import argparse
import shutil
import subprocess
from pathlib import Path
import stat

from build_support import compile_cs, standard_refs
from install import require_closed, checksum
from project import BASE, OUTPUT, valheim_dir


def no_links(path):
    path = Path(path).absolute()
    for part in (path, *path.parents):
        try:
            info = part.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('Diagnostic paths must not traverse links/reparse points')
    return path.resolve()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--installed-library', action='store_true',
                        help='Check the installed separate physics DLL instead of embedding its sources.')
    args = parser.parse_args()
    require_closed()
    game = no_links(valheim_dir())
    out = no_links(OUTPUT / 'tests/driftunity' / datetime.now().strftime('%Y%m%d-%H%M%S-%f'))
    out.mkdir(parents=True)
    managed = game / 'valheim_Data/Managed'
    refs = standard_refs() + [managed / (n + '.dll') for n in (
        'UnityEngine', 'UnityEngine.CoreModule', 'UnityEngine.PhysicsModule',
        'UnityEngine.VehiclesModule', 'assembly_valheim', 'assembly_utils', 'gui_framework')]
    refs.append(game / 'BepInEx/core/BepInEx.dll')
    physics = sorted((BASE / 'physics').glob('*.cs'))
    if args.installed_library:
        library = no_links(game / 'BepInEx/plugins/Rachel350Z/NFVU2.DriftPhysics.dll')
        if checksum(library) != checksum(OUTPUT / 'mod/NFVU2.DriftPhysics.dll'):
            raise RuntimeError('Installed physics DLL differs from the current build.')
        refs.append(library)
        physics = []
    source = out / 'NFVU2.DriftProbe.dll'
    compile_cs(source, [BASE / 'tests/driftunity/Probe.cs'] + physics +
               [BASE / 'src' / (n + '.cs') for n in (
                   'DriftVehicleAdapter', 'VehicleHandling', 'VehicleDriftTuning', 'DrivingMath', 'ControlRules')], refs, library=True)
    require_closed()
    installed = no_links(game / 'BepInEx/plugins/NFVU2.DriftProbe.dll')
    if installed.exists():
        raise SystemExit('Refusing to replace an existing private diagnostic plugin.')
    try:
        shutil.copy2(source, installed)
        if checksum(source) != checksum(installed):
            raise RuntimeError('Diagnostic plugin checksum mismatch.')
        require_closed()
        process = subprocess.Popen([
            str(game / 'valheim.exe'), '-batchmode', '-nographics', '-noaudio',
            '-nfvu2-drift-probe', '-nfvu2-probe-output=' + str(out),
            *(['-nfvu2-installed-physics'] if args.installed_library else []),
            '-logFile', str(out / 'unity.log')], cwd=game,
            creationflags=subprocess.CREATE_NO_WINDOW)
        try:
            process.wait(timeout=180)
        except subprocess.TimeoutExpired:
            process.terminate()
            process.wait(timeout=30)
            raise
        if process.returncode != 0:
            raise RuntimeError('Unity diagnostic exited with code ' + str(process.returncode))
    finally:
        require_closed()
        no_links(installed)
        if installed.exists():
            if checksum(installed) != checksum(source):
                raise RuntimeError('Diagnostic plugin changed; refusing to remove it.')
            installed.unlink()
    print('Runtime report:', out)
    if (out / 'failed.txt').exists():
        raise SystemExit((out / 'failed.txt').read_text())
    if not (out / 'completed.txt').exists() or (out / 'completed.txt').read_text() != 'PASS':
        raise SystemExit((out / 'checks.txt').read_text() if (out / 'checks.txt').exists() else 'Runtime diagnostic did not complete.')
    print((out / 'checks.txt').read_text().splitlines()[-1])
    print((out / 'summary.csv').read_text())
    print((out / 'sustained.csv').read_text())
    print((out / 'response.csv').read_text())


if __name__ == '__main__':
    main()
