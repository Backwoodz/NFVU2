using System;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    bool CanOpenStorage=>LocalPlayer() is Player local&&!local.IsDead()&&!local.IsTeleporting()&&!local.IsAttached()&&
        CanAccessCurrent(local)&&!Multiplayer()&&!driving&&!Flying&&!HordeVehicle&&body!=null&&body.linearVelocity.magnitude<=.5f&&
        Vector3.Distance(local.transform.position,car.transform.TransformPoint(new Vector3(0,.5f,IsHummer?-1.8f:-1.3f)))<3.5f;
    void OpenVehicleStorage() {
        if(!CanOpenStorage)return;
        try {
            if(current.storage==null)current.storage=new VehicleStorage(car,IsHummer,trunkLayout.Value);
            CloseVehicleMenu();
            if(current.storage.Open(Tr("Stauraum")+" · "+VehicleName)) {
                if(showroom!=null)showroom.State.Open[3]=true;
            } else ShowVehicleWarning("Stauraum ist gerade nicht verfügbar.");
        } catch(Exception e){Logger.LogError("Vehicle storage: "+e);ShowVehicleWarning("Stauraum ist gerade nicht verfügbar.");}
    }
    void DrawStorageButton(Rect rect) {
        int capacity=current.storage?.Capacity??VehicleStorageRules.Capacity(IsHummer,trunkLayout.Value);
        int count=current.storage?.Count??0;
        GUI.enabled=CanOpenStorage;
        if(MenuButton(rect,Tr("Stauraum öffnen")+" · "+(count<0?"?":count.ToString())+" / "+capacity))OpenVehicleStorage();
        GUI.enabled=true;
    }
    bool SetTrunkLayout(int layout) {
        layout=VehicleStorageRules.Layout(layout);
        foreach(var v in fleet)if(!v.Hummer&&v.storage!=null&&!v.storage.CanResize(layout)) {
            ShowVehicleWarning("Für diesen Ausbau zuerst Stauraum leeren und schließen.");return false;
        }
        foreach(var v in fleet)if(!v.Hummer&&v.storage!=null)v.storage.Resize(layout);
        trunkLayout.Value=layout;return true;
    }
    void TickVehicleStorage() {
        if(current.storage==null)return;
        current.storage.Tick();
        current.storage.SaveVehicle(Flying,hasBeenDriven);
        if(!IsHummer&&current.storage.Layout!=trunkLayout.Value&&!SetTrunkLayout(trunkLayout.Value))trunkLayout.Value=current.storage.Layout;
    }
}
}
