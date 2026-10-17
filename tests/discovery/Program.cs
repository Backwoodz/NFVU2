using System;
using Rachel350Z;
using UnityEngine;
static class DiscoveryTests {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static void Main() {
        var driver=new Player();Player.m_localPlayer=driver;
        VehicleDiscovery.Step(driver);
        Check(driver.discoveryCalls==1&&Math.Abs(driver.discoveryTime-.02f)<.00001f,
            "disabled seated local player receives the native discovery method and frame time");
        Check(!driver.enabled&&driver.fullUpdates==0,"discovery does not reenable Player or forward its movement and input updates");
        VehicleDiscovery.Step(null);VehicleDiscovery.Step(new Player());
        Check(driver.discoveryCalls==1,"missing and foreign players cannot receive local discovery");
        driver.enabled=true;VehicleDiscovery.Step(driver);driver.enabled=false;
        Check(driver.discoveryCalls==1,"enabled player keeps its original native discovery without double updates");
        driver.dead=true;VehicleDiscovery.Step(driver);driver.dead=false;
        driver.teleporting=true;VehicleDiscovery.Step(driver);driver.teleporting=false;
        driver.owner=false;VehicleDiscovery.Step(driver);driver.owner=true;
        driver.gameObject.activeInHierarchy=false;VehicleDiscovery.Step(driver);driver.gameObject.activeInHierarchy=true;
        Check(driver.discoveryCalls==1,"dead, teleporting, nonowned and inactive players are excluded");
        foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity}){Time.deltaTime=dt;VehicleDiscovery.Step(driver);}
        Check(driver.discoveryCalls==1,"pause and invalid frame times cannot advance discovery");
        Time.deltaTime=.02f;
        WorldGenerator.instance=null;VehicleDiscovery.Step(driver);WorldGenerator.instance=new WorldGenerator();
        Minimap.instance=null;VehicleDiscovery.Step(driver);Minimap.instance=new Minimap();
        MessageHud.instance=null;VehicleDiscovery.Step(driver);MessageHud.instance=new MessageHud();
        Check(driver.discoveryCalls==1,"world loading and HUD teardown cannot call unready native discovery");
        Time.deltaTime=5;VehicleDiscovery.Step(driver);
        Check(driver.discoveryCalls==2&&Math.Abs(driver.discoveryTime-1.02f)<.00001f,"long resume frames receive at most one second of discovery budget");
        var next=new Player();Player.m_localPlayer=next;Time.deltaTime=.02f;
        VehicleDiscovery.Step(driver);VehicleDiscovery.Step(next);
        Check(driver.discoveryCalls==2&&next.discoveryCalls==1,"world or player changes immediately follow the new local owner without carrying helper timers");
        Console.WriteLine(checks+" vehicle discovery checks passed. Native location radius, biome banners and Unity timing require the game test.");
    }
}
