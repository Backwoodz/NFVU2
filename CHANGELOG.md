# Changelog

## 0.32.0 - Local installer, 2026-10-25

- Separate local build from commit `0b1cbb8`, including the accepted tyre-smoke
  and neon improvements, own drift physics, practice course and Parkade Drift 1.
- Exclude the subsequent Parkade Drift 2 spiral venue and its blue entrance
  circle. The subsequent spiral-map development was discarded.
- Allow the packaged tyre audio, loading artwork and first-Parkade sponsor
  images through the installer, including normal install and removal checks.
- Mod and setup in this isolated build both use 0.32.0. This is a local package;
  public downloads remain unchanged. Unity gameplay acceptance remains separate
  from automated build, logic, installer and checksum verification.

## Unreleased - Drift build world and own drift physics

- Keep the Windows setup's ownership checksums synchronized after local
  developer installs. Back up and roll back its record together with the mod
  files so later wizard uninstall/reinstall accepts the verified local build.

- Thicken the rotating tyre mantle with nearly three times the wisps, larger
  compact puffs and reserved particle capacity, retaining the total smoke cap.
- Draw cabin glass before tyre smoke so dark panes cannot be sorted over a
  foreground cloud by the bounds of its distant trail. Preserve window tint.

- Make tyre wisps visibly wrap around both outer shoulders and over the crown,
  following the wheel frame during donuts before peeling into the cloud.
- Fix W+S and dual-trigger stationary burnouts: preserve forward throttle,
  hold the front axle, release rear brakes and select first gear. Brake before
  engaging at road speed; retain the separate rear handbrake.
- Keep direct drift steering below 4 m/s and blend countersteer assistance
  smoothly between 4 and 7 m/s, retaining the existing assisted rack at speed.

- Rebuild rear-tyre smoke with independent friction/heat-driven density,
  short rotation-directed wisps around each tyre, and detached expanding
  plumes carried by rolling eddies and Valheim's wind. Use the native lit
  animated smoke material with a bounded particle budget; retain the smoke
  opt-out. Driving physics and audio are unchanged.

- Dress Parkade as a night drift festival: 108 animated Greydwarfs on the
  36 existing roof terraces, illuminated iron railings, two cheering Trolls
  on supported timber stages, nine sponsor billboards and 23 course lanterns.
  Use six original locally imported sponsor marks; fall back to native text
  when the optional images are absent. Limit additional lights to four nearby
  shadowless sources, combine static decoration and cull distant crowds.

- Fix both Troll fans facing away from the road and crossing their arms:
  calibrate the native face toward the track and raise each arm on its own
  shoulder side. Suppress story ravens only in drift worlds without marking
  tutorials read or changing the player's global tutorial setting.
- Use custom Escape/Start/Options pause at Parkade, including free drive there.
  Preserve native pause on the practice course and parking; active races retain
  custom pause even when off track. Keep F9/L3 assigned to the garage.
- Open the race pause menu with Escape or the controller menu button.
  Resume, restart the three-lap event or return to the practice parking area
  in the same car. Back/Escape resumes a paused race; restarting discards only
  the unfinished attempt and retains saved records.

- Add a native Greydwarf race starter: raised arms, a crouch/drop start signal,
  then a lateral run to the verge. Hold the car until the signal and keep the
  start view clear with a small countdown instead of the modal event panel.
- Flank the Parkade start with eight animated Greydwarf spectators on raised
  grey viewing plinths. Add a continuously scrolling green RIP McRib marquee.

- Show the approved NFVU2 loading artwork during course preparation and return,
  with three animated green segments and separate real construction progress.
- Navigate the event menu with stick/D-pad and confirm the selected action;
  reject held-button repeats across screen transitions.
- Preserve the drift chain and multiplier across intermediate race laps while
  banking each lap once. New races and normal chain breaks still reset them.

- Keep wide drift lines scoring through Parkade navigation corners without
  treating projection jumps as teleports. Preserve physical-distance scoring,
  ordered gates and reverse protection.
- Replace the event cone with a labelled native-font ground ring; hide native
  eye/stealth and stamina widgets while seated and restore them on exit.
  Block build selection, direct menu opening and local hammer placement only
  in drift single-player worlds.
- Add two distinct original NFS drift tyre recordings, with NFVU2's own
  slip/speed/contact-dependent timbre blend, pitch and smooth attack/release.
  Keep the embedded tyre sample as fallback and leave vehicle forces unchanged.

- Enter drift worlds at the parking area in a seated 350Z, reusing an existing
  saved car and retaining its cargo/customization where possible. Keep the
  existing 1,491 m circuit as unlimited practice.
- Fix venue preparation waiting indefinitely for distant native areas: prepare
  pending tiles in sequence, retain their saved progress through unloading and
  return the held car to the ready start before countdown. Synchronize the
  driver's native sector and rigidbody before remote streaming transfers.
- Add a glowing parking event area with a native-font course-preview menu and
  separate event high score. Stage the car at a separate Parkade Drift 1 venue,
  wait for native area/terrain construction readiness, count down, then score
  exactly three complete laps. Support pause, retry and return to parking.
- Reconstruct the measured 503.381 m navigation route and approximately 7.10 m
  road grade from the locally configured original disc data. Adapt the width
  to 18 m, raise the venue above Valheim water, and build 724 native stone,
  banner, torch and sign pieces with independent saved construction markers.
  Add a collision-free checker start line and a separate mini-/world-map layer.
  No original road/building meshes or source-game controller are required at
  runtime. A disposable native world passed seated arrival, complete 1080/1080
  construction, driver transfer and countdown readiness. Actual race driving,
  existing-world reload, terrain persistence and long-run stability remain pending.

- Resume drift scoring after returning from grass. Off-road driving breaks
  the combo and earns no points while retaining the lap, total score and
  anti-farming progress. Lap time continues; teleport checks remain active.

- Make the drift world a night circuit: 350Z drift driving only, no flight,
  Transformer or horde, clear weather, no random creature spawns/raids and
  bounded removal of loose ground loot. Other worlds and saved mode preferences
  retain their normal behaviour.
- Add 34 animated scenic Greydwarf spectators, a ground checker start line and
  46 amber lantern posts with at most six nearby shadowless lights. Decorations
  are local, noncombat and do not block cars or change saved construction.
- Score forward drift distance per valid lap with combo multipliers, reverse
  anti-farming and native world records for best points and fastest lap. Reset
  at start/finish, suspend off-course points and reject teleports; display the score with
  original drift backing artwork and Valheim's native Norse font.

- Draw the drift circuit, pit apron and approach on the minimap and world map,
  using the same pavement coordinates as construction. A turquoise stripe marks
  start/finish. The supplied course plan remains visible in unexplored areas;
  ordinary exploration, map pins and other worlds remain unchanged.

- Extend the original U section into a 1,491 m closed drift circuit with broad
  S transitions, outer straights and a return to start. Add a native Viking
  paddock with four covered bays, workshop benches, chests, terraced spectator
  seating, signs, banners, runoff fencing and up to twelve fuelled lamps.
- Persist the extension under separate v2 tile markers, preserving original
  construction progress, player dismantling and edited signs. Keep terrain
  heights and handling intact; bound construction to loaded, owned areas and
  two operations per frame. Add production-builder reload/ownership checks
  and a scaled plan rendered directly from the C# layout.

- Keep exhaust burble audible during falling-RPM coasting, beyond the initial
  short lift-off pops. Add a separate optional continuous exhaust texture,
  with RPM-driven pitch/level, a quiet loop seam and fade-out on renewed gas.
  Preserve the existing flame bang and the current blow-off mix.
- Add optional short exhaust recordings: a decaying lift-off burble and a
  stronger one-shot tied to the existing backfire flame request. Use NFVU2's
  own trigger/timing model, rear emitters and normal vehicle sound volume;
  stop on throttle return, menu/focus blocking, flight and exit. Prepare three
  neutral-named PCM clips locally; no source-game library is shipped or loaded.
  Raise blow-off by 2.3 dB relative to the previous softened mix.
- Fix the 350Z front/rear engine loops crowding out tyre, turbo and release
  voices at Valheim's 32-real-voice limit. Let silent RPM bands yield to audible
  effects through normal loop priority; keep the global audio settings intact.
- Fix the new front/rear sound emitters' initialization order: create each
  AudioSource before adding its native filter. The reversed order rejected
  the filter and aborted vehicle audio loading, silencing all vehicle sounds.
- Refine 350Z audio using the existing NFSU2 recordings: soften blow-off and
  shift releases, ignore throttle feathering, and
  fade long release tails to silence within 550 ms. Keep boost/RPM/handling
  controls separate from these acoustic changes.
- Place filtered engine and exhaust voices at the front and rear, sharing the
  decoded engine clips and starting on the same DSP time. Smoothly emphasize
  engine/turbo when listening from the front and exhaust from behind. This is
  NFVU2's own mix, not a recovered NFS/Forza implementation; Hummer stays on
  its existing audio path. Add an offline front/rear listening illustration.
- Add `Effects.TyreSmokeEnabled` (default true) to skip rear-tyre smoke creation
  entirely when disabled, allowing a controlled rendering comparison after
  repeated GPU hangs. Restart required; this is not a confirmed crash fix.
- Add a separately enabled 350Z drift chase camera: follow 95% of the bounded
  travel angle with softer heading response and a little more road look-ahead.
  Show more of the car's side through a slide while preserving mouse/right-stick
  inspection and reverse protection. Enable through Camera.DriftFollow even when
  ordinary DynamicFollow is disabled; expose the toggle in the driving display menu.
- Fix jittering drift wheels by attaching their poses to the interpolated
  rendered chassis. Blend suspension, steering and signed wheel rotation
  between fixed ticks; keep brake calipers and cockpit steering synchronized.
  Preserve full wheel revolutions at high RPM instead of interpolating backward
  through the shortest quaternion path. Driving forces and tyre tuning are unchanged.
- Replace the private native backend experiment with `NFVU2.DriftPhysics.dll`,
  an independent C# library built from the repository's `physics/` sources.
  Implement an own engine/clutch/automatic gearbox and combined-slip tyres;
  require no original racing-game binary or external game installation.
- Connect the library to Valheim through raycast suspension and an own steering
  adapter. Let Unity integrate chassis forces and resolve collisions; disable
  duplicate WheelCollider forces and per-body Unity gravity while active.
  Use 9.81 m/s2 vehicle gravity without changing the world's gravity.
- Restore previous physics state on mode changes, flight, transformation, exit
  and cleanup. Keep other drive modes and Hummer handling on their existing path.
- Feed own wheel poses, RPM, gear and contact telemetry into gauges, sound,
  tyre smoke and NFVU2 nitro. Enable the new backend for 350Z drift mode through
  `[DriftPhysics] Enabled`; retain the previous drift controller when disabled.
- Build the portable library separately and include it in the regular payload.
  Preserve the existing plugin ID and `Rachel350Z.dll` name. Validate the new
  dependency in installer payloads while retaining older package compatibility.
- Provide portable-library and adapter test suites plus an explicit menu-only
  Unity/PhysX probe. Pass 52 core checks, 187 adapter checks and 134 actual Unity
  checks with the separately installed physics library. Personal gameplay
  acceptance remains open.
- Retain the former CarX Playtest experiment and its provenance only in ignored
  local archives. Its original native library is no longer a runtime dependency.

- Add NFVU2's own world generation for the exact seed `drift` (case insensitive),
  built into the existing `Rachel350Z.dll`. FlatGrass is not required.
- Generate flat Meadows at height 32 without native structures, vegetation or
  ore deposits, and supply a world-centre start point. Keep native world-edge
  behavior and normal-world generation.
- Keep the existing NFVU2 plugin identity and regular build/install workflow.
  No extra world plugin, separate installer or FlatGrass bridge is needed.
- Highlight the exact `drift` seed in green in the new-world menu, using the
  same predicate as world generation. Restore the previous text color on edits,
  panel changes and plugin shutdown, preserving feedback supplied by other mods.
- Rewind the native generation-result buffer before its caller reads it. This
  fixes an `EndOfStreamException` that left drift worlds stuck at "Generating".
  The regression test now uses Valheim's actual ZPackage implementation.
- Add the first 335 m drift test section east of the spawn: two 108 m straights
  joined by a 38 m radius hairpin, 16–20 m wide. Use native paved terrain,
  sparse grounded stone guides, supported signs/start banners and a map pin.
- Build loaded tiles incrementally after native readiness and save progress
  alongside native terrain data. Resume unfinished tiles; completed construction
  does not repaint or respawn deliberately dismantled guides on each login.
  Keep terrain height and vehicle handling unchanged.
- Turn generated sign faces away from their supporting posts. Correct saved
  NFVU2 signs when their original position, text and rotation are unchanged;
  preserve player edits and do not reconstruct dismantled signs.

## 0.31.2 — ZIP updates and older installer compatibility

- Detect and prefer installer ZIP releases, verify the ZIP checksum, extract
  only the expected setup EXE and verify its inner checksum before starting it.
  Keep the selected Valheim path and update handoff behavior.
- Reject unexpected ZIP contents, linked entries, oversized files and incorrect
  checksums. Cancellation and failed downloads clean up their own partial files.
- Keep ZIP as the main download and publish the identical standalone EXE and its
  checksum for older installers, including 0.31.0 and 0.31.1. Those installers
  can update to this version; this version can use ZIP updates thereafter.
- Gameplay, models, sounds and all 27 radio tracks remain unchanged.

## 0.31.1 — Installer ZIP download

- Distribute the Windows installer in `NFVU2-Setup-0.31.1.zip`, with the setup
  EXE, its SHA-256 checksum and extraction/installation instructions inside.
  Publish a separate SHA-256 checksum for the ZIP.
- Extract the ZIP before running the setup. BepInEx, all 27 radio tracks,
  models, sounds and gameplay are unchanged from 0.31.0; only the shared
  mod/installer version and release packaging change.
- Download future ZIP releases from GitHub manually. The existing installer
  update downloader requires standalone EXE assets and cannot load ZIP releases.
  ZIP packaging does not change the executable's security properties.

## 0.31.0 — 350Z Transformer and Rust minigun

- Transform the 350Z into a pilotable robot and back, with five-second panel
  animations, articulated walking, sprinting, turning and a rotating flame reactor.
  Climb onto the robot and dismount with a backflip; keep human knees bending forward.
- Add the Rust minigun on the left shoulder with mouse/camera aiming, a laser,
  1000-round magazines, audible spin-up, automatic reload and lingering barrel heat.
  Left mouse / LB / L1 fires; loaded targets can be engaged up to 250 m away.
  Direct shots deal 12 pierce damage and confirmed hits show remaining enemy health.
- Use explosive minigun rounds by default, reusing the existing impact explosions,
  blast cover checks and small terrain craters. F9 / L3 while the boarded robot is
  stationary opens its minigun/settings pages, including Normal/Explosive ammunition.
  The car roof turret retains its separate ammunition setting.
- Add varied bass-layered footfalls, pivot sounds, short mechanical and hydraulic
  transformation layers, reactor loops and a late deployment sub-drop. Preserve
  engine idle under the robot audio and play a separate return transformation mix.
- Leave temporary dirt footprints and contact dust; flatten nearby grass without
  permanent footstep terrain deformation. Run over native trees, logs and small
  hostile enemies while protecting players, tames, bosses and buildings.
- Allow shallow wading up to the robot's chest, block deeper water and use terrain
  support rather than floating across lakes. Loose loot and destruction fragments
  no longer become false ground or stop the robot.
- Add a tighter cinematic deployment camera with a slow orbit and zoom, obstacle
  avoidance and smooth handoff. Mouse/right-stick input takes control immediately.
  Raise the bottom-right vehicle hints so the last line stays on screen.
- Save and restore parked robot form with the world. Add vehicle map markers and
  location discovery while driving, earlier restoration of nearby saved vehicles,
  subtle idle exhaust vapour and nitro purge while coasting.
- Use E to enter/exit, retaining native object interactions and F7 as an alternative.
  RB / R1 transforms while boarded and stopped; flight retains its altitude controls.
- Include the Transformer model, animation, prepared sounds and minigun in the
  normal installer. Enable the experimental Transformer by default for fresh
  configurations; existing settings are preserved, including an explicit disable.
- Lift a sprint support foot before its reach limit makes it slide, retaining the
  existing stride and speed. The 50 Hz physics / 60 Hz render trace now meets its
  unchanged 4 mm planted-foot bound.

Experimental single-player feature. Automated checks use controlled doubles;
Unity terrain, collision, equipment, camera feel and controller hardware still
need gameplay verification on other installations.

## 0.30.0 — Persistent vehicles

- Save each 350Z/H2 with its world: model, position, rotation, flight state and
  cargo share the existing native chest record. Empty cars persist too.
- Restore cars automatically when their area loads, retaining the same cargo
  inventory. Distant cars count toward the four-car limit; parked cars wait
  for terrain collision without requesting distant terrain generation.
- Remove cars only through the vehicle menu; F8 only cancels loading. Removing
  a car still leaves nonempty cargo in a normal chest.
- Previously released recovery chests contain no vehicle metadata and remain
  ordinary chests. Single-player only. The user confirmed that leaving and
  re-entering a world retains the car; additional edge cases remain in ROADMAP.

## 0.29.0 — Installer updates, world streaming and vehicle inner surfaces

- Windows setup 0.29.0 checks the latest public GitHub release when
  opened, without blocking offline installation. Compare Major.Minor.Patch numerically
  and provide a manual retry and an in-window Changelog button.
- Use a shared Major.Minor.Patch version for mod and installer; remove the setup
  revision suffix. The installer is named `NFVU2-Setup-0.29.0.exe`. Legacy public
  filenames remain readable, but revisions do not influence update ordering.
- Download updates only from this repository's releases; verify exact size,
  SHA-256 sidecar and available GitHub digests before starting the new setup.
  Forward the selected Valheim path so the new installer can continue installation
  with its own current install engine. Downloads show progress and support cancel.
- Include the packaged changelog for offline use. Existing r6 and older EXEs need
  a one-time manual download of 0.29.0 or later to gain update detection.
- Keep Valheim's world-streaming reference at the live driver while driving or
  flying. Prepare approaching terrain on the native builder and wait only for
  immediately missing terrain collision, with delayed messages and preserved
  momentum across brief loading gaps. Native location generation can still stutter.

- Remove black-backed ring decals that made the closed brake caliper bosses
  look like exposed pistons. Replace hub/bolt atlas squares with solid metal
  discs and separate the metal hub finish from red caliper paint.
- Replace the plain stock hood underside with a STYLE07-shaped graphite inner
  skin, reinforcement profiles, four matte pads and fasteners. This inner
  finish is authored for NFVU2; the original carbon exterior and hinge remain.
- Fit the donor dashboard and pillar fronts behind the original windshield
  and seal, including faces crossing the upper edge. Check clearance against
  all three engine variants and inspect closed/open hood previews.
- Preserve the original door edge returns when loading the detailed cabin.
  Split the original rear inner shell from the simplified cockpit so the
  hatch opening stays connected to the body with every audio enclosure.
- Restore the cabin's full A pillars and roof lining instead of cutting them
  off at one metre; fit the lining below the actual exterior roof. Verify
  the fit in external previews; native shading still needs an in-game check.
- Add reverse faces with separate inward normals to the imported body, wheels,
  brakes and cabin meshes, including roof, doors, hatch and glass. Existing
  pivots and wheel sizing apply to both sides. Audio membranes retain their
  original vertex layout.
- Check winding, normals, UV/color preservation and 16/32-bit index limits.
  Visibility, transparent glass and lighting still need an in-game check.

## 0.28.0 — Horde mode, minimap radar and vehicle storage

Public release highlights since 0.22.3 (Windows setup revision 6):

- New 350Z horde mode: five mixed enemy waves, a Moder finale, vehicle health,
  a shimmering hexagonal shield, colored powerups, combos and victory medals.
- Red enemy dots on the minimap, a localized start hint, and smooth transitions
  to stormy night at the start and clear daytime at the end.
- Randomized god pacts with benefits and drawbacks, including freezing ice
  ammunition and chain lightning. Choices open immediately between waves.
- Vehicle storage: 24 / 18 / 12 / 6 slots by 350Z trunk enclosure and 32 for H2.
- Fix radio dropouts and crowded-out turret audio, shield rendering over windows,
  distracting lightning tint flashes and excessive boss burst damage. Remove
  wave-long automatic fire from the finale's pact selection.
- Bound horde spawns and clean up corpses/projectiles without enemy loot buildup.
  The following development entries document the individual changes in detail.

Vehicle storage details:

- Add native chest inventory access via Vehicle → Open storage while parked,
  outside and near the rear. Each vehicle has an independent inventory.
- 350Z enclosures provide 24 / 18 / 12 / 6 slots; H2 provides 32. Block an
  undersized enclosure for any loaded 350Z; repack retained item instances.
- Save cargo through persistent vanilla chest ZDOs. Loaded cargo follows the
  car; removal/world exit restores a normal chest with its existing contents.
  Zone unloading never creates a replacement inventory. No custom save format.
- Show occupancy/capacity and localized German/English access guidance; opening
  the 350Z storage also opens its hatch. Keep driving/handling unchanged.
- Add automated storage lifecycle/resize checks. Native UI, world save/reload,
  chest placement and controller operation need in-game verification.

## 0.27.2 — Boss damage spikes and horde discovery hint

- Reduce encounter Moder's incoming damage by 45%, cap final hits at 180
  (225 enraged), and limit her multi-hit breath/projectile bursts to one
  accepted vehicle hit per second. Other enemy and collision damage is unchanged.
- Add an outlined gold/white German/English start hint below the minimap in a
  ready, grounded 350Z: “Face the horde / Numpad 9 · Begin the battle”. Position
  follows the native map and biome label; hide during gameplay UI and active runs.
- Verify boss burst protection, post-modifier caps, regular enemy damage and
  localization in automated checks. In-game boss balance and hint layout remain
  manual checks.

## 0.27.1 — Horde feedback and damage tuning

- Remove lightning's blue enemy tint flashes, retaining chain arcs and colored
  tracers. Ice keeps its freeze tint and thaw restoration.
- Exclude Valkyrie's Watch from the final wave's offers; retain three distinct
  choices with at least two changes from the previous wave. Temporary auto
  pickups remain available.
- Increase incoming enemy attack damage in horde mode by 20%. Pact and boss
  enrage multipliers still apply; collision damage is unchanged.

## 0.27.0 — Elemental ammunition and dragon finale

- Open pact selection immediately after a completed wave. Offer three of six
  pacts, ensuring at least two offers change between consecutive waves.
- Add Skadi's Winter: briefly freeze ordinary enemies with white-blue tint and
  icy tracers; reload 20% slower. Bosses are slowed rather than immobilized.
  An immunity window prevents permanent freezing; thaw/cleanup restores visuals
  and movement. Add Storm Chain: visible lightning arcs to up to three other
  nearby horde enemies, with falling damage, obstruction checks and a shared
  cooldown; direct bullet damage is reduced by 20%.
- Add Valkyrie's Watch: automatic turret for the whole wave, in exchange for
  25% more incoming enemy damage. Replace redundant ammo/auto pickups during
  corresponding pacts. All new UI supports German and English.
- Replace the troll finale with native Moder, two drakes and four Fulings.
  Retain native 7,500 base boss HP and attacks; add a half-health enrage and
  faster transitions back to flight. Extend auto targeting to 60 m in horde mode.
- Suppress the encounter dragon's progression flags, cinematic and loot;
  clean up its tracked projectiles and suppress persistent ice obstacles.
  Use a separate best-score entry for the changed rules.
- Automated checks cover offer diversity, immediate transitions, freeze/thaw,
  boss slow, material restoration, chain filtering/range/cover/cooldown, bounded
  arc visuals, expanded auto range and existing combat behavior. Native visuals,
  boss flight/navigation, attack balance and controller input need in-game testing.

## 0.26.0 — Mixed waves and pacts of the gods

- Replace the long forest-only encounter with five mixed waves of 15/20/18/14/13
  enemies: forest creatures, skeletons, Draugr, wolves, Fulings and berserkers,
  ending with a marked troll and boss health bar. Encounter members are allied.
- Choose Odin's Haste (40% shorter reload / 20% faster enemies), Thor's Wrath
  (wave-long explosive ammunition / 35% more enemy HP), Iron Chariot (double
  run-over damage / 25% less turret damage), or no pact before each wave.
  Choices support mouse, number keys and controller; temporary effects reset
  between waves and on exit, defeat, reset and world changes.
- Clear 85% of actual spawns plus every elite to advance; remaining small enemies
  despawn without score. Retain the eight-second break, then open pact selection.
- Waves 2–4 offer a marked elite hunt: 25 seconds for 500 points and 250 HP repair.
  Use a separate personal-best entry for the new wave rules.
- Add German/English selection, pact, objective and boss UI. Preserve existing
  shield, corpse cleanup, no-loot policy, minimap radar and weather transitions.
- Build and targeted automated checks cover scheduling, deadlines, pact expiry,
  reload time, impact damage, bullet/blast damage and prior horde integrations.
  Native AI, terrain navigation, UI input and difficulty still need an in-game test.

## 0.25.5 — Horde minimap radar

- Show living horde enemies as small red dots with a dark outline on the native
  minimap. Positions follow movement, map scrolling and zoom; off-screen dots
  are hidden within the minimap border.
- Reuse at most 100 UI dots. Remove dead/missing enemies from the display and
  dispose the radar on run end, reset or world exit. Respect the hidden HUD.
- Use a local UI overlay without saved/shared pins, icon-filter changes or
  exploration changes. Keep the large map and unrelated creatures untouched.
- Native build and projection/horde/weather checks passed; minimap appearance
  and HUD scaling still need an in-game check.

## 0.25.4 — Horde storm and night transition

- Starting a horde run transitions to midnight and native ThunderStorm weather
  over eight seconds. Victory, defeat, reset and other encounter exits transition
  back to noon and Clear weather, including after removing the car.
- Keep clear daytime for the current world session. Restore previous local
  environment/time overrides when leaving the world or unloading the plugin;
  never advance the saved world clock or carry overrides into another world.
- Weather lifecycle, restart/abort, world-switch and existing horde checks passed;
  sky/rain/lightning presentation still needs an in-game check.

## 0.25.3 — Shield over glass and turret audio priority

- Give the shield its own material, drawn after transparent cabin windows;
  retain world depth testing and leave pickup materials unchanged.
- Prioritize occupied-car turret shots over ordinary crowd sound effects,
  with quieter tails and servos below the shot attacks. Radio retains first
  priority; parked turrets return to ordinary sound priority.
- Verify bounded gunfire playback and priority transitions with adapter checks.
  Window compositing and crowd-combat audibility need an in-game check.

## 0.25.2 — Shield dome and radio during combat

- Project a connected hexagonal grid over a larger shield dome. A visible,
  gently shimmering baseline remains at any positive charge; hits brighten it.
- Rename the pickup to “Explosivmunition” / “Explosive ammunition” and remove
  “test” from horde reset/already-active messages.
- Give occupied-car radio the same audio priority as native Valheim music.
  Bound turret gunfire to four shot sources and two quieter tail sources,
  replacing stacked three-second one-shots that could crowd out the radio.
- Build, bounded-audio, radio, turret, localization and horde checks passed.
  Actual music continuity and dome appearance still require an in-game check.

## 0.25.1 — Horde cleanup and visual polish

- Keep horde entry restricted to a grounded 350Z with turret assets; H2 remains excluded.
- Track horde ragdolls separately: no saved loot, nonpersistent, three-second
  lifetime, maximum twelve and immediate cleanup on run end/reset. Ordinary
  world corpses and loot are unaffected.
- Replace gray horde HUD/pickup/result boxes with larger outlined text,
  slim colored health/shield bars, wave milestones and a combo countdown.
- Replace the shield rings with a subtle animated hexagonal shell that flares
  on shield hits and recharge. Health-only hits do not revive the shield effect.
- Build and adapter/logic checks passed; HUD layout rendered in DE/EN at
  720p/1080p. Native ragdoll cleanup and shield appearance still need an in-game check.

## 0.25.0 — Vehicle survival and powerups

- Make the five-wave horde a 350Z vehicle mode with 1,000 HP, 300 shield and
  off-road driving. Native enemy hits and hard obstacle collisions drain shield
  before HP. Route seated-driver hits to the vehicle while the mode is active.
- Add colored beacon/rune pickups: one 30-round explosive magazine, 15 seconds
  of automatic turret fire, 150 shield or 250 HP. Limit pickups to four; place
  them on clear dry terrain near the route and expire them after 45 active seconds.
- Add a shield visual, health/shield HUD, combo scoring up to x5, victory medals,
  survival/time bonuses and a local best score. Incomplete formations cannot set
  records. Losing ends the run and keeps the vehicle available for another try.
- Scope driving/turret overrides to the encounter, restore normal settings on
  cleanup and suppress other parked turrets during the run. No saved preference
  changes for temporary buffs. Exiting also cancels the encounter.
- Clear each spawned enemy's drop list as well as disabling direct drops, so
  native ragdolls cannot generate loot. Mark spawned enemies as test spawns.
- New logic and native callback adapter checks; in-game balance, collision
  behavior, HUD and beacon rendering still require playtesting.

## 0.24.0 — The forest strikes back

- Replace the 30-Greyling test with five waves: 60 Greylings; 90 Greylings;
  50 Greydwarfs/8 brutes/2 shamans; 40 Greydwarfs/10 brutes/1 troll;
  30 Greydwarfs/3 trolls. All enemies remain at no-star level.
- Eight-second breaks after clearing each wave, a wave/enemy/countdown HUD,
  five DE/EN attack announcements and a final victory message.
- Groups approach from the front and diagonally, following the driver's current
  position. Trolls spawn farther away with clearance based on their collider.
- Cap living test enemies at 100; bound placement work per frame and per slot.
  Abort completely blocked waves; keep reset, pause and world-change cleanup.
- Build and automated logic/adapter checks do not establish Unity gameplay or
  performance. The initial 30-Greyling test ran well on the user's PC.

## 0.23.0 — Local horde recording test

- Numpad 9 spawns up to 30 no-star Greylings in a forward arc, 30–45 m away,
  on clear, dry terrain. Works on foot and while driving; uses native hunting AI.
- Announce the attack in the selected DE/EN interface language. Spawn gradually,
  cap placement attempts and allow only one living test horde at a time.
- Numpad 0 removes this test horde and cancels pending spawns. Death, teleport
  and world changes clear it too. Test enemies are not saved and drop no loot.
- Menu, chat, pause and focus loss block the hotkeys and pause pending spawns.
  Vehicle handling, collision damage and turret damage are unchanged.
- Local build; Unity AI, placement, combat and PC performance await playtesting.

## 0.22.3 — Vehicle crosshair fix

- Hide Valheim's crosshair while seated in either vehicle, including when no
  turret is equipped. Restore it on exit; keep the turret's own aim marker.
- Windows setup revision 5 packages the crosshair fix; vehicle handling is unchanged.
- Add project skills for local installer builds and GitHub release publication.

## 0.22.2 — Rust menu badge fix

- Keep the Rust logo on the Turret menu entry when the menu is moved or scaled.
  Preserve the tilted sticker style without shifting it onto the Display entry.
- Add three animated in-game previews to the README: front, rear and flight mode.
- Windows setup revision 4 packages the updated mod; vehicle handling is unchanged.

## 0.22.1 — First public installer release

Windows setup revision 4 includes the complete radio playlist in Ogg Vorbis
format. The installer is approximately 148 MB.

### Windows setup and documentation

- Setup revision 4 explains that BepInEx loads mods, is required by NFVU2,
  is installed automatically when needed and should be kept for other mods.
- Retired the old prototype tag `v0.1.5`; reserve tags for actual releases.

- Setup revision 3 supports a smaller radio payload: prepare verified Ogg
  Vorbis copies at a 192 kbit/s target while retaining local WAV masters.
  The build preserves all tracks, titles and order, validates source/output
  checksums and rejects stale compressed copies. The encoder stays local.
- Installer regression checks cover WAV-to-Ogg updates, removal of obsolete
  owned WAV files and preservation of personal music files.

- Added an offline Windows setup EXE with game selection, installation,
  updates and removal; no Python or developer tools needed by players.
- Bundled BepInExPack Valheim for fresh installs; existing loaders are preserved.
- Added SHA-256 verification, recovery after
  interrupted operations and retention of two timestamped backups.
- Setup revision 2 adds the supplied logo to the wizard and README, renames
  the notices button to LICENSE and adds Close/Escape/Enter to its dialog.
  Radio music is always included and installed; removed the radio checkbox.
- Uninstall preserves settings and other mods. The loader can also be removed
  when this wizard installed it and no other plugins or patchers need it.
- Replaced the README with a short English player guide. Moved development
  requirements to CONTRIBUTING and kept detailed import/gameplay notes in docs.
- Package filenames now use the mod version instead of a fixed old version.
- No changes to vehicle handling, game controls or the plugin identity.

### Controller rumble, door warnings and nitro visuals

- Clear native camera shake while the vehicle/menu camera owns the view, so
  a shake cannot keep controller vibration running without its normal decay.
  Restore native rumble behavior on exit.
- Show blocked driver-door messages in the center of the screen, like spawn messages.
- Strengthen the nitro edge blur, set its depth test explicitly and smoothly
  widen the camera field of view during boost. Restore the original view on
  release, menus, pause, exit and effect removal.
- Avoid accessing an absent vehicle rigidbody during exit/world teardown.
- Document vehicle removal with a controller in the README.
- Automated checks cover logic/adapters; vibration and rendered appearance
  still need an in-game controller check.

Earlier entries below retain their original German wording.

## 0.22.0 — Turbolast, originale Druckanzeige und kleine Bäume

- Kleine Bäume/Büsche ab 0,25 m/s statt 2,5 m/s; native Kontakte können die
  Pflanze auch nach dem Abbremsen durch die Physik treffen. Gerichtetes Gas
  bei echtem Kontakt befreit ein bereits hängendes Fahrzeug. Gegner behalten
  ihre bisherige Schadensschwelle, Wände und große Bäume bleiben geschützt.
- Turbo und Blow-off folgen einem gemeinsamen Lastzustand. Ereignislautstärke
  wird vor Druckabbau gespeichert; keine starren Low-/High-Pegel mehr. Der
  Import erhält relative Samplepegel mit gemeinsamem Gain statt RMS-Normalisierung.
- Analoge Gasrücknahme über mehrere Frames erzeugt ebenfalls einen Blow-off;
  kleine Trigger-Schwankungen nicht. Menü, Pause, Ausstieg und Flug setzen
  den Turbo zurück, ohne eine künstliche Entladung auszulösen.
- Originale Turbo-Zifferblätter aller elf NFS-Stile ersetzen die zusätzliche
  Geschwindigkeitsanzeige. Digitale Geschwindigkeit bleibt erhalten; H2 ohne
  Turbo blendet das kleine Instrument aus. Ältere HUD-Dateien bleiben lesbar.
- Unterdruck und 0,42 s abklingendes Flattern ergänzen die Nadelbewegung.
  Die Druck-/Gain-Kurve ist eigene NFVU2-Abstimmung, keine vollständige
  Rekonstruktion der NFS-Auslöselogik. Belege/Abgrenzung siehe REFERENCES.
- Offline-Soundvergleich: Teil-/Volllast unterscheiden sich um ca. 8,3 dB,
  ohne Motorlast bleibt der Turbo still; keine übersteuerten Samples.
  Unity-Physik, tatsächliche Mischlautstärke und HUD im Spiel noch prüfen.
- Build, 54 Python-Tests sowie 87 Audio-, 24 HUD-/Fahranzeige-, 59 Kollisions-
  und 31 Ladeprüfungen bestanden. Dies sind keine Unity-Laufzeittests.

## 0.21.0 — Menüführung, Antrieb, Turret und Auspuff

- Kategorien und Inhalt haben getrennten Controller-Fokus; Stick/Steuerkreuz
  navigieren beide Bereiche. LB/RB springen zwischen ihnen; Regler und
  Auswahlfelder haben einen mit A/B bedienbaren Bearbeitungsmodus.
- Ohne erreichbares Auto bleiben Garage/Einstellungen sichtbar. Autorenzeile
  „by BCKWDZ“ und optionales originales Rust-Logo am Turret-Eintrag ergänzt.
- Normale Munition von 35 auf 14 Stichschaden reduziert; Explosivmunition
  erhält native Holz-/Steinschäden für große Bäume und abbaubare Felsen.
- Mehr Motormoment und gezielter Anfahrschub vorwärts/rückwärts; Federung und
  Fahrwerksgeometrie bleiben von Audio- und Auspuffänderungen getrennt.
- Neues Auspuff-Menü für Klangvarianten der vorhandenen Originalsamples und
  Nitroflammenfarbe. Klangabstimmungen und Fahrphysik sind NFVU2-eigene Regeln.
- Zehn originale 350Z-Auspuffblenden STYLE01–10 aus lokalen NFS-Dateien;
  Flammenpositionen folgen den tatsächlichen Öffnungen. Bisherige STYLE08
  bleibt Standard, ältere Modellimporte behalten ihre vorhandenen Endrohre.
- Build, 53 Python-Tests und alle 20 C#-Testsuiten bestanden. Blender-Prüfung
  von STYLE01/STYLE08 zeigt korrekt montierte Einzel-/Doppelrohre ohne
  überlagerte Varianten. Plugin-ID und Rachel350Z.dll bleiben erhalten;
  Controller, Fahrgefühl, Klang und Partikeleffekte benötigen Unity-Spieltests.

## 0.20.5 — Fahrzeughinweise mit Valheim-Controller-Symbolen

- Native Kampf-/Bau-/Angelhinweise im Fahrzeug durch ein zweispaltiges Feld
  unten rechts ersetzt; nutzt Valheims Schrift und vorhandene Xbox-/PS5-Sprites.
- Passende Belegung für Straße, Flug und offenes Modmenü; LB/L1 im Flug ist
  Sinken, das Geschütz wird als automatisch angezeigt. H2-Nitro nur im Flug.
- Gerätewechsel, erzwungener Controller-Typ, DE/EN und Hinweisgröße berücksichtigt.
  Tacho oberhalb der Hinweise, bisherige Textleiste im Fahrzeug entfällt.
- Aussteigen stellt die nativen Hinweise wieder her; Inventar/Pause behalten
  ihre nativen Hinweise. Ausgeblendetes HUD blendet auch Fahrzeughinweise aus.
- Glyphnamen in installierten Spielassets geprüft; Zuordnungs- und bestehende
  Logiktests bestanden. Layout-/Controller-Prüfung im Spiel steht noch aus.

## 0.20.4 — Kamera im offenen Modmenü

- Rechter Stick dreht die Kamera auch im Menü. Mit der Maus rechte Taste halten
  und ziehen; im Fahrzeug zoomt währenddessen das Mausrad.
- Zu Fuß bleibt die native Kamerapositionierung mit Hindernisprüfung erhalten.
  Menüklicks, Songlisten-Scrollen und Verschieben lösen keine Kameradrehung aus.
- Fahr-, Nitro- und Waffenaktionen bleiben gesperrt. Pause/Fokusverlust beendet
  die Kamerabedienung. Hinweise zur Bedienung stehen im Menü (DE/EN).
- Build und Logikprüfungen ersetzen keinen Kamera-/Cursor-Test im Spiel.

## 0.20.3 — Fahrzeugspawn als Spielmeldung

- Nach erfolgreichem Laden und Platzieren erscheint „Gespawnt: Nissan 350Z“
  bzw. „Gespawnt: Hummer H2“ in der nativen gelben Mitte-Anzeige (DE/EN).
- Abgebrochene oder fehlgeschlagene Ladevorgänge zeigen keine Erfolgsmeldung.

## 0.20.2 — Fahrmodus als Spielmeldung

- Erfolgreiche Fahrmoduswechsel per Tastatur, Controller und Menü zeigen eine
  native gelbe Meldung in der Mitte: „Fahrmodus: 'Gelände/Straße/Drift/Fliegen'“.
- DE/EN folgt der Spracheinstellung. Unveränderte Auswahl und abgelehnte
  Flugstarts erzeugen keine Wechselmeldung; beim Landen erscheint der gewählte
  Bodenmodus. Kein zusätzlicher Eintrag im Nachrichtenlog.

## 0.20.1 — Schnellleiste während der Fahrzeugsteuerung sperren

- Native Hotbar-Navigation und Gegenstandsbenutzung während Fahrt, Flug und
  geöffnetem Modmenü gesperrt; zu Fuß bei geschlossenem Menü wieder verfügbar.
- Die Anzeige von Inventar, Haltbarkeit und Stapelgrößen aktualisiert sich weiter.
- Regressionstests für die Eingabefilterung; Prüfung im Spiel steht noch aus.

## 0.20.0 — Controller, direkte Aktionen und Sprache

- Analoge Xbox-/PS5-Steuerung, L3-Menü und B/Kreis Ein-/Aussteigen. Xbox:
  X Nitro, A Handbremse, LB Geschütz; PS5 Quadrat, Kreuz, L1. Flug: RB/R1
  steigen, LB/L1 sinken. Fluggeschütz nutzt automatisch vorhandene Zielerkennung.
- Fahrmodi direkt per 1–4 oder Steuerkreuz hoch/runter, H2 überspringt Drift.
  F7/B/Kreis im Flug landet vor dem Ausstiegsversuch. Songs per Pfeil-/Steuerkreuz
  links/rechts, Titel-Einblendung nach tatsächlichem Wiedergabestart.
- Controller-Menüfokus für Buttons, Toggles, Regler, Auswahlfelder und Songliste;
  Kategorien per Schultertasten, zurück per B/Kreis; Mausbedienung erhalten.
- DE/EN unter globalen Einstellungen, Gerätehinweise mit größerer einstellbarer
  Schrift; Controller-Typ automatisch oder manuell Xbox/PlayStation.
- Separate Eingabepolitik, Freigabe erst nach Loslassen nach Unterbrechungen,
  gezielte Sperre nativer Spielersteuerung. Bestehende Fahrwerksabstimmung bleibt.
- Automatische Logiktests; Controller-, UI- und Unity-Spieltests stehen noch aus.

## 0.19.0 — Scheinwerfer-Show nach dem Aussteigen

- F9 → Lichter: „Show-off beim Aussteigen“, standardmäßig aus, für 350Z und H2
  separat gespeichert. Nach dem Aussteigen zweimal links, zweimal rechts,
  dann Pause; 2,4-s-Zyklus mit vier 160-ms-Blitzen. Links/rechts aus Fahrersicht.
- Lichtkegel und leuchtende Lampenflächen folgen derselben Phase. Seiten aus
  Fahrzeugkoordinaten statt spiegelverkehrter NFS-Teilenamen bestimmt.
- Einsteigen beendet die Show und aktiviert normales Fahrlicht. Abschalten
  stellt den normalen Lichtzustand wieder her; erneutes Aktivieren startet links.
  Pause/Fokusverlust friert die Phase ein. Eigenständiger Zustand pro Fahrzeug.
- Farbe und Stärke gelten auch für die Show; Motor-/Radio-Weiterlaufen ist
  unabhängig. Brems-/Rückfahr-/Rücklicht unverändert. Reset schaltet Show-off aus.
- Automatische Ablaufprüfungen; sichtbare Lichtkegel, Emission und Menü im Spiel prüfen.

## 0.18.4 — Metallic-Lack

- Lackflächen werden beim Import ausdrücklich markiert. 350Z-Lack/Vinyl und
  gelbe H2-Karosserie erhalten ein frisches Material mit Metallic 0,72 und
  Smoothness 0,88; geladener Standard-Shader mit geprüftem nativem Fallback.
  Keine geerbten Gebäude-Normal-/Glanzmasken. Farbe und Vinyl bleiben erhalten.
  Carbonhaube, Scheiben, Innenraum und Felgen behalten ihre eigenen Einstellungen.
- Importzuordnung automatisch geprüft; Glanz und Reflexionen müssen bei
  Sonne/Schatten/Nacht im Spiel geprüft werden.

## 0.17.0 — Nitro-Tunnelblick und Rücklichtspuren

- Sechs radial versetzte Bildlagen verwischen beim tatsächlichen Nitro-Boost
  den Bildrand. Ein weicher Schutzbereich hält die Fahrzeugprojektion und
  die Bildmitte scharf; HUD/Overlay werden danach gezeichnet. GPU-Kopie in
  halber Auflösung, keine CPU-Bildrücklesung. Filter nur während Boost/Fade aktiv.
- Zwei kurze rote Lichtbänder folgen den Rücklichtpositionen. Weicher Querschnitt,
  verjüngtes Ende und ungefähr drei Meter Maximallänge. Tiefentest bleibt aktiv.
  Nur bei eingeschalteten Rückleuchten und tatsächlichem Boost, auch im Flug.
- F9 → Nitro: Tunnelblick und Rücklichtspuren separat abschaltbar; gemeinsame
  Stärke 0–100 %, Standard 70 %. Gemeinsame Einstellungen für beide Fahrzeuge.
  Menü, Pause, Fokusverlust, Ausstieg, Welt-/Kamerawechsel räumen Effekte auf.
- Eigene UVs, Gewichtung, Maske und Spurzeiten; keine unveränderte Übernahme
  der NFS-Shader. Die ergänzend geprüfte yugecin-Quelle liefert Grafikschalter
  und Parameternamen, keine vollständige Nitro-/Spur-Implementierung.
- Keine Änderung von Fahrzeugsteuerung, Nitro-Schub, Tank oder Auspuffeffekten.
  Automatische Prüfungen ersetzen keine Unity-Darstellungs-/GPU-Messung.

## 0.16.1 — Auspuff-Nitro auch im Flug

- Die gemeinsame Effektsperre enthielt bisher den gesamten Flugmodus und
  unterdrückte dadurch die vorhandenen 350Z-Auspuffflammen und Nitro-Sounds.
  Beide folgen jetzt dem aktiven Nitro-Zustand auf Boden und im Flug.
  Ohne tatsächlichen Boost bleiben sie aus; die vier Schwebe-Düsen sind unabhängig.
- Reifenrauch, Purge und Schubknallen bleiben im Flug gesperrt. Menü,
  Fokusverlust, Ausstieg und noch nicht abgeschlossener Einstieg sperren
  weiterhin die Auspuffeffekte. Keine Änderung von Schub, Tank oder Fahrwerk.
- 41 Nitro- und 37 Flugprüfungen bestanden; Partikel und Sound benötigen
  weiterhin den Unity-Spieltest.
- NFSU2-Bildfilter und Lichtspuren untersucht: getrennte Grafikoptionen,
  eingebettete D3D9-Filter und lokale Flare-Textur nachgewiesen. Die genaue
  Nitro-Kopplung, Filtergewichte und Trail-Historie bleiben unbestätigt.
  Quellen und Grenzen in REFERENCES; noch kein neuer Bildschirmfilter eingebaut.

## 0.16.0 — Fahrzeuge im Hintergrund vorbereiten und schrittweise spawnen

- Bisher liefen Dateilesen, JSON-Auswertung, Textur-Uploads, Mesh-Aufbau und
  Audioerstellung zusammen im Spawnaufruf. Der optionale Innenraum ergänzt
  rund 24 MB JSON. Modelldaten und PCM-Sounds werden nun auf einem Worker
  vorbereitet; der JSON-Reader liest aus dem Stream statt einer zusätzlichen
  vollständigen Dateikopie als String.
- Ladeanzeige mit aktuellem Abschnitt und animiertem Balken beim Einlesen.
  Aufbau von Karosserie, Rädern, Innenraum, Turret, Tacho und Motorclips in
  kleinen Arbeitsschritten mit etwa 3 ms Budget je Update. Einzelne Unity-
  Aufrufe können das Budget überschreiten; keine harte Framerate-Garantie.
- Unveränderte verwaltete Modell-/Sounddaten werden innerhalb derselben Welt
  wiederverwendet. Keine geteilten veränderbaren Unity-Materialien oder Meshes.
  Dateigröße/Änderungszeit invalidieren den Cache; Weltwechsel gibt ihn frei.
- Das Auto bleibt bis zum Abschluss inaktiv und außerhalb der Fahrzeugliste.
  Spawnplatz wird vor Freigabe erneut geprüft. F8/Abbrechen, Tod, Teleport,
  Weltwechsel und Ladefehler entsorgen Teilressourcen. Bereits vorhandene
  Fahrzeuge bleiben unabhängig; parallele Spawns und Einstieg während Laden
  werden gesperrt. Fehlende optionale Module behalten ihren bisherigen Fallback.
- Automatische Prüfungen für Hintergrundladen echter Assets, Cache/Invalidierung,
  Abbruch, Fehler und Ressourcen-Finalisierung. Im .NET-Test etwa 1,4 Sekunden
  beim ersten Einlesen gegenüber etwa 4 ms aus dem Cache; das sind keine Unity-
  Frametimes. Log ergänzt Gesamtdauer und längsten Hauptthread-Arbeitsschritt.
- Plugin-ID, DLL-Name und VehicleHandling bleiben erhalten. Spieltest offen.

## 0.15.2 — Nitro-Eingabe, Flammenanschluss und Reifenmaterial

- Flammenansatz von 48 auf 26 cm Abstand zum Radmittelpunkt korrigiert;
  die nicht gestreckten Partikel bleiben erhalten.
- Nutzer meldet weiterhin fehlendes Flug-Nitro. Installierte 0.15.1 und aktive
  Nitro-Konfiguration bestätigt; im bisherigen Log fehlen Eingabedaten.
  Zusätzlich zu Unity-Legacy-Mouse wird nun Valheims vorhandenes
  `ZInput.GetMouseButton` gelesen. W + linke Strg als Flug-Alternative,
  sichtbarer Boost-/Tank-/Loslassstatus und begrenzte Diagnoseausgaben.
  Der konkrete Laufzeitfehler ist damit noch nicht als reproduziert behauptet.
- Originaler 128×128-Racing-Hart-Atlas enthält Reifenprofil und Seitenwand;
  Textur und UV-Zuordnung waren vorhanden. Die 350Z-Gummifarbe multiplizierte
  diese auf 16 % herunter. Jetzt 80 %, nicht metallisch und rau, mit frischem
  Material statt geerbter Prop-Einstellungen. Keine neuen Reifenassets.
- Tests prüfen native Eingabe bei ausgefallenem Legacy-Mauswert, Tastaturalternative,
  Loslass-Sperre, Flugschub sowie bestehende Bodensteuerung; Reifen gerendert.
  Tatsächliche Eingabe und Effektposition benötigen den Spieltest.

## 0.15.1 — Flugtempo, Schubrichtung, Luft-Nitro und Tachonadel

- Nutzer bestätigt den Flugmodus. Steigen jetzt bis 12 m/s, manuelles Sinken
  bis 16 m/s, automatische Landung bis 10 m/s mit Abbremsen vor dem Boden.
  Die frühere Zwei-Meter-Zielbegrenzung drosselte die vertikale Geschwindigkeit.
- Felgen-Sichtseite beim Klappen nach oben gedreht. Räder, Bremsen und Spinner
  neigen sich mit dem Schub. Vorwärts zeigt die Flamme nach hinten, rückwärts
  nach vorne, beim Lenken zur Gegenseite; beim Loslassen gleitet die Neigung zurück.
- Düsen unter die Räder versetzt; normale statt gestreckter Partikel vermeiden
  die rückwärts über den Austritt hinausragenden Flammensprites. Emission startet
  erst gegen Ende des Umklappens, damit keine Flammen durch den Aufbau laufen.
- W + linke Maustaste aktiviert Nitro im Flug: höheres Vorwärtstempo und mehr
  Schub, Tankverbrauch und Nachladung. Gemeinsamer Boden-/Flugtank beim 350Z;
  H2 mit eigenem Flug-Nitro. Keine zusätzliche Boden-Nitrofreigabe bei Sprüngen.
- Deutlicher Drehzahlzeiger über den übrigen HUD-Ebenen und zusätzliche analoge
  Geschwindigkeitsanzeige mit passender KM/H-/MPH-Skala, neben dem NFS-Instrument.
- Build und gezielte Flug-, Nitro-, HUD-, Fahrwerks-, H2- und Audioprüfungen;
  HUD-Vorschau gerendert. Neue Fahr- und Effektwirkung noch in Unity prüfen.

## 0.15.0 — Flugmodus mit umklappenden Rädern

- F9 → Fahrzeug → Fliegen für 350Z/H2: animierte 90°-Radklappung, sanftes
  Abheben, Schweben mit Positionshaltung. W/S vor/zurück, A/D drehen,
  Shift steigen, Leertaste sinken. Vorwärts maximal etwa 65 km/h.
- Vier blaue Rad-Düsen aus dem vorhandenen NFS-FIREB-Atlas; tiefer abgestimmter
  lokaler Nitroloop mit Schubmodulation und Purge-Sound als optionalem Fallback.
  Keine neuen Originalassets, eigene Flugregelung und Animationsabläufe.
- Separater Flugcontroller übernimmt Kräfte und deaktiviert temporär die
  WheelCollider. Der bewährte VehicleHandling-Controller bleibt unverändert;
  Gravitation, Dämpfung und Radcollider werden bei Landung/Abbau zurückgesetzt.
- Auswahl eines Bodenmodus startet eine kontrollierte Landung mit anschließendem
  Ausklappen. F7 und Menü-Ausstieg warten darauf; letzterer nutzt wieder den
  regulären Ausstiegsablauf. F8/Tod/Weltwechsel haben weiterhin einen Abbaupfad.
- Nitro/Burnout und Reifengeräusche sind im Flug gesperrt. Menü/Fokusverlust
  neutralisieren Eingaben; Schwebestabilisierung bleibt aktiv. Jeder Wagen
  besitzt seinen eigenen Flugzustand; neue Fahrzeuge starten am Boden.
- Neue Zustands-/Controllerprüfungen mit kontrollierten Physikantworten.
  Tatsächliches Flugverhalten, Kollisionen, Partikel und Audio benötigen Unity.

Build, vollständiger Testsatz (49 Python-Tests und 546 C#-Prüfungen einschließlich
32 Flugprüfungen) sowie Radgeometrie-Vorschau geprüft. Keine Unity-Laufzeittests.

## 0.14.3 — Polierte Chrom-/Goldfelgen

Der Felgenrand war nur 28 % metallisch, der Stern 22 %. Eine zusätzliche
orange-goldene Farbe multiplizierte die bereits goldene Originaltextur.
Beide Flächen verwenden nun vollständig metallische Materialien mit hoher
Glätte; der Stern erhält nur eine leichte kühlere Korrektur der Originalfarbe.

Die Felgen verwenden ein frisches Standard-Material, sofern dessen Shader
verfügbar ist, sonst eine frische Instanz des vorhandenen Fahrzeugshaders.
Keine geerbten Prop-Texturen oder Eigenleuchtung; Reflexionen und Glanzlichter
sind aktiviert. Die Rad-Renderer nutzen Valheims vorhandene Reflection Probes.
Die Shaderauswahl steht im Log. Geometrie, Reifen und Fahrphysik bleiben gleich.
Build sowie 142 Modell-, Innenraum- und H2-/Garagenprüfungen bestanden;
Felgendetail zusätzlich in Blender gerendert. Die genaue Wirkung unter
Valheim-Beleuchtung benötigt einen Spieltest.

## 0.14.2 — Rotstich der Scheibentönung

Die bisherige Materialauswahl konnte `lavalanternglass_mat` bzw. dessen
Bruchvariante kopieren. Beide enthalten in den lokalen Spielassets eine rote
Eigenleuchtfarbe und das aktive Shader-Schlüsselwort `_EMISSION`. Die Tönung
änderte nur die Grundfarbe und ließ diese Eigenleuchtung erhalten.

Das Fenstermaterial verwendet jetzt ein frisches Material mit der geladenen
Shaderreferenz; fremde Texturen, Farben und Schlüsselwörter werden nicht mehr
kopiert. Eigenleuchtung wird zusätzlich explizit abgeschaltet. Tönungsfarben
sind neutral grau. Die gewählte Shaderreferenz wird zur Diagnose protokolliert.
Build und Innenraumprüfungen bestanden; Darstellung im Spiel noch prüfen.

## 0.14.1 — Fahrerhaltung, offenes Fenster und Scheibentönung

- Nutzer bestätigt Innenraum und Ein-/Ausstieg von 0.14.0; Kopf ragte durch
  das Dach, Hände saßen zu weit vorne und die Lenkanimation war umgekehrt.
- Sitzposition abgesenkt und etwas zurückversetzt; zusätzliche Begrenzung
  anhand des Kopfknochens mit Reserve für Kopf/Haar unter dem Dach.
- Rechte Hand mit zurückversetztem Handgelenk am Lenkrad, korrigierte
  Drehrichtung und Handorientierung. Linker Arm ruht am offenen Fenster;
  beim Öffnen der Tür bzw. Aussteigen wird er nach innen geführt.
- F9 → Türen: gespeicherte Scheibentönung 0–100 %. Die physisch linke
  Seitenscheibe wird beim Sitzen ausgeblendet und beim Aussteigen wieder sichtbar.
- Linke Tür nach dem Einstieg wieder manuell bedienbar: Menüknopf entsperrt,
  Sitzanimation überschreibt den Türzustand nicht mehr. Türbewegung ist im
  Menü möglich, sobald der Fahrer sitzt; Ein-/Ausstiege behalten ihre Sperre.
- Build und gezielte Fahrer-, Tür- und Fahrwerksprüfungen bestanden.
  Neue Pose, Scheibenwirkung und Ausrüstung benötigen erneuten Spieltest.

## 0.14.0 — Lokaler Innenraum und sichtbarer Fahrer

- Optionaler lokaler BeamNG-350Z-Innenraumimport: Cockpit, zwei Sitze, Pedale,
  Schalthebel und an den NFS-Türen befestigte Innenverkleidungen. Separate
  Lenkraddrehung, transparente Fenster mit verfügbarem Spielshader.
- F7 im Stand: Weg zur Fahrertür, Öffnen, Übergang in Valheims Sitzanimation,
  Hände am Lenkrad und Füße an den Pedalen, Schließen und Fahrfreigabe.
  Rückweg beim Aussteigen; freie Bodenfläche und Platz für den Spieler nötig.
- Eingaben/Nitro/Turret während des Übergangs gesperrt. Menü/Fokusverlust
  pausiert den Ablauf; Entfernen, Fehler und Weltwechsel stellen den Spieler wieder her.
- Native Sitzanimation plus eigene Übergänge/Gliedmaßenausrichtung, keine
  übernommene BeamNG-Charakteranimation. Visuelle Sitzskalierung auf 85 %.
  H2 und bestehende Fahrwerksabstimmung bleiben unverändert.
- Import, Modelldaten und Zustandsabläufe automatisiert geprüft; externe
  Modellvorschau geprüft. Unity-Spieltest einschließlich Kleidungsüberschneidungen offen.
- Originalautor des bereitgestellten Modland-Archivs unbekannt. Assets bleiben
  lokal in ignorierten Ordnern; keine Weiterveröffentlichungsfreigabe behauptet.

## 0.13.1 — Verschiebbares Fahrzeugmenü

F9-Menü per linker Maustaste an der Titelleiste verschieben. Die Position wird
gespeichert und bei Auflösungswechsel auf den sichtbaren Bildschirm begrenzt.
Schließen/Fokusverlust beendet das Ziehen. Schließen-Button und Inhaltsregler
bleiben unabhängig bedienbar. Die bestehende Fahrzeugbindung der Kategorien
bleibt erhalten: Ohne Fahrzeug ist nur die Garage aktiv.
Build und Menütests geprüft; tatsächliches Unity-Ziehen noch im Spiel prüfen.

## 0.13.0 — Tieferlegung, NFS-Menü und Parkautomatik

- F9 → Räder: zusätzlicher Tieferlegungsregler 0–12 cm mit 0,5-cm-Schritten,
  für H2/350Z getrennt gespeichert; physischer Versatz der Aufhängungspunkte.
  Serienhöhe beim Zurücksetzen, Feder-/Dämpfer- und Fahrmodusabstimmung erhalten.
- Vorderräder behalten beim Aussteigen ihren tatsächlichen Lenkwinkel.
  Wiedereinstieg setzt die geglättete Lenkung fort; Parkbremse bleibt aktiv.
- Neues F9-Layout mit Kategorienleiste, Original-NFSU2-Icons, ausgewählten
  deutschen Originaltiteln, Tastaturnavigation und drei Frontend-Sounds.
  Lokaler Import mit `tools/export_menu.py`, optionaler Medienordner `menu/`.
  Layout/Aktionen und Soundrollen sind angepasst; keine NFS-Menüemulation.
- Aktivierter 350Z-Turret bewacht nach dem Aussteigen automatisch: sichtbare
  Lasersuche, vorderer 180°-Sektor, 30 m Reichweite, feindliche Kreaturen,
  Laufnachführung, kontrollierte Schusslinie und automatisches Nachladen.
  Schalter unter Turret; unabhängig vom Motor-/Radioweiterlauf. Laserfarbe,
  Stärke und Munition werden übernommen. Spieler/zahme Tiere sind keine Ziele.
- Menüs, Fokusverlust und Spielertod sperren Automatik; nach manueller Übernahme
  bleibt gehaltenes RMB bis zum Loslassen gesperrt. Jeder Turret hat eigene
  Suche, Munition und Bewegung. H2-Turret weiterhin nicht eingerichtet.
- Geprüft mit Build, Import-/Payloadtests, Fahrwerks- und Turret-Zustandstests
  sowie kontrollierten Sichtlinien-API-Antworten. Kein Unity-Spieltest.

## 0.12.0 — Hummer H2, Spinner und Garage

Originaler H2 aus den eigenen NFSU2-Dateien: Serienkarosserie in Gelb,
26-Zoll-SPINNER_STYLE01 mit separatem Originaleinsatz, 34 cm breite Reifen
und für den schwereren SUV angepasste Federung, Bremsen und Anfahrkraft.
Die Einsätze beschleunigen verzögert und laufen nach dem Anhalten aus.
Felgengröße 22–28 Zoll; Reifenaußendurchmesser bleibt erhalten.
Originale Radpositionen aus dem HUMMER-Global-Datensatz, NFVU2-Fahrphysik.
Rückleuchten mit passender Serien-Textur und getrennten roten/weißen Flächen;
die zwei Atlasbereiche des hinteren HUMMER-Schriftzugs werden gemeinsam gespiegelt.

Eigener Serien-Sound aus GIN_Hummer.gin: acht Drehzahlbereiche, 1000–4782 U/min
und vier akustische Vorwärtsgänge. Der originale Stock-Datensatz verwendet
keinen zweiten Ginsu-Schubkanal; die Mod nimmt denselben Hummer-Sound leiser
beim Gaswegnehmen. Keine umbenannten oder tiefer gepitchten 350Z-Aufnahmen.

F9 → Garage ist auch ohne Auto erreichbar. Modellauswahl und Spawnknopf;
F6 spawnt die gespeicherte Auswahl. Bis zu vier unabhängige temporäre Autos,
Freiplatzsuche nebeneinander, kein Ersetzen des vorherigen Autos.
F7/F9 wählen zu Fuß das nächste Auto, F8 löscht nur dieses. Während der Fahrt
bleibt die Auswahl beim Fahrerauto. Spawnen erst nach dem Aussteigen.
Modell-/Ladefehler beim Spawnen erhalten die bereits vorhandenen Autos.

Pro Auto getrennte Physik, Meshes, Materialien, Audio, Radio, Nitro- und
Turret-Zustände; zentrale Kamera-/Spielersteuerung und Musikdämpfung.
Licht, Radio, Räder und Fahrmodus haben separate H2-Konfigurationseinträge,
vorhandene 350Z-Einträge bleiben gültig. Autos desselben Modells teilen
Einstellungen. Kamera/Tacho bleiben gemeinsam. H2-Kamera höher und weiter weg.

Beim H2 zunächst kein Motorraum-/Kofferraumausbau, keine Türanimationen,
kein Nitro/Turret. Radio, Licht, Neon, Burnout und Aufprallsystem werden
wiederverwendet. Kein Speichern der Fahrzeuge, weiterhin lokaler Einzelspieler.
Automatische Prüfungen und Blender-Vorschauen ersetzen keinen Unity-Spieltest.

## 0.11.0 — Kräftiger, einstellbarer Laser und bewegte Subwoofer

Breiterer Laser mit hellem Kern und größerem Treffpunkt. Unter F9 → Turret →
Laser einstellen: 25–400 Prozent Stärke (Standard 200), sieben Farbvorlagen
und RGB-Regler. Farbe und Stärke werden gespeichert und gelten für Strahl
und Punkt; Hindernisse stoppen den Laser weiterhin.

Das Radio analysiert den Bassanteil der tatsächlich wiedergegebenen Samples
ohne sie zu verändern. Getrennte Kanalanalyse vermeidet Auslöschung bei
gegenphasigem Stereo. Die Basshüllkurve steuert eine sichtbare Membranschwingung
bis 14 mm Hub in beide Richtungen. Gehäuse und Außenrand bleiben fest.
Jeder Lautsprecherplatz besitzt eigene animierte Meshkopien; Verstärker und
LCDs bewegen sich nicht. Gilt auch für das Radio nach dem Aussteigen.
Pause, ausgeschaltetes Radio, Lautstärke null und ausbleibende Audiodaten
lassen die Bewegung ausklingen; Titelwechsel setzt die Auslenkung zurück.
Die Bewegung ist eine Bassvisualisierung, keine samplegenaue Lautsprechersimulation.

## 0.10.0 — Freie Zielsicht, Rust-Laser und Explosivmunition

Im Turret-Modus höherer Kameradrehpunkt, bei steilem Blick nach unten zusätzlich
angehoben. Visier bei 62 Prozent der Bildhöhe über der unteren Bildschirmkante;
der Lauf richtet sich auf den Weltpunkt dahinter aus. Orange zeigt Nachlauf,
Neigegrenze oder blockierenden Fahrzeugrumpf. Normale Fahrkamera bei abgeschaltetem
Turret unverändert. Munitionsart und Vorrat links unten.

Originales Rust-Lasersight-Entity-Modell samt beiden Materialbereichen und
Emitterpunkt lokal importiert und unter der AK montiert. Roter Strahl und
Treffpunkt folgen dem Lauf und enden an Hindernissen; im Menü abschaltbar.
F9 → Turret: Normal/Explosiv. Explosiv ergänzt den direkten Treffer um kleinen
Flächenschaden gegen Gegner mit Entfernungsabfall und Deckungsprüfung sowie
einen gepoolten Partikel-/Lichtblitz und Rusts `explosive-ammo-close`-Sound.

Jeder direkte Bodentreffer mit Explosivmunition senkt eine runde Fläche von
1,4 m Radius im Zentrum um bis zu 18 cm ab, entfernt Gras und speichert über
Valheims Terrain-Compiler. Wiederholte Treffer graben weiter bis zum normalen
Grablimit. Treffer auf Felsen, Gebäude oder Kreaturen graben nicht darunter.
Krater bleiben in der Welt, auch nachdem das temporäre Auto entfernt wurde.
Modelle/Samples original; Laserstrahl, Partikel, Wirkung und Krater sind NFVU2.
Zusätzliche Tests für Terrain-Treffer, Kachelgrenzen, Besitzer und Wiederholung.

## 0.9.1 — Zielmarkierung und Dachmontage korrigiert

Das Geschütz folgt der Kamerablickrichtung vom Lauf aus. Der bisherige
Bildschirmmittelpunkt zielte bei der Orbitkamera auf den Bereich des Autos.
Ein einziges, kontrastreiches Fadenkreuz zeigt nun den projizierten tatsächlichen
Auftreffpunkt; Nachlauf und Neigegrenzen bleiben sichtbar. Munition links unten.
Valheims eigenes Mittelkreuz und Bogenvisier werden während der Turret-Steuerung
ausgeblendet; beim Ausstieg, Abschalten und Entfernen wiederhergestellt.
Turret um 20 cm nach hinten und 7,5 cm tiefer versetzt, damit die Stützen in
der gekrümmten Dachfläche sitzen. Geometrievorschau aktualisiert.

## 0.9.0 — Rust-AutoTurret mit AK auf dem Dach

Neuer F9-Reiter „Turret“, standardmäßig ausgeschaltet, mit gespeichertem
Ein-/Ausschalter und separater Lautstärke. Lokaler Import der originalen
AutoTurret-/AK-Weltmodelle, fünf Diffustexturen und sieben Rust-Sounds.
Originale Dreh-/Neigepunkte, AK aufrecht mit nach vorne gerichtetem Lauf;
auf die Dachfläche skaliert. Das Abdeckblech der leeren Waffenaufnahme entfällt.

Mausblick steuert Drehung und Neigung mit begrenztem Nachlauf. Zielmarkierung
zeigt den tatsächlichen Auftreffpunkt. Rechte Maustaste hält Dauerfeuer:
30 Schuss, 2,5 Sekunden Nachladen, unbegrenzte Reservemunition. Raycast bis
150 m, 35 Stichschaden vor Valheims Widerständen an gegnerischen Kreaturen.
Felsen, Gebäude und eigene Karosserie blockieren. Kein Beschuss von Spielern,
gezähmten Tieren oder Bauwerken. Schussvarianten, Außennachhall, Servoloop,
Aktivierungs- und Nachladegeräusche werden räumlich abgespielt.

Eigene Rückstoßanimation, Mündungsfeuer, Lichtblitz und kurze Leuchtspur.
Keine Übernahme von Rust-Skripten oder Charakteranimationen. Schießen nur
am Fahrersitz; nach Fokusverlust, Menü oder Ausstieg ist Loslassen erforderlich.
Fehlende/fehlerhafte optionale Assets deaktivieren nur den Turret. Fahrwerk,
Nitro und bestehende Bedienung bleiben erhalten. Build und automatisierte
Prüfungen sowie Blender-Geometrievorschau; Unity-Spieltest noch offen.

## 0.8.0 — NFS-Tachos, dynamische Kamera und Driftsetup

Elf Original-Designs aus HUD_CustomTextures_00 bis 10 mit 7500-RPM-Skala und
Zeigern sowie NOS-Grafiken aus InGameRace.bun. Der lokale Import erhält Alpha
und entfernt ungenutzte Texturränder. Anzeige rechts unten: Geschwindigkeit,
akustische Drehzahl/Gang, Nitrotank und Boostfarbe. Menü → Anzeige: Design,
Größe, Farbe, KM/H/MPH und Sichtbarkeit; ohne Assets einfacher Text-Fallback.
Layout, Zeigersweep und NOS-Füllanimation sind eigene Valheim-Anpassungen.

Kamera folgt einem geglätteten Weltwinkel statt starr dem Fahrzeugwinkel.
Begrenzter Anteil der Bewegungsrichtung und vorausschauender Zielpunkt für
Kurven/Drifts; Rückwärtsfahrt dreht die Kamera nicht um. Mausblick pausiert
das automatische Nachziehen für 2,2 Sekunden; im Stand keine Rückzentrierung.
Im Anzeigemenü abschaltbar und in der Stärke einstellbar. Hindernisprüfung bleibt.

Optionaler Driftmodus zusätzlich zu Gelände/Straße: Heckantrieb, weniger
Seitengrip hinten, erweiterter Lenkwinkel, schnelleres Gegenlenken, 20 Prozent
mehr Drehmoment gegenüber Straße und weniger Traktionsregelung bei Schlupf.
Leertaste als Hinterrad-Handbremse; Menü/Exit/Fokusverlust bremsen alle Räder.
Originaler Reifensound und Rauch bei Schlupf, W+S-Burnout weiterhin möglich.
Driftparameter liegen separat; Umschalten stellt die bisherigen Grundwerte her.
Prüfungen und externe HUD-Vorschau ersetzen keinen Fahr-/Kameratest in Unity.

## 0.7.2 — Außenmusik bei offenen Türen und Heckklappe

Die tatsächlichen Animationsstände steuern Lautstärke, Tiefpass und Position
der räumlichen Radioquelle. Geschlossen bleibt die bisherige Dämpfung erhalten
(30 Prozent, 1400 Hz); eine offene Tür lässt mehr Musik heraus, die vollständig
offene Heckklappe erreicht 85 Prozent und 12 kHz. Werte sind Mod-Abstimmungen.
Die Quelle wandert zur geöffneten Fahrzeugseite oder zum Kofferraum; mehrere
Öffnungen ergeben eine gewichtete Position mit begrenztem Gesamtpegel.
Der bestehende Stream läuft durch, ohne neue Wiedergabe oder doppelte Songs.
Innenwiedergabe, Benutzerlautstärke, Pause, Fokus-/Menüsperren und der
Weiterlaufen-Schalter bleiben erhalten. Die Motorhaube ist ausgenommen.
Automatisierte Mischungsprüfungen ersetzen keinen Hörtest in Valheim.

## 0.7.1 — Fehlende obere Rückleuchten-Einsätze

Der Import hat vom geteilten STYLE04-Leuchtenmaterial nur die unteren
orangefarbenen Streifen übernommen. Die sechs oberen Dreiecke je Seite
fehlten; dadurch war Kofferraum-Neon durch die Rückleuchten sichtbar.
Beide oberen Einsätze werden jetzt mit Originalgeometrie und Originaltextur
übernommen. Die separat modellierten klaren Außenabdeckungen bleiben offen.
Neonposition, Lichtsteuerung und Fahrverhalten bleiben unverändert.
Regression prüft den vollständigen, duplikatfreien Erhalt der Dreiecke.
Blender-Vorschau geprüft; die Darstellung in Valheim noch bestätigen.

## 0.7.0 — Kofferraumausbauten, Türanimationen und Motorraum

F9 → Ausbau: Serie und drei originale 350Z-Gehäuse mit 2/4/6 Plätzen.
Je Platz Abdeckung, vier Subwooferdesigns, vier Verstärker und vier LCDs.
Originale AUDIO-Geometrie, AUDIO_SKIN-Textur und MARKER_AUDIO_COMP-Matrizen;
Komponentengröße nach Geometriepassform gewählt. Standard: KIT02 mit vier
SPEAKER1-Subwoofern. Dies ist eine Mod-Voreinstellung, kein nachgewiesener
Bestandteil von Rachels Preset. Separates Kofferraum-Neon mit sieben Farben,
Ein-/Ausschalter, statischer oder pulsierender Darstellung und Zurücksetzen.
Leuchtstreifen und Pulsverlauf sind eigene Mod-Effekte; LCDs bleiben statisch.

F9 → Türen: linke/rechte Tür, Haube und Heckklappe öffnen im Stand animiert.
Normale/Suicide-/Scherentüren verwenden Original-Quaternionen, Scharnierpunkte
und bei Scherentüren auch die originale seitliche Translation. Wiedergabe auf
eine Sekunde normiert; Türmechanik wechselt erst nach dem Schließen.
Spiegel, Türverkleidungen, Haubenunterseiten und Spoiler folgen den Gruppen.
Anfahren/Einsteigen schließt die Teile automatisch. Keine neuen Türcollider;
Antrieb und WheelCollider bleiben unverändert.

Drei Original-Motormodelle samt UNLIT-Innenflächen sind unter der Haube
wählbar. Das Basismodell enthält nur einfache Innenraumgeometrie; ein
detailliertes Cockpit wurde nicht gefunden. Scheiben bleiben undurchsichtig.
Neue Metadaten sind optional für ältere Modelle; importierte Audiomeshes
werden beim Teilewechsel wiederverwendet. Build und 322 automatisierte Prüfungen
sowie Blender-Sichtprüfungen; Unity-Darstellung/Bedienung noch im Spiel prüfen.

## 0.6.0 — Überfahren und laufendes Fahrzeug nach dem Aussteigen

Ab 2,5 m/s Kontaktgeschwindigkeit verursachen Fahrzeugtreffer nativen Valheim-
Schaden und Rückstoß an feindlichen, lebenden Kreaturen. Spieler und gezähmte
Tiere bleiben ausgenommen. Kleine Buchen/Tannen und vier Buschvarianten nutzen
nativen Hackschaden mit normalen Zerstörungseffekten und Beute. Große Bäume,
Felsen, Bauwerke und gepflanzte Setzlinge sind ausgenommen. Kurze Vorschau des
Fahrzeugvolumens plus Kollisionskontakte; pro Ziel 0,8 Sekunden Trefferschutz.
Schaden und Rückstoß sind begrenzt; hohe Gesundheit/Rüstung schützt weiterhin.
Fahrwerk und Antrieb bleiben unverändert. Der Build referenziert nun zusätzlich
die lokal installierte assembly_valheim.dll, ohne sie mitzuliefern.

F9 → Fahrzeug: „Beim Aussteigen weiterlaufen lassen“ ist standardmäßig an
und wird gespeichert. Nach mindestens einem Einstieg bleiben Motor im Leerlauf,
eingeschaltetes Neon und Radio aktiv. Der Schalter wirkt auch neben dem Auto;
Radio-Pause und eigener Neon-/Radioschalter bleiben wirksam. Draußen wird das
Radio räumlich, auf 30 Prozent gedämpft und auf 1,4 kHz tiefpassgefiltert;
es blendet bis 28 Meter aus. Valheim-Musik wird nur beim Fahren unterdrückt.
Motor draußen auf 55 Prozent und 30 Meter Reichweite begrenzt.
Fokus-/UI-Pause und Entfernen räumen weiterhin alle Quellen auf.

Build und 297 automatisierte Prüfungen einschließlich 41 neuer Treffer-Adaptertests
bestanden. Unity-Physik, Spiel-Schadensverarbeitung und Hörwirkung sind noch
im Spiel zu bestätigen.

## 0.5.0 — Kräftigeres Anfahren, Rückwärts-Sound und Burnout

Vorwärts-Anfahrmoment um 20 Prozent bis 3 m/s erhöht, linear bis 6 m/s auf
das bisherige Niveau reduziert. Die erste akustische Übersetzung steigt
von 3,79 auf 4,15; übrige Gänge und Höchstgeschwindigkeiten bleiben erhalten.
Das Rückwärts-Getriebeheulen ist eine neue synthetische Schleife mit
geschwindigkeitsabhängiger Tonhöhe/Lautstärke. Kein eindeutig zugeordneter
Original-Rückwärts-Sample wurde in der Dateiinventur und Ereignissuche gefunden.
Rückfahrlicht: Intensität 1,6 → 0,45, Reichweite 6 → 4 m, Emission 2 → 0,7.

W+S startet unter 0,65 m/s mit beiden Hinterrädern und mindestens einem
Vorderrad am Boden einen Burnout. Die Frontachse bremst, die Hinterachse
erhält Drehmoment und vorübergehend reduzierten Längsgrip. Über 2 m/s,
bei Kontaktverlust, Loslassen, Leertaste, Menü oder Ausstieg wird beendet.
Normale Reibungswerte werden wiederhergestellt; Nitro ist während W+S gesperrt.
Motor hält im Burnout den ersten Gang mit hoher Last. Originaler SKID_PAV-Sound
und SFX_SMOKE_B_CS-Rauchatlas reagieren auf tatsächlich drehende Hinterräder.
Rauchposition folgt auch dem visuellen Radversatz. Partikel werden begrenzt
und bei Sperre/Entfernen gelöscht. Effekte, Audio und Steuerung bleiben getrennt.

256 automatisierte Prüfungen bestanden; tatsächliche WheelCollider-Physik,
Rauchdarstellung und Hörwirkung benötigen den Spieltest.

## 0.4.1 — Menü links oben und einstellbare Frontscheinwerfer

Das Menü sitzt mit 16 Pixel Abstand links oben und skaliert bei kleinen
Fenstern so, dass unten Platz für die Fahrhinweise bleibt.
F9 → Licht bietet 14 Farbvorlagen, einen Stärke-Regler von 0 bis 300 Prozent
und Zurücksetzen auf Serie/100 Prozent. Auswahl und Stärke werden gespeichert.
Lichtkegel und emissive Frontleuchten erhalten gemeinsam Farbe und Stärke;
Rück-, Brems-, Rückfahr- und Kennzeichenlicht behalten ihr bisheriges Verhalten.

Die Vorlagennamen stammen aus den lokalen Originaldateien
LANGUAGES/English.bin, German.bin und Labels.bin. RGB-Werte und der Faktor
1,45 der extrahellen Vorlagen sind NFVU2-Anpassungen für Unity.
Build und 232 bestehende automatisierte Prüfungen bestanden.
Menüdarstellung und tatsächliche Lichtwirkung sind noch im Spiel zu bestätigen.

## 0.4.0 — Sturz, Radversatz und Felgengröße im Menü

F9 → Räder bietet getrennten Sturz (−15 bis +10 Grad) und seitlichen Versatz
(−10 bis +20 cm je Rad) für Vorder-/Hinterachse. Ein gemeinsamer Regler ändert
die nominale Felgengröße von 16 bis 22 Zoll relativ zum NFS-20-Zoll-Ausgangsmodell.
Die Reifenflanke wird angepasst, der 70-cm-Außendurchmesser bleibt erhalten.
Änderungen erscheinen sofort und bleiben in der Konfiguration gespeichert;
ein Zurücksetzen-Knopf stellt alle fünf Ausgangswerte wieder her.

Sturz liegt vor der Radrotation, damit geneigte Räder beim Rollen nicht taumeln.
Die Bremsen folgen Sturz/Versatz; Sättel bleiben von der Raddrehung getrennt.
Felgenänderungen verwenden unveränderte Ausgangsvertices statt kumulativer
Skalierung und passen die Normalen an. Der Versatz wird je Seite gespiegelt.
Die Funktion ist optisch: Radstand, Kontaktgeometrie und VehicleHandling bleiben
unverändert. Spieltest von Bedienung, Extremwerten und Beleuchtung noch offen.
232 automatisierte Prüfungen bestanden; externe 16-/22-Zoll-Vorschauen geprüft.

## 0.3.1 — Heckschriftzug richtig ausgerichtet

Die UV-Zuordnung des hinteren 350Z-Schriftzugs ist horizontal korrigiert,
damit er von hinten lesbar ist. Nur seine beiden Dreiecke sind betroffen;
der gemeinsame Originalatlas und die übrigen Embleme bleiben erhalten.
Nahaufnahme vor/nach dem Export geprüft; Spieltest noch offen.

## 0.3.0 — Nitro, Purge und zweifarbige Felgen

Linke Maustaste halten: Vorwärtsfahrt mit Gas und Bodenkontakt gibt Zusatzschub
und grüne Flammen aus den vier vermessenen Endrohren; im Stand entstehen helle
Purge-Strahlen an Stoßstange und Haube. Originale SFX_FIREB-/SFX_SMOKE_B_CS-Atlanten
und zwei Schleifen aus Nitrous_00_MB.abk werden verwendet. Die Loop-Rollenzuordnung,
Purge-Düsenpositionen und Unity-Partikelanimation sind dokumentierte Annäherungen.
Sechs Sekunden Tank, zwei Sekunden Nachladeverzögerung, zehn Sekunden Ladung
während der Fahrt. Zusatzschub liegt getrennt vom bestätigten VehicleHandling.
Menü, Fokusverlust und Ausstieg stoppen Effekte; Menüklicks lösen keinen Boost aus.

F9 enthält Nitro-Einstellungen und optionale, zunächst ausgeschaltete Flammen
bei Gaswegnahme nach hoher Last. NFS kennt Sputter-/Blow-off-Effekte; genaue
Tuningbedingungen und der richtige Knall-Substream bleiben offen. Deshalb wird
kein unbestätigter Backfire-Sound als Original ausgegeben oder installiert.

Die Originalfelge wird ohne Geometrieverlust in Chromring und Goldstern getrennt.
Angepasste Materialwerte verhindern die bisher fast schwarze Metallfläche.
Der Texturparser überspringt keine echten führenden 0x11-Pixelbytes mehr.
Externe Felgenvorschau und 222 automatisierte Prüfungen bestanden;
Unity-Fahr-, Partikel- und Hörtest noch offen.

## 0.2.1 — Heckleuchten, breitere Reifen, Sportbremsen und Songtitel

Der Nutzer bestätigt die grundsätzliche Funktion von 0.2.0 in Valheim.
Fehlende orange Streifen unter den STYLE04-Rückleuchten und die Einsätze der
Heckstoßstange werden wieder importiert. Die inneren Stoßstangenflächen leuchten
bei eingelegter Rückwärtsrichtung weiß und beleuchten den Boden dahinter.
S beim Vorwärtsfahren bleibt zunächst Bremsen; Aussteigen beendet Rückfahrlicht.
Die Aufteilung der Stoßstangenflächen ist eine dokumentierte Annäherung.
Orange Streifen sind passiv; eine Blinkersteuerung ist noch nicht implementiert.

Reifenbreite von etwa 19,5 auf 28 cm erhöht; 70-cm-Durchmesser, äußere Position
und Fahrwerk beibehalten. Originale STYLE03-Sportbremse mit roten Sätteln und
38-cm-Scheibe. Die Rotorfläche wurde wegen der ursprünglich quadratischen
Alpha-Maske rund nachgebildet; ihr Originalatlas bleibt erhalten.
Scheiben drehen mit dem Rad, Bremssättel folgen nur der Lenkung.

Das Radio zeigt 27 echte Künstler-/Songnamen aus der EA-Trax-Tabelle.
MPF-Ereignisse, originale EXE-Strings und WAV-Prüfsummen sichern die Zuordnung.
16 weitere Containerstreams bleiben lokal, erscheinen aber nicht mehr als Songs
im Radio. Build/Paket/Installer übernehmen nur die aktive Playlist.
Die Änderungen wurden extern gerendert und mit 176 automatisierten Prüfungen
getestet. Der Nutzer bestätigt anschließend auch die Funktion in Valheim.

## 0.2.0 — Fahrzeugmenü, Radio und Beleuchtung

F9 öffnet ein skalierbares Fahrzeugmenü für Fahrmodus, Radio, Licht und Neon.
Eingabe-/Kamerasperre und Audio-Pause sind getrennt; das Radio bleibt beim
Bedienen hörbar. Türen/Kofferraum sind ausdrücklich noch deaktivierte Vorbereitung.
Lokale Stereo-Musik wird bedarfsgerecht geladen, mit Titelauswahl, Vor/Zurück,
Pause und Lautstärke. Originalmusik kommt aus SDAT.VIV; die 43 importierten
Musikstreams haben bis zur gesicherten Zuordnung neutrale Titelnummern.

Front-, Rück- und Kennzeichenlicht schalten beim Einstieg ein und beim Ausstieg
aus. Bremslicht reagiert zusätzlich auf Bremsen. Die originale LICENSE_PLATE-
Textur liegt auf eigener Rechteckgeometrie; ihre Platzierung ist angenähert.
Originale 350Z_NEON-Maske mit bodenangepasster Projektion, Farbwahl und eigener
Pulsanimation. Shaderwirkung und Lichtstärken sind noch in Valheim zu prüfen.

Die akustischen Übersetzungen schalten im Gelände früher; Schaltunterbrechungen
betragen 140/120 ms. Radschlupf erhöht den Ton, ohne die Gangauswahl hochzuzählen.
Originale TURBO_MED_2-Aufnahmen ergänzen Aufladung und Blow-off; Zuordnung und
Auslösung sind eine dokumentierte Annäherung. VehicleHandling bleibt getrennt.
Installer/Paket unterstützen die externen Medien, Dateisicherungen, SHA-256 und
Rücksicherung bei Kopierfehlern. Der Nutzer hat die grundsätzliche Funktion
anschließend in Valheim bestätigt; Nachbesserungen sind in 0.2.1 beschrieben.

## 0.1.9 — Textur-Upload beim Spawnen

Der Spieltest von 0.1.8 scheiterte mit `LoadRawTextureData: not enough data provided`.
Die Texturen besitzen eine Mipmap-Kette, die Modelldatei enthält jedoch nur Stufe 0.
`SetPixelData<byte>(pixels, 0)` lädt jetzt gezielt diese Stufe; `Apply(true, true)`
erzeugt anschließend die kleineren Stufen. Der Nutzer hat die Darstellung
anschließend in Valheim bestätigt.

## 0.1.8 — Tatsächlichen Reifendurchmesser korrigiert

Der Import verwendete bisher die Diagonale einer quadratischen Felgen-Rückfläche
als Radius. Damit waren die sichtbaren Reifen von 0.1.7 nur etwa 52 cm groß,
obwohl 72 cm angegeben wurden. Gemessen wird jetzt ausschließlich die originale
Reifenoberfläche: vorne und hinten tatsächlich 70 cm Durchmesser, etwa 35 %
größer als zuvor. Die überstehenden Rückflächen-Dreiecke werden ausgelassen.
Zwei Regressionstests prüfen den Quellradius und den exportierten Reifen.
Die Vorschau enthält zusätzlich eine Seitenansicht und eine tiefe perspektivische
Ansicht für den Vergleich mit dem Referenzbild. Der Unity-Spieltest steht aus.

## 0.1.7 — Radproportionen und Einbauposition

Radskalierung erhöht (Durchmesserangabe später in 0.1.8 korrigiert), Räder weiter nach außen
gesetzt und längs in den Widebody-Radhäusern zentriert. Rad- und Bremsdarstellung
folgen gemeinsam dem Fahrwerk; ihre Versätze rotieren nicht mit den Rädern.
Die Karosseriedarstellung sitzt 4 cm höher, um den größeren Rädern Platz zu geben.
Collider, Antrieb und Federungsparameter bleiben auf dem bisherigen Stand.
Die externe Vorschau zeigt eine angenäherte eingefederte Stellung anstelle der
Collider-Aufhängungspunkte. Freigang bei Lenkeinschlag und Einfedern muss noch
in Valheim geprüft werden.

## 0.1.6 — Rachels Originalteile und Vinyl

Die ursprüngliche Voreinstellung `DDAY_PLAYER_CAR` aus `GlobalB.lzc` bestimmt
Widebody 02, Carbonhaube 07, Spoiler 02, Leuchten, Racing-Hart-Räder und
Endrohre. Originale UVs, Normalen, Materialbereiche, Texturen und das
`350Z_ART_F_6`-Vinyl einschließlich Maskenpalette werden lokal aufbereitet.
Räder folgen den bisherigen WheelCollidern; Bremssättel drehen nicht mit.
Fahrphysik und Sound bleiben unverändert. Modell und Texturen sind weiterhin
ignorierte lokale Daten. Build, 83 Mod-Prüfungen und drei Asset-Prüfungen bestanden; externe Vorschau
gerendert. Der Unity-Spieltest steht aus. Lack/Felgentönung sind angenähert;
Scheibentransparenz, Leuchtenabdeckungen und Kennzeichen bleiben offen.

## 0.1.5 — Fahrzeug-Sound

Originale NFSU2-Aufnahmen für Beschleunigen und Gaswegnehmen, jeweils acht
Drehzahlbereiche, eigene Drehzahl-/Schaltlogik, Roll- und Rutschgeräusche und
synthetisierte Hupe. Geräusche sind räumlich am Fahrzeug angebunden.
Der Nutzer bestätigt die grundsätzliche Funktion; Klang-Feinschliff ist offen.

## 0.1.4 — Anfahren wiederhergestellt

Motorleistung unabhängig von Radkontaktmeldungen, Physik bei Eingaben aktivieren
und Bremse beim Gasgeben vollständig lösen. Vom Nutzer bestätigt: Auto fährt
wieder, Geländemodus funktioniert sehr gut.

## 0.1.3 — Kamera und Gelände

Mauskamera mit Zoom und Hindernisabstand; umschaltbarer Gelände-/Straßenmodus.
Kamera und Moduswechsel bestätigt; Anfahrproblem anschließend in 0.1.4 korrigiert.

## 0.1.2 — Darstellung im Spiel

Vorhandenes Valheim-Material mit geladenem Shader verwenden. Spawnen, Fahren
und Ein-/Aussteigen erstmals vom Nutzer bestätigt.

## 0.1.1 — Modell laden

Newtonsoft.Json für die Modelldaten statt des ungeeigneten Unity-JSON-Laders.

## 0.1.0 — Erster Prototyp

Serienkarosserie aus NFSU2 extrahiert: 16 Teile, 3.867 Vertices, 3.449 Dreiecke.

Die Git-Historie beginnt mit dem Projektimport auf Basis von 0.1.5. Die früheren
Versionen sind hier dokumentiert; frühere lokale Sicherungen sind keine Git-Tags.
