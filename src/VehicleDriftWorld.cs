using System;

namespace Rachel350Z {
public partial class Plugin {
    void UpdateDriftSeedFeedback() {
        var startup=FejdStartup.instance;
        DriftSeedFeedback.Tick(startup!=null?startup.m_newWorldSeed:null);
    }
    void InitializeDriftWorld() {
        foreach(var patch in new[]{typeof(DriftBiome),typeof(DriftHeight),typeof(DriftLocationGeneration),
            typeof(DriftVegetation),typeof(DriftLocations),typeof(DriftStartPoint)})
            controlsHarmony.PatchAll(patch);
        Logger.LogInfo("NFVU2 drift world ready: seed 'drift', flat Meadows at height 32. No FlatGrass required; no track placed yet.");
    }
}
}
