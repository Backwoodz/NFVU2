namespace Rachel350Z {
// Session state is independent of Unity rendering so entry, manual override,
// braking and exit behaviour can be exercised without a running game.
public sealed class VehicleLightingState {
    bool previousDriving;
    bool reverseSelected;
    public bool Enabled { get; set; }
    public bool BrakeLightsOn { get; private set; }
    public bool ReverseLightsOn { get; private set; }
    public bool RearLightsOn => Enabled || BrakeLightsOn;
    public void Update(bool driving, bool braking, float forwardSpeed=0, float throttle=0) {
        if (driving && !previousDriving) Enabled = true;
        if (!driving && previousDriving) Enabled = false;
        previousDriving = driving;
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
