using System;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    DriftTrackBuilder driftTrack;
    DriftCircuitMap driftCircuitMap;
    DriftCircuitAtmosphere driftCircuitAtmosphere;
    void TickDriftTrack(MonoBehaviour local) {
        if(local==null||!DriftWorldRules.Current||Multiplayer()||Game.instance==null||Game.instance.IsShuttingDown()) {
            driftCircuitAtmosphere?.Clear();ClearDriftCircuitHud();driftCircuitMap?.Clear();driftTrack?.Clear();return;
        }
        if(driftCircuitMap==null)driftCircuitMap=new DriftCircuitMap(message=>Logger.LogWarning(message));
        driftCircuitMap.Tick(Minimap.instance,ZDOMan.instance,true);
        if(driftTrack==null)driftTrack=new DriftTrackBuilder(message=>Logger.LogWarning(message));
        driftTrack.Tick();
        if(driftCircuitAtmosphere==null)driftCircuitAtmosphere=new DriftCircuitAtmosphere(message=>Logger.LogWarning(message));
        driftCircuitAtmosphere.Tick(local.transform.position,ZDOMan.instance);
    }
    void UpdateDriftSeedFeedback() {
        var startup=FejdStartup.instance;
        DriftSeedFeedback.Tick(startup!=null?startup.m_newWorldSeed:null);
    }
    void InitializeDriftWorld() {
        foreach(var patch in new[]{typeof(DriftBiome),typeof(DriftHeight),typeof(DriftLocationGeneration),
            typeof(DriftVegetation),typeof(DriftLocations),typeof(DriftStartPoint)})
            controlsHarmony.PatchAll(patch);
        Logger.LogInfo("NFVU2 drift world ready: seed 'drift'; parking arrival, unlimited 1.49 km practice and separate graded 503 m Parkade event with three laps. No FlatGrass required.");
        InitializeDriftSession();
        InitializeDriftArrival();
    }
}
}
