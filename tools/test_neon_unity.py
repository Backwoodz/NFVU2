"""Silent native neon regression: menu-only fixture, no saved world or mod replacement."""
from datetime import datetime
import shutil
import subprocess

from build_support import compile_cs, standard_refs
from install import checksum, require_closed
from project import BASE, OUTPUT, valheim_dir
from test_drift_unity import no_links


def main():
    require_closed()
    game = no_links(valheim_dir())
    out = no_links(OUTPUT / 'tests/neonunity' / datetime.now().strftime('%Y%m%d-%H%M%S-%f'))
    out.mkdir(parents=True)
    managed = game / 'valheim_Data/Managed'
    refs = standard_refs() + [managed / (n + '.dll') for n in (
        'UnityEngine', 'UnityEngine.CoreModule', 'UnityEngine.PhysicsModule',
        'UnityEngine.ImageConversionModule', 'UnityEngine.AudioModule',
        'assembly_valheim', 'assembly_utils', 'assembly_postprocessing')]
    refs += [game / 'BepInEx/core/BepInEx.dll',
             ('InstalledPlugin', game / 'BepInEx/plugins/Rachel350Z/Rachel350Z.dll')]
    bridge = out / 'ModelBridge.cs'
    bridge.write_text('extern alias InstalledPlugin;\n'
                      'global using ModelData = InstalledPlugin::Rachel350Z.ModelData;\n'
                      'global using ModelFile = InstalledPlugin::Rachel350Z.ModelFile;\n'
                      'global using ModelTexture = InstalledPlugin::Rachel350Z.ModelTexture;\n')
    # Pin the pre-fix source blob so the comparison survives future commits.
    # No old binary is installed and the regular plugin is never overwritten.
    baseline = subprocess.check_output(
        ['git', 'show', 'b0ec61a665699279cee71cfd7ff7212be793a7fe'], cwd=BASE, text=True)
    baseline = baseline.replace('VehicleNeon', 'BaselineNeon')
    old = out / 'BaselineNeon.cs'
    old.write_text(baseline, encoding='utf-8')
    source = out / 'NFVU2.NeonProbe.dll'
    compile_cs(source, [BASE / 'tests/neonunity/Probe.cs', bridge, old,
                       BASE / 'src/VehicleNeon.cs', BASE / 'src/NeonGlowMask.cs',
                       BASE / 'src/VehicleMaterials.cs'], refs, library=True)
    installed = no_links(game / 'BepInEx/plugins/NFVU2.NeonProbe.dll')
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
            str(game / 'valheim.exe'), '-force-d3d11', '-noaudio', '-nfvu2-neon-probe',
            '-nfvu2-output=' + str(out), '-nfvu2-model=' + str(OUTPUT / 'mod/body.json'),
            '-logFile', str(out / 'unity.log')], cwd=game, startupinfo=startup)
        process.wait(timeout=180)
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
    print('Native neon report:', out)
    if (out / 'error.txt').exists():
        raise RuntimeError((out / 'error.txt').read_text())
    if process.returncode or not (out / 'completed.txt').exists():
        raise RuntimeError('Native neon check did not finish; inspect its logs.')
    print((out / 'completed.txt').read_text())
    print((out / 'results.txt').read_text())


if __name__ == '__main__':
    main()
