using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public class Object {public static int Destroyed;public static void Destroy(Object value){Destroyed++;}}
 public class GameObject:Object {
  public readonly List<Component> components=new List<Component>();public GameObject(string name=""){}
  public T AddComponent<T>() where T:Component,new(){var c=new T();c.gameObject=this;components.Add(c);return c;}
 }
 public class Component:Object {
  public GameObject gameObject=new GameObject();
  public T GetComponent<T>() where T:class=>gameObject.components.OfType<T>().FirstOrDefault();
  public T GetComponentInParent<T>() where T:class=>GetComponent<T>();
  public T[] GetComponentsInChildren<T>() where T:class=>gameObject.components.OfType<T>().ToArray();
 }
 public class MonoBehaviour:Component {}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new Vector3();public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
  public Vector3 normalized=>magnitude>0?this*(1/magnitude):zero;
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
 }
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class Texture2D:Object {public static Texture2D whiteTexture=new Texture2D();}
 public class Material:Object {public bool HasProperty(string s)=>true;}
 public class MaterialPropertyBlock {public Dictionary<string,Color> colors=new Dictionary<string,Color>();public void SetColor(string s,Color c)=>colors[s]=c;}
 public class Renderer:Component {
  public Material[] sharedMaterials=new[]{new Material()};public MaterialPropertyBlock block=new MaterialPropertyBlock();
  public void GetPropertyBlock(MaterialPropertyBlock b,int i)=>b.colors=new Dictionary<string,Color>(block.colors);
  public void SetPropertyBlock(MaterialPropertyBlock b,int i){block=new MaterialPropertyBlock();block.colors=new Dictionary<string,Color>(b.colors);}
 }
 public class MeshRenderer:Renderer {}public class SkinnedMeshRenderer:Renderer {}
 public class LineRenderer:Renderer {
  public static List<LineRenderer> All=new List<LineRenderer>();public LineRenderer(){All.Add(this);}
  public Material sharedMaterial;public bool useWorldSpace,receiveShadows,enabled;public int positionCount;
  public float startWidth,endWidth;public Color startColor,endColor;public Rendering.ShadowCastingMode shadowCastingMode;
  public void SetPosition(int i,Vector3 point){}
 }
 namespace Rendering {public enum ShadowCastingMode{Off}}
 public class Collider:Component {}
 public struct RaycastHit {public Collider collider;public Vector3 point;}
 public enum QueryTriggerInteraction {Ignore}
 public static class Physics {public const int DefaultRaycastLayers=0;public static RaycastHit[] covers=Array.Empty<RaycastHit>();public static RaycastHit[] RaycastAll(Vector3 a,Vector3 b,float c,int d,QueryTriggerInteraction e)=>covers;}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Random {public static Vector3 insideUnitSphere=>Vector3.zero;}
}
public class Character:UnityEngine.MonoBehaviour {
 public static List<Character> All=new List<Character>();public Character(){All.Add(this);}
 public bool dead,player,tame,enemy=true;public int hits;public float lightning,animationFreeze;
 public float m_speed=4,m_runSpeed=8,m_walkSpeed=2,m_swimSpeed=3,m_flySlowSpeed=5,m_flyFastSpeed=12;
 public UnityEngine.Vector3 point;public UnityEngine.Vector3 GetCenterPoint()=>point;
 public bool IsDead()=>dead;public bool IsPlayer()=>player;public bool IsTamed()=>tame;
 public static List<Character> GetAllCharacters()=>All;
 public void SetMoveDir(UnityEngine.Vector3 p){}public void FreezeFrame(float t)=>animationFreeze=t;
 public void Damage(HitData hit){hits++;lightning+=hit.m_damage.m_lightning;}
}
public class HitData {public UnityEngine.Vector3 m_point,m_dir;public Damage m_damage;public struct Damage {public float m_lightning;}public void SetAttacker(Character c){}}
public class BaseAI:UnityEngine.MonoBehaviour {public static bool IsEnemy(Character a,Character b)=>a.enemy;}
public class MonsterAI:BaseAI {public int stopped;public bool UpdateAI(float dt)=>true;public void StopMoving()=>stopped++;}
namespace Rachel350Z {
 public class HordeEnemyMarker:UnityEngine.MonoBehaviour {}
 public static class VehicleMaterials {public static UnityEngine.Material CreateGlowMaterial(UnityEngine.Texture2D texture)=>new UnityEngine.Material();}
}
