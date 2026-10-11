using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Rachel350Z {
// A local UI overlay, not saved/shared map pins. Reuses at most 100 dot images.
public sealed class HordeRadar : IDisposable {
    readonly List<RectTransform> dots=new List<RectTransform>();
    Texture2D texture;
    Sprite sprite;
    GameObject root;
    RawImage mapImage;
    bool disposed;
    void Create(RawImage map) {
        if(root!=null)UnityEngine.Object.Destroy(root);dots.Clear();mapImage=map;
        if(sprite==null) {
            const int size=32;texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="NFVU2_HordeRadarDot",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float radius=new Vector2(x-15.5f,y-15.5f).magnitude;
                // Red center, dark outline and a softly antialiased edge.
                var color=Color.Lerp(new Color(1,.08f,.06f),new Color(.12f,.015f,.015f),Mathf.Clamp01((radius-10.5f)/1.5f));
                color.a=Mathf.Clamp01(15-radius);colors[y*size+x]=color;
            }
            texture.SetPixels(colors);texture.Apply(false,true);
            sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }
        root=new GameObject("NFVU2_HordeRadar",typeof(RectTransform));
        var rect=(RectTransform)root.transform;rect.SetParent(map.rectTransform,false);
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
    public void Hide(){if(root!=null)root.SetActive(false);}
    public void Draw(Minimap map,IReadOnlyList<Vector3> enemies) {
        if(disposed)return;
        if(map==null||map.m_mode!=Minimap.MapMode.Small||map.m_mapImageSmall==null||!map.m_mapImageSmall.gameObject.activeInHierarchy||Hud.IsUserHidden()){Hide();return;}
        if(root==null||mapImage!=map.m_mapImageSmall)Create(map.m_mapImageSmall);
        root.SetActive(true);
        var bounds=mapImage.rectTransform.rect;int visible=0;
        for(int i=0;i<enemies.Count&&visible<HordeWave.MaxAlive;i++) {
            if(!HordeMapProjection.TryProject(enemies[i],map.m_pixelSize,map.m_textureSize,mapImage.uvRect,new Vector2(bounds.width,bounds.height),out var position))continue;
            if(visible==dots.Count) {
                var go=new GameObject("EnemyDot",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(root.transform,false);
                var image=go.GetComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
                var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,.5f);
                rect.sizeDelta=Vector2.one*HordeMapProjection.DotSize;dots.Add(rect);
            }
            var dot=dots[visible++];dot.anchoredPosition=position;dot.gameObject.SetActive(true);
        }
        for(int i=visible;i<dots.Count;i++)dots[i].gameObject.SetActive(false);
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        if(root!=null)UnityEngine.Object.Destroy(root);dots.Clear();
        if(sprite!=null)UnityEngine.Object.Destroy(sprite);if(texture!=null)UnityEngine.Object.Destroy(texture);
        root=null;mapImage=null;
    }
}
public partial class Plugin {
    HordeRadar hordeRadar;
    readonly List<Vector3> hordeRadarPositions=new List<Vector3>(HordeWave.MaxAlive);
    void TickHordeRadar() {
        if(!hordeWave.Active||!HordeGameUsable||hordeManager!=ZDOMan.instance){hordeRadar?.Dispose();hordeRadar=null;return;}
        if(hordeRadar==null)hordeRadar=new HordeRadar();
        if(menuOpen||!Application.isFocused){hordeRadar.Hide();return;}
        hordeRadarPositions.Clear();
        foreach(var enemy in horde) {
            if(enemy.Killed)continue;
            if(enemy.Character!=null) {
                if(!enemy.Character.IsDead())hordeRadarPositions.Add(enemy.Character.transform.position);
            }else {
                var data=hordeManager.GetZDO(enemy.Id);
                if(data!=null)hordeRadarPositions.Add(data.GetPosition());
            }
        }
        hordeRadar.Draw(Minimap.instance,hordeRadarPositions);
    }
}
}
