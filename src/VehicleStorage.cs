using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
// A real vanilla chest owns the items and saves them to the world, independently
// of the temporary car. Never parent it to the disposable vehicle object.
internal sealed class VehicleStorage {
    static readonly System.Reflection.MethodInfo Load=AccessTools.Method(typeof(Container),"Load");
    static readonly System.Reflection.FieldInfo InventoryName=AccessTools.Field(typeof(Inventory),"m_name");
    readonly bool hummer;
    readonly GameObject vehicle,prefab;
    readonly ZDO data;
    Container container;
    ZNetView view;
    Renderer[] renderers;
    Collider[] colliders;
    bool[] rendering,collision;
    WearNTear wear;
    bool roofWear,supportWear;
    bool released;
    public int Layout {get;private set;}
    public int Capacity=>VehicleStorageRules.Capacity(hummer,Layout);
    public int Count=>container!=null?container.GetInventory().NrOfItems():-1;
    public bool InUse=>container!=null&&container.IsInUse();
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
        renderers=root.GetComponentsInChildren<Renderer>(true);rendering=renderers.Select(r=>r.enabled).ToArray();
        colliders=root.GetComponentsInChildren<Collider>(true);collision=colliders.Select(c=>c.enabled).ToArray();
        foreach(var renderer in renderers)renderer.enabled=false;
        foreach(var collider in colliders)collider.enabled=false;
        wear=root.GetComponent<WearNTear>();
        if(wear!=null){roofWear=wear.m_noRoofWear;supportWear=wear.m_noSupportWear;wear.m_noRoofWear=wear.m_noSupportWear=false;}
        // If another native interaction filled a reloaded chest before this
        // frame, preserve that cargo and fall back to the stock trunk capacity.
        if(!Resize(Layout)&&!hummer){Layout=0;Resize(Layout);}
    }
    bool Resolve() {
        if(released||ZNetScene.instance==null||ZDOMan.instance==null)return false;
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
        if(vehicle==null||!Resolve())return;
        var position=vehicle.transform.TransformPoint(new Vector3(0,.5f,hummer?-1.8f:-1.3f));
        container.transform.SetPositionAndRotation(position,Quaternion.Euler(0,vehicle.transform.eulerAngles.y,0));
        // Save a grounded recovery position even when the vehicle is flying.
        var saved=position;
        if(ZoneSystem.instance!=null&&ZoneSystem.instance.GetGroundHeight(position,out var ground))saved.y=ground+.15f;
        if(view.IsOwner()){data.SetPosition(saved);data.SetRotation(container.transform.rotation);}
    }
    public void Release() {
        if(released)return;
        Tick();
        if(ZNetScene.instance!=null&&ZDOMan.instance!=null&&container!=null&&view!=null&&view.IsValid()) {
            if(InUse&&InventoryGui.instance!=null&&Player.m_localPlayer!=null)InventoryGui.instance.Hide();
            if(Count==0&&view.IsOwner())view.Destroy();
            else {
                // Restore the original vanilla chest in place; its existing ZDO
                // and inventory stay intact. No cloning or cross-save transfer.
                container.m_height=4;container.GetInventory().SetHeight(4);
                InventoryName.SetValue(container.GetInventory(),prefab.GetComponent<Container>().m_name);
                container.transform.SetPositionAndRotation(data.GetPosition(),data.GetRotation());
                for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=rendering[i];
                for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=collision[i];
                if(wear!=null){wear.m_noRoofWear=roofWear;wear.m_noSupportWear=supportWear;}
            }
        }
        // An unloaded chest already has a persistent vanilla ZDO; leave it alone.
        released=true;
    }
}
}
