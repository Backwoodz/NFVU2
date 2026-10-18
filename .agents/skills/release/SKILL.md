---
name: release
description: Veröffentliche einen NFVU2-Release auf GitHub mit Versionstag, Release-Notizen, geprüftem Windows-Installer als ZIP und SHA-256. Verwenden bei /release, $release oder einem ausdrücklichen Veröffentlichungsauftrag.
---

# NFVU2 GitHub Release

Veröffentliche den aktuellen beauftragten Projektstand als GitHub-Release.
Ein Aufruf von `/release` oder `$release` beauftragt Vorbereitung, Build,
Commit der zugehörigen Änderungen, Push, Versionstag, Asset-Upload und
Veröffentlichung. Bei einem ausdrücklich gewünschten Entwurf stoppe beim Draft.
Eine Frage über Releases oder das Anlegen dieses Skills ist kein
Veröffentlichungsauftrag. `/installer` baut ausschließlich lokal.

## Stand und Zugriff

- Lies `AGENTS.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `docs/ROADMAP.md` und
  den [Installer-Skill](../installer/SKILL.md). Befehle laufen im Repo-Stamm.
- Prüfe Remote, Branch, Arbeitsbaum, relevante Änderungen sowie lokale und
  entfernte Tags/Releases. Das erwartete Repository ist `Backwoodz/NFVU2`;
  verwende kein anderes Ziel ohne entsprechenden Auftrag.
- Prüfe GitHub-Werkzeuge und Anmeldung mit Schreibzugriff. Bevorzuge vorhandene
  GitHub CLI (`gh auth status`) oder passende API-Werkzeuge. Ist `gh` nicht im
  PATH, suche vorhandene lokale Installationen. Alternativ verwende den
  angemeldeten Browser mit dem Computer-Use-Skill. Gib keine Tokens aus.
- Committe nur zum beauftragten Release gehörende Änderungen. Falls fremde
  Änderungen die Wahl des Stands unklar machen, bereite unabhängig davon
  Notizen und Prüfungen vor und kläre nur diesen konkreten Punkt.

## Version, Build und Commit

- Übernimm eine gewünschte Version. Sonst wähle für einen neuen Bugfix-Release
  die nächste freie Patch-Version oberhalb der aktuellen Mod-Version und bereits
  veröffentlichten Versionen. Bestehende Release-Tags nicht wiederverwenden oder
  verschieben. Änderungen bestehender Releases benötigen einen ausdrücklichen Auftrag.
- Aktualisiere `src/NFVU2.csproj` und das `BepInPlugin`-Attribut zusammen.
  Überführe zugehörige Unreleased-Einträge in einen Changelog-Versionsabschnitt.
- Baue und verifiziere den Installer nach dem Installer-Skill. Erstelle englische
  Release-Notizen mit konkreten Änderungen, Installationshinweis, Plattform und
  Prüfergebnissen. Behaupte nur durchgeführte Spieltests. Schreibe die Notizen unter
  `artifacts/` und verwende bei `gh` `--notes-file` für echte Zeilenumbrüche.
- Verpacke danach mit `python tools/package_installer.py` die geprüfte EXE als
  `artifacts/installer/NFVU2-Setup-<version>.zip`. Die ZIP enthält exakt die EXE,
  ihre `.sha256` und `INSTALL.txt`; die äußere Prüfsumme heißt
  `NFVU2-Setup-<version>.zip.sha256`. Prüfe ZIP-Inhalt, CRC, enthaltene EXE gegen
  die ursprüngliche EXE-Prüfsumme und die ZIP gegen ihre eigene Prüfsumme.
  Die Nutzlast ist bereits komprimiert; die äußere ZIP darf ZIP_STORED verwenden.
- Standard: ZIP als Hauptdownload, dazu dieselbe EXE und ihre innere `.sha256`
  als Kompatibilitätsassets für ältere Installer (einschließlich 0.31.0/0.31.1).
  Nicht auf ZIP-only umstellen: ältere bereits heruntergeladene Installer können
  ihre EXE-Suche nicht nachträglich ändern. Ab 0.31.2 bevorzugt der Updater die ZIP,
  prüft äußere und innere SHA-256 und startet die verifizierte entpackte EXE.
  Es bleibt ein Installerpaket, kein manuelles Modpaket. Versprich keine zusätzliche
  Sicherheit durch ZIP. Für manuelle Downloads: entpacken, dann Setup.exe starten.
- Committe den geprüften Stand einschließlich gemeinsamer Mod-/Installer-Version mit
  expliziten Dateipfaden. Ignorierte Assets/Binaries bleiben lokal. Der Tag muss
  exakt auf den Commit zeigen, dessen Build-Eingaben den Installer erzeugt haben.
  Nach weiteren Build-relevanten Änderungen erneut bauen und prüfen.
- Pushe den Release-Commit zum vorgesehenen Branch ohne Force-Push. Erstelle
  einen annotierten Tag `v<version>` auf dessen exakter SHA und pushe diesen Tag.
  Prüfe die entfernte Tag-SHA. Bei Konflikten oder Fehlern nicht veröffentlichen.

## GitHub-Release

- Erstelle zuerst einen Draft mit Tag, Titel, Notizen und genau vier Assets:
  Installer-ZIP, `.zip.sha256`, identische Kompatibilitäts-EXE und deren `.sha256`.
  Lade weder `.rsp` noch `payload.zip` hoch.
  GitHub stellt die Source-code-Archive selbst bereit. Mit CLI beispielsweise:

```powershell
gh release create <tag> <installer.zip> <installer.zip.sha256> <installer.exe> <installer.sha256> --repo Backwoodz/NFVU2 --verify-tag --draft --title <title> --notes-file <notes-file>
gh release view <tag> --repo Backwoodz/NFVU2 --json url,isDraft,tagName,assets
```

- Ersetze die Platzhalter durch konkrete geprüfte Werte. Kontrolliere bei
  Wiederholungsversuchen, ob Draft oder Upload bereits existieren. Bestehende
  Assets nicht blind mit `--clobber` ersetzen; beachte unveränderliche Releases.
- Lade hochgeladene Assets in ein separates ignoriertes Prüfverzeichnis herunter
  und vergleiche ihre SHA-256 mit lokalen Dateien. Öffne auch die heruntergeladene
  ZIP und prüfe die enthaltene EXE gegen die ursprüngliche EXE-Prüfsumme.
  Vergleiche sie auch mit der separat hochgeladenen Kompatibilitäts-EXE.
  Prüfe Tag, Zielcommit, Titel und
  Notizen. Bei Fehlern belasse den Draft und melde den Fehler.
- Bei Veröffentlichungsauftrag publiziere den geprüften Draft, beispielsweise
  mit `gh release edit <tag> --repo Backwoodz/NFVU2 --draft=false`.
  Prüfe danach die erreichbare Release-Seite und alle vier Download-Assets.
  Prüfe den öffentlichen neuesten Release mit altem Updater-Code (EXE wird
  erkannt) und neuem Code (ZIP wird bevorzugt). Der echte ZIP-Download muss eine
  geprüfte EXE für die Übergabe liefern; den Installer dabei nicht ins Spiel
  installieren oder die heruntergeladene EXE ungefragt starten.
  Bezeichne einen Entwurf nie als veröffentlicht.
- Aktualisiere anschließend README-Downloadlink und Größenangabe auf das
  tatsächlich veröffentlichte Asset; committe und pushe diese Dokumentationsänderung.
  Verschiebe den Release-Tag nicht auf den nachträglichen Dokumentationscommit.

## Abschluss

Liefere den direkten GitHub-Release-Link, Version/Tag, ZIP-Downloadlink und
Prüfergebnis; benenne ausstehende Spieltests. Bei fehlendem Zugriff erledige die
lokalen Vorbereitungen und nenne den noch erforderlichen Anmeldeschritt.
Ein normaler Push führt diesen Skill nicht aus; ein automatischer
Release-Workflow ist im Projekt bisher nicht eingerichtet.

Referenzen:
- [GitHub: Releases verwalten](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
- [GitHub CLI: release create](https://cli.github.com/manual/gh_release_create)
