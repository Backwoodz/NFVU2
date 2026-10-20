using System;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    DriftSessionEnvironment driftEnvironment;
    DriftSessionCleanup driftCleanup;
    float driftCleanupAt;
    void InitializeDriftSession() {
        foreach(var patch in new[]{typeof(DriftRandomSpawns),typeof(DriftCreatureSpawns),typeof(DriftAreaSpawns),typeof(DriftRandomRaids)})
            controlsHarmony.PatchAll(patch);
    }
    void TickDriftSession(MonoBehaviour local) {
        if(local==null||!DriftSessionRules.Active||Multiplayer()||Game.instance==null||Game.instance.IsShuttingDown()) {
            ClearDriftSession();return;
        }
        if(driftEnvironment!=null&&!driftEnvironment.IsCurrent){driftEnvironment.Dispose();driftEnvironment=null;}
        if(EnvMan.instance!=null&&driftEnvironment==null) {
            // End any earlier horde override before capturing the normal environment.
            if(hordeWave.Active||horde.Count>0)ClearHorde();
            hordeWeather?.Dispose();hordeWeather=null;
            driftEnvironment=new DriftSessionEnvironment(EnvMan.instance,ZDOMan.instance);
        }
        driftEnvironment?.Tick();
        if(RandEventSystem.instance!=null&&RandEventSystem.instance.GetCurrentRandomEvent()!=null)
            RandEventSystem.instance.ResetRandomEvent();
        if(Time.unscaledTime>=driftCleanupAt) {
            driftCleanupAt=Time.unscaledTime+.5f;
            if(driftCleanup==null)driftCleanup=new DriftSessionCleanup();
            driftCleanup.Tick();
        }
    }
    void ClearDriftSession() {
        driftEnvironment?.Dispose();driftEnvironment=null;driftCleanup=null;driftCleanupAt=0;
    }
}
}
