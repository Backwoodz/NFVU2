from pathlib import Path
import json,re,subprocess
from project import LOCAL

def version_key(text):return tuple(int(x) for x in re.findall(r'\d+',text)[:3])

def dotnet_toolchain():
    sdks=[]
    for line in subprocess.check_output(['dotnet','--list-sdks'],text=True).splitlines():
        m=re.fullmatch(r'(\S+) \[(.+)\]',line)
        if m and '-' not in m[1]:sdks.append((m[1],Path(m[2])))
    if not sdks:raise SystemExit('A .NET SDK (7 or newer) is required.')
    version,root=max(sdks,key=lambda x:version_key(x[0]))
    if version_key(version)[0]<7:raise SystemExit('A .NET SDK (7 or newer) is required.')
    csc=root/version/'Roslyn/bincore/csc.dll'
    return root.parent,csc

def compile_cs(output,sources,refs,resources=(),library=False):
    _,compiler=dotnet_toolchain()
    output.parent.mkdir(parents=True,exist_ok=True)
    args=['-nostdlib+','-deterministic+','-langversion:latest','-target:'+('library' if library else 'exe'),f'-out:"{output}"']
    for reference in refs:
        if isinstance(reference,tuple):
            alias,path=reference
            args.append(f'-reference:{alias}="{path}"')
        else:args.append(f'-reference:"{reference}"')
    args += [f'-resource:"{p}",Rachel350Z.Audio.{p.name}' for p in resources]
    args += [f'"{p}"' for p in sources]
    response=output.with_suffix('.rsp');response.write_text('\n'.join(args),encoding='utf-8')
    subprocess.run(['dotnet',str(compiler),'@'+str(response)],check=True)

def standard_refs():
    root,_=dotnet_toolchain()
    packs=root/'packs/NETStandard.Library.Ref'
    versions=[p for p in packs.iterdir() if (p/'ref/netstandard2.1').is_dir()]
    if not versions:raise SystemExit('Missing NETStandard.Library.Ref 2.1 reference pack.')
    return sorted((max(versions,key=lambda p:version_key(p.name))/'ref/netstandard2.1').glob('*.dll'))

def test_refs():
    root,_=dotnet_toolchain()
    packs=root/'packs/Microsoft.NETCore.App.Ref'
    installed=subprocess.check_output(['dotnet','--list-runtimes'],text=True)
    versions=[p for p in packs.iterdir() if version_key(p.name)[0]>=7 and f'Microsoft.NETCore.App {p.name} [' in installed]
    if not versions:raise SystemExit('Matching .NET runtime and reference pack (7 or newer) required for tests.')
    pack=max(versions,key=lambda p:version_key(p.name));tfm='net'+str(version_key(pack.name)[0])+'.0'
    return sorted((pack/'ref'/tfm).glob('*.dll')),tfm,pack.name

def runtime_config(output,tfm,version):
    output.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':tfm,'framework':{'name':'Microsoft.NETCore.App','version':version}}}),encoding='utf-8')
