using System;
using Rachel350Z;
using UnityEngine;
class StreamingTests {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static bool Zero(Vector3 v)=>v.x==0&&v.y==0&&v.z==0;
    static void World(){
        ZoneSystem.instance=new ZoneSystem();ZNetScene.instance=new ZNetScene();ZNet.instance=new ZNet();
        Heightmap.missing.Clear();Heightmap.maps.Clear();HeightmapBuilder.instance=new HeightmapBuilder();WorldGenerator.instance=new WorldGenerator();
    }
    static void Main() {
        World();var driver=new Player{enabled=false};Player.m_localPlayer=driver;
        for(int i=0;i<100;i++) {
            driver.transform.position=new Vector3(i*70,300,i*-55);VehicleWorldStreaming.Follow(driver);
        }
        Check(ZNet.instance.reference.x==6930&&ZNet.instance.reference.y==300&&ZNet.instance.reference.z==-5445,
            "disabled cabin player advances native streaming reference across distant zones and at flight altitude");
        int calls=ZNet.instance.calls;VehicleWorldStreaming.Follow(null);VehicleWorldStreaming.Follow(new Player());
        Check(ZNet.instance.calls==calls,"null and other players cannot steal streaming reference");
        ZNet.instance=null;VehicleWorldStreaming.Follow(driver);World();
        var body=new Rigidbody{position=new Vector3(0,500,0),linearVelocity=new Vector3(60,-16,0)};
        var stream=new VehicleWorldStreaming(body);
        ZoneSystem.instance.missing.Add(new Vector2s(0,0));ZNetScene.instance.missing.Add(new Vector2s(0,0));
        for(int i=0;i<100;i++)CheckReady(stream);
        Check(!stream.Waiting&&!body.isKinematic&&body.linearVelocity.x==60&&body.linearVelocity.y==-16,
            "pending scenery and flapping area readiness never stop a car or flight over usable terrain");
        Check(ZoneSystem.instance.queries==0&&ZNetScene.instance.queries==0,"expensive full-world-object readiness scans are absent");
        World();body.position=new Vector3(0,500,0);body.linearVelocity=new Vector3(60,0,0);
        Heightmap.missing.Add(new Vector2s(1,0));Heightmap.missing.Add(new Vector2s(2,0));
        stream=new VehicleWorldStreaming(body);
        Check(stream.Step()&&!body.isKinematic,"missing terrain half a second ahead does not freeze current movement");
        Check(HeightmapBuilder.instance.requests.Contains(new Vector2s(1,0))&&HeightmapBuilder.instance.requests.Contains(new Vector2s(2,0)),
            "approaching terrain is queued on native asynchronous builder before arrival");
        Check(HeightmapBuilder.instance.width==64&&HeightmapBuilder.instance.scale==1&&!HeightmapBuilder.instance.distant,
            "background jobs use the actual zone prefab's terrain dimensions");
        int queued=HeightmapBuilder.instance.calls;
        stream.Step();Check(HeightmapBuilder.instance.calls==queued,"prefetch submissions are throttled instead of repeated every physics frame");
        body.position=new Vector3(25,500,0);body.angularVelocity=new Vector3(0,2,0);
        Check(!stream.Step()&&stream.Waiting&&body.isKinematic&&Zero(body.linearVelocity),"missing collider within next physics steps holds chassis safely");
        Check(!stream.TakeNotice(),"brief loading holds do not post screen messages");
        Check(!stream.Step(),"waiting retains original velocity corridor after clearing body velocity");
        Heightmap.missing.Clear();
        Check(stream.Step()&&!body.isKinematic&&body.linearVelocity.x==60&&body.angularVelocity.y==2,
            "short collider handoff preserves momentum instead of restarting acceleration");
        Heightmap.missing.Add(new Vector2s(1,0));
        Check(!stream.Step(),"later genuinely missing collider can still protect the vehicle");
        for(int i=0;i<40;i++)CheckHold(stream);
        Check(stream.TakeNotice()&&!stream.TakeNotice(),"sustained loading posts exactly one delayed notice");
        Check(body.position.x==25&&body.position.y==500,"long hold never drops or teleports vehicle");
        Heightmap.missing.Clear();
        Check(stream.Step()&&!body.isKinematic&&Zero(body.linearVelocity),"long waits restart safely from rest");
        foreach(var velocity in new[]{new Vector3(-60,0,0),new Vector3(0,0,-60),new Vector3(60,0,60)}) {
            World();body.position=new Vector3(velocity.x<0?-25:25,0,velocity.z<0?-25:25);body.linearVelocity=velocity;
            stream=new VehicleWorldStreaming(body);
            var next=ZoneSystem.GetZone(body.position+Vector3.ClampMagnitude(velocity*.08f,8)+new Vector3(velocity.x<0?-4:4,0,velocity.z<0?-4:4));
            Heightmap.missing.Add(next);
            Check(!stream.Step(),"reverse, sideways and diagonal travel protect imminent terrain boundary");
            stream.Dispose();Check(!body.isKinematic&&Zero(body.linearVelocity),"exit releases hold without launching parked car");
        }
        foreach(int invalid in new[]{0,1,2}) {
            World();body.position=Vector3.zero;body.linearVelocity=Vector3.zero;stream=new VehicleWorldStreaming(body);
            var collider=Heightmap.FindHeightmap(Vector3.zero).collider;
            if(invalid==0)collider.enabled=false;
            if(invalid==1)collider.gameObject.activeInHierarchy=false;
            if(invalid==2)collider.sharedMesh=null;
            Check(!stream.Step(),"terrain object without usable collider cannot release safeguard");stream.Dispose();
        }
        World();body.position=new Vector3(30,0,30);body.linearVelocity=Vector3.zero;
        Heightmap.missing.Add(new Vector2s(1,1));stream=new VehicleWorldStreaming(body);
        Check(!stream.Step(),"diagonal chassis corner is checked even when centre terrain exists");stream.Dispose();
        World();body.position=Vector3.zero;body.isKinematic=true;ZoneSystem.instance=null;
        stream=new VehicleWorldStreaming(body);Check(!stream.Step(),"world teardown avoids access to absent services");
        stream.Dispose();Check(body.isKinematic,"cleanup preserves pre-existing kinematic ownership");
        World();body.isKinematic=false;body.position=new Vector3(31,500,31);body.linearVelocity=new Vector3(10000,0,10000);
        for(int x=-1;x<5;x++)for(int z=-1;z<5;z++)Heightmap.missing.Add(new Vector2s(x,z));
        stream=new VehicleWorldStreaming(body);stream.Step();
        Check(HeightmapBuilder.instance.calls<=4,"corner and extreme-speed prefetch have a bounded job budget");
        var parked=new Rigidbody();var other=new VehicleWorldStreaming(parked);
        Check(!parked.isKinematic,"loading holds belong only to their own vehicle");stream.Dispose();other.Dispose();
        World();body.isKinematic=false;body.position=new Vector3(1000,200,1000);body.linearVelocity=Vector3.zero;
        Heightmap.missing.Add(ZoneSystem.GetZone(body.position));stream=new VehicleWorldStreaming(body);
        Check(!stream.Step(false)&&body.isKinematic&&HeightmapBuilder.instance.calls==0,"parked restored car waits for local collider without generating remote terrain");
        for(int i=0;i<40;i++)stream.Step(false);
        Heightmap.missing.Clear();Check(stream.Step(false)&&!body.isKinematic&&Zero(body.linearVelocity),"parked restored car safely resumes when its terrain loads");stream.Dispose();
        Console.WriteLine(checks+" streaming checks passed; native Unity streaming still needs a game test.");
    }
    static void CheckReady(VehicleWorldStreaming stream){if(!stream.Step())throw new Exception("scenery caused a false stop");}
    static void CheckHold(VehicleWorldStreaming stream){if(stream.Step())throw new Exception("missing terrain released hold");}
}
