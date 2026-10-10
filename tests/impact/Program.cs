using System;
using System.Reflection;
using Rachel350Z;
using UnityEngine;
class ImpactTests {
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static Collider Target(Component target) {
        var c=new Collider();c.parents[target.GetType()]=target;return c;
    }
    static void Main() {
        var unloaded=new VehicleImpact();
        unloaded.Step(null,false);
        unloaded.Step(new Character(),true);
        Check(true,"exit and world teardown tolerate a missing rigidbody and hull");
        foreach(string name in new[]{"Beech_small1","Beech_small2","FirTree_small","FirTree_small_dead","Bush01","Bush01_heath","Bush01_deepnorth","Bush02_en"})
            Check(VehicleImpactRules.SmallVegetation(name+"(Clone)"),"eligible vegetation: "+name);
        foreach(string name in new[]{null,"Beech1","FirTree","Beech_Sapling","Beech_Stub","rock1","wood_wall","Bush01_unknown"})
            Check(!VehicleImpactRules.SmallVegetation(name),"protected object: "+name);
        Check(VehicleImpactRules.Damage(0,true)==0&&VehicleImpactRules.Damage(.24f,true)==0
            &&VehicleImpactRules.Damage(2.49f,false)==0,"idle vegetation and slow character contacts cause no damage");
        Check(VehicleImpactRules.Damage(.25f,true)>80&&VehicleImpactRules.Damage(.25f,false)==0,
            "vegetation clears at 0.9 km/h without lowering character threshold");
        Check(VehicleImpactRules.Damage(float.NaN,false)==0&&VehicleImpactRules.Damage(float.PositiveInfinity,true)==0,"invalid speed rejected");
        Check(VehicleImpactRules.Damage(8,false)>VehicleImpactRules.Damage(3,false),"enemy damage grows with closing speed");
        Check(VehicleImpactRules.Damage(100,false)==300&&VehicleImpactRules.Damage(100,true)==500,"high-speed damage capped");
        var gate=new VehicleImpactCooldown();
        Check(gate.TryHit(1,0)&&!gate.TryHit(1,.1f)&&gate.TryHit(2,.1f),"multiple colliders share target cooldown, separate targets do not");
        Check(gate.TryHit(1,.81f),"later pass can damage the same target again");
        var body=new Rigidbody{linearVelocity=new Vector3(0,0,8)};
        var hull=new BoxCollider{size=new Vector3(1.62f,.8f,3.8f)};
        var car=new VehicleImpact();car.Initialize(body,hull,message=>throw new Exception(message));
        var driver=new Character{player=true};
        var enemy=new Character();var collider=Target(enemy);
        Physics.nearest=new RaycastHit{collider=collider,normal=new Vector3(0,0,-1)};
        car.Step(driver,true);
        Check(enemy.hits==1&&enemy.lastHit.m_damage.m_blunt==128&&enemy.lastHit.attacker==driver,"native enemy hit gets blunt damage and driver attribution");
        Check(enemy.lastHit.m_pushForce>0&&enemy.lastHit.m_hitCollider==collider,"enemy hit carries knockback and exact collider");
        Check(Math.Abs(Physics.castDistance-.19f)<.001f,"sweep is limited to one physics step plus contact margin");
        var collision=new Collision{collider=collider,contacts=new[]{new ContactPoint{normal=new Vector3(0,0,-1)}}};
        var contact=typeof(VehicleImpact).GetMethod("OnCollisionEnter",BindingFlags.Instance|BindingFlags.NonPublic);
        contact.Invoke(car,new object[]{collision});
        Check(enemy.hits==1,"collision callback cannot duplicate the sweep hit");
        Time.time=1;car.Step(null,false);contact.Invoke(car,new object[]{collision});
        Check(enemy.hits==1,"exit or blocked driving disables both sweep and contact damage");
        enemy.tamed=true;car.Step(driver,true);
        Check(enemy.hits==1,"tamed creature excluded");
        enemy.tamed=false;enemy.player=true;car.Step(driver,true);
        Check(enemy.hits==1,"players excluded");
        enemy.player=false;enemy.dead=true;car.Step(driver,true);
        Check(enemy.hits==1,"dead creature excluded");
        enemy.dead=false;enemy.enemy=false;car.Step(driver,true);
        Check(enemy.hits==1,"neutral creature excluded");
        enemy.enemy=true;body.linearVelocity=new Vector3(8,0,0);car.Step(driver,true);
        Check(enemy.hits==1,"glancing contact has no artificial forward damage");
        body.linearVelocity=new Vector3(0,-20,0);car.Step(driver,true);
        Check(enemy.hits==1,"falling onto a target does not count as driving into it");
        body.linearVelocity=new Vector3(0,0,-8);
        Physics.nearest=new RaycastHit{collider=collider,normal=new Vector3(0,0,1)};car.Step(driver,true);
        Check(enemy.hits==2&&enemy.lastHit.m_dir.z<0,"reverse impact has matching damage and push direction");
        var bush=new Destructible{name="Bush01(Clone)"};collider=Target(bush);
        Physics.nearest=new RaycastHit{collider=collider,normal=new Vector3(0,0,1)};car.Step(driver,true);
        Check(bush.hits==1&&bush.lastHit.m_damage.m_chop>0&&bush.lastHit.m_damage.m_blunt==0,"vegetation uses native chopping destruction");
        Time.time=2;collider.parents[typeof(Piece)]=new Piece();car.Step(driver,true);
        Check(bush.hits==1,"building component overrides vegetation eligibility");
        var wall=new Destructible{name="wood_wall(Clone)"};
        Physics.nearest=new RaycastHit{collider=Target(wall),normal=new Vector3(0,0,1)};car.Step(driver,true);
        Check(wall.hits==0&&enemy.hits==2,"nearest solid wall is neither damaged nor swept through");
        var own=new Collider{transform=car.transform};
        var enemyHit=new RaycastHit{collider=Target(enemy),normal=new Vector3(0,0,1),distance=.1f};
        Physics.hits=new[]{enemyHit,new RaycastHit{collider=own,distance=0}};
        car.Step(driver,true);
        Check(enemy.hits==3,"own overlapping hull cannot hide a forward target");
        Time.time=3;
        Physics.hits=new[]{enemyHit,new RaycastHit{collider=Target(wall),distance=.02f}};
        car.Step(driver,true);
        Check(enemy.hits==3&&wall.hits==0,"unsorted sweep selects nearer wall before enemy");
        enemyHit.distance=0;Physics.hits=new[]{enemyHit};car.Step(driver,true);
        Check(enemy.hits==3,"initial overlap waits for a real contact normal");
        Physics.hits=new RaycastHit[32];Physics.hits[0]=enemyHit;car.Step(driver,true);
        Check(enemy.hits==3,"full sweep buffer falls back to physical contacts");
        // Exercise slow forward/reverse sweeps and contact-only clearing separately.
        var sapling=new Destructible{name="Beech_small1(Clone)"};var saplingCollider=Target(sapling);
        var saplingHit=new RaycastHit{collider=saplingCollider,normal=new Vector3(0,0,-1),distance=.01f};
        body.linearVelocity=new Vector3(0,0,.4f);Physics.hits=new[]{saplingHit};car.Step(driver,true);
        Check(sapling.hits==1&&sapling.lastHit.m_damage.m_chop>80,"slow forward sweep chops a small tree");
        Check(Math.Abs(Physics.castDistance-.038f)<.001f,"slow sweep stays within one fixed step and contact margin");
        Time.time=4;body.linearVelocity=new Vector3(0,0,-.4f);saplingHit.normal=new Vector3(0,0,1);
        Physics.hits=new[]{saplingHit};car.Step(driver,true);
        Check(sapling.hits==2&&sapling.lastHit.m_dir.z<0,"slow reverse sweep clears vegetation in reverse direction");
        Time.time=5;Physics.hits=new[]{saplingHit,new RaycastHit{collider=Target(wall),distance=.005f}};
        car.Step(driver,true);Check(sapling.hits==2,"near wall blocks low-speed vegetation sweep");
        Physics.hits=new[]{new RaycastHit{collider=Target(enemy),normal=new Vector3(0,0,1),distance=.01f}};
        car.Step(driver,true);Check(enemy.hits==3,"slow sweep cannot damage a hostile character");
        Physics.hits=new RaycastHit[0];body.linearVelocity=Vector3.zero;car.Step(driver,true);
        collision=new Collision{collider=saplingCollider,relativeVelocity=new Vector3(0,0,.4f),
            contacts=new[]{new ContactPoint{normal=new Vector3(0,0,-1)}}};
        contact.Invoke(car,new object[]{collision});
        Check(sapling.hits==3,"collision incoming speed clears tree even after body was stopped by physics");
        contact.Invoke(car,new object[]{collision});
        Check(sapling.hits==3,"slow contact still observes per-target cooldown");
        Time.time=6;collision.relativeVelocity=Vector3.zero;car.Step(driver,true);
        contact.Invoke(car,new object[]{collision});Check(sapling.hits==3,"stationary occupied car does not chop on contact");
        car.Step(driver,true,1);
        Check(sapling.hits==3,"stationary throttle alone cannot sweep through nearby objects");
        collision.contacts[0]=new ContactPoint{normal=new Vector3(0,0,1)};
        contact.Invoke(car,new object[]{collision});Check(sapling.hits==3,"forward throttle does not chop a tree behind the car");
        collision.contacts[0]=new ContactPoint{normal=new Vector3(1,0,0)};
        contact.Invoke(car,new object[]{collision});Check(sapling.hits==3,"forward throttle does not chop a tree touching the side");
        collision.contacts[0]=new ContactPoint{normal=new Vector3(0,0,-1)};
        var stay=typeof(VehicleImpact).GetMethod("OnCollisionStay",BindingFlags.Instance|BindingFlags.NonPublic);
        stay.Invoke(car,new object[]{collision});
        Check(sapling.hits==4,"powered physical contact frees a car already stuck against a sapling");
        Time.time=7;car.Step(driver,true,-1);collision.contacts[0]=new ContactPoint{normal=new Vector3(0,0,1)};
        stay.Invoke(car,new object[]{collision});Check(sapling.hits==5&&sapling.lastHit.m_dir.z<0,"reverse powered contact frees a stuck car");
        Time.time=8;car.Step(null,false,-1);stay.Invoke(car,new object[]{collision});
        Check(sapling.hits==5,"parked or blocked car cannot clear vegetation even with stale throttle");
        car.Step(driver,true,-1);collision.collider=Target(enemy);collision.relativeVelocity=new Vector3(0,0,-8);
        stay.Invoke(car,new object[]{collision});
        Check(enemy.hits==3,"contact drive and vegetation velocity fallback never bypass character speed threshold");
        collision.collider=Target(new Destructible{name="FirTree(Clone)"});stay.Invoke(car,new object[]{collision});
        Check(collision.collider.GetComponentInParent<Destructible>().hits==0,"powered contact keeps large trees protected");
        collision.collider=saplingCollider;saplingCollider.parents[typeof(Piece)]=new Piece();stay.Invoke(car,new object[]{collision});
        Check(sapling.hits==5,"powered physical contact cannot damage a building component");
        Console.WriteLine(checks+" impact checks passed (test doubles, not Unity physics).");
    }
}
