using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
namespace Rachel350Z {
// Managed-only reader, also exercised outside Unity. No runtime type activation.
public sealed class TransformerNode {public string name;public int parent;public float[] neutral;}
public sealed class TransformerRelease {public int node;public float[] offset;}
public sealed class TransformerMaterial {public string name,sourcePart;public int source,neonGroup;public float[] color,emission;public float metallic,smoothness;}
public sealed class TransformerManifest {
    public int schema,fps,frames,batches,vertices,gaitVersion;public float duration;
    public bool walkingReady;
    public string bodySha256,blendSha256,payloadSha256;
    public TransformerNode[] nodes;public TransformerMaterial[] materials;public string[] representedParts;
    public TransformerRelease[] gaitReleases;
}
public sealed class TransformerBatch {
    public int Material;public bool Concealed;public int[] Bones,Indices,Weights;public float[] Vertices;
}
public sealed class TransformerAsset {
    public TransformerManifest Manifest;public float[][] Frames;public TransformerBatch[] Batches;
    public static string Hash(string path) {using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    static int Count(BinaryReader r,int min,int max) {int n=r.ReadInt32();if(n<min||n>max)throw new InvalidDataException("Invalid transformer array size");return n;}
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v)&&Math.Abs(v)<100000;
    static float Number(BinaryReader r){float v=r.ReadSingle();if(!Finite(v))throw new InvalidDataException("Non-finite transformer coordinate");return v;}
    public static TransformerAsset Load(string folder,string bodyPath) {
        var json=Path.Combine(folder,"transformer.json");var binary=Path.Combine(folder,"transformer.bin");
        if(new FileInfo(json).Length>2*1024*1024||new FileInfo(binary).Length>128*1024*1024)throw new InvalidDataException("Transformer asset too large");
        TransformerManifest m;
        using(var stream=File.OpenText(json))using(var reader=new JsonTextReader(stream))
            m=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=16,CheckAdditionalContent=true}).Deserialize<TransformerManifest>(reader);
        if(m==null||m.schema!=1||m.nodes==null||m.nodes.Length<1||m.nodes.Length>2048||m.materials==null||m.materials.Length<1||m.materials.Length>256||m.representedParts==null||m.representedParts.Length>320||m.fps!=48||m.frames!=241||m.duration!=5||m.batches<1||m.batches>512||m.vertices<3||m.vertices>2000000)
            throw new InvalidDataException("Unsupported transformer manifest");
        if(Hash(bodyPath)!=m.bodySha256||Hash(binary)!=m.payloadSha256)throw new InvalidDataException("Transformer asset does not match the vehicle or payload checksum");
        if(m.walkingReady&&m.gaitVersion!=TransformerGait.Version)throw new InvalidDataException("Unreviewed transformer gait version");
        for(int i=0;i<m.nodes.Length;i++) {
            var n=m.nodes[i];if(n==null||string.IsNullOrEmpty(n.name)||n.name.Length>256||n.parent< -1||n.parent>=i||n.neutral==null||n.neutral.Length!=16||n.neutral.Any(v=>!Finite(v)))throw new InvalidDataException("Invalid transformer node");
        }
        if(m.nodes.Select(n=>n.name).Distinct().Count()!=m.nodes.Length)throw new InvalidDataException("Duplicate transformer node");
        if(m.gaitReleases==null||m.gaitReleases.Length>100)throw new InvalidDataException("Missing gait releases");
        foreach(var release in m.gaitReleases)
            if(release==null||release.node<0||release.node>=m.nodes.Length||!(m.nodes[release.node].name.StartsWith("panel.")||m.nodes[release.node].name=="neon.trunk rear")||release.offset==null||release.offset.Length!=3||release.offset.Any(v=>!Finite(v)||Math.Abs(v)>.5f))throw new InvalidDataException("Invalid gait release");
        if(m.gaitReleases.Select(r=>r.node).Distinct().Count()!=m.gaitReleases.Length)throw new InvalidDataException("Duplicate gait release");
        foreach(var a in m.materials)if(a==null||a.source< -1||a.source>255||a.color==null||a.color.Length!=4||a.emission==null||a.emission.Length!=4||a.color.Concat(a.emission).Any(v=>!Finite(v))||!Finite(a.metallic)||!Finite(a.smoothness))throw new InvalidDataException("Invalid transformer material");
        var result=new TransformerAsset{Manifest=m};
        using(var r=new BinaryReader(File.OpenRead(binary))) {
            if(System.Text.Encoding.ASCII.GetString(r.ReadBytes(8))!="NFVT0001")throw new InvalidDataException("Unknown transformer binary");
            int nodes=Count(r,1,2048),frames=Count(r,2,512),batches=Count(r,1,512);
            if(nodes!=m.nodes.Length||frames!=m.frames||batches!=m.batches)throw new InvalidDataException("Transformer header mismatch");
            result.Frames=new float[frames][];
            for(int f=0;f<frames;f++) {
                var row=new float[nodes*10];result.Frames[f]=row;
                for(int i=0;i<row.Length;i++)row[i]=Number(r);
                for(int n=0;n<nodes;n++) {
                    int k=n*10;float norm=0;for(int j=3;j<7;j++)norm+=row[k+j]*row[k+j];
                    if(Math.Abs(norm-1)>.002f||row[k+7]<=0||row[k+8]<=0||row[k+9]<=0)throw new InvalidDataException("Invalid transformer rotation/scale");
                }
            }
            result.Batches=new TransformerBatch[batches];int total=0;
            for(int i=0;i<batches;i++) {
                var b=new TransformerBatch{Material=Count(r,0,m.materials.Length-1),Concealed=Count(r,0,1)!=0};result.Batches[i]=b;
                int count=Count(r,1,48);b.Bones=new int[count];for(int j=0;j<count;j++)b.Bones[j]=Count(r,0,nodes-1);
                int vertices=Count(r,3,60000),triangles=Count(r,3,360000);
                if(triangles%3!=0||(total+=vertices)>m.vertices)throw new InvalidDataException("Invalid transformer geometry budget");
                b.Vertices=new float[vertices*8];b.Weights=new int[vertices];
                for(int v=0;v<vertices;v++){for(int j=0;j<8;j++)b.Vertices[v*8+j]=Number(r);b.Weights[v]=Count(r,0,count-1);}
                b.Indices=new int[triangles];for(int j=0;j<triangles;j++)b.Indices[j]=Count(r,0,vertices-1);
            }
            if(total!=m.vertices||r.BaseStream.Position!=r.BaseStream.Length)throw new InvalidDataException("Transformer payload size mismatch");
        }
        return result;
    }
}
// Time advances only while the world/input context permits it. Requests during
// a transition are ignored, and the same samples implement both directions.
public sealed class TransformerState {
    public float Progress {get;private set;}
    public bool Active {get;private set;}
    public bool Returning {get;private set;}
    public bool CanWalk=>Active&&!Returning&&Progress>=1;
    public bool Begin(bool occupied,bool parked,bool grounded,bool clear) {
        if(Active||!occupied||!parked||!grounded||!clear)return false;
        Active=true;Returning=false;Progress=0;return true;
    }
    public bool Return(){if(!CanWalk)return false;Returning=true;return true;}
    public void Step(float dt,bool blocked) {
        if(!Active||blocked||float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
        Progress=Math.Max(0,Math.Min(1,Progress+(Returning?-1:1)*Math.Min(dt,.1f)/5));
        if(Returning&&Progress==0)Reset();
    }
    public void Reset(){Active=false;Returning=false;Progress=0;}
}
}
