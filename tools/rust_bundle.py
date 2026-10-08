"""Read local UnityFS bundles without loading multi-gigabyte texture banks.

Only uncompressed payload blocks are accepted; the directory may use LZ4.
Original files are opened read-only. UnityPy handles serialized object formats.
"""
import io,struct,sys
from pathlib import Path
from project import LOCAL,configured_path
sys.path.insert(0,str(LOCAL/'tools/unitypy'))
import lz4.block
import UnityPy

def cstring(f):
    result=bytearray()
    for _ in range(4096):
        b=f.read(1)
        if not b:raise ValueError('Truncated bundle string')
        if b==b'\0':return result.decode('utf-8')
        result.extend(b)
    raise ValueError('Oversized bundle string')

class Slice(io.RawIOBase):
    def __init__(self,path,start,size):self.file=open(path,'rb');self.start=start;self.size=size;self.pos=0
    def readable(self):return True
    def seekable(self):return True
    def tell(self):return self.pos
    def seek(self,offset,whence=0):
        if whence not in (0,1,2):raise ValueError('Invalid slice seek origin')
        pos=offset if whence==0 else self.pos+offset if whence==1 else self.size+offset
        if not 0<=pos<=self.size:raise ValueError('Bundle slice seek out of bounds')
        self.pos=pos;return pos
    def read(self,n=-1):
        n=self.size-self.pos if n<0 else min(n,self.size-self.pos)
        self.file.seek(self.start+self.pos);data=self.file.read(n);self.pos+=len(data);return data
    def close(self):self.file.close();super().close()

def nodes(path):
    path=Path(path)
    with path.open('rb') as f:
        if cstring(f)!='UnityFS' or struct.unpack('>I',f.read(4))[0]<7:raise ValueError('Unsupported bundle')
        cstring(f);version=cstring(f)
        size,packed,raw,flags=struct.unpack('>QIII',f.read(20))
        if size!=path.stat().st_size or raw>16*1024*1024 or flags&0x400:raise ValueError('Invalid or encrypted bundle')
        f.seek((f.tell()+15)&~15);start=f.tell()
        if flags&0x80:f.seek(size-packed)
        table=f.read(packed)
        if flags&0x3f in (2,3):table=lz4.block.decompress(table,uncompressed_size=raw)
        elif flags&0x3f:raise ValueError('Unsupported directory compression')
        data=io.BytesIO(table);data.seek(16);count=struct.unpack('>I',data.read(4))[0]
        total=0
        for _ in range(count):
            unpacked,compressed,blockflags=struct.unpack('>IIH',data.read(10))
            if blockflags&0x3f or unpacked!=compressed:raise ValueError('Compressed payload requires a different reader')
            total+=unpacked
        begin=start if flags&0x80 else start+packed
        if flags&0x200:begin=(begin+15)&~15
        if begin+total>(size-packed if flags&0x80 else size):raise ValueError('Bundle blocks exceed payload')
        count=struct.unpack('>I',data.read(4))[0];result=[]
        for _ in range(count):
            offset,length,nodeflags=struct.unpack('>QQI',data.read(20));name=cstring(data)
            if offset+length>total or begin+offset+length>size:raise ValueError('Bundle node exceeds file')
            result.append((name,begin+offset,length,nodeflags))
        return result

def load_bundles(names,scene_filter='prefabs'):
    env=UnityPy.Environment();root=configured_path('rust_dir')/'Bundles/shared'
    for name in names:
        path=root/name
        for node,start,size,flags in nodes(path):
            if name=='assetscenes.bundle' and scene_filter not in node:continue
            env.load_file(Slice(path,start,size),name=node)
    return env

if __name__=='__main__':
    for path in (configured_path('rust_dir')/'Bundles/shared').glob('*.bundle'):
        print(path.name,nodes(path))
