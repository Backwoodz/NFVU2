"""Prepare two original local NFS drift loops; extracted data stays ignored.

These banks establish available recordings, not the original executable's
mixing rules. NFVU2's speed/slip response is its own acoustic implementation.
"""
import hashlib
import json
import subprocess
import wave
import zipfile
import numpy as np
from project import LOCAL, disc_root, vgmstream_path
from inspect_discs import Disc
from extract_assets import DiscFile


def main():
    research=LOCAL/'tyre-research';research.mkdir(exist_ok=True)
    output=LOCAL/'audio-tyres';output.mkdir(exist_ok=True)
    # Both banks share substream 1 byte-for-byte. Use distinct recordings:
    # lower-centroid stream 3 for scrub, broader stream 4 for hard sliding.
    wanted={'SOUND/SKIDS/SKIDS_DRIFT2_MB.ABK':('scrub.wav',3),'SOUND/SKIDS/SKIDS_DRIFT_MB.ABK':('slide.wav',4)}
    banks={}
    for image in disc_root().glob('CD*/*.bin'):
        disc=Disc(image)
        try:
            for rec in disc.walk():
                if rec['name']!='COMPRESS.ZIP':continue
                with zipfile.ZipFile(DiscFile(disc,rec)) as archive:
                    for item in archive.infolist():
                        name=item.filename.replace('\\','/').upper()
                        if name in wanted:
                            raw=archive.read(item)  # ZIP reader verifies the original CRC.
                            path=research/name.split('/')[-1];path.write_bytes(raw)
                            banks[name]=(path,hashlib.sha256(raw).hexdigest())
        finally:disc.f.close()
    if set(banks)!=set(wanted):raise ValueError('Both original drift banks are required')
    entries=[]
    for name,(target,index) in wanted.items():
        bank,digest=banks[name];decoder=str(vgmstream_path())
        metadata=json.loads(subprocess.check_output([decoder,'-m','-I','-s',str(index),str(bank)],text=True))
        decoded=research/(bank.stem+'.wav')
        subprocess.run([decoder,'-i','-s',str(index),'-o',str(decoded),str(bank)],check=True,stdout=subprocess.DEVNULL)
        with wave.open(str(decoded)) as wav:
            if wav.getnchannels()!=1 or wav.getsampwidth()!=2:raise ValueError('Expected original mono PCM16')
            rate=wav.getframerate();samples=np.frombuffer(wav.readframes(wav.getnframes()),dtype='<i2').astype(float)/32768
        loop=metadata.get('loopingInfo')
        if not loop or not 0<=loop['start']<loop['end']<=len(samples):raise ValueError('Missing valid original loop')
        samples=samples[loop['start']:loop['end']].copy();samples-=np.mean(samples)
        fade=min(round(rate*.02),len(samples)//8);t=np.linspace(0,1,fade)
        samples=np.concatenate((samples[fade:-fade],samples[-fade:]*(1-t)+samples[:fade]*t))
        entries.append((target,name,digest,index,metadata,rate,samples))
    peak=max(np.max(np.abs(e[-1])) for e in entries)
    rms=max(np.sqrt(np.mean(e[-1]**2)) for e in entries)
    if rms<.0001:raise ValueError('Silent drift recordings')
    gain=min(.14/rms,.82/peak);manifest=[]
    for target,source,digest,index,metadata,rate,samples in entries:
        samples*=gain;pcm=np.rint(samples*32767).astype('<i2');path=output/target
        with wave.open(str(path),'wb') as wav:
            wav.setparams((1,2,rate,len(pcm),'NONE','not compressed'));wav.writeframes(pcm.tobytes())
        manifest.append(dict(file=target,source=source,source_sha256=digest,subsong=index,
            bank_subsongs=metadata['streamInfo']['total'],original_loop=metadata['loopingInfo'],
            sample_rate=rate,samples=len(samples),rms=float(np.sqrt(np.mean(samples*samples))),
            peak=float(np.max(np.abs(samples))),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
            adaptation='20 ms loop seam; common gain; NFVU2 physical slip/speed crossfade, not recovered NFS controller'))
    if len({entry['sha256'] for entry in manifest})!=2:raise ValueError('Tyre layers must be distinct source recordings')
    (output/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    for entry in manifest:print(entry['file'],entry['bank_subsongs'],'original substreams;',entry['samples']/entry['sample_rate'],'seconds, RMS',round(entry['rms'],3))


if __name__=='__main__':main()
