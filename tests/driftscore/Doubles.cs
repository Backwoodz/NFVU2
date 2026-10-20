using System;
using System.Collections.Generic;
namespace UnityEngine {
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 forward=>new Vector3(0,0,1);
    }
    public struct Quaternion {
        readonly float fx,fz;
        public Quaternion(float x,float z){fx=x;fz=z;}
        public static Vector3 operator*(Quaternion q,Vector3 v)=>new Vector3(q.fx,0,q.fz);
    }
    public class Rigidbody {public Vector3 position,linearVelocity;public Quaternion rotation;}
    public class WheelCollider {public bool isGrounded=true;}
    public static class Mathf {public static float Abs(float n)=>Math.Abs(n);}
    public static class Time {public static float timeScale=1,deltaTime=.05f,unscaledTime;}
    public static class Application {public static bool isFocused=true;}
}
public class Game {public static Game instance;public bool shutdown;public bool IsShuttingDown()=>shutdown;}
public class ZNet {public static ZNet instance;public long id=100;public long GetWorldUID()=>id;}
public class ZDOMan {public static ZDOMan instance;}
public class ZRoutedRpc {public static ZRoutedRpc instance;}
public class Player {
    public static Player m_localPlayer;
    public bool dead,teleport;
    public bool IsDead()=>dead;
    public bool IsTeleporting()=>teleport;
}
public class ZoneSystem {
    public static ZoneSystem instance;
    public readonly Dictionary<string,string> keys=new Dictionary<string,string>();
    public int writes;
    public bool fail;
    public bool GetGlobalKey(string key,out string value)=>keys.TryGetValue(key,out value);
    public void SetGlobalKey(string line) {
        if(fail)throw new InvalidOperationException("fixture unavailable");
        int split=line.IndexOf(' ');keys[line.Substring(0,split)]=line.Substring(split+1);writes++;
    }
}
namespace Rachel350Z {
    internal static class DriftWorldRules {public static bool Current=true;public const float GroundHeight=32;}
    internal class DriftVehicleAdapter {public bool Grounded(int i)=>true;}
    internal class VehicleRuntime {public DriftVehicleAdapter driftPhysics;}
    public partial class Plugin {
        bool driving=true,IsHummer,Flying,Transforming,EffectiveDrift=true,DriverInputReady=true,audioBlocked,menuOpen,multiplayer,ui;
        object car=new object();
        UnityEngine.Rigidbody body=new UnityEngine.Rigidbody();
        UnityEngine.WheelCollider[] wheels={new UnityEngine.WheelCollider(),new UnityEngine.WheelCollider(),new UnityEngine.WheelCollider(),new UnityEngine.WheelCollider()};
        VehicleRuntime current=new VehicleRuntime();
        bool DriftPhysicsActive=>current.driftPhysics!=null;
        bool Multiplayer()=>multiplayer;
        bool UIVisible()=>ui;
        readonly Log Logger=new Log();
        class Log {public int warnings;public void LogWarning(string message){warnings++;}}
        internal DriftLapScore TestScore=>driftLapScore;
        public bool TestVisible=>DriftCircuitScoreVisible;
        public int TestWarnings=>Logger.warnings;
        public void TestClear()=>ClearDriftCircuitScore();
        internal void TestTick(DriftLapSample s) {
            body.position=new UnityEngine.Vector3(s.X,32.8f,s.Z);
            body.linearVelocity=new UnityEngine.Vector3(s.VelocityX,0,s.VelocityZ);
            body.rotation=new UnityEngine.Quaternion(s.ForwardX,s.ForwardZ);
            UnityEngine.Time.deltaTime=s.DeltaTime;UnityEngine.Time.unscaledTime+=.05f;
            foreach(var wheel in wheels)wheel.isGrounded=s.Grounded;
            TickDriftCircuitScore();
        }
        public void TestBlock(bool value){ui=value;}
        public void TestDriving(bool value){driving=value;}
        public void TestHummer(bool value){IsHummer=value;}
        public void TestFlying(bool value){Flying=value;}
        public void TestTransformer(bool value){Transforming=value;}
        public void TestMultiplayer(bool value){multiplayer=value;}
        public void TestNextVehicle(){current=new VehicleRuntime();}
        public void TestDriftAdapter(){current.driftPhysics=new DriftVehicleAdapter();}
    }
}
