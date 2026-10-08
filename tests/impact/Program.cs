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
        foreach(string name in new[]{"Beech_small1","Beech_small2","FirTree_small","FirTree_small_dead","Bush01","Bush01_heath","Bush01_deepnorth","Bush02_en"})
            Check(VehicleImpactRules.SmallVegetation(name+"(Clone)"),"eligible vegetation: "+name);
        foreach(string name in new[]{null,"Beech1","FirTree","Beech_Sapling","Beech_Stub","rock1","wood_wall","Bush01_unknown"})
            Check(!VehicleImpactRules.SmallVegetation(name),"protected object: "+name);
        Check(VehicleImpactRules.Damage(0,false)==0&&VehicleImpactRules.Damage(2.49f,true)==0,"stationary and parking-speed contacts cause no damage");
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
        Console.WriteLine(checks+" impact checks passed (test doubles, not Unity physics).");
    }
}
