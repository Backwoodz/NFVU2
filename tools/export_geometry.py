from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import json, math, re, struct
from pathlib import Path

def chunks(data, start=0, end=None, depth=0):
    end = len(data) if end is None else end
    if depth > 20: raise ValueError('excessive nesting')
    while start + 8 <= end:
        ident, size = struct.unpack_from('<II', data, start)
        body = start + 8
        if body + size > end: raise ValueError(f'chunk exceeds parent at {start:x}')
        yield ident, body, body + size
        start = body + size

def leaves(data, start, end):
    for ident, a, b in chunks(data, start, end):
        if ident >> 28 == 8:
            yield from leaves(data, a, b)
        elif ident:
            yield ident, data[a:b]

def objects(data, start=0, end=None):
    for ident, a, b in chunks(data, start, end):
        if ident == 0x80134010:
            yield a, b
        elif ident >> 28 == 8:
            yield from objects(data, a, b)

def unpad(data, pair=False):
    offset = 0
    step = 2 if pair else 1
    filler = b'\x11' * step
    while data[offset:offset+step] == filler: offset += step
    return data[offset:]

def parse(path):
    data = Path(path).read_bytes()
    result = []
    for a,b in objects(data):
        table = dict(leaves(data,a,b))
        header = unpad(table.get(0x134011,b''))
        names = re.findall(rb'350Z_[A-Z0-9_]+', header)
        if not names: continue
        name = max(names,key=len).decode()
        if len(name)==24:
            key=struct.unpack_from('<I',header,16)[0]
            for suffix in ('A','B','C','D','_A','_B','_C','_D','R_A','R_B','R_C','R_D','T_A','T_B','T_C'):
                candidate=name+suffix
                h=0xffffffff
                for c in candidate.encode():h=(h*33+c)&0xffffffff
                if h==key:name=candidate;break
        vb = unpad(table.get(0x134b01,b''))
        ib = unpad(table.get(0x134b03,b''),True)
        if not vb or len(vb)%36 or len(ib)%2: continue
        vertices = [struct.unpack_from('<3f',vb,i) for i in range(0,len(vb),36)]
        uv = [struct.unpack_from('<2f',vb,i+28) for i in range(0,len(vb),36)]
        indices = list(struct.unpack('<'+'H'*(len(ib)//2),ib))
        indices = indices[:len(indices)//3*3]
        if any(i>=len(vertices) for i in indices): raise ValueError('invalid index '+name)
        if any(not math.isfinite(v) or abs(v)>100 for xyz in vertices for v in xyz): raise ValueError('invalid position '+name)
        result.append({'name':name,'vertices':vertices,'uv':uv,'indices':indices})
    return result

if __name__ == '__main__':
    parts = parse(LOCAL/'nfs-assets/CARS/350Z/GEOMETRY.BIN')
    (LOCAL/'350z_parts.json').write_text(json.dumps(parts), encoding='utf-8')
    print('Parsed meshes:',len(parts))
    for p in parts[:8]:print(p['name'],len(p['vertices']),len(p['indices']))
    selected = [p for p in parts if p['name']=='350Z_BASE_A' or (p['name'].startswith('350Z_KIT00_') and p['name'].endswith('_A') and not any(x in p['name'] for x in ('AUDIO','UNLIT','UNDER','ENGINE','WHEEL','BRAKE_A','DOOR_PANEL','DOOR_SILL')))]
    for prefix in ('350Z_KIT00_HEADLIGHT_RIGHT_', '350Z_KIT00_LEFT_SIDE_MIRROR', '350Z_KIT00_RIGHT_SIDE_MIRRO'):
        candidates=[p for p in parts if p['name']==prefix]
        if candidates:selected.append(max(candidates,key=lambda p:len(p['indices'])))
    out = (LOCAL/'model');out.mkdir(parents=True,exist_ok=True)
    with (out/'350z.obj').open('w',encoding='utf-8') as f:
        offset=1
        for p in selected:
            f.write('o '+p['name']+'\n')
            for x,y,z in p['vertices']:f.write(f'v {x} {y} {z}\n')
            for u,v in p['uv']:f.write(f'vt {u} {1-v}\n')
            for i in range(0,len(p['indices']),3):
                ids=[v+offset for v in p['indices'][i:i+3]]
                f.write('f '+' '.join(f'{v}/{v}' for v in ids)+'\n')
            offset+=len(p['vertices'])
    print('Exported preliminary stock body:',len(selected),'parts')
    (out/'body.json').write_text(json.dumps({'parts':[{'name':p['name'],'vertices':[{'x':y,'y':z,'z':x} for x,y,z in p['vertices']],'indices':p['indices']} for p in selected]}),encoding='utf-8')
