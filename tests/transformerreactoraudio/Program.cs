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
    static WavData Sample(float seconds=8)=>new WavData{Channels=1,SampleRate=16000,Samples=new float[(int)Math.Round(16000*seconds)]};
    static void Tick(TransformerReactorAudio a,float progress=1,float dt=.01f,bool blocked=false,bool active=true,bool returning=false,float volume=1)=>a.Tick(dt,active,returning,progress,blocked,volume,Point);
    static void Adapter() {
        var car=new GameObject("car");int before=AudioSource.All.Count;
        var a=new TransformerReactorAudio(car,Sample(),_=>{});var s=AudioSource.All.Skip(before).Single();
        Check(a.Owns(s)&&!a.Owns(null)&&!a.Owns(new AudioSource()),"only owned reactor source is excluded from ordinary vehicle audio suppression");
        Check(s.loop&&s.spatialBlend==1&&s.dopplerLevel==0&&s.pitch==1&&s.priority==90,"reactor is one spatial low-priority loop without pitch or Doppler drift");
        Tick(a,blocked:true);Check(s.Starts==0&&!s.isPlaying,"restoring a robot while blocked does not start a queued layer");
        Tick(a);Check(s.Starts==1&&s.isPlaying&&Near(s.volume,.75f),"saved parked robot at full deployment starts its louder continuous reactor without replaying transformation");
        s.Advance(1.5f);int cursor=s.timeSamples;
        for(int i=0;i<100;i++)Tick(a);
        Check(s.Starts==1&&s.timeSamples==cursor&&s.Seeks==0,"ordinary idle keeps the live loop without restart or authoritative-phase seeks");
        s.Advance(16.25f);Check(s.isPlaying&&s.timeSamples==28000,"loop naturally wraps across multiple periods");
        Tick(a,volume:.3f);Check(Near(s.volume,.225f)&&s.Starts==1,"live master changes reactor volume without restarting");
        Tick(a,volume:0);s.Advance(.2f);Tick(a,volume:1);
        Check(s.Starts==1&&s.isPlaying&&Near(s.volume,.75f),"mute and unmute preserve continuous loop phase");
        Tick(a,blocked:true);int frozen=s.timeSamples;
        s.Advance(100);for(int i=0;i<20;i++)Tick(a,dt:5,blocked:true);
        Check(s.Pauses==1&&s.Paused&&!s.isPlaying&&s.volume==0&&s.timeSamples==frozen,"menu focus and streaming pauses preserve cursor and silence immediately");
        Tick(a);Check(s.UnPauses==1&&s.Starts==1&&s.timeSamples==frozen&&s.isPlaying,"unblocking unpauses at exact stored sample without backlog");
        Tick(a,dt:0);Check(s.Paused&&s.volume==0,"zero scaled game time pauses the actual Unity source");
        Tick(a,dt:float.NaN);Check(s.Pauses==2,"invalid game time remains paused without repeated Pause calls");
        Tick(a);Check(s.UnPauses==2&&s.Starts==1,"resuming valid game time unpauses existing loop");
        Tick(a,.775f);Check(Near(s.volume,.375f),"deployment midpoint uses smooth authoritative core phase");
        Tick(a,.775f,returning:true);Check(Near(s.volume,.375f)&&s.Starts==1,"reverse direction uses identical visible phase without artificial direction clock");
        Tick(a,.65f,dt:100,returning:true);float phase=(.65f-.55f)/.45f;
        Check(Near(s.volume,phase*phase*(3-2*phase)*.75f),"frame hitch cannot advance reactor envelope beyond visible core progress");
        Tick(a,.55f,returning:true);Check(!s.isPlaying&&!s.Paused&&s.volume==0,"hidden retracted core stops loop immediately");
        int starts=s.Starts;Tick(a,.2f);Tick(a,0);Check(s.Starts==starts,"car form never starts reactor loop");
        Tick(a,.56f);Check(s.isPlaying&&s.volume>0&&s.volume<.002f,"visible core begins with gentle phase-based fade in");
        Check(s.transform.position.x==Point.x&&s.transform.position.y==Point.y,"reactor source follows supplied moving core world position");
        a.Tick(.01f,true,false,1,false,1,new Vector3(float.NaN,0,0));
        Check(s.transform.position.x==car.transform.position.x,"invalid emitter position safely falls back to vehicle");
        Tick(a,volume:float.NaN);Check(s.volume==0&&s.isPlaying,"nonfinite master cannot poison Unity gain or restart timing");
        Tick(a,volume:5);Check(Near(s.volume,.75f)&&s.volume<=1,"master gain is bounded to the configured reactor layer ceiling");
        Tick(a,active:false);Check(!s.isPlaying&&s.volume==0,"deactivation immediately stops and clears reactor state");
        Tick(a);a.Stop();Check(!s.isPlaying&&s.volume==0,"explicit cleanup stop is immediate");
        Tick(a);Check(s.isPlaying,"restored active robot may restart sustained reactor after explicit stop");
        Tick(a,progress:float.NaN);Check(!s.isPlaying,"invalid deployment phase safely stops loop");
        Tick(a);a.Dispose();a.Dispose();Tick(a);
        Check(s.Destroyed&&s.clip==null&&!s.isPlaying&&!a.Owns(s),"idempotent disposal destroys emitter and releases clip");
        Check(AudioClip.All.All(c=>c.Destroyed),"all successful reactor clips are released");
    }
    static void Failures() {
        var car=new GameObject("car");int before=AudioSource.All.Count;
        using(var missing=new TransformerReactorAudio(car,null,_=>{})){Tick(missing);missing.Stop();}
        Check(AudioSource.All.Count==before,"missing optional reactor asset creates no emitter and is safe to tick");
        var bad=new List<WavData>{new WavData{Channels=2,SampleRate=16000,Samples=new float[100]},Sample(12.1f),Sample(.1f),new WavData{Channels=1,SampleRate=16000,Samples=null},new WavData{Channels=1,SampleRate=0,Samples=new float[16000]}};
        var nonfinite=Sample();nonfinite.Samples[0]=float.NaN;bad.Add(nonfinite);
        var clipped=Sample();clipped.Samples[0]=1.01f;bad.Add(clipped);
        foreach(var data in bad)using(var broken=new TransformerReactorAudio(car,data,_=>{}))Tick(broken);
        Check(AudioSource.All.Count==before,"corrupt stereo oversized undersized nonfinite and out-of-range samples are rejected");
        AudioClip.FailNextUpload=true;
        using(var broken=new TransformerReactorAudio(car,Sample(),_=>{}))Tick(broken);
        Check(AudioSource.All.Count==before&&AudioClip.All.All(c=>c.Destroyed),"failed Unity upload releases partial clip without emitter");
        var errors=new List<string>();
        using(var failed=new TransformerReactorAudio(car,Sample(),errors.Add)) {
            var s=AudioSource.All.Last();AudioSource.FailNextPlay=true;Tick(failed);Tick(failed);
            Check(s.Destroyed&&s.clip==null&&!failed.Owns(s)&&errors.Count(v=>v.Contains("disabled"))==1,"runtime failure releases owned resources and disables only reactor audio once");
        }
        Check(AudioClip.All.All(c=>c.Destroyed),"all optional-failure clips are released");
    }
    static void Main(){Adapter();Failures();Console.WriteLine(checks+" Transformer reactor audio checks passed; native final mix requires in-game listening.");}
}
