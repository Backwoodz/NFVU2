using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> nitroBlurEnabled,nitroTrailsEnabled;
    ConfigEntry<float> nitroVisualStrength;
    readonly NitroVisualState nitroVisualState=new NitroVisualState();
    NitroScreenEffect nitroScreen;
    VehicleNitroTrails nitroTrails;
    Transform nitroVisualCar;
    Camera nitroVisualCamera;
    bool trailsAttempted,screenAttempted;
    void InitializeNitroVisuals() {
        nitroBlurEnabled=Config.Bind("NitroVisuals","RadialBlur",true,"Radial edge blur during actual ground/flight boost. NFVU2 approximation, not the original NFS shader.");
        nitroTrailsEnabled=Config.Bind("NitroVisuals","TailLightTrails",true,"Short red trails from lit rear lamps during boost.");
        nitroVisualStrength=Config.Bind("NitroVisuals","Strength",.7f,new ConfigDescription("Shared screen/trail strength. Zero disables both.",new AcceptableValueRange<float>(0,1)));
    }
    void TickNitroVisuals() {
        if(!driving||car==null||body==null||Transforming){ClearNitroVisuals();return;}
        var camera=Camera.main;
        if(nitroVisualCar!=car.transform||nitroVisualCamera!=camera) {
            ClearNitroVisuals();nitroVisualCar=car.transform;nitroVisualCamera=camera;
        }
        bool allowed=DriverInputReady&&!audioBlocked&&!menuOpen&&Application.isFocused&&Time.timeScale>0;
        bool boost=nitroEnabled.Value&&ActiveNitro!=null&&ActiveNitro.Boosting;
        float strength=nitroVisualStrength.Value;
        if(float.IsNaN(strength)||float.IsInfinity(strength))strength=0;
        nitroVisualState.Step(Time.unscaledDeltaTime,boost,allowed&&nitroBlurEnabled.Value,strength);
        if(nitroScreen==null&&!screenAttempted&&nitroVisualState.Amount>0&&camera!=null) {
            screenAttempted=true;
            try {
                if(GraphicsSettings.currentRenderPipeline==null) {
                    nitroScreen=camera.gameObject.AddComponent<NitroScreenEffect>();
                    nitroScreen.Initialize(message=>Logger.LogWarning(message));
                }else Logger.LogWarning("Nitro radial blur requires the built-in render pipeline; light trails remain available.");
            }catch(Exception error){Logger.LogWarning("Nitro screen attachment unavailable: "+error.Message);}
        }
        if(nitroScreen!=null)nitroScreen.SetEffect(nitroVisualState.Amount,ProtectedCarRect(camera));
        bool trailAllowed=allowed&&nitroTrailsEnabled.Value&&vehicleLighting!=null&&vehicleLighting.RearLightsOn;
        if(!trailsAttempted&&trailAllowed&&boost&&strength>0) {
            trailsAttempted=true;
            try {nitroTrails=new VehicleNitroTrails(car.transform,vehicleLighting.RearLightPositions());}
            catch(Exception error){Logger.LogWarning("Nitro rear light trails unavailable: "+error.Message);}
        }
        nitroTrails?.Tick(boost,trailAllowed,strength,body.linearVelocity.magnitude);
    }
    Rect ProtectedCarRect(Camera camera) {
        if(camera==null)return Rect.MinMaxRect(0,0,1,1);
        float left=1,right=0,bottom=1,top=0;
        float width=IsHummer?1.2f:1.1f,height=IsHummer?2.15f:1.55f,length=IsHummer?2.7f:2.5f;
        for(int i=0;i<8;i++) {
            var p=camera.WorldToViewportPoint(car.transform.TransformPoint(new Vector3((i&1)==0?-width:width,(i&2)==0?0:height,(i&4)==0?-length:length)));
            // Close camera / clipping: protect the full image instead of flashing
            // or smearing the cabin when corners straddle the camera plane.
            if(p.z<=camera.nearClipPlane)return Rect.MinMaxRect(0,0,1,1);
            left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);bottom=Mathf.Min(bottom,p.y);top=Mathf.Max(top,p.y);
        }
        return Rect.MinMaxRect(left-.025f,bottom-.025f,right+.025f,top+.025f);
    }
    void ClearNitroVisuals() {
        nitroVisualState.Reset();
        if(nitroScreen!=null){nitroScreen.SetEffect(0,new Rect());Destroy(nitroScreen);}
        nitroScreen=null;nitroTrails?.Dispose();nitroTrails=null;
        nitroVisualCar=null;nitroVisualCamera=null;screenAttempted=trailsAttempted=false;
    }
    void DrawNitroVisualSettings() {
        nitroBlurEnabled.Value=UiToggle(new Rect(25,322,510,28),nitroBlurEnabled.Value," Nitro-Tunnelblick");
        nitroTrailsEnabled.Value=UiToggle(new Rect(25,355,510,28),nitroTrailsEnabled.Value," Rote Rücklichtspuren bei Nitro");
        UiLabel(new Rect(25,394,510,25),"Effektstärke  "+Mathf.RoundToInt(nitroVisualStrength.Value*100)+" %",labelStyle);
        nitroVisualStrength.Value=UiSlider(new Rect(25,428,505,22),nitroVisualStrength.Value,0,1);
        UiLabel(new Rect(25,463,510,44),"Gilt für beide Autos, am Boden und im Flug.\nRücklichtspuren erscheinen bei eingeschaltetem Licht.",mutedStyle);
    }
}
}
