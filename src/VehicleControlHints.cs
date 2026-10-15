using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    GUIStyle hintStyle;
    float controlHintHeight;
    void DrawControlHints() {
        if(audioBlocked||Hud.IsUserHidden())return;
        if(hintStyle==null)hintStyle=new GUIStyle(GUI.skin.box){wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,6,6)};
        hintStyle.fontSize=Mathf.RoundToInt(19*hintScale.Value);
        string text;
        if(padActive) {
            string pedals=ButtonName("RT/LT","R2/L2"),exit=ButtonName("B","Kreis"),nitro=ButtonName("X","Quadrat");
            if(driving)text=DriveModeName+" | "+pedals+": Gas/Bremse | LS: lenken · RS: Kamera\n"+(Flying?ButtonName("RB/LB","R1/L1")+": hoch/runter · "+exit+": landen & aussteigen":ButtonName("A","Kreuz")+": Handbremse · "+ButtonName("LB","L1")+": Geschütz · "+exit+": aussteigen")+" · "+nitro+": Nitro\nSteuerkreuz ←/→: Songs · ↑/↓: Fahrmodus · L3: Menü";
            else text="L3: Garage / Modmenü"+(CanAccessCurrent(LocalPlayer())?" · "+exit+": einsteigen":"")+"\nFahrzeug erstellen: Garage → Fahrzeug spawnen";
        }else text=driving?DriveModeName+" | "+(Flying?"W/S: vor/zurück · A/D: drehen · Shift/Leertaste: hoch/runter":"WASD: fahren · Leertaste: Handbremse/Bremse · RMB: Geschütz")+"\nF7: "+(Flying?"landen & aussteigen":"aussteigen")+" · 1–4: Fahrmodus · ←/→: Songs · LMB/Strg: Nitro · "+menuKey.Value+": Menü":"F6: Fahrzeug erstellen · F7: einsteigen · "+menuKey.Value+": Garage";
        if(Transforming)text="Transformer | "+string.Join(" | ",TransformerHints());
        text=Tr(text);
        float width=Mathf.Min(Screen.width-30,1050);
        if(driving&&hudEnabled.Value)width=Mathf.Min(width,Mathf.Max(300,Screen.width-350));
        float height=hintStyle.CalcHeight(new GUIContent(text),width);controlHintHeight=height;
        if(!driving||!NativeVehicleHintsVisible)GUI.Box(new Rect(15,Screen.height-height-15,width,height),text,hintStyle);
        if(!driving&&!string.IsNullOrEmpty(status)&&!status.StartsWith("F6:"))UiBox(new Rect(15,Screen.height-height-57,width,36),status);
        if(driving&&Time.unscaledTime<songToastUntil&&!string.IsNullOrEmpty(songToast)) {
            var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(songToastUntil-Time.unscaledTime));
            GUI.Box(new Rect(Mathf.Max(15,(Screen.width-620)/2),28,Mathf.Min(620,Screen.width-30),58),"♫ "+songToast,hintStyle);GUI.color=old;
        }
    }
}
}
