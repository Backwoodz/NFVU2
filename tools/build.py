import shutil
from project import BASE,LOCAL,OUTPUT,valheim_dir
from build_support import standard_refs,compile_cs

def build():
    game=valheim_dir();managed=game/'valheim_Data/Managed'
    refs=standard_refs()
    for name in ('UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule','UnityEngine.VehiclesModule','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.AudioModule','Newtonsoft.Json'):
        refs.append(managed/(name+'.dll'))
    refs.append(game/'BepInEx/core/BepInEx.dll')
    missing=[str(p) for p in refs if not p.exists()]
    if missing:raise SystemExit('Missing installed game/mod-loader references:\n'+'\n'.join(missing))
    audio=sorted((LOCAL/'audio-assets').glob('*.wav'))
    if len(audio)!=19:raise SystemExit('Run the audio extraction, decoding and preparation scripts first (19 WAV resources required).')
    model=LOCAL/'model/body.json'
    if not model.exists():raise SystemExit('Run tools/export_geometry.py first to generate body.json.')
    output=OUTPUT/'mod/Rachel350Z.dll'
    compile_cs(output,sorted((BASE/'src').glob('*.cs')),refs,audio,library=True)
    shutil.copy2(model,output.parent/'body.json')
    print('Built:',output)
    return output

if __name__=='__main__':build()
