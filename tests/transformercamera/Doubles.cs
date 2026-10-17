using System;
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 up=>new Vector3(0,1,0);
    public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp(t,0,1);
}
public class Transform {public Vector3 position,eulerAngles;}
public class Camera {public float fieldOfView;public readonly Transform transform=new Transform();}
public static class Mathf {
    public static float Max(float a,float b)=>Math.Max(a,b);
    public static float Clamp(float value,float low,float high)=>Math.Max(low,Math.Min(high,value));
    public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp(t,0,1);
    public static float LerpAngle(float a,float b,float t)=>a+Rachel350Z.DrivingMath.WrapAngle(b-a)*Clamp(t,0,1);
}
}
public class GameCamera {public static GameCamera instance;public UnityEngine.Camera m_skyCamera;}
namespace Rachel350Z {
// Adapter harness supplies the ordinary orbit state. The cinematic partial
// and pure progression policy are compiled unchanged from production sources.
public sealed partial class VehicleOrbitCamera {
    bool active=true;
    UnityEngine.Transform vehicle=new UnityEngine.Transform();
    UnityEngine.Camera camera;
    float yaw,pitch=18,distance=6,smoothedDistance=6;
    UnityEngine.Vector3 smoothedPivot=new UnityEngine.Vector3(0,1.05f,0);
    readonly ChaseCameraState chase=new ChaseCameraState();
    public float RobotBlend=1;
    public float ViewYaw,ViewPitch,Radius;
    public UnityEngine.Vector3 Pivot;
    public float ChaseYaw=>chase.Yaw;
    public float OrbitPitch=>pitch;
    public float BaseRadius=>distance;
    public bool IsShot=>deploymentShot.Active||deploymentShot.Releasing;
    public void Bind(UnityEngine.Camera scene,float heading){ResetTransformationShot();camera=scene;camera.transform.eulerAngles=new UnityEngine.Vector3(18,heading,0);chase.Reset(heading);}
    public void Render(){Pivot=new UnityEngine.Vector3(0,4.1f,0);ViewYaw=chase.Yaw;ViewPitch=pitch;Radius=distance+3.5f;ApplyTransformationShot(ref Pivot,ref ViewYaw,ref ViewPitch,ref Radius,out _);}
    public void TakeOver()=>InterruptTransformationShot();
    public void StopShot()=>ResetTransformationShot();
    public void ReplaceCamera(UnityEngine.Camera replacement){camera=replacement;}
}
}
