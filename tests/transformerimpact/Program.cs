using System;
using Rachel350Z;
using UnityEngine;
class TransformerImpactTests {
    static int checks;
    static readonly Character driver=new Character {player=true};
    static readonly Transform car=new Transform();
    static readonly Vector3 forward=new Vector3(0,0,1);
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static Collider At(float z,params Component[] parents){var c=new Collider {closest=new Vector3(0,1,z)};c.parents.AddRange(parents);return c;}
    static void Hit(TransformerImpact impact,float speed=4,float input=1,Character owner=null) {
        Time.time+=1;impact.Step(car,owner??driver,car.position,forward,speed,input);
    }
    static void Main() {
        var impact=new TransformerImpact();var tree=new TreeBase();var treeCollider=At(.8f,tree);
        Physics.overlaps=new[]{treeCollider,At(1,tree),treeCollider};Hit(impact,0);
        Check(tree.hits==1&&tree.last.m_damage.m_chop==2000&&tree.last.m_toolTier==6,"all native standing tree species use tier-six chopping even when blocked at rest, once per tree");
        Check(tree.last.attacker==driver&&tree.last.m_hitCollider==treeCollider&&tree.last.m_dir.z==1,"tree felling retains the driver, contact and forward fall direction");
        impact.Step(car,driver,car.position,forward,4,1);
        Check(tree.hits==1,"multiple fixed updates do not bypass the target cooldown");
        var log=new TreeLog();var sapling=new Destructible{kind=DestructibleType.Tree};
        Physics.overlaps=new[]{At(1,log),At(1,sapling)};Hit(impact);
        Check(log.hits==1&&sapling.hits==1,"fallen trunks and destructible saplings receive native tree damage");
        Check(TransformerImpact.PassThroughVegetation(treeCollider)&&TransformerImpact.PassThroughVegetation(At(1,log))
            &&TransformerImpact.PassThroughVegetation(At(1,sapling)),"standing trees, freshly falling logs and stumps cannot veto robot movement while damage is pending");
        Check(!TransformerImpact.PassThroughVegetation(At(1,new Piece(),new TreeBase()))
            &&!TransformerImpact.PassThroughVegetation(At(1,new Character()))
            &&!TransformerImpact.PassThroughVegetation(At(1,new Destructible()))
            &&!TransformerImpact.PassThroughVegetation(new Collider()),"vegetation pass-through never opens a path through buildings, creatures or rocks");
        var loot=At(1,new ItemDrop());var debris=At(1);debris.gameObject.layer=LayerMask.NameToLayer("effect");
        var gibs=At(1,new Gibber());
        Check(TransformerImpact.IgnoreMovementCollider(loot)&&TransformerImpact.IgnoreMovementCollider(debris)
            &&TransformerImpact.IgnoreMovementCollider(gibs),"loose wood drops and independent destruction fragments are excluded from both obstacle and ground queries");
        Check((TransformerImpact.CollisionMask&(1<<debris.gameObject.layer))==0,"effect fragments cannot fill the limited physics query buffers");
        Check(!TransformerImpact.IgnoreMovementCollider(At(1,new Piece(),new ItemDrop()))
            &&!TransformerImpact.IgnoreMovementCollider(At(1,new Character(),new Gibber()))
            &&!TransformerImpact.IgnoreMovementCollider(new Collider()),"loot/debris exclusions retain buildings, living creatures and ordinary solid obstacles");
        var enemy=new Character();Physics.overlaps=new[]{At(1,enemy),At(.8f,enemy)};Hit(impact,8.5f);
        Check(enemy.hits==1&&enemy.last.m_damage.m_blunt>250&&enemy.last.m_pushForce==80,"a sprinting machine hits a small enemy once with heavy knockback");
        Hit(impact,0);Check(enemy.hits==1,"stationary movement intent never damages enemies");
        var protectedTargets=new[]{driver,new Character{player=true},new Character{tamed=true},new Character{enemy=false},new Character{boss=true},new Character{dead=true},new Character{height=4},new Character{radius=1.5f}};
        var scaled=new Character();scaled.transform.lossyScale=new Vector3(2,2,2);
        var list=new System.Collections.Generic.List<Collider>{At(1,scaled)};
        foreach(var c in protectedTargets)list.Add(At(1,c));Physics.overlaps=list.ToArray();Hit(impact);
        Check(Array.TrueForAll(protectedTargets,c=>c.hits==0)&&scaled.hits==0,"players, tames, neutral creatures, bosses and oversized enemies remain protected");
        var buildingTree=new TreeBase();var buildingLog=new TreeLog();var prop=new Destructible();
        Physics.overlaps=new[]{At(1,buildingTree,new Piece()),At(1,buildingLog,new WearNTear()),At(1,prop)};Hit(impact);
        Check(buildingTree.hits==0&&buildingLog.hits==0&&prop.hits==0,"buildings, rocks and arbitrary props are never demolition targets");
        var covered=new TreeBase();Physics.overlaps=new[]{At(1,covered)};Physics.cover=new[]{At(.5f,new Piece())};Hit(impact);
        Check(covered.hits==0,"solid walls prevent damage through cover");Physics.cover=Array.Empty<Collider>();
        Physics.cover=new[]{loot,debris,gibs};Hit(impact);
        Check(covered.hits==1,"wood drops and fragments never shield the next tree from contact damage");Physics.cover=Array.Empty<Collider>();
        var rear=new TreeBase();Physics.overlaps=new[]{At(-1,rear)};Hit(impact);Check(rear.hits==0,"walking forward does not fell trees behind the robot");
        Physics.overlaps=new[]{treeCollider};Hit(impact,4,0);Check(tree.hits==1,"idle and turning-only input never trigger contact damage");
        Time.time+=1;impact.Step(car,null,car.position,forward,8.5f,1);Check(tree.hits==1,"missing driver never triggers contact damage");
        Console.WriteLine(checks+" Transformer contact checks passed; native physics and falling logs require gameplay testing.");
    }
}
