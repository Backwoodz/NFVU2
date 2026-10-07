using System;
using System.IO;

namespace Rachel350Z {
public sealed class WavData {
    public int SampleRate,Channels;
    public float[] Samples;
}
public static class WavPcm16 {
    // Read the bounded PCM16 resources bundled with this plugin, without a
    // web request or dependence on platform audio-file decoders.
    public static WavData Read(Stream stream) {
        if(stream==null)throw new InvalidDataException("Missing embedded audio resource");
        using(var r=new BinaryReader(stream)) {
            if(!stream.CanSeek || stream.Length<44 || stream.Length>16000000)throw new InvalidDataException("Invalid WAV size");
            if(r.ReadUInt32()!=0x46464952)throw new InvalidDataException("Missing RIFF header");
            long end=8L+r.ReadUInt32();
            if(end>stream.Length || end<44 || r.ReadUInt32()!=0x45564157)throw new InvalidDataException("Invalid WAVE header");
            int channels=0,rate=0,align=0;byte[] pcm=null;
            while(stream.Position+8<=end) {
                uint id=r.ReadUInt32(),size=r.ReadUInt32();long next=stream.Position+size+(size&1);
                if(next>end)throw new InvalidDataException("Truncated WAV chunk");
                if(id==0x20746d66) {
                    if(size<16 || r.ReadUInt16()!=1)throw new InvalidDataException("Expected PCM audio");
                    channels=r.ReadUInt16();rate=r.ReadInt32();int bytesPerSecond=r.ReadInt32();align=r.ReadUInt16();int bits=r.ReadUInt16();
                    if(channels!=1 || rate<8000 || rate>96000 || bits!=16 || align!=channels*2 || bytesPerSecond!=rate*align)
                        throw new InvalidDataException("Expected mono 16-bit WAV");
                } else if(id==0x61746164) {
                    if(pcm!=null)throw new InvalidDataException("Duplicate WAV data");
                    pcm=r.ReadBytes(checked((int)size));
                }
                stream.Position=next;
            }
            if(rate==0 || pcm==null || pcm.Length<align || pcm.Length%align!=0 || pcm.Length>rate*align*15)
                throw new InvalidDataException("Invalid WAV samples");
            var samples=new float[pcm.Length/2];
            for(int i=0;i<samples.Length;i++)samples[i]=(short)(pcm[2*i]|pcm[2*i+1]<<8)/32768f;
            return new WavData{SampleRate=rate,Channels=channels,Samples=samples};
        }
    }
}
}
