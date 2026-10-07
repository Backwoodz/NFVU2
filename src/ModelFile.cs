using System;
using System.IO;
using Newtonsoft.Json;

namespace Rachel350Z {
public sealed class ModelData { public ModelPart[] parts; }
public sealed class ModelPart { public string name; public ModelVertex[] vertices; public int[] indices; }
public struct ModelVertex { public float x, y, z; }

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
        }
        return model;
    }
    static bool ValidCoordinate(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100f;
}
}
