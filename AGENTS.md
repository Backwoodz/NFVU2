# NFVU2 development notes

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
