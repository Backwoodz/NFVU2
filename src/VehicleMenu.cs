using System;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<KeyboardShortcut> menuKey;
    ConfigEntry<bool> radioEnabled, neonEnabled, neonPulsing;
    ConfigEntry<float> radioVolume;
    ConfigEntry<string> neonColor;
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
        menuKey=Config.Bind("Controls","VehicleMenu",new KeyboardShortcut(KeyCode.F9),"Open the vehicle menu while seated or within 5 metres of the car.");
        radioEnabled=Config.Bind("Radio","Enabled",true,"Play the local NFS radio while seated.");
        radioVolume=Config.Bind("Radio","Volume",.55f,new ConfigDescription("Radio volume, independent of engine volume.",new AcceptableValueRange<float>(0,1)));
        neonEnabled=Config.Bind("Neon","Enabled",false,"Enable underbody neon while seated.");
        neonPulsing=Config.Bind("Neon","Pulsing",false,"Pulse the underbody neon.");
        neonColor=Config.Bind("Neon","Color","Grün",new ConfigDescription("Underbody neon colour.",new AcceptableValueList<string>(ColorNames)));
    }
    bool CanUseVehicleMenu(MonoBehaviour local)=>car!=null&&local!=null&&!Multiplayer()&&(driving||Vector3.Distance(local.transform.position,car.transform.position)<5);
    void BlockDrivingInput(){throttle=steer=0;brake=1;honking=false;orbitCamera?.InputFrame(false,mouseSensitivity.Value,invertMouseY.Value);}
    void SetVehicleMenu(bool open) {
        if(open==menuOpen)return;
        if(open){
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
    }
    void DisposeMenu(){if(menuBackground!=null)Destroy(menuBackground);menuBackground=null;panelStyle=titleStyle=labelStyle=mutedStyle=songButtonStyle=null;}
    void OnGUI() {
        if(!ready||LocalPlayer()==null)return;
        var text=driving?(terrainMode.Value?"Gelände / Allradhilfe":"Straße / Heckantrieb")+" | WASD: fahren | Leertaste: bremsen | H: Hupe | F7: aussteigen\nMaus: umschauen | Mausrad: Zoom | "+menuKey.Value+": Fahrzeugmenü":status;
        GUI.Box(new Rect(15,Screen.height-80,Mathf.Min(920,Screen.width-30),60),text);
        if(!menuOpen||audioBlocked)return;
        EnsureMenuStyles();
        float scale=Mathf.Min(1,Mathf.Min(Screen.width/600f,Screen.height/610f));
        var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;
        try {
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-560*scale)/2,(Screen.height-560*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            GUI.Box(new Rect(0,0,560,560),GUIContent.none,panelStyle);
            GUI.Label(new Rect(22,15,480,36),"NFVU2  /  350Z",titleStyle);
            if(GUI.Button(new Rect(513,17,28,28),"×")){CloseVehicleMenu();return;}
            GUI.Label(new Rect(23,55,510,25),driving?"Fahrzeug bremst · Radio bleibt bedienbar":"Fahrzeugsteuerung",mutedStyle);
            menuTab=GUI.Toolbar(new Rect(22,90,516,34),menuTab,new[]{"Fahrzeug","Radio","Licht","Neon"});
            switch(menuTab){case 0:DrawCarMenu();break;case 1:DrawRadioMenu();break;case 2:DrawLightsMenu();break;case 3:DrawNeonMenu();break;}
            GUI.Label(new Rect(23,522,516,28),menuKey.Value+": schließen  ·  Einstellungen werden gespeichert",mutedStyle);
        } finally {GUI.matrix=oldMatrix;GUI.enabled=oldEnabled;}
    }
    void DrawCarMenu() {
        GUI.Label(new Rect(25,145,510,30),"Fahrmodus",labelStyle);
        bool terrain=GUI.Toggle(new Rect(25,180,500,30),terrainMode.Value," Gelände / Allradhilfe");
        if(terrain!=terrainMode.Value){terrainMode.Value=terrain;handling?.SetTerrainMode(terrain);}
        GUI.Label(new Rect(25,215,510,45),terrain?"Für kurze Strecken und unebenen Boden.":"Straßenmodus mit Heckantrieb.",mutedStyle);
        if(GUI.Button(new Rect(25,280,245,38),driving?"Aussteigen":"Einsteigen")){
            var local=LocalPlayer();CloseVehicleMenu();if(driving)Exit();else if(CanUseVehicleMenu(local))Enter(local);return;
        }
        if(GUI.Button(new Rect(285,280,245,38),"Fahrzeug entfernen")){Cleanup();return;}
        GUI.Label(new Rect(25,345,510,27),"Karosserie",labelStyle);
        GUI.enabled=false;
        GUI.Button(new Rect(25,383,245,38),"Türen · noch nicht verfügbar");
        GUI.Button(new Rect(285,383,245,38),"Kofferraum · noch nicht verfügbar");
        GUI.enabled=true;
        GUI.Label(new Rect(25,436,510,52),"Öffnen und Schließen wird mit den späteren Fahrzeuganimationen ergänzt.",mutedStyle);
    }
    void DrawRadioMenu() {
        radioEnabled.Value=GUI.Toggle(new Rect(25,143,510,28),radioEnabled.Value," Radio beim Fahren einschalten");
        GUI.Label(new Rect(25,185,510,25),"Lautstärke  "+Mathf.RoundToInt(radioVolume.Value*100)+" %",labelStyle);
        radioVolume.Value=GUI.HorizontalSlider(new Rect(25,220,505,22),radioVolume.Value,0,1);
        if(vehicleRadio==null||vehicleRadio.Count==0){GUI.Label(new Rect(25,260,510,95),"Noch keine Musik verfügbar. Die NFS-Musik muss zuerst lokal importiert werden.",labelStyle);return;}
        GUI.Label(new Rect(25,255,510,48),vehicleRadio.CurrentTitle,labelStyle);
        GUI.enabled=driving&&radioEnabled.Value;
        if(GUI.Button(new Rect(25,310,160,34),"◀ Zurück"))vehicleRadio.Previous();
        if(GUI.Button(new Rect(195,310,165,34),vehicleRadio.Paused?"Abspielen":"Pause"))vehicleRadio.TogglePause();
        if(GUI.Button(new Rect(370,310,160,34),"Weiter ▶"))vehicleRadio.Next();
        songScroll=GUI.BeginScrollView(new Rect(25,360,510,130),songScroll,new Rect(0,0,488,vehicleRadio.Count*44));
        var titles=vehicleRadio.TrackTitles;
        try {for(int i=0;i<vehicleRadio.Count;i++)if(GUI.Button(new Rect(0,i*44,483,42),(i==vehicleRadio.CurrentIndex?"▶ ":"")+titles[i],songButtonStyle))vehicleRadio.Select(i);}
        finally {GUI.EndScrollView();GUI.enabled=true;}
        GUI.Label(new Rect(25,493,510,23),driving?vehicleRadio.Status:"Zum Musikhören einsteigen.",mutedStyle);
    }
    void DrawLightsMenu() {
        GUI.Label(new Rect(25,145,510,50),"Beim Einsteigen geht das Fahrlicht automatisch an.",labelStyle);
        GUI.enabled=vehicleLighting!=null;
        bool lights=GUI.Toggle(new Rect(25,213,510,32),vehicleLighting!=null&&vehicleLighting.Enabled," Scheinwerfer, Rücklicht und Kennzeichenlicht");
        if(vehicleLighting!=null)vehicleLighting.Enabled=lights;
        GUI.enabled=true;
        GUI.Label(new Rect(25,270,510,90),"Brems- und weiße Rückfahrleuchten reagieren auf Bremse und Rückwärtsgang. Beim Aussteigen schaltet sich das Fahrlicht aus.",mutedStyle);
        GUI.Label(new Rect(25,370,510,35),"Kennzeichen: Original NFS Underground 2",labelStyle);
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
        GUI.Label(new Rect(25,420,510,60),"Farbe und Pulsmodus bleiben gespeichert. Die Unterbodenbeleuchtung wird beim Fahren aktiv.",mutedStyle);
    }
}
}
