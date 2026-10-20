using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Rachel350Z {
internal static class DriftScoreArt {
    internal static Texture2D Load(string directory) {
        var file=new FileInfo(Path.Combine(directory,"hud","drift-score.json"));
        if(!file.Exists)return null;
        if(file.Length<=0||file.Length>128*1024)throw new InvalidDataException("Invalid drift HUD artwork size");
        ModelTexture data;
        using(var stream=file.OpenText())using(var reader=new JsonTextReader(stream))
            data=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=6,CheckAdditionalContent=true}).Deserialize<ModelTexture>(reader);
        if(data==null||data.name!="DRIFT_STYLE_POINTS_BACK"||data.width!=256||data.height!=32||data.rgba==null||data.rgba.Length!=256*32*4)
            throw new InvalidDataException("Invalid drift HUD artwork");
        var flipped=new byte[data.rgba.Length];int row=data.width*4;
        for(int y=0;y<data.height;y++)Array.Copy(data.rgba,y*row,flipped,(data.height-1-y)*row,row);
        var texture=new Texture2D(data.width,data.height,TextureFormat.RGBA32,false){name="NFVU2 drift score backing",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        try {texture.SetPixelData<byte>(flipped,0);texture.Apply(false,true);return texture;}
        catch {UnityEngine.Object.Destroy(texture);throw;}
    }
}
}
