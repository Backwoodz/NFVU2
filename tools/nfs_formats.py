"""Read local UG2 compression/texture data. Layout references: docs/REFERENCES.md.

Original payloads and converted textures must only be written under .local/.
"""
import io
import struct
from PIL import Image


def bin_hash(name):
    value = 0xffffffff
    for byte in name.encode('ascii'):
        value = (value * 33 + byte) & 0xffffffff
    return value


def jdlz(data):
    if data[:4] != b'JDLZ':
        raise ValueError('Not a JDLZ stream')
    size = struct.unpack_from('<I', data, 8)[0]
    if size > 64 * 1024 * 1024:
        raise ValueError('Oversized JDLZ stream')
    out = bytearray()
    flags1 = flags2 = 1
    pos = 16
    while len(out) < size:
        if flags1 == 1:
            flags1 = data[pos] | 0x100; pos += 1
        if flags2 == 1:
            flags2 = data[pos] | 0x100; pos += 1
        if flags1 & 1:
            a, b = data[pos:pos+2]; pos += 2
            if flags2 & 1:
                count, distance = (b | (a & 0xf0) << 4) + 3, (a & 15) + 1
            else:
                count, distance = (a & 31) + 3, (b | (a & 0xe0) << 3) + 17
            if distance > len(out) or len(out) + count > size:
                raise ValueError('Invalid JDLZ back reference')
            for _ in range(count): out.append(out[-distance])
            flags2 >>= 1
        else:
            out.append(data[pos]); pos += 1
        flags1 >>= 1
    return bytes(out)


def huffman(data, capacity):
    pos = 0
    def bits(n):
        nonlocal pos
        if pos+n > len(data)*8: raise ValueError('Truncated Huffman stream')
        value = 0
        for _ in range(n):
            value = value*2 + ((data[pos//8] >> (7-pos%8)) & 1); pos += 1
        return value
    def variable():
        width = 2
        while bits(1) == 0:
            width += 1
            if width > 24: raise ValueError('Invalid variable integer')
        return bits(width) + (1 << width) - 4
    method = bits(16)
    if method & 0xf0ff != 0x30fb: raise ValueError('Unknown Huffman method')
    if method & 0x100: bits(24)
    size, escape = bits(24), bits(8)
    if not 0 < size <= capacity: raise ValueError('Invalid Huffman size')
    counts = [0]; code = 0
    while len(counts) <= 16 and code << (17-len(counts)) < 65536:
        count = variable()
        if count > 256: raise ValueError('Invalid Huffman tree')
        counts.append(count); code = code*2 + count
    total = sum(counts)
    if not 0 < total <= 256: raise ValueError('Invalid Huffman symbols')
    symbols = []; used = set(); value = 255
    for _ in range(total):
        step = variable()+1
        if step > 256: raise ValueError('Invalid Huffman symbol')
        while step:
            value = (value+1) & 255
            if value not in used: step -= 1
        symbols.append(value); used.add(value)
    first = []; index = []; code = offset = 0
    for count in counts:
        first.append(code); index.append(offset)
        code = (code+count)*2; offset += count
    out = bytearray()
    while len(out) < size:
        code = 0
        for width in range(1,len(counts)):
            code = code*2+bits(1)
            if 0 <= code-first[width] < counts[width]:
                symbol = symbols[index[width]+code-first[width]]; break
        else: raise ValueError('Invalid Huffman code')
        if symbol != escape: out.append(symbol); continue
        count = variable()
        if count:
            out.extend(bytes([out[-1] if out else 0])*count)
        else:
            if bits(1): break
            out.append(bits(8))
    if len(out) != size: raise ValueError('Huffman size mismatch')
    if method & 0xfeff in (0x32fb,0x34fb):
        x = y = 0
        for i, byte in enumerate(out):
            x = (x+byte)&255; y = (y+x)&255
            out[i] = y if method & 0xfeff == 0x34fb else x
    return bytes(out)


def dxt_image(raw, w, h, fourcc):
    # Pillow's DDS decoder handles BC1/2/3, including their alpha channels.
    header = struct.pack('<7I',124,0x81007,h,w,len(raw),0,1) + bytes(44)
    header += struct.pack('<II4s5I',32,4,fourcc,0,0,0,0,0)
    header += struct.pack('<5I',0x1000,0,0,0,0)
    return Image.open(io.BytesIO(b'DDS '+header+raw)).convert('RGBA')


class TexturePack:
    def __init__(self, path):
        self.data = path.read_bytes(); self.slots = {}; self.records = {}
        data = self.data
        start = data.find(struct.pack('<I',0xb3310000))
        if start < 0: return
        end = start+8+struct.unpack_from('<I',data,start+4)[0]; pos=start+8
        while pos+8 <= end:
            tag, size = struct.unpack_from('<II',data,pos); pos += 8
            if pos+size > end: raise ValueError('Invalid texture table')
            if tag == 0x33310003:
                for i in range(pos,pos+size,24):
                    key,off,encoded,decoded,tail,_ = struct.unpack_from('<6I',data,i)
                    self.slots[key]=(off,encoded,decoded,tail)
            if tag == 0x33310004:
                # Uncompressed packs use 124-byte records with a 12-byte prefix.
                for i in range(pos,pos+size,124):
                    q=i+12
                    key=struct.unpack_from('<I',data,q+24)[0]
                    self.records[key]=q
            pos += size
        pixel=data.find(struct.pack('<I',0x33320002),end)
        self.pixel_base=pixel+8 if pixel>=0 else 0
        while self.pixel_base and data[self.pixel_base:self.pixel_base+1]==b'\x11': self.pixel_base+=1

    def decode(self, key):
        if key in self.records:
            p=self.records[key];data=self.data
            off,paloff,size,palsize=struct.unpack_from('<4I',data,p+0x24)
            w,h=struct.unpack_from('<HH',data,p+0x38);fmt=data[p+0x3e]
            raw=data[self.pixel_base+off:self.pixel_base+off+size]
            name=data[p:p+24].split(b'\0')[0].decode('ascii',errors='replace')
            if fmt in (0x22,0x24,0x26):
                image=dxt_image(raw,w,h,{0x22:b'DXT1',0x24:b'DXT3',0x26:b'DXT5'}[fmt])
            elif fmt==8 and palsize>=1024:
                image=Image.frombytes('P',(w,h),raw[:w*h]);image.putpalette(data[self.pixel_base+paloff:self.pixel_base+paloff+1024],'RGBA');image=image.convert('RGBA')
            else: raise ValueError(f'Unsupported global texture format {fmt:x}')
            return name,image,raw
        off,encoded,decoded,tail = self.slots[key]
        payload = self.data[off:off+encoded]
        if len(payload)!=encoded or decoded > 32*1024*1024: raise ValueError('Invalid texture payload')
        raw = huffman(payload[16:],decoded) if payload[:4]==b'HUFF' else jdlz(payload)
        p = decoded-tail+0x88
        if p < 0 or p+0x28>len(raw) or struct.unpack_from('<I',raw,p)[0]!=key:
            raise ValueError('Texture header does not match slot')
        w,h=struct.unpack_from('<HH',raw,p+0x20); fmt=raw[p+0x26]
        if not 0 < w <= 4096 or not 0 < h <= 4096: raise ValueError('Invalid texture dimensions')
        name=raw[p-24:p].split(b'\0')[0].decode('ascii',errors='replace')
        if fmt in (0x22,0x24,0x26):
            count=((w+3)//4)*((h+3)//4)*(8 if fmt==0x22 else 16)
            image=dxt_image(raw[:count],w,h,{0x22:b'DXT1',0x24:b'DXT3',0x26:b'DXT5'}[fmt])
        elif fmt==0x20:
            image=Image.frombytes('RGBA',(w,h),raw[:w*h*4],'raw','BGRA')
        elif fmt in (8,0x80,0x81):
            image=Image.frombytes('P',(w,h),raw[:w*h])
            palette_start=p-24-12-1024
            if palette_start < w*h: raise ValueError('Missing indexed texture palette')
            image.putpalette(raw[palette_start:palette_start+1024],'RGBA')
        else: raise ValueError(f'Unsupported texture format {fmt:x}')
        return name,image,raw
