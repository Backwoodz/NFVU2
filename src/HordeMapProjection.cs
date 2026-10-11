using System;
using UnityEngine;
namespace Rachel350Z {
public static class HordeMapProjection {
    public const float DotSize=9;
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    // Same world-to-UV mapping as the installed Minimap, followed by its current
    // displayed UV rectangle. Y is altitude and has no effect on the map.
    public static bool TryProject(Vector3 world,float pixelSize,int textureSize,Rect uv,Vector2 size,out Vector2 point) {
        point=Vector2.zero;
        if(!Finite(world.x)||!Finite(world.z)||!Finite(pixelSize)||pixelSize<=0||textureSize<=0||
           !Finite(uv.x)||!Finite(uv.y)||!Finite(uv.width)||!Finite(uv.height)||uv.width<=0||uv.height<=0||
           !Finite(size.x)||!Finite(size.y)||size.x<=DotSize||size.y<=DotSize)return false;
        float mx=(world.x/pixelSize+textureSize/2)/textureSize;
        float my=(world.z/pixelSize+textureSize/2)/textureSize;
        float x=(mx-uv.x)/uv.width*size.x,y=(my-uv.y)/uv.height*size.y;
        float margin=DotSize*.5f;
        if(!Finite(x)||!Finite(y)||x<margin||y<margin||x>size.x-margin||y>size.y-margin)return false;
        point=new Vector2(x,y);return true;
    }
}
}
