using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rachel350Z {
public partial class Plugin {
    KeyHints vehicleHintOwner;
    GameObject[] originalHintGroups;
    bool[] originalHintStates;
    RectTransform vehicleHintPanel;
    TextMeshProUGUI vehicleHintTitle,vehicleHintLeft,vehicleHintRight;
    Canvas vehicleHintCanvas;
    string vehicleHintKey;
    int vehicleHintRows;
    float vehicleHintHeight;
    bool vehicleHintFailed;
    bool NativeVehicleHintsVisible=>vehicleHintPanel!=null&&vehicleHintPanel.gameObject.activeInHierarchy;
    float VehicleHintBottomInset=>NativeVehicleHintsVisible?vehicleHintHeight+32:90;

    void RestoreVehicleHints() {
        if(originalHintGroups!=null)for(int i=0;i<originalHintGroups.Length;i++)
            if(originalHintGroups[i]!=null)originalHintGroups[i].SetActive(originalHintStates[i]);
        originalHintGroups=null;originalHintStates=null;vehicleHintOwner=null;
        if(vehicleHintPanel!=null)vehicleHintPanel.gameObject.SetActive(false);
    }
    void DisposeVehicleHints() {
        RestoreVehicleHints();
        if(vehicleHintPanel!=null)Destroy(vehicleHintPanel.gameObject);
        vehicleHintPanel=null;vehicleHintCanvas=null;vehicleHintKey=null;
    }
    TextMeshProUGUI CreateVehicleHintText(string name,TMP_Text source) {
        var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(vehicleHintPanel,false);
        var text=obj.AddComponent<TextMeshProUGUI>();
        text.font=source.font;text.fontSharedMaterial=source.fontSharedMaterial;text.spriteAsset=source.spriteAsset;
        text.color=Color.white;text.richText=true;text.raycastTarget=false;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.alignment=TextAlignmentOptions.TopLeft;
        text.overflowMode=TextOverflowModes.Truncate;
        return text;
    }
    void CreateVehicleHints(KeyHints owner) {
        var source=owner.m_buildMenuKey!=null?owner.m_buildMenuKey:owner.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        var canvas=owner.GetComponentInParent<Canvas>();
        if(source==null||canvas==null)return;
        vehicleHintCanvas=canvas.rootCanvas;
        vehicleHintPanel=new GameObject("NFVU2 Vehicle Controls",typeof(RectTransform)).GetComponent<RectTransform>();
        vehicleHintPanel.SetParent(vehicleHintCanvas.transform,false);
        vehicleHintPanel.anchorMin=vehicleHintPanel.anchorMax=vehicleHintPanel.pivot=new Vector2(1,0);
        var background=vehicleHintPanel.gameObject.AddComponent<Image>();
        background.color=new Color(.025f,.035f,.045f,.8f);background.raycastTarget=false;
        vehicleHintTitle=CreateVehicleHintText("Mode",source);
        vehicleHintTitle.color=new Color(1,.84f,.45f);
        vehicleHintLeft=CreateVehicleHintText("Left controls",source);
        vehicleHintRight=CreateVehicleHintText("Right controls",source);
        vehicleHintKey=null;
    }
    static void PlaceVehicleHint(TextMeshProUGUI text,float x,float y,float width,float height,float size,float lineHeight) {
        var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        text.fontSize=size;
        // Fixed line height keeps both columns aligned even for wide controller glyphs.
        text.text="<line-height="+lineHeight.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+">"+text.text;
    }
    void UpdateVehicleHints() {
        var owner=KeyHints.instance;
        if(!driving||audioBlocked||owner==null||Hud.instance==null||Hud.IsUserHidden()){RestoreVehicleHints();return;}
        try {
            if(vehicleHintOwner!=owner) {
                DisposeVehicleHints();vehicleHintOwner=owner;
                originalHintGroups=new[]{owner.m_combatHints,owner.m_buildHints,owner.m_fishingHints,owner.m_inventoryHints,owner.m_inventoryWithContainerHints,owner.m_barberHints,owner.m_radialHints};
                originalHintStates=originalHintGroups.Select(group=>group!=null&&group.activeSelf).ToArray();
            }
            // Native KeyHints.Update runs first and may re-enable these each frame.
            foreach(var group in originalHintGroups)if(group!=null)group.SetActive(false);
            if(vehicleHintFailed)return;
            if(vehicleHintPanel==null)CreateVehicleHints(owner);
            if(vehicleHintPanel==null)return;
            vehicleHintPanel.gameObject.SetActive(true);
            float scale=Mathf.Min(hintScale.Value,Mathf.Min((Screen.width-36)/550f,Screen.height/850f));
            float units=scale/Mathf.Max(.01f,vehicleHintCanvas.scaleFactor);
            string title=Transforming?"Transformer" :menuOpen?Tr("Modmenü"):Tr(DriveModeName);
            string key=padActive+"|"+Playstation+"|"+Flying+"|"+menuOpen+"|"+IsHummer+"|"+uiLanguage.Value+"|"+menuKey.Value+"|"+title+"|"+units+"|"+transformerEnabled.Value+"|"+transformerKey.Value;
            if(key!=vehicleHintKey) {
                var rows=Transforming?TransformerHints():VehicleHintContent.Build(padActive,Playstation,Flying,menuOpen,IsHummer,menuKey.Value.ToString(),Tr);
                if(!Transforming&&!Flying&&!menuOpen&&transformerEnabled.Value&&!IsHummer&&current.transformerAsset!=null)
                    rows=rows.Concat(new[]{TransformerToggleHint+"  Verwandeln (im Stand)"}).ToArray();
                vehicleHintRows=(rows.Length+1)/2;
                vehicleHintTitle.text=title;
                vehicleHintLeft.text=string.Join("\n",rows.Take(vehicleHintRows));
                vehicleHintRight.text=string.Join("\n",rows.Skip(vehicleHintRows));
                PlaceVehicleHint(vehicleHintTitle,14*units,10*units,522*units,29*units,20*units,29*units);
                PlaceVehicleHint(vehicleHintLeft,14*units,42*units,258*units,vehicleHintRows*29*units,18*units,29*units);
                PlaceVehicleHint(vehicleHintRight,284*units,42*units,252*units,vehicleHintRows*29*units,18*units,29*units);
                vehicleHintKey=key;
            }
            vehicleHintHeight=(52+vehicleHintRows*29)*scale;
            vehicleHintPanel.sizeDelta=new Vector2(550*units,(52+vehicleHintRows*29)*units);
            vehicleHintPanel.anchoredPosition=new Vector2(-18,-16)/Mathf.Max(.01f,vehicleHintCanvas.scaleFactor);
        }catch(Exception error) {
            vehicleHintFailed=true;DisposeVehicleHints();Logger.LogWarning("Vehicle icon hints unavailable; using text hints: "+error);
        }
    }
}
}
