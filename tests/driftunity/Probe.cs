// Explicit private diagnostic, compiled separately from the shipped plugin.
// Runs real Unity/PhysX in Valheim's menu without loading or saving a world.
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using Rachel350Z;
[BepInPlugin("local.nfvu2.private.driftprobe","NFVU2 drift physics probe","0.0.1")]
public sealed class DriftUnityProbe : BaseUnityPlugin {
    int checks,failures;
    StreamWriter results;
    Rigidbody renderBody;
    DriftVehicleAdapter renderAdapter;
    int renderSamples,betweenTickSamples,oldFrameRate,oldVsync;
    float maxHubError,maxSpinError;
    double previousRenderTime,previousFixedTime;
    Vector3 previousRenderAxis;
    string renderError;
    void Check(bool ok,string message) {checks++;if(!ok)failures++;results.WriteLine((ok?"PASS ":"FAIL ")+message);results.Flush();}
    static float Beta(Rigidbody body) {var v=Quaternion.Inverse(body.rotation)*body.linearVelocity;return Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg;}
    IEnumerator Start() {
        var args=Environment.GetCommandLineArgs();
        if(!args.Contains("-nfvu2-drift-probe"))yield break;
        yield return null;
        string output=args.Single(a=>a.StartsWith("-nfvu2-probe-output=")).Substring(20);
        try {
            using(results=new StreamWriter(Path.Combine(output,"checks.txt")))Run(output);
        } catch(Exception error){File.WriteAllText(Path.Combine(output,"failed.txt"),error.ToString());Logger.LogError(error);}
        if(!File.Exists(Path.Combine(output,"failed.txt"))) {
            try {BeginRenderCheck();}catch(Exception error){renderError=error.ToString();}
            float deadline=Time.realtimeSinceStartup+4;
            while(renderError==null&&renderSamples<90&&Time.realtimeSinceStartup<deadline)yield return null;
            try {
                using(results=new StreamWriter(Path.Combine(output,"checks.txt"),true)) {
                    Check(renderError==null&&renderSamples>=60,"render probe receives real frames without a simulation error: "+renderError);
                    Check(maxHubError<.001f,"all four hubs follow the interpolated chassis during real render frames; max error="+maxHubError);
                    Check(betweenTickSamples>5&&maxSpinError<.25f,"wheel rotation advances smoothly between fixed ticks; samples="+betweenTickSamples+", max angle error="+maxSpinError);
                    results.WriteLine(checks+" real Unity checks, "+failures+" failures");
                }
            } finally {
                renderAdapter?.Dispose();renderAdapter=null;
                if(renderBody!=null)DestroyBody(renderBody);renderBody=null;
                Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVsync;
            }
            File.WriteAllText(Path.Combine(output,"completed.txt"),failures==0?"PASS":"FAIL");
        }
        Application.Quit();
    }
    void BeginRenderCheck() {
        oldFrameRate=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;
        Application.targetFrameRate=144;QualitySettings.vSyncCount=0;
        renderBody=Body(out var wheels);renderBody.position=new Vector3(0,1100,0);
        // The initial menu scene is replaced while these render frames run.
        DontDestroyOnLoad(renderBody.gameObject);
        renderBody.rotation=Quaternion.Euler(0,35,0);renderBody.linearVelocity=renderBody.rotation*Vector3.forward*20;
        Physics.SyncTransforms();renderAdapter=DriftVehicleAdapter.Create(renderBody,wheels,message=>renderError=message);
    }
    void FixedUpdate() {
        if(renderAdapter==null||renderError!=null)return;
        try {if(!renderAdapter.Step(true,0,0,0)&&renderError==null)renderError="render fixture physics step failed";}
        catch(Exception error){renderError=error.ToString();}
    }
    void LateUpdate() {
        if(renderAdapter==null||renderError!=null)return;
        try {
            for(int i=0;i<4;i++) {
                renderAdapter.TryGetWheelPose(i,out var point,out var rotation);
                var local=renderBody.transform.InverseTransformPoint(point);
                maxHubError=Mathf.Max(maxHubError,Math.Abs(local.x-(i%2==0?-.8f:.8f)),Math.Abs(local.z-(i<2?1.3f:-1.35f)));
                if(i!=0)continue;
                var axis=Quaternion.Inverse(renderBody.transform.rotation)*rotation*Vector3.up;
                double now=Time.timeAsDouble,elapsed=now-previousRenderTime;
                if(renderSamples>1&&Time.fixedTimeAsDouble==previousFixedTime&&elapsed>0&&elapsed<.025) {
                    float angle=Mathf.Atan2(Vector3.Cross(previousRenderAxis,axis).x,Vector3.Dot(previousRenderAxis,axis))*Mathf.Rad2Deg;
                    float expected=renderAdapter.WheelRpm(0)*6*(float)elapsed;
                    maxSpinError=Mathf.Max(maxSpinError,Math.Abs(angle-expected));betweenTickSamples++;
                }
                previousRenderAxis=axis;previousRenderTime=now;previousFixedTime=Time.fixedTimeAsDouble;
            }
            renderSamples++;
        }catch(Exception error){renderError=error.ToString();}
    }
    static Rigidbody Body(out WheelCollider[] wheels) {
        // Same 350Z hull, wheel roots and handling setup as VehicleSpawning.
        var root=new GameObject("private-drift-test");root.SetActive(false);root.transform.position=new Vector3(0,1000.3f,0);
        var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.76f,0);box.size=new Vector3(1.62f,.8f,3.8f);
        box.sharedMaterial=new PhysicsMaterial("private-drift-chassis"){dynamicFriction=.08f,staticFriction=.08f,frictionCombine=PhysicsMaterialCombine.Minimum};
        var body=root.AddComponent<Rigidbody>();body.mass=1450;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        wheels=new WheelCollider[4];
        for(int i=0;i<4;i++) {
            var wheel=new GameObject("wheel"+i);wheel.transform.SetParent(root.transform,false);
            wheel.transform.localPosition=new Vector3(i%2==0?-.8f:.8f,.34f,i<2?1.3f:-1.35f);
            wheels[i]=wheel.AddComponent<WheelCollider>();wheels[i].radius=.34f;
        }
        new VehicleHandling(body,wheels,false).SetDriftMode();root.SetActive(true);Physics.SyncTransforms();return body;
    }
    static void DestroyBody(Rigidbody body) {
        var material=body.GetComponent<BoxCollider>().sharedMaterial;
        DestroyImmediate(body.gameObject);DestroyImmediate(material);Physics.SyncTransforms();
    }
    static void Tick(DriftVehicleAdapter adapter,bool driving,float gas,float steer,float brake=0,bool handbrake=false,bool burnout=false) {
        if(!adapter.Step(driving,gas,steer,brake,burnout,handbrake))throw new InvalidOperationException("Drift adapter step failed.");
        Physics.Simulate(Time.fixedDeltaTime);
    }
    void Run(string output) {
        var worldGravity=Physics.gravity;
        var oldSimulation=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
        var floor=new GameObject("private-drift-floor");floor.transform.position=new Vector3(0,999,0);
        floor.AddComponent<BoxCollider>().size=new Vector3(3000,2,3000);Physics.SyncTransforms();
        try {
            results.WriteLine("Unity="+Application.unityVersion+", gravity="+worldGravity+", fixedDeltaTime="+Time.fixedDeltaTime);
            var physicsAssembly=typeof(NFVU2.DriftPhysics.DriftPowertrain).Assembly;
            results.WriteLine("Physics assembly="+physicsAssembly.Location);
            if(Environment.GetCommandLineArgs().Contains("-nfvu2-installed-physics"))
                Check(Path.GetFullPath(physicsAssembly.Location)==Path.GetFullPath(Path.Combine(Paths.PluginPath,"Rachel350Z","NFVU2.DriftPhysics.dll")),"uses the installed separate NFVU2 physics library");
            Check(Math.Abs(worldGravity.y+20)<.01,"actual Valheim gravity is represented in this runtime check");
            var falling=Body(out var fallWheels);falling.position=new Vector3(0,1100,0);Physics.SyncTransforms();
            try {using(var adapter=DriftVehicleAdapter.Create(falling,fallWheels)) {
                for(int i=0;i<20;i++)Tick(adapter,true,0,0);
                Check(Math.Abs(falling.linearVelocity.y/(20*Time.fixedDeltaTime)+9.81f)<.1,"own vehicle falls at 9.81 m/s2 inside the actual -20 m/s2 Unity world");
            }}finally{DestroyBody(falling);}
            var parked=Body(out var parkWheels);
            try {using(var adapter=DriftVehicleAdapter.Create(parked,parkWheels)) {
                for(int i=0;i<500;i++)Tick(adapter,true,0,0);
                Check(parked.linearVelocity.magnitude<.15,"zero-pedal input stays stationary for ten seconds in real Unity physics");
                for(int i=0;i<150;i++)Tick(adapter,true,1,0,1,true);
                float revs=adapter.Rpm;
                Check(revs>3500&&Math.Abs(adapter.WheelRpm(2))<20,"shared keyboard handbrake retains rear wheels and allows stationary revving");
                for(int i=0;i<100;i++)Tick(adapter,true,0,0,1,true);
                Check(adapter.Rpm<revs-100,"throttle release under handbrake reduces physical RPM for the blow-off path");
            }}finally{DestroyBody(parked);}
            var straight=Body(out var straightWheels);
            try {using(var adapter=DriftVehicleAdapter.Create(straight,straightWheels)) {
                for(int i=0;i<200;i++)Tick(adapter,false,0,0);
                using(var samples=new StreamWriter(Path.Combine(output,"straight.csv"))) {
                    samples.WriteLine("seconds,speed,gear,rpm,rearRpm,forwardSlip,height");
                    for(int i=0;i<500;i++) { Tick(adapter,true,1,0); if(i%5==0)samples.WriteLine(string.Join(",",new[]{i*Time.fixedDeltaTime,straight.linearVelocity.magnitude,adapter.Gear,adapter.Rpm,adapter.WheelRpm(2),adapter.ForwardSlip(2),straight.position.y}.Select(v=>v.ToString(CultureInfo.InvariantCulture)))); }
                }
                Check(straight.linearVelocity.magnitude>28&&adapter.Gear>=3,"straight acceleration reaches higher gears without a drift input");
                Check(Math.Abs(Beta(straight))<1&&Quaternion.Angle(straight.rotation,Quaternion.identity)<5,"zero steering stays straight under sustained throttle");
            }}finally{DestroyBody(straight);}
            using(var summary=new StreamWriter(Path.Combine(output,"summary.csv"))) {
                summary.WriteLine("entryMps,direction,handbrake,peakBeta,driftSeconds,exitSpeed,recoveryBeta,recoverySpeed,maxSteer");
                foreach(float entry in new[]{12f,20f,28f})foreach(float direction in new[]{-1f,1f})foreach(bool handbrake in new[]{false,true}) {
                    var body=Body(out var wheels);
                    try {using(var adapter=DriftVehicleAdapter.Create(body,wheels)) {
                        for(int i=0;i<200;i++)Tick(adapter,false,0,0);
                        int accelerationTicks=0;
                        while(body.linearVelocity.magnitude<entry&&accelerationTicks++<1500)Tick(adapter,true,1,0);
                        Check(accelerationTicks<1500,"vehicle accelerates to entry speed "+entry);
                        float maximum=0,seconds=0,maxSteer=0;
                        int ticks=(int)Math.Round(3/Time.fixedDeltaTime);
                        for(int i=0;i<ticks;i++) {
                            bool pull=handbrake&&i*Time.fixedDeltaTime<.3f;
                            Tick(adapter,true,1,direction,pull?1:0,pull);
                            float beta=Math.Abs(Beta(body));maximum=Mathf.Max(maximum,beta);maxSteer=Mathf.Max(maxSteer,Mathf.Abs(adapter.SteeringAngle(0)));
                            if(beta>=15&&beta<85)seconds+=Time.fixedDeltaTime;
                        }
                        float exit=body.linearVelocity.magnitude;
                        string scenario="entry="+entry+", direction="+direction+", handbrake="+handbrake;
                        // A short handbrake initiation should work even at the
                        // higher entry speeds; power-only grip depends on speed.
                        if(handbrake||entry==12)Check(maximum>15&&seconds>.3f,"real Unity rear breakaway: "+scenario);
                        Check(maximum<65&&exit>8&&Enumerable.Range(0,4).All(adapter.Grounded),"drift retains speed/contact without a spin: "+scenario);
                        for(int i=0;i<200;i++)Tick(adapter,true,0,0);
                        Check(Math.Abs(Beta(body))<8&&body.linearVelocity.magnitude<exit,"release recovers grip: "+scenario);
                        summary.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3}",entry,direction,handbrake,maximum,seconds,exit,Beta(body),body.linearVelocity.magnitude,maxSteer));summary.Flush();
                    }}finally{DestroyBody(body);}
                }
            }
            using(var sustained=new StreamWriter(Path.Combine(output,"sustained.csv"))) {
                sustained.WriteLine("entryMps,input,direction,lateMeanBeta,lateDriftSeconds,driftGear2Seconds,driftGear3Seconds,exitSpeed,exitGear,recoveryBeta");
                foreach(float entry in new[]{12f,20f})foreach(float input in new[]{.5f,.75f,1f})foreach(float direction in new[]{-1f,1f}) {
                    var body=Body(out var wheels);
                    try {using(var adapter=DriftVehicleAdapter.Create(body,wheels)) {
                        for(int i=0;i<200;i++)Tick(adapter,false,0,0);
                        int accelerationTicks=0;
                        while(body.linearVelocity.magnitude<entry&&accelerationTicks++<1500)Tick(adapter,true,1,0);
                        Check(accelerationTicks<1500,"sustained entry speed reached: "+entry);
                        float lateSum=0,lateTime=0,second=0,third=0,peak=0;int lateCount=0;
                        int ticks=(int)Math.Round(8/Time.fixedDeltaTime);
                        for(int i=0;i<ticks;i++) {
                            bool pull=i*Time.fixedDeltaTime<.3f;
                            Tick(adapter,true,1,input*direction,pull?1:0,pull);
                            float beta=Math.Abs(Beta(body));peak=Mathf.Max(peak,beta);
                            bool drifting=beta>=15&&beta<=65&&body.linearVelocity.magnitude>8;
                            if(drifting&&adapter.Gear==2)second+=Time.fixedDeltaTime;
                            if(drifting&&adapter.Gear==3)third+=Time.fixedDeltaTime;
                            if(i*Time.fixedDeltaTime>=2){lateSum+=beta;lateCount++;if(drifting)lateTime+=Time.fixedDeltaTime;}
                        }
                        string scenario="entry="+entry+", input="+input*direction;
                        Check(lateTime>4&&lateSum/lateCount>=15,"drift continues for seconds after initiation at partial/full input: "+scenario);
                        Check(second+third>3,"sustained drift occurs above first gear: "+scenario);
                        Check(peak<65&&Enumerable.Range(0,4).All(adapter.Grounded),"sustained turn keeps tyre contact without a spin: "+scenario);
                        float exit=body.linearVelocity.magnitude;int exitGear=adapter.Gear;
                        for(int i=0;i<200;i++)Tick(adapter,true,0,0);
                        Check(Math.Abs(Beta(body))<8&&body.linearVelocity.magnitude<exit,"sustained drift recovers after release: "+scenario);
                        sustained.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8},{9:F3}",entry,input,direction,lateSum/lateCount,lateTime,second,third,exit,exitGear,Beta(body)));sustained.Flush();
                    }}finally{DestroyBody(body);}
                }
            }
            foreach(float direction in new[]{-1f,1f}) {
                var body=Body(out var wheels);
                try {using(var adapter=DriftVehicleAdapter.Create(body,wheels)) {
                    for(int i=0;i<200;i++)Tick(adapter,false,0,0);
                    int accelerationTicks=0;
                    while(body.linearVelocity.magnitude<8&&accelerationTicks++<1500)Tick(adapter,true,1,0);
                    float secondGearDrift=0,peak=0;
                    for(int i=0;i<150;i++) {
                        Tick(adapter,true,1,direction);
                        float beta=Math.Abs(Beta(body));peak=Mathf.Max(peak,beta);
                        if(adapter.Gear==2&&beta>15&&body.linearVelocity.magnitude>5)secondGearDrift+=Time.fixedDeltaTime;
                    }
                    results.WriteLine("Second-gear drift seconds="+secondGearDrift+", direction="+direction);
                    Check(secondGearDrift>.3f,"physical drift is available in second gear, direction="+direction);
                    for(int i=0;i<200;i++){Tick(adapter,true,1,-direction*.75f);peak=Mathf.Max(peak,Math.Abs(Beta(body)));}
                    Check(Beta(body)*direction>15,"opposite driver steering transitions into the other drift direction, initial="+direction);
                    Check(peak<65&&Enumerable.Range(0,4).All(adapter.Grounded),"drift direction change stays below a spin, initial="+direction);
                }}finally{DestroyBody(body);}
            }
            CheckBurnoutAndDonut(output);
            CheckDriftResponse(output);
            Check((Physics.gravity-worldGravity).sqrMagnitude==0,"all tests leave world gravity unchanged");
            results.WriteLine(checks+" real Unity checks, "+failures+" failures");
        }finally {DestroyImmediate(floor);Physics.simulationMode=oldSimulation;}
    }
    static void TickPedals(DriftVehicleAdapter adapter,Rigidbody body,float gas,float reverse,float steering=0,bool handbrake=false) {
        float speed=Vector3.Dot(body.linearVelocity,body.rotation*Vector3.forward);
        ControlRules.Pedals(gas,reverse,speed,out var throttle,out var brake);
        bool request=ControlRules.ResolveBurnout(gas,reverse,body.linearVelocity.magnitude,adapter.BurnoutActive,handbrake,ref throttle,ref brake);
        Tick(adapter,true,throttle,steering,brake,handbrake,request);
    }
    void CheckBurnoutAndDonut(string output) {
        using(var samples=new StreamWriter(Path.Combine(output,"burnout-donut.csv"))) {
            samples.WriteLine("scenario,seconds,speed,frontRpm,rearRpm,gear,rack,beta,active");
            foreach(bool afterDrive in new[]{false,true}) {
                var body=Body(out var wheels);
                try {using(var adapter=DriftVehicleAdapter.Create(body,wheels)) {
                    for(int i=0;i<200;i++)Tick(adapter,false,0,0);
                    if(afterDrive) {
                        int guard=0;while(body.linearVelocity.magnitude<16&&guard++<1200)Tick(adapter,true,1,0);
                        Check(adapter.Gear>=2,"burnout restart fixture reaches a higher gear before stopping");
                        for(int i=0;i<250;i++)TickPedals(adapter,body,1,1);
                        for(int i=0;i<150;i++)Tick(adapter,true,0,0,1);
                    }
                    var origin=body.position;float maximumSpeed=0,maximumFront=0,minimumLateRear=float.MaxValue;int active=0;
                    for(int i=0;i<200;i++) {
                        TickPedals(adapter,body,1,1);
                        maximumSpeed=Math.Max(maximumSpeed,body.linearVelocity.magnitude);
                        maximumFront=Math.Max(maximumFront,Math.Abs(adapter.WheelRpm(0)));
                        if(adapter.BurnoutActive)active++;
                        if(i>100)minimumLateRear=Math.Min(minimumLateRear,Math.Abs(adapter.WheelRpm(2)));
                        if(i%10==0)samples.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8}",afterDrive?"burnout-restart":"burnout-rest",i*Time.fixedDeltaTime,body.linearVelocity.magnitude,adapter.WheelRpm(0),adapter.WheelRpm(2),adapter.Gear,adapter.SteeringAngle(0),Beta(body),adapter.BurnoutActive));
                    }
                    float travel=Vector3.ProjectOnPlane(body.position-origin,Vector3.up).magnitude;
                    Check(active>180&&minimumLateRear>150,"dual-pedal input produces sustained physical rear-wheel burnout; afterDrive="+afterDrive+", rearMin="+minimumLateRear);
                    Check(maximumSpeed<1&&maximumFront<55&&travel<1.5f,"front tyres hold a stationary burnout; afterDrive="+afterDrive+", maxSpeed="+maximumSpeed+", maxFront="+maximumFront+", travel="+travel);
                    Check(adapter.Gear==1,"stationary burnout uses first gear after rest or a drive");
                    float spinning=Math.Abs(adapter.WheelRpm(2));
                    for(int i=0;i<10;i++)TickPedals(adapter,body,0,1);
                    Check(!adapter.BurnoutActive&&Math.Abs(adapter.WheelRpm(2))<spinning,"releasing gas exits burnout and slows the spinning rear tyres before reversing");
                    for(int i=0;i<150;i++)Tick(adapter,true,0,0,1);
                    Check(Math.Abs(adapter.WheelRpm(2))<25,"ordinary foot braking stops the released rear tyres");
                    int guard2=0;while(body.linearVelocity.magnitude<12&&guard2++<1200)Tick(adapter,true,1,0);
                    float moving=body.linearVelocity.magnitude;
                    for(int i=0;i<20;i++)TickPedals(adapter,body,1,1);
                    Check(!adapter.BurnoutActive&&body.linearVelocity.magnitude<moving-1,"dual pedals brake normally at road speed");
                }}finally{DestroyBody(body);}
            }
            foreach(float direction in new[]{-1f,1f}) {
                var body=Body(out var wheels);
                try {using(var adapter=DriftVehicleAdapter.Create(body,wheels)) {
                    for(int i=0;i<200;i++)Tick(adapter,false,0,0);
                    body.linearVelocity=new Vector3(-direction*3,0,2);body.angularVelocity=Vector3.up*(direction*.8f);
                    for(int i=0;i<10;i++) {
                        float speed=body.linearVelocity.magnitude;
                        Tick(adapter,true,1,direction);
                        samples.WriteLine(string.Format(CultureInfo.InvariantCulture,"donut-init,{0},{1},{2},{3},{4},{5},{6},{7}",i*Time.fixedDeltaTime,body.linearVelocity.magnitude,adapter.WheelRpm(0),adapter.WheelRpm(2),adapter.Gear,adapter.SteeringAngle(0),Beta(body),adapter.BurnoutActive));
                        if(speed<=6)Check(adapter.SteeringAngle(0)*direction>0,"low-speed spinning/sliding donut entry retains the driver's steering direction");
                    }
                    Check(adapter.SteeringAngle(0)*direction>30,"donut entry develops useful manual front steering while rear rotates");
                }}finally{DestroyBody(body);}
            }
        }
    }
    void CheckDriftResponse(string output) {
        using(var summary=new StreamWriter(Path.Combine(output,"response.csv"))) {
            summary.WriteLine("case,direction,meanBeta,meanRadius,zeroCrossSeconds,opposite15Seconds,exitSpeed,peakBeta");
            foreach(string manoeuvre in new[]{"long","tight","switch"})foreach(float direction in new[]{-1f,1f}) {
                var body=Body(out var wheels);
                try {using(var adapter=DriftVehicleAdapter.Create(body,wheels)) {
                    for(int i=0;i<200;i++)Tick(adapter,false,0,0);
                    int guard=0;float entry=manoeuvre=="tight"?12:18;
                    while(body.linearVelocity.magnitude<entry&&guard++<1500)Tick(adapter,true,1,0);
                    float mean=0,radii=0,peak=0,zero=-1,opposite=-1;int count=0;
                    if(manoeuvre!="tight")for(int i=0;i<(manoeuvre=="switch"?250:75);i++)Tick(adapter,true,1,direction*.75f);
                    int ticks=manoeuvre=="switch"?200:400;
                    float previousBeta=Beta(body)*Mathf.Deg2Rad;
                    for(int i=0;i<ticks;i++) {
                        float input=manoeuvre=="long"?.25f:manoeuvre=="tight"?1:-.75f;
                        float gas=manoeuvre=="tight"?.6f:1;
                        Tick(adapter,true,gas,input*direction);
                        float beta=Beta(body),time=(i+1)*Time.fixedDeltaTime;
                        peak=Mathf.Max(peak,Math.Abs(beta));
                        if(manoeuvre=="switch") {
                            if(zero<0&&beta*direction>0)zero=time;
                            if(opposite<0&&beta*direction>15)opposite=time;
                        }
                        float pathYaw=body.angularVelocity.y+(beta*Mathf.Deg2Rad-previousBeta)/Time.fixedDeltaTime;
                        previousBeta=beta*Mathf.Deg2Rad;
                        if(time>2) {mean+=Math.Abs(beta);radii+=body.linearVelocity.magnitude/Math.Max(.02f,Math.Abs(pathYaw));count++;}
                    }
                    string scenario=manoeuvre+", direction="+direction;
                    if(manoeuvre=="long") {
                        Check(mean/count>12&&radii/count>70&&radii/count<160,"small input sustains drift on a wide line: "+scenario);
                    }else if(manoeuvre=="tight") {
                        Check(mean/count>15&&radii/count<28,"moderated throttle can tighten the drift radius: "+scenario);
                    }else {
                        Check(zero>0&&zero<.8f&&opposite>0&&opposite<1.05f,"opposite steering promptly crosses into the new drift direction: "+scenario);
                    }
                    Check(peak<65&&body.linearVelocity.magnitude>8&&Enumerable.Range(0,4).All(adapter.Grounded),"line correction retains motion/contact without a spin: "+scenario);
                    summary.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3}",manoeuvre,direction,mean/count,radii/count,zero,opposite,body.linearVelocity.magnitude,peak));summary.Flush();
                }}finally{DestroyBody(body);}
            }
        }
    }
}
