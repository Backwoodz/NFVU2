from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
from pathlib import Path
import hashlib, json, wave
import numpy as np

base=BASE
decoded=LOCAL/'audio-decoded'
out=LOCAL/'audio-assets';out.mkdir(exist_ok=True)
inventory=json.loads((decoded/'inventory.json').read_text())
anchors=[1400,2100,2800,3500,4300,5100,5900,6500]
manifest=[]

def read(path):
    with wave.open(str(path),'rb') as w:
        return w.getframerate(),np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768

def seam(samples,n):
    n=min(n,len(samples)//4); t=np.linspace(0,1,n)
    return np.concatenate((samples[n:-n],samples[-n:]*(1-t)+samples[:n]*t))

def save(name,rate,samples,source,rpm=None):
    samples=samples-np.mean(samples)
    rms=np.sqrt(np.mean(samples*samples));peak=np.max(np.abs(samples))
    assert rms>.0001
    samples*=min(.18/rms,.82/peak)
    pcm=np.rint(samples*32767).astype('<i2')
    with wave.open(str(out/name),'wb') as w:
        w.setparams((1,2,rate,len(pcm),'NONE','not compressed'));w.writeframes(pcm.tobytes())
    manifest.append({'file':name,'source':source,'rpm':rpm,'sample_rate':rate,'samples':len(pcm),
                     'peak':float(np.max(np.abs(samples))),'rms':float(np.sqrt(np.mean(samples*samples))),
                     'seam_step':float(abs(samples[-1]-samples[0])),'sha256':hashlib.sha256((out/name).read_bytes()).hexdigest()})

for bank,stem in [('on','GIN_350Z_Jason_shrt2'),('off','GIN_Nissan_350ZX_Jason_DCL')]:
    row=next(x for x in inventory if x['wav']==stem+'.wav')
    rate,samples=read(decoded/row['wav'])
    cuts=np.array(row['table_b']);rpm_axis=np.linspace(*row['rpm_range'],len(row['table_a']))
    for rpm in anchors:
        center=np.interp(rpm,rpm_axis,row['table_a'])
        idx=int(np.argmin(abs(cuts-center)));start=max(0,min(idx-6,len(cuts)-13))
        length=round(120*rate/rpm) # Each GIN grain spans two crankshaft revolutions.
        cycles=[]
        for k in range(start,start+12):
            chunk=samples[cuts[k]:cuts[k+1]]
            positions=np.arange(length)*len(chunk)/length
            cycles.append(np.interp(positions,np.arange(len(chunk)+1),np.append(chunk,samples[cuts[k+1]])))
        loop=seam(np.concatenate(cycles),length)
        save(f'engine_{bank}_{rpm}.wav',rate,loop,row['source'],rpm)

for name,src in [('road.wav','ROADNOISE_00_MB_4.wav'),('skid.wav','SKID_PAV_MB_1.wav')]:
    rate,samples=read(decoded/src);row=next(x for x in inventory if x['wav']==src)
    save(name,rate,seam(samples,round(rate*.03)),row['source']+' subsong '+str(row['subsong']))

rate=32000;t=np.arange(rate//2)/rate
horn=sum(np.sin(2*np.pi*f*t)+.3*np.sin(2*np.pi*2*f*t)+.12*np.sin(2*np.pi*3*f*t) for f in (420,500))
save('horn.wav',rate,horn,'Synthesized two-tone horn, created for this prototype')
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
assert all(m['peak']<=.83 and m['rms']>.01 and m['samples']>2000 for m in manifest)
print('Prepared',len(manifest),'mono loops; peak <= 0.82; every file non-silent.')
for m in manifest:print(m['file'],round(m['samples']/m['sample_rate'],2),'sec; seam',round(m['seam_step'],5))
