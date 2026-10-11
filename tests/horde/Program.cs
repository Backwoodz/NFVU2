using System;
using System.Collections.Generic;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;}
    static void Main() {
        checks+=HordeRunChecks.Run();
        var wave=new HordeWave();int calls=0;
        Func<string,bool> spawn=name=>{calls++;return true;};
        Check(!wave.Begin(1),"a living horde blocks another run");
        Check(wave.Begin(0)&&!wave.Begin(0),"start cannot stack pending waves");
        wave.Tick(100,0,spawn);Check(calls==0&&wave.Choosing,"choice blocks spawning indefinitely");
        Check(!wave.Choose((HordePact)99)&&wave.Choosing,"invalid choice leaves selection open");
        Check(wave.Choose(wave.Offers[0])&&!wave.Choose(HordePact.Thor),"exactly one pact can be selected");
        Check(wave.Remaining(0)==15,"counter includes queued enemies");
        foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})wave.Tick(dt,0,spawn);
        Check(calls==0,"paused and invalid frames spawn nothing");
        wave.Tick(10,HordeWave.MaxAlive,spawn);Check(calls==0&&wave.Attempts==0,"live cap defers spawns without discarding slots");
        wave.Tick(10,HordeWave.MaxAlive-1,spawn);Check(calls==1,"slow frame creates at most one enemy");
        wave.Tick(.01f,0,spawn);Check(calls==1,"spawn interval throttles fast frames");
        wave.Cancel();calls=0;wave.Tick(10,0,spawn);
        Check(calls==0&&!wave.Active&&!wave.Choosing&&wave.Pact==HordePact.None,"cancel clears pending spawn and pact state");
        var expected=new[]{new[]{12,3,0,0,0,0,0,0,0,0,0},new[]{0,6,2,0,0,12,0,0,0,0,0},new[]{0,0,0,2,0,0,10,6,0,0,0},new[]{0,0,0,0,0,0,0,0,12,2,0},new[]{0,0,0,0,1,0,0,0,4,0,2}};
        wave.Begin(0);int total=0;
        for(int number=1;number<=5;number++) {
            Check(wave.Choosing&&wave.Pact==HordePact.None,"each wave asks for a new pact");
            if(number==5)Check(Array.IndexOf(wave.Offers,HordePact.Valkyrie)<0&&!wave.Choose(HordePact.Valkyrie)&&wave.Choosing,"final selection excludes and rejects Valkyrie");
            wave.Choose(HordePact.None);
            var counts=new Dictionary<string,int>();int alive=0,frames=0;
            while(wave.Pending&&frames++<2000)wave.Tick(1,alive,name=>{counts[name]=counts.ContainsKey(name)?counts[name]+1:1;alive++;return true;});
            Check(!wave.Pending&&wave.Number==number,"finite spawn queue completes "+number);
            for(int type=0;type<11;type++)Check((counts.ContainsKey(HordeWave.Prefabs[type])?counts[HordeWave.Prefabs[type]]:0)==expected[number-1][type],"enemy count in wave "+number+" type "+type);
            total+=alive;Check(wave.Remaining(alive)==alive&&alive<=24,"wave stays below live cap");
            wave.Tick(100,0,spawn,0,0);
            Check(!wave.Choosing&&wave.Active,"disappeared enemies do not count as kills");
            wave.Tick(100,1,spawn,wave.Spawned,1);
            Check(!wave.Choosing&&wave.Active,"required elite or boss blocks progression despite quota");
            wave.Tick(1,alive-wave.KillGoal,spawn,wave.KillGoal,0);
            if(number==5)break;
            Check(wave.Choosing&&wave.Number==number+1&&wave.Spawned==0,"next selection opens immediately with clean counters");
            calls=0;wave.Tick(100,0,spawn);Check(calls==0&&wave.Number==number+1,"new wave waits for explicit selection, not a timer");
        }
        Check(total==74&&wave.Completed&&!wave.Active,"all five new rosters total 74 and end in victory");
        calls=0;wave.Tick(100,0,spawn);Check(calls==0,"completed encounter stays finished");
        wave.Begin(0);wave.Choose(HordePact.None);calls=0;
        wave.Tick(.1f,0,name=>{calls++;return false;});Check(calls==3,"blocked ground gets three probes per frame");
        for(int i=0;i<1000;i++)wave.Tick(1,0,name=>{calls++;return false;});
        Check(calls==15*HordeWave.AttemptsPerEnemy&&!wave.Active&&wave.NoGround&&!wave.Completed,"no ground stops run without victory");
        wave.Begin(0);wave.Choose(HordePact.None);
        for(int i=0;i<1000&&wave.Pending;i++)wave.Tick(1,0,name=>name=="Greydwarf");
        Check(wave.Spawned==3&&wave.Resolved==15&&wave.KillGoal==3,"quota uses actual spawned population on partial ground");
        wave.Tick(1,0,spawn,3);wave.Tick(8,0,spawn);wave.Choose(HordePact.None);
        for(int i=0;i<1000&&wave.Pending;i++)wave.Tick(1,0,name=>!HordeWave.Required(name));
        Check(!wave.Active&&wave.NoGround&&!wave.Completed,"missing required elite fails run instead of silently skipping it");
        Check(HordeWave.Required("Dragon")&&HordeWave.Required("GoblinBrute")&&!HordeWave.Required("Wolf"),"boss and elites are mandatory");
        var bonus=new HordeBonus();bonus.Begin();bonus.Tick(24.9f);
        Check(bonus.Claim()&&!bonus.Claim(),"timed hunt rewards once before deadline");
        bonus.Begin();bonus.Tick(25);Check(!bonus.Claim(),"expired hunt cannot grant reward");
        bonus.Begin();bonus.Tick(float.NaN);bonus.Tick(-1);bonus.Tick(0);Check(bonus.Left==25,"invalid and paused bonus frames are ignored");
        bonus.Clear();Check(!bonus.Claim(),"wave cleanup cancels hunt reward");
        Console.WriteLine(checks+" horde scheduling checks passed. Terrain, native AI and frame times require an in-game test.");
    }
}
