using System;
using System.Collections.Generic;
namespace UnityEngine {
    public class Object {
        public string name;public bool Destroyed;
        public static void Destroy(Object value){value.Destroyed=true;if(value is GameObject go)foreach(var c in go.Components)c.Destroyed=true;}
    }
    public class GameObject:Object {
        public readonly Transform transform=new Transform();public readonly List<Object> Components=new List<Object>();
        public GameObject(string name){this.name=name;}
        public T AddComponent<T>() where T:Component,new(){var value=new T{transform=transform};Components.Add(value);return value;}
    }
    public class Component:Object {public Transform transform;}
    public class Transform {public Vector3 position;public Transform parent;public void SetParent(Transform parent,bool worldPositionStays){this.parent=parent;}}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public enum AudioRolloffMode {Linear}
    public class AudioClip:Object {
        public static readonly List<AudioClip> All=new List<AudioClip>();public static bool FailNextUpload;
        public static AudioClip Create(string name,int count,int channels,int rate,bool stream){var c=new AudioClip{name=name};All.Add(c);return c;}
        public bool SetData(float[] data,int offset){if(FailNextUpload){FailNextUpload=false;return false;}return true;}
    }
    public class AudioSource:Component {
        public static readonly List<AudioSource> All=new List<AudioSource>();
        public AudioSource(){All.Add(this);}
        public AudioClip clip;public bool playOnAwake,loop,Playing;public int priority,Starts,Stops;
        public float volume,pitch,spatialBlend,minDistance,maxDistance,dopplerLevel,spread;
        public AudioRolloffMode rolloffMode;public Audio.AudioMixerGroup outputAudioMixerGroup;
        public void Play(){Playing=true;Starts++;}public void Stop(){Playing=false;Stops++;}
    }
    public static class Resources {public static T[] FindObjectsOfTypeAll<T>()=>new T[0];}
    public static class Mathf {public static float Clamp01(float value)=>Math.Max(0,Math.Min(1,value));}
}
namespace UnityEngine.Audio {public class AudioMixerGroup:UnityEngine.Object {}}
