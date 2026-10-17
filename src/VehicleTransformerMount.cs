using System.Reflection;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    static readonly FieldInfo robotFallHeight=typeof(Character).GetField("m_maxAirAltitude",BindingFlags.Instance|BindingFlags.NonPublic);
    static readonly FieldInfo robotFallTimer=typeof(Character).GetField("m_fallTimer",BindingFlags.Instance|BindingFlags.NonPublic);
    static void ResetRobotDismountFall(Player rider) {
        if(rider==null)return;
        robotFallHeight?.SetValue(rider,rider.transform.position.y);
        robotFallTimer?.SetValue(rider,0f);
    }
    bool PrepareRobotEntry(MonoBehaviour local,out Vector3 ground) {
        ground=local!=null?local.transform.position:Vector3.zero;
        // The rider routes the approach to the rear. Position, terrain and
        // nearby scenery never veto boarding; settling happens after taking control.
        return local!=null&&current.interior!=null&&current.transformer!=null;
    }
    void RequestRobotExit() {
        if(!driving||!Transforming)return;
        if(cabinDriver!=null&&cabinDriver.RobotTransitionActive&&!cabinDriver.RobotTransitionEntering)return;
        var t=current.transformer;
        if(transformerEntryPending||cabinDriver!=null&&cabinDriver.RobotTransitionActive) {
            // Exit during approach/climbing always releases the rider immediately.
            Exit(true,TransformerMountClearance.FindLanding(car.transform,player.transform));return;
        }
        if(cabinDriver==null||!t.State.CanWalk||t.ReturnRequested) {
            Exit(true,TransformerMountClearance.FindLanding(car.transform,player.transform));return;
        }
        transformerExitPending=true;BlockDrivingInput();
        status="Transformer hält zum Absteigen an …";
    }
    // Return true only when Exit has released the native player/camera.
    bool TickRobotMount(VehicleTransformer t,bool blocked) {
        if(transformerEntryPending&&t.ReadyForMount&&t.Visual.ReadyToWalk&&!blocked) {
            t.Visual.CacheMountClimbRoute();
            if(!cabinDriver.TryBeginRobotEntry(lastExitPosition)) {
                Exit(true,lastExitPosition);return true;
            }
            transformerEntryPending=false;
        }
        if(transformerExitPending&&!transformerEntryPending&&!cabinDriver.RobotTransitionActive
            &&t.ReadyForMount&&t.Visual.ReadyToWalk&&!blocked) {
            transformerExitPending=false;
            var landing=TransformerMountClearance.FindLanding(car.transform,player.transform);
            if(!cabinDriver.TryBeginRobotExit(landing)) {
                Exit(true,landing);return true;
            }
        }
        if(cabinDriver.RobotTransitionActive) {
            cabinDriver.TickRobotTransition(t.Visual,Time.deltaTime,blocked);
            if(cabinDriver.RobotTransitionComplete&&!cabinDriver.RobotTransitionEntering) {
                var landing=cabinDriver.RobotExitPosition;
                // Always hand native control back, including if scenery moved
                // into the landing during the animation. Keep the robot parked.
                Exit(true,landing);
                return true;
            }
        } else if(!transformerEntryPending)cabinDriver.TickTransformer(t.Visual,t.State.Progress);
        return false;
    }
}
}
