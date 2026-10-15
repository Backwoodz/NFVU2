using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static void Reject(Action action,string message){try{action();}catch(InvalidDataException){Check(true,message);return;}throw new Exception("Accepted: "+message);}
    static void Main(string[] args) {
        float velocity=0;
        for(int i=0;i<50;i++)velocity=TransformerGait.AdvanceSpeed(velocity,1,.02f,true);
        Check(velocity>.6f&&velocity<.7f,"first second builds speed gradually instead of jumping to cruise");
        for(int i=0;i<125;i++)velocity=TransformerGait.AdvanceSpeed(velocity,1,.02f,true);
        Check(velocity==TransformerGait.MaxSpeed,"holding forward reaches the faster cruise speed after several seconds");
        float before=velocity;velocity=TransformerGait.AdvanceSpeed(velocity,-1,.02f,true);
        Check(velocity>0&&velocity<before,"reversing first brakes the forward motion");
        for(int i=0;i<60;i++)velocity=TransformerGait.AdvanceSpeed(velocity,0,.02f,false);
        Check(velocity==0,"release or return request brakes faster than acceleration");
        var gait=new TransformerGait();
        for(int i=0;i<30;i++)gait.Step(1f/60,0,0,true);
        Check(gait.Ready&&gait.Amount==0,"armor deploys before feet or vehicle begin moving");
        for(int i=0;i<120;i++)gait.Step(1f/60,TransformerGait.MaxSpeed,0,true);
        Check(gait.Amount==1&&gait.Left==1&&gait.Right==1,"forward walking reaches its bounded full stride");
        float phase=gait.Phase,amount=gait.Amount;
        gait.Step(0,0,0,false);gait.Step(float.NaN,0,0,false);gait.Step(float.PositiveInfinity,0,0,false);
        Check(gait.Phase==phase&&gait.Amount==amount,"paused or invalid gait time preserves foot pose");
        for(int i=0;i<120;i++)gait.Step(1f/60,0,1,true);
        Check(gait.Left<-.99f&&gait.Right>.99f,"pivot turn gives the feet opposite travel directions");
        for(int i=0;i<240;i++) {
            gait.Step(1f/60,(float)Math.Sin(i*.03)*TransformerGait.MaxSpeed,(float)Math.Cos(i*.05),true);
            foreach(bool right in new[]{false,true}) {
                gait.Foot(right,out float forward,out float lift,out float swing);
                if(Math.Abs(forward)>TransformerGait.Stride+.0001||lift<-.0001||lift>TransformerGait.Lift+.0001||Math.Abs(swing)>TransformerGait.ArmSwing+.0001)throw new Exception("Gait escaped reviewed pose domain");
                if(forward*swing<-.0001||gait.Elbow(right)<0||gait.Elbow(right)>10.001f)throw new Exception("Arms must counter-swing with bounded elbow flexion");
            }
        }
        Check(true,"reversals and mixed turns stay within reviewed stride, lift and arm limits");
        gait.Step(1f/60,0,0,false);
        Check(gait.Release==1&&!gait.Settled,"stopping retains clearance until feet settle");
        for(int i=0;i<120;i++)gait.Step(1f/60,0,0,false);
        Check(gait.Settled&&gait.Phase==0,"stop closes armor and restores exact neutral gait state");
        gait.Step(.1f,0,0,true);gait.Reset();
        Check(gait.Settled&&!gait.Ready,"emergency gait reset clears deployment");
        var state=new TransformerState();
        Check(!state.Begin(false,true,true,true)&&!state.Begin(true,false,true,true)&&!state.Begin(true,true,false,true)&&!state.Begin(true,true,true,false),"entry requires rider, stop, ground and clearance");
        Check(state.Begin(true,true,true,true)&&!state.CanWalk&&!state.Return(),"transition blocks walking and premature return");
        state.Step(1,true);state.Step(float.NaN,false);state.Step(float.PositiveInfinity,false);state.Step(-1,false);
        Check(state.Progress==0,"pause and invalid time never advance transformation");
        state.Step(20,false);Check(Math.Abs(state.Progress-.02f)<.00001f,"long frame cannot skip deployment");
        for(int i=0;i<300;i++)state.Step(1f/48,false);
        Check(state.CanWalk&&state.Progress==1&&!state.Begin(true,true,true,true),"robot endpoint holds without retriggering");
        Check(state.Return()&&!state.Return()&&!state.CanWalk,"return locks locomotion and duplicate requests");
        for(int i=0;i<300;i++)state.Step(1f/48,false);
        Check(!state.Active&&state.Progress==0,"return restores exact car state");
        state.Begin(true,true,true,true);state.Step(.1f,false);state.Reset();
        Check(!state.Active&&!state.Returning&&state.Progress==0,"emergency reset clears transition");
        var asset=TransformerAsset.Load(args[0],args[1]);var m=asset.Manifest;
        Check(!m.walkingReady||m.gaitVersion==TransformerGait.Version,"walking requires the reviewed gait version");
        Check(asset.Frames.Length==241&&m.fps==48&&m.duration==5,"approved half-frame samples survive import");
        Check(m.nodes.Count(n=>n.name.StartsWith("neon."))==7,"seven whole neon tracks retained");
        Check(new[]{"panel.front_wheel_L","panel.front_wheel_R","panel.rear_wheel_L","panel.rear_wheel_R","panel.exhaust_L","panel.exhaust_R"}.All(name=>m.nodes.Any(n=>n.name==name)),"live wheel and exhaust attachment nodes present");
        Check(m.nodes.Any(n=>n.name=="rider.anchor")&&m.nodes.Count(n=>n.name.StartsWith("target:"))==5,"existing-player IK targets present");
        Check(asset.Batches.All(b=>b.Bones.Length<=48)&&asset.Batches.Sum(b=>b.Weights.Length)==m.vertices,"bounded skin palettes and complete geometry");
        Check(m.gaitReleases.Length>0&&m.gaitReleases.All(r=>m.nodes[r.node].name.StartsWith("panel.")||m.nodes[r.node].name=="neon.trunk rear"),"walking releases address only rigid armor and the whole neon mount");
        foreach(var b in asset.Batches)for(int i=0;i<b.Weights.Length;i++)if(b.Weights[i]<0||b.Weights[i]>=b.Bones.Length)throw new Exception("Bad rigid binding");
        Check(true,"each vertex bound rigidly to one valid node");
        var tmp=Path.Combine(AppContext.BaseDirectory,"invalid-transformer");Directory.CreateDirectory(tmp);
        var json=Path.Combine(tmp,"transformer.json");var bin=Path.Combine(tmp,"transformer.bin");
        File.Copy(Path.Combine(args[0],"transformer.bin"),bin,true);
        string original=File.ReadAllText(Path.Combine(args[0],"transformer.json"));
        Action save=()=>File.WriteAllText(json,JsonConvert.SerializeObject(m));
        m.schema=2;save();Reject(()=>TransformerAsset.Load(tmp,args[1]),"unknown asset schema rejected");m.schema=1;
        bool ready=m.walkingReady;int gaitVersion=m.gaitVersion;m.walkingReady=true;m.gaitVersion=999;save();Reject(()=>TransformerAsset.Load(tmp,args[1]),"unknown active gait rejected");m.walkingReady=ready;m.gaitVersion=gaitVersion;
        float shift=m.gaitReleases[0].offset[0];m.gaitReleases[0].offset[0]=float.NaN;save();Reject(()=>TransformerAsset.Load(tmp,args[1]),"invalid walking release rejected");m.gaitReleases[0].offset[0]=shift;
        string hash=m.bodySha256;m.bodySha256=new string('0',64);save();Reject(()=>TransformerAsset.Load(tmp,args[1]),"different vehicle model rejected");m.bodySha256=hash;
        m.nodes[1].parent=1;save();Reject(()=>TransformerAsset.Load(tmp,args[1]),"cyclic node hierarchy rejected");
        File.WriteAllText(json,original);
        using(var f=File.Open(bin,FileMode.Open,FileAccess.Write)){f.Position=f.Length-1;f.WriteByte(255);}
        Reject(()=>TransformerAsset.Load(tmp,args[1]),"corrupt binary rejected before allocation");
        m=JsonConvert.DeserializeObject<TransformerManifest>(original);m.payloadSha256=TransformerAsset.Hash(bin);save();
        Reject(()=>TransformerAsset.Load(tmp,args[1]),"invalid triangle index rejected even with matching checksum");
        File.Copy(Path.Combine(args[0],"transformer.bin"),bin,true);
        using(var f=File.Open(bin,FileMode.Open,FileAccess.Write)){f.Position=20;var bytes=BitConverter.GetBytes(float.NaN);f.Write(bytes,0,4);}
        m.payloadSha256=TransformerAsset.Hash(bin);save();Reject(()=>TransformerAsset.Load(tmp,args[1]),"NaN animation coordinate rejected");
        GaitPreview.Save(Path.Combine(AppContext.BaseDirectory,"runtime-gait-preview.json"));
        Console.WriteLine(checks+" transformer checks passed; Unity physics, skinning and live Player are NOT executed here.");
    }
}
