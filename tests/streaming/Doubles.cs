using System;
using System.Collections.Generic;
namespace UnityEngine {
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 ClampMagnitude(Vector3 v,float max) {
            float length=(float)Math.Sqrt(v.x*v.x+v.y*v.y+v.z*v.z);
            return length>max?v*(max/length):v;
        }
    }
    public class Transform {public Vector3 position;}
    public class Mesh {}
    public class GameObject {
        public bool activeInHierarchy=true;
        public Heightmap template;
        public T GetComponentInChildren<T>() where T:class=>template as T;
    }
    public class MeshCollider {
        public bool enabled=true;
        public Mesh sharedMesh=new Mesh();
        public GameObject gameObject=new GameObject();
    }
    public static class Time {public static float fixedDeltaTime=.02f;}
    public class Rigidbody {
        public Vector3 position,linearVelocity,angularVelocity;
        public bool isKinematic,useGravity=true;public int wakes;
        public void WakeUp(){wakes++;}
    }
}
public struct Vector2s : IEquatable<Vector2s> {
    public int x,y;
    public Vector2s(int x,int y){this.x=x;this.y=y;}
    public bool Equals(Vector2s other)=>x==other.x&&y==other.y;
    public override bool Equals(object obj)=>obj is Vector2s other&&Equals(other);
    public override int GetHashCode()=>x*397^y;
}
public class Player {
    public static Player m_localPlayer;
    public bool enabled;
    public readonly UnityEngine.Transform transform=new UnityEngine.Transform();
}
public class ZNet {
    public static ZNet instance;
    public UnityEngine.Vector3 reference;public int calls;
    public void SetReferencePosition(UnityEngine.Vector3 position){reference=position;calls++;}
}
public class ZoneSystem {
    public static ZoneSystem instance;
    public UnityEngine.GameObject m_zonePrefab=new UnityEngine.GameObject{template=new Heightmap()};
    public readonly HashSet<Vector2s> missing=new HashSet<Vector2s>();
    public int queries;
    public static Vector2s GetZone(UnityEngine.Vector3 p)=>new Vector2s((int)Math.Floor((p.x+32)/64),(int)Math.Floor((p.z+32)/64));
    public static UnityEngine.Vector3 GetZonePos(Vector2s zone)=>new UnityEngine.Vector3(zone.x*64,0,zone.y*64);
    public bool IsZoneLoaded(Vector2s zone){queries++;return !missing.Contains(zone);}
}
public class Heightmap {
    public static readonly HashSet<Vector2s> missing=new HashSet<Vector2s>();
    public static readonly Dictionary<Vector2s,Heightmap> maps=new Dictionary<Vector2s,Heightmap>();
    public int m_width=64;public float m_scale=1;public bool IsDistantLod;
    public UnityEngine.MeshCollider collider=new UnityEngine.MeshCollider();
    public T GetComponent<T>() where T:class=>collider as T;
    public static Heightmap FindHeightmap(UnityEngine.Vector3 p) {
        var zone=ZoneSystem.GetZone(p);if(missing.Contains(zone))return null;
        if(!maps.TryGetValue(zone,out var map)){map=new Heightmap();maps.Add(zone,map);}return map;
    }
}
public class WorldGenerator {public static WorldGenerator instance;}
public class HeightmapBuilder {
    public static HeightmapBuilder instance;
    public readonly HashSet<Vector2s> requests=new HashSet<Vector2s>();
    public int calls;public int width;public float scale;public bool distant;
    public bool IsTerrainReady(UnityEngine.Vector3 p,int width,float scale,bool distant,WorldGenerator generator) {
        calls++;requests.Add(ZoneSystem.GetZone(p));this.width=width;this.scale=scale;this.distant=distant;
        return false; // Jobs remain pending to prove callers never wait for them.
    }
}
public class ZNetScene {
    public static ZNetScene instance;
    public readonly HashSet<Vector2s> missing=new HashSet<Vector2s>();
    public int queries;
    public bool IsAreaReady(UnityEngine.Vector3 p){queries++;return !missing.Contains(ZoneSystem.GetZone(p));}
}
