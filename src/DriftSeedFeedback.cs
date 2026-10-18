using TMPro;
using UnityEngine;

namespace Rachel350Z {
internal static class DriftSeedFeedback {
    internal static readonly Color MatchColor=new Color(.35f,1f,.5f,1f);
    static TMP_Text label;
    static Color normal;
    static bool highlighted;

    // Run after input events in LateUpdate. This also works when the world
    // creation panel was already open, without event listeners or extra hooks.
    public static void Tick(TMP_InputField input) {
        var next=input!=null&&input.isActiveAndEnabled?input.textComponent:null;
        if(next!=label){Restore();label=next;}
        if(label==null)return;
        if(DriftWorldRules.MatchesSeed(input.text)) {
            if(!highlighted||label.color!=MatchColor)normal=label.color;
            highlighted=true;label.color=MatchColor;
        } else Restore();
    }
    static void Restore() {
        // Another mod/theme may have supplied its own feedback in this frame.
        // Restore only the tint that NFVU2 still owns.
        if(highlighted&&label!=null&&label.color==MatchColor)label.color=normal;
        highlighted=false;
    }
    public static void Clear(){Restore();label=null;}
}
}
