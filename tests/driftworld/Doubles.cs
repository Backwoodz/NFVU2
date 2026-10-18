extern alias NativeValheim;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace UnityEngine {
    public struct Color {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color black=>new Color{a=1};
        public static bool operator ==(Color x,Color y)=>x.r==y.r&&x.g==y.g&&x.b==y.b&&x.a==y.a;
        public static bool operator !=(Color x,Color y)=>!(x==y);
        public override bool Equals(object other)=>other is Color value&&this==value;
        public override int GetHashCode()=>r.GetHashCode()^g.GetHashCode()^b.GetHashCode()^a.GetHashCode();
    }
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
}
namespace TMPro {
    public class TMP_Text {public UnityEngine.Color color;}
    public class TMP_InputField {public bool isActiveAndEnabled=true;public string text;public TMP_Text textComponent;}
}
public class World {public string m_seedName;public bool m_menu;}
public class Heightmap {public enum Biome {Meadows,Mountain,Ocean}}
public class WorldGenerator {
    public static WorldGenerator instance;
    public World m_world;
    public int HeightCalls,BiomeCalls;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Heightmap.Biome GetBiome(float wx,float wy,float oceanLevel=.02f,bool waterAlwaysOcean=false){BiomeCalls++;return Heightmap.Biome.Mountain;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public float GetBiomeHeight(Heightmap.Biome biome,float wx,float wy,out UnityEngine.Color mask,bool preGeneration=false,bool riverPreDN=true){HeightCalls++;mask=new UnityEngine.Color{r=1};return 123;}
}
// Use the installed game's real stream-backed package. A queue double hid
// the missing reader rewind that caused world generation to hang in Valheim.
public class ZPackage : NativeValheim::ZPackage {}
public class Game {public static Game instance;public string m_StartLocation="StartTemple";}
public class ZoneSystem {
    public class ZoneLocation {public int m_quantity=3;public bool m_enable=true;}
    public int NativeAttempts,NativeVegetation,NativeLocations,Completed;
    public readonly List<string> SavedPlayerBuildings=new List<string>();
    [MethodImpl(MethodImplOptions.NoInlining)]
    IEnumerator GenerateLocationsTimeSliced(ZoneLocation location,Stopwatch timeSliceStopwatch,ZPackage iterationsPkg) {
        NativeAttempts++;iterationsPkg.Write(17);iterationsPkg.SetPos(0);return Empty();
    }
    static IEnumerator Empty(){yield break;}
    public int GenerateOne(ZoneLocation location) {
        var result=new ZPackage();var worker=GenerateLocationsTimeSliced(location,new Stopwatch(),result);
        while(worker.MoveNext()){}
        Completed++;return result.ReadInt();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PlaceVegetation(){NativeVegetation++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PlaceLocations(){NativeLocations++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool GetLocationIcon(string name,out UnityEngine.Vector3 pos){pos=new UnityEngine.Vector3(1,2,3);return false;}
}
