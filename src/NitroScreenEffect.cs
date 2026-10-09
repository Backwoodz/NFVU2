using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Built-in render pipeline image effect. Uses the game's compiled UI shader
// with authored UV layers; no runtime shader compiler, readback or game asset edits.
public sealed class NitroScreenEffect : MonoBehaviour {
    const int Columns=32,Rows=20,Layers=6;
    Material material;
    Mesh mesh;
    Color[] colors;
    float amount;
    Rect carRect;
    Action<string> log;
    bool failed;
    public void Initialize(Action<string> logger) {
        enabled=false;log=logger;
        try {
            var shader=Shader.Find("UI/Default");
            if(shader==null||!shader.isSupported||!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                throw new InvalidOperationException("UI/Default shader or RGB floating-point render target unavailable");
            material=new Material(shader){name="NFVU2_NitroRadial",hideFlags=HideFlags.HideAndDontSave};
            material.SetColor("_Color",Color.white);
            material.SetVector("_TextureSampleAdd",Vector4.zero);
            material.SetInt("_StencilComp",(int)CompareFunction.Always);
            material.SetInt("_Stencil",0);material.SetInt("_StencilOp",0);
            material.SetInt("_StencilReadMask",255);material.SetInt("_StencilWriteMask",0);
            material.SetInt("_ColorMask",15);
            material.DisableKeyword("UNITY_UI_ALPHACLIP");material.DisableKeyword("UNITY_UI_CLIP_RECT");
            int count=(Columns+1)*(Rows+1);
            var vertices=new Vector3[count];var uv=new Vector2[count];colors=new Color[count];
            var triangles=new int[Columns*Rows*6];int n=0;
            for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++) {
                vertices[y*(Columns+1)+x]=new Vector3((float)x/Columns,(float)y/Rows,0);
                uv[y*(Columns+1)+x]=new Vector2((float)x/Columns,(float)y/Rows);
            }
            for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++) {
                int a=y*(Columns+1)+x,b=a+1,c=a+Columns+1,d=c+1;
                triangles[n++]=a;triangles[n++]=c;triangles[n++]=b;
                triangles[n++]=b;triangles[n++]=c;triangles[n++]=d;
            }
            mesh=new Mesh{name="NFVU2_NitroScreenGrid",hideFlags=HideFlags.HideAndDontSave};
            mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
        }catch(Exception error){Fail(error);}
    }
    public void SetEffect(float value,Rect protectedCar) {
        amount=value;carRect=protectedCar;
        enabled=!failed&&material!=null&&amount>.001f;
    }
    void OnRenderImage(RenderTexture source,RenderTexture destination) {
        // Always provide a valid frame, including failures or a disabled effect.
        Graphics.Blit(source,destination);
        if(failed||material==null||amount<=.001f)return;
        RenderTexture sample=null;
        var previous=RenderTexture.active;bool previousSRGB=GL.sRGBWrite;
        bool pushed=false;
        try {
            // RGB-only target guarantees alpha=1 regardless of the game's source
            // alpha. Downsample on the GPU; only used during the boost/fade.
            sample=RenderTexture.GetTemporary(Math.Max(1,source.width/2),Math.Max(1,source.height/2),0,
                RenderTextureFormat.RGB111110Float,RenderTextureReadWrite.Linear);
            sample.filterMode=FilterMode.Bilinear;sample.wrapMode=TextureWrapMode.Clamp;
            GL.sRGBWrite=false;Graphics.Blit(source,sample);
            Graphics.SetRenderTarget(destination);
            GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear&&(destination==null||destination.sRGB);
            material.mainTexture=sample;
            GL.PushMatrix();pushed=true;GL.LoadOrtho();
            bool flip=SystemInfo.graphicsUVStartsAtTop&&sample.texelSize.y<0;
            for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++) {
                int i=y*(Columns+1)+x;
                colors[i]=new Color(1,1,1,.14f*amount*NitroVisualState.EdgeMask((float)x/Columns,(float)y/Rows,
                    carRect.xMin,carRect.yMin,carRect.xMax,carRect.yMax));
            }
            mesh.colors=colors;
            for(int layer=1;layer<=Layers;layer++) {
                float scale=1-.075f*amount*layer/Layers;
                // The installed UI shader transforms UV with _MainTex_ST and
                // premultiplies RGB by coverage before blending One/InvSrcAlpha.
                // Keep geometry fixed across the six draws; only change uniforms.
                material.mainTextureScale=new Vector2(scale,flip?-scale:scale);
                material.mainTextureOffset=new Vector2(.5f*(1-scale),flip?1-.53f*(1-scale):.53f*(1-scale));
                if(!material.SetPass(0))throw new InvalidOperationException("Radial material pass unavailable");
                Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
            }
        }catch(Exception error){Fail(error);Graphics.Blit(source,destination);}
        finally {
            if(pushed)GL.PopMatrix();
            GL.sRGBWrite=previousSRGB;RenderTexture.active=previous;
            if(material!=null)material.mainTexture=null;
            if(sample!=null)RenderTexture.ReleaseTemporary(sample);
        }
    }
    void Fail(Exception error){failed=true;enabled=false;log?.Invoke("Nitro screen effect disabled: "+error.Message);}
    void OnDestroy(){if(material!=null)Destroy(material);if(mesh!=null)Destroy(mesh);}
}
}
