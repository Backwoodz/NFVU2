using System;
using UnityEngine;

namespace Rachel350Z {
internal static class ParkadeMapDrawing {
    internal const int Size=384;
    internal static readonly float Span=Math.Max(ParkadeLayout.Bounds.MaxX-ParkadeLayout.Bounds.MinX,ParkadeLayout.Bounds.MaxZ-ParkadeLayout.Bounds.MinZ)+14;
    internal static readonly float MinX=(ParkadeLayout.Bounds.MinX+ParkadeLayout.Bounds.MaxX-Span)*.5f;
    internal static readonly float MinZ=(ParkadeLayout.Bounds.MinZ+ParkadeLayout.Bounds.MaxZ-Span)*.5f;
    internal static Color32[] Pixels() {
        var pixels=new Color32[Size*Size];
        foreach(var p in ParkadeLayout.Samples) {
            float px=(p.X-MinX)/Span*Size,pz=(p.Z-MinZ)/Span*Size,r=p.HalfWidth/Span*Size;
            for(int z=Math.Max(0,(int)(pz-r-1));z<=Math.Min(Size-1,(int)(pz+r+1));z++)for(int x=Math.Max(0,(int)(px-r-1));x<=Math.Min(Size-1,(int)(px+r+1));x++) {
                float dx=x+.5f-px,dz=z+.5f-pz,alpha=Math.Max(0,Math.Min(1,r+.5f-(float)Math.Sqrt(dx*dx+dz*dz)));int i=z*Size+x;
                if(alpha>pixels[i].a/255f)pixels[i]=new Color(.88f,.83f,.62f,alpha);
            }
        }
        int sx=(int)((ParkadeLayout.StartX-MinX)/Span*Size),sz=(int)((ParkadeLayout.StartZ-MinZ)/Span*Size);
        for(int z=sz-4;z<=sz+4;z++)for(int x=sx-4;x<=sx+4;x++)if(x>=0&&z>=0&&x<Size&&z<Size)pixels[z*Size+x]=new Color(.2f,1,.5f,1);
        return pixels;
    }
    internal static Texture2D Texture() {
        var texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false){name="Measured Parkade course plan",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        texture.SetPixels32(Pixels());texture.Apply(false,true);return texture;
    }
}
}
