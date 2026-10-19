using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
    public class Object {
        public string name;public bool Destroyed;
        public static void Destroy(Object value){value.Destroyed=true;if(value is GameObject go)foreach(var c in go.Components)c.Destroyed=true;}
    }
    public class GameObject:Object {
        public static readonly List<GameObject> All=new List<GameObject>();
        public readonly Transform transform=new Transform();public readonly List<Object> Components=new List<Object>();
        public GameObject(string name){this.name=name;All.Add(this);}
        public T AddComponent<T>() where T:Component,new(){
            // Native Unity rejects built-in audio filters until their object
            // owns an AudioSource/AudioListener. Our old double missed this.
            if((typeof(T)==typeof(AudioHighPassFilter)||typeof(T)==typeof(AudioLowPassFilter))&&!Components.Exists(c=>c is AudioSource))
                throw new InvalidOperationException("Add AudioSource before its filter");
            var value=new T{transform=transform};Components.Add(value);return value;
        }
    }
    public class Component:Object {public Transform transform=new Transform();}
    public class AudioHighPassFilter:Component {public float cutoffFrequency,highpassResonanceQ;}
    public class AudioLowPassFilter:Component {public float cutoffFrequency,lowpassResonanceQ;}
    public class Camera:Component {public static Camera main;}
    public class Transform {public Vector3 position,localPosition;public Vector3 forward=new Vector3(0,0,1);public Transform parent;public void SetParent(Transform parent,bool worldPositionStays){this.parent=parent;}}
    public struct Vector3 {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    }
    public class Rigidbody:Component {public Vector3 linearVelocity;}
    public struct WheelHit {public float sidewaysSlip,forwardSlip;}
    public class WheelCollider:Component {
        public bool enabled=true,Grounded=true;public float rpm;public WheelHit Hit;
        public bool GetGroundHit(out WheelHit hit){hit=Hit;return Grounded;}
    }
    public enum AudioRolloffMode {Linear}
    public class AudioClip:Object {
        public static readonly List<AudioClip> All=new List<AudioClip>();
        public static AudioClip Create(string name,int count,int channels,int rate,bool stream){var c=new AudioClip{name=name};All.Add(c);return c;}
        public bool SetData(float[] data,int offset)=>true;
    }
    public class AudioSource:Component {
        public static readonly List<AudioSource> All=new List<AudioSource>();
        public AudioSource(){All.Add(this);}
        public AudioClip clip;public bool playOnAwake,loop,Playing,Paused;public int priority,Starts,Stops,Pauses,UnPauses;
        public float volume,pitch,spatialBlend,minDistance,maxDistance,dopplerLevel,spread;
        public AudioRolloffMode rolloffMode;public Audio.AudioMixerGroup outputAudioMixerGroup;
        // Unity resolves voice priority before comparing audibility. Silent
        // high-priority loops can therefore crowd out lower-priority effects.
        public bool isVirtual=>!Playing||!All.Where(s=>s.Playing).OrderBy(s=>s.priority).ThenByDescending(s=>s.volume).Take(32).Contains(this);
        public void Play(){Playing=true;Paused=false;Starts++;}
        public double ScheduledAt;
        public void PlayScheduled(double time){ScheduledAt=time;Play();}
        public void Stop(){Playing=Paused=false;Stops++;}
        public void Pause(){Paused=Playing;Playing=false;Pauses++;}
        public void UnPause(){if(Paused){Playing=true;Paused=false;}UnPauses++;}
    }
    public static class Resources {public static T[] FindObjectsOfTypeAll<T>()=>new T[0];}
    public static class Time {public static float unscaledDeltaTime=.02f;}
    public static class AudioSettings {public static double dspTime=100;}
    public static class Mathf {
        public static float Clamp(float value,float min,float max)=>Math.Max(min,Math.Min(max,value));
        public static float Clamp01(float value)=>Clamp(value,0,1);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Abs(float value)=>Math.Abs(value);
        public static float Sqrt(float value)=>(float)Math.Sqrt(value);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float InverseLerp(float a,float b,float value)=>a==b?0:Clamp01((value-a)/(b-a));
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*(3*t*t-2*t*t*t);}
        public static float MoveTowards(float a,float b,float step)=>Math.Abs(b-a)<=step?b:a+Math.Sign(b-a)*step;
    }
}
namespace UnityEngine.Audio {public class AudioMixerGroup:UnityEngine.Object {}}
namespace Rachel350Z {
    // All decoding has already completed when the adapter receives SpawnAudio.
    public class SpawnAudio {
        readonly WavData sample=new WavData{Channels=1,SampleRate=8000,Samples=new float[800]};
        public WavData Reverse=>sample;
        public WavData Embedded(string name)=>sample;
        public WavData FileData(string path)=>sample;
    }
}
