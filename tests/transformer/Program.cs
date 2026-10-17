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
        values.Add(g.RunBlend);return values.ToArray();
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
    // BEGIN_TERRAIN_PHASE_TESTS
    static void TerrainPhaseChecks() {
        var p=new TransformerTerrainPhase();p.Reset(false);
        Check(p.Stage==TransformerTerrainStage.Flat&&p.Alignment==1&&p.CanMove&&!p.HoldReady&&!p.CanRetract,"flat preparation preserves the existing upright locomotion path");
        p.Step(.02f,false,true,true,closed:false);
        Check(!p.CanRetract,"a nearly closed flat pose cannot retract without explicit closed evidence");
        p.Step(.02f,false,true,true,closed:true);
        Check(p.CanRetract&&!p.CanMove,"flat return waits for settled feet and complete armor closure");
        p.Reset(true);
        for(int i=0;i<30;i++)p.Step(.02f,false,true,false);
        Check(p.Stage==TransformerTerrainStage.Open&&p.Alignment==0&&p.HoldReady&&!p.CanMove&&!p.CanRetract,"terrain preparation holds the original support-parallel transform until fully open");
        p.Step(.02f,true,true,false);
        p.Step(.1f,true,true,false);
        Check(p.Stage==TransformerTerrainStage.Align&&Math.Abs(p.Alignment-.25f)<1e-6&&!p.CanMove&&p.HoldReady,"only the open stationary robot starts bounded upright alignment");
        float held=p.Alignment;var heldStage=p.Stage;
        foreach(float dt in new[]{.1f,0,-1,float.NaN,float.PositiveInfinity})p.Step(dt,true,true,true,blocked:true,closed:true);
        foreach(float dt in new[]{0f,-1,float.NaN,float.NegativeInfinity})p.Step(dt,true,true,true);
        Check(p.Alignment==held&&p.Stage==heldStage&&!p.ReturnStarted&&!p.CanMove,"streaming focus pauses and invalid time freeze terrain preparation and return latches");
        for(int i=0;i<3;i++)p.Step(.1f,true,true,false);
        Check(p.Stage==TransformerTerrainStage.Move&&p.Alignment==1&&p.CanMove&&p.HoldReady&&!p.CanRetract,"movement unlocks only after all .4 seconds of open-robot alignment");
        p.Step(.1f,true,false,true);
        Check(!p.ReturnStarted&&p.Stage==TransformerTerrainStage.Move,"an airborne or unsettled robot cannot start terrain return alignment");
        p.Step(.1f,true,true,true);
        Check(p.ReturnStarted&&p.Stage==TransformerTerrainStage.AlignBack&&Math.Abs(p.Alignment-.75f)<1e-6&&!p.CanMove&&p.HoldReady,"settled return aligns back while holding armor fully open");
        for(int i=0;i<3;i++)p.Step(.1f,true,true,false);
        Check(p.Stage==TransformerTerrainStage.Close&&p.Alignment==0&&!p.HoldReady&&!p.CanMove&&!p.CanRetract,"return request remains latched through alignment before armor may close");
        p.Step(.02f,false,true,false,closed:false);
        Check(!p.CanRetract,"not-ready does not mistake partially closed armor for a safe retraction endpoint");
        p.Step(.02f,false,false,false,closed:true);
        Check(!p.CanRetract,"closed armor still requires rested feet before original inverse transformation");
        p.Step(.02f,false,true,false,closed:true);
        Check(p.CanRetract,"original inverse transformation unlocks only on terrain-parallel fully closed settled pose");
        p.Reset(true);p.Step(.02f,true,true,false);p.Step(.1f,true,true,false);p.Step(.1f,true,true,false);
        float before=p.Alignment;p.Step(.02f,true,true,true);
        Check(p.ReturnStarted&&p.Stage==TransformerTerrainStage.AlignBack&&Math.Abs(p.Alignment-(before-.05f))<1e-6,"return during partial upright alignment reverses continuously without an orientation snap");
        p.Reset(false);p.ActivateUpright();
        Check(p.Stage==TransformerTerrainStage.Move&&p.Alignment==1&&p.HoldReady&&p.CanMove&&!p.ReturnStarted,"a walking robot meeting terrain stays upright and open without replaying preparation");
        p.Step(.1f,true,true,true);p.ActivateUpright();
        Check(p.Stage==TransformerTerrainStage.AlignBack&&!p.CanMove,"terrain activation cannot cancel a latched return");
        p.Reset(true);p.Step(.02f,true,true,false);p.Step(10,true,true,false);
        Check(Math.Abs(p.Alignment-.25f)<1e-6&&!p.CanMove,"a long stalled frame cannot skip preparation alignment");
        p.Reset(true);
        Check(!p.ReturnStarted&&p.Alignment==0&&p.Stage==TransformerTerrainStage.Open&&!p.CanRetract,"reset clears return and alignment history");
        p.Step(.02f,false,true,false);p.Step(.02f,false,true,true);
        Check(p.Stage==TransformerTerrainStage.Close&&p.Alignment==0&&!p.CanRetract,"canceling before opening keeps the original support-parallel pose and still waits for closure");
        float low=1,high=0;
        foreach(int hz in new[]{30,60,120}) {
            var q=new TransformerTerrainPhase();q.Reset(true);q.Step(1f/hz,true,true,false);
            for(int i=0;i<3*hz/10;i++)q.Step(1f/hz,true,true,false);
            low=Math.Min(low,q.Alignment);high=Math.Max(high,q.Alignment);
            if(q.CanMove||!q.HoldReady||q.Stage!=TransformerTerrainStage.Align)throw new Exception("premature terrain movement at "+hz+"Hz");
            for(int i=0;i<hz;i++)q.Step(1f/hz,true,true,false);
            if(!q.CanMove||q.Alignment!=1)throw new Exception("terrain preparation never completes at "+hz+"Hz");
        }
        Check(high-low<1e-5&&Math.Abs(low-.75f)<1e-5,"30 60 and 120Hz preparation share the same continuous alignment and endpoint");
    }
    // END_TERRAIN_PHASE_TESTS
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
            var coarse=Rehearse(30,time);var fine=Rehearse(120,time);
            Bounded(coarse);Bounded(fine);
            // Contact-triggered replacement may choose a different exact
            // foot phase. Rate, movement amount and posture must stay stable.
            if(Math.Abs(coarse.Cadence-fine.Cadence)>.08f||Math.Abs(coarse.Amount-fine.Amount)>.08f||Math.Abs(coarse.TorsoLean-fine.TorsoLean)>1.5f)
                throw new Exception("Walking cadence, amount or lean changes with render rate at "+time);
        }
        Check(true,"walking cadence, movement amount and lean remain consistent across30/120Hz render rates");
        gait.Step(.1f,0,0,true);gait.Reset();
        Check(gait.Settled&&!gait.Ready&&Pose(gait).All(v=>v==0),"emergency reset clears complete deployment and posture");
    }
    static void Bounded(TransformerGait gait) {
        if(Pose(gait).Any(v=>float.IsNaN(v)||float.IsInfinity(v)))throw new Exception("Nonfinite pose output");
        if(gait.Phase<0||gait.Phase>=1||gait.Amount<0||gait.Amount>1||gait.Release<0||gait.Release>1)throw new Exception("Invalid gait state");
        foreach(bool right in new[]{false,true}) {
            gait.Foot(right,out float forward,out float lift,out float swing);
            if(Math.Abs(forward)>TransformerGait.SprintStride+.0001||lift<0||lift>TransformerGait.SprintLift+.0001||Math.Abs(swing)>Math.Max(TransformerGait.ArmSwing,TransformerGait.SprintArmSwing)+.0001)throw new Exception("Foot/arm escaped bounded controller domain");
            if(Math.Abs(gait.FootSide(right))>.301f||Math.Abs(gait.FootYaw(right))>45.01f||gait.Elbow(right)<0||gait.Elbow(right)>70.01f)throw new Exception("Articulation escaped bounded controller domain");
            if(gait.FootGrounded(right)&&lift!=0)throw new Exception("Grounded foot is floating");
        }
    }
static void SprintChecks() {
        float velocity=0;
        for(int i=0;i<400;i++)velocity=TransformerGait.AdvanceSpeed(velocity,1,.02f,true,true);
        Check(velocity==TransformerGait.SprintSpeed&&velocity>TransformerGait.MaxSpeed*2,"Shift enables a substantially faster forward sprint");
        float before=velocity;velocity=TransformerGait.AdvanceSpeed(velocity,1,.02f,true,false);
        Check(velocity<before&&velocity>TransformerGait.MaxSpeed,"releasing Shift brakes gradually toward walk speed");
        for(int i=0;i<200;i++)velocity=TransformerGait.AdvanceSpeed(velocity,1,.02f,true,false);
        Check(velocity==TransformerGait.MaxSpeed,"holding forward after Shift release returns to walk speed");
        velocity=0;for(int i=0;i<200;i++)velocity=TransformerGait.AdvanceSpeed(velocity,-1,.02f,true,true);
        Check(velocity==-TransformerGait.ReverseSpeed&&TransformerGait.AdvanceSpeed(0,0,.02f,true,true)==0,"Shift does not turn reverse travel or a pivot into a forward sprint");
        var g=new TransformerGait();float speed=0,minimum=0,maximum=0,maxArm=0,maxLean=0,maxElbow=0,maxAir=0,air=0;
        float lowestPelvis=0,highestPelvis=float.NegativeInfinity,previousPelvis=0,maxPelvisStep=0,maxSteadyArmStep=0;
        float previousLeftArm=0,previousRightArm=0;double flightPelvis=0,supportPelvis=0;
        int flightSamples=0,leftSupports=0,rightSupports=0,supportSamples=0;
        for(int i=0;i<600;i++) {
            speed=TransformerGait.AdvanceSpeed(speed,1,1f/60,g.Ready,true);g.Step(1f/60,speed,0,true);Bounded(g);
            g.Foot(false,out float forward,out _,out float swing);
            g.Foot(true,out _,out _,out float rightSwing);
            if(i>360) {
                minimum=Math.Min(minimum,forward);maximum=Math.Max(maximum,forward);maxArm=Math.Max(maxArm,Math.Abs(swing));
                maxLean=Math.Max(maxLean,g.TorsoLean);maxElbow=Math.Max(maxElbow,g.Elbow(false));
                lowestPelvis=Math.Min(lowestPelvis,g.PelvisHeight);highestPelvis=Math.Max(highestPelvis,g.PelvisHeight);
                if(i>361) {
                    maxSteadyArmStep=Math.Max(maxSteadyArmStep,Math.Max(Math.Abs(swing-previousLeftArm),Math.Abs(rightSwing-previousRightArm)));
                    maxPelvisStep=Math.Max(maxPelvisStep,Math.Abs(g.PelvisHeight-previousPelvis));
                }
                if(g.FootGrounded(false))leftSupports++;if(g.FootGrounded(true))rightSupports++;
                if(g.Flying){flightSamples++;flightPelvis+=g.PelvisHeight;air+=1f/60;maxAir=Math.Max(maxAir,air);}
                else {air=0;supportPelvis+=g.PelvisHeight;supportSamples++;}
            }
            previousLeftArm=swing;previousRightArm=rightSwing;previousPelvis=g.PelvisHeight;
        }
        Console.WriteLine("SPRINT range="+(maximum-minimum)+" flight="+flightSamples+" maxAir="+maxAir+" arm="+maxArm+" lean="+maxLean+" elbow="+maxElbow);
        Check(g.RunBlend>.98f&&g.CurrentStride>TransformerGait.Stride&&maximum-minimum>1.7f,"sprint changes stride mechanics instead of speeding up the walking shuffle");
        Check(flightSamples>10&&leftSupports>30&&rightSupports>30&&maxAir>.12f&&maxAir<.4f,"heavy sprint has longer flight while retaining left and right load-bearing contacts");
        Check(maxArm>15&&maxElbow>40&&maxLean>9,"sprint moves bent arms and leans the machine into its acceleration");
        Check(maxArm<=TransformerGait.SprintArmSwing+.001f&&maxSteadyArmStep*60<=TransformerGait.SprintArmSpeed+.01f,"sprint keeps arm travel and angular speed restrained");
        Console.WriteLine("SPRINT heave="+(highestPelvis-lowestPelvis)+" maxStep="+maxPelvisStep+" flightHeight="+(flightPelvis/flightSamples)+" supportHeight="+(supportPelvis/supportSamples));
        Check(highestPelvis-lowestPelvis>.20f&&highestPelvis-lowestPelvis<.34f&&maxPelvisStep<.06f,
            "heavy sprint has a larger smooth center-of-mass rise and fall");
        Check(flightPelvis/flightSamples>supportPelvis/supportSamples+.035,
            "sprint center of mass rises into real push-off and flight rather than bobbing with the lifted knee");
        // Stop immediately during flight, as a swept hull collision can do.
        for(int i=0;i<180&&!g.Flying;i++)g.Step(1f/60,TransformerGait.SprintSpeed,0,true);
        Check(g.Flying,"collision-stop fixture starts during an actual airborne phase");
        float[] old=Pose(g);float maxFootDelta=0,maxArmDelta=0;
        for(int i=0;i<300;i++) {
            g.Step(1f/60,0,0,false);Bounded(g);var now=Pose(g);
            foreach(int start in new[]{11,18}) {
                maxFootDelta=Math.Max(maxFootDelta,Math.Abs(now[start]-old[start]));
                maxFootDelta=Math.Max(maxFootDelta,Math.Abs(now[start+1]-old[start+1]));
                maxArmDelta=Math.Max(maxArmDelta,Math.Abs(now[start+2]-old[start+2]));
            }
            old=now;
        }
        Console.WriteLine("SPRINT collision maximum foot="+maxFootDelta+" arm="+maxArmDelta);
        Check(g.Settled&&Pose(g).All(v=>v==0)&&maxFootDelta<.30f&&maxArmDelta<5,"an airborne sprint collision settles both feet and arms without teleporting the pose");
        foreach(float time in new[]{5.4f,6.8f,8.2f,9.8f}) {
            var coarse=RehearseSprint(30,time);var fine=RehearseSprint(120,time);
            Bounded(coarse);Bounded(fine);
            // Longer steps can change which foot initiates a support transfer.
            // Compare gait selection/rate and posture, not a prescribed phase
            // of one particular foot at one wall-clock instant.
            if(Math.Abs(coarse.Cadence-fine.Cadence)>.08f||Math.Abs(coarse.RunBlend-fine.RunBlend)>.08f||Math.Abs(coarse.TorsoLean-fine.TorsoLean)>2)
                throw new Exception("Sprint cadence, gait selection or lean changes with render rate at "+time);
        }
        Check(true,"sprint and walk-transition cadence, gait selection and lean remain consistent at30/120Hz");
        Check(RehearseSprint(30,17).Settled&&RehearseSprint(120,17).Settled,"all render rates settle after sprint, walk and reverse transitions");
    }
    static TransformerGait RehearseSprint(int hz,float duration) {
        var g=new TransformerGait();float dt=1f/hz;
        for(int i=0;i<(int)Math.Round(duration*hz);i++) {
            double t=(double)i/hz,mid=t+.5/hz;
            float speed=(float)((SprintDistance(t+1.0/hz)-SprintDistance(t))*hz);
            g.Step(dt,speed,mid>=5.5&&mid<8?.25f:0,mid>=.5&&mid<13);
        }
        return g;
    }
    static double SprintDistance(double t) {
        if(t<1)return 0;
        if(t<5){double u=t-1;return .8*u*u;}
        if(t<6)return 12.8+6.4*(t-5);
        if(t<7){double u=t-6;return 19.2+6.4*u-1.9*u*u;}
        if(t<9)return 23.7+2.6*(t-7);
        if(t<10){double u=t-9;return 28.9+2.6*u-1.3*u*u;}
        if(t<11)return 30.2;
        if(t<12){double u=t-11;return 30.2-.45*u*u;}
        if(t<13){double u=t-12;return 29.75-.9*u+.45*u*u;}
        return 29.3;
    }
    sealed class TraceEvidence {public double maximumPlant,maximumAirTime;public int flights,supports,airClearanceSamples;}
    static TraceEvidence CheckActualTrace(string path) {
        var poses=JArray.Parse(File.ReadAllText(path));
        double[] previousX=new double[2],previousZ=new double[2];bool[] planted=new bool[2];
        double maximum=0,airTime=0,lastTime=0;string worst="";int checkedSupports=0;var evidence=new TraceEvidence();
        foreach(var p in poses) {
            double time=(double)p["time"],blend=(double)p["runBlend"];
            bool flying=(bool)p["flying"],bothAir=!(bool)p["leftFoot"]["grounded"]&&!(bool)p["rightFoot"]["grounded"];
            if(flying!=bothAir)throw new Exception("Flight metadata does not match actual foot contacts at "+time);
            if(flying){airTime+=time-lastTime;evidence.maximumAirTime=Math.Max(evidence.maximumAirTime,airTime);}else airTime=0;
            if(blend>.95){if(flying){evidence.flights++;if(Math.Min((double)p["leftFoot"]["lift"],(double)p["rightFoot"]["lift"])>.01)evidence.airClearanceSamples++;}else evidence.supports++;}
            lastTime=time;
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
        Check(evidence.flights>10&&evidence.supports>30&&evidence.maximumAirTime<.4,"actual Shift transitions include brief flight, grounded load transfer and bounded airborne time");
        Check(evidence.airClearanceSamples>5,"sprint flight has both feet visibly above the floor rather than only airborne flags");
        evidence.maximumPlant=maximum;return evidence;
    }
    static void SaveBehaviorProof(string assetDirectory,TraceEvidence evidence) {
        string root=Path.GetFullPath(Path.Combine(assetDirectory,"..",".."));
        var sources=new System.Collections.Generic.Dictionary<string,string>();
        var missing=new System.Collections.Generic.List<string>();
        foreach(string path in new[]{"src/TransformerGait.cs","src/TransformerVisual.cs","src/VehicleTransformer.cs","src/TransformerPlacement.cs","src/TransformerClearance.cs","src/TransformerTerrainPhase.cs","src/Plugin.cs","src/VehicleControls.cs","src/VehicleTransformerControls.cs","tests/transformer/Program.cs","tests/transformer/GaitPreview.cs","tools/transformer_sprint_v4.json"})
            if(File.Exists(Path.Combine(root,path)))sources.Add(path,TransformerAsset.Hash(Path.Combine(root,path)));else {sources.Add(path,null);missing.Add(path);}
        var proof=new {passed=missing.Count==0,missingSources=missing,gaitVersion=TransformerGait.Version,checks=checks,sprintTested=true,maximumPlantedError=evidence.maximumPlant,
            maximumAirTime=evidence.maximumAirTime,sprintFlightSamples=evidence.flights,sprintSupportSamples=evidence.supports,sprintAirClearanceSamples=evidence.airClearanceSamples,
            traceSha256=TransformerAsset.Hash(Path.Combine(AppContext.BaseDirectory,GaitPreview.TraceName)),
            domainSha256=TransformerAsset.Hash(Path.Combine(AppContext.BaseDirectory,GaitPreview.DomainName)),sourceHashes=sources};
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,GaitPreview.BehaviorName),JsonConvert.SerializeObject(proof,Formatting.Indented));
        if(missing.Count>0)Console.WriteLine("Diagnostic only: behavior proof stays locked; missing source bindings "+string.Join(", ",missing));
    }
    static void Main(string[] args) {
        if(args.Length==1&&args[0]=="--gait-only") {GaitChecks();SprintChecks();return;}
        string behaviorProof=Path.Combine(AppContext.BaseDirectory,GaitPreview.BehaviorName);
        if(File.Exists(behaviorProof))File.Delete(behaviorProof);
        if(args.Length==1&&args[0]=="--trace-only") {
            GaitPreview.Save(Path.Combine(AppContext.BaseDirectory,GaitPreview.TraceName));
            Console.WriteLine("Diagnostic trace exported; behavior assertions were not run.");return;
        }
        GaitChecks();
        SprintChecks();
        TerrainPhaseChecks();
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
        var sprintDefinition=JObject.Parse(File.ReadAllText(Path.Combine(args[0],"..","..","tools","transformer_sprint_v4.json")));
        Check((int)sprintDefinition["version"]==TransformerGait.Version&&
            (float)sprintDefinition["sprintSpeed"]==TransformerGait.SprintSpeed&&
            (float)sprintDefinition["sprintStride"]==TransformerGait.SprintStride&&
            (float)sprintDefinition["sprintLift"]==TransformerGait.SprintLift&&
            (float)sprintDefinition["sprintArmSwing"]==TransformerGait.SprintArmSwing&&
            (float)sprintDefinition["sprintArmSpeed"]==TransformerGait.SprintArmSpeed&&
            (float)sprintDefinition["sprintPelvisBase"]==TransformerGait.SprintPelvisBase&&
            (float)sprintDefinition["sprintHeave"]==TransformerGait.SprintHeave&&
            (float)sprintDefinition["sprintAcceleration"]==TransformerGait.SprintAcceleration&&
            (float)sprintDefinition["sprintBraking"]==TransformerGait.SprintBraking&&
            (float)sprintDefinition["sprintSupportFraction"]==TransformerGait.SprintSupportFraction,"V4 definition and actual sprint constants agree");
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
        m.walkingReady=true;m.gaitVersion=3;save();Reject(()=>TransformerAsset.Load(tmp,args[1]),"old walking-only V3 approval cannot activate V4 sprint");m.walkingReady=ready;m.gaitVersion=gaitVersion;
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
        GaitPreview.Save(Path.Combine(AppContext.BaseDirectory,GaitPreview.TraceName));
        var evidence=CheckActualTrace(Path.Combine(AppContext.BaseDirectory,GaitPreview.TraceName));
        SaveBehaviorProof(args[0],evidence);
        Console.WriteLine(checks+" transformer checks passed; Unity physics, skinning and live Player are NOT executed here.");
    }
}
