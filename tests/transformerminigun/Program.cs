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
        Check(enemy.hits==1&&Near(enemy.last.m_damage.m_pierce,12),"an ordinary hostile body receives the increased twelve-pierce native hit");
        Check(enemy.last.attacker==driver&&enemy.last.m_hitCollider==collider&&enemy.last.m_point.x==1&&enemy.last.m_dir.z==1,"native attacker collider point and bore direction are retained");
        Check(enemy.last.m_staggerMultiplier==0&&enemy.last.m_backstabBonus==1&&enemy.last.m_blockable&&enemy.last.m_dodgeable&&enemy.last.m_ranged,"minigun uses native ranged hits without added stagger/backstab and retains block/dodge rules");
        for(int i=1;i<3;i++)TransformerMinigunDamage.Bullet(impact,direction,driver);
        Check(!enemy.dead&&enemy.health>0,"three ordinary hits leave a forty-HP normal-pierce body alive");
        TransformerMinigunDamage.Bullet(impact,direction,driver);Check(enemy.dead&&enemy.hits==4,"fourth ordinary hit kills the forty-HP Greydwarf after the requested damage increase");
        TransformerMinigunDamage.Bullet(impact,direction,driver);Check(enemy.hits==4,"already dead actors cannot receive another native damage dispatch");
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
    static void RangeAndFeedback() {
        Check(TransformerMinigunCombatRules.Range==250&&TransformerMinigunCombatRules.BulletDamage<TurretCombatRules.BulletDamage,"extended minigun range is 250 metres while each round remains weaker than the roof turret");
        var driver=new Character{player=true};var vehicle=new Transform();var direction=new Vector3(0,0,1);
        foreach(float distance in new[]{20f,75,150,200,249.9f,250,250.1f}) {
            var enemy=new Character{health=175,maxHealth=175};var body=new Collider{character=enemy};
            Physics.hits=new[]{new RaycastHit{collider=body,distance=distance,point=new Vector3(0,0,distance)}};
            bool hit=TurretTargeting.Ray(new Vector3(),direction,TransformerMinigunCombatRules.Range,vehicle,driver,out var impact,true);
            if(hit)TransformerMinigunDamage.Bullet(impact,direction,driver);
            Check(Physics.LastRange==250&&(distance<=250?enemy.health==163:enemy.health==175),"production trace/damage adapter observes finite extended range at "+distance+" metres");
        }
        var distant=new Character{health=175,maxHealth=175};var distantBody=new Collider{character=distant};var wall=new Collider();
        Physics.hits=new[]{new RaycastHit{collider=distantBody,distance=200},new RaycastHit{collider=wall,distance=10}};
        TurretTargeting.Ray(new Vector3(),direction,250,vehicle,driver,out var blocked);
        var noHit=TransformerMinigunDamage.Bullet(blocked,direction,driver);
        Check(distant.health==175&&noHit.Damage==0,"a nearer wall still blocks damage and cannot generate confirmed-hit feedback");
        foreach(float hp in new[]{175f,350,525,800,1600,2400}) {
            var enemy=new Character{health=hp,maxHealth=hp};var impact=new RaycastHit{collider=new Collider{character=enemy}};
            int expected=(int)Math.Ceiling(hp/12);
            for(int i=0;i<expected-1;i++)TransformerMinigunDamage.Bullet(impact,direction,driver);
            Check(!enemy.dead,"ordinary native damage leaves the "+hp+"-HP Fuling/Berserker alive until its last required round");
            var result=TransformerMinigunDamage.Bullet(impact,direction,driver);
            Check(enemy.dead&&enemy.hits==expected&&result.Health==0&&result.MaximumHealth==hp&&result.Damage>0,"last hit confirms death at "+expected+" rounds for "+hp+" HP");
        }
        var target=new Character{health=175,maxHealth=175,resultScale=.5f,label="fuling"};var targetImpact=new RaycastHit{collider=new Collider{character=target}};
        var observed=TransformerMinigunDamage.Bullet(targetImpact,direction,driver);
        Check(observed.Damage==6&&observed.Health==169&&observed.Name=="fuling","feedback reports observed native health loss rather than the unmodified damage request");
        var feedback=new TransformerMinigunFeedback();feedback.Observe(observed,200);
        Check(feedback.Visible&&feedback.Flash&&feedback.Distance==200&&feedback.Hit.Health==169,"confirmed distant hits carry their real health and muzzle distance without vanilla text range gating");
        target.ignoreDamage=true;var ignored=TransformerMinigunDamage.Bullet(targetImpact,direction,driver);
        var empty=new TransformerMinigunFeedback();empty.Observe(ignored,200);empty.Observe(noHit,200);
        Check(!empty.Visible&&ignored.Damage==0,"immune/dodged/blocked native outcomes cannot produce a false hit confirmation");
        feedback.Step(.1f,true);feedback.Step(.1f,true);Check(feedback.Visible&&!feedback.Flash,"hit marker flash is brief while the last health readout persists");
        feedback.Step(0,true);Check(feedback.Visible,"paused feedback time does not expire the readout");
        for(int i=0;i<26;i++)feedback.Step(.1f,true);Check(!feedback.Visible&&feedback.Hit.Name==null,"readout expires and releases the old target name");
        feedback.Observe(observed,200);feedback.Step(.1f,false);Check(!feedback.Visible,"inactive controls, exit and transformation clear old hit feedback");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1})feedback.Observe(observed,invalid);
        Check(!feedback.Visible,"invalid hit distances cannot poison the readout");
    }
    static void ExplosiveDispatch() {
        var driver=new Character{player=true};var enemy=new Character{health=100,maxHealth=100};
        var impact=new RaycastHit{collider=new Collider{character=enemy}};var direction=new Vector3(0,0,1);
        int explosions=0;bool afterBullet=false;
        var hit=TransformerMinigunDamage.Bullet(impact,direction,driver,()=>{
            explosions++;afterBullet=enemy.health==88;
            enemy.Damage(new HitData{m_damage=new HitData.DamageTypes{m_pierce=20}});
        });
        Check(explosions==1&&afterBullet&&hit.Health==68&&hit.Damage==32,"one explosive impact follows the direct bullet and feedback observes their combined actual health loss");
        var ground=new RaycastHit{collider=new Collider()};
        var environment=TransformerMinigunDamage.Bullet(ground,direction,driver,()=>explosions++);
        Check(explosions==2&&environment.Damage==0,"terrain or object impacts can explode without inventing a direct creature confirmation");
        var friendly=new Character{tamed=true};
        var protectedImpact=new RaycastHit{collider=new Collider{character=friendly}};
        var protectedHit=TransformerMinigunDamage.Bullet(protectedImpact,direction,driver,()=>explosions++);
        Check(explosions==3&&friendly.hits==0&&protectedHit.Damage==0,"explosive composition preserves friendly direct-hit protection");
        TransformerMinigunDamage.Bullet(default(RaycastHit),direction,driver,()=>explosions++);
        TransformerMinigunDamage.Bullet(ground,direction,null,()=>explosions++);
        driver.dead=true;TransformerMinigunDamage.Bullet(ground,direction,driver,()=>explosions++);
        Check(explosions==3,"missing impact or dead/missing owner cannot create an explosion");
        driver.dead=false;var dying=new Character{health=30,maxHealth=30,healthFallbackAfterDeath=true};
        var fatal=TransformerMinigunDamage.Bullet(new RaycastHit{collider=new Collider{character=dying}},direction,driver,
            ()=>dying.Damage(new HitData{m_damage=new HitData.DamageTypes{m_pierce=20}}));
        Check(dying.dead&&fatal.Health==0&&fatal.Damage==30,"death remains confirmed when native health access falls back to maximum after ZDO destruction");
    }
    static void Main(){Model();Damage();RangeAndFeedback();ExplosiveDispatch();Console.WriteLine(checks+" minigun model/trace/damage/feedback checks passed; no native physics skinning audio or resistance simulation executed.");}
}
