using System;
namespace Rachel350Z {
// Screen-pixel placement, independent of the menu's content scale.
public sealed class MenuPlacement {
    public float X {get;private set;}
    public float Y {get;private set;}
    public bool Dragging {get;private set;}
    float grabX,grabY;
    static float Finite(float n,float fallback)=>float.IsNaN(n)||float.IsInfinity(n)?fallback:n;
    public MenuPlacement(float x,float y){X=Finite(x,16);Y=Finite(y,16);}
    public void Constrain(float screenWidth,float screenHeight,float width,float height) {
        X=Math.Max(0,Math.Min(X,Math.Max(0,screenWidth-width)));
        Y=Math.Max(0,Math.Min(Y,Math.Max(0,screenHeight-height)));
    }
    public void Begin(float mouseX,float mouseY){grabX=mouseX-X;grabY=mouseY-Y;Dragging=true;}
    public void Move(float mouseX,float mouseY,float screenWidth,float screenHeight,float width,float height) {
        if(!Dragging)return;
        X=Finite(mouseX-grabX,X);Y=Finite(mouseY-grabY,Y);
        Constrain(screenWidth,screenHeight,width,height);
    }
    public void End(){Dragging=false;}
}
}
