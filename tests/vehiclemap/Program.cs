using System;
using System.Linq;
using Rachel350Z;
using UnityEngine;
class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
    static void Main() {
        var registry=new VehicleMapPins();var map=new Minimap();var otherMap=new Minimap();
        var icon=new Sprite();var world=new object();var a=new ZDOID(1);var b=new ZDOID(2);
        var manual=map.AddPin(new Vector3(10,0,0),Minimap.PinType.Icon0,"mine",true,false);
        var native=new Sprite();map.Icons.Add(Minimap.PinType.Icon0,native);
        var entries=new[]{new VehicleMapEntry(a,new Vector3(1,0,2),false),new VehicleMapEntry(b,new Vector3(9000,0,2),true)};
        registry.Tick(map,world,entries,icon);
        Check(map.Pins.Count==3&&map.SavedCount==1,"two transient car pins preserve manual saved pin");
        Check(map.Pins.Skip(1).All(p=>p.m_type==Minimap.PinType.None&&p.m_icon==icon&&!p.m_checked),"per-pin car sprite with native safe enum");
        Check(map.Pins[1].m_name=="Nissan 350Z"&&map.Pins[2].m_name=="Hummer H2","vehicle models have native big-map labels");
        Check(map.Icons.Count==1&&map.Icons[Minimap.PinType.Icon0]==native,"native icon table untouched");
        var carPin=map.Pins[1];map.Render();registry.Tick(map,world,entries,icon);
        Check(map.Pins.Count==3&&ReferenceEquals(carPin,map.Pins[1])&&!map.Dirty,"stable frame keeps native pins and avoids forced redraw");
        entries[0]=new VehicleMapEntry(a,new Vector3(3,6,4),false);registry.Tick(map,world,entries,icon);
        Check(carPin.m_pos==entries[0].Position&&map.Dirty,"moving car updates native position and redraw");
        registry.Tick(map,world,new[]{entries[0]},icon);
        Check(map.Pins.Count==2&&map.Pins.Contains(manual),"vehicle removal deletes only its transient marker");
        registry.Tick(map,new object(),Array.Empty<VehicleMapEntry>(),icon);
        Check(map.Pins.Count==1&&map.Pins[0]==manual,"world switch removes stale fleet pins");
        registry.Tick(map,world,entries,icon);registry.Tick(otherMap,world,entries,icon);
        Check(map.Pins.Count==1&&otherMap.Pins.Count==2,"map recreation cleans old instance and creates new pins");
        registry.Clear();registry.Clear();Check(otherMap.Pins.Count==0,"cleanup is idempotent");
        registry.Tick(map,world,new[]{new VehicleMapEntry(a,new Vector3(float.NaN,0,0),false)},icon);
        Check(map.Pins.Count==1,"invalid saved location cannot create corrupt marker");
        registry.Tick(map,world,new[]{entries[0],entries[0]},icon);
        Check(map.Pins.Count==2,"duplicate vehicle ID creates one marker");registry.Clear();
        registry.Tick(map,null,entries,icon);Check(map.Pins.Count==1,"no world never creates fleet pins");
        Minimap.instance=new Minimap();ZDOMan.instance=new ZDOMan();Player.m_localPlayer=new Player();
        var plugin=new Plugin();plugin.Tick();Check(VehicleMapIcon.Loads==0,"empty garage does not load icon asset");
        var saved=new ZDO(4,40);var distant=new ZDO(5,9000,true);plugin.Saved.Add(saved);plugin.Saved.Add(distant);
        plugin.Live(saved,45);plugin.Tick();
        Check(Minimap.instance.Pins.Count==2&&Minimap.instance.Pins[0].m_pos.x==45,"live vehicle overrides its saved location without duplicate pin");
        Check(Minimap.instance.Pins[1].m_pos.x==9000,"distant saved car remains marked without scene instance");
        plugin.MoveLive(60);plugin.Tick();Check(Minimap.instance.Pins[0].m_pos.x==60&&Minimap.instance.Pins[0].m_pos.y==5,"live root remains tracked during movement and robot form");
        distant.Removed=true;plugin.Tick();Check(Minimap.instance.Pins.Count==1,"forgotten persistent car loses map marker");
        plugin.MultiplayerMode=true;plugin.Tick();Check(Minimap.instance.Pins.Count==0,"multiplayer suppresses fleet map");
        plugin.MultiplayerMode=false;plugin.Tick();Check(Minimap.instance.Pins.Count==1&&VehicleMapIcon.Loads==1,"single-player resumption reuses owned icon");
        Player.m_localPlayer=null;plugin.Tick();Check(Minimap.instance.Pins.Count==0,"logout clears map markers");
        Player.m_localPlayer=new Player();plugin.Tick();plugin.Dispose();Check(Minimap.instance.Pins.Count==0&&VehicleMapIcon.Disposals==1,"plugin disposal removes markers and owned icon");
        Console.WriteLine(checks+" vehicle map checks passed.");
    }
}
