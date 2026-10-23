using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Rachel350Z;

public sealed partial class SmokeProbe {
 sealed class OcclusionPart {public Renderer Renderer;public Material Normal;public int Marker;}
 sealed class OcclusionView {public string Name;public Vector3 Eye,Target;public bool[] Mask;public Color[] Clear,Inverse;}
 Texture2D occlusionReadback,occlusionPng;
 // Diagnostic only: use the real opaque stock wheel geometry, with grey chrome
 // and gold surfaces that can be inverted. The difference between two frozen
 // smoke renders reveals transmitted rim contrast, independently of smoke's
 // own brightness. A depth-tested ID pass restricts measurement to the wheel.
 void CheckWheelOcclusion() {
  var fixture=new GameObject("Stock wheel contrast measurement fixture");var assets=new List<UnityEngine.Object>();var opaque=new List<OcclusionPart>();
  VehicleBurnoutEffects current=null;BaselineSmoke baseline=null;GameObject currentRoot=null,baselineRoot=null;Camera eye=null;RenderTexture target=null;
  try {
   var dark=OcclusionMaterial(new Color(.10f,.10f,.10f),assets);var bright=OcclusionMaterial(new Color(.82f,.82f,.82f),assets);
   var rubber=OcclusionMaterial(new Color(.018f,.018f,.018f),assets);var bodyMat=OcclusionMaterial(new Color(.11f,.23f,.28f),assets);var road=OcclusionMaterial(new Color(.14f,.15f,.16f),assets);
   var black=OcclusionMaterial(Color.black,assets);var red=OcclusionMaterial(Color.red,assets);var blue=OcclusionMaterial(Color.blue,assets);
   OcclusionBox(fixture,"Measurement chassis",new Vector3(0,.79f,.1f),new Vector3(1.55f,.6f,3.7f),bodyMat,opaque);
   OcclusionBox(fixture,"Measurement road",new Vector3(0,-.07f,0),new Vector3(400,.1f,400),road,opaque);
   var rigid=fixture.AddComponent<Rigidbody>();rigid.useGravity=false;rigid.constraints=RigidbodyConstraints.FreezeRotation;
   var colliders=new WheelCollider[4];var rendered=new Transform[4];
   var path=Environment.GetCommandLineArgs().Single(a=>a.StartsWith("-nfvu2-model=")).Substring(13);var model=ModelFile.Load(path);
   var parts=model.parts.Where(p=>p.group=="wheel").ToArray();Check(parts.Length>0,"wheel occlusion fixture loads original stock wheel geometry");
   var meshes=parts.Select(part=>{var mesh=new Mesh();mesh.vertices=part.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.normals=part.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.uv=part.uv.Select(v=>new Vector2(v.u,v.v)).ToArray();mesh.triangles=part.indices;mesh.colors=Enumerable.Repeat(Color.white,part.vertices.Length).ToArray();VehicleMesh.AddBackfaces(mesh);mesh.RecalculateBounds();assets.Add(mesh);return mesh;}).ToArray();
   for(int wheel=0;wheel<4;wheel++) {
    var hub=new GameObject("Stock wheel "+wheel);hub.transform.SetParent(fixture.transform,false);hub.transform.localPosition=new Vector3(wheel%2==0?-.8f:.8f,.35f,wheel<2?1.3f:-1.35f);rendered[wheel]=hub.transform;
    colliders[wheel]=hub.AddComponent<WheelCollider>();colliders[wheel].radius=.34f;colliders[wheel].enabled=false;
    var shape=new GameObject("Original wheel orientation");shape.transform.SetParent(hub.transform,false);shape.transform.localRotation=Quaternion.Euler(0,wheel%2==0?180:0,0);
    for(int part=0;part<parts.Length;part++) {
     string name=model.materials[parts[part].material].name;int marker=name.StartsWith("wheel_chrome_")?1:name.StartsWith("wheel_gold_")?2:0;
     var obj=new GameObject(parts[part].name);obj.layer=30;obj.transform.SetParent(shape.transform,false);obj.AddComponent<MeshFilter>().sharedMesh=meshes[part];var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=marker==1?bright:marker==2?dark:rubber;
     opaque.Add(new OcclusionPart{Renderer=renderer,Normal=renderer.sharedMaterial,Marker=wheel==2?marker:0});
    }
   }
   current=VehicleBurnoutEffects.Create(fixture,null,colliders,rendered,s=>report.Add(s));baseline=BaselineSmoke.Create(fixture,null,colliders,rendered,s=>report.Add(s));
   Check(current!=null&&baseline!=null,"current and installed baseline wheel-occlusion emitters initialize");
   currentRoot=(GameObject)typeof(VehicleBurnoutEffects).GetField("root",F).GetValue(current);baselineRoot=(GameObject)typeof(BaselineSmoke).GetField("root",F).GetValue(baseline);
   var currentRenderers=currentRoot.GetComponentsInChildren<ParticleSystemRenderer>();var baselineRenderers=baselineRoot.GetComponentsInChildren<ParticleSystemRenderer>();
   foreach(var r in currentRenderers.Concat(baselineRenderers)){r.gameObject.layer=30;r.enabled=false;}
   eye=new GameObject("Wheel occlusion diagnostic camera").AddComponent<Camera>();eye.enabled=false;eye.cullingMask=1<<30;eye.clearFlags=CameraClearFlags.SolidColor;eye.backgroundColor=new Color(.25f,.34f,.42f);eye.fieldOfView=38;eye.farClipPlane=180;eye.depthTextureMode=DepthTextureMode.Depth;eye.allowHDR=true;
   target=new RenderTexture(640,360,24,RenderTextureFormat.ARGBHalf);eye.targetTexture=target;
   var views=new[]{
    new OcclusionView{Name="side",Eye=new Vector3(-4.5f,1.15f,-1.35f),Target=new Vector3(-.8f,.4f,-1.35f)},
    new OcclusionView{Name="rear-quarter",Eye=new Vector3(-3.8f,1.35f,-4.7f),Target=new Vector3(-.65f,.6f,-1)},
    new OcclusionView{Name="front-quarter",Eye=new Vector3(-3.8f,1.2f,2.5f),Target=new Vector3(-.65f,.55f,-1)},
    new OcclusionView{Name="elevated-rear",Eye=new Vector3(-3.7f,2.9f,-4.3f),Target=new Vector3(-.55f,.6f,-1)}};
   foreach(var view in views) {
    OcclusionPose(eye,fixture.transform,view);OcclusionPattern(opaque,bright,dark,black,red,blue,2);var background=eye.backgroundColor;eye.backgroundColor=Color.black;
    var ids=OcclusionFrame(eye,target,null);eye.backgroundColor=background;view.Mask=ids.Select(c=>(c.r>.08f&&c.r>c.g*2&&c.r>c.b*2)||(c.b>.08f&&c.b>c.r*2&&c.b>c.g*2)).ToArray();
    OcclusionPattern(opaque,bright,dark,black,red,blue,0);view.Clear=OcclusionFrame(eye,target,"wheel-occlusion-clear-"+view.Name+".png");
    OcclusionPattern(opaque,bright,dark,black,red,blue,1);view.Inverse=OcclusionFrame(eye,target,null);OcclusionPattern(opaque,bright,dark,black,red,blue,0);
    Check(view.Mask.Count(value=>value)>0,"opaque depth-tested stock rim ROI is visible from "+view.Name);
   }
   float rpmValue=950;var wind=new Vector3(.15f,0,.05f);var velocity=new Vector3(10,0,20);var advance=typeof(BaselineSmoke).GetMethod("Advance",F);
   Func<int,TyreSmokeContact> contact=i=>new TyreSmokeContact{Grounded=true,Hub=rendered[i].position,Point=rendered[i].position-Vector3.up*.34f,Normal=Vector3.up,Rpm=rpmValue,Load=3500};
   Func<int,BaselineContact> oldContact=i=>new BaselineContact{Grounded=true,Hub=rendered[i].position,Point=rendered[i].position-Vector3.up*.34f,Normal=Vector3.up,Rpm=rpmValue,Load=3500};
   var csv=new List<string>{"phase,time_s,view,version,rim_pixels,weighted_transmission,median_transmission,p10_transmission,p90_transmission,actual_rim_contrast,clear_rim_contrast,particles"};
   var hot=new Dictionary<string,List<double>>();float elapsed=0;
   Action<string,int> capture=(phase,sample)=> {
    foreach(var view in views) {
     OcclusionPose(eye,fixture.transform,view);
     // Native opaque materials may apply world-position-dependent fog or
     // environmental shading even in this isolated menu fixture. Calibrate at
     // this exact pose instead of comparing a travelled wheel with world zero.
     foreach(var r in currentRenderers.Concat(baselineRenderers))r.enabled=false;
     OcclusionPattern(opaque,bright,dark,black,red,blue,0);view.Clear=OcclusionFrame(eye,target,phase.StartsWith("hot",StringComparison.Ordinal)&&sample==0?"wheel-occlusion-reference-"+phase+"-"+view.Name+".png":null);
     OcclusionPattern(opaque,bright,dark,black,red,blue,1);view.Inverse=OcclusionFrame(eye,target,null);OcclusionPattern(opaque,bright,dark,black,red,blue,0);
     foreach(bool old in new[]{true,false}) {
      foreach(var r in currentRenderers)r.enabled=!old;foreach(var r in baselineRenderers)r.enabled=old;
      string label=old?"installed":"current";bool isHot=phase.StartsWith("hot",StringComparison.Ordinal);bool save=!isHot||sample==0||sample==3||sample==7;
      OcclusionPattern(opaque,bright,dark,black,red,blue,0);var normal=OcclusionFrame(eye,target,save?"wheel-occlusion-"+phase+"-"+sample+"-"+view.Name+"-"+label+".png":null);
      OcclusionPattern(opaque,bright,dark,black,red,blue,1);var inverted=OcclusionFrame(eye,target,null);OcclusionPattern(opaque,bright,dark,black,red,blue,0);
      int count=old?baselineRoot.GetComponentsInChildren<ParticleSystem>().Sum(ps=>ps.particleCount):currentRoot.GetComponentsInChildren<ParticleSystem>().Sum(ps=>ps.particleCount);
      double transmission=OcclusionMetric(view,normal,inverted,phase,elapsed,label,count,csv);string key=phase+"/"+label+"/"+view.Name;
      if(isHot){if(!hot.ContainsKey(key))hot[key]=new List<double>();hot[key].Add(transmission);}
     }
     foreach(var r in currentRenderers.Concat(baselineRenderers))r.enabled=false;
    }
   };
   for(int frame=1;frame<=522;frame++) {
    elapsed=frame/60f;fixture.transform.position=velocity*elapsed;rigid.linearVelocity=velocity;
    current.Advance(1f/60,true,true,contact,wind);advance.Invoke(baseline,new object[]{1f/60,true,true,oldContact,wind});
    if(frame==15)capture("cold",0);
   if(frame>=480&&(frame-480)%6==0)capture("hot",(frame-480)/6);
   }
   // Moderate wheel overspeed with substantial lateral slip, after a fresh
   // eight-second equilibration at this RPM, complements the extreme source.
   rpmValue=650;
   for(int frame=1;frame<=522;frame++) {
    elapsed+=1f/60;fixture.transform.position+=velocity/60;rigid.linearVelocity=velocity;
    current.Advance(1f/60,true,true,contact,wind);advance.Invoke(baseline,new object[]{1f/60,true,true,oldContact,wind});
    if(frame>=480&&(frame-480)%6==0)capture("hot-650",(frame-480)/6);
   }
   // Repeat both wheel-speed cases while sliding toward the opposite side.
   // Eight seconds exceeds every cloud lifetime, so the sampled wake belongs
   // entirely to this lateral direction rather than the preceding manoeuvre.
   velocity=new Vector3(-10,0,20);
   foreach(float oppositeRpm in new[]{950f,650f}) {
    rpmValue=oppositeRpm;string phase=oppositeRpm>900?"hot-negative":"hot-650-negative";
    for(int frame=1;frame<=522;frame++) {
     elapsed+=1f/60;fixture.transform.position+=velocity/60;rigid.linearVelocity=velocity;
     current.Advance(1f/60,true,true,contact,wind);advance.Invoke(baseline,new object[]{1f/60,true,true,oldContact,wind});
     if(frame>=480&&(frame-480)%6==0)capture(phase,(frame-480)/6);
    }
   }
   // Let all emitted clouds expire; source-off visibility is measured at the
   // same real wheel, rather than inferred from a distant bright plume.
   rigid.linearVelocity=Vector3.zero;
   for(int frame=0;frame<390;frame++){elapsed+=1f/60;current.Advance(1f/60,false,false,contact,wind);advance.Invoke(baseline,new object[]{1f/60,false,false,oldContact,wind});}
   capture("released",0);
   Check(currentRoot.GetComponentsInChildren<ParticleSystem>().All(ps=>ps.particleCount==0)&&baselineRoot.GetComponentsInChildren<ParticleSystem>().All(ps=>ps.particleCount==0),"occlusion clears after source-off clouds expire");
   rpmValue=20*30/(Mathf.PI*.34f);velocity=Vector3.forward*20;
   for(int frame=0;frame<120;frame++){elapsed+=1f/60;fixture.transform.position+=velocity/60;rigid.linearVelocity=velocity;current.Advance(1f/60,true,true,contact,wind);advance.Invoke(baseline,new object[]{1f/60,true,true,oldContact,wind});}
   capture("rolling",0);
   Check(currentRoot.GetComponentsInChildren<ParticleSystem>().All(ps=>ps.particleCount==0)&&baselineRoot.GetComponentsInChildren<ParticleSystem>().All(ps=>ps.particleCount==0),"normal rolling without slip leaves the rim free of new smoke");
   File.WriteAllLines(Path.Combine(folder,"wheel-occlusion.csv"),csv);
   foreach(var pair in hot)report.Add("Hot fast-drift rim contrast "+pair.Key+": transmitted fraction min/mean/max="+pair.Value.Min().ToString("F4",CultureInfo.InvariantCulture)+"/"+pair.Value.Average().ToString("F4",CultureInfo.InvariantCulture)+"/"+pair.Value.Max().ToString("F4",CultureInfo.InvariantCulture));
   report.Add("Wheel occlusion diagnostic: stock wheel geometry; grey chrome/gold measurement materials; real opaque depth; 20m/s forward and both +10/-10m/s lateral directions, separate 950/650rpm cases each with 8s warmup and eight hot snapshots, cold/released/normal-rolling controls. CSV uses locally calibrated frozen pattern inversion to measure transmitted wheel contrast, not smoke brightness. Diagnostic only: stronger occlusion does not establish visual quality. The forced dense source passed the former contrast bounds but was rejected in actual overhead gameplay and reverted.");
  } finally {
   if(currentRoot!=null)currentRoot.SetActive(false);if(baselineRoot!=null)baselineRoot.SetActive(false);current?.Dispose();baseline?.Dispose();fixture.SetActive(false);Destroy(fixture);
   if(eye!=null)Destroy(eye.gameObject);if(target!=null){target.Release();Destroy(target);}foreach(var asset in assets)Destroy(asset);
   if(occlusionReadback!=null)Destroy(occlusionReadback);if(occlusionPng!=null)Destroy(occlusionPng);occlusionReadback=occlusionPng=null;
  }
 }
 Material OcclusionMaterial(Color color,List<UnityEngine.Object> assets) {
  var shader=Shader.Find("Unlit/Color");var material=shader!=null&&shader.isSupported?new Material(shader):Solid(color);if(material.HasProperty("_Color"))material.SetColor("_Color",color);assets.Add(material);return material;
 }
 void OcclusionBox(GameObject parent,string name,Vector3 position,Vector3 size,Material material,List<OcclusionPart> parts) {
  var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.layer=30;Destroy(obj.GetComponent<Collider>());obj.transform.SetParent(parent.transform,false);obj.transform.localPosition=position;obj.transform.localScale=size;var renderer=obj.GetComponent<Renderer>();renderer.sharedMaterial=material;parts.Add(new OcclusionPart{Renderer=renderer,Normal=material});
 }
 void OcclusionPose(Camera camera,Transform source,OcclusionView view){var eye=source.TransformPoint(view.Eye);var target=source.TransformPoint(view.Target);camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));}
 void OcclusionPattern(List<OcclusionPart> parts,Material bright,Material dark,Material black,Material red,Material blue,int mode) {
  foreach(var part in parts)part.Renderer.sharedMaterial=mode==2?(part.Marker==1?red:part.Marker==2?blue:black):part.Marker==0?part.Normal:part.Marker==1?(mode==0?bright:dark):(mode==0?dark:bright);
 }
 Color[] OcclusionFrame(Camera camera,RenderTexture target,string name) {
  camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
  if(occlusionReadback==null)occlusionReadback=new Texture2D(target.width,target.height,TextureFormat.RGBAHalf,false,true);
  try {
   occlusionReadback.ReadPixels(new Rect(0,0,target.width,target.height),0,0);occlusionReadback.Apply();var pixels=occlusionReadback.GetPixels();
   if(name!=null){if(occlusionPng==null)occlusionPng=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);occlusionPng.SetPixels(pixels.Select(c=>new Color(Mathf.LinearToGammaSpace(c.r),Mathf.LinearToGammaSpace(c.g),Mathf.LinearToGammaSpace(c.b))).ToArray());occlusionPng.Apply();File.WriteAllBytes(Path.Combine(folder,name),occlusionPng.EncodeToPNG());}
   return pixels;
  } finally {RenderTexture.active=old;}
 }
 double OcclusionMetric(OcclusionView view,Color[] normal,Color[] inverted,string phase,float time,string version,int count,List<string> csv) {
  var transmission=new List<double>();double clearSum=0,actualSum=0;
  for(int i=0;i<normal.Length;i++)if(view.Mask[i]) {
   double clear=Math.Abs(OcclusionLuma(view.Clear[i])-OcclusionLuma(view.Inverse[i]));if(clear<.04)continue;
   double actual=Math.Abs(OcclusionLuma(normal[i])-OcclusionLuma(inverted[i]));clearSum+=clear;actualSum+=actual;transmission.Add(actual/clear);
  }
  Check(transmission.Count>0&&clearSum>0,"native wheel contrast inversion has a valid depth-tested ROI: "+view.Name);
  transmission.Sort();double weighted=actualSum/clearSum;
  Check(!double.IsNaN(weighted)&&!double.IsInfinity(weighted),"wheel-contrast transmission is finite: "+view.Name);
  if(count==0)Check(Math.Abs(weighted-1)<.001,"empty smoke renderer preserves the locally calibrated wheel contrast: "+view.Name);
  csv.Add(string.Join(",",new[]{phase,time.ToString("F3",CultureInfo.InvariantCulture),view.Name,version,transmission.Count.ToString(),weighted.ToString("F6",CultureInfo.InvariantCulture),transmission[transmission.Count/2].ToString("F6",CultureInfo.InvariantCulture),transmission[(int)((transmission.Count-1)*.1)].ToString("F6",CultureInfo.InvariantCulture),transmission[(int)((transmission.Count-1)*.9)].ToString("F6",CultureInfo.InvariantCulture),(actualSum/transmission.Count).ToString("F6",CultureInfo.InvariantCulture),(clearSum/transmission.Count).ToString("F6",CultureInfo.InvariantCulture),count.ToString()}));return weighted;
 }
 double OcclusionLuma(Color c)=>c.r*.2126+c.g*.7152+c.b*.0722;
}
