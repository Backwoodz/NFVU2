using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace UnityEngine {
    public class Object {
        public string name;public bool Destroyed;
        public static void Destroy(Object value){if(value==null)return;value.Destroyed=true;if(value is GameObject go)foreach(var source in go.Sources)source.Destroyed=true;}
    }
    public class Transform {public GameObject gameObject;}
    public class GameObject:Object {
        public readonly Transform transform;public readonly List<AudioSource> Sources=new List<AudioSource>();
        public GameObject(){transform=new Transform{gameObject=this};}
        public T AddComponent<T>()where T:new(){var result=new T();if(result is AudioSource source)Sources.Add(source);return result;}
    }
    public class AudioClip:Object {
        public static readonly List<AudioClip> All=new List<AudioClip>();public static bool FailNextUpload;
        public int samples,channels,frequency;public float[] Uploaded;
        public static AudioClip Create(string name,int samples,int channels,int frequency,bool stream){var clip=new AudioClip{name=name,samples=samples,channels=channels,frequency=frequency};All.Add(clip);return clip;}
        public bool SetData(float[] data,int offset){if(FailNextUpload){FailNextUpload=false;return false;}Uploaded=(float[])data.Clone();return true;}
    }
    public enum AudioRolloffMode {Linear}
    public class AudioSource:Object {
        public bool playOnAwake,loop,isPlaying;public float volume,pitch=1,spatialBlend,minDistance,maxDistance,dopplerLevel;
        public int priority,Starts,Stops;public AudioClip clip;public AudioRolloffMode rolloffMode;public Audio.AudioMixerGroup outputAudioMixerGroup;
        public void Play(){if(clip==null)throw new Exception("missing clip");isPlaying=true;Starts++;}
        public void Stop(){isPlaying=false;Stops++;}
        public void Finish(){isPlaying=false;}
    }
    public static class Resources {public static Audio.AudioMixerGroup Sfx=new Audio.AudioMixerGroup{name="SFX"};public static T[] FindObjectsOfTypeAll<T>()=>new[]{(T)(object)Sfx};}
}
namespace UnityEngine.Audio {public class AudioMixerGroup:UnityEngine.Object {}}
namespace Rachel350Z {
    public sealed class SpawnAudio {
        public readonly Dictionary<string,WavData> Data=new Dictionary<string,WavData>();
        public SpawnAudio(string folder){foreach(var path in Directory.GetFiles(folder,"*.wav"))using(var stream=File.OpenRead(path))Data[Path.GetFileNameWithoutExtension(path)]=WavPcm16.Read(stream,15);}
        public WavData FileData(string path){if(!Data.TryGetValue(Path.GetFileNameWithoutExtension(path),out var data))throw new InvalidDataException("missing optional sample");return data;}
    }
    public sealed partial class TransformerMinigun:IDisposable {
        readonly UnityEngine.Transform muzzle=new UnityEngine.GameObject().transform;
        bool disposed;
        public void LoadForTest(string folder,SpawnAudio data,Action<string> log){foreach(var step in LoadAudio(folder,data,log)){} }
        public void TickForTest(bool active,bool firing,float spin,bool reload=false,float volume=1){if(!disposed)UpdateAudio(active,firing,spin,reload,volume);}
        public UnityEngine.AudioSource Pitched=>motorLoop;
        public UnityEngine.AudioSource Unpitched=>motorUnpitchedLoop;
        public UnityEngine.AudioSource MotorAction=>motorAction;
        public UnityEngine.AudioSource FireLoop=>fireLoop;
        public UnityEngine.AudioSource FireAction=>fireAction;
        public UnityEngine.AudioSource MechLoop=>mechLoop;
        public UnityEngine.AudioSource LfeLoop=>lfeLoop;
        public UnityEngine.AudioSource ReloadAction=>reloadAction;
        public UnityEngine.AudioSource[] Voices=>muzzle.gameObject.Sources.ToArray();
        public UnityEngine.AudioClip Clip(string name)=>audioClips.TryGetValue(name,out var clip)?clip:null;
        public void Dispose(){if(disposed)return;disposed=true;UpdateAudio(false,false,0,false,0);DisposeAudio();UnityEngine.Object.Destroy(muzzle.gameObject);}
    }
}
