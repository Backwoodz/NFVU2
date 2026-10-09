using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<float> rimInches;
    ConfigEntry<float> h2RimInches;
    ConfigEntry<float> z_lowering;
    ConfigEntry<float> lowering=>VehicleConfig(z_lowering);

    void InitializeWheelTuning() {
        z_lowering=WheelSetting("LoweringCm",0,0,12,"Lower the body by raising suspension anchors, in centimetres. Zero restores stock height; less ground clearance at higher values.");
        h2RimInches=Config.Bind("Hummer.Wheels","RimInches",26f,new ConfigDescription("H2 spinner rim diameter, tyre diameter unchanged.",new AcceptableValueRange<float>(22,28)));
        frontCamber=WheelSetting("FrontCamber",0,-15,10,"Front visual camber in degrees. Negative tilts the top inward.");
        rearCamber=WheelSetting("RearCamber",0,-15,10,"Rear visual camber in degrees. Negative tilts the top inward.");
        frontWheelOffset=WheelSetting("FrontOffsetCm",0,-10,20,"Front visual offset per wheel in centimetres. Positive moves outward.");
        rearWheelOffset=WheelSetting("RearOffsetCm",0,-10,20,"Rear visual offset per wheel in centimetres. Positive moves outward.");
        rimInches=WheelSetting("RimInches",20,16,22,"Nominal rim diameter relative to the original NFS 20-inch wheel. Tyre outside diameter stays fixed.");
    }
    ConfigEntry<float> WheelSetting(string key,float value,float min,float max,string description) =>
        BindVehicle("Wheels",key,value,new ConfigDescription(description,new AcceptableValueRange<float>(min,max)));

    void DrawWheelMenu() {
        GUI.enabled=originalWheels;
        WheelSlider(frontCamber,"Sturz vorne",140,-15,10,.5f,"°");
        WheelSlider(rearCamber,"Sturz hinten",191,-15,10,.5f,"°");
        WheelSlider(frontWheelOffset,"Radversatz vorne",242,-10,20,.5f," cm / Rad");
        WheelSlider(rearWheelOffset,"Radversatz hinten",293,-10,20,.5f," cm / Rad");
        WheelSlider(IsHummer?h2RimInches:rimInches,"Felgengröße",344,IsHummer?22:16,IsHummer?28:22,.5f," Zoll");
        GUI.enabled=true;
        WheelSlider(lowering,"Tieferlegung",395,0,12,.5f," cm");
        if(MenuButton(new Rect(25,455,510,30),"Räder und Tieferlegung zurücksetzen")) {
            lowering.Value=0;
            frontCamber.Value=rearCamber.Value=0;
            frontWheelOffset.Value=rearWheelOffset.Value=0;if(IsHummer)h2RimInches.Value=26;else rimInches.Value=20;
        }
        GUI.enabled=true;
        GUI.Label(new Rect(25,490,510,30),originalWheels?
            "Tieferlegung reduziert die Bodenfreiheit. Sturz und Versatz sind optisch.":
            "Für die importierten Originalräder verfügbar.",mutedStyle);
    }
    void WheelSlider(ConfigEntry<float> setting,string label,float y,float min,float max,float step,string unit) {
        float current=WheelFitMath.Sanitize(setting.Value,min,max,(float)setting.DefaultValue);
        GUI.Label(new Rect(25,y,510,24),label+"   "+current.ToString("0.0")+unit,labelStyle);
        float next=GUI.HorizontalSlider(new Rect(25,y+31,510,20),current,min,max);
        next=Mathf.Round(next/step)*step;
        if(next!=setting.Value)setting.Value=next;
    }

    void UpdateWheelVisual(int index) {
        wheels[index].GetWorldPose(out var pos,out var rot);
        if(!originalWheels) {
            visuals[index].SetPositionAndRotation(pos,rot*Quaternion.Euler(0,0,90));return;
        }
        float side=index%2==0?-1:1;
        float camber=WheelFitMath.Sanitize((index<2?frontCamber:rearCamber).Value,-15,10,0);
        float offset=WheelFitMath.Sanitize((index<2?frontWheelOffset:rearWheelOffset).Value,-10,20,0)*.01f;
        // Translation stays in the car frame; camber precedes the wheel's spin
        // so neither offset nor tilt rotates around the axle during driving.
        pos+=car.transform.up*wheelVisualLift+car.transform.forward*(index<2?frontWheelVisualOffset:rearWheelVisualOffset)
            +car.transform.right*(side*offset);
        var steering=car.transform.rotation*Quaternion.Euler(0,wheels[index].steerAngle,0);
        var tilt=Quaternion.AngleAxis(-side*camber,Vector3.forward);
        var mirror=Quaternion.Euler(0,index%2==0?180:0,0);
        visuals[index].SetPositionAndRotation(pos,steering*tilt*Quaternion.Inverse(steering)*rot*mirror);
        if(brakeVisuals[index]!=null)brakeVisuals[index].SetPositionAndRotation(pos,steering*tilt*mirror);
        if(current.spinners!=null&&current.spinners[index]!=null) {
            var motion=current.spinnerMotion[index];motion.Step(Time.deltaTime,wheels[index].rpm);
            current.spinners[index].SetPositionAndRotation(pos,steering*tilt*Quaternion.AngleAxis(motion.Angle,Vector3.right)*mirror);
            float scale=WheelFitMath.Sanitize(h2RimInches.Value,22,28,26)/26;
            current.spinners[index].localScale=new Vector3(1,scale,scale);
        }
    }
}
}
