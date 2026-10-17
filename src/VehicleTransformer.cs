using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
// Deliberately isolated from VehicleHandling: owns physics only during robot
// mode, restores every captured flag on return, death, despawn and disposal.
internal sealed class VehicleTransformer:IDisposable {
    public readonly TransformerState State=new TransformerState();
    public readonly TransformerTerrainPhase Terrain=new TransformerTerrainPhase();
    public readonly TransformerVisual Visual;
    readonly Transform car;
    readonly Rigidbody body;
    readonly VehicleRuntime owner;
    VehicleStorage storage=>owner.storage;
    readonly CapsuleCollider hull;
    readonly RaycastHit[] groundHits=new RaycastHit[32];
    readonly Collider[] nearby=new Collider[64];
    readonly TransformerImpact impact=new TransformerImpact();
    readonly List<Tuple<Collider,bool>> colliders=new List<Tuple<Collider,bool>>();
    readonly List<Tuple<Renderer,bool>> renderers=new List<Tuple<Renderer,bool>>();
    readonly List<Tuple<Light,bool>> lights=new List<Tuple<Light,bool>>();
    readonly List<AudioSource> sounds=new List<AudioSource>();
    bool kinematic,gravity,motionRequested,returnRequested,returnBlocked,returnPrepared;RigidbodyConstraints constraints;
    float groundY,carClearance,moveSpeed,turnSpeed,returnHeight,nextObstacleReport;
    Quaternion carTilt=Quaternion.identity,supportTilt=Quaternion.identity;
    Quaternion transformationYaw=Quaternion.identity;
    Vector3 gaitPosition;
    float gaitYaw;
    Transform player;
    public bool Active=>State.Active;
    public bool WalkingReady {get;private set;}
    public bool Mounting {get;set;}
    public void SetRider(Transform rider){player=rider;}
    public float Speed=>moveSpeed;
    public bool ReturnRequested=>returnRequested;
    public bool ReadyForMount=>State.CanWalk&&!returnRequested&&Terrain.CanMove&&!Terrain.ReturnStarted
        &&Visual.FeetSettled&&Mathf.Abs(moveSpeed)<.05f&&Mathf.Abs(turnSpeed)<.01f;
    public bool TakeBlockedReturn(){bool result=returnBlocked;returnBlocked=false;return result;}
    public VehicleTransformer(VehicleRuntime vehicle,TransformerAsset asset,bool staged=false) {
        owner=vehicle;car=vehicle.car.transform;body=vehicle.body;
        WalkingReady=asset.Manifest.walkingReady;
        Visual=new TransformerVisual(car,asset,vehicle.vehicleMaterials,staged);
        Visual.GroundPlane=FootPlane;
        // This core supplies solid interaction with dynamic objects. External
        // walls/rocks are checked against the actual moving rigid parts.
        hull=vehicle.car.AddComponent<CapsuleCollider>();hull.enabled=false;hull.radius=.7f;hull.height=4.6f;hull.center=new Vector3(0,2.6f,0);
    }
    public IEnumerable<int> LoadSteps()=>Visual.LoadSteps();
    bool Own(Collider c)=>c==null||c.transform.IsChildOf(car)||player!=null&&c.transform.IsChildOf(player);
    bool Ground(Vector3 point,out float height,out Vector3 normal)=>Ground(point,out height,out normal,out _);
    bool Ground(Vector3 point,out float height,out Vector3 normal,out Collider support) {
        // Loaded terrain is support regardless of its slope or local roughness.
        // Probe from just above the root/terrain, not above the treetops.
        height=point.y;normal=Vector3.up;support=null;bool found=false;
        var map=Heightmap.FindHeightmap(point);
        if(map!=null&&map.GetWorldHeight(point,out float terrainHeight)) {height=terrainHeight;support=map.GetComponent<Collider>();found=true;}
        float probeTop=Mathf.Max(point.y,height)+1.8f;
        int count=Physics.RaycastNonAlloc(new Vector3(point.x,probeTop,point.z),Vector3.down,groundHits,found?Mathf.Max(8,probeTop-height+2):128,TransformerImpact.CollisionMask,QueryTriggerInteraction.Ignore);
        float closest=float.PositiveInfinity;
        for(int i=0;i<count;i++)if(!Own(groundHits[i].collider)&&groundHits[i].distance<closest) {
            var hit=groundHits[i];
            if(hit.normal.y<.1f||TransformerImpact.IgnoreMovementCollider(hit.collider)
                ||hit.collider.GetComponentInParent<Character>()!=null)continue;
            closest=hit.distance;
            if(!found||hit.point.y>=height-.05f){height=hit.point.y;normal=hit.normal;support=hit.collider;found=true;}
        }
        // Wade at the surface rather than following a deep sea floor into black water.
        if(found&&ZoneSystem.instance!=null)height=Mathf.Max(height,ZoneSystem.instance.m_waterLevel-.8f);
        return found;
    }
    bool Supported(Vector3 position,Quaternion rotation,out TransformerGroundPlane plane,bool walking=false) {
        plane=new TransformerGroundPlane();
        if(!Ground(position,out float height,out _))return false;
        plane.Height=height;plane.NormalY=1;return true;
    }
    Vector4 NominalFootPlane(Vector3 local,Vector3 origin,Quaternion rotation) {
        var inverse=Quaternion.Inverse(rotation);
        var normal=(inverse*(transformationYaw*supportTilt*Vector3.up)).normalized;
        var point=inverse*(new Vector3(origin.x,groundY,origin.z)-origin);
        float height=(Vector3.Dot(normal,point)-normal.x*local.x-normal.z*local.z)/Mathf.Max(.8f,normal.y);
        return new Vector4(normal.x,normal.y,normal.z,height);
    }
    Vector4 FootPlane(Vector3 local)=>FootPlane(local,car.position,car.rotation,Terrain.Alignment);
    Vector4 FootPlane(Vector3 local,Vector3 origin,Quaternion rotation,float alignment) {
        if(alignment>=1)return ActualFootPlane(local,origin,rotation);
        var fit=NominalFootPlane(local,origin,rotation);
        if(alignment<=0)return fit;
        var actual=ActualFootPlane(local,origin,rotation);
        var nFit=new Vector3(fit.x,fit.y,fit.z);var nActual=new Vector3(actual.x,actual.y,actual.z);
        float dFit=Vector3.Dot(nFit,new Vector3(local.x,fit.w,local.z));
        float dActual=Vector3.Dot(nActual,new Vector3(local.x,actual.w,local.z));
        // Both planes are expressed in the CURRENT root basis. On a uniform
        // slope their normals are identical: only local bumps/residual height
        // are blended, rather than applying the root's slope a second time.
        var n=Vector3.Lerp(nFit,nActual,alignment);
        float d=Mathf.Lerp(dFit,dActual,alignment);
        float h=(d-n.x*local.x-n.z*local.z)/Mathf.Max(.8f,n.y);
        n.Normalize();return new Vector4(n.x,n.y,n.z,h);
    }
    Vector4 ActualFootPlane(Vector3 local,Vector3 origin,Quaternion rotation) {
        var world=origin+rotation*local;
        float height=groundY;
        if(Ground(world,out float actual,out _))height=actual;
        // Cosmetic foot adaptation must never stop locomotion or force the
        // whole machine into a deep crouch on a steep hillside.
        float localHeight=Mathf.Clamp(height-origin.y,-.3f,.3f);
        return new Vector4(0,1,0,localHeight);
    }
    Quaternion Yaw=>Quaternion.Euler(0,car.eulerAngles.y,0);
    bool CoreClear(Vector3 next,Quaternion rotation,Collider support=null) {
        int count=Physics.OverlapCapsuleNonAlloc(next+Vector3.up, next+Vector3.up*4.2f,.65f,nearby,TransformerImpact.CollisionMask,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++) {
            var other=nearby[i];if(Own(other)||other==support||other.GetComponent<Heightmap>()!=null)continue;
            // A tree/log may keep its solid collider until native felling RPCs
            // finish. It is damageable scenery, never a braking obstacle for
            // the robot. Keep walls, rocks and characters in the solid check.
            if(TransformerImpact.IgnoreMovementCollider(other))continue;
            if(!Physics.ComputePenetration(hull,next,rotation,other,other.transform.position,other.transform.rotation,out _,out float after)||after<.015f)continue;
            // Never imprison an already intersecting core between colliders:
            // allow movement until free instead of requiring every overlap to
            // improve simultaneously (impossible in some terrain seams).
            Physics.ComputePenetration(hull,body.position,body.rotation,other,other.transform.position,other.transform.rotation,out _,out float before);
            if(before<=.015f) {
                // Keep the exact remaining blocker available after a playtest,
                // instead of guessing from a screenshot of the nearby tree.
                if(Time.unscaledTime>=nextObstacleReport) {
                    nextObstacleReport=Time.unscaledTime+2;
                    Debug.Log("NFVU2 Transformer movement blocked by "+other.name+" under "+other.transform.root.name
                        +" (layer "+LayerMask.LayerToName(other.gameObject.layer)+")");
                }
                return false;
            }
        }
        return true;
    }
    static Quaternion SupportTilt(TransformerGroundPlane plane)=>Quaternion.FromToRotation(Vector3.up,new Vector3(-plane.SlopeX,1,-plane.SlopeZ).normalized);
    static bool NeedsPreparation(TransformerGroundPlane plane)=>Mathf.Abs(plane.SlopeX)+Mathf.Abs(plane.SlopeZ)>.001f||plane.Residual>.002f;
    public bool Begin(Transform livePlayer,out string reason) {
        reason="Zum Verwandeln kurz anhalten; die Welt unter dem Fahrzeug muss geladen sein.";player=livePlayer;
        if(Active||body.linearVelocity.magnitude>.15f||body.angularVelocity.magnitude>.1f||!Supported(car.position,car.rotation,out var plane))return false;
        groundY=plane.Height+plane.Residual;carClearance=body.position.y-groundY;transformationYaw=Yaw;carTilt=Quaternion.Inverse(transformationYaw)*body.rotation;supportTilt=SupportTilt(plane);
        var origin=new Vector3(car.position.x,groundY,car.position.z);
        Visual.CaptureCurrentCar();
        colliders.Clear();foreach(var c in car.GetComponentsInChildren<Collider>(true))if(c!=hull)colliders.Add(Tuple.Create(c,c.enabled));
        renderers.Clear();foreach(var r in car.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(Visual.Root.transform))renderers.Add(Tuple.Create(r,r.enabled));
        lights.Clear();foreach(var l in car.GetComponentsInChildren<Light>(true))lights.Add(Tuple.Create(l,l.enabled));
        sounds.Clear();foreach(var s in car.GetComponentsInChildren<AudioSource>(true))if(s.isPlaying){sounds.Add(s);s.Pause();}
        foreach(var p in car.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        kinematic=body.isKinematic;gravity=body.useGravity;constraints=body.constraints;
        if(storage!=null)storage.PositionForSave=()=>new Vector3(car.position.x,groundY+carClearance,car.position.z);
        gaitPosition=car.position;gaitYaw=car.eulerAngles.y;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;body.useGravity=false;
        foreach(var c in colliders)c.Item1.enabled=false;
        Terrain.Reset(NeedsPreparation(plane));Visual.HoldReady=false;returnPrepared=false;
        State.Begin(true,true,true,true);Visual.Evaluate(0,0,0,0);Visual.Root.SetActive(true);HideCar();reason=null;return true;
    }
    public bool Return() {
        if(!State.CanWalk)return false;
        // Exit controls repeat this request every frame until the inverse
        // source animation starts. Preserve the already checked return path.
        if(returnRequested)return true;
        // Braking can still change position. Fit and collision-check the return
        // only after both feet have settled at the final stationary location.
        returnRequested=true;returnPrepared=false;returnBlocked=false;motionRequested=false;return true;
    }
    public bool RestoreParked(out string reason) {
        if(!Begin(null,out reason))return false;
        State.RestoreParked();Terrain.Reset(false);
        supportTilt=carTilt=Quaternion.identity;
        body.position=new Vector3(car.position.x,groundY,car.position.z);
        body.rotation=transformationYaw;gaitPosition=car.position;gaitYaw=car.eulerAngles.y;
        hull.enabled=true;Visual.Evaluate(1,0,0,0);HideCar();return true;
    }
    void HideCar(){foreach(var r in renderers)if(r.Item1!=null)r.Item1.enabled=false;foreach(var l in lights)if(l.Item1!=null)l.Item1.enabled=false;}
    public void Step(bool blocked,float forward,float turn,bool sprint=false) {
        if(!Active)return;
        bool walking=State.CanWalk&&WalkingReady&&Terrain.CanMove&&!blocked;
        motionRequested=walking&&!returnRequested&&(Mathf.Abs(forward)>.01f||Mathf.Abs(turn)>.01f);
        moveSpeed=TransformerGait.AdvanceSpeed(moveSpeed,forward,Time.fixedDeltaTime,walking&&!returnRequested&&Visual.ReadyToWalk,sprint);
        turnSpeed=Mathf.MoveTowards(turnSpeed,walking&&!returnRequested&&Visual.ReadyToWalk?Mathf.Clamp(turn,-1,1):0,Time.fixedDeltaTime*4.5f);
        if(!walking){moveSpeed=0;turnSpeed=0;return;}
        float fast=Mathf.InverseLerp(TransformerGait.MaxSpeed,TransformerGait.SprintSpeed,Mathf.Max(0,moveSpeed));
        var rotation=Quaternion.Euler(0,body.rotation.eulerAngles.y+turnSpeed*Mathf.Lerp(TransformerGait.TurnDegrees,TransformerGait.SprintTurnDegrees,fast)*Time.fixedDeltaTime,0);
        var movement=rotation*Vector3.forward*(moveSpeed*Time.fixedDeltaTime);
        var next=body.position+movement;
        if(!Ground(next,out float height,out _,out var support)){moveSpeed=0;motionRequested=false;body.MoveRotation(rotation);return;}
        next.y=Mathf.MoveTowards(body.position.y,height,Time.fixedDeltaTime*18);
        if(next.y<height-.1f) {next.x=body.position.x;next.z=body.position.z;}
        if(!returnRequested&&Visual.ReadyToWalk)
            impact.Step(car,player!=null?player.GetComponent<Character>():null,next,rotation*Vector3.forward*(forward<0?-1:1),Mathf.Abs(moveSpeed),forward);
        if(!CoreClear(next,rotation,support)){moveSpeed=0;motionRequested=false;body.MoveRotation(rotation);return;}
        // Ground height alone does not require terrain preparation. Latching
        // the open terrain stage here (even at zero speed) holds every armor
        // carrier out forever instead of letting the gait open and close it.
        groundY=height;body.MovePosition(next);body.MoveRotation(rotation);
    }
    public void Tick(float dt,bool blocked) {
        if(!Active)return;
        bool preparing=!Terrain.CanMove;
        if(returnRequested&&!returnPrepared&&State.CanWalk&&Visual.FeetSettled&&Mathf.Abs(moveSpeed)<.001f&&Mathf.Abs(turnSpeed)<.001f&&!blocked) {
            if(Ground(car.position,out float height,out _))groundY=height;
            supportTilt=carTilt=Quaternion.identity;transformationYaw=Yaw;
            returnHeight=body.position.y;returnPrepared=true;
        }
        if(State.CanWalk)Terrain.Step(dt,Visual.ReadyToWalk,Visual.FeetSettled,returnPrepared,blocked,Visual.Settled);
        Visual.HoldReady=State.CanWalk&&(Terrain.HoldReady||Mounting);
        if(returnPrepared&&State.CanWalk&&Terrain.CanRetract&&Visual.Settled&&!blocked)State.Return();
        State.Step(dt,blocked);
        if(!Active){Restore();return;}
        if(!State.CanWalk) {
            float y=groundY+carClearance*(1-Mathf.SmoothStep(0,1,State.Progress));
            var p=body.position;p.y=y;body.position=p;
            body.rotation=transformationYaw*Quaternion.Slerp(carTilt,supportTilt,TransformerPlacement.Align(State.Progress));
        } else if(preparing||!Terrain.CanMove||Terrain.ReturnStarted) {
            body.rotation=transformationYaw*Quaternion.Slerp(supportTilt,Quaternion.identity,Terrain.Alignment);
            if(returnPrepared) {
                var p=body.position;p.y=Mathf.Lerp(groundY,returnHeight,Terrain.Alignment);body.position=p;
            }
        }
        hull.enabled=State.CanWalk;
        // The interpolated root is the visual ground reference. Drive the feet
        // from distance actually travelled, including collision stops, rather
        // than a desired velocity that can slide the planted feet.
        float gaitDt=blocked?0:Mathf.Min(dt,.1f),actualSpeed=0,actualTurn=0;
        if(gaitDt>0&&State.CanWalk&&Terrain.CanMove&&!preparing) {
            var displacement=Quaternion.Euler(0,-gaitYaw,0)*(car.position-gaitPosition);
            actualSpeed=displacement.z/gaitDt;
            actualTurn=Mathf.DeltaAngle(gaitYaw,car.eulerAngles.y)/(gaitDt*TransformerGait.TurnDegrees);
        }
        gaitPosition=car.position;gaitYaw=car.eulerAngles.y;
        Visual.Evaluate(State.Progress,gaitDt,actualSpeed,actualTurn,motionRequested);HideCar();
    }
    public void Restore() {
        Mounting=false;Visual.ClearMountPose();
        if(colliders.Count==0)return;
        Visual.Root.SetActive(false);hull.enabled=false;
        foreach(var r in renderers)if(r.Item1!=null)r.Item1.enabled=r.Item2;
        foreach(var l in lights)if(l.Item1!=null)l.Item1.enabled=l.Item2;
        foreach(var c in colliders)if(c.Item1!=null)c.Item1.enabled=c.Item2;
        if(body!=null){var restored=body.position;restored.y=groundY+carClearance;body.position=restored;body.isKinematic=kinematic;body.useGravity=gravity;body.constraints=constraints;if(!kinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}}
        foreach(var s in sounds)if(s!=null)s.UnPause();sounds.Clear();colliders.Clear();renderers.Clear();lights.Clear();State.Reset();Terrain.Reset(false);Visual.HoldReady=false;moveSpeed=turnSpeed=0;motionRequested=returnRequested=returnBlocked=returnPrepared=false;
        if(storage!=null)storage.PositionForSave=null;
    }
    public Vector3 ExitPoint() {
        for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++) {
            float angle=i*Mathf.PI/4;
            var point=car.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(2.5f+ring*1.5f);
            if(!Ground(point,out float height,out _))continue;
            point.y=height+.15f;
            int count=Physics.OverlapCapsuleNonAlloc(point+Vector3.up*.35f,point+Vector3.up*1.5f,.28f,nearby,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            bool clear=true;for(int j=0;j<count;j++)if(!Own(nearby[j])){clear=false;break;}
            if(clear)return point;
        }
        var fallback=car.position+car.right*3;fallback.y=Mathf.Max(car.position.y,groundY)+2;return fallback;
    }
    public void Dispose(){Restore();Visual.Dispose();if(hull!=null)UnityEngine.Object.Destroy(hull);}
}
}
