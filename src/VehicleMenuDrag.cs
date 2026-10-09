using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<float> menuPositionX,menuPositionY;
    MenuPlacement menuPlacement;
    int menuDragControl;
    void InitializeMenuPlacement() {
        menuPositionX=Config.Bind("Menu","PositionX",16f,"Menu left edge in screen pixels. Drag the title bar to move it.");
        menuPositionY=Config.Bind("Menu","PositionY",16f,"Menu top edge in screen pixels. Automatically kept inside the screen.");
        menuPlacement=new MenuPlacement(menuPositionX.Value,menuPositionY.Value);
    }
    void SaveMenuPlacement() {
        if(menuPlacement==null)return;
        menuPositionX.Value=menuPlacement.X;menuPositionY.Value=menuPlacement.Y;
    }
    void StopMenuDrag() {
        if(menuPlacement==null||!menuPlacement.Dragging)return;
        menuPlacement.End();
        if(GUIUtility.hotControl==menuDragControl)GUIUtility.hotControl=0;
        menuDragControl=0;SaveMenuPlacement();
    }
    // Called before applying GUI.matrix, so pointer and window positions are
    // both in screen pixels. The close button and all content stay outside.
    void HandleMenuDrag(float scale) {
        menuPlacement.Constrain(Screen.width,Screen.height,740*scale,560*scale);
        int id=GUIUtility.GetControlID(0x4e46564d,FocusType.Passive);
        var e=Event.current;
        var header=new Rect(menuPlacement.X+10*scale,menuPlacement.Y+8*scale,665*scale,75*scale);
        switch(e.GetTypeForControl(id)) {
            case EventType.MouseDown:
                if(e.button==0&&GUIUtility.hotControl==0&&header.Contains(e.mousePosition)) {
                    GUIUtility.hotControl=id;GUIUtility.keyboardControl=0;menuDragControl=id;
                    menuPlacement.Begin(e.mousePosition.x,e.mousePosition.y);e.Use();
                }
                break;
            case EventType.MouseDrag:
                if(GUIUtility.hotControl==id&&menuPlacement.Dragging) {
                    menuPlacement.Move(e.mousePosition.x,e.mousePosition.y,Screen.width,Screen.height,740*scale,560*scale);e.Use();
                }
                break;
            case EventType.MouseUp:
                if(e.button==0&&GUIUtility.hotControl==id&&menuPlacement.Dragging){StopMenuDrag();e.Use();}
                break;
        }
    }
}
}
