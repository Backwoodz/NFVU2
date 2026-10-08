using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Rachel350Z {
// Verified against the installed assembly_valheim MusicMan IL: instance and
// m_musicSource exist; UpdateMusic rewrites source.volume but never source.mute.
// Mute only that source, so radio routed through the Music mixer stays audible.
// No persistent settings or shared mixer parameters are changed.
public sealed class ValheimMusicDucking : IDisposable {
    readonly Action<string> log;
    PropertyInfo instance;
    FieldInfo sourceField;
    AudioSource source;
    bool previousMute,failed,disposed;
    public ValheimMusicDucking(Action<string> logger) {
        log=logger;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("MusicMan",false)).FirstOrDefault(t=>t!=null);
        instance=type?.GetProperty("instance",BindingFlags.Public|BindingFlags.Static);
        sourceField=type?.GetField("m_musicSource",BindingFlags.NonPublic|BindingFlags.Instance);
        if(instance==null||sourceField==null||sourceField.FieldType!=typeof(AudioSource)) {
            failed=true;log?.Invoke("350Z radio: verified MusicMan API unavailable; automatic Valheim music muting disabled.");
        }
    }
    public void Tick(bool radioAudible) {
        if(disposed)return;
        if(!radioAudible){Restore();return;}
        if(failed)return;
        try {
            var manager=instance.GetValue(null,null) as UnityEngine.Object;
            var current=manager!=null?sourceField.GetValue(manager) as AudioSource:null;
            if(current!=source) {
                Restore();source=current;
                if(source!=null)previousMute=source.mute;
            }
            if(source!=null)source.mute=true;
        }catch(Exception e) {
            Restore();failed=true;
            log?.Invoke("350Z radio: Valheim music muting unavailable: "+e.Message);
        }
    }
    void Restore() {
        if(source!=null)source.mute=previousMute;
        source=null;
    }
    public void Dispose() {
        if(disposed)return;
        Restore();disposed=true;
    }
}
}
