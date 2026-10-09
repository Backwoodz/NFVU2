using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
namespace Rachel350Z {
// Managed data only: safe on a worker. Cache lifetime is one local world.
public sealed class SpawnAssetCache {
    sealed class Entry {public long Size,Ticks;public object Data;}
    readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
    public T Load<T>(string path,Func<string,T> read,CancellationToken token,string cacheKey=null) where T:class {
        token.ThrowIfCancellationRequested();
        var file=new FileInfo(path);
        long size=file.Length,ticks=file.LastWriteTimeUtc.Ticks;
        string key=cacheKey??path;
        lock(entries)if(entries.TryGetValue(key,out var entry)&&entry.Size==size&&entry.Ticks==ticks)return (T)entry.Data;
        var data=read(path);
        token.ThrowIfCancellationRequested();
        lock(entries)entries[key]=new Entry{Size=size,Ticks=ticks,Data=data};
        return data;
    }
}
public sealed class SpawnAssets {
    public ModelData Model,Interior,Turret,Flames;
    public HudData Hud;
    public SpawnAudio Audio;
    public readonly List<string> Warnings=new List<string>();
    public static SpawnAssets Load(string folder,bool hummer,SpawnAssetCache cache,CancellationToken token) {
        var result=new SpawnAssets();
        result.Model=cache.Load(Path.Combine(folder,hummer?"hummer/h2.json":"body.json"),ModelFile.Load,token);
        result.Flames=result.Model;
        if(hummer)result.Optional(()=>result.Flames=cache.Load(Path.Combine(folder,"body.json"),ModelFile.Load,token),"Flight atlas",token);
        else {
            result.Optional(()=>result.Interior=cache.Load(Path.Combine(folder,"interior/interior.json"),ModelFile.Load,token),"Cabin",token);
            result.Optional(()=>result.Turret=cache.Load(Path.Combine(folder,"turret/turret.json"),ModelFile.Load,token),"Turret",token);
        }
        result.Optional(()=>result.Hud=cache.Load(Path.Combine(folder,"hud/hud.json"),HudFile.Load,token),"HUD",token);
        result.Audio=new SpawnAudio(folder,hummer,cache,token);
        return result;
    }
    void Optional(Action load,string label,CancellationToken token) {
        token.ThrowIfCancellationRequested();
        try {load();}catch(OperationCanceledException){throw;}catch(Exception error){Warnings.Add(label+" unavailable: "+error.Message);}
    }
}
public sealed class SpawnAudio {
    readonly Dictionary<string,WavData> clips=new Dictionary<string,WavData>();
    readonly Dictionary<string,Exception> errors=new Dictionary<string,Exception>();
    public readonly WavData Reverse;
    public SpawnAudio(string folder,bool hummer,SpawnAssetCache cache,CancellationToken token) {
        var assembly=typeof(SpawnAudio).Assembly;
        foreach(var name in assembly.GetManifestResourceNames())if(name.StartsWith("Rachel350Z.Audio.",StringComparison.Ordinal)&&name.EndsWith(".wav",StringComparison.Ordinal))
            Read(name,()=>cache.Load(assembly.Location,_=>WavPcm16.Read(assembly.GetManifestResourceStream(name)),token,name),token);
        foreach(var subfolder in hummer?new[]{"audio/effects","audio/hummer"}:new[]{"audio/effects","audio/turbo","turret"}) {
            string path=Path.Combine(folder,subfolder);
            if(!Directory.Exists(path))continue;
            foreach(var file in Directory.GetFiles(path,"*.wav"))
                Read(file,()=>cache.Load(file,p=>WavPcm16.Read(File.OpenRead(p),subfolder=="turret"?30:15),token),token);
        }
        token.ThrowIfCancellationRequested();
        Reverse=ReverseGearSound.Create();
    }
    void Read(string key,Func<WavData> load,CancellationToken token) {
        key=Key(key);
        token.ThrowIfCancellationRequested();
        try {clips[key]=load();}catch(OperationCanceledException){throw;}catch(Exception error){errors[key]=error;}
    }
    public WavData Embedded(string name)=>FileData("Rachel350Z.Audio."+name);
    public WavData FileData(string path) {
        path=Key(path);
        if(clips.TryGetValue(path,out var clip))return clip;
        errors.TryGetValue(path,out var error);
        throw new InvalidDataException("Audio unavailable: "+path,error);
    }
    static string Key(string value)=>value.StartsWith("Rachel350Z.Audio.",StringComparison.Ordinal)?value:Path.GetFullPath(value).ToUpperInvariant();
}
}
