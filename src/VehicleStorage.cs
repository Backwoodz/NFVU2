using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
// A real vanilla chest owns the items and saves them to the world, independently
// of the rendered car. Never parent it to the disposable vehicle object.
internal sealed class VehicleStorage {
    static readonly System.Reflection.MethodInfo Load=AccessTools.Method(typeof(Container),"Load");
    static readonly System.Reflection.FieldInfo InventoryName=AccessTools.Field(typeof(Inventory),"m_name");
    readonly bool hummer;
    readonly GameObject vehicle,prefab;
    readonly ZDO data;
    readonly ZDOMan world=ZDOMan.instance;
    readonly ZNetScene scene=ZNetScene.instance;
    Container container;
    ZNetView view;
    VehicleCargoVisibility visibility;
    bool released;
    public int Layout {get;private set;}
    public int Capacity=>VehicleStorageRules.Capacity(hummer,Layout);
    public int Count=>container!=null?container.GetInventory().NrOfItems():-1;
    public bool InUse=>container!=null&&container.IsInUse();
    public ZDO Data=>data;
    public VehicleStorage(GameObject vehicle,ZDO saved) {
        this.vehicle=vehicle;data=saved;hummer=VehiclePersistence.Hummer(saved);Layout=VehiclePersistence.Trunk(saved);
        prefab=ZNetScene.instance.GetPrefab(hummer?"piece_chest_blackmetal":"piece_chest");
        data.SetOwner(ZDOMan.GetSessionID());
        Resolve();
    }
    public VehicleStorage(GameObject vehicle,bool hummer,int layout) {
        this.vehicle=vehicle;this.hummer=hummer;Layout=VehicleStorageRules.Layout(layout);
        prefab=ZNetScene.instance.GetPrefab(hummer?"piece_chest_blackmetal":"piece_chest");
        if(prefab==null||prefab.GetComponent<Container>()==null)throw new InvalidOperationException("Native cargo chest unavailable");
        var original=prefab.GetComponent<Container>();
        if(original.m_width!=VehicleStorageRules.Width(hummer)||original.m_height!=4)throw new InvalidOperationException("Unexpected native chest dimensions");
        var root=UnityEngine.Object.Instantiate(prefab,vehicle.transform.position,Quaternion.identity);
        view=root.GetComponent<ZNetView>();data=view.GetZDO();
        if(data==null){UnityEngine.Object.Destroy(root);throw new InvalidOperationException("Cargo needs an active world");}
        data.Persistent=true;
        Attach(root);
        Tick();
    }
    void Attach(GameObject root) {
        view=root.GetComponent<ZNetView>();container=root.GetComponent<Container>();
        // Container normally loads on its next CheckForChanges tick. Load first,
        // before changing dimensions, especially after a parked zone reload.
        Load.Invoke(container,null);
        visibility=Hide(root);
        // If another native interaction filled a reloaded chest before this
        // frame, preserve that cargo and fall back to the stock trunk capacity.
        if(!Resize(Layout)&&!hummer){Layout=0;Resize(Layout);}
    }
    public static VehicleCargoVisibility Hide(GameObject root) {
        var visibility=root.GetComponent<VehicleCargoVisibility>()??root.AddComponent<VehicleCargoVisibility>();
        visibility.Hide();return visibility;
    }
    bool Resolve() {
        if(released||scene==null||world==null||scene!=ZNetScene.instance||world!=ZDOMan.instance)return false;
        if(container!=null&&view!=null&&view.IsValid())return true;
        container=null;view=null;
        var root=ZNetScene.instance.FindInstance(data.m_uid);
        if(root==null)return false;
        Attach(root);return true;
    }
    public bool CanResize(int layout) {
        if(layout==Layout)return true;
        return Resolve()&&VehicleStorageRules.CanResize(hummer,layout,Count,InUse);
    }
    public bool Resize(int layout) {
        layout=VehicleStorageRules.Layout(layout);
        if(container==null||!VehicleStorageRules.CanResize(hummer,layout,Count,InUse))return false;
        var inventory=container.GetInventory();
        int rows=VehicleStorageRules.Rows(hummer,layout);
        if(inventory.GetHeight()!=rows) {
            // Repack existing instances, retaining stacks, durability and custom data.
            var items=inventory.GetAllItems();
            for(int i=0;i<items.Count;i++)items[i].m_gridPos=new Vector2i(i%inventory.GetWidth(),i/inventory.GetWidth());
            container.m_height=rows;inventory.SetHeight(rows);inventory.m_onChanged?.Invoke();
        }
        Layout=layout;return true;
    }
    public bool Open(string name) {
        if(!Resolve()||!view.IsOwner()||InventoryGui.instance==null)return false;
        InventoryName.SetValue(container.GetInventory(),name);
        container.SetInUse(true);InventoryGui.instance.Show(container);return true;
    }
    public void Tick() {
        if(vehicle==null||released||scene==null||scene!=ZNetScene.instance||world==null||world!=ZDOMan.instance)return;
        bool loaded=Resolve();
        var position=vehicle.transform.TransformPoint(new Vector3(0,.5f,hummer?-1.8f:-1.3f));
        var rotation=Quaternion.Euler(0,vehicle.transform.eulerAngles.y,0);
        if(loaded)container.transform.SetPositionAndRotation(position,rotation);
        // Save a grounded recovery position even when the vehicle is flying.
        var saved=position;
        if(ZoneSystem.instance!=null&&ZoneSystem.instance.GetGroundHeight(position,out var ground))saved.y=ground+.15f;
        // The ZDO must follow the car even during native zone unload; otherwise
        // its cargo/restore trigger would be stranded in the previous sector.
        if(data.IsOwner()){data.SetPosition(saved);data.SetRotation(rotation);}
    }
    public void SaveVehicle(bool flying,bool driven) {
        if(released||vehicle==null||world!=ZDOMan.instance||!VehiclePersistence.IsVehicle(data))return;
        VehiclePersistence.Save(data,vehicle.transform,Layout,flying,driven);
    }
    public void Release(bool removeVehicle=false) {
        if(released)return;
        Tick();
        if(scene!=null&&scene==ZNetScene.instance&&world==ZDOMan.instance&&InUse&&InventoryGui.instance!=null&&Player.m_localPlayer!=null)InventoryGui.instance.Hide();
        // World teardown disposes only local resources. Even an empty car keeps
        // its world record; only explicit removal converts cargo back to a chest.
        if(VehiclePersistence.IsVehicle(data)&&!removeVehicle){released=true;return;}
        if(removeVehicle&&world==ZDOMan.instance)VehiclePersistence.Forget(data);
        Resolve();
        if(scene!=null&&scene==ZNetScene.instance&&world==ZDOMan.instance&&container!=null&&view!=null&&view.IsValid()) {
            if(Count==0&&view.IsOwner())view.Destroy();
            else {
                // Restore the original vanilla chest in place; its existing ZDO
                // and inventory stay intact. No cloning or cross-save transfer.
                container.m_height=4;container.GetInventory().SetHeight(4);
                InventoryName.SetValue(container.GetInventory(),prefab.GetComponent<Container>().m_name);
                container.transform.SetPositionAndRotation(data.GetPosition(),data.GetRotation());
                visibility?.Show();
            }
        }
        // An unloaded chest already has a persistent vanilla ZDO; leave it alone.
        released=true;
    }
}
// Capture native states once, including when a saved cargo chest loads before
// the staged vehicle. Reattaching must not capture its already-hidden states.
internal sealed class VehicleCargoVisibility : MonoBehaviour {
    Renderer[] renderers;Collider[] colliders;bool[] rendering,collision;
    WearNTear wear;bool roofWear,supportWear;
    public void Hide() {
        if(renderers!=null)return;
        renderers=gameObject.GetComponentsInChildren<Renderer>(true);rendering=renderers.Select(r=>r.enabled).ToArray();
        colliders=gameObject.GetComponentsInChildren<Collider>(true);collision=colliders.Select(c=>c.enabled).ToArray();
        foreach(var renderer in renderers)renderer.enabled=false;
        foreach(var collider in colliders)collider.enabled=false;
        wear=GetComponent<WearNTear>();
        if(wear!=null){roofWear=wear.m_noRoofWear;supportWear=wear.m_noSupportWear;wear.m_noRoofWear=wear.m_noSupportWear=false;}
    }
    public void Show() {
        if(renderers==null)return;
        for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=rendering[i];
        for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=collision[i];
        if(wear!=null){wear.m_noRoofWear=roofWear;wear.m_noSupportWear=supportWear;}
        renderers=null;colliders=null;
    }
}
}
