using System;
using System.Globalization;

namespace Rachel350Z {
internal enum DriftLapStatus {WaitingForStart,Running,WrongWay,OffCourse,Interrupted}

internal struct DriftLapSample {
    public readonly float X,Z,VelocityX,VelocityZ,ForwardX,ForwardZ,DeltaTime;
    public readonly bool Grounded;
    public DriftLapSample(float x,float z,float vx,float vz,float fx,float fz,float dt,bool grounded=true) {
        X=x;Z=z;VelocityX=vx;VelocityZ=vz;ForwardX=fx;ForwardZ=fz;DeltaTime=dt;Grounded=grounded;
    }
}

// Scores newly covered forward course metres, never elapsed time or wheelspin.
// The high-water mark survives reversing, so a previously scored bend cannot
// pay twice. A lap is banked only at the physical start plane after all gates.
internal sealed class DriftLapScore {
    const float GateSpacing=25,MaximumScore=1000000000;
    DriftLapSample previous;
    bool havePrevious,paused,previousOnCourse;
    float previousAlong,position,furthest,chainDistance,quietTime,nextGate;
    double points,combo;
    public int Score=>(int)Math.Min(MaximumScore,points);
    public int Combo=>(int)Math.Min(MaximumScore,combo);
    public int BestScore {get;private set;}
    public int LastLapScore {get;private set;}
    public float LapTime {get;private set;}
    public float BestLapTime {get;private set;}
    public int LapCount {get;private set;}
    public int RecordRevision {get;private set;}
    public bool Active {get;private set;}
    public DriftLapStatus Status {get;private set;}=DriftLapStatus.WaitingForStart;
    public float Progress=>Math.Max(0,Math.Min(1,furthest/DriftCircuitLayout.TotalLength));
    public int Multiplier=>Math.Min(5,1+(int)(chainDistance/60));

    public void LoadRecord(int best,float fastest) {
        Clear();BestScore=Math.Max(0,Math.Min((int)MaximumScore,best));
        BestLapTime=Finite(fastest)&&fastest>0?fastest:0;
    }
    public void Clear() {
        havePrevious=paused=previousOnCourse=Active=false;points=combo=0;position=furthest=chainDistance=quietTime=0;
        BestScore=LastLapScore=LapCount=RecordRevision=0;LapTime=BestLapTime=0;
        Status=DriftLapStatus.WaitingForStart;nextGate=GateSpacing;
    }
    public void Interrupt() {
        Active=false;havePrevious=false;paused=false;ResetChain();Status=DriftLapStatus.Interrupted;
    }
    public void Pause(){paused=true;ResetChain();}

    public void Step(DriftLapSample value) {
        if(!Valid(value)){Interrupt();return;}
        var projection=DriftCircuitLayout.Project(value.X,value.Z);
        bool onCourse=projection.SignedEdgeDistance<=.75f;
        if(!havePrevious){Remember(value,projection.DistanceAlong,onCourse);return;}
        float dx=value.X-previous.X,dz=value.Z-previous.Z;
        float moved=Length(dx,dz),speed=Length(value.VelocityX,value.VelocityZ);
        if(paused) {
            paused=false;if(moved>2)Interrupt();Remember(value,projection.DistanceAlong,onCourse);return;
        }
        // Large frame gaps and jumps cannot bridge gates or trigger a finish.
        if(moved>Math.Max(3,speed*value.DeltaTime*1.75f+1)||Math.Abs(value.X)>11000||Math.Abs(value.Z)>11000) {
            Interrupt();Remember(value,projection.DistanceAlong,onCourse);return;
        }
        float change=projection.DistanceAlong-previousAlong,length=DriftCircuitLayout.TotalLength;
        if(change>length*.5f)change-=length;
        if(change<-length*.5f)change+=length;
        if(Math.Abs(change)>moved*1.75f+.75f) {
            Interrupt();Remember(value,projection.DistanceAlong,onCourse);return;
        }
        bool crossing=onCourse&&previousOnCourse&&CrossesStart(previous,value)&&change>0&&value.VelocityX>1;
        if(crossing) {
            if(Active&&furthest>=length-10&&nextGate>=length&&position>length-10)Complete();
            Begin(projection.DistanceAlong<10?projection.DistanceAlong:0);
            Remember(value,projection.DistanceAlong,onCourse);return;
        }
        if(!Active){Remember(value,projection.DistanceAlong,onCourse);return;}
        LapTime+=value.DeltaTime;
        position+=change;
        float newDistance=Math.Max(0,position-furthest);
        // A discontinuity cannot leap multiple gates. Normally each 25 m gate
        // is reached in one small, physically continuous forward step.
        if(newDistance>0&&position>=nextGate) {
            if(position-nextGate>25){Interrupt();Remember(value,projection.DistanceAlong,onCourse);return;}
            nextGate+=GateSpacing;
        }
        // Keep tracking continuous progress off the road, without awarding it.
        // Returning cannot collect the skipped grass metres or pay twice.
        furthest=Math.Max(furthest,Math.Min(position,length));
        float routeSpeed=value.VelocityX*projection.TangentX+value.VelocityZ*projection.TangentZ;
        Status=!onCourse?DriftLapStatus.OffCourse:change<-.05f||routeSpeed<-1?DriftLapStatus.WrongWay:DriftLapStatus.Running;
        float facingLength=Length(value.ForwardX,value.ForwardZ);
        float forward=(value.VelocityX*value.ForwardX+value.VelocityZ*value.ForwardZ)/facingLength;
        float sideways=Math.Abs(value.VelocityX*value.ForwardZ-value.VelocityZ*value.ForwardX)/facingLength;
        float angle=(float)(Math.Atan2(sideways,forward)*180/Math.PI);
        bool drift=onCourse&&previousOnCourse&&value.Grounded&&speed>=4&&routeSpeed>=2&&forward>1&&angle>=10&&angle<=75&&newDistance>.015f&&change>0;
        if(drift) {
            float metres=Math.Min(newDistance,Math.Min(moved,speed*value.DeltaTime*1.3f));
            chainDistance+=metres;quietTime=0;
            double earned=metres*10*Math.Min(1,angle/45)*(1+Math.Min(3,speed/25))*Multiplier;
            points=Math.Min(MaximumScore,points+earned);combo=Math.Min(MaximumScore,combo+earned);
        } else if(!onCourse) {
            ResetChain();
        } else {
            quietTime+=value.DeltaTime;if(quietTime>=1.5f)ResetChain();
        }
        Remember(value,projection.DistanceAlong,onCourse);
    }
    void Begin(float along) {
        Active=true;points=combo=0;position=furthest=along;LapTime=0;nextGate=GateSpacing;
        ResetChain();Status=DriftLapStatus.Running;
    }
    void Complete() {
        LastLapScore=Score;LapCount++;bool changed=false;
        if(Score>BestScore){BestScore=Score;changed=true;}
        if(LapTime>0&&(BestLapTime<=0||LapTime<BestLapTime)){BestLapTime=LapTime;changed=true;}
        if(changed)RecordRevision++;
    }
    void ResetChain(){combo=0;chainDistance=quietTime=0;}
    void Remember(DriftLapSample value,float along,bool onCourse){previous=value;previousAlong=along;previousOnCourse=onCourse;havePrevious=true;}
    static bool CrossesStart(DriftLapSample a,DriftLapSample b) {
        float ax=a.X-DriftTrackLayout.StartX,bx=b.X-DriftTrackLayout.StartX;
        if(ax>=0||bx<0||bx-ax<.001f)return false;
        float z=a.Z+(b.Z-a.Z)*(-ax)/(bx-ax);
        return Math.Abs(z-DriftTrackLayout.StartZ)<=DriftTrackLayout.StraightWidth*.5f;
    }
    static bool Valid(DriftLapSample s)=>Finite(s.X)&&Finite(s.Z)&&Finite(s.VelocityX)&&Finite(s.VelocityZ)
        &&Finite(s.ForwardX)&&Finite(s.ForwardZ)&&Finite(s.DeltaTime)&&s.DeltaTime>0&&s.DeltaTime<=.25f
        &&Length(s.ForwardX,s.ForwardZ)>.5f&&Length(s.VelocityX,s.VelocityZ)<150;
    static float Length(float x,float z)=>(float)Math.Sqrt((double)x*x+(double)z*z);
    static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
}

internal static class DriftLapRecord {
    public const string Key="nfvu2_drift_best_v1";
    public static string Encode(int score,float time)=>Math.Max(0,score).ToString(CultureInfo.InvariantCulture)+","+
        (float.IsNaN(time)||float.IsInfinity(time)||time<0?0:time).ToString("R",CultureInfo.InvariantCulture);
    public static void Decode(string value,out int score,out float time) {
        score=0;time=0;if(string.IsNullOrEmpty(value))return;
        var parts=value.Split(',');if(parts.Length!=2)return;
        if(!int.TryParse(parts[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out score)||score<0)score=0;
        if(!float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out time)||float.IsNaN(time)||float.IsInfinity(time)||time<0)time=0;
    }
}
}
