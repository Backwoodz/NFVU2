using System;
using Rachel350Z;
using UnityEngine;
class Program {
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static void Model() {
        var shot=new TransformerCinematicCamera();
        shot.Step(.1f,true,false,.6f,false);Check(!shot.Active&&!shot.Releasing,"observing a restored or parked robot cannot arm a cinematic");
        shot.Begin();var initial=shot.Pose;
        Check(shot.Active&&Near(initial.Pullback,0)&&Near(initial.Rise,0)&&Near(initial.YawBlend,0)&&Near(initial.Zoom,0),"explicit deployment starts at the existing view without zoom or orbit offsets");
        shot.Step(.01f,true,false,.4f,false);var pose=shot.Pose;
        for(int i=0;i<20;i++)shot.Step(.1f,true,false,.4f,false);
        Check(Near(shot.Pose.Orbit,pose.Orbit)&&Near(shot.Pose.Rise,pose.Rise),"wall time cannot move the camera ahead of a stalled animation");
        shot.Step(.1f,true,false,.8f,true);Check(Near(shot.Pose.Orbit,pose.Orbit),"blocked camera holds its authoritative animation sample");
        foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})shot.Step(dt,true,false,.8f,false);
        Check(Near(shot.Pose.Orbit,pose.Orbit),"invalid or paused time cannot move the shot");
        bool bounded=true,continuous=true;var previous=TransformerCinematicCamera.Sample(0);
        for(int i=1;i<=10000;i++) {
            var p=TransformerCinematicCamera.Sample(i/10000f);
            bounded&=p.Orbit>=0&&p.Orbit<=45&&p.Zoom>=0&&p.Zoom<=5&&Math.Abs(p.Impulse)<=.2f;
            continuous&=p.Orbit>=previous.Orbit&&p.Rise>=previous.Rise&&p.Dolly>=previous.Dolly&&Math.Abs(p.Orbit-previous.Orbit)<.02f;
            previous=p;
        }
        Check(bounded&&continuous,"shot curves have bounded zoom/impulse and a smooth monotonic 45-degree main arc");
        var last=TransformerCinematicCamera.Sample(1);
        Check(Near(last.Orbit,45)&&Near(last.Rise,1)&&Near(last.Dolly,1)&&Near(last.PitchBlend,1)&&Near(last.Impulse,0),"end pose finishes the dolly, rise and low-angle view with no residual impact");
        Check(TransformerCinematicCamera.Sample(.6f).Impulse==0&&Math.Abs(TransformerCinematicCamera.Sample(.76f).Impulse)>.05f&&TransformerCinematicCamera.Sample(.9f).Impulse==0,"short final impulse follows the existing 3.75-second sub-drop cue rather than early movement");
        shot.Step(.01f,true,false,1,false);Check(!shot.Active&&shot.Releasing&&shot.Weight==1,"completed transformation begins a continuous full-weight camera handoff");
        shot.Step(.1f,true,false,1,true);Check(shot.Weight==1,"pause also freezes the handoff");
        float prior=1;
        for(int i=0;i<100;i++){shot.Step(.01f,true,false,1,false);CheckWeight(shot.Weight,ref prior);}
        Check(!shot.Active&&!shot.Releasing&&shot.Weight==0,"handoff finishes and cannot restart while the robot stands at one");
        foreach(bool returning in new[]{false,true}) {
            shot.Begin();shot.Step(.01f,true,false,.5f,false);
            shot.Step(.01f,!returning,returning,returning?.5f:.3f,false);
            Check(!shot.Active&&shot.Releasing,"return or rewound deployment exits the forward-only shot");
        }
        shot.Begin();shot.Step(.01f,true,false,.5f,false);shot.Cancel();shot.Cancel();
        shot.Step(.1f,true,false,.7f,false);float weight=shot.Weight;shot.Cancel();
        Check(shot.Releasing&&weight<1&&Near(shot.Weight,weight),"repeated look input cannot restart or prolong the handoff");
        shot.Step(20,true,false,1,false);Check(shot.Weight>0,"a large frame cannot skip the whole handoff");
        shot.Reset();Check(!shot.Active&&!shot.Releasing&&shot.Weight==0,"exit/despawn reset removes all shot history");
        shot.Begin();shot.Step(.01f,true,false,float.NaN,false);Check(!shot.Active,"non-finite animation progress cannot poison a camera pose");
        var same60=new TransformerCinematicCamera();var same120=new TransformerCinematicCamera();same60.Begin();same120.Begin();same60.Cancel();same120.Cancel();
        for(int i=0;i<30;i++)same60.Step(1f/60,true,false,1,false);for(int i=0;i<60;i++)same120.Step(1f/120,true,false,1,false);
        Check(Near(same60.Weight,same120.Weight),"handoff timing agrees at ordinary frame rates");
    }
    static void CheckWeight(float weight,ref float previous){if(weight<0||weight>previous+.00001f)throw new Exception("handoff reversed");previous=weight;}
    static void Adapter() {
        var gunView=new VehicleOrbitCamera();var scene=new Camera{fieldOfView=65};var sky=new Camera{fieldOfView=70};
        GameCamera.instance=new GameCamera{m_skyCamera=sky};gunView.Bind(scene,179);
        gunView.BeginTransformationShot();gunView.Render();
        Check(Near(gunView.ViewYaw,179)&&Near(gunView.ViewPitch,18)&&Near(gunView.Radius,6)&&Near(scene.fieldOfView,65),"production camera adapter begins exactly at the current view including a wrapped heading");
        gunView.TransformationFrame(.01f,true,false,.8f,false);gunView.Render();
        Check(Near(gunView.ViewYaw,90+TransformerCinematicCamera.Sample(.8f).Orbit)&&gunView.Radius>=9.5f&&gunView.Radius<=9.8f&&gunView.Pivot.y>2.5f,"production shot reaches the front-left while keeping the robot closer in frame");
        Check(scene.fieldOfView<65&&Near(scene.fieldOfView,sky.fieldOfView),"mild zoom updates both native scene and sky projection");
        float displayedYaw=gunView.ViewYaw,displayedPitch=gunView.ViewPitch,displayedRadius=gunView.Radius;
        gunView.TakeOver();
        Check(Near(gunView.ChaseYaw,displayedYaw)&&Near(gunView.OrbitPitch,displayedPitch)&&Near(gunView.BaseRadius,displayedRadius-3.5f),"manual takeover adopts the displayed orbit and robot distance instead of the old pre-shot view");
        gunView.Render();Check(Near(gunView.Radius,displayedRadius),"manual takeover starts with continuous framing");
        for(int i=0;i<90;i++)gunView.TransformationFrame(.01f,true,false,1,false);
        Check(Near(scene.fieldOfView,65)&&Near(sky.fieldOfView,70),"finished handoff restores both exact original FOV values");
        gunView.BeginTransformationShot();gunView.TransformationFrame(.01f,true,false,.7f,false);gunView.Render();gunView.StopShot();
        Check(Near(scene.fieldOfView,65)&&Near(sky.fieldOfView,70),"exit or camera rebind restores scene and sky projection immediately");
        gunView.BeginTransformationShot();gunView.TransformationFrame(.01f,true,false,.7f,false);gunView.Render();gunView.ReplaceCamera(new Camera{fieldOfView=90});gunView.Render();
        Check(Near(scene.fieldOfView,65)&&Near(sky.fieldOfView,70)&&!gunView.IsShot,"replacing the main camera releases old projections without touching a new camera");
        GameCamera.instance=null;var solo=new Camera{fieldOfView=20};gunView.Bind(solo,-170);gunView.BeginTransformationShot();gunView.Render();
        Check(Near(solo.fieldOfView,20),"initial cinematic does not change a custom narrow field of view");gunView.StopShot();
    }
    static void Main(){Model();Adapter();Console.WriteLine(checks+" cinematic camera checks passed; native framing/collision and controller feel require a game test.");}
}
