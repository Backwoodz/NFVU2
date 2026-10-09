using System;
using System.IO;
using Newtonsoft.Json;

namespace Rachel350Z {
public sealed class ModelData {
    public int schemaVersion; public string preset;
    public float wheelVisualLift, frontWheelVisualOffset, rearWheelVisualOffset;
    public ModelPart[] parts; public ModelMaterial[] materials; public ModelTexture[] textures;
    public ModelLight[] lights; public int neonTexture = -1;
    public ModelEffectEmitter[] effectEmitters;
    public int nitroFlameTexture = -1, nitroPurgeTexture = -1;
    public ModelAnimation[] animations;
    public ModelTrunkLayout[] trunkLayouts;
    public ModelAudioComponent[] audioComponents;
    public TurretRig turretRig;
}
public sealed class ModelAnimation { public string name; public ModelVertex pivot; public ModelAnimationFrame[] frames; }
public sealed class ModelAnimationFrame { public ModelRotation rotation; public ModelVertex offset; }
public struct ModelRotation { public float x,y,z,w; }
public sealed class ModelTrunkLayout { public string title; public ModelAudioSocket[] sockets; }
public sealed class ModelAudioSocket { public string size; public ModelVertex position,right,up,forward; }
public sealed class ModelAudioComponent { public string id,title,kind,size; }
public sealed class ModelEffectEmitter { public string role; public ModelVertex position,direction; }
public sealed class ModelPart { public string name, group, lightRole, movingPart, optionGroup, option; public bool rotatesWithWheel; public int material; public ModelVertex[] vertices, normals; public ModelUV[] uv; public int[] indices; }
public sealed class ModelLight { public string role; public ModelVertex position, direction; }
public struct ModelVertex { public float x, y, z; }
public struct ModelUV { public float u, v; }
public sealed class ModelMaterial { public string name; public float[] color; public float metallic, smoothness; public int texture; public bool bodyPaint; }
public sealed class ModelTexture { public string name; public int width, height; public byte[] rgba; }

// Use the managed JSON reader shipped with Valheim. Unity's native serializer
// did not populate the external plugin's model fields in the installed game.
public static class ModelFile {
    public static ModelData Load(string path) {
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("350Z model file missing", path);
        if (file.Length == 0 || file.Length > 32 * 1024 * 1024)
            throw new InvalidDataException("350Z model file has an invalid size: " + file.Length);
        ModelData model;
        using(var stream=File.OpenText(path))using(var reader=new JsonTextReader(stream))
            model=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=32,CheckAdditionalContent=true}).Deserialize<ModelData>(reader);
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
            if (model.neonTexture < -1 || model.neonTexture >= model.textures.Length) throw new InvalidDataException("Invalid neon texture");
            if (model.nitroFlameTexture < -1 || model.nitroFlameTexture >= model.textures.Length ||
                model.nitroPurgeTexture < -1 || model.nitroPurgeTexture >= model.textures.Length)
                throw new InvalidDataException("Invalid nitro texture");
            if(model.effectEmitters!=null) {
                if(model.effectEmitters.Length>16)throw new InvalidDataException("Too many effect emitters");
                foreach(var emitter in model.effectEmitters) {
                    if(emitter==null || (emitter.role!="exhaust"&&emitter.role!="purge") ||
                        !ValidVector(emitter.position)||!ValidVector(emitter.direction))
                        throw new InvalidDataException("Invalid effect emitter");
                    var d=emitter.direction;
                    if(d.x*d.x+d.y*d.y+d.z*d.z<.001f)throw new InvalidDataException("Zero emitter direction");
                }
            }
            if (model.lights != null) {
                if (model.lights.Length > 16) throw new InvalidDataException("Too many vehicle lights");
                foreach (var light in model.lights) {
                    if (light == null || (light.role != "headlight" && light.role != "taillight" && light.role != "plate" && light.role != "reverse") ||
                        !ValidVector(light.position) || !ValidVector(light.direction)) throw new InvalidDataException("Invalid vehicle light");
                    var d = light.direction;
                    if (d.x*d.x+d.y*d.y+d.z*d.z < .001f) throw new InvalidDataException("Zero light direction");
                }
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
                if (part.rotatesWithWheel && part.group != "brake") throw new InvalidDataException("Only brake rotors may opt into wheel rotation");
                if (!string.IsNullOrEmpty(part.lightRole) && part.lightRole != "headlight" && part.lightRole != "taillight" && part.lightRole != "brakelight" && part.lightRole != "reverse")
                    throw new InvalidDataException("Invalid light surface role");
                if (part.material < 0 || part.material >= model.materials.Length ||
                    (part.group != "body" && part.group != "wheel" && part.group != "brake" && part.group != "audio" && part.group != "spinner") ||
                    part.uv == null || part.uv.Length != part.vertices.Length || part.normals == null || part.normals.Length != part.vertices.Length)
                    throw new InvalidDataException("Invalid visual mesh attributes");
                foreach (var uv in part.uv) if (!ValidCoordinate(uv.u) || !ValidCoordinate(uv.v)) throw new InvalidDataException("Invalid UV");
                foreach (var n in part.normals) if (!ValidCoordinate(n.x) || !ValidCoordinate(n.y) || !ValidCoordinate(n.z)) throw new InvalidDataException("Invalid normal");
            }
        }
        ValidateShowroom(model);
        return model;
    }
    static void ValidateShowroom(ModelData model) {
        if(model.animations==null&&model.trunkLayouts==null&&model.audioComponents==null)return;
        if(model.schemaVersion!=2||model.animations==null||model.animations.Length!=8||model.trunkLayouts==null||model.trunkLayouts.Length!=4||model.audioComponents==null||model.audioComponents.Length>64)
            throw new InvalidDataException("Incomplete showroom catalog");
        var names=new System.Collections.Generic.HashSet<string>();
        foreach(var animation in model.animations) {
            if(animation==null||!names.Add(animation.name)||!ValidVector(animation.pivot)||animation.frames==null||animation.frames.Length<2||animation.frames.Length>120)
                throw new InvalidDataException("Invalid showroom animation");
            foreach(var frame in animation.frames) {
                if(frame==null||!ValidVector(frame.offset))throw new InvalidDataException("Invalid motion frame");
                var q=frame.rotation;float norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
                if(!ValidCoordinate(norm)||Math.Abs(norm-1)>.01f)throw new InvalidDataException("Invalid motion quaternion");
            }
        }
        foreach(var name in new[]{"HOOD_REG","TRUNK_REG","LEFT_DOOR_REG","RIGHT_DOOR_REG","LEFT_DOOR_SUICIDE","RIGHT_DOOR_SUICIDE","LEFT_DOOR_SCISSOR","RIGHT_DOOR_SCISSOR"})
            if(!names.Contains(name))throw new InvalidDataException("Missing motion track");
        foreach(var layout in model.trunkLayouts) {
            if(layout==null||layout.sockets==null||layout.sockets.Length>6)throw new InvalidDataException("Invalid trunk layout");
            foreach(var socket in layout.sockets) {
                if(socket==null||!AudioSize(socket.size)||!ValidVector(socket.position)||!Basis(socket.right,socket.up,socket.forward))
                    throw new InvalidDataException("Invalid audio socket");
            }
        }
        names.Clear();
        foreach(var component in model.audioComponents) {
            if(component==null||string.IsNullOrEmpty(component.id)||!names.Add(component.id)||string.IsNullOrEmpty(component.title)||!AudioSize(component.size)||
                (component.kind!="speaker"&&component.kind!="amp"&&component.kind!="lcd"&&component.kind!="empty"))throw new InvalidDataException("Invalid audio component");
        }
        foreach(var layout in model.trunkLayouts)foreach(var socket in layout.sockets) {
            int count=0;foreach(var component in model.audioComponents)if(component.size==socket.size)count++;
            if(count!=13)throw new InvalidDataException("Incomplete component choices for socket size");
        }
        var available=new System.Collections.Generic.HashSet<string>();
        foreach(var part in model.parts) {
            if(!string.IsNullOrEmpty(part.movingPart)&&part.movingPart!="leftDoor"&&part.movingPart!="rightDoor"&&part.movingPart!="hood"&&part.movingPart!="trunk")throw new InvalidDataException("Invalid moving part");
            if(part.group=="audio") {
                if(part.optionGroup!="audio"||!names.Contains(part.option)||!string.IsNullOrEmpty(part.movingPart))throw new InvalidDataException("Unknown audio mesh");
                available.Add(part.option);
            } else if(!string.IsNullOrEmpty(part.optionGroup)) {
                int max=part.optionGroup=="trunk"?3:part.optionGroup=="engine"?2:-1;
                if(part.group!="body"||!int.TryParse(part.option,out int index)||index<0||index>max)throw new InvalidDataException("Invalid showroom option");
            }
        }
        if(!available.SetEquals(names))throw new InvalidDataException("Audio component has no mesh");
    }
    static bool AudioSize(string size)=>size=="SMALL"||size=="MEDIUM"||size=="LARGE";
    static float Dot(ModelVertex a,ModelVertex b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    static bool Basis(ModelVertex a,ModelVertex b,ModelVertex c) {
        if(!ValidVector(a)||!ValidVector(b)||!ValidVector(c))return false;
        float determinant=a.x*(b.y*c.z-b.z*c.y)-b.x*(a.y*c.z-a.z*c.y)+c.x*(a.y*b.z-a.z*b.y);
        return Math.Abs(Dot(a,a)-1)<.01&&Math.Abs(Dot(b,b)-1)<.01&&Math.Abs(Dot(c,c)-1)<.01&&Math.Abs(Dot(a,b))<.01&&Math.Abs(Dot(a,c))<.01&&Math.Abs(Dot(b,c))<.01&&Math.Abs(determinant-1)<.01;
    }
    static bool ValidVector(ModelVertex v) => ValidCoordinate(v.x) && ValidCoordinate(v.y) && ValidCoordinate(v.z);
    static bool ValidCoordinate(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100f;
    static bool Unit(float value) => ValidCoordinate(value) && value >= 0 && value <= 1;
}
}
