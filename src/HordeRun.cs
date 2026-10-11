using System;
using System.Collections.Generic;
namespace Rachel350Z {
public enum HordePowerup {Explosive,Automatic,Shield,Repair}
// Arcade balance and scoring; no Unity or persistent vehicle settings.
public sealed class HordeRun {
    public const float MaxHealth=1000,MaxShield=300,AutoDuration=15,ComboWindow=5;
    public bool Active {get;private set;}
    public float Health {get;private set;}
    public float Shield {get;private set;}
    public float AutoLeft {get;private set;}
    public float ComboLeft {get;private set;}
    public float Seconds {get;private set;}
    public float DamageTaken {get;private set;}
    public int ExplosiveRounds {get;private set;}
    public int Kills {get;private set;}
    public int Score {get;private set;}
    public int Combo {get;private set;}
    public int BestCombo {get;private set;}
    public int Multiplier=>Math.Min(5,1+Combo/5);
    public HordePact Pact {get;private set;}
    public bool Explosive=>Pact==HordePact.Thor||ExplosiveRounds>0;
    public bool Automatic=>Pact==HordePact.Valkyrie||AutoLeft>0;
    public void BeginWave(HordePact pact){Pact=pact;AutoLeft=ComboLeft=0;Combo=ExplosiveRounds=0;}
    public void EndWave(){Pact=HordePact.None;AutoLeft=ComboLeft=0;ExplosiveRounds=Combo=0;}
    public void Bonus(){if(Active&&!Dead){Score+=500;Health=Math.Min(MaxHealth,Health+250);}}
    public bool Dead=>Active&&Health<=0;
    public void Begin(){Active=true;Pact=HordePact.None;Health=MaxHealth;Shield=MaxShield;AutoLeft=ComboLeft=Seconds=DamageTaken=0;ExplosiveRounds=Kills=Score=Combo=BestCombo=0;}
    static bool Positive(float value)=>value>0&&!float.IsNaN(value)&&!float.IsInfinity(value);
    public void Tick(float dt) {
        if(!Active||!Positive(dt)||Dead)return;
        Seconds+=dt;AutoLeft=Math.Max(0,AutoLeft-dt);ComboLeft=Math.Max(0,ComboLeft-dt);
        if(ComboLeft==0)Combo=0;
    }
    public bool Damage(float amount) {
        if(!Active||Dead||!Positive(amount))return false;
        float absorbed=Math.Min(Shield,amount);Shield-=absorbed;amount-=absorbed;
        float lost=Math.Min(Health,amount);Health-=lost;DamageTaken+=absorbed+lost;return true;
    }
    public bool Collect(HordePowerup kind) {
        if(!Active||Dead)return false;
        switch(kind) {
            case HordePowerup.Explosive:ExplosiveRounds=30;break;
            case HordePowerup.Automatic:AutoLeft=AutoDuration;break;
            case HordePowerup.Shield:if(Shield>=MaxShield)return false;Shield=Math.Min(MaxShield,Shield+150);break;
            case HordePowerup.Repair:if(Health>=MaxHealth)return false;Health=Math.Min(MaxHealth,Health+250);break;
            default:return false;
        }
        return true;
    }
    public bool ConsumeExplosiveShot(){if(!Active||Dead)return false;if(Pact==HordePact.Thor)return true;if(ExplosiveRounds<=0)return false;ExplosiveRounds--;return true;}
    public void Kill(string prefab) {
        if(!Active||Dead)return;
        int points=prefab=="Dragon"?2000:prefab=="Hatchling"?100:prefab=="Troll"?500:prefab=="GoblinBrute"?250:prefab=="Goblin"?70:prefab=="Draugr"?50:prefab=="Wolf"?35:prefab=="Skeleton"?20:prefab=="Greydwarf_Elite"?100:prefab=="Greydwarf_Shaman"?80:prefab=="Greydwarf"?30:10;
        Kills++;Combo++;BestCombo=Math.Max(BestCombo,Combo);ComboLeft=ComboWindow;Score+=points*Multiplier;
    }
    public int Finish(bool victory) {
        if(!Active)return Score;
        if(victory&&!Dead)Score+=2000+(int)Health*2+(int)Shield+(int)Math.Max(0,1800-Seconds*3);
        Active=false;EndWave();ComboLeft=0;return Score;
    }
    public string Medal(bool victory)=>!victory?"Versuch beendet":Health>=750&&Seconds<=300?"Gold":Health>=400?"Silber":"Bronze";
    public static float CrashDamage(float closingSpeed)=>Positive(closingSpeed)?Math.Min(200,Math.Max(0,closingSpeed-8)*10):0;
}
// Encounter-only incoming damage; ordinary enemy tuning stays separate from boss spikes.
public static class HordeIncomingDamage {
    public const float BossHitInterval=1f;
    public static float Attack(float raw,HordePact pact,bool boss,bool enraged) {
        if(raw<=0||float.IsNaN(raw)||float.IsInfinity(raw))return 0;
        float damage=raw*HordePacts.EnemyDamage(pact);
        if(!boss)return damage;
        // Tame native breath/projectile spikes after every multiplier, including rage.
        return Math.Min(damage*.55f*(enraged?1.25f:1),enraged?225:180);
    }
}
// Multiple native colliders / a driver and hull hit from one swing count once.
public sealed class HordeHitGate {
    readonly Dictionary<int,float> last=new Dictionary<int,float>();
    public bool Accept(int source,float time,float interval=.3f) {
        if(float.IsNaN(time)||float.IsInfinity(time)||interval<=0||float.IsNaN(interval)||float.IsInfinity(interval))return false;
        if(last.TryGetValue(source,out var before)&&time-before<interval)return false;
        last[source]=time;return true;
    }
    public void Clear()=>last.Clear();
}
}
