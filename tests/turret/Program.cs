using System;
using System.IO;
using System.Linq;
using Rachel350Z;
class TurretTests {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static void Main(string[] args) {
        var s=new TurretState();
        Check(!s.Step(.02f,true,true)&&s.Ammo==30,"entering with held right mouse cannot fire");
        s.Step(.02f,true,false);Check(s.Step(.02f,true,true)&&s.Ammo==29,"release then press fires one round");
        Check(!s.Step(.02f,true,true),"shot cooldown prevents per-frame firing");
        int fired=1;
        for(int i=0;i<500&&s.ReloadLeft==0;i++)if(s.Step(.02f,true,true))fired++;
        Check(fired==30&&s.Ammo==0&&s.ReloadStarted,"magazine empties after exactly thirty shots and starts reload");
        float before=s.ReloadLeft;s.Step(.1f,false,true);
        Check(s.ReloadLeft==before&&!s.ReloadStarted,"menu freezes reload without repeating its event");
        bool staleShot=false;for(int i=0;i<130;i++)staleShot|=s.Step(.02f,true,true);
        Check(!staleShot,"held input after blocked state stays disarmed through reload completion");
        Check(s.Ammo==30&&s.ReloadLeft==0,"reload restores one full magazine");
        s.Step(.02f,true,false);Check(s.Step(.02f,true,true),"fresh press after menu resumes firing");
        s.Step(.1f,false,false);Check(!s.Step(.1f,true,true),"exit and reentry also require release");
        s.Step(.1f,true,false);Check(!s.Step(float.NaN,true,true)&&!s.Step(float.PositiveInfinity,true,true),"invalid delta cannot fire");
        Check(s.Step(10,true,true)&&s.Ammo==28,"large delta emits at most one shot without backlog");
        string asset=Path.Combine(args[0],"turret.json");
        if(!File.Exists(asset)){Console.WriteLine(checks+" turret state checks passed; optional local Rust assets absent.");return;}
        var model=ModelFile.Load(asset);model.turretRig.Validate();
        Check(model.parts.Length==9&&model.textures.Length==7,"real imported turret AK and laser sections validate");
        Check(model.parts.Select(p=>p.name.Split('|')[0]).Distinct().Count()==5,"base yaw pitch gun and laser groups are separate");
        Check(model.turretRig.laser.HasValue&&model.parts.Count(p=>p.name.StartsWith("laser|"))==2,"original laser body and lens imported with effect bone");
        Check(model.turretRig.laser.Value.y<model.turretRig.muzzle.y&&model.turretRig.laser.Value.z<model.turretRig.muzzle.z,"laser lens is under and behind the muzzle");
        Check(!model.parts.Any(p=>p.name.Contains("cover")),"armed turret omits empty socket cover");
        Check(model.turretRig.muzzle.z>.3f&&model.turretRig.muzzle.z<.35f,"aligned original barrel marker points forward of attachment");
        var magazine=model.parts.First(p=>p.name.Contains("MagGrip"));
        Check(magazine.vertices.Min(v=>v.y)<-.14f&&magazine.vertices.Max(v=>Math.Abs(v.x))<.04f,"dropped worldmodel rolled upright with magazine below bore");
        foreach(var name in new[]{"shot1","shot2","shot3","tail","servo","reload","online","explosion"}) {
            var wav=WavPcm16.Read(File.OpenRead(Path.Combine(args[0],name+".wav")),30);
            Check(wav.Channels==1&&wav.Samples.Length>1000,"original "+name+" sample accepted by runtime PCM reader");
        }
        bool tooLong=false;try{WavPcm16.Read(File.OpenRead(Path.Combine(args[0],"servo.wav")));}catch(InvalidDataException){tooLong=true;}
        Check(tooLong,"16.5 second servo loop uses explicit turret limit; existing audio limit unchanged");
        var rig=new TurretRig{yawPivot=model.turretRig.yawPivot,pitchOffset=model.turretRig.pitchOffset,muzzle=model.turretRig.muzzle};
        rig.muzzle.z=float.NaN;bool invalid=false;try{rig.Validate();}catch(InvalidDataException){invalid=true;}
        Check(invalid,"invalid pivot rejected before native transform upload");
        Check(TurretBlastRules.Damage(0)==55&&TurretBlastRules.Damage(1)>0&&TurretBlastRules.Damage(1)<55,"explosion damage falls off from centre");
        Check(TurretBlastRules.Damage(TurretBlastRules.Radius)==0&&TurretBlastRules.Damage(-1)==0&&TurretBlastRules.Damage(float.NaN)==0,"invalid or outside blast positions cause no area damage");
        Check(TurretAimSettings.ViewY>.5f&&TurretAimSettings.CameraHeight>2,"sight is above screen centre and camera pivot clears roof turret");
        Check(TurretAimSettings.PivotHeight(18)==TurretAimSettings.CameraHeight&&TurretAimSettings.PivotHeight(70)>3.5f,"steep downward aim raises camera further to keep bonnet below reticle");
        Console.WriteLine(checks+" turret checks passed. Native aiming, damage and playback still require an in-game check.");
    }
}
