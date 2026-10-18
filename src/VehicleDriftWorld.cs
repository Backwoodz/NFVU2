using System;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    DriftTrackBuilder driftTrack;
    void TickDriftTrack(MonoBehaviour local) {
        if(local==null||!DriftWorldRules.Current||Multiplayer()) {driftTrack?.Clear();return;}
        if(driftTrack==null)driftTrack=new DriftTrackBuilder(message=>Logger.LogWarning(message));
        driftTrack.Tick();
    }
    void UpdateDriftSeedFeedback() {
        var startup=FejdStartup.instance;
        DriftSeedFeedback.Tick(startup!=null?startup.m_newWorldSeed:null);
    }
    void InitializeDriftWorld() {
        foreach(var patch in new[]{typeof(DriftBiome),typeof(DriftHeight),typeof(DriftLocationGeneration),
            typeof(DriftVegetation),typeof(DriftLocations),typeof(DriftStartPoint)})
            controlsHarmony.PatchAll(patch);
        Logger.LogInfo("NFVU2 drift world ready: seed 'drift', flat Meadows at height 32; the 335 m drift test course builds nearby. No FlatGrass required.");
    }
}
}
