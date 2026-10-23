using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rachel350Z;

public sealed partial class SmokeProbe {
 void CheckContactPatchSmoke() {
  foreach(int hz in new[]{30,60,144}) {
   car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;
   rpm=850;rightGround=true;Reset();for(int i=0;i<hz*3;i++)Step(1f/hz);
   var plumes=(Array)typeof(VehicleBurnoutEffects).GetField("plumes",F).GetValue(effect);
   var freshContact=new List<ParticleSystem.Particle>();float envelope=0;
   for(int wheel=0;wheel<2;wheel++) {
    var native=Particles(wheel);var plume=plumes.GetValue(wheel);var puffs=(Array)plume.GetType().GetField("Puffs",F).GetValue(plume);
    int contacts=0,clouds=0,visibleContact=0,mantle=0;float maximumDistance=0;
    for(int i=0;i<native.Length;i++) {
     var p=native[i];var puff=puffs.GetValue(i);var type=puff.GetType();
     bool contact=(bool)type.GetField("Contact",F).GetValue(puff),wisp=(bool)type.GetField("Wisp",F).GetValue(puff);
     float age=p.startLifetime-p.remainingLifetime,orbit=(float)type.GetField("Orbit",F).GetValue(puff);
     if(!wisp)clouds++;
     if(contact) {
      contacts++;Check(orbit==0,"direct contact smoke leaves the footprint without orbiting first");
      if(age>.001f&&age<.14f) {
       var ground=(Vector3)type.GetField("Ground",F).GetValue(puff);var offset=p.position-ground;
       float distance=Vector3.ProjectOnPlane(offset,Vector3.up).magnitude;maximumDistance=Mathf.Max(maximumDistance,distance);
       Check(distance<.22f&&offset.y>.02f&&offset.y<.16f,"fresh native contact smoke remains compact and above the road at "+hz+" Hz");
       if(p.startColor.a>8)visibleContact++;
       freshContact.Add(p);
      }
     }
     if(wisp&&age>.001f&&age<=orbit) {
      float width=Mathf.Abs(Vector3.Dot(p.position-visuals[wheel+2].position,car.transform.right));
      float extent=width+p.startSize*.5f;envelope=Mathf.Max(envelope,extent);mantle++;
      Check(width<=.174f&&extent<.29f,"native mantle stays close to the tyre in an overhead view");
     }
    }
    Check(contacts>20&&contacts>clouds*.25f&&contacts<clouds*.4f,"direct contact source shares the existing cloud budget, wheel="+wheel);
    Check(visibleContact>=2,"multiple visible fresh puffs originate at each contact patch at "+hz+" Hz");
    Check(mantle>20,"tighter mantle still has a continuous populated wheel wrap");
    report.Add("Contact patch "+hz+" Hz, wheel="+wheel+": contact clouds="+contacts+" / "+clouds+", visible fresh="+visibleContact+", maximum fresh footprint distance="+maximumDistance+" m");
   }
   report.Add("Tight mantle "+hz+" Hz: maximum attached centre plus half billboard width="+envelope+" m");
   if(hz==144) {
    var all=Particles(0).Concat(Particles(1)).ToArray();var system=Systems[0];
    CaptureWisps("tight-mantle-overhead.png",new Vector3(-.8f,5,-3),new Vector3(0,.2f,-1.2f));
    Capture("contact-and-mantle-overhead.png",new Vector3(-.8f,5,-3),new Vector3(0,.2f,-1.2f));
    system.SetParticles(freshContact.ToArray());Capture("contact-patch-only.png",new Vector3(-3,.7f,-3.8f),new Vector3(0,.15f,-1.2f));
    system.SetParticles(new ParticleSystem.Particle[0]);Capture("contact-patch-hidden.png",new Vector3(-3,.7f,-3.8f),new Vector3(0,.15f,-1.2f));
    Check(PixelDifference("contact-patch-only.png","contact-patch-hidden.png")>50,"native camera renders the isolated direct road-contact source visibly");
    system.SetParticles(all);
   }
  }
  car.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.linearVelocity=body.angularVelocity=Vector3.zero;rpm=850;Reset();for(int i=0;i<180;i++)Step();
 }
}
