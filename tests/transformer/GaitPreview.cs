using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Rachel350Z;
// Records the actual managed gait controller for the Blender preview. The
// trajectory supplies ideal flat ground; it does not emulate Unity collisions.
static class GaitPreview {
    static float Move(float a,float b,float d)=>a<b?Math.Min(b,a+d):Math.Max(b,a-d);
    public static void Save(string path) {
        var gait=new TransformerGait();var frames=new List<object>();
        float speed=0,turn=0,x=0,z=0,yaw=0;bool requested=false;
        for(int tick=0;tick<=7200;tick++) {
            float time=tick/600f;
            float forward=time>=.25f&&time<5.5f?1:time>=7.5f&&time<9.5f?-1:0;
            float steer=time>=5.5f&&time<7.5f?1:0;
            if(tick%12==0) {
                requested=forward!=0||steer!=0;
                speed=TransformerGait.AdvanceSpeed(speed,forward,.02f,gait.Ready);
                turn=Move(turn,gait.Ready?steer:0,.02f*3);
                yaw+=turn*TransformerGait.TurnDegrees*.02f;
                x+=(float)Math.Sin(yaw*Math.PI/180)*speed*.02f;
                z+=(float)Math.Cos(yaw*Math.PI/180)*speed*.02f;
            }
            if(tick%10==0)gait.Step(1f/60,speed,turn,requested);
            if(tick%25==0)frames.Add(new {phase=gait.Phase,amount=gait.Amount,release=gait.Release,left=gait.Left,right=gait.Right,x,z,yaw});
        }
        if(!gait.Settled)throw new Exception("Preview must finish settled");
        File.WriteAllText(path,JsonConvert.SerializeObject(frames,Formatting.Indented));
    }
}
