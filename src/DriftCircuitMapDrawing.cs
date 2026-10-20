using System;
using UnityEngine;

namespace Rachel350Z {
internal static class DriftCircuitMapDrawing {
    internal const int Size=512;
    internal const float MinX=-64,MinZ=-144,WorldSize=512;
    internal static Color32[] Pixels() {
        var road=new float[Size*Size];var edge=new float[road.Length];
        var pit=new float[road.Length];var pitEdge=new float[road.Length];
        foreach(var p in DriftTrackConstructionPlan.PaintStamps){Stamp(road,p,0);Stamp(edge,p,2);}
        foreach(var p in DriftCircuitPlan.PaintStamps) {
            if(p.DistanceAlong<0){Stamp(pit,p,0);Stamp(pitEdge,p,1.5f);}
            else {Stamp(road,p,0);Stamp(edge,p,2);}
        }
        var pixels=new Color32[road.Length];
        for(int i=0;i<pixels.Length;i++) {
            Color color=Color.clear;
            Over(ref color,new Color(.08f,.10f,.09f,.85f),pitEdge[i]);
            Over(ref color,new Color(.66f,.61f,.46f,.85f),pit[i]);
            Over(ref color,new Color(.08f,.10f,.09f,.95f),edge[i]);
            Over(ref color,new Color(.96f,.88f,.64f,1),road[i]);
            int x=i%Size,z=i/Size;
            float wx=MinX+(x+.5f)*WorldSize/Size,wz=MinZ+(z+.5f)*WorldSize/Size;
            if(Math.Abs(wx-DriftTrackLayout.StartX)<2&&Math.Abs(wz-DriftTrackLayout.StartZ)<8)
                color=new Color(.24f,.91f,.89f,1);
            pixels[i]=color;
        }
        return pixels;
    }
    static void Over(ref Color target,Color source,float coverage) {
        float alpha=source.a*coverage,total=alpha+target.a*(1-alpha);
        if(total<=0)return;
        target=new Color((source.r*alpha+target.r*target.a*(1-alpha))/total,
            (source.g*alpha+target.g*target.a*(1-alpha))/total,
            (source.b*alpha+target.b*target.a*(1-alpha))/total,total);
    }
    static void Stamp(float[] pixels,DriftTrackStamp p,float extra) {
        float metres=WorldSize/Size,radius=p.Radius+extra;
        int x0=Math.Max(0,(int)Math.Floor((p.X-radius-1-MinX)/metres));
        int x1=Math.Min(Size-1,(int)Math.Ceiling((p.X+radius+1-MinX)/metres));
        int z0=Math.Max(0,(int)Math.Floor((p.Z-radius-1-MinZ)/metres));
        int z1=Math.Min(Size-1,(int)Math.Ceiling((p.Z+radius+1-MinZ)/metres));
        for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++) {
            float dx=MinX+(x+.5f)*metres-p.X,dz=MinZ+(z+.5f)*metres-p.Z;
            float coverage=Math.Max(0,Math.Min(1,(radius+.5f*metres-(float)Math.Sqrt(dx*dx+dz*dz))/metres));
            int index=z*Size+x;pixels[index]=Math.Max(pixels[index],coverage);
        }
    }
    internal static DriftMapPlacement Project(int size,float metres,float uvX,float uvY,float uvWidth,float uvHeight,float width,float height,float minX=MinX,float minZ=MinZ,float worldSize=WorldSize) {
        if(size<=0||!Positive(metres)||!Positive(uvWidth)||!Positive(uvHeight)||!Positive(width)||!Positive(height)||!Finite(uvX)||!Finite(uvY))return default;
        // Match native WorldToMapPoint, including integer division for odd sizes.
        double span=(double)size*metres,origin=(double)(size/2)/size;
        float x=(float)((origin+minX/span-uvX)/uvWidth*width);
        float y=(float)((origin+minZ/span-uvY)/uvHeight*height);
        float w=(float)(worldSize/span/uvWidth*width),h=(float)(worldSize/span/uvHeight*height);
        if(!Finite(x)||!Finite(y)||!Positive(w)||!Positive(h))return default;
        return new DriftMapPlacement(x,y,w,h,x<width&&y<height&&x+w>0&&y+h>0);
    }
    static bool Positive(float value)=>Finite(value)&&value>0;
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
internal struct DriftMapPlacement {
    internal readonly float X,Y,Width,Height;
    internal readonly bool Visible;
    internal DriftMapPlacement(float x,float y,float width,float height,bool visible){X=x;Y=y;Width=width;Height=height;Visible=visible;}
}
}
