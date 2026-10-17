using System;
namespace Rachel350Z {
// Per-vehicle ammunition and rate limiting; independent of input devices,
// camera/render frequency and Rust's original ammunition/combat simulation.
public sealed class TransformerMinigunModel {
    public const int Capacity=1000;
    public const float RoundsPerMinute=600,ShotInterval=60f/RoundsPerMinute;
    public const float WindupSeconds=1.5f,SpindownSeconds=.4f,ReloadDuration=3.5f;
    public const int MaximumShotsPerFrame=2;
    public int Ammo {get;private set;}=Capacity;
    public float ReloadLeft {get;private set;}
    public float Spin {get;private set;}
    public bool ReloadStarted {get;private set;}
    public bool Firing {get;private set;}
    public int Shots {get;private set;}
    bool released;
    float cooldown;
    public int Step(float dt,bool allowed,bool held,bool reload=false) {
        Shots=0;ReloadStarted=Firing=false;
        if(!Finite(dt)||dt<=0||!allowed){Stop();return 0;}
        dt=Math.Min(dt,.1f);
        if(!held)released=true;
        if(reload&&Ammo<Capacity&&ReloadLeft<=0)BeginReload();
        if(ReloadLeft>0) {
            Spin=0;cooldown=0;
            ReloadLeft=Math.Max(0,ReloadLeft-dt);
            if(ReloadLeft==0)Ammo=Capacity;
            return 0;
        }
        bool trigger=released&&held&&Ammo>0;
        float previous=Spin;
        Spin=Math.Max(0,Math.Min(1,Spin+dt*(trigger?1/WindupSeconds:-1/SpindownSeconds)));
        if(!trigger){cooldown=0;return 0;}
        if(Spin<1)return 0;
        // Only the portion AFTER spin-up can accrue shots. A long game frame
        // cannot manufacture a backlog of rounds that were never simulated.
        float firingTime=previous<1?Math.Max(0,dt-(1-previous)*WindupSeconds):dt;
        cooldown-=firingTime;
        while(cooldown<=.000001f&&Shots<MaximumShotsPerFrame&&Ammo>0) {
            Ammo--;Shots++;cooldown+=ShotInterval;
        }
        cooldown=Math.Max(0,cooldown);
        Firing=Ammo>0;
        if(Ammo==0)BeginReload();
        return Shots;
    }
    void BeginReload(){ReloadLeft=ReloadDuration;ReloadStarted=true;Firing=false;Spin=0;cooldown=0;}
    // Aborting aim, pausing, dismounting or returning does not refill a magazine
    // or complete a reload. Releasing the trigger is required after any abort.
    public void Stop(){Spin=cooldown=0;released=Firing=false;Shots=0;}
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
public static class TransformerMinigunCombatRules {
    public const float Range=150;
    // Twelve ordinary unmodified pierce hits on a 40HP, normal-pierce Greydwarf.
    // Native resistance, difficulty modifiers and pre-existing stagger still apply.
    public const float BulletDamage=3.34f;
}
}
