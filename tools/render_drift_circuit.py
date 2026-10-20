"""Render the exact C# circuit/test export as a scaled SVG construction plan."""
import csv
import math
from html import escape
from project import BASE, OUTPUT


def main():
    folder=OUTPUT/'tests/drifttrack'
    with (folder/'circuit-plan.csv').open() as file:
        samples=[{key:float(value) for key,value in row.items()} for row in csv.DictReader(file)]
    with (folder/'circuit-pieces.csv').open() as file:pieces=list(csv.DictReader(file))
    svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 1080">',
         '<rect width="1000" height="1080" fill="#121b20"/>',
         '<rect x="30" y="105" width="940" height="870" rx="16" fill="#20352d"/>',
         '<g font-family="Segoe UI,Arial,sans-serif">']
    def point(x,z):return 30+(x+30)*1.85,110+(345-z)*1.85
    def text(x,y,value,size=15,color='#f1ead9',extra=''):
        svg.append(f'<text x="{x:.1f}" y="{y:.1f}" fill="{color}" font-size="{size}" {extra}>{escape(value)}</text>')
    def line(x,z,xx,zz,color,width=1):
        a,b=point(x,z);c,d=point(xx,zz);svg.append(f'<path d="M{a:.2f},{b:.2f} L{c:.2f},{d:.2f}" stroke="{color}" stroke-width="{width}" fill="none"/>')
    def rectangle(x,z,width,depth,color):
        a,b=point(x,z+depth);svg.append(f'<rect x="{a:.2f}" y="{b:.2f}" width="{width*1.85:.2f}" height="{depth*1.85:.2f}" fill="{color}"/>')
    def path(points,closed=False):return 'M'+' L'.join(f'{x:.2f},{y:.2f}' for x,y in points)+(' Z' if closed else '')
    def label(value,x,z,xx,zz):
        line(x,z,xx,zz,'#d3c39e',1);a,b=point(xx,zz);width=len(value)*6.6+16
        svg.append(f'<rect x="{a-8:.1f}" y="{b-19:.1f}" width="{width:.1f}" height="28" rx="5" fill="#182429" stroke="#62776b"/>')
        text(a,b,value,12)
    left=[point(p['x']-p['tz']*p['half_width'],p['z']+p['tx']*p['half_width']) for p in samples]
    right=[point(p['x']+p['tz']*p['half_width'],p['z']-p['tx']*p['half_width']) for p in samples]
    rectangle(107,-32,82,45,'#8c8a78');line(36,-2,110,-2,'#8c8a78',12)
    svg.append(f'<path d="{path(left,True)} {path(right,True)}" fill="#9b9c91" fill-rule="evenodd" stroke="#e4e0c9" stroke-width="1"/>')
    centre=path([point(p['x'],p['z']) for p in samples]);svg.append(f'<path d="{centre}" fill="none" stroke="#dedbc6" stroke-width=".8" stroke-dasharray="7 9"/>')
    for piece in pieces:
        x,y=point(float(piece['x']),float(piece['z']));name=piece['prefab']
        if name=='wood_fence':svg.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="1.5" fill="#a68051"/>')
        elif name=='piece_groundtorch':svg.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="4" fill="#efc46a"/>')
        elif name.startswith('piece_banner'):svg.append(f'<path d="M{x-3:.1f},{y-5:.1f} l8,3 -8,3z" fill="#cd7057"/>')
    rectangle(120,-24,24,8,'#624b3c')
    for x in (126,132,138):line(x,-24,x,-16,'#c29a64',1)
    rectangle(118,52,28,13,'#8b765a')
    for z in (54,58,62):line(120,z,144,z,'#30291f',3)
    for distance in range(30,int(samples[-1]['distance']),100):
        p=min(samples,key=lambda p:abs(p['distance']-distance));x,y=point(p['x'],p['z'])
        angle=-math.degrees(math.atan2(p['tz'],p['tx']))
        svg.append(f'<path d="M-5,-3 L4,0 -5,3" transform="translate({x:.1f},{y:.1f}) rotate({angle:.1f})" fill="none" stroke="#2c4549" stroke-width="2"/>')
    label('START / ZIEL',108,24,151,4)
    label('WEITE KEHRE · R38',241,62,270,82)
    label('DOPPELTER RICHTUNGSWECHSEL · R32',151,284,103,320)
    label('AUSSENPASSAGE',360,120,375,170)
    label('RÜCKFÜHRUNG · R48 / R40',62,-15,-20,-58)
    label('VIER ÜBERDACHTE BOXEN',132,-20,190,-28)
    label('TRIBÜNE · 18 SITZBÄNKE',133,60,145,152)
    x,y=point(0,0);svg.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="6" fill="#78c9ce"/>');text(x-42,y-16,'WELTSTART',12,'#b6e2e1')
    rectangle(105,23,4.38,1.93,'#5dc7cb')
    line(260,-95,310,-95,'#f1ead9',4);x,y=point(275,-90);text(x,y,'50 m',12)
    line(344,305,344,323,'#f1ead9',2);line(341,319,344,323,'#f1ead9',2);line(347,319,344,323,'#f1ead9',2)
    x,y=point(341,328);text(x,y,'N',16)
    text(55,48,'NFVU2 · WIKINGER-DRIFTKURS',26,extra='font-weight="bold"')
    text(55,80,f'{samples[-1]["distance"]/1000:.2f} km Rundkurs · 16–20 m Fahrbahn · 350Z maßstäblich',16,'#aebeb7')
    text(55,1010,'Bauplan aus den implementierten C#-Koordinaten. Keine Aufnahme aus Valheim.',14,'#aebeb7')
    text(55,1035,'Bestehender U-Abschnitt bleibt erhalten; Aufbau in geladenen Weltbereichen.',14,'#aebeb7')
    svg.append('</g></svg>')
    destination=BASE/'docs/images/drift-circuit-plan.svg';destination.parent.mkdir(exist_ok=True)
    destination.write_text('\n'.join(svg),encoding='utf-8');print(destination)


if __name__=='__main__':main()
