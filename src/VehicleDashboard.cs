using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
public sealed class VehicleDashboard : IDisposable {
    readonly List<Texture2D> textures=new List<Texture2D>();
    readonly Texture2D[] faces=new Texture2D[11],lines=new Texture2D[11],needles=new Texture2D[11],nitroLevels=new Texture2D[49];
    Texture2D nitroBacking;
    GUIStyle speedStyle,smallStyle,gearStyle;
    public static VehicleDashboard Create(string path,Action<string> log) {
        var hud=new VehicleDashboard();
        try {hud.Load(HudFile.Load(path));return hud;}
        catch(Exception e){hud.Dispose();log("350Z original HUD unavailable: "+e.Message);return null;}
    }
    Texture2D Texture(ModelTexture data,byte[] pixels=null) {
        var result=new Texture2D(data.width,data.height,TextureFormat.RGBA32,false){name="350Z_HUD_"+data.name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        textures.Add(result);pixels=pixels??data.rgba;var flipped=new byte[pixels.Length];int row=data.width*4;
        for(int y=0;y<data.height;y++)Array.Copy(pixels,y*row,flipped,(data.height-1-y)*row,row);
        result.SetPixelData<byte>(flipped,0);result.Apply(false,true);return result;
    }
    void Load(HudData data) {
        for(int i=0;i<11;i++){faces[i]=Texture(data.designs[i].fill);lines[i]=Texture(data.designs[i].lines);needles[i]=Texture(data.designs[i].needle);}
        nitroBacking=Texture(data.nitroBacking);
        for(int level=0;level<nitroLevels.Length;level++) {
            var pixels=(byte[])data.nitro.rgba.Clone();
            for(int y=0;y<data.nitro.height;y++)for(int x=0;x<data.nitro.width;x++)
                if(level==0||HudMath.NitroPixelProgress(x,y)>level/48f)pixels[(y*data.nitro.width+x)*4+3]=0;
            nitroLevels[level]=Texture(data.nitro,pixels);
        }
    }
    public void Draw(int design,float scale,Color accent,bool mph,float speed,float rpm,int gear,float charge,bool nitroEnabled,bool boosting) {
        if(speedStyle==null) {
            speedStyle=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleRight};
            smallStyle=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleRight};
            gearStyle=new GUIStyle(smallStyle){fontSize=22,fontStyle=FontStyle.Bold};
        }
        scale=Mathf.Min(Mathf.Clamp(scale,.65f,1.4f),Mathf.Min(Screen.width/620f,Screen.height/510f));
        design=Mathf.Clamp(design,0,10);
        var matrix=GUI.matrix;var color=GUI.color;
        try {
            GUI.matrix=Matrix4x4.TRS(new Vector3(Screen.width-325*scale-18,Screen.height-315*scale-90),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;GUI.DrawTexture(new Rect(0,30,280,280),faces[design]);
            GUI.color=accent;GUI.DrawTexture(new Rect(12,42,256,256),lines[design]);
            var baseMatrix=GUI.matrix;
            GUIUtility.RotateAroundPivot(HudMath.NeedleAngle(rpm),new Vector2(140,170));
            GUI.color=Color.white;GUI.DrawTexture(new Rect(128,30,24,140),needles[design]);GUI.matrix=baseMatrix;
            var nosRect=new Rect(-40,-50,460.8f,230.4f);
            GUI.color=new Color(.65f,.65f,.65f,1);GUI.DrawTexture(nosRect,nitroBacking);
            if(nitroEnabled) {
                GUI.color=boosting?new Color(.3f,1,.25f):accent;
                GUI.DrawTexture(nosRect,nitroLevels[Mathf.RoundToInt(HudMath.Unit(charge)*48)]);
            }
            GUI.color=Color.white;
            speedStyle.normal.textColor=Color.white;smallStyle.normal.textColor=accent;gearStyle.normal.textColor=accent;
            GUI.Label(new Rect(145,180,145,58),Mathf.RoundToInt(HudMath.Speed(speed,mph)).ToString(),speedStyle);
            GUI.Label(new Rect(190,237,100,24),mph?"MPH":"KM/H",smallStyle);
            GUI.Label(new Rect(192,143,98,34),gear<0?"R":"G "+Mathf.Clamp(gear,1,6),gearStyle);
            GUI.Label(new Rect(150,275,155,26),nitroEnabled?"NOS "+Mathf.RoundToInt(HudMath.Unit(charge)*100)+" %":"NOS AUS",smallStyle);
        } finally {GUI.matrix=matrix;GUI.color=color;}
    }
    public void Dispose(){foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);textures.Clear();}
}
}
