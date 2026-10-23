using System;
using System.Linq;
using UnityEngine;
using Rachel350Z;

public sealed partial class SmokeProbe {
 void CheckFullWheelWrap() {
  foreach(int hz in new[]{60,144})foreach(int direction in new[]{1,-1}) {
   car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;
   rpm=850*direction;rightGround=true;Reset();for(int i=0;i<hz*3;i++)Step(1f/hz);
   var native=Particles(0);
   // Births are initially invisible at the contact point; track a wisp only
   // after its first movement has placed it on the visible tyre shoulder.
   var tracked=native.Where(p=>p.startLifetime<1.1f&&p.startLifetime>.85f&&p.startLifetime-p.remainingLifetime>.001f&&p.startLifetime-p.remainingLifetime<.06f)
    .OrderBy(p=>p.startLifetime-p.remainingLifetime).First();
   int index=Array.FindIndex(native,p=>p.randomSeed==tracked.randomSeed);
   var plumes=(Array)typeof(VehicleBurnoutEffects).GetField("plumes",F).GetValue(effect);var plume=plumes.GetValue(0);
   var puffs=(Array)plume.GetType().GetField("Puffs",F).GetValue(plume);var puff=puffs.GetValue(index);
   float orbit=(float)puff.GetType().GetField("Orbit",F).GetValue(puff);
   Func<Vector3,float> angle=p=>Mathf.Atan2(-Vector3.Dot(p,car.transform.forward),-Vector3.Dot(p,car.transform.up))*Mathf.Rad2Deg;
   var relative=tracked.position-visuals[2].position;var previousLocal=car.transform.InverseTransformDirection(relative);float previous=angle(relative),travel=previous*direction;
   float maximum=travel,startWidth=Mathf.Abs(Vector3.Dot(relative,car.transform.right)),peel=0,maxStep=0;int closingVisible=0;
   for(int frame=0;frame<hz*.65f;frame++) {
    float age=tracked.startLifetime-tracked.remainingLifetime;
    body.linearVelocity=car.transform.forward*8+car.transform.right*2;body.angularVelocity=Vector3.up*1.2f;
    car.transform.position+=body.linearVelocity/hz;car.transform.rotation=Quaternion.AngleAxis(1.2f*Mathf.Rad2Deg/hz,Vector3.up)*car.transform.rotation;
    Step(1f/hz);var next=Particles(0).Single(p=>p.randomSeed==tracked.randomSeed);
    if(age+1f/hz<=orbit+.00001f) {
     var local=next.position-visuals[2].position;float now=angle(local);
     travel+=Mathf.DeltaAngle(previous,now)*direction;maximum=Mathf.Max(maximum,travel);previous=now;
     var currentLocal=car.transform.InverseTransformDirection(local);float displacement=Vector3.Distance(currentLocal,previousLocal);
     if(displacement>.18f)report.Add("Wrap trace large step: frame="+frame+", age="+age+", orbit="+orbit+", old="+previousLocal+", new="+currentLocal+", distance="+displacement);
     maxStep=Mathf.Max(maxStep,displacement);previousLocal=currentLocal;
     peel=Mathf.Max(peel,Mathf.Abs(Vector3.Dot(local,car.transform.right))-startWidth);
     if(travel>300&&travel<350&&next.startColor.a>20)closingVisible++;
     Check(next.position.y>=.02f,"full-turn smoke stays above the road at "+hz+" Hz, direction="+direction);
     relative=local;
    }
    tracked=next;
   }
   Check(maximum>345&&maximum<400,"the same native wisp travels approximately one complete turn while attached, direction="+direction+", hz="+hz);
   Check(closingVisible>0,"the previously missing final quarter remains visible before release");
   Check(peel>.008f&&peel<.014f,"full-turn wisp peels gently outward without a wide detached ring");
   Check(maxStep<.18f,"longer wheel wrap has no abrupt per-frame position jump: max="+maxStep);
   var before=tracked.position;car.transform.position+=car.transform.right*1.2f;Step(1f/hz);
   tracked=Particles(0).Single(p=>p.randomSeed==tracked.randomSeed);
   Check(Vector3.Distance(before,tracked.position)<.35f,"completed full-turn wisp detaches and stops following wheel translation");
   report.Add("Full native wheel wrap "+hz+" Hz, direction="+direction+": attached travel="+maximum+" degrees, outward peel="+peel+" m, visible closing samples="+closingVisible+", maximum step="+maxStep+" m");
   car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;Reset();
   for(int i=0;i<hz*3;i++)Step(1f/hz);
   CaptureWisps("wheel-full-wrap-"+hz+"-"+direction+".png",new Vector3(-2.7f,.8f,-2.8f),visuals[2].position);
  }
  rpm=850;car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;Reset();for(int i=0;i<180;i++)Step();
 }
}
