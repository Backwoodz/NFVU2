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
        public int samples,frequency;
        public static AudioClip Create(string name,int count,int channels,int rate,bool stream){var c=new AudioClip{name=name,samples=count,frequency=rate};All.Add(c);return c;}
        public bool SetData(float[] data,int offset){if(FailNextUpload){FailNextUpload=false;return false;}return true;}
    }
    // Audio samples advance independently from the game clock, as Unity audio
    // would during a frame hitch or a paused Time.timeScale without Pause().
    public class AudioSource:Component {
        public static readonly List<AudioSource> All=new List<AudioSource>();
        public static bool FailNextPlay;
        public AudioSource(){All.Add(this);}
        public AudioClip clip;public bool playOnAwake,loop,isPlaying,Paused;public int priority,Starts,Stops,Pauses,UnPauses,Seeks;
        public float volume,pitch,spatialBlend,minDistance,maxDistance,dopplerLevel,spread;
        public AudioRolloffMode rolloffMode;public Audio.AudioMixerGroup outputAudioMixerGroup;
        int sample;
        public int timeSamples {get=>sample;set{if(clip==null||value<0||value>=clip.samples)throw new Exception("bad seek");sample=value;Seeks++;}}
        public void Play(){if(FailNextPlay){FailNextPlay=false;throw new Exception("simulated Unity play failure");}isPlaying=true;Paused=false;sample=0;Starts++;}
        public void Stop(){isPlaying=Paused=false;sample=0;Stops++;}
        public void Pause(){if(isPlaying){isPlaying=false;Paused=true;}Pauses++;}
        public void UnPause(){if(Paused){isPlaying=true;Paused=false;}UnPauses++;}
        public void Advance(float seconds){if(!isPlaying||clip==null)return;sample+=(int)Math.Round(seconds*clip.frequency);if(sample>=clip.samples){sample=clip.samples-1;isPlaying=false;}}
    }
    public static class Resources {public static T[] FindObjectsOfTypeAll<T>()=>new T[0];}
}
namespace UnityEngine.Audio {public class AudioMixerGroup:UnityEngine.Object {}}
