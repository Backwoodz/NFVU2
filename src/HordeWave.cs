using System;
using System.Collections.Generic;
namespace Rachel350Z {
// Pure encounter state; the Unity adapter supplies placement and living counts.
public sealed class HordeWave {
    public const int WaveCount=5,MaxAlive=24,AttemptsPerEnemy=12;
    public const float Interval=.12f;
    readonly Random random=new Random();
    public HordePact[] Offers {get;private set;}=Array.Empty<HordePact>();
    public static readonly string[] Prefabs={"Greyling","Greydwarf","Greydwarf_Elite","Greydwarf_Shaman","Dragon","Skeleton","Draugr","Wolf","Goblin","GoblinBrute","Hatchling"};
    static readonly int[][] Counts={new[]{12,3,0,0,0,0,0,0,0,0,0},new[]{0,6,2,0,0,12,0,0,0,0,0},new[]{0,0,0,2,0,0,10,6,0,0,0},new[]{0,0,0,0,0,0,0,0,12,2,0},new[]{0,0,0,0,1,0,0,0,4,0,2}};
    static readonly string[] Announcements={"Etwas regt sich im Unterholz …","Die Toten erheben sich!","Fäulnis und Fänge jagen dich!","Die Krieger der Ebenen greifen an!","Moders Schwingen verdunkeln den Himmel!"};
    string[] roster=Array.Empty<string>();
    float delay;
    int slotAttempts;
    public int Number {get;private set;}
    public int Spawned {get;private set;}
    public int Resolved {get;private set;}
    public int Attempts {get;private set;}
    public int Size=>roster.Length;
    public bool Active {get;private set;}
    public bool Completed {get;private set;}
    public bool NoGround {get;private set;}
    public bool Pending=>Active&&Resolved<Size;
    public bool Choosing {get;private set;}
    public HordePact Pact {get;private set;}
    public int KillGoal=>(int)Math.Ceiling(Spawned*.85);
    public static bool Required(string prefab)=>prefab=="Greydwarf_Elite"||prefab=="Greydwarf_Shaman"||prefab=="GoblinBrute"||prefab=="Dragon";
    public string Announcement=>Number>0?Announcements[Number-1]:"";
    public int Remaining(int alive)=>Math.Max(0,alive)+(Pending?Size-Resolved:0);
    public bool Begin(int alive) {
        if(Active||alive>0)return false;
        Active=true;Completed=NoGround=false;Number=0;Next();return true;
    }
    void Next() {
        Number++;Spawned=Resolved=Attempts=slotAttempts=0;delay=0;Choosing=true;Pact=HordePact.None;Offers=HordePacts.Draw(random,Offers,Number);
        var counts=Counts[Number-1];var list=new List<string>();
        // Spread support units and trolls through the small-enemy formation.
        int total=0;foreach(int count in counts)total+=count;
        var added=new int[counts.Length];
        for(int slot=0;slot<total;slot++) {
            int best=-1;float earliest=float.MaxValue;
            for(int type=0;type<counts.Length;type++)if(added[type]<counts[type]) {
                float position=(added[type]+.5f)/counts[type];
                if(position<earliest){earliest=position;best=type;}
            }
            list.Add(Prefabs[best]);added[best]++;
        }
        roster=list.ToArray();
    }
    public bool Choose(HordePact pact) {
        if(!Active||!Choosing||pact!=HordePact.None&&Array.IndexOf(Offers,pact)<0)return false;
        Pact=pact;Choosing=false;return true;
    }
    public void Cancel(){Active=Choosing=false;Pact=HordePact.None;Completed=NoGround=false;}
    public void Tick(float dt,int alive,Func<string,bool> spawn,int kills=-1,int requiredAlive=0) {
        if(!Active||Choosing||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
        if(Pending) {
            if(alive>=MaxAlive)return;
            delay-=dt;if(delay>0)return;
            // Three probes and at most one native Instantiate per frame, no backlog.
            for(int i=0;i<3&&Pending;i++) {
                Attempts++;slotAttempts++;
                bool created=spawn(roster[Resolved]);
                if(created)Spawned++;
                if(!created&&slotAttempts>=AttemptsPerEnemy&&Required(roster[Resolved])){Active=false;NoGround=true;return;}
                if(created||slotAttempts>=AttemptsPerEnemy){Resolved++;slotAttempts=0;}
                if(created){delay=Interval;break;}
            }
            if(!Pending&&Spawned==0){Active=false;NoGround=true;}
            return;
        }
        if(kills<0)kills=Spawned-alive;
        if(kills<KillGoal||requiredAlive>0)return;
        if(Number==WaveCount){Active=false;Completed=true;return;}
        Next();
    }
}
}
