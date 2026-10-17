using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Rachel350Z {
// Render-only masks. Native matrices are borrowed for ONE CustomUpdate call
// and restored by both Postfix and Finalizer. No terrain/paint/persistence APIs.
internal static class TransformerFootstepGrass {
    const int OwnerLimit=8,Slots=32,PrototypeLimit=32,InstanceLimit=1024,DrawLimit=4;
    sealed class OwnerMasks {
        public object Owner;
        public readonly Mask[] Masks=new Mask[Slots];
        public int Count;
    }
    struct Mask {
        public Vector3 Center,Forward,Right;
        public Vector2 Half;
        public float Strength,MinX,MaxX,MinZ,MaxZ;
    }
    struct Prototype {public Mesh Mesh;public Material Material;}
    internal sealed class DrawState {
        internal readonly int[] Indices=new int[InstanceLimit];
        internal readonly Matrix4x4[] Originals=new Matrix4x4[InstanceLimit];
        internal Matrix4x4[] Matrices;
        internal int Count;
        internal bool InUse;
    }
    static readonly OwnerMasks[] Owners=CreateOwners();
    static readonly DrawState[] Draws=CreateDraws();
    static readonly Prototype[] Prototypes=new Prototype[PrototypeLimit];
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly FieldInfo MatricesField=typeof(InstanceRenderer).GetField("m_instances",Flags);
    static readonly FieldInfo CountField=typeof(InstanceRenderer).GetField("m_instanceCount",Flags);
    static readonly FieldInfo DirtyField=typeof(InstanceRenderer).GetField("m_dirtyBounds",Flags);
    static readonly MethodInfo UpdateBounds=typeof(InstanceRenderer).GetMethod("UpdateBounds",Flags);
    static AccessTools.FieldRef<InstanceRenderer,Matrix4x4[]> readMatrices;
    static AccessTools.FieldRef<InstanceRenderer,int> readCount;
    static AccessTools.FieldRef<InstanceRenderer,bool> readDirty;
    static Action<InstanceRenderer> buildOriginalBounds;
    static ClutterSystem prototypeSource;
    static int prototypeCount,clutterCount=-1,totalMasks;
    static float nextPrototypeRefresh;
    static bool installed,failed;
    static Action<string> log;
    static OwnerMasks[] CreateOwners(){var result=new OwnerMasks[OwnerLimit];for(int i=0;i<result.Length;i++)result[i]=new OwnerMasks();return result;}
    static DrawState[] CreateDraws(){var result=new DrawState[DrawLimit];for(int i=0;i<result.Length;i++)result[i]=new DrawState();return result;}
    public static void Initialize(Harmony harmony,Action<string> report) {
        if(installed||failed)return;log=report;
        if(harmony==null||MatricesField?.FieldType!=typeof(Matrix4x4[])||CountField?.FieldType!=typeof(int)
            ||DirtyField?.FieldType!=typeof(bool)||UpdateBounds==null||UpdateBounds.ReturnType!=typeof(void)) {
            Disable("Native grass render API unavailable");return;
        }
        try {
            // Cached ref delegates avoid reflection boxing/Invoke allocations
            // in each render callback. Reflection is used only at initialization.
            readMatrices=AccessTools.FieldRefAccess<InstanceRenderer,Matrix4x4[]>(MatricesField);
            readCount=AccessTools.FieldRefAccess<InstanceRenderer,int>(CountField);
            readDirty=AccessTools.FieldRefAccess<InstanceRenderer,bool>(DirtyField);
            buildOriginalBounds=(Action<InstanceRenderer>)UpdateBounds.CreateDelegate(typeof(Action<InstanceRenderer>));
            harmony.PatchAll(typeof(NativeTransformerFootstepGrassDraw));installed=true;
        }
        catch(Exception error){Disable("Native grass masking unavailable: "+error.Message);}
    }
    public static void Set(object owner,int slot,Vector3 center,Vector3 forward,Vector2 size,float strength) {
        if(owner==null||slot<0||slot>=Slots||failed)return;
        if(!Finite(center)||!Finite(forward)||!Finite(size.x)||!Finite(size.y)||!Finite(strength)
            ||size.x<.1f||size.y<.1f||size.x>3||size.y>4||strength<=0){Remove(owner,slot);return;}
        forward.y=0;float magnitude=forward.magnitude;
        if(magnitude<.001f){Remove(owner,slot);return;}forward/=magnitude;
        OwnerMasks target=null;
        for(int i=0;i<Owners.Length;i++)if(ReferenceEquals(Owners[i].Owner,owner)){target=Owners[i];break;}
        if(target==null)for(int i=0;i<Owners.Length;i++)if(Owners[i].Owner==null){target=Owners[i];target.Owner=owner;break;}
        if(target==null)return;
        if(target.Masks[slot].Strength<=0){target.Count++;totalMasks++;}
        var right=new Vector3(forward.z,0,-forward.x);
        // Keep suppression slightly inside the authored soft-edged footprint.
        var half=new Vector2(size.x*.46f,size.y*.475f);
        float extentX=Mathf.Abs(right.x)*half.x+Mathf.Abs(forward.x)*half.y;
        float extentZ=Mathf.Abs(right.z)*half.x+Mathf.Abs(forward.z)*half.y;
        target.Masks[slot]=new Mask {Center=center,Forward=forward,Right=right,Half=half,Strength=Mathf.Clamp01(strength),
            MinX=center.x-extentX,MaxX=center.x+extentX,MinZ=center.z-extentZ,MaxZ=center.z+extentZ};
    }
    public static void Remove(object owner,int slot) {
        if(owner==null||slot<0||slot>=Slots)return;
        for(int i=0;i<Owners.Length;i++)if(ReferenceEquals(Owners[i].Owner,owner)) {
            var target=Owners[i];if(target.Masks[slot].Strength>0){target.Masks[slot]=default;target.Count--;totalMasks--;}
            if(target.Count==0)target.Owner=null;
            return;
        }
    }
    public static void RemoveOwner(object owner) {
        if(owner==null)return;
        for(int i=0;i<Owners.Length;i++)if(ReferenceEquals(Owners[i].Owner,owner)) {
            totalMasks-=Owners[i].Count;Owners[i].Count=0;Owners[i].Owner=null;Array.Clear(Owners[i].Masks,0,Slots);
        }
        if(totalMasks==0){RestoreAll();ClearPrototypes();}
    }
    public static void Shutdown() {
        RestoreAll();totalMasks=0;
        for(int i=0;i<Owners.Length;i++){Owners[i].Owner=null;Owners[i].Count=0;Array.Clear(Owners[i].Masks,0,Slots);}
        ClearPrototypes();log=null;installed=false;failed=false;
        // The owning controls Harmony unpatches this class during Plugin teardown.
    }
    static void ClearPrototypes(){prototypeSource=null;prototypeCount=0;clutterCount=-1;nextPrototypeRefresh=0;Array.Clear(Prototypes,0,Prototypes.Length);}
    static void Disable(string reason){failed=true;RestoreAll();log?.Invoke(reason+"; temporary dirt prints and dust remain enabled.");}
    static bool GrassName(string name) {
        if(string.IsNullOrEmpty(name)||name.IndexOf("grass",StringComparison.OrdinalIgnoreCase)<0)return false;
        foreach(string denied in DeniedNames)if(name.IndexOf(denied,StringComparison.OrdinalIgnoreCase)>=0)return false;
        return true;
    }
    static bool DeniedName(string name) {
        if(string.IsNullOrEmpty(name))return false;
        foreach(string denied in DeniedNames)if(name.IndexOf(denied,StringComparison.OrdinalIgnoreCase)>=0)return true;
        return false;
    }
    static readonly string[] DeniedNames={"bush","shrub","fern","flower","stone","rock","tree","reed"};
    static void RefreshPrototypes(ClutterSystem clutter) {
        var entries=clutter.m_clutter;
        if(entries==null)return;
        if(prototypeSource==clutter&&clutterCount==entries.Count&&Time.unscaledTime<nextPrototypeRefresh)return;
        prototypeSource=clutter;clutterCount=entries.Count;prototypeCount=0;nextPrototypeRefresh=Time.unscaledTime+5;
        Array.Clear(Prototypes,0,Prototypes.Length);
        for(int i=0;i<entries.Count&&prototypeCount<PrototypeLimit;i++) {
            var entry=entries[i];
            if(entry==null||!entry.m_enabled||!entry.m_instanced||entry.m_prefab==null)continue;
            // An affirmative prefab/entry identity is required: a generic wind
            // shader or shared texture must never classify rocks/shrubs as grass.
            if(DeniedName(entry.m_name)||DeniedName(entry.m_prefab.name)
                ||(!GrassName(entry.m_name)&&!GrassName(entry.m_prefab.name)))continue;
            var renderer=entry.m_prefab.GetComponent<InstanceRenderer>();
            if(renderer==null||renderer.m_mesh==null||renderer.m_material==null||DeniedName(renderer.m_material.name))continue;
            Prototypes[prototypeCount++]=new Prototype{Mesh=renderer.m_mesh,Material=renderer.m_material};
        }
    }
    static bool MatchingPrototype(InstanceRenderer renderer) {
        for(int i=0;i<prototypeCount;i++)if(renderer.m_mesh==Prototypes[i].Mesh&&renderer.m_material==Prototypes[i].Material)return true;
        return false;
    }
    static bool PatchNearMasks(Vector3 center,float halfPatch) {
        for(int i=0;i<Owners.Length;i++)if(Owners[i].Count>0)for(int j=0;j<Slots;j++) {
            var mask=Owners[i].Masks[j];if(mask.Strength<=0)continue;
            if(center.x+halfPatch>=mask.MinX&&center.x-halfPatch<=mask.MaxX
                &&center.z+halfPatch>=mask.MinZ&&center.z-halfPatch<=mask.MaxZ)return true;
        }
        return false;
    }
    static float ScaleAt(Vector3 point) {
        float suppression=0;
        for(int i=0;i<Owners.Length;i++)if(Owners[i].Count>0)for(int j=0;j<Slots;j++) {
            var mask=Owners[i].Masks[j];if(mask.Strength<=0||Mathf.Abs(point.y-mask.Center.y)>1.5f
                ||point.x<mask.MinX||point.x>mask.MaxX||point.z<mask.MinZ||point.z>mask.MaxZ)continue;
            float x=point.x-mask.Center.x,z=point.z-mask.Center.z;
            float localX=Mathf.Abs(x*mask.Right.x+z*mask.Right.z)/mask.Half.x;
            float localZ=Mathf.Abs(x*mask.Forward.x+z*mask.Forward.z)/mask.Half.y;
            float edge=Mathf.Clamp01((1-Mathf.Max(localX,localZ))/.16f);
            suppression=Mathf.Max(suppression,mask.Strength*edge);
        }
        return 1-suppression; // Overlaps take maximum, never multiply/double-flatten.
    }
    internal static DrawState Begin(InstanceRenderer renderer) {
        if(!installed||failed||totalMasks==0||renderer==null)return null;
        DrawState state=null;
        try {
            var clutter=ClutterSystem.instance;
            if(clutter==null||!renderer.transform.IsChildOf(clutter.transform))return null;
            float halfPatch=clutter.m_grassPatchSize*.5f;
            if(!Finite(halfPatch)||halfPatch<=0||halfPatch>32||!PatchNearMasks(renderer.transform.position,halfPatch))return null;
            RefreshPrototypes(clutter);if(!MatchingPrototype(renderer))return null;
            var matrices=readMatrices(renderer);
            int count=readCount(renderer);
            if(matrices==null||count<1||count>InstanceLimit||count>matrices.Length)return null;
            // A reentrant draw of the same borrowed array must not capture its
            // already scaled contents as another 'original'. It inherits masks.
            for(int i=0;i<Draws.Length;i++)if(Draws[i].InUse&&ReferenceEquals(Draws[i].Matrices,matrices))return null;
            for(int i=0;i<Draws.Length;i++)if(!Draws[i].InUse){state=Draws[i];break;}
            if(state==null)return null;
            // Build conservative native bounds from ORIGINAL geometry first.
            // Drawing zero-scaled instances must not cache shrunken LOD bounds.
            if(readDirty(renderer))buildOriginalBounds(renderer);
            state.InUse=true;state.Matrices=matrices;state.Count=0;
            for(int i=0;i<count;i++) {
                var original=matrices[i];float scale=ScaleAt(new Vector3(original.m03,original.m13,original.m23));
                if(scale>=.999f)continue;
                state.Indices[state.Count]=i;state.Originals[state.Count]=original;state.Count++;
                // Keep world translation and homogeneous row. A zero matrix
                // would send geometry to the world origin with invalid w.
                var masked=original;
                masked.m00*=scale;masked.m01*=scale;masked.m02*=scale;
                masked.m10*=scale;masked.m11*=scale;masked.m12*=scale;
                masked.m20*=scale;masked.m21*=scale;masked.m22*=scale;
                matrices[i]=masked;
            }
            return state;
        }catch(Exception error){Restore(state);Disable("Grass render masking disabled: "+error.Message);return null;}
    }
    internal static void Restore(DrawState state) {
        if(state==null||!state.InUse)return;
        var matrices=state.Matrices;
        for(int i=state.Count-1;i>=0;i--) {
            int index=state.Indices[i];if(matrices!=null&&index>=0&&index<matrices.Length)matrices[index]=state.Originals[i];
        }
        state.Count=0;state.Matrices=null;state.InUse=false;
    }
    static void RestoreAll(){for(int i=0;i<Draws.Length;i++)Restore(Draws[i]);}
    static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
[HarmonyPatch(typeof(InstanceRenderer),nameof(InstanceRenderer.CustomUpdate))]
internal static class NativeTransformerFootstepGrassDraw {
    static void Prefix(InstanceRenderer __instance,out TransformerFootstepGrass.DrawState __state){__state=TransformerFootstepGrass.Begin(__instance);}
    static void Postfix(TransformerFootstepGrass.DrawState __state){TransformerFootstepGrass.Restore(__state);}
    static Exception Finalizer(Exception __exception,TransformerFootstepGrass.DrawState __state){TransformerFootstepGrass.Restore(__state);return __exception;}
}
}
