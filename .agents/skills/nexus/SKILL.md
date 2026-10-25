---
name: nexus
description: Baue ein eigenstaendiges NFVU2-Mod-ZIP mit entpacktem Ordner und SHA-256 fuer Nexus Mods. Verwenden bei /nexus, $nexus oder einem Auftrag fuer ein Nexus-Uploadpaket; kein GitHub-Release und kein EXE-Installer.
---

# NFVU2 fuer Nexus Mods

Erzeuge aus dem aktuellen Projektstand einschliesslich offener Aenderungen ein
direkt entpackbares Mod-ZIP. Der Build benoetigt keinen GitHub-Release, Tag,
API-Zugriff oder Setup-Updater. Nutze die vorhandenen lokalen Assets und
`config.local.json`. Behalte Plugin-ID und `Rachel350Z.dll` bei.

## Ablauf

1. Lies `AGENTS.md`, `README.md`, `docs/ROADMAP.md` und die Build-Hinweise in
   `CONTRIBUTING.md`. Pruefe Status und relevanten Diff.
2. Baue und pruefe aus dem Repository-Stamm:

   ```powershell
   python tools/build.py
   python tools/test.py --skip-build
   python tools/build_nexus.py
   ```

   Bereits am unveraenderten Stand bestandene Tests nicht wiederholen; nach
   Aenderungen die betroffenen Pruefungen ausfuehren. Bei Fehlern keinen alten
   Build als aktuellen ausgeben.
3. Der Builder erstellt unter `artifacts/nexus/<Zeitstempel>/` ein
   `NFVU2-Nexus-<Modversion>.zip`, dessen `.zip.sha256` und den entpackten
   `package/`-Ordner. Die Modversion kommt aus Projekt und Plugin, nicht von
   GitHub. Wiederholte Builds haben eigene Ordner und ueberschreiben nichts;
   eine neue Version nur auf entsprechenden Auftrag aendern.
4. Pruefe ZIP-Pruefsumme und `NFVU2-PACKAGE.json`: alle Dateihashes stimmen,
   Haupt-DLL und Physik-DLL entsprechen dem frischen Build, vollstaendige
   Radioplaylist samt allen Titeln enthalten. Der Builder prueft das ZIP bereits
   gegen die kopierten Dateien; vergleiche zusaetzlich die Paketliste mit der
   Mod-Allowlist aus `tools/mod_files.py` und fuehre `git diff --check` aus.

## Paketvertrag

- ZIP-Wurzel: `BepInEx/plugins/Rachel350Z/`, `INSTALL.txt` und
  `NFVU2-PACKAGE.json`. Keine uebergeordnete Versionsordner-Ebene im ZIP.
- Nur eigene Mod-DLLs und die vorgesehenen Runtime-Assets aus der Allowlist.
  Keine Setup-EXE, Skripte, eingebetteten Paketarchive, BepInEx-/Doorstop-Binaries,
  privaten Plugins, Benutzerkonfigurationen oder Original-Quellarchive.
- BepInEx 5 ist eine separat zu installierende Voraussetzung. Die Anleitung
  beschreibt manuelle Installation, Aktualisierung, Entfernung und den Wechsel
  vom bisherigen Installer: erst dort NFVU2 deinstallieren, Loader behalten,
  dann das Mod-ZIP kopieren. Manuelles Ueberschreiben sonst installer-eigener
  Dateien wuerde dessen gespeicherte Pruefsummen ungueltig machen.
- Das Nexus-Paket wird unabhaengig vom GitHub-Setup gebaut. Dessen Versionen,
  Dateien, Updater und Downloadlinks nicht fuer diesen Auftrag veraendern.

## Uebergabe und Nexus-Seite

Liefere klickbare Pfade zu ZIP, SHA-256 und entpacktem Ordner sowie Modversion
und Pruefergebnis. Das Paket wird nur lokal gebaut: Ein Upload, eine Nachricht
an Nexus, ein Commit oder eine Spielinstallation ist kein Teil von `/nexus`.

Technische Paketpruefungen sind keine Garantie fuer Nexus-Freigabe, Rechte an
importierten Assets oder eine bestandene Spielabnahme. Fuer die Nexus-Seite
Quellcode und Build-Anleitung angeben; die zutreffenden KI-Tags nach den
[Einreichungsregeln](https://help.nexusmods.com/article/28-file-submission-guidelines)
anwenden (bei ueberwiegend generiertem Code `AI-Generated Content`, bei
KI-Promomedien gegebenenfalls zusaetzlich `AI Media`). Keine pauschale
"garantiert virenfrei"-Aussage. Eine vorhandene Quarantaene erfordert eventuell
weiterhin eine [manuelle Nexus-Pruefung](https://help.nexusmods.com/article/117-why-has-my-mod-been-quarantined).
