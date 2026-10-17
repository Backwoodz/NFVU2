using System;
using System.Collections.Generic;
using System.Linq;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static bool Quiet(TransformerFootstepModel m)=>!m.Left.Active&&!m.Right.Active;
    static WavData Sample()=>new WavData{Channels=1,SampleRate=16000,Samples=new float[16000]};
    static void Model() {
        var m=new TransformerFootstepModel(4,17);
        m.Step(.01f,true,false,10,20,0,1);Check(Quiet(m),"binding existing gait consumes its baseline without replay");
        m.Step(.01f,true,false,11,20,0,1);Check(m.Left.Active&&!m.Right.Active,"only left contact produces a left event");
        m.Step(.01f,true,false,11,21,0,1);Check(!m.Left.Active&&m.Right.Active,"only right contact produces a right event");
        m.Step(.01f,true,false,12,22,0,1);Check(m.Left.Active&&m.Right.Active&&m.Left.Clip!=m.Right.Clip,"simultaneous foot contacts preserve both with different variants");
        m.Step(.01f,false,false,13,23,0,1);Check(Quiet(m),"disabled form consumes contacts silently");
        m.Step(.01f,true,false,13,23,0,1);Check(Quiet(m),"enabling form does not replay disabled contacts");
        m.Step(.01f,true,true,14,24,0,1);Check(Quiet(m),"pause menu and focus gating consume contacts silently");
        m.Step(.01f,true,false,14,24,0,1);Check(Quiet(m),"resuming cannot replay a blocked contact");
        m.Step(.01f,true,false,15,24,1,1);Check(m.Left.Active,"fresh contacts after resume remain audible");
        m.Step(.01f,true,false,0,0,0,1);Check(Quiet(m),"gait reset does not create false foot contacts");
        m.Step(.01f,true,false,100,100,0,1);Check(m.Left.Active&&m.Right.Active,"counter jumps remain a bounded pair of current contacts");
        foreach(float bad in new[]{0,-1,float.NaN,float.PositiveInfinity}){m.Step(bad,true,false,101,101,0,1);if(!Quiet(m))throw new Exception("invalid dt emitted");}
        Check(Quiet(m),"paused or invalid elapsed time cannot create contacts");
        m.Step(.01f,true,false,102,102,float.NaN,1);Check(Quiet(m),"invalid movement blend cannot poison pitch");
        m.Step(.01f,true,false,103,103,0,float.PositiveInfinity);Check(Quiet(m),"invalid volume is silent");
        m.Step(.01f,true,false,104,104,0,0);m.Step(.01f,true,false,104,104,0,1);Check(Quiet(m),"unmuting does not replay muted contacts");
        m.Reset();m.Step(.01f,true,false,999,999,0,1);Check(Quiet(m),"explicit stop resets the sampling baseline");
        var empty=new TransformerFootstepModel(0);empty.Step(.01f,true,false,0,0,0,1);empty.Step(.01f,true,false,1,1,0,1);Check(Quiet(empty),"missing optional clips remain silent");
        var single=new TransformerFootstepModel(1);single.Step(.01f,true,false,0,0,0,1);single.Step(.01f,true,false,1,0,0,1);Check(single.Left.Active&&single.Left.Clip==0,"one valid optional clip is usable");
        var shuffle=new TransformerFootstepModel(4,77);shuffle.Step(.01f,true,false,0,0,0,1);
        int last=-1;var cycle=new HashSet<int>();bool unique=true,bounded=true;float walk=0,run=0;
        for(uint i=1;i<=400;i++) {
            shuffle.Step(.01f,true,false,i,0,(i%2==0)?0:1,2);var e=shuffle.Left;
            unique&=e.Active&&e.Clip!=last&&cycle.Add(e.Clip);last=e.Clip;
            if(i%4==0)cycle.Clear();
            bounded&=e.Gain>=.6909f-1e-5f&&e.Gain<=.945f&&e.Pitch>=.955f&&e.Pitch<=1.02f;
            if(i%2==0)walk+=e.Gain;else run+=e.Gain;
        }
        Check(unique,"shuffle cycles use every clip once without repeats at boundaries");
        Check(bounded,"variation remains subtle and volume is bounded despite excessive master gain");
        Check(run>walk*1.2f,"sprint contacts gain weight while retaining the same family of sounds");
    }
    static void Rehearse(bool sprint,bool turn) {
        var g=new TransformerGait();var m=new TransformerFootstepModel(4,11);float speed=0;int audible=0,settling=0;const float dt=1f/120;
        m.Step(dt,true,false,0,0,0,1);
        for(int i=0;i<16*120;i++) {
            bool request=i>=120&&i<10*120;uint l=g.LeftFootfalls,r=g.RightFootfalls;
            speed=TransformerGait.AdvanceSpeed(speed,request&&!turn?1:0,dt,g.Ready,sprint);
            g.Step(dt,speed,request&&turn?45:0,request);
            m.Step(dt,true,false,g.LeftFootfalls,g.RightFootfalls,g.RunBlend,1);
            if(m.Left.Active!=(g.LeftFootfalls>l)||m.Right.Active!=(g.RightFootfalls>r))throw new Exception("audio diverged from actual landings");
            int count=(m.Left.Active?1:0)+(m.Right.Active?1:0);audible+=count;if(!request)settling+=count;
        }
        Check(audible>8&&g.Settled&&Quiet(m),"real "+(turn?"standing turns":sprint?"sprint":"walk")+" produces audio only at landings and settles quietly");
        Check(settling>0,"remaining "+(turn?"turn":sprint?"sprint":"walk")+" contacts stay audible after movement input is released");
    }
    static void Adapter() {
        var car=new GameObject("car");var data=new Dictionary<string,WavData>{{"step_01.wav",Sample()},{"broken.wav",new WavData()},{"step_02.wav",Sample()}};
        int first=AudioSource.All.Count;
        var a=new TransformerFootstepAudio(car,data,_=>{});var sources=AudioSource.All.Skip(first).ToArray();
        Check(a.ClipCount==2&&sources.Length==6,"adapter skips invalid optional sample and creates six bounded spatial voices");
        Check(sources.All(s=>a.Owns(s)&&!s.loop&&s.spatialBlend==1&&s.dopplerLevel==0),"own voices can be excluded from generic transform pausing");
        var left=new Vector3(1,2,3);var right=new Vector3(4,5,6);
        a.Tick(.01f,true,false,0,0,0,1,left,right);a.Tick(.01f,true,false,1,1,0,1,left,right);
        Check(sources.Sum(s=>s.Starts)==2&&sources[0].transform.position.x==1&&sources[3].transform.position.x==4,"left and right contacts sound from supplied foot positions");
        float full=sources[0].volume;a.Tick(.01f,true,false,1,1,0,.5f,left,right);
        Check(Math.Abs(sources[0].volume-full*.5f)<.00001f,"live volume edits affect already playing tails");
        car.transform.position=new Vector3(20,20,20);sources[0].transform.position=car.transform.position;
        a.Tick(.01f,true,false,1,1,0,.5f,left,right);
        Check(sources[0].transform.position.x==left.x&&sources[0].transform.position.y==left.y,"ringing impact tails remain at world contact points while the car moves");
        a.Tick(.01f,true,true,2,2,0,1,left,right);Check(sources.All(s=>!s.Playing),"blocking stops every tail immediately");
        a.Tick(.01f,true,false,2,2,0,1,left,right);Check(sources.Sum(s=>s.Starts)==2,"resume never restarts stopped tails or blocked contacts");
        for(uint i=3;i<100;i++)a.Tick(.01f,true,false,i,i,0,1,left,right);
        Check(AudioSource.All.Count-first==6&&sources.Sum(s=>s.Starts)==196,"many contacts reuse fixed voice pool without accumulating sources");
        a.Stop();int starts=sources.Sum(s=>s.Starts);a.Tick(.01f,true,false,101,101,0,1,left,right);
        Check(sources.All(s=>!s.Playing)&&sources.Sum(s=>s.Starts)==starts,"stop silences voices and rebinds counter baseline");
        a.Dispose();a.Dispose();a.Tick(.01f,true,false,102,102,0,1,left,right);
        Check(sources.All(s=>!s.Playing&&s.clip==null)&&AudioClip.All.All(c=>c.Destroyed),"dispose is idempotent, clears sources and destroys owned clips");
        int before=AudioSource.All.Count;
        using(var missing=new TransformerFootstepAudio(car,null,_=>{}))missing.Tick(.01f,true,false,1,1,0,1);
        Check(AudioSource.All.Count==before,"missing sample set creates no audio objects");
        AudioClip.FailNextUpload=true;
        using(var bad=new TransformerFootstepAudio(car,new Dictionary<string,WavData>{{"bad_upload",Sample()}},_=>{}))Check(bad.ClipCount==0,"Unity sample upload failure safely disables optional footsteps");
        Check(AudioClip.All.All(c=>c.Destroyed),"failed optional upload also releases its clip");
    }
    static void Main(){Model();Rehearse(false,false);Rehearse(true,false);Rehearse(false,true);Adapter();Console.WriteLine(checks+" Transformer sound checks passed; timing/spatial mix still require in-game listening.");}
}
