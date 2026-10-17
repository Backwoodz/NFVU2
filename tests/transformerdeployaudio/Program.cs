using System;
using System.Collections.Generic;
using System.Linq;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static readonly Vector3 Point=new Vector3(1,3,5);
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static WavData Sample(float seconds=5.3f)=>new WavData{Channels=1,SampleRate=16000,Samples=new float[(int)Math.Round(16000*seconds)]};
    static void Tick(TransformerDeployAudio a,float progress,float dt=.01f,bool blocked=false,bool active=true,bool returning=false,float volume=1)=>a.Tick(dt,active,returning,progress,blocked,volume,Point);
    static void Model() {
        var m=new TransformerDeploySoundModel(5.3f);
        m.Step(.01f,true,false,1,false,1);Check(!m.Playing,"saved parked robot does not replay deployment");
        m.Step(.01f,true,false,.002f,false,1);Check(m.Playing&&!m.Paused&&Near(m.Position,.01f),"first live forward frame starts at authoritative phase");
        m.Step(.01f,true,false,.002f,false,1);Check(m.Playing&&m.Paused,"stalled authoritative progress freezes soundtrack");
        m.Step(.01f,true,false,.004f,false,.5f);Check(!m.Paused&&Near(m.Gain,.5f),"progress resumes with live master gain");
        m.Step(.7f,true,false,.144f,false,1);Check(!m.Paused&&Near(m.Position,.72f),"hitch advances directly to authoritative position");
        m.Step(.01f,true,true,.14f,false,1);Check(!m.Playing,"returning state cancels the forward layer immediately");
        m.Step(.01f,true,false,.998f,false,1);m.Step(.01f,true,false,1,false,1);
        Check(m.Playing&&Near(m.Position,5),"a live completed deployment retains only its recorded ring");
        m.Step(.15f,true,false,1,false,1);Check(m.Playing&&Near(m.Position,5.15f),"ending ring follows bounded elapsed time after end pose");
        m.Step(100,true,false,1,true,1);Check(m.Playing&&m.Paused&&Near(m.Position,5.15f),"blocked ending ring freezes rather than expiring or advancing");
        m.Step(.151f,true,false,1,false,1);Check(!m.Playing,"ending ring expires after at most 300ms of live time");
        m.Step(.01f,true,false,1,false,1);Check(!m.Playing,"parked robot never restarts an expired ring");
        m.Step(.01f,true,false,.9f,false,1);m.Step(.8f,true,false,1,false,1);
        Check(m.Playing&&Near(m.Position,5),"raw hitch dt does not skip final ring after clamped animation reaches its end pose");
        m.Reset();m.Step(.01f,true,false,.9f,true,1);m.Step(.01f,true,false,1,true,1);m.Step(.01f,true,false,1,false,1);
        Check(!m.Playing,"completion observed while blocked does not create a ring on resume");
        m.Reset();m.Step(.01f,true,false,.5f,false,1);m.Step(.01f,false,false,.5f,false,1);
        Check(!m.Playing&&m.Gain==0,"inactive vehicle clears live state");
        m.Step(.01f,true,false,.5f,false,1);m.Step(0,true,false,.5f,false,1);Check(m.Paused,"zero game time pauses an active transformation");
        m.Step(float.NaN,true,false,.5f,false,float.NaN);Check(m.Paused&&m.Gain==0,"invalid timing and volume cannot poison live state");
        m.Step(.01f,true,false,float.NaN,false,1);Check(!m.Playing,"invalid progress clears live state safely");
        m=new TransformerDeploySoundModel(6);m.Step(.01f,true,false,.998f,false,1);m.Step(.01f,true,false,1,false,1);m.Step(.31f,true,false,1,false,1);
        Check(!m.Playing,"longer optional WAV cannot lengthen the 300ms ring limit");
    }
    static void Adapter() {
        var car=new GameObject("car");int before=AudioSource.All.Count;
        var a=new TransformerDeployAudio(car,Sample(),_=>{});var s=AudioSource.All.Skip(before).Single();
        Check(a.Owns(s)&&!a.Owns(null)&&!a.Owns(new AudioSource()),"adapter reports only its one owned source for vehicle suppression exclusion");
        Check(!s.loop&&s.spatialBlend==1&&s.dopplerLevel==0&&s.pitch==1,"deployment uses a spatial source with no loop pitch or Doppler drift");
        Tick(a,1);Tick(a,1);Check(s.Starts==0&&!s.isPlaying,"restored parked robot has no soundtrack or closing cue");
        Tick(a,.003f);Check(s.Starts==1&&s.isPlaying&&s.timeSamples==240,"real deployment starts once at current authoritative sample");
        int seeks=s.Seeks;s.Advance(.01f);Tick(a,.005f);
        Check(s.Starts==1&&s.Seeks==seeks,"normal synchronized frames do not repeatedly seek or restart Unity playback");
        s.Advance(.01f);Tick(a,.007f,volume:.4f);Check(Near(s.volume,.4f),"master gain updates an already playing soundtrack");
        s.Advance(.01f);Tick(a,.009f,volume:0);Check(s.isPlaying&&s.volume==0&&s.Starts==1,"live mute changes gain without restarting timing");
        s.Advance(.01f);Tick(a,.011f,volume:.8f);Check(Near(s.volume,.8f)&&s.Starts==1,"unmute restores live gain at the existing phase");
        s.Advance(.01f);Tick(a,.013f,blocked:true);int pausedSample=s.timeSamples;
        s.Advance(5);for(int i=0;i<100;i++)Tick(a,.013f,blocked:true);
        Check(!s.isPlaying&&s.Paused&&s.Pauses==1&&s.timeSamples==pausedSample,"menu focus and streaming blocks pause once and freeze Unity sample cursor");
        Tick(a,.015f);Check(s.isPlaying&&s.UnPauses==1&&s.Starts==1&&s.timeSamples==1200,"resume seeks authoritative phase then unpauses without duplicate start");
        s.Advance(1.2f);Tick(a,.12f,dt:.525f);Check(s.timeSamples==9600&&s.Starts==1,"frame hitch corrects audio drift to authoritative animation clock");
        s.Advance(.02f);Tick(a,.122f);Check(s.timeSamples==9920,"small audio drift remains within 50ms without jittering seeks");
        Tick(a,.122f,dt:0);Check(s.Paused,"zero scaled game time actually pauses Unity source");
        Tick(a,.124f);Check(s.isPlaying&&s.timeSamples==9920&&s.Starts==1,"zero-time resume stays aligned without replay");
        s.Advance(20);Check(!s.isPlaying,"independent Unity audio clock can naturally finish during a game frame hitch");
        Tick(a,.144f,dt:1);Check(s.isPlaying&&s.Starts==2&&s.timeSamples==11520,"naturally finished source recovers once at authoritative progress after hitch");
        s.Advance(.01f);Tick(a,.146f);Check(s.Starts==2,"recovered source does not duplicate starts on normal subsequent frames");
        Tick(a,.122f,returning:true);Check(!s.isPlaying&&!s.Paused&&s.volume==0,"beginning reverse conversion immediately cancels the forward soundtrack");
        int starts=s.Starts;Tick(a,.5f,returning:true);Tick(a,0,returning:true);Check(s.Starts==starts,"returning never starts the forward texture at any progress");
        Tick(a,0,active:false);Tick(a,.002f);Check(s.Starts==starts+1&&s.timeSamples==160,"a later true forward transformation starts a fresh synchronized play");
        Tick(a,.998f,dt:4.98f);s.Advance(.01f);Tick(a,1);Check(s.isPlaying&&s.timeSamples==80000,"reaching end pose follows the actual recorded closing ring");
        Tick(a,1,dt:.1f);Check(s.isPlaying&&s.timeSamples==81600,"closing ring advances through at most three tenths of a second");
        Tick(a,1,blocked:true);Tick(a,1,dt:5,blocked:true);Check(s.Paused,"closing ring also respects gameplay blocking");
        Tick(a,1,dt:.1f);Check(s.isPlaying&&s.timeSamples==83200,"ring resumes at frozen ring phase without starting again");
        Tick(a,1,dt:.101f);Check(!s.isPlaying&&s.volume==0,"actual Unity source stops when bounded ring finishes");
        starts=s.Starts;for(int i=0;i<100;i++)Tick(a,1);Check(s.Starts==starts,"idle ticks cannot replay completed closing ring");
        Tick(a,.002f);Tick(a,.004f,active:false);Check(!s.isPlaying,"deactivation and removal cancel source immediately");
        Tick(a,.002f);a.Stop();starts=s.Starts;Tick(a,1);Check(!s.isPlaying&&s.Starts==starts,"explicit stop followed by world restore cannot replay deployment");
        Tick(a,.002f);Tick(a,.004f);Check(s.isPlaying,"advancing forward clock remains audible");
        Tick(a,.004f);Check(s.Paused,"defensive stalled-clock pause protects against asynchronous audio drift");
        Tick(a,.006f);Check(s.isPlaying&&s.Starts==starts+1,"stalled-clock resume unpauses without extra starts");
        Check(s.transform.position.x==Point.x&&s.transform.position.y==Point.y,"sound emitter follows supplied thorax world position");
        a.Tick(.01f,true,false,.008f,false,1,new Vector3(float.NaN,0,0));
        Check(s.transform.position.x==car.transform.position.x,"invalid emitter position falls back to vehicle");
        a.Dispose();a.Dispose();Tick(a,.1f);Check(s.Destroyed&&s.clip==null&&!s.isPlaying&&!a.Owns(s),"idempotent disposal destroys owned source and releases clip");
        Check(AudioClip.All.All(c=>c.Destroyed),"all valid clip objects are released");
    }
    static void Failures() {
        var car=new GameObject("car");int before=AudioSource.All.Count;
        using(var missing=new TransformerDeployAudio(car,null,_=>{})){Tick(missing,.1f);missing.Stop();}
        Check(AudioSource.All.Count==before,"missing optional soundtrack allocates no source and is safe to tick");
        var bad=new List<WavData>{new WavData{Channels=2,SampleRate=16000,Samples=new float[100]},Sample(6.1f),new WavData{Channels=1,SampleRate=16000,Samples=new[]{float.NaN}},new WavData{Channels=1,SampleRate=16000,Samples=new[]{1.01f}},new WavData{Channels=1,SampleRate=0,Samples=new float[10]},new WavData{Channels=1,SampleRate=16000,Samples=new float[0]}};
        foreach(var data in bad)using(var broken=new TransformerDeployAudio(car,data,_=>{}))Tick(broken,.1f);
        Check(AudioSource.All.Count==before,"corrupt stereo oversized nonfinite and out-of-range optional PCM are rejected");
        AudioClip.FailNextUpload=true;
        using(var broken=new TransformerDeployAudio(car,Sample(),_=>{}))Tick(broken,.1f);
        Check(AudioSource.All.Count==before&&AudioClip.All.All(c=>c.Destroyed),"failed Unity upload releases partial resources and creates no source");
        using(var shortAudio=new TransformerDeployAudio(car,Sample(.5f),_=>{})) {
            var s=AudioSource.All.Last();Tick(shortAudio,.01f);Tick(shortAudio,.2f);int starts=s.Starts;
            for(int i=0;i<20;i++)Tick(shortAudio,.3f+i*.01f);
            Check(s.Starts==starts&&!s.isPlaying,"short optional texture completes without last-sample replay spam");
        }
        using(var pausedEnded=new TransformerDeployAudio(car,Sample(),_=>{})) {
            var s=AudioSource.All.Last();Tick(pausedEnded,.2f);s.Advance(10);
            Tick(pausedEnded,.2f,blocked:true);Tick(pausedEnded,.202f);
            Check(s.isPlaying&&s.Starts==2&&s.UnPauses==1&&s.timeSamples==16160,"resume recovers an already naturally ended source when UnPause alone cannot play it");
            s.Advance(.01f);Tick(pausedEnded,.204f);
            Check(s.Starts==2,"recovered paused source remains one live soundtrack on subsequent frames");
        }
        var errors=new List<string>();
        using(var failedRuntime=new TransformerDeployAudio(car,Sample(),errors.Add)) {
            var s=AudioSource.All.Last();AudioSource.FailNextPlay=true;Tick(failedRuntime,.2f);Tick(failedRuntime,.3f);
            Check(s.Destroyed&&s.clip==null&&!failedRuntime.Owns(s)&&errors.Count(v=>v.Contains("disabled"))==1,"runtime Unity failure releases owned resources and disables only optional deployment audio once");
        }
        Check(AudioClip.All.All(c=>c.Destroyed),"optional failure and short-audio cleanup release all clips");
    }
    static void Main(){Model();Adapter();Failures();Console.WriteLine(checks+" Transformer deployment audio checks passed; native final mix still requires in-game listening.");}
}
