using UnityEngine;
namespace Rachel350Z {
public sealed partial class VehicleOrbitCamera {
    // Reset spatial smoothing after a transfer without recapturing the cursor's
    // original state (Begin is reserved for actually entering the vehicle).
    internal void SnapDriftTransfer() {
        if(!active||vehicle==null)return;
        ResetTransformationShot();footsteps.Reset();hadInput=false;
        yaw=0;chase.Reset(vehicle.eulerAngles.y);smoothedDistance=distance;
        smoothedPivot=vehicle.position+Vector3.up*(suv?1.5f:1.05f);
    }
}
}
