using UnityEngine;

namespace Rachel350Z {
public sealed class RadioBassTap : MonoBehaviour {
    readonly BassSignal signal=new BassSignal();
    public volatile int SampleRate=48000,Sequence;
    public volatile float Energy;
    public volatile bool ResetRequested;
    // Unity audio thread: no Unity API calls, allocations, locks or sample modifications.
    void OnAudioFilterRead(float[] data,int channels) {
        if(ResetRequested){signal.Reset();ResetRequested=false;}
        Energy=signal.Read(data,channels,SampleRate);Sequence++;
    }
}
}
