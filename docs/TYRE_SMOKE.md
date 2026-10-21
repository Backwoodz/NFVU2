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
- New wisps follow the tyre's rotation on the outside of both shoulders, where
  rubber does not hide the movement. Under strong wheelspin the wispy boundary
  layer follows a roughly 210–260-degree arc, over the crown, and gradually
  spreads outward before shedding. It follows the current wheel frame through
  turning motion; loss of the source releases it immediately. Angular
  entrainment is capped; the whole cloud never spins at wheel RPM and there is
  no closed, rigid smoke ring. Small wisps live 0.65–1.05 seconds. Larger puffs
  detach after 0.10–0.22 seconds,
  retain some wake momentum, approach Valheim's wind and expand over 3.6–6.2
  seconds. A continuous, divergence-free velocity field creates rolling eddies.
- Positions stay in world space. Births are distributed along the travelled
  contact path, with simulation steps no longer than 25 ms. Pauses freeze age,
  flow and heat. Menus, flight, dismounting and transformation stop the source;
  existing smoke ages out. Teleports clear the plume and heat state.
- Current drift-physics contacts supply normal and load through read-only
  adapter accessors. Ordinary driving/burnouts use WheelCollider contact data.
  The force integration and separate physics library are unchanged.

## Rendering and bounds

The renderer clones Valheim's loaded `slowwispysmoke` material and uses its native
8×8 animated atlas with flipbook blending, environment lighting, shadows/fog and
depth fading. It does not instantiate native emitters, game `Smoke` scripts,
network objects, colliders, lights or audio sources. Native textures remain owned
by the game. If that material is unavailable, the existing original NFS atlas
uses an alpha-blended fallback; there is no additive/glow fallback.

There are two particle renderers, with a hard limit of 1,400 particles each.
Source rates never exceed 210 cloud puffs plus 260 wisps per second per wheel.
The tyre mantle has nearly three times the earlier wisp rate, larger compact
puffs and greater opacity. Each wheel reserves up to 350 slots for wisps and
1,050 for detached clouds so a lingering trail cannot starve the rotating layer.
Independent random streams per wheel/layer keep lifetime distributions stable
when rendering frame rate changes.
Cloud diameter is capped at 3.8 m and renderer screen size at 0.35. Arrays and
materials are reused. There are no smoke raycasts per particle. A contact-plane
guard within 6 m of each birth contact prevents nearby flow from sinking into
the road. It does not resolve walls, ceilings or arbitrary terrain beyond that
local plane. Billboards have no volumetric self-shadowing or fluid pressure
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
seconds. It also compares source counts at 30 and 144 Hz. The fixture uses a
simple reference chassis and synthetic contacts; it does not validate real
driving, all lighting conditions, wall collisions or sustained GPU performance.
