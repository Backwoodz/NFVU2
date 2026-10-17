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
    static WavData Sample(float seconds=4.2f)=>new WavData{Channels=1,SampleRate=16000,Samples=new float[(int)Math.Round(16000*seconds)]};
    static void Tick(TransformerReturnAudio a,float progress,float dt=.01f,bool blocked=false,bool active=true,bool returning=true,float volume=1)=>a.Tick(dt,active,returning,progress,blocked,volume,Point);
    static void Model() {
        var m=new TransformerReturnSoundModel(4.2f);
        m.Step(.01f,true,false,1,false,1);Check(!m.Playing,"parked or pending ReturnRequested robot does not start reverse audio");
        m.Step(.01f,true,false,.5f,false,1);Check(!m.Playing,"forward animation cannot start reverse layer");
        m.Step(.01f,true,false,0,false,1);Check(!m.Playing,"parked car cannot start reverse layer");
        m.Step(.01f,true,true,1,false,1);Check(m.Playing&&!m.Paused&&Near(m.Position,0),"actual Returning begins immediately at robot end pose");
        m.Step(.01f,true,true,.998f,false,.5f);Check(m.Playing&&!m.Paused&&Near(m.Position,.01f)&&Near(m.Gain,.5f),"decreasing authoritative progress supplies reverse clock and live gain");
        m.Step(1,true,true,.998f,true,1);Check(m.Paused,"blocked reverse playback pauses");
        m.Step(.01f,true,true,.998f,false,1);Check(m.Paused,"stalled progress also pauses the reverse clock");
        m.Step(.01f,true,true,.996f,false,1);Check(!m.Paused,"decreasing progress resumes");
        m.Step(0,true,true,.994f,false,1);Check(m.Paused,"zero scaled time pauses reverse playback");
        m.Step(float.NaN,true,true,.992f,false,float.NaN);Check(m.Paused&&m.Gain==0,"nonfinite time and gain are safe");
        m.Step(.01f,true,true,.161f,false,1);Check(m.Playing&&Near(m.Position,4.195f),"main layer plays just before selected 4.2-second end");
        m.Step(.01f,true,true,.16f,false,1);Check(!m.Playing,"main layer ends at 4.2 seconds leaving final 800ms free");
        m.Step(.01f,true,true,.15f,false,1);m.Step(.01f,true,true,.01f,false,1);Check(!m.Playing,"later reverse phases cannot replay the last sample");
        m.Step(.01f,true,true,.9f,false,1);Check(!m.Playing,"finished cycle does not replay on stray backwards progress");
        m.Step(.01f,true,false,1,false,1);m.Step(.01f,true,true,1,false,1);Check(m.Playing&&m.Position==0,"new actual return cycle starts cleanly");
        m.Step(.01f,false,true,.8f,false,1);Check(!m.Playing&&m.Gain==0,"deactivation clears return state");
        m.Step(.01f,true,true,float.NaN,false,1);Check(!m.Playing,"invalid progress cannot become a seek");
        m=new TransformerReturnSoundModel(6);m.Step(.01f,true,true,.1f,false,1);Check(m.Playing&&Near(m.Position,4.5f),"longer layered return can accompany final mechanical accents");
        m=new TransformerReturnSoundModel(.5f);m.Step(.01f,true,true,.8f,false,1);Check(!m.Playing,"short optional clip also stops at its own duration");
    }
    static void Adapter() {
        var car=new GameObject("car");int before=AudioSource.All.Count;
        var a=new TransformerReturnAudio(car,Sample(),_=>{});var s=AudioSource.All.Skip(before).Single();
        Check(a.Owns(s)&&!a.Owns(null)&&!a.Owns(new AudioSource()),"only dedicated return voice is excluded from generic capture");
        Check(!s.loop&&!s.playOnAwake&&s.spatialBlend==1&&s.pitch==1&&s.dopplerLevel==0&&s.priority==60,"fixed spatial reverse voice has no pitch loop or Doppler change");
        Tick(a,1,returning:false);Tick(a,.5f,returning:false);Tick(a,0,active:false,returning:false);
        Check(s.Starts==0,"parked restore and forward phases remain silent");
        // Pending ReturnRequested during braking/foot-set/terrain alignment is
        // represented by returning=false, even though the robot remains active.
        for(int i=0;i<50;i++)Tick(a,1,returning:false);
        Check(s.Starts==0,"braking foot-set and terrain wait cannot start main reverse texture");
        Tick(a,1);Check(s.Starts==1&&s.isPlaying&&s.timeSamples==0,"Returning true starts immediately rather than waiting one second");
        int seeks=s.Seeks;s.Advance(.01f);Tick(a,.998f);
        Check(s.Starts==1&&s.Seeks==seeks,"normal reverse frames do not restart or jitter-seek");
        s.Advance(.01f);Tick(a,.996f,volume:.4f);Check(Near(s.volume,.4f),"master volume adjusts live reverse layer");
        s.Advance(.01f);Tick(a,.994f,volume:0);Check(s.isPlaying&&s.volume==0&&s.Starts==1,"mute leaves authoritative playback phase intact");
        s.Advance(.01f);Tick(a,.992f,volume:.8f);Check(Near(s.volume,.8f)&&s.Starts==1,"unmute does not duplicate the growl");
        Tick(a,.992f,blocked:true);int frozen=s.timeSamples;
        s.Advance(20);for(int i=0;i<100;i++)Tick(a,.992f,blocked:true);
        Check(s.Paused&&!s.isPlaying&&s.Pauses==1&&s.timeSamples==frozen,"blocking pauses once and freezes independent Unity sample cursor");
        Tick(a,.99f);Check(s.isPlaying&&s.Starts==1&&s.UnPauses==1&&s.timeSamples==800,"resume unpauses at current reverse phase without new start");
        s.Advance(1.2f);Tick(a,.88f,dt:.55f);Check(s.Starts==1&&s.timeSamples==9600,"frame hitch seeks back to authoritative bounded animation clock");
        Tick(a,.88f,dt:0);frozen=s.timeSamples;s.Advance(5);
        Check(s.Paused&&s.timeSamples==frozen,"zero scaled time freezes actual playback samples");
        Tick(a,.878f);Check(s.isPlaying&&s.Starts==1&&s.timeSamples==9760,"scaled-time resume uses current phase");
        s.Advance(20);Tick(a,.85f,dt:1);
        Check(s.isPlaying&&s.Starts==2&&s.timeSamples==12000,"naturally finished audio during hitch recovers once before selected clip end");
        s.Advance(.01f);Tick(a,.848f);Check(s.Starts==2,"recovered source stays a single voice on following frames");
        Tick(a,.161f);Check(s.isPlaying&&s.timeSamples==67120,"last valid main phase remains audible");
        s.Advance(.01f);Tick(a,.16f);Check(!s.isPlaying&&s.volume==0,"clip boundary stops the actual source");
        int starts=s.Starts;for(int i=0;i<100;i++)Tick(a,.15f-i*.001f);Tick(a,0);
        Check(s.Starts==starts&&!s.isPlaying,"main completion and car endpoint never repeatedly restart final sample");
        Tick(a,1,returning:false);Tick(a,1);Check(s.Starts==starts+1&&s.timeSamples==0,"second actual return begins a fresh selected segment");
        Tick(a,.98f,returning:false);Check(!s.isPlaying&&!s.Paused&&s.volume==0,"immediate forward direction change stops reverse audio");
        Tick(a,.9f);Tick(a,.8f,active:false);Check(!s.isPlaying,"deactivation immediately stops return texture");
        Tick(a,.9f);a.Stop();starts=s.Starts;Tick(a,1,returning:false);
        Check(!s.isPlaying&&s.Starts==starts,"explicit stop followed by parked restore stays silent");
        Tick(a,.8f);Tick(a,.8f);Check(s.Paused,"stalled progress pauses actual source");
        Tick(a,.798f);Check(s.isPlaying&&s.Starts==starts+1,"stalled phase resumes rather than starting another voice");
        Check(s.transform.position.x==Point.x&&s.transform.position.y==Point.y,"emitter follows supplied current thorax or body pose");
        a.Tick(.01f,true,true,.796f,false,1,new Vector3(float.NaN,0,0));
        Check(s.transform.position.x==car.transform.position.x,"invalid emitter falls back to vehicle body");
        a.Dispose();a.Dispose();Tick(a,.5f);
        Check(s.Destroyed&&s.clip==null&&!s.isPlaying&&!a.Owns(s),"idempotent disposal releases source clip and ownership");
        Check(AudioClip.All.All(c=>c.Destroyed),"successful adapter path releases every clip");
    }
    static void Failures() {
        var car=new GameObject("car");int before=AudioSource.All.Count;
        using(var missing=new TransformerReturnAudio(car,null,_=>{})){Tick(missing,1);missing.Stop();}
        Check(AudioSource.All.Count==before,"missing optional return WAV is safe and creates no voice");
        var bad=new List<WavData>{new WavData{Channels=2,SampleRate=16000,Samples=new float[100]},Sample(6.1f),new WavData{Channels=1,SampleRate=16000,Samples=new[]{float.NaN}},new WavData{Channels=1,SampleRate=16000,Samples=new[]{1.01f}},new WavData{Channels=1,SampleRate=0,Samples=new float[10]},new WavData{Channels=1,SampleRate=16000,Samples=new float[0]}};
        foreach(var data in bad)using(var broken=new TransformerReturnAudio(car,data,_=>{}))Tick(broken,1);
        Check(AudioSource.All.Count==before,"corrupt oversized nonfinite and stereo PCM rejected without voice allocation");
        AudioClip.FailNextUpload=true;
        using(var broken=new TransformerReturnAudio(car,Sample(),_=>{}))Tick(broken,1);
        Check(AudioSource.All.Count==before&&AudioClip.All.All(c=>c.Destroyed),"upload failure cleans partial clip allocation");
        using(var shortAudio=new TransformerReturnAudio(car,Sample(.5f),_=>{})) {
            var s=AudioSource.All.Last();Tick(shortAudio,1);Tick(shortAudio,.8f);int starts=s.Starts;
            for(int i=0;i<20;i++)Tick(shortAudio,.7f-i*.01f);
            Check(s.Starts==starts&&!s.isPlaying,"short optional texture finishes without final-sample replay spam");
        }
        using(var pausedEnded=new TransformerReturnAudio(car,Sample(),_=>{})) {
            var s=AudioSource.All.Last();Tick(pausedEnded,.9f);s.Advance(10);
            Tick(pausedEnded,.9f,blocked:true);Tick(pausedEnded,.898f);
            Check(s.isPlaying&&s.Starts==2&&s.UnPauses==1&&s.timeSamples==8160,"resume recovers naturally finished source when UnPause alone cannot revive it");
            s.Advance(.01f);Tick(pausedEnded,.896f);Check(s.Starts==2,"paused finished source recovery remains a single play");
        }
        var errors=new List<string>();
        using(var failedRuntime=new TransformerReturnAudio(car,Sample(),errors.Add)) {
            var s=AudioSource.All.Last();AudioSource.FailNextPlay=true;Tick(failedRuntime,.9f);Tick(failedRuntime,.8f);
            Check(s.Destroyed&&s.clip==null&&!failedRuntime.Owns(s)&&errors.Count(v=>v.Contains("disabled"))==1,"Unity runtime error disables only optional return layer once");
        }
        Check(AudioClip.All.All(c=>c.Destroyed),"failure paths release all optional clips");
    }
    static void ClosingRing() {
        var m=new TransformerReturnSoundModel(5.3f);
        m.CompleteReturn();m.Step(.01f,false,false,0,false,1);
        Check(!m.Playing,"completion without previously observed Returning cannot invent parked ring");
        m.Step(.01f,true,false,1,false,1);m.CompleteReturn();m.Step(.01f,false,false,0,false,1);
        Check(!m.Playing,"pending return or forward phase does not authorize closing ring");
        m.Step(.01f,true,true,.002f,false,1);m.CompleteReturn();m.Step(.8f,false,false,0,false,1);
        Check(m.Playing&&Near(m.Position,5),"natural completion starts authorized ring at end pose without raw hitch overshoot");
        m.Step(.1f,false,false,0,false,.6f);Check(m.Playing&&Near(m.Position,5.1f)&&Near(m.Gain,.6f),"car-state ring advances and takes live master gain");
        m.CompleteReturn();m.Step(.05f,false,false,0,false,1);
        Check(m.Playing&&Near(m.Position,5.15f),"duplicate natural completion callback cannot restart or lengthen live ring");
        m.Step(100,false,false,0,true,1);Check(m.Playing&&m.Paused&&Near(m.Position,5.15f),"menu focus and streaming freeze authorized closing ring");
        m.Step(0,false,false,0,false,1);Check(m.Paused&&Near(m.Position,5.15f),"zero scaled time cannot consume closing ring");
        m.Step(.05f,false,false,0,false,1);Check(m.Playing&&Near(m.Position,5.2f),"ring resumes after frozen car state");
        m.Step(.101f,false,false,0,false,1);Check(!m.Playing,"car-state ring ends within at most 300ms of live time");
        m.CompleteReturn();m.Step(.01f,false,false,0,false,1);Check(!m.Playing,"completed ring cannot be rearmed by repeated completion");
        m.Step(.01f,true,true,.002f,false,1);m.CompleteReturn();m.Step(.01f,false,false,0,false,1);m.Step(.01f,true,false,.002f,false,1);
        Check(!m.Playing,"new forward activation cancels authorized return ring immediately");
        m=new TransformerReturnSoundModel(6);m.Step(.01f,true,true,.002f,false,1);m.CompleteReturn();m.Step(.01f,false,false,0,false,1);m.Step(.301f,false,false,0,false,1);
        Check(!m.Playing,"six-second optional file cannot lengthen maximum 300ms closing ring");
        m=new TransformerReturnSoundModel(4.2f);m.Step(.01f,true,true,.002f,false,1);m.CompleteReturn();m.Step(.01f,false,false,0,false,1);
        Check(!m.Playing,"previous short A-only clip cannot synthesize a missing closing ring");
        var car=new GameObject("car");
        using(var a=new TransformerReturnAudio(car,Sample(5.3f),_=>{})) {
            var s=AudioSource.All.Last();
            Tick(a,.15f);Check(s.isPlaying&&s.timeSamples==68000,"mixed runtime continues beyond 4.2-second growl into mechanical closing section");
            Tick(a,.002f);s.Advance(.01f);a.CompleteReturn();Tick(a,0,dt:.8f,active:false,returning:false);
            Check(s.isPlaying&&s.Starts==1&&s.timeSamples==80000,"natural car restore preserves one source through authoritative final impact");
            var restoredBody=new Vector3(3,2,1);
            a.Tick(.1f,false,false,0,false,.5f,restoredBody);
            Check(s.timeSamples==81600&&s.transform.position.x==3&&Near(s.volume,.5f),"inactive driven or parked hook follows restored body and live gain");
            Tick(a,0,active:false,returning:false,blocked:true);int sample=s.timeSamples;
            s.Advance(20);Tick(a,0,dt:10,active:false,returning:false,blocked:true);
            Check(s.Paused&&s.timeSamples==sample,"inactive tail hook pauses real Unity cursor during blocking");
            Tick(a,0,dt:.1f,active:false,returning:false);Check(s.isPlaying&&s.Starts==1&&s.timeSamples==83200,"inactive hook resumes ring without a duplicate Play");
            Tick(a,0,dt:.101f,active:false,returning:false);int starts=s.Starts;
            for(int i=0;i<100;i++)Tick(a,0,active:false,returning:false);
            Check(!s.isPlaying&&s.Starts==starts,"normal car ticks cannot replay naturally completed closing section");
            Tick(a,.002f);a.CompleteReturn();Tick(a,0,active:false,returning:false);a.Stop();
            Tick(a,0,active:false,returning:false);Check(!s.isPlaying,"explicit deactivate restore Stop cancels an armed or playing tail");
            Tick(a,.002f);a.CompleteReturn();Tick(a,0,active:false,returning:false);Tick(a,.002f,returning:false);
            Check(!s.isPlaying,"direction switch interrupts active closing ring");
            Tick(a,.002f);a.CompleteReturn();Tick(a,0,active:false,returning:false);a.Dispose();
            Check(!s.isPlaying&&s.Destroyed&&s.clip==null,"disposal releases active car-end ring immediately");
        }
        Check(AudioClip.All.All(c=>c.Destroyed),"ring coverage leaves no audio clip allocated");
    }
    static void Main(){Model();Adapter();Failures();ClosingRing();Console.WriteLine(checks+" Transformer return audio checks passed; native mix still requires in-game listening.");}
}
