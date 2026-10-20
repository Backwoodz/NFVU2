using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

// These checks inspect the installed API and built production IL. They catch
// Harmony binding failures and missing coordinator hooks that pure policies
// cannot detect; they are not a substitute for a real saved-world load.
static class ArrivalNativeContracts {
    static TypeDefinition Type(AssemblyDefinition assembly,string name)=>assembly.MainModule.Types.Single(t=>t.Name==name);
    static MethodDefinition Method(TypeDefinition type,string name)=>type.Methods.Single(m=>m.Name==name);
    static bool Calls(MethodDefinition method,string type,string name)=>method.Body.Instructions.Any(i=>i.Operand is MethodReference call&&call.DeclaringType.Name==type&&call.Name==name);
    static int CallIndex(MethodDefinition method,string type,string name,int skip=0)=>method.Body.Instructions
        .Select((i,n)=>new{Instruction=i,Index=n})
        .Where(x=>x.Instruction.Operand is MethodReference call&&call.DeclaringType.Name==type&&call.Name==name).Skip(skip).Select(x=>x.Index).DefaultIfEmpty(-1).First();
    static void Parameters(MethodDefinition method,Action<bool,string> check,params string[] expected) {
        check(method.Parameters.Count==expected.Length,method.FullName+" parameter count matches installed native contract");
        for(int i=0;i<expected.Length;i++)check(method.Parameters[i].Name+":"+method.Parameters[i].ParameterType.FullName==expected[i],method.FullName+" parameter "+i+" binds by its installed name/type");
    }
    internal static void Run(string gamePath,string pluginPath,Action<bool,string> check) {
        using(var native=AssemblyDefinition.ReadAssembly(Path.Combine(gamePath,"valheim_Data/Managed/assembly_valheim.dll")))
        using(var built=AssemblyDefinition.ReadAssembly(pluginPath)) {
            var game=Type(native,"Game");var find=Method(game,"FindSpawnPoint");var spawn=Method(game,"SpawnPlayer");
            check(find.IsPrivate&&!find.IsStatic&&find.ReturnType.FullName=="System.Boolean","FindSpawnPoint remains one private instance bool method");
            Parameters(find,check,"point:UnityEngine.Vector3&","usedLogoutPoint:System.Boolean&","dt:System.Single");
            check(find.Parameters[0].IsOut&&find.Parameters[1].IsOut,"FindSpawnPoint output parameters remain native out values");
            Parameters(spawn,check,"spawnPoint:UnityEngine.Vector3","spawnValkyrie:System.Boolean");
            check(spawn.IsPrivate&&!spawn.IsStatic&&spawn.ReturnType.FullName=="Player","SpawnPlayer remains a private Player-returning instance method");
            foreach(var contract in new[]{new[]{"m_firstSpawn","System.Boolean"},new[]{"m_respawnWait","System.Single"},new[]{"m_respawnLoadDuration","System.Single"}})
                check(game.Fields.Any(f=>f.Name==contract[0]&&f.FieldType.FullName==contract[1]&&!f.IsStatic),"Harmony field injection exists: Game."+contract[0]);
            var updateRespawn=Method(game,"UpdateRespawn");
            int findCall=CallIndex(updateRespawn,"Game","FindSpawnPoint"),spawnCall=CallIndex(updateRespawn,"Game","SpawnPlayer");
            check(findCall>=0&&spawnCall>findCall,"native respawn resolves spawn readiness before creating Player");
            var firstSpawnWrite=updateRespawn.Body.Instructions.Select((i,n)=>new{Instruction=i,Index=n})
                .Where(x=>x.Instruction.OpCode.Code==Mono.Cecil.Cil.Code.Stfld&&x.Instruction.Operand is FieldReference field&&field.Name=="m_firstSpawn").Select(x=>x.Index).DefaultIfEmpty(-1).First();
            check(firstSpawnWrite>spawnCall,"native world-entry flag remains true while SpawnPlayer prefix suppresses intro");
            var scene=Type(native,"ZNetScene");var area=Method(scene,"IsAreaReady");
            check(area.IsPublic&&area.ReturnType.FullName=="System.Boolean"&&area.Parameters.Count==1&&area.Parameters[0].ParameterType.FullName=="UnityEngine.Vector3","native scene readiness accepts a world position");
            var zone=Type(native,"ZoneSystem");
            check(zone.Methods.Any(m=>m.Name=="GetGroundHeight"&&m.IsPublic&&m.ReturnType.FullName=="System.Boolean"&&m.Parameters.Count==2&&m.Parameters[0].ParameterType.FullName=="UnityEngine.Vector3"&&m.Parameters[1].IsOut&&m.Parameters[1].ParameterType.FullName=="System.Single&"),"native terrain readiness provides actual loaded ground height");
            var net=Type(native,"ZNet");
            check(net.Methods.Any(m=>m.Name=="SetReferencePosition"&&m.IsPublic&&m.Parameters.Count==1&&m.Parameters[0].ParameterType.FullName=="UnityEngine.Vector3"),"native streaming reference can move to the destination");
            var nativeMenuShow=Method(Type(native,"Menu"),"Show");
            check(nativeMenuShow.IsPublic&&!nativeMenuShow.IsStatic&&nativeMenuShow.ReturnType.FullName=="System.Void"&&nativeMenuShow.Parameters.Count==0,"native pause menu Show is a public parameterless instance method");
            foreach(var target in new[]{new[]{"DriftParkingWorldEntry","FindSpawnPoint"},new[]{"DriftParkingIntro","SpawnPlayer"}}) {
                var patch=Type(built,target[0]);
                check(patch.CustomAttributes.Any(a=>a.AttributeType.FullName=="HarmonyLib.HarmonyPatch"&&a.ConstructorArguments.Count==2
                    &&a.ConstructorArguments[0].Value is TypeReference patchType&&patchType.Name=="Game"
                    &&(string)a.ConstructorArguments[1].Value==target[1]),"built Harmony patch targets native Game."+target[1]);
                check(Method(patch,"Prefix").IsStatic,"Harmony entry prefix is static: "+target[0]);
            }
            Parameters(Method(Type(built,"DriftParkingWorldEntry"),"Prefix"),check,
                "___m_firstSpawn:System.Boolean","___m_respawnWait:System.Single&","___m_respawnLoadDuration:System.Single","dt:System.Single",
                "point:UnityEngine.Vector3&","usedLogoutPoint:System.Boolean&","__result:System.Boolean&");
            Parameters(Method(Type(built,"DriftParkingIntro"),"Prefix"),check,"___m_firstSpawn:System.Boolean","spawnValkyrie:System.Boolean&");
            var plugin=Type(built,"Plugin");
            check(Calls(Method(plugin,"InitializeDriftWorld"),"Plugin","InitializeDriftArrival"),"built world initialization registers arrival patches");
            var initialize=Method(plugin,"InitializeDriftArrival");
            foreach(string name in new[]{"DriftParkingWorldEntry","DriftParkingIntro","DriftEventNativeMenu"})
                check(initialize.Body.Instructions.Any(i=>i.Operand is TypeReference type&&type.Name==name),"built patch registration includes "+name);
            var menuPatch=Type(built,"DriftEventNativeMenu");
            check(menuPatch.CustomAttributes.Any(a=>a.AttributeType.FullName=="HarmonyLib.HarmonyPatch"&&a.ConstructorArguments.Count==2
                &&a.ConstructorArguments[0].Value is TypeReference menuType&&menuType.Name=="Menu"&&(string)a.ConstructorArguments[1].Value=="Show"),"event input patch targets native Menu.Show only");
            check(Calls(Method(menuPatch,"Prefix"),"Plugin","CaptureDriftEventMenu"),"native pause suppression delegates to live event-UI ownership");
            var capture=Method(plugin,"CaptureDriftEventMenu");
            check(Calls(capture,"DriftSessionRules","get_Active")&&Calls(capture,"Plugin","Multiplayer")&&Calls(capture,"Game","IsShuttingDown"),"pause suppression excludes other worlds, multiplayer and shutdown");
            foreach(string fieldName in new[]{"driftRaceUi","driftEventGame","driftEventWorld","driftEventVehicle"})
                check(capture.Body.Instructions.Any(i=>i.Operand is FieldReference field&&field.Name==fieldName),"pause suppression verifies current event ownership: "+fieldName);
            foreach(string methodName in new[]{"IsDead","IsTeleporting"})
                check(Calls(capture,"Player",methodName)||Calls(capture,"Character",methodName),"pause suppression releases invalid player state: "+methodName);
            var update=Method(plugin,"Update");
            int adopt=CallIndex(update,"Plugin","AdoptVehicleWorldEntry"),restore=CallIndex(update,"Plugin","TickVehicleRestore"),spawnTick=CallIndex(update,"Plugin","TickSpawn",1),arrivalTick=CallIndex(update,"Plugin","TickDriftArrival",1),eventTick=CallIndex(update,"Plugin","TickDriftEvents");
            check(adopt>=0&&restore>adopt&&spawnTick>restore&&arrivalTick>spawnTick,"built Update adopts preload owner, restores and completes staged car before boarding arrival");
            check(CallIndex(update,"Plugin","TickDriftArrival")<adopt,"built Update also observes arrival during player-null/menu cleanup frames");
            check(eventTick>arrivalTick,"race events run after initial parking boarding");
            check(eventTick>=0&&CallIndex(update,"Plugin","SetVehicleMenu")>eventTick&&CallIndex(update,"Plugin","TickControllerMenu")>eventTick,"event UI owns Update before ordinary garage toggle/controller-menu handling");
            var practiceScore=Method(plugin,"TickDriftCircuitScore");
            check(CallIndex(practiceScore,"Plugin","get_DriftRaceOwnsVehicle")>=0&&CallIndex(practiceScore,"Plugin","get_DriftRaceOwnsVehicle")<CallIndex(practiceScore,"DriftLapScore","Step"),"endless practice scoring checks event ownership before counting any drift sample");
            check(Calls(Method(plugin,"get_DriftRaceOwnsVehicle"),"DriftRaceRun","get_UsesRaceTrack"),"practice exclusion follows the event state machine's actual race-track ownership");
            var events=Method(plugin,"TickDriftEvents");
            check(Calls(events,"DriftRaceRun","Step")&&!Calls(events,"DriftLapScore","Step"),"event samples enter the separate three-lap race coordinator");
            var hud=Method(plugin,"TickDriftCircuitHud");
            check(Calls(hud,"DriftRaceRun","get_Score")&&hud.Body.Instructions.Any(i=>i.Operand is FieldReference field&&field.Name=="driftLapScore"),"shared native HUD can render event score separately from endless practice score");
            var destroy=Method(plugin,"OnDestroy");
            check(CallIndex(destroy,"Plugin","ClearDriftEvents")>=0&&CallIndex(destroy,"Plugin","ClearDriftEvents")<CallIndex(destroy,"Plugin","CleanupAll"),"plugin teardown releases event resources/holds before disposing vehicles");
            foreach(var target in new[]{new[]{"DriftEventPanel","Clear"},new[]{"DriftEventPad","Clear"},new[]{"ParkadeVenue","Clear"}})
                check(Calls(Method(plugin,"ClearDriftEvents"),target[0],target[1]),"event teardown clears owned resource: "+target[0]);
            var callbacks=events.Body.Instructions.Where(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Ldftn&&i.Operand is MethodReference callback&&callback.Name.StartsWith("<TickDriftEvents>")&&callback.Parameters.Count==0)
                .Select(i=>plugin.Methods.Single(m=>m.Name==((MethodReference)i.Operand).Name)).ToArray();
            check(callbacks.Length==2,"production event panel receives two actual generated button callbacks");
            foreach(string actionName in new[]{"AcceptDriftEvent","CancelDriftEvent"}) {
                var callback=callbacks.Single(m=>m.Body.Instructions.Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Ldftn&&i.Operand is MethodReference action&&action.Name==actionName));
                check(Calls(callback,"Plugin","RunDriftEventAction"),"native button routes through exception boundary: "+actionName);
            }
            var actionBoundary=Method(plugin,"RunDriftEventAction");
            check(actionBoundary.Body.ExceptionHandlers.Any(e=>e.HandlerType==Mono.Cecil.Cil.ExceptionHandlerType.Catch&&e.CatchType.FullName=="System.Exception")&&Calls(actionBoundary,"Plugin","FailDriftEvent"),"button exception boundary catches failures and delegates production recovery");
            var fail=Method(plugin,"FailDriftEvent");
            foreach(var target in new[]{new[]{"Plugin","ReleaseDriftRaceHold"},new[]{"Plugin","CloseDriftEventUI"},new[]{"DriftRaceRun","Practice"},new[]{"DriftEventPanel","Clear"}})
                check(Calls(fail,target[0],target[1]),"event failure removes stale UI/hold state: "+target[0]+"."+target[1]);
            var step=Method(plugin,"StepVehicle");
            check(CallIndex(step,"Plugin","get_DriftRaceHoldsCurrent")>=0&&CallIndex(step,"Plugin","get_DriftRaceHoldsCurrent")<CallIndex(step,"Plugin","StepDriftPhysics"),"held transfer car exits physics step before own drift simulation");
            var prepare=Method(plugin,"PrepareDriftParkingSpawn");
            check(Calls(prepare,"DriftSessionRules","get_Active")&&Calls(prepare,"Game","IsShuttingDown"),"arrival prefix is gated to active, non-shutdown drift sessions");
            check(Calls(prepare,"ZNet","SetReferencePosition")&&Calls(prepare,"ZNetScene","IsAreaReady")&&Calls(prepare,"ZoneSystem","GetGroundHeight"),"arrival uses native streaming, scene and ground readiness");
            check(Calls(prepare,"VehiclePersistence","Save")&&Calls(prepare,"ZDO","SetPosition")&&Calls(prepare,"ZDO","SetRotation"),"selected car is relocated through its existing persisted native record");
            check(!prepare.Body.Instructions.Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Newobj&&i.Operand is MethodReference ctor&&ctor.DeclaringType.Name=="ZDO"),"arrival preparation does not clone vehicle or cargo ZDOs");
            check(Calls(prepare,"Plugin","StartVehicleLoad"),"first-ever car uses existing staged construction behind loading screen");
            var arrival=Method(plugin,"TickDriftArrival");
            check(CallIndex(arrival,"Plugin","Enter")>=0&&CallIndex(arrival,"CabinTransition","SitImmediately")>CallIndex(arrival,"Plugin","Enter"),"arrival takes normal player/camera/collider ownership before requesting seated cabin pose");
            foreach(string name in new[]{"IsDead","IsTeleporting","IsAttached"})check(Calls(arrival,"Player",name)||Calls(arrival,"Character",name)||Calls(arrival,"Humanoid",name),"arrival waits for native player readiness: "+name);
            var move=Method(plugin,"MoveDriftCarTo");
            check(move.Parameters.Count==3&&move.Parameters[2].Name=="requireGround"&&move.Parameters[2].HasConstant&&(bool)move.Parameters[2].Constant,"shared transfer defaults to loaded ground requirement");
            foreach(var call in new[]{new[]{"Plugin","ReleaseDriftPhysics"},new[]{"Physics","SyncTransforms"},new[]{"VehicleStorage","Tick"},new[]{"VehicleStorage","SaveVehicle"},new[]{"VehicleWorldStreaming","Follow"},new[]{"VehicleOrbitCamera","SnapDriftTransfer"}})
                check(Calls(move,call[0],call[1]),"shared transfer synchronizes production subsystem: "+call[0]+"."+call[1]);
            check(Calls(move,"Heightmap","FindHeightmap")&&Calls(move,"Rigidbody","get_isKinematic"),"transfer checks loaded terrain or an explicit kinematic hold");
            check(CallIndex(move,"Plugin","SyncDriftDriverTransfer")>=0&&CallIndex(move,"Plugin","SyncDriftDriverTransfer")<CallIndex(move,"VehicleWorldStreaming","Follow"),"remote transfer publishes native player sector before switching the streaming reference");
            var syncDriver=Method(plugin,"SyncDriftDriverTransfer");
            check(Calls(syncDriver,"ZDO","SetPosition")&&Calls(syncDriver,"ZDO","SetRotation")&&Calls(syncDriver,"ZNetView","IsOwner"),"driver transfer updates only its owned native position/rotation record");
            check(Calls(syncDriver,"Rigidbody","set_position")&&Calls(syncDriver,"Rigidbody","set_rotation")&&Calls(syncDriver,"Physics","SyncTransforms"),"driver physics pose is synchronized before the native publisher can restore old coordinates");
            var iterator=plugin.NestedTypes.Single(t=>t.Name.StartsWith("<BuildVehicle>"));var build=Method(iterator,"MoveNext");
            check(CallIndex(build,"Plugin","DriftArrivalSpawnPoint")>=0&&CallIndex(build,"Plugin","DriftArrivalSpawnPoint")<CallIndex(build,"Plugin","FindSpawnPoint"),"real staged builder honors parking origin before generic player-relative spawn search");
            var accept=Method(plugin,"AcceptDriftEvent");
            check(CallIndex(accept,"Plugin","HoldDriftRaceCar")>=0&&CallIndex(accept,"Plugin","MoveToParkadeStart")>CallIndex(accept,"Plugin","HoldDriftRaceCar"),"event controller holds car before moving it into unloaded venue");
            var raceStart=Method(plugin,"MoveToParkadeStart");
            check(Calls(raceStart,"ParkadeLayout","GroundHeightAt")&&Calls(raceStart,"Plugin","MoveDriftCarTo"),"race arrival uses measured venue grade and common held transfer");
            check(Calls(events,"ParkadeVenue","TryPreparationPoint")&&Calls(events,"ParkadeVenue","get_ConstructionComplete")&&Calls(events,"Plugin","MoveToParkadeStart"),"loading stages native streaming and returns to start only after the whole construction completes");
            check(Calls(Method(plugin,"TickDriftEvents"),"Plugin","DriftTransferGroundReady"),"event loading validates native target ground before releasing countdown");
            var snap=Method(Type(built,"VehicleOrbitCamera"),"SnapDriftTransfer");
            check(!Calls(snap,"VehicleOrbitCamera","Begin")&&!Calls(snap,"Cursor","set_lockState"),"camera transfer resets position without replacing saved cursor ownership");
        }
    }
}
