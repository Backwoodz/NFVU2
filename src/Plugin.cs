using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
// Disposable local prototype. No ZNetView: never use in multiplayer or persist into saves.
[BepInPlugin("local.rachel350z.prototype", "350Z vehicle prototype", "0.1.5")]
public class Plugin : BaseUnityPlugin {
    GameObject car; Rigidbody body; WheelCollider[] wheels; Transform[] visuals;
    Material[] vehicleMaterials;
    PhysicsMaterial chassisMaterial;
    VehicleHandling handling;
    VehicleOrbitCamera orbitCamera;
    VehicleAudio vehicleAudio;
    bool audioBlocked, honking;
    ConfigEntry<bool> terrainMode, invertMouseY;
    ConfigEntry<float> mouseSensitivity, soundVolume;
    readonly System.Collections.Generic.List<Mesh> vehicleMeshes = new System.Collections.Generic.List<Mesh>();
    MonoBehaviour player, gameCamera; Rigidbody playerBody; bool playerEnabled, cameraEnabled, playerKinematic;
    Renderer[] playerRenderers; bool[] renderStates; Collider[] playerColliders; bool[] colliderStates;
    bool driving; string status = "F6: 350Z erstellen | F7: einsteigen | F8: entfernen";
    Vector3 lastExitPosition; float steer, throttle, brake;
    Type playerType, netType; bool ready;
    void Awake() {
        var asm=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetType("Player")!=null);
        playerType=asm?.GetType("Player");netType=asm?.GetType("ZNet");
        terrainMode=Config.Bind("Driving","TerrainMode",true,"F9 toggles terrain suspension, traction and front-wheel assistance. Off uses rear-wheel drive.");
        mouseSensitivity=Config.Bind("Camera","MouseSensitivity",.15f,new ConfigDescription("Orbit sensitivity in degrees per mouse pixel.",new AcceptableValueRange<float>(.02f,1f)));
        invertMouseY=Config.Bind("Camera","InvertMouseY",false,"Invert vertical mouse look while driving.");
        soundVolume=Config.Bind("Audio","Volume",.8f,new ConfigDescription("Vehicle sound volume. 0 mutes engine, tyres and horn; 1 is full volume.",new AcceptableValueRange<float>(0,1)));
        orbitCamera=new VehicleOrbitCamera(message=>Logger.LogInfo(message));
        ready=playerType!=null;Logger.LogInfo("350Z prototype loaded. Single-player, temporary vehicle only.");
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
        var local=LocalPlayer();
        if(local==null){Cleanup();return;}
        if(driving && (local!=player || Multiplayer())){Cleanup();return;}
        audioBlocked=UIVisible()||!Application.isFocused;
        if(audioBlocked){throttle=steer=0;brake=1;honking=false;orbitCamera.InputFrame(false,mouseSensitivity.Value,invertMouseY.Value);return;}
        try {
            if(Input.GetKeyDown(KeyCode.F6)) { if(Multiplayer()){status="Nur in einer lokalen Einzelspieler-Testwelt verwenden.";return;} Spawn(local); }
            if(Input.GetKeyDown(KeyCode.F8))Cleanup();
            if(Input.GetKeyDown(KeyCode.F9) && handling!=null) {
                terrainMode.Value=!terrainMode.Value;handling.SetTerrainMode(terrainMode.Value);
                status="F9: "+(terrainMode.Value?"Geländemodus mit Allradhilfe":"Straßenmodus mit Heckantrieb")+" | F7: einsteigen";
                Logger.LogInfo("350Z terrain assistance: "+terrainMode.Value);
            }
            if(Input.GetKeyDown(KeyCode.F7)) { if(driving)Exit();else if(car!=null && Vector3.Distance(local.transform.position,car.transform.position)<5)Enter(local); }
            if(driving){throttle=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);steer=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);brake=Input.GetKey(KeyCode.Space)?1:0;honking=Input.GetKey(KeyCode.H);orbitCamera.InputFrame(true,mouseSensitivity.Value,invertMouseY.Value);}
        }catch(Exception e){Logger.LogError(e);Cleanup();status="Fehler – siehe BepInEx/LogOutput.log";}
    }
    void Spawn(MonoBehaviour local) {
        Cleanup();
        var path=Path.Combine(Path.GetDirectoryName(Info.Location),"body.json");
        var model=ModelFile.Load(path);
        Logger.LogInfo("350Z model loaded: "+model.parts.Length+" parts, "+model.parts.Sum(p=>p.vertices.Length)+" vertices, "+model.parts.Sum(p=>p.indices.Length/3)+" triangles. Source: "+path);
        Vector3 origin=local.transform.position+local.transform.forward*5+Vector3.up*3;
        if(Physics.Raycast(origin,Vector3.down,out var hit,15,~0,QueryTriggerInteraction.Ignore))origin=hit.point+Vector3.up*.7f;
        car=new GameObject("350Z_Local_Prototype");car.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,local.transform.eulerAngles.y,0));
        vehicleMaterials=VehicleMaterials.Create(local, message=>Logger.LogInfo(message));
        var paint=vehicleMaterials[0];var dark=vehicleMaterials[1];
        foreach(var p in model.parts) {
            var obj=new GameObject(p.name);obj.transform.SetParent(car.transform,false);
            var mesh=new Mesh{name=p.name,vertices=p.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray(),triangles=p.indices};vehicleMeshes.Add(mesh);mesh.RecalculateNormals();mesh.RecalculateBounds();
            // Game shaders may read UV0, tangents and vertex colour even for a solid tint.
            mesh.uv=new Vector2[p.vertices.Length];
            mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,1),p.vertices.Length).ToArray();
            mesh.colors=Enumerable.Repeat(Color.white,p.vertices.Length).ToArray();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=p.name.Contains("BASE")?dark:paint;
        }
        // A shorter, raised hull catches fewer small rocks with its bumper corners.
        var box=car.AddComponent<BoxCollider>();box.center=new Vector3(0,.76f,0);box.size=new Vector3(1.62f,.8f,3.8f);
        chassisMaterial=new PhysicsMaterial("350Z_Chassis"){dynamicFriction=.08f,staticFriction=.08f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum};box.sharedMaterial=chassisMaterial;
        body=car.AddComponent<Rigidbody>();body.mass=1450;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        wheels=new WheelCollider[4];visuals=new Transform[4];
        for(int i=0;i<4;i++) {
            var wheel=new GameObject("Wheel"+i);wheel.transform.SetParent(car.transform,false);wheel.transform.localPosition=new Vector3(i%2==0?-.8f:.8f,.34f,i<2?1.3f:-1.35f);
            var wc=wheel.AddComponent<WheelCollider>();wc.radius=.34f;wheels[i]=wc;
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(visual.GetComponent<Collider>());visual.transform.SetParent(car.transform,false);visual.transform.localScale=new Vector3(.68f,.12f,.68f);visual.GetComponent<Renderer>().sharedMaterial=dark;visuals[i]=visual.transform;
        }
        handling=new VehicleHandling(body,wheels,terrainMode.Value,message=>Logger.LogInfo(message));
        vehicleAudio=VehicleAudio.Create(car,body,wheels,message=>Logger.LogInfo(message));
        Logger.LogInfo("350Z spawned: "+model.parts.Length+" meshes, 4 wheels. Terrain assistance: "+terrainMode.Value);
        status="350Z bereit. F7: einsteigen | F9: Gelände/Straße | F8: entfernen";
    }
    void Enter(MonoBehaviour local) {
        if(Multiplayer())return;
        player=local;playerBody=player.GetComponent<Rigidbody>();if(playerBody==null){player=null;return;}
        lastExitPosition=player.transform.position;playerEnabled=player.enabled;playerKinematic=playerBody.isKinematic;
        playerRenderers=player.GetComponentsInChildren<Renderer>();renderStates=playerRenderers.Select(r=>r.enabled).ToArray();
        playerColliders=player.GetComponentsInChildren<Collider>();colliderStates=playerColliders.Select(c=>c.enabled).ToArray();
        foreach(var r in playerRenderers)r.enabled=false;foreach(var c in playerColliders)c.enabled=false;
        player.enabled=false;playerBody.isKinematic=true;
        var cameraType=playerType.Assembly.GetType("GameCamera");
        gameCamera=cameraType?.GetProperty("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as MonoBehaviour;
        if(gameCamera!=null){cameraEnabled=gameCamera.enabled;gameCamera.enabled=false;}
        orbitCamera.Begin(car.transform);
        driving=true;
    }
    void FixedUpdate() {
        if(body==null||wheels==null)return;
        handling?.Step(driving,throttle,steer,brake);
        if(driving&&player!=null)player.transform.position=car.transform.TransformPoint(new Vector3(0,.8f,.3f));
    }
    void LateUpdate() {
        if(wheels!=null && visuals!=null)for(int i=0;i<4;i++)if(wheels[i]!=null && visuals[i]!=null){wheels[i].GetWorldPose(out var pos,out var rot);visuals[i].SetPositionAndRotation(pos,rot*Quaternion.Euler(0,0,90));}
        if(driving)orbitCamera.LateFrame();
        if(vehicleAudio!=null)try {
            vehicleAudio.Tick(driving,throttle,brake>0,honking,audioBlocked,terrainMode.Value,soundVolume.Value);
        } catch(Exception e) {
            vehicleAudio.Dispose();vehicleAudio=null;Logger.LogWarning("350Z sound disabled after audio error: "+e);
        }
    }
    void Exit() {
        driving=false;throttle=steer=0;brake=1;honking=false;
        orbitCamera?.End();
        if(player!=null){player.transform.position=lastExitPosition;
            if(car!=null){var candidate=car.transform.TransformPoint(new Vector3(2.5f,2,0));if(Physics.Raycast(candidate,Vector3.down,out var hit,10,~0,QueryTriggerInteraction.Ignore))player.transform.position=hit.point+Vector3.up*.3f;}
            for(int i=0;playerRenderers!=null&&i<playerRenderers.Length;i++)if(playerRenderers[i]!=null)playerRenderers[i].enabled=renderStates[i];
            for(int i=0;playerColliders!=null&&i<playerColliders.Length;i++)if(playerColliders[i]!=null)playerColliders[i].enabled=colliderStates[i];
            if(playerBody!=null)playerBody.isKinematic=playerKinematic;player.enabled=playerEnabled;}
        if(gameCamera!=null)gameCamera.enabled=cameraEnabled;
        player=null;gameCamera=null;status="F6: 350Z erstellen | F7: einsteigen | F8: entfernen";
    }
    void Cleanup(){
        vehicleAudio?.Dispose();vehicleAudio=null;
        if(player!=null||driving)Exit();if(car!=null)Destroy(car);
        foreach(var mesh in vehicleMeshes)if(mesh!=null)Destroy(mesh);vehicleMeshes.Clear();
        if(vehicleMaterials!=null)foreach(var material in vehicleMaterials)if(material!=null)Destroy(material);
        if(chassisMaterial!=null)Destroy(chassisMaterial);
        orbitCamera?.End();handling=null;chassisMaterial=null;vehicleMaterials=null;car=null;body=null;wheels=null;visuals=null;
    }
    void OnDestroy(){Cleanup();}
    void OnGUI(){if(ready&&LocalPlayer()!=null){
        var text=driving?(terrainMode.Value?"Gelände / Allradhilfe":"Straße / Heckantrieb")+" | WASD: fahren | Leertaste: bremsen | H: Hupe | F7: aussteigen\nMaus: umschauen | Mausrad: Zoom | Mittelklick: zentrieren | F9: Fahrmodus":status;
        GUI.Box(new Rect(15,Screen.height-80,Mathf.Min(920,Screen.width-30),60),text);
    }}
}
}
