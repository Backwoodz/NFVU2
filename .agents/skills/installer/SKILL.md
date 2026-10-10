---
name: installer
description: Baue einen aktuellen lokalen NFVU2-Windows-Installer mit geprüfter Mod und SHA-256. Verwenden bei /installer, $installer oder einem Auftrag, einen aktuellen lokalen Installer zu erstellen.
---

# NFVU2 Installer

Liefere einen aktuellen Windows-Installer aus dem jetzigen Projektstand,
einschließlich noch nicht commiteter Änderungen. Alle folgenden Pfade und
Befehle beziehen sich auf das Repository-Stammverzeichnis.

## Umfang und Vorbereitung

- Lies `AGENTS.md`, `README.md`, `docs/ROADMAP.md` und die Build-Anleitung in
  `CONTRIBUTING.md`. Prüfe `git status --short` und den relevanten Diff.
- `/installer` beauftragt einen lokalen Build. Commit, Tag, Push, GitHub-Release
  und Installation im Spiel benötigen einen entsprechenden zusätzlichen Auftrag.
- Verwende vorhandene lokale Assets und `config.local.json`. Bei fehlenden
  Voraussetzungen benenne den konkreten Blocker; liefere keinen alten Installer
  als aktuellen Release und lösche dann keine bisherigen Installer.
- Behalte Plugin-ID und `Rachel350Z.dll` bei. Ändere kein Fahrzeugverhalten
  allein für den Release-Build.

## Version und Dokumentation

- Übernimm standardmäßig die aktuelle Mod-Version. Prüfe die Übereinstimmung
  von `src/NFVU2.csproj` und dem `BepInPlugin`-Attribut in `src/Plugin.cs`.
  Bei einer ausdrücklich gewünschten neuen Mod-Version aktualisiere beide.
- Setze `SETUP_REVISION` in `tools/build_installer.py` so, dass für die gewählte
  Mod-Version ein neuer Dateiname entsteht. Falls bereits ein Installer dieser
  Revision existiert, erhöhe auf die nächste freie Revision oberhalb aller
  vorhandenen Revisionen dieser Version. Überschreibe keinen bisherigen Installer.
- Dokumentiere enthaltene Änderungen und die lokale Setup-Revision in
  `CHANGELOG.md`; halte ausstehende Spieltests in `docs/ROADMAP.md` fest.
  Ändere öffentliche Downloadlinks erst bei tatsächlich erfolgter Veröffentlichung.

## Bauen und prüfen

Führe aus dem Repository-Stammverzeichnis aus:

```powershell
python tools/build.py
python tools/test.py --skip-build
python tools/build_installer.py
```

- Fahre nur bei erfolgreichen Schritten fort. Der Testlauf umfasst auch die
  Installer-Tests. Bereits am unveränderten aktuellen Stand erfolgreich gelaufene
  Prüfungen müssen nicht wiederholt werden; nach Änderungen prüfe betroffene Teile.
- Der Installer muss die frisch gebaute DLL und die vollständige Radiomusik
  enthalten. Verwende den bestehenden Payload-Builder mit seiner Allowlist und
  dem festgelegten BepInEx-Paket; paketiere keine privaten Plugins oder Konfigurationen.
- Prüfe die erzeugte EXE gegen ihre `.sha256`-Datei. Prüfe alle Einträge aus
  `payload.json` gegen die Bytes in `artifacts/installer/payload.zip` und die dort
  enthaltene `Rachel350Z.dll` gegen `artifacts/mod/Rachel350Z.dll`.
- Prüfe außerdem, dass die eingebettete Assembly-Ressource `NFVU2.Payload`
  der EXE denselben SHA-256 wie `payload.zip` besitzt. Das geht unter Windows
  über `[Reflection.Assembly]::LoadFile()` und `GetManifestResourceStream()`;
  starte dafür weder den Wizard noch eine Installation.
- Führe `git diff --check` aus. Generierte Dateien bleiben ignoriert.

## Alte Installer aufräumen und Ergebnis liefern

- Nach erfolgreichem Build und vollständiger Verifikation behalte nur den neu
  erzeugten Installer und seine `.sha256`-/`.rsp`-Dateien. Entferne ältere
  Dateien direkt in `artifacts/installer`, deren Name exakt dem Schema
  `NFVU2-Setup-<version>-r<revision>.(exe|sha256|rsp)` entspricht.
- Prüfe vor Löschung die absoluten Zielpfade und überspringe Links/Junctions.
  Lösche keine anderen Dateien oder Verzeichnisse. `payload.zip`, Quellassets
  und separat benannte Dateien bleiben erhalten. Installationsbackups unterliegen
  weiterhin der eigenen Aufbewahrungsregel aus `AGENTS.md`.
- Nenne im Abschluss den klickbaren Pfad zur neuen EXE und zur Prüfsumme,
  Version/Revision sowie das Prüfergebnis. Unterscheide automatische Prüfungen
  klar von noch ausstehenden Unity-Spieltests.
