# NFVU2.DriftPhysics

Portable C# drivetrain and tyre model written for NFVU2. This assembly has no
Unity references, native imports, external physics binaries or game assets.
It replaces the earlier local third-party-library experiment; the old binaries
and research are retained only in ignored local archives, not in this build.

`DriftPowertrain` implements an analytic torque curve, engine inertia, a slipping
clutch, reverse and a six-speed automatic. Gear selection uses chassis road
speed rather than wheelspin. The short first gear is for starting and donuts;
an early upshift and separate downshift threshold retain second gear in slower
drifts. Clutch coupling includes the reflected inertia of both driven wheels
to prevent alternating drive/brake impulses at the engine limiter.

`DriftTyre` combines longitudinal and lateral slip using a smooth friction circle.
Its implicit wheel rotation solve handles braking and wheel lock at low speed.
All inputs use SI units; torque output from the drivetrain is **per rear wheel**.
The adapter applies the resulting contact force once, not drive torque again.

The Unity adapter supplies raycast contact loads, suspension, steering and wheel
poses. Unity integrates the rigid body and resolves chassis collisions. Intended
runtime tick: 0.02 seconds. Internal substeps stabilize the engine and wheel
solves, but this sequential coupling is not claimed to be timestep invariant.
Terrain currently uses one shared tyre calibration; surface-specific tuning
and in-world handling acceptance remain separate work.

Build with `python tools/build.py`. Core and adapter checks:
`python tools/test.py --skip-build --suite driftphysics --suite driftadapter`.
The explicit Unity probe is documented in [DRIFT_WORLD.md](../docs/DRIFT_WORLD.md).
