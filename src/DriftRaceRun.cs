using System;

namespace Rachel350Z {
internal enum DriftRacePhase {Practice,Preview,Loading,Countdown,Racing,Paused,Finished,Returning}

// Event scoring is isolated from endless practice and from other race records.
internal sealed class DriftRaceRun {
    internal const int Laps=3;
    internal DriftRacePhase Phase {get;private set;}
    internal DriftLapScore Score {get;private set;}
    internal int BankedScore {get;private set;}
    internal int BestScore {get;private set;}
    internal int FinalScore {get;private set;}
    internal int RecordRevision {get;private set;}
    internal float Elapsed {get;private set;}
    internal float Countdown {get;private set;}
    internal float LaunchElapsed {get;private set;}
    internal float BestTime {get;private set;}
    internal bool NewRecord {get;private set;}
    int observedLaps;
    internal bool BlocksDriving=>Phase!=DriftRacePhase.Practice&&Phase!=DriftRacePhase.Racing;
    internal bool UsesRaceTrack=>Phase==DriftRacePhase.Loading||Phase==DriftRacePhase.Countdown||Phase==DriftRacePhase.Racing||Phase==DriftRacePhase.Paused||Phase==DriftRacePhase.Finished;
    internal int TotalScore=>(int)Math.Min(1000000000L,(long)BankedScore+(Phase==DriftRacePhase.Finished?0:Score?.Score??0));
    internal void LoadRecord(int best,float time) {Reset();BestScore=Math.Max(0,Math.Min(1000000000,best));BestTime=Finite(time)&&time>0?time:0;}
    internal void Preview(){if(Phase==DriftRacePhase.Practice)Phase=DriftRacePhase.Preview;}
    internal void Start(DriftScoreRoute route) {
        if(Phase!=DriftRacePhase.Preview&&Phase!=DriftRacePhase.Finished&&Phase!=DriftRacePhase.Paused)return;
        Score=new DriftLapScore(route,preserveChainAcrossLaps:true);BankedScore=FinalScore=observedLaps=0;Elapsed=Countdown=LaunchElapsed=0;NewRecord=false;Phase=DriftRacePhase.Loading;
    }
    internal void Ready(){if(Phase==DriftRacePhase.Loading){Countdown=3;Phase=DriftRacePhase.Countdown;}}
    internal void TickCountdown(float dt) {
        if(Phase!=DriftRacePhase.Countdown||!Finite(dt)||dt<=0||dt>.5f)return;
        Countdown=Math.Max(0,Countdown-dt);if(Countdown<=0)Phase=DriftRacePhase.Racing;
    }
    internal void TickLaunch(float dt) {
        if(Phase==DriftRacePhase.Racing&&Finite(dt)&&dt>0&&dt<=.5f)LaunchElapsed=Math.Min(10,LaunchElapsed+dt);
    }
    internal void Step(DriftLapSample sample) {
        if(Phase!=DriftRacePhase.Racing||Score==null)return;
        Score.Step(sample);
        if(Score.Active&&Finite(sample.DeltaTime)&&sample.DeltaTime>0&&sample.DeltaTime<=.25f)Elapsed+=sample.DeltaTime;
        if(Score.LapCount<=observedLaps)return;
        observedLaps=Score.LapCount;BankedScore=(int)Math.Min(1000000000L,(long)BankedScore+Score.LastLapScore);
        if(observedLaps<Laps)return;
        FinalScore=BankedScore;Phase=DriftRacePhase.Finished;
        bool changed=false;
        if(FinalScore>BestScore){BestScore=FinalScore;NewRecord=changed=true;}
        if(Elapsed>0&&(BestTime<=0||Elapsed<BestTime)){BestTime=Elapsed;changed=true;}
        if(changed)RecordRevision++;
    }
    internal void Pause(){if(Phase==DriftRacePhase.Racing){Score.Pause();Phase=DriftRacePhase.Paused;}}
    internal void Resume(){if(Phase==DriftRacePhase.Paused)Phase=DriftRacePhase.Racing;}
    internal void Return(){Score?.Interrupt();Phase=DriftRacePhase.Returning;}
    internal void Practice(){Score=null;BankedScore=FinalScore=observedLaps=0;Countdown=Elapsed=LaunchElapsed=0;NewRecord=false;Phase=DriftRacePhase.Practice;}
    internal void Reset(){Practice();BestScore=RecordRevision=0;BestTime=0;}
    static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
}
}
