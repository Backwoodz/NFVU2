# Tyre smoke: local development

The rear-tyre effect now combines a rotating boundary layer at the wheel with
a detached, growing plume. It replaces the previous fixed-rate, two-emitter
effect. This is an authored particle flow, not a volumetric fluid simulation.
Final appearance and frame-time acceptance while driving in a saved world remain
open; isolated native rendering checks do not establish long-session GPU stability.

## Behaviour

- Each rear tyre has its own visual heat state. Wheel surface speed minus road
  velocity, lateral contact velocity and contact load determine the source.
  Ordinary rolling, unloaded wheels and airborne wheelspin produce no new smoke.
  Sustained friction builds density over several seconds; recovered grip stops
  new emission while the existing cloud continues to disperse. Heat is a
  normalized visual proxy, not a tyre temperature or a change to grip.
- New wisps closely follow both tyre shoulders, with smaller billboards and
  limited sideways peeling so the mantle does not protrude as a separate ring
  when viewed from above. Centres start 14–16 cm from the wheel midplane;
  axial peeling adds at most 1.2 cm. Under strong wheelspin the wispy boundary
  layer follows roughly one full turn (about 360–380 degrees), gradually
  spreading outward during the latter part of the turn before shedding.
  The randomized attachment time is capped at 0.50–0.56 seconds, so slower
  wheelspin releases the smoke before completing a full circle. The earlier
  210–260-degree limit left a visible open quarter in the mantle.
  It follows the current wheel frame through
  turning motion; loss of the source releases it immediately. Angular
  entrainment is capped; the whole cloud never spins at wheel RPM and there is
  no closed, rigid smoke ring. Small wisps live 0.65–1.05 seconds. Larger puffs
  detach after 0.10–0.22 seconds,
  retain some wake momentum, approach Valheim's wind and expand over 3.6–6.2
  seconds. Free clouds retain directed circulation in their stored birth frame:
  the wake moves along the old world trail, its rolling radius expands, and
  circulation fades over the following seconds. Slow/zero wheel rotation scales
  that directed component down. A smooth local eddy field adds smaller billows.
  Released smoke never uses the current wheel as its circulation centre.
- One in three cloud births starts directly across the tyre's contact patch,
  instead of orbiting first. These puffs become visible quickly, spread near
  the road and feed the detached cloud. The source uses the same friction/load
  gating, sampled/rendered contact pair and existing cloud rate/budget; no
  additional renderer or particle allocation is needed. A released contact puff
  remains in world space rather than being pulled along by the wheel.
- Free clouds use stable birth variation in size, growth, opacity, lateral
  spread and sub-frame position. This breaks uniform rows without frame-wise
  random jitter. Stronger overlap and slower dilution make the cloud denser;
  source rates and lifetimes are unchanged, avoiding starvation at the existing
  cloud cap. The accepted narrow tyre mantle and its random stream are unchanged.
- Positions stay in world space. Births are distributed along the travelled
  contact path, with simulation steps no longer than 25 ms. Pauses freeze age,
  flow and heat. Menus, flight, dismounting and transformation stop the source;
  existing smoke ages out. Teleports clear the plume and heat state.
  The 25 ms value is a maximum integration step, not a refresh timer: every
  rendered `LateUpdate` with positive game delta updates particle positions,
  ages and the native particle buffer. At 144 Hz that means approximately
  6.94 ms between updates. There is no former-neon-style 60 ms hold to remove.
- Current drift-physics contacts supply normal and load through read-only
  adapter accessors. Contact point and hub are cached from the same physics
  sample, then shifted together to the rendered wheel. Previously the drift
  provider mixed a sampled contact point with an interpolated hub, cancelling
  that shift and holding the source between physics ticks. Ordinary
  driving/burnouts use WheelCollider contact data.
  The force integration and separate physics library are unchanged.

## Rendering and bounds

The renderer clones Valheim's loaded `slowwispysmoke` material and uses its native
8×8 animated atlas with flipbook blending, environment lighting, shadows/fog and
depth fading. It does not instantiate native emitters, game `Smoke` scripts,
network objects, colliders, lights or audio sources. Native textures remain owned
by the game. If that material is unavailable, the existing original NFS atlas
uses an alpha-blended fallback; there is no additive/glow fallback.

Both tyres share one native particle renderer, sorted by individual particle
distance with a hard limit of 2,800 particles. Simulation, heat, random streams
and the 1,400-particle budget remain independent for each tyre. Separate
renderers previously sorted whole overlapping plumes against one another;
crossing their sorting boundary could abruptly reverse their alpha blend order.
The shared upload uses a reused buffer and includes both plumes every update.
Source rates never exceed 210 cloud puffs plus 260 wisps per second per wheel.
The tyre mantle keeps its high wisp rate with smaller puffs close to the rubber.
Each wheel reserves up to 350 slots for wisps and
1,050 for detached clouds so a lingering trail cannot starve the rotating layer.
Independent random streams per wheel/layer keep lifetime distributions stable
when rendering frame rate changes.
Cloud diameter is capped at 3.8 m and renderer screen size at 0.35. Arrays and
materials are reused. There are no smoke raycasts per particle. A contact-plane
guard within 6 m of each birth contact prevents nearby flow from sinking into
the road. Free clouds also damp downward velocity near their stored birth plane,
so a fast rolling wake does not dive below it after leaving that short position
guard. These rules do not resolve walls, ceilings or arbitrary terrain beyond
the birth plane. Billboards have no volumetric self-shadowing or fluid pressure
solver; overlapping tyre plumes share a flow field but do not exchange mass.

`[Effects] TyreSmokeEnabled=false` still avoids all smoke renderer/material/texture
allocation. Changing this existing setting requires a game restart.

## Reference and implementation sources

The [requested video](https://www.youtube.com/watch?v=FMGl2q9BJqY) demonstrates
Assetto Corsa smoke settings. Its later exterior views informed the desired
compact source and expanding trail. It is not a Unity implementation tutorial.

The official [CSP particle configuration](https://github.com/ac-custom-shaders-patch/acc-extension-config/blob/master/config/particles_fx.ini)
separates smoke amount, heating, lighting and long-lasting smoke, and exposes
particle/coverage limits. That supports tuning these concerns separately;
NFVU2 does not reuse its renderer or reproduce its persistent minute-long haze.

Unity documents [manual particle state updates](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.SetParticles.html)
and [texture-sheet animation](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysTexSheetAnimModule.html).
The implementation updates reusable particle buffers and lets the native
renderer animate the atlas. No Unity Editor, external shader bundle or new
redistributed texture is required.

## Verification

Build with `python tools/build.py`. Run the portable heat/emission checks with
`python tools/test.py --skip-build --suite tyresmoke`; these are not Unity tests.
Related adapter, physics, tyre-audio and exhaust suites check the adjacent paths.

With Valheim closed, `python tools/test_tyre_smoke_unity.py` explicitly launches
a hidden, **silent** D3D11 menu fixture. It compiles the current smoke sources
into a temporary diagnostic plugin, reuses installed model/material helpers,
and removes that plugin after checksum verification. It never loads a saved
world or changes saved player settings. Reports and images go to ignored
`artifacts/tests/tyresmokeunity/`.

The production cabin glass pass uses queue 2990, before tyre smoke at 3000.
Previously both used 3000: Unity could sort the aggregate bounds of a long smoke
trail behind the window renderer even when some puffs were in front of it.
The tinted panes then appeared as black silhouettes over the smoke. Drawing
glass first lets foreground smoke cover it while preserving the tint and its
transparent depth-write setting. This is still ordinary alpha blending, not
general order-independent transparency for every intersecting transparent surface.
Unity documents this queue/renderer sorting in its
[Built-in rendering order reference](https://docs.unity.com/en-us/engine/6000.0/manual/cameras/built-in-rendering-order).

The explicit native test also loads the original 350Z body/window geometry,
uses the production glass-material factory and compares old/corrected ordering
with controlled foreground smoke and a distant emitter tail, from three angles
and at clear/dark tint. A smoke-free comparison checks that the ordering change
does not change the appearance of the windows themselves.

The native fixture checks initialization under an inactive vehicle parent,
actual rendered smoke pixels, forward/reverse wheel entrainment, external
shoulder placement, travel beyond the tyre crown, wheel-frame following and
immediate release when the source is disabled, independent
ground contact, expansion, world-space persistence, source release, particle
expiry, teleport reset, pause delta and the particle cap over 80 simulated
seconds. It also compares source counts at 30 and 144 Hz and checks independent
source clearing in the shared renderer. Most flow checks use a simple reference
chassis and synthetic contacts. They do not validate real driving, all lighting
conditions, wall collisions or sustained GPU performance.

An additional cadence regression tracks the same moving particle for ten
successive updates at 30, 60 and 144 Hz, checking age and position each time.
All three runs have zero held particle frames. This rules out that specific
refresh-timer hypothesis in the exercised path. The original synthetic provider
did not exercise the drift adapter's mixed physics/render poses.

The added native contact fixture uses the current drift adapter, real PhysX
road contacts and held physics samples with a moving render pose at 112 km/h.
At 144 Hz the old provider holds the source for 94 frames in one second, with
up to 0.622 m wheel offset. The corrected pair has no held frames at 30/60/144 Hz
and stays within 0.00025 m of the rendered tyre in this fixture.

Frozen-particle rendering checks separate atlas animation, depth fading and
transparent sorting. Native UV/animation streams blend adjacent atlas frames;
5 mm road-depth increments produce smooth fading, so neither setting is changed.
Mature stationary and donut plumes retain their production colours, random atlas
starts, ages and positions while the camera moves only 4 mm across the old
renderer boundary. In the donut case, separate renderers change 14,982 pixels
strongly, versus 182 with global particle sorting; the stationary case drops
from 832 to 24. These figures describe isolated fixture views, not a guarantee
for every driving scene. CSV reports and comparison PNGs preserve the old
split-renderer reference alongside the corrected shared rendering.

The combined correction passes 3,832 native checks, plus 31,031 portable adapter
and 16,020 heat/emission checks. Production uploads match the globally sorted
frozen reference in the startup, mature stationary and donut scenes, with zero
strongly changed pixels. The contact fixture controls physics/render timing;
uneven saved-world terrain and final driving appearance still require acceptance.

The subsequent full-turn mantle adjustment passes 4,053 native checks and the
16,020 portable heat/emission checks. A tracked visible wisp reaches roughly
355–360 degrees in sampled frames at 60/144 Hz, forward and reverse, while the
vehicle translates and turns. The test checks outward peeling, visibility in
the closing quarter, road clearance and detachment after the turn. Separate
images isolate the wheel wisps. The shared sorting/contact-source regressions
still pass; the emission counts and per-wheel particle caps are unchanged.

The tighter mantle and direct contact source pass 5,151 native checks and
16,020 portable heat/emission checks. At 30/60/144 Hz, fresh contact puffs remain
within 16 cm of their sampled birth footprint in the stationary fixture, above
the road, and are visibly rendered in isolation. Attached mantle centres plus
half their billboard size reach about 27 cm from the wheel midplane. Overhead
and side images document the appearance; full-turn, world-trail, source timing,
global sorting, expiry and budget regressions also pass. This remains an
isolated rendering fixture rather than saved-world visual acceptance.

The first detached rolling-cloud comparison passes 8,699 native checks and the 16,020
portable heat/emission checks. Directed circulation is exercised at 1–2 s,
including reverse/slow/zero wheel rotation, smooth dissipation, rotated birth
frames, local continuity and bounded velocities. Sixteen real native particles
are tracked for 20 updates while their source moves 12 m and turns 240 degrees;
their trajectories match a stationary-source control, with no held frames.

A comparison compiles the new source alongside the installed previous build
using an assembly alias. Its SHA-256 is recorded in `baseline-sha256.txt`.
Stationary and curved moving scenes use the same clock, wheel inputs and camera;
tyre wisps retain identical seeds, positions, sizes and colours, and live particle
counts match. The dense-pixel area in these two isolated views increases from
58,901 to 97,546 and 66,103 to 103,214. These image metrics describe coverage in
the fixture, not physical smoke density or parity with another game's renderer.
Before/after/background PNGs preserve the visual comparison. Self-shadowed
volumetric fluid simulation remains outside this particle effect.

The final version adds smooth descent damping near the birth contact plane and
passes 11,402 native checks. A further 20 m/s fixture emits for six seconds and
then releases for three seconds: 954 distinct clouds travel outside the 6 m
position guard, with at least 0.1799 m centre clearance there and no held updates
or trajectory jumps. The broader comparison covers 241,156 native cloud samples.
The descent adjustment leaves the denser image coverage within one percent of
the first rolling-cloud version and again preserves the installed mantle exactly.

The subsequent rim-occlusion fixture loads the original stock wheel mesh and
measures transmitted rim contrast through frozen production smoke. It inverts
two opaque measurement materials and calibrates against a smoke-free capture
at the same pose; a depth-tested ID mask excludes the surrounding cloud.
It exercises 20 m/s forward plus both signs of 10 m/s lateral velocity, at
950 and 650 RPM after eight-second warmups, from four viewpoints. Cold,
source-off and ordinary-rolling controls distinguish real rim occlusion from
merely rendering a bright wake. Comparison PNGs and `wheel-occlusion.csv`
retain the installed baseline; its SHA-256 identifies the exact comparison.
The targeted run is available via
`python tools/test_tyre_smoke_unity.py --wheel-occlusion-only`.
An experimental dense source passed 12,108 native checks and reduced hot
side-on rim contrast to 1–7 percent, but the player rejected its actual
overhead appearance: large, broad overlapping sources formed two bright,
uniform tubes. That experiment was reverted, including its extra attachment,
source-following volume and initial velocity carry. The restored build is
SHA-256-identical to the saved pre-experiment DLL and again passes the 16,020
portable checks. Earlier flicker fixes, the narrow mantle, contact source and
detached rolling flow remain intact. The contrast fixture is retained for
diagnosis; forced minimum occlusion is no longer a visual acceptance condition.
Future appearance checks must also assess overhead shape and actual gameplay,
rather than infer realism from the amount of hidden rim contrast.
