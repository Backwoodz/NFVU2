using System;
using System.IO;
using System.Linq;
using Rachel350Z;
class RadioTests {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
    static void Main(){
        var closed=RadioAcoustics.Evaluate(1,0,0,0);
        var left=RadioAcoustics.Evaluate(1,1,0,0);
        var right=RadioAcoustics.Evaluate(1,0,1,0);
        var both=RadioAcoustics.Evaluate(1,1,1,0);
        var hatch=RadioAcoustics.Evaluate(1,0,0,1);
        var all=RadioAcoustics.Evaluate(1,1,1,1);
        Check(Near(closed.Gain,.3f)&&Near(closed.Cutoff,1400),"closed cabin retains muffled exterior radio");
        Check(left.Gain>closed.Gain&&left.Cutoff>closed.Cutoff,"one open door makes music louder and clearer");
        Check(Near(left.Gain,right.Gain)&&Near(left.Cutoff,right.Cutoff)&&Near(left.X,-right.X)&&left.X<0,"door sound follows the correct mirrored physical side");
        Check(both.Gain>left.Gain&&Near(both.X,0),"two doors increase exterior sound with a centred origin");
        Check(hatch.Gain>left.Gain&&hatch.Cutoff>left.Cutoff&&Near(hatch.Z,-1.45f),"open hatch exposes the trunk music at the rear");
        Check(Near(all.Gain,.85f)&&Near(all.Cutoff,12000)&&Near(all.X,0),"all openings keep a bounded mix without stacking streams");
        var seated=RadioAcoustics.Evaluate(0,1,1,1);
        Check(Near(seated.Gain,1)&&Near(seated.Cutoff,22000)&&Near(seated.X,0),"open panels do not alter seated playback");
        var transition=RadioAcoustics.Evaluate(.5f,0,0,1);
        Check(Near(transition.Gain,(1+hatch.Gain)/2)&&Near(transition.Cutoff,(22000+hatch.Cutoff)/2),"enter and exit smoothly blend the same exterior mix");
        bool continuous=true;var previous=closed;
        for(int i=1;i<=100;i++) {
            var next=RadioAcoustics.Evaluate(1,i/100f,0,i/100f);
            continuous&=next.Gain>=previous.Gain&&next.Cutoff>=previous.Cutoff&&next.Gain-previous.Gain<.01f&&Math.Abs(next.Z-previous.Z)<.03f;
            previous=next;
        }
        Check(continuous,"partial openings change volume, filtering and origin continuously");
        var shutAgain=RadioAcoustics.Evaluate(1,0,0,0);
        Check(Near(shutAgain.Gain,closed.Gain)&&Near(shutAgain.Cutoff,closed.Cutoff),"closing panels restores the muffled mix without retained gain");
        var invalid=RadioAcoustics.Evaluate(1,float.NaN,float.PositiveInfinity,-1);
        var clamped=RadioAcoustics.Evaluate(2,2,2,2);
        Check(Near(invalid.Gain,closed.Gain)&&Near(invalid.Cutoff,closed.Cutoff)&&Near(clamped.Gain,all.Gain),"invalid animation inputs cannot poison or overdrive audio");
        var empty=new RadioPlaybackState(0);
        Check(!empty.HasPlayable&&!empty.Move(1)&&!empty.Select(0),"missing music leaves radio unavailable");
        var state=new RadioPlaybackState(3);int original=state.Revision;
        state.Move(-1);Check(state.Index==2,"previous wraps playlist");
        state.Move(1);Check(state.Index==0,"next wraps playlist");
        Check(!state.IsCurrent(original),"rapid switches invalidate earlier asynchronous results");
        int current=state.Revision;
        Check(!state.MarkFailed(original)&&state.Index==0,"stale load failure cannot skip current song");
        Check(state.MarkFailed(current)&&state.Index==1,"corrupt song skips to next track");
        state.Move(-1);Check(state.Index==2,"manual navigation skips known failed song");
        state.MarkFailed(state.Revision);Check(state.Index==1,"failure wraps to sole playable track");
        state.MarkFailed(state.Revision);Check(!state.HasPlayable,"all corrupt tracks stop retrying");
        Check(!state.Move(1)&&!state.HasPlayable,"no endless failed playlist loop");
        Check(state.Select(0)&&state.HasPlayable,"explicit selection retries a repaired file");
        state.TogglePause();state.Move(1);Check(state.Paused,"track navigation preserves user pause");
        state.TogglePause();Check(!state.Paused,"explicit play resumes");
        Check(!state.Select(-1)&&!state.Select(3),"invalid menu selections are ignored");
        string folder=Path.Combine(Path.GetTempPath(),"nfvu2-radio-"+Guid.NewGuid().ToString("N"));
        try {
            Check(RadioPlaylist.Read(folder,_=>{}).Length==0,"missing directory is a valid empty playlist");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,"b.wav"),new byte[0]);
            File.WriteAllBytes(Path.Combine(folder,"a.ogg"),new byte[0]);
            File.WriteAllBytes(Path.Combine(folder,"unsupported.mp3"),new byte[0]);
            Check(string.Join(",",RadioPlaylist.Read(folder,_=>{}).Select(t=>t.Title))=="a,b","folder tracks sorted with supported extensions only");
            File.WriteAllText(Path.Combine(folder,"tracks.json"),"[{\"file\":\"b.wav\",\"title\":\"Song B\"},{\"file\":\"b.wav\"},{\"file\":\"../escape.wav\"}]");
            var tracks=RadioPlaylist.Read(folder,_=>{});
            Check(tracks.Length==1&&tracks[0].Title=="Song B","manifest order and title preserved without duplicate, stale or traversal entries");
            File.WriteAllText(Path.Combine(folder,"tracks.json"),"{");bool warned=false;
            Check(RadioPlaylist.Read(folder,_=>warned=true).Length==2&&warned,"malformed manifest falls back to playable local files");
        }finally {
            if(Directory.Exists(folder)){foreach(string path in Directory.GetFiles(folder))File.Delete(path);Directory.Delete(folder);}
        }
        Console.WriteLine(checks+" radio state checks passed; Unity streaming requires an in-game check.");
    }
}
