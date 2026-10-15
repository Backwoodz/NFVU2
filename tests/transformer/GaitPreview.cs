using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Rachel350Z;
// Actual controller samples on ideal flat ground; Unity physics is not run.
static class GaitPreview {
    static float Move(float a,float b,float d)=>a<b?Math.Min(b,a+d):Math.Max(b,a-d);
    static object Foot(TransformerGait gait,bool right) {
        gait.Foot(right,out float forward,out float lift,out float swing);
        return new {forward,lift,swing,elbow=gait.Elbow(right),side=gait.FootSide(right),
            pitch=gait.FootPitch(right),yaw=gait.FootYaw(right),grounded=gait.FootGrounded(right)};
    }
    static object Pose(TransformerGait gait,string scenario,float time,float speed,float turn,float x,float z,float yaw) => new {
        scenario,time,speed,turn,x,z,yaw,phase=gait.Phase,amount=gait.Amount,release=gait.Release,left=gait.Left,right=gait.Right,
        leftFoot=Foot(gait,false),rightFoot=Foot(gait,true),pelvisHeight=gait.PelvisHeight,pelvisSide=gait.PelvisSide,
        pelvisYaw=gait.PelvisYaw,torsoLean=gait.TorsoLean,torsoRoll=gait.TorsoRoll,torsoYaw=gait.TorsoYaw};
    static void Input(float t,out float forward,out float steer,out string scenario) {
        forward=steer=0;
        if(t<.5f)scenario="idle";
        else if(t<6.5f){scenario="accelerate-cruise";forward=1;}
        else if(t<9){scenario="brake-and-settle";}
        else if(t<12){scenario="left-pivot";steer=-1;}
        else if(t<15){scenario="right-pivot-reversal";steer=1;}
        else if(t<19){scenario="restart-curved-forward";forward=1;steer=-.45f;}
        else if(t<24){scenario="forward-to-reverse";forward=-1;steer=.35f;}
        else if(t<28){scenario="reverse-to-forward";forward=1;steer=-.6f;}
        else if(t<30){scenario="release-mid-stride";}
        else if(t<30.35f){scenario="short-restart";forward=1;}
        else scenario="final-settle";
    }
    public static void Save(string path) {
        var gait=new TransformerGait();var frames=new List<object>();
        float speed=0,turn=0,x=0,z=0,yaw=0,lastX=0,lastZ=0,lastYaw=0;bool requested=false;string scenario="idle";
        // Exact 50Hz physics and 60Hz visual ticks on a common 600Hz clock.
        for(int tick=0;tick<=20400;tick++) {
            float time=tick/600f;
            Input(time,out float forward,out float steer,out scenario);
            requested=forward!=0||steer!=0;
            if(tick%12==0) {
                speed=TransformerGait.AdvanceSpeed(speed,forward,.02f,gait.Ready);
                turn=Move(turn,gait.Ready?steer:0,.02f*3);
                yaw+=turn*TransformerGait.TurnDegrees*.02f;
                x+=(float)Math.Sin(yaw*Math.PI/180)*speed*.02f;
                z+=(float)Math.Cos(yaw*Math.PI/180)*speed*.02f;
            }
            if(tick%10==0) {
                // Runtime samples interpolated car transforms, not requested
                // velocity. This ideal fixture uses held50Hz transforms and
                // intentionally retains their60Hz observation pulses.
                float measured=((x-lastX)*(float)Math.Sin(lastYaw*Math.PI/180)+(z-lastZ)*(float)Math.Cos(lastYaw*Math.PI/180))*60;
                float measuredTurn=(yaw-lastYaw)*60/TransformerGait.TurnDegrees;
                gait.Step(1f/60,measured,measuredTurn,requested);
                frames.Add(Pose(gait,scenario,time,measured,measuredTurn,x,z,yaw));
                lastX=x;lastZ=z;lastYaw=yaw;
            }
        }
        if(!gait.Settled)throw new Exception("Preview must finish settled");
        File.WriteAllText(path,JsonConvert.SerializeObject(frames,Formatting.Indented));
        var domain=new List<object>();
        // Real controller states cover partial/full travel and both turn signs.
        foreach(float fraction in new[]{.1f,.25f,.5f,.75f,1f})
        foreach(int direction in new[]{-1,1})
        foreach(float steering in new[]{-1f,0,1f}) {
            float v=fraction*(direction<0?-TransformerGait.ReverseSpeed:TransformerGait.MaxSpeed);
            Domain(domain,"travel/"+fraction+"/"+direction+"/"+steering,v,steering);
        }
        foreach(float steering in new[]{-1f,-.5f,-.2f,.2f,.5f,1f})Domain(domain,"pivot/"+steering,0,steering);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path),"runtime-gait-domain.json"),JsonConvert.SerializeObject(domain,Formatting.Indented));
    }
    static void Domain(List<object> frames,string scenario,float speed,float turn) {
        var gait=new TransformerGait();float x=0,z=0,yaw=0;const float dt=1f/60;
        for(int tick=0;tick<=360;tick++) {
            gait.Step(dt,speed,turn,true);
            yaw+=turn*TransformerGait.TurnDegrees*dt;
            x+=(float)Math.Sin(yaw*Math.PI/180)*speed*dt;z+=(float)Math.Cos(yaw*Math.PI/180)*speed*dt;
            if(tick%3==0)frames.Add(Pose(gait,scenario,tick*dt,speed,turn,x,z,yaw));
        }
    }
}
