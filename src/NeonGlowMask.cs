using System;
using UnityEngine;

namespace Rachel350Z {
// Keep the original four light pools, but diffuse their emitted light in world
// units. This is generated once per vehicle, never while driving.
internal static class NeonGlowMask {
    internal const float Width=3.7f,Length=6.2f;
    const float SourceWidth=2.7f,SourceLength=5.2f;
    internal static Texture2D Create(ModelTexture data) {
        int width=data.width,height=data.height;
        var source=new float[width*height];int peak=1;
        for(int i=0;i<data.rgba.Length;i+=4)peak=Math.Max(peak,Math.Max(data.rgba[i],Math.Max(data.rgba[i+1],data.rgba[i+2])));
        bool linear=QualitySettings.activeColorSpace==ColorSpace.Linear;
        for(int i=0;i<source.Length;i++) {
            int p=i*4;float mask=(float)Math.Max(data.rgba[p],Math.Max(data.rgba[p+1],data.rgba[p+2]))/peak;
            // The old additive texture multiplied RGB by alpha. Reconstruct
            // its actual radiance before blurring, instead of squaring a new
            // gradient again or making the softened neon brighter.
            source[i]=(linear?Mathf.GammaToLinearSpace(mask):mask)*mask;
        }
        var expanded=new float[source.Length];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
            float u=((float)x/(width-1)-.5f)*Length/SourceLength+.5f;
            float v=((float)y/(height-1)-.5f)*Width/SourceWidth+.5f;
            expanded[y*width+x]=Sample(source,width,height,u*(width-1),v*(height-1));
        }
        var core=Blur(expanded,width,height,.16f);
        var halo=Blur(expanded,width,height,.38f);
        var pixels=new Color32[source.Length];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
            float edge=Math.Min(Math.Min((float)x/(width-1)*Length,(float)(width-1-x)/(width-1)*Length),
                Math.Min((float)y/(height-1)*Width,(float)(height-1-y)/(height-1)*Width));
            float fade=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.18f));
            // Compensate the single-coverage shader path against the old
            // RGB-times-alpha mask's measured native light budget.
            byte coverage=(byte)Mathf.RoundToInt(255*Mathf.Clamp01((core[y*width+x]*.6f+halo[y*width+x]*.4f)*fade*.82f));
            // One linear coverage channel gives the long, soft falloff. White
            // RGB also avoids a second edge multiplication on alpha fallbacks.
            pixels[(height-1-y)*width+x]=new Color32(255,255,255,coverage);
        }
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,true,true) {
            name=data.name+"_SoftGroundGlow",wrapMode=TextureWrapMode.Clamp,
            filterMode=FilterMode.Trilinear,anisoLevel=4
        };
        try {texture.SetPixels32(pixels);texture.Apply(true,true);return texture;}
        catch {UnityEngine.Object.Destroy(texture);throw;}
    }
    static float Sample(float[] values,int width,int height,float x,float y) {
        if(x<0||y<0||x>width-1||y>height-1)return 0;
        int left=(int)x,bottom=(int)y,right=Math.Min(left+1,width-1),top=Math.Min(bottom+1,height-1);
        return Mathf.Lerp(Mathf.Lerp(values[bottom*width+left],values[bottom*width+right],x-left),
            Mathf.Lerp(values[top*width+left],values[top*width+right],x-left),y-bottom);
    }
    static float[] Blur(float[] input,int width,int height,float sigma) {
        var temporary=new float[input.Length];var result=new float[input.Length];
        var horizontal=Kernel(sigma*(width-1)/Length);var vertical=Kernel(sigma*(height-1)/Width);
        int rx=horizontal.Length/2,ry=vertical.Length/2;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
            float value=0;for(int k=-rx;k<=rx;k++)if(x+k>=0&&x+k<width)value+=input[y*width+x+k]*horizontal[k+rx];
            temporary[y*width+x]=value;
        }
        for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
            float value=0;for(int k=-ry;k<=ry;k++)if(y+k>=0&&y+k<height)value+=temporary[(y+k)*width+x]*vertical[k+ry];
            result[y*width+x]=value;
        }
        return result;
    }
    static float[] Kernel(float sigma) {
        int radius=Math.Max(1,Mathf.CeilToInt(sigma*3));var values=new float[radius*2+1];float sum=0;
        for(int k=-radius;k<=radius;k++){float value=Mathf.Exp(-k*k/(2*sigma*sigma));values[k+radius]=value;sum+=value;}
        for(int i=0;i<values.Length;i++)values[i]/=sum;
        return values;
    }
}
}
