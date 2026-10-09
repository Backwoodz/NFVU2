using UnityEngine;
namespace Rachel350Z {
// All spawned-car resources are owned independently; player/camera remain with Plugin.
internal sealed class VehicleRuntime {
    public bool Hummer;
    public Transform[] spinners;
    public readonly SpinnerMotion[] spinnerMotion={new SpinnerMotion(),new SpinnerMotion(),new SpinnerMotion(),new SpinnerMotion()};
    public GameObject car;
    public Rigidbody body;
    public WheelCollider[] wheels;
    public Transform[] visuals;
    public Material[] vehicleMaterials;
    public Texture2D[] vehicleTextures;
    public Transform[] brakeVisuals;
    public WheelSizeVisuals wheelSizeVisuals;
    public bool originalWheels;
    public float wheelVisualLift;
    public float frontWheelVisualOffset;
    public float rearWheelVisualOffset;
    public PhysicsMaterial chassisMaterial;
    public VehicleHandling handling;
    public VehicleRideHeight rideHeight;
    public VehicleShowroom showroom;
    public VehicleImpact vehicleImpact;
    public bool hasBeenDriven;
    public VehicleAudio vehicleAudio;
    public VehicleLighting vehicleLighting;
    public VehicleNeon vehicleNeon;
    public VehicleRadio vehicleRadio;
    public VehicleNitro vehicleNitro;
    public VehicleNitroEffects nitroEffects;
    public VehicleBurnoutEffects burnoutEffects;
    public VehicleEffectsAudio effectsAudio;
    public bool nitroHeld;
    public bool burnoutHeld;
    public bool honking;
    public bool driving;
    public float steer;
    public float throttle;
    public float brake;
    public VehicleTurret turret;
    public VehicleDashboard dashboard;
    public readonly System.Collections.Generic.Dictionary<ModelPart,Mesh> audioMeshes=new System.Collections.Generic.Dictionary<ModelPart,Mesh>();
    public readonly System.Collections.Generic.List<Mesh> vehicleMeshes=new System.Collections.Generic.List<Mesh>();
    public readonly NitroInputGate nitroInput=new NitroInputGate();
    public readonly BackfireState backfireState=new BackfireState();
}
public partial class Plugin {
    readonly VehicleFleet<VehicleRuntime> fleet=new VehicleFleet<VehicleRuntime>(new VehicleRuntime());
    VehicleRuntime current {get=>fleet.Current;set=>fleet.Current=value;}
    bool IsHummer=>current.Hummer;
    string VehicleName=>IsHummer?"Hummer H2":"Nissan 350Z";
    GameObject car {get=>current.car;set=>current.car=value;}
    Rigidbody body {get=>current.body;set=>current.body=value;}
    WheelCollider[] wheels {get=>current.wheels;set=>current.wheels=value;}
    Transform[] visuals {get=>current.visuals;set=>current.visuals=value;}
    Material[] vehicleMaterials {get=>current.vehicleMaterials;set=>current.vehicleMaterials=value;}
    Texture2D[] vehicleTextures {get=>current.vehicleTextures;set=>current.vehicleTextures=value;}
    Transform[] brakeVisuals {get=>current.brakeVisuals;set=>current.brakeVisuals=value;}
    WheelSizeVisuals wheelSizeVisuals {get=>current.wheelSizeVisuals;set=>current.wheelSizeVisuals=value;}
    bool originalWheels {get=>current.originalWheels;set=>current.originalWheels=value;}
    float wheelVisualLift {get=>current.wheelVisualLift;set=>current.wheelVisualLift=value;}
    float frontWheelVisualOffset {get=>current.frontWheelVisualOffset;set=>current.frontWheelVisualOffset=value;}
    float rearWheelVisualOffset {get=>current.rearWheelVisualOffset;set=>current.rearWheelVisualOffset=value;}
    PhysicsMaterial chassisMaterial {get=>current.chassisMaterial;set=>current.chassisMaterial=value;}
    VehicleHandling handling {get=>current.handling;set=>current.handling=value;}
    VehicleShowroom showroom {get=>current.showroom;set=>current.showroom=value;}
    VehicleImpact vehicleImpact {get=>current.vehicleImpact;set=>current.vehicleImpact=value;}
    bool hasBeenDriven {get=>current.hasBeenDriven;set=>current.hasBeenDriven=value;}
    VehicleAudio vehicleAudio {get=>current.vehicleAudio;set=>current.vehicleAudio=value;}
    VehicleLighting vehicleLighting {get=>current.vehicleLighting;set=>current.vehicleLighting=value;}
    VehicleNeon vehicleNeon {get=>current.vehicleNeon;set=>current.vehicleNeon=value;}
    VehicleRadio vehicleRadio {get=>current.vehicleRadio;set=>current.vehicleRadio=value;}
    VehicleNitro vehicleNitro {get=>current.vehicleNitro;set=>current.vehicleNitro=value;}
    VehicleNitroEffects nitroEffects {get=>current.nitroEffects;set=>current.nitroEffects=value;}
    VehicleBurnoutEffects burnoutEffects {get=>current.burnoutEffects;set=>current.burnoutEffects=value;}
    VehicleEffectsAudio effectsAudio {get=>current.effectsAudio;set=>current.effectsAudio=value;}
    bool nitroHeld {get=>current.nitroHeld;set=>current.nitroHeld=value;}
    bool burnoutHeld {get=>current.burnoutHeld;set=>current.burnoutHeld=value;}
    bool honking {get=>current.honking;set=>current.honking=value;}
    bool driving {get=>current.driving;set=>current.driving=value;}
    float steer {get=>current.steer;set=>current.steer=value;}
    float throttle {get=>current.throttle;set=>current.throttle=value;}
    float brake {get=>current.brake;set=>current.brake=value;}
    VehicleTurret turret {get=>current.turret;set=>current.turret=value;}
    VehicleDashboard dashboard {get=>current.dashboard;set=>current.dashboard=value;}
    System.Collections.Generic.Dictionary<ModelPart,Mesh> audioMeshes=>current.audioMeshes;
    System.Collections.Generic.List<Mesh> vehicleMeshes=>current.vehicleMeshes;
    NitroInputGate nitroInput=>current.nitroInput;
    BackfireState backfireState=>current.backfireState;
}
}
