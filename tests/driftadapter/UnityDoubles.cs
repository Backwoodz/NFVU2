// Own adapter boundary checks, not a replacement for Unity's physics runtime.
using System;
using System.Collections.Generic;
public sealed class Character { }
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 zero=>new Vector3();
    public static Vector3 up=>new Vector3(0,1,0);
    public static Vector3 forward=>new Vector3(0,0,1);
    public float sqrMagnitude=>x*x+y*y+z*z;
    public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
    public Vector3 normalized=>magnitude>1e-8f?this/magnitude:zero;
    public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    public static Vector3 operator -(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);
    public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
    public static Vector3 operator /(Vector3 a,float f)=>a*(1/f);
    public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
    public static Vector3 ProjectOnPlane(Vector3 v,Vector3 n)=>v-n*(Dot(v,n)/Math.Max(1e-20f,n.sqrMagnitude));
}
public struct Quaternion {
    public float x,y,z,w;
    public static Quaternion identity=>new Quaternion{w=1};
    public static Quaternion Inverse(Quaternion q){float n=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;return new Quaternion{x=-q.x/n,y=-q.y/n,z=-q.z/n,w=q.w/n};}
    public static Quaternion operator *(Quaternion a,Quaternion b)=>new Quaternion {
        x=a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,y=a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,
        z=a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,w=a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z};
    public static Vector3 operator *(Quaternion q,Vector3 v){var r=q*new Quaternion{x=v.x,y=v.y,z=v.z}*Inverse(q);return new Vector3(r.x,r.y,r.z);}
    public static Quaternion AngleAxis(float degrees,Vector3 axis){var n=axis.normalized;float h=degrees*Mathf.Deg2Rad*.5f,s=(float)Math.Sin(h);return new Quaternion{x=n.x*s,y=n.y*s,z=n.z*s,w=(float)Math.Cos(h)};}
    public static Quaternion Euler(float x,float y,float z)=>AngleAxis(y,Vector3.up)*AngleAxis(x,new Vector3(1,0,0))*AngleAxis(z,new Vector3(0,0,1));
}
public sealed class Transform {
    public Vector3 position;
    public Quaternion rotation=Quaternion.identity;
    public Transform parent;
    public Vector3 forward=>rotation*Vector3.forward;
    public Vector3 TransformPoint(Vector3 point)=>position+rotation*point;
    public Vector3 InverseTransformPoint(Vector3 point)=>Quaternion.Inverse(rotation)*(point-position);
    public bool IsChildOf(Transform root)=>this==root||parent!=null&&parent.IsChildOf(root);
}
public sealed class Rigidbody {
    public float mass=1450,linearDamping=.08f,angularDamping=.8f,maxAngularVelocity=7;
    public Vector3 centerOfMass=new Vector3(0,.05f,0),linearVelocity,angularVelocity;
    public readonly Transform transform=new Transform();
    Vector3 physicalPosition;Quaternion physicalRotation=Quaternion.identity;
    public Vector3 position {get=>physicalPosition;set{physicalPosition=value;transform.position=value;}}
    public Quaternion rotation {get=>physicalRotation;set{physicalRotation=value;transform.rotation=value;}}
    public void SetPhysicsPose(Vector3 p,Quaternion q){physicalPosition=p;physicalRotation=q;}
    public RigidbodyInterpolation interpolation;
    public bool isKinematic,useGravity=true;
    public int forceCalls,directCalls,pointCalls,wakeCalls;
    public Vector3 impulse,angularImpulse,acceleration;
    public readonly List<Vector3> directImpulses=new List<Vector3>();
    public void WakeUp(){wakeCalls++;}
    public Vector3 GetPointVelocity(Vector3 point)=>linearVelocity+Vector3.Cross(angularVelocity,point-position-rotation*centerOfMass);
    public void AddForce(Vector3 value,ForceMode mode){forceCalls++;directCalls++;if(mode==ForceMode.Impulse){impulse+=value;directImpulses.Add(value);}else if(mode==ForceMode.Acceleration)acceleration+=value;else throw new Exception("Unsupported force mode");}
    public void AddForceAtPosition(Vector3 value,Vector3 point,ForceMode mode){if(mode!=ForceMode.Impulse)throw new Exception("Contact force must be an impulse");forceCalls++;pointCalls++;impulse+=value;angularImpulse+=Vector3.Cross(point-position-rotation*centerOfMass,value);}
}
public struct JointSpring {public float spring,damper;}
public struct WheelHit { }
public sealed class WheelCollider {
    public float radius=.34f,suspensionDistance=.24f,motorTorque,brakeTorque;
    public JointSpring suspensionSpring=new JointSpring{spring=48000,damper=6000};
    public readonly Transform transform=new Transform();
    public bool enabled=true;
    public int groundSamples;
    public bool GetGroundHit(out WheelHit hit){groundSamples++;hit=new WheelHit();return false;}
}
public sealed class Collider {
    public readonly Transform transform=new Transform();
    public Rigidbody attachedRigidbody;
    public bool character;
    public T GetComponentInParent<T>() where T:class=>character?new Character() as T:null;
}
public struct RaycastHit {public Collider collider;public Vector3 point,normal;public float distance;}
public enum ForceMode {Impulse,Acceleration}
public enum RigidbodyInterpolation {None,Interpolate,Extrapolate}
public enum QueryTriggerInteraction {Ignore}
public static class Physics {
    public const int DefaultRaycastLayers=-5;
    public static Vector3 gravity=new Vector3(0,-20,0);
    public static readonly Collider Floor=new Collider();
    public static Func<Vector3,Vector3,float,RaycastHit[]> Override;
    public static int queries;
    public static RaycastHit[] RaycastAll(Vector3 origin,Vector3 direction,float distance,int layers,QueryTriggerInteraction triggers){
        queries++;if(layers!=DefaultRaycastLayers||triggers!=QueryTriggerInteraction.Ignore)throw new Exception("Wrong contact query filter");
        return Override==null?new RaycastHit[0]:Override(origin,direction,distance);
    }
}
public static class Time {public static float fixedDeltaTime=.02f;public static double timeAsDouble,fixedTimeAsDouble;}
public static class Mathf {
    public const float PI=(float)Math.PI,Deg2Rad=PI/180,Rad2Deg=180/PI;
    public static float Abs(float v)=>Math.Abs(v);
    public static float Clamp(float v,float lo,float hi)=>Math.Max(lo,Math.Min(hi,v));
    public static float Clamp01(float v)=>Clamp(v,0,1);
}
}
