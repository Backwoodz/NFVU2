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
            string deployPath=Path.Combine(folder,"deploy.wav");
            Pcm(deployPath,sample,5.3f);
            File.WriteAllText(Path.Combine(folder,"step_broken.wav"),"not a wave");
            Pcm(Path.Combine(folder,"turn_valid.wav"),sample,.2f);
            Pcm(Path.Combine(folder,"turn_long.wav"),sample,3.1f);
            File.WriteAllText(Path.Combine(folder,"turn_broken.wav"),"not a wave");
            var cache=new SpawnAssetCache();
            var audio=Task.Run(()=>new SpawnAudio(root,false,cache,CancellationToken.None)).GetAwaiter().GetResult();
            Check(audio.TransformerSteps.Count==1&&audio.TransformerSteps.ContainsKey("step_valid.wav"),"worker keeps valid short robot impact while excluding broken, long and unrelated clips");
            var clip=audio.TransformerSteps["step_valid.wav"];
            Check(clip.Channels==1&&clip.Samples.Length>0&&clip.Samples.Length<=clip.SampleRate*3,"worker provides decoded bounded mono robot samples");
            Check(audio.TransformerTurns.Count==1&&audio.TransformerTurns.ContainsKey("turn_valid.wav"),"worker keeps valid turn mechanic while excluding malformed, long and unrelated clips");
            var turn=audio.TransformerTurns["turn_valid.wav"];
            Check(turn.Channels==1&&turn.Samples.Length>0&&turn.Samples.Length<=turn.SampleRate*3,"worker provides decoded bounded mono turning samples");
            var deploy=audio.TransformerDeploy;
            Check(deploy!=null&&deploy.Channels==1&&deploy.Samples.Length==(int)(deploy.SampleRate*5.3f),"worker decodes the optional full deployment and its short release tail");
            Check(ReferenceEquals(deploy,audio.FileData(deployPath)),"deployment property and runtime file lookup share decoded data");
            Reject<InvalidDataException>(()=>audio.FileData(Path.Combine(folder,"step_long.wav")),"long robot clip cannot enter runtime file cache");
            Reject<InvalidDataException>(()=>audio.FileData(Path.Combine(folder,"step_broken.wav")),"malformed robot clip remains isolated");
            Reject<InvalidDataException>(()=>audio.FileData(Path.Combine(folder,"turn_long.wav")),"long turning clip cannot enter runtime file cache");
            Reject<InvalidDataException>(()=>audio.FileData(Path.Combine(folder,"turn_broken.wav")),"malformed turning clip remains isolated");
            var warm=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(ReferenceEquals(clip,warm.TransformerSteps["step_valid.wav"]),"repeat robot spawn shares worker-decoded samples");
            Check(ReferenceEquals(turn,warm.TransformerTurns["turn_valid.wav"]),"repeat robot spawn shares worker-decoded turning samples");
            Check(ReferenceEquals(deploy,warm.TransformerDeploy),"repeat robot spawn shares worker-decoded deployment samples");
            var hummer=new SpawnAudio(root,true,cache,CancellationToken.None);
            Check(hummer.TransformerSteps.Count==0&&hummer.TransformerTurns.Count==0&&hummer.TransformerDeploy==null,"H2 does not load robot impacts, turning mechanics or deployment");
            Reject<InvalidDataException>(()=>hummer.FileData(deployPath),"H2 excludes deployment from its runtime file data");
            File.WriteAllText(deployPath,"not a wave");
            var brokenDeploy=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(brokenDeploy.TransformerDeploy==null&&brokenDeploy.TransformerSteps.Count==1&&brokenDeploy.TransformerTurns.Count==1,"malformed optional deployment preserves approved steps and turning audio");
            Reject<InvalidDataException>(()=>brokenDeploy.FileData(deployPath),"malformed deployment cannot reuse old cached samples");
            Pcm(deployPath,sample,6.1f);
            var longDeploy=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(longDeploy.TransformerDeploy==null&&longDeploy.Embedded("road.wav").Samples.Length>0,"deployment beyond six seconds is optional and leaves ordinary vehicle audio usable");
            Reject<InvalidDataException>(()=>longDeploy.FileData(deployPath),"overlong deployment cannot enter runtime file data");
            Pcm(deployPath,sample,6);
            var boundedDeploy=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(boundedDeploy.TransformerDeploy!=null&&boundedDeploy.TransformerDeploy.Samples.Length==sample.SampleRate*6,"deployment accepts the six-second worker limit");
            File.Delete(deployPath);
            var noDeploy=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(noDeploy.TransformerDeploy==null&&noDeploy.TransformerSteps.Count==1&&noDeploy.TransformerTurns.Count==1,"deleted optional deployment cannot return stale data or remove movement audio");
            File.Delete(Path.Combine(folder,"turn_valid.wav"));
            File.Delete(Path.Combine(folder,"turn_long.wav"));
            File.Delete(Path.Combine(folder,"turn_broken.wav"));
            var stepsOnly=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(stepsOnly.TransformerTurns.Count==0&&stepsOnly.TransformerSteps.Count==1&&ReferenceEquals(clip,stepsOnly.TransformerSteps["step_valid.wav"]),"missing turning set preserves approved footstep data without stale turn cache entries");
            string empty=Path.Combine(root,"empty");Directory.CreateDirectory(empty);
            var missing=new SpawnAudio(empty,false,cache,CancellationToken.None);
            Check(missing.TransformerSteps.Count==0&&missing.TransformerTurns.Count==0&&missing.TransformerDeploy==null&&missing.Embedded("road.wav").Samples.Length>0,"missing robot audio preserves ordinary embedded vehicle audio");
            string bounded=Path.Combine(root,"bounded"),many=Path.Combine(bounded,"audio/transformer");Directory.CreateDirectory(many);
            for(int i=0;i<20;i++)Pcm(Path.Combine(many,"step_"+i.ToString("D2")+".wav"),sample,.02f);
            for(int i=0;i<12;i++)Pcm(Path.Combine(many,"turn_"+i.ToString("D2")+".wav"),sample,.02f);
            var limited=new SpawnAudio(bounded,false,cache,CancellationToken.None);
            Check(limited.TransformerSteps.Count==16,"optional robot audio set is bounded to sixteen decoded impacts");
            Check(limited.TransformerTurns.Count==8&&limited.TransformerTurns.ContainsKey("turn_07.wav")&&!limited.TransformerTurns.ContainsKey("turn_08.wav"),"turn mechanics have an independent deterministic limit of eight decoded clips");
        }finally{Directory.Delete(root,true);}
    }
    static void ReactorAudio(WavData sample) {
        string root=Path.Combine(Path.GetTempPath(),"NFVU2-reactor-loading-"+Guid.NewGuid().ToString("N"));
        string folder=Path.Combine(root,"audio/transformer");Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"reactor.wav");var cache=new SpawnAssetCache();
        try {
            Pcm(path,sample,8);
            var audio=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(audio.TransformerReactor!=null&&audio.TransformerReactor.Channels==1&&audio.TransformerReactor.Samples.Length==sample.SampleRate*8,"worker decodes bounded optional reactor loop");
            Check(ReferenceEquals(audio.TransformerReactor,audio.FileData(path)),"reactor property and file lookup share decoded data");
            Check(ReferenceEquals(audio.TransformerReactor,new SpawnAudio(root,false,cache,CancellationToken.None).TransformerReactor),"warm spawn shares worker-decoded reactor data");
            var hummer=new SpawnAudio(root,true,cache,CancellationToken.None);
            Check(hummer.TransformerReactor==null,"H2 does not load the Transformer reactor");
            Reject<InvalidDataException>(()=>hummer.FileData(path),"H2 file lookup excludes reactor data");
            File.WriteAllText(path,"not a wave");
            var broken=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(broken.TransformerReactor==null&&broken.Embedded("road.wav").Samples.Length>0,"invalid reactor loop cannot prevent ordinary audio loading");
            Reject<InvalidDataException>(()=>broken.FileData(path),"broken reactor cannot reuse a cached valid sample");
            Pcm(path,sample,12.1f);
            Check(new SpawnAudio(root,false,cache,CancellationToken.None).TransformerReactor==null,"reactor over twelve seconds is rejected as optional");
            Pcm(path,sample,12);
            Check(new SpawnAudio(root,false,cache,CancellationToken.None).TransformerReactor.Samples.Length==sample.SampleRate*12,"reactor accepts the twelve-second worker bound");
            File.Delete(path);Pcm(Path.Combine(folder,"step_valid.wav"),sample,.1f);Pcm(Path.Combine(folder,"deploy.wav"),sample,5.3f);
            var missing=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(missing.TransformerReactor==null&&missing.TransformerSteps.Count==1&&missing.TransformerDeploy!=null,"missing reactor preserves deployment and movement samples without stale reactor data");
        }finally{Directory.Delete(root,true);}
    }
    static void ReturnAudio(WavData sample) {
        string root=Path.Combine(Path.GetTempPath(),"NFVU2-return-loading-"+Guid.NewGuid().ToString("N"));
        string folder=Path.Combine(root,"audio/transformer");Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"return.wav");var cache=new SpawnAssetCache();
        try {
            Pcm(Path.Combine(folder,"deploy.wav"),sample,5.3f);
            Pcm(Path.Combine(folder,"reactor.wav"),sample,8);
            Pcm(Path.Combine(folder,"step_valid.wav"),sample,.1f);
            Pcm(Path.Combine(folder,"turn_valid.wav"),sample,.2f);
            var missing=new SpawnAudio(root,false,cache,CancellationToken.None);
            var deploy=missing.TransformerDeploy;var reactor=missing.TransformerReactor;
            var step=missing.TransformerSteps["step_valid.wav"];var turn=missing.TransformerTurns["turn_valid.wav"];
            Check(missing.TransformerReturn==null&&deploy!=null&&reactor!=null,"missing return soundtrack preserves deployment and reactor audio");
            Reject<InvalidDataException>(()=>missing.FileData(path),"missing return soundtrack is absent from runtime file data");
            Pcm(path,sample,5.3f);
            var audio=Task.Run(()=>new SpawnAudio(root,false,cache,CancellationToken.None)).GetAwaiter().GetResult();
            var clip=audio.TransformerReturn;
            Check(clip!=null&&clip.Channels==1&&clip.Samples.Length==(int)(sample.SampleRate*5.3f),"worker decodes the five-second layered return soundtrack and its bounded closing ring");
            Check(ReferenceEquals(clip,audio.FileData(path)),"return property and runtime file lookup share decoded data");
            Check(ReferenceEquals(deploy,audio.TransformerDeploy)&&ReferenceEquals(reactor,audio.TransformerReactor)&&ReferenceEquals(step,audio.TransformerSteps["step_valid.wav"])&&ReferenceEquals(turn,audio.TransformerTurns["turn_valid.wav"]),"loading return soundtrack leaves approved audio samples unchanged");
            Check(ReferenceEquals(clip,new SpawnAudio(root,false,cache,CancellationToken.None).TransformerReturn),"warm spawn shares worker-decoded return samples");
            var hummer=new SpawnAudio(root,true,cache,CancellationToken.None);
            Check(hummer.TransformerReturn==null&&hummer.TransformerDeploy==null&&hummer.TransformerReactor==null&&hummer.TransformerSteps.Count==0&&hummer.TransformerTurns.Count==0,"H2 excludes return audio together with all other Transformer layers");
            Reject<InvalidDataException>(()=>hummer.FileData(path),"H2 cannot look up cached return audio from an earlier 350Z spawn");
            File.WriteAllText(path,"not a wave");
            var broken=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(broken.TransformerReturn==null&&ReferenceEquals(deploy,broken.TransformerDeploy)&&ReferenceEquals(reactor,broken.TransformerReactor)&&ReferenceEquals(step,broken.TransformerSteps["step_valid.wav"])&&ReferenceEquals(turn,broken.TransformerTurns["turn_valid.wav"]),"malformed optional return audio preserves deployment, reactor and movement clips");
            Reject<InvalidDataException>(()=>broken.FileData(path),"malformed return audio cannot reuse previously cached valid samples");
            Pcm(path,sample,6.1f);
            var overlong=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(overlong.TransformerReturn==null&&overlong.Embedded("road.wav").Samples.Length>0,"return soundtrack over six seconds remains optional and preserves ordinary vehicle audio");
            Reject<InvalidDataException>(()=>overlong.FileData(path),"overlong return soundtrack cannot enter runtime file data");
            Pcm(path,sample,6);
            var bounded=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(bounded.TransformerReturn!=null&&bounded.TransformerReturn.Samples.Length==sample.SampleRate*6,"return soundtrack accepts the six-second worker limit");
            File.Delete(path);
            var deleted=new SpawnAudio(root,false,cache,CancellationToken.None);
            Check(deleted.TransformerReturn==null&&ReferenceEquals(deploy,deleted.TransformerDeploy)&&ReferenceEquals(reactor,deleted.TransformerReactor)&&ReferenceEquals(step,deleted.TransformerSteps["step_valid.wav"])&&ReferenceEquals(turn,deleted.TransformerTurns["turn_valid.wav"]),"deleted return soundtrack cannot remove approved layers or return stale optional data");
            Reject<InvalidDataException>(()=>deleted.FileData(path),"deleted return soundtrack cannot be retrieved from a warm runtime cache");
        }finally{Directory.Delete(root,true);}
    }
    static void MinigunAssets(string folder,SpawnAssetCache cache) {
        if(!File.Exists(Path.Combine(folder,"minigun/minigun.json"))||!File.Exists(Path.Combine(folder,"transformer/transformer.json"))) {
            Console.WriteLine("Optional shoulder minigun assets absent; worker asset checks skipped.");return;
        }
        var robot=Task.Run(()=>SpawnAssets.Load(folder,false,cache,CancellationToken.None,true)).GetAwaiter().GetResult();
        Check(robot.Transformer!=null&&robot.Minigun!=null&&robot.MinigunRig!=null,"worker loads actual robot, original minigun model and validated barrel rig together");
        Check(robot.Warnings.Count==0,"original minigun assets pass optional worker validation");
        var warm=SpawnAssets.Load(folder,false,cache,CancellationToken.None,true);
        Check(ReferenceEquals(robot.Minigun,warm.Minigun)&&ReferenceEquals(robot.MinigunRig,warm.MinigunRig),"repeat robot spawn shares parsed minigun mesh and rig data");
        foreach(string name in new[]{"spin_start","spin_loop","fire_start","fire_loop","fire_tail","reload"}) {
            string path=Path.Combine(folder,"minigun",name+".wav");
            var clip=robot.Audio.FileData(path);
            Check(clip.Channels==1&&clip.Samples.Length>0&&clip.Samples.Length<=clip.SampleRate*15,"worker decodes bounded original minigun "+name);
            Check(ReferenceEquals(clip,warm.Audio.FileData(path)),"repeat robot spawn reuses minigun audio "+name);
        }
        string temporary=Path.Combine(Path.GetTempPath(),"NFVU2-minigun-rig-"+Guid.NewGuid().ToString("N")+".json");
        try {
            File.WriteAllText(temporary,"{\"schemaVersion\":1,\"forward\":\"+Z\",\"up\":\"+Y\",\"units\":\"metres\",\"barrelAxis\":{\"z\":1},\"muzzle\":{\"z\":0.6}}");
            Check(TransformerMinigunRig.Load(temporary).muzzle.z==.6f,"managed minigun rig reader accepts a bounded forward muzzle");
            File.WriteAllText(temporary,File.ReadAllText(temporary).Replace("\"z\":1","\"z\":0"));
            Reject<InvalidDataException>(()=>TransformerMinigunRig.Load(temporary),"invalid minigun spin axis cannot reach runtime construction");
        } finally {File.Delete(temporary);}
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
        ReactorAudio(assets.Audio.Embedded("road.wav"));
        ReturnAudio(assets.Audio.Embedded("road.wav"));
        MinigunAssets(args[0],cache);
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
