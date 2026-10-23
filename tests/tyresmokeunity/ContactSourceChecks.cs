using System;
using UnityEngine;
using Rachel350Z;

public sealed partial class SmokeProbe {
 void CheckContactSourceInterpolation() {
  var fixture=new GameObject("Sampled contact/render smoke fixture");
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Contact fixture road";
  ground.transform.position=new Vector3(0,2199.9f,0);ground.transform.localScale=new Vector3(200,.2f,200);
  var rigid=fixture.AddComponent<Rigidbody>();rigid.mass=1450;rigid.useGravity=false;rigid.interpolation=RigidbodyInterpolation.Interpolate;
  var colliders=new WheelCollider[4];var rendered=new Transform[4];
  var origin=new Vector3(0,2199.95f,0);fixture.transform.position=origin;rigid.position=origin;
  for(int i=0;i<4;i++) {
   var obj=new GameObject("Contact wheel "+i);obj.transform.SetParent(fixture.transform,false);
   obj.transform.localPosition=new Vector3(i%2==0?-.86f:.86f,.5f,i<2?1.25f:-1.2f);
   colliders[i]=obj.AddComponent<WheelCollider>();colliders[i].radius=.34f;colliders[i].suspensionDistance=.25f;
   var spring=colliders[i].suspensionSpring;spring.spring=35000;spring.damper=4500;colliders[i].suspensionSpring=spring;
   var visual=new GameObject("Rendered contact wheel "+i);visual.transform.SetParent(fixture.transform,false);visual.transform.localPosition=obj.transform.localPosition;rendered[i]=visual.transform;
  }
  DriftVehicleAdapter adapter=null;VehicleBurnoutEffects smoke=null;
  try {
   Physics.SyncTransforms();rigid.linearVelocity=Vector3.forward*(112f/3.6f);
   adapter=DriftVehicleAdapter.Create(rigid,colliders,s=>report.Add(s));
   Check(adapter.Step(true,0,0,0),"real drift adapter samples the native contact fixture");
   for(int i=0;i<4;i++) {
    Check(adapter.Grounded(i)&&adapter.ContactLoad(i)>0,"native wheel contact is loaded, wheel="+i);
    Check(Vector3.Distance(adapter.ContactHub(i),adapter.ContactPoint(i)+Vector3.up*colliders[i].radius)<.0005f,
     "sampled hub and contact share the same flat-road physics pose, wheel="+i);
   }
   smoke=VehicleBurnoutEffects.Create(fixture,null,colliders,rendered,s=>report.Add(s));Check(smoke!=null,"contact-source smoke fixture initializes");
   var read=typeof(VehicleBurnoutEffects).GetMethod("ReadSource",F);
   Func<int,bool,TyreSmokeContact> contact=(i,old)=> {
    var hub=adapter.ContactHub(i);
    if(old)adapter.TryGetWheelPose(i,out hub,out var rotation);
    return new TyreSmokeContact {Grounded=adapter.Grounded(i),Point=adapter.ContactPoint(i),Hub=hub,
     Normal=adapter.ContactNormal(i),Rpm=850,Load=adapter.ContactLoad(i)};
   };
   Func<int,TyreSmokeContact> corrected=i=>contact(i,false),legacy=i=>contact(i,true);
   Func<Func<int,TyreSmokeContact>,Vector3> sourceHub=provider=> {
    var source=read.Invoke(smoke,new object[]{2,provider});return (Vector3)source.GetType().GetField("Hub",F).GetValue(source);
   };
   foreach(int hz in new[]{30,60,144}) {
    int sampled=-1,held=0,oldHeld=0;float error=0,oldError=0,maxStepError=0;
    Vector3 previous=Vector3.zero,previousOld=Vector3.zero;
    float fixedDt=Time.fixedDeltaTime,speed=112f/3.6f;
    // Transform writes made to emulate interpolation also dirty the physics
    // transform. Reset both representations before SyncTransforms; otherwise
    // a preceding render pose can overwrite the deliberately sampled body pose.
    fixture.transform.SetPositionAndRotation(origin,Quaternion.identity);rigid.position=origin;rigid.rotation=Quaternion.identity;
    rigid.linearVelocity=Vector3.forward*speed;rigid.angularVelocity=Vector3.zero;Physics.SyncTransforms();
    for(int frame=0;frame<hz;frame++) {
     float t=frame/(float)hz;int tick=Mathf.FloorToInt(t/fixedDt);
     if(tick!=sampled) {
      var sampledPosition=origin+Vector3.forward*(tick*fixedDt*speed);
      fixture.transform.SetPositionAndRotation(sampledPosition,Quaternion.identity);rigid.position=sampledPosition;rigid.rotation=Quaternion.identity;
      rigid.linearVelocity=Vector3.forward*speed;rigid.angularVelocity=Vector3.zero;Physics.SyncTransforms();
      Check(Vector3.Distance(rigid.position,sampledPosition)<.0005f,"physics pose matches the intended contact sample at "+hz+" Hz");
      Check(adapter.Step(true,0,0,0),"sampled native contact step at "+hz+" Hz");sampled=tick;
      for(int i=0;i<4;i++) {
       float expectedZ=sampledPosition.z+(i<2?1.25f:-1.2f);
       Check(adapter.Grounded(i)&&Mathf.Abs(adapter.ContactPoint(i).z-expectedZ)<.0005f&&Mathf.Abs(adapter.ContactHub(i).z-expectedZ)<.0005f,
        "native contact and sampled hub match the analytic tick position at "+hz+" Hz, wheel="+i);
      }
     }
     // Hold the physics sample while its rendered chassis moves between ticks.
     // This is the same pose split used by Rigidbody interpolation in LateUpdate.
     fixture.transform.position=origin+Vector3.forward*((t-fixedDt)*speed);
     for(int i=0;i<4;i++) {adapter.TryGetWheelPose(i,out var hub,out var rotation);rendered[i].SetPositionAndRotation(hub,rotation);}
     Check(Mathf.Abs(rendered[2].position.z-((t-fixedDt)*speed-1.2f))<.0005f,"rendered hub matches the analytic render position at "+hz+" Hz");
     var current=sourceHub(corrected);var old=sourceHub(legacy);
     error=Mathf.Max(error,Vector3.Distance(current,rendered[2].position));oldError=Mathf.Max(oldError,Vector3.Distance(old,rendered[2].position));
     if(frame>0) {
      if(Vector3.Distance(current,previous)<.00001f)held++;
      if(Vector3.Distance(old,previousOld)<.00001f)oldHeld++;
      maxStepError=Mathf.Max(maxStepError,Mathf.Abs((current-previous).z-speed/hz));
     }
     Check(Vector3.Distance(current,rendered[2].position)<.0005f,"smoke source follows the rendered tyre between native contact samples at "+hz+" Hz");
     previous=current;previousOld=old;
    }
    Check(held==0&&maxStepError<.0005f,"no held source frames or physics-step jumps at "+hz+" Hz");
    Check(oldError>.25f,"legacy mixed-pose contact provider reproduces a visible source offset at "+hz+" Hz");
    Check(oldError<=speed*fixedDt+.001f,"legacy mixed-pose offset is bounded by one physics interval at "+hz+" Hz");
    if(hz==144)Check(oldHeld>hz/3,"legacy source demonstrably holds between native physics ticks at 144 Hz");
    report.Add("Contact source "+hz+" Hz: corrected max wheel error="+error+" m, held frames="+held+", legacy max error="+oldError+" m, legacy held frames="+oldHeld+", corrected step error="+maxStepError+" m");
   }
  } finally {
   smoke?.Dispose();adapter?.Dispose();fixture.SetActive(false);ground.SetActive(false);Destroy(fixture);Destroy(ground);
  }
 }
}
