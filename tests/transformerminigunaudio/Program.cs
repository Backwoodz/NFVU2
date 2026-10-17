using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.00001f;
    static float Peak(float[] data)=>data.Max(v=>Math.Abs(v));
    static double Rms(float[] data)=>Math.Sqrt(data.Sum(v=>(double)v*v)/data.Length);
    static string Hash(string path){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
    static void Model() {
        var original=new[]{-.20f,.10f,.05f};var prepared=TransformerMinigunSoundLevels.Prepare("spin_start",original,out float peak);
        Check(!ReferenceEquals(prepared,original)&&original[0]==-.20f&&Near(prepared[0],original[0]*TransformerMinigunSoundLevels.MotorBoost),"motor upload adds exactly twelve dB without editing worker cached PCM");
        Check(peak<.97f&&Near(peak,Peak(prepared)),"ordinary motor boost preserves waveform and measured peak headroom");
        prepared=TransformerMinigunSoundLevels.Prepare("spin_unpitched_loop",new[]{-.4f,.8f,.2f},out peak);
        Check(peak<=.97f&&Near(prepared[1]/prepared[0],-2)&&Near(prepared[2]/prepared[1],.25f),"high motor peak uses one constant bounded gain rather than waveform clipping");
        Check(ReferenceEquals(TransformerMinigunSoundLevels.Prepare("fire_loop",original,out _),original),"nonmotor upload passes original PCM values and reference through unchanged");
        bool rejects=false;try{TransformerMinigunSoundLevels.Prepare("spin_start",new[]{float.NaN},out _);}catch(InvalidDataException){rejects=true;}
        Check(rejects,"nonfinite optional PCM is rejected before Unity upload");
        bool safe=true,early=false;
        for(int i=0;i<=150;i++) {
            float spin=i/150f;var levels=TransformerMinigunSoundLevels.Motor(spin,1,.97f,.97f,.97f);
            safe&=levels.Pitched*.97f+levels.Unpitched*.97f+levels.Action*.97f<=.970001f;
            if(i==1)early=levels.Pitched>.15f&&levels.Unpitched>.15f;
        }
        Check(safe,"entire spin ramp keeps all three motor voices inside conservative combined peak budget");
        Check(early,"first spin frame already has audible pitched and unpitched floors");
        var muted=TransformerMinigunSoundLevels.Motor(.5f,0,.3f,.9f,.7f);
        Check(muted.Pitched==0&&muted.Unpitched==0&&muted.Action==0,"master mute zeros every motor voice even with gain floor");
        var invalid=TransformerMinigunSoundLevels.Motor(float.NaN,float.PositiveInfinity,1,1,1);
        Check(invalid.Pitched==0&&invalid.Unpitched==0&&invalid.Action==0,"invalid master and spin cannot produce nonfinite or audible source levels");
    }
    static void Actual(string folder) {
        var data=new SpawnAudio(folder);var before=data.Data.ToDictionary(p=>p.Key,p=>(float[])p.Value.Samples.Clone());
        var files=Directory.GetFiles(folder,"*.wav").ToDictionary(p=>p,Hash);var gun=new TransformerMinigun();var logs=new List<string>();
        gun.LoadForTest(folder,data,logs.Add);var voices=gun.Voices;
        Check(voices.Length==10&&voices.Distinct().Count()==10,"two native motor layers add exactly one bounded voice to the original nine");
        Check(voices.All(v=>!v.playOnAwake&&v.priority==24&&v.spatialBlend==1&&v.minDistance==5&&v.maxDistance==100&&v.dopplerLevel==0&&ReferenceEquals(v.outputAudioMixerGroup,Resources.Sfx)),"all ten voices retain original spatial SFX routing priority and Doppler policy");
        bool preserved=true,motorSafe=true,sourcePreserved=true;double startDb=0,loopDb=0,unpitchedDb=0;
        foreach(var pair in data.Data) {
            var clip=gun.Clip(pair.Key);if(clip==null)continue;
            sourcePreserved&=pair.Value.Samples.SequenceEqual(before[pair.Key]);
            if(TransformerMinigunSoundLevels.IsMotor(pair.Key)) {
                motorSafe&=Peak(clip.Uploaded)<=.970001f;
                double db=20*Math.Log10(Rms(clip.Uploaded)/Rms(pair.Value.Samples));
                if(pair.Key=="spin_start")startDb=db;if(pair.Key=="spin_loop")loopDb=db;if(pair.Key=="spin_unpitched_loop")unpitchedDb=db;
            }else preserved&=clip.Uploaded.SequenceEqual(pair.Value.Samples);
        }
        Check(preserved,"actual fire mech LFE reload deploy and tails upload native samples byte for byte unchanged");
        Check(sourcePreserved,"all worker-cached original motor and nonmotor arrays stay unchanged");
        Check(motorSafe&&Math.Abs(startDb-12)<.001&&Math.Abs(loopDb-12)<.001&&unpitchedDb>11.8&&unpitchedDb<12,"actual native motor PCM gains twelve dB with only unpitched peak modestly bounded below point nine seven");
        gun.TickForTest(true,false,1f/150);
        Check(gun.Pitched.isPlaying&&gun.Unpitched.isPlaying&&gun.MotorAction.isPlaying&&gun.FireLoop.Starts==0&&gun.FireAction.Starts==0,"first windup frame plays both motor textures and motor attack before any gunfire");
        Check(gun.Pitched.volume>.3f&&gun.Unpitched.volume>.25f&&gun.Pitched.volume>.6f/150*50,"actual first-frame motor floor exceeds previous almost silent ramp by a clear margin");
        Check(Near(gun.Unpitched.pitch,1)&&Near(gun.Pitched.pitch,.7f+.3f/150),"unpitched layer stays at pitch one while original pitched spin acceleration remains");
        int start=gun.MotorAction.Starts,pitched=gun.Pitched.Starts,unpitched=gun.Unpitched.Starts;
        gun.TickForTest(true,false,.4f);gun.TickForTest(true,false,1);
        Check(gun.MotorAction.Starts==start&&gun.Pitched.Starts==pitched&&gun.Unpitched.Starts==unpitched,"spin ramp never retriggers attack or either looping source");
        float budget=gun.Pitched.volume*Peak(gun.Pitched.clip.Uploaded)+gun.Unpitched.volume*Peak(gun.Unpitched.clip.Uploaded)+gun.MotorAction.volume*Peak(gun.MotorAction.clip.Uploaded);
        Check(budget<=.970001f,"actual adapter gain staging bounds combined motor attack and both loop peaks");
        gun.MotorAction.Finish();gun.TickForTest(true,false,1,volume:.3f);
        Check(gun.Pitched.volume>0&&gun.Pitched.volume<=.3f&&gun.Unpitched.volume>0&&gun.Unpitched.volume<=.3f,"live master volume scales boosted motor loops while preserving their phase");
        gun.TickForTest(true,true,1,volume:.7f);
        Check(gun.FireAction.clip.name=="RustMinigun_fire_start"&&gun.FireAction.Starts==1&&gun.FireLoop.isPlaying,"firing still starts only on its original firing edge");
        Check(Near(gun.FireLoop.volume,.7f)&&Near(gun.FireAction.volume,.7f)&&Near(gun.MechLoop.volume,.7f*.45f)&&Near(gun.LfeLoop.volume,.7f*.45f),"shot mechanism and LFE source gains remain exactly the existing runtime mix");
        int fireStarts=gun.FireAction.Starts;gun.TickForTest(true,true,1,volume:.7f);
        Check(gun.FireAction.Starts==fireStarts,"sustained firing does not repeat the first-shot edge");
        gun.TickForTest(true,false,.8f,true,.7f);
        Check(gun.MotorAction.clip.name=="RustMinigun_spin_release"&&gun.FireAction.clip.name=="RustMinigun_fire_tail"&&gun.ReloadAction.clip.name=="RustMinigun_reload","release and belt reload keep separate original phase edges");
        int reload=gun.ReloadAction.Starts;gun.TickForTest(true,false,.7f,volume:.7f);
        Check(gun.ReloadAction.isPlaying&&gun.ReloadAction.Starts==reload&&Near(gun.ReloadAction.volume,.7f),"following release frames neither interrupt nor boost reload voice");
        gun.TickForTest(true,false,0,volume:.7f);
        Check(!gun.Pitched.isPlaying&&!gun.Unpitched.isPlaying&&gun.MotorAction.clip.name=="RustMinigun_spin_stop","spin zero stops both motor loops and plays stop accent once");
        int stops=gun.MotorAction.Starts;gun.TickForTest(true,false,0,volume:.7f);
        Check(gun.MotorAction.Starts==stops,"idle ticks cannot replay the motor stop edge");
        gun.TickForTest(true,false,.01f);gun.TickForTest(true,false,.02f,volume:0);
        Check(voices.All(v=>v.volume==0),"master mute zeros all ten actual sources including attack reload and gain floors");
        gun.TickForTest(false,false,0);int totalStarts=voices.Sum(v=>v.Starts);
        gun.TickForTest(false,false,0);gun.TickForTest(true,false,0);
        Check(voices.All(v=>!v.isPlaying)&&voices.Sum(v=>v.Starts)==totalStarts,"explicit inactive Stop and following idle frame never autoresume old sources");
        gun.TickForTest(true,false,.01f);
        Check(gun.MotorAction.clip.name=="RustMinigun_spin_start"&&gun.Pitched.isPlaying&&gun.Unpitched.isPlaying&&gun.Voices.Length==10,"new deliberate spin cycle reuses same fixed voices with a fresh attack");
        gun.TickForTest(true,false,.1f,volume:float.NaN);
        Check(voices.All(v=>v.volume==0),"invalid incoming master cannot bypass master mute");
        gun.Dispose();gun.Dispose();gun.TickForTest(true,true,1);
        Check(voices.All(v=>!v.isPlaying&&v.clip==null&&v.Destroyed)&&AudioClip.All.All(c=>c.Destroyed),"idempotent owner disposal stops every source releases all uploaded clips and prevents replay");
        Check(files.All(p=>p.Value==Hash(p.Key)),"all original native WAV files retain their exact SHA-256 after runtime preparation");
    }
    static void OptionalFailures(string folder) {
        var data=new SpawnAudio(folder);data.Data.Remove("spin_unpitched_loop");var errors=new List<string>();
        using(var gun=new TransformerMinigun()) {
            gun.LoadForTest(folder,data,errors.Add);gun.TickForTest(true,false,.1f);
            Check(gun.Pitched.isPlaying&&!gun.Unpitched.isPlaying&&gun.Voices.Length==10&&errors.Count(v=>v.Contains("skipped"))==1,"missing optional unpitched texture preserves strengthened pitched motor with fixed bounded allocation");
        }
        data=new SpawnAudio(folder);data.Data["spin_start"].Samples=new[]{float.NaN};errors.Clear();
        using(var gun=new TransformerMinigun()) {
            gun.LoadForTest(folder,data,errors.Add);gun.TickForTest(true,true,1);
            Check(gun.Clip("spin_start")==null&&gun.Pitched.isPlaying&&gun.FireLoop.isPlaying&&errors.Count(v=>v.Contains("skipped"))==1,"invalid optional motor PCM disables only its own clip and leaves firing intact");
        }
        data=new SpawnAudio(folder);AudioClip.FailNextUpload=true;errors.Clear();
        using(var gun=new TransformerMinigun()) {
            gun.LoadForTest(folder,data,errors.Add);gun.TickForTest(true,false,.1f);
            Check(gun.Clip("spin_start")==null&&gun.Pitched.isPlaying&&gun.Unpitched.isPlaying&&errors.Count(v=>v.Contains("skipped"))==1,"failed Unity upload releases partial clip without losing remaining motor layers");
        }
        using(var gun=new TransformerMinigun()) {
            gun.LoadForTest(folder,null,_=>{});for(int i=0;i<100;i++)gun.TickForTest(true,false,.1f);
            Check(gun.Voices.Length==10&&gun.Voices.All(v=>v.Starts==0),"absent optional audio cannot allocate extra voices or repeatedly start empty loops");
        }
        Check(AudioClip.All.All(c=>c.Destroyed),"all successful and failed optional upload paths release every clip");
    }
    static void Main(string[] args){string folder=args.Length>0?args[0]:Path.Combine(Environment.CurrentDirectory,".local","minigun");Model();Actual(folder);OptionalFailures(folder);Console.WriteLine(checks+" minigun motor audio checks passed; native listening remains required.");}
}
