using UnityEngine;

namespace Rachel350Z {
// Own both projections while boosting; Valheim renders the sky separately.
public sealed class NitroCameraView {
    Camera scene,sky;
    float originalScene,originalSky;
    public void Apply(Camera sceneCamera,Camera skyCamera,float amount) {
        if(sceneCamera==null||amount<=.001f||float.IsNaN(amount)||float.IsInfinity(amount)){Reset();return;}
        if(skyCamera==sceneCamera)skyCamera=null;
        if(scene!=sceneCamera||sky!=skyCamera) {
            Reset();scene=sceneCamera;sky=skyCamera;
            originalScene=scene.fieldOfView;
            if(sky!=null)originalSky=sky.fieldOfView;
        }
        scene.fieldOfView=Mathf.Min(179,originalScene+9*Mathf.Clamp01(amount));
        if(sky!=null)sky.fieldOfView=scene.fieldOfView;
    }
    public void Reset() {
        if(scene!=null)scene.fieldOfView=originalScene;
        if(sky!=null)sky.fieldOfView=originalSky;
        scene=sky=null;
    }
}
}
