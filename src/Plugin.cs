using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
// Rendered vehicles are local; metadata/cargo use persistent vanilla world ZDOs.
[BepInPlugin("local.rachel350z.prototype", "350Z vehicle prototype", "0.30.0")]
[DefaultExecutionOrder(10000)]
public partial class Plugin : BaseUnityPlugin {
    VehicleOrbitCamera orbitCamera;
    ValheimMusicDucking fleetMusic;
    bool audioBlocked;
    ConfigEntry<bool> invertMouseY;
    ConfigEntry<float> mouseSensitivity;
    MonoBehaviour player, gameCamera; Rigidbody playerBody; bool playerEnabled, cameraEnabled, playerKinematic;
    Renderer[] playerRenderers; bool[] renderStates; Collider[] playerColliders; bool[] colliderStates;
    string status = "F6: Fahrzeug erstellen | F7: einsteigen | F9: Garage";
    Vector3 lastExitPosition;
    VehicleCabinDriver cabinDriver;
    bool DriverInputReady=>cabinDriver==null||cabinDriver.Motion.CanDrive;
    Type playerType, netType; bool ready;
    bool ParkedRunning=>car!=null&&hasBeenDriven&&!driving&&keepRunning.Value;
    void Awake() {
        var asm=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetType("Player")!=null);
        playerType=asm?.GetType("Player");netType=asm?.GetType("ZNet");
        terrainMode=BindVehicle("Driving","TerrainMode",true,"Terrain suspension, traction and front-wheel assistance. Change in the vehicle menu; off uses rear-wheel drive.");
        mouseSensitivity=Config.Bind("Camera","MouseSensitivity",.15f,new ConfigDescription("Orbit sensitivity in degrees per mouse pixel.",new AcceptableValueRange<float>(.02f,1f)));
        invertMouseY=Config.Bind("Camera","InvertMouseY",false,"Invert vertical mouse look while driving.");
        soundVolume=BindVehicle("Audio","Volume",.8f,new ConfigDescription("Vehicle sound volume. 0 mutes engine, tyres and horn; 1 is full volume.",new AcceptableValueRange<float>(0,1)));
        orbitCamera=new VehicleOrbitCamera(message=>Logger.LogInfo(message));
        InitializeControls();
        InitializeMenu();
        InitializeExhaustMenu();
        InitializeNitroVisuals();
        InitializeBoostInput();
        fleetMusic=new ValheimMusicDucking(message=>Logger.LogWarning(message));
        ready=playerType!=null;Logger.LogInfo("350Z prototype loaded. Single-player with persistent world vehicles.");
    }
    MonoBehaviour LocalPlayer() => playerType?.GetField("m_localPlayer",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as MonoBehaviour;
    bool Multiplayer() {
        if(netType==null)return true;
        var single=netType.GetProperty("IsSinglePlayer",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
        if(single!=null)return !(bool)single.GetValue(null);
        object net=netType.GetProperty("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null)
            ??netType.GetField("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null);
        if(net==null)return true;
        var m=netType.GetMethod("IsSinglePlayer",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(m!=null)return !(bool)m.Invoke(net,null);
        return true; // Fail closed until the installed game's API has been verified.
    }
    bool UIVisible() {
        foreach(var name in new[]{"Console","Chat","InventoryGui","Menu","TextInput"}) {
            var t=playerType.Assembly.GetType(name);if(t==null)continue;
            var m=t.GetMethod("IsVisible",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null)
                ??t.GetMethod("IsMenuVisible",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null);
            if(m!=null && m.ReturnType==typeof(bool) && (bool)m.Invoke(null,null))return true;
            if(name=="Chat"){
                var chat=t.GetProperty("instance",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(null);
                var visible=t.GetMethod("IsDialogVisible",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null);
                if(chat!=null&&visible!=null&&(bool)visible.Invoke(chat,null))return true;
            }
        }return false;
    }
    void Update() {
        if(!ready)return;
        SampleControls();
        var local=LocalPlayer();
        TickHordeWeather(local);
        CheckHordeContext(local);
        if(local==null){CleanupAll();return;}
        if((fleetOwner!=null && local!=fleetOwner) || (driving && local!=player) || (fleet.Count>0 && Multiplayer())){CleanupAll();return;}
        if(SpawnBusy&&Application.isFocused&&!UIVisible()&&!menuOpen&&(Input.GetKeyDown(KeyCode.F8)||PadDown("back"))){CancelSpawn("Laden abgebrochen.");return;}
        TickVehicleRestore(local);
        TickSpawn(local);
        if(driving&&local is Player livePlayer&&livePlayer.IsDead()){Exit();return;}
        if(!driving&&!menuOpen)SelectNearest(local);
        audioBlocked=UIVisible()||!Application.isFocused||Time.timeScale<=0;
        if(audioBlocked){CloseVehicleMenu();BlockDrivingInput();return;}
        try {
            if(menuOpen && !CanUseVehicleMenu(local))CloseVehicleMenu();
            if((menuKey.Value.IsDown()||PadDown("menu")) && CanUseVehicleMenu(local)){SetVehicleMenu(!menuOpen);BlockDrivingInput();return;}
            if(menuOpen){TickControllerMenu();BlockDrivingInput(false);if(menuOpen)TickMenuCamera();return;}
            TickHorde(local);
            if(hordeWave.Active&&hordeWave.Choosing){BlockDrivingInput();TickHordePactInput();return;}
            if(Input.GetKeyDown(KeyCode.F6)) { if(Multiplayer()){status="Nur in einer lokalen Einzelspieler-Testwelt verwenden.";return;} Spawn(local); }
            if(Input.GetKeyDown(KeyCode.F8)&&SpawnBusy)CancelSpawn("Laden abgebrochen.");
            TickControlActions(local);
            if(driving&&!DriverInputReady){BlockDrivingInput();return;}
            if(driving)ReadVehicleControls();
            TickSongToast();
        }catch(Exception e){Logger.LogError(e);CleanupAll();status="Fehler – siehe BepInEx/LogOutput.log";}
    }
    Transform AddVisual(ModelPart p, Transform parent, ModelData model, Material paint, Material dark) {
        var obj=new GameObject(p.name);obj.transform.SetParent(parent,false);
        Mesh mesh;
        if(p.group!="audio"||!audioMeshes.TryGetValue(p,out mesh)) {
            mesh=new Mesh{name=p.name};vehicleMeshes.Add(mesh);
            mesh.vertices=p.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.triangles=p.indices;
            if(p.normals!=null)mesh.normals=p.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();else mesh.RecalculateNormals();
            mesh.uv=p.uv!=null?p.uv.Select(v=>new Vector2(v.u,v.v)).ToArray():new Vector2[p.vertices.Length];
            mesh.colors=Enumerable.Repeat(Color.white,p.vertices.Length).ToArray();
            // Audio membranes use the original vertex-to-model mapping.
            if(p.group!="audio")VehicleMesh.AddBackfaces(mesh);
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            if(p.group=="wheel")wheelSizeVisuals?.Track(mesh);
            if(p.group=="audio")audioMeshes.Add(p,mesh);
        }
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=model.schemaVersion==2?vehicleMaterials[p.material]:(p.name.Contains("BASE")?dark:paint);
        if(p.group=="wheel")renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
        return obj.transform;
    }
    void Enter(MonoBehaviour local) {
        if(Multiplayer())return;
        if(SpawnBusy){status="Bitte den Ladevorgang abwarten oder mit F8 abbrechen.";return;}
        if(Flying){status="Bitte warten, bis das Fahrzeug gelandet ist.";return;}
        if(local is Player native&&(native.IsDead()||native.IsAttached()||native.IsTeleporting())){status="Zum Einsteigen zuerst die aktuelle Aktion beenden.";return;}
        if(body.linearVelocity.magnitude>.5f){status="Zum Einsteigen muss das Fahrzeug stehen.";return;}
        current.parkedStreaming?.Dispose();current.parkedStreaming=null;
        Vector3 outside=Vector3.zero;
        if(current.interior!=null&&!FindCabinExit(local,out outside)){ShowVehicleWarning("Die Fahrertür ist blockiert. Bitte etwas Platz schaffen.");return;}
        CloseVehicleMenu();
        player=local;playerBody=player.GetComponent<Rigidbody>();if(playerBody==null){player=null;return;}
        lastExitPosition=player.transform.position;playerEnabled=player.enabled;playerKinematic=playerBody.isKinematic;
        playerRenderers=player.GetComponentsInChildren<Renderer>();renderStates=playerRenderers.Select(r=>r.enabled).ToArray();
        playerColliders=player.GetComponentsInChildren<Collider>();colliderStates=playerColliders.Select(c=>c.enabled).ToArray();
        if(current.interior!=null) {
            try {cabinDriver=new VehicleCabinDriver(local,car.transform,current.interior,outside);}
            catch(Exception e){Logger.LogWarning("Cabin driver unavailable: "+e);cabinDriver?.Dispose();cabinDriver=null;}
        }
        if(cabinDriver==null)foreach(var r in playerRenderers)r.enabled=false;
        foreach(var c in playerColliders)c.enabled=false;
        player.enabled=false;playerBody.isKinematic=true;
        var cameraType=playerType.Assembly.GetType("GameCamera");
        gameCamera=cameraType?.GetProperty("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as MonoBehaviour;
        if(gameCamera!=null){cameraEnabled=gameCamera.enabled;gameCamera.enabled=false;}
        orbitCamera.Begin(car.transform,IsHummer);
        driving=true;hasBeenDriven=true;
        current.worldStreaming=new VehicleWorldStreaming(body);
        VehicleWorldStreaming.Follow(player as Player);
        showroom?.State.CloseAll();
        nitroInput.Reset();nitroHeld=false;
    }
    void FixedUpdate() {ForEachVehicle(StepVehicle);}
    void StepVehicle() {
        if(body==null||wheels==null)return;
        if(!driving) {
            if(current.parkedStreaming==null)current.parkedStreaming=new VehicleWorldStreaming(body);
            if(!current.parkedStreaming.Step(false))return;
        }
        if(driving) {
            VehicleWorldStreaming.Follow(player as Player);
            if(current.worldStreaming!=null) {
                bool waiting=current.worldStreaming.Waiting;
                if(!current.worldStreaming.Step()) {
                    if(!waiting) {
                        Logger.LogInfo("Vehicle waiting for world streaming at "+body.position);
                    }
                    if(current.worldStreaming.TakeNotice())ShowVehicleWarning("Welt wird geladen. Fahrzeug wartet kurz.");
                    return;
                }
                if(waiting) {
                    Logger.LogInfo("Vehicle world streaming ready; movement resumed.");
                    if(status=="Welt wird geladen. Fahrzeug wartet kurz.")status=VehicleName;
                }
            }
        }
        if(!Flying)current.rideHeight?.Apply(lowering.Value);
        if(vehicleImpact!=null)vehicleImpact.EnemyDamageMultiplier=HordeVehicle?HordePacts.Impact(hordeRun.Pact):1;
        vehicleImpact?.Step(player,driving&&!Flying&&!audioBlocked&&!menuOpen&&Application.isFocused&&(!HordeVehicle||!hordeWave.Choosing),
            DriverInputReady&&brake<=0&&!handbrakeHeld&&!burnoutHeld?throttle:0);
        if(Flying) {
            current.flight.Step(driving&&DriverInputReady&&!audioBlocked&&!menuOpen&&Application.isFocused,throttle,steer,current.flightVertical,nitroHeld&&nitroEnabled.Value);
            TraceFlightBoost();
            if(!Flying){if(driving)BlockDrivingInput();ApplyDriveMode();}
        } else {
            bool handbrake=EffectiveDrift&&driving&&handbrakeHeld&&!audioBlocked&&!menuOpen&&Application.isFocused;
            handling?.Step(driving&&DriverInputReady,throttle,steer,handbrake?0:brake,burnoutHeld&&!audioBlocked&&!menuOpen&&Application.isFocused,handbrake);
            // Scale controller pedal braking after the unchanged wheel handler.
            if(padActive&&driving&&DriverInputReady&&!audioBlocked&&!menuOpen&&!handbrake&&brake>0)
                foreach(var wheel in wheels)wheel.brakeTorque*=brake;
            if(padActive&&driving&&DriverInputReady&&!audioBlocked&&!menuOpen&&handbrakeHeld&&!EffectiveDrift) {
                for(int i=0;i<4;i++){wheels[i].motorTorque=0;wheels[i].brakeTorque=i<2?0:4200;}
            }
            if(IsHummer)HummerTuning.AfterStep(wheels);
            vehicleNitro?.Step(driving,audioBlocked||menuOpen||!DriverInputReady||burnoutHeld||!Application.isFocused||!nitroEnabled.Value,nitroHeld,throttle,brake>0,EffectiveTerrain);
        }
        if(driving&&player!=null&&cabinDriver==null)player.transform.position=car.transform.TransformPoint(new Vector3(0,.8f,.3f));
    }
    void LateUpdate() {ForEachVehicle(TickVehicle);LateMenuCamera();TickNitroVisuals();UpdateGameCrosshair();UpdateVehicleHints();TickHordeRadar();fleetMusic?.Tick(driving&&!audioBlocked&&vehicleRadio!=null&&vehicleRadio.CabinAudible);}
    void TickVehicle() {
        if(car==null)return;
        TickVehicleStorage();
        TickExhaustCustomization();
        if(showroom!=null)try {
            ApplyShowroomSettings();
            int color=Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value));
            float showroomDt=driving&&cabinDriver!=null&&(audioBlocked||!Application.isFocused||menuOpen&&!DriverInputReady)?0:Time.deltaTime;
            int activeDoorStyle=driving&&cabinDriver!=null&&!DriverInputReady?showroom.State.DoorStyle:doorStyle.Value;
            showroom.Tick(showroomDt,Flying||body.linearVelocity.magnitude>.5f,activeDoorStyle,driving||ParkedRunning,trunkNeonEnabled.Value,trunkNeonPulsing.Value,NeonColors[color]);
        }
        catch(Exception e){showroom.Dispose();showroom=null;Logger.LogWarning("350Z showroom disabled: "+e);}
        current.interior?.Tick(wheels!=null?wheels[0].steerAngle:0,windowTint.Value,driving&&cabinDriver!=null);
        if(driving&&cabinDriver!=null)try {
            if(showroom==null){Exit();return;}
            cabinDriver.Tick(Time.deltaTime,audioBlocked||menuOpen||!Application.isFocused,showroom.State);
            if(cabinDriver.Failed){Exit();ShowVehicleWarning("Weg zur Fahrertür blockiert.");return;}
            if(cabinDriver.Motion.Phase==CabinPhase.Complete){
                if(!FindCabinExit(player,out var clearExit)){cabinDriver.Motion.CancelExit();ShowVehicleWarning("Ausstieg inzwischen blockiert.");}
                else {cabinDriver.SetExitPosition(clearExit);Exit();return;}
            }
        } catch(Exception e){Logger.LogError("Cabin animation stopped: "+e);Exit();return;}
        if(driving)VehicleWorldStreaming.Follow(player as Player);
        ApplyNeonSettings();
        bool burnout=!Flying&&driving&&burnoutHeld&&handling!=null&&handling.BurnoutActive&&!audioBlocked&&!menuOpen&&Application.isFocused;
        wheelSizeVisuals?.Apply(IsHummer?h2RimInches.Value:rimInches.Value);
        if(wheels!=null && visuals!=null)for(int i=0;i<4;i++)if(wheels[i]!=null && visuals[i]!=null){
            UpdateWheelVisual(i);
        }
        TickFlight();
        if(driving)orbitCamera.LateFrame(body.linearVelocity,dynamicCamera.Value,cameraFollow.Value,audioBlocked||menuOpen,!Flying&&turret!=null&&(HordeVehicle||turretEnabled.Value));
        TickTurret();
        if(vehicleLighting!=null)try {
            vehicleLighting.SetHeadlights(headlightPreset.Value,headlightStrength.Value);
            vehicleLighting.ShowOffEnabled=headlightShowOff.Value;
            float speed=Vector3.Dot(body.linearVelocity,car.transform.forward);
            vehicleLighting.Tick(driving,driving&&(burnoutHeld||brake>0||DrivingMath.DirectionBrake(speed,throttle)),speed,burnout?1:throttle,Time.deltaTime,!Application.isFocused||Time.timeScale<=0);
        }
        catch(Exception e){vehicleLighting.Dispose();vehicleLighting=null;Logger.LogWarning("350Z lighting disabled: "+e);}
        if(vehicleNeon!=null)try {vehicleNeon.Tick(driving||ParkedRunning,Time.deltaTime);}
        catch(Exception e){vehicleNeon.Dispose();vehicleNeon=null;Logger.LogWarning("350Z neon disabled: "+e);}
        if(vehicleRadio!=null)try {
            // NFS RIGHT maps to Unity -X (the physical left door), as in the menu.
            vehicleRadio.Tick(driving,audioBlocked,radioVolume.Value,radioEnabled.Value,ParkedRunning,
                showroom?.State.Progress[1]??0,showroom?.State.Progress[0]??0,showroom?.State.Progress[3]??0);
        }
        catch(Exception e){vehicleRadio.Dispose();vehicleRadio=null;Logger.LogWarning("350Z radio disabled: "+e);}
        if(showroom!=null)try {showroom.TickSubwoofers(vehicleRadio?.SubwooferOffset??0);}
        catch(Exception e){showroom.Dispose();showroom=null;Logger.LogWarning("350Z subwoofer visuals disabled: "+e);}
        if(vehicleAudio!=null)try {
            vehicleAudio.Tick(driving,throttle,brake>0||burnoutHeld&&!burnout,honking,audioBlocked,EffectiveTerrain,soundVolume.Value,burnout,ParkedRunning,Flying,!menuOpen&&DriverInputReady);
        } catch(Exception e) {
            vehicleAudio.Dispose();vehicleAudio=null;Logger.LogWarning("350Z sound disabled after audio error: "+e);
        }
        bool effectsBlocked=!driving||!DriverInputReady||audioBlocked||menuOpen||!Application.isFocused;
        if(burnoutEffects!=null)try {burnoutEffects.Tick(burnout,Flying||effectsBlocked,EffectiveDrift&&body!=null&&body.linearVelocity.magnitude>3);}
        catch(Exception e){burnoutEffects.Dispose();burnoutEffects=null;Logger.LogWarning("350Z tyre smoke disabled: "+e);}
        bool backfire=backfireState.Step(Time.deltaTime,!Flying&&!effectsBlocked,
            vehicleAudio!=null?vehicleAudio.Rpm:950,throttle,
            body!=null?Vector3.Dot(body.linearVelocity,body.transform.forward):0,backfireEnabled.Value);
        // Exhaust follows actual boost/fuel consumption in either driving mode.
        // Wheel thrusters keep their separate hover loop; purge/backfire stay on the ground.
        bool boost=nitroEnabled.Value&&ActiveNitro!=null&&ActiveNitro.Boosting;
        bool purge=!Flying&&nitroEnabled.Value&&vehicleNitro!=null&&vehicleNitro.State.Purging;
        if(nitroEffects!=null)try {nitroEffects.Tick(boost,purge,backfire,effectsBlocked,Time.deltaTime);}
        catch(Exception e){nitroEffects.Dispose();nitroEffects=null;Logger.LogWarning("350Z effects disabled: "+e);}
        if(effectsAudio!=null)try {effectsAudio.Tick(boost,purge,backfire,effectsBlocked,soundVolume.Value);}
        catch(Exception e){effectsAudio.Dispose();effectsAudio=null;Logger.LogWarning("350Z effect sounds disabled: "+e);}
    }
    bool FindCabinExit(MonoBehaviour local,out Vector3 position) {
        // Grounded capsule beside the physical left door; never put the player inside a wall.
        foreach(float z in new[]{-.12f,-.48f,.22f}) {
            var origin=car.transform.TransformPoint(new Vector3(-1.3f,2.2f,z));
            foreach(var hit in Physics.RaycastAll(origin,Vector3.down,5,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)) {
                if(hit.collider.transform.IsChildOf(car.transform)||hit.collider.transform.IsChildOf(local.transform)||hit.normal.y<.65f)continue;
                var foot=hit.point+Vector3.up*.06f;
                bool occupied=Physics.OverlapCapsule(foot+Vector3.up*.32f,foot+Vector3.up*1.5f,.27f,~0,QueryTriggerInteraction.Ignore)
                    .Any(c=>!c.transform.IsChildOf(local.transform));
                if(occupied)break;position=foot;return true;
            }
        }
        position=Vector3.zero;return false;
    }
    void RequestExit() {
        if(Flying){status="Zum Aussteigen unter F9 einen Bodenmodus wählen und landen.";return;}
        if(cabinDriver==null){Exit();return;}
        if(!cabinDriver.Motion.CanDrive)return;
        if(body.linearVelocity.magnitude>.5f){status="Zum Aussteigen erst anhalten.";return;}
        if(!FindCabinExit(player,out var destination)){ShowVehicleWarning("Die Fahrertür ist blockiert. Bitte anders parken.");return;}
        if(HordeVehicle)ClearHorde();
        cabinDriver.BeginExit(destination);BlockDrivingInput();
    }
    void Exit() {
        current.worldStreaming?.Dispose();current.worldStreaming=null;
        if(HordeVehicle)ClearHorde();
        ReleaseCameraRumble(gameCamera);
        exitAfterLanding=false;controlsGate.Block();
        ClearNitroVisuals();
        bool animated=cabinDriver!=null&&cabinDriver.Motion.Phase==CabinPhase.Complete;
        var animatedExit=animated?cabinDriver.ExitPosition:lastExitPosition;
        bool hadCabin=cabinDriver!=null;
        cabinDriver?.Dispose();cabinDriver=null;
        current.interior?.SetDriverWindowOpen(false);
        showroom?.State.CloseAll();
        RestoreGameCrosshair();
        CloseVehicleMenu();
        driving=false;RestoreVehicleHints();throttle=steer=0;brake=1;honking=false;burnoutHeld=false;
        current.flightVertical=0;current.flight?.Land();
        vehicleImpact?.Step(null,false);
        nitroHeld=false;nitroInput.Reset();vehicleNitro?.State.Stop();
        orbitCamera?.End();
        if(player!=null){player.transform.position=animatedExit;
            if(hadCabin&&!animated&&car!=null&&FindCabinExit(player,out var emergency))player.transform.position=emergency;
            else if(!hadCabin&&car!=null&&!Flying){var candidate=car.transform.TransformPoint(new Vector3(2.5f,2,0));if(Physics.Raycast(candidate,Vector3.down,out var hit,10,~0,QueryTriggerInteraction.Ignore))player.transform.position=hit.point+Vector3.up*.3f;}
            player.transform.rotation=Quaternion.Euler(0,player.transform.eulerAngles.y,0);
            for(int i=0;playerRenderers!=null&&i<playerRenderers.Length;i++)if(playerRenderers[i]!=null)playerRenderers[i].enabled=renderStates[i];
            for(int i=0;playerColliders!=null&&i<playerColliders.Length;i++)if(playerColliders[i]!=null)playerColliders[i].enabled=colliderStates[i];
            if(playerBody!=null){playerBody.isKinematic=playerKinematic;if(!playerKinematic){playerBody.linearVelocity=Vector3.zero;playerBody.angularVelocity=Vector3.zero;}}player.enabled=playerEnabled;}
        VehicleWorldStreaming.Follow(player as Player);
        if(gameCamera!=null)gameCamera.enabled=cameraEnabled;
        player=null;gameCamera=null;status="F6: Fahrzeug erstellen | F7: einsteigen | F9: Garage";
    }
    void Cleanup(){
        bool cargoLeft=current.storage!=null&&current.storage.Count!=0;
        if(driving)Exit();
        RestoreGameCrosshair();
        CloseVehicleMenu();
        ReleaseVehicle(true);
        if(cargoLeft)ShowVehicleWarning("Der Inhalt bleibt als Kiste am Fahrzeugstandort.");
    }
    void ReleaseVehicle(bool removeVehicle=false){
        current.worldStreaming?.Dispose();current.worldStreaming=null;
        current.parkedStreaming?.Dispose();current.parkedStreaming=null;
        current.storage?.Release(removeVehicle);current.storage=null;
        current.flight?.Dispose();current.flight=null;
        current.flightEffects?.Dispose();current.flightEffects=null;
        current.flightAudio?.Dispose();current.flightAudio=null;
        turret?.Dispose();turret=null;
        dashboard?.Dispose();dashboard=null;
        current.interior?.Dispose();current.interior=null;
        showroom?.Dispose();showroom=null;audioMeshes.Clear();
        burnoutHeld=false;burnoutEffects?.Dispose();burnoutEffects=null;
        nitroHeld=false;nitroInput.Reset();vehicleNitro=null;
        nitroEffects?.Dispose();nitroEffects=null;
        effectsAudio?.Dispose();effectsAudio=null;
        vehicleRadio?.Dispose();vehicleRadio=null;
        vehicleLighting?.Dispose();vehicleLighting=null;
        vehicleNeon?.Dispose();vehicleNeon=null;
        vehicleAudio?.Dispose();vehicleAudio=null;
        if(car!=null){car.SetActive(false);Destroy(car);}
        hasBeenDriven=false;vehicleImpact=null;
        foreach(var mesh in vehicleMeshes)if(mesh!=null)Destroy(mesh);vehicleMeshes.Clear();
        wheelSizeVisuals=null;
        if(vehicleMaterials!=null)foreach(var material in vehicleMaterials)if(material!=null)Destroy(material);
        if(vehicleTextures!=null)foreach(var texture in vehicleTextures)if(texture!=null)Destroy(texture);
        if(chassisMaterial!=null)Destroy(chassisMaterial);
        if(fleet.Contains(current))fleet.Remove(current);
        handling=null;chassisMaterial=null;vehicleMaterials=null;vehicleTextures=null;car=null;body=null;wheels=null;visuals=null;brakeVisuals=null;originalWheels=false;
    }
    void ShowVehicleWarning(string message) {
        status=message;
        if(MessageHud.instance!=null)MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,Tr(message),log:false);
    }
    void OnDestroy(){ReleaseCameraRumble(gameCamera);ReleaseCameraRumble(menuCamera);controlsHarmony?.UnpatchSelf();if(ControlsOwner==this)ControlsOwner=null;ClearNitroVisuals();CleanupAll();hordeWeather?.Dispose();hordeWeather=null;DisposeVehicleHints();fleetMusic?.Dispose();DisposeMenu();}
}
}
