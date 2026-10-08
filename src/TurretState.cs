using System;

namespace Rachel350Z {
// NFVU2 firing rules, independent of Unity and of Rust's original combat logic.
public sealed class TurretState {
    public const int Capacity=30;
    public const float ShotInterval=60f/450,ReloadDuration=2.5f;
    public int Ammo {get;private set;}=Capacity;
    public float ReloadLeft {get;private set;}
    public float Recoil {get;private set;}
    public bool ReloadStarted {get;private set;}
    bool released;
    float cooldown;
    public bool Step(float dt,bool allowed,bool held) {
        ReloadStarted=false;
        dt=float.IsNaN(dt)||float.IsInfinity(dt)?0:Math.Max(0,Math.Min(dt,.1f));
        Recoil=Math.Max(0,Recoil-dt*9);
        if(!allowed){released=false;return false;}
        if(!held)released=true;
        cooldown=Math.Max(0,cooldown-dt);
        if(ReloadLeft>0) {
            ReloadLeft=Math.Max(0,ReloadLeft-dt);
            if(ReloadLeft==0)Ammo=Capacity;
            return false;
        }
        if(!released||!held||cooldown>0||dt==0)return false;
        Ammo--;cooldown=ShotInterval;Recoil=1;
        if(Ammo==0){ReloadLeft=ReloadDuration;ReloadStarted=true;}
        return true;
    }
}
public sealed class TurretRig {
    public ModelVertex yawPivot,pitchOffset,muzzle;
    public ModelVertex? laser;
    public void Validate() {
        foreach(var v in new[]{yawPivot,pitchOffset,muzzle})
            foreach(float n in new[]{v.x,v.y,v.z})
                if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>2)throw new System.IO.InvalidDataException("Invalid turret pivot");
        if(yawPivot.y<.1f||pitchOffset.y<.01f||muzzle.z<.05f)throw new System.IO.InvalidDataException("Incomplete turret rig");
        if(laser.HasValue)foreach(float n in new[]{laser.Value.x,laser.Value.y,laser.Value.z})
            if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>2)throw new System.IO.InvalidDataException("Invalid laser emitter");
    }
}
public static class TurretAimSettings {
    public const float ViewX=.5f,ViewY=.62f,CameraHeight=2.25f,CameraMinDistance=4.5f;
    public static float PivotHeight(float pitch)=>CameraHeight+Math.Max(0,pitch-45)*.055f;
}
public static class TurretBlastRules {
    public const float Radius=2.25f,CraterRadius=1.4f,CraterDepth=.18f;
    public static float Damage(float distance) {
        if(float.IsNaN(distance)||float.IsInfinity(distance)||distance<0||distance>=Radius)return 0;
        return 55*(1-distance/Radius);
    }
}
}
