from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
from pathlib import Path
import json, struct, subprocess, wave

base=BASE
exe=vgmstream_path()
source=LOCAL/'nfs-assets/SOUND'
out=LOCAL/'audio-decoded';out.mkdir(exist_ok=True)
inventory=[]
for path in sorted(source.rglob('*')):
    if path.suffix.lower() not in ('.gin','.abk') or '/GLOBAL/' in path.as_posix():continue
    def metadata(index=1):
        return json.loads(subprocess.check_output([str(exe),'-m','-I','-s',str(index),str(path)],text=True))
    info=metadata(); count=max(1,info['streamInfo']['total'])
    for i in range(1,count+1):
        info=metadata(i); target=out/(path.stem+('' if count==1 else '_'+str(i))+'.wav')
        subprocess.run([str(exe),'-i','-s',str(i),'-o',str(target),str(path)],check=True,stdout=subprocess.DEVNULL)
        with wave.open(str(target),'rb') as w:
            assert w.getnframes()==info['numberOfSamples']
            assert w.getsampwidth()==2 and w.getnchannels()==1
        entry={'source':str(path.relative_to(LOCAL/'nfs-assets')),'wav':target.name,'subsong':i,'metadata':info}
        if path.suffix.lower()=='.gin':
            data=path.read_bytes(); rpms=struct.unpack_from('<ff',data,8); counts=struct.unpack_from('<II',data,16)
            entry['rpm_range']=rpms;entry['table_counts']=counts
            print(path.name,'seconds',round(info['numberOfSamples']/info['sampleRate'],3),'rpm',rpms,'tables',counts)
            a=struct.unpack_from('<'+str(counts[0]+1)+'I',data,32)
            b=struct.unpack_from('<'+str(counts[1]+1)+'I',data,32+(counts[0]+1)*4)
            entry['table_a']=a;entry['table_b']=b
            print(' A:',a[:8],a[-8:],'B:',b[:8],b[-8:])
        inventory.append(entry)
(out/'inventory.json').write_text(json.dumps(inventory,indent=2))
print('Decoded',len(inventory),'original sounds.')
