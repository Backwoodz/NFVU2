using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> tyreSmokeEnabled;
    void InitializeTyreSmoke() {
        tyreSmokeEnabled=Config.Bind("Effects","TyreSmokeEnabled",true,
            "Enable rear-tyre smoke during burnouts and drifting. Restart the game after changing this setting.");
    }
    VehicleBurnoutEffects CreateTyreSmoke(ModelData model) {
        if(!tyreSmokeEnabled.Value) {
            Logger.LogInfo("Rear-tyre smoke disabled by configuration; no smoke particle systems, materials or textures created.");
            return null;
        }
        return VehicleBurnoutEffects.Create(car,model,wheels,visuals,message=>Logger.LogInfo(message));
    }
}
}
