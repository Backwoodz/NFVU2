using System;
using Rachel350Z;
static class HordeRunChecks {
    static int checks;
    static void Check(bool pass,string name){if(!pass)throw new Exception(name);checks++;}
    public static int Run() {
        var run=new HordeRun();
        Check(!run.Damage(50)&&!run.Collect(HordePowerup.Explosive),"inactive mode cannot absorb hits or grant buffs");
        run.Begin();Check(run.Health==1000&&run.Shield==300,"new run begins with full HP/shield");
        Check(!run.Collect(HordePowerup.Repair)&&!run.Collect(HordePowerup.Shield),"full bars leave repair/shield pickups available");
        run.Damage(200);Check(run.Shield==100&&run.Health==1000,"shield absorbs first damage");
        run.Damage(200);Check(run.Shield==0&&run.Health==900,"shield overflow damages HP exactly");
        foreach(float invalid in new[]{-1,0,float.NaN,float.PositiveInfinity})Check(!run.Damage(invalid),"invalid damage ignored");
        run.Collect(HordePowerup.Repair);Check(run.Health==1000,"repair clamps to maximum");
        run.Collect(HordePowerup.Shield);Check(run.Shield==150,"shield rune restores 150");
        run.Collect(HordePowerup.Shield);Check(run.Shield==300,"shield refill clamps to maximum");
        run.Collect(HordePowerup.Automatic);run.Tick(4);Check(run.AutoLeft==11,"auto buff spends active game time");
        run.Tick(0);run.Tick(float.NaN);Check(run.AutoLeft==11,"pause and invalid time freeze buffs");
        run.Collect(HordePowerup.Automatic);Check(run.AutoLeft==15,"repeat auto pickup refreshes rather than stacking forever");
        run.Tick(15);Check(run.AutoLeft==0,"auto aim expires exactly");
        run.Collect(HordePowerup.Explosive);int explosive=0;
        for(int i=0;i<50;i++)if(run.ConsumeExplosiveShot())explosive++;
        Check(explosive==30&&run.ExplosiveRounds==0,"one pickup authorizes exactly 30 shots including misses");
        run.Collect(HordePowerup.Explosive);run.ConsumeExplosiveShot();run.Collect(HordePowerup.Explosive);
        Check(run.ExplosiveRounds==30,"another magazine replaces remaining bonus rounds");
        run.Begin();for(int i=0;i<5;i++)run.Kill("Greyling");
        Check(run.Score==60&&run.Multiplier==2&&run.Kills==5,"five quick kills raise score multiplier");
        run.Tick(5);Check(run.Combo==0&&run.Multiplier==1&&run.BestCombo==5,"combo expires while best streak survives");
        for(int i=0;i<30;i++)run.Kill("Greydwarf");Check(run.Multiplier==5,"combo multiplier is capped");
        run.Kill("Troll");Check(run.BestCombo==31,"boss kill counts in streak");
        int before=run.Score;int finished=run.Finish(true);
        Check(finished>before&&!run.Active&&run.ExplosiveRounds==0&&run.AutoLeft==0,"victory bonus closes temporary combat state");
        Check(run.Finish(true)==finished,"victory cannot award a second bonus");
        run.Kill("Troll");Check(run.Score==finished,"cleanup deaths cannot award points");
        run.Begin();run.Damage(100000);Check(run.Dead&&run.Health==0&&run.Shield==0,"lethal hit clamps at zero");
        Check(!run.Collect(HordePowerup.Repair),"pickup cannot revive a lost run");
        Check(run.Finish(true)==0,"dead run never receives victory bonus");
        Check(HordeRun.CrashDamage(8)==0&&HordeRun.CrashDamage(18)==100&&HordeRun.CrashDamage(1000)==200,"crash threshold and damage cap");
        Check(HordeRun.CrashDamage(float.NaN)==0,"invalid collision speed does not harm car");
        var gate=new HordeHitGate();
        Check(gate.Accept(1,0)&&!gate.Accept(1,.1f),"multiple hit colliders from one enemy deduplicate");
        Check(gate.Accept(2,.1f),"different enemies can hit simultaneously");
        Check(gate.Accept(1,.31f),"later enemy attack is accepted");
        gate.Clear();Check(gate.Accept(1,0),"new encounter clears damage gate");
        gate.Clear();run.Begin();int bossHits=0;
        for(int i=0;i<10;i++)if(gate.Accept(10,i*.1f,HordeIncomingDamage.BossHitInterval)) {
            bossHits++;run.Damage(HordeIncomingDamage.Attack(2000,HordePact.None,true,true));
        }
        Check(bossHits==1&&run.Health==1000&&run.Shield==75,"a heavy enraged boss volley within one second cannot stack into a full-health one-shot");
        Check(gate.Accept(10,1,HordeIncomingDamage.BossHitInterval),"boss can hurt again after the protection interval");
        Check(gate.Accept(11,.4f)&&gate.Accept(11,.71f),"boss protection does not alter other enemies' hit cadence");
        Check(HordeIncomingDamage.Attack(2000,HordePact.None,true,false)==180&&HordeIncomingDamage.Attack(2000,HordePact.Valkyrie,true,true)==225,"boss spike ceiling is applied after all modifiers");
        Check(Math.Abs(HordeIncomingDamage.Attack(100,HordePact.None,true,false)-66)<.001f,"ordinary boss hits are reduced without increasing weak attacks to the cap");
        Check(Math.Abs(HordeIncomingDamage.Attack(100,HordePact.None,false,true)-120)<.001f,"ordinary enemies retain their accepted 20 percent damage tuning even during boss rage");
        foreach(float invalid in new[]{-1,0,float.NaN,float.PositiveInfinity})Check(HordeIncomingDamage.Attack(invalid,HordePact.None,true,true)==0,"invalid boss damage stays harmless");
        run.Begin();run.BeginWave(HordePact.Thor);
        for(int i=0;i<100;i++)Check(run.ConsumeExplosiveShot(),"Thor supplies multiple explosive magazines");
        Check(run.Explosive&&run.ExplosiveRounds==0,"Thor HUD works without pickup ammunition");
        run.Collect(HordePowerup.Automatic);run.EndWave();
        Check(run.Pact==HordePact.None&&!run.Explosive&&run.AutoLeft==0&&!run.ConsumeExplosiveShot(),"wave end expires pact and powerups");
        run.BeginWave(HordePact.Odin);run.BeginWave(HordePact.Iron);
        Check(run.Pact==HordePact.Iron&&HordePacts.Reload(run.Pact)==1&&HordePacts.Impact(run.Pact)==2&&HordePacts.Turret(run.Pact)==.75f,"pacts replace rather than stack");
        Check(HordePacts.Speed(HordePact.Odin)==1.2f&&HordePacts.Reload(HordePact.Odin)==.6f&&HordePacts.Health(HordePact.Thor)==1.35f,"pact tradeoffs match selected balance");
        run.Damage(800);before=run.Score;run.Bonus();
        Check(run.Health==750&&run.Score==before+500,"hunt reward repairs vehicle and grants score");
        run.Bonus();run.Bonus();Check(run.Health==1000,"bonus repair cannot exceed maximum HP");
        run.Finish(false);Check(run.Pact==HordePact.None,"cancel or defeat clears pact");
        run.Begin();run.BeginWave(HordePact.Valkyrie);run.Tick(200);
        Check(run.Automatic&&run.AutoLeft==0,"Valkyrie auto survives beyond pickup lifetime");
        run.EndWave();Check(!run.Automatic,"Valkyrie auto expires at wave end");
        var random=new Random(937);HordePact[] offers=null;var seen=new System.Collections.Generic.HashSet<HordePact>();
        for(int draw=0;draw<60;draw++) {
            int wave=draw%5+1;
            var next=HordePacts.Draw(random,offers,wave);var unique=new System.Collections.Generic.HashSet<HordePact>(next);
            if(wave==5)Check(!unique.Contains(HordePact.Valkyrie),"final wave never offers permanent auto turret");
            Check(unique.Count==3&&!unique.Contains(HordePact.None),"three distinct positive offers plus separate neutral option");
            int overlap=0;if(offers!=null)foreach(var pact in offers)if(unique.Contains(pact))overlap++;
            Check(overlap<=1,"at least two fresh offers per successive wave");
            foreach(var pact in next)seen.Add(pact);offers=next;
        }
        Check(seen.Count==6,"all six pacts occur across runs");
        Check(Math.Abs(HordePacts.EnemyDamage(HordePact.None)-1.2f)<.001f&&Math.Abs(HordePacts.EnemyDamage(HordePact.Valkyrie)-1.5f)<.001f,"enemy damage tuning combines multiplicatively with pact drawback");
        var frost=new HordeFrostTimer();Check(frost.TryFreeze(),"first ice hit freezes");
        frost.Tick(.6f);Check(!frost.TryFreeze()&&frost.Left<.7f,"rapid ice hits do not extend freeze indefinitely");
        frost.Tick(.7f);Check(frost.Left==0&&!frost.TryFreeze(),"thawed enemy gets temporary freeze immunity");
        frost.Tick(3);Check(frost.TryFreeze(),"ice works again after immunity expires");
        Check(HordeLightningRules.Damage(0)>HordeLightningRules.Damage(1)&&HordeLightningRules.Damage(3)>0&&HordeLightningRules.Damage(4)==0,"chain damage decreases and stops after three jumps");
        return checks;
    }
}
