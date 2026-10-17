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
        headlightShowOff=BindVehicle("Lights","ShowOffAfterExit",false,"After exiting: two left flashes, two right flashes, pause, repeat. Left/right from the driver's seat. Stops on entry; independent of parked engine/radio.");
        menuKey=Config.Bind("Controls","VehicleMenu",new KeyboardShortcut(KeyCode.F9),"Open the garage anywhere, or vehicle controls while seated/within 5 metres.");
        nitroEnabled=BindVehicle("Nitro","Enabled",true,"Hold left mouse: forward throttle boosts; stationary purges. Six seconds of boost, refills while driving after release.");
        backfireEnabled=BindVehicle("Nitro","LiftOffBackfire",false,"Optional high-RPM lift-off effect. Uses original assets with NFVU2 trigger thresholds, not verified NFS tuning rules.");
        radioEnabled=BindVehicle("Radio","Enabled",true,"Play the local NFS radio while seated, and outside if KeepRunningAfterExit is enabled.");
        radioVolume=BindVehicle("Radio","Volume",.55f,new ConfigDescription("Radio volume, independent of engine volume.",new AcceptableValueRange<float>(0,1)));
        neonEnabled=BindVehicle("Neon","Enabled",false,"Enable underbody neon while seated, and outside if KeepRunningAfterExit is enabled.");
        neonPulsing=BindVehicle("Neon","Pulsing",false,"Pulse the underbody neon.");
        neonColor=BindVehicle("Neon","Color","Grün",new ConfigDescription("Underbody neon colour.",new AcceptableValueList<string>(ColorNames)));
    }
    bool CanUseVehicleMenu(MonoBehaviour local)=>local!=null&&!Multiplayer()&&!Transforming;
    void BlockDrivingInput(bool blockCamera=true){controlsGate.Block();handbrakeHeld=turretHeld=false;throttle=steer=0;current.flightVertical=0;brake=1;honking=false;burnoutHeld=false;nitroHeld=false;nitroInput.Reset();ActiveNitro?.Stop();if(blockCamera)orbitCamera?.InputFrame(false,mouseSensitivity.Value,invertMouseY.Value);}
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
            BeginMenuCamera();
            uiFocus=0;uiNavigation.SelectPane(true);uiLastDirection=0;uiRepeat=0;uiWaitNeutral=true;controlsGate.Block();
            BlockDrivingInput();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        else {
            controlsGate.Block();uiActivate=false;uiHorizontal=uiVertical=0;
            StopMenuDrag();SaveMenuPlacement();
            MenuSound(2);
            menuOpen=false;EndMenuCamera();
            if(menuPlayer!=null)menuPlayer.enabled=menuPlayerEnabled;
            ReleaseCameraRumble(menuCamera);
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
        if(!ready)return;
        if(LocalPlayer()==null){if(vehicleLoadGame!=null)DrawSpawnProgress();return;}
        DrawControlHints();
        DrawSpawnProgress();
        DrawDashboard();
        DrawTurretAim();
        DrawHordeHud();
        if(driving&&!Flying&&!hudEnabled.Value&&ActiveNitro!=null&&nitroEnabled.Value)
            UiBox(new Rect(15,Screen.height-120,310,32),(Flying?"LMB + W: Flug-Nitro":"LMB: Nitro / Purge")+" | Tank "+Mathf.RoundToInt(ActiveNitro.Charge*100)+" %");
        if(!menuOpen||audioBlocked)return;
        if(menuMouseLookHeld&&(Event.current.isMouse||Event.current.type==EventType.ScrollWheel))Event.current.Use();
        uiCount=0;uiRegion=0;uiBuilding=false;uiPointerInRegion=true;
        if(Event.current.type==EventType.Repaint)uiBounds.Clear();
        EnsureMenuStyles();
        float scale=Mathf.Max(.1f,Mathf.Min(1,Mathf.Min((Screen.width-32f)/740f,(Screen.height-140f)/560f)));
        HandleMenuDrag(scale);
        var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;
        var oldSkin=GUI.skin;bool oldChanged=GUI.changed;var inputEvent=Event.current.type;
        try {
            GUI.skin=menuSkin;GUI.changed=false;
            GUI.matrix=Matrix4x4.TRS(new Vector3(menuPlacement.X,menuPlacement.Y,0),Quaternion.identity,Vector3.one*scale);
            UiBox(new Rect(0,0,740,560),GUIContent.none,panelStyle);
            var oldColor=GUI.color;GUI.color=MenuAccent;GUI.DrawTexture(new Rect(0,0,740,3),Texture2D.whiteTexture);GUI.color=oldColor;
            UiLabel(new Rect(22,15,650,36),"NFVU2  /  "+(CanAccessCurrent(LocalPlayer())?VehicleName:"Garage"),titleStyle);
            if(UiButton(new Rect(690,19,30,30),"×")){CloseVehicleMenu();return;}
            UiLabel(new Rect(23,57,660,25),"UNDERGROUND GARAGE   /   "+(Flying?DriveModeName:driving?"Fahrzeug bremst":"Tuning & Fahrzeugsteuerung")+"   /   by BCKWDZ",mutedStyle);
            if(menuTab!=10&&menuTab!=11&&!CanAccessCurrent(LocalPlayer()))ChangeMenuTab(10);
            MenuNavigation(CanAccessCurrent(LocalPlayer()));
            uiBuilding=true;
            GUI.BeginGroup(new Rect(180,0,560,522));
            try {
                UiLabel(new Rect(25,99,510,28),MenuNames[menuTab],titleStyle);
                switch(menuTab){case 12:DrawExhaustMenu();break;case 11:DrawInterfaceSettings();break;case 10:DrawGarageMenu();break;case 0:DrawCarMenu();break;case 1:DrawRadioMenu();break;case 2:DrawLightsMenu();break;case 3:DrawNeonMenu();break;case 4:DrawNitroMenu();break;case 5:DrawWheelMenu();break;case 6:DrawBodyMenu();break;case 7:DrawTrunkMenu();break;case 8:DrawDrivingDisplayMenu();break;case 9:DrawTurretMenu();break;}
            }finally{GUI.EndGroup();}
            uiBuilding=false;uiLastCount=uiCount;uiFocus=Math.Min(uiFocus,Math.Max(0,uiCount-1));
            if(Event.current.type==EventType.Repaint){uiActivate=false;uiHorizontal=uiVertical=0;}
            string controllerHint=uiNavigation.Editing?"↑↓←→: Wert ändern · "+ButtonName("A/B","Kreuz/Kreis")+": fertig":"↑↓←→: Navigation · "+ButtonName("A","Kreuz")+": wählen / bearbeiten · "+ButtonName("B","Kreis")+": zurück";
            UiLabel(new Rect(23,523,700,36),padActive?controllerHint+"\n"+ButtonName("LB","L1")+": Kategorien · "+ButtonName("RB","R1")+": Inhalt · L3: schließen · RS: Kamera":menuKey.Value+": schließen · ↑↓: Kategorie\nRMB halten: Kamera · Titelleiste ziehen: verschieben",mutedStyle);
            if(GUI.changed&&(inputEvent==EventType.MouseUp||inputEvent==EventType.MouseDown||inputEvent==EventType.MouseDrag||inputEvent==EventType.KeyDown))MenuSound(0);
        } finally {uiBuilding=false;GUI.matrix=oldMatrix;GUI.enabled=oldEnabled;GUI.skin=oldSkin;GUI.changed|=oldChanged;}
    }
    void DrawCarMenu() {
        UiLabel(new Rect(25,145,510,30),"Fahrmodus",labelStyle);
        int flightIndex=IsHummer?2:3;
        int selected=HordeVehicle?0:Flying&&!current.flight.State.Landing?flightIndex:!IsHummer&&driftMode.Value?2:terrainMode.Value?0:1;
        GUI.enabled=!HordeVehicle;
        int mode=UiToolbar(new Rect(25,180,510,30),selected,IsHummer?new[]{"Gelände","Straße","Fliegen"}:new[]{"Gelände","Straße","Drift","Fliegen"});
        if(mode!=selected)SelectControlMode(mode==flightIndex?3:mode);
        GUI.enabled=true;
        string description=Flying?(current.flight.State.Landing?"Landet automatisch und klappt die Räder aus.\nW/S und A/D: Landeplatz korrigieren · Fliegen: abbrechen":"W/S: vor/zurück · A/D: drehen · Shift: hoch · Leertaste: runter\nOhne Eingabe schweben. Bodenmodus wählen zum Landen."):
            (!driving?"Für den Flugmodus zuerst einsteigen.\n":"")+(mode==2&&!IsHummer?"Drift: Heckantrieb · Leertaste: Handbremse":mode==0?"Geländemodus mit Allradhilfe.":"Straßenmodus mit Heckantrieb.");
        if(padActive)description=Flying?(current.flight.State.Landing?"Landet automatisch und klappt die Räder aus.":ButtonName("RT/LT","R2/L2")+": vor/zurück · LS: drehen · "+ButtonName("RB/LB","R1/L1")+": hoch/runter")+"\nBodenmodus wählen zum Landen.":ButtonName("RT/LT","R2/L2")+": Gas/Bremse · LS: lenken · "+ButtonName("A","Kreuz")+": Handbremse";
        UiLabel(new Rect(25,218,510,52),description,mutedStyle);
        GUI.enabled=!Flying&&(!driving||DriverInputReady);
        if(MenuButton(new Rect(25,280,245,38),driving?"Aussteigen":"Einsteigen")){
            var local=LocalPlayer();CloseVehicleMenu();if(driving)RequestExit();else if(CanAccessCurrent(local))Enter(local);return;
        }
        GUI.enabled=true;
        if(MenuButton(new Rect(285,280,245,38),"Fahrzeug entfernen")){Cleanup();return;}
        keepRunning.Value=UiToggle(new Rect(25,340,510,32),keepRunning.Value," Beim Aussteigen weiterlaufen lassen");
        UiLabel(new Rect(25,378,510,60),"Motor im Leerlauf, aktiviertes Neon und gedämpfte Musik. Auch neben dem Auto ein-/ausschaltbar.\nBeim Entfernen bleibt Inhalt als Kiste zurück.",mutedStyle);
        if(MenuButton(new Rect(25,445,245,38),"Türen und Motorhaube"))ChangeMenuTab(6);
        if(MenuButton(new Rect(285,445,245,38),"Kofferraum-Ausbau"))ChangeMenuTab(7);
        DrawStorageButton(new Rect(25,488,245,30));
        UiLabel(new Rect(285,488,245,30),"Zum Beladen parken und ans Heck gehen.",mutedStyle);
    }
    void DrawNitroMenu() {
        nitroEnabled.Value=UiToggle(new Rect(25,145,510,30),nitroEnabled.Value,IsHummer?" Nitro im Flug aktivieren":" Nitro aktivieren");
        UiLabel(new Rect(25,186,510,66),padActive?ButtonName("X","Quadrat")+": Nitro · "+ButtonName("RT","R2")+": Gas\n"+(IsHummer?"Nitro nur im Flug.":"Nitro am Boden und im Flug. Im Stand: Purge."):IsHummer
            ?"Im Flug W + LMB/Strg: mehr Schub. Am Boden bleibt der H2 ohne Nitro. Sechs Sekunden Tank; Nachladen bei Bewegung."
            :"W + LMB/Strg: Nitro am Boden und im Flug. Im Stand: Purge. Sechs Sekunden Tank; Nachladen bei Bewegung.",labelStyle);
        if(!IsHummer)backfireEnabled.Value=UiToggle(new Rect(25,270,510,30),backfireEnabled.Value," Auspuffflammen beim Gaswegnehmen");
        DrawNitroVisualSettings();
    }

    void DrawRadioMenu() {
        radioEnabled.Value=UiToggle(new Rect(25,143,510,28),radioEnabled.Value," Radio einschalten");
        UiLabel(new Rect(25,185,510,25),"Lautstärke  "+Mathf.RoundToInt(radioVolume.Value*100)+" %",labelStyle);
        radioVolume.Value=UiSlider(new Rect(25,220,505,22),radioVolume.Value,0,1);
        if(vehicleRadio==null||vehicleRadio.Count==0){UiLabel(new Rect(25,260,510,95),"Noch keine Musik verfügbar. Die NFS-Musik muss zuerst lokal importiert werden.",labelStyle);return;}
        GUI.Label(new Rect(25,255,510,48),vehicleRadio.CurrentTitle,labelStyle);
        GUI.enabled=(driving||ParkedRunning)&&radioEnabled.Value;
        if(MenuButton(new Rect(25,310,160,34),"◀ Zurück"))vehicleRadio.Previous();
        if(MenuButton(new Rect(195,310,165,34),vehicleRadio.Paused?"Abspielen":"Pause"))vehicleRadio.TogglePause();
        if(MenuButton(new Rect(370,310,160,34),"Weiter ▶"))vehicleRadio.Next();
        if(padActive&&!uiNavigation.Sidebar&&uiFocus>=uiCount){float y=(uiFocus-uiCount)*44;if(y<songScroll.y)songScroll.y=y;else if(y+44>songScroll.y+130)songScroll.y=y+44-130;}
        uiPointerInRegion=new Rect(25,360,510,130).Contains(Event.current.mousePosition);
        songScroll=GUI.BeginScrollView(new Rect(25,360,510,130),songScroll,new Rect(0,0,488,vehicleRadio.Count*44));
        var titles=vehicleRadio.TrackTitles;uiRegion=1;
        try {for(int i=0;i<vehicleRadio.Count;i++)if(MenuButton(new Rect(0,i*44,483,42),(i==vehicleRadio.CurrentIndex?"▶ ":"")+titles[i],songButtonStyle,false))vehicleRadio.Select(i);}
        finally {uiRegion=0;uiPointerInRegion=true;GUI.EndScrollView();GUI.enabled=true;}
        UiLabel(new Rect(25,493,510,23),driving||ParkedRunning?vehicleRadio.Status:hasBeenDriven?"Zum Musikhören einsteigen oder Weiterlaufen aktivieren.":"Zum Musikhören einmal einsteigen.",mutedStyle);
    }
    void DrawLightsMenu() {
        GUI.enabled=vehicleLighting!=null;
        bool lights=UiToggle(new Rect(25,140,510,32),vehicleLighting!=null&&vehicleLighting.Enabled," Scheinwerfer, Rücklicht und Kennzeichenlicht");
        if(vehicleLighting!=null)vehicleLighting.Enabled=lights;
        bool showOff=UiToggle(new Rect(25,176,510,30),headlightShowOff.Value," Show-off beim Aussteigen: 2× links / 2× rechts");
        if(showOff!=headlightShowOff.Value)headlightShowOff.Value=showOff;
        UiLabel(new Rect(25,207,510,32),"Doppelblinken im Wechsel, dann Pause. Links/rechts aus Fahrersicht. Stoppt beim Einsteigen.",mutedStyle);
        float strength=headlightStrength.Value;
        if(float.IsNaN(strength)||float.IsInfinity(strength))strength=1;
        UiLabel(new Rect(25,242,510,26),"Scheinwerferstärke   "+Mathf.RoundToInt(strength*100)+" %",labelStyle);
        float next=Mathf.Round(UiSlider(new Rect(25,271,510,20),strength,0,3)*20)/20;
        if(next!=headlightStrength.Value)headlightStrength.Value=next;
        UiLabel(new Rect(25,296,510,25),"Scheinwerferfarbe",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(VehicleLighting.HeadlightPresets,headlightPreset.Value));
        int choice=UiGrid(new Rect(25,324,510,112),selected,VehicleLighting.HeadlightPresets,4,songButtonStyle);
        if(choice!=selected)headlightPreset.Value=VehicleLighting.HeadlightPresets[choice];
        if(MenuButton(new Rect(25,446,510,30),"Scheinwerfer zurücksetzen")) {headlightPreset.Value="Serie";headlightStrength.Value=1;headlightShowOff.Value=false;}
        GUI.enabled=true;
        UiLabel(new Rect(25,482,510,36),"Farbe und Stärke gelten auch für die Show. Brems- und Rückfahrlicht bleiben automatisch.",mutedStyle);
    }
    void DrawNeonMenu() {
        GUI.enabled=vehicleNeon!=null;
        bool enabled=UiToggle(new Rect(25,145,510,32),neonEnabled.Value," Unterbodenbeleuchtung einschalten");
        bool pulse=UiToggle(new Rect(25,190,510,32),neonPulsing.Value," Pulsierendes Licht");
        UiLabel(new Rect(25,244,510,30),"Farbe",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(ColorNames,neonColor.Value));
        int choice=UiGrid(new Rect(25,285,510,100),selected,ColorNames,4);
        if(enabled!=neonEnabled.Value||pulse!=neonPulsing.Value||choice!=selected){neonEnabled.Value=enabled;neonPulsing.Value=pulse;neonColor.Value=ColorNames[choice];ApplyNeonSettings();}
        GUI.enabled=true;
        UiLabel(new Rect(25,420,510,70),"Farbe und Pulsmodus bleiben gespeichert. Mit „Beim Aussteigen weiterlaufen lassen“ bleibt aktiviertes Neon auch draußen an.",mutedStyle);
    }
}
}
