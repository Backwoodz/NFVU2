# Lokaler Transformer-Prototyp

Aktuell: Reaktor 120 Grad/s. Die groessere Flamme startet vor dem Einfassungsrand
und verwendet zur Kamera gerichtete Sprites fuer bessere Frontalansicht.
Lose Beute und Truemmer der nativen Baumzerstoerung werden jetzt ebenfalls
bei Hindernis-, Boden- und Deckungsabfragen ignoriert, ohne die Beute zu loeschen.
Diese eigenstaendigen Collider hatten keine Baum-Elternobjekte mehr und konnten
weiterhin abbremsen oder als falsche Bodenstufe wirken. Verbleibende feste
Bewegungsblocker stehen gedrosselt im Spiel-Log. Build und 18 Kontaktregeln-Checks
bestanden; Spielabnahme fuer Sichtbarkeit und ungebremstes Durchlaufen offen.

Nachkorrektur: Reifen rollen direkt um die Achse des Original-Radmeshes,
nicht um den anders orientierten Panelhalter. Der Reaktor dreht mit 65 statt
38 Grad/s und verwendet eine sichtbare, kleine Flamme aus dem vorhandenen
Auspuff-Flammenatlas. Baum-/Stammcollider bremsen den Roboter nicht mehr ab,
waehrend die native Faellung noch laeuft. 14 Kontaktregeln-Checks bestanden;
Rad-/Flammendarstellung und ungebremstes Durchlaufen brauchen Spielabnahme.

Neu fuer den lokalen Spieltest: Sprintschritte rund 9 % kuerzer (1,32 m
statt 1,45 m), Tempo bleibt 8,5 m/s. Die vier vorhandenen Raeder rollen beim
Drehen links/rechts gegenlaeufig; die Bremssaettel drehen nicht mit. Die
Reaktorkuppel wird zur konkaven Mulde mit kleinem, dezent leuchtendem Kern,
im Uhrzeigersinn rotierender vorhandener Einfassung und kleiner Plasmaflamme.
Kleine feindliche Gegner und native Baeume/Staemme/Setzlinge erhalten bei
Bewegung Kontakt-Schaden ueber Valheims eigenes System. Spieler, Zahmtiere,
Bosse, grosse Kreaturen und Gebaeude bleiben geschuetzt; feste Deckung blockiert
Treffer. 30 Controller- und 14 Kontaktregeln-Checks, keine Unity-Laufzeitabnahme.

Aktueller lokaler Spielteststand: 8,5 m/s, laengere/höhere Schritte mit
laengerer Flugphase und enger gefuehrten Armen. Die neunfache Bodenpassung
und Panzer-Kollisionssperre werden fuer Bewegung nicht mehr verwendet;
der Rumpf folgt geladener Bodenhoehe und ein kleiner zentraler Collider prueft
feste Hindernisse. Bestehende Ueberlappungen erlauben Bewegung zum Befreien.
F7 steigt sofort aus, F10-Rueckwandlung hat keine Hang-/Freiraumsperre.
Robotermeshes werden beim Fahrzeugladen ueber mehrere Frames vorbereitet,
statt beim ersten F10 aufgebaut. Die sehr grosse synchrone Umbauwegpruefung
entfaellt. Die Bauch-Neonroehre samt Haltern wird im Robotermodell ausgeblendet.
Build und kurze Controller-/Ladetests bestanden. Auf Nutzerwunsch wurden
keine neuen Vorschauen oder langen Geometriepruefungen erzeugt; die unten
beschriebene vollstaendige Freigabe gilt fuer den vorherigen Stand.

Ausgangspunkt ist das vorhandene 350Z-Modell und die freigegebene
V18-Animation. `scratch_3.txt` ist eine Ideensammlung und kein Arbeitsauftrag.
Nutzer bestaetigt Standform und grundlegendes Gehen im Spiel. Gezielte
Spielabnahmen fuer Physik, Untergrund und neue Bewegungsvarianten bleiben offen.

**Aktiver Umfang:** Verwandlung, derselbe Fahrer am Nacken, Kamera,
Vor-/Rueckwaertsgehen, Sprinten, Drehen und Rueckverwandlung. Neue Bewegungsvarianten
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
- V4 ergaenzt einen Sprint bis 7 m/s mit eigener Beschleunigung und staerkerem
  Abbremsen. Der Uebergang folgt der tatsaechlichen Bewegung: laengere Schritte,
  kuerzere Stuetzphase, kurze Flugphasen, gebeugte
  Ellbogen und Vorneigung. In schnellen Kurven sinkt die Drehgeschwindigkeit.
  Die Parameter sind eine Anpassung an dieses Rig und die Spielsteuerung.
- Kleine Sprintkorrektur nach Spieltest: Armschwungziel 34 statt 48 Grad,
  Winkeltempo maximal 180 statt 290 Grad/s; Schulterabspreizung beim Sprint
  22 statt 30 Grad. Der bestehende Schritt-/Fussablauf bleibt erhalten.
  Beckenkompression und Abhebung folgen dem tatsaechlichen Stuetzkontakt,
  mit rund 15 cm Hub und einer niedrigeren Basis fuer feste Beinlaengen.
- Die vorderen Knieplatten parken waehrend der stationaeren Vorbereitung auf
  ihren Rumpftraegern. Sie bleiben dort bei allen Geschwindigkeiten, auch wenn
  beim Bremsen noch ein hoher Sprintschritt ausklingt. Erst nach Fussruhe fahren
  sie auf demselben Weg zurueck; Tempo und Panzerfreiraum steuern nicht mehr
  gegeneinander dieselbe Baugruppe.
- Beschleunigung und Abbremsen bleiben vom Fahrzeughandling getrennt.
  Die Parameter stehen in `src/TransformerGait.cs`,
  `tools/transformer_gait_v3.json` und `tools/transformer_sprint_v4.json`.
  Der aktuelle Stand basiert auf 0.30.0 samt
  Weltladen und dauerhaft gespeicherten Fahrzeugen. Speichern im Robotermodus
  verwendet eine sichere Auto-Rueckkehrhoehe; Neustart stellt ein Auto wieder her.
- Kamera folgt der Hoehe. Ein schmaler Rumpf-Collider und Abfragen der einzelnen
  bewegten Bauteile ersetzen die bisherige grosse Freiraumbox. Leere Bereiche
  zwischen Armen, Beinen und Karosserie sind dadurch kein pauschales Hindernis.
  Ganze Verwandlungsframes und die Bewegung zwischen ihnen werden abgefragt.
  Die nativen Unity-Abfragen brauchen weiterhin einen Spieltest.
- Einstieg im Stillstand auf tragfaehigem Boden; leichte Neigung bis 8 Grad
  ist vorgesehen. Neun Bodenproben pruefen Neigung und Unebenheit gemeinsam,
  mit maximal 6 cm Restabweichung von der angepassten Ebene. Zusaetzlich werden
  10 cm Hoehenunterschied zwischen den Fussbahnen geometrisch geprueft.
  Fuer das Gehen des bereits aufgebauten Roboters gelten getrennte tolerantere
  Grenzen: 20 Grad, maximal 25 cm Ebenenrestfehler und 30 cm Spanne der
  Restfehler. Hoehenspruenge zwischen Bewegungsschritten sind ebenfalls begrenzt.
  Bodennahes Fuss-/Schienbeinmaterial samt dessen Anbauten darf bis 22 cm in
  Heightmap-Gelaende tauchen; obere Bauteile und feste Fels-/Wandcollider erhalten
  diese Ausnahme nicht. Kleine Fussueberschneidungen sind bewusst akzeptiert.
  Die Lockerung betrifft Heightmap-Gelaende. Bei festen Box-/konvexen
  Mesh-Rampen bleibt die konservative Bodenfreigabe auf etwa 15 Grad begrenzt;
  steilere gebaute Rampen koennen weiterhin anhalten.
  Die bisherige 8-Grad-Grenze fuer alles entstand aus einer zu strengen Forderung
  nach vollstaendig bodenfreien Neonendkappen (bei 10 Grad bis 8,2 mm Einschnitt).
  Die Exposure-Normierung der Freiraumbewegungen bleibt bei 15 Grad bzw.
  20 cm; die bisherigen vollstaendigen Geometrieprueffaelle verwenden 8 Grad
  und 10 cm. Die erweiterten Gehgrenzen werden separat als Bodenpolicy geprueft;
  native Unity-Physik und das Gefuehl auf echten Huegeln brauchen einen Spieltest. Eine
  Umnormierung aendert die Kinematik und erfordert eine neue Abnahme.
  Der Originalumbau folgt zunaechst der tragenden Gelaendeebene. Danach oeffnen
  die bestehenden Panzertraeger, bevor sich der Roboter in 0,4 Sekunden
  aufrichtet und seine Sohlen am Boden ausrichtet. Auf Gelaende bleibt diese
  Freiraumstellung auch nach dem Anhalten erhalten. Die Rueckkehr wartet erst
  auf Fussruhe, neigt den Koerper zur Bodenebene zurueck und schliesst dann die
  Panzer; erst danach beginnt der inverse Originalclip. Die Reihenfolge vermeidet
  ein Verbiegen der noch geschlossenen Karosserieteile durch die Boden-IK.
  Groessere Stufen, Loecher, Abgruende und Wasser bleiben ausserhalb des
  unterstuetzten Bereichs.
- Erweiterter Geh-Smoke: 42 Posen auf vier 20-Grad-Haengen und zwei
  30-cm-Fusshoehenunterschieden. Alle erlauben die begrenzte Runtime-
  Bodentoleranz, Haende bleiben mindestens 28,9 mm frei. Vier Posen
  zeigen noch einen Schwellerkontakt (4,5 mm) beziehungsweise Neon-/Hinterrad-
  Ueberschneidungen (47 bis 59 mm) auf 20-Grad-Haengen. Das sperrt die Bewegung
  nicht, ist aber eine verbleibende optische Grenze. Der angewandte Koerperhub
  nach der Reichweitenbegrenzung betraegt 14,95 cm.
- Die Hindernispruefung erfasst auch 129 vorbereitende und aufrichtende Posen
  aus derselben Laufzeitberechnung samt echten hypothetischen Bodenabfragen.
  Das Sampling veraendert weder Spielobjekte noch den aktuellen Laufcontroller.
  Bei der Rueckkehr wird die aktuelle Standhoehe stetig auf die sichere
  Bodenreferenz ueberfuehrt; diese Hoehenbewegung ist in der Abfrage enthalten.
- Am Starttempel besitzt der zentrale Sockel eine ebene konvexe Kollisionsflaeche.
  Leere Ecken einer gedrehten Fuss-Pruefbox koennen darunter liegen, obwohl die
  Modellflaechen darueber bleiben. Diese Faelle werden anhand der wirklichen
  Vertices und einer tragenden konvexen Flaeche unterschieden; Steine und Waende
  werden weiterhin geprueft. Originale Spielassets bleiben unter `artifacts/`.
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
blender --background --threads 2 --python tools/render_transformer_locomotion.py -- --animation --clip-start 34 --clip-seconds 9
```

Die kurze MP4-Vorschau zeigt einen Ausschnitt des aufgezeichneten Controllers
mit Beschleunigung zum Sprint und Loslassen von Shift. Blender-Datei und Video
verwenden denselben gewaehlten Ausschnitt. Der komplette Controllertrace dauert
61 Sekunden; Metadaten halten Quellenpruefsumme und Zeitfenster fest.

## Spaeterer lokaler Spieltest

Erst nach gesicherter Installation des Teststands samt passendem `transformer`
Unterordner in der BepInEx-Konfiguration aktivieren:

```ini
[TransformerPrototype]
Enabled = true
ToggleKey = F10
```

Im Einzelspieler in den 350Z einsteigen, Einstieg abschliessen, alle Klappen
schliessen und auf einer freien, tragfaehigen Flaeche anhalten. F10 verwandelt.
Die Maus bzw. RS am Gamepad bewegt die Kamera. W/S geht vor/zurueck, A/D dreht;
Shift zusammen mit W beschleunigt zum Sprint (links oder rechts Shift);
am Gamepad RT/LT und linker Stick, zum Sprinten X/Square halten.
Die kurze Vorbereitung der Kniepanzerung
vor dem ersten Schritt ist beabsichtigt. Fuer den Rueckweg F10 oder F7 druecken;
der Roboter bremst und setzt die Fuesse ab, bevor er sich zusammenfaltet.
Waehrend der Roboterphase sind Fahrzeugmenue, Flug, Nitro, Geschuetz und
Hordenmodus nicht verfuegbar.

Vor Ersetzen einer installierten Mod: Valheim schliessen, Backup erstellen,
Kopierpruefsummen vergleichen; erst danach alte zeitgestempelte Backups auf
die zwei neuesten reduzieren. Export und Build installieren nichts. Der lokale
V4-Lauf-/Sprint-Teststand wurde separat mit dem bestehenden Installerkern, Backup und
SHA-256-Vergleich installiert. Die Transformerdateien bleiben eine optionale
Ergaenzung; die bestehende Konfiguration wurde unveraendert erhalten.
Die erneute Spielabnahme von Sprint, Handfreiraum und Terrain steht noch aus.

## Bewegungsreferenzen

Der Entwurf orientiert sich an den Phasen von Beschleunigung, Stuetzzeit und
Flugzeit aus [Nagahara et al. (2014)](https://pmc.ncbi.nlm.nih.gov/articles/PMC4133722/)
und an Untersuchungen zum [Armschwung beim Gehen und Laufen](https://pubmed.ncbi.nlm.nih.gov/31289110/).
Fuer die schwere mechanische Gesamtbewegung dient ausserdem
[Atlas von Boston Dynamics](https://bostondynamics.com/blog/leaps-bounds-and-backflips/)
als visuelle Referenz. Die konkreten Geschwindigkeiten, Winkel und Zeiten sind
unsere Spiel- und Rig-Anpassung; es wird keine physikalisch simulierte
Humanoidensteuerung behauptet.

## Grenzen der bisherigen Pruefung

Die Managed-Tests pruefen Import, ungueltige Daten, Moduswechsel und bestehende
Fahrzeugfunktionen. Sie fuehren weder Unity-Skinning noch Physik oder den
nativen Player-Animator aus. Der Laufcheck prueft geometrische Zwischenposen
des Blender-Modells; aktuelle Rad-/Auspuffvarianten und Avatar-Ausruestung
brauchen eigene Spieltests. Innere Leitungen sind noch starr gruppiert.

Alte Laufstudien und Fitter bleiben fehlgeschlagene Entwicklungskandidaten.
V4 ergaenzt `transformer_sprint_v4.json` zu den vorhandenen V3-Ausfahrwerten.
Die Freigabe braucht einen bestandenen
Verhaltenstest des echten Managed-Controllers sowie die vollstaendigen Geometrie-
und Trace-Pruefungen einschliesslich IK-Reichweite und Beinlaengen. Die Vorschau
spielt die tatsaechlichen Fuss- und Koerperkanaele ab; sie schaetzt V4 nicht aus
Phase und Tempo nach. Ein Quellen- oder Trace-Wechsel macht die Freigabe ungueltig.
Die erneute Spielabnahme steht aus; Offline-Pruefungen ersetzen sie nicht.
Der bisherige V3-Stand bestand 41 Managed-Checks, eine Geometriepruefung
mit 4.485 Posen sowie 2.041 aufeinanderfolgende Controllerposen ohne neue
Oberflaechenkontakte, Bodenverletzungen oder IK-Fehler im damaligen Pruefumfang.
Die vollstaendigen Haende waren darin noch nicht enthalten. V4 prueft beide
Haende einschliesslich saemtlicher Finger gegen Bein- und Hueftpanzer mit
mindestens 25 mm Abstand; die Handgeometrie bleibt erhalten.
Vier urspruengliche starre Lager-/Bolzenmontagen am eigenen Unterarm behalten
ihren Originalspalt. Fuenf Mikrometer Vergleichstoleranz beruecksichtigen dort
die Float32-Rekonstruktion zweier gemeinsam bewegter Teile; Finger, Palmen und
Bein-/Hueftpanzer erhalten keine solche Montageausnahme.
Die aktuelle V4-Freigabe umfasst 79 Managed-Transformerchecks, 34 Bodenchecks,
115 Python-Tests sowie 5.930 Geometrieposen und 3.661 aufeinanderfolgende
Controllerposen eines 61-Sekunden-Ablaufs. Im beschriebenen Pruefumfang keine
neuen Koerper-/Handkontakte, Bodenverletzungen oder IK-Fehler; kleinster absolut
gepruefter Handabstand 28,9 mm. Die Geometriepruefung schliesst 356 Terrainposen
ein. Ein frischer Build vor dem Verhaltenstest und ein identischer DLL-SHA-256
nach Freigabe verbinden den tatsaechlich getesteten binaeren Stand mit der
lokalen Installation.
Die im Originalclip kompakt gefalteten Haende werden vor der vollstaendigen
Roboterpose gegen exakt denselben flachen V18-Frame verglichen: vorhandene
Kontaktpaare duerfen nicht tiefer werden, neue Kontakte sind unzulaessig.
Das ist keine Ausnahme fuer fertiges Stehen, Gehen oder Sprinten. Die
Bodenpruefung bleibt auch waehrend dieser Verwandlungsphasen absolut.
Die originale Ruhepose
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
