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

internal sealed class DriftScoreRoute {
    internal readonly float Length,StartX,StartZ,TangentX,TangentZ,HalfWidth;
    internal readonly Func<float,float,DriftTrackProjection> Project;
    internal static readonly DriftScoreRoute Practice=new DriftScoreRoute(DriftCircuitLayout.TotalLength,
        DriftTrackLayout.StartX,DriftTrackLayout.StartZ,1,0,DriftTrackLayout.StraightWidth*.5f,DriftCircuitLayout.Project);
    internal DriftScoreRoute(float length,float x,float z,float tx,float tz,float halfWidth,Func<float,float,DriftTrackProjection> project) {
        if(project==null||float.IsNaN(length)||float.IsInfinity(length)||length<30||
            float.IsNaN(x)||float.IsInfinity(x)||float.IsNaN(z)||float.IsInfinity(z)||
            float.IsNaN(tx)||float.IsInfinity(tx)||float.IsNaN(tz)||float.IsInfinity(tz)||
            float.IsNaN(halfWidth)||float.IsInfinity(halfWidth)||halfWidth<=0)throw new ArgumentException("Invalid drift route");
        float magnitude=(float)Math.Sqrt(tx*tx+tz*tz);if(magnitude<.001f)throw new ArgumentException("Missing route heading");
        Length=length;StartX=x;StartZ=z;TangentX=tx/magnitude;TangentZ=tz/magnitude;HalfWidth=halfWidth;Project=project;
    }
}

// Scores newly covered forward course metres, never elapsed time or wheelspin.
// The high-water mark survives reversing, so a previously scored bend cannot
// pay twice. A lap is banked only at the physical start plane after all gates.
internal sealed class DriftLapScore {
    readonly DriftScoreRoute route;
    readonly bool preserveChainAcrossLaps;
    internal DriftLapScore(DriftScoreRoute route=null,bool preserveChainAcrossLaps=false){this.route=route??DriftScoreRoute.Practice;this.preserveChainAcrossLaps=preserveChainAcrossLaps;}
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
    public float Progress=>Math.Max(0,Math.Min(1,furthest/route.Length));
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
        var projection=route.Project(value.X,value.Z);
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
        float change=projection.DistanceAlong-previousAlong,length=route.Length;
        if(change>length*.5f)change-=length;
        if(change<-length*.5f)change+=length;
        // A legal wide line crosses a polyline corner's projection bisector:
        // the nearest authored point can jump by several metres while the car
        // barely moves. Allow one road-width of this geometric discontinuity.
        // World displacement, ordered gates, high-water tracking and the cap
        // by actual travelled metres still reject teleports and double points.
        float cornerAllowance=onCourse&&previousOnCourse?projection.HalfWidth*2:0;
        if(Math.Abs(change)>moved*1.75f+.75f+cornerAllowance) {
            Interrupt();Remember(value,projection.DistanceAlong,onCourse);return;
        }
        bool crossing=onCourse&&previousOnCourse&&CrossesStart(previous,value)&&change>0&&value.VelocityX*route.TangentX+value.VelocityZ*route.TangentZ>1;
        if(crossing) {
            bool completed=Active&&furthest>=length-10&&nextGate>=length&&position>length-10;
            if(completed)Complete();
            Begin(projection.DistanceAlong<10?projection.DistanceAlong:0,preserveChainAcrossLaps&&completed);
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
    void Begin(float along,bool keepChain) {
        Active=true;points=0;position=furthest=along;LapTime=0;nextGate=GateSpacing;
        if(!keepChain)ResetChain();Status=DriftLapStatus.Running;
    }
    void Complete() {
        LastLapScore=Score;LapCount++;bool changed=false;
        if(Score>BestScore){BestScore=Score;changed=true;}
        if(LapTime>0&&(BestLapTime<=0||LapTime<BestLapTime)){BestLapTime=LapTime;changed=true;}
        if(changed)RecordRevision++;
    }
    void ResetChain(){combo=0;chainDistance=quietTime=0;}
    void Remember(DriftLapSample value,float along,bool onCourse){previous=value;previousAlong=along;previousOnCourse=onCourse;havePrevious=true;}
    bool CrossesStart(DriftLapSample a,DriftLapSample b) {
        float ax=(a.X-route.StartX)*route.TangentX+(a.Z-route.StartZ)*route.TangentZ;
        float bx=(b.X-route.StartX)*route.TangentX+(b.Z-route.StartZ)*route.TangentZ;
        if(ax>=0||bx<0||bx-ax<.001f)return false;
        float t=-ax/(bx-ax),x=a.X+(b.X-a.X)*t-route.StartX,z=a.Z+(b.Z-a.Z)*t-route.StartZ;
        return Math.Abs(-x*route.TangentZ+z*route.TangentX)<=route.HalfWidth;
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
