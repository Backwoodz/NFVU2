namespace UnityEngine {
    public static class Time {public static float deltaTime=.02f;}
    public class GameObject {public bool activeInHierarchy=true;}
}
public class Player {
    public static Player m_localPlayer;
    public bool enabled,dead,teleporting,owner=true;
    public readonly UnityEngine.GameObject gameObject=new UnityEngine.GameObject();
    public int discoveryCalls,fullUpdates;
    public float discoveryTime;
    public bool IsOwner()=>owner;
    public bool IsDead()=>dead;
    public bool IsTeleporting()=>teleporting;
    private void UpdateBiome(float dt){discoveryCalls++;discoveryTime+=dt;}
    private void Update(){fullUpdates++;}
    private void FixedUpdate(){fullUpdates++;}
}
public class WorldGenerator {public static WorldGenerator instance=new WorldGenerator();}
public class Minimap {public static Minimap instance=new Minimap();}
public class MessageHud {public static MessageHud instance=new MessageHud();}
