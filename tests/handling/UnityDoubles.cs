// Controller tests only: no Unity physics is simulated by these doubles.
using System;
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;
    public float magnitude => (float)Math.Sqrt(x*x+y*y+z*z);
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    public static Vector3 operator -(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);
    public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
}
public class Transform {
    public Vector3 position,localPosition,up=new Vector3(0,1,0),forward=new Vector3(0,0,1);
    public Vector3 InverseTransformPoint(Vector3 point)=>point;
}
public class Rigidbody {
    public float mass=1450,linearDamping,angularDamping;
    public int solverIterations,solverVelocityIterations;
    public Vector3 centerOfMass,linearVelocity;
    public Transform transform=new Transform();
    public bool isKinematic,sleeping;
    public int wakeCalls;
    public void WakeUp(){sleeping=false;wakeCalls++;}
    public bool IsSleeping()=>sleeping;
    public void AddForceAtPosition(Vector3 force,Vector3 position){}
}
public struct JointSpring { public float spring,damper,targetPosition; }
public struct WheelFrictionCurve { public float extremumSlip,extremumValue,asymptoteSlip,asymptoteValue,stiffness; }
public struct WheelHit { public Vector3 point; public float forwardSlip; }
public class WheelCollider {
    public float mass,wheelDampingRate,forceAppPointDistance,suspensionDistance,radius=.34f,motorTorque,brakeTorque,steerAngle,rpm;
    public JointSpring suspensionSpring;
    public WheelFrictionCurve forwardFriction,sidewaysFriction;
    public Transform transform=new Transform();
    public bool contact;
    public float slip;
    public bool GetGroundHit(out WheelHit hit){hit=new WheelHit{forwardSlip=slip};return contact;}
    public void ConfigureVehicleSubsteps(float speedThreshold,int below,int above){}
}
public static class Time { public static float fixedDeltaTime=.02f; }
public static class Mathf {
    public static float Abs(float n)=>Math.Abs(n);
    public static float Clamp01(float n)=>Math.Max(0,Math.Min(1,n));
    public static float MoveTowards(float a,float b,float step)=>Math.Abs(b-a)<=step?b:a+Math.Sign(b-a)*step;
}
}
