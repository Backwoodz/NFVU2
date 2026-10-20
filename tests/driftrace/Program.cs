using System;
using Rachel350Z;

static class Program {
    static int checks;
    static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static DriftScoreRoute Route()=>new DriftScoreRoute(ParkadeLayout.TotalLength,ParkadeLayout.StartX,ParkadeLayout.StartZ,
        ParkadeLayout.StartTangentX,ParkadeLayout.StartTangentZ,ParkadeLayout.StartHalfWidth,ParkadeLayout.Project);
    static DriftLapSample At(float along,float angle=30) {
        float length=ParkadeLayout.TotalLength,d=(along%length+length)%length;var p=ParkadeLayout.AtDistance(d);double r=angle*Math.PI/180;
        return new DriftLapSample(p.X,p.Z,p.TangentX*20,p.TangentZ*20,(float)(p.TangentX*Math.Cos(r)-p.TangentZ*Math.Sin(r)),(float)(p.TangentX*Math.Sin(r)+p.TangentZ*Math.Cos(r)),.05f);
    }
    static DriftRaceRun Begin() {
        var race=new DriftRaceRun();race.LoadRecord(123,0);race.Preview();race.Start(Route());
        Check(race.Phase==DriftRacePhase.Loading&&race.BlocksDriving,"Race waits for actual venue construction");
        race.TickCountdown(5);Check(race.Phase==DriftRacePhase.Loading,"Unready venue cannot start countdown");
        race.Ready();race.TickCountdown(float.NaN);Check(race.Countdown==3,"Invalid countdown delta ignored");
        for(int i=0;i<31;i++)race.TickCountdown(.1f);
        Check(race.Phase==DriftRacePhase.Racing&&!race.BlocksDriving,"Countdown releases driving");
        for(int i=-3;i<=0;i++)race.Step(At(i));Check(race.Score.Active,"Measured rotated start plane begins lap");
        return race;
    }
    static void Main() {
        var race=Begin();int end=(int)Math.Ceiling(3*ParkadeLayout.TotalLength);
        int previousCount=0,expected=0;
        for(int i=1;i<=end;i++) {
            int multiplier=race.Score.Multiplier,combo=race.Score.Combo;
            race.Step(At(i));
            if(race.Score.LapCount>previousCount) {
                previousCount=race.Score.LapCount;expected+=race.Score.LastLapScore;
                Check(race.BankedScore==expected,"Completed lap points accumulate exactly once");
                Check(multiplier==5&&race.Score.Multiplier==multiplier,"Start/finish preserves the race multiplier");
                Check(combo>0&&race.Score.Combo>=combo,"Start/finish preserves the continuous race drift chain");
                Check(race.Score.Score==0&&race.Score.LastLapScore>0,"Lap points bank without double-counting the retained chain");
                if(previousCount<3)Check(race.Phase==DriftRacePhase.Racing&&race.RecordRevision==0,"First and second laps cannot finish or save race record");
            }
        }
        Check(race.Phase==DriftRacePhase.Finished&&race.Score.LapCount==3,"Race ends after exactly three measured laps");
        Check(race.FinalScore==expected&&race.TotalScore==expected&&race.BestScore==expected&&race.NewRecord,"Three-lap score becomes independent event record");
        int final=race.FinalScore;for(int i=end+1;i<end+200;i++)race.Step(At(i));
        Check(race.FinalScore==final&&race.Score.LapCount==3&&race.RecordRevision==1,"Finished event cannot accumulate a fourth lap");
        race.Start(Route());Check(race.Phase==DriftRacePhase.Loading&&race.BestScore==final,"Retry preserves event record");
        Check(race.Score.Multiplier==1&&race.Score.Combo==0&&race.TotalScore==0,"A new race clears the previous event chain and multiplier");
        race.Return();Check(race.Phase==DriftRacePhase.Returning&&race.BlocksDriving,"Returning holds the car until parking terrain is ready");
        race.Practice();Check(!race.BlocksDriving&&race.Phase==DriftRacePhase.Practice&&race.BestScore==final,"Endless practice is separate and preserves event record");
        race=Begin();for(int i=1;i<=100;i++)race.Step(At(i));int points=race.TotalScore;
        race.Pause();for(int i=0;i<100;i++)race.Step(At(101));Check(race.TotalScore==points&&race.Phase==DriftRacePhase.Paused,"Menu pause cannot add drift points");
        race.Resume();race.Step(At(100));race.Step(At(101));Check(race.TotalScore>points,"Resume preserves actual current lap");
        points=race.TotalScore;for(int i=100;i>=40;i--)race.Step(At(i));for(int i=41;i<=101;i++)race.Step(At(i));
        Check(race.TotalScore==points,"Race mode preserves reverse/redrift anti-farming");
        race.Step(At(400));Check(!race.Score.Active&&race.RecordRevision==0,"Teleport cannot finish a three-lap record");
        race.Return();race.Practice();Check(race.BestScore==123,"Abandoned partial race never replaces record");
        race.Reset();Check(race.BestScore==0&&race.Score==null,"World change clears prior event context");
        var practice=new DriftLapScore();for(int i=-2;i<=0;i++)practice.Step(PracticeAt(i));
        for(int i=1;i<=Math.Ceiling(4*DriftCircuitLayout.TotalLength);i++)practice.Step(PracticeAt(i));
        Check(practice.LapCount==4&&practice.Active,"Original practice remains unlimited beyond three laps");
        RotatedRoute();
        MenuAndLoading();
        foreach(float offset in new[]{-6f,-4f,4f,6f})WideLine(offset);
        Console.WriteLine(checks+" measured Parkade three-lap, practice-isolation and event-state checks passed.");
    }
    static void MenuAndLoading() {
        var nav=new DriftEventMenuNavigation();double time=0;
        Func<DriftRacePhase,float,float,bool,bool,DriftEventMenuAction> step=(phase,x,y,a,b)=>nav.Step(phase,x,y,a,b,time+=.05);
        var preview=DriftRacePhase.Preview;
        Check(step(preview,0,0,true,false)==DriftEventMenuAction.None,"Held handbrake at event entry cannot start a race");
        Check(step(preview,0,0,true,false)==DriftEventMenuAction.None,"Held confirm cannot repeat");
        step(preview,0,0,false,false);
        Check(step(preview,0,0,true,false)==DriftEventMenuAction.Accept,"Fresh controller confirm activates default Start");
        step(preview,0,0,false,false);step(preview,1,0,false,false);
        Check(nav.Selected==1,"Stick right selects Back");
        Check(step(preview,1,0,true,false)==DriftEventMenuAction.Cancel,"Confirm activates selected Back rather than always starting");
        Check(step(preview,1,0,true,false)==DriftEventMenuAction.None,"Holding confirm does not cancel twice");
        step(preview,0,0,false,false);step(preview,-1,0,false,false);
        Check(nav.Selected==0,"Stick left returns to Start");
        step(preview,0,0,false,false);step(preview,0,-1,false,false);
        Check(nav.Selected==1,"D-pad down also selects Back");
        step(preview,0,0,false,false);
        Check(step(preview,0,0,false,true)==DriftEventMenuAction.Cancel,"Controller B cancels independently of focused action");
        var loading=DriftRacePhase.Loading;
        Check(step(loading,0,0,false,true)==DriftEventMenuAction.None,"Held B across a transition cannot cancel a second screen");
        Check(nav.Selected==1,"Loading selects the only enabled Cancel action");
        step(loading,0,0,false,false);
        Check(step(loading,0,0,true,false)==DriftEventMenuAction.Cancel,"Loading confirm activates Cancel, never Start");
        step(DriftRacePhase.Returning,0,0,false,false);
        Check(step(DriftRacePhase.Returning,0,0,true,true)==DriftEventMenuAction.None,"Returning has no actionable Start or Cancel");
        foreach(var phase in new[]{DriftRacePhase.Paused,DriftRacePhase.Finished}) {
            step(phase,0,0,false,false);
            Check(step(phase,0,0,true,false)==DriftEventMenuAction.Accept,"Resume/retry uses its default primary action");
        }
        for(int i=0;i<12;i++) {
            double t=i*.14+.001;float sum=0;int bright=0;
            for(int segment=0;segment<3;segment++) {
                float value=DriftLoadingAnimation.Brightness(t,segment);sum+=value;if(value==1)bright=segment;
                Check(value>=.25f&&value<=1,"Animated segment brightness stays bounded");
            }
            Check(bright==i%3&&Math.Abs(sum-1.9f)<.001f,"Three-segment highlight advances left to right with constant overall brightness");
        }
        Check(DriftLoadingAnimation.Brightness(double.NaN,0)==1,"Invalid loading clock remains drawable");
    }
    static DriftLapSample PracticeAt(float along) {
        float len=DriftCircuitLayout.TotalLength;var p=DriftCircuitLayout.AtDistance((along%len+len)%len);
        return new DriftLapSample(p.X,p.Z,p.TangentX*20,p.TangentZ*20,p.TangentX*.866f-p.TangentZ*.5f,p.TangentX*.5f+p.TangentZ*.866f,.05f);
    }
    static void RotatedRoute() {
        Func<float,float,DriftTrackProjection> project=(x,z)=> {
            var p=DriftCircuitLayout.Project(z+700,2000-x);
            return new DriftTrackProjection(2000-p.Z,p.X-700,-p.TangentZ,p.TangentX,p.DistanceAlong,p.HalfWidth,p.DistanceToCenter);
        };
        var route=new DriftScoreRoute(DriftCircuitLayout.TotalLength,2000-DriftTrackLayout.StartZ,DriftTrackLayout.StartX-700,0,1,8,project);
        var score=new DriftLapScore(route);
        for(int i=-2;i<=Math.Ceiling(DriftCircuitLayout.TotalLength);i++) {
            var p=PracticeAt(i);score.Step(new DriftLapSample(2000-p.Z,p.X-700,-p.VelocityZ,p.VelocityX,-p.ForwardZ,p.ForwardX,p.DeltaTime));
        }
        Check(score.LapCount==1&&score.BestScore>0,"Route abstraction supports a rotated start and velocity, not hardcoded eastbound practice");
    }
    static void WideLine(float offset) {
        var score=new DriftLapScore(Route());float lastX=0,lastZ=0;int firstBend=0,laterBend=0;
        for(int i=-15;i<=Math.Ceiling(3*ParkadeLayout.TotalLength*5)+15;i++) {
            float s=i*.2f,len=ParkadeLayout.TotalLength;
            Func<float,DriftTrackSample> sample=d=>ParkadeLayout.AtDistance((d%len+len)%len);
            var p=sample(s);var a=sample(s-4);var b=sample(s+4);
            float tx=b.X-a.X,tz=b.Z-a.Z,m=(float)Math.Sqrt(tx*tx+tz*tz);
            float x=p.X-tz/m*offset,z=p.Z+tx/m*offset;
            if(i>-15) {
                float vx=(x-lastX)/.02f,vz=(z-lastZ)/.02f,v=(float)Math.Sqrt(vx*vx+vz*vz);
                score.Step(new DriftLapSample(x,z,vx,vz,(vx*.866f-vz*.5f)/v,(vx*.5f+vz*.866f)/v,.02f));
                if(i>5)Check(score.Active,"continuous wide line stays valid through polyline bends, offset "+offset+" at "+s);
            }
            if(i==300)firstBend=score.Score;if(i==1000)laterBend=score.Score;
            lastX=x;lastZ=z;
        }
        Check(laterBend>firstBend&&firstBend>0,"points continue after first bend on wide drift line "+offset);
        Check(score.LapCount==3,"three complete off-centre laps remain countable "+offset);
    }
}
