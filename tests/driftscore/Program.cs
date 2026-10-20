using System;
using Rachel350Z;

static class Program {
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static DriftLapSample At(float distance,float angle=30,float dt=.05f,float speed=20,bool grounded=true) {
        float length=DriftCircuitLayout.TotalLength;distance=(distance%length+length)%length;
        var p=DriftCircuitLayout.AtDistance(distance);
        double a=angle*Math.PI/180;
        float fx=(float)(p.TangentX*Math.Cos(a)-p.TangentZ*Math.Sin(a));
        float fz=(float)(p.TangentX*Math.Sin(a)+p.TangentZ*Math.Cos(a));
        return new DriftLapSample(p.X,p.Z,p.TangentX*speed,p.TangentZ*speed,fx,fz,dt,grounded);
    }
    static DriftLapScore Start(){var m=new DriftLapScore();m.Step(At(-2));m.Step(At(-1));m.Step(At(0));Check(m.Active,"Forward physical line crossing starts lap");return m;}
    static DriftLapSample Offset(float distance,float offset) {
        var s=At(distance);var p=DriftCircuitLayout.AtDistance(distance);
        return new DriftLapSample(s.X-p.TangentZ*offset,s.Z+p.TangentX*offset,s.VelocityX,s.VelocityZ,s.ForwardX,s.ForwardZ,s.DeltaTime);
    }
    static void Drive(DriftLapScore m,int from,int to,float angle=30) {
        int direction=to>=from?1:-1;
        for(int i=from;;i+=direction){m.Step(At(i,angle));if(i==to)break;}
    }
    static void Main() {
        var model=new DriftLapScore();model.Step(At(500));Check(!model.Active,"Joining halfway cannot start score");
        Drive(model,501,520);Check(model.Score==0&&model.BestScore==0,"No score before crossing start");
        model=Start();Drive(model,1,100);
        Check(model.Score>0&&model.Combo>0&&model.Multiplier==2,"Forward drift earns points and distance multiplier");
        Check(model.BestScore==0&&model.LastLapScore==0,"Partial lap never becomes record");
        int earned=model.Score;Drive(model,99,40);Check(model.Score==earned&&model.Status==DriftLapStatus.WrongWay,"Reverse earns zero");
        Drive(model,41,100);Check(model.Score==earned,"Redrifting the same route cannot farm points");
        model.Step(At(101));Check(model.Score>earned,"Only new course metres score again");
        for(int i=0;i<400;i++)model.Step(At(101));
        Check(model.Combo==0&&model.Multiplier==1,"Stationary waiting breaks combo without earning");
        int stationary=model.Score;for(int i=0;i<100;i++)model.Step(new DriftLapSample(197,24,0,0,0,1,.05f));
        Check(model.Score==stationary,"Stationary rotation and engine/wheel speed cannot earn");
        model=Start();Drive(model,1,30,0);Check(model.Score==0,"Straight driving earns no drift points");
        Drive(model,31,60,90);Check(model.Score==0,"Spinning sideways at ninety degrees earns no drift points");
        model=Start();for(int i=1;i<=40;i++)model.Step(At(i,30,.05f,20,false));Check(model.Score==0,"Airborne car earns no points");
        model=Start();Drive(model,1,100);int partial=model.Score;
        model.Step(At(700));Check(!model.Active&&model.BestScore==0,"Teleport cannot skip gates or bank partial score");
        Drive(model,701,800);Check(model.Score==partial,"Invalid lap stays stopped after teleport");
        model=Start();Drive(model,1,80);
        model.Step(new DriftLapSample(176,50,20,0,1,0,.05f));
        Check(!model.Active&&model.Status==DriftLapStatus.Interrupted,"Teleport onto grass still invalidates lap");
        model=Start();Drive(model,1,80);int paused=model.Score;float time=model.LapTime;
        model.Pause();model.Step(At(80));Check(model.Active&&model.Score==paused&&model.LapTime==time,"Stationary pause preserves lap and freezes score/time");
        model.Step(At(81));Check(model.Score>paused,"Normal scoring resumes after paused anchor");
        model.Pause();model.Step(At(150));Check(!model.Active,"Moving while UI is blocked cannot bridge a course section");
        model=Start();Drive(model,1,40);model.Step(At(41,30,.3f));Check(!model.Active,"Long frame gap fails closed");
        model=Start();model.Step(new DriftLapSample(float.NaN,24,20,0,1,0,.05f));Check(!model.Active,"NaN sample invalidates");
        model=Start();model.Step(new DriftLapSample(97,24,float.PositiveInfinity,0,1,0,.05f));Check(!model.Active,"Infinite velocity invalidates");
        model=Start();model.Step(new DriftLapSample(97,24,20,0,0,0,.05f));Check(!model.Active,"Missing heading invalidates");
        model=Start();model.Step(new DriftLapSample(97,24,20,0,1,0,0));Check(!model.Active,"Zero delta cannot score");
        model=Start();Drive(model,1,20);Drive(model,19,-2);model.Step(At(-1));model.Step(At(0));
        Check(model.Active&&model.Score==0&&model.Progress<.01&&model.BestScore==0,"Premature start-line recross resets score without banking");
        model=Start();int end=(int)Math.Ceiling(DriftCircuitLayout.TotalLength);
        Drive(model,1,end);
        Check(model.Active&&model.LapCount==1,"Complete sequential lap immediately starts next lap");
        Check(model.Score==0&&model.LastLapScore>10000&&model.BestScore==model.LastLapScore,"Only complete lap banks best and resets current score");
        Check(model.RecordRevision==1&&model.BestLapTime>0&&model.LapTime==0,"Complete lap records score and lap time once");
        int best=model.BestScore;float fastest=model.BestLapTime;
        Drive(model,end+1,2*end,0);
        Check(model.LapCount==2&&model.LastLapScore==0&&model.BestScore==best,"Lower-scoring subsequent lap cannot replace best");
        Check(model.BestLapTime<=fastest,"Fastest valid time retained independently of score");
        string record=DriftLapRecord.Encode(model.BestScore,model.BestLapTime);
        DriftLapRecord.Decode(record,out int stored,out float storedTime);
        Check(stored==model.BestScore&&storedTime==model.BestLapTime,"Native world-key record round trips exactly");
        var loaded=new DriftLapScore();loaded.LoadRecord(stored,storedTime);
        Check(loaded.BestScore==best&&!loaded.Active&&loaded.Score==0&&loaded.LapCount==0,"Record restore excludes unfinished lap state");
        foreach(string bad in new[]{null,"","bad","3","1,2,3","-6,NaN","oops,Infinity","21474836470,-12"}) {
            DriftLapRecord.Decode(bad,out int n,out float t);Check(n>=0&&t>=0&&!float.IsNaN(t)&&!float.IsInfinity(t),"Corrupt world record sanitized");
        }
        loaded.Clear();Check(loaded.BestScore==0&&!loaded.Active,"World switch removes prior world record");
        // Repeat complete laps over all four drift angle bands and the entire
        // actual geometry, including the exact closed seam and all S turns.
        foreach(float angle in new[]{10f,20f,45f,70f}) {
            model=Start();Drive(model,1,end,angle);Check(model.LapCount==1&&model.BestScore>0,"Full real-course lap at drift angle "+angle);
        }
        GrassChecks(end);AdapterChecks(end);
        Console.WriteLine("Drift lap scoring checks passed: "+checks+" (actual route, reverse anti-farming, start/finish, pause, interruption, world records).");
    }
    static void GrassChecks(int end) {
        var model=Start();Drive(model,1,40);
        for(int i=41;i<=48;i++)model.Step(Offset(i,i-40));
        int before=model.Score;float elapsed=model.LapTime;
        for(int i=49;i<=60;i++) {
            model.Step(Offset(i,Math.Min(12,i-40)));
            Check(model.Active&&model.Status==DriftLapStatus.OffCourse&&model.Score==before,"Grass keeps lap and total but earns no points");
        }
        Check(model.Combo==0&&model.Multiplier==1,"Grass breaks only the drift combo");
        Check(model.LapTime>elapsed,"Lap clock continues during grass excursion");
        for(int i=61;i<=64;i++)model.Step(Offset(i,72-i));
        Check(model.Active&&model.Status==DriftLapStatus.Running&&model.Score==before,"First road sample does not award the off-road interval");
        for(int i=65;i<=72;i++)model.Step(Offset(i,72-i));
        Check(model.Score>before&&model.Active,"New road drifts resume without crossing start again");
        int resumed=model.Score;Drive(model,71,60);Drive(model,61,72);
        Check(model.Score==resumed,"Previously covered grass and road cannot be scored by returning");
        Drive(model,73,end);
        Check(model.LapCount==1&&model.BestScore>0,"Continuous full lap with grass excursion still banks normally");
        model=Start();Drive(model,1,80);before=model.Score;
        for(int i=79;i>=55;i--)model.Step(Offset(i,Math.Min(12,80-i)));
        for(int i=56;i<=67;i++)model.Step(Offset(i,67-i));
        Drive(model,68,80);
        Check(model.Active&&model.Score==before,"Reverse grass detour preserves high-water mark and cannot farm bends");
        model.Step(At(81));Check(model.Score>before,"Scoring resumes only beyond previous high-water mark");
    }
    static void AdapterChecks(int end) {
        Game.instance=new Game();ZNet.instance=new ZNet();ZDOMan.instance=new ZDOMan();
        ZRoutedRpc.instance=new ZRoutedRpc();ZoneSystem.instance=new ZoneSystem();Player.m_localPlayer=new Player();
        var plugin=new Plugin();
        plugin.TestTick(At(-2));plugin.TestTick(At(-1));plugin.TestTick(At(0));
        Check(plugin.TestScore.Active&&plugin.TestVisible,"Production adapter starts only selected drift car");
        for(int i=1;i<=20;i++)plugin.TestTick(At(i));
        int score=plugin.TestScore.Score;
        plugin.TestBlock(true);for(int i=0;i<25;i++)plugin.TestTick(At(20));
        Check(plugin.TestScore.Active&&plugin.TestScore.Score==score,"Actual UI gate freezes scoring without losing stationary lap");
        plugin.TestBlock(false);plugin.TestTick(At(20));plugin.TestTick(At(21));
        Check(plugin.TestScore.Score>score,"Production adapter resumes after UI");
        Player.m_localPlayer.dead=true;plugin.TestTick(At(22));Check(!plugin.TestScore.Active,"Death interrupts current score");
        Player.m_localPlayer.dead=false;plugin.TestTick(At(-2));plugin.TestTick(At(-1));plugin.TestTick(At(0));
        for(int i=1;i<=end;i++)plugin.TestTick(At(i));
        Check(ZoneSystem.instance.writes==1&&plugin.TestScore.BestScore>0,"Production adapter persists completed record exactly once");
        int best=plugin.TestScore.BestScore;var savedWorld=ZoneSystem.instance;
        plugin.TestClear();plugin.TestTick(At(10));
        Check(plugin.TestScore.BestScore==best&&!plugin.TestScore.Active,"World reload restores saved native record");
        ZNet.instance.id=101;ZoneSystem.instance=new ZoneSystem();plugin.TestTick(At(11));
        Check(plugin.TestScore.BestScore==0,"Distinct world UID does not inherit record");
        ZNet.instance.id=100;ZoneSystem.instance=savedWorld;plugin.TestTick(At(12));
        Check(plugin.TestScore.BestScore==best,"Returning to previous world loads its own record");
        plugin.TestMultiplayer(true);plugin.TestTick(At(13));Check(plugin.TestScore.BestScore==0,"Multiplayer gate clears scoring and persistence context");
        plugin.TestMultiplayer(false);plugin.TestTick(At(-2));plugin.TestTick(At(-1));plugin.TestTick(At(0));
        Check(plugin.TestScore.Active,"Fresh permitted context can restart lap");
        plugin.TestNextVehicle();plugin.TestTick(At(1));Check(!plugin.TestScore.Active,"Vehicle change interrupts shared current lap");
        plugin.TestHummer(true);Check(!plugin.TestVisible,"Hummer excluded from HUD");plugin.TestHummer(false);
        plugin.TestFlying(true);Check(!plugin.TestVisible,"Flight excluded from HUD");plugin.TestFlying(false);
        plugin.TestTransformer(true);Check(!plugin.TestVisible,"Transformer excluded from HUD");plugin.TestTransformer(false);
        DriftWorldRules.Current=false;plugin.TestTick(At(2));Check(plugin.TestScore.BestScore==0&&!plugin.TestVisible,"Ordinary world has no circuit state or HUD");
        DriftWorldRules.Current=true;
        // Native record write failure cannot throw into the driving update;
        // retain dirty revision and retry, without repeatedly logging per frame.
        ZNet.instance.id=102;ZoneSystem.instance=new ZoneSystem{fail=true};
        plugin.TestTick(At(-2));plugin.TestTick(At(-1));plugin.TestTick(At(0));plugin.TestDriftAdapter();
        for(int i=1;i<=end;i++)plugin.TestTick(At(i));
        Check(plugin.TestWarnings==1&&ZoneSystem.instance.writes==0,"Failed native save is isolated and delayed");
        plugin.TestDriving(false);ZoneSystem.instance.fail=false;
        for(int i=0;i<110;i++)plugin.TestTick(At(end));
        Check(ZoneSystem.instance.writes==1,"Dirty completed record retries while driver has exited car");
        Check(!plugin.TestScore.Active&&plugin.TestScore.BestScore>0,"Interrupted driving preserves completed record for retry");
        plugin.TestDriving(true);ZNet.instance.id=103;ZoneSystem.instance=new ZoneSystem{fail=true};
        plugin.TestTick(At(-2));plugin.TestTick(At(-1));plugin.TestTick(At(0));
        for(int i=1;i<=end;i++)plugin.TestTick(At(i));
        Check(ZoneSystem.instance.writes==0,"Second world fixture has pending completed record");
        plugin.TestBlock(true);ZoneSystem.instance.fail=false;
        for(int i=0;i<110;i++)plugin.TestTick(At(end));
        Check(ZoneSystem.instance.writes==1,"Dirty completed record retries while menu blocks driving");
        plugin.TestBlock(false);
    }
}
