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
            // Rotate in menu-local coordinates, then apply the menu's placement
            // and scale. RotateAroundPivot left-multiplies GUI.matrix and moves
            // the sticker away from its row when the menu is transformed.
            var pivot=new Vector3(badge.center.x,badge.center.y,0);
            GUI.matrix=saved*Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,-12),Vector3.one)
                *Matrix4x4.TRS(-pivot,Quaternion.identity,Vector3.one);
            GUI.color=Color.white;
            GUI.DrawTexture(badge,logo,ScaleMode.ScaleToFit,true);
        } finally {GUI.matrix=saved;GUI.color=color;}
    }
}
}
