using System;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    readonly HordeRun hordeRun=new HordeRun();
    readonly HordeHitGate hordeHits=new HordeHitGate();
    VehicleRuntime hordeVehicle;
    VehicleHordeHull hordeHull;
    HordeArenaVisuals hordeVisuals;
    HordeWeather hordeWeather;
    HordeElementCombat hordeElements;
    ConfigEntry<int> hordeBestScore;
    float hordeHitFlash,pickupSpawnDelay;
    int pickupSequence;
    Vector3 lastHordePosition;
    bool hordeCompleteRoster;
    string hordeResult;
    float hordeResultLeft;
    bool HordeVehicle=>hordeRun.Active&&current==hordeVehicle;
    string HordeAmmoName=>hordeRun.Pact==HordePact.Frost?"Eismunition":hordeRun.Pact==HordePact.Storm?"Blitzmunition":hordeRun.Explosive?"Explosiv":"Normal";
    Color HordeAmmoTint=>hordeRun.Pact==HordePact.Frost?new Color(.6f,.9f,1):hordeRun.Pact==HordePact.Storm?new Color(.65f,.65f,1):new Color(1,.72f,.22f);
    void OnHordeTurretHit(RaycastHit hit,Character driver)=>hordeElements?.Hit(hit,driver,hordeRun.Pact);
    bool EffectiveTerrain=>!DriftSessionRules.Active&&(HordeVehicle||terrainMode.Value);
    bool EffectiveDrift=>!HordeVehicle&&(DriftSessionRules.Active||driftMode.Value);
    bool HordeGameUsable=>hordeRun.Active&&hordeVehicle!=null&&hordeVehicle.car!=null&&hordeVehicle.driving&&hordeOwner!=null;
    void InitializeHordeGame() {
        hordeBestScore=Config.Bind("Horde","BestScoreV3",0,"Best score from the elemental-pact waves and dragon finale (v3 rules).");
        controlsHarmony.PatchAll(typeof(HordeDriverDamage));
        controlsHarmony.PatchAll(typeof(HordeRagdollSetup));
        controlsHarmony.PatchAll(typeof(HordeFrozenAI));
        controlsHarmony.PatchAll(typeof(HordeProjectileSetup));
    }
    void BeginHordeGame() {
        hordeVehicle=current;hordeRun.Begin();hordeCompleteRoster=true;hordeHits.Clear();
        hordeResult=null;hordeResultLeft=0;hordeHitFlash=0;pickupSpawnDelay=0;pickupSequence=0;
        lastHordePosition=car.transform.position;
        hordeHull=car.AddComponent<VehicleHordeHull>();hordeHull.OnHit=ReceiveHordeHit;
        hordeHull.OnCrash=speed=>{if(HordeDamageAllowed())ApplyHordeDamage(HordeRun.CrashDamage(speed));};
        hordeVisuals=new HordeArenaVisuals(car.transform);
        hordeElements=new HordeElementCombat();
        if(EnvMan.instance!=null) {
            if(hordeWeather!=null&&!hordeWeather.IsCurrent){hordeWeather.Dispose();hordeWeather=null;}
            if(hordeWeather==null)hordeWeather=new HordeWeather(EnvMan.instance,ZDOMan.instance);
            hordeWeather.Begin();
        }
        turret.State.Refill();ApplyDriveMode();
    }
    bool HordeDamageAllowed()=>HordeGameUsable&&!hordeRun.Dead&&!hordeWave.Choosing&&!audioBlocked&&!menuOpen&&Application.isFocused&&Time.timeScale>0;
    internal bool RouteHordeDriverDamage(Character target,HitData hit) {
        if(!HordeGameUsable||target!=hordeOwner)return false;
        ReceiveHordeHit(hit);return true;
    }
    void ReceiveHordeHit(HitData hit) {
        if(hit==null||!HordeDamageAllowed())return;
        var attacker=hit.GetAttacker();
        if(attacker==null||attacker.IsPlayer()||attacker.IsTamed()||!BaseAI.IsEnemy(attacker,hordeOwner))return;
        // Tree/mining-only damage on a troll weapon is not extra vehicle damage.
        float amount=hit.GetTotalDamage()-hit.m_damage.m_chop-hit.m_damage.m_pickaxe-hit.m_damage.m_nonPlayer;
        if(amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return;
        bool boss=attacker==hordeBoss?.Character;
        if(hordeHits.Accept(attacker.GetInstanceID(),Time.time,boss?HordeIncomingDamage.BossHitInterval:.3f))
            ApplyHordeDamage(HordeIncomingDamage.Attack(amount,hordeRun.Pact,boss,hordeBossEnraged));
    }
    void ApplyHordeDamage(float amount){float shield=hordeRun.Shield;if(hordeRun.Damage(amount)&&shield>0)hordeHitFlash=.35f;}
    void TickHordeGame() {
        if(!HordeGameUsable)return;
        hordeRun.Tick(Time.deltaTime);
        hordeElements?.Tick(Time.deltaTime);
        foreach(var member in horde)if(member.Character!=null)member.Character.GetComponent<HordeElementStatus>()?.Tick(Time.deltaTime);
        hordeHitFlash=Mathf.Max(0,hordeHitFlash-Time.deltaTime);
        var position=hordeVehicle.car.transform.position;
        // Segment pickup detection also catches driving through a rune between frames.
        for(int i=hordeVisuals.Pickups.Count-1;i>=0;i--) {
            var pickup=hordeVisuals.Pickups[i];
            var segment=position-lastHordePosition;float length=segment.sqrMagnitude;
            float t=length>.001f?Mathf.Clamp01(Vector3.Dot(pickup.Point-lastHordePosition,segment)/length):0;
            var closest=lastHordePosition+segment*t;var delta=pickup.Point-closest;
            if(Mathf.Abs(delta.y)<3&&new Vector2(delta.x,delta.z).magnitude<2.6f&&hordeRun.Collect(pickup.Kind)) {
                if(pickup.Kind==HordePowerup.Explosive)hordeVehicle.turret.State.Refill();
                if(pickup.Kind==HordePowerup.Shield)hordeHitFlash=.6f;
                ShowVehicleWarning(PowerupMessage(pickup.Kind));hordeVisuals.Remove(pickup);continue;
            }
            if(pickup.Age>45||Vector3.Distance(pickup.Point,position)>120)hordeVisuals.Remove(pickup);
        }
        lastHordePosition=position;
        pickupSpawnDelay-=Time.deltaTime;
        if(hordeVisuals.Pickups.Count<4&&pickupSpawnDelay<=0) {
            bool placed=false;
            for(int attempt=0;attempt<3&&!placed;attempt++)placed=TryPlacePowerup(position);
            pickupSpawnDelay=placed?3:1;
        }
        hordeVisuals.Tick(Time.deltaTime,hordeRun.Shield/HordeRun.MaxShield,hordeHitFlash);
    }
    bool TryPlacePowerup(Vector3 position) {
        var heading=hordeVehicle.car.transform.forward;heading.y=0;heading.Normalize();
        var point=position+Quaternion.Euler(0,UnityEngine.Random.Range(-75f,75f),0)*heading*UnityEngine.Random.Range(22f,42f);
        ZoneSystem.instance.GetGroundData(ref point,out var normal,out var biome,out var area,out var ground);
        if(ground==null||normal.y<.9f||Mathf.Abs(point.y-position.y)>6||point.y<Floating.GetLiquidLevel(point)+.4f||ZoneSystem.instance.IsLava(point))return false;
        if(Physics.CheckCapsule(point+Vector3.up*1.2f,point+Vector3.up*2.2f,.9f,~0,QueryTriggerInteraction.Ignore))return false;
        foreach(var other in hordeVisuals.Pickups)if(Vector3.Distance(other.Point,point)<9)return false;
        var kind=(HordePowerup)(pickupSequence++%4);
        if(kind==HordePowerup.Explosive&&(hordeRun.Pact==HordePact.Thor||HordePacts.Elemental(hordeRun.Pact)))kind=HordePowerup.Shield;
        if(kind==HordePowerup.Automatic&&hordeRun.Pact==HordePact.Valkyrie)kind=HordePowerup.Repair;
        hordeVisuals.Add(point+Vector3.up*.1f,kind);return true;
    }
    static string PowerupMessage(HordePowerup kind)=>kind==HordePowerup.Explosive?"Explosivmunition: 30 Schuss!":kind==HordePowerup.Automatic?"Autoturret: 15 Sekunden!":kind==HordePowerup.Shield?"Schild aufgeladen: +150":"Reparatur: +250 HP";
    static string PowerupName(HordePowerup kind)=>kind==HordePowerup.Explosive?"Explosivmunition":kind==HordePowerup.Automatic?"Autoturret":kind==HordePowerup.Shield?"Schild": "Reparatur";
    void FinishHordeGame(bool victory) {
        bool ranked=victory&&hordeCompleteRoster;
        string medal=hordeRun.Medal(ranked);
        int score=hordeRun.Finish(ranked);
        bool best=ranked&&score>hordeBestScore.Value;
        if(best)hordeBestScore.Value=score;
        hordeResult=(ranked?"HORDE BESIEGT!":victory?"Training abgeschlossen":"FAHRZEUG KAMPFUNFÄHIG")+"\n"+
            Tr(medal)+"  ·  "+score+" "+Tr("Punkte")+"\n"+hordeRun.Kills+" "+Tr("Abschüsse")+"  ·  "+Mathf.FloorToInt(hordeRun.Seconds/60)+":"+((int)hordeRun.Seconds%60).ToString("00")+
            "  ·  "+Tr("Beste Combo")+" "+hordeRun.BestCombo+"\n"+(best?Tr("NEUER REKORD!"):Tr("Bestwert")+": "+hordeBestScore.Value)+"\n"+Tr("Numpad 9: erneut starten · Numpad 0: schließen");
        hordeResultLeft=25;
        if(hordeVehicle?.body!=null){hordeVehicle.body.linearVelocity=Vector3.zero;hordeVehicle.body.angularVelocity=Vector3.zero;}
        ClearHorde();
        ShowVehicleWarning(ranked?"Der Wald verstummt. Alle fünf Wellen besiegt!":victory?"Training abgeschlossen – Gegner fehlten. Keine Rekordwertung.":"Fahrzeug kampfunfähig. Numpad 9 für einen neuen Versuch.");
    }
    void ReleaseHordeGame() {
        hordeWeather?.End();
        hordeRun.Finish(false);hordeHits.Clear();
        if(hordeHull!=null){hordeHull.OnHit=null;hordeHull.OnCrash=null;Destroy(hordeHull);hordeHull=null;}
        hordeVisuals?.Dispose();hordeVisuals=null;
        hordeElements?.Dispose();hordeElements=null;
        if(hordeVehicle!=null&&hordeVehicle.car!=null) {
            var selected=current;
            try {current=hordeVehicle;ApplyDriveMode();if(turret!=null){turret.State.ReloadMultiplier=1;turret.State.Refill();}if(vehicleImpact!=null)vehicleImpact.EnemyDamageMultiplier=1;}
            finally {current=selected;}
        }
        hordeVehicle=null;
    }
    void TickHordeWeather(MonoBehaviour local) {
        if(hordeWeather==null)return;
        if(local==null||Multiplayer()||!hordeWeather.IsCurrent){hordeWeather.Dispose();hordeWeather=null;return;}
        // Continue the clear-day transition after the encounter and car are gone.
        hordeWeather.Tick(Time.deltaTime);
    }
}
}
