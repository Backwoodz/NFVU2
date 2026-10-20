using System;

namespace Rachel350Z {
// Absolute race clocks keep this authored gesture stationary while the race is
// paused. No Animator clock, random state or gameplay controller participates.
internal readonly struct ParkadeStarterPose {
    internal readonly float Raise,Crouch,Across,Run,Stride,Turn;
    internal const float StartAhead=2f;
    // The native barrier is centred at 11.2 m (0.5 m half-thickness).
    // A 10 m destination leaves a 0.7 m verge gap for the visual body.
    internal const float ShoulderDistance=10f;
    ParkadeStarterPose(float raise,float crouch,float across,float run,float stride,float turn) {
        Raise=raise;Crouch=crouch;Across=across;Run=run;Stride=stride;Turn=turn;
    }
    static float Clamp(float x)=>Math.Max(0,Math.Min(1,x));
    static float Smooth(float x){x=Clamp(x);return x*x*(3-2*x);}
    internal static ParkadeStarterPose At(float countdownRemaining,float elapsedSinceGo,bool started) {
        float remaining=float.IsNaN(countdownRemaining)||float.IsInfinity(countdownRemaining)?3:Math.Max(0,Math.Min(3,countdownRemaining));
        if(!started) {
            float raise=Smooth((3-remaining)/.75f),drop=Smooth((.32f-remaining)/.32f);
            return new ParkadeStarterPose(raise*(1-drop),drop,0,0,0,0);
        }
        float t=float.IsNaN(elapsedSinceGo)||float.IsInfinity(elapsedSinceGo)?0:Math.Max(0,elapsedSinceGo);
        // Accelerates sideways at GO; by 0.5 s the actor is more than 2 m
        // outside the centre line. Stops on the verge before the spectators.
        float travel=Clamp((t-.07f)/1.38f);
        float across=ShoulderDistance*Smooth(travel);
        float run=Smooth(t/.12f)*(1-Smooth((t-1.26f)/.22f));
        return new ParkadeStarterPose(0,1-Smooth(t/.35f),across,run,t*4.1f*(float)(2*Math.PI),Smooth(t/.17f)*(1-Smooth((t-1.46f)/.3f)));
    }
}
}
