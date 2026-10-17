using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Rachel350Z;
class Program {
    static int checks,advanced,disposed,stepThread;
    static void Check(bool condition,string label){if(!condition)throw new Exception(label);Console.WriteLine("PASS "+label);checks++;}
    static void Reject<T>(Action action,string label) where T:Exception {try{action();}catch(T){Check(true,label);return;}throw new Exception(label);}
    static IEnumerable<int> Steps(bool fail=false) {
        try {
            advanced++;stepThread=Thread.CurrentThread.ManagedThreadId;yield return StagedWork.NextFrame;
            advanced++;if(fail)throw new InvalidDataException("broken optional module");yield return 0;
            advanced++;
        }finally{disposed++;}
    }
    static void Pcm(string path,WavData source,float seconds) {
        int count=(int)(source.SampleRate*seconds);
        using(var file=File.Create(path))using(var writer=new BinaryWriter(file)) {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
            writer.Write((short)1);writer.Write((short)1);writer.Write(source.SampleRate);
            writer.Write(source.SampleRate*2);writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            for(int i=0;i<count;i++)writer.Write((short)(source.Samples[i%source.Samples.Length]*32767));
        }
    }
    static void TransformerAudio(WavData sample) {
        string root=Path.Combine(Path.GetTempPath(),"NFVU2-step-loading-"+Guid.NewGuid().ToString("N"));
        string folder=Path.Combine(root,"audio/transformer");Directory.CreateDirectory(folder);
        try {
            Pcm(Path.Combine(folder,"step_valid.wav"),sample,.15f);
            Pcm(Path.Combine(folder,"step_long.wav"),sample,3.1f);
            Pcm(Path.Combine(folder,"idle.wav"),sample,.1f);
            File.WriteAllText(Path.Combine(folder,"step_broken.wav"),"not a wave");
            var cache=new SpawnAssetCache();
            var audio=Task.Run(()=>new SpawnAudio(root,false,cache,CancellationToken.None)).GetAwaiter().GetResult();
            Check(audio.TransformerSteps.Count==1&&audio.TransformerSteps.ContainsKey("step_valid.wav"),"worker keeps valid short robot impact while excluding broken, long and unrelated clips");
            var clip=audio.TransformerSteps["step_valid.wav"];
            Check(clip.Channels==1&&clip.Samples.Length>0&&clip.Samples.Length<=clip.SampleRate*3,"worker provides decoded bounded mono robot samples");
            Reject<InvalidDataException>(()=>audio.FileData(Path.Combine(folder,"step_long.wav")),"long robot clip cannot enter runtime file cache");
            Reject<InvalidDataException>(()=>audio.FileData(Path.Combine(folder,"step_broken.wav")),"malformed robot clip remains isolated");
            var warm=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(ReferenceEquals(clip,warm.TransformerSteps["step_valid.wav"]),"repeat robot spawn shares worker-decoded samples");
            Check(new SpawnAudio(root,true,cache,CancellationToken.None).TransformerSteps.Count==0,"H2 does not load robot impacts");
            string empty=Path.Combine(root,"empty");Directory.CreateDirectory(empty);
            var missing=new SpawnAudio(empty,false,cache,CancellationToken.None);
            Check(missing.TransformerSteps.Count==0&&missing.Embedded("road.wav").Samples.Length>0,"missing robot audio preserves ordinary embedded vehicle audio");
            string bounded=Path.Combine(root,"bounded"),many=Path.Combine(bounded,"audio/transformer");Directory.CreateDirectory(many);
            for(int i=0;i<20;i++)Pcm(Path.Combine(many,"step_"+i.ToString("D2")+".wav"),sample,.02f);
            Check(new SpawnAudio(bounded,false,cache,CancellationToken.None).TransformerSteps.Count==16,"optional robot audio set is bounded to sixteen decoded impacts");
        }finally{Directory.Delete(root,true);}
    }
    static void Main(string[] args) {
        int caller=Thread.CurrentThread.ManagedThreadId;
        var work=new StagedWork(Steps());
        Check(advanced==0,"construction does not block the initial loading indicator");
        Check(work.Tick(1000)&&advanced==1,"frame boundary yields to the game");
        Check(work.Tick(0)&&advanced==2,"budget returns after one indivisible unit");
        Check(!work.Tick(1000)&&advanced==3&&disposed==1,"completion disposes the iterator once");
        Check(stepThread==caller,"Unity construction steps remain on the caller thread");
        work.Dispose();Check(disposed==1&&!work.Tick(1),"completed work cannot resume");
        work=new StagedWork(Steps());work.Tick(1000);work.Dispose();
        Check(disposed==2&&!work.Tick(1)&&advanced==4,"cancel releases partial work without running later steps");
        work=new StagedWork(Steps(true));work.Tick(1000);
        Reject<InvalidDataException>(()=>work.Tick(1000),"fatal failures propagate");
        Check(disposed==3&&!work.Tick(1),"failed iterator is disposed and cannot resume");
        int errors=0;work=new StagedWork(StagedWork.Optional(Steps(true),e=>errors++));work.Tick(1000);
        Check(!work.Tick(1000)&&errors==1&&disposed==4,"optional module failure rolls back and finishes");
        work=new StagedWork(StagedWork.Optional(Steps(),e=>errors++));work.Tick(1000);work.Dispose();
        Check(disposed==5&&errors==1,"cancelling nested work runs its cleanup");

        string path=Path.Combine(AppContext.BaseDirectory,"cache.txt");File.WriteAllText(path,"one");
        var cache=new SpawnAssetCache();int reads=0;Func<string,string> read=p=>{reads++;return File.ReadAllText(p);};
        Check(cache.Load(path,read,CancellationToken.None)=="one","cache reads initial asset");
        Check(cache.Load(path,read,CancellationToken.None)=="one"&&reads==1,"unchanged asset skips parsing");
        File.WriteAllText(path,"two");File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddSeconds(3));
        Check(cache.Load(path,read,CancellationToken.None)=="two"&&reads==2,"same size with changed timestamp reloads");
        File.WriteAllText(path,"longer");Check(cache.Load(path,read,CancellationToken.None)=="longer"&&reads==3,"changed length reloads");
        using(var cancel=new CancellationTokenSource()) {
            cancel.Cancel();Reject<OperationCanceledException>(()=>cache.Load(path,read,cancel.Token),"cancelled cache hit stays cancelled");
        }
        cache=new SpawnAssetCache();
        using(var cancel=new CancellationTokenSource())Reject<OperationCanceledException>(()=>cache.Load(path,p=>{cancel.Cancel();return "stale";},cancel.Token),"cancelled worker result is not published");
        Check(cache.Load(path,read,CancellationToken.None)=="longer","retry cannot receive a cancelled result");
        File.Delete(path);Reject<FileNotFoundException>(()=>cache.Load(path,read,CancellationToken.None),"deleted file cannot return stale cache data");

        var timer=Stopwatch.StartNew();cache=new SpawnAssetCache();int worker=caller;
        var assets=Task.Run(()=>{worker=Thread.CurrentThread.ManagedThreadId;return SpawnAssets.Load(args[0],false,cache,CancellationToken.None);}).GetAwaiter().GetResult();
        double cold=timer.Elapsed.TotalMilliseconds;timer.Restart();
        var warm=Task.Run(()=>SpawnAssets.Load(args[0],false,cache,CancellationToken.None)).GetAwaiter().GetResult();
        double cached=timer.Elapsed.TotalMilliseconds;
        Check(worker!=caller,"real payload is parsed on a background thread without Unity");
        Check(assets.Model.parts.Length>0&&assets.Interior.parts.Length>0&&assets.Turret.parts.Length>0&&assets.Hud.designs.Length==11,"body, cabin, turret and HUD load together");
        Check(assets.Warnings.Count==0,"installed optional model assets validate");
        Check(ReferenceEquals(assets.Model,warm.Model)&&ReferenceEquals(assets.Interior,warm.Interior)&&ReferenceEquals(assets.Hud,warm.Hud),"repeat spawn reuses parsed model data");
        Check(ReferenceEquals(assets.Audio.Embedded("road.wav"),warm.Audio.Embedded("road.wav")),"repeat spawn reuses decoded audio");
        TransformerAudio(assets.Audio.Embedded("road.wav"));
        Check(assets.Audio.FileData(Path.Combine(args[0],"audio","effects","nitro.wav")).Samples.Length>0,"native path separators resolve prefetched audio");
        Check(ReferenceEquals(assets.Audio.FileData(Path.Combine(args[0],"audio/effects/nitro.wav")),assets.Audio.FileData(Path.Combine(args[0],"audio","effects","nitro.wav"))),"mixed path separators share one audio key");
        var h2=SpawnAssets.Load(args[0],true,cache,CancellationToken.None);
        Check(h2.Model.parts.Length>0&&h2.Interior==null&&h2.Turret==null&&ReferenceEquals(h2.Flames,assets.Model),"H2 shares the flight atlas without re-reading 350Z data");
        Check(h2.Audio.FileData(Path.Combine(args[0],"audio/hummer/engine_1000.wav")).Samples.Length>0,"H2 engine audio decoded in preparation");
        string optional=Path.Combine(AppContext.BaseDirectory,"optional");Directory.CreateDirectory(optional);
        File.WriteAllText(Path.Combine(optional,"body.json"),"{\"parts\":[{\"name\":\"test\",\"vertices\":[{}, {}, {}],\"indices\":[0,1,2]}]}");
        var missing=SpawnAssets.Load(optional,false,new SpawnAssetCache(),CancellationToken.None);
        Check(missing.Model!=null&&missing.Interior==null&&missing.Hud==null&&missing.Warnings.Count==3,"missing optional files retain a usable base model");
        Directory.CreateDirectory(Path.Combine(optional,"interior"));File.WriteAllText(Path.Combine(optional,"interior/interior.json"),"broken");
        Check(SpawnAssets.Load(optional,false,new SpawnAssetCache(),CancellationToken.None).Interior==null,"corrupt optional cabin falls back");
        File.WriteAllText(Path.Combine(optional,"body.json"),"broken");
        Reject<Newtonsoft.Json.JsonException>(()=>SpawnAssets.Load(optional,false,new SpawnAssetCache(),CancellationToken.None),"corrupt required body fails the spawn");
        Console.WriteLine($"{checks} loading checks passed. Managed payload cold: {cold:0.0} ms, cached: {cached:0.0} ms. No Unity runtime/frame timing measured.");
    }
}
