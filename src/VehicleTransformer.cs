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
    readonly TransformerGroundProbe groundProbe;
    readonly Collider[] nearby=new Collider[64];
    readonly TransformerWaterPoint[] waterFootprint=new TransformerWaterPoint[19],waterFuture=new TransformerWaterPoint[18];
    readonly float[] waterStartingDepth=new float[19];
    readonly WaterVolume[] waterVolumes=new WaterVolume[16];
    readonly Bounds[] waterVolumeBounds=new Bounds[16];
    int waterVolumeCount;
    bool waterVolumeOverflow;
    readonly TransformerWaterProbe waterProbe;
    readonly float waterThoraxHeight,waterShoulderHeight;
    float maximumWadingDepth=TransformerWaterRules.DefaultMaximumDepth;
    float waterReferenceY;
    bool waterRecovery;
    readonly TransformerImpact impact=new TransformerImpact();
    readonly List<Tuple<Collider,bool>> colliders=new List<Tuple<Collider,bool>>();
    readonly List<Tuple<Renderer,bool>> renderers=new List<Tuple<Renderer,bool>>();
    readonly List<Tuple<Light,bool>> lights=new List<Tuple<Light,bool>>();
    readonly List<AudioSource> sounds=new List<AudioSource>();
    bool kinematic,gravity,motionRequested,returnRequested,returnBlocked,returnPrepared;RigidbodyConstraints constraints;
    float groundY,carClearance,moveSpeed,turnSpeed,returnHeight,nextObstacleReport,nextGroundReport;
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
    public float ActualTurnRate {get;private set;}
    public bool ReturnRequested=>returnRequested;
    public float MaximumWadingDepth=>maximumWadingDepth;
    public bool ReadyForMount=>State.CanWalk&&!returnRequested&&Terrain.CanMove&&!Terrain.ReturnStarted
        &&Visual.FeetSettled&&Mathf.Abs(moveSpeed)<.05f&&Mathf.Abs(turnSpeed)<.01f;
    public bool TakeBlockedReturn(){bool result=returnBlocked;returnBlocked=false;return result;}
    public VehicleTransformer(VehicleRuntime vehicle,TransformerAsset asset,bool staged=false) {
        owner=vehicle;car=vehicle.car.transform;body=vehicle.body;
        groundProbe=new TransformerGroundProbe(Own);
        waterProbe=WaterSupport;
        waterThoraxHeight=asset.Manifest.nodes.First(n=>n.name=="thorax").neutral[7];
        waterShoulderHeight=(asset.Manifest.nodes.First(n=>n.name=="clavicle.L").neutral[7]+asset.Manifest.nodes.First(n=>n.name=="clavicle.R").neutral[7])*.5f;
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
    bool Ground(Vector3 point,out float height,out Vector3 normal,out Collider support)=>groundProbe.Sample(point,out height,out normal,out support);
    bool WaterSupport(float x,float z,out float height,out float liquid) {
        liquid=-10000;
        height=0;if(waterVolumeOverflow)return false;
        if(!Ground(new Vector3(x,waterReferenceY,z),out height,out _,out _))return false;
        liquid=LiquidAt(x,z,height);return true;
    }
    float LiquidAt(float x,float z,float height) {
        float liquid=-10000;
        if(ZoneSystem.instance!=null)liquid=ZoneSystem.instance.m_waterLevel;
        // Retain native local liquids (including tar); ocean volumes below are
        // cached once per sweep so no second overlap query is needed per point.
        liquid=Mathf.Max(liquid,Floating.GetLiquidLevel(new Vector3(x,height+.02f,z),1));
        for(int i=0;i<waterVolumeCount;i++) {
            var bounds=waterVolumeBounds[i];
            if(x>=bounds.min.x&&x<=bounds.max.x&&z>=bounds.min.z&&z<=bounds.max.z&&waterVolumes[i]!=null)
                liquid=Mathf.Max(liquid,waterVolumes[i].GetWaterSurface(new Vector3(x,height,z),1));
        }
        return liquid;
    }
    void PrepareWaterVolumes(Vector3 from,Vector3 to) {
        waterVolumeCount=0;waterVolumeOverflow=false;
        float minX=Mathf.Min(from.x,to.x)-4,maxX=Mathf.Max(from.x,to.x)+4,minZ=Mathf.Min(from.z,to.z)-4,maxZ=Mathf.Max(from.z,to.z)+4;
        foreach(var volume in WaterVolume.Instances) {
            if(volume==null)continue;var collider=volume.GetComponent<Collider>();if(collider==null||!collider.enabled)continue;
            var bounds=collider.bounds;
            if(bounds.max.x<minX||bounds.min.x>maxX||bounds.max.z<minZ||bounds.min.z>maxZ)continue;
            if(waterVolumeCount==waterVolumes.Length){waterVolumeOverflow=true;return;}
            waterVolumes[waterVolumeCount]=volume;waterVolumeBounds[waterVolumeCount++]=bounds;
        }
    }
    int WaterFootprint(bool live,float futureStride=0) {
        int count=0;waterFootprint[count++]=new TransformerWaterPoint(0,0);
        float soleHeight=float.PositiveInfinity;
        var inverse=Quaternion.Inverse(car.rotation);
        for(int side=0;side<2;side++) {
            Vector3 center,direction;Vector2 size;
            Visual.RestFootContact(side!=0,out var restCenter,out var restSize);
            soleHeight=Mathf.Min(soleHeight,restCenter.y);
            if(live) {
                Visual.FootContact(side!=0,out center,out direction,out size);
                center=inverse*(center-car.position);direction=inverse*direction;
                direction.y=0;direction.Normalize();
            } else {center=restCenter;size=restSize;direction=Vector3.forward;}
            var right=Vector3.Cross(Vector3.up,direction);
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++) {
                var p=center+right*(x*size.x*.5f)+direction*(z*size.y*.5f);
                waterFootprint[count++]=new TransformerWaterPoint(p.x,p.z);
            }
            // Future soles are separate from the real standing baseline, and
            // only in the actual movement direction (signed stride). Standing
            // near water can therefore never disable reversing away from it.
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                waterFuture[side*9+(x+1)*3+z+1]=new TransformerWaterPoint(center.x+x*size.x*.5f,futureStride+z*size.y*.5f+restCenter.z);
        }
        maximumWadingDepth=TransformerWaterRules.ChestDepth(waterThoraxHeight,waterShoulderHeight,soleHeight)*Mathf.Abs(car.lossyScale.y);
        return count;
    }
    static TransformerWaterPose WaterPose(Vector3 point,Quaternion rotation)=>new TransformerWaterPose(point.x,point.z,rotation.eulerAngles.y*Mathf.Deg2Rad);
    bool WaterPathClear(Vector3 next,Quaternion rotation,bool sprint) {
        waterReferenceY=body.position.y;
        PrepareWaterVolumes(body.position,next);
        float stride=Mathf.Lerp(TransformerGait.Stride,TransformerGait.SprintStride,sprint?1:Visual.RunBlend)*Mathf.Sign(moveSpeed);
        int count=WaterFootprint(true,stride),futureCount=Mathf.Abs(moveSpeed)>.0001f?waterFuture.Length:0;
        bool allowed=TransformerWaterRules.CanTraverse(WaterPose(body.position,body.rotation),WaterPose(next,rotation),waterFootprint,count,waterProbe,waterStartingDepth,waterRecovery,waterFuture,futureCount,maximumWadingDepth);
        if(allowed&&waterRecovery&&TransformerWaterRules.WithinDepth(WaterPose(next,rotation),waterFootprint,count,waterProbe,maximumWadingDepth))waterRecovery=false;
        return allowed;
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
        int count=Physics.OverlapCapsuleNonAlloc(next+Vector3.up, next+Vector3.up*4.2f,.65f,nearby,TransformerGroundProbe.MovementMask,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++) {
            var other=nearby[i];if(Own(other)||TransformerGroundProbe.IsFluid(other)||other==support||other.GetComponent<Heightmap>()!=null)continue;
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
    void ReportGrounding(Collider support,float floor,float previousY) {
        if(Time.unscaledTime<nextGroundReport)return;
        nextGroundReport=Time.unscaledTime+1;
        float liquid=LiquidAt(body.position.x,body.position.z,floor),gap=previousY-floor;
        if(Mathf.Abs(gap)<.15f&&liquid<floor+.05f)return;
        nextGroundReport=Time.unscaledTime+10;
        Debug.Log("NFVU2 Transformer ground: rootY="+previousY.ToString("F3")+" floorY="+floor.ToString("F3")
            +" gap="+gap.ToString("F3")+" liquidY="+liquid.ToString("F3")+" support="+(support!=null?support.name:"none")
            +" layer="+(support!=null?LayerMask.LayerToName(support.gameObject.layer):"none"));
    }
    void SettleCurrentSupport() {
        if(!Ground(body.position,out float height,out _,out var support))return;
        var next=body.position;next.y=TransformerGroundProbe.FollowHeight(next.y,height,Time.fixedDeltaTime);
        ReportGrounding(support,height,body.position.y);
        if(!CoreClear(next,body.rotation,support))return;
        groundY=height;body.MovePosition(next);
    }
    static Quaternion SupportTilt(TransformerGroundPlane plane)=>Quaternion.FromToRotation(Vector3.up,new Vector3(-plane.SlopeX,1,-plane.SlopeZ).normalized);
    static bool NeedsPreparation(TransformerGroundPlane plane)=>Mathf.Abs(plane.SlopeX)+Mathf.Abs(plane.SlopeZ)>.001f||plane.Residual>.002f;
    public bool Begin(Transform livePlayer,out string reason)=>BeginCore(livePlayer,false,out reason);
    bool BeginCore(Transform livePlayer,bool restoringParked,out string reason) {
        reason="Zum Verwandeln kurz anhalten; die Welt unter dem Fahrzeug muss geladen sein.";player=livePlayer;
        if(Active||body.linearVelocity.magnitude>.15f||body.angularVelocity.magnitude>.1f||!Supported(car.position,car.rotation,out var plane))return false;
        waterReferenceY=body.position.y;
        PrepareWaterVolumes(body.position,body.position);
        int waterCount=WaterFootprint(false,0);
        bool dry=TransformerWaterRules.Dry(WaterPose(car.position,Yaw),waterFootprint,waterCount,waterProbe);
        if(!restoringParked) {
            if(!dry) {
                reason="Zum Verwandeln muss die gesamte Aufstandsflaeche trocken sein. Erst ans Ufer oder auf eine trockene Bruecke fahren.";return false;
            }
        }
        waterRecovery=restoringParked&&!TransformerWaterRules.WithinDepth(WaterPose(car.position,Yaw),waterFootprint,waterCount,waterProbe,maximumWadingDepth);
        groundY=plane.Height+plane.Residual;carClearance=body.position.y-groundY;transformationYaw=Yaw;carTilt=Quaternion.Inverse(transformationYaw)*body.rotation;supportTilt=SupportTilt(plane);
        var origin=new Vector3(car.position.x,groundY,car.position.z);
        Visual.CaptureCurrentCar();
        colliders.Clear();foreach(var c in car.GetComponentsInChildren<Collider>(true))if(c!=hull)colliders.Add(Tuple.Create(c,c.enabled));
        renderers.Clear();foreach(var r in car.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(Visual.Root.transform)&&owner.exhaustSmoke?.Owns(r.transform)!=true)renderers.Add(Tuple.Create(r,r.enabled));
        lights.Clear();foreach(var l in car.GetComponentsInChildren<Light>(true))if(!l.transform.IsChildOf(Visual.Root.transform))lights.Add(Tuple.Create(l,l.enabled));
        // Engine idle and foot contacts have their own pause/lifecycle handling.
        // Pausing them here would bypass their playback bookkeeping.
        sounds.Clear();foreach(var s in car.GetComponentsInChildren<AudioSource>(true))if(s.isPlaying&&owner.vehicleAudio?.Owns(s)!=true&&owner.transformerAudio?.Owns(s)!=true&&owner.transformerTurnAudio?.Owns(s)!=true&&owner.transformerDeployAudio?.Owns(s)!=true&&owner.transformerReactorAudio?.Owns(s)!=true&&owner.transformerReturnAudio?.Owns(s)!=true){sounds.Add(s);s.Pause();}
        foreach(var p in car.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        kinematic=body.isKinematic;gravity=body.useGravity;constraints=body.constraints;
        if(storage!=null)storage.PositionForSave=()=>new Vector3(car.position.x,groundY+carClearance,car.position.z);
        gaitPosition=car.position;gaitYaw=car.eulerAngles.y;ActualTurnRate=0;
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
        // Existing wet parked saves remain recoverable. This does not permit a
        // player to start a new underwater transformation through Begin().
        if(!BeginCore(null,true,out reason))return false;
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
        if((moveSpeed!=0||turnSpeed!=0)&&!WaterPathClear(next,rotation,sprint||Visual.RunBlend>.5f)) {
            moveSpeed=turnSpeed=0;motionRequested=false;SettleCurrentSupport();return;
        }
        if(!Ground(next,out float height,out _,out var support)){moveSpeed=0;motionRequested=false;SettleCurrentSupport();body.MoveRotation(rotation);return;}
        ReportGrounding(support,height,body.position.y);
        next.y=TransformerGroundProbe.FollowHeight(body.position.y,height,Time.fixedDeltaTime);
        if(next.y<height-.1f) {next.x=body.position.x;next.z=body.position.z;}
        if(!returnRequested&&Visual.ReadyToWalk)
            impact.Step(car,player!=null?player.GetComponent<Character>():null,next,rotation*Vector3.forward*(forward<0?-1:1),Mathf.Abs(moveSpeed),forward);
        if(!CoreClear(next,rotation,support)){moveSpeed=0;motionRequested=false;SettleCurrentSupport();body.MoveRotation(rotation);return;}
        // Ground height alone does not require terrain preparation. Latching
        // the open terrain stage here (even at zero speed) holds every armor
        // carrier out forever instead of letting the gait open and close it.
        groundY=height;body.MovePosition(next);body.MoveRotation(rotation);
    }
    public void Tick(float dt,bool blocked) {
        ActualTurnRate=0;
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
        if(!Active){owner.transformerReturnAudio?.CompleteReturn();Restore(true);return;}
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
        ActualTurnRate=actualTurn*TransformerGait.TurnDegrees;
        gaitPosition=car.position;gaitYaw=car.eulerAngles.y;
        Visual.Evaluate(State.Progress,gaitDt,actualSpeed,actualTurn,motionRequested);HideCar();
    }
    public void Restore(bool preserveReturnAudio=false) {
        owner.transformerMinigun?.Stop();
        owner.transformerAudio?.Stop();
        owner.transformerTurnAudio?.Stop();ActualTurnRate=0;
        owner.transformerDeployAudio?.Stop();
        owner.transformerReactorAudio?.Stop();
        if(!preserveReturnAudio)owner.transformerReturnAudio?.Stop();
        waterRecovery=false;
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
            int count=Physics.OverlapCapsuleNonAlloc(point+Vector3.up*.35f,point+Vector3.up*1.5f,.28f,nearby,TransformerGroundProbe.MovementMask,QueryTriggerInteraction.Ignore);
            bool clear=true;for(int j=0;j<count;j++)if(!Own(nearby[j])&&!TransformerGroundProbe.IsFluid(nearby[j])){clear=false;break;}
            if(clear)return point;
        }
        var fallback=car.position+car.right*3;fallback.y=Mathf.Max(car.position.y,groundY)+2;return fallback;
    }
    public void Dispose(){Restore();Visual.Dispose();if(hull!=null)UnityEngine.Object.Destroy(hull);}
}
}
