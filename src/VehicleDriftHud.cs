using System.IO;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    DriftCircuitHud driftCircuitHud;
    bool driftCircuitHudFailed;
    void TickDriftCircuitHud() {
        if(!DriftWorldRules.Current||Multiplayer()||Player.m_localPlayer==null){ClearDriftCircuitHud();return;}
        if(driftCircuitHudFailed)return;
        try {
            if(driftCircuitHud==null)driftCircuitHud=new DriftCircuitHud(Path.GetDirectoryName(Info.Location),message=>Logger.LogWarning(message));
            driftCircuitHud.Tick(Hud.instance,driftLapScore,
                DriftCircuitScoreVisible&&!menuOpen&&!UIVisible()&&!Hud.IsUserHidden()&&!Minimap.IsOpen(),uiLanguage.Value=="DE");
        } catch(System.Exception error) {
            ClearDriftCircuitHud();driftCircuitHudFailed=true;
            Logger.LogWarning("Drift score display unavailable: "+error.GetBaseException().Message);
        }
    }
    void ClearDriftCircuitHud(){driftCircuitHud?.Clear();driftCircuitHud=null;driftCircuitHudFailed=false;}
}
}
