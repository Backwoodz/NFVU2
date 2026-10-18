# Drift-Bauwelt in NFVU2 (lokale Entwicklung)

NFVU2 erzeugt beim exakten Seed **`drift`** eine ebene Wiesenwelt als Grundlage
fuer unsere kuenftige Driftstrecke. Die Funktion steckt direkt in der vorhandenen
**Rachel350Z.dll**. FlatGrass oder eine separate Drift-DLL sind nicht erforderlich.

## Welt erstellen

1. Den aktuellen lokalen NFVU2-Build installieren und Valheim starten.
2. Eine neue Einzelspielerwelt anlegen. Der Weltname ist frei waehlbar.
3. Im Feld **Seed** exakt `drift` eingeben. `DRIFT` funktioniert ebenfalls.
4. Der erste Drift-Testabschnitt entsteht etwa 100 m oestlich vom Weltstart.
   Folge dem Schild oder der Kartenmarkierung **NFVU2 Drift**. Beim ersten
   Besuch einige Sekunden fuer den Aufbau der nahen Abschnitte einplanen.

Auch die bestehende `drift`-Welt erhaelt den Testabschnitt beim naechsten Laden.
Der Bau arbeitet an gerade geladenen Bereichen weiter, waehrend du dich naeherst.

Der Seed-Text wird bei exakt `drift` gruen (auch `DRIFT`). Bei anderen Eingaben
kehrt seine normale Farbe zurueck. `drift2`, `adrift`, `my-drift` und normale
Seeds aktivieren diese Funktion nicht. Auch die Menuewelt
bleibt unveraendert. FlatGrass kann separat fuer vorhandene `flatgrass`-Welten
installiert bleiben; die Drift-Welt liest keine seiner Einstellungen.

## Umfang

- Flaches Wiesenbiom im Weltinneren, Hoehe 32, zwei Meter ueber dem Meeresspiegel.
  Valheims urspruengliches Verhalten am aeusseren Weltrand bleibt erhalten.
- Keine zufaelligen Vanilla-Gebaeude, Startsteine, Baeume, Felsen oder Erzlager.
  Gezielt platzierte Streckenbauteile sind davon getrennt. Gras und normale
  Wiesen-Kreaturen bleiben ausserhalb der Pflasterung.
- Standardstart im Weltzentrum bei `(0,32,0)`. Native Ladepruefungen, Betten und
  gespeicherte Logoutpositionen bleiben zustaendig.
- Selbst gebaute Elemente und Terrainbearbeitungen verwenden die normale
  Weltspeicherung. NFVU2 setzt sie beim Betreten nicht zurueck.

Der erste Fahrabschnitt besteht aus 108 m Gerade, einer weiten 180-Grad-Kehre
mit 38 m Radius und 108 m Rueckgerade. Die Fahrbahn ist 16 m breit, in der Kurve
20 m. Sie nutzt echtes Valheim-Pflaster auf dem bestehenden Terrain; Hoehe und
Fahrzeughandling bleiben gleich. Niedrige Markierungssteine stehen etwa 4 m
ausserhalb der Fahrbahnkante (bis Steinmitte, ca. 3,5 m freier Auslauf).
Schilder und zwei Banner markieren Start/Ende. Der Verlauf steht im
[massstaeblichen Entwurf](DRIFT_TRACK_DESIGN.md), die nativen Materialien in
[der Bauteilrecherche](DRIFT_TRACK_MATERIALS.md).

Die komplette Strecke nach dem Referenzbild und Checkpoints folgen nach dem
Fahrtest dieses offenen Abschnitts. Abgeschlossene Abschnitte werden nicht bei
jedem Laden neu gepflastert; absichtlich entfernte Streckenbauteile bleiben weg.
NFVU2 fuer eine bebaute `drift`-Welt aktiv lassen, damit deren Grundgelaende beim
erneuten Laden gleich bleibt.

## Entwicklung und Installation

```powershell
python tools/build.py
python tools/test.py --skip-build --suite driftworld
python tools/test.py --skip-build --suite drifttrack
python tools/install.py
```

Der regulaere NFVU2-Build und dessen Payload enthalten die Welterzeugung.
Installation bei geschlossenem Spiel, mit Backup und SHA-256-Pruefung ueber das
vorhandene Installationswerkzeug. Es gibt keinen separaten Drift-Installer.

Die Quellen liegen in `src/DriftWorldRules.cs`, `src/DriftWorldPatches.cs` und
`src/VehicleDriftWorld.cs`. Die bestehende NFVU2-Harmony-Instanz initialisiert
die sechs Weltpatches und entfernt sie beim Plugin-Abbau wieder.

Die Tests pruefen native API-Metadaten, Integration in die gebaute NFVU2-DLL und
produktive Harmony-Patches an Doubles sowie Seed-Farbe und deren Wiederherstellung.
Fuer den Location-Ergebnispuffer wird Valheims echte ZPackage-Klasse verwendet;
der Lesecursor muss nach dem Schreiben wie im nativen Worker zurueckgesetzt werden.
Sie laden keine echte Unity-Welt. Der Nutzer hat nach der Pufferkorrektur im
Spiel bestaetigt, dass die Welt laedt und komplett flach ist. Speichern/Neuladen,
Weltwechsel, die Seed-Farbe und ein Start mit deaktiviertem FlatGrass bleiben
als native Pruefungen offen. Mehrspieler ist nicht abgenommen.

Der neue Streckenadapter wurde auf Geometrie, Bereichsgrenzen, gespeicherten
Fortschritt und echte native Paint-Settings geprueft. Darstellung, Fahrkontakt,
Schilderstuetzung und Speichern/Neuladen der neuen Strecke sind im Spiel offen.
