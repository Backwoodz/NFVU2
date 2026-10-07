using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rachel350Z;

class Program {
    static int checks;
    static void Check(bool valid,string name) {
        if(!valid)throw new Exception(name);
        Console.WriteLine("PASS "+name);checks++;
    }
    static void ExpectInvalid<T>(string name, string json) where T:Exception {
        string path=Path.Combine(AppContext.BaseDirectory,name+".json");
        File.WriteAllText(path,json);
        try { ModelFile.Load(path); }
        catch(T) { Console.WriteLine("PASS rejected "+name);checks++;return; }
        throw new Exception("Unexpectedly accepted "+name);
    }
    static void Main(string[] args) {
        var model=ModelFile.Load(args[0]);
        Check(model.schemaVersion==2 && model.preset=="DDAY_PLAYER_CAR","original beginning-of-game preset");
        Check(model.parts.Any(p=>p.name.StartsWith("350Z_KITW02_BODY_A")) && model.parts.Any(p=>p.name.StartsWith("SPOILER_STYLE02_A")),"widebody and spoiler imported");
        Check(model.parts.Any(p=>p.group=="wheel") && model.parts.Any(p=>p.group=="brake"),"original rim and brake geometry");
        var tyre=model.parts.Single(p=>p.group=="wheel" && model.materials[p.material].name.StartsWith("78743ca1_"));
        double tyreRadius=tyre.indices.Select(i=>tyre.vertices[i]).Max(v=>Math.Sqrt(v.y*v.y+v.z*v.z));
        Check(Math.Abs(tyreRadius-.35)<.00001,"visible tyre is 70 cm across, excluding backing geometry");
        Check(model.textures.Any(t=>t.name=="rachel_paint" && t.width==512 && t.height==512),"original vinyl atlas present");
        Check(model.parts.All(p=>p.normals.Length==p.vertices.Length && p.uv.Length==p.vertices.Length),"authored UVs and normals retained");
        Console.WriteLine("PASS model: "+model.parts.Length+" slices, "+model.parts.Sum(p=>p.indices.Length/3)+" triangles");checks++;
        ExpectInvalid<InvalidDataException>("missing-parts","{}");
        ExpectInvalid<InvalidDataException>("empty-file","");
        ExpectInvalid<JsonException>("truncated-json","{\"parts\":[");
        var doc=JObject.Parse(File.ReadAllText(args[0]));
        doc["parts"][0]["indices"][0]=-1;
        ExpectInvalid<InvalidDataException>("negative-index",doc.ToString());
        doc["parts"][0]["indices"][0]=999999;
        ExpectInvalid<InvalidDataException>("oversized-index",doc.ToString());
        doc=JObject.Parse(File.ReadAllText(args[0]));
        doc["parts"][0]["vertices"][0]["x"]=1000;
        ExpectInvalid<InvalidDataException>("invalid-coordinate",doc.ToString());
        doc=JObject.Parse(File.ReadAllText(args[0]));doc["parts"][0]["material"]=999;
        ExpectInvalid<InvalidDataException>("invalid-material-index",doc.ToString());
        doc=JObject.Parse(File.ReadAllText(args[0]));doc["parts"][0]["uv"]=new JArray();
        ExpectInvalid<InvalidDataException>("missing-uvs",doc.ToString());
        doc=JObject.Parse(File.ReadAllText(args[0]));doc["textures"][0]["width"]=2049;
        ExpectInvalid<InvalidDataException>("oversized-texture",doc.ToString());
        doc=JObject.Parse(File.ReadAllText(args[0]));doc["textures"][0]["rgba"]="AA==";
        ExpectInvalid<InvalidDataException>("truncated-texture",doc.ToString());
        doc=JObject.Parse(File.ReadAllText(args[0]));doc["materials"][0]["texture"]=999;
        ExpectInvalid<InvalidDataException>("invalid-texture-index",doc.ToString());
        doc=JObject.Parse(File.ReadAllText(args[0]));doc["frontWheelVisualOffset"]=10;
        ExpectInvalid<InvalidDataException>("invalid-visual-wheel-offset",doc.ToString());
        Check(DrivingMath.DirectionBrake(10,-1) && DrivingMath.TorqueFactor(10,-1,true)==0,"reverse input brakes forward motion");
        Check(DrivingMath.DirectionBrake(-5,1) && DrivingMath.TorqueFactor(-5,1,true)==0,"forward input brakes reverse motion");
        Check(!DrivingMath.DirectionBrake(0,-1) && DrivingMath.TorqueFactor(0,-1,true)==1,"reverse engages at standstill");
        Check(DrivingMath.TorqueFactor(20,1,true)==0 && DrivingMath.TorqueFactor(-8,-1,true)==0,"terrain and reverse speed limits");
        Check(DrivingMath.SteeringLimit(25,true)<DrivingMath.SteeringLimit(0,true) && DrivingMath.SteeringLimit(-25,true)==DrivingMath.SteeringLimit(25,true),"steering reduced with speed in either direction");
        float[] terrainWeights=Enumerable.Range(0,4).Select(i=>DrivingMath.WheelWeight(i,true)).ToArray();
        Check(Math.Abs(terrainWeights.Sum()-1f)<.00001f && terrainWeights[0]>0,"terrain assistance uses all four wheels");
        Check(DrivingMath.WheelWeight(0,false)==0 && DrivingMath.WheelWeight(2,false)>0,"road mode preserves rear-wheel drive");
        Check(Math.Abs(Enumerable.Range(0,4).Sum(i=>DrivingMath.WheelWeight(i,false))-1f)<.00001f,"road mode delivers full torque to the rear axle");
        Check(DrivingMath.GripTorqueFactor(3)<DrivingMath.GripTorqueFactor(0) && DrivingMath.GripTorqueFactor(3)>0,"wheelspin reduces torque without cutting it completely");
        Check(DrivingMath.WrapAngle(540)==-180 && DrivingMath.WrapAngle(-540)==-180 && DrivingMath.WrapAngle(361)==1,"camera can rotate repeatedly in both directions");
        Console.WriteLine(checks+" checks passed against the compiled plugin.");
    }
}
