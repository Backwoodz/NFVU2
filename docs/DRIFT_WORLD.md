# Drift-Bauwelt in NFVU2 (lokale Entwicklung)

NFVU2 erzeugt beim exakten Seed **`drift`** eine ebene Wiesenwelt als Grundlage
mit einem 1.491 m langen Drift-Rundkurs. Die Funktion steckt direkt in der vorhandenen
**Rachel350Z.dll**. FlatGrass oder ein zusaetzliches Weltplugin sind nicht erforderlich.
Die eigene Fahrzeugphysik wird als `NFVU2.DriftPhysics.dll` mitgeliefert; siehe unten.

## Welt erstellen

1. Den aktuellen lokalen NFVU2-Build installieren und Valheim starten.
2. Eine neue Einzelspielerwelt anlegen. Der Weltname ist frei waehlbar.
3. Im Feld **Seed** exakt `drift` eingeben. `DRIFT` funktioniert ebenfalls.
4. Start und Ziel liegen etwa 100 m oestlich vom Weltstart.
   Folge dem Schild oder der Kartenmarkierung **NFVU2 Drift**. Beim ersten
   Besuch einige Sekunden fuer den Aufbau der nahen Abschnitte einplanen.

Auch die bestehende `drift`-Welt erhaelt die Erweiterung beim naechsten Laden.
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

Der geschlossene Rundkurs ist **1.490,96 m** lang und 16 bis 20 m breit.
Der vorhandene U-Abschnitt bleibt an seiner bisherigen Position: 108 m Gerade,
38-m-Kehre und Rueckgerade. Neue 32-m-Kurven, eine doppelte S-Kombination,
eine lange Aussenpassage und 48-/40-m-Boegen schliessen die Runde.
Die Fahrbahn besteht durchgehend aus nativem Paved-Terrain auf Hoehe 32;
es werden keine Fahrkraefte oder Gripwerte durch den Streckenbau geaendert.

Der Wikinger-Rennplatz ergaenzt 928 native Bauteile: Holzzaeune hinter dem
Auslauf, verschiedenfarbige Banner, nummerierte Kurvenschilder, vier
ueberdachte Boxen mit Werkbaenken und Kisten sowie eine dreistufige Tribuene
mit 18 Sitzbaenken und seitlichen Treppen. Bis zu zwoelf native Fackeln beginnen mit
vollem Brennstoff; danach gelten Valheims Nachfuellen, Wetter und Verschleiss.
Zufahrt und Boxenvorfeld werden ebenfalls gepflastert. Die Zufahrt kreuzt die
Zielkurve als offene Einmuendung; vor dem Einfahren auf Verkehr achten.

Der [massstaebliche Rundkursplan](images/drift-circuit-plan.svg) wird aus den
implementierten C#-Koordinaten erzeugt. Er zeigt die geplante Geometrie und
Einrichtung, keinen Screenshot einer bereits abgenommenen Spielwelt.

Mini- und Weltkarte zeigen den Rundkurs automatisch als helle Linie mit dunklem
Rand, das Boxenvorfeld/Zufahrt dezenter und Start/Ziel als tuerkisen Querstrich.
Die Darstellung stammt aus denselben Pflasterkoordinaten wie der Streckenbau
und folgt dem normalen Kartenzoom und Verschieben. Der geplante Kurs ist auch
in noch nicht erkundeten Bereichen sichtbar; das uebrige Gelaende bleibt unter
dem normalen Kartennebel. Der vorhandene Startpin und Fahrzeugmarker bleiben.
Eigene Umbauten der Fahrbahn werden nicht aus dem gespeicherten Terrain erkannt.
Die Ebene erscheint nur in Einzelspielerwelten mit Seed `drift`, veraendert keine
Kartendateien und wird bei Weltwechsel/Plugin-Abbau wieder entfernt. Eine gemeinsame
512x512-Textur wird einmal pro Welt erzeugt; es gibt keine Textur-Uploads pro Frame.

Massstab und Archiv des ersten Abschnitts stehen im
[Streckenentwurf](DRIFT_TRACK_DESIGN.md), native Teile in der
[Bauteilrecherche](DRIFT_TRACK_MATERIALS.md).

Version 2 hat separate gespeicherte Baufortschritte. Der alte Abschnitt wird
nicht neu aufgestellt; die Erweiterung entsteht einmalig in geladenen Bereichen.
Abgeschlossene Baubereiche werden nach dem Neuladen nicht erneut gebaut;
absichtlich entfernte Teile bleiben entfernt. Bearbeitete oder verschobene
Schilder bleiben erhalten. Nur unveraenderte alte Start-/Endschilder erhalten
passende Rundkursbeschriftungen. Spielfremde Bauten werden nicht entfernt.
NFVU2 fuer eine bebaute `drift`-Welt aktiv lassen, damit deren Grundgelaende
beim erneuten Laden gleich bleibt.

## Rennbetrieb in der Driftwelt

In der `drift`-Einzelspielerwelt gilt ausschliesslich der Driftmodus des 350Z.
Flug, Transformer und Hordenmodus sind gesperrt. H2-Fahrzeuge werden dort weder
neu erstellt noch automatisch restauriert, ihre gespeicherten Weltobjekte
bleiben erhalten und zaehlen weiter zum Fahrzeuglimit. Die normalen Einstellungen
fuer andere Welten werden nicht ueberschrieben; dort bleibt Drift verfuegbar.

Die Driftwelt bleibt nachts bei klarem Wetter. Nur die lokale Umgebungsdarstellung
wird ueberschrieben; Weltalter und Netzwerkzeit laufen normal weiter. Zufallsspawns,
Kreaturenspawner und Raids sind gesperrt. Vorhandene fremde Kreaturen und lose
ItemDrop-Objekte werden in begrenzten Durchlaeufen entfernt, ohne Tod/Loot auszulösen.
Das betrifft auch manuell auf den Boden geworfene Gegenstaende. Spielerinventar,
Kisteninhalt, platzierte Dekoration und gezaehmte Tiere bleiben erhalten.

34 animierte Greydwarf-Zuschauer stehen auf den Tribuenen und neben dem Kurs.
Es sind lokale Kulissenmodelle ohne Kampf-KI, Kollisionen oder Loot, mit nativen
Idle- und einzelnen Tanzanimationen. 46 warme Laternenpfosten ergaenzen die
vorhandenen nativen Fackeln; maximal sechs nahe, schattenlose Punktlichter sind
aktiv. Die Kulissen werden nur in der Driftwelt erzeugt und beim Verlassen entfernt.
An Start/Ziel liegt eine flache zweireihige Schachbrettmarkierung ohne Collider.

Die Runde beginnt beim Vorwaertsueberqueren der Startlinie. Punkte entstehen nur
fuer neue vorwaerts gefahrene Kursmeter bei mindestens 4 m/s und 10 bis 75 Grad
Driftwinkel, mit einem bis zu fuenffachen Multiplikator. Rueckwaerts fahren und
denselben Abschnitt erneut driften erhoeht den Punktestand nicht. Nach vollstaendiger
Runde durch die aufeinanderfolgenden 25-m-Kontrollabschnitte wird an Start/Ziel
abgerechnet und die naechste Runde beginnt bei null. Beste Punktzahl und schnellste
gueltige Rundenzeit werden im nativen Weltspeicher unter `nfvu2_drift_best_v1`
gesichert. Auf der Wiese bleiben Runde und Punktestand erhalten; dort entstehen
keine Punkte und die Combo endet. Auf der Strecke zaehlen neue Driftmeter wieder,
ohne erneutes Ueberqueren der Startlinie. Auch auf der Wiese bleibt der bereits
gefahrene Fortschritt gespeichert, damit Zurueckfahren und erneutes Driften keine
doppelten Punkte bringen. Die Rundenzeit laeuft waehrend des Ausflugs weiter.
Teleport, Tod oder Fahrzeugwechsel unterbrechen weiterhin die Runde;
stehende Menuepausen bringen keine Punkte.

Die Anzeige nutzt die originale `DRIFT_STYLE_POINTS_BACK`-Hintergrundgrafik aus
dem lokal vorhandenen `GLOBAL/InGameDrift.bun` und die geladene Valheim-Schrift
`Valheim-Norsebold`. Layout, Wertung und Beschriftung sind eigene NFVU2-Logik.
`python tools/export_drift_hud.py` erzeugt die optionale Grafik im ignorierten
Medienordner; ohne sie bleibt die Anzeige mit schlichtem Hintergrund verwendbar.
Ein Publikumsklang aus NFSU2 wurde in den untersuchten lokalen Archiven nicht
eindeutig identifiziert; deshalb wird kein unbestaetigter Klang als Publikum ausgegeben.

Die Wertungs-/Sessiontests pruefen Regeln und native API-Vertraege ohne Spielwelt.
Die separate Unity-Menueprobe rendert echte Greydwarf-Rigs und native Materialien,
prueft laufende Animation, Fussausrichtung, Lichtlimit, Bodenmarkierung und Abbau.
Die HUD-Probe bestaetigt Norsebold und Texte ohne Ueberlauf. Diese Proben laden
keine gespeicherte Welt; Rundenfahren, Welt-Speichern/Neuladen, Nachtwirkung und
Bildrate auf dem gesamten befahrenen Kurs bleiben als Spielabnahme offen.

## Entwicklung und Installation

```powershell
python tools/build.py
python tools/test.py --skip-build --suite driftworld
python tools/test.py --skip-build --suite drifttrack --suite driftbuild
python tools/test.py --skip-build --suite driftmap --suite vehiclemap
python tools/test.py --skip-build --suite driftsession --suite driftscore --suite driftatmosphere
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
Die produktive Bauklasse ist zusaetzlich an Besitz-/Lade-Doubles getestet: neue
und bestehende Welten, getrennte v1/v2-Marker, Zwei-Operationen-Budget, Neuladen,
Spielerabbau, manuelle Schilder und unveraenderter Fackelbrennstoff. Das ersetzt
keine native Fahr-, Grafik- oder Stabilitaetsabnahme der kompletten Anlage.

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
