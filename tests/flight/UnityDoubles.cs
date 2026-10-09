// Controlled physics observations, not a Unity collision/flight simulation.
using System;
using N=System.Numerics;
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 zero=>new Vector3();
    public static Vector3 up=>new Vector3(0,1,0);
    public static Vector3 down=>new Vector3(0,-1,0);
    public static Vector3 forward=>new Vector3(0,0,1);
    public float sqrMagnitude=>x*x+y*y+z*z;
    public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
    public Vector3 normalized=>magnitude>0?this*(1/magnitude):zero;
    public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    public static Vector3 operator -(Vector3 a,Vector3 b)=>a+b*-1;
    public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    public static Vector3 ProjectOnPlane(Vector3 a,Vector3 n)=>a-n*(a.x*n.x+a.y*n.y+a.z*n.z);
    public static Vector3 ClampMagnitude(Vector3 a,float limit)=>a.magnitude>limit?a.normalized*limit:a;
}
public struct Quaternion {
    public N.Quaternion q;
    public static Quaternion identity=>new Quaternion{q=N.Quaternion.Identity};
    public static Quaternion Euler(float x,float y,float z)=>new Quaternion{q=N.Quaternion.CreateFromYawPitchRoll(y*Mathf.Deg2Rad,x*Mathf.Deg2Rad,z*Mathf.Deg2Rad)};
    public Vector3 eulerAngles=>new Vector3(0,(float)Math.Atan2(2*(q.W*q.Y+q.X*q.Z),1-2*(q.Y*q.Y+q.X*q.X))/Mathf.Deg2Rad,0);
    public static Quaternion Inverse(Quaternion a)=>new Quaternion{q=N.Quaternion.Inverse(a.q)};
    public static Quaternion operator *(Quaternion a,Quaternion b)=>new Quaternion{q=a.q*b.q};
    public static Vector3 operator *(Quaternion a,Vector3 b){var v=N.Vector3.Transform(new N.Vector3(b.x,b.y,b.z),a.q);return new Vector3(v.X,v.Y,v.Z);}
    public void ToAngleAxis(out float angle,out Vector3 axis){angle=2*(float)Math.Acos(Mathf.Clamp(q.W,-1,1))/Mathf.Deg2Rad;axis=new Vector3(q.X,q.Y,q.Z).normalized;}
}
public class Transform {
    public Vector3 position,localPosition;
    public Quaternion rotation=Quaternion.identity;
    public Vector3 up=>rotation*Vector3.up;
    public Vector3 TransformPoint(Vector3 p)=>position+rotation*p;
    public Vector3 InverseTransformPoint(Vector3 p)=>Quaternion.Inverse(rotation)*(p-position);
    public bool IsChildOf(Transform t)=>this==t;
}
public enum ForceMode{Acceleration}
public class Rigidbody {
    public Transform transform=new Transform();
    public Vector3 position {get=>transform.position;set=>transform.position=value;}
    public Quaternion rotation {get=>transform.rotation;set=>transform.rotation=value;}
    public bool useGravity=true;
    public float linearDamping=.08f,angularDamping=.8f;
    public Vector3 linearVelocity,angularVelocity,force,torque;
    public void WakeUp(){}
    public void AddForce(Vector3 f,ForceMode mode){force+=f;}
    public void AddTorque(Vector3 t,ForceMode mode){torque+=t;}
    public void Integrate(){linearVelocity+=(force+(useGravity?Vector3.down*9.81f:Vector3.zero))*Time.fixedDeltaTime;position+=linearVelocity*Time.fixedDeltaTime;force=Vector3.zero;torque=Vector3.zero;}
}
public class WheelCollider {
    public Transform transform=new Transform();
    public bool enabled=true;
    public float motorTorque=100,brakeTorque=100;
    public void GetWorldPose(out Vector3 p,out Quaternion r){if(!enabled)throw new Exception("Read disabled wheel pose");p=transform.position;r=transform.rotation;}
}
public class Collider {
    public Transform transform=new Transform();
    public T GetComponentInParent<T>() where T:class=>null;
}
public struct RaycastHit {public Vector3 point,normal;public Collider collider;}
public enum QueryTriggerInteraction{Ignore}
public static class Physics {
    public static float GroundY=0;
    public static RaycastHit[] RaycastAll(Vector3 p,Vector3 direction,float length,int mask,QueryTriggerInteraction ignore)=>
        p.y<GroundY||p.y-GroundY>length?new RaycastHit[0]:new[]{new RaycastHit{point=new Vector3(p.x,GroundY,p.z),normal=Vector3.up,collider=new Collider()}};
}
public static class Time {public static float fixedDeltaTime=.02f;}
public static class Mathf {
    public const float Deg2Rad=(float)Math.PI/180;
    public static float Abs(float v)=>Math.Abs(v);
    public static float Max(float a,float b)=>Math.Max(a,b);
    public static float Clamp(float v,float low,float high)=>Math.Max(low,Math.Min(high,v));
    public static float Clamp01(float v)=>Clamp(v,0,1);
}
}
public class Player {}
