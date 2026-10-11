using System;
using Rachel350Z;
using UnityEngine;
class TargetingTests {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static void Main() {
        var root=new Transform();var vehicle=new Transform();var owner=new Character{player=true};
        var enemy=new Character();enemy.transform.position=new Vector3(0,0,15);
        var enemyCollider=new Collider{character=enemy};enemyCollider.transform.parent=enemy.transform;
        var ownerCollider=new Collider{character=owner};ownerCollider.transform.parent=owner.transform;
        var wall=new Collider();var hull=new Collider();hull.transform.parent=vehicle;
        var targetHit=new RaycastHit{collider=enemyCollider,distance=15};
        Physics.hits=new[]{targetHit};
        Check(TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle),"visible hostile in front can be acquired");
        foreach(var blocker in new[]{wall,hull,ownerCollider}) {
            Physics.hits=new[]{targetHit,new RaycastHit{collider=blocker,distance=3}};
            Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle),"nearer wall, own hull or owner blocks acquisition despite unsorted hits");
            Check(!TurretTargeting.CanFire(enemy,owner,true,true,blocker),"blocker at bore stops fire even when sight is aligned");
        }
        Physics.hits=new[]{targetHit,new RaycastHit{collider=ownerCollider,distance=1},new RaycastHit{collider=hull,distance=2}};
        TurretTargeting.Ray(new Vector3(),new Vector3(0,0,1),150,vehicle,owner,out var bore);
        Check(bore.collider==hull,"manual bore ignores seated driver but retains own hull");
        TurretTargeting.Ray(new Vector3(),new Vector3(0,0,1),150,vehicle,owner,out var camera,true);
        Check(camera.collider==enemyCollider,"manual camera can look over own hull");
        Physics.hits=new[]{targetHit};
        enemy.player=true;Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle),"players never acquired");enemy.player=false;
        enemy.tamed=true;Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle)&&!TurretTargeting.CanFire(enemy,owner,true,true,enemyCollider),"tamed creature cancels acquisition and fire");enemy.tamed=false;
        enemy.dead=true;Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle),"dead targets discarded");enemy.dead=false;
        enemy.hostile=false;Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle),"neutral creatures excluded");enemy.hostile=true;
        enemy.transform.position=new Vector3(0,0,-5);Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle),"rear targets cannot be acquired");
        enemy.transform.position=new Vector3(0,0,31);Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle),"targets outside thirty metres excluded");
        enemy.transform.position=new Vector3(0,12,45);Check(TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle,60),"horde auto reaches flying bosses at extended range");
        enemy.transform.position=new Vector3(0,0,61);Check(!TurretTargeting.VisibleEnemy(enemy,owner,root,new Vector3(),vehicle,60),"extended horde auto still has a finite range");
        enemy.transform.position=new Vector3(0,0,15);
        Check(!TurretTargeting.CanFire(enemy,owner,false,true,enemyCollider)&&!TurretTargeting.CanFire(enemy,owner,true,false,enemyCollider),"no fire before alignment or without actual bore hit");
        Check(TurretTargeting.CanFire(enemy,owner,true,true,enemyCollider),"aligned bore on verified hostile permits fire");
        Check(!TurretTargeting.CanFire(enemy,null,true,true,enemyCollider),"missing owner fails closed");
        Console.WriteLine(checks+" targeting checks passed. Physics API responses are controlled test doubles.");
    }
}
