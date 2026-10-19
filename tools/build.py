import shutil
from project import BASE,LOCAL,OUTPUT,valheim_dir
from build_support import standard_refs,compile_cs

def build():
    game=valheim_dir();managed=game/'valheim_Data/Managed'
    refs=standard_refs()
    for name in ('UnityEngine','UnityEngine.CoreModule','Unity.InputSystem','Unity.TextMeshPro','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.AnimationModule','UnityEngine.PhysicsModule','UnityEngine.VehiclesModule','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.AudioModule','UnityEngine.ParticleSystemModule','UnityEngine.UnityWebRequestModule','UnityEngine.UnityWebRequestAudioModule','UnityEngine.TextRenderingModule','Newtonsoft.Json'):
        refs.append(managed/(name+'.dll'))
    refs.append(game/'BepInEx/core/BepInEx.dll')
    refs.append(game/'BepInEx/core/0Harmony.dll')
    refs.append(managed/'assembly_valheim.dll')
    refs.append(managed/'gui_framework.dll')
    refs.append(managed/'assembly_utils.dll')
    refs.append(managed/'Splatform.dll')
    missing=[str(p) for p in refs if not p.exists()]
    if missing:raise SystemExit('Missing installed game/mod-loader references:\n'+'\n'.join(missing))
    audio=sorted((LOCAL/'audio-assets').glob('*.wav'))
    if len(audio)!=19:raise SystemExit('Run the audio extraction, decoding and preparation scripts first (19 WAV resources required).')
    model=LOCAL/'model/body.json'
    if not model.exists():raise SystemExit('Run tools/export_geometry.py first to generate body.json.')
    output=OUTPUT/'mod/Rachel350Z.dll'
    physics=output.parent/'NFVU2.DriftPhysics.dll'
    physics_sources=sorted((BASE/'physics').glob('*.cs'))
    if not physics_sources:raise SystemExit('Missing own drift physics sources in physics/.')
    # The reusable physics assembly has no Unity, BepInEx or game references.
    compile_cs(physics,physics_sources,standard_refs(),library=True)
    refs.append(physics)
    compile_cs(output,sorted((BASE/'src').glob('*.cs')),refs,audio,library=True)
    shutil.copy2(model,output.parent/'body.json')
    # Long stereo songs and optional original turbo clips stay external.
    from mod_files import sync_optional_assets
    sync_optional_assets(LOCAL,output.parent)
    print('Built:',output)
    return output

if __name__=='__main__':build()
