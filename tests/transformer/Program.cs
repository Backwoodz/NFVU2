using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static void Reject(Action action,string message){try{action();}catch(InvalidDataException){Check(true,message);return;}throw new Exception("Accepted: "+message);}
    static float[] Pose(TransformerGait g) {
        var values=new System.Collections.Generic.List<float>{g.Phase,g.Amount,g.Release,g.Left,g.Right,g.PelvisHeight,g.PelvisSide,g.PelvisYaw,g.TorsoLean,g.TorsoRoll,g.TorsoYaw};
        foreach(bool right in new[]{false,true}) {
            g.Foot(right,out float forward,out float lift,out float swing);
            values.AddRange(new[]{forward,lift,swing,g.FootSide(right),g.FootPitch(right),g.FootYaw(right),g.Elbow(right)});
        }
        return values.ToArray();
    }
    static TransformerGait Rehearse(int hz,float duration=9) {
        var g=new TransformerGait();float dt=1f/hz;
        for(int i=0;i<(int)Math.Round(duration*hz);i++) {
            double t=(double)i/hz,mid=t+.5/hz;
            // Identical physical displacement at every render frequency;
            // independently integrating acceleration here would vary both the
            // input trajectory and the controller under test.
            float speed=(float)((Distance(t+1.0/hz)-Distance(t))*hz);
            float turn=mid>=4&&mid<6.2?.5f:0;
            g.Step(dt,speed,turn,mid>=.5&&mid<7);
        }
        return g;
    }
    static double Distance(double t) {
        if(t<1)return 0;
        if(t<4){double u=t-1;return .45*u*u;}
        if(t<5.2){double u=t-4;return 4.05+2.7*u-1.125*u*u;}
        if(t<6.2){double u=t-5.2;return 5.67-.45*u*u;}
        if(t<7){double u=t-6.2;return 5.22-.9*u+.5625*u*u;}
        return 4.86;
    }
    static void GaitChecks() {
        float velocity=0;
        for(int i=0;i<50;i++)velocity=TransformerGait.AdvanceSpeed(velocity,1,.02f,true);
        Check(velocity>.85f&&velocity<.95f,"heavy machine builds speed gradually during its first second");
        for(int i=0;i<150;i++)velocity=TransformerGait.AdvanceSpeed(velocity,1,.02f,true);
        Check(velocity==TransformerGait.MaxSpeed&&velocity>=2.7f,"acceleration reaches purposeful full-size cruise");
        float before=velocity;velocity=TransformerGait.AdvanceSpeed(velocity,-1,.02f,true);
        Check(velocity>0&&velocity<before,"reversal brakes before changing travel direction");
        for(int i=0;i<90;i++)velocity=TransformerGait.AdvanceSpeed(velocity,0,.02f,false);
        Check(velocity==0,"release and transformation requests brake to exact rest");
        var gait=new TransformerGait();
        for(int i=0;i<30;i++)gait.Step(1f/60,0,0,true);
        Check(gait.Ready&&gait.Amount==0,"armor deploys before the first step");
        float minimum=0,maximum=0,maxArm=0,maxLean=0,maxSway=0;double correlation=0;
        int supports=0,flights=0;float previousFoot=0;bool previousGrounded=false;
        for(int i=0;i<600;i++) {
            gait.Step(1f/60,TransformerGait.MaxSpeed,0,true);
            gait.Foot(false,out float forward,out float lift,out float swing);
            if(i>120) {
                minimum=Math.Min(minimum,forward);maximum=Math.Max(maximum,forward);
                maxArm=Math.Max(maxArm,Math.Abs(swing));correlation+=forward*swing;
                maxLean=Math.Max(maxLean,gait.TorsoLean);maxSway=Math.Max(maxSway,Math.Abs(gait.PelvisSide));
                if(gait.FootGrounded(false)&&previousGrounded) {
                    if(Math.Abs(forward-previousFoot+TransformerGait.MaxSpeed/60)>.0001f)throw new Exception("Planted foot slides during straight travel");
                    supports++;
                }
                if(!gait.FootGrounded(false)&&lift>.08f)flights++;
            }
            previousFoot=forward;previousGrounded=gait.FootGrounded(false);
            Bounded(gait);
        }
        Console.WriteLine("CRUISE support="+supports+" lifted="+flights+" footRange="+(maximum-minimum)+" arm="+maxArm);
        Check(supports>150&&flights>60,"long support and clear swing phases give a heavy grounded walk");
        Check(maximum-minimum>1.15f,"cruise has a full humanoid stride rather than tiny shuffling steps");
        Check(maxArm>12&&correlation>100,"same-side arm travels opposite the foot with substantial counter-swing");
        Check(maxLean>4&&maxSway>.025f,"cruise visibly leans forward and transfers weight between supports");
        var saved=Pose(gait);
        gait.Step(0,0,0,false);gait.Step(float.NaN,0,0,false);gait.Step(float.PositiveInfinity,0,0,false);
        gait.Step(.02f,float.NaN,0,true);gait.Step(.02f,0,float.PositiveInfinity,true);
        Check(Pose(gait).SequenceEqual(saved),"invalid time and invalid motion leave the complete pose unchanged");
        // During turns compare the actual planted ankle in the world frame,
        // including both transverse and forward compensation.
        gait.Reset();for(int i=0;i<30;i++)gait.Step(1f/60,0,0,true);
        double x=0,z=0,yaw=0;double[] previousX=new double[2],previousZ=new double[2];bool[] planted=new bool[2];int turnSupports=0;
        const float dt=1f/120;const float turn=.7f,speed=1.2f;
        for(int i=0;i<1440;i++) {
            x+=Math.Sin(yaw)*speed*dt;z+=Math.Cos(yaw)*speed*dt;
            yaw+=turn*TransformerGait.TurnDegrees*Math.PI/180*dt;
            gait.Step(dt,speed,turn,true);
            for(int side=0;side<2;side++) {
                bool right=side==1;gait.Foot(right,out float forward,out _,out _);
                double localX=(right?-.88:.88)+gait.FootSide(right),localZ=-.02+forward;
                double wx=x+Math.Cos(yaw)*localX+Math.Sin(yaw)*localZ,wz=z-Math.Sin(yaw)*localX+Math.Cos(yaw)*localZ;
                bool grounded=gait.FootGrounded(right);
                if(i>120&&grounded&&planted[side]) {
                    if(Math.Sqrt(Math.Pow(wx-previousX[side],2)+Math.Pow(wz-previousZ[side],2))>.0002)throw new Exception("Planted foot slides through a curve");
                    turnSupports++;
                }
                previousX[side]=wx;previousZ[side]=wz;planted[side]=grounded;
            }
        }
        Check(turnSupports>700,"curved travel preserves world-space support instead of rotating feet through the floor");
        // Full transitions include braking, zero-speed pivot, opposite pivot,
        // reverse and interrupted restart; cap measured per-frame pose motion.
        gait.Reset();float commanded=0,previousLean=0,maxFootDelta=0,maxArmDelta=0,maxLeanDelta=0;float[] old=null;
        for(int i=0;i<1800;i++) {
            float t=i/60f,input=t<6?1:t<9?0:t<12?-1:t<16?1:0;
            float steering=t>=6&&t<9?1:t>=12&&t<16?-.7f:0;
            commanded=TransformerGait.AdvanceSpeed(commanded,input,1f/60,gait.Ready);
            gait.Step(1f/60,commanded,steering,input!=0||steering!=0);Bounded(gait);
            var pose=Pose(gait);
            if(old!=null)foreach(int start in new[]{11,18}) {
                maxFootDelta=Math.Max(maxFootDelta,Math.Abs(pose[start]-old[start]));
                maxFootDelta=Math.Max(maxFootDelta,Math.Abs(pose[start+1]-old[start+1]));
                maxArmDelta=Math.Max(maxArmDelta,Math.Abs(pose[start+2]-old[start+2]));
            }
            maxLeanDelta=Math.Max(maxLeanDelta,Math.Abs(gait.TorsoLean-previousLean));previousLean=gait.TorsoLean;old=pose;
        }
        Console.WriteLine("TRANSIENT MAX foot="+maxFootDelta+" arm="+maxArmDelta+" lean="+maxLeanDelta);
        Check(maxFootDelta<.16f&&maxArmDelta<=3.001f&&maxLeanDelta<1,"starts, reversals, pivots and settling avoid visible pose snaps");
        Check(gait.Settled&&gait.Phase==0&&Pose(gait).All(v=>v==0),"stop returns every pose channel to exact neutral before retraction");
        var p30=Pose(Rehearse(30));var p60=Pose(Rehearse(60));var p120=Pose(Rehearse(120));
        Check(p30.Zip(p60,(a,b)=>Math.Abs(a-b)).Max()<.06f&&p60.Zip(p120,(a,b)=>Math.Abs(a-b)).Max()<.06f,"30/60/120Hz transition runs converge to the same settled pose");
        foreach(float time in new[]{3.5f,4.8f,5.5f,6.5f}) {
            var coarse=Pose(Rehearse(30,time));var fine=Pose(Rehearse(120,time));
            foreach(int channel in new[]{11,12,14,18,19,21})
                if(Math.Abs(coarse[channel]-fine[channel])>.12f)throw new Exception("Moving timestep mismatch at "+time+" channel "+channel+" 30Hz="+coarse[channel]+" 120Hz="+fine[channel]);
            if(Math.Abs(coarse[8]-fine[8])>1.5f)throw new Exception("Timestep changes torso lean");
        }
        Check(true,"moving and reversing foot trajectories remain consistent across30/120Hz render rates");
        gait.Step(.1f,0,0,true);gait.Reset();
        Check(gait.Settled&&!gait.Ready&&Pose(gait).All(v=>v==0),"emergency reset clears complete deployment and posture");
    }
    static void Bounded(TransformerGait gait) {
        if(Pose(gait).Any(v=>float.IsNaN(v)||float.IsInfinity(v)))throw new Exception("Nonfinite pose output");
        if(gait.Phase<0||gait.Phase>=1||gait.Amount<0||gait.Amount>1||gait.Release<0||gait.Release>1)throw new Exception("Invalid gait state");
        foreach(bool right in new[]{false,true}) {
            gait.Foot(right,out float forward,out float lift,out float swing);
            if(Math.Abs(forward)>TransformerGait.Stride+.0001||lift<0||lift>TransformerGait.Lift+.0001||Math.Abs(swing)>TransformerGait.ArmSwing+.0001)throw new Exception("Foot/arm escaped bounded controller domain");
            if(Math.Abs(gait.FootSide(right))>.301f||Math.Abs(gait.FootYaw(right))>45.01f||gait.Elbow(right)<0||gait.Elbow(right)>17.01f)throw new Exception("Articulation escaped bounded controller domain");
            if(gait.FootGrounded(right)&&lift!=0)throw new Exception("Grounded foot is floating");
        }
    }
    static double CheckActualTrace(string path) {
        var poses=JArray.Parse(File.ReadAllText(path));
        double[] previousX=new double[2],previousZ=new double[2];bool[] planted=new bool[2];
        double maximum=0;string worst="";int checkedSupports=0;
        foreach(var p in poses) {
            double yaw=(double)p["yaw"]*Math.PI/180;
            for(int side=0;side<2;side++) {
                var foot=p[side==0?"leftFoot":"rightFoot"];
                double localX=(side==0?.88:-.88)+(double)foot["side"],localZ=-.02+(double)foot["forward"];
                double wx=(double)p["x"]+Math.Cos(yaw)*localX+Math.Sin(yaw)*localZ;
                double wz=(double)p["z"]-Math.Sin(yaw)*localX+Math.Cos(yaw)*localZ;
                bool grounded=(bool)foot["grounded"];
                if(grounded&&planted[side]) {
                    double error=Math.Sqrt(Math.Pow(wx-previousX[side],2)+Math.Pow(wz-previousZ[side],2));
                    if(error>maximum){maximum=error;worst=p["scenario"]+"/"+p["time"]+"/"+(side==0?"L":"R");}
                    checkedSupports++;
                }
                planted[side]=grounded;previousX[side]=wx;previousZ[side]=wz;
            }
        }
        Console.WriteLine("ACTUAL TRACE planted maximum="+maximum+"m at "+worst);
        Check(checkedSupports>1500&&maximum<.004,"actual50Hzphysics/60Hzrender starts, curves, pivots and reversals preserve planted supports within4mm perframe");
        return maximum;
    }
    static void SaveBehaviorProof(string assetDirectory,double plantError) {
        string root=Path.GetFullPath(Path.Combine(assetDirectory,"..",".."));
        var sources=new System.Collections.Generic.Dictionary<string,string>();
        foreach(string path in new[]{"src/TransformerGait.cs","src/TransformerVisual.cs","src/VehicleTransformer.cs","tests/transformer/Program.cs","tests/transformer/GaitPreview.cs"})
            sources.Add(path,TransformerAsset.Hash(Path.Combine(root,path)));
        var proof=new {passed=true,gaitVersion=TransformerGait.Version,checks=checks,maximumPlantedError=plantError,
            traceSha256=TransformerAsset.Hash(Path.Combine(AppContext.BaseDirectory,"runtime-gait-preview.json")),
            domainSha256=TransformerAsset.Hash(Path.Combine(AppContext.BaseDirectory,"runtime-gait-domain.json")),sourceHashes=sources};
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"runtime-gait-behavior.json"),JsonConvert.SerializeObject(proof,Formatting.Indented));
    }
    static void Main(string[] args) {
        string behaviorProof=Path.Combine(AppContext.BaseDirectory,"runtime-gait-behavior.json");
        if(File.Exists(behaviorProof))File.Delete(behaviorProof);
        if(args.Length==1&&args[0]=="--trace-only") {
            GaitPreview.Save(Path.Combine(AppContext.BaseDirectory,"runtime-gait-preview.json"));
            Console.WriteLine("Diagnostic trace exported; behavior assertions were not run.");return;
        }
        GaitChecks();
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
        double plantError=CheckActualTrace(Path.Combine(AppContext.BaseDirectory,"runtime-gait-preview.json"));
        SaveBehaviorProof(args[0],plantError);
        Console.WriteLine(checks+" transformer checks passed; Unity physics, skinning and live Player are NOT executed here.");
    }
}
