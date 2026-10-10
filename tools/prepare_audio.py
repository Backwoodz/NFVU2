from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
from pathlib import Path
import argparse, hashlib, json, wave
import numpy as np

base=BASE
parser=argparse.ArgumentParser()
parser.add_argument('--turbo-only',action='store_true')
parser.add_argument('--turbo-bank',choices=('0','1','2'),default='2')
parser.add_argument('--charge-stream',type=int,default=1)
parser.add_argument('--low-stream',type=int,default=2)
parser.add_argument('--high-stream',type=int,default=3)
args=parser.parse_args()
decoded=LOCAL/'audio-decoded'
if args.turbo_only:
    source_inventory=decoded/'turbo-inventory.json'
    if not source_inventory.is_file():source_inventory=decoded/'inventory.json'
    inventory=json.loads(source_inventory.read_text(encoding='utf-8'))
    out=LOCAL/'audio-turbo';out.mkdir(exist_ok=True)
    manifest=[]
    for name,index in [('charge.wav',args.charge_stream),('blowoff_low.wav',args.low_stream),('blowoff_high.wav',args.high_stream)]:
        stem=f'TURBO_MED_{args.turbo_bank}_MB'
        row=next((r for r in inventory if r['wav']==f'{stem}_{index}.wav'),None)
        if row is None:raise SystemExit(f'Missing decoded {stem} substream {index}. Run turbo extraction and decoding first.')
        with wave.open(str(decoded/row['wav']),'rb') as w:
            if w.getnchannels()!=1 or w.getsampwidth()!=2:raise ValueError('Expected original mono PCM16 turbo sample')
            rate=w.getframerate();samples=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
        if not 0<len(samples)<=15*rate:raise ValueError('Invalid turbo sample duration')
        if name=='charge.wav':
            loop=row['metadata'].get('loopingInfo')
            if not loop:raise ValueError('Charge source must have a verified loop in vgmstream metadata')
            samples=samples[loop['start']:loop['end']]
            n=min(round(rate*.012),len(samples)//8);t=np.linspace(0,1,n)
            samples=np.concatenate((samples[n:-n],samples[-n:]*(1-t)+samples[:n]*t))
        else:
            if row['metadata'].get('loopingInfo'):raise ValueError('Blow-off source must be a non-looping substream')
            n=min(round(rate*.004),len(samples)//8)
            samples[:n]*=np.linspace(0,1,n);samples[-n:]*=np.linspace(1,0,n)
        samples-=np.mean(samples)
        peak=float(np.max(np.abs(samples)));rms=float(np.sqrt(np.mean(samples*samples)))
        if rms<.0001:raise ValueError('Silent turbo sample')
        # A shared gain preserves the original relationship between charge and
        # release recordings. Per-clip RMS normalisation erased that dynamic.
        samples*=.8
        if np.max(np.abs(samples))>.83:raise ValueError('Unexpected original turbo peak after shared gain')
        with wave.open(str(out/name),'wb') as w:
            w.setparams((1,2,rate,len(samples),'NONE','not compressed'));w.writeframes(np.rint(samples*32767).astype('<i2').tobytes())
        original=LOCAL/'nfs-assets'/row['source']
        manifest.append({'file':name,'source':row['source'],'source_sha256':hashlib.sha256(original.read_bytes()).hexdigest(),
                         'decoder_version':row['metadata'].get('version'),'subsong':index,'sample_rate':rate,'samples':len(samples),
                         'peak':float(np.max(np.abs(samples))),'rms':float(np.sqrt(np.mean(samples*samples))),
                         'source_rms_after_edge_processing':rms,'shared_gain':.8,
                         'sha256':hashlib.sha256((out/name).read_bytes()).hexdigest()})
    (out/'manifest.json').write_text(json.dumps({'bank':f'TURBO_MED_{args.turbo_bank}_MB.abk','files':manifest,
        'provenance':'Original local NFSU2 samples decoded with vgmstream. Shared gain 0.8 preserves relative sample levels; no per-clip RMS normalisation. Role assignments remain configurable approximations from loop/one-shot metadata. Pressure, dynamic volume and flutter are NFVU2 approximations; dataset ChargeTime and volume ratios are recovered parameters.',
        'mapping_reference':'https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarDataMapping/02.ini'},indent=2),encoding='utf-8')
    print('Prepared 3 original turbo clips in .local/audio-turbo; role assignment needs in-game listening verification.')
    raise SystemExit(0)
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
