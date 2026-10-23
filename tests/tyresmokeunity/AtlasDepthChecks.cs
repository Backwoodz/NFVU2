using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class SmokeProbe {
 // Frozen single-puff comparisons isolate texture animation/depth fading from
 // source cadence, flow, spawn/death and inter-renderer transparency sorting.
 void CheckAtlasDepth() {
  var source=Systems[0];var original=source.GetComponent<ParticleSystemRenderer>().sharedMaterial;
  var rows=new List<string>{"production shader="+original.shader.name+" queue="+original.renderQueue,
   "production keywords="+string.Join(",",original.shaderKeywords),"global soft particles="+Shader.IsKeywordEnabled("SOFTPARTICLES_ON")};
  foreach(var mat in new[]{Resources.FindObjectsOfTypeAll<Material>().First(m=>m.name=="slowwispysmoke"),original}) {
   rows.Add("Material "+mat.name+" keywords="+string.Join(",",mat.shaderKeywords));
   for(int n=0;n<mat.shader.GetPropertyCount();n++) {
    var key=mat.shader.GetPropertyName(n);var type=mat.shader.GetPropertyType(n);
    if(type==ShaderPropertyType.Float||type==ShaderPropertyType.Range)rows.Add(key+"="+mat.GetFloat(key));
    else if(type==ShaderPropertyType.Color)rows.Add(key+"="+mat.GetColor(key));
    else if(type==ShaderPropertyType.Vector)rows.Add(key+"="+mat.GetVector(key));
    else if(type==ShaderPropertyType.Texture) {var tex=mat.GetTexture(key);rows.Add(key+"="+(tex==null?"null":tex.name+" "+tex.width+"x"+tex.height));}
   }
  }
  var productionSheet=source.textureSheetAnimation;rows.Add("Production sheet="+productionSheet.numTilesX+"x"+productionSheet.numTilesY+" cycles="+productionSheet.cycleCount+" timeMode="+productionSheet.timeMode+" start="+productionSheet.startFrame.mode);
  File.WriteAllLines(Path.Combine(folder,"atlas-depth-materials.txt"),rows);
  Check(original.IsKeywordEnabled("_FLIPBOOK_BLENDING"),"native smoke flipbook blend shader keyword is enabled");
  Check(original.GetFloat("_EnableFlipbookBlending")>.5f,"native smoke material enables flipbook blending");
  var active=new List<ParticleSystemVertexStream>();source.GetComponent<ParticleSystemRenderer>().GetActiveVertexStreams(active);
  Check(active.Contains(ParticleSystemVertexStream.UV2)&&active.Contains(ParticleSystemVertexStream.AnimBlend),"native smoke sends both adjacent atlas UVs and interpolation weight");
  var hidden=Systems.Select(ps=>ps.GetComponent<ParticleSystemRenderer>()).ToArray();var was=hidden.Select(r=>r.enabled).ToArray();foreach(var r in hidden)r.enabled=false;
  var ps=new GameObject("Frozen atlas and depth diagnostic").AddComponent<ParticleSystem>();ps.gameObject.layer=31;
  ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
  var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
  var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=sheet.numTilesY=8;sheet.startFrame=new ParticleSystem.MinMaxCurve(0);sheet.cycleCount=1;
  var renderer=ps.GetComponent<ParticleSystemRenderer>();var material=new Material(original);renderer.sharedMaterial=material;renderer.maxParticleSize=.9f;renderer.shadowCastingMode=ShadowCastingMode.Off;
  renderer.SetActiveVertexStreams(active);ps.Play(false);ps.Pause(false);
  var testCamera=new GameObject("Atlas depth diagnostic camera").AddComponent<Camera>();testCamera.enabled=false;testCamera.cullingMask=1<<31;testCamera.clearFlags=CameraClearFlags.SolidColor;testCamera.backgroundColor=new Color(.25f,.34f,.42f);testCamera.fieldOfView=58;testCamera.farClipPlane=30;testCamera.depthTextureMode=DepthTextureMode.Depth;
  var target=new RenderTexture(640,360,24);testCamera.targetTexture=target;
  try {
   var puff=new ParticleSystem.Particle{position=new Vector3(20,3,0),startLifetime=10,remainingLifetime=5,startSize=2.5f,startColor=new Color(.92f,.92f,.92f,.6f),randomSeed=42,rotation=23};
   testCamera.transform.SetPositionAndRotation(new Vector3(20,3,-4.5f),Quaternion.identity);
   sheet.frameOverTime=new ParticleSystem.MinMaxCurve(.4f);ps.SetParticles(new[]{puff});
   var frozen=AtlasDepthPixels(testCamera,target,"atlas-frozen.png");
   var repeat=AtlasDepthPixels(testCamera,target,null);
   Check(AtlasDepthDifference(frozen,repeat)<.00001,"identical frozen smoke position, frame and camera renders identically");
   var mesh=new Mesh();renderer.BakeMesh(mesh,testCamera,true);var uv=new List<Vector4>();mesh.GetUVs(0,uv);var blend=new List<Vector4>();mesh.GetUVs(1,blend);
   rows=new List<string>{"Frozen baked UV0="+string.Join(";",uv),"Frozen baked UV1="+string.Join(";",blend)};
   // Real shader, constant geometry/color, samples immediately around a tile
   // boundary. Compare interpolation with deliberately disabled blending.
   foreach(bool blending in new[]{true,false}) {
    material.SetFloat("_EnableFlipbookBlending",blending?1:0);if(blending)material.EnableKeyword("_FLIPBOOK_BLENDING");else material.DisableKeyword("_FLIPBOOK_BLENDING");
    var samples=new List<Color32[]>();
    foreach(float frame in new[]{31.90f,31.99f,32.00f,32.01f,32.10f}) {
     sheet.frameOverTime=new ParticleSystem.MinMaxCurve(frame/64);ps.SetParticles(new[]{puff});samples.Add(AtlasDepthPixels(testCamera,target,blending&&frame==32?"atlas-boundary-blended.png":null));
    }
    rows.Add("Atlas "+(blending?"blended":"unblended")+" boundary mean RGB steps="+string.Join(",",Enumerable.Range(1,samples.Count-1).Select(i=>AtlasDepthDifference(samples[i-1],samples[i]).ToString("F6"))));
   }
   material.SetFloat("_EnableFlipbookBlending",1);material.EnableKeyword("_FLIPBOOK_BLENDING");
   sheet.frameOverTime=new ParticleSystem.MinMaxCurve(.4f);
   // A complete stationary flipbook cycle quantifies its authored texture
   // changes independently of physical smoke motion or actual frame time.
   Color32[] previous=null;float largest=0;int peak=0;
   for(int tile=0;tile<=64;tile++) {
    sheet.frameOverTime=new ParticleSystem.MinMaxCurve((tile%64)/64f);ps.SetParticles(new[]{puff});var pixels=AtlasDepthPixels(testCamera,target,null);
    if(previous!=null){float delta=AtlasDepthDifference(previous,pixels);if(delta>largest){largest=delta;peak=tile;}}previous=pixels;
   }
   rows.Add("Full stationary 64-tile blended cycle: largest mean RGB adjacent-frame change="+largest+" at tile="+peak);
   // The road top in the shared fixture is y=-.03. The camera and atlas frame
   // remain fixed while the billboard is moved by 5 mm per comparison.
   testCamera.transform.SetPositionAndRotation(new Vector3(20,1.4f,-4.5f),Quaternion.LookRotation(new Vector3(0,-1.1f,4.5f)));
   sheet.frameOverTime=new ParticleSystem.MinMaxCurve(.4f);renderer.enabled=false;var clear=AtlasDepthPixels(testCamera,target,null);renderer.enabled=true;
   foreach(float invFade in new[]{2.5f,1f,1000f}) {
    material.SetFloat("_InvFade",invFade);previous=null;float maximumStep=0;
    var coverage=new List<float>();
    for(int h=0;h<7;h++) {
     puff.position=new Vector3(20,.20f+h*.005f,0);ps.SetParticles(new[]{puff});var pixels=AtlasDepthPixels(testCamera,target,h==0?"depth-fade-"+invFade.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png":null);
     coverage.Add(AtlasDepthDifference(clear,pixels));if(previous!=null)maximumStep=Mathf.Max(maximumStep,AtlasDepthDifference(previous,pixels));previous=pixels;
    }
    rows.Add("Road fade InvFade="+invFade+" mean RGB signal at +0..30mm="+string.Join(",",coverage.Select(v=>v.ToString("F6")))+" maximum 5mm step="+maximumStep);
   }
   File.WriteAllLines(Path.Combine(folder,"atlas-depth-results.txt"),rows);foreach(var row in rows)report.Add(row);
   Destroy(mesh);
  } finally {
   for(int i=0;i<hidden.Length;i++)hidden[i].enabled=was[i];
   ps.gameObject.SetActive(false);Destroy(ps.gameObject);Destroy(material);target.Release();Destroy(target);Destroy(testCamera.gameObject);
  }
 }
 Color32[] AtlasDepthPixels(Camera cam,RenderTexture target,string filename) {
  cam.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
  image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();var pixels=image.GetPixels32();
  if(filename!=null)File.WriteAllBytes(Path.Combine(folder,filename),image.EncodeToPNG());Destroy(image);RenderTexture.active=previous;return pixels;
 }
 static float AtlasDepthDifference(Color32[] a,Color32[] b) {
  double total=0;for(int i=0;i<a.Length;i++)total+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);return (float)(total/(a.Length*3));
 }
}
