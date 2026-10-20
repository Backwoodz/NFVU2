using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    readonly Vector3[] hordeHintCorners=new Vector3[4];
    void DrawHordeEntryHint() {
        if(DriftSessionRules.Active)return;
        if(!driving||IsHummer||Flying||!DriverInputReady||Multiplayer()||turret==null||!turret.ExplosiveAvailable)return;
        var map=Minimap.instance;
        if(map==null||map.m_mode!=Minimap.MapMode.Small||map.m_mapImageSmall==null||!map.m_mapImageSmall.gameObject.activeInHierarchy)return;
        var rect=map.m_mapImageSmall.rectTransform;
        var canvas=rect.GetComponentInParent<Canvas>()?.rootCanvas;
        Camera uiCamera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
        // Follow the actual minimap and biome label, including native HUD scaling.
        rect.GetWorldCorners(hordeHintCorners);
        var bottomLeft=RectTransformUtility.WorldToScreenPoint(uiCamera,hordeHintCorners[0]);
        var bottomRight=RectTransformUtility.WorldToScreenPoint(uiCamera,hordeHintCorners[3]);
        float bottom=Screen.height-Mathf.Min(bottomLeft.y,bottomRight.y);
        if(map.m_biomeNameSmall!=null&&map.m_biomeNameSmall.gameObject.activeInHierarchy) {
            map.m_biomeNameSmall.rectTransform.GetWorldCorners(hordeHintCorners);
            foreach(var corner in hordeHintCorners)bottom=Mathf.Max(bottom,Screen.height-RectTransformUtility.WorldToScreenPoint(uiCamera,corner).y);
        }
        float scale=Mathf.Clamp(Screen.height/1080f,.8f,1.3f);
        float width=Mathf.Min(320*scale,Screen.width-16);
        float x=Mathf.Clamp((bottomLeft.x+bottomRight.x-width)/2,8,Screen.width-width-8);
        float y=Mathf.Clamp(bottom+12*scale,8,Screen.height-80*scale);
        HordeFill(new Rect(x+width*.15f,y,width*.7f,2*scale),HordeGold);
        HordeLabel(new Rect(x,y+7*scale,width,29*scale),Tr("Stelle dich der Horde"),Mathf.RoundToInt(22*scale),HordeGold);
        HordeLabel(new Rect(x,y+37*scale,width,25*scale),Tr("Numpad 9 · Kampf beginnen"),Mathf.RoundToInt(18*scale),HordeWhite);
    }
}
}
