"""Original stock H2 GIN, decoded locally; phase-aligned loops for Unity playback."""
import hashlib
import json
import struct
import subprocess
import wave
import numpy as np
from project import LOCAL, vgmstream_path

BANDS=[1000,1500,2000,2500,3000,3600,4200,4782]

def main():
    source=LOCAL/'nfs-assets/SOUND/ENGINE/GIN_Hummer.gin'
    decoded=LOCAL/'audio-decoded/hummer.wav';decoded.parent.mkdir(parents=True,exist_ok=True)
    subprocess.run([str(vgmstream_path()),'-i','-o',str(decoded),str(source)],check=True,stdout=subprocess.DEVNULL)
    data=source.read_bytes();rpm_range=struct.unpack_from('<ff',data,8);na,nb=struct.unpack_from('<II',data,16)
    a=struct.unpack_from('<'+'I'*(na+1),data,32);cuts=np.array(struct.unpack_from('<'+'I'*(nb+1),data,32+(na+1)*4))
    with wave.open(str(decoded),'rb') as w:
        if w.getnchannels()!=1 or w.getsampwidth()!=2:raise ValueError('Expected mono PCM16')
        rate=w.getframerate();samples=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
    out=LOCAL/'audio-hummer';out.mkdir(parents=True,exist_ok=True);files=[]
    for rpm in BANDS:
        center=np.interp(rpm,np.linspace(*rpm_range,len(a)),a);idx=int(np.argmin(abs(cuts-center)));start=max(0,min(idx-6,len(cuts)-13))
        length=round(120*rate/rpm);cycles=[]
        for k in range(start,start+12):
            chunk=samples[cuts[k]:cuts[k+1]]
            cycles.append(np.interp(np.arange(length)*len(chunk)/length,np.arange(len(chunk)+1),np.append(chunk,samples[cuts[k+1]])))
        loop=np.concatenate(cycles);t=np.linspace(0,1,length)
        loop=np.concatenate((loop[length:-length],loop[-length:]*(1-t)+loop[:length]*t));loop-=loop.mean()
        rms=float(np.sqrt(np.mean(loop*loop)));peak=float(np.max(abs(loop)))
        if rms<.0001:raise ValueError('Silent H2 sample')
        loop*=min(.18/rms,.82/peak);name=f'engine_{rpm}.wav'
        with wave.open(str(out/name),'wb') as w:
            w.setparams((1,2,rate,len(loop),'NONE','not compressed'));w.writeframes(np.rint(loop*32767).astype('<i2').tobytes())
        files.append(dict(file=name,rpm=rpm,samples=len(loop),sampleRate=rate,sha256=hashlib.sha256((out/name).read_bytes()).hexdigest()))
    manifest=dict(source='SOUND/ENGINE/GIN_Hummer.gin',sourceSha256=hashlib.sha256(data).hexdigest(),rpmRange=rpm_range,files=files,
        reference='https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/EngineData/38.ini',
        provenance='Stock HUMMER mapping 14 -> EngineData 38: GIN_Hummer, UseDualGinsu=0, 1000-4782 RPM. Unity uses this original bank for load and quieter coast; no separate deceleration recording is claimed. Gear timings are adapted for Valheim.')
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8');print('Prepared',len(files),'original H2 engine loops; source RPM range:',rpm_range)

if __name__=='__main__':main()
