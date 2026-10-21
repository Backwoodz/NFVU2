# NFVU2 development notes

- For `/installer`, `$installer`, or a request to build a local installer,
  read and follow `.agents/skills/installer/SKILL.md`.
- For `/release`, `$release`, or an explicit GitHub publication request,
  read and follow `.agents/skills/release/SKILL.md`.
- Publish the Windows installer as `NFVU2-Setup-<version>.zip` plus its ZIP
  SHA-256 sidecar. The ZIP contains the setup EXE, its checksum and instructions;
  also publish the identical EXE and its checksum for older installer updaters.
  Keep the ZIP as the primary player download; new installers prefer ZIP updates.
- For `/tiktok`, `$tiktok`, or a request to cut NFVU2 gameplay into vertical
  social clips, read and follow `.agents/skills/tiktok/SKILL.md`.
- This repository is the source of truth for further NFVU2 development.
- Read README.md and docs/ROADMAP.md for current scope and known limitations.
- Keep original game files, extracted assets, downloaded tools and generated
  binaries in ignored `.local/` or `artifacts/`. Never force-add those files.
- Preserve the existing plugin ID and Rachel350Z.dll name unless a deliberate
  migration is requested; the user already has the mod installed.
- VehicleHandling.cs from version 0.1.4 is confirmed working in-game. Keep audio
  and visual changes separate from handling changes when possible.
- Build with `python tools/build.py`; run relevant checks with
  `python tools/test.py --skip-build`. These are not Unity runtime tests.
- Launch native visual, UI and state probes with `-noaudio` so repeated game
  starts do not play the intro. Keep saved player audio settings unchanged.
- Game installation paths belong in ignored config.local.json. Do not commit
  user-specific absolute paths, logs, credentials or personal identifiers.
- Do not overwrite the installed mod while Valheim is running. Keep a backup
  before replacing an installed version and verify the copied file's checksum.
- Keep only the two newest timestamped installation backups per backup folder.
  Remove older backups only after a successful, checksum-verified installation
  or activation/deactivation. Apply this to local extension installers as well.
  Preserve separately named backups (for example Flatgrass), source assets and
  private plugin sources. Never follow junctions/symlinks during backup cleanup.
- Be precise about provenance: the current model is a stock 350Z; Rachel's
  complete tuning/paint setup and a dual-game bridge remain future work.
