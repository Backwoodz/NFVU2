using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
// Deliberately isolated from VehicleHandling: owns physics only during robot
// mode, restores every captured flag on return, death, despawn and disposal.
internal sealed class VehicleTransformer:IDisposable {
    public readonly TransformerState State=new TransformerState();
    public readonly TransformerVisual Visual;
    readonly Transform car;
    readonly Rigidbody body;
    readonly VehicleStorage storage;
    readonly CapsuleCollider hull;
    readonly List<Tuple<Collider,bool>> colliders=new List<Tuple<Collider,bool>>();
    readonly List<Tuple<Renderer,bool>> renderers=new List<Tuple<Renderer,bool>>();
    readonly List<Tuple<Light,bool>> lights=new List<Tuple<Light,bool>>();
    readonly List<AudioSource> sounds=new List<AudioSource>();
    bool kinematic,gravity,motionRequested,returnRequested,returnBlocked;RigidbodyConstraints constraints;
    const float HullRadius=2.35f,HullBottom=2.4f,HullTop=3.05f;
    float groundY,carClearance,moveSpeed,turnSpeed;
    Vector3 gaitPosition;
    float gaitYaw;
    Transform player;
    public bool Active=>State.Active;
    public bool WalkingReady {get;private set;}
    public float Speed=>moveSpeed;
    public bool ReturnRequested=>returnRequested;
    public bool TakeBlockedReturn(){bool result=returnBlocked;returnBlocked=false;return result;}
    public VehicleTransformer(VehicleRuntime vehicle,TransformerAsset asset) {
        car=vehicle.car.transform;body=vehicle.body;storage=vehicle.storage;
        WalkingReady=asset.Manifest.walkingReady;
        Visual=new TransformerVisual(car,asset,vehicle.vehicleMaterials);
        hull=vehicle.car.AddComponent<CapsuleCollider>();hull.enabled=false;hull.radius=HullRadius;hull.height=5.35f;hull.center=new Vector3(0,2.725f,0);
    }
    bool Own(Collider c)=>c==null||c.transform.IsChildOf(car)||player!=null&&c.transform.IsChildOf(player);
    bool Ground(Vector3 point,out float height) {
        var hits=Physics.RaycastAll(point+Vector3.up*1.5f,Vector3.down,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>!Own(h.collider)).OrderBy(h=>h.distance).ToArray();
        height=point.y;if(hits.Length==0||hits[0].normal.y<.97f)return false;
        height=hits[0].point.y;return true;
    }
    bool Supported(Vector3 position,Quaternion rotation,out float height) {
        if(!Ground(position,out height))return false;
        // First prototype refuses ledges, stairs, water and uneven footholds.
        // Cover the entire longer stride, including toes, before moving the root.
        foreach(var offset in new[]{new Vector3(-1.25f,0,1.25f),new Vector3(1.25f,0,1.25f),new Vector3(-1.25f,0,-1.25f),new Vector3(1.25f,0,-1.25f)})
            if(!Ground(position+rotation*offset,out float h)||Mathf.Abs(h-height)>.08f)return false;
        if(ZoneSystem.instance!=null&&height<ZoneSystem.instance.m_waterLevel+.05f)return false;
        return true;
    }
    bool Clear(Vector3 origin,bool transformation) {
        if(transformation)return !Physics.OverlapBox(origin+Vector3.up*2.9f,new Vector3(3,2.7f,3),Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!Own(c));
        return !Physics.OverlapCapsule(origin+Vector3.up*HullBottom,origin+Vector3.up*HullTop,HullRadius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!Own(c));
    }
    public bool Begin(Transform livePlayer,out string reason) {
        reason="Zum Verwandeln auf einer freien, ebenen Flaeche anhalten.";player=livePlayer;
        if(Active||body.linearVelocity.magnitude>.15f||body.angularVelocity.magnitude>.1f||car.up.y<.995f||!Supported(car.position,car.rotation,out groundY))return false;
        var origin=new Vector3(car.position.x,groundY,car.position.z);
        if(!Clear(origin,true)) {reason="Fuer die Verwandlung rundherum und nach oben mehr Platz schaffen.";return false;}
        Visual.CaptureCurrentCar();
        colliders.Clear();foreach(var c in car.GetComponentsInChildren<Collider>(true))if(c!=hull)colliders.Add(Tuple.Create(c,c.enabled));
        renderers.Clear();foreach(var r in car.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(Visual.Root.transform))renderers.Add(Tuple.Create(r,r.enabled));
        lights.Clear();foreach(var l in car.GetComponentsInChildren<Light>(true))lights.Add(Tuple.Create(l,l.enabled));
        sounds.Clear();foreach(var s in car.GetComponentsInChildren<AudioSource>(true))if(s.isPlaying){sounds.Add(s);s.Pause();}
        foreach(var p in car.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        carClearance=body.position.y-groundY;kinematic=body.isKinematic;gravity=body.useGravity;constraints=body.constraints;
        if(storage!=null)storage.PositionForSave=()=>new Vector3(car.position.x,groundY+carClearance,car.position.z);
        gaitPosition=car.position;gaitYaw=car.eulerAngles.y;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;body.useGravity=false;
        foreach(var c in colliders)c.Item1.enabled=false;
        State.Begin(true,true,true,true);Visual.Evaluate(0,0,0,0);Visual.Root.SetActive(true);HideCar();reason=null;return true;
    }
    public bool Return() {
        if(!State.CanWalk||!Clear(new Vector3(car.position.x,groundY,car.position.z),true))return false;
        returnRequested=true;returnBlocked=false;motionRequested=false;return true;
    }
    void HideCar(){foreach(var r in renderers)if(r.Item1!=null)r.Item1.enabled=false;foreach(var l in lights)if(l.Item1!=null)l.Item1.enabled=false;}
    public void Step(bool blocked,float forward,float turn) {
        if(!Active)return;
        bool walking=State.CanWalk&&WalkingReady&&!blocked;
        motionRequested=walking&&!returnRequested&&(Mathf.Abs(forward)>.01f||Mathf.Abs(turn)>.01f);
        moveSpeed=TransformerGait.AdvanceSpeed(moveSpeed,forward,Time.fixedDeltaTime,walking&&!returnRequested&&Visual.ReadyToWalk);
        turnSpeed=Mathf.MoveTowards(turnSpeed,walking&&!returnRequested&&Visual.ReadyToWalk?Mathf.Clamp(turn,-1,1):0,Time.fixedDeltaTime*3);
        if(!walking){moveSpeed=0;turnSpeed=0;return;}
        var rotation=Quaternion.Euler(0,body.rotation.eulerAngles.y+turnSpeed*TransformerGait.TurnDegrees*Time.fixedDeltaTime,0);
        var movement=rotation*Vector3.forward*(moveSpeed*Time.fixedDeltaTime);
        var next=body.position+movement;
        bool swept=movement.sqrMagnitude>.00000001f&&Physics.CapsuleCastAll(body.position+Vector3.up*HullBottom,body.position+Vector3.up*HullTop,HullRadius,movement.normalized,movement.magnitude+.03f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(h=>!Own(h.collider));
        if(swept||!Supported(next,rotation,out float height)||!Clear(new Vector3(next.x,height,next.z),false)){moveSpeed=turnSpeed=0;motionRequested=false;return;}
        groundY=height;next.y=groundY;body.MovePosition(next);body.MoveRotation(rotation);
    }
    public void Tick(float dt,bool blocked) {
        if(!Active)return;
        if(returnRequested&&State.CanWalk&&Visual.Settled&&Mathf.Abs(moveSpeed)<.001f&&Mathf.Abs(turnSpeed)<.001f&&!blocked) {
            if(Clear(new Vector3(car.position.x,groundY,car.position.z),true))State.Return();
            else {returnRequested=false;returnBlocked=true;}
        }
        State.Step(dt,blocked);
        if(!Active){Restore();return;}
        if(!State.CanWalk) {
            float y=groundY+carClearance*(1-Mathf.SmoothStep(0,1,State.Progress));
            var p=body.position;p.y=y;body.position=p;
        }
        hull.enabled=State.CanWalk;
        // The interpolated root is the visual ground reference. Drive the feet
        // from distance actually travelled, including collision stops, rather
        // than a desired velocity that can slide the planted feet.
        float gaitDt=blocked?0:Mathf.Min(dt,.1f),actualSpeed=0,actualTurn=0;
        if(gaitDt>0&&State.CanWalk) {
            var displacement=Quaternion.Euler(0,-gaitYaw,0)*(car.position-gaitPosition);
            actualSpeed=displacement.z/gaitDt;
            actualTurn=Mathf.DeltaAngle(gaitYaw,car.eulerAngles.y)/(gaitDt*TransformerGait.TurnDegrees);
        }
        gaitPosition=car.position;gaitYaw=car.eulerAngles.y;
        Visual.Evaluate(State.Progress,gaitDt,actualSpeed,actualTurn,motionRequested);HideCar();
    }
    public void Restore() {
        if(colliders.Count==0)return;
        Visual.Root.SetActive(false);hull.enabled=false;
        foreach(var r in renderers)if(r.Item1!=null)r.Item1.enabled=r.Item2;
        foreach(var l in lights)if(l.Item1!=null)l.Item1.enabled=l.Item2;
        foreach(var c in colliders)if(c.Item1!=null)c.Item1.enabled=c.Item2;
        if(body!=null){var restored=body.position;restored.y=groundY+carClearance;body.position=restored;body.isKinematic=kinematic;body.useGravity=gravity;body.constraints=constraints;if(!kinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}}
        foreach(var s in sounds)if(s!=null)s.UnPause();sounds.Clear();colliders.Clear();renderers.Clear();lights.Clear();State.Reset();moveSpeed=turnSpeed=0;motionRequested=returnRequested=returnBlocked=false;
        if(storage!=null)storage.PositionForSave=null;
    }
    public void Dispose(){Restore();Visual.Dispose();if(hull!=null)UnityEngine.Object.Destroy(hull);}
}
}
