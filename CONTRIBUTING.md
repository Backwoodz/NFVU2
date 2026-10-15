# Contributing to NFVU2

This repository is the source of truth for development. Read [AGENTS.md](AGENTS.md),
[ROADMAP](docs/ROADMAP.md) and [CHANGELOG](CHANGELOG.md) before changing behaviour.
Player installation belongs in the [README](README.md).

## Prerequisites

- Windows, Python 3.10+ and a .NET SDK 7+.
- A local Valheim installation with BepInEx. The current development baseline is
  Valheim 1.0.17 / Unity 6000.0.75f1 and BepInExPack Valheim 5.4.2351.
- Your own NFS Underground 2 disc images in the tested MODE1/2352 BIN format.
- NumPy/Pillow from `requirements.txt`, and a local
  [vgmstream decoder](https://github.com/vgmstream/vgmstream/releases/tag/r2117).
- For the Windows setup build: 64-bit Windows with .NET Framework 4.8 and
  `curl.exe`. The .NET SDK is only a developer requirement.

Copy `config.example.json` to ignored `config.local.json` and set your paths.
Install Python dependencies with `python -m pip install -r requirements.txt`.
Game, Unity and BepInEx references are read from your installation; no game
libraries are checked in and the build does not need a NuGet restore.

## Generate local assets

Run these commands from the repository root after configuring your disc images
and decoder:

```powershell
python tools/extract_assets.py
python tools/export_geometry.py
python tools/export_rachel.py
python tools/extract_audio.py
python tools/decode_audio.py
python tools/prepare_audio.py
```

This supplies the required model and 19 embedded vehicle WAV resources.
Optional HUD, menu, effects, Hummer, turbo, radio, interior and turret import
commands and source details are in [ASSET_PIPELINE](docs/ASSET_PIPELINE.md).
The stock 350Z is the base; imported tuning pieces do not establish a complete
reconstruction of Rachel's appearance or driving behaviour.

## Build and test

For a smaller player download, prepare Ogg Vorbis copies of the radio after
importing its WAV masters. Use a local [FFmpeg](https://ffmpeg.org/download.html)
build with [libvorbis](https://ffmpeg.org/ffmpeg-codecs.html#libvorbis):

```powershell
python tools/prepare_radio.py --ffmpeg path/to/ffmpeg.exe
```

The default is a 192 kbit/s target, keeping the source sample rate and stereo.
Every song is decoded again to check completeness and duration. Masters remain
in `.local/radio/`; compressed copies go to `.local/radio-ogg/`. The build uses
the verified Ogg playlist automatically, retaining song titles and order.
If the masters or playlist change, rerun preparation; stale compressed copies
are rejected. Without a compressed folder, the original WAV build still works.

For the local test setup, FFmpeg can also be obtained from the pinned
[imageio-ffmpeg wheel](https://pypi.org/project/imageio-ffmpeg/0.6.0/):

```powershell
python -m pip install --target .local/tools/audio-encoder imageio-ffmpeg==0.6.0
python tools/prepare_radio.py
```

The encoder is a developer tool and is not included in the player installer.
Listen to the compressed music in Valheim before publishing; automated decoding
does not establish perceptual quality or Unity streaming behavior.

```powershell
python tools/build.py
python tools/test.py --skip-build
```

The full test suite expects locally imported optional assets. Without game
assets you can run the handling checks with `python tools/test.py --handling-only`.
To run just the wizard's filesystem tests:

```powershell
python -m unittest discover -s tests -p test_installer_wizard.py
```

Tests run logic and controlled doubles outside Unity. They do not verify driving
feel, controller hardware, graphics, audio playback or compatibility with a new
Valheim release. Record those checks separately in the roadmap.

`src/NFVU2.csproj` supports editors; set `ValheimDir` or `NFVU2_VALHEIM` locally.
The supported build entry point remains `tools/build.py`.

## Build the player installer

The project skills separate local builds from GitHub publication:
`/installer` (or `$installer`) builds and verifies a local setup, then removes
older local installers after success. `/release` (or `$release`) prepares a new
version, builds and tests it, commits and pushes the release changes, creates
a version tag and publishes a GitHub Release with the installer and checksum.
Use `/release` only when you intend to publish; creating or editing the skill
does not publish anything. A normal push does not invoke either skill.

After building and testing the mod:

```powershell
python tools/build_installer.py
```

The result is `artifacts/installer/NFVU2-Setup-<version>.exe` and its SHA-256
sidecar. The first build downloads the pinned
[BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
archive into `.local/installer/` and checks its SHA-256 before use. Subsequent
builds reuse it. The player EXE works offline and embeds the local payload;
end users do not install Python or an SDK. Radio music is always packaged and
installed; a player installer build fails if music is missing from the payload.
Run `python tools/extract_music.py` and rebuild the mod to supply it.
The installer embeds the same user-provided logo as the README from
`docs/images/nfvu2-logo.png`. Its `LICENSE` window has a Close button and supports
Escape and Enter.
Mod and installer share one Major.Minor.Patch version, currently 0.30.0.
Use minor versions for features and patch versions for fixes, including installer fixes.
There is no separate setup revision. Update both `src/NFVU2.csproj` and the
`BepInPlugin` attribute together; the builder checks that they match. Existing
installer files are never overwritten; use the next project version for a changed build.
The installer asynchronously checks GitHub's latest stable public release and
provides a Changelog window (release notes for an available update, packaged notes offline).
It compares Major.Minor.Patch numerically, verifies downloaded EXEs with the release's
SHA-256 sidecar and available GitHub digests, then starts the new setup with the selected
Valheim path. The new setup performs the installation with its own normal guards.
Publish a matching `NFVU2-Setup-<version>.exe` and `.sha256` pair under the
matching `v<version>` release tag. Legacy revision filenames remain readable for
compatibility, but revisions never affect update ordering. Existing public r6 EXEs
require a one-time manual download of 0.29.0 or later to gain update detection.
A local build does not change public downloads.

The builder uses an explicit payload allowlist. It never packages arbitrary
files from the installed game, private plugins, local config, source archives
or everything under `artifacts/`. Changing a pinned dependency requires
checking its source and updating its digest deliberately.

The EXE is unsigned. Creating it does not publish a GitHub Release or change
repository visibility. Release uploads should contain the player EXE and its
checksum, with version-specific notes and results of the manual checks below.
Create a version tag only for an actual release, pointing to the commit used
to build its downloadable installer. Development snapshots and local builds
do not receive tags; the former prototype tag `v0.1.5` was retired.
Generated packages contain the locally imported assets; their provenance is
documented in [REFERENCES](docs/REFERENCES.md) and the asset pipeline notes.

`python tools/package.py` still creates a manual-install ZIP for an existing
BepInEx installation. Its filename follows the current project version.

## Installer behaviour and validation

- The wizard selects `valheim.exe` and checks `valheim_Data`. It refuses running
  Valheim, concurrent wizard operations, unsafe paths and linked directories.
- Fresh installs receive the Windows files from the pinned BepInEx pack.
  Existing complete BepInEx 5 installations are kept; partial/foreign loaders
  produce an error before replacement. External mod-manager profiles are not
  managed by this wizard; it targets the selected game folder directly.
- Owned mod files and hashes are recorded in `<Valheim>/.nfvu2/install.json`.
  Legacy manifests from `tools/install.py` are supported. Untracked manual
  copies should first be installed with the wizard to establish ownership.
- Each operation snapshots affected files, verifies copies and writes a
  recovery journal before changing installed files. The next operation recovers
  an interrupted transaction. Keep Valheim closed during this process.
- Backups live in `<Valheim>/.nfvu2/backups/`. After success, only the two newest
  timestamped snapshots remain. Named backups and linked contents are preserved.
- Uninstall removes tracked mod files and retains settings/saves. Modified mod
  files block removal until the user saves their changes and repairs the install.
  Optional loader removal only applies to a loader installed by this wizard,
  with unchanged binaries and no other plugin/patcher files. Modified BepInEx
  settings, runtime logs/cache, empty directories and backups may remain.
- Removing the shared loader is opt-in. A previously installed BepInEx is never
  owned or removed by this wizard. Windows access errors offer a manual
  “Run as administrator” retry; no automatic elevation is used.

Before a public binary release, check the actual wizard on a clean Windows
Valheim install and an existing modded install: Browse, install, game launch,
spawn, update, uninstall, optional loader removal, permissions and DPI scaling.
Automated tests use disposable fake game folders and never the installed game.

The developer-only `python tools/install.py` also backs up and verifies copies.
Use the wizard for installations it owns; do not mix installer manifests with
manual file replacement.

## Repository layout and change rules

- `src/`: game plugin; `installer/`: standalone Windows setup UI and engine.
- `tools/`: imports, builds, packaging and developer installation.
- `tests/`: Python and C# checks; `docs/`: sources, detailed notes and roadmap.
- `.local/`, `artifacts/`, `config.local.json`: ignored machine data and output.

Keep the plugin ID `local.rachel350z.prototype` and filename `Rachel350Z.dll`.
Keep handling changes separate from audio/visual changes where possible; the
0.1.4 handling is the established in-game baseline. Explain what changed, why,
which checks passed and which in-game checks remain. Add user-facing changes
to the root changelog.

Never force-add game files, extracted assets, downloaded tools, generated
binaries, logs, credentials or machine-specific paths. Do not replace the
installed mod while Valheim runs. Preserve named backups and source assets;
never follow junctions/symlinks while cleaning backups.
