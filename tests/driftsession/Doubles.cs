using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace UnityEngine {
    public class GameObject {
        readonly Dictionary<Type,object> parts=new Dictionary<Type,object>();
        public bool Destroyed;
        public T GetComponent<T>() where T:class=>parts.TryGetValue(typeof(T),out var value)?value as T:null;
        public T Add<T>(T part) where T:Component {parts[typeof(T)]=part;part.gameObject=this;return part;}
    }
    public class Component {public GameObject gameObject;public T GetComponent<T>()where T:class=>gameObject.GetComponent<T>();}
}
public class World {public string m_seedName;public bool m_menu;}
public class WorldGenerator {public static WorldGenerator instance;public World m_world;}
public static class ZNet {public static bool IsSinglePlayer=true;}
public class ZDOMan {public static ZDOMan instance;}
public class EnvMan {
    public static EnvMan instance;
    public bool m_debugTimeOfDay;
    public float m_debugTime;
    public string m_debugEnv;
}
public class ZDO {
    readonly HashSet<string> flags=new HashSet<string>();
    public void Set(string name,bool value){if(value)flags.Add(name);else flags.Remove(name);}
    public bool GetBool(string name)=>flags.Contains(name);
}
public static class ZDOVars {public const string s_piece="placed";}
public class ZNetView:UnityEngine.Component {
    public bool Valid=true,Owner=true;
    public ZDO Data=new ZDO();
    public bool IsValid()=>Valid&&!gameObject.Destroyed;
    public bool IsOwner()=>Owner;
    public ZDO GetZDO()=>Data;
}
public class Character:UnityEngine.Component {
    public static readonly List<Character> All=new List<Character>();
    public bool Player,Tame;
    public bool IsPlayer()=>Player;
    public bool IsTamed()=>Tame;
    public static List<Character> GetAllCharacters()=>All;
}
public class ItemDrop:UnityEngine.Component {
    static List<ItemDrop> s_instances=new List<ItemDrop>();
    public static List<ItemDrop> All=>s_instances;
}
public class ZNetScene {
    public static ZNetScene instance;
    public int Destroyed;
    public void Destroy(UnityEngine.GameObject obj) {
        obj.Destroyed=true;Destroyed++;
        Character.All.Remove(obj.GetComponent<Character>());ItemDrop.All.Remove(obj.GetComponent<ItemDrop>());
    }
}
// Native method signatures deliberately match the patched private entry points.
public class SpawnSystem { [MethodImpl(MethodImplOptions.NoInlining)] void UpdateSpawning(){} }
public class CreatureSpawner { [MethodImpl(MethodImplOptions.NoInlining)] void UpdateSpawner(){} }
public class SpawnArea { [MethodImpl(MethodImplOptions.NoInlining)] void UpdateSpawn(){} }
public class Hud {public void TogglePieceSelection(){}}
public class BuildUi {public void OpenBuildMenu(){}}
public class PieceTable {}
public class Player:Character {public static Player m_localPlayer;void SetPlaceMode(PieceTable buildPieces){}}
public class RandomEvent {}
public class RandEventSystem { [MethodImpl(MethodImplOptions.NoInlining)] void SetRandomEvent(RandomEvent ev,UnityEngineDummy.Vector3 pos){} }
namespace UnityEngineDummy {public struct Vector3 {}}
