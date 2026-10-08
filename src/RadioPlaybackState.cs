using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Rachel350Z {
public sealed class RadioTrack {
    public string Path {get;private set;}
    public string Title {get;private set;}
    public RadioTrack(string path,string title){Path=path;Title=title;}
}
public static class RadioPlaylist {
    public static RadioTrack[] Read(string folder,Action<string> log) {
        var tracks=new List<RadioTrack>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if(!Directory.Exists(folder))return tracks.ToArray();
        bool manifestValid=false;
        string manifest=System.IO.Path.Combine(folder,"tracks.json");
        if(File.Exists(manifest))try {
            foreach(var entry in JArray.Parse(File.ReadAllText(manifest))) {
                string file=(string)entry["file"],title=(string)entry["title"];
                if(string.IsNullOrEmpty(file)||file!=System.IO.Path.GetFileName(file)||file.IndexOfAny(new[]{'/', '\\', ':'})>=0)continue;
                Add(System.IO.Path.Combine(folder,file),title,tracks,seen);
            }
            manifestValid=true;
        }catch(Exception e){log("350Z radio manifest ignored: "+e.Message);}
        if(!manifestValid)foreach(string path in Directory.GetFiles(folder).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))Add(path,null,tracks,seen);
        return tracks.ToArray();
    }
    static void Add(string path,string title,List<RadioTrack> tracks,HashSet<string> seen) {
        string ext=System.IO.Path.GetExtension(path).ToLowerInvariant();
        if((ext!=".wav"&&ext!=".ogg")||!File.Exists(path)||!seen.Add(System.IO.Path.GetFullPath(path)))return;
        tracks.Add(new RadioTrack(path,string.IsNullOrWhiteSpace(title)?System.IO.Path.GetFileNameWithoutExtension(path):title));
    }
}
// No Unity dependencies: selection, failed-track recovery and async load identity.
public sealed class RadioPlaybackState {
    readonly bool[] failed;
    public int Count { get { return failed.Length; } }
    public int Index { get; private set; }
    public int Revision { get; private set; }
    public bool Paused { get; private set; }
    public bool HasPlayable { get { return Index >= 0; } }
    public RadioPlaybackState(int count) {
        if(count<0)throw new ArgumentOutOfRangeException(nameof(count));
        failed=new bool[count];Index=count>0?0:-1;
    }
    public bool Select(int index) {
        if(index<0||index>=Count)return false;
        failed[index]=false;Index=index;Revision++;return true;
    }
    public bool Move(int direction) {
        if(Count==0)return false;
        int start=Index<0?0:Index;
        for(int step=1;step<=Count;step++) {
            int candidate=((start+(direction<0?-step:step))%Count+Count)%Count;
            if(!failed[candidate]) {Index=candidate;Revision++;return true;}
        }
        Index=-1;Revision++;return false;
    }
    public bool MarkFailed(int revision) {
        if(revision!=Revision||Index<0)return false;
        failed[Index]=true;Move(1);return true;
    }
    public bool IsCurrent(int revision) {return revision==Revision&&Index>=0;}
    public void TogglePause(){Paused=!Paused;}
}
}
