using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    void DrawRustMenuBadge(Rect button) {
        var logo=menuAssets?.RustLogo;
        if(logo==null)return;
        var saved=GUI.matrix;
        var color=GUI.color;
        try {
            // Small slanted sticker at the right; leave the category label legible.
            var badge=new Rect(button.xMax-58,button.y+8,53,14);
            GUIUtility.RotateAroundPivot(-12,badge.center);
            GUI.color=Color.white;
            GUI.DrawTexture(badge,logo,ScaleMode.ScaleToFit,true);
        } finally {GUI.matrix=saved;GUI.color=color;}
    }
}
}
