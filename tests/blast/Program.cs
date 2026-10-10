using System;
using Rachel350Z;
using UnityEngine;
class BlastTests {
    static int checks;
    static readonly Character driver=new Character {player=true};
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static Collider At(float x,params Component[] components){var c=new Collider{closest=new Vector3(x,0,0)};c.parents.AddRange(components);return c;}
    static RaycastHit Impact(Collider collider)=>new RaycastHit{collider=collider};
    static void Blast(params Collider[] colliders){Physics.overlaps=colliders;TurretDamage.Blast(Impact(At(0)),driver);}
    static void Main() {
        Check(TurretCombatRules.BulletDamage*2<40&&TurretCombatRules.BulletDamage*3>=40,"40 HP ordinary target requires three bullets before native resistance");
        var enemy=new Character();var enemyCollider=At(.5f,enemy);
        TurretDamage.Bullet(Impact(enemyCollider),new Vector3(1,0,0),driver);
        Check(enemy.hits==1&&enemy.last.m_damage.m_pierce==14&&enemy.last.attacker==driver,"bullet uses reduced piercing damage and native attacker attribution");
        var tree=new TreeBase();var treeCollider=At(0,tree);
        Physics.overlaps=new[]{treeCollider,At(1,tree),treeCollider};
        TurretDamage.Blast(Impact(treeCollider),driver);
        Check(tree.hits==1&&tree.last.m_damage.m_chop==300&&tree.last.m_toolTier==4,"large direct-hit tree receives one native tier-four chopping hit despite duplicate colliders");
        Check(tree.last.attacker==driver&&tree.last.m_hitCollider==treeCollider,"environment hit retains owner and actual collider");
        var log=new TreeLog();var plant=new Destructible{kind=DestructibleType.Tree};
        Blast(At(1,log),At(1,plant));
        Check(log.hits==1&&plant.hits==1&&log.last.m_damage.m_chop<300,"fallen logs and destructible trees use radial chopping damage");
        var rock=new MineRock();var rock5=new MineRock5();var sectionA=At(.3f,rock);var sectionB=At(.6f,rock);var sectionC=At(.9f,rock5);
        Blast(sectionA,sectionA,sectionB,sectionC);
        Check(rock.hits==2&&rock5.hits==1&&rock.last.m_hitCollider==sectionB&&rock5.last.m_hitCollider==sectionC,"both native rock types damage each section exactly once");
        Check(rock.last.m_damage.m_pickaxe>0&&rock.last.m_damage.m_chop==0&&rock.last.m_toolTier==4,"rocks receive native tier-four pickaxe damage");
        TurretDamage.Bullet(Impact(treeCollider),new Vector3(),driver);TurretDamage.Bullet(Impact(sectionA),new Vector3(),driver);
        Check(tree.hits==1&&rock.hits==2,"ordinary ammunition cannot chop trees or mine rocks");
        var player=new Character{player=true};var tame=new Character{tamed=true};var neutral=new Character{enemy=false};var dead=new Character{dead=true};
        foreach(var excluded in new[]{driver,player,tame,neutral,dead}) {
            var collider=At(.5f,excluded);Blast(collider);TurretDamage.Bullet(Impact(collider),new Vector3(),driver);
            Check(excluded.hits==0,"player, tame, neutral, dead and driver targets remain protected");
        }
        var buildingTree=new TreeBase();var buildingRock=new MineRock();var prop=new Destructible();
        Blast(At(.5f,buildingTree,new Piece()),At(.5f,buildingRock,new WearNTear()),At(.5f,prop));
        Check(buildingTree.hits==0&&buildingRock.hits==0&&prop.hits==0,"pieces, structures and arbitrary destructible props stay protected");
        var coveredTree=new TreeBase();var coveredEnemy=new Character();
        Physics.cover=At(.1f,new Piece());Blast(At(.5f,coveredTree),At(1,coveredEnemy));
        Check(coveredTree.hits==0&&coveredEnemy.hits==0,"solid cover blocks environment and enemy splash");
        Physics.cover=At(.1f,coveredTree);Blast(At(.5f,coveredTree));
        Check(coveredTree.hits==1,"another collider of the same tree does not hide its surface");
        Physics.cover=sectionA;Blast(sectionB);
        Check(rock.hits==2,"rock sections behind another solid section are not damaged through it");
        Physics.cover=null;
        var far=new TreeBase();Blast(At(TurretBlastRules.Radius,far));
        Check(far.hits==0&&TurretBlastRules.EnvironmentDamage(float.NaN)==0,"radius boundary and invalid distances cause no damage");
        var duplicateEnemy=new Character();var farther=At(1,duplicateEnemy);var nearer=At(.2f,duplicateEnemy);Blast(farther,nearer,nearer);
        Check(duplicateEnemy.hits==1&&duplicateEnemy.last.m_hitCollider==nearer,"multi-collider creatures use their nearest surface exactly once");
        Check(duplicateEnemy.last.m_damage.m_blunt+duplicateEnemy.last.m_damage.m_fire<20,"enemy blast damage falls off from twenty at the center");
        var ownerless=new TreeBase();Physics.overlaps=new[]{At(.2f,ownerless)};
        TurretDamage.Blast(Impact(At(0)),null);TurretDamage.Blast(new RaycastHit(),driver);
        Check(ownerless.hits==0,"missing owner or impact cannot damage anything");
        Console.WriteLine(checks+" blast adapter checks passed. Native physics, RPCs and drops require an in-game test.");
    }
}
