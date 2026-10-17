using System;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    void TickTransformerMinigun() {
        var weapon=current.transformerMinigun;
        if(weapon==null)return;
        try {
            var robot=current.transformer;
            bool visible=robot!=null&&robot.Active&&robot.State.Progress>=.96f;
            bool active=visible&&robot.State.CanWalk&&!robot.ReturnRequested&&!robot.Mounting
                &&driving&&DriverInputReady&&!transformerEntryPending&&!transformerExitPending
                &&!FootstepEffectsBlocked;
            weapon.Tick(visible,active,active&&turretHeld,active?LocalPlayer() as Character:null,
                soundVolume.Value*turretVolume.Value,active&&Input.GetKeyDown(KeyCode.R));
            weapon.SetLaser(active&&turretLaser.Value,laserStrength.Value,LaserColor);
        } catch(Exception error) {
            weapon.Dispose();current.transformerMinigun=null;
            Logger.LogWarning("Transformer minigun disabled: "+error);
        }
    }
    void DrawTransformerMinigunAim() {
        var weapon=current.transformerMinigun;
        if(!driving||menuOpen||audioBlocked||!Application.isFocused||!DriverInputReady
            ||weapon==null||!weapon.Enabled||Camera.main==null)return;
        var robot=current.transformer;
        if(robot==null||!robot.State.CanWalk||robot.ReturnRequested||robot.Mounting)return;
        var previous=GUI.color;
        try {
            var sight=Camera.main.ViewportToScreenPoint(new Vector3(TransformerAimSettings.ViewX,TransformerAimSettings.ViewY,0));
            float x=sight.x,y=Screen.height-sight.y;
            var bars=new[]{new Rect(x-12,y-1,7,2),new Rect(x+5,y-1,7,2),new Rect(x-1,y-12,2,7),new Rect(x-1,y+5,2,7)};
            GUI.color=new Color(0,0,0,.85f);
            foreach(var bar in bars)GUI.DrawTexture(new Rect(bar.x-1,bar.y-1,bar.width+2,bar.height+2),Texture2D.whiteTexture);
            GUI.color=weapon.Aligned?new Color(.5f,1,.2f):new Color(1,.45f,.1f);
            foreach(var bar in bars)GUI.DrawTexture(bar,Texture2D.whiteTexture);
            GUI.color=Color.white;
            string label=weapon.State.ReloadLeft>0?"Minigun · Nachladen…":
                (padActive?ButtonName("LB","L1"):"LMB")+": Minigun · "+weapon.State.Ammo+" / 1000"
                +(weapon.Obstructed?" · Blockiert":"");
            UiBox(new Rect(15,Screen.height-controlHintHeight-52,430,28),label);
        } finally {GUI.color=previous;}
    }
}
}
