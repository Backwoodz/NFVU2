using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
public sealed class VehicleNeon : IDisposable {
    const int Columns = 7, Rows = 13;
    static readonly string[] TintProperties = { "_Color", "_TintColor", "_BaseColor" };
    readonly Transform car;
    readonly List<Light> lights = new List<Light>();
    readonly RaycastHit[] hits = new RaycastHit[24];
    readonly Vector3[] vertices = new Vector3[Columns * Rows];
    readonly bool[] grounded = new bool[Columns * Rows];
    readonly List<int> triangles = new List<int>();
    readonly int groundMask;
    GameObject projection;
    Mesh mesh;
    MeshRenderer renderer;
    Material material;
    Texture2D texture;
    float phase, sampleIn;
    bool disposed;
    public bool Enabled { get; set; } = true;
    public bool Pulsing { get; set; }
    public Color Color { get; set; } = new Color(.12f, 1, .3f);
    VehicleNeon(Transform car) {
        this.car = car;
        groundMask = LayerMask.GetMask("terrain", "piece", "static_solid", "Default");
        if (groundMask == 0) groundMask = ~0;
    }
    public static VehicleNeon Create(GameObject car, ModelData model, Action<string> log) {
        var result = new VehicleNeon(car.transform);
        try {
            if (model.neonTexture >= 0) {
                result.texture = VehicleMaterials.CreateEffectTexture(model.textures[model.neonTexture]);
                try { result.material = VehicleMaterials.CreateGlowMaterial(result.texture); }
                catch (InvalidOperationException e) { log?.Invoke("350Z neon projection unavailable: " + e.Message + "; ground lights remain active"); }
                if (result.material != null) {
                    result.projection = new GameObject("350Z_GroundedNeonMask");
                    result.mesh = new Mesh { name = "350Z_TerrainNeon" }; result.mesh.MarkDynamic();
                    var uv = new Vector2[Columns * Rows]; var colors = new Color[uv.Length]; var normals = new Vector3[uv.Length];
                    for (int row = 0; row < Rows; row++) for (int col = 0; col < Columns; col++) {
                        int i = row * Columns + col;
                        uv[i] = new Vector2((float)row / (Rows - 1), (float)col / (Columns - 1));
                        colors[i] = UnityEngine.Color.white; normals[i] = Vector3.up;
                    }
                    result.mesh.vertices = result.vertices; result.mesh.uv = uv; result.mesh.colors = colors; result.mesh.normals = normals;
                    result.projection.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                    result.renderer = result.projection.AddComponent<MeshRenderer>(); result.renderer.sharedMaterial = result.material;
                    result.renderer.shadowCastingMode = ShadowCastingMode.Off; result.renderer.receiveShadows = false; result.renderer.enabled = false;
                }
            }
            foreach (var position in new[] { new Vector3(-.75f, .08f, 0), new Vector3(.75f, .08f, 0), new Vector3(0, .08f, 1.65f), new Vector3(0, .08f, -1.65f) }) {
                var obj = new GameObject("350Z_NeonLight"); obj.transform.SetParent(car.transform, false); obj.transform.localPosition = position;
                obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var light = obj.AddComponent<Light>(); result.lights.Add(light);
                light.type = LightType.Spot; light.range = 1.6f; light.spotAngle = 145; light.shadows = LightShadows.None; light.enabled = false;
            }
            log?.Invoke("350Z neon: original 350Z_NEON mask, terrain projection and adjustable NFVU2 pulse");
            return result;
        } catch { result.Dispose(); throw; }
    }
    public void Tick(bool driving, float deltaTime) {
        if (disposed || car == null) return;
        bool active = Enabled && driving;
        phase = (phase + Mathf.Clamp(deltaTime, 0, .2f) * 2.5f) % (Mathf.PI * 2);
        float intensity = Pulsing ? .25f + .75f * (.5f + .5f * Mathf.Sin(phase)) : 1;
        foreach (var light in lights) { light.enabled = active; light.color = Color; light.intensity = intensity * 1.3f; }
        if (renderer == null) return;
        renderer.enabled = active;
        if (!active) { sampleIn = 0; return; }
        var tint = new Color(Color.r * intensity, Color.g * intensity, Color.b * intensity, .8f);
        foreach (var property in TintProperties) if (material.HasColor(property)) material.SetColor(property, tint);
        sampleIn -= Mathf.Max(0, deltaTime);
        if (sampleIn > 0) return;
        sampleIn = .06f;
        UpdateGround();
    }
    void UpdateGround() {
        // Independent world transform keeps the pool on the sampled ground
        // between updates, instead of making the terrain rotate with the car.
        projection.transform.position = car.position;
        for (int row = 0; row < Rows; row++) for (int col = 0; col < Columns; col++) {
            int index = row * Columns + col;
            var point = car.TransformPoint(new Vector3(((float)col / (Columns - 1) - .5f) * 2.7f, 0, ((float)row / (Rows - 1) - .5f) * 5.2f));
            var origin = point + Vector3.up * .9f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, 2.4f, groundMask, QueryTriggerInteraction.Ignore);
            float distance = float.MaxValue; int best = -1;
            for (int j = 0; j < count; j++) {
                var hit = hits[j];
                if (hit.collider == null || hit.collider.transform.IsChildOf(car) || Vector3.Dot(hit.normal, Vector3.up) < .35f || hit.distance >= distance) continue;
                best = j; distance = hit.distance;
            }
            grounded[index] = best >= 0;
            vertices[index] = best >= 0 ? hits[best].point + hits[best].normal * .025f - car.position : point - car.position;
        }
        triangles.Clear();
        for (int row = 0; row < Rows - 1; row++) for (int col = 0; col < Columns - 1; col++) {
            int a = row * Columns + col, b = a + Columns;
            AddTriangle(a, b, a + 1); AddTriangle(a + 1, b, b + 1);
        }
        mesh.vertices = vertices; mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
    }
    void AddTriangle(int a, int b, int c) {
        if (!grounded[a] || !grounded[b] || !grounded[c]) return;
        // Do not stretch a glowing sheet across steps, cliffs, or missing hits.
        if (Mathf.Abs(vertices[a].y - vertices[b].y) > .65f || Mathf.Abs(vertices[a].y - vertices[c].y) > .65f || Mathf.Abs(vertices[b].y - vertices[c].y) > .65f) return;
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
    }
    public void Dispose() {
        if (disposed) return; disposed = true;
        foreach (var light in lights) if (light != null) UnityEngine.Object.Destroy(light.gameObject);
        if (projection != null) UnityEngine.Object.Destroy(projection);
        if (mesh != null) UnityEngine.Object.Destroy(mesh);
        if (material != null) UnityEngine.Object.Destroy(material);
        if (texture != null) UnityEngine.Object.Destroy(texture);
        lights.Clear();
    }
}
}
