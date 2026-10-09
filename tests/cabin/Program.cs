using System;
using System.Linq;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static void Frames(CabinTransition m,ShowroomState door,int n,bool blocked=false) {
        for(int i=0;i<n;i++){m.Step(.02f,blocked,door.Progress[1]);m.ApplyDoorRequest(door);door.Step(blocked?0:.02f,false,0);}
    }
    static void Main(string[] args) {
        var motion=new CabinTransition();var door=new ShowroomState();
        Check(!motion.CanDrive&&!motion.BeginExit(),"approach cannot drive or exit");
        motion.Arrive();Frames(motion,door,25);Check(!motion.CanDrive&&motion.SeatBlend==0,"wait for actual door opening");
        float progress=door.Progress[1];Frames(motion,door,100,true);Check(door.Progress[1]==progress&&motion.SeatBlend==0,"menu/focus pause retains transition");
        Frames(motion,door,30);Check(motion.Phase==CabinPhase.Enter,"enter after door is open");
        Frames(motion,door,25);Check(motion.SeatBlend>0&&motion.SeatBlend<1&&!motion.CanDrive,"seat blend is gradual and driving blocked");
        Frames(motion,door,200);Check(motion.CanDrive&&motion.SeatBlend==1&&door.Progress[1]==0,"driving begins after seating and closed door");
        door.Toggle(1,true);Frames(motion,door,80);
        Check(motion.CanDrive&&door.Open[1]&&door.Progress[1]==1,"seated driver can open door without animation overriding it");
        door.Toggle(1,true);Frames(motion,door,80);
        Check(!door.Open[1]&&door.Progress[1]==0,"seated driver can close door again");
        Check(motion.BeginExit()&&!motion.BeginExit(),"repeated exit requests do not restart animation");
        Frames(motion,door,20);Check(motion.SeatBlend==1&&!motion.CanDrive,"remain seated while exit door opens");
        motion.CancelExit();Check(motion.CanDrive&&motion.SeatBlend==1,"blocked exit can return to seat");
        motion.BeginExit();Frames(motion,door,250);Check(motion.Phase==CabinPhase.Complete&&motion.SeatBlend==0,"exit completes standing outside");
        var invalid=new CabinTransition();invalid.Arrive();invalid.Step(float.NaN,false,0);invalid.Step(float.PositiveInfinity,false,0);
        Check(invalid.TimeInPhase==0,"invalid elapsed times ignored");
        Check(CabinAppearance.SteeringDegrees(20)==80&&CabinAppearance.SteeringDegrees(-20)==-80,"corrected steering follows road wheels about driver-facing axis");
        // The wheel axis points predominantly towards the driver (-Z). Positive rotation
        // moves its right-hand side down: clockwise as seen from the driver's seat.
        double angle=CabinAppearance.SteeringDegrees(20)*Math.PI/180;
        Check(-.934839*Math.Sin(angle)<0,"right turn moves right-hand grip clockwise, not left");
        Check(CabinAppearance.WindowAlpha(0)<.05&&CabinAppearance.WindowAlpha(1)>.95,"tint endpoints run from clear to dark");
        Check(CabinAppearance.WindowAlpha(.3f)<CabinAppearance.WindowAlpha(.7f)&&CabinAppearance.Tint(2)==1&&CabinAppearance.Tint(-1)==0,"tint is monotonic and bounded");
        Check(CabinAppearance.Tint(float.NaN)==.16f&&CabinAppearance.SteeringDegrees(float.PositiveInfinity)==0,"invalid appearance settings have safe fallbacks");
        foreach(float height in new[]{.8f,1.0f,1.2f}) {
            float correction=CabinAppearance.RoofLowering(height,CabinAppearance.DriverScale);
            Check(correction>=0&&height+.18f*CabinAppearance.DriverScale-correction<=CabinAppearance.HeadCeiling+.00001f,"head clearance at measured bone height "+height);
        }
        var model=ModelFile.Load(args[0]);
        Check(model.preset=="LOCAL_BEAMNG_350Z_CABIN"&&model.parts.Length>20,"runtime validates actual imported cabin");
        Check(model.parts.Any(p=>p.name.StartsWith("Cabin_Nis350Z_seat_FL_"))&&model.parts.Any(p=>p.name.StartsWith("Cabin_Nis350Z_seat_FR_")),"both seats present");
        Check(model.parts.Any(p=>p.movingPart=="rightDoor")&&model.parts.Any(p=>p.movingPart=="leftDoor"),"door panels mapped to mirrored NFS door pivots");
        Check(model.parts.Any(p=>p.name.StartsWith("Cabin_Nis350Z_steer_"))&&model.textures.Length>0,"separate steering geometry and embedded textures");
        Check(model.parts.SelectMany(p=>p.vertices).All(v=>Math.Abs(v.x)<.9&&v.y>0&&v.y<1.25&&Math.Abs(v.z)<1.7),"cabin fits expected 350Z interior bounds");
        Console.WriteLine(checks+" cabin checks passed (no Unity runtime).");
    }
}
