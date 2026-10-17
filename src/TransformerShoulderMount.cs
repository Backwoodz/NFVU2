using UnityEngine;
namespace Rachel350Z {
internal sealed partial class TransformerVisual {
    Transform leftShoulderMount;
    public Transform LeftShoulderMount {
        get {
            if(leftShoulderMount==null) {
                leftShoulderMount=new GameObject("Transformer_LeftShoulderWeaponMount").transform;
                leftShoulderMount.SetParent(Root.transform,false);UpdateShoulderMount();
            }
            return leftShoulderMount;
        }
    }
    void UpdateShoulderMount() {
        if(leftShoulderMount==null)return;
        // The legacy rig labels +X as L. For the +Z-facing robot, physical
        // left is -X, so use its existing R carrier without renaming gait bones.
        int clavicle=ids["clavicle.R"];
        // Follow the left shoulder's real animated carrier. Its neutral inverse
        // leaves an upright mounting frame for the weapon's own yaw/pitch rig.
        var frame=pose[clavicle]*neutral[clavicle].inverse
            *Matrix4x4.Translate(new Vector3(-.83f,4.10f,-.04f));
        Apply(leftShoulderMount,frame);
    }
}
}
