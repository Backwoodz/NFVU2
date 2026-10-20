"""Capture an unresponsive Valheim process without changing game or GPU settings.

Windows only. Run with Python; diagnostics stay in ignored .local/. The watcher
does not terminate the game, install a debugger, or change system dump settings.
"""
import argparse
import ctypes as c
from ctypes import wintypes as w
from datetime import datetime
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

BASE = Path(__file__).resolve().parents[1]
ROOT = BASE / '.local' / 'diagnostics'
STATUS = ROOT / 'hang-watch-status.json'
kernel = c.WinDLL('kernel32', use_last_error=True)
user = c.WinDLL('user32', use_last_error=True)
dbghelp = c.WinDLL('dbghelp', use_last_error=True)
kernel.OpenProcess.argtypes = [w.DWORD, w.BOOL, w.DWORD]
kernel.OpenProcess.restype = w.HANDLE
kernel.CloseHandle.argtypes = [w.HANDLE]
kernel.CreateFileW.argtypes = [w.LPCWSTR, w.DWORD, w.DWORD, c.c_void_p, w.DWORD, w.DWORD, w.HANDLE]
kernel.CreateFileW.restype = w.HANDLE
dbghelp.MiniDumpWriteDump.argtypes = [w.HANDLE, w.DWORD, w.HANDLE, w.DWORD, c.c_void_p, c.c_void_p, c.c_void_p]
dbghelp.MiniDumpWriteDump.restype = w.BOOL
CALLBACK = c.WINFUNCTYPE(w.BOOL, w.HWND, w.LPARAM)
user.EnumWindows.argtypes = [CALLBACK, w.LPARAM]
user.GetWindowThreadProcessId.argtypes = [w.HWND, c.POINTER(w.DWORD)]
user.IsWindowVisible.argtypes = [w.HWND]
user.GetClassNameW.argtypes = [w.HWND, w.LPWSTR, c.c_int]
user.SendMessageTimeoutW.argtypes = [w.HWND, w.UINT, w.WPARAM, w.LPARAM, w.UINT, w.UINT, c.POINTER(c.c_size_t)]
user.SendMessageTimeoutW.restype = w.LPARAM


def stamp():
    return datetime.now().astimezone().isoformat(timespec='seconds')


def status(state, **fields):
    ROOT.mkdir(parents=True, exist_ok=True)
    data = dict(state=state, updated=stamp(), watcher_pid=os.getpid(), **fields)
    temp = STATUS.with_suffix('.tmp')
    temp.write_text(json.dumps(data, indent=2), encoding='utf-8')
    temp.replace(STATUS)


def game_pids():
    # tasklist does not require debug privileges; never match by window title.
    import csv
    result = subprocess.run(['tasklist', '/FI', 'IMAGENAME eq valheim.exe', '/FO', 'CSV', '/NH'],
                            capture_output=True, text=True, errors='replace', creationflags=subprocess.CREATE_NO_WINDOW,
                            timeout=10, check=True)
    return [int(row[1]) for row in csv.reader(result.stdout.splitlines())
            if len(row) > 1 and row[0].lower() == 'valheim.exe']


def windows(pid):
    found = []
    @CALLBACK
    def visit(hwnd, _):
        owner = w.DWORD()
        user.GetWindowThreadProcessId(hwnd, c.byref(owner))
        if owner.value == pid and user.IsWindowVisible(hwnd):
            name = c.create_unicode_buffer(256)
            user.GetClassNameW(hwnd, name, len(name))
            # BepInEx's separate console can remain responsive during a game
            # hang. Only the Unity game window determines responsiveness.
            if name.value == 'UnityWndClass':
                found.append(hwnd)
        return True
    user.EnumWindows(visit, 0)
    return found


def responds(hwnd):
    result = c.c_size_t()
    # WM_NULL, SMTO_BLOCK | SMTO_ABORTIFHUNG; no input or game command sent.
    return bool(user.SendMessageTimeoutW(hwnd, 0, 0, 0, 3, 1000, c.byref(result)))


def write_dump(pid, target):
    process = kernel.OpenProcess(0x0400 | 0x0010, False, pid)  # QUERY_INFORMATION | VM_READ
    if not process:
        raise c.WinError(c.get_last_error())
    file = None
    try:
        file = kernel.CreateFileW(str(target), 0x40000000, 0, None, 1, 0x80, None)
        if file == c.c_void_p(-1).value:
            file = None
            raise c.WinError(c.get_last_error())
        # Stacks, thread information, indirect referenced memory and module
        # metadata. No full-memory multi-gigabyte dump or process suspension.
        flags = 0x20 | 0x40 | 0x100 | 0x800 | 0x1000
        if not dbghelp.MiniDumpWriteDump(process, pid, file, flags, None, None, None):
            raise c.WinError(c.get_last_error())
    finally:
        if file:
            kernel.CloseHandle(file)
        kernel.CloseHandle(process)
    with target.open('rb') as stream:
        if stream.read(4) != b'MDMP':
            raise RuntimeError('Dump header verification failed')
    return target.stat().st_size


def copy_logs(folder):
    config = json.loads((BASE / 'config.local.json').read_text(encoding='utf-8-sig'))
    sources = {
        'BepInEx.log': Path(config['valheim_dir']) / 'BepInEx' / 'LogOutput.log',
        'Player.log': Path(os.environ['USERPROFILE']) / 'AppData/LocalLow/IronGate/Valheim/Player.log',
    }
    errors = {}
    for name, source in sources.items():
        try:
            shutil.copyfile(source, folder / name)
        except OSError as error:
            errors[name] = str(error)
    return errors


def capture(pid):
    folder = ROOT / (datetime.now().strftime('hang-%Y%m%d-%H%M%S-') + str(pid))
    folder.mkdir(parents=True)
    record = dict(pid=pid, detected=stamp(), log_errors=copy_logs(folder), dumps=[])
    status('capturing', game_pid=pid, folder=str(folder))
    try:
        for index in range(2):
            path = folder / f'threads-{index + 1}.dmp'
            record['dumps'].append(dict(file=path.name, bytes=write_dump(pid, path), time=stamp()))
            if index == 0:
                time.sleep(5)
    except Exception as error:
        record['capture_error'] = repr(error)
    record['final_log_errors'] = copy_logs(folder)
    script = r'''
$start = (Get-Date).AddMinutes(-20)
$events = foreach ($name in @('Application','System')) {
    Get-WinEvent -FilterHashtable @{LogName=$name; StartTime=$start} -ErrorAction SilentlyContinue |
        Where-Object {$_.ProviderName -match 'Application Hang|Application Error|Windows Error Reporting|Display|nvlddmkm|WHEA'} |
        Select-Object TimeCreated,Id,ProviderName,Message
}
ConvertTo-Json -InputObject @($events) -Depth 5
'''
    try:
        events = subprocess.run(['powershell', '-NoProfile', '-Command', script],
                                capture_output=True, text=True, errors='replace', timeout=25,
                                creationflags=subprocess.CREATE_NO_WINDOW)
        (folder / 'windows-events.json').write_text(events.stdout, encoding='utf-8')
        if events.returncode:
            record['event_error'] = events.stderr
    except Exception as error:
        record['event_error'] = repr(error)
    (folder / 'capture.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    status('captured' if record['dumps'] else 'capture_failed', game_pid=pid, folder=str(folder),
           dumps=len(record['dumps']), error=record.get('capture_error'))


def watch(hours):
    # One watcher owns this lock; Windows releases it even if Python exits.
    import msvcrt
    ROOT.mkdir(parents=True, exist_ok=True)
    with (ROOT / 'hang-watch.lock').open('a+b') as lock:
        lock.seek(0)
        lock.write(b'0')
        lock.flush()
        lock.seek(0)
        msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        deadline = time.monotonic() + hours * 3600
        current, since, hung_since = None, 0, None
        status('waiting')
        while time.monotonic() < deadline:
            pids = game_pids()
            if current not in pids:
                current = pids[0] if pids else None
                since, hung_since = time.monotonic(), None
            if current:
                handles = windows(current)
                responsive = any(responds(handle) for handle in handles)
                if handles and not responsive and time.monotonic() - since > 60:
                    if hung_since is None:
                        hung_since = time.monotonic()
                    if time.monotonic() - hung_since >= 10:
                        capture(current)
                        return
                else:
                    hung_since = None
                status('suspected_hang' if hung_since else 'watching', game_pid=current,
                       windows=len(handles), responsive=responsive,
                       grace_remaining=max(0, round(60 - (time.monotonic() - since))))
            else:
                status('waiting')
            time.sleep(2)
        status('expired')


def self_test():
    # Dump an expendable child, never the user's game, to verify permissions,
    # pointer widths, file handles and the dump header before arming the watcher.
    child = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(60)'],
                             creationflags=subprocess.CREATE_NO_WINDOW)
    try:
        time.sleep(.5)
        folder = ROOT / datetime.now().strftime('hang-capture-selftest-%Y%m%d-%H%M%S')
        folder.mkdir(parents=True)
        path = folder / 'child.dmp'
        print(json.dumps(dict(test='passed', bytes=write_dump(child.pid, path), file=str(path))))
    finally:
        child.terminate()
        child.wait(timeout=10)
    # An invisible disposable Win32 window first pumps messages, then stops.
    # Verify detection itself, without freezing or interacting with Valheim.
    program = r'''
import ctypes as c, time
from ctypes import wintypes as w
u = c.WinDLL('user32')
u.CreateWindowExW.argtypes = [w.DWORD,w.LPCWSTR,w.LPCWSTR,w.DWORD,c.c_int,c.c_int,c.c_int,c.c_int,w.HWND,w.HMENU,w.HINSTANCE,c.c_void_p]
u.CreateWindowExW.restype = w.HWND
h = u.CreateWindowExW(0,'STATIC','NFVU2 diagnostic test',0,0,0,1,1,None,None,None,None)
assert h
print(h,flush=True)
end = time.monotonic()+2
msg = w.MSG()
while time.monotonic()<end:
    while u.PeekMessageW(c.byref(msg),None,0,0,1):
        u.TranslateMessage(c.byref(msg))
        u.DispatchMessageW(c.byref(msg))
    time.sleep(.005)
time.sleep(60)
'''
    child = subprocess.Popen([sys.executable, '-c', program], stdout=subprocess.PIPE,
                             text=True, creationflags=subprocess.CREATE_NO_WINDOW)
    try:
        hwnd = int(child.stdout.readline())
        assert responds(hwnd), 'Responsive test window was reported hung'
        time.sleep(2.1)
        assert not responds(hwnd), 'Hung test window was reported responsive'
        print('Window responsiveness detection passed (responsive -> hung).')
    finally:
        child.terminate()
        child.wait(timeout=10)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--hours', type=float, default=4)
    parser.add_argument('--self-test', action='store_true')
    args = parser.parse_args()
    if args.self_test:
        self_test()
    else:
        try:
            watch(args.hours)
        except Exception as error:
            # Duplicate watchers must not overwrite the active watcher's status.
            if isinstance(error, OSError) and error.errno == 13:
                raise
            status('error', error=repr(error))
            raise
