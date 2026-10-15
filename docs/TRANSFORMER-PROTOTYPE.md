# Lokaler Transformer-Prototyp

Ausgangspunkt ist das vorhandene 350Z-Modell und die freigegebene
V18-Animation. `scratch_3.txt` ist eine Ideensammlung und kein Arbeitsauftrag.
Nutzer bestaetigt Standform und grundlegendes Gehen im Spiel. Gezielte
Spielabnahmen fuer Physik, Untergrund und neue Bewegungsvarianten bleiben offen.

**Aktiver Umfang:** Verwandlung, derselbe Fahrer am Nacken, Kamera,
Vor-/Rueckwaertsgehen, Drehen und Rueckverwandlung. Neue Bewegungsvarianten
werden erst mit passendem bestandenem Geometrie- und Controller-Audit ueber
`walkingReady` und `gaitVersion` aktiviert.

## Umgesetzt

- Optionaler, standardmaessig ausgeschalteter Modus. Plugin-ID und
  `Rachel350Z.dll` bleiben gleich; `VehicleHandling.cs` bleibt unveraendert.
- `tools/export_transformer_runtime.py` exportiert V18 mit 48 Abtastungen/s,
  SHA-256-Bindung an `body.json`, maximal 48 Bones je Rendergruppe und starrer
  Bindung jedes Vertex. Ganze Scheiben, Leuchten und sieben Neonroehren bleiben.
  Die generische Blender-Figur wird nicht als Spielfigur exportiert.
- Vorhandene Fahrzeugmaterialien, getoente Scheiben und Leuchtenmaterialien
  werden verwendet. Vier Raeder/Bremsen mit aktuellem Sturz, Versatz und
  Felgengroesse sowie die gewaehlten Auspuffe kommen direkt vom gespawnten Auto.
  Innenraum, Motorausbau und Kofferraumequipment werden zusaetzlich mitgefuehrt
  und beim Oeffnen verdeckt komprimiert. Unterboden-/Kofferraum-Neon verwenden
  ihre getrennten aktuellen Farb-, Aktivierungs- und Pulseinstellungen.
- Derselbe lebende Player bleibt erhalten. Seine vorhandene Sitzpose wird
  ueber IK-Ziele auf die Nackenposition ueberfuehrt; kein Player-Klon, kein
  Inventar-/Gesundheits-/Equipment-/Netzwerkwechsel.
- Fuenf Sekunden Verwandlung, freigegebene Standhaltung und inverse Verwandlung.
  Langsames Vor-/Rueckwaertsgehen und Drehen verwenden Zweigelenk-IK und
  getrennte Schrittrichtungen je Fuss. Panelhalter werden zwischen ihren
  bewegten Endpunkten aktualisiert. Die seitlichen Knieplatten fahren zuerst
  aus; nach dem Absetzen der Fuesse schliessen sie wieder. F10/F7 wartet diesen
  Ablauf vor der Rueckverwandlung ab.
- V3 ersetzt den bisherigen Schrittschwinger durch getrennte Stand- und
  Schwungphasen. Gesetzte Fuesse werden gegen die gemessene Verschiebung und
  Drehung des interpolierten Roboter-Rumpfs gefuehrt. Nur angehobene Fuesse
  bekommen neue Landepunkte. Anhalten und Rueckwaertswechsel setzen die Fuesse
  einzeln ab; vor der Rueckverwandlung kehren alle Kanaele in die Standpose.
- Laengere Schritte, nach vorn gefuehrte Knie und ein schmalerer Gehstand.
  Zweigelenk-IK erhaelt die Laengen der Mechanik; notwendige Beckenabsenkung
  wird fuer beide Beine gemeinsam geloest. Abrollen beruecksichtigt die echte
  Fuss-/Zehensohle. Beckenlast, Brust-Gegendrehung, Vorneigung und gegenlaeufige
  Arme/Ellbogen passen zum Tempo und zum jeweiligen Standbein. Fahrer, Spoiler,
  Leuchten und Neon bleiben an ihren bewegten Halterungen.
- Beschleunigung und staerkeres Abbremsen bleiben vom Fahrzeughandling getrennt.
  Die konkreten V3-Parameter stehen in `src/TransformerGait.cs` und
  `tools/transformer_gait_v3.json`. Der aktuelle Stand basiert auf 0.30.0 samt
  Weltladen und dauerhaft gespeicherten Fahrzeugen. Speichern im Robotermodus
  verwendet eine sichere Auto-Rueckkehrhoehe; Neustart stellt ein Auto wieder her.
- Kamera folgt der Hoehe. Roboterbewegung verwendet einen eigenen
  Kollisionskoerper; Sweeps und Bodenabfragen stoppen vor Hindernissen und Kanten.
  Einstieg nur stehend und aufrecht
  auf freier, ebener Flaeche. Unebene Boeden, Stufen, Abgruende und Wasser sind
  bewusst noch kein unterstuetzter Bewegungsbereich.
- Fokusverlust und Spielpause halten die Verwandlung an. F7 fordert Rueckkehr
  und anschliessenden normalen Ausstieg an. Tod, Entfernen und Mod-Abbau stellen
  gespeicherte Rigidbody-, Collider-, Renderer-, Licht- und Fahrerzustaende her.

## Reproduzieren

Die vorhandene V18-Datei und ihr zur Datei passender bestandener Audit sind
Voraussetzung. Blender-Prozesse einzeln mit zwei CPU-Threads starten:

```text
blender --background --threads 2 --python tools/export_transformer_runtime.py
python tools/build.py
python tools/test.py --skip-build
```

Export: `.local/transformer/transformer.json` und `transformer.bin`.
Build: `artifacts/mod/Rachel350Z.dll` mit `artifacts/mod/transformer/`.
Der Build installiert nichts und aendert keine Spielkonfiguration.

Die optionale Offline-Laufkontrolle verwendet die V18-Geometrie und spiegelt
die Laufberechnung zum visuellen Pruefen. Sie ersetzt keinen Unity-Test:

```text
python tools/build.py
python tools/test.py --skip-build --suite transformer
blender --background --threads 2 --python tools/check_transformer_locomotion.py
blender --background --threads 2 --python tools/check_transformer_gait_trace.py
python tools/transformer_locomotion_review.py
python tools/build.py
blender --background --threads 2 --python tools/render_transformer_locomotion.py -- --animation --preview-seconds 8
```

Die kurze MP4-Vorschau zeigt die ersten acht Sekunden mit Anlaufen, vollen
Schritten und Abbremsen. Die gespeicherte Blender-Datei enthaelt weiterhin
den kompletten 34-Sekunden-Controllerablauf; ohne `--preview-seconds` rendert
das Werkzeug auch die ganze Sequenz als Video.

## Spaeterer lokaler Spieltest

Erst nach gesicherter Installation des Teststands samt passendem `transformer`
Unterordner in der BepInEx-Konfiguration aktivieren:

```ini
[TransformerPrototype]
Enabled = true
ToggleKey = F10
```

Im Einzelspieler in den 350Z einsteigen, Einstieg abschliessen, alle Klappen
schliessen und auf einer freien ebenen Flaeche anhalten. F10 verwandelt.
Die Maus bzw. RS am Gamepad bewegt die Kamera. W/S geht vor/zurueck, A/D dreht;
am Gamepad RT/LT und linker Stick. Die kurze Vorbereitung der Kniepanzerung
vor dem ersten Schritt ist beabsichtigt. Fuer den Rueckweg F10 oder F7 druecken;
der Roboter bremst und setzt die Fuesse ab, bevor er sich zusammenfaltet.
Waehrend der Roboterphase sind Fahrzeugmenue, Flug, Nitro, Geschuetz und
Hordenmodus nicht verfuegbar.

Vor Ersetzen einer installierten Mod: Valheim schliessen, Backup erstellen,
Kopierpruefsummen vergleichen; erst danach alte zeitgestempelte Backups auf
die zwei neuesten reduzieren. Export und Build installieren nichts. Der lokale
V3-Lauf-Teststand wurde separat mit dem bestehenden Installerkern, Backup und
SHA-256-Vergleich installiert. Die Transformerdateien bleiben eine optionale
Ergaenzung; die bestehende Konfiguration wurde unveraendert erhalten.
Die Spielabnahme von Gehen/Drehen steht noch aus.

## Grenzen der bisherigen Pruefung

Die Managed-Tests pruefen Import, ungueltige Daten, Moduswechsel und bestehende
Fahrzeugfunktionen. Sie fuehren weder Unity-Skinning noch Physik oder den
nativen Player-Animator aus. Der Laufcheck prueft geometrische Zwischenposen
des Blender-Modells; aktuelle Rad-/Auspuffvarianten und Avatar-Ausruestung
brauchen eigene Spieltests. Innere Leitungen sind noch starr gruppiert.

Alte Laufstudien und Fitter bleiben fehlgeschlagene Entwicklungskandidaten.
V3 verwendet `transformer_gait_v3.json`. Die Freigabe braucht einen bestandenen
Verhaltenstest des echten Managed-Controllers sowie die vollstaendigen Geometrie-
und Trace-Pruefungen einschliesslich IK-Reichweite und Beinlaengen. Die Vorschau
spielt die tatsaechlichen Fuss- und Koerperkanaele ab; sie schaetzt V3 nicht aus
Phase und Tempo nach. Ein Quellen- oder Trace-Wechsel macht die Freigabe ungueltig.
Die erneute Spielabnahme steht aus; Offline-Pruefungen ersetzen sie nicht.
Der aktuelle V3-Stand besteht 41 Managed-Checks, die komplette Geometriepruefung
mit 4.485 Posen sowie 2.041 aufeinanderfolgende Controllerposen ohne neue
Oberflaechenkontakte, Bodenverletzungen oder IK-Fehler. Die originale Ruhepose
bleibt erhalten; Kniepanzer klappen auf ihren Haltern aus dem Bewegungsweg,
und die sieben Neonroehren bleiben ganze, verbundene Bauteile.
Die Freigabe ist an die SHA-256-Werte dieser Quellen gebunden und wird bei
fehlender/veraenderter Pruefung nicht automatisch erneut aktiviert.
Bekannte Ueberlappungen der geschlossenen Standpose sind im Audit ausgewiesen.
Beim Herausfahren werden nur diese vorhandenen Kontaktpaare toleriert, ohne
Zunahme ihrer gemessenen maximalen transversalen Dreieckstiefe (1,5 mm Toleranz).
Fuer Gehposen werden neue Dreieckskontakte gegen die Standpose geprueft.
Das ist eine diskrete Oberflaechenpruefung, keine vollstaendige Volumensimulation.

Offen fuer die erste Spielabnahme: Uebergang der echten Kabinenpose und des
offenen Fahrerfensters, Material-/Lichtwirkung, Performance beim ersten
Aufbau, SkinnedMesh-Bounds, Standfussruhe beim Anhalten/Drehen, Kontakte der
inneren Mechanik, fremde bewegte Hindernisse waehrend der Verwandlung sowie
Kamera/Notausstieg/Tod/Despawn mit dem nativen Avatar. Kein Multiplayer,
Kampfmodus, Tanz, Musikritual oder weitere Idee aus den Scratch-Notizen.
