using System;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<KeyboardShortcut> menuKey;
    static readonly string[] ColorNames={"Grün","Blau","Cyan","Violett","Rot","Gelb","Weiß"};
    static readonly Color[] NeonColors={new Color(.2f,1,.08f),new Color(.12f,.3f,1),Color.cyan,new Color(.7f,.08f,1),Color.red,new Color(1,.8f,.05f),Color.white};
    bool menuOpen;
    int menuTab;
    Vector2 songScroll;
    CursorLockMode menuOldLock;
    bool menuOldVisible;
    MonoBehaviour menuPlayer, menuCamera;
    bool menuPlayerEnabled, menuCameraEnabled;
    Texture2D menuBackground;
    GUIStyle panelStyle, titleStyle, labelStyle, mutedStyle, songButtonStyle;
    void InitializeMenu() {
        InitializeMenuPlacement();
        InitializeMenuTheme();
        InitializeGarage();
        InitializeWheelTuning();
        InitializeShowroomMenu();
        InitializeDrivingMenu();
        InitializeTurretMenu();
        keepRunning=BindVehicle("Vehicle","KeepRunningAfterExit",true,"After driving, keep the engine idling, enabled neon and muffled spatial radio running outside. Can be toggled beside the car.");
        headlightPreset=BindVehicle("Lights","HeadlightPreset","Serie",new ConfigDescription("Front headlight colour preset. Names follow NFSU2; RGB values are adapted for Unity.",new AcceptableValueList<string>(VehicleLighting.HeadlightPresets)));
        headlightStrength=BindVehicle("Lights","HeadlightStrength",1f,new ConfigDescription("Front headlight strength multiplier: 0 to 3. Rear, reverse and plate lights are independent.",new AcceptableValueRange<float>(0,3)));
        menuKey=Config.Bind("Controls","VehicleMenu",new KeyboardShortcut(KeyCode.F9),"Open the garage anywhere, or vehicle controls while seated/within 5 metres.");
        nitroEnabled=BindVehicle("Nitro","Enabled",true,"Hold left mouse: forward throttle boosts; stationary purges. Six seconds of boost, refills while driving after release.");
        backfireEnabled=BindVehicle("Nitro","LiftOffBackfire",false,"Optional high-RPM lift-off effect. Uses original assets with NFVU2 trigger thresholds, not verified NFS tuning rules.");
        radioEnabled=BindVehicle("Radio","Enabled",true,"Play the local NFS radio while seated, and outside if KeepRunningAfterExit is enabled.");
        radioVolume=BindVehicle("Radio","Volume",.55f,new ConfigDescription("Radio volume, independent of engine volume.",new AcceptableValueRange<float>(0,1)));
        neonEnabled=BindVehicle("Neon","Enabled",false,"Enable underbody neon while seated, and outside if KeepRunningAfterExit is enabled.");
        neonPulsing=BindVehicle("Neon","Pulsing",false,"Pulse the underbody neon.");
        neonColor=BindVehicle("Neon","Color","Grün",new ConfigDescription("Underbody neon colour.",new AcceptableValueList<string>(ColorNames)));
    }
    bool CanUseVehicleMenu(MonoBehaviour local)=>local!=null&&!Multiplayer();
    void BlockDrivingInput(){throttle=steer=0;brake=1;honking=false;burnoutHeld=false;nitroHeld=false;nitroInput.Reset();vehicleNitro?.State.Stop();orbitCamera?.InputFrame(false,mouseSensitivity.Value,invertMouseY.Value);}
    void SetVehicleMenu(bool open) {
        if(open==menuOpen)return;
        if(open){
            EnsureMenuAssets();MenuSound(1);hoveredTab=-1;
            if(!CanAccessCurrent(LocalPlayer()))menuTab=10;
            menuOldLock=Cursor.lockState;menuOldVisible=Cursor.visible;menuOpen=true;
            if(!driving){
                menuPlayer=LocalPlayer();if(menuPlayer!=null){menuPlayerEnabled=menuPlayer.enabled;menuPlayer.enabled=false;}
                var type=playerType.Assembly.GetType("GameCamera");
                menuCamera=type?.GetProperty("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as MonoBehaviour;
                if(menuCamera!=null){menuCameraEnabled=menuCamera.enabled;menuCamera.enabled=false;}
            }
            BlockDrivingInput();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        else {
            StopMenuDrag();SaveMenuPlacement();
            MenuSound(2);
            menuOpen=false;
            if(menuPlayer!=null)menuPlayer.enabled=menuPlayerEnabled;
            if(menuCamera!=null)menuCamera.enabled=menuCameraEnabled;
            menuPlayer=null;menuCamera=null;
            Cursor.lockState=menuOldLock;Cursor.visible=menuOldVisible;Config.Save();
        }
    }
    void CloseVehicleMenu(){if(menuOpen)SetVehicleMenu(false);}
    void ApplyNeonSettings() {
        if(vehicleNeon==null)return;
        vehicleNeon.Enabled=neonEnabled.Value;vehicleNeon.Pulsing=neonPulsing.Value;
        int index=Array.IndexOf(ColorNames,neonColor.Value);vehicleNeon.Color=NeonColors[Math.Max(0,index)];
    }
    void EnsureMenuStyles() {
        if(panelStyle!=null)return;
        menuBackground=new Texture2D(1,1);menuBackground.SetPixel(0,0,new Color(.035f,.045f,.055f,.98f));menuBackground.Apply();
        panelStyle=new GUIStyle(GUI.skin.box);panelStyle.normal.background=menuBackground;
        titleStyle=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold};titleStyle.normal.textColor=new Color(.5f,1,.15f);
        labelStyle=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};labelStyle.normal.textColor=Color.white;
        mutedStyle=new GUIStyle(labelStyle){fontSize=13};mutedStyle.normal.textColor=new Color(.65f,.7f,.72f);
        songButtonStyle=new GUIStyle(GUI.skin.button){fontSize=13,wordWrap=true,alignment=TextAnchor.MiddleLeft};
        titleStyle.normal.textColor=MenuAccent;
        BuildMenuSkin();
    }
    void DisposeMenu(){DisposeMenuTheme();if(menuBackground!=null)Destroy(menuBackground);menuBackground=null;panelStyle=titleStyle=labelStyle=mutedStyle=songButtonStyle=null;}
    void OnGUI() {
        if(!menuOpen||audioBlocked||!Application.isFocused)StopMenuDrag();
        if(!ready||LocalPlayer()==null)return;
        var text=driving?DriveModeName+" | WASD: fahren | Leertaste: "+(driftMode.Value?"Handbremse":"bremsen")+" | H: Hupe | F7: aussteigen\nMaus: umschauen | Mausrad: Zoom | "+menuKey.Value+": Fahrzeugmenü":status;
        GUI.Box(new Rect(15,Screen.height-80,Mathf.Min(920,Screen.width-30),60),text);
        DrawDashboard();
        DrawTurretAim();
        if(driving&&!hudEnabled.Value&&vehicleNitro!=null&&nitroEnabled.Value)
            GUI.Box(new Rect(15,Screen.height-120,310,32),"LMB: Nitro / Purge  |  Tank "+Mathf.RoundToInt(vehicleNitro.State.Charge*100)+" %");
        if(!menuOpen||audioBlocked)return;
        EnsureMenuStyles();
        float scale=Mathf.Max(.1f,Mathf.Min(1,Mathf.Min((Screen.width-32f)/740f,(Screen.height-140f)/560f)));
        HandleMenuDrag(scale);
        var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;
        var oldSkin=GUI.skin;bool oldChanged=GUI.changed;var inputEvent=Event.current.type;
        try {
            GUI.skin=menuSkin;GUI.changed=false;
            GUI.matrix=Matrix4x4.TRS(new Vector3(menuPlacement.X,menuPlacement.Y,0),Quaternion.identity,Vector3.one*scale);
            GUI.Box(new Rect(0,0,740,560),GUIContent.none,panelStyle);
            var oldColor=GUI.color;GUI.color=MenuAccent;GUI.DrawTexture(new Rect(0,0,740,3),Texture2D.whiteTexture);GUI.color=oldColor;
            GUI.Label(new Rect(22,15,650,36),"NFVU2  /  "+(CanAccessCurrent(LocalPlayer())?VehicleName:"Garage"),titleStyle);
            if(GUI.Button(new Rect(690,19,30,30),"×")){CloseVehicleMenu();return;}
            GUI.Label(new Rect(23,57,660,25),"UNDERGROUND GARAGE   /   "+(driving?"Fahrzeug bremst":"Tuning & Fahrzeugsteuerung"),mutedStyle);
            if(menuTab!=10&&!CanAccessCurrent(LocalPlayer()))menuTab=10;
            MenuNavigation(CanAccessCurrent(LocalPlayer()));
            GUI.BeginGroup(new Rect(180,0,560,522));
            try {
                GUI.Label(new Rect(25,99,510,28),menuAssets?.Titles[menuTab]??MenuNames[menuTab],titleStyle);
                switch(menuTab){case 10:DrawGarageMenu();break;case 0:DrawCarMenu();break;case 1:DrawRadioMenu();break;case 2:DrawLightsMenu();break;case 3:DrawNeonMenu();break;case 4:DrawNitroMenu();break;case 5:DrawWheelMenu();break;case 6:DrawBodyMenu();break;case 7:DrawTrunkMenu();break;case 8:DrawDrivingDisplayMenu();break;case 9:DrawTurretMenu();break;}
            }finally{GUI.EndGroup();}
            GUI.Label(new Rect(23,530,700,25),menuKey.Value+": schließen  ·  ↑↓: Kategorie  ·  Titelleiste ziehen: verschieben",mutedStyle);
            if(GUI.changed&&(inputEvent==EventType.MouseUp||inputEvent==EventType.MouseDown||inputEvent==EventType.MouseDrag||inputEvent==EventType.KeyDown))MenuSound(0);
        } finally {GUI.matrix=oldMatrix;GUI.enabled=oldEnabled;GUI.skin=oldSkin;GUI.changed|=oldChanged;}
    }
    void DrawCarMenu() {
        GUI.Label(new Rect(25,145,510,30),"Fahrmodus",labelStyle);
        int current=!IsHummer&&driftMode.Value?2:terrainMode.Value?0:1;
        int mode=GUI.Toolbar(new Rect(25,180,510,30),current,IsHummer?new[]{"Gelände","Straße"}:new[]{"Gelände","Straße","Drift"});
        if(mode!=current){driftMode.Value=mode==2;terrainMode.Value=mode==0;ApplyDriveMode();}
        GUI.Label(new Rect(25,218,510,52),mode==2?"Heckantrieb, lockeres Heck, schnelles Gegenlenken.\nLeertaste: Hinterrad-Handbremse · S: bremsen":(mode==0?"Für kurze Strecken und unebenen Boden.":"Straßenmodus mit Heckantrieb.")+"\nW + S im Stand: Burnout · Leertaste: abbrechen",mutedStyle);
        if(MenuButton(new Rect(25,280,245,38),driving?"Aussteigen":"Einsteigen")){
            var local=LocalPlayer();CloseVehicleMenu();if(driving)Exit();else if(CanAccessCurrent(local))Enter(local);return;
        }
        if(MenuButton(new Rect(285,280,245,38),"Fahrzeug entfernen")){Cleanup();return;}
        keepRunning.Value=GUI.Toggle(new Rect(25,340,510,32),keepRunning.Value," Beim Aussteigen weiterlaufen lassen");
        GUI.Label(new Rect(25,378,510,48),"Motor im Leerlauf, aktiviertes Neon und gedämpfte Musik. Auch neben dem Auto ein-/ausschaltbar.",mutedStyle);
        if(MenuButton(new Rect(25,445,245,38),"Türen und Motorhaube"))menuTab=6;
        if(MenuButton(new Rect(285,445,245,38),"Kofferraum-Ausbau"))menuTab=7;
    }
    void DrawNitroMenu() {
        if(IsHummer){GUI.Label(new Rect(25,150,510,80),"Der H2 startet mit Serienmotor. Nitro und Auspuffeffekte sind hier noch nicht eingerichtet.",labelStyle);return;}
        nitroEnabled.Value=GUI.Toggle(new Rect(25,145,510,32),nitroEnabled.Value," Nitro aktivieren");
        GUI.Label(new Rect(25,195,510,75),"Linke Maustaste halten: Beim Vorwärtsfahren mit Gas gibt es Schub und grüne Auspuffflammen.",labelStyle);
        GUI.Label(new Rect(25,275,510,70),"Im Stand strömt stattdessen Gas aus den Düsen vorne und an der Haube.",labelStyle);
        GUI.Label(new Rect(25,350,510,60),"Sechs Sekunden Boost. Nach dem Loslassen lädt der Tank beim Fahren wieder auf.",mutedStyle);
        backfireEnabled.Value=GUI.Toggle(new Rect(25,427,510,32),backfireEnabled.Value," Auspuffflammen beim Gaswegnehmen");
        GUI.Label(new Rect(25,464,510,44),effectsAudio!=null&&effectsAudio.BackfireAvailable?"Kurzer Flammenstoß nach hoher Drehzahl; separat zuschaltbar.":"Kurzer Flammenstoß nach hoher Drehzahl. Knallgeräusch noch nicht verfügbar.",mutedStyle);
    }
    void DrawRadioMenu() {
        radioEnabled.Value=GUI.Toggle(new Rect(25,143,510,28),radioEnabled.Value," Radio einschalten");
        GUI.Label(new Rect(25,185,510,25),"Lautstärke  "+Mathf.RoundToInt(radioVolume.Value*100)+" %",labelStyle);
        radioVolume.Value=GUI.HorizontalSlider(new Rect(25,220,505,22),radioVolume.Value,0,1);
        if(vehicleRadio==null||vehicleRadio.Count==0){GUI.Label(new Rect(25,260,510,95),"Noch keine Musik verfügbar. Die NFS-Musik muss zuerst lokal importiert werden.",labelStyle);return;}
        GUI.Label(new Rect(25,255,510,48),vehicleRadio.CurrentTitle,labelStyle);
        GUI.enabled=(driving||ParkedRunning)&&radioEnabled.Value;
        if(MenuButton(new Rect(25,310,160,34),"◀ Zurück"))vehicleRadio.Previous();
        if(MenuButton(new Rect(195,310,165,34),vehicleRadio.Paused?"Abspielen":"Pause"))vehicleRadio.TogglePause();
        if(MenuButton(new Rect(370,310,160,34),"Weiter ▶"))vehicleRadio.Next();
        songScroll=GUI.BeginScrollView(new Rect(25,360,510,130),songScroll,new Rect(0,0,488,vehicleRadio.Count*44));
        var titles=vehicleRadio.TrackTitles;
        try {for(int i=0;i<vehicleRadio.Count;i++)if(MenuButton(new Rect(0,i*44,483,42),(i==vehicleRadio.CurrentIndex?"▶ ":"")+titles[i],songButtonStyle))vehicleRadio.Select(i);}
        finally {GUI.EndScrollView();GUI.enabled=true;}
        GUI.Label(new Rect(25,493,510,23),driving||ParkedRunning?vehicleRadio.Status:hasBeenDriven?"Zum Musikhören einsteigen oder Weiterlaufen aktivieren.":"Zum Musikhören einmal einsteigen.",mutedStyle);
    }
    void DrawLightsMenu() {
        GUI.enabled=vehicleLighting!=null;
        bool lights=GUI.Toggle(new Rect(25,140,510,32),vehicleLighting!=null&&vehicleLighting.Enabled," Scheinwerfer, Rücklicht und Kennzeichenlicht");
        if(vehicleLighting!=null)vehicleLighting.Enabled=lights;
        float strength=headlightStrength.Value;
        if(float.IsNaN(strength)||float.IsInfinity(strength))strength=1;
        GUI.Label(new Rect(25,181,510,26),"Scheinwerferstärke   "+Mathf.RoundToInt(strength*100)+" %",labelStyle);
        float next=Mathf.Round(GUI.HorizontalSlider(new Rect(25,216,510,20),strength,0,3)*20)/20;
        if(next!=headlightStrength.Value)headlightStrength.Value=next;
        GUI.Label(new Rect(25,247,510,25),"Scheinwerferfarbe",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(VehicleLighting.HeadlightPresets,headlightPreset.Value));
        int choice=GUI.SelectionGrid(new Rect(25,279,510,148),selected,VehicleLighting.HeadlightPresets,4,songButtonStyle);
        if(choice!=selected)headlightPreset.Value=VehicleLighting.HeadlightPresets[choice];
        if(MenuButton(new Rect(25,444,510,32),"Scheinwerfer zurücksetzen")) {headlightPreset.Value="Serie";headlightStrength.Value=1;}
        GUI.enabled=true;
        GUI.Label(new Rect(25,483,510,36),"Farbe und Stärke gelten vorne. Fahrlicht geht beim Einstieg an; Brems- und Rückfahrlicht reagieren automatisch.",mutedStyle);
    }
    void DrawNeonMenu() {
        GUI.enabled=vehicleNeon!=null;
        bool enabled=GUI.Toggle(new Rect(25,145,510,32),neonEnabled.Value," Unterbodenbeleuchtung einschalten");
        bool pulse=GUI.Toggle(new Rect(25,190,510,32),neonPulsing.Value," Pulsierendes Licht");
        GUI.Label(new Rect(25,244,510,30),"Farbe",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(ColorNames,neonColor.Value));
        int choice=GUI.SelectionGrid(new Rect(25,285,510,100),selected,ColorNames,4);
        if(enabled!=neonEnabled.Value||pulse!=neonPulsing.Value||choice!=selected){neonEnabled.Value=enabled;neonPulsing.Value=pulse;neonColor.Value=ColorNames[choice];ApplyNeonSettings();}
        GUI.enabled=true;
        GUI.Label(new Rect(25,420,510,70),"Farbe und Pulsmodus bleiben gespeichert. Mit „Beim Aussteigen weiterlaufen lassen“ bleibt aktiviertes Neon auch draußen an.",mutedStyle);
    }
}
}
