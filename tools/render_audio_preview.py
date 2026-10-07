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
def play(name,pitch):
    with wave.open(str(LOCAL/'audio-assets'/name),'rb') as w:
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
peak=float(np.max(np.abs(signal)));rms=float(np.sqrt(np.mean(signal**2)))
assert peak<1 and rms>.01,'Preview must be audible without clipping'
target=OUTPUT/'350Z-Soundvorschau.wav'
with wave.open(str(target),'wb') as w:
    w.setparams((1,2,rate,len(signal),'NONE','not compressed'));w.writeframes(np.rint(signal*32767).astype('<i2').tobytes())
report={'seconds':15,'peak':peak,'rms':rms,'clipped_samples':int(np.sum(abs(signal)>=1)),
        'note':'Offline rendering from the compiled sound model and embedded-loop source files. Not a Valheim recording.'}
(OUTPUT/'tests/audio/preview-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
