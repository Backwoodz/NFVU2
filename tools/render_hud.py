"""Standalone gauge layout preview using the same imported art and positions.

Not a Unity/IMGUI runtime test. Preview images remain in ignored artifacts/.
"""
import base64,json,math,os
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from project import LOCAL,OUTPUT

def image(t):return Image.frombytes('RGBA',(t['width'],t['height']),base64.b64decode(t['rgba']))

def render(design,charge=.65,rpm=4500,speed=86,mph=False):
    data=json.loads((LOCAL/'hud/hud.json').read_text());d=data['designs'][design]
    canvas=Image.new('RGBA',(515,400),(19,25,31,255));offset=(190,50)
    def paste(im,box,tint=None):
        im=im.resize((round(box[2]),round(box[3])),Image.Resampling.LANCZOS)
        if tint:
            from PIL import ImageChops
            im=ImageChops.multiply(im,Image.new('RGBA',im.size,tint))
        canvas.alpha_composite(im,(round(box[0]+offset[0]),round(box[1]+offset[1])))
    accent=(60,222,255,255)
    paste(image(d['fill']),(0,30,280,280));paste(image(d['lines']),(12,42,256,256),accent)
    pointer=Image.new('RGBA',(280,280));pointer.alpha_composite(image(d['needle']).resize((24,140)),(128,0))
    pointer=pointer.rotate(-(180+240*rpm/7500),resample=Image.Resampling.BICUBIC,center=(140,140))
    paste(image(data['nitroBacking']),(-40,-50,460.8,230.4),(165,165,165,255))
    nitro=image(data['nitro']);pixels=nitro.load()
    for y in range(nitro.height):
        for x in range(nitro.width):
            progress=max(0,min(1,(math.degrees(math.atan2(y-122,x-100))+180)/137))
            if charge==0 or progress>charge:pixels[x,y]=(*pixels[x,y][:3],0)
    paste(nitro,(-40,-50,460.8,230.4),accent)
    draw=ImageDraw.Draw(canvas)
    angle=math.radians(180+240*rpm/7500)
    centre=(offset[0]+140,offset[1]+170)
    draw.line((centre,(centre[0]+math.sin(angle)*116,centre[1]-math.cos(angle)*116)),fill=(255,217,77),width=3)
    paste(pointer,(0,30,280,280))
    def label(x,y,text,size,color):
        fontpath=Path(os.environ.get('WINDIR','C:/Windows'))/'Fonts/segoeuib.ttf'
        font=ImageFont.truetype(str(fontpath),size) if fontpath.exists() else ImageFont.load_default()
        draw.text((x+offset[0],y+offset[1]),text,font=font,fill=color,anchor='ra')
    label(290,186,str(round(speed)),42,'white');label(290,237,'MPH' if mph else 'KM/H',14,accent);label(290,146,'G 3',22,accent)
    label(305,279,f'NOS {round(charge*100)} %',14,accent)
    label(285,316,d['name'],16,'white')
    cx,cy=offset[0]-84,offset[1]+222
    draw.ellipse((cx-68,cy-68,cx+68,cy+68),fill=(6,9,11),outline=(77,89,102),width=3)
    maximum=160 if mph else 240
    for i in range(21):
        a=math.radians(-130+260*i/20);inner=52 if i%5==0 else 57
        draw.line(((cx+math.sin(a)*inner,cy-math.cos(a)*inner),(cx+math.sin(a)*61,cy-math.cos(a)*61)),fill='white' if i%5==0 else accent,width=2)
        if i%5==0:
            font=ImageFont.truetype(str(Path(os.environ.get('WINDIR','C:/Windows'))/'Fonts/segoeuib.ttf'),11)
            draw.text((cx+math.sin(a)*44,cy-math.cos(a)*44),str(round(maximum*i/20)),font=font,anchor='mm',fill='white')
    draw.text((cx,cy+30),'MPH' if mph else 'KM/H',font=font,anchor='mm',fill='white')
    a=math.radians(-130+260*min(1,abs(speed)/maximum))
    draw.line(((cx,cy),(cx+math.sin(a)*51,cy-math.cos(a)*51)),fill='white',width=3)
    draw.ellipse((cx-3,cy-3,cx+3,cy+3),fill=accent)
    return canvas

if __name__=='__main__':
    OUTPUT.mkdir(exist_ok=True)
    sheet=Image.new('RGBA',(515*4,400*3),(12,16,22,255))
    for i in range(11):sheet.alpha_composite(render(i),((i%4)*515,(i//4)*400))
    sheet.convert('RGB').save(OUTPUT/'hud-designs.png')
    render(0).save(OUTPUT/'hud-preview.png')
    print('HUD previews written to artifacts/')
