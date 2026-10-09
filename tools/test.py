import argparse,shutil,subprocess,sys
from project import BASE,LOCAL,OUTPUT,valheim_dir
from build import build
from build_support import compile_cs,test_refs,runtime_config

def main():
    suites=['model','handling','audio','radio','nitro','impact','showroom','driving','turret','targeting','terrain','hummer','menu','cabin']
    parser=argparse.ArgumentParser();parser.add_argument('--skip-build',action='store_true');parser.add_argument('--handling-only',action='store_true')
    parser.add_argument('--suite',action='append',choices=suites,help='Run only these C# suites; repeat to combine.')
    args=parser.parse_args()
    if not args.handling_only and not args.skip_build:build()
    if not args.handling_only and not args.suite:
        subprocess.run([sys.executable,'-m','unittest','discover','-s',str(BASE/'tests'),'-p','test_*.py'],check=True)
    references,tfm,version=test_refs()
    for suite in (['handling'] if args.handling_only else args.suite or suites):
        out=OUTPUT/'tests'/suite;out.mkdir(parents=True,exist_ok=True)
        sources=list((BASE/'tests'/suite).glob('*.cs'));refs=list(references);arguments=[]
        if suite=='handling':sources.extend(BASE/'src'/name for name in ('DrivingMath.cs','VehicleHandling.cs','VehicleDriftTuning.cs','HummerTuning.cs','VehicleRideHeight.cs'))
        elif suite=='impact':sources.extend(BASE/'src'/name for name in ('VehicleImpactRules.cs','VehicleImpact.cs'))
        elif suite=='terrain':sources.extend(BASE/'src'/name for name in ('TurretTerrain.cs','TurretState.cs'))
        elif suite=='targeting':sources.extend(BASE/'src'/name for name in ('TurretTargeting.cs','TurretState.cs'))
        else:
            plugin=out/'Rachel350Z.dll';shutil.copy2(OUTPUT/'mod/Rachel350Z.dll',plugin);refs.append(plugin)
        if suite=='model':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'model/body.json')]
        if suite=='audio':arguments=[str(out/'preview-state.csv'),str(OUTPUT/'mod/audio/effects')]
        if suite=='radio':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
        if suite=='menu':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'menu')]
        if suite=='driving':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'hud/hud.json')]
        if suite=='cabin':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'interior/interior.json')]
        if suite=='hummer':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'hummer/h2.json'),str(LOCAL/'audio-hummer')]
        if suite=='turret':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'turret')]
        dll=out/(suite+'Tests.dll');compile_cs(dll,sources,refs);runtime_config(dll,tfm,version)
        subprocess.run(['dotnet',str(dll),*arguments],check=True)
    print('Requested checks passed. These tests do not execute Unity physics or playback.')

if __name__=='__main__':main()
