using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<string> spawnModel;
    MonoBehaviour fleetOwner;
    void InitializeGarage(){spawnModel=Config.Bind("Garage","SpawnModel","Nissan 350Z",new ConfigDescription("Vehicle created with F6 or the garage button.",new AcceptableValueList<string>("Nissan 350Z","Hummer H2")));}
    bool CanAccessCurrent(MonoBehaviour local)=>car!=null&&local!=null&&(driving||Vector3.Distance(local.transform.position,car.transform.position)<5);
    void SelectNearest(MonoBehaviour local) {
        var nearest=fleet.Nearest(v=>v.car!=null?(v.car.transform.position-local.transform.position).sqrMagnitude:float.PositiveInfinity);
        if(nearest!=null)current=nearest;
    }
    void ForEachVehicle(Action action)=>fleet.Visit(action);
    void CleanupAll() {
        ClearNitroVisuals();
        if(spawnCacheUsed){spawnCache=new SpawnAssetCache();spawnCacheUsed=false;}
        CancelSpawn(null);
        if(driving||player!=null)Exit();
        CloseVehicleMenu();
        foreach(var vehicle in fleet.ToArray()){current=vehicle;Cleanup();}
        current=new VehicleRuntime();fleetOwner=null;orbitCamera?.End();fleetMusic?.Tick(false);
    }
    bool FindSpawnPoint(MonoBehaviour local,bool hummer,out Vector3 result) {
        var rotation=Quaternion.Euler(0,local.transform.eulerAngles.y,0);
        // Try alternating spots alongside the first car, then the next row.
        for(int i=0;i<18;i++) {
            int column=i%6;float side=column==0?0:((column+1)/2)*3.8f*(column%2==1?1:-1);
            var origin=local.transform.position+rotation*new Vector3(side,6,6+i/6*6);
            if(!Physics.Raycast(origin,Vector3.down,out var hit,18,~0,QueryTriggerInteraction.Ignore))continue;
            if(hit.normal.y<.7f)continue;
            var root=hit.point+Vector3.up*(hummer?.3f:.35f);
            if(fleet.Any(v=>v.car!=null&&Vector3.Distance(v.car.transform.position,root)<3.7f))continue;
            var centre=root+Vector3.up*(hummer?1.02f:.76f);
            var half=hummer?new Vector3(1.18f,.7f,2.55f):new Vector3(1.1f,.4f,2.4f);
            if(Physics.CheckBox(centre,half,rotation,~0,QueryTriggerInteraction.Ignore))continue;
            result=root;return true;
        }
        result=Vector3.zero;return false;
    }
    void DrawGarageMenu() {
        UiLabel(new Rect(25,145,510,30),"Fahrzeug spawnen",labelStyle);
        int selected=spawnModel.Value=="Hummer H2"?1:0;
        int next=UiToolbar(new Rect(25,185,510,40),selected,new[]{"Nissan 350Z","Hummer H2"});
        if(next!=selected)spawnModel.Value=next==1?"Hummer H2":"Nissan 350Z";
        UiLabel(new Rect(25,240,510,65),next==1?"Gelber H2 · 26-Zoll-Spinner · eigener SUV-Sound\nOhne Motorraum- und Kofferraumausbau.":"350Z mit deinen bisherigen Funktionen und Einstellungen.",mutedStyle);
        GUI.enabled=!SpawnBusy&&!driving&&fleet.Count<4;
        if(MenuButton(new Rect(25,310,510,40),padActive?"Fahrzeug spawnen ("+ButtonName("A","Kreuz")+")":"Ausgewähltes Fahrzeug spawnen (F6)")){Spawn(LocalPlayer());return;}
        GUI.enabled=true;
        UiLabel(new Rect(25,366,510,60),"Gespawnt: "+fleet.Count+" / 4\n"+string.Join(" · ",fleet.Select(v=>v.Hummer?"H2":"350Z")),labelStyle);
        UiLabel(new Rect(25,436,510,72),"Bestehende Autos bleiben stehen. Zum Einsteigen oder Einstellen zum gewünschten Auto gehen: F7 / F9. F8 entfernt nur dieses Auto. Fahrzeuge bleiben bis zum Weltwechsel.",mutedStyle);
    }
}
}
