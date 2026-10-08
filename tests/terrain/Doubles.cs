using System;
using System.Collections.Generic;
namespace UnityEngine {
public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();}
public sealed class Collider {public Heightmap map;public T GetComponentInParent<T>() where T:class=>map as T;}
public struct RaycastHit {public Collider collider;public Vector3 point;}
public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);}
}
namespace Rachel350Z {public struct ModelVertex {public float x,y,z;}}
public class TerrainModifier {public enum PaintType {Dirt}}
public class TerrainOp {
 public class Settings {
  public bool m_raise,m_square,m_level,m_smooth,m_paintCleared,m_paintHeightCheck,m_halfOffset;
  public float m_raiseRadius,m_raiseDelta,m_raisePower,m_paintRadius;public TerrainModifier.PaintType m_paintType;
  public float GetRadius()=>Math.Max(m_raiseRadius,m_paintRadius);
 }
}
public class Heightmap {
 public static List<Heightmap> maps=new List<Heightmap>();public static float queriedRadius;
 public TerrainComp compiler=new TerrainComp();public float height=10;public bool validHeight=true;
 public bool GetWorldHeight(UnityEngine.Vector3 point,out float value){value=height;return validHeight;}
 public static void FindHeightmap(UnityEngine.Vector3 point,float radius,List<Heightmap> results){queriedRadius=radius;results.AddRange(maps);}
 public TerrainComp GetAndCreateTerrainCompiler()=>compiler;
}
public class TerrainComp {
 public bool owner=true;public int operations;public UnityEngine.Vector3 point;public TerrainOp.Settings settings;
 public bool IsOwner()=>owner;
 private void DoOperation(UnityEngine.Vector3 point,UnityEngine.Vector3 rotation,TerrainOp.Settings settings){operations++;this.point=point;this.settings=settings;}
}
