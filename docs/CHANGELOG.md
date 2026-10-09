# Versionsgeschichte

## 0.19.0 — Scheinwerfer-Show nach dem Aussteigen

- F9 → Lichter: „Show-off beim Aussteigen“, standardmäßig aus, für 350Z und H2
  separat gespeichert. Nach dem Aussteigen zweimal links, zweimal rechts,
  dann Pause; 2,4-s-Zyklus mit vier 160-ms-Blitzen. Links/rechts aus Fahrersicht.
- Lichtkegel und leuchtende Lampenflächen folgen derselben Phase. Seiten aus
  Fahrzeugkoordinaten statt spiegelverkehrter NFS-Teilenamen bestimmt.
- Einsteigen beendet die Show und aktiviert normales Fahrlicht. Abschalten
  stellt den normalen Lichtzustand wieder her; erneutes Aktivieren startet links.
  Pause/Fokusverlust friert die Phase ein. Eigenständiger Zustand pro Fahrzeug.
- Farbe und Stärke gelten auch für die Show; Motor-/Radio-Weiterlaufen ist
  unabhängig. Brems-/Rückfahr-/Rücklicht unverändert. Reset schaltet Show-off aus.
- Automatische Ablaufprüfungen; sichtbare Lichtkegel, Emission und Menü im Spiel prüfen.

## 0.18.4 — Metallic-Lack

- Lackflächen werden beim Import ausdrücklich markiert. 350Z-Lack/Vinyl und
  gelbe H2-Karosserie erhalten ein frisches Material mit Metallic 0,72 und
  Smoothness 0,88; geladener Standard-Shader mit geprüftem nativem Fallback.
  Keine geerbten Gebäude-Normal-/Glanzmasken. Farbe und Vinyl bleiben erhalten.
  Carbonhaube, Scheiben, Innenraum und Felgen behalten ihre eigenen Einstellungen.
- Importzuordnung automatisch geprüft; Glanz und Reflexionen müssen bei
  Sonne/Schatten/Nacht im Spiel geprüft werden.

## 0.17.0 — Nitro-Tunnelblick und Rücklichtspuren

- Sechs radial versetzte Bildlagen verwischen beim tatsächlichen Nitro-Boost
  den Bildrand. Ein weicher Schutzbereich hält die Fahrzeugprojektion und
  die Bildmitte scharf; HUD/Overlay werden danach gezeichnet. GPU-Kopie in
  halber Auflösung, keine CPU-Bildrücklesung. Filter nur während Boost/Fade aktiv.
- Zwei kurze rote Lichtbänder folgen den Rücklichtpositionen. Weicher Querschnitt,
  verjüngtes Ende und ungefähr drei Meter Maximallänge. Tiefentest bleibt aktiv.
  Nur bei eingeschalteten Rückleuchten und tatsächlichem Boost, auch im Flug.
- F9 → Nitro: Tunnelblick und Rücklichtspuren separat abschaltbar; gemeinsame
  Stärke 0–100 %, Standard 70 %. Gemeinsame Einstellungen für beide Fahrzeuge.
  Menü, Pause, Fokusverlust, Ausstieg, Welt-/Kamerawechsel räumen Effekte auf.
- Eigene UVs, Gewichtung, Maske und Spurzeiten; keine unveränderte Übernahme
  der NFS-Shader. Die ergänzend geprüfte yugecin-Quelle liefert Grafikschalter
  und Parameternamen, keine vollständige Nitro-/Spur-Implementierung.
- Keine Änderung von Fahrzeugsteuerung, Nitro-Schub, Tank oder Auspuffeffekten.
  Automatische Prüfungen ersetzen keine Unity-Darstellungs-/GPU-Messung.

## 0.16.1 — Auspuff-Nitro auch im Flug

- Die gemeinsame Effektsperre enthielt bisher den gesamten Flugmodus und
  unterdrückte dadurch die vorhandenen 350Z-Auspuffflammen und Nitro-Sounds.
  Beide folgen jetzt dem aktiven Nitro-Zustand auf Boden und im Flug.
  Ohne tatsächlichen Boost bleiben sie aus; die vier Schwebe-Düsen sind unabhängig.
- Reifenrauch, Purge und Schubknallen bleiben im Flug gesperrt. Menü,
  Fokusverlust, Ausstieg und noch nicht abgeschlossener Einstieg sperren
  weiterhin die Auspuffeffekte. Keine Änderung von Schub, Tank oder Fahrwerk.
- 41 Nitro- und 37 Flugprüfungen bestanden; Partikel und Sound benötigen
  weiterhin den Unity-Spieltest.
- NFSU2-Bildfilter und Lichtspuren untersucht: getrennte Grafikoptionen,
  eingebettete D3D9-Filter und lokale Flare-Textur nachgewiesen. Die genaue
  Nitro-Kopplung, Filtergewichte und Trail-Historie bleiben unbestätigt.
  Quellen und Grenzen in REFERENCES; noch kein neuer Bildschirmfilter eingebaut.

## 0.16.0 — Fahrzeuge im Hintergrund vorbereiten und schrittweise spawnen

- Bisher liefen Dateilesen, JSON-Auswertung, Textur-Uploads, Mesh-Aufbau und
  Audioerstellung zusammen im Spawnaufruf. Der optionale Innenraum ergänzt
  rund 24 MB JSON. Modelldaten und PCM-Sounds werden nun auf einem Worker
  vorbereitet; der JSON-Reader liest aus dem Stream statt einer zusätzlichen
  vollständigen Dateikopie als String.
- Ladeanzeige mit aktuellem Abschnitt und animiertem Balken beim Einlesen.
  Aufbau von Karosserie, Rädern, Innenraum, Turret, Tacho und Motorclips in
  kleinen Arbeitsschritten mit etwa 3 ms Budget je Update. Einzelne Unity-
  Aufrufe können das Budget überschreiten; keine harte Framerate-Garantie.
- Unveränderte verwaltete Modell-/Sounddaten werden innerhalb derselben Welt
  wiederverwendet. Keine geteilten veränderbaren Unity-Materialien oder Meshes.
  Dateigröße/Änderungszeit invalidieren den Cache; Weltwechsel gibt ihn frei.
- Das Auto bleibt bis zum Abschluss inaktiv und außerhalb der Fahrzeugliste.
  Spawnplatz wird vor Freigabe erneut geprüft. F8/Abbrechen, Tod, Teleport,
  Weltwechsel und Ladefehler entsorgen Teilressourcen. Bereits vorhandene
  Fahrzeuge bleiben unabhängig; parallele Spawns und Einstieg während Laden
  werden gesperrt. Fehlende optionale Module behalten ihren bisherigen Fallback.
- Automatische Prüfungen für Hintergrundladen echter Assets, Cache/Invalidierung,
  Abbruch, Fehler und Ressourcen-Finalisierung. Im .NET-Test etwa 1,4 Sekunden
  beim ersten Einlesen gegenüber etwa 4 ms aus dem Cache; das sind keine Unity-
  Frametimes. Log ergänzt Gesamtdauer und längsten Hauptthread-Arbeitsschritt.
- Plugin-ID, DLL-Name und VehicleHandling bleiben erhalten. Spieltest offen.

## 0.15.2 — Nitro-Eingabe, Flammenanschluss und Reifenmaterial

- Flammenansatz von 48 auf 26 cm Abstand zum Radmittelpunkt korrigiert;
  die nicht gestreckten Partikel bleiben erhalten.
- Nutzer meldet weiterhin fehlendes Flug-Nitro. Installierte 0.15.1 und aktive
  Nitro-Konfiguration bestätigt; im bisherigen Log fehlen Eingabedaten.
  Zusätzlich zu Unity-Legacy-Mouse wird nun Valheims vorhandenes
  `ZInput.GetMouseButton` gelesen. W + linke Strg als Flug-Alternative,
  sichtbarer Boost-/Tank-/Loslassstatus und begrenzte Diagnoseausgaben.
  Der konkrete Laufzeitfehler ist damit noch nicht als reproduziert behauptet.
- Originaler 128×128-Racing-Hart-Atlas enthält Reifenprofil und Seitenwand;
  Textur und UV-Zuordnung waren vorhanden. Die 350Z-Gummifarbe multiplizierte
  diese auf 16 % herunter. Jetzt 80 %, nicht metallisch und rau, mit frischem
  Material statt geerbter Prop-Einstellungen. Keine neuen Reifenassets.
- Tests prüfen native Eingabe bei ausgefallenem Legacy-Mauswert, Tastaturalternative,
  Loslass-Sperre, Flugschub sowie bestehende Bodensteuerung; Reifen gerendert.
  Tatsächliche Eingabe und Effektposition benötigen den Spieltest.

## 0.15.1 — Flugtempo, Schubrichtung, Luft-Nitro und Tachonadel

- Nutzer bestätigt den Flugmodus. Steigen jetzt bis 12 m/s, manuelles Sinken
  bis 16 m/s, automatische Landung bis 10 m/s mit Abbremsen vor dem Boden.
  Die frühere Zwei-Meter-Zielbegrenzung drosselte die vertikale Geschwindigkeit.
- Felgen-Sichtseite beim Klappen nach oben gedreht. Räder, Bremsen und Spinner
  neigen sich mit dem Schub. Vorwärts zeigt die Flamme nach hinten, rückwärts
  nach vorne, beim Lenken zur Gegenseite; beim Loslassen gleitet die Neigung zurück.
- Düsen unter die Räder versetzt; normale statt gestreckter Partikel vermeiden
  die rückwärts über den Austritt hinausragenden Flammensprites. Emission startet
  erst gegen Ende des Umklappens, damit keine Flammen durch den Aufbau laufen.
- W + linke Maustaste aktiviert Nitro im Flug: höheres Vorwärtstempo und mehr
  Schub, Tankverbrauch und Nachladung. Gemeinsamer Boden-/Flugtank beim 350Z;
  H2 mit eigenem Flug-Nitro. Keine zusätzliche Boden-Nitrofreigabe bei Sprüngen.
- Deutlicher Drehzahlzeiger über den übrigen HUD-Ebenen und zusätzliche analoge
  Geschwindigkeitsanzeige mit passender KM/H-/MPH-Skala, neben dem NFS-Instrument.
- Build und gezielte Flug-, Nitro-, HUD-, Fahrwerks-, H2- und Audioprüfungen;
  HUD-Vorschau gerendert. Neue Fahr- und Effektwirkung noch in Unity prüfen.

## 0.15.0 — Flugmodus mit umklappenden Rädern

- F9 → Fahrzeug → Fliegen für 350Z/H2: animierte 90°-Radklappung, sanftes
  Abheben, Schweben mit Positionshaltung. W/S vor/zurück, A/D drehen,
  Shift steigen, Leertaste sinken. Vorwärts maximal etwa 65 km/h.
- Vier blaue Rad-Düsen aus dem vorhandenen NFS-FIREB-Atlas; tiefer abgestimmter
  lokaler Nitroloop mit Schubmodulation und Purge-Sound als optionalem Fallback.
  Keine neuen Originalassets, eigene Flugregelung und Animationsabläufe.
- Separater Flugcontroller übernimmt Kräfte und deaktiviert temporär die
  WheelCollider. Der bewährte VehicleHandling-Controller bleibt unverändert;
  Gravitation, Dämpfung und Radcollider werden bei Landung/Abbau zurückgesetzt.
- Auswahl eines Bodenmodus startet eine kontrollierte Landung mit anschließendem
  Ausklappen. F7 und Menü-Ausstieg warten darauf; letzterer nutzt wieder den
  regulären Ausstiegsablauf. F8/Tod/Weltwechsel haben weiterhin einen Abbaupfad.
- Nitro/Burnout und Reifengeräusche sind im Flug gesperrt. Menü/Fokusverlust
  neutralisieren Eingaben; Schwebestabilisierung bleibt aktiv. Jeder Wagen
  besitzt seinen eigenen Flugzustand; neue Fahrzeuge starten am Boden.
- Neue Zustands-/Controllerprüfungen mit kontrollierten Physikantworten.
  Tatsächliches Flugverhalten, Kollisionen, Partikel und Audio benötigen Unity.

Build, vollständiger Testsatz (49 Python-Tests und 546 C#-Prüfungen einschließlich
32 Flugprüfungen) sowie Radgeometrie-Vorschau geprüft. Keine Unity-Laufzeittests.

## 0.14.3 — Polierte Chrom-/Goldfelgen

Der Felgenrand war nur 28 % metallisch, der Stern 22 %. Eine zusätzliche
orange-goldene Farbe multiplizierte die bereits goldene Originaltextur.
Beide Flächen verwenden nun vollständig metallische Materialien mit hoher
Glätte; der Stern erhält nur eine leichte kühlere Korrektur der Originalfarbe.

Die Felgen verwenden ein frisches Standard-Material, sofern dessen Shader
verfügbar ist, sonst eine frische Instanz des vorhandenen Fahrzeugshaders.
Keine geerbten Prop-Texturen oder Eigenleuchtung; Reflexionen und Glanzlichter
sind aktiviert. Die Rad-Renderer nutzen Valheims vorhandene Reflection Probes.
Die Shaderauswahl steht im Log. Geometrie, Reifen und Fahrphysik bleiben gleich.
Build sowie 142 Modell-, Innenraum- und H2-/Garagenprüfungen bestanden;
Felgendetail zusätzlich in Blender gerendert. Die genaue Wirkung unter
Valheim-Beleuchtung benötigt einen Spieltest.

## 0.14.2 — Rotstich der Scheibentönung

Die bisherige Materialauswahl konnte `lavalanternglass_mat` bzw. dessen
Bruchvariante kopieren. Beide enthalten in den lokalen Spielassets eine rote
Eigenleuchtfarbe und das aktive Shader-Schlüsselwort `_EMISSION`. Die Tönung
änderte nur die Grundfarbe und ließ diese Eigenleuchtung erhalten.

Das Fenstermaterial verwendet jetzt ein frisches Material mit der geladenen
Shaderreferenz; fremde Texturen, Farben und Schlüsselwörter werden nicht mehr
kopiert. Eigenleuchtung wird zusätzlich explizit abgeschaltet. Tönungsfarben
sind neutral grau. Die gewählte Shaderreferenz wird zur Diagnose protokolliert.
Build und Innenraumprüfungen bestanden; Darstellung im Spiel noch prüfen.

## 0.14.1 — Fahrerhaltung, offenes Fenster und Scheibentönung

- Nutzer bestätigt Innenraum und Ein-/Ausstieg von 0.14.0; Kopf ragte durch
  das Dach, Hände saßen zu weit vorne und die Lenkanimation war umgekehrt.
- Sitzposition abgesenkt und etwas zurückversetzt; zusätzliche Begrenzung
  anhand des Kopfknochens mit Reserve für Kopf/Haar unter dem Dach.
- Rechte Hand mit zurückversetztem Handgelenk am Lenkrad, korrigierte
  Drehrichtung und Handorientierung. Linker Arm ruht am offenen Fenster;
  beim Öffnen der Tür bzw. Aussteigen wird er nach innen geführt.
- F9 → Türen: gespeicherte Scheibentönung 0–100 %. Die physisch linke
  Seitenscheibe wird beim Sitzen ausgeblendet und beim Aussteigen wieder sichtbar.
- Linke Tür nach dem Einstieg wieder manuell bedienbar: Menüknopf entsperrt,
  Sitzanimation überschreibt den Türzustand nicht mehr. Türbewegung ist im
  Menü möglich, sobald der Fahrer sitzt; Ein-/Ausstiege behalten ihre Sperre.
- Build und gezielte Fahrer-, Tür- und Fahrwerksprüfungen bestanden.
  Neue Pose, Scheibenwirkung und Ausrüstung benötigen erneuten Spieltest.

## 0.14.0 — Lokaler Innenraum und sichtbarer Fahrer

- Optionaler lokaler BeamNG-350Z-Innenraumimport: Cockpit, zwei Sitze, Pedale,
  Schalthebel und an den NFS-Türen befestigte Innenverkleidungen. Separate
  Lenkraddrehung, transparente Fenster mit verfügbarem Spielshader.
- F7 im Stand: Weg zur Fahrertür, Öffnen, Übergang in Valheims Sitzanimation,
  Hände am Lenkrad und Füße an den Pedalen, Schließen und Fahrfreigabe.
  Rückweg beim Aussteigen; freie Bodenfläche und Platz für den Spieler nötig.
- Eingaben/Nitro/Turret während des Übergangs gesperrt. Menü/Fokusverlust
  pausiert den Ablauf; Entfernen, Fehler und Weltwechsel stellen den Spieler wieder her.
- Native Sitzanimation plus eigene Übergänge/Gliedmaßenausrichtung, keine
  übernommene BeamNG-Charakteranimation. Visuelle Sitzskalierung auf 85 %.
  H2 und bestehende Fahrwerksabstimmung bleiben unverändert.
- Import, Modelldaten und Zustandsabläufe automatisiert geprüft; externe
  Modellvorschau geprüft. Unity-Spieltest einschließlich Kleidungsüberschneidungen offen.
- Originalautor des bereitgestellten Modland-Archivs unbekannt. Assets bleiben
  lokal in ignorierten Ordnern; keine Weiterveröffentlichungsfreigabe behauptet.

## 0.13.1 — Verschiebbares Fahrzeugmenü

F9-Menü per linker Maustaste an der Titelleiste verschieben. Die Position wird
gespeichert und bei Auflösungswechsel auf den sichtbaren Bildschirm begrenzt.
Schließen/Fokusverlust beendet das Ziehen. Schließen-Button und Inhaltsregler
bleiben unabhängig bedienbar. Die bestehende Fahrzeugbindung der Kategorien
bleibt erhalten: Ohne Fahrzeug ist nur die Garage aktiv.
Build und Menütests geprüft; tatsächliches Unity-Ziehen noch im Spiel prüfen.

## 0.13.0 — Tieferlegung, NFS-Menü und Parkautomatik

- F9 → Räder: zusätzlicher Tieferlegungsregler 0–12 cm mit 0,5-cm-Schritten,
  für H2/350Z getrennt gespeichert; physischer Versatz der Aufhängungspunkte.
  Serienhöhe beim Zurücksetzen, Feder-/Dämpfer- und Fahrmodusabstimmung erhalten.
- Vorderräder behalten beim Aussteigen ihren tatsächlichen Lenkwinkel.
  Wiedereinstieg setzt die geglättete Lenkung fort; Parkbremse bleibt aktiv.
- Neues F9-Layout mit Kategorienleiste, Original-NFSU2-Icons, ausgewählten
  deutschen Originaltiteln, Tastaturnavigation und drei Frontend-Sounds.
  Lokaler Import mit `tools/export_menu.py`, optionaler Medienordner `menu/`.
  Layout/Aktionen und Soundrollen sind angepasst; keine NFS-Menüemulation.
- Aktivierter 350Z-Turret bewacht nach dem Aussteigen automatisch: sichtbare
  Lasersuche, vorderer 180°-Sektor, 30 m Reichweite, feindliche Kreaturen,
  Laufnachführung, kontrollierte Schusslinie und automatisches Nachladen.
  Schalter unter Turret; unabhängig vom Motor-/Radioweiterlauf. Laserfarbe,
  Stärke und Munition werden übernommen. Spieler/zahme Tiere sind keine Ziele.
- Menüs, Fokusverlust und Spielertod sperren Automatik; nach manueller Übernahme
  bleibt gehaltenes RMB bis zum Loslassen gesperrt. Jeder Turret hat eigene
  Suche, Munition und Bewegung. H2-Turret weiterhin nicht eingerichtet.
- Geprüft mit Build, Import-/Payloadtests, Fahrwerks- und Turret-Zustandstests
  sowie kontrollierten Sichtlinien-API-Antworten. Kein Unity-Spieltest.

## 0.12.0 — Hummer H2, Spinner und Garage

Originaler H2 aus den eigenen NFSU2-Dateien: Serienkarosserie in Gelb,
26-Zoll-SPINNER_STYLE01 mit separatem Originaleinsatz, 34 cm breite Reifen
und für den schwereren SUV angepasste Federung, Bremsen und Anfahrkraft.
Die Einsätze beschleunigen verzögert und laufen nach dem Anhalten aus.
Felgengröße 22–28 Zoll; Reifenaußendurchmesser bleibt erhalten.
Originale Radpositionen aus dem HUMMER-Global-Datensatz, NFVU2-Fahrphysik.
Rückleuchten mit passender Serien-Textur und getrennten roten/weißen Flächen;
die zwei Atlasbereiche des hinteren HUMMER-Schriftzugs werden gemeinsam gespiegelt.

Eigener Serien-Sound aus GIN_Hummer.gin: acht Drehzahlbereiche, 1000–4782 U/min
und vier akustische Vorwärtsgänge. Der originale Stock-Datensatz verwendet
keinen zweiten Ginsu-Schubkanal; die Mod nimmt denselben Hummer-Sound leiser
beim Gaswegnehmen. Keine umbenannten oder tiefer gepitchten 350Z-Aufnahmen.

F9 → Garage ist auch ohne Auto erreichbar. Modellauswahl und Spawnknopf;
F6 spawnt die gespeicherte Auswahl. Bis zu vier unabhängige temporäre Autos,
Freiplatzsuche nebeneinander, kein Ersetzen des vorherigen Autos.
F7/F9 wählen zu Fuß das nächste Auto, F8 löscht nur dieses. Während der Fahrt
bleibt die Auswahl beim Fahrerauto. Spawnen erst nach dem Aussteigen.
Modell-/Ladefehler beim Spawnen erhalten die bereits vorhandenen Autos.

Pro Auto getrennte Physik, Meshes, Materialien, Audio, Radio, Nitro- und
Turret-Zustände; zentrale Kamera-/Spielersteuerung und Musikdämpfung.
Licht, Radio, Räder und Fahrmodus haben separate H2-Konfigurationseinträge,
vorhandene 350Z-Einträge bleiben gültig. Autos desselben Modells teilen
Einstellungen. Kamera/Tacho bleiben gemeinsam. H2-Kamera höher und weiter weg.

Beim H2 zunächst kein Motorraum-/Kofferraumausbau, keine Türanimationen,
kein Nitro/Turret. Radio, Licht, Neon, Burnout und Aufprallsystem werden
wiederverwendet. Kein Speichern der Fahrzeuge, weiterhin lokaler Einzelspieler.
Automatische Prüfungen und Blender-Vorschauen ersetzen keinen Unity-Spieltest.

## 0.11.0 — Kräftiger, einstellbarer Laser und bewegte Subwoofer

Breiterer Laser mit hellem Kern und größerem Treffpunkt. Unter F9 → Turret →
Laser einstellen: 25–400 Prozent Stärke (Standard 200), sieben Farbvorlagen
und RGB-Regler. Farbe und Stärke werden gespeichert und gelten für Strahl
und Punkt; Hindernisse stoppen den Laser weiterhin.

Das Radio analysiert den Bassanteil der tatsächlich wiedergegebenen Samples
ohne sie zu verändern. Getrennte Kanalanalyse vermeidet Auslöschung bei
gegenphasigem Stereo. Die Basshüllkurve steuert eine sichtbare Membranschwingung
bis 14 mm Hub in beide Richtungen. Gehäuse und Außenrand bleiben fest.
Jeder Lautsprecherplatz besitzt eigene animierte Meshkopien; Verstärker und
LCDs bewegen sich nicht. Gilt auch für das Radio nach dem Aussteigen.
Pause, ausgeschaltetes Radio, Lautstärke null und ausbleibende Audiodaten
lassen die Bewegung ausklingen; Titelwechsel setzt die Auslenkung zurück.
Die Bewegung ist eine Bassvisualisierung, keine samplegenaue Lautsprechersimulation.

## 0.10.0 — Freie Zielsicht, Rust-Laser und Explosivmunition

Im Turret-Modus höherer Kameradrehpunkt, bei steilem Blick nach unten zusätzlich
angehoben. Visier bei 62 Prozent der Bildhöhe über der unteren Bildschirmkante;
der Lauf richtet sich auf den Weltpunkt dahinter aus. Orange zeigt Nachlauf,
Neigegrenze oder blockierenden Fahrzeugrumpf. Normale Fahrkamera bei abgeschaltetem
Turret unverändert. Munitionsart und Vorrat links unten.

Originales Rust-Lasersight-Entity-Modell samt beiden Materialbereichen und
Emitterpunkt lokal importiert und unter der AK montiert. Roter Strahl und
Treffpunkt folgen dem Lauf und enden an Hindernissen; im Menü abschaltbar.
F9 → Turret: Normal/Explosiv. Explosiv ergänzt den direkten Treffer um kleinen
Flächenschaden gegen Gegner mit Entfernungsabfall und Deckungsprüfung sowie
einen gepoolten Partikel-/Lichtblitz und Rusts `explosive-ammo-close`-Sound.

Jeder direkte Bodentreffer mit Explosivmunition senkt eine runde Fläche von
1,4 m Radius im Zentrum um bis zu 18 cm ab, entfernt Gras und speichert über
Valheims Terrain-Compiler. Wiederholte Treffer graben weiter bis zum normalen
Grablimit. Treffer auf Felsen, Gebäude oder Kreaturen graben nicht darunter.
Krater bleiben in der Welt, auch nachdem das temporäre Auto entfernt wurde.
Modelle/Samples original; Laserstrahl, Partikel, Wirkung und Krater sind NFVU2.
Zusätzliche Tests für Terrain-Treffer, Kachelgrenzen, Besitzer und Wiederholung.

## 0.9.1 — Zielmarkierung und Dachmontage korrigiert

Das Geschütz folgt der Kamerablickrichtung vom Lauf aus. Der bisherige
Bildschirmmittelpunkt zielte bei der Orbitkamera auf den Bereich des Autos.
Ein einziges, kontrastreiches Fadenkreuz zeigt nun den projizierten tatsächlichen
Auftreffpunkt; Nachlauf und Neigegrenzen bleiben sichtbar. Munition links unten.
Valheims eigenes Mittelkreuz und Bogenvisier werden während der Turret-Steuerung
ausgeblendet; beim Ausstieg, Abschalten und Entfernen wiederhergestellt.
Turret um 20 cm nach hinten und 7,5 cm tiefer versetzt, damit die Stützen in
der gekrümmten Dachfläche sitzen. Geometrievorschau aktualisiert.

## 0.9.0 — Rust-AutoTurret mit AK auf dem Dach

Neuer F9-Reiter „Turret“, standardmäßig ausgeschaltet, mit gespeichertem
Ein-/Ausschalter und separater Lautstärke. Lokaler Import der originalen
AutoTurret-/AK-Weltmodelle, fünf Diffustexturen und sieben Rust-Sounds.
Originale Dreh-/Neigepunkte, AK aufrecht mit nach vorne gerichtetem Lauf;
auf die Dachfläche skaliert. Das Abdeckblech der leeren Waffenaufnahme entfällt.

Mausblick steuert Drehung und Neigung mit begrenztem Nachlauf. Zielmarkierung
zeigt den tatsächlichen Auftreffpunkt. Rechte Maustaste hält Dauerfeuer:
30 Schuss, 2,5 Sekunden Nachladen, unbegrenzte Reservemunition. Raycast bis
150 m, 35 Stichschaden vor Valheims Widerständen an gegnerischen Kreaturen.
Felsen, Gebäude und eigene Karosserie blockieren. Kein Beschuss von Spielern,
gezähmten Tieren oder Bauwerken. Schussvarianten, Außennachhall, Servoloop,
Aktivierungs- und Nachladegeräusche werden räumlich abgespielt.

Eigene Rückstoßanimation, Mündungsfeuer, Lichtblitz und kurze Leuchtspur.
Keine Übernahme von Rust-Skripten oder Charakteranimationen. Schießen nur
am Fahrersitz; nach Fokusverlust, Menü oder Ausstieg ist Loslassen erforderlich.
Fehlende/fehlerhafte optionale Assets deaktivieren nur den Turret. Fahrwerk,
Nitro und bestehende Bedienung bleiben erhalten. Build und automatisierte
Prüfungen sowie Blender-Geometrievorschau; Unity-Spieltest noch offen.

## 0.8.0 — NFS-Tachos, dynamische Kamera und Driftsetup

Elf Original-Designs aus HUD_CustomTextures_00 bis 10 mit 7500-RPM-Skala und
Zeigern sowie NOS-Grafiken aus InGameRace.bun. Der lokale Import erhält Alpha
und entfernt ungenutzte Texturränder. Anzeige rechts unten: Geschwindigkeit,
akustische Drehzahl/Gang, Nitrotank und Boostfarbe. Menü → Anzeige: Design,
Größe, Farbe, KM/H/MPH und Sichtbarkeit; ohne Assets einfacher Text-Fallback.
Layout, Zeigersweep und NOS-Füllanimation sind eigene Valheim-Anpassungen.

Kamera folgt einem geglätteten Weltwinkel statt starr dem Fahrzeugwinkel.
Begrenzter Anteil der Bewegungsrichtung und vorausschauender Zielpunkt für
Kurven/Drifts; Rückwärtsfahrt dreht die Kamera nicht um. Mausblick pausiert
das automatische Nachziehen für 2,2 Sekunden; im Stand keine Rückzentrierung.
Im Anzeigemenü abschaltbar und in der Stärke einstellbar. Hindernisprüfung bleibt.

Optionaler Driftmodus zusätzlich zu Gelände/Straße: Heckantrieb, weniger
Seitengrip hinten, erweiterter Lenkwinkel, schnelleres Gegenlenken, 20 Prozent
mehr Drehmoment gegenüber Straße und weniger Traktionsregelung bei Schlupf.
Leertaste als Hinterrad-Handbremse; Menü/Exit/Fokusverlust bremsen alle Räder.
Originaler Reifensound und Rauch bei Schlupf, W+S-Burnout weiterhin möglich.
Driftparameter liegen separat; Umschalten stellt die bisherigen Grundwerte her.
Prüfungen und externe HUD-Vorschau ersetzen keinen Fahr-/Kameratest in Unity.

## 0.7.2 — Außenmusik bei offenen Türen und Heckklappe

Die tatsächlichen Animationsstände steuern Lautstärke, Tiefpass und Position
der räumlichen Radioquelle. Geschlossen bleibt die bisherige Dämpfung erhalten
(30 Prozent, 1400 Hz); eine offene Tür lässt mehr Musik heraus, die vollständig
offene Heckklappe erreicht 85 Prozent und 12 kHz. Werte sind Mod-Abstimmungen.
Die Quelle wandert zur geöffneten Fahrzeugseite oder zum Kofferraum; mehrere
Öffnungen ergeben eine gewichtete Position mit begrenztem Gesamtpegel.
Der bestehende Stream läuft durch, ohne neue Wiedergabe oder doppelte Songs.
Innenwiedergabe, Benutzerlautstärke, Pause, Fokus-/Menüsperren und der
Weiterlaufen-Schalter bleiben erhalten. Die Motorhaube ist ausgenommen.
Automatisierte Mischungsprüfungen ersetzen keinen Hörtest in Valheim.

## 0.7.1 — Fehlende obere Rückleuchten-Einsätze

Der Import hat vom geteilten STYLE04-Leuchtenmaterial nur die unteren
orangefarbenen Streifen übernommen. Die sechs oberen Dreiecke je Seite
fehlten; dadurch war Kofferraum-Neon durch die Rückleuchten sichtbar.
Beide oberen Einsätze werden jetzt mit Originalgeometrie und Originaltextur
übernommen. Die separat modellierten klaren Außenabdeckungen bleiben offen.
Neonposition, Lichtsteuerung und Fahrverhalten bleiben unverändert.
Regression prüft den vollständigen, duplikatfreien Erhalt der Dreiecke.
Blender-Vorschau geprüft; die Darstellung in Valheim noch bestätigen.

## 0.7.0 — Kofferraumausbauten, Türanimationen und Motorraum

F9 → Ausbau: Serie und drei originale 350Z-Gehäuse mit 2/4/6 Plätzen.
Je Platz Abdeckung, vier Subwooferdesigns, vier Verstärker und vier LCDs.
Originale AUDIO-Geometrie, AUDIO_SKIN-Textur und MARKER_AUDIO_COMP-Matrizen;
Komponentengröße nach Geometriepassform gewählt. Standard: KIT02 mit vier
SPEAKER1-Subwoofern. Dies ist eine Mod-Voreinstellung, kein nachgewiesener
Bestandteil von Rachels Preset. Separates Kofferraum-Neon mit sieben Farben,
Ein-/Ausschalter, statischer oder pulsierender Darstellung und Zurücksetzen.
Leuchtstreifen und Pulsverlauf sind eigene Mod-Effekte; LCDs bleiben statisch.

F9 → Türen: linke/rechte Tür, Haube und Heckklappe öffnen im Stand animiert.
Normale/Suicide-/Scherentüren verwenden Original-Quaternionen, Scharnierpunkte
und bei Scherentüren auch die originale seitliche Translation. Wiedergabe auf
eine Sekunde normiert; Türmechanik wechselt erst nach dem Schließen.
Spiegel, Türverkleidungen, Haubenunterseiten und Spoiler folgen den Gruppen.
Anfahren/Einsteigen schließt die Teile automatisch. Keine neuen Türcollider;
Antrieb und WheelCollider bleiben unverändert.

Drei Original-Motormodelle samt UNLIT-Innenflächen sind unter der Haube
wählbar. Das Basismodell enthält nur einfache Innenraumgeometrie; ein
detailliertes Cockpit wurde nicht gefunden. Scheiben bleiben undurchsichtig.
Neue Metadaten sind optional für ältere Modelle; importierte Audiomeshes
werden beim Teilewechsel wiederverwendet. Build und 322 automatisierte Prüfungen
sowie Blender-Sichtprüfungen; Unity-Darstellung/Bedienung noch im Spiel prüfen.

## 0.6.0 — Überfahren und laufendes Fahrzeug nach dem Aussteigen

Ab 2,5 m/s Kontaktgeschwindigkeit verursachen Fahrzeugtreffer nativen Valheim-
Schaden und Rückstoß an feindlichen, lebenden Kreaturen. Spieler und gezähmte
Tiere bleiben ausgenommen. Kleine Buchen/Tannen und vier Buschvarianten nutzen
nativen Hackschaden mit normalen Zerstörungseffekten und Beute. Große Bäume,
Felsen, Bauwerke und gepflanzte Setzlinge sind ausgenommen. Kurze Vorschau des
Fahrzeugvolumens plus Kollisionskontakte; pro Ziel 0,8 Sekunden Trefferschutz.
Schaden und Rückstoß sind begrenzt; hohe Gesundheit/Rüstung schützt weiterhin.
Fahrwerk und Antrieb bleiben unverändert. Der Build referenziert nun zusätzlich
die lokal installierte assembly_valheim.dll, ohne sie mitzuliefern.

F9 → Fahrzeug: „Beim Aussteigen weiterlaufen lassen“ ist standardmäßig an
und wird gespeichert. Nach mindestens einem Einstieg bleiben Motor im Leerlauf,
eingeschaltetes Neon und Radio aktiv. Der Schalter wirkt auch neben dem Auto;
Radio-Pause und eigener Neon-/Radioschalter bleiben wirksam. Draußen wird das
Radio räumlich, auf 30 Prozent gedämpft und auf 1,4 kHz tiefpassgefiltert;
es blendet bis 28 Meter aus. Valheim-Musik wird nur beim Fahren unterdrückt.
Motor draußen auf 55 Prozent und 30 Meter Reichweite begrenzt.
Fokus-/UI-Pause und Entfernen räumen weiterhin alle Quellen auf.

Build und 297 automatisierte Prüfungen einschließlich 41 neuer Treffer-Adaptertests
bestanden. Unity-Physik, Spiel-Schadensverarbeitung und Hörwirkung sind noch
im Spiel zu bestätigen.

## 0.5.0 — Kräftigeres Anfahren, Rückwärts-Sound und Burnout

Vorwärts-Anfahrmoment um 20 Prozent bis 3 m/s erhöht, linear bis 6 m/s auf
das bisherige Niveau reduziert. Die erste akustische Übersetzung steigt
von 3,79 auf 4,15; übrige Gänge und Höchstgeschwindigkeiten bleiben erhalten.
Das Rückwärts-Getriebeheulen ist eine neue synthetische Schleife mit
geschwindigkeitsabhängiger Tonhöhe/Lautstärke. Kein eindeutig zugeordneter
Original-Rückwärts-Sample wurde in der Dateiinventur und Ereignissuche gefunden.
Rückfahrlicht: Intensität 1,6 → 0,45, Reichweite 6 → 4 m, Emission 2 → 0,7.

W+S startet unter 0,65 m/s mit beiden Hinterrädern und mindestens einem
Vorderrad am Boden einen Burnout. Die Frontachse bremst, die Hinterachse
erhält Drehmoment und vorübergehend reduzierten Längsgrip. Über 2 m/s,
bei Kontaktverlust, Loslassen, Leertaste, Menü oder Ausstieg wird beendet.
Normale Reibungswerte werden wiederhergestellt; Nitro ist während W+S gesperrt.
Motor hält im Burnout den ersten Gang mit hoher Last. Originaler SKID_PAV-Sound
und SFX_SMOKE_B_CS-Rauchatlas reagieren auf tatsächlich drehende Hinterräder.
Rauchposition folgt auch dem visuellen Radversatz. Partikel werden begrenzt
und bei Sperre/Entfernen gelöscht. Effekte, Audio und Steuerung bleiben getrennt.

256 automatisierte Prüfungen bestanden; tatsächliche WheelCollider-Physik,
Rauchdarstellung und Hörwirkung benötigen den Spieltest.

## 0.4.1 — Menü links oben und einstellbare Frontscheinwerfer

Das Menü sitzt mit 16 Pixel Abstand links oben und skaliert bei kleinen
Fenstern so, dass unten Platz für die Fahrhinweise bleibt.
F9 → Licht bietet 14 Farbvorlagen, einen Stärke-Regler von 0 bis 300 Prozent
und Zurücksetzen auf Serie/100 Prozent. Auswahl und Stärke werden gespeichert.
Lichtkegel und emissive Frontleuchten erhalten gemeinsam Farbe und Stärke;
Rück-, Brems-, Rückfahr- und Kennzeichenlicht behalten ihr bisheriges Verhalten.

Die Vorlagennamen stammen aus den lokalen Originaldateien
LANGUAGES/English.bin, German.bin und Labels.bin. RGB-Werte und der Faktor
1,45 der extrahellen Vorlagen sind NFVU2-Anpassungen für Unity.
Build und 232 bestehende automatisierte Prüfungen bestanden.
Menüdarstellung und tatsächliche Lichtwirkung sind noch im Spiel zu bestätigen.

## 0.4.0 — Sturz, Radversatz und Felgengröße im Menü

F9 → Räder bietet getrennten Sturz (−15 bis +10 Grad) und seitlichen Versatz
(−10 bis +20 cm je Rad) für Vorder-/Hinterachse. Ein gemeinsamer Regler ändert
die nominale Felgengröße von 16 bis 22 Zoll relativ zum NFS-20-Zoll-Ausgangsmodell.
Die Reifenflanke wird angepasst, der 70-cm-Außendurchmesser bleibt erhalten.
Änderungen erscheinen sofort und bleiben in der Konfiguration gespeichert;
ein Zurücksetzen-Knopf stellt alle fünf Ausgangswerte wieder her.

Sturz liegt vor der Radrotation, damit geneigte Räder beim Rollen nicht taumeln.
Die Bremsen folgen Sturz/Versatz; Sättel bleiben von der Raddrehung getrennt.
Felgenänderungen verwenden unveränderte Ausgangsvertices statt kumulativer
Skalierung und passen die Normalen an. Der Versatz wird je Seite gespiegelt.
Die Funktion ist optisch: Radstand, Kontaktgeometrie und VehicleHandling bleiben
unverändert. Spieltest von Bedienung, Extremwerten und Beleuchtung noch offen.
232 automatisierte Prüfungen bestanden; externe 16-/22-Zoll-Vorschauen geprüft.

## 0.3.1 — Heckschriftzug richtig ausgerichtet

Die UV-Zuordnung des hinteren 350Z-Schriftzugs ist horizontal korrigiert,
damit er von hinten lesbar ist. Nur seine beiden Dreiecke sind betroffen;
der gemeinsame Originalatlas und die übrigen Embleme bleiben erhalten.
Nahaufnahme vor/nach dem Export geprüft; Spieltest noch offen.

## 0.3.0 — Nitro, Purge und zweifarbige Felgen

Linke Maustaste halten: Vorwärtsfahrt mit Gas und Bodenkontakt gibt Zusatzschub
und grüne Flammen aus den vier vermessenen Endrohren; im Stand entstehen helle
Purge-Strahlen an Stoßstange und Haube. Originale SFX_FIREB-/SFX_SMOKE_B_CS-Atlanten
und zwei Schleifen aus Nitrous_00_MB.abk werden verwendet. Die Loop-Rollenzuordnung,
Purge-Düsenpositionen und Unity-Partikelanimation sind dokumentierte Annäherungen.
Sechs Sekunden Tank, zwei Sekunden Nachladeverzögerung, zehn Sekunden Ladung
während der Fahrt. Zusatzschub liegt getrennt vom bestätigten VehicleHandling.
Menü, Fokusverlust und Ausstieg stoppen Effekte; Menüklicks lösen keinen Boost aus.

F9 enthält Nitro-Einstellungen und optionale, zunächst ausgeschaltete Flammen
bei Gaswegnahme nach hoher Last. NFS kennt Sputter-/Blow-off-Effekte; genaue
Tuningbedingungen und der richtige Knall-Substream bleiben offen. Deshalb wird
kein unbestätigter Backfire-Sound als Original ausgegeben oder installiert.

Die Originalfelge wird ohne Geometrieverlust in Chromring und Goldstern getrennt.
Angepasste Materialwerte verhindern die bisher fast schwarze Metallfläche.
Der Texturparser überspringt keine echten führenden 0x11-Pixelbytes mehr.
Externe Felgenvorschau und 222 automatisierte Prüfungen bestanden;
Unity-Fahr-, Partikel- und Hörtest noch offen.

## 0.2.1 — Heckleuchten, breitere Reifen, Sportbremsen und Songtitel

Der Nutzer bestätigt die grundsätzliche Funktion von 0.2.0 in Valheim.
Fehlende orange Streifen unter den STYLE04-Rückleuchten und die Einsätze der
Heckstoßstange werden wieder importiert. Die inneren Stoßstangenflächen leuchten
bei eingelegter Rückwärtsrichtung weiß und beleuchten den Boden dahinter.
S beim Vorwärtsfahren bleibt zunächst Bremsen; Aussteigen beendet Rückfahrlicht.
Die Aufteilung der Stoßstangenflächen ist eine dokumentierte Annäherung.
Orange Streifen sind passiv; eine Blinkersteuerung ist noch nicht implementiert.

Reifenbreite von etwa 19,5 auf 28 cm erhöht; 70-cm-Durchmesser, äußere Position
und Fahrwerk beibehalten. Originale STYLE03-Sportbremse mit roten Sätteln und
38-cm-Scheibe. Die Rotorfläche wurde wegen der ursprünglich quadratischen
Alpha-Maske rund nachgebildet; ihr Originalatlas bleibt erhalten.
Scheiben drehen mit dem Rad, Bremssättel folgen nur der Lenkung.

Das Radio zeigt 27 echte Künstler-/Songnamen aus der EA-Trax-Tabelle.
MPF-Ereignisse, originale EXE-Strings und WAV-Prüfsummen sichern die Zuordnung.
16 weitere Containerstreams bleiben lokal, erscheinen aber nicht mehr als Songs
im Radio. Build/Paket/Installer übernehmen nur die aktive Playlist.
Die Änderungen wurden extern gerendert und mit 176 automatisierten Prüfungen
getestet. Der Nutzer bestätigt anschließend auch die Funktion in Valheim.

## 0.2.0 — Fahrzeugmenü, Radio und Beleuchtung

F9 öffnet ein skalierbares Fahrzeugmenü für Fahrmodus, Radio, Licht und Neon.
Eingabe-/Kamerasperre und Audio-Pause sind getrennt; das Radio bleibt beim
Bedienen hörbar. Türen/Kofferraum sind ausdrücklich noch deaktivierte Vorbereitung.
Lokale Stereo-Musik wird bedarfsgerecht geladen, mit Titelauswahl, Vor/Zurück,
Pause und Lautstärke. Originalmusik kommt aus SDAT.VIV; die 43 importierten
Musikstreams haben bis zur gesicherten Zuordnung neutrale Titelnummern.

Front-, Rück- und Kennzeichenlicht schalten beim Einstieg ein und beim Ausstieg
aus. Bremslicht reagiert zusätzlich auf Bremsen. Die originale LICENSE_PLATE-
Textur liegt auf eigener Rechteckgeometrie; ihre Platzierung ist angenähert.
Originale 350Z_NEON-Maske mit bodenangepasster Projektion, Farbwahl und eigener
Pulsanimation. Shaderwirkung und Lichtstärken sind noch in Valheim zu prüfen.

Die akustischen Übersetzungen schalten im Gelände früher; Schaltunterbrechungen
betragen 140/120 ms. Radschlupf erhöht den Ton, ohne die Gangauswahl hochzuzählen.
Originale TURBO_MED_2-Aufnahmen ergänzen Aufladung und Blow-off; Zuordnung und
Auslösung sind eine dokumentierte Annäherung. VehicleHandling bleibt getrennt.
Installer/Paket unterstützen die externen Medien, Dateisicherungen, SHA-256 und
Rücksicherung bei Kopierfehlern. Der Nutzer hat die grundsätzliche Funktion
anschließend in Valheim bestätigt; Nachbesserungen sind in 0.2.1 beschrieben.

## 0.1.9 — Textur-Upload beim Spawnen

Der Spieltest von 0.1.8 scheiterte mit `LoadRawTextureData: not enough data provided`.
Die Texturen besitzen eine Mipmap-Kette, die Modelldatei enthält jedoch nur Stufe 0.
`SetPixelData<byte>(pixels, 0)` lädt jetzt gezielt diese Stufe; `Apply(true, true)`
erzeugt anschließend die kleineren Stufen. Der Nutzer hat die Darstellung
anschließend in Valheim bestätigt.

## 0.1.8 — Tatsächlichen Reifendurchmesser korrigiert

Der Import verwendete bisher die Diagonale einer quadratischen Felgen-Rückfläche
als Radius. Damit waren die sichtbaren Reifen von 0.1.7 nur etwa 52 cm groß,
obwohl 72 cm angegeben wurden. Gemessen wird jetzt ausschließlich die originale
Reifenoberfläche: vorne und hinten tatsächlich 70 cm Durchmesser, etwa 35 %
größer als zuvor. Die überstehenden Rückflächen-Dreiecke werden ausgelassen.
Zwei Regressionstests prüfen den Quellradius und den exportierten Reifen.
Die Vorschau enthält zusätzlich eine Seitenansicht und eine tiefe perspektivische
Ansicht für den Vergleich mit dem Referenzbild. Der Unity-Spieltest steht aus.

## 0.1.7 — Radproportionen und Einbauposition

Radskalierung erhöht (Durchmesserangabe später in 0.1.8 korrigiert), Räder weiter nach außen
gesetzt und längs in den Widebody-Radhäusern zentriert. Rad- und Bremsdarstellung
folgen gemeinsam dem Fahrwerk; ihre Versätze rotieren nicht mit den Rädern.
Die Karosseriedarstellung sitzt 4 cm höher, um den größeren Rädern Platz zu geben.
Collider, Antrieb und Federungsparameter bleiben auf dem bisherigen Stand.
Die externe Vorschau zeigt eine angenäherte eingefederte Stellung anstelle der
Collider-Aufhängungspunkte. Freigang bei Lenkeinschlag und Einfedern muss noch
in Valheim geprüft werden.

## 0.1.6 — Rachels Originalteile und Vinyl

Die ursprüngliche Voreinstellung `DDAY_PLAYER_CAR` aus `GlobalB.lzc` bestimmt
Widebody 02, Carbonhaube 07, Spoiler 02, Leuchten, Racing-Hart-Räder und
Endrohre. Originale UVs, Normalen, Materialbereiche, Texturen und das
`350Z_ART_F_6`-Vinyl einschließlich Maskenpalette werden lokal aufbereitet.
Räder folgen den bisherigen WheelCollidern; Bremssättel drehen nicht mit.
Fahrphysik und Sound bleiben unverändert. Modell und Texturen sind weiterhin
ignorierte lokale Daten. Build, 83 Mod-Prüfungen und drei Asset-Prüfungen bestanden; externe Vorschau
gerendert. Der Unity-Spieltest steht aus. Lack/Felgentönung sind angenähert;
Scheibentransparenz, Leuchtenabdeckungen und Kennzeichen bleiben offen.

## 0.1.5 — Fahrzeug-Sound

Originale NFSU2-Aufnahmen für Beschleunigen und Gaswegnehmen, jeweils acht
Drehzahlbereiche, eigene Drehzahl-/Schaltlogik, Roll- und Rutschgeräusche und
synthetisierte Hupe. Geräusche sind räumlich am Fahrzeug angebunden.
Der Nutzer bestätigt die grundsätzliche Funktion; Klang-Feinschliff ist offen.

## 0.1.4 — Anfahren wiederhergestellt

Motorleistung unabhängig von Radkontaktmeldungen, Physik bei Eingaben aktivieren
und Bremse beim Gasgeben vollständig lösen. Vom Nutzer bestätigt: Auto fährt
wieder, Geländemodus funktioniert sehr gut.

## 0.1.3 — Kamera und Gelände

Mauskamera mit Zoom und Hindernisabstand; umschaltbarer Gelände-/Straßenmodus.
Kamera und Moduswechsel bestätigt; Anfahrproblem anschließend in 0.1.4 korrigiert.

## 0.1.2 — Darstellung im Spiel

Vorhandenes Valheim-Material mit geladenem Shader verwenden. Spawnen, Fahren
und Ein-/Aussteigen erstmals vom Nutzer bestätigt.

## 0.1.1 — Modell laden

Newtonsoft.Json für die Modelldaten statt des ungeeigneten Unity-JSON-Laders.

## 0.1.0 — Erster Prototyp

Serienkarosserie aus NFSU2 extrahiert: 16 Teile, 3.867 Vertices, 3.449 Dreiecke.

Die Git-Historie beginnt mit dem Projektimport auf Basis von 0.1.5. Die früheren
Versionen sind hier dokumentiert; frühere lokale Sicherungen sind keine Git-Tags.
