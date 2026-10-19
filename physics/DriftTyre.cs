using System;

namespace NFVU2.DriftPhysics
{
    public struct TyreOutput
    {
        public float LongitudinalForce, SideForce, SlipRatio, SlipAngle, Skid, AngularSpeed;
    }

    /// <summary>A wheel's own rotational inertia and a smooth combined-slip friction circle.
    /// SI units throughout. Forces are in wheel coordinates; positive side velocity produces
    /// negative SideForce. The caller applies forces to the chassis, never wheel torque again.</summary>
    public sealed class DriftTyre
    {
        public float AngularSpeed { get; set; }
        public float FrontFriction { get; set; } = 1.2f;
        public float RearFriction { get; set; } = .9f;
        public float WheelInertia { get; set; } = 1.8f;
        public float LongitudinalStiffness { get; set; } = 10f;
        public float LateralStiffness { get; set; } = 6f;
        public float RollingResistance { get; set; } = .012f;

        public TyreOutput Step(float dt, float forwardSpeed, float sideSpeed, float load,
            float radius, float motorTorque, float brakeTorque, bool front)
        {
            Require(dt > 0 && dt <= .1f && Finite(dt), "dt");
            Require(Finite(forwardSpeed) && Finite(sideSpeed) && Finite(load) && Finite(radius)
                && Finite(motorTorque) && Finite(brakeTorque) && Finite(AngularSpeed), "wheel state");
            Require(radius >= .05f && WheelInertia > 0 && Finite(WheelInertia), "wheel dimensions");
            float mu = front ? FrontFriction : RearFriction;
            Require(mu >= 0 && Finite(mu) && LongitudinalStiffness > 0 && Finite(LongitudinalStiffness)
                && LateralStiffness > 0 && Finite(LateralStiffness)
                && RollingResistance >= 0 && Finite(RollingResistance), "tyre settings");
            load = Math.Max(0, load);
            double omega = AngularSpeed;
            double forceSum = 0, sideSum = 0;
            // Implicit wheel/contact coupling avoids unstable explicit wheelspin at rest.
            // Substeps also resolve brake locking without reversing a wheel under braking.
            int steps = Math.Max(1, (int)Math.Ceiling(dt / .005f));
            double h = dt / steps;
            double brake = Math.Max(0, brakeTorque) + RollingResistance * load * radius;
            for (int i = 0; i < steps; i++)
            {
                double before = omega;
                double zero = Residual(0, before, h, motorTorque, radius, forwardSpeed, sideSpeed, mu * load);
                if (Math.Abs(zero) <= brake) omega = 0;
                else
                {
                    double sign = zero < 0 ? 1 : -1;
                    double lo = sign > 0 ? 0 : -Math.Abs(before) - h * (Math.Abs(motorTorque) + brake + mu * load * radius) / WheelInertia - 1;
                    double hi = sign < 0 ? 0 : Math.Abs(before) + h * (Math.Abs(motorTorque) + brake + mu * load * radius) / WheelInertia + 1;
                    for (int n = 0; n < 36; n++)
                    {
                        double mid = (lo + hi) * .5;
                        double residual = Residual(mid, before, h, motorTorque, radius, forwardSpeed, sideSpeed, mu * load) + sign * brake;
                        if (residual > 0) hi = mid; else lo = mid;
                    }
                    omega = (lo + hi) * .5;
                }
                double fx, fy;
                Forces(omega, radius, forwardSpeed, sideSpeed, mu * load, out fx, out fy);
                forceSum += fx;
                sideSum += fy;
            }
            AngularSpeed = (float)omega;
            float slip = (float)((omega * radius - forwardSpeed) / Math.Max(1, Math.Abs(forwardSpeed)));
            float angle = (float)Math.Atan2(sideSpeed, Math.Max(1, Math.Abs(forwardSpeed)));
            return new TyreOutput {
                LongitudinalForce = (float)(forceSum / steps), SideForce = (float)(sideSum / steps),
                SlipRatio = slip, SlipAngle = angle, AngularSpeed = AngularSpeed,
                Skid = load > 0 ? Clamp((Math.Abs(slip) - .08f) * .8f + (Math.Abs(angle) - .06f) * 1.8f, 0, 1) : 0
            };
        }

        double Residual(double omega, double before, double dt, double motor, double radius,
            double speed, double side, double capacity)
        {
            double fx, fy;
            Forces(omega, radius, speed, side, capacity, out fx, out fy);
            return WheelInertia * (omega - before) / dt - motor + fx * radius;
        }

        void Forces(double omega, double radius, double speed, double side, double capacity, out double fx, out double fy)
        {
            double x = LongitudinalStiffness * (omega * radius - speed) / Math.Max(1, Math.Abs(speed));
            double y = -LateralStiffness * Math.Atan2(side, Math.Max(1, Math.Abs(speed)));
            double length = Math.Sqrt(x * x + y * y);
            double scale = length > 1e-10 ? capacity * Math.Tanh(length) / length : capacity;
            fx = x * scale;
            fy = y * scale;
        }

        static bool Finite(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
        static void Require(bool condition, string name) { if (!condition) throw new ArgumentOutOfRangeException(name); }
        static float Clamp(float x, float lo, float hi) { return Math.Max(lo, Math.Min(hi, x)); }
    }
}
