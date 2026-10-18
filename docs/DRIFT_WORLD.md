# Drift-Bauwelt in NFVU2 (lokale Entwicklung)

NFVU2 erzeugt beim exakten Seed **`drift`** eine ebene Wiesenwelt als Grundlage
fuer unsere kuenftige Driftstrecke. Die Funktion steckt direkt in der vorhandenen
**Rachel350Z.dll**. FlatGrass oder eine separate Drift-DLL sind nicht erforderlich.

## Welt erstellen

1. Den aktuellen lokalen NFVU2-Build installieren und Valheim starten.
2. Eine neue Einzelspielerwelt anlegen. Der Weltname ist frei waehlbar.
3. Im Feld **Seed** exakt `drift` eingeben. `DRIFT` funktioniert ebenfalls.
4. Die freie Wiesenflaeche wird unser Baugrund fuer die spaetere Strecke.

Der Seed-Text wird bei exakt `drift` gruen (auch `DRIFT`). Bei anderen Eingaben
kehrt seine normale Farbe zurueck. `drift2`, `adrift`, `my-drift` und normale
Seeds aktivieren diese Funktion nicht. Auch die Menuewelt
bleibt unveraendert. FlatGrass kann separat fuer vorhandene `flatgrass`-Welten
installiert bleiben; die Drift-Welt liest keine seiner Einstellungen.

## Umfang

- Flaches Wiesenbiom im Weltinneren, Hoehe 32, zwei Meter ueber dem Meeresspiegel.
  Valheims urspruengliches Verhalten am aeusseren Weltrand bleibt erhalten.
- Keine neu erzeugten nativen Gebaeude, Startsteine, Baeume, Felsen oder Erzlager.
  Gras und normale Wiesen-Kreaturen bleiben.
- Standardstart im Weltzentrum bei `(0,32,0)`. Native Ladepruefungen, Betten und
  gespeicherte Logoutpositionen bleiben zustaendig.
- Selbst gebaute Elemente und Terrainbearbeitungen verwenden die normale
  Weltspeicherung. NFVU2 setzt sie beim Betreten nicht zurueck.

In dieser ersten Stufe entstehen nur der Baugrund und der Startpunkt.
Die Fahrbahn nach dem Referenzbild, Valheim-Dekorationen und Checkpoints folgen.
NFVU2 fuer eine bebaute `drift`-Welt aktiv lassen, damit deren Grundgelaende beim
erneuten Laden gleich bleibt.

## Entwicklung und Installation

```powershell
python tools/build.py
python tools/test.py --skip-build --suite driftworld
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
