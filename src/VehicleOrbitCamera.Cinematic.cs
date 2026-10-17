using UnityEngine;
namespace Rachel350Z {
public sealed partial class VehicleOrbitCamera {
    readonly TransformerCinematicCamera deploymentShot=new TransformerCinematicCamera();
    Camera shotScene,shotSky;
    float shotFov,shotSkyFov,startShotYaw,startShotPitch,startShotRadius;
    Vector3 startShotOffset,lastShotOffset;
    float lastShotYaw,lastShotPitch,lastShotRadius,lastShotZoom;
    bool shotRendered;
    public void BeginTransformationShot() {
        ResetTransformationShot();
        if(!active||vehicle==null||camera==null)return;
        shotScene=camera;shotSky=GameCamera.instance!=null?GameCamera.instance.m_skyCamera:null;
        if(shotSky==shotScene)shotSky=null;
        shotFov=shotScene.fieldOfView;if(shotSky!=null)shotSkyFov=shotSky.fieldOfView;
        startShotYaw=camera.transform.eulerAngles.y;
        startShotPitch=DrivingMath.WrapAngle(camera.transform.eulerAngles.x);
        startShotRadius=smoothedDistance;startShotOffset=smoothedPivot-vehicle.position;
        deploymentShot.Begin();
    }
    public void TransformationFrame(float dt,bool deployed,bool returning,float progress,bool blocked) {
        bool wasActive=deploymentShot.Active;
        deploymentShot.Step(dt,deployed,returning,progress,blocked);
        if(wasActive&&!deploymentShot.Active)AdoptShotOrbit();
        if(!deploymentShot.Active&&!deploymentShot.Releasing)ResetTransformationShot();
    }
    void InterruptTransformationShot() {
        if(!deploymentShot.Active)return;
        deploymentShot.Cancel();AdoptShotOrbit();
    }
    void AdoptShotOrbit() {
        if(!shotRendered){ResetTransformationShot();return;}
        // Continue from the displayed angle. Taking control never sends the
        // camera back around the robot to the angle from before the shot.
        yaw=DrivingMath.WrapAngle(lastShotYaw-vehicle.eulerAngles.y);
        pitch=DrivingMath.Clamp(lastShotPitch,-10,70);
        distance=DrivingMath.Clamp(lastShotRadius-RobotBlend*3.5f,3,10);
        chase.Reset(lastShotYaw);chase.Hold();
    }
    void ApplyTransformationShot(ref Vector3 pivot,ref float viewYaw,ref float viewPitch,ref float radius,out float impulse) {
        impulse=0;
        if(!deploymentShot.Active&&!deploymentShot.Releasing)return;
        if(camera!=shotScene){ResetTransformationShot();return;}
        if(deploymentShot.Active) {
            var pose=deploymentShot.Pose;
            // Establish from the current view without a cut, then make the
            // authored 45-degree arc from the left flank to the front-left.
            viewYaw=Mathf.LerpAngle(startShotYaw,vehicle.eulerAngles.y+90+pose.Orbit,pose.YawBlend);
            viewPitch=Mathf.Lerp(startShotPitch,-2,pose.PitchBlend);
            float far=Mathf.Max(9.8f,startShotRadius);
            radius=Mathf.Lerp(Mathf.Lerp(startShotRadius,far,pose.Pullback),9.5f,pose.Dolly);
            pivot=vehicle.position+Vector3.Lerp(startShotOffset,Vector3.up*2.65f,pose.Rise);
            lastShotYaw=viewYaw;lastShotPitch=viewPitch;lastShotRadius=radius;
            lastShotOffset=pivot-vehicle.position;lastShotZoom=pose.Zoom;shotRendered=true;
            impulse=pose.Impulse;
        } else {
            float weight=deploymentShot.Weight;
            pivot=Vector3.Lerp(pivot,vehicle.position+lastShotOffset,weight);
            radius=Mathf.Lerp(radius,lastShotRadius,weight);
        }
        float fov=Mathf.Clamp(shotFov-lastShotZoom*deploymentShot.Weight,1,179);
        shotScene.fieldOfView=fov;if(shotSky!=null)shotSky.fieldOfView=fov;
    }
    void ResetTransformationShot() {
        deploymentShot.Reset();shotRendered=false;
        if(shotScene!=null)shotScene.fieldOfView=shotFov;
        if(shotSky!=null)shotSky.fieldOfView=shotSkyFov;
        shotScene=shotSky=null;
    }
}
}
