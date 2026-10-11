using System;
using System.Linq;
using Rachel350Z;
using UnityEngine;
class Program {
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;}
    static void Main() {
        for(int layout=0;layout<4;layout++) {
            Check(VehicleStorageRules.Capacity(false,layout)==24-layout*6,"350Z capacity");
            Check(VehicleStorageRules.Capacity(true,layout)==32,"H2 fixed capacity");
        }
        Check(VehicleStorageRules.Capacity(false,-50)==24&&VehicleStorageRules.Capacity(false,99)==6,"configuration bounds");
        var car=new GameObject();car.transform.position=new Vector3(50,100,60);
        var storage=new VehicleStorage(car,false,0);
        var root=ZNetScene.instance.objects.Values.Single();var container=root.GetComponent<Container>();var data=root.GetComponent<ZNetView>().data;
        Check(data.Persistent&&data.GetPosition().y==10.15f,"persistent grounded recovery position while flying");
        Check(!root.GetComponent<Renderer>().enabled&&!root.GetComponent<Collider>().enabled,"hidden cargo cannot collide with car");
        Check(!root.GetComponent<WearNTear>().m_noSupportWear&&!root.GetComponent<WearNTear>().m_noRoofWear,"carried chest has no support/rain damage");
        for(int i=0;i<16;i++)container.inventory.items.Add(new Item{m_gridPos=new Vector2i(i%6,3),stack=i+1,custom="custom-"+i});
        container.inventory.m_onChanged();var items=container.inventory.items.ToArray();
        Check(!storage.CanResize(2)&&!storage.Resize(2)&&storage.Layout==0,"overfull downgrade refused");
        Check(items.Select((item,i)=>item.m_gridPos.y==3).All(v=>v),"failed downgrade leaves positions intact");
        Check(storage.Resize(1)&&container.inventory.GetHeight()==3,"legal downgrade succeeds");
        Check(items.Select((item,i)=>ReferenceEquals(item,container.inventory.items[i])&&item.m_gridPos.x==i%6&&item.m_gridPos.y==i/6&&item.stack==i+1&&item.custom=="custom-"+i).All(v=>v),"repack retains item identity, stacks and custom data");
        Check(data.items.Count==16,"repack saved to existing world chest");
        Check(storage.Open("350Z cargo")&&InventoryGui.instance.current==container&&container.inventory.GetName()=="350Z cargo","native inventory opens with cargo title");
        Check(!storage.CanResize(0)&&!storage.Resize(0),"resize blocked during inventory drag session");
        car.transform.position=new Vector3(250,200,360);storage.Tick();
        Check(data.GetPosition().x==250&&data.GetPosition().z==358.7f,"moving vehicle updates persistent sector position");
        storage.Release();storage.Release();
        Check(!data.deleted&&data.items.Count==16&&ZNetScene.instance.created==1,"nonempty release keeps same chest, no copied items");
        Check(container.inventory.GetHeight()==4&&InventoryGui.instance.current==null,"release closes inventory and restores vanilla dimensions");
        Check(root.GetComponent<Renderer>().enabled&&root.GetComponent<Collider>().enabled&&root.GetComponent<WearNTear>().m_noSupportWear,"release restores physical chest");
        Check(root.transform.position.y==data.GetPosition().y,"recovered chest is grounded");
        ZNetScene.instance=new ZNetScene();
        var empty=new VehicleStorage(car,true,3);var emptyRoot=ZNetScene.instance.objects.Values.Single();
        Check(empty.Capacity==32,"H2 inventory capacity");empty.Release();empty.Release();
        Check(emptyRoot.GetComponent<ZNetView>().data.deleted&&ZNetScene.instance.objects.Count==0,"empty cargo removed once");
        var parked=new VehicleStorage(car,false,2);var parkedRoot=ZNetScene.instance.objects.Values.Single();var saved=parkedRoot.GetComponent<ZNetView>().data;
        parkedRoot.GetComponent<Container>().inventory.items.Add(new Item{stack=42,custom="parked"});parkedRoot.GetComponent<Container>().inventory.m_onChanged();
        UnityEngine.Object.Destroy(parkedRoot);ZNetScene.instance.objects.Clear();var created=ZNetScene.instance.created;
        parked.Tick();Check(!parked.CanResize(3)&&!parked.Open("closed")&&ZNetScene.instance.created==created,"unloaded cargo is never replaced or resized");
        var loaded=ZNetScene.instance.GetPrefab("piece_chest").Clone(saved.GetPosition(),saved.GetRotation());
        ZNetScene.instance.objects.Clear();loaded.GetComponent<ZNetView>().data=saved;
        loaded.GetComponent<Container>().inventory.m_onChanged=()=>saved.items=loaded.GetComponent<Container>().inventory.items.ToList();
        ZNetScene.instance.objects[saved.m_uid]=loaded;parked.Tick();
        Check(parked.Count==1&&loaded.GetComponent<Container>().inventory.items[0].stack==42&&loaded.GetComponent<Container>().inventory.GetHeight()==2,"zone reload loads contents before restoring cargo size");
        UnityEngine.Object.Destroy(loaded);ZNetScene.instance.objects.Clear();parked.Release();
        Check(!saved.deleted&&saved.items.Count==1,"unloaded release retains persistent cargo");
        var quit=new VehicleStorage(car,false,0);var quitRoot=ZNetScene.instance.objects.Values.Single();var quitData=quitRoot.GetComponent<ZNetView>().data;
        quitRoot.GetComponent<Container>().inventory.items.Add(new Item{stack=7});quitRoot.GetComponent<Container>().inventory.m_onChanged();
        quit.Open("quit");Player.m_localPlayer=null;ZNetScene.instance=null;quit.Release();
        Check(!quitData.deleted&&quitData.items.Count==1,"world teardown retains saved cargo without native UI calls");
        Console.WriteLine(checks+" storage checks passed (native API doubles; game save/UI tests still required).");
    }
}
