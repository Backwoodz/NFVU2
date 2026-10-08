using System.Collections.Generic;
using BepInEx.Configuration;
namespace Rachel350Z {
public partial class Plugin {
    readonly Dictionary<ConfigEntryBase,ConfigEntryBase> h2Settings=new Dictionary<ConfigEntryBase,ConfigEntryBase>();
    ConfigEntry<T> BindVehicle<T>(string section,string key,T value,string description)=>BindVehicle(section,key,value,new ConfigDescription(description));
    ConfigEntry<T> BindVehicle<T>(string section,string key,T value,ConfigDescription description) {
        var stock=Config.Bind(section,key,value,description);
        h2Settings[stock]=Config.Bind("Hummer."+section,key,value,description);return stock;
    }
    ConfigEntry<T> VehicleConfig<T>(ConfigEntry<T> stock)=>stock!=null&&IsHummer&&h2Settings.TryGetValue(stock,out var other)?(ConfigEntry<T>)other:stock;
    ConfigEntry<bool> z_terrainMode;
    ConfigEntry<bool> terrainMode {get=>VehicleConfig(z_terrainMode);set=>z_terrainMode=value;}
    ConfigEntry<float> z_soundVolume;
    ConfigEntry<float> soundVolume {get=>VehicleConfig(z_soundVolume);set=>z_soundVolume=value;}
    ConfigEntry<bool> z_radioEnabled;
    ConfigEntry<bool> radioEnabled {get=>VehicleConfig(z_radioEnabled);set=>z_radioEnabled=value;}
    ConfigEntry<bool> z_neonEnabled;
    ConfigEntry<bool> neonEnabled {get=>VehicleConfig(z_neonEnabled);set=>z_neonEnabled=value;}
    ConfigEntry<bool> z_neonPulsing;
    ConfigEntry<bool> neonPulsing {get=>VehicleConfig(z_neonPulsing);set=>z_neonPulsing=value;}
    ConfigEntry<bool> z_nitroEnabled;
    ConfigEntry<bool> nitroEnabled {get=>VehicleConfig(z_nitroEnabled);set=>z_nitroEnabled=value;}
    ConfigEntry<bool> z_backfireEnabled;
    ConfigEntry<bool> backfireEnabled {get=>VehicleConfig(z_backfireEnabled);set=>z_backfireEnabled=value;}
    ConfigEntry<bool> z_keepRunning;
    ConfigEntry<bool> keepRunning {get=>VehicleConfig(z_keepRunning);set=>z_keepRunning=value;}
    ConfigEntry<float> z_radioVolume;
    ConfigEntry<float> radioVolume {get=>VehicleConfig(z_radioVolume);set=>z_radioVolume=value;}
    ConfigEntry<string> z_neonColor;
    ConfigEntry<string> neonColor {get=>VehicleConfig(z_neonColor);set=>z_neonColor=value;}
    ConfigEntry<string> z_headlightPreset;
    ConfigEntry<string> headlightPreset {get=>VehicleConfig(z_headlightPreset);set=>z_headlightPreset=value;}
    ConfigEntry<float> z_headlightStrength;
    ConfigEntry<float> headlightStrength {get=>VehicleConfig(z_headlightStrength);set=>z_headlightStrength=value;}
    ConfigEntry<float> z_frontCamber;
    ConfigEntry<float> frontCamber {get=>VehicleConfig(z_frontCamber);set=>z_frontCamber=value;}
    ConfigEntry<float> z_rearCamber;
    ConfigEntry<float> rearCamber {get=>VehicleConfig(z_rearCamber);set=>z_rearCamber=value;}
    ConfigEntry<float> z_frontWheelOffset;
    ConfigEntry<float> frontWheelOffset {get=>VehicleConfig(z_frontWheelOffset);set=>z_frontWheelOffset=value;}
    ConfigEntry<float> z_rearWheelOffset;
    ConfigEntry<float> rearWheelOffset {get=>VehicleConfig(z_rearWheelOffset);set=>z_rearWheelOffset=value;}
    ConfigEntry<bool> z_driftMode;
    ConfigEntry<bool> driftMode {get=>VehicleConfig(z_driftMode);set=>z_driftMode=value;}
}
}
