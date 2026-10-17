using System;
namespace Rachel350Z {
// Flat-ground locomotion in robot-local metres/degrees. A supporting foot is
// integrated against measured root displacement, rather than sliding along
// a sine wave. Only a lifted foot may acquire a new landing position.
public sealed class TransformerGait {
    public const int Version=4;
    public const float Stride=.76f,Lift=.23f,MaxSpeed=2.8f,ReverseSpeed=1.65f,TurnDegrees=90f,HalfTrack=.76f,ArmSwing=27f;
    public const float SprintTurnDegrees=65f;
    public const float Acceleration=.9f,Braking=2.3f,SupportFraction=.62f;
    public const float SprintSpeed=8.5f,SprintStride=1.32f,SprintLift=.8f,SprintArmSwing=26f;
    public const float SprintAcceleration=1.5f,SprintBraking=3.5f,SprintSupportFraction=.30f;
    public const float SprintArmSpeed=125f,SprintPelvisBase=-.42f,SprintHeave=.32f;
    const float NeutralTrack=.88f,NeutralForward=-.02f;
    sealed class Leg {
        public float x,z,y,yaw,pitch,arm,age,reach;
        public bool swing,settling;
        public float elapsed,duration,startX,startZ,startY,startYaw,startPitch,startVelocity;
        public float targetX,targetZ,targetYaw,height,endVelocity;
    }
    readonly Leg left=new Leg(),right=new Leg();
    bool active,nextRight=true;
    float frequency=.6f,nextStep,smoothSpeed,smoothTurn,previousSpeed,acceleration,stillTime,runHeave;
    public float Phase {get;private set;}
    public float Amount {get;private set;}
    public float Release {get;private set;}
    public float Left {get;private set;}
    public float Right {get;private set;}
    public float PelvisHeight {get;private set;}
    // Numeric +X is the model's L side. Roll means lean toward +X; yaw uses
    // Unity +Y. Positive TorsoLean means lean forward (+Z).
    public float PelvisSide {get;private set;}
    public float PelvisYaw {get;private set;}
    public float TorsoLean {get;private set;}
    public float TorsoRoll {get;private set;}
    public float TorsoYaw {get;private set;}
    public float RunBlend {get;private set;}
    public uint LeftFootfalls {get;private set;}
    public uint RightFootfalls {get;private set;}
    public bool Flying=>left.swing&&right.swing;
    public float CurrentStride=>Math.Max(Stride+(SprintStride-Stride)*RunBlend,Math.Max(Math.Abs(left.z),Math.Abs(right.z)));
    public float CurrentLift=>Math.Max(Lift+(SprintLift-Lift)*RunBlend,Math.Max(left.y,right.y));
    public float CurrentArmSwing=>Math.Max(ArmSwing+(SprintArmSwing-ArmSwing)*RunBlend,Math.Max(Math.Abs(left.arm),Math.Abs(right.arm)));
    public float Support=>SupportFraction+(SprintSupportFraction-SupportFraction)*RunBlend;
    public float Cadence=>frequency;
    float TargetStride=>Stride+(SprintStride-Stride)*RunBlend;
    public bool Ready=>Release>=1;
    // Terrain preparation keeps the armor open while the feet and upper body
    // settle. Waiting for Release here would deadlock the return-to-car path.
    public bool FeetSettled=>!active&&Amount==0&&RunBlend==0&&left.arm==0&&right.arm==0&&TorsoLean==0&&TorsoRoll==0;
    public bool Settled=>FeetSettled&&Release==0;
    internal static TransformerGait Preparation(float release) {
        var result=new TransformerGait();result.Reset();
        result.Release=Finite(release)?Clamp(release):0;return result;
    }
    static float Move(float value,float target,float step)=>value<target?Math.Min(target,value+step):Math.Max(target,value-step);
    static float Clamp(float value,float lo=0,float hi=1)=>Math.Max(lo,Math.Min(hi,value));
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static float Smooth(float t)=>t*t*(3-2*t);
    static float Hermite(float a,float b,float va,float vb,float t,float duration) {
        float t2=t*t,t3=t2*t;
        return (2*t3-3*t2+1)*a+(t3-2*t2+t)*duration*va+(-2*t3+3*t2)*b+(t3-t2)*duration*vb;
    }
    static float Blend(float value,float target,float dt,float time)=>value+(target-value)*(1-(float)Math.Exp(-dt/time));
    public static float AdvanceSpeed(float current,float input,float dt,bool ready,bool sprint=false) {
        if(!Finite(current)||!Finite(input)||!Finite(dt)||dt<=0)return Finite(current)?current:0;
        bool fast=sprint&&input>0;
        float target=ready?Clamp(input,-1,1)*(input<0?ReverseSpeed:fast?SprintSpeed:MaxSpeed):0;
        if(current*target<0)target=0;
        float rate=Math.Abs(target)<Math.Abs(current)?(current>MaxSpeed?SprintBraking:Braking):(fast?SprintAcceleration:Acceleration);
        return Move(current,target,Math.Min(dt,.1f)*rate);
    }
    public void Step(float dt,float speed,float turn,bool requested) {
        if(!Finite(dt)||dt<=0||!Finite(speed)||!Finite(turn))return;
        // Substeps make foot contacts independent of 30/60/120 Hz rendering.
        // A stalled frame cannot skip an entire weight transfer.
        // Measured physics displacement can arrive in 50Hz pulses observed at
        // 60Hz. Clipping those pulses to commanded cruise loses root distance.
        dt=Math.Min(.1f,dt);speed=Clamp(speed,-SprintSpeed*4,SprintSpeed*4);turn=Clamp(turn,-4,4);
        int count=(int)Math.Ceiling(dt*120);float h=dt/count;
        for(int i=0;i<count;i++)Tick(h,speed,turn,requested);
    }
    void Tick(float dt,float speed,float turn,bool requested) {
        float omega=turn*TurnDegrees*(float)Math.PI/180;
        float lv=speed-omega*HalfTrack,rv=speed+omega*HalfTrack;
        float peak=Math.Max(Math.Abs(lv),Math.Abs(rv));
        Release=Move(Release,requested||peak>.015f||active||Amount>0?1:0,dt*2.5f);
        if(!Ready){speed=turn=omega=lv=rv=peak=0;}
        bool moving=peak>.015f;
        stillTime=moving?0:stillTime+dt;
        bool travelling=moving||(active&&stillTime<.12f);
        smoothSpeed=Blend(smoothSpeed,speed,dt,.16f);smoothTurn=Blend(smoothTurn,turn,dt,.2f);
        acceleration=Clamp(Blend(acceleration,(smoothSpeed-previousSpeed)/dt,dt,.35f),-SprintBraking,SprintAcceleration);previousSpeed=smoothSpeed;
        float runTarget=travelling?Smooth(Clamp((smoothSpeed-3.1f)/3.1f)):0;
        RunBlend=Move(RunBlend,runTarget,dt*(runTarget>RunBlend?1.4f:2));
        float filteredPeak=Math.Max(Math.Abs(smoothSpeed-smoothTurn*TurnDegrees*(float)Math.PI/180*HalfTrack),Math.Abs(smoothSpeed+smoothTurn*TurnDegrees*(float)Math.PI/180*HalfTrack));
        float targetAmount=travelling?Clamp(.22f+.78f*filteredPeak/MaxSpeed):active?.25f:0;
        Amount=Move(Amount,targetAmount,dt*2);
        if(moving){Left=Move(Left,lv/peak,dt*5);Right=Move(Right,rv/peak,dt*5);}
        else if(!travelling){Left=Move(Left,0,dt*5);Right=Move(Right,0,dt*5);}
        if(travelling) {
            float travelCadence=filteredPeak*Support/(2*TargetStride*(1-.10f*RunBlend));
            // A faster pivot needs earlier, shorter replacement steps; waiting
            // for the forward-stride clock forces a late foot snap at its limit.
            float turnCadence=Math.Abs(smoothTurn)*TurnDegrees*Support/35;
            frequency=Move(frequency,Clamp(Math.Max(travelCadence,turnCadence),.52f,1.6f-.3f*RunBlend),dt*1.5f);
            if(!active) {
                active=true;Phase=0;nextRight=false;
                left.age=Support/frequency*.25f;
                // The first step leaves neutral, so it has a shorter swing.
                BeginSwing(right,true,.24f/frequency,rv,false);
                nextStep=.25f/frequency;
            }
            Phase+=dt*frequency;Phase-=(float)Math.Floor(Phase);
            IntegrateSupport(left,false,speed,omega,dt);IntegrateSupport(right,true,speed,omega,dt);
            RetargetSwing(left,false,dt);RetargetSwing(right,true,dt);
            if(RunBlend>.6f)TryRunLift(speed,omega,dt);
            if(RunBlend<=.6f) {
                GuardSwingTime(left,right,true,speed,omega,dt);GuardSwingTime(right,left,false,speed,omega,dt);
            }
            if(stillTime>.04f&&RunBlend>0) {
                // A collision can remove all root motion between physics
                // ticks. Bring airborne legs down; do not finish a long run
                // swing as if the machine were still travelling forwards.
                if(left.swing)left.elapsed=Math.Min(left.duration,left.elapsed+dt*.25f);
                if(right.swing)right.elapsed=Math.Min(right.duration,right.elapsed+dt*.25f);
            }
            UpdateSwing(left,dt);UpdateSwing(right,dt);
            nextStep-=dt;
            if(!left.swing&&!right.swing) {
                bool reachLeft=AtReach(left,false,speed,omega),reachRight=AtReach(right,true,speed,omega);
                if(nextStep<=0||reachLeft||reachRight) {
                    bool side=reachLeft?false:reachRight?true:nextRight;
                    float travel=smoothSpeed+(side?1:-1)*smoothTurn*TurnDegrees*(float)Math.PI/180*HalfTrack;
                    BeginSwing(side?right:left,side,(1-Support)/frequency,travel,false);
                    nextRight=!side;nextStep=.5f/frequency;
                }
            } else if(RunBlend>.6f)TryRunLift(speed,omega,0);
        } else if(active) {
            // Place each foot independently instead of shrinking planted feet
            // toward zero. Replan an airborne foot once when braking ends.
            if(left.swing&&!left.settling)BeginSwing(left,false,.32f,0,true);
            if(right.swing&&!right.settling)BeginSwing(right,true,.32f,0,true);
            UpdateSwing(left,dt);UpdateSwing(right,dt);
            if(!left.swing&&!right.swing) {
                if(!Neutral(left))BeginSwing(left,false,.34f,0,true);
                else if(!Neutral(right))BeginSwing(right,true,.34f,0,true);
                else active=false;
            }
        }
        UpdateArm(left,dt);UpdateArm(right,dt);
        float asymmetry=(left.z-right.z)/(2*CurrentStride);
        float airborne=Math.Max(left.y,right.y)/CurrentLift;
        float weight=Flying?0:left.swing?-1:right.swing?1:nextRight?1:-1;
        // Follow the actual supporting leg even when acceleration shortens a
        // step. An unrelated phase oscillator would lean onto a lifted foot.
        // Compress over the real planted leg, then rise through its push-off.
        // Contact age, not a free-running phase or the swinging knee's height,
        // keeps this heave synchronized through starts and changed step timing.
        float heaveTarget=0;
        if(active) {
            if(Flying)heaveTarget=1;
            else {
                var support=left.swing?right:right.swing?left:left.age<=right.age?left:right;
                float t=Clamp(support.age/(Support/frequency));
                heaveTarget=.5f+.5f*(float)Math.Cos(2*Math.PI*t);
            }
        }
        runHeave=Blend(runHeave,heaveTarget,dt,.055f);
        float walkHeight=-.105f*Amount+.018f*Amount*(2*airborne-1);
        PelvisHeight=walkHeight+(SprintPelvisBase+SprintHeave*runHeave-walkHeight)*RunBlend;
        PelvisSide=Move(PelvisSide,(.042f-.02f*RunBlend)*Amount*weight,dt*.24f);
        PelvisYaw=-(5f+3f*RunBlend)*asymmetry;
        float walkLean=7.5f*Clamp(smoothSpeed/MaxSpeed,-1,1)+1.3f*Clamp(acceleration,-Braking,Acceleration)/Acceleration;
        float runLean=12+Clamp(6*acceleration/SprintAcceleration,-5,6);
        TorsoLean=Blend(TorsoLean,walkLean+(runLean-walkLean)*RunBlend,dt,.12f);
        TorsoRoll=Blend(TorsoRoll,2.5f*smoothTurn-1.5f*Amount*weight,dt,.15f);
        TorsoYaw=-.65f*PelvisYaw;
        if(!active&&Amount==0) {
            Phase=0;Left=Right=0;PelvisHeight=PelvisSide=PelvisYaw=TorsoYaw=0;
            left.reach=right.reach=Stride;
            // Exact neutral endpoints before retracting the armor.
            TorsoLean=Move(TorsoLean,0,dt*12);TorsoRoll=Move(TorsoRoll,0,dt*12);
        }
    }
    void TryRunLift(float speed,float omega,float anticipation) {
        // Evaluate take-off before the walking reach guard can prematurely
        // force the other foot down. A forward run intentionally transfers
        // load through a short flight interval.
        var candidate=nextRight?right:left;var other=nextRight?left:right;
        if(candidate.swing||!other.swing)return;
        bool reach=AtReach(candidate,nextRight,speed,omega);
        if((nextStep<=anticipation||reach)&&other.elapsed>=other.duration*(reach?.60f:.80f)) {
            float travel=smoothSpeed+(nextRight?1:-1)*smoothTurn*TurnDegrees*(float)Math.PI/180*HalfTrack;
            BeginSwing(candidate,nextRight,(1-Support)/frequency,travel,false);
            nextRight=!nextRight;nextStep=.5f/frequency;
        }
    }
    void IntegrateSupport(Leg leg,bool isRight,float speed,float omega,float dt) {
        if(leg.swing)return;
        float sign=isRight?-1:1;
        float x=sign*NeutralTrack+leg.x,z=NeutralForward+leg.z-speed*dt;
        float a=omega*dt,c=(float)Math.Cos(a),s=(float)Math.Sin(a);
        // Inverse root transform: support stays at its world point.
        leg.x=Clamp(c*x-s*z-sign*NeutralTrack,-.3f,.3f);
        float reach=Math.Max(TargetStride,leg.reach);
        leg.z=Clamp(s*x+c*z-NeutralForward,-reach,reach);
        leg.yaw=Clamp(leg.yaw-omega*180/(float)Math.PI*dt,-45,45);
        leg.age+=dt;
        float stance=Support/frequency,t=Clamp(leg.age/stance);
        float direction=Math.Sign(smoothSpeed-smoothTurn*TurnDegrees*(float)Math.PI/180*sign*HalfTrack);
        float pitch=direction*((9-3*RunBlend)*(1-Smooth(Clamp(t/.19f)))-(12+6*RunBlend)*Smooth(Clamp((t-.76f)/.24f)));
        leg.pitch=Move(leg.pitch,pitch,dt*150);
    }
    bool AtReach(Leg leg,bool isRight,float speed,float omega) {
        float vx=-omega*(NeutralForward+leg.z),vz=omega*((isRight?-1:1)*NeutralTrack+leg.x)-speed;
        return (Math.Abs(leg.z)>Math.Max(TargetStride,leg.reach)*.90f&&leg.z*vz>0)||
            (Math.Abs(leg.x)>.25f&&leg.x*vx>0)||(Math.Abs(leg.yaw)>38&&leg.yaw*omega<0);
    }
    void BeginSwing(Leg leg,bool isRight,float duration,float travel,bool settle) {
        leg.startX=leg.x;leg.startZ=leg.z;leg.startY=leg.y;leg.startYaw=leg.yaw;leg.startPitch=leg.pitch;
        leg.startVelocity=leg.swing?0:-travel;
        if(!settle&&RunBlend<.6f) {
            // Accelerating during a slow swing used to exhaust the planted
            // leg's reach. Land in time to transfer its load, rather than
            // clamping that supporting foot and skating it along the floor.
            var support=isRight?left:right;
            float sign=isRight?1:-1,x=sign*NeutralTrack+support.x,z=NeutralForward+support.z;
            float omega=smoothTurn*TurnDegrees*(float)Math.PI/180,available=duration/.90f;
            for(float t=.02f;t<=duration;t+=.02f) {
                float v=smoothSpeed+acceleration*(t+.16f),a=omega*.02f,c=(float)Math.Cos(a),s=(float)Math.Sin(a);
                float oldX=x,oldZ=z;
                z-=v*.02f;float nx=c*x-s*z;z=s*x+c*z;x=nx;
                float offsetX=x-sign*NeutralTrack,offsetZ=z-NeutralForward;
                if((Math.Abs(offsetZ)>Math.Max(TargetStride,support.reach)-.10f&&offsetZ*(z-oldZ)>0)||
                    (Math.Abs(offsetX)>.25f&&offsetX*(x-oldX)>0)||
                    (Math.Abs(support.yaw-omega*180/(float)Math.PI*t)>38&&support.yaw*omega<0)) {
                    available=t;break;
                }
            }
            duration=Math.Min(duration,Math.Max(.22f,available*.90f));
        }
        leg.elapsed=0;leg.duration=Math.Max(.2f,duration);leg.swing=true;leg.settling=settle;
        leg.reach=Math.Max(TargetStride,Math.Abs(leg.startZ));
        leg.targetX=settle?0:(isRight?.12f:-.12f);
        // Anticipate acceleration at landing, so the initial low-speed step
        // grows into the coming load rather than shuffling underneath it.
        float predicted=travel+acceleration*leg.duration*.65f;
        if(predicted*travel<0)predicted=0;
        leg.targetZ=settle?0:Clamp(predicted*Support/frequency*.5f,-TargetStride*.94f,TargetStride*.94f);
        leg.targetYaw=0;leg.endVelocity=settle?0:-travel;
        leg.height=settle?.13f:(Lift+(SprintLift-Lift)*RunBlend)*Clamp(.45f+.55f*Math.Abs(travel)/MaxSpeed,.45f,1);
    }
    void RetargetSwing(Leg leg,bool isRight,float dt) {
        if(!leg.swing||leg.settling)return;
        // Steering or reversing mid-air must change the landing footprint;
        // otherwise a foot lands on the wrong end of its next support arc.
        float travel=smoothSpeed+(isRight?1:-1)*smoothTurn*TurnDegrees*(float)Math.PI/180*HalfTrack;
        float predicted=travel+acceleration*Math.Max(0,leg.duration-leg.elapsed)*.65f;
        if(predicted*travel<0)predicted=0;
        float target=Clamp(predicted*Support/frequency*.5f,-TargetStride*.94f,TargetStride*.94f);
        leg.reach=Math.Max(leg.reach,TargetStride);
        leg.targetZ=Move(leg.targetZ,target,dt*2);
        leg.endVelocity=-travel;
    }
    void GuardSwingTime(Leg airborne,Leg support,bool supportRight,float speed,float omega,float dt) {
        if(!airborne.swing||airborne.settling||support.swing)return;
        // The landing forecast is made before the next physics samples exist.
        // If acceleration or a reversed turn consumes reach faster than that
        // forecast, advance the remaining swing continuously to accept load.
        float sign=supportRight?-1:1,x=sign*NeutralTrack+support.x,z=NeutralForward+support.z;
        float vx=-omega*z,vz=omega*x-speed,limit=float.PositiveInfinity;
        if(support.z*vz>0)limit=Math.Min(limit,(Math.Max(TargetStride,support.reach)-Math.Abs(support.z)-.045f)/Math.Max(.01f,Math.Abs(vz)));
        if(support.x*vx>0)limit=Math.Min(limit,(.3f-Math.Abs(support.x)-.025f)/Math.Max(.01f,Math.Abs(vx)));
        if(support.yaw*omega<0)limit=Math.Min(limit,(44-Math.Abs(support.yaw))/Math.Max(.01f,Math.Abs(omega)*180/(float)Math.PI));
        float remaining=airborne.duration-airborne.elapsed;
        float rate=Clamp(remaining/Math.Max(.02f,limit),1,3);
        airborne.elapsed=Math.Min(airborne.duration,airborne.elapsed+dt*(rate-1));
    }
    void UpdateSwing(Leg leg,float dt) {
        if(!leg.swing)return;
        leg.elapsed=Math.Min(leg.duration,leg.elapsed+dt);float u=leg.elapsed/leg.duration,t=Smooth(u);
        leg.x=leg.startX+(leg.targetX-leg.startX)*t;
        leg.z=Clamp(Hermite(leg.startZ,leg.targetZ,leg.startVelocity,leg.endVelocity,u,leg.duration),-leg.reach,leg.reach);
        float arch=(float)Math.Sin(Math.PI*u);arch*=arch;
        leg.y=Clamp(leg.startY*(1-t)+(leg.height-.5f*leg.startY)*arch,0,SprintLift);
        leg.yaw=leg.startYaw+(leg.targetYaw-leg.startYaw)*t;
        float landingPitch=leg.settling?0:(9-3*RunBlend)*Clamp(leg.targetZ/.25f,-1,1);
        leg.pitch=leg.startPitch+(landingPitch-leg.startPitch)*t;
        if(leg.elapsed>=leg.duration) {
            if(leg==right)RightFootfalls++;else LeftFootfalls++;
            leg.swing=false;leg.x=leg.targetX;leg.z=leg.targetZ;leg.y=0;leg.yaw=leg.targetYaw;leg.pitch=landingPitch;leg.age=0;
        }
    }
    void UpdateArm(Leg leg,float dt) {
        // +X swings the hanging same-side arm backward when its foot is forward.
        float target=Clamp(leg.z/CurrentStride,-1,1)*(ArmSwing+(SprintArmSwing-ArmSwing)*RunBlend);
        leg.arm=Move(leg.arm,target,dt*(150+(SprintArmSpeed-150)*RunBlend));
    }
    static bool Neutral(Leg leg)=>!leg.swing&&leg.x==0&&leg.z==0&&leg.y==0&&leg.yaw==0&&leg.pitch==0;
    public void Foot(bool isRight,out float forward,out float lift,out float swing) {
        var leg=isRight?right:left;forward=leg.z;lift=leg.y;swing=leg.arm;
    }
    public float FootSide(bool isRight)=>(isRight?right:left).x;
    public float FootPitch(bool isRight)=>(isRight?right:left).pitch;
    public float FootYaw(bool isRight)=>(isRight?right:left).yaw;
    public bool FootGrounded(bool isRight)=>!(isRight?right:left).swing;
    public float Elbow(bool isRight) {
        float arm=(isRight?right:left).arm;
        // Open the elbow on the backswing so the hand can pass the hip,
        // rather than keeping both forearms permanently in front of the chest.
        float walk=8*Amount+9*Math.Max(0,-arm/ArmSwing),run=55+15*Math.Max(0,-arm/SprintArmSwing)-25*Math.Max(0,arm/SprintArmSwing);
        return walk+(run-walk)*RunBlend;
    }
    public void Reset() {
        LeftFootfalls=RightFootfalls=0;
        Clear(left);Clear(right);active=false;nextRight=true;frequency=.6f;nextStep=smoothSpeed=smoothTurn=previousSpeed=acceleration=stillTime=runHeave=0;
        Phase=Amount=Release=Left=Right=PelvisHeight=PelvisSide=PelvisYaw=TorsoLean=TorsoRoll=TorsoYaw=RunBlend=0;
    }
    static void Clear(Leg leg) {
        leg.x=leg.z=leg.y=leg.yaw=leg.pitch=leg.arm=leg.age=leg.reach=leg.elapsed=leg.duration=0;leg.swing=leg.settling=false;
        leg.startX=leg.startZ=leg.startY=leg.startYaw=leg.startPitch=leg.startVelocity=leg.targetX=leg.targetZ=leg.targetYaw=leg.height=leg.endVelocity=0;
    }
}
}
