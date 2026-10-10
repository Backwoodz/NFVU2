using System;
using Rachel350Z;
using UnityEngine;

namespace UnityEngine {
public class Camera {public float fieldOfView;}
public static class Mathf {
    public static float Min(float a,float b)=>Math.Min(a,b);
    public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));
}
}
class NitroViewTests {
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static void Main() {
        var view=new NitroCameraView();
        var scene=new Camera{fieldOfView=65};var sky=new Camera{fieldOfView=65};
        view.Apply(scene,sky,0);
        Check(scene.fieldOfView==65&&sky.fieldOfView==65,"inactive effect leaves both cameras alone");
        view.Apply(scene,sky,.7f);float boosted=scene.fieldOfView;
        Check(boosted>65&&sky.fieldOfView==boosted,"boost widens scene and sky together");
        for(int i=0;i<100;i++)view.Apply(scene,sky,.7f);
        Check(scene.fieldOfView==boosted,"sustained boost does not accumulate field of view");
        view.Apply(scene,sky,.2f);
        Check(scene.fieldOfView>65&&scene.fieldOfView<boosted,"fade moves back toward the original view");
        view.Apply(scene,sky,0);
        Check(scene.fieldOfView==65&&sky.fieldOfView==65,"zero strength or blocked visuals restores both cameras");
        scene.fieldOfView=95;sky.fieldOfView=90;view.Apply(scene,sky,1);view.Reset();view.Reset();
        Check(scene.fieldOfView==95&&sky.fieldOfView==90,"exit restores independently captured values and reset is idempotent");
        view.Apply(scene,sky,1);
        var replacement=new Camera{fieldOfView=75};view.Apply(replacement,null,.5f);
        Check(scene.fieldOfView==95&&sky.fieldOfView==90&&replacement.fieldOfView>75,"camera replacement restores old pair before capturing new view");
        view.Apply(replacement,null,float.NaN);
        Check(replacement.fieldOfView==75,"invalid strength restores the camera");
        view.Apply(replacement,replacement,1);view.Reset();
        Check(replacement.fieldOfView==75,"shared scene and sky cannot overwrite the saved value");
        replacement.fieldOfView=120;view.Apply(replacement,null,1);
        Check(replacement.fieldOfView>120,"wide user field of view is not narrowed by boost");
        view.Apply(null,null,1);
        Check(replacement.fieldOfView==120,"missing camera releases the previous view");
        Console.WriteLine(checks+" nitro camera checks passed (test doubles, not rendered frames).");
    }
}
