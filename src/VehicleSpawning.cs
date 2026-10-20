using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    SpawnAssetCache spawnCache=new SpawnAssetCache();
    bool spawnCacheUsed;
    StagedWork spawnWork;
    VehicleRuntime loadingVehicle;
    MonoBehaviour spawnOwner;
    CancellationTokenSource spawnCancellation;
    Task<SpawnAssets> spawnRead;
    Stopwatch spawnTimer;
    string spawnStage,loadingName;
    float spawnProgress;
    bool spawnCommitted;
    bool SpawnBusy=>spawnWork!=null;
    void Spawn(MonoBehaviour local) {
        if(local==null||Multiplayer())return;
        if(SpawnBusy){status="Ein Fahrzeug wird bereits geladen. F8: abbrechen.";return;}
        if(driving){status="Zum Spawnen bitte erst aussteigen.";return;}
        if(local is Player live&&live.IsDead())return;
        if(WorldVehicleCount>=VehicleFleet<VehicleRuntime>.Capacity){status="Maximal vier Fahrzeuge. Erst ein Fahrzeug entfernen.";return;}
        bool hummer=!DriftSessionRules.Active&&spawnModel.Value=="Hummer H2";
        if(!FindSpawnPoint(local,hummer,out var origin)){status="Kein freier Platz vor dir. Bitte eine freie Stelle suchen.";return;}
        StartVehicleLoad(local,hummer,null);
    }
    void StartVehicleLoad(MonoBehaviour local,bool hummer,ZDO saved) {
        CloseVehicleMenu();
        restoringVehicle=saved;
        loadingName=hummer?"Hummer H2":"Nissan 350Z";
        loadingVehicle=new VehicleRuntime{Hummer=hummer};spawnOwner=local;spawnCommitted=false;
        spawnCancellation=new CancellationTokenSource();spawnTimer=Stopwatch.StartNew();
        LoadStage("Dateien im Hintergrund einlesen",0);
        var folder=Path.GetDirectoryName(Info.Location);
        var cache=spawnCache;var token=spawnCancellation.Token;
        bool loadTransformer=!DriftSessionRules.Active&&(transformerEnabled.Value||saved!=null&&VehiclePersistence.Transformed(saved));
        spawnCacheUsed=true;
        spawnRead=Task.Run(()=>SpawnAssets.Load(folder,hummer,cache,token,loadTransformer),token);
        // Observe failures even if the player cancels/leaves before the worker finishes.
        spawnRead.ContinueWith(task=>{var ignored=task.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
        spawnWork=new StagedWork(BuildVehicle(local));
    }
    void LoadStage(string label,float progress) {
        spawnStage=label;spawnProgress=progress;
        status=loadingName+" wird geladen … F8: abbrechen";
    }
    void TickSpawn(MonoBehaviour local) {
        if(!SpawnBusy)return;
        if(local==null||local!=spawnOwner||Multiplayer()||local is Player live&&(live.IsDead()||live.IsTeleporting())){CancelSpawn("Laden abgebrochen.");return;}
        var selected=current;
        try {
            current=loadingVehicle;
            if(spawnWork.Tick(3))return;
            Logger.LogInfo("Vehicle spawn completed in "+spawnTimer.Elapsed.TotalSeconds.ToString("0.00")+" s; longest main-thread step "+spawnWork.SlowestStepMilliseconds.ToString("0.0")+" ms.");
            selected=loadingVehicle;
            spawnWork=null;loadingVehicle=null;spawnOwner=null;spawnRead=null;restoringVehicle=null;
            spawnCancellation.Dispose();spawnCancellation=null;
        }catch(Exception error) {
            Logger.LogError(error);
            CancelSpawn("Fahrzeug konnte nicht geladen werden. Siehe BepInEx-Log.");
        }finally {current=selected;}
    }
    void CancelSpawn(string message) {
        if(!SpawnBusy)return;
        spawnCancellation.Cancel();
        if(restoringVehicle!=null)deferredVehicles.Add(restoringVehicle.m_uid);
        spawnWork.Dispose();spawnWork=null;
        var selected=current;
        try {if(loadingVehicle!=null&&!spawnCommitted){current=loadingVehicle;ReleaseVehicle();}}
        finally {
            current=selected;loadingVehicle=null;spawnOwner=null;spawnRead=null;restoringVehicle=null;
            spawnCancellation.Dispose();spawnCancellation=null;
        }
        if(message!=null)status=message;
    }
    void DrawSpawnProgress() {
        if(!SpawnBusy)return;
        float width=Mathf.Min(460,Screen.width-30),x=(Screen.width-width)*.5f;
        UiBox(new Rect(x,18,width,102),loadingName+" wird geladen …");
        UiLabel(new Rect(x+14,43,width-28,23),spawnStage);
        var color=GUI.color;
        GUI.color=new Color(.14f,.17f,.2f,1);GUI.DrawTexture(new Rect(x+14,72,width-28,6),Texture2D.whiteTexture);
        GUI.color=new Color(.2f,.85f,1,1);
        if(spawnProgress==0) {
            float travel=(float)(spawnTimer.Elapsed.TotalSeconds*.55%1);
            GUI.DrawTexture(new Rect(x+14+travel*(width-100),72,72,6),Texture2D.whiteTexture);
        }else GUI.DrawTexture(new Rect(x+14,72,(width-28)*spawnProgress,6),Texture2D.whiteTexture);
        GUI.color=color;
        if(UiButton(new Rect(x+width-139,86,125,25),"Abbrechen ("+(padActive?ButtonName("B","Kreis"):"F8")+")"))CancelSpawn("Laden abgebrochen.");
    }
    IEnumerable<int> BuildVehicle(MonoBehaviour local) {
        // Give OnGUI a frame before the first Unity object allocation.
        yield return StagedWork.NextFrame;
        while(!spawnRead.IsCompleted)yield return StagedWork.NextFrame;
        var assets=spawnRead.GetAwaiter().GetResult();
        if(!DriftSessionRules.Active&&restoringVehicle!=null&&VehiclePersistence.Transformed(restoringVehicle)&&assets.Transformer==null)
            throw new InvalidOperationException("Gespeicherter Transformer kann ohne seine Modelldaten nicht wiederhergestellt werden.");
        foreach(var warning in assets.Warnings)Logger.LogWarning(warning);
        var model=assets.Model;
        current.transformerAsset=assets.Transformer;
        Logger.LogInfo("Vehicle data ready after "+spawnTimer.Elapsed.TotalSeconds.ToString("0.00")+" s; "+model.parts.Length+" body parts.");
        car=new GameObject(IsHummer?"H2_Local_Prototype":"350Z_Local_Prototype");car.SetActive(false);
        originalWheels=model.schemaVersion==2;
        if(originalWheels)wheelSizeVisuals=IsHummer?new WheelSizeVisuals(model,26,22,28):new WheelSizeVisuals(model);
        wheelVisualLift=model.wheelVisualLift;frontWheelVisualOffset=model.frontWheelVisualOffset;rearWheelVisualOffset=model.rearWheelVisualOffset;
        LoadStage("Texturen und Materialien",.25f);
        if(originalWheels)foreach(var step in VehicleMaterials.CreateForModelSteps(local,model,message=>Logger.LogInfo(message),(m,t)=>{vehicleMaterials=m;current.vehicleTextures=t;}))yield return step;
        else vehicleMaterials=VehicleMaterials.Create(local,message=>Logger.LogInfo(message));
        LoadStage("Karosserie",.42f);
        var paint=vehicleMaterials[0];var dark=vehicleMaterials[1];
        foreach(var p in model.parts.Where(p=>!originalWheels||p.group=="body")){AddVisual(p,car.transform,model,paint,dark);yield return 0;}
        current.exhaustVisuals=new VehicleExhaustVisuals(car,model);
        current.exhaustVisuals.Apply(exhaustStyle.Value);
        LoadStage("Fahrwerk und Räder",.54f);
        // A shorter, raised hull catches fewer small rocks with its bumper corners.
        var box=car.AddComponent<BoxCollider>();box.center=IsHummer?new Vector3(0,1.02f,0):new Vector3(0,.76f,0);box.size=IsHummer?new Vector3(1.92f,1.42f,4.35f):new Vector3(1.62f,.8f,3.8f);
        chassisMaterial=new PhysicsMaterial("350Z_Chassis"){dynamicFriction=.08f,staticFriction=.08f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum};box.sharedMaterial=chassisMaterial;
        body=car.AddComponent<Rigidbody>();body.mass=IsHummer?2900:1450;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        wheels=new WheelCollider[4];visuals=new Transform[4];brakeVisuals=new Transform[4];
        if(IsHummer)current.spinners=new Transform[4];
        for(int i=0;i<4;i++) {
            var wheel=new GameObject("Wheel"+i);wheel.transform.SetParent(car.transform,false);wheel.transform.localPosition=IsHummer?new Vector3(i%2==0?-.881f:.881f,.50f,i<2?1.521f:-1.58538f):new Vector3(i%2==0?-.8f:.8f,.34f,i<2?1.3f:-1.35f);
            var wc=wheel.AddComponent<WheelCollider>();wc.radius=IsHummer?.4364f:.34f;wheels[i]=wc;
            if(originalWheels) {
                var visual=new GameObject("OriginalWheel"+i);visual.transform.SetParent(car.transform,false);visuals[i]=visual.transform;
                var brakeVisual=new GameObject("OriginalBrake"+i);brakeVisual.transform.SetParent(car.transform,false);brakeVisuals[i]=brakeVisual.transform;
                foreach(var p in model.parts.Where(p=>p.group=="wheel"||(p.group=="brake"&&p.rotatesWithWheel))) {AddVisual(p,visual.transform,model,paint,dark);yield return 0;}
                if(IsHummer){var spinner=new GameObject("Spinner"+i);spinner.transform.SetParent(car.transform,false);current.spinners[i]=spinner.transform;
                    foreach(var p in model.parts.Where(p=>p.group=="spinner")){AddVisual(p,spinner.transform,model,paint,dark);yield return 0;}}
                foreach(var p in model.parts.Where(p=>p.group=="brake"&&!p.rotatesWithWheel)) {AddVisual(p,brakeVisual.transform,model,paint,dark);yield return 0;}
            } else {
                var visual=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(visual.GetComponent<Collider>());visual.transform.SetParent(car.transform,false);visual.transform.localScale=new Vector3(.68f,.12f,.68f);visual.GetComponent<Renderer>().sharedMaterial=dark;visuals[i]=visual.transform;
            }
        }
        handling=new VehicleHandling(body,wheels,EffectiveTerrain,message=>Logger.LogInfo(message));
        if(EffectiveDrift&&!IsHummer)handling.SetDriftMode();
        if(IsHummer)HummerTuning.Configure(body,wheels);
        current.rideHeight=new VehicleRideHeight(body,wheels);
        current.rideHeight.Apply(lowering.Value);
        LoadStage("Tacho",.62f);
        if(assets.Hud!=null) {
            dashboard=new VehicleDashboard();
            foreach(var step in StagedWork.Optional(dashboard.LoadSteps(assets.Hud),error=>{dashboard.Dispose();dashboard=null;Logger.LogWarning("HUD unavailable: "+error);}))yield return step;
        }
        LoadStage("Effekte und Sound",.72f);
        vehicleImpact=car.AddComponent<VehicleImpact>();
        vehicleImpact.Initialize(body,box,message=>Logger.LogWarning(message));
        burnoutEffects=CreateTyreSmoke(model);
        if(!IsHummer)vehicleNitro=new VehicleNitro(body,wheels);
        InitializeFlight(assets.Flames,assets.Audio);
        yield return 0;
        try {if(!IsHummer)nitroEffects=VehicleNitroEffects.Create(car,model,message=>Logger.LogInfo(message));}
        catch(Exception e){Logger.LogWarning("350Z nitro effects unavailable: "+e);}
        if(!IsHummer)effectsAudio=VehicleEffectsAudio.Create(car,message=>Logger.LogInfo(message),assets.Audio);
        current.exhaustSmoke=VehicleExhaustSmoke.Create(car,model,exhaustStyle.Value,message=>Logger.LogInfo(message));
        yield return 0;
        vehicleAudio=new VehicleAudio(IsHummer);
        foreach(var step in StagedWork.Optional(vehicleAudio.LoadSteps(car,body,wheels,message=>Logger.LogInfo(message),assets.Audio),error=>{vehicleAudio.Dispose();vehicleAudio=null;Logger.LogWarning("Vehicle audio unavailable: "+error);}))yield return step;
        if(!IsHummer&&assets.Transformer!=null) {
            current.transformerAudio=new TransformerFootstepAudio(car,assets.Audio.TransformerSteps,message=>Logger.LogInfo(message));
            yield return 0;
            current.transformerTurnAudio=new TransformerTurnAudio(car,assets.Audio.TransformerTurns,message=>Logger.LogInfo(message));
            yield return 0;
            current.transformerDeployAudio=new TransformerDeployAudio(car,assets.Audio.TransformerDeploy,message=>Logger.LogInfo(message));
            yield return 0;
            current.transformerReactorAudio=new TransformerReactorAudio(car,assets.Audio.TransformerReactor,message=>Logger.LogInfo(message));
            yield return 0;
            current.transformerReturnAudio=new TransformerReturnAudio(car,assets.Audio.TransformerReturn,message=>Logger.LogInfo(message));
            yield return 0;
        }
        try {vehicleLighting=VehicleLighting.Create(car,model,message=>Logger.LogInfo(message));}
        catch(Exception e){Logger.LogWarning("350Z lighting unavailable: "+e);}
        try {vehicleNeon=VehicleNeon.Create(car,model,message=>Logger.LogInfo(message));}
        catch(Exception e){Logger.LogWarning("350Z neon unavailable: "+e);}
        ApplyNeonSettings();
        yield return 0;
        LoadStage("Turret",.8f);
        if(assets.Turret!=null) {
            turret=new VehicleTurret();
            foreach(var step in StagedWork.Optional(turret.LoadSteps(car,local,Path.Combine(Path.GetDirectoryName(Info.Location),"turret"),assets.Turret,message=>Logger.LogInfo(message),assets.Audio),error=>{turret.Dispose();turret=null;Logger.LogWarning("Turret unavailable: "+error);}))yield return step;
        }
        LoadStage("Ausbau und Innenraum",.88f);
        vehicleRadio=VehicleRadio.Create(this,car,message=>Logger.LogWarning(message),false);
        if(model.animations!=null) {
            showroom=new VehicleShowroom(car,model,(part,parent)=>AddVisual(part,parent,model,paint,dark),dark);
            ApplyShowroomSettings();
        }
        yield return 0;
        if(assets.Interior!=null&&showroom!=null) {
            current.interior=new VehicleInterior();
            foreach(var step in StagedWork.Optional(current.interior.LoadSteps(car,local,assets.Interior,message=>Logger.LogInfo(message)),error=>{current.interior.Dispose();current.interior=null;Logger.LogWarning("Cabin unavailable; original visuals retained: "+error);}))yield return step;
        }
        LoadStage("Fahrzeug bereitstellen",.98f);
        if(!IsHummer&&current.transformerAsset!=null) {
            LoadStage("Transformer vorbereiten",.96f);
            current.transformer=new VehicleTransformer(current,current.transformerAsset,true);
            foreach(var step in current.transformer.LoadSteps())yield return step;
            if(assets.Minigun!=null&&assets.MinigunRig!=null) {
                current.transformerMinigun=new TransformerMinigun();
                foreach(var step in StagedWork.Optional(current.transformerMinigun.LoadSteps(car,local,
                    current.transformer.Visual.LeftShoulderMount,Path.Combine(Path.GetDirectoryName(Info.Location),"minigun"),
                    assets.Minigun,assets.MinigunRig,message=>Logger.LogInfo(message),assets.Audio),error=>{
                        current.transformerMinigun.Dispose();current.transformerMinigun=null;
                        Logger.LogWarning("Transformer minigun unavailable: "+error);
                    }))yield return step;
            }
        }
        yield return StagedWork.NextFrame;
        // Player/world may have moved since F6. Resolve a free location again.
        if(restoringVehicle!=null) {
            car.transform.SetPositionAndRotation(VehiclePersistence.Location(restoringVehicle),VehiclePersistence.Orientation(restoringVehicle));
            current.storage=new VehicleStorage(car,restoringVehicle);
            hasBeenDriven=VehiclePersistence.WasDriven(restoringVehicle);
        } else {
            if(!FindSpawnPoint(local,IsHummer,out var origin))throw new InvalidOperationException("Kein freier Spawnplatz mehr vorhanden.");
            car.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,local.transform.eulerAngles.y,0));
            current.storage=new VehicleStorage(car,IsHummer,trunkLayout.Value);
            VehiclePersistence.Mark(current.storage.Data,IsHummer);
            current.storage.SaveVehicle(false,false);
            savedVehicleIds.Add(current.storage.Data.m_uid);
        }
        car.SetActive(true);
        // Prime every movable source before RestoreParked snapshots its meshes.
        // Robot mode bypasses the ordinary car LateUpdate, including this setup.
        wheelSizeVisuals?.Apply(IsHummer?h2RimInches.Value:rimInches.Value);
        for(int i=0;i<4;i++)if(wheels[i]!=null&&visuals[i]!=null)UpdateWheelVisual(i,true);
        current.interior?.Tick(0,windowTint.Value,false);
        if(showroom!=null)showroom.Tick(0,false,doorStyle.Value,false,trunkNeonEnabled.Value,trunkNeonPulsing.Value,
            NeonColors[Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value))]);
        if(!DriftSessionRules.Active&&restoringVehicle!=null&&VehiclePersistence.Transformed(restoringVehicle)) {
            if(!current.transformer.RestoreParked(out var reason))throw new InvalidOperationException("Transformer konnte nicht wiederhergestellt werden: "+reason);
        } else if(!DriftSessionRules.Active&&restoringVehicle!=null&&VehiclePersistence.Flying(restoringVehicle))current.flight?.Fly();
        // Freeze until the saved terrain collider is available, even if this
        // staged load finishes after its zone has unloaded again.
        current.parkedStreaming=new VehicleWorldStreaming(body);
        current.parkedStreaming.Step(false);
        fleet.Add(current);fleetOwner=local;spawnCommitted=true;
        status=VehicleName+" bereit. E: einsteigen | "+menuKey.Value+": Fahrzeugmenü";
        if(local is Player&&MessageHud.instance!=null)
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,Tr("Gespawnt:")+" "+VehicleName,log:false);
    }
}
}
