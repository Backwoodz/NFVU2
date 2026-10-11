using System;
using System.Collections.Generic;
namespace Rachel350Z {
public enum HordePact {None,Odin,Thor,Iron,Frost,Storm,Valkyrie}
public static class HordePacts {
    public const float BaseEnemyDamage=1.2f;
    public static float Reload(HordePact pact)=>pact==HordePact.Odin?.6f:pact==HordePact.Frost?1.2f:1;
    public static float Speed(HordePact pact)=>pact==HordePact.Odin?1.2f:1;
    public static float Health(HordePact pact)=>pact==HordePact.Thor?1.35f:1;
    public static float EnemyDamage(HordePact pact)=>BaseEnemyDamage*(pact==HordePact.Valkyrie?1.25f:1);
    public static float Impact(HordePact pact)=>pact==HordePact.Iron?2:1;
    public static float Turret(HordePact pact)=>pact==HordePact.Iron?.75f:pact==HordePact.Storm?.8f:1;
    public static bool Elemental(HordePact pact)=>pact==HordePact.Frost||pact==HordePact.Storm;
    public static string Name(HordePact pact)=>pact==HordePact.Odin?"Odins Eile":pact==HordePact.Thor?"Thors Zorn":pact==HordePact.Iron?"Eiserner Wagen":pact==HordePact.Frost?"Skadis Winter":pact==HordePact.Storm?"Sturmkette":pact==HordePact.Valkyrie?"Wacht der Walküre":"Ohne Pakt";
    public static string Benefit(HordePact pact)=>pact==HordePact.Odin?"40 % kürzere Nachladezeit":pact==HordePact.Thor?"Explosivmunition für die ganze Welle":pact==HordePact.Iron?"Doppelter Überfahrschaden":pact==HordePact.Frost?"Eismunition: friert Gegner kurz ein":pact==HordePact.Storm?"Blitzmunition: springt auf bis zu 3 Gegner":pact==HordePact.Valkyrie?"Autoturret für die ganze Welle":"Keine Veränderungen";
    public static string Cost(HordePact pact)=>pact==HordePact.Odin?"Gegner laufen 20 % schneller":pact==HordePact.Thor?"Gegner haben 35 % mehr Leben":pact==HordePact.Iron?"25 % weniger Turret-Schaden":pact==HordePact.Frost?"20 % längere Nachladezeit; Bosse werden nur verlangsamt":pact==HordePact.Storm?"20 % weniger direkter Kugelschaden":pact==HordePact.Valkyrie?"Gegner verursachen 25 % mehr Schaden":"Kein zusätzlicher Nachteil";
    public static HordePact[] Draw(Random random,IReadOnlyList<HordePact> previous,int wave) {
        var pool=wave>=5?new[]{HordePact.Odin,HordePact.Thor,HordePact.Iron,HordePact.Frost,HordePact.Storm}
            :new[]{HordePact.Odin,HordePact.Thor,HordePact.Iron,HordePact.Frost,HordePact.Storm,HordePact.Valkyrie};
        for(int i=pool.Length-1;i>0;i--){int j=random.Next(i+1);var swap=pool[i];pool[i]=pool[j];pool[j]=swap;}
        bool WasOffered(HordePact pact){if(previous!=null)foreach(var old in previous)if(old==pact)return true;return false;}
        int repeated=0;for(int i=0;i<3;i++)if(WasOffered(pool[i]))repeated++;
        for(int i=0;i<3&&repeated>1;i++)if(WasOffered(pool[i]))for(int j=3;j<pool.Length;j++)if(!WasOffered(pool[j])){var swap=pool[i];pool[i]=pool[j];pool[j]=swap;repeated--;break;}
        return new[]{pool[0],pool[1],pool[2]};
    }
}
}
