using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Original NFS texture atlases, with particle motion/timing recreated for Unity.
// This component has no authority over boost forces, fuel or vehicle handling.
public sealed class VehicleNitroEffects : IDisposable {
    readonly List<ParticleSystem> flames = new List<ParticleSystem>();
    readonly List<ParticleSystem> jets = new List<ParticleSystem>();
    readonly List<ParticleSystem> pops = new List<ParticleSystem>();
    readonly List<Material> materials = new List<Material>();
    readonly List<Texture2D> textures = new List<Texture2D>();
    GameObject root;
    bool boosting, purging, backfireHeld, blockedLast, disposed;
    float backfireCooldown;

    public static VehicleNitroEffects Create(GameObject car, ModelData model, Action<string> log) {
        var effects = new VehicleNitroEffects();
        try {
            effects.Initialize(car, model, log);
            return effects;
        } catch (Exception error) {
            effects.Dispose();
            log("350Z nitro visuals unavailable: " + error.Message);
            return null;
        }
    }

    void Initialize(GameObject car, ModelData model, Action<string> log) {
        if (model.effectEmitters == null || model.effectEmitters.Length == 0) {
            log("350Z nitro visuals: model has no effect emitters; re-export the visual model.");
            return;
        }
        root = new GameObject("350Z_NitroEffects");
        root.transform.SetParent(car.transform, false);
        Material flame = null, purge = null, pop = null;
        if (model.nitroFlameTexture >= 0 && model.nitroFlameTexture < model.textures.Length) {
            var texture = CreateMask(model.textures[model.nitroFlameTexture], false);
            flame = CreateMaterial(texture, "350Z_NitroFlame", new Color(.12f, 1f, .025f, 1f));
            pop = CreateMaterial(texture, "350Z_Backfire", new Color(1f, .52f, .08f, 1f));
        }
        if (model.nitroPurgeTexture >= 0 && model.nitroPurgeTexture < model.textures.Length) {
            var texture = CreateMask(model.textures[model.nitroPurgeTexture], true);
            purge = CreateMaterial(texture, "350Z_NitrousPurge", new Color(.82f, .94f, 1f, 1f));
        }
        foreach (var emitter in model.effectEmitters) {
            if (emitter.role == "exhaust" && flame != null) {
                flames.Add(CreateEmitter(emitter, flame, false, false));
                pops.Add(CreateEmitter(emitter, pop, false, true));
            } else if (emitter.role == "purge" && purge != null) {
                jets.Add(CreateEmitter(emitter, purge, true, false));
            }
        }
        log("350Z nitro visuals: " + flames.Count + " green exhaust jets, " + jets.Count +
            " purge jets; original NFS effect textures, Unity particle motion.");
    }

    Texture2D CreateMask(ModelTexture data, bool useAlpha) {
        var texture = new Texture2D(data.width, data.height, TextureFormat.RGBA32, false) {
            name = data.name + "_NitroMask", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
        };
        textures.Add(texture);
        // FIREB is an opaque RGB additive atlas; SMOKE_B_CS stores coverage in
        // alpha. Treating the smoke's gray RGB as coverage produces square jets.
        int peak = 1;
        for (int i = 0; i < data.rgba.Length; i += 4)
            peak = Math.Max(peak, useAlpha ? data.rgba[i + 3] : Math.Max(data.rgba[i], Math.Max(data.rgba[i + 1], data.rgba[i + 2])));
        var pixels = new byte[data.rgba.Length];
        for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++) {
            int src = (y * data.width + x) * 4, dst = ((data.height - 1 - y) * data.width + x) * 4;
            int coverage = useAlpha ? data.rgba[src + 3] : Math.Max(data.rgba[src], Math.Max(data.rgba[src + 1], data.rgba[src + 2]));
            byte mask = (byte)(255 * coverage / peak);
            pixels[dst] = pixels[dst + 1] = pixels[dst + 2] = mask;
            pixels[dst + 3] = mask;
        }
        texture.SetPixelData<byte>(pixels, 0);
        texture.Apply(false, true);
        return texture;
    }

    Material CreateMaterial(Texture2D texture, string name, Color color) {
        var material = VehicleMaterials.CreateGlowMaterial(texture);
        materials.Add(material); material.name = name;
        // Retain color even when the supported fallback shader ignores the
        // particle renderer's vertex colors. Never mutate a game material.
        foreach (string property in new[] { "_Color", "_TintColor", "_BaseColor" })
            if (material.HasColor(property)) material.SetColor(property, color);
        return material;
    }

    ParticleSystem CreateEmitter(ModelEffectEmitter emitter, Material material, bool purge, bool pop) {
        var holder = new GameObject(purge ? "NitrousPurge" : pop ? "ExhaustBackfire" : "GreenExhaustFlame");
        holder.transform.SetParent(root.transform, false);
        holder.transform.localPosition = Vector(emitter.position);
        var direction = Vector(emitter.direction).normalized;
        holder.transform.localRotation = Quaternion.LookRotation(direction,
            Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > .98f ? Vector3.forward : Vector3.up);
        var system = holder.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false; main.loop = !pop; main.duration = 1f;
        main.simulationSpace = purge ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = purge ? 64 : pop ? 12 : 32;
        main.startLifetime = purge ? new ParticleSystem.MinMaxCurve(.3f, .48f) : new ParticleSystem.MinMaxCurve(.09f, pop ? .2f : .17f);
        main.startSpeed = purge ? new ParticleSystem.MinMaxCurve(4.5f, 6f) : new ParticleSystem.MinMaxCurve(2.2f, pop ? 3.5f : 4.2f);
        main.startSize = purge ? new ParticleSystem.MinMaxCurve(.2f, .3f) : new ParticleSystem.MinMaxCurve(.09f, pop ? .17f : .14f);
        main.startColor = Color.white; main.gravityModifier = 0f;
        main.startRotation = new ParticleSystem.MinMaxCurve(-.3f, .3f);
        var emission = system.emission;
        emission.enabled = !pop; emission.rateOverTime = purge ? 90f : 80f;
        var shape = system.shape;
        shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = purge ? 7f : 4f; shape.radius = purge ? .014f : .023f;
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, purge
            ? new AnimationCurve(new Keyframe(0f, .15f), new Keyframe(.3f, .6f), new Keyframe(1f, 1f))
            : new AnimationCurve(new Keyframe(0f, .55f), new Keyframe(.2f, 1f), new Keyframe(1f, .05f)));
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(purge ? .45f : .9f, 0f), new GradientAlphaKey(purge ? .3f : .7f, .35f), new GradientAlphaKey(0f, 1f) });
        var color = system.colorOverLifetime; color.enabled = true; color.color = gradient;
        // Both imported atlases contain four authored sprites arranged 2 x 2.
        var sheet = system.textureSheetAnimation;
        sheet.enabled = true; sheet.numTilesX = 2; sheet.numTilesY = 2;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 1f); sheet.cycleCount = 1;
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = purge ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.8f; renderer.velocityScale = .04f;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return system;
    }

    public void Tick(bool boost, bool purge, bool backfire, bool blocked, float dt) {
        if (disposed) return;
        if (blocked) {
            if (!blockedLast) StopAll();
            blockedLast = true; backfireHeld = false; backfireCooldown = 0f;
            return;
        }
        blockedLast = false;
        if (float.IsNaN(dt) || float.IsInfinity(dt)) dt = 0f;
        backfireCooldown = Mathf.Max(0f, backfireCooldown - Mathf.Clamp(dt, 0f, .1f));
        if (boost != boosting) { SetActive(flames, boost); boosting = boost; }
        if (purge != purging) { SetActive(jets, purge); purging = purge; }
        if (backfire && !backfireHeld && !boost && backfireCooldown <= 0f) {
            foreach (var system in pops) { system.Play(false); system.Emit(6); }
            backfireCooldown = .15f;
        }
        backfireHeld = backfire;
    }

    static Vector3 Vector(ModelVertex value) { return new Vector3(value.x, value.y, value.z); }
    static void SetActive(List<ParticleSystem> systems, bool active) {
        foreach (var system in systems)
            if (active) system.Play(false); else system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }
    void StopAll() {
        foreach (var group in new[] { flames, jets, pops }) foreach (var system in group)
            if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        boosting = purging = false;
    }
    public void Dispose() {
        if (disposed) return;
        disposed = true; StopAll();
        if (root != null) UnityEngine.Object.Destroy(root);
        foreach (var material in materials) if (material != null) UnityEngine.Object.Destroy(material);
        foreach (var texture in textures) if (texture != null) UnityEngine.Object.Destroy(texture);
        flames.Clear(); jets.Clear(); pops.Clear(); materials.Clear(); textures.Clear();
    }
}
}
