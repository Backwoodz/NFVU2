using System;
using System.Linq;
using UnityEngine;

namespace Rachel350Z {
public static class VehicleMaterials {
    public static Material[] CreateForModel(MonoBehaviour local, ModelData model, Action<string> log, out Texture2D[] textures) {
        textures = new Texture2D[model.textures.Length];
        var templates = Create(local, log);
        var result = new Material[model.materials.Length];
        try {
            for (int i = 0; i < textures.Length; i++) {
                var data = model.textures[i];
                var texture = new Texture2D(data.width, data.height, TextureFormat.RGBA32, true) { name = data.name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                textures[i] = texture;
                // Original PNG/image rows are top-down; Unity raw upload is bottom-up.
                var pixels = new byte[data.rgba.Length]; int stride = data.width * 4;
                for (int row = 0; row < data.height; row++) Array.Copy(data.rgba, row * stride, pixels, (data.height - 1 - row) * stride, stride);
                // body.json contains level 0 only. LoadRawTextureData expects
                // bytes for the entire allocated mip chain; SetPixelData targets
                // just the supplied level, then Apply generates the smaller mips.
                texture.SetPixelData<byte>(pixels, 0);
                texture.Apply(true, true);
            }
            for (int i = 0; i < result.Length; i++) {
                var data = model.materials[i]; var c = data.color;
                var material = Clone(templates[0], "350Z_" + data.name, new Color(c[0], c[1], c[2], c[3])); result[i] = material;
                foreach (var property in new[] { "_MainTex", "_BaseMap", "_Albedo" })
                    if (data.texture >= 0 && material.HasTexture(property)) material.SetTexture(property, textures[data.texture]);
                if (material.HasFloat("_Metallic")) material.SetFloat("_Metallic", data.metallic);
                foreach (var property in new[] { "_Glossiness", "_Smoothness" }) if (material.HasFloat(property)) material.SetFloat(property, data.smoothness);
            }
            return result;
        } catch {
            foreach (var m in result) if (m != null) UnityEngine.Object.Destroy(m);
            foreach (var t in textures) if (t != null) UnityEngine.Object.Destroy(t);
            textures = null; throw;
        } finally { foreach (var m in templates) UnityEngine.Object.Destroy(m); }
    }
    // Materials loaded by the game retain their actual asset-bundle shader
    // references; Shader.Find by an assumed name does not provide that guarantee.
    public static Material[] Create(MonoBehaviour local, Action<string> log) {
        var loaded = Resources.FindObjectsOfTypeAll<Material>();
        var source = loaded.Where(m => Score(m) >= 0)
            .OrderByDescending(Score).ThenBy(m => m.name, StringComparer.Ordinal).FirstOrDefault();
        if (source == null && local != null) {
            source = local.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
                .Where(m => m != null && m.shader != null && m.shader.isSupported)
                .OrderByDescending(Score).FirstOrDefault();
        }
        if (source == null) {
            var names = loaded.Where(m => m != null && m.shader != null)
                .Select(m => m.shader.name).Distinct().OrderBy(n => n).Take(30);
            throw new InvalidOperationException("No loaded material available for 350Z. Loaded shaders: " + string.Join(", ", names));
        }
        log("350Z material template: " + source.name + "; shader: " + source.shader.name + "; queue: " + source.renderQueue);
        Material paint = null, dark = null;
        try {
            paint = Clone(source, "350Z_Paint", new Color(.25f, .7f, .06f, 1f));
            dark = Clone(source, "350Z_Dark", new Color(.045f, .045f, .05f, 1f));
            return new[] { paint, dark };
        } catch {
            if (paint != null) UnityEngine.Object.Destroy(paint);
            if (dark != null) UnityEngine.Object.Destroy(dark);
            throw;
        }
    }
    static int Score(Material material) {
        if (material == null || material.shader == null || !material.shader.isSupported) return -1;
        if (material.name.StartsWith("350Z_", StringComparison.Ordinal)) return -1;
        var shader = material.shader.name.ToLowerInvariant();
        if (new[] { "hidden/", "ui/", "particle", "sky", "water", "terrain", "grass", "vegetation", "tree", "decal", "effect" }.Any(shader.Contains)) return -1;
        if (material.renderQueue >= 2450 || material.GetTag("RenderType", false, "").IndexOf("transparent", StringComparison.OrdinalIgnoreCase) >= 0) return -1;
        if (!material.HasColor("_Color") && !material.HasColor("_BaseColor")) return -1;
        if (shader.Contains("piece")) return 400;
        if (shader.Contains("static")) return 350;
        if (shader.Contains("standard") || shader.EndsWith("/lit")) return 300;
        if (shader.Contains("creature")) return 200;
        if (shader.Contains("player")) return 150;
        return 100;
    }
    static Material Clone(Material source, string name, Color color) {
        var material = new Material(source) { name = name, renderQueue = 2000 };
        try {
            foreach (var property in new[] { "_Color", "_BaseColor", "_SkinColor" })
                if (material.HasColor(property)) material.SetColor(property, color);
            foreach (var property in new[] { "_MainTex", "_BaseMap", "_Albedo" })
                if (material.HasTexture(property)) {
                    material.SetTexture(property, Texture2D.whiteTexture);
                    material.SetTextureScale(property, Vector2.one);
                    material.SetTextureOffset(property, Vector2.zero);
                }
            foreach (var property in new[] { "_BumpMap", "_NormalMap" })
                if (material.HasTexture(property)) material.SetTexture(property, Texture2D.normalTexture);
            if (material.HasColor("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
            if (material.HasTexture("_EmissionMap")) material.SetTexture("_EmissionMap", Texture2D.blackTexture);
            if (material.HasFloat("_Metallic")) material.SetFloat("_Metallic", .2f);
            if (material.HasFloat("_Glossiness")) material.SetFloat("_Glossiness", .45f);
            if (material.HasFloat("_Smoothness")) material.SetFloat("_Smoothness", .45f);
            return material;
        } catch { UnityEngine.Object.Destroy(material); throw; }
    }
}
}
