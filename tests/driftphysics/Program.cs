using System;
using NFVU2.DriftPhysics;

static class Program
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Near(float value, float expected, float tolerance, string message) { Check(Math.Abs(value - expected) <= tolerance, message + ": " + value); }
    static void Main()
    {
        var tyre = new DriftTyre();
        var rest = tyre.Step(.02f, 0, 0, 3600, .34f, 0, 0, false);
        Near(rest.LongitudinalForce, 0, .001f, "stationary tyre has no self propulsion");
        Near(rest.SideForce, 0, .001f, "stationary tyre has no side force");
        var spinning = new DriftTyre();
        var air = spinning.Step(.02f, 0, 0, 0, .34f, 500, 0, false);
        Near(air.LongitudinalForce, 0, .001f, "air has no traction");
        Near(air.AngularSpeed, 500 * .02f / spinning.WheelInertia, .001f, "air wheel integrates torque");
        var left = new DriftTyre { AngularSpeed = 20 / .34f };
        var right = new DriftTyre { AngularSpeed = 20 / .34f };
        var a = left.Step(.02f, 20, 5, 3600, .34f, 1000, 0, false);
        var b = right.Step(.02f, 20, -5, 3600, .34f, 1000, 0, false);
        Near(a.SideForce, -b.SideForce, .001f, "lateral symmetry");
        Check(a.SideForce < 0, "side force opposes contact side velocity");
        Check(Math.Sqrt(a.LongitudinalForce * a.LongitudinalForce + a.SideForce * a.SideForce) <= 3600 * .9 + .1, "combined grip stays in friction circle");
        var locked = new DriftTyre { AngularSpeed = 20 / .34f };
        for (int i = 0; i < 20; i++) a = locked.Step(.02f, 20, 0, 3600, .34f, 0, 5000, false);
        Near(locked.AngularSpeed, 0, .001f, "braking locks wheel without reversing it");
        Check(a.LongitudinalForce < -2500, "locked wheel slows moving car");
        var reverse = new DriftTyre();
        a = reverse.Step(.02f, 0, 0, 3600, .34f, -1000, 0, false);
        Check(a.LongitudinalForce < 0 && a.AngularSpeed < 0, "negative wheel torque drives reverse");
        // Contact force impulse and wheel angular momentum must account for all input torque.
        var momentum = new DriftTyre { RollingResistance = 0 };
        a = momentum.Step(.02f, 5, 2, 3600, .34f, 900, 0, false);
        Near(momentum.WheelInertia * momentum.AngularSpeed, (900 - a.LongitudinalForce * .34f) * .02f, .001f, "wheel/contact momentum balance");
        foreach (float dt in new[] { .005f, .02f, .05f })
        {
            var t = new DriftTyre();
            for (int i = 0; i < (int)(2 / dt); i++) a = t.Step(dt, .05f, 0, 3600, .34f, 100, 0, false);
            Check(!float.IsNaN(a.AngularSpeed) && Math.Abs(a.AngularSpeed) < 10, "implicit low-speed contact stable at " + dt);
        }
        var idle = new DriftPowertrain();
        DriveOutput drive = default(DriveOutput);
        for (int i = 0; i < 500; i++) drive = idle.Step(.02f, 0, 0, 0, true, false, false, false);
        Near(drive.RearWheelTorque, 0, .001f, "idle does not drive wheels");
        Check(drive.EngineRpm >= 900 && drive.EngineRpm < 1100, "idle rpm is bounded");
        for (int i = 0; i < 150; i++) drive = idle.Step(.02f, 1, 0, 0, true, true, false, false);
        Check(drive.EngineRpm > 6000, "handbrake plus gas freely raises rpm");
        Near(drive.RearWheelTorque, 0, .001f, "handbrake opens clutch");
        float highRpm = drive.EngineRpm;
        for (int i = 0; i < 75; i++) drive = idle.Step(.02f, 0, 0, 0, true, true, false, false);
        Check(drive.EngineRpm < highRpm - 1000, "lifting gas lowers free engine rpm");
        var direction = new DriftPowertrain();
        drive = direction.Step(.02f, -1, 10, 10 / .34f, true, false, false, false);
        Check(drive.DirectionBrake && Math.Abs(drive.RearWheelTorque) < .01, "opposite request brakes before changing direction");
        for (int i = 0; i < 75; i++) drive = direction.Step(.02f, -1, 0, 0, true, false, false, false);
        Check(drive.Gear == -1 && drive.RearWheelTorque < -100, "reverse engages and provides reverse torque");
        drive = direction.Step(.02f, 1, -10, -10 / .34f, true, false, false, false);
        Check(drive.DirectionBrake && drive.RearWheelTorque == 0, "reverse to forward also brakes first");
        var burnout = new DriftPowertrain();
        for (int i = 0; i < 75; i++) drive = burnout.Step(.02f, 1, 0, 0, true, true, false, true);
        Check(drive.RearWheelTorque > 100, "separate burnout request permits rear drive");
        var higherGearBurnout = new DriftPowertrain();
        for (int i = 0; i < 100; i++) higherGearBurnout.Step(.02f, 1, 35, 35 / .34f, true, false, false, false);
        Check(higherGearBurnout.Gear > 1, "burnout regression begins after normal higher-gear travel");
        drive = higherGearBurnout.Step(.02f, 1, 0, 0, true, false, false, true);
        Check(drive.Gear == 1 && drive.RearWheelTorque > 100, "accepted stationary burnout selects first without a dead pending shift after a higher-gear stop");
        for (int i = 0; i < 200; i++) drive = higherGearBurnout.Step(.02f, 1, 0, 100, true, false, false, true);
        Check(drive.Gear == 1, "stationary burnout stays in first despite driven-wheel rpm");
        for (int i = 0; i < 100; i++) drive = higherGearBurnout.Step(.02f, 1, 35, 35 / .34f, true, false, false, false);
        Check(drive.Gear > 1, "releasing burnout restores ordinary road-speed shifts");
        var reverseBurnout = new DriftPowertrain();
        for (int i = 0; i < 100; i++) drive = reverseBurnout.Step(.02f, -1, 0, 0, true, false, false, false);
        Check(drive.Gear == -1 && drive.RearWheelTorque < 0, "ordinary reverse remains available before a stationary burnout");
        drive = reverseBurnout.Step(.02f, 1, 0, 0, true, false, false, true);
        Check(drive.Gear == 1 && drive.RearWheelTorque > 0, "forward stationary burnout can start after reversing without a stale reverse shift");
        RoadSpeedShifts();
        StraightCar(false);
        StraightCar(false, .005f);
        StraightCar(false, .05f);
        StraightCar(true);
        Console.WriteLine(checks + " independent drift physics checks passed");
    }

    static void RoadSpeedShifts()
    {
        var rolling = new DriftPowertrain();
        var wheelspin = new DriftPowertrain();
        for (int i = 0; i < 300; i++)
        {
            rolling.Step(.02f, 1, 4, 4 / .34f, true, false, false, false);
            wheelspin.Step(.02f, 1, 4, 400, true, false, false, false);
        }
        Check(rolling.Gear == 1 && wheelspin.Gear == rolling.Gear, "sustained wheelspin at low road speed does not force an upshift");
        var fastRolling = new DriftPowertrain();
        var fastSpinning = new DriftPowertrain();
        for (int i = 0; i < 100; i++)
        {
            fastRolling.Step(.02f, 1, 35, 35 / .34f, true, false, false, false);
            fastSpinning.Step(.02f, 1, 35, 400, true, false, false, false);
        }
        Check(fastRolling.Gear == 3 && fastSpinning.Gear == 3, "higher road speed selects the same gear despite wheelspin");
        var standardRadius = new DriftPowertrain();
        var largerRadius = new DriftPowertrain { WheelRadius = .5f };
        for (int i = 0; i < 100; i++)
        {
            standardRadius.Step(.02f, 1, 10, 10 / .34f, true, false, false, false);
            largerRadius.Step(.02f, 1, 10, 10 / .5f, true, false, false, false);
        }
        Check(standardRadius.Gear == 2 && largerRadius.Gear == 1, "rolling radius adjusts speed-to-gear scheduling");
        var launchGear = new DriftPowertrain();
        for (int i = 0; i < 50; i++) launchGear.Step(.02f, .6f, 8, 8 / .34f, true, false, false, false);
        Check(launchGear.Gear == 1, "first remains available below short launch upshift speed");
        for (int i = 0; i < 50; i++) launchGear.Step(.02f, .6f, 10, 10 / .34f, true, false, false, false);
        Check(launchGear.Gear == 2, "low-speed drift can enter second at ten metres per second longitudinal speed");
        bool heldSecond = true;
        for (int i = 0; i < 150; i++)
        {
            launchGear.Step(.02f, .6f, 8, 8 / .34f, true, false, false, false);
            heldSecond &= launchGear.Gear == 2;
        }
        Check(heldSecond, "second holds below first upshift speed without hunting");
        for (int i = 0; i < 50; i++) launchGear.Step(.02f, .6f, 6, 6 / .34f, true, false, false, false);
        Check(launchGear.Gear == 1, "very low road speed returns to launch gear");
        foreach (float invalid in new[] { 0f, -.34f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { new DriftPowertrain { WheelRadius = invalid }.Step(.02f, 1, 0, 0, true, false, false, false); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "invalid rolling radius rejected");
        }
    }

    static void StraightCar(bool reverse, float dt = .02f)
    {
        var engine = new DriftPowertrain();
        var wheels = new[] { new DriftTyre(), new DriftTyre(), new DriftTyre(), new DriftTyre() };
        const float mass = 1450, radius = .34f;
        float speed = 0;
        int topGear = 0;
        bool finiteMotion = true;
        bool stableClutch = true;
        for (int i = 0; i < (int)(12 / dt); i++)
        {
            var drive = engine.Step(dt, reverse ? -1 : 1, speed, (wheels[2].AngularSpeed + wheels[3].AngularSpeed) * .5f, true, false, false, false);
            float force = 0;
            for (int w = 0; w < 4; w++) force += wheels[w].Step(dt, speed, 0, mass * 9.81f / 4, radius, w >= 2 ? drive.RearWheelTorque : 0, 0, w < 2).LongitudinalForce;
            force -= .4f * speed * Math.Abs(speed);
            speed += force / mass * dt;
            topGear = Math.Max(topGear, drive.Gear);
            finiteMotion &= !float.IsNaN(speed) && Math.Abs(speed) < 100;
            if (!reverse) stableClutch &= drive.RearWheelTorque > -100;
        }
        Check(finiteMotion, "finite straight integrated motion");
        if (!reverse) Check(stableClutch, "full-throttle clutch does not oscillate into strong reverse wheel torque");
        Check(reverse ? speed < -10 : speed > 20, "straight acceleration in requested direction " + speed);
        if (!reverse) Check(topGear >= (dt <= .02f ? 3 : 2), "automatic reaches higher gears dt=" + dt + " speed=" + speed + " gear=" + topGear);
        float before = Math.Abs(speed);
        for (int i = 0; i < (int)(4 / dt); i++)
        {
            var drive = engine.Step(dt, 0, speed, (wheels[2].AngularSpeed + wheels[3].AngularSpeed) * .5f, true, false, false, false);
            float force = 0;
            for (int w = 0; w < 4; w++) force += wheels[w].Step(dt, speed, 0, mass * 9.81f / 4, radius, w >= 2 ? drive.RearWheelTorque : 0, 0, w < 2).LongitudinalForce;
            force -= .4f * speed * Math.Abs(speed);
            speed += force / mass * dt;
        }
        Check(Math.Abs(speed) < before, "lifting gas decelerates rather than adding energy");
    }
}
