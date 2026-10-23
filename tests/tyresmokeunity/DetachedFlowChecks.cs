using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Rachel350Z;

public sealed partial class SmokeProbe {
 void CheckDetachedCloudFlow() {
  var type=typeof(VehicleBurnoutEffects).GetNestedType("Puff",BindingFlags.NonPublic);
  var method=typeof(VehicleBurnoutEffects).GetMethod("CloudFlow",BindingFlags.Static|BindingFlags.NonPublic);
  Check(type!=null&&method!=null,"detached cloud exposes the authored birth-frame flow for native diagnostics");
  Func<object,Vector3,float,Vector3,Vector3> flow=(p,position,age,wind)=>(Vector3)method.Invoke(null,new object[]{p,position,age,wind});
  var hub=new Vector3(3,2,-5);var plus=DetachedPuff(type,hub,12);var minus=DetachedPuff(type,hub,-12);
  float smallestCurl=float.MaxValue;
  foreach(float age in new[]{1f,1.5f,2f})foreach(float radius in new[]{.5f,1f})foreach(var direction in new[]{Vector3.up,Vector3.down,Vector3.forward,Vector3.back}) {
   // Sample about the documented advected vortex centre, holding every input
   // except rotation sign fixed. The subtraction removes lift/eddy/spread.
   var radial=direction*radius;var point=hub+Vector3.up*(.12f+age*.13f)+radial;
   var circulation=(flow(plus,point,age,Vector3.zero)-flow(minus,point,age,Vector3.zero))*.5f;
   var tangent=Vector3.Cross(Vector3.right,radial).normalized;
   Check(circulation.magnitude>.1f&&Vector3.Dot(circulation.normalized,tangent)>.995f,"detached smoke retains directed wheel curl at "+age+" s and radius "+radius);
   Check(Mathf.Abs(Vector3.Dot(circulation,radial.normalized))<.0001f,"isolated detached circulation is tangential rather than a radial source");
   smallestCurl=Mathf.Min(smallestCurl,circulation.magnitude);
  }
  var sample=hub+Vector3.up*(.12f+1.5f*.13f)+Vector3.forward*.7f;
  var fullPositive=flow(plus,sample,1.5f,Vector3.zero);var fullNegative=flow(minus,sample,1.5f,Vector3.zero);var neutral=(fullPositive+fullNegative)*.5f;
  Check(Vector3.Distance(flow(DetachedPuff(type,hub,0),sample,1.5f,Vector3.zero),neutral)<.00001f,"a stationary wheel does not inject directed detached circulation");
  float full=(fullPositive-neutral).magnitude;float slow=(flow(DetachedPuff(type,hub,1),sample,1.5f,Vector3.zero)-neutral).magnitude;
  Check(slow>full*.05f&&slow<full*.5f,"slow wheel rotation scales detached circulation smoothly below the strong-spin field");
  foreach(float radius in new[]{.5f,1f}) {
   var strengths=new List<float>();
   foreach(float age in new[]{1f,4f,8f}) {
    var point=hub+Vector3.up*(.12f+age*.13f)+Vector3.forward*radius;
    strengths.Add((flow(plus,point,age,Vector3.zero)-flow(minus,point,age,Vector3.zero)).magnitude*.5f);
   }
   Check(strengths[1]<strengths[0]*.5f&&strengths[2]<strengths[0]*.1f,"detached circulation dissipates with age at a fixed radial offset "+radius);
   report.Add("Detached curl radius="+radius+" speed at 1/4/8s="+string.Join(",",strengths));
  }
  // Rotate all stored source vectors and translate the birth: a local eddy
  // field should follow that frame, independent of world axes and location.
  var rotations=new[]{Quaternion.Euler(0,90,0),Quaternion.Euler(24,73,-16)};var shift=new Vector3(70,11,-30);
  DetachedSet(plus,"Velocity",new Vector3(7,.4f,3));
  foreach(var rotation in rotations)foreach(float age in new[]{.5f,1.5f,3f}) {
   var point=hub+new Vector3(.7f,.9f,.4f);var wind=new Vector3(.3f,.1f,-.2f);var original=flow(plus,point,age,wind);
   var rotated=DetachedPuff(type,rotation*hub+shift,12);
   foreach(string name in new[]{"Normal","Forward","Axle","Velocity"})DetachedSet(rotated,name,rotation*(Vector3)type.GetField(name,F).GetValue(plus));
   DetachedSet(rotated,"Ground",rotation*(Vector3)type.GetField("Ground",F).GetValue(plus)+shift);
   var transformed=flow(rotated,rotation*point+shift,age,rotation*wind);
   Check(Vector3.Distance(transformed,rotation*original)<.0002f,"detached cloud eddies rotate with the stored driving frame at "+age+" s");
   Check(Vector3.Distance(flow(plus,point+Vector3.right*.001f,age,wind),original)<.02f,"nearby detached samples share a spatially coherent flow");
  }
  float maxSpeed=0;
  DetachedSet(plus,"Velocity",new Vector3(0,0,20));
  foreach(float age in new[]{0f,.1f,1f,2f,5f,20f})foreach(float x in new[]{-10f,-1f,0f,1f,10f})foreach(float y in new[]{-3f,0f,3f})foreach(float z in new[]{-10f,-1f,0f,1f,10f}) {
   var velocity=flow(plus,hub+new Vector3(x,y,z),age,new Vector3(3,0,0));
   Check(DetachedFinite(velocity)&&velocity.magnitude<20,"detached wake target stays finite and bounded across age and spatial extent");maxSpeed=Mathf.Max(maxSpeed,velocity.magnitude);
  }
  DetachedSet(plus,"Velocity",Vector3.zero);
  Check(Vector3.Distance(flow(plus,hub+Vector3.up*3,20,new Vector3(3,0,0)),new Vector3(3,0,0))<1.2f,"old detached smoke approaches wind while circulation decays");
  report.Add("Detached cloud flow minimum directed 1--2s curl="+smallestCurl+" m/s; maximum exercised target="+maxSpeed+" m/s.");
  CheckDetachedNativeTrail();
  CheckFastDetachedGroundClearance();
 }
 object DetachedPuff(Type type,Vector3 hub,float roll) {
  var puff=Activator.CreateInstance(type);
  foreach(var pair in new[]{new KeyValuePair<string,object>("Hub",hub),new KeyValuePair<string,object>("Ground",hub-Vector3.up*.34f),new KeyValuePair<string,object>("Normal",Vector3.up),new KeyValuePair<string,object>("Forward",Vector3.forward),new KeyValuePair<string,object>("Axle",Vector3.right),new KeyValuePair<string,object>("Velocity",Vector3.zero),new KeyValuePair<string,object>("Roll",roll),new KeyValuePair<string,object>("Orbit",.16f),new KeyValuePair<string,object>("Radius",.34f),new KeyValuePair<string,object>("Seed",1.3f),new KeyValuePair<string,object>("SizeScale",1.2f),new KeyValuePair<string,object>("Growth",1.1f),new KeyValuePair<string,object>("Spread",.22f)})DetachedSet(puff,pair.Key,pair.Value);
  return puff;
 }
 void DetachedSet(object puff,string name,object value){puff.GetType().GetField(name,F).SetValue(puff,value);}
 bool DetachedFinite(Vector3 value)=>!float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&!float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&!float.IsNaN(value.z)&&!float.IsInfinity(value.z);
 void CheckDetachedNativeTrail() {
  var rigs=new GameObject[2];var smoke=new VehicleBurnoutEffects[2];var contacts=new Func<int,TyreSmokeContact>[2];
  var origin=new Vector3(0,3000,0);
  try {
   for(int rig=0;rig<2;rig++) {
    rigs[rig]=new GameObject(rig==0?"Detached turning source":"Detached stationary source control");rigs[rig].transform.position=origin;
    var rigid=rigs[rig].AddComponent<Rigidbody>();rigid.useGravity=false;rigid.constraints=RigidbodyConstraints.FreezeAll;
    var colliders=new WheelCollider[4];var rendered=new Transform[4];
    for(int wheel=0;wheel<4;wheel++) {
     var obj=new GameObject("Detached source wheel "+wheel);obj.transform.SetParent(rigs[rig].transform,false);obj.transform.localPosition=new Vector3(wheel%2==0?-.86f:.86f,.36f,wheel<2?1.25f:-1.2f);
     colliders[wheel]=obj.AddComponent<WheelCollider>();colliders[wheel].radius=.34f;colliders[wheel].enabled=false;rendered[wheel]=obj.transform;
    }
    contacts[rig]=index=>new TyreSmokeContact{Grounded=true,Hub=rendered[index].position,Point=rendered[index].position-Vector3.up*.34f,Normal=Vector3.up,Rpm=850,Load=3500};
    smoke[rig]=VehicleBurnoutEffects.Create(rigs[rig],null,colliders,rendered,s=>report.Add(s));Check(smoke[rig]!=null,"independent native detached-cloud fixture initializes");
    foreach(var ps in rigs[rig].GetComponentsInChildren<ParticleSystem>())ps.GetComponent<ParticleSystemRenderer>().enabled=false;
   }
   for(int frame=0;frame<240;frame++)for(int rig=0;rig<2;rig++)smoke[rig].Advance(1f/60,true,true,contacts[rig],new Vector3(.2f,0,.1f));
   var tracked=new ParticleSystem.Particle[2][];var initial=new Dictionary<uint,Vector3>[2];int held=0;float maxSourceInfluence=0,maxStep=0;
   for(int wheel=0;wheel<2;wheel++) {
    tracked[wheel]=Particles(smoke[0],wheel).Where(p=>p.startLifetime>2&&p.startLifetime-p.remainingLifetime>1&&p.startLifetime-p.remainingLifetime<2&&p.velocity.magnitude>.2f).OrderByDescending(p=>p.velocity.magnitude).Take(8).ToArray();
    Check(tracked[wheel].Length==8,"native fixture provides eight detached cloud particles per tyre for trajectory tracking");initial[wheel]=tracked[wheel].ToDictionary(p=>p.randomSeed,p=>p.position);
   }
   for(int frame=0;frame<20;frame++) {
    rigs[0].transform.SetPositionAndRotation(origin+Vector3.right*((frame+1)*.6f),Quaternion.Euler(0,(frame+1)*12,0));
    for(int rig=0;rig<2;rig++)smoke[rig].Advance(1f/60,false,false,contacts[rig],new Vector3(.2f,0,.1f));
    for(int wheel=0;wheel<2;wheel++) {
     var current=Particles(smoke[0],wheel);var control=Particles(smoke[1],wheel).ToDictionary(p=>p.randomSeed);
     Check(current.Length<=1400&&PlumeCount(smoke[0],0)+PlumeCount(smoke[0],1)<=2800,"detached motion preserves independent and shared particle caps");
     for(int i=0;i<tracked[wheel].Length;i++) {
      var before=tracked[wheel][i];var after=current.Single(p=>p.randomSeed==before.randomSeed);float step=Vector3.Distance(after.position,before.position);
      if(step<.000001f)held++;maxStep=Mathf.Max(maxStep,step);float influence=Vector3.Distance(after.position,control[after.randomSeed].position);maxSourceInfluence=Mathf.Max(maxSourceInfluence,influence);
      Check(Mathf.Abs((before.remainingLifetime-after.remainingLifetime)-1f/60)<.00001f,"detached cloud ages on every native render update");
      Check(step<.2f,"detached cloud trajectory remains continuous between native render updates");
      Check(influence<.0002f,"released cloud remains on the same world trajectory while its source turns and moves away");tracked[wheel][i]=after;
     }
    }
   }
   Check(held==0,"tracked free clouds move on every native render update without held frames");
   for(int wheel=0;wheel<2;wheel++)Check(tracked[wheel].Average(p=>Vector3.Distance(p.position,initial[wheel][p.randomSeed]))>.03f,"detached native smoke develops a moving trail after source loss");
   report.Add("Detached native trail: 16 clouds x20 frames, held="+held+", maximum step="+maxStep+" m, moving-source vs fixed-source difference="+maxSourceInfluence+" m; source moved12m and turned240deg.");
  } finally {
   for(int rig=0;rig<2;rig++){smoke[rig]?.Dispose();if(rigs[rig]!=null){rigs[rig].SetActive(false);Destroy(rigs[rig]);}}
  }
 }
 void CheckFastDetachedGroundClearance() {
  var fixture=new GameObject("Fast detached cloud birth-plane guard");var origin=new Vector3(0,3000,0);fixture.transform.position=origin;
  var rigid=fixture.AddComponent<Rigidbody>();rigid.useGravity=false;rigid.constraints=RigidbodyConstraints.FreezeRotation;
  var colliders=new WheelCollider[4];var rendered=new Transform[4];VehicleBurnoutEffects smoke=null;
  try {
   for(int wheel=0;wheel<4;wheel++) {
    var obj=new GameObject("Fast smoke wheel "+wheel);obj.transform.SetParent(fixture.transform,false);obj.transform.localPosition=new Vector3(wheel%2==0?-.86f:.86f,.36f,wheel<2?1.25f:-1.2f);
    colliders[wheel]=obj.AddComponent<WheelCollider>();colliders[wheel].radius=.34f;colliders[wheel].enabled=false;rendered[wheel]=obj.transform;
   }
   Func<int,TyreSmokeContact> contact=index=>new TyreSmokeContact{Grounded=true,Hub=rendered[index].position,Point=rendered[index].position-Vector3.up*.34f,Normal=Vector3.up,Rpm=850,Load=3500};
   smoke=VehicleBurnoutEffects.Create(fixture,null,colliders,rendered,s=>report.Add(s));Check(smoke!=null,"fast native cloud fixture initializes");
   foreach(var ps in fixture.GetComponentsInChildren<ParticleSystem>())ps.GetComponent<ParticleSystemRenderer>().enabled=false;
   var plumes=(Array)typeof(VehicleBurnoutEffects).GetField("plumes",F).GetValue(smoke);var plumeType=plumes.GetValue(0).GetType();var puffArrays=new[]{(Array)plumeType.GetField("Puffs",F).GetValue(plumes.GetValue(0)),(Array)plumeType.GetField("Puffs",F).GetValue(plumes.GetValue(1))};
   var puffType=puffArrays[0].GetType().GetElementType();var groundField=puffType.GetField("Ground",F);var normalField=puffType.GetField("Normal",F);var ageField=puffType.GetField("Age",F);var wispField=puffType.GetField("Wisp",F);
   var previous=new[]{new Dictionary<uint,ParticleSystem.Particle>(),new Dictionary<uint,ParticleSystem.Particle>()};var beyond=new HashSet<ulong>();
   float minimum=float.MaxValue,minimumBeyond=float.MaxValue,maximumStep=0;int checkedClouds=0,beyondSamples=0,comparedSamples=0;
   for(int frame=0;frame<540;frame++) {
    bool emitting=frame<360;
    if(emitting)fixture.transform.position=origin+Vector3.forward*((frame+1)*(20f/60));
    rigid.linearVelocity=emitting?Vector3.forward*20:Vector3.zero;
    smoke.Advance(1f/60,emitting,emitting,contact,Vector3.zero);
    bool above=true,continuous=true,agesAdvance=true;int moved=0,compared=0;string failure=null;
    for(int wheel=0;wheel<2;wheel++) {
     // This is the actual shared native upload, split by its per-wheel counts.
     // Puff order is kept alongside that buffer when expired slots are swapped.
     var particles=Particles(smoke,wheel);var next=new Dictionary<uint,ParticleSystem.Particle>();
     Check(particles.Length<=1400&&PlumeCount(smoke,0)+PlumeCount(smoke,1)<=2800,"fast detached smoke preserves per-wheel and shared particle budgets");
     for(int index=0;index<particles.Length;index++) {
      var puff=puffArrays[wheel].GetValue(index);if((bool)wispField.GetValue(puff)||(float)ageField.GetValue(puff)<=.2f)continue;
      var particle=particles[index];var ground=(Vector3)groundField.GetValue(puff);var normal=(Vector3)normalField.GetValue(puff);var offset=particle.position-ground;
      float clearance=Vector3.Dot(offset,normal);float planar=Vector3.ProjectOnPlane(offset,normal).magnitude;checkedClouds++;minimum=Mathf.Min(minimum,clearance);
      if(!DetachedFinite(particle.position)||clearance<.03f){above=false;if(failure==null)failure=" frame="+frame+" wheel="+wheel+" age="+ageField.GetValue(puff)+" clearance="+clearance+" birth-distance="+planar;}
      if(planar>6){beyond.Add(((ulong)wheel<<32)|particle.randomSeed);beyondSamples++;minimumBeyond=Mathf.Min(minimumBeyond,clearance);}
      if(previous[wheel].TryGetValue(particle.randomSeed,out var before)) {
       float step=Vector3.Distance(particle.position,before.position);maximumStep=Mathf.Max(maximumStep,step);compared++;comparedSamples++;if(step>.000001f)moved++;
       if(step>=.6f)continuous=false;
       if(Mathf.Abs((before.remainingLifetime-particle.remainingLifetime)-1f/60)>.00001f)agesAdvance=false;
      }
      next[particle.randomSeed]=particle;
     }
     previous[wheel]=next;
    }
    Check(above,"moving native free clouds remain above their stored birth plane, including beyond the 6m local guard"+failure);
    Check(continuous&&agesAdvance,"fast native clouds retain continuous positions and per-render ages");
    Check(compared==0||moved>0,"fast native cloud upload advances on every render update");
   }
   Check(beyond.Count>10,"fast native fixture exercises more than ten distinct free clouds beyond the 6m birth-plane guard");
   Check(checkedClouds>100&&comparedSamples>100&&minimumBeyond>=.03f,"fast detached clearance is measured from real repeated native uploads beyond the nearby guard");
   report.Add("Fast detached 20m/s trail: 6s emission +3s release, cloud samples="+checkedClouds+", continuous comparisons="+comparedSamples+", distinct beyond6m="+beyond.Count+", beyond samples="+beyondSamples+", minimum plane clearance="+minimum+" m, beyond6m minimum="+minimumBeyond+" m, maximum step="+maximumStep+" m.");
  } finally {smoke?.Dispose();fixture.SetActive(false);Destroy(fixture);}
 }
}
