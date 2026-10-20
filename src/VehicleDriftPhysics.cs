using System;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> driftPhysicsEnabled;
    Func<int,TyreSmokeContact> driftSmokeContacts;
    bool DriftPhysicsActive=>current.driftPhysics!=null&&current.driftPhysics.Active;
    void InitializeDriftPhysics() {
        driftSmokeContacts=DriftSmokeContact;
        driftPhysicsEnabled=Config.Bind("DriftPhysics","Enabled",true,
            "Use NFVU2's own drift physics library for the 350Z drift mode.");
    }
    void ReleaseDriftPhysics(){current.driftPhysics?.Dispose();current.driftPhysics=null;}
    VehiclePhysicsTelemetry? DriftTelemetry() {
        if(!DriftPhysicsActive)return null;
        var physics=current.driftPhysics;
        var sample=new VehiclePhysicsTelemetry {EngineRpm=physics.Rpm,Gear=physics.Gear,RearWheelRpm=(Mathf.Abs(physics.WheelRpm(2))+Mathf.Abs(physics.WheelRpm(3)))*.5f};
        for(int i=0;i<4;i++)if(physics.Grounded(i)){sample.Contacts++;sample.Skid=Mathf.Max(sample.Skid,physics.SkidVolume(i));}
        int rearContacts=0;float forward=Vector3.Dot(body.linearVelocity,body.transform.forward);
        for(int i=2;i<4;i++)if(physics.Grounded(i)) {
            sample.TyreSlipSpeed+=Mathf.Abs(physics.WheelRpm(i)*(2*Mathf.PI/60)*wheels[i].radius-forward);rearContacts++;
        }
        if(rearContacts>0)sample.TyreSlipSpeed/=rearContacts;
        return sample;
    }
    TyreSmokeContact DriftSmokeContact(int index) {
        if(!DriftPhysicsActive)return new TyreSmokeContact();
        var physics=current.driftPhysics;
        if(!physics.TryGetWheelPose(index,out var hub,out var rotation))return new TyreSmokeContact();
        return new TyreSmokeContact {Grounded=physics.Grounded(index),Point=physics.ContactPoint(index),Hub=hub,Rpm=physics.WheelRpm(index),Sliding=physics.SkidVolume(index)>.1f};
    }
    bool StepDriftPhysics(bool handbrake) {
        bool eligible=driftPhysicsEnabled!=null&&driftPhysicsEnabled.Value&&EffectiveDrift&&!IsHummer&&!Flying&&!Transforming&&driving;
        if(!eligible){ReleaseDriftPhysics();return false;}
        if(current.driftPhysics==null) {
            if(current.driftPhysicsFailed||!DriverInputReady)return false;
            try {current.driftPhysics=DriftVehicleAdapter.Create(body,wheels,message=>Logger.LogInfo(message));Logger.LogInfo("350Z drift uses NFVU2.DriftPhysics, with its own drivetrain, tyres and steering.");}
            catch(Exception error){current.driftPhysicsFailed=true;Logger.LogWarning("NFVU2 drift initialization failed: "+error.Message);ShowVehicleWarning("Driftphysik nicht verfuegbar. Bisheriger Driftmodus aktiv; Details im Log.");return false;}
        }
        bool allowed=DriverInputReady&&!audioBlocked&&!menuOpen&&Application.isFocused;
        if(current.driftPhysics.Step(allowed,allowed?throttle:0,allowed?steer:0,allowed?brake:1,allowed&&burnoutHeld,allowed&&handbrake))return true;
        current.driftPhysicsFailed=true;ReleaseDriftPhysics();ShowVehicleWarning("Driftphysik nicht verfuegbar. Bisheriger Driftmodus aktiv; Details im Log.");return false;
    }
}
}
