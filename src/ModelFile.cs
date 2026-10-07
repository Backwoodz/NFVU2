using System;
using System.IO;
using Newtonsoft.Json;

namespace Rachel350Z {
public sealed class ModelData {
    public int schemaVersion; public string preset;
    public float wheelVisualLift, frontWheelVisualOffset, rearWheelVisualOffset;
    public ModelPart[] parts; public ModelMaterial[] materials; public ModelTexture[] textures;
}
public sealed class ModelPart { public string name, group; public int material; public ModelVertex[] vertices, normals; public ModelUV[] uv; public int[] indices; }
public struct ModelVertex { public float x, y, z; }
public struct ModelUV { public float u, v; }
public sealed class ModelMaterial { public string name; public float[] color; public float metallic, smoothness; public int texture; }
public sealed class ModelTexture { public string name; public int width, height; public byte[] rgba; }

// Use the managed JSON reader shipped with Valheim. Unity's native serializer
// did not populate the external plugin's model fields in the installed game.
public static class ModelFile {
    public static ModelData Load(string path) {
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("350Z model file missing", path);
        if (file.Length == 0 || file.Length > 16 * 1024 * 1024)
            throw new InvalidDataException("350Z model file has an invalid size: " + file.Length);
        var model = JsonConvert.DeserializeObject<ModelData>(File.ReadAllText(path),
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 32 });
        if (model?.parts == null || model.parts.Length == 0 || model.parts.Length > 256)
            throw new InvalidDataException("350Z model must contain between 1 and 256 parts: " + path);
        if (model.schemaVersion != 0 && model.schemaVersion != 2) throw new InvalidDataException("Unsupported visual model schema");
        if (model.schemaVersion == 2) {
            foreach (var offset in new[] { model.wheelVisualLift, model.frontWheelVisualOffset, model.rearWheelVisualOffset })
                if (!ValidCoordinate(offset) || Math.Abs(offset) > .2f) throw new InvalidDataException("Invalid visual wheel offset");
            if (model.materials == null || model.materials.Length == 0 || model.materials.Length > 256 || model.textures == null || model.textures.Length > 128)
                throw new InvalidDataException("Invalid visual material table");
            foreach (var texture in model.textures)
                if (texture == null || texture.width < 1 || texture.height < 1 || texture.width > 2048 || texture.height > 2048 ||
                    texture.rgba == null || texture.rgba.Length != (long)texture.width * texture.height * 4)
                    throw new InvalidDataException("Invalid RGBA texture");
            foreach (var material in model.materials) {
                if (material == null || material.color == null || material.color.Length != 4 || material.texture < -1 || material.texture >= model.textures.Length ||
                    !Unit(material.metallic) || !Unit(material.smoothness)) throw new InvalidDataException("Invalid material");
                foreach (var c in material.color) if (!Unit(c)) throw new InvalidDataException("Invalid material color");
            }
        }
        foreach (var part in model.parts) {
            if (part == null || string.IsNullOrWhiteSpace(part.name) || part.vertices == null ||
                part.vertices.Length < 3 || part.vertices.Length > 65535 ||
                part.indices == null || part.indices.Length < 3 || part.indices.Length % 3 != 0)
                throw new InvalidDataException("350Z model contains an incomplete mesh");
            foreach (var vertex in part.vertices)
                if (!ValidCoordinate(vertex.x) || !ValidCoordinate(vertex.y) || !ValidCoordinate(vertex.z))
                    throw new InvalidDataException("350Z model has an invalid vertex: " + part.name);
            foreach (var index in part.indices)
                if (index < 0 || index >= part.vertices.Length)
                    throw new InvalidDataException("350Z model has an out-of-range triangle index: " + part.name);
            if (model.schemaVersion == 2) {
                if (part.material < 0 || part.material >= model.materials.Length ||
                    (part.group != "body" && part.group != "wheel" && part.group != "brake") ||
                    part.uv == null || part.uv.Length != part.vertices.Length || part.normals == null || part.normals.Length != part.vertices.Length)
                    throw new InvalidDataException("Invalid visual mesh attributes");
                foreach (var uv in part.uv) if (!ValidCoordinate(uv.u) || !ValidCoordinate(uv.v)) throw new InvalidDataException("Invalid UV");
                foreach (var n in part.normals) if (!ValidCoordinate(n.x) || !ValidCoordinate(n.y) || !ValidCoordinate(n.z)) throw new InvalidDataException("Invalid normal");
            }
        }
        return model;
    }
    static bool ValidCoordinate(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100f;
    static bool Unit(float value) => ValidCoordinate(value) && value >= 0 && value <= 1;
}
}
