# Drift-Bauwelt in NFVU2 (lokale Entwicklung)

NFVU2 erzeugt beim exakten Seed **`drift`** eine ebene Wiesenwelt als Grundlage
fuer unsere kuenftige Driftstrecke. Die Funktion steckt direkt in der vorhandenen
**Rachel350Z.dll**. FlatGrass oder ein zusaetzliches Weltplugin sind nicht erforderlich.
Die eigene Fahrzeugphysik wird als `NFVU2.DriftPhysics.dll` mitgeliefert; siehe unten.

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
20 m. Sie nutzt echtes Valheim-Pflaster auf dem bestehenden Terrain und laesst
dessen Hoehe unveraendert. Der Streckenbau veraendert das Fahrzeughandling nicht.
Niedrige Markierungssteine stehen etwa 4 m
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

## Eigene Driftphysik

Der aktuelle lokale Build verwendet im 350Z-Driftmodus die eigene
**`NFVU2.DriftPhysics.dll`**. Sie entsteht aus den C#-Quellen unter `physics/`
und wird zusammen mit `Rachel350Z.dll` installiert. Die Bibliothek benoetigt
keine Unity- oder Spieldateireferenzen. NFVU2 laedt keine Originalbibliothek
eines anderen Rennspiels und greift auf keine fremde Spielinstallation zu.
Die bestehende Plugin-ID und der Name `Rachel350Z.dll` bleiben erhalten.

Die portable Bibliothek berechnet Motor, Kupplung, Sechsgangautomatik,
Hinterradantrieb sowie Laengs- und Seitenkraefte der Reifen. Der eigene
Unity-Adapter in `src/DriftVehicleAdapter.cs` liefert Bodenkontakte per
Strahlabfrage, berechnet Federung und Lenkung und uebergibt Kraefte an das
dynamische Chassis. Unity integriert dessen Bewegung und loest Kollisionen.
Die WheelCollider sind dabei ausgeschaltet. Der Adapter verwendet eine eigene
Fahrzeugschwerkraft von 9,81 m/s2; Valheims globale Schwerkraft bleibt erhalten.
Beim Moduswechsel, Flug, Transformer, Ausstieg und Fahrzeugabbau wird der
vorherige Zustand des Fahrzeugs wiederhergestellt.

Radposen, Motordrehzahl, Gang, Fahr-/Reifengeraeusche und Reifenrauch verwenden
die eigene Telemetrie. Nitro bleibt die zusaetzliche NFVU2-Beschleunigung und
nutzt die vom Adapter ermittelten Bodenkontakte. Andere Fahrmodi und der Hummer
verwenden weiterhin den bisherigen Controller.

Die Radgrafik folgt dem interpolierten Render-Transform der Karosserie.
Federweg, Lenkwinkel und die vorzeichenbehaftete Raddrehung werden zwischen
Physikschritten interpoliert, einschliesslich ganzer Umdrehungen bei hohem RPM.
Das beseitigt den festgestellten Versatz zwischen den aktuellsten Physikposen
der Raeder und der geglaetteten Karosserie. Bremszangen und Cockpitlenkung nutzen
denselben dargestellten Lenkwinkel. Diese Korrektur veraendert keine Fahrkraefte.

Der regulaere Build erstellt beide DLLs, und das regulaere Installationswerkzeug
sichert und prueft beide per SHA-256. Ein separater privater Installationsschritt
ist nicht mehr erforderlich:

```powershell
python tools/build.py
python tools/test.py --skip-build --suite driftphysics --suite driftadapter
python tools/install.py
```

Vor der Installation Valheim schliessen. Beide DLLs gehoeren zum vollstaendigen
Payload; die Physik-DLL nicht einzeln entfernen. Im Fahrzeugmenue den Driftmodus
waehlen. Die aktuelle lokale Konfiguration lautet:

```ini
[DriftPhysics]
Enabled = true
```

`Enabled = false` verwendet wieder den bisherigen Driftcontroller nach einem
Neustart. Bei einem Fehler waehrend der Adapterinitialisierung oder Simulation
stellt NFVU2 den bisherigen Controller wieder her und meldet den Fehler im Log.
Die erforderliche Physik-DLL bleibt auch bei abgeschalteter Nutzung installiert.

Fuer die ausdrueckliche Entwicklungskontrolle in Valheims echter Unity-/PhysX-
Laufzeit steht `python tools/test_drift_unity.py` bereit. Valheim muss geschlossen
sein. Das Werkzeug kompiliert die eigenen Physikquellen in ein temporaeres
Diagnoseplugin, startet das Spiel ohne Grafik bis zum Menue und laedt keine Welt.
Mit `--installed-library` prueft es stattdessen die separat installierte DLL und
kontrolliert deren tatsaechlich geladenen Dateipfad.
Kopie und Entfernung des Diagnoseplugins werden per SHA-256 geprueft;
Berichte liegen ignoriert unter `artifacts/tests/driftunity/`. Die normalen
Tests mit Doubles starten Valheim nicht.

Am 18.10.2026 bestanden der regulaere Build, 52 portable Physikpruefungen,
187 Adapterpruefungen und 134 echte Unity-/PhysX-Pruefungen mit der separat
installierten Bibliothek. Auch die betroffenen Handling-, Audio-, Eingabe-,
Streaming- und Nitropruefungen bestanden. Die Installation wurde gesichert und
per SHA-256 verifiziert; die alte Original-DLL liegt ausschliesslich archiviert.
Die Menueprobe misst rund 88 m Radius bei kleinem Lenkeingang, rund 20 m in der
engen Driftlinie und 0,74 s bis zum entgegengesetzten Driftwinkel von 15 Grad.
Diese synthetischen Manoever ersetzen keine persoenliche Spielabnahme.
Insbesondere Fahrgefuehl auf der Strecke,
Kollisionen, Moduswechsel und Speichern/Neuladen bleiben zu pruefen.
Die erste Abstimmung verwendet gemeinsame Reifenreibung fuer die Untergruende;
eine separate Klassifikation aller Valheim-Boeden steht noch aus.
Identisches Handling zu einem Referenzspiel ist nicht belegt.

Zur Herkunft: Der vorherige lokale Machbarkeitsversuch nutzte eine kopierte
CarX-Playtest-Bibliothek. Dieser Versuch samt Analysebelegen ist im ignorierten
lokalen Archiv erhalten. Die aktuelle Implementierung und ihr regulaerer Build
verwenden diese Originalbibliothek nicht.
