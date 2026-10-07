using System;
using System.Linq;
using UnityEngine;

namespace Rachel350Z {
public static class VehicleMaterials {
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
