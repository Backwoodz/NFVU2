using System;
using System.IO;
using Newtonsoft.Json;
namespace Rachel350Z {
public sealed class HudDesign { public string name; public ModelTexture fill,lines,needle; }
public sealed class HudData { public int schemaVersion; public HudDesign[] designs; public ModelTexture nitro,nitroBacking; }
public static class HudFile {
    public static HudData Load(string path) {
        var file=new FileInfo(path);
        if(!file.Exists||file.Length==0||file.Length>8*1024*1024)throw new InvalidDataException("Missing or oversized HUD data");
        HudData data;
        using(var stream=File.OpenText(path))using(var reader=new JsonTextReader(stream))
            data=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=12,CheckAdditionalContent=true}).Deserialize<HudData>(reader);
        if(data==null||data.schemaVersion!=1||data.designs==null||data.designs.Length!=11)throw new InvalidDataException("Invalid HUD catalog");
        foreach(var d in data.designs) {
            if(d==null||string.IsNullOrWhiteSpace(d.name)||d.name.Length>40)throw new InvalidDataException("Invalid HUD design");
            Texture(d.fill);Texture(d.lines);Texture(d.needle);
        }
        Texture(data.nitro);Texture(data.nitroBacking);return data;
    }
    static void Texture(ModelTexture t) {
        if(t==null||t.width<1||t.height<1||t.width>256||t.height>256||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4)
            throw new InvalidDataException("Invalid HUD texture");
    }
}
public static class HudMath {
    public static float Unit(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:DrivingMath.Clamp(value,0,1);
    public static float NeedleAngle(float rpm)=>180+240*Unit(rpm/7500);
    public static float SpeedDialMaximum(bool mph)=>mph?160:240;
    public static float SpeedNeedleAngle(float metresPerSecond,bool mph)=>-130+260*Unit(Speed(metresPerSecond,mph)/SpeedDialMaximum(mph));
    public static float Speed(float metresPerSecond,bool mph)=>float.IsNaN(metresPerSecond)||float.IsInfinity(metresPerSecond)?0:Math.Abs(metresPerSecond)*(mph?2.2369363f:3.6f);
    // Top-down source pixels, counterclockwise-looking left endpoint to upper-right.
    public static float NitroPixelProgress(int x,int y)=>Unit(((float)Math.Atan2(y-122,x-100)*(180f/(float)Math.PI)+180)/137);
}
}
