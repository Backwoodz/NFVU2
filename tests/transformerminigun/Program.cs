using System;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static TransformerMinigunModel Armed() {var m=new TransformerMinigunModel();m.Step(.01f,true,false);return m;}
    static int FireFor(TransformerMinigunModel m,float seconds,int hz) {
        int shots=0;for(int i=0;i<(int)Math.Round(seconds*hz);i++)shots+=m.Step(1f/hz,true,true);return shots;
    }
    static void Model() {
        var m=new TransformerMinigunModel();
        Check(TransformerMinigunModel.Capacity==1000&&m.Ammo==1000,"each fresh vehicle owns a full thousand-round magazine");
        Check(TransformerMinigunModel.RoundsPerMinute==600&&Near(TransformerMinigunModel.WindupSeconds,1.5f)&&Near(TransformerMinigunModel.ReloadDuration,3.5f),"rate spin-up and reload match the extracted Rust prefab timings");
        Check(FireFor(m,3,60)==0&&m.Ammo==1000&&m.Spin==0,"held trigger across robot activation requires release before any spin or round");
        m.Step(.01f,true,false);
        Check(FireFor(m,1.4f,100)==0&&m.Spin>.9f,"wind-up rotates barrels without prematurely issuing damage");
        Check(FireFor(m,.2f,100)>=1&&m.Ammo<1000,"first round becomes available only after completed spin-up");
        Check(m.Firing,"sustained trigger exposes firing state for continuous original audio");
        int ammo=m.Ammo;m.Step(.1f,true,false);
        Check(m.Ammo==ammo&&!m.Firing&&m.Spin<1&&m.Spin>0,"trigger release stops rounds immediately while barrels coast down");
        m.Step(.1f,false,true);
        Check(m.Spin==0&&!m.Firing&&m.Ammo==ammo,"pause dismount mount and return gate stop instantly without refilling");
        Check(FireFor(m,2,60)==0,"held resume does not replay a paused burst");
        m.Step(.01f,true,false);FireFor(m,2,60);m.Step(0,true,true);int afterZero=m.Ammo;
        Check(m.Spin==0&&m.Shots==0&&m.Ammo==afterZero,"zero game time safely stops firing");
        foreach(float dt in new[]{float.NaN,float.PositiveInfinity,-1})m.Step(dt,true,true);
        Check(m.Shots==0&&m.Spin==0,"invalid frame timing never creates rounds or nonfinite spin");
        int minimum=int.MaxValue,maximum=0;
        foreach(int hz in new[]{30,60,144,240}) {
            var sample=Armed();int fired=FireFor(sample,11.5f,hz);
            minimum=Math.Min(minimum,fired);maximum=Math.Max(maximum,fired);
        }
        Check(minimum>=99&&maximum<=102&&maximum-minimum<=2,"ten seconds after spin-up deliver about one hundred rounds across render rates");
        m=Armed();FireFor(m,2,60);int hitch=m.Step(100,true,true);
        Check(hitch<=TransformerMinigunModel.MaximumShotsPerFrame&&hitch<=2,"a one-hundred-second hitch cannot backlog thousands of bullets");
        ammo=m.Ammo;m.Step(.02f,true,true,true);
        Check(m.ReloadStarted&&m.ReloadLeft>3&&m.Shots==0&&m.Spin==0,"manual partial-magazine reload stops firing and starts one timed cycle");
        float remaining=m.ReloadLeft;for(int i=0;i<100;i++)m.Step(.1f,false,true);
        Check(m.ReloadLeft==remaining&&m.Ammo==ammo,"blocked reload neither advances nor silently refills");
        m.Step(.01f,true,false);for(int i=0;i<40;i++)m.Step(.1f,true,false);
        Check(m.Ammo==1000&&m.ReloadLeft==0,"reload completes to exactly one thousand rounds while allowed");
        m.Step(.02f,true,false,true);Check(!m.ReloadStarted&&m.ReloadLeft==0,"manual reload of a full magazine is ignored");
        m=Armed();int total=0;bool empty=false;
        for(int i=0;i<15000;i++){total+=m.Step(.01f,true,true);if(m.ReloadStarted){empty=true;break;}}
        Check(empty&&total==1000&&m.Ammo==0&&Near(m.ReloadLeft,3.5f),"the thousandth round triggers automatic reload without negative ammo");
        Check(!m.Firing&&m.Spin==0,"an exhausted magazine stops the fire loop and barrel drive");
        var other=new TransformerMinigunModel();Check(other.Ammo==1000&&m.Ammo==0,"parked vehicles cannot share another vehicle's ammunition");
        m.Stop();Check(m.Ammo==0&&m.ReloadLeft>0,"explicit runtime stop preserves both ammo and incomplete reload");
    }
    static void Damage() {
        var driver=new Character{player=true};var enemy=new Character();var collider=new Collider{character=enemy};
        var impact=new RaycastHit{collider=collider,point=new Vector3(1,2,3)};var direction=new Vector3(0,0,1);
        TransformerMinigunDamage.Bullet(impact,direction,driver);
        Check(enemy.hits==1&&Near(enemy.last.m_damage.m_pierce,3.34f),"an ordinary hostile body receives one small native pierce hit");
        Check(enemy.last.attacker==driver&&enemy.last.m_hitCollider==collider&&enemy.last.m_point.x==1&&enemy.last.m_dir.z==1,"native attacker collider point and bore direction are retained");
        Check(enemy.last.m_staggerMultiplier==0&&enemy.last.m_backstabBonus==1&&enemy.last.m_blockable&&enemy.last.m_dodgeable,"minigun adds no stagger or backstab multiplier and retains native block dodge rules");
        for(int i=1;i<11;i++)TransformerMinigunDamage.Bullet(impact,direction,driver);
        Check(!enemy.dead&&enemy.health>0,"eleven ordinary hits do not kill a forty-HP normal-pierce body");
        TransformerMinigunDamage.Bullet(impact,direction,driver);Check(enemy.dead&&enemy.hits==12,"twelfth ordinary hit reaches the requested Greydwarf baseline");
        TransformerMinigunDamage.Bullet(impact,direction,driver);Check(enemy.hits==12,"already dead actors cannot receive another native damage dispatch");
        bool protectedActors=true;
        foreach(var target in new[]{new Character{player=true},new Character{tamed=true},new Character{hostile=false},driver}) {
            collider.character=target;TransformerMinigunDamage.Bullet(impact,direction,driver);protectedActors&=target.hits==0;
        }
        Check(protectedActors,"players tamed neutral actors and the rider cannot be damaged");
        enemy=new Character();collider.character=enemy;collider.piece=new Piece();TransformerMinigunDamage.Bullet(impact,direction,driver);
        collider.piece=null;collider.wear=new WearNTear();TransformerMinigunDamage.Bullet(impact,direction,driver);
        Check(enemy.hits==0,"building and wear-and-tear collider ancestry prevents creature damage dispatch");
        collider.wear=null;driver.dead=true;TransformerMinigunDamage.Bullet(impact,direction,driver);
        TransformerMinigunDamage.Bullet(impact,direction,null);TransformerMinigunDamage.Bullet(default(RaycastHit),direction,driver);
        Check(enemy.hits==0,"missing impact owner or a dead rider is harmless");
        collider.character=null;driver.dead=false;TransformerMinigunDamage.Bullet(impact,direction,driver);
        Check(enemy.hits==0,"terrain rocks and other non-character colliders receive no destructive calls");
    }
    static void Main(){Model();Damage();Console.WriteLine(checks+" minigun model/damage checks passed; no native physics skinning audio or resistance simulation executed.");}
}
