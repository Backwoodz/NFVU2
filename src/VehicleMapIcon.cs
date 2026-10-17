using System;
using System.IO;
using UnityEngine;

namespace Rachel350Z {
internal sealed class VehicleMapIcon : IDisposable {
    Texture2D texture;
    public Sprite Sprite { get; private set; }

    public static VehicleMapIcon Load(string directory) {
        var result=new VehicleMapIcon();
        try {
            // Original NFSU2 FRONTEND/FrontB.lzc, key 0xa964095b,
            // CRIB_ICON_CAR_SELECT, already shipped with the menu artwork.
            var data=MenuData.Load(Path.Combine(directory,"menu","menu.json")).icons[10];
            if(data==null||data.name!="CRIB_ICON_CAR_SELECT"||data.width!=128||data.height!=64)
                throw new InvalidDataException("Missing original car-select icon");
            // Remove the selection arrows and empty padding, retain the car and
            // backing. Square canvas preserves the artwork's aspect on map pins.
            const int left=30,top=8,width=67,height=48,size=72;
            var pixels=new Color32[size*size];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
                int source=((top+y)*data.width+left+x)*4;
                pixels[((size-height)/2+height-1-y)*size+(size-width)/2+x]=
                    new Color32(data.rgba[source],data.rgba[source+1],data.rgba[source+2],data.rgba[source+3]);
            }
            result.Create(pixels,size,"NFSU2_CRIB_ICON_CAR_SELECT");
        } catch {
            result.Dispose();
            // Asset-free fallback remains a car rather than a generic map dot.
            // Code-authored top view, used only if optional menu art is absent.
            const int size=32;
            var pixels=new Color32[size*size];
            for(int y=3;y<29;y++)for(int x=7;x<25;x++) {
                bool wheel=(x<9||x>22)&&((y>=7&&y<=11)||(y>=21&&y<=25));
                bool body=x>=9&&x<=22&&y>=4&&y<=27;
                if(!wheel&&!body)continue;
                bool edge=wheel||x==9||x==22||y==4||y==27;
                bool glass=x>=11&&x<=20&&((y>=9&&y<=12)||(y>=20&&y<=23));
                pixels[y*size+x]=edge||glass ? new Color32(25,32,36,255) : new Color32(235,240,245,255);
            }
            result.Create(pixels,size,"NFVU2_CarMapFallback");
        }
        return result;
    }

    void Create(Color32[] pixels,int size,string name) {
        // A narrow dark border keeps the original white artwork legible on snow
        // and light terrain. Original interior texels and alpha remain intact.
        var outlined=(Color32[])pixels.Clone();
        for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++) {
            int i=y*size+x;if(pixels[i].a!=0)continue;
            byte alpha=0;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                alpha=Math.Max(alpha,pixels[(y+dy)*size+x+dx].a);
            if(alpha>0)outlined[i]=new Color32(15,20,24,alpha);
        }
        texture=new Texture2D(size,size,TextureFormat.RGBA32,false) {
            name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp
        };
        texture.SetPixels32(outlined);texture.Apply(false,true);
        Sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        Sprite.name=name;
    }

    public void Dispose() {
        if(Sprite!=null)UnityEngine.Object.Destroy(Sprite);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
        Sprite=null;texture=null;
    }
}
}
