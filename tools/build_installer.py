"""Build a local, offline Windows wizard; end users need neither Python nor an SDK."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import zipfile

from build_support import dotnet_toolchain
from mod_files import payload_files
from project import BASE, LOCAL, OUTPUT

LOADER_VERSION = '5.4.2351'
LOADER_URL = f'https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/{LOADER_VERSION}/'
LOADER_SHA256 = 'bce631497976a93977ceb08e166712e6c31d15244956f89f17df092a9b62e29f'
MOD_ROOT = 'BepInEx/plugins/Rachel350Z/'
SETUP_REVISION = 5


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def version():
    return re.search(r'<Version>([^<]+)</Version>', (BASE/'src/NFVU2.csproj').read_text()).group(1)


def framework():
    root = Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319'
    if not (root/'System.Windows.Forms.dll').is_file():
        raise SystemExit('Windows with .NET Framework 4.8 is required to build the wizard.')
    return root


def compile_installer(output, sources, payload=None, windowed=False):
    _, compiler = dotnet_toolchain()
    root = framework()
    refs = ['mscorlib', 'System', 'System.Core', 'System.Web.Extensions',
            'System.IO.Compression', 'System.Windows.Forms', 'System.Drawing']
    output.parent.mkdir(parents=True, exist_ok=True)
    args = ['-nologo', '-nostdlib+', '-deterministic+', '-langversion:latest',
            '-platform:x64', '-target:' + ('winexe' if windowed else 'exe'), f'-out:"{output}"']
    args += [f'-reference:"{root/(n+".dll")}"' for n in refs]
    if windowed:
        args.append(f'-win32manifest:"{BASE/"installer/app.manifest"}"')
        args.append(f'-resource:"{BASE/"installer/THIRD_PARTY_NOTICES.txt"}",NFVU2.Notices')
        args.append(f'-resource:"{BASE/"docs/images/nfvu2-logo.png"}",NFVU2.Logo')
    if payload:
        args.append(f'-resource:"{payload}",NFVU2.Payload')
    args += [f'"{p}"' for p in sources]
    rsp = output.with_suffix('.rsp')
    rsp.write_text('\n'.join(args), encoding='utf-8')
    subprocess.run(['dotnet', str(compiler), '@'+str(rsp)], check=True)


def loader_archive():
    archive = LOCAL/'installer'/f'BepInExPack_Valheim-{LOADER_VERSION}.zip'
    archive.parent.mkdir(parents=True, exist_ok=True)
    if not archive.exists():
        temporary = archive.with_suffix('.download')
        subprocess.run(['curl.exe', '--fail', '--location', '--retry', '2',
                        '--user-agent', 'Mozilla/5.0', LOADER_URL, '--output', str(temporary)], check=True)
        if sha256(temporary) != LOADER_SHA256:
            raise ValueError('BepInEx download checksum mismatch; archive was not accepted.')
        temporary.replace(archive)
    if sha256(archive) != LOADER_SHA256:
        raise ValueError('BepInEx archive checksum mismatch.')
    return archive


def make_payload(target, source, loader):
    hashes = {}
    names = list(payload_files(source))
    if not any(n.parts[0] == 'radio' and n.suffix.lower() in ('.wav', '.ogg') for n in names):
        raise ValueError('Radio music is required for the player installer. Import music and build the mod first.')
    with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as output:
        for name in names:
            original = source/name
            for path in [original, *original.parents]:
                if path.is_symlink() or getattr(path.stat(), 'st_file_attributes', 0) & 0x400:
                    raise ValueError('Linked payload path: '+str(name))
            destination = MOD_ROOT+name.as_posix()
            hashes[destination] = sha256(original)
            output.write(original, destination)
        with zipfile.ZipFile(loader) as archive:
            prefix = 'BepInExPack_Valheim/'
            for name in archive.namelist():
                n = name.removeprefix(prefix)
                if not name.startswith(prefix) or name.endswith('/'):
                    continue
                if n not in ('winhttp.dll', 'doorstop_config.ini', '.doorstop_version', 'BepInEx/config/BepInEx.cfg') and not re.fullmatch(r'BepInEx/core/[\w.-]+\.(dll|xml)', n):
                    continue
                data = archive.read(name)
                output.writestr(n, data)
                hashes[n] = hashlib.sha256(data).hexdigest()
        output.writestr('payload.json', json.dumps(hashes, indent=2))
    with zipfile.ZipFile(target) as archive:
        if archive.testzip() is not None:
            raise ValueError('Payload ZIP verification failed.')
    return hashes


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.parse_args()
    target = OUTPUT/'installer'
    target.mkdir(parents=True, exist_ok=True)
    payload = target/'payload.zip'
    make_payload(payload, OUTPUT/'mod', loader_archive())
    executable = target/f'NFVU2-Setup-{version()}-r{SETUP_REVISION}.exe'
    compile_installer(executable, sorted((BASE/'installer').glob('*.cs')), payload, windowed=True)
    digest = sha256(executable)
    executable.with_suffix('.sha256').write_text(f'{digest}  {executable.name}\n', encoding='utf-8')
    print('Local installer:', executable)
    print('SHA-256:', digest)


if __name__ == '__main__':
    main()
