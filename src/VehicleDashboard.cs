using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
public sealed class VehicleDashboard : IDisposable {
    readonly List<Texture2D> textures=new List<Texture2D>();
    readonly Texture2D[] faces=new Texture2D[11],lines=new Texture2D[11],needles=new Texture2D[11],nitroLevels=new Texture2D[49];
    Texture2D nitroBacking;
    Texture2D speedDial;
    GUIStyle speedStyle,smallStyle,gearStyle,dialStyle;
    Texture2D Texture(ModelTexture data,byte[] pixels=null) {
        var result=new Texture2D(data.width,data.height,TextureFormat.RGBA32,false){name="350Z_HUD_"+data.name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        textures.Add(result);pixels=pixels??data.rgba;var flipped=new byte[pixels.Length];int row=data.width*4;
        for(int y=0;y<data.height;y++)Array.Copy(pixels,y*row,flipped,(data.height-1-y)*row,row);
        result.SetPixelData<byte>(flipped,0);result.Apply(false,true);return result;
    }
    public IEnumerable<int> LoadSteps(HudData data) {
        speedDial=new Texture2D(128,128,TextureFormat.RGBA32,false){name="NFVU2_SpeedDial",filterMode=FilterMode.Bilinear};textures.Add(speedDial);
        var dialPixels=new Color[128*128];
        for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
            float radius=Mathf.Sqrt((x-63.5f)*(x-63.5f)+(y-63.5f)*(y-63.5f));
            dialPixels[y*128+x]=radius>63?Color.clear:radius>60?new Color(.3f,.35f,.4f,.95f):new Color(.025f,.035f,.045f,.94f);
        }
        speedDial.SetPixels(dialPixels);speedDial.Apply(false,true);
        for(int i=0;i<11;i++){faces[i]=Texture(data.designs[i].fill);yield return 0;lines[i]=Texture(data.designs[i].lines);yield return 0;needles[i]=Texture(data.designs[i].needle);yield return 0;}
        nitroBacking=Texture(data.nitroBacking);
        for(int level=0;level<nitroLevels.Length;level++) {
            var pixels=(byte[])data.nitro.rgba.Clone();
            for(int y=0;y<data.nitro.height;y++)for(int x=0;x<data.nitro.width;x++)
                if(level==0||HudMath.NitroPixelProgress(x,y)>level/48f)pixels[(y*data.nitro.width+x)*4+3]=0;
            nitroLevels[level]=Texture(data.nitro,pixels);
            yield return 0;
        }
    }
    public void Draw(int design,float scale,Color accent,bool mph,float speed,float rpm,int gear,float charge,bool nitroEnabled,bool boosting) {
        if(speedStyle==null) {
            speedStyle=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleRight};
            smallStyle=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleRight};
            gearStyle=new GUIStyle(smallStyle){fontSize=22,fontStyle=FontStyle.Bold};
            dialStyle=new GUIStyle(smallStyle){fontSize=11,alignment=TextAnchor.MiddleCenter};dialStyle.normal.textColor=Color.white;
        }
        scale=Mathf.Min(Mathf.Clamp(scale,.65f,1.4f),Mathf.Min(Screen.width/620f,Screen.height/510f));
        design=Mathf.Clamp(design,0,10);
        var matrix=GUI.matrix;var color=GUI.color;
        try {
            GUI.matrix=Matrix4x4.TRS(new Vector3(Screen.width-325*scale-18,Screen.height-315*scale-90),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;GUI.DrawTexture(new Rect(0,30,280,280),faces[design]);
            GUI.color=accent;GUI.DrawTexture(new Rect(12,42,256,256),lines[design]);
            var nosRect=new Rect(-40,-50,460.8f,230.4f);
            GUI.color=new Color(.65f,.65f,.65f,1);GUI.DrawTexture(nosRect,nitroBacking);
            if(nitroEnabled) {
                GUI.color=boosting?new Color(.3f,1,.25f):accent;
                GUI.DrawTexture(nosRect,nitroLevels[Mathf.RoundToInt(HudMath.Unit(charge)*48)]);
            }
            // Draw pointers last, with a solid contrast core, so sparse/dark
            // original needle artwork and the NOS backing cannot hide them.
            Pointer(new Vector2(140,170),HudMath.NeedleAngle(rpm),116,new Color(1,.85f,.3f));
            var needleMatrix=GUI.matrix;
            GUI.matrix=needleMatrix*Matrix4x4.TRS(new Vector3(140,170,0),Quaternion.Euler(0,0,HudMath.NeedleAngle(rpm)),Vector3.one);
            GUI.color=Color.white;GUI.DrawTexture(new Rect(-12,-140,24,140),needles[design]);GUI.matrix=needleMatrix;
            DrawSpeedDial(speed,mph,accent);
            GUI.color=Color.white;
            speedStyle.normal.textColor=Color.white;smallStyle.normal.textColor=accent;gearStyle.normal.textColor=accent;
            GUI.Label(new Rect(145,180,145,58),Mathf.RoundToInt(HudMath.Speed(speed,mph)).ToString(),speedStyle);
            GUI.Label(new Rect(190,237,100,24),mph?"MPH":"KM/H",smallStyle);
            GUI.Label(new Rect(192,143,98,34),gear<0?"R":"G "+Mathf.Clamp(gear,1,6),gearStyle);
            GUI.Label(new Rect(150,275,155,26),nitroEnabled?"NOS "+Mathf.RoundToInt(HudMath.Unit(charge)*100)+" %":"NOS AUS",smallStyle);
        } finally {GUI.matrix=matrix;GUI.color=color;}
    }
    static void Pointer(Vector2 centre,float angle,float length,Color colour) {
        var saved=GUI.matrix;
        GUI.matrix=saved*Matrix4x4.TRS(new Vector3(centre.x,centre.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        GUI.color=Color.black;GUI.DrawTexture(new Rect(-3,-length-1,6,length+7),Texture2D.whiteTexture);
        GUI.color=colour;GUI.DrawTexture(new Rect(-1.5f,-length,3,length+4),Texture2D.whiteTexture);
        GUI.matrix=saved;
    }
    void DrawSpeedDial(float speed,bool mph,Color accent) {
        var centre=new Vector2(-84,222);
        GUI.color=Color.white;GUI.DrawTexture(new Rect(centre.x-68,centre.y-68,136,136),speedDial);
        var saved=GUI.matrix;
        for(int i=0;i<=20;i++) {
            float angle=-130+260*i/20f;
            GUI.matrix=saved*Matrix4x4.TRS(new Vector3(centre.x,centre.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
            GUI.color=i%5==0?Color.white:accent;
            GUI.DrawTexture(new Rect(-1,-61,2,i%5==0?9:4),Texture2D.whiteTexture);
            GUI.matrix=saved;
            if(i%5==0) {
                float rad=angle*Mathf.Deg2Rad;
                GUI.color=Color.white;GUI.Label(new Rect(centre.x+Mathf.Sin(rad)*44-16,centre.y-Mathf.Cos(rad)*44-9,32,18),Mathf.RoundToInt(HudMath.SpeedDialMaximum(mph)*i/20).ToString(),dialStyle);
            }
        }
        GUI.color=Color.white;GUI.Label(new Rect(centre.x-35,centre.y+22,70,18),mph?"MPH":"KM/H",dialStyle);
        Pointer(centre,HudMath.SpeedNeedleAngle(speed,mph),51,Color.white);
        GUI.color=accent;GUI.DrawTexture(new Rect(centre.x-3,centre.y-3,6,6),Texture2D.whiteTexture);
    }
    public void Dispose(){foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);textures.Clear();}
}
}
