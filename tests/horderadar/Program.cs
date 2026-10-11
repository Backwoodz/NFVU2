using System;
using Rachel350Z;
using UnityEngine;
namespace UnityEngine {
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2(0,0);}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
}
static class Program {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.002f;
    static void Main() {
        var uv=new Rect(.495f,.495f,.01f,.01f);var size=new Vector2(256,256);
        bool Project(float x,float z,out Vector2 p)=>HordeMapProjection.TryProject(new Vector3(x,99,z),64,256,uv,size,out p);
        Check(Project(0,0,out var center)&&Near(center.x,128)&&Near(center.y,128),"world center maps to viewport center");
        Check(Project(40,0,out var east)&&Near(east.x,190.5f)&&Near(east.y,128),"east moves right by the native world/UV scale");
        Check(Project(0,40,out var north)&&Near(north.x,128)&&Near(north.y,190.5f),"north uses world Z and moves up");
        Check(Project(-40,-40,out var southwest)&&Near(southwest.x,65.5f)&&Near(southwest.y,65.5f),"negative coordinates retain orientation");
        Check(HordeMapProjection.TryProject(new Vector3(0,-999,0),64,256,uv,size,out var below)&&Near(below.x,center.x)&&Near(below.y,center.y),"altitude does not offset a minimap dot");
        uv=new Rect(.505f,.475f,.01f,.01f);
        Check(Project(163.84f,-327.68f,out var moved)&&Near(moved.x,128)&&Near(moved.y,128),"map scrolling follows a car away from world origin");
        uv=new Rect(.49f,.49f,.02f,.02f);
        Check(Project(40,0,out var zoomed)&&Near(zoomed.x,159.25f),"zoom out halves the pixel offset");
        uv=new Rect(.495f,.495f,.01f,.01f);size=new Vector2(512,256);
        Check(Project(40,40,out var resized)&&Near(resized.x,381)&&Near(resized.y,190.5f),"non-square viewport scales each axis independently");
        size=new Vector2(256,256);
        Check(!Project(82,0,out _)&&!Project(-82,0,out _)&&!Project(0,82,out _)&&!Project(0,-82,out _),"all four out-of-view edges are hidden");
        Check(!Project(80,0,out _)&&Project(78,0,out _),"dot radius stays inside the minimap border");
        Check(!Project(float.NaN,0,out _)&&!Project(0,float.PositiveInfinity,out _),"invalid enemy coordinates cannot enter UI geometry");
        Check(!HordeMapProjection.TryProject(new Vector3(),0,256,uv,size,out _)&&!HordeMapProjection.TryProject(new Vector3(),64,0,uv,size,out _),"uninitialized map dimensions safely hide dots");
        Check(!HordeMapProjection.TryProject(new Vector3(),64,256,new Rect(0,0,0,.01f),size,out _),"zero zoom width safely hides dots");
        Check(!HordeMapProjection.TryProject(new Vector3(),64,256,uv,new Vector2(0,0),out _),"collapsed map viewport safely hides dots");
        Console.WriteLine(checks+" horde radar projection checks passed (UI rendering requires an in-game check).");
    }
}
