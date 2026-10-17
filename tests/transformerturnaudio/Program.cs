using System;
using System.Collections.Generic;
using System.Linq;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static readonly Vector3 Point=new Vector3(1,3,5);
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static WavData Sample(float length=1)=>new WavData{Channels=1,SampleRate=16000,Samples=new float[(int)(16000*length)]};
    static void Tick(TransformerTurnAudio a,float rate=45,float volume=1,bool blocked=false,float dt=.01f,bool enabled=true)=>a.Tick(dt,enabled,blocked,rate,volume,Point);
    static void Model() {
        var m=new TransformerTurnSoundModel(new[]{.6f,.9f,1.2f},14);
        m.Step(.01f,true,false,0,1);Check(!m.Event.Active&&m.Gain==0,"zero measured rotation stays silent regardless of steering intent");
        m.Step(.01f,true,false,1.99f,1);Check(!m.Event.Active&&m.Gain==0,"small root jitter is below the two degree deadband");
        m.Step(.01f,true,false,45,1);Check(m.Event.Active&&m.Gain>0&&m.Gain<.34f,"actual rotation alone starts a quiet texture with attack ramp");
        for(int i=0;i<9;i++)m.Step(.01f,true,false,45,1);
        float full=m.Gain;Check(Math.Abs(full-.34f)<.00001f,"full turn reaches bounded level below approved footstep gain");
        m.Step(.01f,true,false,-45,1);Check(!m.Event.Active&&Math.Abs(m.Gain-full)<.00001f,"rapid direction reversal preserves ongoing texture without restart");
        m.Step(.01f,true,false,0,1);Check(!m.Event.Active&&m.Gain>0&&m.Gain<full,"ordinary stop releases instead of abruptly cutting");
        for(int i=0;i<15;i++)m.Step(.01f,true,false,0,1);
        Check(m.Gain==0&&!m.Event.Active,"ordinary stop is fully quiet after 150ms rather than retaining long tails");
        m.Step(.01f,true,true,45,1);Check(m.ImmediateSilence&&m.Gain==0,"pause or focus block resets immediately");
        for(int i=0;i<200;i++)m.Step(.01f,true,true,45,1);
        m.Step(.01f,true,false,0,1);Check(!m.Event.Active,"blocked rotation does not become a burst after resume");
        var empty=new TransformerTurnSoundModel(new float[0]);empty.Step(.01f,true,false,45,1);Check(empty.Gain==0&&!empty.Event.Active,"missing optional textures stay silent");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity}) {
            m.Step(.01f,true,false,invalid,1);if(!m.ImmediateSilence)throw new Exception("bad rate audible");
            m.Step(.01f,true,false,45,invalid);if(!m.ImmediateSilence)throw new Exception("bad gain audible");
        }
        foreach(float invalid in new[]{0,-1,.8f,float.NaN,float.PositiveInfinity}){m.Step(invalid,true,false,45,1);if(!m.ImmediateSilence)throw new Exception("bad time audible");}
        Check(m.Gain==0,"invalid rotation gain and elapsed time cannot poison the live envelope");
        int last=-1,events=0;bool unique=true,pitch=true;var cycle=new HashSet<int>();
        m=new TransformerTurnSoundModel(new[]{.6f,.9f,1.2f},14);
        for(int i=0;i<10000;i++) {
            m.Step(.01f,true,false,i%2==0?45:-45,1);var e=m.Event;if(!e.Active)continue;
            unique&=e.Clip!=last&&cycle.Add(e.Clip);last=e.Clip;events++;if(events%3==0)cycle.Clear();
            pitch&=e.Pitch>=.975f&&e.Pitch<=1.025f;
        }
        Check(unique&&events>100,"sustained turns shuffle all textures without consecutive repeats across bag boundaries");
        Check(pitch,"pitch variation remains subtle and bounded");
        Check(events<250,"rapid reversals do not cause restart spam");
        events=0;m=new TransformerTurnSoundModel(new[]{1f},14);
        for(int i=0;i<1000;i++){m.Step(.01f,true,false,i%2==0?(i%4==0?45:-45):0,1);if(m.Event.Active)events++;}
        Check(events>=10&&events<20,"direction reversals through a zero-speed frame preserve texture scheduling");
        for(int i=0;i<20;i++)m.Step(.01f,true,false,0,1);
        m.Step(.01f,true,false,-45,1);Check(m.Event.Active,"fresh turn after a fully settled stop starts immediately");
        var slow=new TransformerTurnSoundModel(new[]{1f},42);var fast=new TransformerTurnSoundModel(new[]{1f},42);
        for(int i=0;i<20;i++){slow.Step(.01f,true,false,8,1);fast.Step(.01f,true,false,45,1);}
        Check(slow.Gain>0&&slow.Gain<fast.Gain*.5f,"mechanical layer level tracks actual angular speed");
    }
    static void Adapter() {
        var car=new GameObject("car");int before=AudioSource.All.Count;
        var samples=new Dictionary<string,WavData>{{"turn_01",Sample()},{"bad_channels",new WavData{Channels=2,SampleRate=16000,Samples=new float[100]}},{"turn_02",Sample(.8f)},{"turn_03",Sample(1.2f)}};
        var a=new TransformerTurnAudio(car,samples,_=>{});var voices=AudioSource.All.Skip(before).ToArray();
        Check(a.ClipCount==3&&voices.Length==3,"adapter skips invalid optional data and uses exactly three owned voices");
        Check(voices.All(v=>a.Owns(v)&&!v.loop&&v.spatialBlend==1&&v.dopplerLevel==0),"turn voices are spatial and can be excluded from generic transform source suppression");
        Tick(a,0);Check(voices.All(v=>v.Starts==0),"stationary robot never starts a mechanical voice");
        Tick(a);var first=voices.Single(v=>v.Starts==1);Check(first.Playing&&first.volume==0,"measured movement starts a texture with a zero edge to avoid clicks");
        for(int i=0;i<10;i++)Tick(a);
        Check(first.volume>0&&first.volume<=.34f,"ongoing texture reaches the quiet live rotation gain");
        float full=first.volume;Tick(a,45,.5f);Check(Math.Abs(first.volume-full*.5f)<.00001f,"master volume changes update already playing textures");
        int starts=voices.Sum(v=>v.Starts);Tick(a,-45,.5f);
        Check(voices.Sum(v=>v.Starts)==starts&&Math.Abs(first.volume-full*.5f)<.00001f,"sign reversal leaves live pitch and gain uninterrupted");
        Tick(a,8,.5f);Check(first.volume<full*.5f,"a slowing turn reduces existing voice gain rather than only future sounds");
        float reduced=first.volume;Tick(a,0,.5f);Check(first.Playing&&first.volume>0&&first.volume<reduced,"stopping rotation starts a live tail fade");
        for(int i=0;i<16;i++)Tick(a,0,.5f);
        Check(voices.All(v=>!v.Playing&&v.volume==0),"stop fade actually stops all Unity voices within 150ms");
        starts=voices.Sum(v=>v.Starts);for(int i=0;i<100;i++)Tick(a,0);
        Check(voices.Sum(v=>v.Starts)==starts,"remaining still cannot schedule a delayed texture");
        Tick(a);for(int i=0;i<10;i++)Tick(a);
        Tick(a,45,1,true);Check(voices.All(v=>!v.Playing&&v.volume==0),"pause focus and streaming block cut every voice immediately");
        starts=voices.Sum(v=>v.Starts);for(int i=0;i<500;i++)Tick(a,45,1,true);
        Tick(a,0);Check(voices.Sum(v=>v.Starts)==starts,"resuming without real movement cannot replay a blocked turn");
        Tick(a);Tick(a,45,1,false,.01f,false);Check(voices.All(v=>!v.Playing),"return to car or disabled robot immediately silences the layer");
        Tick(a);Tick(a,45,0);Check(voices.All(v=>!v.Playing),"muting live audio stops textures and clears pending schedule");
        Tick(a);Tick(a,45,1,false,0);Check(voices.All(v=>!v.Playing),"zero game time silences tails rather than letting unscaled audio continue");
        bool overlap=false;int maxPlaying=0;
        for(int i=0;i<3000;i++) {
            Tick(a,i%2==0?45:-45);
            int active=voices.Count(v=>v.Playing);overlap|=active>=2;maxPlaying=Math.Max(active,maxPlaying);
        }
        Check(overlap&&maxPlaying<=3&&AudioSource.All.Count-before==3,"sustained turning overlaps naturally within three fixed Unity voices");
        a.Stop();starts=voices.Sum(v=>v.Starts);
        for(int i=0;i<1000;i++)Tick(a,i%2==0?(i%4==0?45:-45):0);
        int reversedStarts=voices.Sum(v=>v.Starts)-starts;
        Check(reversedStarts>=10&&reversedStarts<22,"actual Unity starts remain bounded while rapid direction changes cross zero");
        for(int i=0;i<20;i++)Tick(a,0);
        starts=voices.Sum(v=>v.Starts);Tick(a);
        Check(voices.Sum(v=>v.Starts)==starts+1,"adapter immediately responds after a genuinely settled stop");
        Check(voices.All(v=>v.transform.position.x==Point.x&&v.transform.position.y==Point.y),"mechanical emitters follow the supplied robot world position");
        Tick(a,45,1);a.Stop();Check(voices.All(v=>!v.Playing&&v.volume==0),"explicit stop clears live audio immediately");
        starts=voices.Sum(v=>v.Starts);Tick(a,0);Check(voices.Sum(v=>v.Starts)==starts,"explicit stop cannot leave a delayed onset");
        a.Dispose();a.Dispose();Tick(a);Check(voices.All(v=>!v.Playing&&v.clip==null)&&AudioClip.All.All(c=>c.Destroyed),"disposal releases clips and stops all voices idempotently");
        before=AudioSource.All.Count;
        using(var missing=new TransformerTurnAudio(car,null,_=>{})){Tick(missing);Check(missing.ClipCount==0,"absent optional turn set is safe");}
        Check(AudioSource.All.Count==before,"absent optional set allocates no voices");
        AudioClip.FailNextUpload=true;
        using(var broken=new TransformerTurnAudio(car,new Dictionary<string,WavData>{{"upload",Sample()}},_=>{}))Check(broken.ClipCount==0,"failed Unity upload disables optional clip safely");
        using(var corrupt=new TransformerTurnAudio(car,new Dictionary<string,WavData>{{"nonfinite",new WavData{Channels=1,SampleRate=16000,Samples=new[]{float.NaN}}},{"too_long",Sample(3.1f)}},_=>{}))Check(corrupt.ClipCount==0,"nonfinite and oversized optional samples are rejected");
        Check(AudioClip.All.All(c=>c.Destroyed),"failed uploads and valid textures release all owned clips");
    }
    static void Main(){Model();Adapter();Console.WriteLine(checks+" Transformer turn audio checks passed; native mix and timing still require in-game listening.");}
}
