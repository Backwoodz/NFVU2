using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public TransformerAsset Transformer;
    public HudData Hud;
    public SpawnAudio Audio;
    public readonly List<string> Warnings=new List<string>();
    public static SpawnAssets Load(string folder,bool hummer,SpawnAssetCache cache,CancellationToken token,bool transformer=false) {
        var result=new SpawnAssets();
        result.Model=cache.Load(Path.Combine(folder,hummer?"hummer/h2.json":"body.json"),ModelFile.Load,token);
        result.Flames=result.Model;
        if(hummer)result.Optional(()=>result.Flames=cache.Load(Path.Combine(folder,"body.json"),ModelFile.Load,token),"Flight atlas",token);
        else {
            if(transformer&&File.Exists(Path.Combine(folder,"transformer/transformer.json")))
                result.Optional(()=>{
                    var manifest=Path.Combine(folder,"transformer/transformer.json");var payload=new FileInfo(Path.Combine(folder,"transformer/transformer.bin"));
                    var body=new FileInfo(Path.Combine(folder,"body.json"));
                    string key=manifest+"|"+body.Length+"|"+body.LastWriteTimeUtc.Ticks+"|"+payload.Length+"|"+payload.LastWriteTimeUtc.Ticks;
                    result.Transformer=cache.Load(manifest,p=>TransformerAsset.Load(Path.GetDirectoryName(p),body.FullName),token,key);
                },"Transformer prototype",token);
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
    public readonly Dictionary<string,WavData> TransformerSteps=new Dictionary<string,WavData>();
    public readonly Dictionary<string,WavData> TransformerTurns=new Dictionary<string,WavData>();
    public readonly WavData TransformerDeploy;
    public readonly WavData TransformerReactor;
    public readonly WavData TransformerReturn;
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
        // Optional short robot impacts are decoded on the same worker. Broken
        // clips must not turn an otherwise usable vehicle into a failed spawn.
        var robotFolder=Path.Combine(folder,"audio/transformer");
        if(!hummer&&Directory.Exists(robotFolder))try {
            foreach(var file in Directory.GetFiles(robotFolder,"step_*.wav").OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Take(16)) {
                token.ThrowIfCancellationRequested();
                Read(file,()=>cache.Load(file,p=>WavPcm16.Read(File.OpenRead(p),3),token),token);
                if(clips.TryGetValue(Key(file),out var data))TransformerSteps[Path.GetFileName(file)]=data;
            }
        }catch(OperationCanceledException){throw;}catch(Exception error){errors[Key(robotFolder)]=error;}
        if(!hummer&&Directory.Exists(robotFolder))try {
            foreach(var file in Directory.GetFiles(robotFolder,"turn_*.wav").OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Take(8)) {
                token.ThrowIfCancellationRequested();
                Read(file,()=>cache.Load(file,p=>WavPcm16.Read(File.OpenRead(p),3),token),token);
                if(clips.TryGetValue(Key(file),out var data))TransformerTurns[Path.GetFileName(file)]=data;
            }
        }catch(OperationCanceledException){throw;}catch(Exception error){errors[Key(robotFolder)]=error;}
        token.ThrowIfCancellationRequested();
        var deployFile=Path.Combine(robotFolder,"deploy.wav");
        if(!hummer&&File.Exists(deployFile)) {
            Read(deployFile,()=>cache.Load(deployFile,p=>WavPcm16.Read(File.OpenRead(p),6),token),token);
            if(clips.TryGetValue(Key(deployFile),out var data))TransformerDeploy=data;
        }
        token.ThrowIfCancellationRequested();
        var reactorFile=Path.Combine(robotFolder,"reactor.wav");
        if(!hummer&&File.Exists(reactorFile)) {
            Read(reactorFile,()=>cache.Load(reactorFile,p=>WavPcm16.Read(File.OpenRead(p),12),token),token);
            if(clips.TryGetValue(Key(reactorFile),out var data))TransformerReactor=data;
        }
        token.ThrowIfCancellationRequested();
        var returnFile=Path.Combine(robotFolder,"return.wav");
        if(!hummer&&File.Exists(returnFile)) {
            Read(returnFile,()=>cache.Load(returnFile,p=>WavPcm16.Read(File.OpenRead(p),6),token),token);
            if(clips.TryGetValue(Key(returnFile),out var data))TransformerReturn=data;
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
