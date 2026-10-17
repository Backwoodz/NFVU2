using System;
using System.IO;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    bool Flying=>current.flight!=null&&current.flight.Active;
    NitroState ActiveNitro=>Flying?current.flight.Nitro:vehicleNitro?.State;
    void InitializeFlight(ModelData model,SpawnAudio prepared) {
        current.flight=new VehicleFlight(body,wheels,vehicleNitro?.State);
        try {
            // The H2 has no exhaust nitro, but can use the 350Z's shared FIREB atlas.
            var effectsModel=model;
            current.flightEffects=VehicleNitroEffects.CreateWheelThrusters(car,effectsModel,visuals,message=>Logger.LogInfo(message));
        }catch(Exception e){Logger.LogWarning("Flight flame atlas unavailable: "+e.Message);}
        current.flightAudio=VehicleEffectsAudio.CreateThruster(car,message=>Logger.LogInfo(message),prepared);
    }
    void SelectFlightMode() {
        if(Transforming)return;
        if(HordeVehicle){ShowVehicleWarning("Hordenmodus: nur Gelände verfügbar.");return;}
        if(!driving||!DriverInputReady){status="Zum Fliegen zuerst einsteigen.";return;}
        if(body.transform.up.y<.5f){status="Zum Starten muss das Auto aufrecht stehen.";return;}
        BlockDrivingInput();
        if(!Flying)handling?.Step(false,0,0,1); // Reset burnout and motor torque before releasing the wheel controller.
        current.flight?.Fly();showroom?.State.CloseAll();
        current.boostLogCount=0;current.boostLogTimer=0;
        status="Flugmodus: W/S vor/zurück · A/D drehen · Shift hoch · Leertaste runter";
    }
    void TickFlight() {
        var flight=current.flight;if(flight==null)return;
        bool blocked=audioBlocked||!Application.isFocused;
        try {current.flightEffects?.TickFlight(flight.Active?flight.State.Blend:0,flight.Thrust,blocked,originalWheels);}
        catch(Exception e){current.flightEffects?.Dispose();current.flightEffects=null;Logger.LogWarning("Flight flames disabled: "+e);}
        try {current.flightAudio?.TickFlight(flight.Active,flight.State.Blend,flight.Thrust,blocked,soundVolume.Value);}
        catch(Exception e){current.flightAudio?.Dispose();current.flightAudio=null;Logger.LogWarning("Flight sound disabled: "+e);}
    }
}
}
