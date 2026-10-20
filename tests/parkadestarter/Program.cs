using System;
using Rachel350Z;

static class Program {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Main() {
        var idle=ParkadeStarterPose.At(3,0,false);
        Check(idle.Raise==0&&idle.Crouch==0&&idle.Across==0,"Starter is standing before the signal");
        var raised=ParkadeStarterPose.At(2,0,false);
        Check(raised.Raise==1&&raised.Crouch==0,"Both hands fully raised during the countdown");
        var held=ParkadeStarterPose.At(.32f,0,false);
        Check(held.Raise==1&&held.Crouch==0,"Holds arms up until the final signal");
        var drop=ParkadeStarterPose.At(.16f,0,false);
        Check(drop.Raise>.45f&&drop.Raise<.55f&&drop.Crouch>.45f,"Visible arm drop and knee bend before GO");
        var cue=ParkadeStarterPose.At(0,0,false);
        var go=ParkadeStarterPose.At(0,0,true);
        Check(cue.Raise==0&&cue.Crouch==1,"The crouch/drop finishes exactly on GO");
        Check(go.Crouch==cue.Crouch&&go.Across==cue.Across,"No pose discontinuity when driving releases");
        Check(ParkadeStarterPose.At(0,.5f,true).Across>2.3f,"Starter rapidly clears the centre of the driving lane");
        var clear=ParkadeStarterPose.At(0,2,true);
        Check(clear.Across==10f&&clear.Crouch==0&&clear.Run==0&&clear.Turn==0,"Starter finishes upright beside the track facing the car");
        float previous=0;
        for(int frame=0;frame<=600;frame++) {
            float t=frame/120f;
            var pose=ParkadeStarterPose.At(0,t,true);
            Check(pose.Across>=previous&&pose.Across<=ParkadeStarterPose.ShoulderDistance,"Escape moves only towards the roadside");previous=pose.Across;
            Check(pose.Raise>=0&&pose.Raise<=1&&pose.Crouch>=0&&pose.Crouch<=1&&pose.Run>=0&&pose.Run<=1&&pose.Turn>=0&&pose.Turn<=1,"All pose blends bounded");
            var paused=ParkadeStarterPose.At(0,t,true);
            Check(paused.Across==pose.Across&&paused.Crouch==pose.Crouch&&paused.Stride==pose.Stride,"Paused clock resamples an identical pose");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1}) {
            var pose=ParkadeStarterPose.At(bad,bad,true);
            Check(!float.IsNaN(pose.Across)&&!float.IsNaN(pose.Crouch)&&!float.IsNaN(pose.Stride),"Invalid clock cannot poison transforms");
        }
        Geometry();
        Console.WriteLine(checks+" Parkade starter timing checks passed; native rig rendering requires Unity verification.");
    }
    static void Geometry() {
        var origin=ParkadeLayout.AtDistance(0);
        float startX=origin.X+origin.TangentX*ParkadeStarterPose.StartAhead,startZ=origin.Z+origin.TangentZ*ParkadeStarterPose.StartAhead;
        float minimum=float.MaxValue;
        for(int frame=0;frame<=180;frame++) {
            var pose=ParkadeStarterPose.At(0,frame/120f,true);
            float x=startX-origin.TangentZ*pose.Across,z=startZ+origin.TangentX*pose.Across;
            foreach(var piece in ParkadePlan.Pieces) {
                float hx=piece.Prefab=="stone_floor"?2:piece.Prefab=="stone_wall_2x1"?1:.55f;
                float hz=piece.Prefab=="stone_floor"?2:piece.Prefab=="stone_wall_2x1"?.5f:.55f;
                double a=piece.Yaw*Math.PI/180,c=Math.Cos(a),s=Math.Sin(a),dx=x-piece.X,dz=z-piece.Z;
                double localX=c*dx-s*dz,localZ=s*dx+c*dz;
                double outsideX=Math.Max(0,Math.Abs(localX)-hx),outsideZ=Math.Max(0,Math.Abs(localZ)-hz);
                float clearance=(float)Math.Sqrt(outsideX*outsideX+outsideZ*outsideZ);
                minimum=Math.Min(minimum,clearance);
                Check(clearance>.69f,"Entire lateral escape stays clear of the saved native construction footprint");
            }
        }
        Check(ParkadeStarterPose.ShoulderDistance>origin.HalfWidth,"Starter finishes beyond the paved driving width");
        Console.WriteLine("Starter path native piece clearance: "+minimum.ToString("F3")+" m centre / "+(minimum-.5f).ToString("F3")+" m with a 0.5 m visual body radius.");
    }
}
