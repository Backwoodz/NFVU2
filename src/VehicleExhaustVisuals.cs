using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
// Original parts are created by the staged loader and owned by the vehicle.
// This selector only changes visibility; it allocates no meshes or materials.
public sealed class VehicleExhaustVisuals {
    readonly Dictionary<GameObject,int> pieces=new Dictionary<GameObject,int>();
    int selected=-1;
    public bool Available=>pieces.Count>0;
    public VehicleExhaustVisuals(GameObject car,ModelData model) {
        if(model.exhaustVariants==null)return;
        foreach(var part in model.parts)if(part.optionGroup=="exhaust") {
            var node=car.transform.Find(part.name);
            if(node!=null&&int.TryParse(part.option,out int style))pieces[node.gameObject]=style;
        }
    }
    public void Apply(int style) {
        style=Mathf.Clamp(style,1,10);
        if(selected==style)return;selected=style;
        foreach(var entry in pieces)if(entry.Key!=null)entry.Key.SetActive(entry.Value==style);
    }
}
}
