using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Rachel350Z {
public partial class Plugin {
    sealed class HordeMember {public Character Character;public ZDOID Id;public float Radius,MaxHealth;public bool Killed,HealthApplied;public string Prefab;public HordePact Pact;}
    readonly HordeWave hordeWave=new HordeWave();
    readonly List<HordeMember> horde=new List<HordeMember>();
    Player hordeOwner;
    ZNetScene hordeScene;
    ZDOMan hordeManager;
    HordeCorpses hordeCorpses;
    readonly Dictionary<string,GameObject> hordePrefabs=new Dictionary<string,GameObject>();
    Vector3 hordeOrigin,hordeForward;
    int hordeGroup=-1,hordeAlive;
    int hordeWaveKills,hordeRequiredAlive;
    HordeMember hordeBonusTarget,hordeBoss;
    bool hordeBossEnraged;
    readonly HordeBonus hordeBonus=new HordeBonus();

    void CheckHordeContext(MonoBehaviour local) {
        if(local==null)hordeResult=null;
        if(hordeOwner==null&&!hordeWave.Active&&horde.Count==0)return;
        if(local!=hordeOwner||hordeOwner==null||hordeOwner.IsDead()||hordeOwner.IsTeleporting()||!HordeGameUsable
            ||hordeScene!=ZNetScene.instance||hordeManager!=ZDOMan.instance||Multiplayer())ClearHorde();
    }
    void TickHorde(MonoBehaviour local) {
        if(DriftSessionRules.Active){if(hordeWave.Active||horde.Count>0)ClearHorde();return;}
        try {
            bool clear=Input.GetKeyDown(KeyCode.Keypad0)||(Keyboard.current?.numpad0Key.wasPressedThisFrame??false);
            bool start=Input.GetKeyDown(KeyCode.Keypad9)||(Keyboard.current?.numpad9Key.wasPressedThisFrame??false);
            if(hordeResultLeft>0)hordeResultLeft=Mathf.Max(0,hordeResultLeft-Time.deltaTime);
            if(clear){ClearHorde();hordeResult=null;ShowVehicleWarning("Die Horde wurde entfernt.");return;}
            if(start)StartHorde(local as Player);
            if(!hordeWave.Active)return;
            hordeCorpses?.Tick(Time.deltaTime);
            TickHordeProjectiles();
            if(hordeRun.Dead){FinishHordeGame(false);return;}
            if(hordeWave.Choosing){hordeVisuals?.Tick(Time.deltaTime,hordeRun.Shield/HordeRun.MaxShield,0);return;}
            hordeBonus.Tick(Time.deltaTime);
            TickHordeGame();
            hordeAlive=CountHordeAlive();
            if(hordeBoss?.Character!=null&&!hordeBoss.Killed&&!hordeBossEnraged&&hordeBoss.Character.GetHealth()<=hordeBoss.MaxHealth*.5f) {
                hordeBossEnraged=true;ConfigureDragonFlight(hordeBoss.Character);
                ShowVehicleWarning("Moder gerät in Zorn! Ihre Angriffe werden stärker.");
            }
            bool pending=hordeWave.Pending;
            int wave=hordeWave.Number;
            hordeWave.Tick(Time.deltaTime,hordeAlive,SpawnHordeMember,hordeWaveKills,hordeRequiredAlive);
            if(hordeWave.Number!=wave){RemoveHordeMembers();EndHordeWave();hordeGroup=-1;hordeWaveKills=0;hordeRequiredAlive=0;hordePactSelection=0;}
            else if(pending&&!hordeWave.Pending) {
                Logger.LogInfo("Forest horde wave "+wave+": "+hordeWave.Spawned+" / "+hordeWave.Size+" enemies; "+hordeWave.Attempts+" placement attempts.");
                if(hordeWave.NoGround){ClearHorde();ShowVehicleWarning("Kein freier Boden für die Horde. Suche eine offene Fläche.");}
                else if(hordeWave.Spawned<hordeWave.Size){hordeCompleteRoster=false;ShowVehicleWarning("Zu wenig freier Boden. Eine kleinere Horde greift an.");}
            }
            if(hordeWave.Completed)FinishHordeGame(true);
        }catch(Exception error){Logger.LogWarning("Test horde stopped: "+error);ClearHorde();ShowVehicleWarning("Horde konnte nicht erstellt werden. Siehe BepInEx-Log.");}
    }
    void StartHorde(Player local) {
        if(DriftSessionRules.Active){ShowVehicleWarning("Driftwelt: Hordenmodus ist deaktiviert.");return;}
        if(local==null||local.IsDead()||local.IsTeleporting())return;
        if(Multiplayer()){ShowVehicleWarning("Nur in einer lokalen Einzelspieler-Testwelt verwenden.");return;}
        if(hordeWave.Active||CountHordeAlive()>0){ShowVehicleWarning("Eine Horde ist noch aktiv. Numpad 0 entfernt sie.");return;}
        if(!driving||IsHummer||Flying||!DriverInputReady||turret==null||!turret.ExplosiveAvailable){ShowVehicleWarning("Hordenmodus: im 350Z mit geladenem Turret auf dem Boden starten.");return;}
        ClearHorde();
        var scene=ZNetScene.instance;
        if(scene==null||ZoneSystem.instance==null||ZDOMan.instance==null)return;
        foreach(var name in HordeWave.Prefabs) {
            var prefab=scene.GetPrefab(name);
            if(prefab==null||prefab.GetComponent<Character>()==null||prefab.GetComponent<MonsterAI>()==null||prefab.GetComponent<CapsuleCollider>()==null||prefab.GetComponent<CharacterDrop>()==null)
                throw new InvalidOperationException("Horde prefab unavailable: "+name);
            hordePrefabs.Add(name,prefab);
        }
        hordeOwner=local;hordeScene=scene;hordeManager=ZDOMan.instance;
        hordeCorpses=new HordeCorpses(hordeScene,hordeManager);
        BeginHordeGame();
        hordeWaveKills=0;hordePactSelection=0;
        hordeWave.Begin(0);
    }
    int CountHordeAlive() {
        int alive=0;hordeRequiredAlive=0;
        foreach(var member in horde) {
            if(member.Character!=null&&member.Character.IsDead())RegisterHordeKill(member);
            if(hordeManager?.GetZDO(member.Id)==null){if(!member.Killed){hordeCompleteRoster=false;if(HordeWave.Required(member.Prefab))throw new InvalidOperationException("Required horde enemy disappeared: "+member.Prefab);}continue;}
            if(member.Character==null) {
                member.Character=hordeScene.FindInstance(member.Id)?.GetComponent<Character>();
                if(member.Character!=null)ConfigureHordeMember(member);
            }
            if(!member.Killed&&(member.Character==null||!member.Character.IsDead())){alive++;if(HordeWave.Required(member.Prefab))hordeRequiredAlive++;}
        }
        return alive;
    }
    void RegisterHordeKill(HordeMember member) {
        if(member.Killed)return;
        member.Killed=true;hordeWaveKills++;hordeRun.Kill(member.Prefab);
        if(member==hordeBonusTarget&&hordeBonus.Claim()){hordeRun.Bonus();ShowVehicleWarning("Jagdauftrag erfüllt: +500 Punkte und +250 HP!");}
    }
    void ConfigureHordeMember(HordeMember member) {
        var enemy=member.Character.gameObject;
        if(enemy.GetComponent<HordeEnemyMarker>()!=null)return;
        // Only instances from this encounter ally with one another; native prefabs stay untouched.
        var character=member.Character;character.m_faction=Character.Faction.ForestMonsters;
        // This encounter must not unlock boss progression or change the world's active-boss counter.
        if(member.Prefab=="Dragon") {
            character.m_boss=false;character.m_bossEvent="";character.m_defeatSetGlobalKey="";character.m_dreamCinematic="";
            ConfigureDragonFlight(character);
        }
        float speed=HordePacts.Speed(member.Pact);
        character.m_speed*=speed;character.m_runSpeed*=speed;character.m_walkSpeed*=speed;character.m_swimSpeed*=speed;
        character.m_flySlowSpeed*=speed;character.m_flyFastSpeed*=speed;
        if(!member.HealthApplied) {
            float health=character.GetMaxHealth()*HordePacts.Health(member.Pact);
            character.SetMaxHealth(health);character.SetHealth(health);member.MaxHealth=health;member.HealthApplied=true;
        } else if(character.GetMaxHealth()!=member.MaxHealth) {
            // Native Awake can reset a full-health creature's maximum when its zone reloads.
            float fraction=character.GetHealth()/character.GetMaxHealth();
            character.SetMaxHealth(member.MaxHealth);character.SetHealth(member.MaxHealth*fraction);
        }
        member.Character.m_onDeath+=()=>RegisterHordeKill(member);
        var drops=enemy.GetComponent<CharacterDrop>();
        if(drops!=null){drops.SetDropsEnabled(false);drops.m_drops=new List<CharacterDrop.Drop>();}
        var marker=enemy.AddComponent<HordeEnemyMarker>();marker.OnCorpse=hordeCorpses.Add;marker.OnProjectile=RegisterHordeProjectile;
        enemy.AddComponent<HordeElementStatus>().Boss=member.Prefab=="Dragon";
    }
    void ConfigureDragonFlight(Character character) {
        var ai=character.GetComponent<MonsterAI>();
        ai.m_flyAbsMinAltitude=0;ai.m_flyAltitudeMin=8;ai.m_flyAltitudeMax=14;
        ai.m_groundDuration=hordeBossEnraged?12:25;ai.m_airDuration=20;
        ai.m_deathMessage="";
    }
    bool SpawnHordeMember(string prefabName) {
        var prefab=hordePrefabs[prefabName];bool boss=prefabName=="Dragon",flying=boss||prefabName=="Hatchling";
        // Re-anchor each ten-enemy group; move an unfinished group if the car has passed it.
        int group=hordeWave.Resolved/10;
        if(group!=hordeGroup||Vector3.Distance(hordeOwner.transform.position,hordeOrigin)>20) {
            hordeGroup=group;
            var anchor=driving&&car!=null?car.transform:hordeOwner.transform;
            hordeOrigin=anchor.position;hordeForward=Vector3.ProjectOnPlane(anchor.forward,Vector3.up).normalized;
            if(hordeForward.sqrMagnitude<.5f)hordeForward=Vector3.forward;
        }
        float angle=boss?UnityEngine.Random.Range(-25f,25f):(group%3-1)*50+UnityEngine.Random.Range(-16f,16f);
        var direction=Quaternion.Euler(0,angle,0)*hordeForward;
        var point=hordeOrigin+direction*UnityEngine.Random.Range(boss?45f:30f,boss?55f:45f);
        ZoneSystem.instance.GetGroundData(ref point,out var normal,out var biome,out var area,out var heightmap);
        if(heightmap==null||!flying&&normal.y<.85f||Mathf.Abs(point.y-hordeOrigin.y)>12)return false;
        if(point.y<Floating.GetLiquidLevel(point)+.3f||ZoneSystem.instance.IsLava(point))return false;
        // Keep later spawns away from the moving car/player, and out of solids.
        if(Vector3.Distance(point,hordeOwner.transform.position)<(boss?35:18))return false;
        var capsule=prefab.GetComponent<CapsuleCollider>();var scale=prefab.transform.localScale;
        float radius=Mathf.Max(.5f,capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z))+.1f);
        float height=Mathf.Max(radius*2,capsule.height*Mathf.Abs(scale.y));
        point+=Vector3.up*(boss?10:flying?6:.2f);
        if(Physics.CheckCapsule(point+Vector3.up*radius,point+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore))return false;
        foreach(var member in horde)if(member.Character!=null&&Vector3.Distance(point,member.Character.transform.position)<radius+member.Radius+1)return false;
        var enemy=UnityEngine.Object.Instantiate(prefab,point,Quaternion.LookRotation(-direction));
        // Track immediately, so a failed setup can remove its native network object too.
        var data=enemy.GetComponent<ZNetView>()?.GetZDO();
        var record=new HordeMember{Character=enemy.GetComponent<Character>(),Id=data!=null?data.m_uid:ZDOID.None,Radius=radius,Prefab=prefabName,Pact=hordeRun.Pact};
        horde.Add(record);
        if(record.Character==null||data==null){hordeScene.Destroy(enemy);throw new InvalidOperationException(prefabName+" has no Character/ZDO.");}
        data.Persistent=false;
        data.Set(ZDOVars.s_cheated,true);
        record.Character.SetLevel(1);
        ConfigureHordeMember(record);
        var ai=enemy.GetComponent<MonsterAI>();
        ai.SetEventCreature(false);ai.SetDespawnInDay(false);ai.ResetPatrolPoint();
        ai.SetHuntPlayer(true);ai.Alert();
        if(boss)hordeBoss=record;
        if(hordeWave.Number>=2&&hordeWave.Number<=4&&hordeBonusTarget==null&&HordeWave.Required(prefabName)){hordeBonusTarget=record;hordeBonus.Begin();}
        hordeAlive++;
        if(hordeWave.Spawned==0)ShowVehicleWarning(hordeWave.Announcement);
        return true;
    }
    void ClearHorde() {
        hordeRadar?.Dispose();hordeRadar=null;hordeRadarPositions.Clear();
        hordeWave.Cancel();
        EndHordeWave();
        ReleaseHordeGame();
        RemoveHordeMembers();
        hordeCorpses?.Clear();hordeCorpses=null;
        hordeOwner=null;hordeScene=null;hordeManager=null;hordePrefabs.Clear();hordeGroup=-1;
    }
    void RemoveHordeMembers() {
        // Never apply old world IDs to a new world's object manager.
        if(hordeManager!=null&&hordeManager==ZDOMan.instance&&hordeScene!=null&&hordeScene==ZNetScene.instance)
            foreach(var member in horde) {
                var instance=hordeScene.FindInstance(member.Id);
                var data=hordeManager.GetZDO(member.Id);
                if(data!=null)data.SetOwner(ZDOMan.GetSessionID());
                if(instance!=null)hordeScene.Destroy(instance);
                else if(data!=null)hordeManager.DestroyZDO(data);
            }
        horde.Clear();hordeAlive=0;
    }
}
}
