"""Offline front/rear listening illustration from compiled control telemetry.

The biquad filters approximate the native playback filters. This is not a Unity
or NFS capture, and does not simulate spatial rolloff, Doppler or room acoustics.
"""
import hashlib
import json
import math
import wave
import numpy as np
from project import LOCAL, OUTPUT

rate=32000
state=np.genfromtxt(OUTPUT/'tests/audio/perspective-state.csv',delimiter=',',names=True)
t=np.arange(12*rate)/rate
def col(name):return np.interp(t,state['time'],state[name])
def read(path):
    with wave.open(str(path),'rb') as stream:
        assert stream.getnchannels()==1 and stream.getsampwidth()==2
        return stream.getframerate(),np.frombuffer(stream.readframes(stream.getnframes()),dtype='<i2').astype(float)/32768
def loop(name,pitch,folder):
    source_rate,samples=read(folder/name)
    position=np.cumsum(np.broadcast_to(pitch,len(t))*source_rate/rate)%len(samples)
    index=position.astype(int);fraction=position-index
    return samples[index]*(1-fraction)+samples[(index+1)%len(samples)]*fraction
def filtered(samples,cutoff,high):
    omega=2*math.pi*cutoff/rate;cos=math.cos(omega);alpha=math.sin(omega)/2  # Q=1
    if high:b0=(1+cos)/2;b1=-(1+cos);b2=b0
    else:b0=(1-cos)/2;b1=1-cos;b2=b0
    a0=1+alpha;a1=-2*cos/a0;a2=(1-alpha)/a0;b0/=a0;b1/=a0;b2/=a0
    output=np.empty_like(samples);x1=x2=y1=y2=0.
    for i,x in enumerate(samples):
        y=b0*x+b1*x1+b2*x2-a1*y1-a2*y2;output[i]=y;x2=x1;x1=x;y2=y1;y1=y
    return output

rpm=col('rpm');load=col('load');mix=.15+.85*load
engine=np.zeros(len(t));bands=np.array([1400,2100,2800,3500,4300,5100,5900,6500])
for i,anchor in enumerate(bands):
    weights=np.zeros(len(bands));weights[i]=1
    gain=np.sqrt(np.interp(np.clip(rpm,bands[0],bands[-1]),bands,weights))
    for bank,part in [('on',np.sqrt(mix)),('off',np.sqrt(1-mix))]:
        engine+=loop(f'engine_{bank}_{anchor}.wav',np.clip(rpm/anchor,.4,2.2),LOCAL/'audio-assets')*gain*part
engine*=col('level')*(.5+.3*load)*col('shift')
signal=filtered(engine,280,True)*col('engine_gain')+filtered(engine,1500,False)*col('exhaust_gain')
turbo=loop('charge.wav',col('charge_pitch'),LOCAL/'audio-turbo')*col('charge_gain')
release=np.zeros(len(t))
for event in state[state['blowoff']>0]:
    name='blowoff_high.wav' if event['blowoff']==2 else 'blowoff_low.wav'
    source_rate,sample=read(LOCAL/'audio-turbo'/name)
    sample=np.interp(np.arange(round(len(sample)*rate/source_rate))*source_rate/rate,np.arange(len(sample)),sample)
    start=round(event['time']*rate);end=min(len(t),start+len(sample))
    release[start:]=0;release[start:end]=sample[:end-start]*event['blowoff_gain']
signal+=(turbo+release*col('release_envelope'))*col('turbo_gain')*col('level')
signal*=.8
peak=float(np.max(np.abs(signal)))
assert peak<1,'Perspective preview clips; do not hide this with normalization'
def high_fraction(start):
    block=signal[(t>=start)&(t<start+1)]
    power=abs(np.fft.rfft(block*np.hanning(len(block))))**2
    frequencies=np.fft.rfftfreq(len(block),1/rate)
    return float(power[frequencies>2000].sum()/power.sum())
rear=high_fraction(1.7);front=high_fraction(5.7)
assert front>rear*1.2,'Front perspective must expose more high-frequency engine/turbo detail'
path=OUTPUT/'350Z-Front-Heck-Soundvorschau.wav'
with wave.open(str(path),'wb') as stream:
    stream.setparams((1,2,rate,len(signal),'NONE','not compressed'));stream.writeframes(np.rint(signal*32767).astype('<i2').tobytes())
report={'seconds':12,'sections':'0-4s rear, 4-8s front, 8-12s rear; repeated acceleration and lift',
        'peak':peak,'rms':float(np.sqrt(np.mean(signal*signal))),'clipped_samples':0,
        'high_frequency_fraction':{'rear':rear,'front':front},'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
        'note':'Offline illustration using original local recordings, compiled control telemetry and approximate biquad filters; not Unity/NFS gameplay audio.'}
(OUTPUT/'tests/audio/perspective-preview-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
