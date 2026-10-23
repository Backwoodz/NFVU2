using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Rachel350Z;

public sealed partial class SmokeProbe {
 // Compare with the locally installed, accepted mantle implementation. The
 // runner records that DLL hash; the temporary plugin alone uses new sources.
 void CheckDetachedBaseline() {
  foreach(bool moving in new[]{false,true}) {
   car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;rpm=850;rightGround=true;Reset();
   var baseline=BaselineSmoke.Create(car,null,wheels,visuals,s=>report.Add(s));Check(baseline!=null,"installed smoke baseline initializes");
   var root=(GameObject)typeof(BaselineSmoke).GetField("root",F).GetValue(baseline);var previous=root.GetComponentInChildren<ParticleSystem>();
   var current=Systems.Single(p=>p!=previous);var beforeRenderer=previous.GetComponent<ParticleSystemRenderer>();var afterRenderer=current.GetComponent<ParticleSystemRenderer>();
   var advance=typeof(BaselineSmoke).GetMethod("Advance",F);Func<int,BaselineContact> provider=i=>{var c=Contact(i);return new BaselineContact{Grounded=c.Grounded,Hub=c.Hub,Point=c.Point,Normal=c.Normal,Rpm=c.Rpm,Load=c.Load};};
   var wind=new Vector3(.15f,0,.05f);
   try {
    beforeRenderer.enabled=afterRenderer.enabled=false;
    for(int frame=0;frame<480;frame++) {
     if(moving&&frame>=120) {
      float t=(frame-120)/60f;float angle=t*.35f;
      car.transform.SetPositionAndRotation(new Vector3(10*(1-Mathf.Cos(angle)),0,10*Mathf.Sin(angle)),Quaternion.Euler(0,angle*Mathf.Rad2Deg,0));
      body.linearVelocity=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*3.5f+car.transform.right*2;body.angularVelocity=Vector3.up*.35f;
     }
     effect.Advance(1f/60,true,true,Contact,wind);advance.Invoke(baseline,new object[]{1f/60,true,true,provider,wind});
     if(frame==119||frame==239||frame==479) {
      var now=Particles(0).Concat(Particles(1)).ToArray();var old=new ParticleSystem.Particle[previous.particleCount];previous.GetParticles(old);
      var wisps=now.Where(p=>p.startLifetime<1.1f).ToDictionary(p=>p.randomSeed);var oldWisps=old.Where(p=>p.startLifetime<1.1f).ToArray();
      Check(wisps.Count==oldWisps.Length,"free-cloud changes preserve the installed mantle particle count");
      foreach(var w in oldWisps) {
       Check(wisps.TryGetValue(w.randomSeed,out var same)&&Vector3.Distance(w.position,same.position)<.00001f&&Math.Abs(w.startSize-same.startSize)<.000001f&&w.startColor.Equals(same.startColor),
        "accepted mantle position, size, colour and seed stay unchanged by free-cloud changes");
      }
      Check(now.Length==old.Length,"denser free cloud preserves total live particle count");
     }
    }
    var eye=car.transform.position+new Vector3(-6,2.6f,-8);var target=car.transform.position+new Vector3(0,1,-2);
    string scene=moving?"moving":"stationary";
    afterRenderer.enabled=false;beforeRenderer.enabled=true;Capture("free-cloud-before-"+scene+".png",eye,target);
    afterRenderer.enabled=true;beforeRenderer.enabled=false;Capture("free-cloud-after-"+scene+".png",eye,target);
    afterRenderer.enabled=false;Capture("free-cloud-hidden-"+scene+".png",eye,target);
    CompareCloudDensity(scene);
   } finally {root.SetActive(false);baseline.Dispose();}
  }
  car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;Reset();for(int i=0;i<180;i++)Step();
 }
 void CompareCloudDensity(string scene) {
  Func<string,Color32[]> pixels=name=>{var t=new Texture2D(2,2);try{ImageConversion.LoadImage(t,File.ReadAllBytes(Path.Combine(folder,name)));return t.GetPixels32();}finally{Destroy(t);}};
  var a=pixels("free-cloud-before-"+scene+".png");var b=pixels("free-cloud-after-"+scene+".png");var empty=pixels("free-cloud-hidden-"+scene+".png");
  int coverageBefore=0,coverageAfter=0,denseBefore=0,denseAfter=0;double signalBefore=0,signalAfter=0;
  for(int i=0;i<a.Length;i++) {
   int da=Math.Abs(a[i].r-empty[i].r)+Math.Abs(a[i].g-empty[i].g)+Math.Abs(a[i].b-empty[i].b);
   int db=Math.Abs(b[i].r-empty[i].r)+Math.Abs(b[i].g-empty[i].g)+Math.Abs(b[i].b-empty[i].b);
   if(da>25)coverageBefore++;if(db>25)coverageAfter++;if(da>180)denseBefore++;if(db>180)denseAfter++;signalBefore+=da;signalAfter+=db;
  }
  report.Add("Free cloud image "+scene+": visible pixels "+coverageBefore+" -> "+coverageAfter+", dense pixels "+denseBefore+" -> "+denseAfter+", integrated smoke signal "+signalBefore+" -> "+signalAfter);
  Check(coverageAfter>6000&&denseAfter>500,"new free cloud renders a substantial visible dense plume: "+scene);
 }
}
