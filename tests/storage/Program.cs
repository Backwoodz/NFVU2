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
        PersistentCars(car);
        Console.WriteLine(checks+" storage checks passed (native API doubles; game save/UI tests still required).");
    }
    static void PersistentCars(GameObject car) {
        ZNetScene.instance=new ZNetScene();Player.m_localPlayer=new Player();
        car.transform.SetPositionAndRotation(new Vector3(40,200,80),Quaternion.Euler(0,125,0));
        var empty=new VehicleStorage(car,false,2);var record=empty.Data;
        VehiclePersistence.Mark(record,false);empty.SaveVehicle(true,true);
        Check(VehiclePersistence.IsVehicle(record)&&!VehiclePersistence.Hummer(record),"350Z world marker");
        Check(VehiclePersistence.Location(record).y==200&&VehiclePersistence.Orientation(record).y==125,"exact car altitude and orientation saved independently of grounded cargo");
        Check(VehiclePersistence.Flying(record)&&VehiclePersistence.WasDriven(record)&&VehiclePersistence.Trunk(record)==2,"flight, driven and trunk state retained");
        Check(!VehiclePersistence.Transformed(record),"existing car records default to car form");
        empty.PositionForSave=()=>new Vector3(40,.32f,80);
        empty.SaveVehicle(false,true,true);
        Check(VehiclePersistence.Location(record).y==.32f&&car.transform.position.y==200,"robot save uses car recovery height without moving the live robot root");
        Check(VehiclePersistence.Transformed(record)&&!VehiclePersistence.Flying(record),"transformer form is saved alongside recovery pose and cargo");
        empty.PositionForSave=null;empty.SaveVehicle(true,true);
        Check(VehiclePersistence.Location(record).y==200&&VehiclePersistence.Flying(record),"return to car clears temporary save pose override");
        Check(!VehiclePersistence.Transformed(record),"returning to car clears saved transformer form");
        empty.Open("saved car");empty.Release();empty.Release();
        Check(InventoryGui.instance.current==null,"persistent cleanup closes cargo UI before disposing vehicle");
        Check(!record.deleted&&VehiclePersistence.IsVehicle(record),"empty car survives teardown and repeated disposal");
        var restoredCar=new GameObject();restoredCar.transform.SetPositionAndRotation(VehiclePersistence.Location(record),VehiclePersistence.Orientation(record));
        var restored=new VehicleStorage(restoredCar,record);
        Check(restored.Capacity==12&&restored.Data==record&&record.owner==ZDOMan.GetSessionID(),"restore reuses record and claims local ownership");
        Check(ZNetScene.instance.created==1&&ZNetScene.instance.objects.Count==1,"restore never duplicates native chest");
        restored.Release(true);
        Check(record.deleted&&!VehiclePersistence.IsVehicle(record),"explicit deletion removes empty car record");

        var full=new VehicleStorage(car,true,0);var cargo=full.Data;
        VehiclePersistence.Mark(cargo,true);full.SaveVehicle(false,false);
        full.SaveVehicle(true,false,true);
        Check(!VehiclePersistence.Transformed(cargo)&&VehiclePersistence.Flying(cargo),"Hummer cannot restore as transformer even with an invalid form request");
        var root=ZNetScene.instance.objects.Values.Single();var container=root.GetComponent<Container>();
        container.inventory.items.Add(new Item{stack=22,custom="saved cargo"});container.inventory.m_onChanged();
        full.Release();
        Check(!cargo.deleted&&cargo.items.Count==1&&VehiclePersistence.Hummer(cargo),"H2 and cargo survive world teardown");
        UnityEngine.Object.Destroy(root);ZNetScene.instance.objects.Clear();
        var loaded=ZNetScene.instance.GetPrefab("piece_chest_blackmetal").Clone(cargo.GetPosition(),cargo.GetRotation());
        ZNetScene.instance.objects.Clear();loaded.GetComponent<ZNetView>().data=cargo;ZNetScene.instance.objects[cargo.m_uid]=loaded;
        loaded.GetComponent<Container>().inventory.m_onChanged=()=>cargo.items=loaded.GetComponent<Container>().inventory.items.ToList();
        VehicleStorage.Hide(loaded); // Container.Awake hook hides before staged restoration.
        var attached=new VehicleStorage(restoredCar,cargo);
        Check(attached.Capacity==32&&attached.Count==1&&loaded.GetComponent<Container>().inventory.items[0].custom=="saved cargo","reloaded H2 restores original cargo metadata");
        Check(!loaded.GetComponent<Renderer>().enabled&&!loaded.GetComponent<Collider>().enabled,"reloaded marker remains hidden");
        attached.Release(true);attached.Release(true);
        Check(!cargo.deleted&&!VehiclePersistence.IsVehicle(cargo)&&cargo.items.Count==1,"explicit full car removal converts same inventory to ordinary chest");
        Check(loaded.GetComponent<Renderer>().enabled&&loaded.GetComponent<Collider>().enabled&&loaded.GetComponent<WearNTear>().m_noSupportWear,"pre-hidden restored marker returns to native visibility and wear");

        var parked=new VehicleStorage(car,false,1);VehiclePersistence.Mark(parked.Data,false);parked.SaveVehicle(false,true);
        var parkedData=parked.Data;var parkedRoot=ZNetScene.instance.FindInstance(parkedData.m_uid);
        UnityEngine.Object.Destroy(parkedRoot);ZNetScene.instance.objects.Remove(parkedData.m_uid);
        car.transform.position=new Vector3(90,140,100);parked.Tick();parked.SaveVehicle(true,true);parked.Release();
        Check(VehiclePersistence.IsVehicle(parkedData)&&VehiclePersistence.Location(parkedData).x==90&&!parkedData.deleted,"unloaded parked vehicle preserves updated car pose and empty record");
        Check(parkedData.GetPosition().x==90&&parkedData.GetPosition().z==98.7f,"unloaded native record follows vehicle to new sector");
        var otherWorld=new VehicleStorage(car,false,0);VehiclePersistence.Mark(otherWorld.Data,false);otherWorld.SaveVehicle(false,false);
        var original=VehiclePersistence.Location(otherWorld.Data);
        ZDOMan.instance=new ZDOMan();ZNetScene.instance=new ZNetScene();car.transform.position=new Vector3(999,999,999);
        otherWorld.Tick();otherWorld.SaveVehicle(true,true);otherWorld.Release();
        Check(VehiclePersistence.Location(otherWorld.Data).x==original.x&&!VehiclePersistence.Flying(otherWorld.Data),"old world storage cannot write using new world context");
        var vanilla=new ZDO();Check(!VehiclePersistence.IsVehicle(vanilla),"ordinary chests never become cars");
        vanilla.Set(VehiclePersistence.Model,99);Check(!VehiclePersistence.IsVehicle(vanilla),"unknown schema or model never restored as a car");
    }
}
