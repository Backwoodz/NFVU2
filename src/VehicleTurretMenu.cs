using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> turretEnabled;
    ConfigEntry<bool> turretAutomatic;
    ConfigEntry<float> turretVolume;
    ConfigEntry<bool> turretLaser;
    ConfigEntry<string> turretAmmo;
    ConfigEntry<float> laserStrength,laserRed,laserGreen,laserBlue;
    bool laserSettingsOpen;
    Hud crosshairHud;
    GameObject[] savedCrosshairs;
    bool[] savedCrosshairActive;
    static readonly FieldInfo[] CrosshairFields={typeof(Hud).GetField("m_crosshair"),typeof(Hud).GetField("m_crosshairBow")};
    void RestoreGameCrosshair() {
        if(savedCrosshairs!=null)for(int i=0;i<savedCrosshairs.Length;i++)
            if(savedCrosshairs[i]!=null)savedCrosshairs[i].SetActive(savedCrosshairActive[i]);
        crosshairHud=null;savedCrosshairs=null;savedCrosshairActive=null;
    }
    void UpdateGameCrosshair() {
        if(!driving){RestoreGameCrosshair();return;}
        var hud=Hud.instance;
        if(hud==null){RestoreGameCrosshair();return;}
        if(savedCrosshairs==null||crosshairHud!=hud) {
            RestoreGameCrosshair();crosshairHud=hud;
            savedCrosshairs=new GameObject[CrosshairFields.Length];savedCrosshairActive=new bool[CrosshairFields.Length];
            for(int i=0;i<CrosshairFields.Length;i++) {
                var component=CrosshairFields[i]?.GetValue(hud) as Component;
                if(component==null)continue;
                savedCrosshairs[i]=component.gameObject;savedCrosshairActive[i]=component.gameObject.activeSelf;
            }
        }
        // Hud.UpdateCrosshair can re-enable these every Update; suppress after it in LateUpdate.
        foreach(var obj in savedCrosshairs)if(obj!=null)obj.SetActive(false);
    }
    void InitializeTurretMenu() {
        turretAutomatic=Config.Bind("Turret","AutomaticAfterExit",true,"After driving, scan a frontal 180-degree sector while unoccupied and attack visible hostile creatures within 30 metres. Independent of parked engine/radio.");
        turretEnabled=Config.Bind("Turret","Enabled",false,"Mount the optional local Rust AutoTurret with AK on the roof. Right mouse while seated fires toward the camera aim.");
        turretVolume=Config.Bind("Turret","Volume",.7f,new ConfigDescription("Turret sound volume, multiplied by Audio.Volume.",new AcceptableValueRange<float>(0,1)));
        turretLaser=Config.Bind("Turret","Laser",true,"Original Rust laser module with visible beam and impact dot.");
        laserStrength=Config.Bind("Turret","LaserStrength",2f,new ConfigDescription("Laser brightness and beam width multiplier.",new AcceptableValueRange<float>(.25f,4)));
        laserRed=Config.Bind("Turret","LaserRed",1f,new ConfigDescription("Laser red channel.",new AcceptableValueRange<float>(0,1)));
        laserGreen=Config.Bind("Turret","LaserGreen",.03f,new ConfigDescription("Laser green channel.",new AcceptableValueRange<float>(0,1)));
        laserBlue=Config.Bind("Turret","LaserBlue",.02f,new ConfigDescription("Laser blue channel.",new AcceptableValueRange<float>(0,1)));
        turretAmmo=Config.Bind("Turret","Ammo","Normal",new ConfigDescription("Explosiv adds blast damage and persistent small craters on terrain hits, within Valheim's dig limit.",new AcceptableValueList<string>("Normal","Explosiv")));
    }
    void TickTurret() {
        bool automatic=ControlRules.AutomaticTurret(Flying,driving,hasBeenDriven,turretAutomatic.Value);
        if(HordeVehicle)automatic=hordeRun.Automatic;
        bool requested=HordeVehicle||turretEnabled.Value;
        if(hordeRun.Active&&!HordeVehicle){requested=false;automatic=false;}
        if(turret!=null)try {turret.State.ReloadMultiplier=HordeVehicle?HordePacts.Reload(hordeRun.Pact):1;
            turret.AutomaticRange=HordeVehicle?60:TurretAutoRules.Range;
            turret.Tick(requested,(driving&&DriverInputReady||automatic)&&!audioBlocked&&!menuOpen&&Application.isFocused&&(!HordeVehicle||!hordeRun.Dead&&!hordeWave.Choosing),
            driving&&!Flying&&turretHeld,LocalPlayer() as Character,soundVolume.Value*turretVolume.Value,turretLaser.Value,!HordeVehicle&&turretAmmo.Value=="Explosiv",laserStrength.Value,LaserColor,automatic,HordeVehicle?(Func<bool>)hordeRun.ConsumeExplosiveShot:null,driving,HordeVehicle?HordePacts.Turret(hordeRun.Pact):1,HordeVehicle?(Action<RaycastHit,Character>)OnHordeTurretHit:null,HordeVehicle?HordeAmmoTint:(Color?)null);}
        catch(Exception e){turret.Dispose();turret=null;Logger.LogWarning("350Z turret disabled: "+e);}
    }
    void DrawTurretMenu() {
        if(Transforming){DrawTransformerMinigunMenu();return;}
        if(IsHummer){UiLabel(new Rect(25,150,510,80),"Der Dach-Turret ist aktuell nur beim 350Z eingerichtet.",labelStyle);return;}
        if(laserSettingsOpen){DrawLaserSettings();return;}
        GUI.enabled=turret!=null&&!HordeVehicle;
        bool enabled=UiToggle(new Rect(25,145,510,32),HordeVehicle||turretEnabled.Value," Rust AutoTurret mit AK auf dem Dach");
        if(!HordeVehicle)turretEnabled.Value=enabled;
        UiLabel(new Rect(25,185,510,50),(Flying?"Im Flug: automatisch zielen und schießen":padActive?"RS: zielen · "+ButtonName("LB","L1")+": schießen":"Mausblick: zielen · Rechte Maustaste: schießen")+"\n30 Schuss · automatisches Nachladen",labelStyle);
        GUI.enabled=turret!=null&&turret.LaserAvailable;
        turretLaser.Value=UiToggle(new Rect(25,242,280,28),turretLaser.Value," Rust-Lasermodul");
        if(MenuButton(new Rect(320,240,215,30),"Laser einstellen")){laserSettingsOpen=true;uiFocus=0;}
        GUI.enabled=turret!=null&&turret.ExplosiveAvailable&&!HordeVehicle;
        int ammo=HordeVehicle?(hordeRun.Explosive?1:0):turretAmmo.Value=="Explosiv"?1:0;
        int next=UiToolbar(new Rect(25,285,510,32),ammo,new[]{"Normale Munition","Explosivmunition"});
        if(next!=ammo&&!HordeVehicle)turretAmmo.Value=next==1?"Explosiv":"Normal";
        GUI.enabled=turret!=null&&!HordeVehicle;
        turretAutomatic.Value=UiToggle(new Rect(25,330,510,28),turretAutomatic.Value," Nach dem Aussteigen automatisch bewachen");
        UiLabel(new Rect(25,363,510,24),"180° vorne · 30 m · "+(Flying?"Flug-Automatik":driving?"Manuelle Steuerung":hasBeenDriven&&turretAutomatic.Value?"Im Menü pausiert":"Bereit nach der Fahrt"),mutedStyle);
        UiLabel(new Rect(25,394,510,25),"Lautstärke  "+Mathf.RoundToInt(turretVolume.Value*100)+" %",labelStyle);
        turretVolume.Value=UiSlider(new Rect(25,429,510,22),turretVolume.Value,0,1);
        GUI.enabled=true;
        UiLabel(new Rect(25,466,510,50),HordeVehicle?"Hordenmodus: Explosivmunition und Automatik durch Powerups sammeln.":turret==null?"Rust-Assets fehlen. Lokal mit tools/export_rust_turret.py importieren.":turretAmmo.Value=="Explosiv"?"Explosiv: Flächenschaden; zerstört große Bäume und abbaubare Felsen. Bodentreffer hinterlassen Krater.":"Automatik trifft nur sichtbare Gegner. Spieler, zahme Tiere, Auto und Deckung blockieren die Schusslinie.",mutedStyle);
    }
    Color LaserColor=>new Color(LaserChannel(laserRed.Value),LaserChannel(laserGreen.Value),LaserChannel(laserBlue.Value),1);
    void DrawTransformerMinigunMenu() {
        if(laserSettingsOpen){DrawLaserSettings();return;}
        var weapon=current.transformerMinigun;
        UiLabel(new Rect(25,145,510,32),"Transformer · Rust Minigun",labelStyle);
        UiLabel(new Rect(25,185,510,58),(padActive?"RS: zielen · "+ButtonName("LB","L1")+": schießen":"Mausblick: zielen · LMB: schießen")+"\n1000 Schuss · automatisches Nachladen",labelStyle);
        GUI.enabled=weapon!=null;
        turretLaser.Value=UiToggle(new Rect(25,250,280,28),turretLaser.Value," Laservisier");
        if(MenuButton(new Rect(320,248,215,30),"Laser einstellen")){laserSettingsOpen=true;uiFocus=0;}
        GUI.enabled=weapon!=null&&weapon.ExplosiveAvailable;
        int ammo=transformerMinigunAmmo.Value=="Explosiv"?1:0;
        int next=UiToolbar(new Rect(25,295,510,32),ammo,new[]{"Normale Munition","Explosivmunition"});
        if(next!=ammo)transformerMinigunAmmo.Value=next==1?"Explosiv":"Normal";
        GUI.enabled=weapon!=null;
        UiLabel(new Rect(25,355,510,25),"Lautstärke  "+Mathf.RoundToInt(turretVolume.Value*100)+" %",labelStyle);
        turretVolume.Value=UiSlider(new Rect(25,390,510,22),turretVolume.Value,0,1);
        GUI.enabled=true;
        UiLabel(new Rect(25,435,510,65),ammo==1?"Explosiv: Flächenschaden; zerstört große Bäume und abbaubare Felsen. Bodentreffer hinterlassen Krater.":"1000 Schuss · 250 m · 12 Stichschaden pro Schuss",mutedStyle);
    }
    static float LaserChannel(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp01(value);
    void DrawLaserSettings() {
        UiLabel(new Rect(25,145,300,30),"Laser · Stärke und Farbe",labelStyle);
        if(MenuButton(new Rect(415,145,120,28),"Zurück")){laserSettingsOpen=false;return;}
        GUI.enabled=Transforming?this.current.transformerMinigun!=null:turret!=null&&turret.LaserAvailable;
        turretLaser.Value=UiToggle(new Rect(25,187,510,28),turretLaser.Value," Laser aktivieren");
        UiLabel(new Rect(25,226,510,25),"Stärke  "+Mathf.RoundToInt(laserStrength.Value*100)+" %",labelStyle);
        laserStrength.Value=Mathf.Round(UiSlider(new Rect(25,262,510,20),laserStrength.Value,.25f,4)*20)/20;
        int selected=-1;var current=LaserColor;
        for(int i=0;i<NeonColors.Length;i++)if(Mathf.Abs(current.r-NeonColors[i].r)+Mathf.Abs(current.g-NeonColors[i].g)+Mathf.Abs(current.b-NeonColors[i].b)<.01f)selected=i;
        int preset=UiGrid(new Rect(25,301,510,52),selected,ColorNames,4);
        if(preset>=0&&preset!=selected){var c=NeonColors[preset];laserRed.Value=c.r;laserGreen.Value=c.g;laserBlue.Value=c.b;}
        var channels=new[]{laserRed,laserGreen,laserBlue};var names=new[]{"Rot","Grün","Blau"};
        for(int i=0;i<3;i++) {
            float y=370+i*40;
            UiLabel(new Rect(25,y,85,24),names[i]+" "+Mathf.RoundToInt(LaserChannel(channels[i].Value)*255),mutedStyle);
            channels[i].Value=UiSlider(new Rect(115,y+8,290,20),LaserChannel(channels[i].Value),0,1);
        }
        var old=GUI.color;GUI.color=LaserColor;GUI.DrawTexture(new Rect(440,375,85,85),Texture2D.whiteTexture);GUI.color=old;
        GUI.enabled=true;
        UiLabel(new Rect(25,490,510,26),"Stärke und Farbe gelten für Strahl und Treffpunkt.",mutedStyle);
    }
    void DrawTurretAim() {
        if(Transforming){DrawTransformerMinigunAim();return;}
        if(!driving||Flying||menuOpen||audioBlocked||hordeWave.Choosing||turret==null||!turret.Enabled||Camera.main==null)return;
        var old=GUI.color;
        try {
            {
                float x=Screen.width*TurretAimSettings.ViewX,y=Screen.height*(1-TurretAimSettings.ViewY);
                // Pixel strokes stay centred on the world hit regardless of GUI font metrics.
                var bars=new[]{new Rect(x-12,y-1,7,2),new Rect(x+5,y-1,7,2),new Rect(x-1,y-12,2,7),new Rect(x-1,y+5,2,7)};
                GUI.color=new Color(0,0,0,.85f);
                foreach(var r in bars)GUI.DrawTexture(new Rect(r.x-1,r.y-1,r.width+2,r.height+2),Texture2D.whiteTexture);
                GUI.color=turret.Aligned?new Color(.5f,1,.2f):new Color(1,.45f,.1f);
                foreach(var r in bars)GUI.DrawTexture(r,Texture2D.whiteTexture);
            }
            GUI.color=Color.white;
            string ammoName=HordeVehicle?HordeAmmoName:turretAmmo.Value;
            string ammoText=turret.State.ReloadLeft>0?"AK · Nachladen…":(HordeVehicle&&hordeRun.Automatic?"AUTO":padActive?ButtonName("LB","L1"):"RMB")+": AK "+ammoName+" · "+turret.State.Ammo+" / 30"+(turret.Obstructed?" · Blockiert":"");
            if(HordeVehicle) {
                if(!Hud.IsUserHidden())HordeLabel(new Rect(20,Screen.height-controlHintHeight-60,580,38),Tr(ammoText),24,HordeWhite,TextAnchor.MiddleLeft);
            }else UiBox(new Rect(15,Screen.height-controlHintHeight-52,340,28),ammoText);
        }finally{GUI.color=old;}
    }
}
}
