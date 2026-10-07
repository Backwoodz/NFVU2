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
        if(model.parts.Length!=16 || model.parts.Sum(p=>p.indices.Length/3)!=3449 ||
           model.parts[0].name!="350Z_BASE_A" || Math.Abs(model.parts[0].vertices[0].x-.7669976f)>.000001f)
            throw new Exception("Model data did not deserialize correctly");
        Console.WriteLine("PASS installed model: "+model.parts.Length+" parts, "+model.parts.Sum(p=>p.vertices.Length)+" vertices, 3449 triangles");checks++;
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
