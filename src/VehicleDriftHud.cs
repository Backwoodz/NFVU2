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
            bool race=driftRace.Phase==DriftRacePhase.Racing;
            driftCircuitHud.Tick(Hud.instance,race?driftRace.Score:driftLapScore,
                DriftCircuitScoreVisible&&!menuOpen&&!UIVisible()&&!Hud.IsUserHidden()&&!Minimap.IsOpen(),uiLanguage.Value=="DE",
                race?"PARKADE 1":null,race?driftRace.BankedScore:0,race?driftRace.BestScore:-1,race?3:0);
        } catch(System.Exception error) {
            ClearDriftCircuitHud();driftCircuitHudFailed=true;
            Logger.LogWarning("Drift score display unavailable: "+error.GetBaseException().Message);
        }
    }
    void ClearDriftCircuitHud(){driftCircuitHud?.Clear();driftCircuitHud=null;driftCircuitHudFailed=false;}
}
}
