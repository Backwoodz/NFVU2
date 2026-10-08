using System;
namespace Rachel350Z {
// Normalized playback only; visual animations never operate the chassis.
public sealed class ShowroomState {
    public readonly float[] Progress=new float[4]; // NFS LEFT, NFS RIGHT, hood, hatch
    public readonly bool[] Open=new bool[4];
    public int DoorStyle {get;private set;}
    int requestedStyle;
    public bool Toggle(int index,bool stationary) {
        if(index<0||index>=4||(!stationary&&!Open[index]))return false;
        Open[index]=!Open[index];return true;
    }
    public void CloseAll(){for(int i=0;i<4;i++)Open[i]=false;}
    public void Step(float dt,bool moving,int style) {
        if(float.IsNaN(dt)||float.IsInfinity(dt))dt=0;
        dt=Math.Max(0,Math.Min(.1f,dt));style=Math.Max(0,Math.Min(2,style));
        requestedStyle=style;
        if(moving)CloseAll();
        if(requestedStyle!=DoorStyle){Open[0]=Open[1]=false;}
        for(int i=0;i<4;i++)Progress[i]=Math.Max(0,Math.Min(1,Progress[i]+(Open[i]?dt:-dt)));
        if(Progress[0]==0&&Progress[1]==0)DoorStyle=requestedStyle;
    }
}
}
