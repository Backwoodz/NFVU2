import argparse,shutil,subprocess,sys
from project import BASE,LOCAL,OUTPUT,valheim_dir
from build import build
from build_support import compile_cs,test_refs,runtime_config

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--skip-build',action='store_true');parser.add_argument('--handling-only',action='store_true');args=parser.parse_args()
    if not args.handling_only and not args.skip_build:build()
    if not args.handling_only:
        subprocess.run([sys.executable,'-m','unittest','discover','-s',str(BASE/'tests'),'-p','test_*.py'],check=True)
    references,tfm,version=test_refs()
    for suite in (['handling'] if args.handling_only else ['model','handling','audio','radio','nitro']):
        out=OUTPUT/'tests'/suite;out.mkdir(parents=True,exist_ok=True)
        sources=list((BASE/'tests'/suite).glob('*.cs'));refs=list(references);arguments=[]
        if suite=='handling':sources.extend(BASE/'src'/name for name in ('DrivingMath.cs','VehicleHandling.cs'))
        else:
            plugin=out/'Rachel350Z.dll';shutil.copy2(OUTPUT/'mod/Rachel350Z.dll',plugin);refs.append(plugin)
        if suite=='model':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'model/body.json')]
        if suite=='audio':arguments=[str(out/'preview-state.csv'),str(OUTPUT/'mod/audio/effects')]
        if suite=='radio':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
        dll=out/(suite+'Tests.dll');compile_cs(dll,sources,refs);runtime_config(dll,tfm,version)
        subprocess.run(['dotnet',str(dll),*arguments],check=True)
    print('Requested checks passed. These tests do not execute Unity physics or playback.')

if __name__=='__main__':main()
