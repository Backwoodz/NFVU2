using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rachel350Z {
// Owns only light objects and private emissive material clones, never handling.
public sealed class VehicleLighting : IDisposable {
    // Names verified in the original LANGUAGES/English.bin and German.bin.
    // RGB and bright-preset gain are calibrated for Unity, not recovered NFS values.
    public static readonly string[] HeadlightPresets={"Serie","Xenon 4000K","Rot","Blau","Grün","Violett","Gelb",
        "Weiß extrahell","Xenon 8000K","Rot extrahell","Blau extrahell","Grün extrahell","Violett extrahell","Gelb extrahell"};
    static readonly Color[] HeadlightColors={new Color(1,.94f,.82f),new Color(.92f,.96f,1),
        new Color(1,.08f,.035f),new Color(.12f,.35f,1),new Color(.18f,1,.1f),new Color(.62f,.15f,1),new Color(1,.85f,.12f),
        Color.white,new Color(.65f,.8f,1),new Color(1,.08f,.035f),new Color(.12f,.35f,1),new Color(.18f,1,.1f),new Color(.62f,.15f,1),new Color(1,.85f,.12f)};
    sealed class Surface { public Renderer renderer; public Material original, material; public string role; }
    readonly List<Surface> surfaces = new List<Surface>();
    readonly List<Light> lights = new List<Light>();
    readonly List<string> roles = new List<string>();
    readonly VehicleLightingState state = new VehicleLightingState();
    bool disposed;
    Color headlightColor=HeadlightColors[0];
    float headlightPower=1;
    public bool Enabled { get => state.Enabled; set => state.Enabled = value; }
    public bool RearLightsOn => state.RearLightsOn;
    public Vector3[] RearLightPositions() => lights.Where((light,index)=>light!=null&&roles[index]=="taillight")
        .Select(light=>light.transform.localPosition).ToArray();
    public void SetHeadlights(string preset,float strength) {
        int index=Math.Max(0,Array.IndexOf(HeadlightPresets,preset));
        headlightColor=HeadlightColors[index];
        strength=float.IsNaN(strength)||float.IsInfinity(strength)?1:Mathf.Clamp(strength,0,3);
        headlightPower=strength*(index>=7?1.45f:1);
    }
    public static VehicleLighting Create(GameObject car, ModelData model, Action<string> log) {
        var result = new VehicleLighting();
        try {
            foreach (var p in model.parts.Where(p => !string.IsNullOrEmpty(p.lightRole))) {
                var node = car.transform.Find(p.name);
                var renderer = node != null ? node.GetComponent<Renderer>() : null;
                if (renderer == null) continue;
                var material = new Material(renderer.sharedMaterial) { name = "350Z_Lit_" + p.name };
                result.surfaces.Add(new Surface { renderer = renderer, original = renderer.sharedMaterial, material = material, role = p.lightRole });
                renderer.sharedMaterial = material;
            }
            foreach (var data in model.lights ?? new ModelLight[0]) {
                var obj = new GameObject("350Z_" + data.role); obj.transform.SetParent(car.transform, false);
                obj.transform.localPosition = Vector(data.position);
                obj.transform.localRotation = Quaternion.LookRotation(Vector(data.direction));
                var light = obj.AddComponent<Light>(); result.lights.Add(light); result.roles.Add(data.role);
                light.type = LightType.Spot; light.shadows = LightShadows.None; light.enabled = false;
                light.color = data.role == "taillight" ? new Color(1, .035f, .015f) : data.role == "reverse" ? Color.white : new Color(1, .94f, .82f);
                light.range = data.role == "headlight" ? 30 : data.role == "plate" ? .7f : data.role == "reverse" ? 4 : 2;
                light.spotAngle = data.role == "headlight" ? 65 : 110;
                light.intensity = data.role == "headlight" ? 3.5f : data.role == "plate" ? 1 : data.role == "reverse" ? .45f : .5f;
            }
            log?.Invoke("350Z lighting: " + result.lights.Count + " lights and " + result.surfaces.Count + " emitting surfaces");
            return result;
        } catch { result.Dispose(); throw; }
    }
    static Vector3 Vector(ModelVertex v) => new Vector3(v.x, v.y, v.z);
    public void Tick(bool driving, bool braking, float forwardSpeed=0, float throttle=0) {
        if (disposed) return;
        state.Update(driving, braking, forwardSpeed, throttle);
        for (int i = 0; i < lights.Count; i++) {
            lights[i].enabled = roles[i] == "reverse" ? state.ReverseLightsOn : roles[i] == "taillight" ? state.RearLightsOn : Enabled;
            if (roles[i] == "headlight") {
                lights[i].enabled=Enabled&&headlightPower>0;
                lights[i].color=headlightColor;lights[i].intensity=3.5f*headlightPower;
            }
            if (roles[i] == "taillight") lights[i].intensity = state.BrakeLightsOn ? 1.4f : .5f;
        }
        foreach (var surface in surfaces) {
            bool brakeOnly = surface.role == "brakelight";
            bool active = surface.role == "reverse" ? state.ReverseLightsOn : brakeOnly ? state.BrakeLightsOn : surface.role == "taillight" ? state.RearLightsOn : Enabled;
            Color color = surface.role == "reverse" ? Color.white : surface.role == "headlight" ? headlightColor : new Color(1, .015f, .005f);
            float power = surface.role == "headlight" ? 2.5f*headlightPower : surface.role == "reverse" ? .7f : braking && driving ? 2.8f : .65f;
            VehicleMaterials.SetEmission(surface.material, active ? color * power : Color.black);
        }
    }
    public void Dispose() {
        if (disposed) return; disposed = true;
        foreach (var surface in surfaces) {
            if (surface.renderer != null) surface.renderer.sharedMaterial = surface.original;
            if (surface.material != null) UnityEngine.Object.Destroy(surface.material);
        }
        foreach (var light in lights) if (light != null) UnityEngine.Object.Destroy(light.gameObject);
        surfaces.Clear(); lights.Clear(); roles.Clear();
    }
}
}
