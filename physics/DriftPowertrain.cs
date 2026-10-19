using System;

namespace NFVU2.DriftPhysics
{
    public struct DriveOutput
    {
        public float RearWheelTorque, EngineRpm;
        public int Gear;
        public bool DirectionBrake;
    }

    /// <summary>Independent analytic engine, slipping clutch and six-speed RWD automatic.
    /// Gas is signed: positive forward, negative reverse. Torque is Nm per rear wheel.
    /// The adapter owns wheel/foot/parking brakes; DirectionBrake requests a direction change stop.</summary>
    public sealed class DriftPowertrain
    {
        readonly float[] ratios = { 3.2f, 2.1f, 1.55f, 1.2f, 1f, .82f };
        float rpm = 900, shiftTime;
        int gear = 1;
        public float Rpm { get { return rpm; } }
        public int Gear { get { return gear; } }
        public float MaxTorque { get; set; } = 450;
        public float MaxRpm { get; set; } = 6800;
        public float IdleRpm { get; set; } = 900;
        public float FinalDrive { get; set; } = 3.7f;
        /// <summary>Effective rolling radius in metres, used only for road-speed shift scheduling.</summary>
        public float WheelRadius { get; set; } = .34f;
        public float EngineInertia { get; set; } = .32f;
        /// <summary>Rotational inertia of one driven wheel in kg m²; match the tyre instances.</summary>
        public float DrivenWheelInertia { get; set; } = 1.8f;
        public float ClutchCapacity { get; set; } = 650;

        public DriveOutput Step(float dt, float gas, float signedSpeed, float rearWheelOmega,
            bool driving, bool handbrake, bool footBrake, bool burnout)
        {
            Require(Finite(dt) && dt > 0 && dt <= .1f, "dt");
            Require(Finite(gas) && Finite(signedSpeed) && Finite(rearWheelOmega), "drive state");
            Require(Finite(MaxTorque) && MaxTorque >= 0 && Finite(MaxRpm) && Finite(IdleRpm)
                && IdleRpm >= 100 && MaxRpm > IdleRpm + 500 && FinalDrive > 0 && Finite(FinalDrive)
                && WheelRadius >= .05f && Finite(WheelRadius)
                && DrivenWheelInertia > 0 && Finite(DrivenWheelInertia)
                && EngineInertia > 0 && Finite(EngineInertia) && ClutchCapacity >= 0 && Finite(ClutchCapacity), "engine settings");
            gas = driving ? Clamp(gas, -1, 1) : 0;
            int direction = gas < -.02f ? -1 : gas > .02f ? 1 : (gear < 0 ? -1 : 1);
            bool directionBrake = driving && Math.Abs(gas) > .02f && signedSpeed * direction < -1.2f;
            if (!driving) gear = 0;
            else if (!directionBrake && Math.Abs(gas) > .02f && Math.Abs(signedSpeed) < 1.5f && Math.Sign(gear) != direction)
            { gear = direction; shiftTime = .16f; }
            else if (gear == 0) gear = direction;

            float throttle = directionBrake || footBrake ? 0 : Math.Abs(gas);
            // Shift scheduling follows longitudinal chassis motion, not driven wheelspin or
            // free engine revs. Actual wheel speed still controls the physical clutch below.
            float roadRpm = Math.Abs(signedSpeed) / WheelRadius * 9.5492966f * FinalDrive * Ratio(gear);
            if (driving && gear > 0 && shiftTime <= 0 && !handbrake && !burnout && !directionBrake)
            {
                // First is a short launch gear. Its early change puts second at about
                // 1970 road RPM; the lower 2->1 threshold leaves useful speed hysteresis
                // even when a drift reduces longitudinal speed relative to total speed.
                float upshift = gear == 1 ? 3000 : 6200;
                float downshift = gear == 2 ? 1500 : 2400;
                if (gear < ratios.Length && roadRpm > upshift) { gear++; shiftTime = .16f; }
                else if (gear > 1 && roadRpm < downshift) { gear--; shiftTime = .16f; }
            }
            float ratio = Ratio(gear) * FinalDrive * (gear < 0 ? -1 : gear > 0 ? 1 : 0);
            double omega = rpm / 9.5492966;
            double wheelEngine = rearWheelOmega * ratio;
            double clutchSum = 0;
            int steps = Math.Max(1, (int)Math.Ceiling(dt / .0025f));
            double h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                double currentRpm = omega * 9.5492966;
                double normalized = Clamp((float)((currentRpm - IdleRpm) / (MaxRpm - IdleRpm)), 0, 1);
                // Broad, explicitly authored torque curve, not fitted game data.
                double combustion = throttle * MaxTorque * (.72 + .28 * Math.Sin(Math.PI * normalized));
                if (currentRpm >= MaxRpm - 100) combustion *= Clamp((float)((MaxRpm - currentRpm) / 100), 0, 1);
                double drag = 22 + currentRpm * .008;
                double idle = Math.Max(0, (IdleRpm + 70 - currentRpm) * .7);
                double freeTorque = combustion + idle - drag;
                double engage = Math.Abs(signedSpeed) > 1.5 ? 1 : throttle * Clamp((float)((currentRpm - IdleRpm + 200) / 1000), 0, 1);
                if (!driving || directionBrake || gear == 0 || shiftTime > 0 || (handbrake && !burnout)) engage = 0;
                // Wheel state is supplied once per caller tick. Include both driven wheels'
                // reflected inertia over that full tick, so a stiff clutch cannot alternately
                // fling them above/below engine speed while our engine alone is substepped.
                double stiffness = engage * 12;
                double coupling = stiffness / (1 + dt * stiffness * ratio * ratio * .92 / (2 * DrivenWheelInertia));
                double freeOmega = omega + h * freeTorque / EngineInertia;
                double coupledOmega = (freeOmega + h * coupling * wheelEngine / EngineInertia) / (1 + h * coupling / EngineInertia);
                double clutch = Clamp((float)(coupling * (coupledOmega - wheelEngine)),
                    (float)(-ClutchCapacity * engage), (float)(ClutchCapacity * engage));
                omega += h * (freeTorque - clutch) / EngineInertia;
                omega = Math.Max(IdleRpm / 9.5492966, Math.Min(MaxRpm / 9.5492966, omega));
                clutchSum += clutch;
                shiftTime = Math.Max(0, shiftTime - (float)h);
            }
            rpm = (float)(omega * 9.5492966);
            return new DriveOutput { EngineRpm = rpm, Gear = gear, DirectionBrake = directionBrake,
                RearWheelTorque = (float)(clutchSum / steps * ratio * .92 / 2) };
        }

        float Ratio(int g) { return g < 0 ? 3.2f : g == 0 ? 0 : ratios[g - 1]; }
        static float Clamp(float x, float lo, float hi) { return Math.Max(lo, Math.Min(hi, x)); }
        static bool Finite(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
        static void Require(bool condition, string name) { if (!condition) throw new ArgumentOutOfRangeException(name); }
    }
}
