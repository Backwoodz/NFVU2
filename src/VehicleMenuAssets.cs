using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Audio;
namespace Rachel350Z {
public sealed class MenuData {
    public int schemaVersion;
    public ModelTexture[] icons;
    public string[] titles;
    public static MenuData Load(string path) {
        var file=new FileInfo(path);
        if(!file.Exists||file.Length>4*1024*1024)throw new InvalidDataException("Missing or oversized menu assets");
        var data=JsonConvert.DeserializeObject<MenuData>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=12});
        if(data==null||data.schemaVersion!=1||data.icons==null||data.icons.Length!=11||data.titles==null||data.titles.Length!=11)throw new InvalidDataException("Invalid menu catalog");
        foreach(var t in data.icons)if(t!=null&&(t.width<1||t.height<1||t.width>256||t.height>256||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4))throw new InvalidDataException("Invalid menu texture");
        foreach(var title in data.titles)if(title!=null&&title.Length>40)throw new InvalidDataException("Invalid menu title");
        return data;
    }
}
internal sealed class VehicleMenuAssets : IDisposable {
    public readonly Texture2D[] Icons=new Texture2D[11];
    public readonly string[] Titles=new string[11];
    public Texture2D RustLogo {get;private set;}
    readonly List<AudioClip> clips=new List<AudioClip>();
    AudioSource source;
    GameObject audioObject;
    float lastSound=-1;
    public static VehicleMenuAssets Load(string folder,Action<string> log) {
        var result=new VehicleMenuAssets();
        try {
            var data=MenuData.Load(Path.Combine(folder,"menu.json"));
            for(int i=0;i<11;i++) {
                result.Titles[i]=data.titles[i];var t=data.icons[i];if(t==null)continue;
                var tex=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false){name=t.name,wrapMode=TextureWrapMode.Clamp};result.Icons[i]=tex;
                var flipped=new byte[t.rgba.Length];int row=t.width*4;
                for(int y=0;y<t.height;y++)Array.Copy(t.rgba,y*row,flipped,(t.height-1-y)*row,row);
                tex.SetPixelData<byte>(flipped,0);tex.Apply(false,true);
            }
        }catch(Exception e){log("Original menu artwork unavailable: "+e.Message);}
        try {
            var path=Path.Combine(folder,"rust-logo.json");
            if(File.Exists(path)) {
                if(new FileInfo(path).Length>512*1024)throw new InvalidDataException("Oversized Rust logo");
                var t=JsonConvert.DeserializeObject<ModelTexture>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=8});
                if(t==null||t.width<1||t.height<1||t.width>512||t.height>128||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4)throw new InvalidDataException("Invalid Rust logo");
                result.RustLogo=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false){name="Official_Rust_Branding",wrapMode=TextureWrapMode.Clamp};
                var pixels=new byte[t.rgba.Length];int row=t.width*4;
                for(int y=0;y<t.height;y++)Array.Copy(t.rgba,y*row,pixels,(t.height-1-y)*row,row);
                result.RustLogo.SetPixelData<byte>(pixels,0);result.RustLogo.Apply(false,true);
            }
        }catch(Exception e){log("Optional Rust logo unavailable: "+e.Message);}
        try {
            foreach(var role in new[]{"navigate","accept","back"}) {
                var wav=WavPcm16.Read(File.OpenRead(Path.Combine(folder,role+".wav")));
                var clip=AudioClip.Create("NFS_Menu_"+role,wav.Samples.Length/wav.Channels,wav.Channels,wav.SampleRate,false);result.clips.Add(clip);
                if(!clip.SetData(wav.Samples,0))throw new InvalidDataException("Menu audio upload failed");
            }
            result.audioObject=new GameObject("NFVU2_MenuAudio");UnityEngine.Object.DontDestroyOnLoad(result.audioObject);
            result.source=result.audioObject.AddComponent<AudioSource>();result.source.playOnAwake=false;result.source.spatialBlend=0;
            result.source.outputAudioMixerGroup=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        }catch(Exception e){log("Original menu sounds unavailable: "+e.Message);}
        return result;
    }
    public void Play(int sound,float volume) {
        if(source==null||clips.Count!=3||!Application.isFocused||float.IsNaN(volume)||float.IsInfinity(volume)||volume<=0||Time.unscaledTime-lastSound<.07f)return;
        lastSound=Time.unscaledTime;source.PlayOneShot(clips[sound],Mathf.Clamp01(volume));
    }
    public void Dispose() {
        if(audioObject!=null)UnityEngine.Object.Destroy(audioObject);
        foreach(var clip in clips)UnityEngine.Object.Destroy(clip);
        foreach(var icon in Icons)if(icon!=null)UnityEngine.Object.Destroy(icon);
        if(RustLogo!=null)UnityEngine.Object.Destroy(RustLogo);
    }
}
}
