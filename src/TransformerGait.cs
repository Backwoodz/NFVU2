using System;
namespace Rachel350Z {
// Managed timing shared by the runtime controller and its tests. Units are
// metres, seconds and degrees; the asset review checks the resulting pose domain.
public sealed class TransformerGait {
    public const int Version=2;
    public const float Stride=.48f,Lift=.16f,MaxSpeed=2.1f,ReverseSpeed=1.4f,TurnDegrees=32f,HalfTrack=.88f,ArmSwing=20f;
    public const float Acceleration=.65f,Braking=1.8f;
    public float Phase {get;private set;}
    public float Amount {get;private set;}
    public float Release {get;private set;}
    public float Left {get;private set;}
    public float Right {get;private set;}
    public bool Ready=>Release>=1;
    public bool Settled=>Amount==0&&Release==0;
    static float Move(float value,float target,float step)=>value<target?Math.Min(target,value+step):Math.Max(target,value-step);
    static float Clamp(float value)=>Math.Max(0,Math.Min(1,value));
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    public static float AdvanceSpeed(float current,float input,float dt,bool ready) {
        if(!Finite(current)||!Finite(input)||!Finite(dt)||dt<=0)return Finite(current)?current:0;
        float target=ready?Math.Max(-1,Math.Min(1,input))*(input<0?ReverseSpeed:MaxSpeed):0;
        // A reversal first brakes to rest, then builds speed in the new direction.
        if(current*target<0)target=0;
        float rate=Math.Abs(target)<Math.Abs(current)?Braking:Acceleration;
        return Move(current,target,Math.Min(dt,.1f)*rate);
    }
    public void Step(float dt,float speed,float turn,bool requested) {
        if(!Finite(dt)||dt<=0||!Finite(speed)||!Finite(turn))return;
        dt=Math.Min(.1f,dt);
        float left=speed-turn*(TurnDegrees*(float)Math.PI/180)*HalfTrack;
        float right=speed+turn*(TurnDegrees*(float)Math.PI/180)*HalfTrack;
        float peak=Math.Max(Math.Abs(left),Math.Abs(right));
        // Open the knee armor before lifting a foot. Keep it clear until both
        // feet have settled, then close it before allowing transformation.
        Release=Move(Release,requested||peak>.001f||Amount>0?1:0,dt*2.5f);
        if(!Ready)peak=0;
        Amount=Move(Amount,peak>.001f?Clamp(.3f+.7f*peak/MaxSpeed):0,dt*2.5f);
        if(peak>.001f) {
            Left=Move(Left,left/peak,dt*6);Right=Move(Right,right/peak,dt*6);
            Phase+=dt*peak/(4*Stride*Math.Max(Amount,.05f));
        } else if(Amount>0)Phase+=dt*.7f;
        Phase-=(float)Math.Floor(Phase);
        if(Amount==0){Phase=0;Left=Right=0;}
    }
    public void Foot(bool right,out float forward,out float lift,out float swing) {
        float cycle=Phase+(right?.5f:0);cycle-=(float)Math.Floor(cycle);
        float u=(cycle-.5f)*2;u=u*u*(3-2*u);
        float direction=right?Right:Left;
        forward=Stride*(cycle<.5f?1-4*cycle:-1+2*u)*Amount*direction;
        lift=cycle<.5f?0:(float)Math.Sin((cycle-.5f)*Math.PI*2)*Lift*Amount;
        // Positive X swings the hanging arm backwards when its same-side foot
        // is forwards. Cosine aligns the extrema with foot placement.
        swing=(float)Math.Cos(cycle*Math.PI*2)*ArmSwing*Amount*direction;
    }
    public float Elbow(bool right) {
        Foot(right,out _,out _,out float swing);
        return 6*Amount+4*Math.Max(0,-swing/ArmSwing);
    }
    public void Reset(){Phase=Amount=Release=Left=Right=0;}
}
}
