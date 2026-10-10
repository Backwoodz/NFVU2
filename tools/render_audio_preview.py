from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
from pathlib import Path
import json,wave
import numpy as np

base=BASE;rate=32000
state=np.genfromtxt(OUTPUT/'tests/audio/preview-state.csv',delimiter=',',names=True)
t=np.arange(15*rate)/rate
def col(name):return np.interp(t,state['time'],state[name])
rpm,load,level,shift,speed=[col(x) for x in ('rpm','load','level','shift','speed')]
mix=.85*(1-np.clip(speed/2,0,1))+(.15+.85*load)*np.clip(speed/2,0,1)
gain=.8*level*(.5+.3*load)*shift
anchors=np.array([1400,2100,2800,3500,4300,5100,5900,6500])
signal=np.zeros(len(t))
def play(name,pitch,folder=None):
    with wave.open(str((folder or LOCAL/'audio-assets')/name),'rb') as w:
        source_rate=w.getframerate();samples=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
    pos=np.cumsum(np.broadcast_to(pitch,len(t))*source_rate/rate)%len(samples)
    idx=pos.astype(int);frac=pos-idx
    return samples[idx]*(1-frac)+samples[(idx+1)%len(samples)]*frac
for i,anchor in enumerate(anchors):
    weights=np.zeros(8);weights[i]=1
    blend=np.sqrt(np.interp(np.clip(rpm,anchors[0],anchors[-1]),anchors,weights))
    for bank,part in [('on',np.sqrt(mix)),('off',np.sqrt(1-mix))]:
        signal+=play(f'engine_{bank}_{anchor}.wav',np.clip(rpm/anchor,.5,2))*blend*gain*part
signal+=play('road.wav',.8+.6*np.clip(speed/25,0,1))*.8*level*.24*np.clip((speed-1)/16,0,1)
horn=np.clip((t-12.1)*18,0,1)*np.clip((12.8-t)*18,0,1)
signal+=play('horn.wav',1)*horn*.8*level*.8
turbo_folder=LOCAL/'audio-turbo'
turbo_available=all((turbo_folder/name).is_file() for name in ('charge.wav','blowoff_low.wav','blowoff_high.wav'))
if turbo_available and 'turbo' in state.dtype.names:
    signal+=play('charge.wav',col('charge_pitch'),turbo_folder)*.8*level*col('charge_gain')
    def release_track(telemetry):
        result=np.zeros(len(t));clips={}
        for variant,name in [(1,'blowoff_low.wav'),(2,'blowoff_high.wav')]:
            with wave.open(str(turbo_folder/name),'rb') as w:
                source_rate=w.getframerate();samples=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
            clips[variant]=np.interp(np.arange(round(len(samples)*rate/source_rate))*source_rate/rate,np.arange(len(samples)),samples)
        for event in telemetry[telemetry['blowoff']>0]:
            sample=clips[int(event['blowoff'])]
            start=round(event['time']*rate);end=min(len(result),start+len(sample))
            # Runtime stops both previous release voices when a new event fires.
            result[start:]=0;result[start:end]=sample[:end-start]*event['blowoff_gain']
        return result
    signal+=release_track(state)*.8*level
    comparison=np.genfromtxt(OUTPUT/'tests/audio/turbo-comparison-state.csv',delimiter=',',names=True)
    turbo_signal=.8*(play('charge.wav',np.interp(t,comparison['time'],comparison['charge_pitch']),turbo_folder)*np.interp(t,comparison['time'],comparison['charge_gain'])+release_track(comparison))
    rms_sections=[float(np.sqrt(np.mean(turbo_signal[(t>=start)&(t<start+5)]**2))) for start in (0,5,10)]
    assert 0<rms_sections[0]<rms_sections[1]*.5 and rms_sections[2]==0,'Turbo comparison must preserve load dynamics and silence without load'
    assert np.max(np.abs(turbo_signal))<1,'Turbo comparison clips'
    with wave.open(str(OUTPUT/'350Z-Turbovergleich.wav'),'wb') as w:
        w.setparams((1,2,rate,len(turbo_signal),'NONE','not compressed'));w.writeframes(np.rint(turbo_signal*32767).astype('<i2').tobytes())
    (OUTPUT/'tests/audio/turbo-comparison-report.json').write_text(json.dumps({
        'sections':'0-5s partial load, 5-10s full load, 10-15s high RPM without load',
        'rms':rms_sections,'full_vs_partial_db':float(20*np.log10(rms_sections[1]/rms_sections[0])),
        'peak':float(np.max(np.abs(turbo_signal))),'clipped_samples':0,
        'note':'Offline render of compiled turbo model and original local samples, not a Valheim or NFS recording.'},indent=2))
peak=float(np.max(np.abs(signal)));rms=float(np.sqrt(np.mean(signal**2)))
assert peak<1 and rms>.01,'Preview must be audible without clipping'
target=OUTPUT/'350Z-Soundvorschau.wav'
with wave.open(str(target),'wb') as w:
    w.setparams((1,2,rate,len(signal),'NONE','not compressed'));w.writeframes(np.rint(signal*32767).astype('<i2').tobytes())
report={'seconds':15,'peak':peak,'rms':rms,'clipped_samples':int(np.sum(abs(signal)>=1)),
        'original_turbo_samples':turbo_available,
        'note':'Offline rendering from the compiled sound model and embedded-loop source files. Not a Valheim recording.'}
(OUTPUT/'tests/audio/preview-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
