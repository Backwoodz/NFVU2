"""Explicit, silent native tyre-smoke rendering check. Never loads a saved world.

Requires Valheim closed and the normal NFVU2 payload installed. Compiles current
smoke sources into a temporary diagnostic plugin; the installed mod is untouched.
"""
from datetime import datetime
import argparse
import shutil
import subprocess

from build_support import compile_cs, standard_refs
from install import checksum, require_closed
from project import BASE, OUTPUT, valheim_dir
from test_drift_unity import no_links


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--wheel-occlusion-only', action='store_true', help='Render the stock-wheel contrast comparison only.')
    args = parser.parse_args()
    require_closed()
    game = no_links(valheim_dir())
    out = no_links(OUTPUT / 'tests/tyresmokeunity' / datetime.now().strftime('%Y%m%d-%H%M%S-%f'))
    out.mkdir(parents=True)
    (out / 'baseline-sha256.txt').write_text(checksum(game / 'BepInEx/plugins/Rachel350Z/Rachel350Z.dll'))
    managed = game / 'valheim_Data/Managed'
    refs = standard_refs() + [managed / (name + '.dll') for name in (
        'UnityEngine', 'UnityEngine.CoreModule', 'UnityEngine.PhysicsModule',
        'UnityEngine.ParticleSystemModule', 'UnityEngine.VehiclesModule',
        'UnityEngine.ImageConversionModule', 'UnityEngine.AudioModule', 'assembly_valheim', 'assembly_utils')]
    refs += [game / 'BepInEx/core/BepInEx.dll',
             OUTPUT / 'mod/NFVU2.DriftPhysics.dll',
             ('InstalledPlugin', game / 'BepInEx/plugins/Rachel350Z/Rachel350Z.dll')]
    # Reuse only model/material helpers from the installed assembly. Alias it so
    # the test executes current smoke sources without duplicate-type ambiguity.
    bridge = out / 'ModelBridge.cs'
    bridge.write_text('extern alias InstalledPlugin;\n'
                      'global using ModelData = InstalledPlugin::Rachel350Z.ModelData;\n'
                      'global using ModelFile = InstalledPlugin::Rachel350Z.ModelFile;\n'
                      'global using CabinAppearance = InstalledPlugin::Rachel350Z.CabinAppearance;\n'
                      'global using VehicleMaterials = InstalledPlugin::Rachel350Z.VehicleMaterials;\n'
                      'global using BaselineSmoke = InstalledPlugin::Rachel350Z.VehicleBurnoutEffects;\n'
                      'global using BaselineContact = InstalledPlugin::Rachel350Z.TyreSmokeContact;\n')
    source = out / 'NFVU2.SmokeProbe.dll'
    compile_cs(source, sorted((BASE / 'tests/tyresmokeunity').glob('*.cs')) + [bridge,
                       BASE / 'src/VehicleBurnoutEffects.cs', BASE / 'src/TyreSmokeModel.cs',
                       BASE / 'src/DriftVehicleAdapter.cs',
                       BASE / 'src/VehicleInterior.cs', BASE / 'src/VehicleMesh.cs'], refs, library=True)
    installed = no_links(game / 'BepInEx/plugins/NFVU2.SmokeProbe.dll')
    if installed.exists():
        raise RuntimeError('Refusing to replace an existing diagnostic plugin.')
    process = None
    try:
        require_closed()
        shutil.copy2(source, installed)
        if checksum(source) != checksum(installed):
            raise RuntimeError('Diagnostic plugin checksum mismatch.')
        require_closed()
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
        process = subprocess.Popen([
            str(game / 'valheim.exe'), '-force-d3d11', '-noaudio', '-nfvu2-smoke-probe',
            '-nfvu2-output=' + str(out), '-nfvu2-model=' + str(OUTPUT / 'mod/body.json'),
            '-logFile', str(out / 'unity.log')] + (['-nfvu2-wheel-occlusion-only'] if args.wheel_occlusion_only else []), cwd=game, startupinfo=startup)
        process.wait(timeout=360)
    finally:
        if process is not None and process.poll() is None:
            process.terminate()
            process.wait(timeout=15)
        require_closed()
        no_links(installed)
        if installed.exists():
            if checksum(installed) != checksum(source):
                raise RuntimeError('Diagnostic plugin changed; refusing cleanup.')
            installed.unlink()
    print('Native smoke report:', out)
    if (out / 'error.txt').exists():
        raise RuntimeError((out / 'error.txt').read_text())
    if process.returncode or not (out / 'completed.txt').exists():
        raise RuntimeError('Native smoke check did not finish; inspect its logs.')
    print((out / 'completed.txt').read_text())
    print((out / 'results.txt').read_text())


if __name__ == '__main__':
    main()
