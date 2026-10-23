using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Rachel350Z;

public sealed partial class SmokeProbe {
 // Freeze both simulation and flipbook time. Compare the native two-renderer
 // order with its two forced alternatives and a single globally sorted buffer.
 // Tinted particles amplify order for diagnosis only; grey uses production's
 // actual .84--.98 colour range. No effect/material settings are persisted.
 void CheckSmokeSorting() {
  var oldRt=rt;var small=new RenderTexture(640,360,24);rt=small;camera.targetTexture=rt;
  var production=Systems;var renderers=production.Select(p=>p.GetComponent<ParticleSystemRenderer>()).ToArray();
  var source=new[]{Particles(0),Particles(1)};
  var enabled=renderers.Select(r=>r.enabled).ToArray();
  var material=renderers[0].sharedMaterial;
  var left=SortingSystem("Sorting frozen left",material);var right=SortingSystem("Sorting frozen right",material);var merged=SortingSystem("Sorting globally sorted reference",material);
  var systems=new[]{left,right,merged};var target=new Vector3(0,1,-1.2f);
  var csv=new List<string>{"scene,view,default_vs_left_first_mean,default_vs_right_first_mean,forced_orders_changed_pixels,forced_orders_mean,default_vs_merged_mean,left_pixels,right_pixels,default_repeat_changed_pixels"};
  try {
   foreach(var renderer in renderers)renderer.enabled=false;
   // The real warm plume, with the original random atlas starts and lifetimes,
   // is rendered from several angles without changing any particle data.
   left.SetParticles(source[0],source[0].Length);right.SetParticles(source[1],source[1].Length);
   merged.SetParticles(source.SelectMany(p=>p).ToArray());
   SortingUploadReference("actual-startup3s",production,systems,merged,new Vector3(-4,2,-5.5f),target);
   for(int view=0;view<8;view++) {
    float angle=view*Mathf.PI/4;var eye=target+new Vector3(Mathf.Sin(angle)*7,2,Mathf.Cos(angle)*7);
    SortingCompare("actual-frozen",view,eye,target,left,right,merged,csv,view%2==0);
   }
   var lc=left.GetComponent<ParticleSystemRenderer>().bounds.center;var rc=right.GetComponent<ParticleSystemRenderer>().bounds.center;
   var split=(rc-lc).normalized;var midpoint=(lc+rc)*.5f;
   var vertical=Vector3.ProjectOnPlane(Vector3.up,split).normalized;var tangent=Vector3.Cross(split,vertical).normalized;
   if(split.sqrMagnitude>.5f&&tangent.sqrMagnitude>.5f) {
    var boundary=midpoint+vertical*2+tangent*7;var images=new List<Color32[]>();var references=new List<Color32[]>();
    foreach(float offset in new[]{-.02f,-.002f,.002f,.02f}) {
     var eye=boundary+split*offset;int view=images.Count;
     SortingCompare("actual-boundary",view,eye,midpoint,left,right,merged,csv,view==1||view==2);
     var lr=left.GetComponent<ParticleSystemRenderer>();var rr=right.GetComponent<ParticleSystemRenderer>();var mr=merged.GetComponent<ParticleSystemRenderer>();
     lr.enabled=rr.enabled=true;mr.enabled=false;lr.sortingOrder=rr.sortingOrder=0;images.Add(SortingFrame(null,eye,midpoint));
     lr.enabled=rr.enabled=false;mr.enabled=true;references.Add(SortingFrame(null,eye,midpoint));
    }
    for(int i=1;i<images.Count;i++) {
     var two=SortingDifference(images[i-1],images[i]);var one=SortingDifference(references[i-1],references[i]);
     report.Add("Actual frozen plume camera boundary "+(i-1)+"->"+i+": separate changed="+two.Item1+", mean="+two.Item2.ToString("F5",CultureInfo.InvariantCulture)+"; merged changed="+one.Item1+", mean="+one.Item2.ToString("F5",CultureInfo.InvariantCulture));
    }
   }
   // Separate simulation instances leave the original freeze control intact.
   // Mature stationary smoke and an overlapping donut trail are more likely to
   // interpenetrate than the initial three-second startup plume above.
   foreach(bool moving in new[]{false,true}) {
    foreach(var ps in systems)ps.GetComponent<ParticleSystemRenderer>().enabled=false;
    var mature=SortingScenario(moving,production,systems,merged);
    left.SetParticles(mature[0],mature[0].Length);right.SetParticles(mature[1],mature[1].Length);merged.SetParticles(mature.SelectMany(p=>p).ToArray());
    SortingMatureBoundary(moving?"actual-donut6s":"actual-stationary8s",left,right,merged,csv);
   }
   foreach(bool tagged in new[]{false,true}) {
    var a=SortingParticles(target,false,tagged);var b=SortingParticles(target,true,tagged);
    left.SetParticles(a,a.Length);right.SetParticles(b,b.Length);merged.SetParticles(a.Concat(b).ToArray());
    var near=new List<Color32[]>();var single=new List<Color32[]>();
    foreach(float x in new[]{-.02f,-.002f,.002f,.02f}) {
     var eye=target+new Vector3(x,1,-7);int view=near.Count;
     SortingCompare(tagged?"tagged-control":"production-grey-control",view,eye,target,left,right,merged,csv,true);
     var lr=left.GetComponent<ParticleSystemRenderer>();var rr=right.GetComponent<ParticleSystemRenderer>();var mr=merged.GetComponent<ParticleSystemRenderer>();
     lr.enabled=rr.enabled=true;mr.enabled=false;lr.sortingOrder=rr.sortingOrder=0;near.Add(SortingFrame(null,eye,target));
     lr.enabled=rr.enabled=false;mr.enabled=true;single.Add(SortingFrame(null,eye,target));
    }
    for(int i=1;i<near.Count;i++) {
     var two=SortingDifference(near[i-1],near[i]);var one=SortingDifference(single[i-1],single[i]);
     report.Add((tagged?"Tagged":"Production-grey")+" frozen sorting camera step "+(i-1)+"->"+i+": separate changed="+two.Item1+", mean="+two.Item2.ToString("F5",CultureInfo.InvariantCulture)+"; merged changed="+one.Item1+", mean="+one.Item2.ToString("F5",CultureInfo.InvariantCulture));
    }
   }
   File.WriteAllLines(Path.Combine(folder,"smoke-sorting.csv"),csv);
   for(int i=0;i<2;i++){var now=Particles(i);Check(now.Select(p=>p.position).SequenceEqual(source[i].Select(p=>p.position)),"sorting camera orbit keeps production smoke particle positions frozen");Check(now.Select(p=>p.remainingLifetime).SequenceEqual(source[i].Select(p=>p.remainingLifetime)),"sorting camera orbit keeps production atlas ages frozen");}
   report.Add("Sorting diagnostic: frozen production orbit, individually hidden plumes, forced inter-renderer orders, production-grey/tagged camera-boundary controls and globally sorted reference recorded in smoke-sorting.csv.");
  } finally {
   foreach(var ps in systems){ps.gameObject.SetActive(false);Destroy(ps.gameObject);}
   for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];
   rt=oldRt;camera.targetTexture=oldRt;small.Release();Destroy(small);
  }
 }
 ParticleSystem.Particle[][] SortingScenario(bool moving,ParticleSystem[] original,ParticleSystem[] diagnostics,ParticleSystem merged) {
  var position=car.transform.position;var rotation=car.transform.rotation;var velocity=body.linearVelocity;var angular=body.angularVelocity;float oldRpm=rpm;bool oldGround=rightGround;
  VehicleBurnoutEffects scenario=null;ParticleSystem[] extra=null;
  try {
   car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;rpm=850;rightGround=true;
   scenario=VehicleBurnoutEffects.Create(car,null,wheels,visuals,s=>report.Add(s));Check(scenario!=null,"independent mature smoke sorting fixture initializes");
   extra=Systems.Except(original).ToArray();Check(extra.Length==1,"mature sorting fixture uploads its two independently simulated plumes to one renderer");foreach(var ps in extra)ps.GetComponent<ParticleSystemRenderer>().enabled=false;
   int frames=moving?360:480;
   for(int i=0;i<frames;i++) {
    if(moving&&i>=120) {
     float angle=(i-120)/60f*1.5f;car.transform.SetPositionAndRotation(new Vector3(4*(1-Mathf.Cos(angle)),0,4*Mathf.Sin(angle)),Quaternion.Euler(0,angle*Mathf.Rad2Deg,0));
     body.linearVelocity=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*6;body.angularVelocity=Vector3.up*1.5f;
    }
    scenario.Advance(1f/60,true,true,Contact,new Vector3(.15f,0,.05f));
   }
   var result=new[]{Particles(scenario,0),Particles(scenario,1)};
   merged.SetParticles(result.SelectMany(p=>p).ToArray());var focus=car.transform.position+new Vector3(0,1,-1.2f);
   SortingUploadReference(moving?"actual-donut6s":"actual-stationary8s",extra,diagnostics,merged,focus+new Vector3(-5,2,-7),focus);
   report.Add("Sorting real "+(moving?"6s donut":"8s stationary")+" particles="+string.Join(",",result.Select(p=>p.Length)));return result;
  } finally {
   if(extra!=null)foreach(var ps in extra)ps.gameObject.SetActive(false);scenario?.Dispose();
   car.transform.SetPositionAndRotation(position,rotation);body.linearVelocity=velocity;body.angularVelocity=angular;rpm=oldRpm;rightGround=oldGround;
  }
 }
 void SortingUploadReference(string scene,ParticleSystem[] production,ParticleSystem[] diagnostics,ParticleSystem merged,Vector3 eye,Vector3 target) {
  foreach(var ps in diagnostics)ps.GetComponent<ParticleSystemRenderer>().enabled=false;
  foreach(var ps in production){ps.gameObject.layer=31;ps.GetComponent<ParticleSystemRenderer>().enabled=true;}
  var actual=SortingFrame(null,eye,target);
  foreach(var ps in production)ps.GetComponent<ParticleSystemRenderer>().enabled=false;
  merged.GetComponent<ParticleSystemRenderer>().enabled=true;var reference=SortingFrame(null,eye,target);merged.GetComponent<ParticleSystemRenderer>().enabled=false;
  var delta=SortingDifference(actual,reference);
  report.Add("Production shared upload vs globally sorted reference "+scene+": changed="+delta.Item1+", mean="+delta.Item2.ToString("F6",CultureInfo.InvariantCulture));
  Check(delta.Item1==0&&delta.Item2<.002,"production shared native renderer matches globally sorted reference with unchanged production particles: "+scene);
 }
 void SortingMatureBoundary(string scene,ParticleSystem left,ParticleSystem right,ParticleSystem merged,List<string> csv) {
  var lr=left.GetComponent<ParticleSystemRenderer>();var rr=right.GetComponent<ParticleSystemRenderer>();var mr=merged.GetComponent<ParticleSystemRenderer>();lr.enabled=rr.enabled=true;mr.enabled=false;
  SortingFrame(null,new Vector3(0,3,-8),new Vector3(0,1,0));
  var lc=lr.bounds.center;var rc=rr.bounds.center;var split=(rc-lc).normalized;var midpoint=(lc+rc)*.5f;
  var vertical=Vector3.ProjectOnPlane(Vector3.up,split).normalized;var tangent=Vector3.Cross(split,vertical).normalized;
  Check(split.sqrMagnitude>.5f&&tangent.sqrMagnitude>.5f,"mature plumes have a defined renderer sorting boundary: "+scene);
  for(int side=0;side<2;side++) {
   var boundary=midpoint+vertical*2+tangent*(side==0?7:-7);var images=new List<Color32[]>();var references=new List<Color32[]>();
   foreach(float offset in new[]{-.002f,.002f}) {
    var eye=boundary+split*offset;int view=side*2+images.Count;SortingCompare(scene,view,eye,midpoint,left,right,merged,csv,true);
    lr.enabled=rr.enabled=true;mr.enabled=false;lr.sortingOrder=rr.sortingOrder=0;images.Add(SortingFrame(null,eye,midpoint));
    lr.enabled=rr.enabled=false;mr.enabled=true;references.Add(SortingFrame(null,eye,midpoint));
   }
   var two=SortingDifference(images[0],images[1]);var one=SortingDifference(references[0],references[1]);
   report.Add(scene+" actual bounds 4mm camera crossing side="+side+": separate changed="+two.Item1+", mean="+two.Item2.ToString("F5",CultureInfo.InvariantCulture)+"; merged changed="+one.Item1+", mean="+one.Item2.ToString("F5",CultureInfo.InvariantCulture));
  }
 }
 ParticleSystem SortingSystem(string name,Material material) {
  var go=new GameObject(name);go.layer=31;var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=ps.main;main.playOnAwake=false;main.maxParticles=2800;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;
  var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
  var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=sheet.numTilesY=8;sheet.startFrame=new ParticleSystem.MinMaxCurve(0,1);sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));sheet.cycleCount=2;
  var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.sortMode=ParticleSystemSortMode.Distance;r.maxParticleSize=.35f;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=true;
  r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});ps.Play(false);ps.Pause(false);return ps;
 }
 ParticleSystem.Particle[] SortingParticles(Vector3 target,bool right,bool tagged) {
  var result=new List<ParticleSystem.Particle>();
  // Different visible depths never change their correct order as the camera
  // crosses x=0. Opposite invisible tails make aggregate bounds exchange order.
  for(int i=0;i<8;i++)result.Add(new ParticleSystem.Particle{position=target+new Vector3((i%4-1.5f)*.19f,(i/4-.5f)*.22f,right?.12f:-.12f),startLifetime=10,remainingLifetime=5,startSize=2.5f,startColor=tagged?(right?new Color(.05f,.8f,1,.35f):new Color(1,.12f,.05f,.35f)):(right?new Color(.98f,.98f,.98f,.35f):new Color(.84f,.84f,.84f,.35f)),randomSeed=(uint)(11+i),rotation=23});
  result.Add(new ParticleSystem.Particle{position=target+new Vector3(right?35:-35,0,0),startLifetime=10,remainingLifetime=5,startSize=.01f,startColor=Color.clear,randomSeed=99});
  // Keep both bounds' z centres identical: otherwise the visible +/-12 cm
  // depth offset would move the aggregate equality plane away from x=0.
  foreach(float z in new[]{-5f,5f})result.Add(new ParticleSystem.Particle{position=target+new Vector3(0,0,z),startLifetime=10,remainingLifetime=5,startSize=.01f,startColor=Color.clear,randomSeed=100});
  return result.ToArray();
 }
 void SortingCompare(string scene,int view,Vector3 eye,Vector3 target,ParticleSystem left,ParticleSystem right,ParticleSystem merged,List<string> csv,bool save) {
  var l=left.GetComponent<ParticleSystemRenderer>();var r=right.GetComponent<ParticleSystemRenderer>();var m=merged.GetComponent<ParticleSystemRenderer>();string stem="sorting-"+scene+"-"+view;
  l.enabled=r.enabled=true;m.enabled=false;l.sortingOrder=r.sortingOrder=0;var normal=SortingFrame(save?stem+"-both.png":null,eye,target);
  var repeat=SortingFrame(null,eye,target);var repeated=SortingDifference(normal,repeat);Check(repeated.Item1==0,"frozen smoke camera render is pixel-repeatable: "+scene+" "+view);
  l.sortingOrder=-1;r.sortingOrder=1;var ab=SortingFrame(null,eye,target);
  l.sortingOrder=1;r.sortingOrder=-1;var ba=SortingFrame(null,eye,target);l.sortingOrder=r.sortingOrder=0;
  r.enabled=false;var onlyLeft=SortingFrame(save&&view==0?stem+"-left.png":null,eye,target);l.enabled=false;r.enabled=true;var onlyRight=SortingFrame(save&&view==0?stem+"-right.png":null,eye,target);
  r.enabled=false;m.enabled=true;var combined=SortingFrame(save?stem+"-merged.png":null,eye,target);m.enabled=false;
  var order=SortingDifference(ab,ba);var dl=SortingDifference(normal,ab);var dr=SortingDifference(normal,ba);var dm=SortingDifference(normal,combined);
  csv.Add(string.Join(",",new[]{scene,view.ToString(),dl.Item2.ToString("F6",CultureInfo.InvariantCulture),dr.Item2.ToString("F6",CultureInfo.InvariantCulture),order.Item1.ToString(),order.Item2.ToString("F6",CultureInfo.InvariantCulture),dm.Item2.ToString("F6",CultureInfo.InvariantCulture),SortingDifference(normal,onlyRight).Item1.ToString(),SortingDifference(normal,onlyLeft).Item1.ToString(),repeated.Item1.ToString()}));
 }
 Color32[] SortingFrame(string name,Vector3 eye,Vector3 target) {
  camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
  try {image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();if(name!=null)File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());return image.GetPixels32();}
  finally {RenderTexture.active=previous;Destroy(image);}
 }
 Tuple<int,double> SortingDifference(Color32[] a,Color32[] b) {
  int count=0;double sum=0;for(int i=0;i<a.Length;i++){int delta=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);if(delta>25)count++;sum+=delta;}return Tuple.Create(count,sum/a.Length);
 }
}
