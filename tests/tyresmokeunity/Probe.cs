using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;
using Rachel350Z;

[BepInPlugin("local.nfvu2.smokeprobe","NFVU2 temporary tyre smoke probe","0.0.1")]
public sealed partial class SmokeProbe:BaseUnityPlugin {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 string folder;GameObject car;Rigidbody body;WheelCollider[] wheels=new WheelCollider[4];Transform[] visuals=new Transform[4];
 VehicleBurnoutEffects effect;Camera camera;RenderTexture rt;Light light;int checks;float rpm=850;bool rightGround=true;
 readonly List<string> report=new List<string>();
 IEnumerator Start() {
  var args=Environment.GetCommandLineArgs();if(!args.Contains("-nfvu2-smoke-probe"))yield break;
  folder=args.Single(a=>a.StartsWith("-nfvu2-output=")).Substring(14);Application.runInBackground=true;
  yield return new WaitForSecondsRealtime(17);
  try {Setup();Run();File.WriteAllLines(Path.Combine(folder,"results.txt"),report);File.WriteAllText(Path.Combine(folder,"completed.txt"),checks+" native tyre smoke checks passed.");}
  catch(Exception e){File.WriteAllText(Path.Combine(folder,"error.txt"),e.ToString());File.WriteAllLines(Path.Combine(folder,"results.txt"),report);}
  finally {effect?.Dispose();if(rt!=null)rt.Release();}
  Application.Quit();
 }
 void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 TyreSmokeContact Contact(int index) {
  var hub=visuals[index].position;
  return new TyreSmokeContact{Grounded=index==2||rightGround,Hub=hub,Point=hub-Vector3.up*.34f,Normal=Vector3.up,Rpm=rpm,Load=3500};
 }
 ParticleSystem[] Systems=>car.GetComponentsInChildren<ParticleSystem>();
 int PlumeCount(VehicleBurnoutEffects smoke,int wheel) {
  var plumes=(Array)typeof(VehicleBurnoutEffects).GetField("plumes",F).GetValue(smoke);var plume=plumes.GetValue(wheel);
  return (int)plume.GetType().GetField("Count",F).GetValue(plume);
 }
 ParticleSystem.Particle[] Particles(int wheel)=>Particles(effect,wheel);
 ParticleSystem.Particle[] Particles(VehicleBurnoutEffects smoke,int wheel) {
  // Read the native upload, not the simulator arrays, so the shared-buffer
  // regression also detects a missing wheel or a stale upload after clearing.
  var root=(GameObject)typeof(VehicleBurnoutEffects).GetField("root",F).GetValue(smoke);
  var ps=root.GetComponentInChildren<ParticleSystem>();var all=new ParticleSystem.Particle[ps.particleCount];ps.GetParticles(all);
  int offset=wheel==0?0:PlumeCount(smoke,0);return all.Skip(offset).Take(PlumeCount(smoke,wheel)).ToArray();
 }
 void Step(float dt=1f/60,bool enabled=true,bool allowed=true){effect.Advance(dt,enabled,allowed,Contact,new Vector3(.15f,0,.05f));}
 void Reset(){effect?.Dispose();if(car!=null)foreach(var ps in car.GetComponentsInChildren<ParticleSystem>())ps.gameObject.SetActive(false);effect=VehicleBurnoutEffects.Create(car,null,wheels,visuals,s=>report.Add(s));Check(effect!=null,"effect initializes with native lit material");}
 void Run() {
  if(Environment.GetCommandLineArgs().Contains("-nfvu2-wheel-occlusion-only")){CheckWheelOcclusion();return;}
  car.SetActive(false);Reset();car.SetActive(true);
  Check((bool)typeof(VehicleBurnoutEffects).GetField("animated",F).GetValue(effect),"native lit 64-frame material selected, including inactive spawn initialization");
  var material=Systems[0].GetComponent<ParticleSystemRenderer>().sharedMaterial;
  Check(material.shader.name=="Lux Lit Particles/ Bumped","lit smoke shader, no additive glow fallback");
  var timing=Stopwatch.StartNew();
  for(int frame=0;frame<480;frame++) {
   Step();
   if(frame==11||frame==59||frame==179||frame==479)Capture("burnout-"+(frame+1)+".png",new Vector3(-4,2.1f,-5.5f),new Vector3(0,.5f,-.8f));
  }
  report.Add("8s startup CPU total ms="+timing.Elapsed.TotalMilliseconds);
  Check(Systems.Length==1,"one globally sorted renderer for both rear plumes");
  Check(Systems[0].main.maxParticles==2800,"shared native renderer retains the total particle budget");
  Check(Systems[0].GetComponent<ParticleSystemRenderer>().sortMode==ParticleSystemSortMode.Distance,"both plumes sort together by particle distance");
  for(int wheel=0;wheel<2;wheel++)Check(Particles(wheel).Length>700&&Particles(wheel).Length<=1400,"each rear tyre independently builds bounded dense smoke");
  foreach(var ps in Systems) {
   Check(ps.particleCount==PlumeCount(effect,0)+PlumeCount(effect,1),"shared native upload contains both independent plume buffers");
   Check(ps.isPaused,"native integration stays paused; particles are not moved twice");
  }
  var dense=Particles(0);report.Add("Dense per wheel="+dense.Length);
  Check(dense.All(p=>p.position.x==p.position.x&&p.startSize>=0&&p.startSize<=3.81f),"finite particle geometry and bounded billboard sizes");
  Check(dense.Any(p=>p.startLifetime-p.remainingLifetime<.18f&&p.position.z<visuals[2].position.z-.08f),"fresh smoke wraps behind the forward-spinning tyre");
  var wisps=dense.Where(p=>p.startLifetime<1.1f).ToArray();
  Check(wisps.Count(p=>p.startLifetime-p.remainingLifetime<.30f)>40,"dense circulating tyre mantle contains substantially more than a sparse handful of wisps");
  var wheelHub=visuals[2].position;
  Check(wisps.Count(p=>p.startLifetime-p.remainingLifetime<.30f&&Math.Abs(p.position.x-wheelHub.x)>.135f)>10,"boundary wisps hug both tyre shoulders instead of hiding inside the rubber silhouette");
  Check(wisps.Any(p=>p.position.y>wheelHub.y+.25f&&p.position.z>wheelHub.z+.05f),"smoke wraps beyond the tyre crown into the upper forward quadrant");
  CaptureWisps("wheel-wrap-shoulders.png",new Vector3(-2.7f,.8f,-2.8f),wheelHub);
  var tracked=dense.First(p=>p.startLifetime<1.1f&&p.startLifetime-p.remainingLifetime<.022f);
  for(int i=0;i<5;i++)Step();
  var curled=Particles(0).Single(p=>p.randomSeed==tracked.randomSeed);
  Check(curled.position.y>tracked.position.y+.05f&&curled.position.z<tracked.position.z-.05f,"the same fresh wisp actually travels upward around the rear of the tyre");
  report.Add("Tracked shoulder wisp displacement="+(curled.position-tracked.position));
  Check(dense.Any(p=>p.startSize>2&&p.position.y>.5f),"detached smoke expands and rises");
  Capture("wheel-detail.png",new Vector3(-2.7f,.8f,-2.8f),visuals[2].position);
  var frozen=Particles(0).Select(p=>p.position).ToArray();Step(0);Check(frozen.SequenceEqual(Particles(0).Select(p=>p.position)),"zero delta/pause leaves particle positions unchanged");
  foreach(var ps in Systems)ps.GetComponent<ParticleSystemRenderer>().enabled=false;
  Capture("without-smoke.png",new Vector3(-4,2.1f,-5.5f),new Vector3(0,.5f,-.8f));
  foreach(var ps in Systems)ps.GetComponent<ParticleSystemRenderer>().enabled=true;
  Check(PixelDifference("burnout-480.png","without-smoke.png")>6000,"native camera renders a visible plume");
  int count=Particles(0).Length;for(int i=0;i<15;i++)Step(enabled:false);
  Check(Particles(0).Length<count,"old smoke survives and naturally expires after throttle release");
  var before=Particles(0).Where(p=>p.startLifetime-p.remainingLifetime>1).ToDictionary(p=>p.randomSeed,p=>p.position);
  car.transform.position+=Vector3.forward*3;Step(.05f,false);
  var old=Particles(0).Where(p=>before.ContainsKey(p.randomSeed)).ToArray();
  Check(old.Length>200&&old.All(p=>Vector3.Distance(p.position,before[p.randomSeed])<.25f),"existing plume stays in world space when vehicle moves");
  car.transform.position+=Vector3.forward*30;Step(.02f,false);
  Check(Systems.All(ps=>ps.particleCount==0),"teleport clears old smoke and thermal state");
  car.transform.position=Vector3.zero;rightGround=false;Reset();for(int i=0;i<180;i++)Step();
  Check(Particles(0).Length>100&&Particles(1).Length==0,"airborne rear wheel does not emit while grounded wheel does");
  rpm=-850;Reset();for(int i=0;i<60;i++)Step();
  Check(Particles(0).Any(p=>p.startLifetime-p.remainingLifetime<.18f&&p.position.z>visuals[2].position.z+.08f),"reverse rotation reverses visible wheel entrainment");
  rpm=850;rightGround=true;Reset();
  body.linearVelocity=Vector3.forward*(rpm*Mathf.PI/30*.34f);
  report.Add("Rolling expected surface="+(rpm*Mathf.PI/30*.34f)+" body="+body.linearVelocity+" point="+body.GetPointVelocity(visuals[2].position));
  for(int i=0;i<180;i++)Step();
  Check(Systems.All(ps=>ps.particleCount==0),"real wheel surface velocity equal to road velocity produces no native particles");
  body.linearVelocity=Vector3.zero;Reset();
  for(int i=0;i<240;i++) {
   car.transform.position+=new Vector3(.06f,0,.12f);body.linearVelocity=new Vector3(3.6f,0,7.2f);Step();
  }
  var at=car.transform.position;Capture("moving-drift-trail.png",at+new Vector3(-7,3,-11),at+new Vector3(-2,0,-5));
  var trail=Particles(0);Check(trail.Count(p=>p.position.z<at.z-7)>100,"moving drift leaves a continuous detached trail behind the car");
  car.transform.position=Vector3.zero;body.linearVelocity=Vector3.zero;Reset();
  timing.Restart();for(int i=0;i<2400;i++) {Step(1f/30);Check(PlumeCount(effect,0)<=1400&&PlumeCount(effect,1)<=1400&&Systems[0].particleCount==PlumeCount(effect,0)+PlumeCount(effect,1),"long-run per-wheel caps and complete shared native upload");}
  report.Add("80s steady CPU total ms="+timing.Elapsed.TotalMilliseconds+"; particles="+PlumeCount(effect,0)+","+PlumeCount(effect,1));
  for(int i=0;i<400;i++)Step(enabled:false);
  Check(Systems.All(ps=>ps.particleCount==0),"all particles expire and no persistent native smoke exists");
  Reset();for(int i=0;i<180;i++)Step();
  tracked=Particles(0).First(p=>p.startLifetime<1.1f&&p.startLifetime-p.remainingLifetime<.022f);
  car.transform.position+=Vector3.right*1.2f;car.transform.rotation=Quaternion.Euler(0,35,0);Step();
  curled=Particles(0).Single(p=>p.randomSeed==tracked.randomSeed);
  Check(Vector3.Distance(curled.position,visuals[2].position)<.50f,"entrained shoulder smoke follows the current wheel frame during turning motion");
  var released=curled.position;Step(allowed:false);
  car.transform.position+=Vector3.right*1.2f;Step(allowed:false);
  curled=Particles(0).Single(p=>p.randomSeed==tracked.randomSeed);
  Check(Vector3.Distance(curled.position,released)<.35f,"source loss releases wrapped smoke instead of dragging it with an inactive wheel");
  car.transform.position=Vector3.zero;car.transform.rotation=Quaternion.identity;
  var counts=new List<int>();
  foreach(int hz in new[]{30,144}) {Reset();for(int i=0;i<hz*6;i++)Step(1f/hz);counts.Add(Particles(0).Length);}
  report.Add("30/144Hz particle counts="+string.Join(",",counts));
  Check(Math.Abs(counts[0]-counts[1])<15,"particle source/lifetimes stay consistent at 30 and 144 Hz");
  CheckRenderCadence();
  CheckFullWheelWrap();
  CheckContactPatchSmoke();
  CheckDetachedCloudFlow();
  CheckDetachedBaseline();
  CheckWheelOcclusion();
  CheckSharedBufferClearing();
  CheckAtlasDepth();
  CheckSmokeSorting();
  CheckContactSourceInterpolation();
  CheckGlassSmoke();
  Check(car.GetComponentsInChildren<Smoke>(true).Length==0,"no game Smoke MonoBehaviours");
  Check(car.GetComponentsInChildren<AudioSource>(true).Length==0,"smoke creates no audio");
 }
 void CheckSharedBufferClearing() {
  Reset();for(int i=0;i<180;i++)Step();
  Check(Particles(0).Length>100&&Particles(1).Length>100,"both tyre buffers populated before single-source reset");
  Func<int,TyreSmokeContact> invalidLeft=index=>{var c=Contact(index);if(index==2)c.Hub=new Vector3(float.NaN,0,0);return c;};
  effect.Advance(1f/60,true,true,invalidLeft,Vector3.zero);
  Check(Particles(0).Length==0&&Particles(1).Length>100&&Systems[0].particleCount==PlumeCount(effect,1),"invalid left source removes its stale native particles and preserves the right plume");
  effect.Advance(float.NaN,true,true,Contact,Vector3.zero);
  Check(Systems[0].particleCount==0&&PlumeCount(effect,0)==0&&PlumeCount(effect,1)==0,"invalid clock clears both native and simulated buffers");
  Step();Check(Particles(0).Length==PlumeCount(effect,0)&&Particles(1).Length==PlumeCount(effect,1),"shared upload recovers after source and clock reset");
  Reset();for(int i=0;i<180;i++)Step();
 }
 void CheckRenderCadence() {
  foreach(int hz in new[]{30,60,144}) {
   car.transform.position=Vector3.zero;car.transform.rotation=Quaternion.identity;body.linearVelocity=Vector3.zero;rpm=850;rightGround=true;Reset();
   for(int i=0;i<hz*3;i++)Step(1f/hz);
   var tracked=Particles(0).First(p=>p.startLifetime<1.1f&&p.startLifetime-p.remainingLifetime<.04f);int held=0;
   for(int frame=0;frame<10;frame++) {
    var before=tracked;body.linearVelocity=new Vector3(8,0,20);car.transform.position+=body.linearVelocity/hz;Step(1f/hz);
    tracked=Particles(0).Single(p=>p.randomSeed==before.randomSeed);
    if(Vector3.Distance(tracked.position,before.position)<.00001f)held++;
    Check(Math.Abs((before.remainingLifetime-tracked.remainingLifetime)-1f/hz)<.00001f,"smoke age advances on every render call at "+hz+" Hz");
    Check(Vector3.Distance(tracked.position,before.position)<body.linearVelocity.magnitude/hz+1,"tracked moving smoke has no multi-frame position jump at "+hz+" Hz");
   }
   report.Add("Smoke render cadence "+hz+"Hz: held particle frames="+held+" / 10");
   Check(held==0,"moving smoke positions update every render call at "+hz+" Hz, without a 60 ms hold");
  }
  car.transform.position=Vector3.zero;body.linearVelocity=Vector3.zero;Reset();for(int i=0;i<180;i++)Step();
 }
 void CheckGlassSmoke() {
  foreach(var ps in Systems)ps.GetComponent<ParticleSystemRenderer>().enabled=false;
  car.transform.Find("Reference chassis").GetComponent<Renderer>().enabled=false;
  var modelPath=Environment.GetCommandLineArgs().Single(a=>a.StartsWith("-nfvu2-model=")).Substring(13);
  var model=ModelFile.Load(modelPath);var original=new GameObject("Actual 350Z body and windows");original.transform.SetParent(car.transform,false);
  var glass=VehicleInterior.CreateGlassMaterial(s=>report.Add(s));Check(glass!=null,"production cabin glass material is available");
  Check(glass.renderQueue<3000&&glass.GetFloat("_ZWrite")==0,"glass stays transparent and precedes the smoke pass");
  report.Add("Glass shader="+glass.shader.name+" queue="+glass.renderQueue);
  var paint=Solid(new Color(.13f,.26f,.3f));
  foreach(var part in model.parts.Where(p=>p.group=="body"&&string.IsNullOrEmpty(p.optionGroup))) {
   var obj=new GameObject(part.name);obj.transform.SetParent(original.transform,false);obj.layer=31;
   var mesh=new Mesh();mesh.vertices=part.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.normals=part.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.uv=part.uv.Select(v=>new Vector2(v.u,v.v)).ToArray();mesh.triangles=part.indices;mesh.colors=Enumerable.Repeat(Color.white,part.vertices.Length).ToArray();VehicleMesh.AddBackfaces(mesh);mesh.RecalculateBounds();mesh.RecalculateTangents();
   obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=model.materials[part.material].name.StartsWith("471a1dca_")?glass:paint;
  }
  var cloud=new GameObject("Controlled foreground smoke and remote tail").AddComponent<ParticleSystem>();cloud.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=cloud.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
  var emission=cloud.emission;emission.enabled=false;var shape=cloud.shape;shape.enabled=false;
  var sheet=cloud.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=sheet.numTilesY=8;sheet.frameOverTime=new ParticleSystem.MinMaxCurve(.4f);
  var r=cloud.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=Systems[0].GetComponent<ParticleSystemRenderer>().sharedMaterial;r.sortMode=ParticleSystemSortMode.Distance;r.maxParticleSize=.8f;cloud.gameObject.layer=31;
  r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});cloud.Play(false);cloud.Pause(false);
  for(int view=0;view<3;view++) {
   var eye=new Vector3(view==1?4:-4,1.8f,view==2?-5.5f:5.5f);var target=new Vector3(0,.9f,0);var front=(eye-target).normalized;
   var particles=new List<ParticleSystem.Particle>();
   for(int i=0;i<24;i++)particles.Add(new ParticleSystem.Particle{position=target+front*1.9f+Vector3.right*((i%4-1.5f)*.19f)+Vector3.up*((i/4-2.5f)*.14f),startLifetime=10,remainingLifetime=5,startSize=2.5f,startColor=new Color(.92f,.92f,.92f,.32f),randomSeed=(uint)(i+1)});
   // A long trail moves the system's sorting bounds behind the car, even
   // though its visible foreground puffs are between camera and windows.
   particles.Add(new ParticleSystem.Particle{position=target-front*50,startLifetime=10,remainingLifetime=5,startSize=.01f,startColor=Color.clear});cloud.SetParticles(particles.ToArray());
   foreach(float tint in new[]{.16f,1f}) {
    float shade=Mathf.Lerp(.2f,.015f,tint);glass.SetColor("_Color",new Color(shade,shade,shade,CabinAppearance.WindowAlpha(tint)));
    string suffix=view+"-"+(tint>.9f?"dark":"clear");
    glass.renderQueue=3000;Capture("glass-old-"+suffix+".png",eye,target);
    glass.renderQueue=2990;Capture("glass-fixed-"+suffix+".png",eye,target);
    if(tint>.9f)Check(PixelDifference("glass-old-"+suffix+".png","glass-fixed-"+suffix+".png")>150,"foreground smoke covers dark production windows despite remote emitter bounds, view="+view);
    if(view==0&&tint>.9f) {
     r.enabled=false;glass.renderQueue=3000;Capture("glass-no-smoke-old.png",eye,target);glass.renderQueue=2990;Capture("glass-no-smoke-fixed.png",eye,target);
     Check(PixelDifference("glass-no-smoke-old.png","glass-no-smoke-fixed.png")<25,"draw-order fix preserves window tint and clear-scene appearance");r.enabled=true;
    }
   }
  }
  cloud.gameObject.SetActive(false);original.SetActive(false);Destroy(cloud.gameObject);Destroy(original);Destroy(glass);Destroy(paint);
 }
 int PixelDifference(string a,string b) {
  var first=new Texture2D(2,2);var second=new Texture2D(2,2);ImageConversion.LoadImage(first,File.ReadAllBytes(Path.Combine(folder,a)));ImageConversion.LoadImage(second,File.ReadAllBytes(Path.Combine(folder,b)));
  var x=first.GetPixels32();var y=second.GetPixels32();int n=0;for(int i=0;i<x.Length;i++)if(Math.Abs(x[i].r-y[i].r)+Math.Abs(x[i].g-y[i].g)+Math.Abs(x[i].b-y[i].b)>25)n++;
  Destroy(first);Destroy(second);report.Add("visible plume pixels="+n);return n;
 }
 Material Solid(Color color) {
  var m=VehicleMaterials.Create(null,s=>{})[0];foreach(var n in new[]{"_Color","_BaseColor","_SkinColor"})if(m.HasProperty(n))m.SetColor(n,color);return m;
 }
 GameObject Box(string name,Vector3 position,Vector3 size,Color color) {
  var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.position=position;obj.transform.localScale=size;obj.layer=31;Destroy(obj.GetComponent<Collider>());
  var m=Solid(color);obj.GetComponent<Renderer>().sharedMaterial=m;return obj;
 }
 void Setup() {
  // Keep translational point velocity available for the rolling-contact check.
  car=new GameObject("Tyre smoke transient fixture");body=car.AddComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeRotation;
  Box("Road",new Vector3(0,-.08f,-8),new Vector3(140,.1f,160),new Color(.14f,.15f,.16f));
  var shell=Box("Reference chassis",new Vector3(0,.72f,.25f),new Vector3(1.6f,.7f,3.7f),new Color(.15f,.28f,.33f));shell.transform.SetParent(car.transform,true);
  for(int i=0;i<4;i++) {
   Vector3 hub=new Vector3(i%2==0?-.86f:.86f,.36f,i<2?1.25f:-1.2f);
   var go=new GameObject("WheelCollider"+i);go.transform.SetParent(car.transform,false);go.transform.localPosition=hub;wheels[i]=go.AddComponent<WheelCollider>();wheels[i].radius=.34f;wheels[i].enabled=false;
   var visual=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(visual.GetComponent<Collider>());visual.transform.SetParent(car.transform,false);visual.transform.localPosition=hub;visual.transform.localRotation=Quaternion.Euler(0,0,90);visual.transform.localScale=new Vector3(.68f,.14f,.68f);visual.layer=31;
   var m=Solid(new Color(.025f,.027f,.03f));visual.GetComponent<Renderer>().sharedMaterial=m;visuals[i]=visual.transform;
  }
  light=new GameObject("Fixture sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(50,-25,0);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.46f,.52f);RenderSettings.fog=false;
  camera=new GameObject("Tyre smoke fixture camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.34f,.42f);camera.fieldOfView=58;camera.farClipPlane=150;camera.depthTextureMode=DepthTextureMode.Depth;
  rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;
 }
 void Capture(string name,Vector3 position,Vector3 target) {
  foreach(var ps in Systems)ps.gameObject.layer=31;
  camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
  var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());Destroy(image);RenderTexture.active=previous;
 }
 void CaptureWisps(string name,Vector3 position,Vector3 target) {
  var particles=Particles(0).Concat(Particles(1)).ToArray();
  var wisps=particles.Where(p=>p.startLifetime<1.1f).ToArray();Systems[0].SetParticles(wisps,wisps.Length);
  Capture(name,position,target);
  Systems[0].SetParticles(particles,particles.Length);
 }
}
