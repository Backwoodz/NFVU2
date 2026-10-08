using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rachel350Z {
public sealed class VehicleShowroom : IDisposable {
    public readonly ShowroomState State=new ShowroomState();
    public ModelTrunkLayout[] Layouts=>model.trunkLayouts;
    readonly ModelData model;
    readonly GameObject car;
    readonly Func<ModelPart,Transform,Transform> create;
    readonly Transform[] moving=new Transform[4];
    readonly List<GameObject> equipment=new List<GameObject>();
    readonly List<Renderer> strips=new List<Renderer>();
    readonly List<Material> ownedMaterials=new List<Material>();
    readonly Dictionary<string,ModelAnimation> animations;
    readonly Dictionary<string,ModelAudioComponent[]> choices;
    readonly Dictionary<ModelPart,GameObject> variants=new Dictionary<ModelPart,GameObject>();
    readonly int[] selected={-1,-1,-1,-1,-1,-1};
    readonly Transform equipmentRoot;
    readonly Light trunkLight;
    int layout=-1,engine=-1;
    float phase;
    public VehicleShowroom(GameObject car,ModelData model,Func<ModelPart,Transform,Transform> create,Material template) {
        this.car=car;this.model=model;this.create=create;
        animations=model.animations.ToDictionary(a=>a.name);
        choices=model.audioComponents.GroupBy(c=>c.size).ToDictionary(g=>g.Key,g=>g.ToArray());
        try {
        string[] roles={"leftDoor","rightDoor","hood","trunk"};
        for(int i=0;i<4;i++) {
            var root=new GameObject("350Z_Motion_"+roles[i]);root.transform.SetParent(car.transform,false);moving[i]=root.transform;
        }
        foreach(var part in model.parts.Where(p=>p.group=="body")) {
            var node=car.transform.Find(part.name);if(node==null)continue;
            int index=Array.IndexOf(roles,part.movingPart);
            if(index>=0)node.SetParent(moving[index],false);
            if(part.optionGroup=="trunk"||part.optionGroup=="engine")variants.Add(part,node.gameObject);
        }
        var audioRoot=new GameObject("350Z_TrunkEquipment");audioRoot.transform.SetParent(car.transform,false);equipmentRoot=audioRoot.transform;
        var neon=new Material(template){name="350Z_TrunkNeon"};ownedMaterials.Add(neon);
        foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(neon.HasTexture(property))neon.SetTexture(property,Texture2D.whiteTexture);
        for(int i=0;i<3;i++) {
            var strip=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.Destroy(strip.GetComponent<Collider>());
            strip.name="350Z_TrunkNeonStrip";strip.transform.SetParent(equipmentRoot,false);
            strip.transform.localPosition=i<2?new Vector3(i==0?-.59f:.59f,.68f,-1.43f):new Vector3(0,.57f,-1.89f);
            strip.transform.localScale=i<2?new Vector3(.012f,.012f,.72f):new Vector3(1.03f,.012f,.012f);
            var renderer=strip.GetComponent<Renderer>();renderer.sharedMaterial=neon;strips.Add(renderer);
        }
        var lamp=new GameObject("350Z_TrunkGlow");lamp.transform.SetParent(equipmentRoot,false);lamp.transform.localPosition=new Vector3(0,.85f,-1.35f);
        trunkLight=lamp.AddComponent<Light>();trunkLight.type=LightType.Point;trunkLight.range=1.2f;trunkLight.shadows=LightShadows.None;trunkLight.enabled=false;
        }catch {Dispose();throw;}
    }
    public ModelAudioComponent[] Choices(int layoutIndex,int slot) {
        var sockets=Layouts[Mathf.Clamp(layoutIndex,0,Layouts.Length-1)].sockets;
        if(slot<0||slot>=sockets.Length)return new ModelAudioComponent[0];
        return choices[sockets[slot].size];
    }
    public void Configure(int trunkLayout,int engineStyle,int[] options) {
        trunkLayout=Mathf.Clamp(trunkLayout,0,Layouts.Length-1);engineStyle=Mathf.Clamp(engineStyle,0,2);
        bool newLayout=layout!=trunkLayout;
        if(newLayout||engine!=engineStyle) {
            layout=trunkLayout;engine=engineStyle;
            foreach(var entry in variants)entry.Value.SetActive(entry.Key.option==(entry.Key.optionGroup=="trunk"?layout:engine).ToString());
        }
        var sockets=Layouts[layout].sockets;
        for(int i=0;i<6;i++) {
            int choice=i<sockets.Length?Mathf.Clamp(options[i],0,Choices(layout,i).Length-1):-1;
            if(!newLayout&&selected[i]==choice)continue;
            selected[i]=choice;
            while(equipment.Count<=i)equipment.Add(null);
            if(equipment[i]!=null){equipment[i].SetActive(false);UnityEngine.Object.Destroy(equipment[i]);equipment[i]=null;}
            if(choice<0)continue;
            var component=Choices(layout,i)[choice];var socket=sockets[i];
            var root=new GameObject("350Z_AudioSlot"+i);root.transform.SetParent(equipmentRoot,false);
            root.transform.localPosition=V(socket.position);root.transform.localRotation=Quaternion.LookRotation(V(socket.forward),V(socket.up));equipment[i]=root;
            foreach(var part in model.parts.Where(p=>p.group=="audio"&&p.option==component.id))create(part,root.transform);
        }
    }
    public void Tick(float dt,bool movingCar,int doorStyle,bool powered,bool neonEnabled,bool pulsing,Color color) {
        State.Step(dt,movingCar,doorStyle);
        string style=new[]{"REG","SUICIDE","SCISSOR"}[State.DoorStyle];
        string[] tracks={"LEFT_DOOR_"+style,"RIGHT_DOOR_"+style,"HOOD_REG","TRUNK_REG"};
        for(int i=0;i<4;i++) {
            var track=animations[tracks[i]];
            float position=State.Progress[i]*(track.frames.Length-1);
            int a=Mathf.FloorToInt(position),b=Mathf.Min(a+1,track.frames.Length-1);float t=position-a;
            var rotation=Quaternion.Slerp(Q(track.frames[a].rotation),Q(track.frames[b].rotation),t);
            var offset=Vector3.Lerp(V(track.frames[a].offset),V(track.frames[b].offset),t);var pivot=V(track.pivot);
            moving[i].localRotation=rotation;moving[i].localPosition=pivot+offset-rotation*pivot;
        }
        phase=(phase+Mathf.Max(0,dt)*2.8f)%(Mathf.PI*2);
        bool active=neonEnabled&&(powered||State.Progress[3]>.01f);
        float gain=active?(pulsing?.3f+.7f*(.5f+.5f*Mathf.Sin(phase)):1):0;
        foreach(var strip in strips)strip.enabled=active;
        var material=ownedMaterials[0];
        foreach(var property in new[]{"_Color","_BaseColor"})if(material.HasColor(property))material.SetColor(property,color);
        VehicleMaterials.SetEmission(material,color*gain*2);
        trunkLight.enabled=active&&State.Progress[3]>.01f;trunkLight.color=color;trunkLight.intensity=gain*State.Progress[3]*1.4f;
    }
    static Vector3 V(ModelVertex v)=>new Vector3(v.x,v.y,v.z);
    static Quaternion Q(ModelRotation q)=>new Quaternion(q.x,q.y,q.z,q.w);
    public void Dispose() {
        // Meshes/materials of imported parts belong to Plugin, as before.
        foreach(var root in moving)if(root!=null){root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;}
        foreach(var material in ownedMaterials)if(material!=null)UnityEngine.Object.Destroy(material);
        ownedMaterials.Clear();
        if(equipmentRoot!=null)UnityEngine.Object.Destroy(equipmentRoot.gameObject);
    }
}
}
