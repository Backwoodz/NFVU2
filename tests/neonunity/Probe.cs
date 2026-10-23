using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.PostProcessing;
using Rachel350Z;

[BepInPlugin("local.nfvu2.neonprobe","NFVU2 temporary neon probe","0.0.1")]
public sealed class NeonProbe:BaseUnityPlugin {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 readonly List<string> report=new List<string>();
 string folder;GameObject car,road;VehicleNeon neon;BaselineNeon baseline;Camera camera;RenderTexture rt;int checks;
 Vector3 origin=new Vector3(0,1000,0);
 IEnumerator Start() {
  var args=Environment.GetCommandLineArgs();if(!args.Contains("-nfvu2-neon-probe"))yield break;
  folder=args.Single(a=>a.StartsWith("-nfvu2-output=")).Substring(14);Application.runInBackground=true;
  yield return new WaitForSecondsRealtime(17);
  try {Setup();Run();File.WriteAllText(Path.Combine(folder,"completed.txt"),checks+" native neon checks passed.");}
  catch(Exception e){File.WriteAllText(Path.Combine(folder,"error.txt"),e.ToString());}
  finally {File.WriteAllLines(Path.Combine(folder,"results.txt"),report);neon?.Dispose();baseline?.Dispose();if(rt!=null)rt.Release();}
  Application.Quit();
 }
 void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 T Field<T>(object effect,string name)=>(T)effect.GetType().GetField(name,F).GetValue(effect);
 Mesh Mesh(object effect)=>Field<Mesh>(effect,"mesh");
 MeshRenderer Renderer(object effect)=>Field<MeshRenderer>(effect,"renderer");
 void Lights(object effect,bool enabled){foreach(var l in Field<List<Light>>(effect,"lights"))l.enabled=enabled;}
 Vector3 Center(object effect) {
  var m=Mesh(effect);return Field<GameObject>(effect,"projection").transform.TransformPoint(m.bounds.center);
 }
 void Setup() {
  road=GameObject.CreatePrimitive(PrimitiveType.Cube);road.name="Neon fixture road";road.layer=LayerMask.NameToLayer("Default");road.transform.position=origin+Vector3.down*.1f;road.transform.localScale=new Vector3(300,.2f,300);
  road.GetComponent<Renderer>().enabled=false; // Depth fixture below uses its own visible layer.
  var surface=new GameObject("Visible road");surface.layer=31;surface.transform.SetParent(road.transform,false);
  surface.AddComponent<MeshFilter>().sharedMesh=road.GetComponent<MeshFilter>().sharedMesh;
  var templates=VehicleMaterials.Create(null,s=>report.Add(s));var m=templates[1];Destroy(templates[0]);surface.AddComponent<MeshRenderer>().sharedMaterial=m;
  car=new GameObject("Neon transient vehicle");car.transform.position=origin+Vector3.up*.2f;
  var path=Environment.GetCommandLineArgs().Single(a=>a.StartsWith("-nfvu2-model=")).Substring(13);
  var model=ModelFile.Load(path);neon=VehicleNeon.Create(car,model,s=>report.Add(s));baseline=BaselineNeon.Create(car,model,s=>report.Add(s));
  Check(Renderer(neon)!=null&&Renderer(baseline)!=null,"production projection shader available");
  foreach(var e in new object[]{neon,baseline})Field<GameObject>(e,"projection").layer=31;
  camera=new GameObject("Neon fixture camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.farClipPlane=100;camera.nearClipPlane=.1f;camera.depthTextureMode=DepthTextureMode.Depth;
  rt=new RenderTexture(640,360,24);rt.Create();camera.targetTexture=rt;
  Physics.SyncTransforms();
  report.Add("shader="+Renderer(neon).sharedMaterial.shader.name+" keywords="+string.Join(",",Renderer(neon).sharedMaterial.shaderKeywords)+"; soft particles="+QualitySettings.softParticles+" global keyword="+Shader.IsKeywordEnabled("SOFTPARTICLES_ON"));
 }
 void Run() {
  // Position and yaw vary at render frequency while real PhysX raycasts find the road.
  // A car-relative camera makes a stationary, steady mask the expected rendered result.
  foreach(int hz in new[]{30,60,144}) {
   neon.Tick(false,0);baseline.Tick(false,0);
   float maxOld=0,maxNew=0;int staleOld=0;Vector3 previousOld=Vector3.zero;var timer=Stopwatch.StartNew();
   for(int frame=0;frame<hz*2;frame++) {
    float t=(float)frame/hz;car.transform.position=origin+new Vector3(8*t,.2f,30*t);car.transform.rotation=Quaternion.Euler(0,50*t,0);
    neon.Tick(true,1f/hz);baseline.Tick(true,1f/hz);
    var expected=new Vector3(car.transform.position.x,origin.y+.06f,car.transform.position.z);
    var actual=Center(neon);var old=Center(baseline);maxNew=Math.Max(maxNew,Vector2.Distance(new Vector2(actual.x,actual.z),new Vector2(expected.x,expected.z)));maxOld=Math.Max(maxOld,Vector2.Distance(new Vector2(old.x,old.z),new Vector2(expected.x,expected.z)));
    if(frame>0&&old==previousOld)staleOld++;previousOld=old;
    Check(Mesh(neon).triangles.Length==2016,"flat road retains complete mask");
   }
   report.Add(hz+"Hz: max horizontal lag baseline="+maxOld+"m, current="+maxNew+"m; stale baseline frames="+staleOld+"; combined CPU ms="+timer.Elapsed.TotalMilliseconds);
   Check(maxOld>.8f&&staleOld>0,"baseline reproduces visible stepping at race speed");
   Check(maxNew<.002f,"current mask follows every rendered vehicle pose");
  }
  car.transform.position=origin+Vector3.up*.2f;car.transform.rotation=Quaternion.identity;neon.Tick(false,0);baseline.Tick(false,0);
  neon.Tick(true,1f/60);baseline.Tick(true,1f/60);Lights(neon,false);Lights(baseline,false);
  Renderer(baseline).enabled=false;
  var first=Capture("fixed-start.png");
  car.transform.position+=Vector3.forward*.5f;neon.Tick(true,1f/60);baseline.Tick(true,1f/60);Lights(neon,false);Lights(baseline,false);Renderer(baseline).enabled=false;
  var second=Capture("fixed-next.png");
  Renderer(neon).enabled=false;Renderer(baseline).enabled=true;var oldImage=Capture("baseline-next.png");
  int steady=Difference(first,second),jump=Difference(second,oldImage);
  report.Add("Chase-camera changed pixels: fixed="+steady+", stale baseline="+jump);
  Check(jump>150,"native rendered baseline visibly moves relative to car");Check(steady<100,"native rendered mask stays steady while following car");
  Renderer(baseline).enabled=false;neon.Tick(false,0);Check(!Renderer(neon).enabled&&Field<List<Light>>(neon,"lights").All(l=>!l.enabled),"unpowered effect fully disabled");
  car.transform.position+=Vector3.right*20;neon.Tick(true,0);Check(Vector3.Distance(Center(neon),new Vector3(car.transform.position.x,origin.y+.06f,car.transform.position.z))<.002f,"reactivation samples immediately even at zero delta");
  neon.Enabled=false;neon.Tick(true,1f/60);Check(!Renderer(neon).enabled&&Field<List<Light>>(neon,"lights").All(l=>!l.enabled),"saved neon off switch remains effective while driving");neon.Enabled=true;
  var lightIntensity=Field<List<Light>>(neon,"lights")[0].intensity;
  for(int i=0;i<100;i++){neon.Tick(true,1f/60);Check(Math.Abs(Field<List<Light>>(neon,"lights")[0].intensity-lightIntensity)<.0001f,"non-pulsing light intensity constant");}
  neon.Pulsing=true;neon.Tick(true,.1f);Check(Math.Abs(Field<List<Light>>(neon,"lights")[0].intensity-lightIntensity)>.05f,"explicit pulse still works");
  car.transform.position=origin+Vector3.up*10;neon.Tick(true,1f/60);Check(Mesh(neon).triangles.Length==0,"no hovering sheet when ground is out of range");
  road.SetActive(false);car.transform.position=origin+Vector3.up*.2f;Physics.SyncTransforms();neon.Tick(true,1f/60);Check(Mesh(neon).triangles.Length==0,"no sheet over missing ground");
  road.SetActive(true);road.transform.rotation=Quaternion.Euler(0,0,20);Physics.SyncTransforms();neon.Tick(true,1f/60);Check(Mesh(neon).triangles.Length>1700,"slope retains grounded projection");
  foreach(var v in Mesh(neon).vertices){var world=Field<GameObject>(neon,"projection").transform.TransformPoint(v);Check(Physics.Raycast(world+Vector3.up*.1f,Vector3.down,out var hit,.3f)&&Vector3.Distance(world,hit.point)>.02f,"slope mask remains above actual collider");}
  road.transform.rotation=Quaternion.identity;Physics.SyncTransforms();car.transform.position=origin+new Vector3(149,.2f,0);neon.Tick(true,1f/60);int partial=Mesh(neon).triangles.Length;Check(partial>0&&partial<2016,"cliff edge omits ungrounded triangles");
  var self=car.AddComponent<BoxCollider>();self.size=new Vector3(4,.1f,6);car.transform.position=origin+Vector3.up*.2f;Physics.SyncTransforms();neon.Tick(true,1f/60);Check(Math.Abs(Center(neon).y-origin.y-.06f)<.002f,"own car collider cannot become neon ground");
  self.enabled=false;
  var step=GameObject.CreatePrimitive(PrimitiveType.Cube);step.transform.position=origin+new Vector3(5,.5f,0);step.transform.localScale=new Vector3(10,1,10);step.GetComponent<Renderer>().enabled=false;Physics.SyncTransforms();neon.Tick(true,1f/60);
  var stepped=Mesh(neon);Check(stepped.triangles.Length>0&&stepped.triangles.Length<2016,"high step retains both surfaces without a glowing connecting sheet");
  var vtx=stepped.vertices;var indices=stepped.triangles;
  for(int i=0;i<indices.Length;i+=3)Check(Math.Abs(vtx[indices[i]].y-vtx[indices[i+1]].y)<=.65f&&Math.Abs(vtx[indices[i]].y-vtx[indices[i+2]].y)<=.65f,"no triangle crosses the high step");
  step.GetComponent<Collider>().isTrigger=true;Physics.SyncTransforms();neon.Tick(true,1f/60);Check(Math.Abs(Center(neon).y-origin.y-.06f)<.002f,"trigger volumes do not become neon ground");step.SetActive(false);
  var timerOnly=Stopwatch.StartNew();for(int i=0;i<2000;i++){car.transform.position=origin+new Vector3(0,.2f,(i%240)*.25f);neon.Tick(true,1f/144);}timerOnly.Stop();
  report.Add("Current projection CPU ms/frame (2000 moving poses, 375 real PhysX rays each)="+timerOnly.Elapsed.TotalMilliseconds/2000);
  CheckPavedSurface();
  neon.Dispose();neon.Tick(true,1f/60);Check(Field<List<Light>>(neon,"lights").Count==0,"disposal removes owned lights and permits harmless late ticks");
 }
 void CheckPavedSurface() {
  road.SetActive(false);neon.Pulsing=false;car.transform.position=origin+Vector3.up*.2f;car.transform.rotation=Quaternion.identity;
  var template=Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m=>m!=null&&m.shader!=null&&m.shader.name=="Custom/Heightmap"&&m.HasTexture("_DiffuseArrayTex")&&m.GetTexture("_DiffuseArrayTex")!=null);
  Check(template!=null,"native terrain material and texture arrays available");
  var mat=new Material(template);report.Add("Terrain template="+template.name+" displacement="+mat.GetFloat("_Displacement")+" tess="+mat.GetFloat("_Tess")+" shader LOD="+mat.shader.maximumLOD+" global="+Shader.globalMaximumLOD);
  var mask=new Texture2D(2,2);mask.SetPixels(Enumerable.Repeat(new Color(0,0,1,1),4).ToArray());mask.Apply();mat.SetTexture("_ClearedMaskTex",mask);
  var obj=new GameObject("Native paved terrain fixture");obj.layer=LayerMask.NameToLayer("Default");obj.transform.position=origin;camera.cullingMask|=1<<obj.layer;
  var mesh=new Mesh();var points=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
  const int size=16;for(int z=0;z<=size;z++)for(int x=0;x<=size;x++){points.Add(new Vector3(x-size/2,0,z-size/2));uv.Add(new Vector2((float)x/size,(float)z/size));}
  for(int z=0;z<size;z++)for(int x=0;x<size;x++){int a=z*(size+1)+x,b=a+size+1;indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
  mesh.vertices=points.ToArray();mesh.uv=uv.ToArray();mesh.colors=Enumerable.Repeat((Color)Heightmap.GetBiomeColor(Heightmap.Biome.Meadows),points.Count).ToArray();mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
  obj.AddComponent<MeshFilter>().sharedMesh=mesh;var ground=obj.AddComponent<MeshRenderer>();ground.sharedMaterial=mat;obj.AddComponent<MeshCollider>().sharedMesh=mesh;
  // Awake sees no material and leaves this menu-only Heightmap disabled. No
  // world generator, terrain painting, TerrainComp or saved data is involved.
  var map=obj.AddComponent<Heightmap>();map.enabled=false;map.m_material=mat;
  var group=obj.GetComponent<RenderGroupSubscriber>();if(group!=null)DestroyImmediate(group);
  var block=new MaterialPropertyBlock();ground.GetPropertyBlock(block);block.SetFloat("_LodHideDistance",5000);ground.SetPropertyBlock(block);ground.enabled=true;
  var sun=new GameObject("Paved fixture sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.8f;sun.transform.rotation=Quaternion.Euler(50,20,0);
  Physics.SyncTransforms();
  CheckSoftFalloff(ground);
  for(int pose=0;pose<3;pose++) {
   car.transform.position=origin+new Vector3(pose*.31f,.2f,pose*.43f);neon.Tick(true,1f/60);baseline.Tick(false,0);baseline.Tick(true,1f/60);Lights(neon,false);Lights(baseline,false);Renderer(neon).enabled=false;Renderer(baseline).enabled=false;
   var off=Capture("paved-off-"+pose+".png");ground.enabled=false;var blank=Capture("paved-blank-"+pose+".png");ground.enabled=true;
   Check(Energy(off,blank)>100000,"native paved fixture actually renders opaque terrain");
   Renderer(baseline).enabled=true;var old=Capture("paved-before-"+pose+".png");Renderer(baseline).enabled=false;Renderer(neon).enabled=true;var current=Capture("paved-fixed-"+pose+".png");
   report.Add("Native paved pose="+pose+" visible green energy: previous="+Energy(old,off)+", current="+Energy(current,off)+"; global soft particles="+Shader.IsKeywordEnabled("SOFTPARTICLES_ON"));
   Check(Energy(current,off)>Energy(old,off)*.9,"dimmed grounded neon stays visible on paved terrain");
   // Compare the same height/mesh too: isolate the material correction from
   // the new geometric clearance and denser terrain sampling.
   var glow=Renderer(baseline).sharedMaterial;glow.SetFloat("_InvFade",1000);Renderer(neon).enabled=false;Renderer(baseline).enabled=true;var materialOnly=Capture("paved-material-only-"+pose+".png");
   Check(Energy(materialOnly,off)>Energy(old,off)*2,"material-only comparison confirms near-ground fade independently");glow.SetFloat("_InvFade",1);Renderer(baseline).enabled=false;
  }
  CheckNightBrightness(ground,sun);
  // Native-sized 1 m terrain triangles with gentle creases between stamps.
  // A coarse neon triangle can cut through those creases despite grounded vertices.
  for(int i=0;i<points.Count;i++){var p=points[i];p.y=.18f*Mathf.Sin(p.x*2.1f)*Mathf.Cos(p.z*1.8f);points[i]=p;}
  mesh.vertices=points.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();var collider=obj.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=mesh;Physics.SyncTransforms();
  int oldBuried=0,newBuried=0;
  for(int pose=0;pose<12;pose++) {
   car.transform.position=origin+new Vector3(pose*.13f,.3f,pose*.17f);car.transform.rotation=Quaternion.Euler(0,pose*7,0);
   baseline.Tick(false,0);baseline.Tick(true,1f/60);neon.Tick(true,1f/60);Lights(baseline,false);Lights(neon,false);
   oldBuried+=BuriedSamples(baseline,collider);newBuried+=BuriedSamples(neon,collider);
   if(pose==4){Renderer(neon).enabled=false;Renderer(baseline).enabled=true;Capture("uneven-before.png");Renderer(baseline).enabled=false;Renderer(neon).enabled=true;Capture("uneven-fixed.png");}
  }
  report.Add("Uneven native-sized terrain: buried triangle samples previous="+oldBuried+", current="+newBuried);
  Check(oldBuried>20,"previous coarse mesh reproduces light disappearing into terrain creases");Check(newBuried==0,"denser lifted mesh stays above all sampled terrain creases during movement and turning");
  Renderer(baseline).enabled=false;Renderer(neon).enabled=false;var unlit=Capture("occlusion-off.png");Renderer(neon).enabled=true;var unoccluded=Capture("occlusion-open.png");
  var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.layer=31;cover.transform.SetPositionAndRotation(car.transform.position+Vector3.up*.3f,car.transform.rotation);cover.transform.localScale=new Vector3(6,.5f,9);cover.GetComponent<Collider>().enabled=false;
  var coverTemplates=VehicleMaterials.Create(null,s=>{});cover.GetComponent<Renderer>().sharedMaterial=coverTemplates[1];Destroy(coverTemplates[0]);
  Renderer(neon).enabled=false;var coveredOff=Capture("occlusion-covered-off.png");Renderer(neon).enabled=true;var coveredOn=Capture("occlusion-covered-on.png");
  report.Add("Opaque cover green energy="+Energy(coveredOn,coveredOff)+"; uncovered="+Energy(unoccluded,unlit));
  Check(Energy(coveredOn,coveredOff)<Energy(unoccluded,unlit)*.1,"opaque car-sized geometry still occludes neon; no depth-test bypass");
  cover.SetActive(false);Destroy(cover);Destroy(coverTemplates[1]);
  obj.SetActive(false);sun.enabled=false;Destroy(obj);Destroy(sun.gameObject);Destroy(mat);Destroy(mesh);Destroy(mask);
 }
 void CheckSoftFalloff(MeshRenderer ground) {
  car.transform.position=origin+Vector3.up*.2f;car.transform.rotation=Quaternion.identity;neon.Tick(true,1f/60);baseline.Tick(false,0);baseline.Tick(true,1f/60);Lights(neon,false);Lights(baseline,false);
  var old=Renderer(baseline).sharedMaterial;old.SetFloat("_InvFade",1000);
  foreach(var p in new[]{"_Color","_TintColor","_BaseColor"})if(old.HasColor(p)&&Renderer(neon).sharedMaterial.HasColor(p))old.SetColor(p,Renderer(neon).sharedMaterial.GetColor(p));
  ground.enabled=false;camera.orthographic=true;camera.orthographicSize=3.5f;
  Renderer(neon).enabled=false;Renderer(baseline).enabled=true;var previous=Capture("falloff-before.png",true);
  Renderer(baseline).enabled=false;Renderer(neon).enabled=true;var current=Capture("falloff-soft.png",true);
  var a=previous.Select(p=>Mathf.GammaToLinearSpace(p.g/255f)).ToArray();var b=current.Select(p=>Mathf.GammaToLinearSpace(p.g/255f)).ToArray();float peak=a.Max();
  int oldTail=a.Count(v=>v>peak*.008f&&v<peak*.16f),newTail=b.Count(v=>v>peak*.008f&&v<peak*.16f);
  float oldGradient=Gradient(a),newGradient=Gradient(b);double energy=a.Sum(v=>(double)v),softEnergy=b.Sum(v=>(double)v);
  report.Add("Soft falloff linear image energy="+softEnergy+" / "+energy+"; peak="+b.Max()+" / "+peak+"; weak halo pixels="+newTail+" / "+oldTail+"; max gradient="+newGradient+" / "+oldGradient);
  Check(softEnergy>energy*.75&&softEnergy<energy*1.15,"soft halo redistributes the existing light budget without overexposure");
  Check(b.Max()<peak*.8f&&newTail>oldTail*1.3f,"lower core and wider faint halo replace the concentrated light strips");
  Check(newGradient<oldGradient*.8f,"rendered transition into darkness has a gentler spatial gradient");
  Check(Field<Texture2D>(neon,"texture").mipmapCount>1,"soft neon texture has mipmaps for distant filtering");
  ground.enabled=true;camera.orthographic=false;old.SetFloat("_InvFade",1);Renderer(baseline).enabled=false;
 }
 float Gradient(float[] pixels) {
  float max=0;for(int y=0;y<rt.height-1;y++)for(int x=0;x<rt.width-1;x++){int i=y*rt.width+x;max=Math.Max(max,Math.Max(Math.Abs(pixels[i]-pixels[i+1]),Math.Abs(pixels[i]-pixels[i+rt.width])));}return max;
 }
 void SetTint(Material material,Color color) {
  foreach(var property in new[]{"_Color","_TintColor","_BaseColor"})if(material.HasColor(property))material.SetColor(property,color);
 }
 void CheckNightBrightness(MeshRenderer ground,Light sun) {
  var template=Resources.FindObjectsOfTypeAll<PostProcessingBehaviour>().FirstOrDefault(p=>p!=null&&p.profile!=null);
  Check(template!=null,"native post-processing profile available for HDR/bloom regression");
  var profile=Instantiate(template.profile);profile.bloom.enabled=true;profile.eyeAdaptation.enabled=false;profile.motionBlur.enabled=false;profile.depthOfField.enabled=false;profile.antialiasing.enabled=false;profile.grain.enabled=false;profile.dithering.enabled=false;
  var post=camera.gameObject.AddComponent<PostProcessingBehaviour>();post.profile=profile;
  report.Add("Night HDR native profile="+template.profile.name+" bloom intensity="+profile.bloom.settings.bloom.intensity+" threshold="+profile.bloom.settings.bloom.threshold+" colour grading="+profile.colorGrading.enabled);
  camera.allowHDR=true;rt.Release();Destroy(rt);rt=new RenderTexture(640,360,24,RenderTextureFormat.ARGBHalf);rt.Create();camera.targetTexture=rt;
  sun.intensity=.04f;sun.color=new Color(.3f,.45f,.8f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.015f,.025f,.045f);
  var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.layer=31;body.transform.position=origin+new Vector3(0,.65f,0);body.transform.localScale=new Vector3(1.7f,.8f,4.2f);body.GetComponent<Collider>().enabled=false;
  var templates=VehicleMaterials.Create(null,s=>{});body.GetComponent<Renderer>().sharedMaterial=templates[1];Destroy(templates[0]);
  car.transform.position=origin+Vector3.up*.2f;car.transform.rotation=Quaternion.identity;Renderer(baseline).enabled=false;
  var colors=new[]{new Color(.2f,1,.08f),new Color(.12f,.3f,1),Color.white};
  for(int index=0;index<colors.Length;index++) {
   neon.Color=colors[index];neon.Tick(false,0);var off=Capture("night-off-"+index+".png");
   neon.Tick(true,1f/60);var current=Capture("night-fixed-"+index+".png");
   // Previous installed brightness, with the same corrected projection,
   // ground, native fill lights and post-processing profile.
   var c=colors[index];SetTint(Renderer(neon).sharedMaterial,new Color(c.r,c.g,c.b,.8f));var bright=Capture("night-overbright-"+index+".png");
   double currentEnergy=TotalEnergy(current,off),brightEnergy=TotalEnergy(bright,off);
   report.Add("Night HDR/bloom colour="+index+": corrected energy="+currentEnergy+", overbright="+brightEnergy+"; clipped pixels="+Clipped(current)+" / "+Clipped(bright));
   Check(currentEnergy>brightEnergy*.08&&currentEnergy<brightEnergy*.65,"native night HDR/bloom retains visible neon with substantially lower output, colour="+index);
   Check(Clipped(current)<Clipped(bright)*.25+5,"night neon no longer forms a large saturated core, colour="+index);
  }
  neon.Color=new Color(.12f,1,.3f);neon.Tick(false,0);body.SetActive(false);Destroy(body);Destroy(templates[1]);post.enabled=false;Destroy(post);Destroy(profile);
  sun.intensity=.8f;sun.color=Color.white;
 }
 double TotalEnergy(Color32[] on,Color32[] off){double n=0;for(int i=0;i<on.Length;i++)n+=Math.Max(0,on[i].r-off[i].r)+Math.Max(0,on[i].g-off[i].g)+Math.Max(0,on[i].b-off[i].b);return n;}
 int Clipped(Color32[] pixels)=>pixels.Count(p=>p.r>=250||p.g>=250||p.b>=250);
 int BuriedSamples(object effect,Collider ground) {
  var mesh=Mesh(effect);var vertices=mesh.vertices;var indices=mesh.triangles;var t=Field<GameObject>(effect,"projection").transform;int buried=0;
  for(int i=0;i<indices.Length;i+=3) {
   var a=t.TransformPoint(vertices[indices[i]]);var b=t.TransformPoint(vertices[indices[i+1]]);var c=t.TransformPoint(vertices[indices[i+2]]);
   foreach(var p in new[]{(a+b+c)/3,(a+b)/2,(b+c)/2,(a+c)/2})if(ground.Raycast(new Ray(p+Vector3.up,Vector3.down),out var hit,2)&&hit.point.y>p.y+.002f)buried++;
  }
  return buried;
 }
 double Energy(Color32[] on,Color32[] off){double n=0;for(int i=0;i<on.Length;i++)n+=Math.Max(0,on[i].g-off[i].g);return n;}
 Color32[] Capture(string name,bool overhead=false) {
  camera.transform.position=car.transform.position+(overhead?Vector3.up*12:new Vector3(-4,2.7f,-6));
  if(overhead)camera.transform.rotation=Quaternion.Euler(90,0,0);else camera.transform.LookAt(car.transform.position);camera.Render();
  var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();RenderTexture.active=old;
  File.WriteAllBytes(Path.Combine(folder,name),t.EncodeToPNG());var pixels=t.GetPixels32();Destroy(t);return pixels;
 }
 // Restrict comparison to green neon: the native road material's distant
 // horizon/texture moves with the camera and is not part of this regression.
 int Difference(Color32[] a,Color32[] b){int n=0;for(int i=0;i<a.Length;i++)if((a[i].g>a[i].r+a[i].b+8||b[i].g>b[i].r+b[i].b+8)&&Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>15)n++;return n;}
}
