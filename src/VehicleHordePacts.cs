using UnityEngine;
using UnityEngine.InputSystem;
namespace Rachel350Z {
public partial class Plugin {
    int hordePactSelection;
    HordePact PactOption(int index)=>index==3?HordePact.None:hordeWave.Offers[index];
    void EndHordeWave() {
        ClearHordeProjectiles();
        hordeRun.EndWave();hordeBonus.Clear();hordeBonusTarget=hordeBoss=null;hordeBossEnraged=false;
        hordeElements?.Dispose();hordeElements=null;
        if(hordeVehicle?.vehicleImpact!=null)hordeVehicle.vehicleImpact.EnemyDamageMultiplier=1;
        if(hordeVisuals!=null)for(int i=hordeVisuals.Pickups.Count-1;i>=0;i--)hordeVisuals.Remove(hordeVisuals.Pickups[i]);
        if(hordeVehicle?.turret!=null){hordeVehicle.turret.State.ReloadMultiplier=1;hordeVehicle.turret.State.Refill();}
    }
    void ChooseHordePact(int index) {
        if(!hordeWave.Choose(PactOption(index)))return;
        hordeRun.BeginWave(hordeWave.Pact);hordeHits.Clear();pickupSpawnDelay=0;
        if(hordeElements==null)hordeElements=new HordeElementCombat();
        lastHordePosition=hordeVehicle.car.transform.position;
        hordeVehicle.turret.State.ReloadMultiplier=HordePacts.Reload(hordeRun.Pact);
        hordeVehicle.turret.State.Refill();controlsGate.Block();
    }
    void TickHordePactInput() {
        if(PadDown("up")||PadDown("prev"))hordePactSelection=(hordePactSelection+3)%4;
        if(PadDown("down")||PadDown("next"))hordePactSelection=(hordePactSelection+1)%4;
        var keyboard=Keyboard.current;
        for(int i=0;i<4;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))||(keyboard!=null&&keyboard[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)){ChooseHordePact(i);return;}
        if(PadDown("confirm"))ChooseHordePact(hordePactSelection);
        else if(PadDown("back"))ChooseHordePact(3);
    }
    void DrawHordePactChoice() {
        float scale=Mathf.Min(Screen.height/720f,(Screen.width-32)/800f);
        float width=760*scale,left=(Screen.width-width)/2,top=(Screen.height-610*scale)/2;
        HordeFill(new Rect(left-16*scale,top-12*scale,width+32*scale,630*scale),new Color(.025f,.04f,.065f,.94f));
        HordeFill(new Rect(left,top,width,3*scale),HordeGold);
        HordeLabel(new Rect(left,top+12*scale,width,42*scale),Tr("Pakte der Götter"),Mathf.RoundToInt(32*scale),HordeGold);
        HordeLabel(new Rect(left,top+57*scale,width,30*scale),Tr("Welle")+" "+hordeWave.Number+" / 5 · "+Tr("Gilt nur für die nächste Welle"),Mathf.RoundToInt(20*scale),HordeWhite);
        for(int i=0;i<4;i++) {
            var pact=PactOption(i);float y=top+(103+i*111)*scale;
            var rect=new Rect(left,y,width,101*scale);
            bool selected=padActive?i==hordePactSelection:rect.Contains(Event.current.mousePosition);
            HordeFill(rect,selected?new Color(.13f,.2f,.25f,.98f):new Color(.055f,.09f,.13f,.98f));
            HordeFill(new Rect(left,y,3*scale,101*scale),selected?HordeGold:new Color(.22f,.38f,.45f));
            if(GUI.Button(rect,"",GUIStyle.none))ChooseHordePact(i);
            float x=left+20*scale,w=width-40*scale;
            HordeLabel(new Rect(x,y+5*scale,w,30*scale),(i+1)+"  "+Tr(HordePacts.Name(pact)),Mathf.RoundToInt(24*scale),selected?HordeGold:HordeWhite,TextAnchor.MiddleLeft);
            HordeLabel(new Rect(x,y+38*scale,w,26*scale),Tr(HordePacts.Benefit(pact)),Mathf.RoundToInt(21*scale),new Color(.5f,.95f,.78f),TextAnchor.MiddleLeft);
            HordeLabel(new Rect(x,y+68*scale,w,26*scale),Tr(HordePacts.Cost(pact)),Mathf.RoundToInt(20*scale),pact==HordePact.None?HordeWhite:new Color(1,.65f,.5f),TextAnchor.MiddleLeft);
        }
        string hint=padActive?Tr("Steuerkreuz: wählen")+" · "+ButtonName("A","Kreuz")+": "+Tr("Bestätigen"):Tr("1–4 oder klicken: Pakt wählen");
        HordeLabel(new Rect(left,top+560*scale,width,28*scale),hint,Mathf.RoundToInt(20*scale),HordeWhite);
        HordeLabel(new Rect(left,top+589*scale,width,24*scale),Tr("Numpad 0: Hordenmodus beenden"),Mathf.RoundToInt(17*scale),new Color(.7f,.78f,.85f));
    }
}
}
