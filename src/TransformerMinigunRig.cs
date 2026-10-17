using System;
using System.IO;
using Newtonsoft.Json;
namespace Rachel350Z {
// Managed only; optional Rust geometry and its extracted pivot metadata are
// validated on the spawn worker before Unity objects are constructed.
public sealed class TransformerMinigunRig {
    public int schemaVersion;
    public ModelVertex barrelPivot,muzzle,barrelAxis;
    public string forward,up,units;
    public static TransformerMinigunRig Load(string path) {
        var file=new FileInfo(path);
        if(!file.Exists||file.Length<=0||file.Length>32768)throw new InvalidDataException("Invalid minigun rig file size");
        TransformerMinigunRig rig;
        using(var stream=File.OpenText(path))using(var reader=new JsonTextReader(stream))
            rig=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=8,CheckAdditionalContent=true}).Deserialize<TransformerMinigunRig>(reader);
        if(rig==null)throw new InvalidDataException("Missing minigun rig");
        rig.Validate();return rig;
    }
    public void Validate() {
        if(schemaVersion!=1||forward!="+Z"||up!="+Y"||units!="metres")throw new InvalidDataException("Unsupported minigun coordinates");
        foreach(var vector in new[]{barrelPivot,muzzle,barrelAxis})foreach(float value in new[]{vector.x,vector.y,vector.z})
            if(float.IsNaN(value)||float.IsInfinity(value)||Math.Abs(value)>5)throw new InvalidDataException("Invalid minigun rig coordinate");
        float norm=barrelAxis.x*barrelAxis.x+barrelAxis.y*barrelAxis.y+barrelAxis.z*barrelAxis.z;
        if(Math.Abs(norm-1)>.001f||Math.Abs(barrelAxis.x)>.01f||Math.Abs(barrelAxis.y)>.01f||barrelAxis.z<.99f
            ||muzzle.z<=barrelPivot.z+.03f)throw new InvalidDataException("Invalid minigun barrel axis or muzzle");
    }
}
}
