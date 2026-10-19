using System;
using System.IO;
using System.Linq;
using Rachel350Z;
using UnityEngine;
static class ExhaustAdapterChecks {
    public static void Run(Action<bool,string> check) {
        string folder=Path.Combine(Path.GetDirectoryName(typeof(ExhaustAdapterChecks).Assembly.Location),"audio","exhaust");
        Directory.CreateDirectory(folder);
        foreach(var name in new[]{"backfire.wav","overrun_01.wav","overrun_02.wav","overrun_loop.wav"})File.WriteAllBytes(Path.Combine(folder,name),new byte[0]);
        int start=AudioSource.All.Count;
        var audio=VehicleEffectsAudio.Create(new GameObject("exhaust_fixture"),_=>{},new SpawnAudio());
        var voices=AudioSource.All.Skip(start).ToArray();
        var bang=voices.Single(s=>s.clip.name=="350Z_backfire.wav");
        var burble=voices.Where(s=>s.clip.name.Contains("overrun_0")).ToArray();
        var bed=voices.Single(s=>s.clip.name=="350Z_overrun_loop.wav");
        check(audio.BackfireAvailable&&voices.Length==4&&burble.All(s=>!s.loop&&s.priority==90)&&bed.loop&&bed.priority==100,"optional exhaust creates three short voices and one continuous bed above the engine priority");
        for(int i=0;i<60;i++)audio.TickExhaust(.02f,true,5900,1,12,false,false,1);
        for(int i=0;i<60;i++) {
            audio.Tick(false,false,i<3,false,1);
            audio.TickExhaust(.02f,true,5900,0,12,i==0,false,1);
        }
        check(bang.Starts==1&&burble.Sum(s=>s.Starts)==2,"flame request plays one bang and the independent delayed two-pop tail");
        check(voices.All(s=>!s.isVirtual)&&bang.volume>burble.Max(s=>s.volume),"bang and softer burble receive real voices at bounded gains");
        for(int i=0;i<120;i++)audio.TickExhaust(.02f,true,3200-i*8,0,12-i*.05f,false,false,.5f);
        check(bed.isPlaying&&!bed.isVirtual&&bed.volume>.1f&&bed.pitch<1,"normal sound volume keeps the real overrun loop audible throughout falling-RPM coasting");
        audio.Tick(false,false,false,true,1);audio.TickExhaust(.02f,true,5900,0,12,false,true,1);
        check(voices.All(s=>!s.isPlaying&&s.volume==0),"blocked controls immediately stop every exhaust source");
        int starts=voices.Sum(s=>s.Starts);
        for(int i=0;i<60;i++)audio.TickExhaust(.02f,true,5900,0,12,false,false,1);
        check(starts==voices.Sum(s=>s.Starts),"unblocking coasting does not replay paused exhaust");
        for(int i=0;i<60;i++)audio.TickExhaust(.02f,true,5900,1,12,false,false,1);
        audio.TickExhaust(.04f,true,5900,0,12,false,false,1);
        check(burble.Any(s=>s.isPlaying),"plain lift-off starts burble with the flame option disabled");
        for(int i=0;i<15;i++)audio.TickExhaust(.02f,true,5900,1,12,false,false,1);
        check(burble.All(s=>!s.isPlaying),"returning to gas cuts the overrun tail");
        check(!bed.isPlaying&&bed.volume==0,"returning to gas also stops the continuous exhaust bed");
        audio.TickExhaust(float.NaN,true,5900,0,12,false,false,1);
        check(!bed.isPlaying&&bed.volume==0,"invalid frame time cannot poison sustained source volume");
        audio.Dispose();audio.Dispose();
        check(voices.All(s=>s.clip==null&&!s.isPlaying),"exhaust disposal is idempotent and releases all voices");
        foreach(var name in new[]{"backfire.wav","overrun_01.wav","overrun_02.wav","overrun_loop.wav"})File.Delete(Path.Combine(folder,name));
        audio=VehicleEffectsAudio.Create(new GameObject("missing_exhaust_fixture"),_=>{},new SpawnAudio());
        check(audio!=null&&!audio.BackfireAvailable,"missing optional exhaust retains a valid silent effect adapter");audio.Dispose();
    }
}
