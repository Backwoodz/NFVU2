namespace Rachel350Z {
// Session state is independent of Unity rendering so entry, manual override,
// braking and exit behaviour can be exercised without a running game.
public sealed class VehicleLightingState {
    bool previousDriving;
    bool reverseSelected;
    bool hasBeenDriven;
    double showOffTime;
    public bool ShowOffEnabled { get; set; }
    public bool ShowOffActive { get; private set; }
    public bool LeftHeadlightOn => ShowOffActive ? showOffTime<.16 || showOffTime>=.30&&showOffTime<.46 : Enabled;
    public bool RightHeadlightOn => ShowOffActive ? showOffTime>=.66&&showOffTime<.82 || showOffTime>=.96&&showOffTime<1.12 : Enabled;
    // Vehicle-local -X is left from the driver's seat; imported part names are reversed.
    public bool HeadlightOn(float localX)=>localX<0 ? LeftHeadlightOn : RightHeadlightOn;
    public bool Enabled { get; set; }
    public bool BrakeLightsOn { get; private set; }
    public bool ReverseLightsOn { get; private set; }
    public bool RearLightsOn => Enabled || BrakeLightsOn;
    public void Update(bool driving, bool braking, float forwardSpeed=0, float throttle=0, float deltaTime=0, bool paused=false) {
        if (driving && !previousDriving) Enabled = true;
        if (!driving && previousDriving) Enabled = false;
        previousDriving = driving;
        if(driving)hasBeenDriven=true;
        bool show=ShowOffEnabled&&hasBeenDriven&&!driving;
        if(!show||!ShowOffActive)showOffTime=0;
        else if(!paused&&deltaTime>0&&!float.IsNaN(deltaTime)&&!float.IsInfinity(deltaTime))
            showOffTime=(showOffTime+deltaTime)%2.4;
        ShowOffActive=show;
        BrakeLightsOn = driving && braking;
        // S first brakes forward travel; reverse is selected only once the
        // existing handling would accept reverse drive. Coasting/braking in R
        // keeps the lamp on until forward drive is selected or the driver exits.
        if (!driving || float.IsNaN(forwardSpeed) || float.IsInfinity(forwardSpeed) ||
            float.IsNaN(throttle) || float.IsInfinity(throttle)) reverseSelected = false;
        else {
            if (throttle != 0 && !DrivingMath.DirectionBrake(forwardSpeed, throttle))
                reverseSelected = throttle < 0;
        }
        ReverseLightsOn = driving && reverseSelected;
    }
}
}
