using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
// Player.LateUpdate normally moves ZNet's streaming reference. The cabin disables
// Player, so the vehicle must keep that reference alive explicitly.
internal sealed class VehicleWorldStreaming : IDisposable {
    readonly Rigidbody body;
    readonly Dictionary<Vector2s,bool> checkedZones=new Dictionary<Vector2s,bool>();
    readonly HashSet<Vector2s> preparedZones=new HashSet<Vector2s>();
    Vector3 heldVelocity,heldSpin;
    float prefetchTimer,waitSeconds;
    bool held,wasKinematic,notified;
    public bool Waiting=>held;
    public VehicleWorldStreaming(Rigidbody body){this.body=body;}
    public static void Follow(Player driver) {
        if(driver!=null&&driver==Player.m_localPlayer&&ZNet.instance!=null)
            ZNet.instance.SetReferencePosition(driver.transform.position);
    }
    public bool Step(bool prefetch=true) {
        if(body==null)return false;
        var velocity=held?heldVelocity:body.linearVelocity;
        if(held)waitSeconds+=Time.fixedDeltaTime;
        if(prefetch)Prefetch(velocity);
        checkedZones.Clear();
        // Only protect the chassis and the next few physics steps. IsAreaReady
        // waits for unrelated ZDOs in neighbouring zones and may flap forever.
        // IsZoneLoaded also waits for decorations after the collider is usable.
        var travel=Vector3.ClampMagnitude(new Vector3(velocity.x,0,velocity.z)*.08f,8);
        bool ready=ZoneSystem.instance!=null;
        for(int i=0;ready&&i<2;i++)for(int x=-1;ready&&x<=1;x+=2)for(int z=-1;ready&&z<=1;z+=2) {
            var point=body.position+travel*i+new Vector3(x*4,0,z*4);
            var zone=ZoneSystem.GetZone(point);
            if(!checkedZones.TryGetValue(zone,out ready)) {
                var map=Heightmap.FindHeightmap(ZoneSystem.GetZonePos(zone));
                var collider=map!=null?map.GetComponent<MeshCollider>():null;
                ready=collider!=null&&collider.enabled&&collider.gameObject.activeInHierarchy&&collider.sharedMesh!=null;
                checkedZones.Add(zone,ready);
            }
        }
        if(!ready) {
            if(!held) {
                wasKinematic=body.isKinematic;heldVelocity=body.linearVelocity;heldSpin=body.angularVelocity;
                waitSeconds=0;notified=false;
                if(!wasKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
                body.isKinematic=true;held=true;
            }
            return false;
        }
        Release(true);return true;
    }
    void Prefetch(Vector3 velocity) {
        prefetchTimer-=Time.fixedDeltaTime;
        if(prefetchTimer>0||ZoneSystem.instance==null||WorldGenerator.instance==null)return;
        prefetchTimer=.25f;preparedZones.Clear();
        var prefab=ZoneSystem.instance.m_zonePrefab;
        var template=prefab!=null?prefab.GetComponentInChildren<Heightmap>():null;
        if(template==null)return;
        var ahead=Vector3.ClampMagnitude(new Vector3(velocity.x,0,velocity.z)*2,112);
        // At most four distinct requests each quarter second. This queues only
        // terrain data on Valheim's builder thread; never instantiate Unity
        // objects off-thread or invoke RequestTerrainSync's blocking loop.
        int queued=0;
        for(int i=0;i<3&&queued<4;i++)for(int x=-1;x<=1&&queued<4;x+=2)for(int z=-1;z<=1&&queued<4;z+=2) {
            var point=body.position+ahead*(i*.5f)+new Vector3(x*4,0,z*4);
            var zone=ZoneSystem.GetZone(point);
            if(!preparedZones.Add(zone)||Heightmap.FindHeightmap(ZoneSystem.GetZonePos(zone))!=null)continue;
            var builder=HeightmapBuilder.instance;
            if(builder!=null){builder.IsTerrainReady(ZoneSystem.GetZonePos(zone),template.m_width,template.m_scale,template.IsDistantLod,WorldGenerator.instance);queued++;}
        }
    }
    public bool TakeNotice() {
        if(!held||notified||waitSeconds<.75f)return false;
        notified=true;return true;
    }
    void Release(bool resume) {
        if(!held)return;
        if(body!=null) {
            body.isKinematic=wasKinematic;
            if(!wasKinematic) {
                // A transient collider handoff should not kill all momentum.
                // Long waits and exit/cleanup restart safely from rest.
                if(resume&&waitSeconds<=.25f){body.linearVelocity=heldVelocity;body.angularVelocity=heldSpin;}
                body.WakeUp();
            }
        }
        held=false;
    }
    public void Dispose()=>Release(false);
}
}
