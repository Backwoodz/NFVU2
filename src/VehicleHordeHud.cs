using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    GUIStyle hordeTextStyle;
    static readonly Color HordeGold=new Color(1,.8f,.4f);
    static readonly Color HordeWhite=new Color(.94f,.98f,1);
    // Shadowed, freely standing type; no default IMGUI boxes or tiny skin labels.
    void HordeLabel(Rect rect,string text,int size,Color color,TextAnchor alignment=TextAnchor.MiddleCenter) {
        if(hordeTextStyle==null)hordeTextStyle=new GUIStyle(GUI.skin.label){fontStyle=FontStyle.Bold,wordWrap=false,richText=false,padding=new RectOffset(0,0,0,0)};
        hordeTextStyle.fontSize=size;hordeTextStyle.alignment=alignment;
        var old=GUI.color;GUI.color=Color.white;
        hordeTextStyle.normal.textColor=new Color(0,0,0,.85f);
        // Four-sided outline stays legible over snow, sky, foliage and bright effects.
        foreach(var offset in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(2,2)})
            GUI.Label(new Rect(rect.x+offset.x,rect.y+offset.y,rect.width,rect.height),text,hordeTextStyle);
        hordeTextStyle.normal.textColor=color;GUI.Label(rect,text,hordeTextStyle);GUI.color=old;
    }
    void HordeFill(Rect rect,Color color) {
        var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
    }
    void DrawHordeHud() {
        if(audioBlocked||menuOpen||Hud.IsUserHidden())return;
        float scale=Mathf.Min(Mathf.Clamp(Screen.height/1080f,.82f,1.4f),(Screen.width-32)/760f);
        int title=Mathf.RoundToInt(32*scale),body=Mathf.RoundToInt(24*scale),small=Mathf.RoundToInt(22*scale);
        if(!hordeWave.Active) {
            DrawHordeEntryHint();
            if(hordeResult!=null&&hordeResultLeft>0) {
                float w=760*scale,x=(Screen.width-w)/2,y=Screen.height*.23f;
                var lines=Tr(hordeResult).Split('\n');
                HordeFill(new Rect(x+w*.15f,y-12*scale,w*.7f,2*scale),HordeGold);
                for(int i=0;i<lines.Length;i++)HordeLabel(new Rect(x,y+i*44*scale,w,42*scale),lines[i],i==0?title:i==lines.Length-1?small:body,i==0||i==3?HordeGold:HordeWhite);
            }
            return;
        }
        if(hordeWave.Choosing){DrawHordePactChoice();return;}
        float width=760*scale,left=(Screen.width-width)/2,top=20*scale,gap=28*scale,half=(width-gap)/2;
        string wave=Tr("Welle")+" "+hordeWave.Number+" / "+HordeWave.WaveCount;
        int goal=hordeWave.Pending?Mathf.CeilToInt(hordeWave.Size*.85f):hordeWave.KillGoal;
        string remaining=Tr("Abschüsse")+"  "+hordeWaveKills+" / "+goal;
        HordeLabel(new Rect(left,top,half,40*scale),wave,title,HordeGold,TextAnchor.MiddleLeft);
        HordeLabel(new Rect(left+half+gap,top,half,40*scale),remaining,body,HordeWhite,TextAnchor.MiddleRight);
        // Five short milestones give the wave progression a visual rhythm.
        for(int i=0;i<HordeWave.WaveCount;i++)HordeFill(new Rect(left+i*width/5,top+43*scale,width/5-5*scale,3*scale),i<hordeWave.Number?HordeGold:new Color(.12f,.17f,.2f,.8f));
        HordeLabel(new Rect(left,top+52*scale,half,34*scale),hordeRun.Score.ToString("N0")+" "+Tr("Punkte"),body,HordeWhite,TextAnchor.MiddleLeft);
        HordeLabel(new Rect(left+half+gap,top+52*scale,half,34*scale),Tr("Combo")+"  x"+hordeRun.Multiplier+"  /  "+hordeRun.Combo,body,hordeRun.Multiplier>1?HordeGold:HordeWhite,TextAnchor.MiddleRight);
        if(hordeRun.ComboLeft>0)HordeFill(new Rect(left+width-180*scale,top+88*scale,180*scale*hordeRun.ComboLeft/HordeRun.ComboWindow,2*scale),HordeGold);
        DrawHordeBar(new Rect(left,top+96*scale,half,43*scale),hordeRun.Health/HordeRun.MaxHealth,
            hordeRun.Health<250?new Color(1,.3f,.18f):HordeArenaVisuals.ColorFor(HordePowerup.Repair),"HP",Mathf.CeilToInt(hordeRun.Health)+" / 1000",small,scale);
        DrawHordeBar(new Rect(left+half+gap,top+96*scale,half,43*scale),hordeRun.Shield/HordeRun.MaxShield,
            HordeArenaVisuals.ColorFor(HordePowerup.Shield),Tr("Schild"),Mathf.CeilToInt(hordeRun.Shield)+" / 300",small,scale);
        bool elemental=HordePacts.Elemental(hordeRun.Pact);
        HordeLabel(new Rect(left,top+147*scale,half,32*scale),elemental?Tr(HordeAmmoName):Tr("Explosiv")+"  "+(hordeRun.Pact==HordePact.Thor?Tr("ganze Welle"):hordeRun.ExplosiveRounds+" / 30"),small,
            elemental?HordeAmmoTint:hordeRun.Explosive?HordeArenaVisuals.ColorFor(HordePowerup.Explosive):new Color(.65f,.7f,.75f),TextAnchor.MiddleLeft);
        HordeLabel(new Rect(left+half+gap,top+147*scale,half,32*scale),"AUTO  "+(hordeRun.Pact==HordePact.Valkyrie?Tr("ganze Welle"):Mathf.CeilToInt(hordeRun.AutoLeft)+" s"),small,
            hordeRun.Automatic?HordeArenaVisuals.ColorFor(HordePowerup.Automatic):new Color(.65f,.7f,.75f),TextAnchor.MiddleRight);
        {
            HordeLabel(new Rect(left,top+182*scale,width,29*scale),Tr(HordePacts.Name(hordeRun.Pact))+" · "+Tr("Elite übrig")+": "+hordeRequiredAlive,small,HordeGold);
            if(hordeBoss?.Character!=null&&!hordeBoss.Killed)DrawHordeBar(new Rect(left,top+218*scale,width,43*scale),hordeBoss.Character.GetHealth()/hordeBoss.Character.GetMaxHealth(),hordeBossEnraged?new Color(1,.35f,.2f):new Color(.5f,.8f,1),hordeBossEnraged?Tr("Moder · Zornphase"):"Moder",Mathf.CeilToInt(hordeBoss.Character.GetHealth())+" HP",small,scale);
            if(hordeBonus.Left>0)HordeLabel(new Rect(left,top+218*scale,width,32*scale),Tr("Jagdauftrag")+": "+Mathf.CeilToInt(hordeBonus.Left)+" s · +500 "+Tr("Punkte")+" / +250 HP",small,HordeGold);
            DrawHordeTarget(hordeBonus.Left>0?hordeBonusTarget:hordeBoss,scale);
        }
        if(hordeVisuals!=null&&Camera.main!=null)foreach(var pickup in hordeVisuals.Pickups) {
            var screen=Camera.main.WorldToScreenPoint(pickup.Point+Vector3.up*3);
            if(screen.z<=0||screen.x<0||screen.x>Screen.width||screen.y<0||screen.y>Screen.height)continue;
            float w=300*scale,px=Mathf.Clamp(screen.x-w/2,8,Screen.width-w-8),py=Mathf.Clamp(Screen.height-screen.y,top+188*scale,Screen.height-70*scale);
            HordeLabel(new Rect(px,py,w,32*scale),Tr(PowerupName(pickup.Kind)),small,HordeArenaVisuals.ColorFor(pickup.Kind));
            HordeLabel(new Rect(px,py+29*scale,w,28*scale),Mathf.RoundToInt(Vector3.Distance(pickup.Point,hordeVehicle.car.transform.position))+" m",Mathf.RoundToInt(19*scale),HordeWhite);
        }
    }
    void DrawHordeTarget(HordeMember target,float scale) {
        if(target?.Character==null||target.Killed||Camera.main==null)return;
        var screen=Camera.main.WorldToScreenPoint(target.Character.GetTopPoint()+Vector3.up);
        if(screen.z<=0)return;
        float width=240*scale;
        HordeLabel(new Rect(screen.x-width/2,Screen.height-screen.y,width,34*scale),"▼ "+Tr(target==hordeBoss?"Moder":"Jagdauftrag"),Mathf.RoundToInt(24*scale),HordeGold);
    }
    void DrawHordeBar(Rect rect,float fill,Color color,string label,string value,int size,float scale) {
        HordeLabel(new Rect(rect.x,rect.y,rect.width*.45f,30*scale),label,size,color,TextAnchor.MiddleLeft);
        HordeLabel(new Rect(rect.x+rect.width*.45f,rect.y,rect.width*.55f,30*scale),value,size,HordeWhite,TextAnchor.MiddleRight);
        var track=new Rect(rect.x,rect.y+34*scale,rect.width,7*scale);
        HordeFill(new Rect(track.x-1,track.y-1,track.width+2,track.height+2),new Color(0,0,0,.7f));
        HordeFill(track,new Color(.12f,.17f,.2f,.8f));
        HordeFill(new Rect(track.x,track.y,track.width*Mathf.Clamp01(fill),track.height),color);
    }
}
}
