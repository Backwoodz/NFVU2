# Nächste Aufgaben

## Release 0.31.2: ZIP-Updater und Kompatibilität

- Neuer Installer bevorzugt ZIP-Releases, prüft äußere ZIP- und innere EXE-SHA-256,
  entpackt nur die erwartete Setup.exe und übergibt weiterhin den gewählten
  Valheim-Pfad. Keine allgemeinen ZIP-Pfade oder zusätzlichen Programme ausführen.
- ZIP bleibt Hauptdownload. Identische EXE mit ihrer Prüfsumme zusätzlich für
  0.31.0/0.31.1 und andere bisherige Installer; diese erkennen so den neuen Release.
- Gemeinsame Version 0.31.2. Gameplay und Assets unverändert. Keine Installation
  ins echte Spiel und keine neue native Spielabnahme für diese Updater-Korrektur.
- Build, 145 Python-Tests (10 übersprungen) und alle 55 Managed-Suiten bestanden;
  darunter 103 Update- und 71 Installerkern-Prüfungen. ZIP-Download, innere/äußere
  Hashes, ungültige Archive, Abbruch, Alt-EXE-Fallback und Dateierhalt geprüft.
- Alle 129 Payload-Einträge, 107 Moddateien und 27 Radiotracks gehasht;
  106 Nicht-DLL-Moddateien bytegleich zu 0.31.1. Eingebettete Ressource,
  EXE-/ZIP-Prüfsummen und identische EXE in der ZIP verifiziert. ZIP: 153,85 MiB.

## Release 0.31.1: Installer als ZIP

- Reiner Verpackungsrelease: Setup.exe, innere SHA-256 und INSTALL.txt in einer
  ZIP, dazu eine äußere ZIP-Prüfsumme. Künftige /release-Aufrufe veröffentlichen
  diese ZIP statt einer eigenständigen EXE. Vollständig entpacken, dann Setup starten.
- Gemeinsame Mod-/Installer-Version 0.31.1; keine Änderung an Gameplay, Modellen,
  Sounds oder Musik gegenüber 0.31.0. Keine neue Spielabnahme behauptet.
- Bestehender Installer-Updater benötigt EXE-Assets. ZIP-Releases werden manuell
  von GitHub heruntergeladen. Eine ZIP-Updater-Implementierung ist nicht Teil
  dieses Verpackungsauftrags.
- Build und vollständige Testsuite bestanden: 145 Python-Tests (10 übersprungen)
  und 55 Managed-Suiten. Alle 129 Payload-Einträge gehasht; 107 Moddateien mit
  27 Radiotracks enthalten. 106 Nicht-DLL-Dateien bytegleich zu 0.31.0, einzige
  Plugin-Quelländerung ist die Versionsangabe. Eingebettete Ressource, EXE- und
  ZIP-Prüfsummen sowie unveränderte EXE-Bytes in der ZIP geprüft. ZIP: 153,84 MiB.

## Release 0.31.0: Transformer und Minigun

- Vollstaendiger Transformer-Branch fuer den angeforderten oeffentlichen Release.
  Installer erlaubt gezielt transformer/transformer.json und transformer.bin;
  keine allgemeine Freigabe fremder Binaerdateien. Modell, Bewegung, Minigun und
  vorbereitete Audiodateien werden im regulaeren Payload mitgefuehrt.
- Frische Konfiguration: Transformer aktiviert. Bestehende Enabled-Werte bleiben
  erhalten. F9/L3 ist beim besetzten stillstehenden Roboter freigegeben; dort nur
  Minigun/Einstellungen, keine Fahrzeugumbauten oder Garage waehrend Robotermodus.
- Alten Sprint-Tracefehler behoben: Reichweitengrenze loest den Abhebevorgang aus,
  statt auf 60 Prozent des anderen Schwungbeins zu warten und den Standfuss am
  Anschlag gleiten zu lassen. Gangtempo und Schrittmass unveraendert. Bestehender
  50-Hz-/60-Hz-Trace: maximale Abweichung 0.940 mm bei unveraenderter 4-mm-Grenze,
  79 Transformer- und 172 Steuerungschecks bestanden; keine Unity-Physiktests.
- Vorherige lokale Pruefnotizen weiter unten sind historische Zwischenstaende.
  Offene Spielpruefungen: unebene Ufer, Hindernisse und Terrain, Ausstattung beim
  Klettern, Kamera-/Controllergefuehl, gleichzeitige Audioeffekte und neue
  Installationen. Keine neue native Spielabnahme fuer den Release behauptet.
- Releasepruefung: Build, 143 Python-Tests und alle 55 Managed-Suiten bestanden.
  Installer 0.31.0 mit 107 Moddateien, 27 Radiotracks und 129 Payload-Eintraegen
  vollstaendig gegen Manifest und aktuelle DLL gehasht; eingebettete Ressource
  entspricht payload.zip. Echter Payload im isolierten Installerkern installiert,
  aktualisiert und deinstalliert: 76 Checks bestanden. Keine Spielinstallation
  ueberschrieben. Der bekannte alte Sprint-Tracefehler ist damit kein offener Test
  mehr; die historischen lokalen Zwischenberichte bleiben zur Nachvollziehbarkeit.

## Transformer: Explosivmunition als Standard (lokale Entwicklung)

- Eigene TransformerPrototype.MinigunAmmo-Einstellung, Standard Explosiv.
  Besetzter Roboter zeigt im Geschuetzmenue seine Minigun mit Normal-/
  Explosiv-Auswahl; Dachgeschuetz-Munition behaelt ihre eigene Einstellung.
  HUD nennt Explosiv, sobald die Effekte verfuegbar und aktiviert sind.
- Wiederverwendung der bestehenden TurretExplosion: Rust-Aufschlagsample
  aus dem vorhandenen Turret-Payload, zwoelf feste Effekt-/Soundplaetze,
  Flaechenschaden mit Deckungspruefung, Baum-/Felsreaktionen und kleine
  persistente Terrain-Krater. Normaler direkter Treffer bleibt 12 Stich;
  Explosiv fuegt den vorhandenen Radius von 2,25 m und bis zu 20 weiteren
  Schaden mit Entfernungsskalierung hinzu. Spieler, zahme/neutrale Figuren
  und Gebaeude bleiben nach der vorhandenen Explosionslogik geschuetzt.
- Effekte folgen Aktiv-/Pause-/Abstiegszustand, sind beim Stop stumm und
  werden samt Clip beim Entfernen freigegeben. Ohne optionales Soundfile
  bleibt der Explosivpfad nutzbar. Vorhandene Assetdateien unveraendert.
- Trefferanzeige liest Rest-LP nach direktem Treffer UND Explosion. Ein
  gestorbener Gegner bleibt bei null, auch wenn nativer GetHealth nach
  ZDO-Entfernung wieder einen Maximalwert zurueckliefern kann.
- Build sowie 65 Minigun-/Kompositions-, 23 Blast-, 10 Terrain-, 38 Audio-,
  16 Menue- und 159 Steuerungschecks bestanden. Native Explosionswirkung,
  Krater und Menuebedienung im Spiel pruefen; Tests verwenden Doubles.
- Mit Backup lokal installiert, alle 107 Dateien per SHA-256 geprueft;
  nur DLL geaendert, Originalassets und Konfiguration bytegleich. Beide
  Installations-Backupordner enthalten je zwei Zeitstempelstaende.

## Transformer: Minigun-Fernkampf und staerkerer Schaden (lokale Entwicklung)

- Minigun-Reichweite von 150 auf 250 m erhoeht. Kamera-Zielstrahl, echter
  Muendungsstrahl und Laser verwenden denselben Grenzwert. Schaden von
  3,34 auf 12 Stichschaden pro Schuss erhoeht, weiterhin unter den 14 des
  Dach-Turrets. 600 RPM, 1000 Schuss, Anlauf, Sounds und Laufglut bleiben.
- Installierte native Goblin-/GoblinBrute-Prefabs direkt gelesen: Fuling
  175 LP, Fuling-Berserker 800 LP, beide normal gegen Stichschaden. Native
  Levelskalierung ergibt 350/525 bzw. 1600/2400 LP bei einem/zwei Sternen.
  Neuer Basisbedarf 15 bzw. 67 normale Treffer; Blocken, Widerstaende und
  Welteinstellungen wirken weiterhin ueber den nativen Schadenspfad.
- Keine feste 20-m-Grenze im Minigun-Code gefunden. Native DamageText-
  Anzeige endet ab Werk 30 m von der Kamera, die Roboterkamera liegt etwa
  9,5 m hinter dem Spieler. Dies kann den gemeldeten Eindruck erklaeren,
  beweist aber noch keine Ursache fuer tatsaechlich ausbleibenden Schaden.
- Eigener letzter bestaetigter Treffer mit Gegnername, realen Rest-/Max-LP
  und Entfernung ab Muendung, dazu kurzer weisser Reticleimpuls. Nur nach
  beobachtetem Lebensverlust im synchronen lokalen Damage-RPC; kein falscher
  Trefferhinweis fuer blockierte/immune/ausgewichene Schadensanforderungen.
  Eine Anzeige pro Waffe, 2,5 s Dauer, Bereinigung bei Pause/Abstieg/Stop.
  Schuesse korrekt als native Fernkampftreffer markiert (m_ranged=true).
- Build sowie 60 Minigun-/Trace-/Schadens-/Feedbackchecks (inkl. 20 bis
  250 m und Grenze darueber), 20 Ziel-, 38 Minigunaudio-, 21 Hitze- und
  27 Kamerafahrtchecks bestanden. Physics-/Schadensantworten in den Tests
  sind kontrollierte Doubles. Native Fernkampftreffer und Balance im Spiel
  anhand der neuen Anzeige pruefen; ungeladene Gegner bleiben unerreichbar.
- Mit Backup lokal installiert, alle 107 Dateien per SHA-256 geprueft;
  nur DLL geaendert, Originalassets und Konfiguration bytegleich. Beide
  Installations-Backupordner enthalten je zwei Zeitstempelstaende.

## Transformer: filmische Auto-zu-Roboter-Kamera (lokale Entwicklung)

- Nur ein erfolgreiches besetztes Auto-zu-Roboter-Begin startet die neue
  Kamerafahrt. Kurven folgen direkt der fuenfsekundigen Animationsphase;
  gespeicherte/geparkte Roboter und Rueckverwandlung starten keine Fahrt.
- Einstieg aus aktueller Perspektive ohne Schnitt, sanfter Rueckzug auf
  9,8 m (vorher 12,8 m), dann Annaeherung auf 9,5 m (vorher 10,2 m).
  Eine bereits weiter entfernte Startperspektive wird nicht noch erweitert.
  Langsamer 45-Grad-Hauptbogen
  von links nach schraeg vorne links; aktueller Startwinkel wird weich in
  diesen Bogen uebernommen. Blickpunkt steigt auf 2,65 m, Blickwinkel endet
  leicht von unten bei -2 Grad. Dezenter Zoom von maximal fuenf FOV-Grad.
  Kurzer begrenzter Winkelimpuls am bestehenden Sub-Drop-Cue bei 3,75 s.
- Vorhandener Kamera-Spherecast folgt auch der filmischen Position und
  zieht bei Hindernissen sofort ein. Mausblick, Mausrad, Mittelklick und
  bewusster rechter Stick uebernehmen den aktuellen Winkel; kleine
  Stickbewegungen innerhalb der bestehenden Deadzone brechen nicht ab.
  Nach Abschluss/Abbruch 0,85 s weiche Rueckgabe von Framing und Zoom an
  die normale Spielkamera. Die aktuelle Blickrichtung bleibt erhalten.
- Menue/Fokusverlust/Pause frieren die Fahrt ein; bewusster Menuekamerablick
  uebernimmt. Ausstieg, Kameratausch und Reset stellen beide urspruenglichen
  FOV-Werte von Szene und Himmel exakt wieder her. Lokale Option
  TransformerPrototype.CinematicCamera ist standardmaessig aktiv.
- Build sowie 27 neue Kamera-Modell-/Produktionsadapterchecks, 18 Schritt-
  Kamera-, 159 Steuerungs-, 11 Nitro-Projektions- und 59 Mountchecks
  bestanden. Adapter verwenden Unity-Doubles, keine gerenderten Frames.
  Native Bildwirkung, Hinderniswege und Maus-/Controllergefuehl im Spiel
  pruefen; Sounds, Verwandlungsdauer und Fahrzeughandling bleiben erhalten.
- Mit Backup lokal installiert, 107 Dateien per SHA-256 geprueft; nur DLL
  geaendert. Originalassets und Konfiguration bytegleich, beide
  Installations-Backupordner auf je zwei Zeitstempelstaende geprueft.
- Nach Nutzerfeedback engeres Framing wie oben angegeben. Tastenpanel
  unten rechts von -16 auf +48 Bildschirm-Pixel ueber dem unteren Rand
  verschoben: positiver Abstand beim Bottom-Right-Anker statt Abschneiden.
  Tacho-Abstand beruecksichtigt den neuen Panelrand ebenfalls.
- Engeres Framing/Hinweisposition: Build, 27 Kamerafahrt-, 159 Steuerungs-
  und 24 Kamera-/HUDchecks bestanden. Erneut lokal mit Backup installiert,
  107 Dateien geprueft, nur DLL geaendert; Assets/Konfiguration bytegleich
  und je zwei Zeitstempelbackups bestaetigt. Sichtabnahme im Spiel offen.

## Fahrzeugsteuerung: Controller-Verwandlung und E-Zugang (lokale Entwicklung)

- RB/R1 verwandelt den besetzten 350Z am Boden und den besetzten Roboter
  zurueck, jeweils mit den vorhandenen Stand-/Platz-/Animationspruefungen.
  Im Flug bleibt RB/R1 Steigen, zu Fuss bleibt die native Belegung erhalten.
  LB/L1 feuert weiterhin die Roboterminigun. F10 bleibt konfigurierbar.
- E steigt ins Fahrzeug/auf den Roboter ein und wieder aus; F7 bleibt
  alternativer Fahrzeugzugang, B/Kreis auf Controller bleibt erhalten.
  Anvisierte native Interaktionen wie Tueren, Kisten und aufhebbare Items
  haben zu Fuss Vorrang. Ein bereits von Player.Update ausgefuehrter Use
  wird pro Frame beobachtet, damit ein zerstoertes Pickup nicht zugleich
  den Einstieg ausloest. Ein verbrauchter E-Druck bleibt bis zum Loslassen
  fuer den Fahrzeugzugang reserviert, auch nach Wiederfreigabe des Spielers.
- Native und alternative Hinweise, Garage, Statusmeldungen, Uebersetzungen
  und README nennen E beziehungsweise RB/R1. Kein Verwandlungshinweis fuer
  RB im Flug; Menues behalten ihre bisherigen Controllerbelegungen.
- Build sowie 159 Steuerungs-, 27 Kabinen-, 59 Mount-, 31 Minigun- und
  37 Flugchecks bestanden. Native Controllerhardware, Harmony-Interaktion
  und Ein-/Ausstieg an Weltobjekten muessen im Spiel bestaetigt werden.
- Mit Backup lokal installiert und alle 107 Dateien per SHA-256 geprueft;
  nur DLL geaendert, Originalassets und Konfiguration bytegleich. Beide
  Installations-Backupordner enthalten je zwei Zeitstempelbackups.

## Transformer: anatomische Knie beim Hochklettern (lokale Entwicklung)

- Die bisherige Kletterpose gab beiden Knien einen stark seitlichen,
  leicht rueckwaertigen IK-Pol vor. Bei typischen Auftritten kehrte dies
  die Kniebeugung um. Jetzt stammt der Pol aus dem jeweiligen echten
  Hueftgelenk und der Hueft-Fuss-Richtung in der Koerperebene; die Knie
  beugen vorwaerts und beim hohen Auftritt nach oben. Keine seitliche
  Ausweichbeugung mehr, kein Richtungswechsel beim Anheben des Fusses.
- Gepflanzte Fussziele, gemessene Beinlaengen, Beckenausgleich und der
  Uebergang zu den vorhandenen Sitzreferenzen bleiben erhalten.
- 59 Mount-/Kletter-/Abstiegschecks bestanden, darunter Regressionen fuer
  beide zuvor verkehrt gebeugten Beine und ein Raster erreichbarer
  Fusspositionen mit festen Knochenlaengen. Native Hautverformung und
  Abstand zur Panzerung waehrend der Klettersequenz im Spiel pruefen.
- Build und 27 Kabinenchecks ebenfalls bestanden. Mit Backup lokal
  installiert: 107 Dateien per SHA-256 geprueft, nur DLL geaendert;
  Assets und Konfiguration bytegleich, je zwei Installationsbackups.

## Transformer: Linksklick, Minigun-Anlauf und Laufglut (lokale Entwicklung)

- Roboterminigun feuert mit gehaltener linker Maustaste, Controller weiter
  LB/L1. Hinweise und HUD angepasst, bestehendes Loslass-Gate bleibt aktiv.
  Originale Anlaufzeit von 1,5 s bis zum ersten Schuss bleibt erhalten.
- Motorclips beim Laufzeit-Upload um bis zu 12 dB angehoben und originale
  feste Rust-Motorschicht ohne Pitchwechsel ergaenzt. Frueher hoerbarer
  Lautstaerkeaufbau statt fast stummem linearem Einblenden. Einzelpeaks und
  gemeinsamer Motorbus auf 0,97 begrenzt; zehn feste Audiostimmen.
  Schuss-/Mechanik-/Bass-/Nachladepegel und Original-WAVs bleiben gleich.
- Vordere originale Laufoberflaechen bekommen eine eigene, rotierende
  Glutschicht mit weichem Frontverlauf. Waerme nur durch abgefeuerte Schuesse:
  sichtbar nach ca. 2 s, volle Glut nach 6 s Dauerfeuer. Danach 2 s halten
  und ca. 5 s sichtbar abkuehlen; auch inaktive/verborgene Waffen kuehlen ab.
  Pause haelt die Zeit an. Keine spielmechanische Ueberhitzungssperre.
- Build sowie 21 Hitze-, 38 Audioadapter-, 31 Minigun-, 145 Steuerungs-,
  98 Lade- und 47 Dach-Turretchecks bestanden. Audioadapter prueft echte
  Original-PCM und Cacheunveraendertheit mit Unity-Doubles. Native Glut-
  darstellung und Lautstaerkeabstimmung muessen im Spiel bestaetigt werden.
- Lokal mit Backup installiert, alle 107 Dateien per SHA-256 geprueft;
  nur DLL geaendert, Originalassets und Konfiguration bytegleich. Beide
  Installations-Backupordner enthalten je zwei Zeitstempelbackups.

## Transformer: Visierkorrektur und echter Wassergrund (lokale Entwicklung)

- Nutzer-Screenshot markiert das gewuenschte Visier unterhalb des bisherigen
  hohen Punkts. Gemeinsamer HUD-/Zielstrahlpunkt wieder Viewport-Y 0,62;
  angehobene Robotkamera mit 4,10-m-Pivot bleibt bestehen.
- Wasserlaufen in originalen installierten Spieldaten erklaert: WaterSurface
  besitzt neben dem Trigger einen aktiven Non-Trigger-BoxCollider auf Layer
  Water. IgnoreTrigger allein liess ihn zum hoechsten Boden werden; die
  daraus berechnete Tiefe war praktisch null. Groundprobe verwendet jetzt
  Valheims native Boden-Layer und verwirft Fluessigkeitskomponenten explizit.
  Wasserhoehe und echter Seeboden/Brueckendeck werden separat abgefragt.
- Bei blockierter Laufbewegung wird die vertikale Bodenannaeherung weiterhin
  ausgefuehrt. Der kinematische Roboter bleibt dadurch nicht auf einer alten
  Schwebehoehe haengen. Brusttiefe von ca. 2,758 m bleibt die erlaubte Grenze.
- Boden- und Hindernismasken getrennt: Figuren bleiben feste Hindernisse,
  Wasseroberflaechen und Fluessigkeitsvolumen werden in beiden gefiltert.
  Bei nassem Support oder Bodenspalt begrenzte Diagnose mit echtem Support,
  Layer, Boden-, Root- und Wasserhoehe (hoechstens eine Meldung pro 10 s).
- Build, 24 echte Groundprobe-Adapterchecks mit Physics-Doubles (inklusive
  Wasseroberflaeche vor Seeboden und integrierter Tiefenentscheidung),
  47 Wasser-, 34 Boden-, 32 Streaming-, 98 Lade- und 31 Minigunchecks
  bestanden. Unabhaengiger Review ohne Blocker; native Sichtabnahme offen.
- Mit Backup lokal installiert und alle 107 Dateien per SHA-256 geprueft.
  Nur DLL geaendert, Modelle/Sounds und Konfiguration unveraendert;
  beide Backupordner nach erfolgreicher Verifikation auf je zwei Staende.

## Transformer: Zielkamera, Laservisier und Waten (vorheriger lokaler Stand)

- Nutzerabnahme des ersten Minigunstands positiv; Fadenkreuz lag noch auf dem
  Fahrer. Eigene gemeinsame Transformer-Zielkoordinaten: Viewport (0,5/0,78).
  HUD projiziert exakt diesen Kamerapunkt; Kameraray verwendet dieselben UVs.
  Robot-Zielkamera mit 4,10 m Pivot und mindestens 9,5 m freier Distanz,
  damit Roboter und Fahrer unterhalb des Zielbereichs liegen. Hindernisse
  duerfen die Kamera weiterhin einziehen. Auto-Framing bleibt unveraendert.
- Eigenes kleines NFVU2-Lasermodul aussen an der linken Minigunseite.
  Sichtbarer Strahl mit hellem Kern und oberflaechengebundenem Trefferpunkt;
  konvergiert zum echten Muendungsziel, eigener Kollisionsweg gegen Waende,
  Eigenrumpf und Figuren. Strahl nur im aktiven besetzten Robotermodus,
  Pause/Rueckverwandlung/Stop entfernen ihn. Bestehende Laserfarbe, Staerke
  und Schalter des Turrets werden genutzt; kein neues importiertes Rustteil.
- Waten jetzt bis zur Brust statt kompletter Wassersperre: Brustlinie aus
  Thorax- und Schulterneutralpose minus echter Sohle, derzeit ca. 2,758 m
  Wassertiefe. Einstieg und Tiefenwechsel bis zur Grenze erlaubt, tiefere
  Wege/Sprintlandungen blockiert. Echte Grundhoehe bleibt massgeblich;
  Bruecken und Rueckweg funktionieren weiter. Neue Auto-zu-Roboter-
  Verwandlungen benoetigen weiterhin einen trockenen Aufstellplatz.
- Build, 47 Wasser-, 31 Minigun-Modell-/Schadens-, 20 Ziel-, 98 Lade- und
  18 Kamera-Schrittimpulschecks bestanden; unabhaengiger Code-Review ohne
  Blocker. Native Sichtpruefung von Kamera/Laser und Waten noch offen.
- Mit Backup lokal installiert, alle 107 Dateien per SHA-256 geprueft;
  nur DLL geaendert. Saemtliche Modell-/Sounddateien und Konfiguration
  bytegleich. Nach erfolgreicher Installation je zwei Zeitstempelbackups
  in beiden Installations-Backupordnern bestaetigt.

## Transformer: Wassergrenze und Rust-Schulterminigun (erster lokaler Stand)

- Laufen, Sprint, Rueckwaertsgehen und Drehen pruefen den gesamten Weg bis
  zum naechsten Fussauftritt auf Wasser. Echte Sohlenflaechen und tatsaechliche
  Colliderhoehen statt Knochelsockel/Meeresspiegel-Wadeclamp; native Wellen
  und lokale Fluessigkeiten werden beruecksichtigt. Trockene Bruecken bleiben
  begehbar, Rueckwaertsgehen vom Ufer bleibt moeglich. Neue Verwandlung nur
  mit trockener Aufstandsflaeche. Bereits nass gespeicherte Roboter duerfen
  ueber gleich tiefe oder flachere Flaechen ans Ufer; keine tiefere Bewegung.
- Originales Rust-Minigun-Weltmodell samt Albedo, Laufgelenk und Original-
  Motor-/Feuer-/Auslauf-/Nachladesamples lokal aus dem vorhandenen Spiel
  exportiert. Werkzeuge und Herkunfts-/Hashpruefungen reproduzierbar;
  Originaldateien und generierte Assets bleiben ignoriert. Eigenes NFVU2-
  Schulterlager mit Drehkranz und Schwenkgelenk folgt der linken Clavicula.
  Waffe fuer den Roboter 1,4-fach skaliert, getrennte Laufrotation.
- Zielpunkt und RMB/LB-Steuerung wie beim manuellen Auto-Turret, gleiche
  erhoehte Reticleposition; echtes Muendungs-Hitscan mit Selbst-/Wandschutz
  entlang des gesamten Laufs. Kein automatisches Wachen als geparkter Roboter.
  Vollstaendig ausgefahren und besetzt erforderlich; Pause, Menues, Mounting,
  Rueckverwandlung und Tod stoppen Feuer und Motor. Munitionsanzeige im HUD.
- 1000 Schuss pro Magazin, native Rust-Werte 600 RPM/1,5 s Anlauf/3,5 s
  Nachladen. Automatisches Nachladen sowie R fuer vorzeitiges Nachladen.
  Vier begrenzte Audioloops und fuenf feste Aktionsstimmen statt Stimmen
  pro Projektil. Nur 3,34 Stichschaden pro Treffer auf feindliche Kreaturen,
  keine Explosiv-/Terrain-/Gebaeudetreffer. Das lokale native Greydwarf-
  Prefab bestaetigt 40 HP und normalen Stichwiderstand: zwoelf gewoehnliche
  Treffer als Basis; vorhandener Stagger und Weltmodifikatoren gelten weiter.
- Originalwaffe statisch in Blender visuell geprueft. Native Darstellung,
  Zielgefuehl, Soundmischung und Wasser-/Brueckenphysik im Spiel noch offen.
- Montage am vollstaendigen gebackenen Roboter-Endzustand ebenfalls visuell
  geprueft, Position (-0,83 / 4,10 / -0,04), seitlicher Kopfabstand 36,4 cm.
  Physisch links nutzt wegen vertauschter Legacy-Rignamen clavicle.R;
  Gait-Knochenbezeichnungen bleiben unveraendert. Originalgun und Halterung
  sind oberhalb der Schulterpanzerung sichtbar; kein nativer Animationstest.
- Build sowie 37 Wasser-, 31 Minigun-Modell-/Schadens-, 98 Lade-, 34 Boden-,
  20 Ziel-, 47 Dach-Turret-, 40 Schritt-Audio-, 24 Fussspur- und 23 Gras-
  Callbackchecks bestanden. Installer: fuenf Pythonchecks mit 62 Datei-
  Engine- und 50 Updatechecks; vier bestehende Rust-Importchecks bestanden.
  Modell-/Damage-Doubles ersetzen keine native Physik-/Audioabnahme.
- Lokal mit Backup installiert; alle 107 Dateien per SHA-256 geprueft.
  Neu: 19 Minigun-Dateien. Von den 88 bisherigen Dateien nur DLL geaendert;
  alle bisherigen Modelle/Sounds und die Konfiguration bytegleich.
  Beide Backupordner nach erfolgreicher Pruefung auf je zwei Zeitstaende
  begrenzt. Noch kein nativer Spiel-/Hoertest.

## Transformer: temporaere Stampfspuren und Staub (lokale Entwicklung)

- Echte linke/rechte Gait-Fusskontakte erzeugen kurze erdige Staubstoesse
  und flache Sohlenabdruecke. Gleiche Kontaktzaehler wie beim Sound, aber
  unabhaengig von Audiolautstaerke und Stimmen; kein zeitgesteuerter Ersatz.
- Kontaktmitte, Richtung und Groesse aus dem unteren 12-cm-Bereich der
  vorhandenen starren Bootgeometrie; derzeit etwa 0,61 x 1,09 m je Fuss.
  Ein 5-x-9-Grid folgt per kurzen Raycasts den echten Terrain-Dreiecken.
  Erdspuren nur auf Gras/Schlamm/normalem Boden, keine durch Gebaeude,
  Felsen oder unter Wasser projizierten Abdruecke.
- Maximal 32 wiederverwendete Abdruecke und 96 Staubpartikel pro Fahrzeug;
  18 s Standzeit, danach 6 s Ausblenden. Sprint erzeugt etwas mehr Staub.
  Auch nach der Rueckverwandlung werden vorhandene Spuren weiter gealtert.
  Menues, Fokusverlust und Streaming halten die Effekte an; keine spaeter
  nachgeholten Kontakte. Fahrzeugentfernung entsorgt alle Ressourcen.
- Gras unter den Sohlen wird nur waehrend nativer Grass-Instanz-Draws
  temporaer abgeflacht und mit dem Abdruck wieder eingeblendet. Native
  Instanzmatrizen werden nach dem Draw auch bei Fehlern wiederhergestellt.
  Optionale Grasmaske: fehlt die passende native API, bleiben Staub und
  Meshabdruecke verfuegbar. Kein Terrain-Paint, keine Hoehenaenderung,
  keine dauerhafte Aenderung an Gras oder Weltstand.
- Native Sichtabnahme von Staubkontrast, Grasmaske und Spurenlaenge offen.
- Build sowie 24 Effektmodell-, 23 Gras-Callback-, 40 Schritt-Audio-,
  81 Lade-, 32 Streaming- und 31 Speicherkoordinationschecks bestanden;
  zusaetzlich 34 Boden- und sieben Leerlaufstaubchecks. Gras-Callbacks
  werden mit Unity/Harmony-Doubles geprueft, nicht mit nativer Grafik oder
  der Detour-Engine. Echter Asset-Geometriecheck der Sohlen bestanden.
- Lokal mit Backup installiert und alle 88 Dateien per SHA-256 geprueft.
  Nur die DLL geaendert; saemtliche Audio-/Modellassets und bestehende
  Konfiguration unveraendert. Noch kein nativer Spiel-/Sichttest.

## Transformer: zusaetzlicher Bass bei jedem Schritt (lokale Entwicklung)

- Nutzeraufnahme bass.mp3 liefert einen kurzen Tieftonimpuls fuer jeden
  echten Fusskontakt. Ausschnitt 0,20–0,48 s (0,28 s), 6 ms Ein-/90 ms
  Ausblendung, maximaler 80-ms-RMS-Zielpegel 0,20. Sein Anstieg wird am
  bestehenden mechanischen Auftritt (~15 ms nach Clipstart) ausgerichtet.
- Alle sechs bisherigen step_01 bis step_05 und step_07 enthalten die
  Bassschicht zusaetzlich; keine siebte Zufallsvariante und kein Auslassen
  einzelner Kontakte. Mechanische Einzelaufnahmen und deren Pegel bleiben
  erhalten, nur die neue Gruppe nutzt bei Bedarf eine Spitzenbegrenzung.
  Die Bassschicht ist kurz genug fuer einzelne Sprintkontakte; kein voller
  langer Ausklang der MP3 unter jedem Schritt.
- Keine Aenderung an Laufzeit, Gait, Schrittzaehlern, Shuffle oder bisherigem
  5-Prozent-Schrittgain. Dieselben raeumlichen Stimmen spielen pro Kontakt
  den kombinierten Clip. Einbau ueber tools/prepare_transformer_audio.py mit
  optionalem --bass-file; ohne diesen Parameter bleibt der bisherige Export
  reproduzierbar. Native Hoerabnahme des zusaetzlichen Schrittbasses offen.
- Build, sechs Aufbereitungschecks mit Originalquellen/CLI-Reproduktion,
  40 Schritt-Audio-/Modellchecks und 81 Ladechecks bestanden. Unabhaengige
  Neuberechnung aller sechs Mischungen bestaetigt originale mechanische
  Grundlagen und Pegel, samplegenaue Attack-Ausrichtung bei 15,011 ms sowie
  6,47–8,44 dB mehr Tieftonenergie (20–100 Hz in den ersten 0,30 s) in jeder
  Variante. Kein Clipping; 81 sonstige Payload-Dateien inklusive DLL,
  Verwandlungen und Reaktor bytegleich. Reviewevidenz unter
  artifacts/audio-transformer/step-bass-independent-review.json.
- Bei geschlossenem Valheim lokal installiert, DLL und alle 88 Payload-Dateien
  per SHA-256 geprueft; Bass und originale Mechanikbasis in jeder der sechs
  Varianten gesondert bestaetigt. Konfiguration erhalten, beide Backupordner
  nach erfolgreicher Pruefung auf je zwei Zeitstempelstaende begrenzt.
  Native Hoerabnahme des zusaetzlichen Schrittbasses noch offen.

## Transformer: spaeter Sub-Drop bei Auto-zu-Roboter (lokale Entwicklung)

- Nutzeraufnahme brvhrtz-sub-drop-short-232033.mp3 als zusaetzlicher
  Abschlussakzent nur in der Vorwaertsverwandlung. Natuerlicher letzter
  abfallender Abschnitt 1,40–2,95 s, Einsatz bei 3,75 s (75 Prozent), Ende
  bei 5,30 s einschliesslich bestehendem Nachklang. 10 ms Ein-/80 ms
  Ausblendung; keine Zeitstreckung, Pitch- oder Animationsaenderung.
- Eigene abschliessende Soundgruppe ueber der bisherigen vollstaendigen
  Vorwaertsmischung, nur ihre Spitzen werden bei Bedarf begrenzt. Keine
  globale Pegelabsenkung der Mechanik/Growls oder des fruehen Bassakzents.
  Die Rueckverwandlung und beide Reaktorschichten bleiben erhalten.
  Neue Hoerabstimmung des Sub-Drops im Spiel offen.
- Build, 14 Aufbereitungschecks mit Originalquellen, 61 Vorwaertsaudio- und
  81 Ladechecks bestanden. Unabhaengig rekonstruierter vorheriger Gesamtmix
  exakt erhalten; fertige PCM-Datei bis 3,75 s byteidentisch. Drop folgt dem
  natuerlichen Quell-Frequenzfall und Ausklang, nur seine Gruppe bekommt bei
  Bedarf Spitzenbegrenzung. 86 andere Payload-Dateien inklusive DLL und
  aktueller Reaktormischung unveraendert. Reviewevidenz unter
  artifacts/audio-transformer/late-subdrop-independent-review.json.
- Bei geschlossenem Valheim lokal installiert; alle 88 Payload-Dateien per
  SHA-256 verifiziert, spaeter Drop und unveraenderter Abschnitt bis 3,75 s
  gesondert geprueft. Konfiguration erhalten, beide Backupordner nach Erfolg
  auf je zwei Zeitstempelstaende begrenzt. Native Hoerabnahme noch offen.

## Transformer: zweite Kernreaktorschicht (lokale Entwicklung)

- Zusaetzlich zu kernreaktor.mp3 nun die vom Nutzer gewaehlte Aufnahme
  freesound_community-evil-robot-or-spaceship-or-something-idk-94720.mp3.
  Aktiver Ausschnitt 0,8–5,6 s mit 800 ms kreisfoermiger Ueberblendung ergibt
  einen 4-s-Loop; zweimal ohne Luecke ueber den bisherigen 8-s-Grundloop gelegt.
  Zusatzschicht mit maximalem 80-ms-RMS-Zielpegel 0,035, bisherige Grundschicht
  mit 0,055 und ihrem Ausschnitt/Pegel erhalten. Keine Zeit-/Pitch-Aenderung.
- Gemeinsames reactor.wav nutzt die bestehende bewegte Reaktorposition,
  Laufzeitverstaerkung 0,75, sichtbare Phase von 55 bis 100 Prozent sowie
  Pause-/Streaming-/Parkverhalten. Keine neue Laufzeitlogik oder Aenderung
  an der SLY-Sichtbarkeit. Native Hoerabnahme der Zusatzschicht offen.
- Build, sieben Aufbereitungschecks inklusive Originalquellen-Regression,
  34 Reaktor-Laufzeitadapterchecks und 81 Ladechecks bestanden. Unabhaengige
  Neuberechnung bestaetigt den unveraenderten Grundloop und die additive
  Mischung aus zwei natuerlichen Zusatzzyklen: kein Ducking/Normalisieren,
  durchgehende Loop-Naht. 86 sonstige Payload-Dateien inklusive DLL sind
  bytegleich zum zuvor installierten SLY-Stand. Reviewevidenz unter
  artifacts/audio-transformer/additional-reactor-independent-review.json.
- Bei geschlossenem Valheim installiert, DLL und alle 88 Payload-Dateien
  per SHA-256 geprueft; zwei Reaktorschichten und unveraenderte Grundschicht
  bestaetigt. Konfiguration erhalten und beide Backupordner nach Erfolg auf
  je zwei Zeitstempelstaende begrenzt. Native Hoerabnahme noch offen.

## Transformer: SLY erst unter dem hochgeklappten Spoiler (lokale Entwicklung)

- Schriftzug und Montagekanaele haben eine eigene Sichtbarkeit statt der
  fruehen allgemeinen Mechanik-Einblendung bei 11,5/120 Fortschritt (~0,48 s).
  Jetzt erst ab Frame 175/240, also 72,92 Prozent bzw. ~3,65 s: Dort liegt
  die gesamte Unterkante des Originalspoiler-Fluegels erstmals ueber der
  Oberkante des Schriftzugs, und seine Breite deckt den Schriftzug ab.
  Ermittelt aus dem vorhandenen gebackenen Clip und den echten Badge-Konturen;
  keine zusaetzlichen Mechanikhalter in die Spoilergrenze einbezogen.
- Beim Rueckweg verschwindet SLY an derselben Pose. Geladene Roboter bei
  Fortschritt 1 behalten den Schriftzug direkt; seine Befestigung und stetige
  gruene Emission bleiben erhalten. Build und 81 Ladechecks bestanden;
  native Sichtabnahme des geaenderten Einblendzeitpunkts offen.
- Bei geschlossenem Valheim lokal installiert; DLL und alle 88 Payload-Dateien
  per SHA-256 geprueft, 87 Dateien ausser der DLL bytegleich zum Vorzustand.
  Konfiguration erhalten, beide Backupordner nach Erfolg auf je zwei
  Zeitstempelstaende begrenzt. Geometriebericht und Seitenansicht unter
  artifacts/audio-transformer/badge-spoiler-reveal.json bzw. .png.

## Transformer: kurze Inside-Impulse und Vorwaerts-Bass (lokale Entwicklung)

- Nutzer lehnt lange durchgaengige Inside-the-Robot-Texturen ab. Alle neun
  Ausschnitte in beiden Richtungen, einschliesslich der vier urspruenglichen
  Vorwaerts-Cues, sind durch isolierte 0,20–0,22-s-Attacks ersetzt bzw.
  entsprechend gekuerzt. Kurze 6-ms-Ein-/65-ms-Ausblendung und mindestens
  0,1 s Abstand zwischen Inside-Impulsen; keine durchgehende Inside-Flaeche.
  Die vorher vollstaendige Vorwaerts-Grundmischung ist dadurch bewusst
  geaendert. Growls, Einrastakzente, Laser und Moving-Tools behalten ihre
  einzelnen Schnitte/Pegel/Einsatzzeiten, A bleibt isoliert byteidentisch.
- Zusaetzliche Auto-zu-Roboter-Testschicht aus imgmidi-moving-bass-sfx-set-336212:
  gewuenschter Abschnitt ab 1:15, aktiver Effekt aus 75,78–77,68 s (1,90 s)
  ohne leisen Vorlauf, Einsatz 0,08 s. Eigene begrenzte Bassgruppe ueber der
  bestehenden Mischung, ohne globale Pegelabsenkung. Keine Bass-Ergaenzung
  im Rueckweg. Gezielter 75–78-s-Decode statt Lockerung der gemeinsamen
  60-s-Quellgrenze; Manifest trennt Originaldauer und Decodierfenster.
- Beide Soundtracks bleiben 5,3 s bei unveraenderter Animation/Laufzeit.
  Nutzer bestaetigt den abgestimmten Soundstand inklusive Bass und kurzen
  Inside-Impulsen im Spiel und beauftragt den Commit. Keine Aenderung an
  Gang-/Fahrphysik oder Motor-/Reaktorkurven durch diese letzte Abstimmung.
- Build, elf Audio-Aufbereitungschecks (mit lokalen Originalquellen) und
  81 Ladechecks bestanden. Unabhaengige Neuberechnung bestaetigt beide
  WAV-Hashes, alle neun kurzen Inside-Cues, Bass-Quellsekunden per separatem
  Sampletrim sowie die unveraenderten isolierten Nicht-Inside-Sounds.
  84 sonstige Payload-Dateien inklusive DLL sind bytegleich zum Vorzustand.
  Reviewevidenz: artifacts/audio-transformer/short-impulse-independent-review.json.
- Bei geschlossenem Valheim installiert; alle 88 Payload-Dateien per SHA-256
  verifiziert, einschliesslich Bass und neun kurzen Inside-Impulsen. Konfiguration
  erhalten und beide Backupordner nach erfolgreicher Pruefung auf je zwei
  Zeitstempelstaende begrenzt. Native Hoerabnahme der neuen Mischung bestaetigt.

## Transformer: Reaktor und weitere Soundabstimmung (lokale Entwicklung)

- Nutzer bestaetigt die erweiterte Mischung. Weitere Balance: Motorleerlauf
  bis 20 Prozent Entfaltung mit vollem Autopegel, danach weich bis auf
  40 Prozent bei fertigem Roboter absenken. Rueckweg entsprechend umgekehrt;
  gleicher Pitch und gleiche besetzte/geparkte Reichweite, keine Loop-Neustarts.
  Reaktor-Laufzeitpegel um 50 Prozent angehoben (0,5 auf 0,75); seine sichtbare
  Einblendphase bleibt bei 55–100 Prozent Entfaltung. Audio-WAVs unveraendert.
- Growling startet bei 0 s mit 40 ms Einblendung statt erst nach 0,85 s.
  Bisheriger 3,5-s-Ausschnitt und Pegel erhalten. Zunaechst vier Bewegungstexturen
  aus inside-the-robot-70923 bei 0,22/1,10/2,10/3,52 s ergaenzt; inzwischen
  auf Einzelimpulse gekuerzt (siehe oben). Laser-Charge
  aus gregorquendel-laser-charge-175727 als 2,8-s-Schicht ab 0,35 s zum Testen.
  Zwoelf bestaetigte Einrastakzente erhalten; neue Schichten begrenzen nur sich
  selbst, ohne Growl/Einrasten herunterzuziehen. Keine Animationsaenderung.
- kernreaktor.mp3 liefert einen eigenen 8-s-Loop am echten bewegten
  Reaktormittelpunkt, mit 800 ms kreisfoermiger Ueberblendung aus 8–16,8 s
  der Originalaufnahme. tools/prepare_transformer_reactor_audio.py erzeugt
  reactor.wav und reactor_manifest.json lokal. Zwischen 55 und 100 Prozent
  Entfaltung einblenden, beim Falten entsprechend ausblenden; geparkte Roboter
  behalten den Reaktor auch unabhaengig vom Motor-Weiterlaufen-Schalter.
  Pause/Fokus-/Streamingstopp pausieren den Loop; Entfernen stoppt/raeumt auf.
- Build, 34 Reaktor-, 156 Leerlauf- und bestehende 66 Ladechecks sowie
  61 Verwandlungschecks fuer die neue Balance bestanden; weitere bestehende
  Verwandlungs-/Dreh-/Schritt-/Audiopruefungen bestanden. Zwei Pythonchecks
  fuer Loop-Aufbereitung und zehn Installations-/Stagingtests gruen.
  Bei geschlossenem Valheim lokal installiert; DLL und alle 86 Payload-Dateien
  per SHA-256 geprueft, Konfiguration erhalten, beide Backupordner nach Erfolg
  auf je zwei Zeitstempelstaende begrenzt. Schritte und Drehsamples unveraendert.
  Frueherer Growl, Bewegungstexturen/Laser und Reaktor vom Nutzer bestaetigt;
  neue Pegelbalance lokal installiert, erneute Spielabstimmung offen.

## Transformer: Mechanik fuer beide Verwandlungen (vorherige lokale Stufe)

- Nutzer bestaetigt Rueckweg-Hauptschicht A im Spiel und wuenscht nun
  Einrastakzente sowie weitere Ausschnitte aus moving-tools-3-40372 und
  inside-the-robot-70923 in beiden Richtungen. Je fuenf neue kurze
  Bewegungstexturen verwenden unterschiedliche aktive Quellabschnitte.
  Auto-zu-Roboter behaelt seine bestaetigte Gesamtmischung als unveraenderte
  Grundlage; eine zusaetzliche, separat begrenzte Texturgruppe kommt hinzu.
- Roboter-zu-Auto erhaelt acht gruppierte Akzente: Spiegel lokal bei 2,125 s,
  Kopfrotation/Kompaktierung bei 2,229 s, Fuss-/Zehenwinkel bei 2,958 s,
  Bodenblech-Weltpose bei 3,583 s, Fusskompaktierung bei 4,062 s, lokale
  Gliedmassen-/Hueftbaugruppen bei 4,500 s, Seitenscheiben bei 4,667 s und
  Karosserieabschluss bei 5,000 s. Fruehe Komponentenendpunkte bedeuten noch
  keine stillstehende gesamte Baugruppe; nahe Ereignisse sind gebuendelt.
  Timing gegen echte Auto-Endpose aller verbleibenden Rueckwaertsframes
  geprueft, nicht durch Spiegeln der Vorwaerts-Akzentzeiten.
- Beide Spuren bleiben 5,3 s bei unveraenderter 5-s-Animation. Im Rueckweg
  darf nur der natuerliche Abschluss maximal 0,3 s Nachklang freigeben;
  Auto-Ticks fuehren ihn auch geparkt weiter. Abbruch/Restore/Dispose und
  erneute Verwandlung stoppen ihn; Pause/Fokus/Streaming frieren ihn ein.
  Kein Nachklang beim Laden geparkter Autos, keine verlaengerten Ringe durch
  wiederholte Abschlussmeldungen. Motor-/Reaktorkurven bleiben erhalten.
- tools/prepare_transformer_mechanical_audio.py beschreibt die neuen
  Texturausschnitte, die beiden bisherigen Aufbereitungsskripte mischen sie
  getrennt hinzu. A bleibt isoliert SHA-256-identisch; die bestaetigte
  Vorwaertsmischung ebenso. Keine globale Pegelabsenkung oder Zeitstreckung;
  neue Texturen begrenzen nur sich selbst in den verbleibenden Spitzenraum.
  Exakte Schnitte, Pegel, Quellen-/Ausgabehashes und Timing in den lokalen
  deploy_manifest.json/return_manifest.json. Native Hoerabnahme offen.
- Build sowie 79 Rueckweg-, 81 Lade-, 61 Vorwaerts-, 156 Leerlauf- und 34
  Reaktorchecks bestanden. Sieben Pythonchecks inklusive Originalquellen-
  Regression fuer beide bestaetigten Grundlagen und zehn Installationschecks
  bestanden. Der zusaetzliche komplette Transformertest scheitert an der
  bestehenden Stuetzkontinuitaet im Walk-to-Sprint-Trace: maximal 0,11711 m
  bei 38,433334 s links statt <0,004 m. Isolierter Nachbau nur aus
  TransformerGait.cs/GaitPreview.cs des vorherigen Commits f656fed erzeugt
  denselben Trace byteidentisch; keine Audioregression. Gait/Testdateien
  unveraendert. Herkunft und Reproduktion im ignorierten Bericht
  artifacts/tests/gait-audio-scope-baseline/evidence.json; keine native
  Bewegungsmessung im Spiel und kein vollstaendiger gruener Transformertest.
- Bei geschlossenem Valheim lokal installiert, DLL und alle 88 Payload-Dateien
  per SHA-256 verifiziert. 30 sonstige Audio-Payload-Dateien unveraendert;
  Konfiguration erhalten und beide Backupordner nach erfolgreicher Pruefung
  auf je zwei Zeitstempelstaende begrenzt. Neue Hoerabnahme im Spiel offen.

## Transformer: Roboter-zu-Auto, bestaetigte Hauptschicht (lokale Entwicklung)

- Nutzer waehlt Hoerkandidat A: transformers-growling-31509, Originalsekunden
  6,0–10,2. Die isolierte Growl-Schicht ist SHA-256-bytegleich zur Vorschau;
  4,2 s Mono-PCM16, 40 ms Ein-/250 ms Ausblendung, unveraendertes Tempo/Pitch.
- Sound beginnt erst mit TransformerState.Returning, nach Bremsen, Fussruhe
  und Gelaendeausrichtung. Der bestehende Animationsfortschritt fuehrt die
  Wiedergabe ueber (1 - Progress) * 5; die Animation bleibt fuenf Sekunden.
  Erste 4,2 s enthalten die Growl-Hauptschicht; Mechanik ergaenzt jetzt den
  gesamten Rueckweg samt Abschlussnachklang (siehe oben).
- Pause/Fokus-/Streamingstopp halten die Spur an; Fortsetzen und Framehitches
  synchronisieren zur echten Phase. Kein Sound bei blosser Rueckkehranfrage,
  Vorwaertsverwandlung oder geladenem Roboter. Kein Wiederanlaufen am Clipende.
- Bestehender Reaktor-/Motoruebergang bleibt: Reaktor ueber die ersten 2,25 s
  ausblenden, Motor zur Autoform wieder zum vollen Pegel bringen. Schritte bei
  vorbereitender Fussruhe erhalten. tools/prepare_transformer_return_audio.py
  --source-dir <Ordner> erzeugt return.wav und return_manifest.json lokal.
- Erste Spielstufe mit nur A und bestehendem Uebergang vom Nutzer im Spiel
  bestaetigt. Morph-47358 bleibt eine optionale weitere Quelle.
- Build, 56 Rueckverwandlungs-, 81 Lade-, 61 Vorwaertsverwandlungs-, 156
  Leerlauf- und 34 Reaktorchecks bestanden. Bei geschlossenem Valheim lokal
  installiert; DLL und alle 88 Payload-Dateien per SHA-256 geprueft. Die 32
  vorherigen Audio-Payload-Dateien sind unveraendert. Konfiguration erhalten,
  beide Backupordner nach Erfolg auf je zwei Zeitstempelstaende begrenzt.
  Native Hoerabnahme der damaligen Hauptschicht bestaetigt; erweiterte Mechanik
  separat abzustimmen.

## Transformer: Auto-zu-Roboter-Sound (lokale Entwicklung)

- Spielrueckmeldung: Growling passt; vorherige Einrastakzente zu leise und
  zu wenige. Zwoelf deutlich kraeftigere, kurze Bauteilgruppen-Akzente aus
  passenden Schritt-/Mechanikaufnahmen ersetzen die drei bisherigen Akzente.
  Gelenkwinkel-/Ausfahrenden liefern fruehe Akzente, vollstaendige lokale
  Bauteil-Endstellungen verdichten sich bei 4,85–5,00 s. Paare werden gruppiert.
- Leerlauffehler korrigiert: Der bisherige Fortschritts-Fade schaltete den
  Motorleerlauf bis 72 Prozent Umbaufortschritt stumm. Die Motor-Grundschicht
  bleibt jetzt in beiden Verwandlungsrichtungen durchgehend erhalten,
  ohne Neustart/Hochdrehen. Pause und Motor-Weiterlaufen-Einstellung bleiben aktiv.
- Vorwaerts-Verwandlungsschicht aus den Nutzeraufnahmen: kurzer
  mechanischer Auftakt, 3,5 s transformers-growling-31509 und Einrastakzente
  aus footstep-020-500843, giant-robot-in-space-352492 und kurzen Ausschnitten
  der bestaetigten Schrittquellen. Keine globale Normalisierung; nur Mechanik-
  Spitzen weich begrenzt, Growl-Ausschnitt und Pegel erhalten. Kein Strecken der
  ganzen 21-s-Datei, keine Aenderung der vorhandenen fuenfsekündigen Animation.
- Akzente anhand der aktuellen gebackenen Posen ab etwa 2,42 s; innere
  Ausfahrmechanik um 3,54 s, Kopfnahe-Endstellung um 4,48 s und Abschluss um 5 s.
  Die Karosserie bewegt sich bis zum Ende weiter; die fruehen Akzente sind
  keine Behauptung bereits vollstaendig verriegelter Fahrzeugteile.
- Ein lokal vorgemischter Mono-PCM16-Clip (5,30 s inklusive kurzem Ausklang)
  folgt dem tatsaechlichen TransformerState-Fortschritt. Pause haelt ihn an;
  bei Fortsetzen/Frame-Aussetzern wird zur Animationsphase synchronisiert.
  Geladene geparkte Roboter starten keinen Umbau-Sound. Rueckverwandlung und
  Entfernen stoppen die Vorwaertsspur. Bestehende Schritte/Drehung/Idle erhalten.
- Reproduzierbar mit tools/prepare_transformer_deploy_audio.py --source-dir
  <Ordner>; deploy.wav und separates deploy_manifest.json bleiben lokal.
  Herkunft, Schnitte, Layerpegel, Cuezeiten und SHA-256 dokumentiert;
  Vorschau artifacts/audio-transformer/deploy-preview.wav. Ueberarbeitete
  Mischung anhand der Wellenform; erneute native Hoerabnahme noch offen.
- Build, 61 Verwandlungs-Soundchecks, 35 aktualisierte Leerlaufchecks und
  56 Ladechecks bestanden; neue Mischung auf zwoelf Akzente, unveraenderte
  Growl-Parameter und Spitzenpegel unter 0,95 geprueft;
  bestehende Dreh-/Schritt-/Leerlauf-/Audio-/Speicherpruefungen sowie zehn
  Installations-/Stagingtests gruen. Lokal bei geschlossenem Valheim installiert;
  DLL und alle 84 Payload-Dateien per SHA-256 verglichen. Bestaetigte Schritt-
  und Drehaudiodateien unveraendert, Konfiguration erhalten, beide Backupordner
  nach Erfolg auf je zwei Zeitstempelstaende begrenzt. Ueberarbeitete Mischung
  samt durchgehendem Leerlauf lokal installiert; erneute Spielabnahme offen.
- Roboter-zu-Auto verwendet jetzt den ausgewaehlten Growl A als erste
  Hauptschicht (siehe oben); morph-transforms-47358 bleibt eine optionale
  Ergaenzung. Kein automatisches Rueckwaertsabspielen der Vorwaertsspur.

## Transformer: Drehmechanik (lokale Entwicklung)

- Vier kurze Ausschnitte aus der vom Nutzer gewaehlten dreh sounds.mp3,
  separat von den bestaetigten Schritten und dem Leerlauf. Mechanik folgt
  der tatsaechlich gemessenen Rumpfdrehung, auch beim Gehen; Tastendruck
  allein erzeugt keinen Sound. Unter 2 Grad/s stumm, Pegel folgt Drehtempo.
- Leise kurze Varianten mit Wechsel ohne direkte Wiederholung, begrenzten
  Stimmen und weichen Uebergaengen. Beim Anhalten rund 150 ms Ausklang;
  Menue/Pause, Fokusverlust, Streamingpause, Rueckwandlung und Entfernen
  stoppen sofort. Bestehende Bewegung und bestaetigte Laufsounds bleiben erhalten.
- tools/prepare_transformer_turn_audio.py erzeugt vier Mono-PCM16-Dateien
  (0,9 bis 1,2 s) samt separatem turn_manifest.json in .local/audio-transformer.
  Herkunft, Schnittgrenzen und SHA-256 dokumentiert; Wellenform-Erstschnitt,
  Nutzer bestaetigt den Drehsound im Spiel als passend.
  Vergleich artifacts/audio-transformer/turns-comparison.wav.
- Build, 45 Dreh-Sound- und 46 Ladechecks sowie bestehende Schritt-, Leerlauf-,
  Kamera- und Audiopruefungen bestanden; zehn Installations-/Stagingtests gruen.
  Lokal bei geschlossenem Valheim installiert, DLL und alle 82 Payload-Dateien
  per SHA-256 verglichen. Sechs Schritt-WAVs und ihre Pegel unveraendert;
  Konfiguration erhalten und beide Backupordner nach Erfolg auf je zwei
  Zeitstempelstaende begrenzt. Native Hoerabnahme der Drehmechanik vom Nutzer bestaetigt.

## Transformer: erste Soundstufe (lokale Entwicklung)

- Sechs kurze Schrittvarianten aus drei vom Nutzer ausgewaehlten MP3s,
  an die vorhandenen linken/rechten Fusslandungen gekoppelt. Zufallsauswahl
  verwendet jede Variante einmal pro Durchlauf und vermeidet direkte
  Wiederholungen; dezente Pegel-/Tonhoehenvariation, beim Sprint mehr Gewicht.
  Fusskontakte beim Drehen und beim Abbremsen nutzen dieselbe Ereignisquelle.
- Hoerrueckmeldung: urspruenglichen sechsten Sound (footstep-015-445103)
  wegen seines herausstechenden hellen Tons entfernt; uebrige Varianten passen.
  Schrittlautstaerke linear um 5 Prozent erhoeht. Anpassung gebaut, geprueft
  und bei geschlossenem Valheim installiert. Leerlauf bleibt unveraendert.
- Vorhandener 350Z-Motorleerlauf bleibt mit gleichem Autopegel erhalten,
  auch waehrend der Fortbewegung und des gesamten Umbaus ohne Hochdrehen.
  Parken folgt der vorhandenen Motor-Weiterlaufen-Einstellung. Menue/Pause,
  Fokusverlust und Streamingpause stoppen Schritte ohne spaeteres Nachholen.
- tools/prepare_transformer_audio.py erzeugt lokale Mono-PCM16-Ausschnitte
  samt Herkunft, Schnittgrenzen und SHA-256; Originale/WAVs bleiben ignoriert.
  Vergleichsdatei: artifacts/audio-transformer/footsteps-comparison.wav.
  Nutzer bestaetigt die Laufsounds nach Entfernung der hellen Variante und
  der Pegelerhoehung im Spiel als passend.
- Naechste getrennte Stufen nach der Drehmechanik: Auto-zu-Roboter mit
  transformers-growling-31509 als zusaetzlicher Schicht,
  Rueckverwandlung mit morph-transforms-47358. Einrastakzente aus footstep-020
  und giant-robot-in-space-352492; footstep-017 fuer Schritt oder Einrasten
  vergleichen. Laufsounds bestaetigt; leisen Leerlauf bei Bedarf weiter abstimmen.
- Build, 40 neue Schritt- und 27 neue Leerlaufpruefungen mit kontrollierten
  Unity-Doubles sowie Audio-/Lade-/Kamera-/Speicher-/Kletterregressionen
  bestanden. 116 Python-Tests inklusive Installer/Staging bestanden.
- Lokal fuer den Spieltest installiert: gepruefte DLL und alle 77 Payload-
  Dateien inklusive sechs Schritt-WAVs und Audiomanifest per SHA-256 mit
  dem Build verglichen. Konfiguration erhalten; Basis- und Erweiterungsbackups
  nach Erfolg auf je zwei Zeitstempelstaende begrenzt. Entfernten Schritt auch
  im installierten Ordner entfernt; sechs beibehaltene WAVs unveraendert.
  Native Hoerabnahme der Laufsounds vom Nutzer bestaetigt; dies ersetzt keine
  vollstaendige Laufzeitpruefung aller Pause-/Lade-/Rueckkehrfaelle.

## Abgas im Leerlauf (lokale Entwicklung)

- Dezente hellgraue Abgaswoelkchen direkt an den gewaehlten Endrohren,
  mit eigener prozeduraler Maske und kurzem Ausblenden. NFVU2-Effekt, kein
  rekonstruierter NFS-Leerlaufeffekt. Pro Endrohr maximal sechs Partikel/s;
  Gasgeben und Fahrt blenden ihn aus. Motor aus und Pause stoppen die Emission.
- In Roboterform folgen die Austrittspunkte denselben bewegten Haltern wie
  die sichtbaren Auspuffteile, auch beim Verwandeln und Wiederherstellen.
  Konfigurationsschalter Exhaust.IdleSmoke, standardmaessig aktiv.
- Build und gezielte Emissionschecks; Dichte und Sichtbarkeit im Spiel pruefen.

## Transformer beim Weltbeitritt (lokale Entwicklung)

- Spieltest-Nachkorrektur: Rad-/Bremsenpositionen, Felgengroesse, Innenraum und
  geschlossene Ausbaupose werden vor der Uebernahme in die geladene Roboterform
  initialisiert. Die Radhalter uebernehmen keine ungesetzten Ursprungspositionen.
- Ruecken-Neon: senkrechte lange Roehre samt Traeger entfernt. Die sechs an
  der Huefte schwebenden Leuchten werden durch den Schriftzug SLY unten mittig
  auf dem Kofferraumdeckel ersetzt. Konturen und Halter folgen der echten
  Deckelflaeche; die gesamte Baugruppe folgt starr dessen Verwandlung und Gang.
  SLY auf 65 Prozent verkleinert und leicht hoeher gesetzt, um den Auspuff
  freizuhalten. Eigene dauerhaft gruene Emission ohne Puls oder Abhaengigkeit
  von den Kofferraum-Neoneinstellungen.
- Die aktive Roboterform wird mit Fahrzeugposition und Stauraum im bestehenden
  Welt-Datensatz gespeichert, vor dem Aufraeumen beim Verlassen der Welt.
  Beim Laden steht der Transformer direkt wieder geparkt da; die Verwandlung
  wird nicht erneut abgespielt. Aufsteigen und Rueckverwandeln bleiben moeglich.
- Wiederherstellung erfolgt im vorhandenen gestaffelten Fahrzeug-Ladeprozess.
  Alte Datensaetze ohne Formangabe bleiben Autos; H2 und Flugmodus behalten
  ihre bisherigen Ladewege. Fehlende Transformerdateien lassen den gespeicherten
  Datensatz unveraendert, statt ihn als Auto zu ueberschreiben.
- Build und gezielte Speicher-/Wiederherstellungschecks; nativen Weltwechsel
  mit abgestelltem und besetztem Transformer im Spiel pruefen.

## Transformer: Hochklettern und Backflip (lokale Entwicklung)

- F7/B/Kreis bremst bis zur Fussruhe. Der echte Spielcharakter steht auf der
  Schulterplattform auf, holt in der Hocke aus und springt gleichzeitig
  nach hinten und oben in einen durchgehenden Bogen (2,15 s). Die Flugbahn
  beruecksichtigt die echte Landehoehe und steigt rund 78 cm ueber den
  Absprungpunkt, bevor sie zunehmend steiler abfaellt. Im Salto
  werden Huefte und Knie deutlich angezogen, Ruecken/Kopf eingerollt und die
  Haende zu den Knien gefuehrt; vor der Landung oeffnet sich die Pose. Die Landung
  wird abgefedert, die Roboterform bleibt geparkt bestehen.
- Nahe dem Roboter von jeder Seite F7/B/Kreis: aussen zum hinteren Bein gehen, abwechselnd an
  vorhandenen Metallflaechen greifen und auftreten, mit festen Hand-/Fusskontakten
  hochziehen und oben auf die Plattform steigen. Erst danach in die Sitzpose
  wechseln (rund 8 s). Die Kontaktpunkte werden aus den aktuellen Modelldreiecken gewaehlt;
  der Roboter bleibt beim Klettern ruhig stehen. Der bestehende Avatar mit
  Inventar und Ausruestung wird weiterverwendet.
- Aufnahmerueckmeldung: die starre Drehpose und ruckartigen Kletterzuege
  ueberarbeitet. Eigene Knochenbasis pro Frame verhindert aufaddierte IK;
  Hueftweg, Reichweitenkorrektur und Hand-/Fussausrichtung werden geglaettet.
  Beim Kofferraumdeckel halten zwischengespeicherte Querschnitte der echten
  Deckel-/Spoilergeometrie den Oberkoerper aussen; Knie werden seitlich gefuehrt.
  Beim oberen Uebersteigen erst hinter der Kante hochziehen, dann mit den
  Schritten darueber wechseln. Tiefere Handgriffe werden dabei frueher geloest.
  Linke/rechte Modellkontakte sind passend zum Avatar zugeordnet, damit Arme
  und Beine sich beim Greifen und Auftreten nicht ueberkreuzen.
  Aufrichten und Absprung nutzen eigene Standkontakte auf den hinteren
  Dachkanten (rund 51 cm hinter der Helm-Hinterkante), nicht die vor dem Kopf
  liegenden Sitz-Fusspunkte. Beim Hinsetzen zuerst hinten absenken, dann in
  die Sitzposition schieben; beim Aufstehen entsprechend umgekehrt.
- Keine Seiten-, Hang-, Wasser- oder Freiraumsperren beim Auf-/Absteigen.
  Boden-/Kapselabfragen bevorzugen einen guenstigen Landeplatz, liefern aber
  immer einen Ersatzpunkt. F7 waehrend des Hochkletterns steigt sofort ab.
  Menue/Pause halten die Animation an. Bewegung und Rueckverwandlung warten
  waehrend der Choreografie. Spieler, Kamera, Animator und Fallhoehe werden
  beim Abschluss/Notausstieg wieder freigegeben. Weltladen bleibt unveraendert.
- Build und 49 gezielte Kletter-/Absprungchecks bestanden. Keine Blender-Render.
  Avatarpose, native Kollisionen und Kamerafuehrung im Spiel pruefen.

## Transformer: Kamera bei Fusslandungen (lokale Entwicklung)

- Jede abgeschlossene Fussschwungphase liefert einen Landekontakt an die
  Fahrzeugkamera. Ein kleiner Nick-/Rollimpuls klingt innerhalb von 0,24 s aus;
  linke und rechte Landung rollen gegenlaeufig. Sprint blendet die Staerke
  bis auf das 1,8-Fache. Keine eigene Schrittuhr und kein dauerndes Wackeln.
- Kameraabstand und Hindernispruefung bleiben erhalten. Menue, Pause,
  Fokusverlust, Rueckverwandlung und Ausstieg loeschen den Effekt; blockierte
  Schritte werden beim Fortsetzen nicht nachgeholt. Gang und Fahrphysik bleiben
  bis auf reine Landekontaktzaehler unveraendert. Build und 18 gezielte Checks
  bestanden; Kameragefuehl im Spiel pruefen.

## Orte im Fahrzeug und Fahrzeugmarker (lokale Entwicklung)

- Der deaktivierte Fahrer erhaelt nach der Sitz-/Transformerpose gezielt
  Valheims native Orts-/Biomaktualisierung. Bekannte Orte, Biome und Meldungen
  folgen damit der Fahrzeugposition; Spielerbewegung und Eingabe bleiben aus.
- Mini- und Weltkarte zeigen eigene temporaere Marker fuer 350Z und H2,
  einschliesslich entfernter gespeicherter Autos. Geladene Fahrzeuge liefern
  ihre aktuelle Position auch im Flug und als Transformer. Entfernen und
  Weltwechsel raeumen die Marker auf; persoenliche Pins bleiben erhalten.
- Symbol aus dem vorhandenen originalen NFSU2-Menue: CRIB_ICON_CAR_SELECT
  (FRONTEND/FrontB.lzc, 0xa964095b), ohne Auswahlpfeile. Bei fehlender optionaler
  Menuegrafik dient eine eigene Autosilhouette als Ersatz.
- Build, 9 Entdeckungs- und 22 Kartenchecks bestanden. Orts-/Biommeldungen
  beim Fahren und Marker auf beiden Karten im Spiel pruefen.

## Gespeicherte Autos im Weltladebildschirm (lokale Entwicklung)

- Nahe gespeicherte Autos werden nach nativer Terrain-/ZDO-Bereitschaft vor
  dem Erzeugen des Spielers aufgebaut. Der vorhandene Weltladebildschirm
  bleibt bis zum Abschluss offen; der Logoutpunkt wird erst danach verbraucht.
  Aufbau weiterhin gestaffelt auf dem Unity-Hauptthread, Dateien im Worker.
  Die fertigen Fahrzeuge wechseln anschliessend zum echten Spieler als Besitzer.
  Entfernte Autos warten weiterhin auf ihre Region. Fehler/F8-Abbruch lassen
  den Welteintritt weiterlaufen; Multiplayer bleibt unveraendert.
  Build, 28 Wiederherstellungs- und 31 Ladechecks bestanden. Weltladen,
  Ladeanzeige und sichtbare Fahrzeugbereitschaft im Spiel pruefen.

## Purge beim Rollen (lokale Entwicklung)

- Purge funktioniert mit gehaltener Nitrotaste auch beim Rollen ohne Gas,
  unabhaengig von der Geschwindigkeit; Gasgeben schaltet auf die bestehende
  Boostlogik um. Bodenkontakt und aktive Fahrzeugsteuerung bleiben erforderlich.
  Gezielte Nitrochecks; Partikel und Sound beim Rollen im Spiel pruefen.

## Transformer: Sprint, Handkontakte und Boden V4 (lokale Entwicklung)

- Ausfahrmechanik beim Anlaufen wiederhergestellt: Die vereinfachte
  Bodenfuehrung aktivierte selbst im Stand dauerhaft die offene Terrainphase.
  Ohne diesen unbedingten Wechsel steuert wieder der Gang die Panzertraeger:
  vor dem ersten Schritt ausfahren, nach Fussruhe wieder einfahren.
  Build und kurze Gang-/Sprintchecks bestanden; die volle Transformer-Suite
  scheitert beim separaten Sprint-Trace an der Standfussgrenze (117 statt 4 mm).
  Gangparameter unveraendert; Aus-/Einfahren vom Nutzer im Spiel bestaetigt.
- Aktuell: Reaktor 120 Grad/s, deutlich groessere Flamme vor dem Einfassungsrand
  mit zur Kamera gerichteten Sprites. Lose Holzbeute und eigenstaendige Truemmer
  werden aus Hindernis-, Boden- und Kontaktdeckungsabfragen entfernt; sie hatten
  trotz Baumfilter weiterhin Stopps/Bodenstufen ausloesen koennen. Verbleibende
  feste Blocker werden hoechstens alle zwei Sekunden protokolliert. Build und
  18 Kontaktregeln-Checks bestanden; Flammenansicht/Waldlauf im Spiel offen.
- Spieltest-Nachkorrektur: Reifen drehen um ihre eigene lokale X-Achse nach
  Anwendung des Panel-/Fahrzeugversatzes statt um die Achse des Panelhalters.
  Reaktor 65 statt 38 Grad/s; sichtbare kleine, nach oben gerichtete Flamme
  aus dem vorhandenen Auspuff-Flammenatlas. Baum-/Stammcollider stoppen den
  Rumpf auch waehrend ausstehender nativer Faellung nicht mehr. Build und
  14 Kontaktregeln-Checks; Rad-/Flammenansicht und Durchlaufen im Spiel offen.
- Neuer lokaler Spieltest: Sprintschrittparameter 1,32 statt 1,45 m (rund 9 %
  kuerzer), weiterhin 8,5 m/s und hoher Fusshub. Vier vorhandene Raeder rollen
  bei Drehung seitenabhaengig gegenlaeufig, Bremsen bleiben stehen. Reaktor:
  konkave Metallmulde, kleiner gruener Kern, vorhandene Lamellen/Ringe im
  Uhrzeigersinn (von vorne), dezente Plasmaflamme. Roboterkontakt trifft kleine
  feindliche Kreaturen und alle nativen Baumarten, Baumstämme und Setzlinge
  ueber Valheims Schaden/Faellung/Beute. Spieler, Zahmtiere, Bosse, grosse
  Kreaturen, Gebaeude und Ziele hinter festen Hindernissen bleiben geschuetzt.
  Umfallende Staemme und Kreaturen liefern keine Bodenhoehe. Build sowie
  30 Controller- und 14 Kontaktregeln-Checks; Darstellung/Physik im Spiel offen.
- Aktuelle Fehlerkorrekturen: Ausstiegskapsel ignoriert das eigene Auto und
  probiert weitere Positionen neben der Tuer. Bodenprobe beginnt knapp ueber
  Terrain/Rumpf statt 64 m darueber; stehende Baeume liefern keine Bodenhoehe.
  Sprint-Handflaechen werden zur Koerperinnenseite ausgerichtet statt fester
  55-Grad-Unterarmdrehung. Drehen 90 Grad/s beim Gehen, 65 Grad/s beim Sprint,
  mit frueherer Fussumsetzung fuer schnelle Kurven. Build und kurze Controller-
  tests; Baum-/Tuerkollisionen und Handansicht bleiben im Spiel zu pruefen.
- Aktueller schneller Spielteststand: Sprint 8,5 m/s, Schrittparameter 1,32 m,
  Fusshub 80 cm, Koerperhubziel 32 cm (Controller gemessen 25,55 cm).
  Laengere Flugphasen (maximal 0,20 s statt 0,05 s), niedrigere Schrittfrequenz,
  engere Oberarme (9 bis 12 Grad) und ruhigerer Armschwung (maximal 125 Grad/s).
  Die alte neunfache Ebenenpassung und die pro-Panzer-Gehausschlusspruefung
  sperren die Fortbewegung nicht mehr. Geladene Bodenhoehe fuehrt den Rumpf;
  nur der zentrale Koerper prueft feste Hindernisse. Bestehende Ueberlappung
  erlaubt Weiterbewegung zum Befreien. F7 steigt sofort aus, unabhaengig von
  Rueckwandlung und Fussruhe. F10-Rueckwandlung hat keine Hang-/Freiraumsperre.
  Vorbereitung der Robotermeshes erfolgt beim Fahrzeugladen ueber mehrere
  Frames; die sehr grosse synchrone Umbauwegpruefung entfaellt. Bauch-Neonroehre
  und zugehoerige Halter werden in der Darstellung entfernt. Wasser wird durch
  eine vereinfachte Wathoehe behandelt; fehlende Welt bleibt Streamingpause.
  Build und kurze Controller-/Ladetests; auf ausdruecklichen Wunsch
  keine neuen Renderings oder vollstaendigen Geometrie-/Tracepruefungen.
  Die nachfolgende vollstaendige V4-Freigabe beschreibt den vorherigen Stand;
  die aktuelle lokale Manifestfreigabe ist ausdruecklich ein manueller Spieltest.
- Shift + W sprintet vorwaerts; am Gamepad X/Quadrat halten. Zieltempo 7 m/s,
  Beschleunigung 1,6 m/s2, staerkeres Abbremsen vor Rueckwaertsfahrt. Pose folgt
  weiterhin gemessener Bewegung: weicher Walk-/Run-Uebergang, groessere Schritte,
  staerker gebeugte Arme, kuerzere Stuetzzeiten und kurze echte Flugphasen.
- Gezielte Nachkorrektur: kleinere und ruhigere Sprintarme, naeher am Koerper;
  rund 15 cm sichtbarer Koerperhub aus echter Stuetzkompression und Abhebung.
  Fussbahnen und Schritttiming bleiben erhalten. Gehen erlaubt getrennt vom
  Umbau nun 20-Grad-Haenge und begrenzte Unebenheiten. Kleine bodennahe
  Fuss-/Schienbeinueberschneidungen werden bewusst zugelassen; Wasser, fehlende
  Welt, grosse Absatze und feste Fels-/Wandhindernisse bleiben Sperren.
- Die bisherige V3-Pruefung erfasste Karosserie, Neon und Referenzfigur, aber
  nicht die vollstaendige Handgeometrie. Das erklaerte trotz damaliger Freigabe
  die gemeldeten Hand-/Beinpenetrationen. V4 prueft jedes Hand-/Fingerbauteil
  gegen Bein-/Hueftpanzer mit mindestens 25 mm Volumenabstand und ungefilterten
  Dreieckskontakten. Schulter-/Ellbogenbahn wird fuer die Robotermorphologie
  angepasst; keine verkleinerten oder ausgeblendeten Haende.
- Knieplatten parken vor dem ersten Schritt und bleiben am Rumpftraeger,
  statt beim Walk-/Run-Wechsel ihre Position zu aendern. Dadurch kann beim
  Sprintabbremsen eine ausklingende hohe Schwungbahn keinen zurueckfahrenden
  Kniepanzer treffen. Schliessen erfolgt erst nach vollstaendiger Fussruhe.
- Statt einer pauschalen 6-m-Box entscheiden enge bauteilbezogene Huellen mit
  nativen Kollisionsabfragen ueber den Umbauweg. Sichtbarkeit und kompakte
  Mechanik werden beruecksichtigt. Ein gemeinsamer breiter Suchkasten dient
  nur zum Finden benachbarter Collider, nicht als Hindernisentscheidung.
- Neun Bodenproben unterscheiden leichte Haenge, Stufen, Loecher und Wasser.
  Umbau und Rueckkehr behalten 8 Grad und 6 cm Restunebenheit; beim Gehen
  gelten 20 Grad, maximal 25 cm Restfehler und 30 cm Restfehlerspanne.
  Fusslokale Ebenen und feste Beinlaengen fuehren die Sohlen. Die bisherige
  vollstaendige Terrain-Geometriepruefung umfasst 8-Grad-Haenge und
  10-cm-Stufen zwischen beiden Fussbahnen. Erweiterte Gehgrenzen haben eigene
  Policytests und brauchen eine erneute Abnahme auf echtem Spielterrain. Der
  Originalumbau bleibt parallel zur tragenden Bodenebene; danach
  oeffnen die Panzertraeger, bevor sich der Roboter aufrichtet. Auf Gelaende
  bleibt diese Freiraumstellung beim Anhalten offen. Zurueckneigen, Schliessen
  und inverse Verwandlung folgen der umgekehrten Reihenfolge. Die reine
  Phasensteuerung hat eigene Tests; Weltladepausen halten auch diese Phasen an.
- Erweiterter Geh-Smoke mit 42 Posen: 20-Grad-Haenge und 30-cm-Fusshoehen-
  unterschiede bestehen die begrenzte Bodentoleranz, Handabstand mindestens
  28,9 mm. Auf 20-Grad-Haengen bleiben vier optische Panzer-/Neonkontakte
  (4,5 mm Schweller bzw. 47 bis 59 mm Neon/Hinterrad). Diese neue Gehfreiheit
  ist keine Zusage kontaktfreier Mechanik auf jedem Hang.
- Lokale Original-StartTemple-Collider ausgewertet, einschliesslich gespawnter
  Queen-/Fadersteine und Zentralplattform. Bosssteinfreier Mittelkreis knapp
  14,9 m Durchmesser; die gebackene Umbaugeometrie hat maximal 3,19 m Radius.
  Das ist eine geometrische Referenz, keine Abnahme jeder Weltposition oder
  einer laufenden V4-Pose. Die konvexe Plattform besitzt eine ebene Oberseite.
  Echte Fussvertices liegen darueber, waehrend leere OBB-Ecken bis 41 cm unter
  ihre Ebene reichen. Nachgewiesene konvexe Bodenauflage wird deshalb getrennt
  von echten Hindernissen behandelt, ohne Collider nach Namen zu ignorieren.
- Aktuelle Quellen-/Trace-gebundene V4-Freigabe bestanden: 79 Managed-
  Transformerchecks, 34 Bodenchecks und 115 Python-Tests. Vollstaendige
  Geometriepruefung mit 5.930 Posen (129 Vorbereitung, 5.445 Bewegungsvarianten,
  356 Terrainproben) sowie 3.661 Posen des zusammenhaengenden 61-Sekunden-
  Controllers bestanden. Keine neuen Koerper-/Handkontakte, Bodenverletzungen
  oder IK-Fehler im beschriebenen Pruefumfang; kleinster absolut gepruefter
  Handabstand rund 28,9 mm. Vorhandene Originalkontakte bleiben ausgewiesen.
  Alte V3-Berichte koennen den Sprint nicht aktivieren. Native Unity-Physik,
  Startkreis-Praxis und Spielfeeling bleiben als Spielabnahme offen.
- V4 lokal ueber den bestehenden Installerkern und die separate optionale
  Transformer-Erweiterung installiert. Getestete DLL unveraendert, beide Assets
  per SHA-256 geprueft, Konfiguration erhalten und Backups nach Erfolg auf die
  zwei neuesten Datumsstaende reduziert. Kein Commit, Push oder Release.

## Transformer: menschlicherer schwerer Gang V3 (lokale Entwicklung)

- Transformer-Branch auf 0.30.0 zusammengefuehrt. Persistente Fahrzeuge und
  Weltladen bleiben erhalten; Speichern waehrend der Roboterform sichert eine
  Auto-Rueckkehrhoehe statt des bodennahen Roboter-Ursprungs. F8 bricht wie in
  0.30.0 nur Laden ab; Entfernen bleibt eine ausdrueckliche Menueaktion.
- Schrittschwinger ersetzt: gemessene Rumpfbewegung fuehrt gesetzte Standfuesse,
  getrennte Schwungphasen setzen neue Landepunkte. Vorhersage und laufende
  Reichweitenpruefung beruecksichtigen Beschleunigen, Drehen und Umkehren.
- Vorwaerts 2,8 m/s, rueckwaerts 1,65 m/s; Beschleunigen 0,9 m/s2, Bremsen
  2,3 m/s2. Cruise-Fussweg im Controller etwa 1,46 m, statt kurzer V2-Schritte.
  Arme schwingen gegenlaeufig, Ellbogen beugen sich. Beckenlast folgt dem
  tatsaechlichen Standbein; Oberkoerper neigt sich vor und dreht gegen das Becken.
- Nach vorn gefuehrte Knie mit fester Beinlaenge, schmalerer Gehstand und
  Abrollen auf der echten starren Sohle. Beide Beine teilen notwendige
  Beckenabsenkung; Halsanker und bestehender Fahrer folgen dem Oberkoerper.
- 41 Managed-Transformerpruefungen bestanden. Voller 34-Sekunden-Ablauf mit
  50-Hz-Physik und 60-Hz-Pose umfasst Anlaufen, Bremsen, Kurven, beide Pivot-
  Richtungen, Umkehren und kurzen Neustart. Groesste gemessene Bewegung eines
  gesetzten Fussankers rund 0,283 mm pro Frame im idealen ebenen Test.
- Zusaetzliche sechs 34-Sekunden-Sequenzen am gebauten Controller: kurze Taps,
  beschleunigender Zickzack, Stop/Start-Kurven, kleine Analogwerte, Vor/Rueck-
  Wechsel und schnelle Pivotwechsel. Alle 12.246 Posen behalten mindestens
  einen Standfuss und enden exakt neutral; groesste Ankerabweichung 0,470 mm
  pro Frame. Das misst Knoechelanker, nicht nativen Unity-Sohlenkontakt.
- Vollstaendige Geometriepruefung bestanden: 4.485 Posen aus Ausfahren,
  Vor-/Rueckwaertsfahrt und Drehen plus 2.041 Posen des zusammenhaengenden
  Controllerablaufs. Im damaligen Karosserie-/Neon-/Figurpruefumfang keine neuen
  Oberflaechenkontakte, Bodenverletzungen oder IK-Fehler; Haende fehlten noch,
  siehe V4. Urspruengliche Ruhepose erhalten. Kniepanzer und ganze Neonroehren
  fahren auf verbundenen Halterungen aus dem Bewegungsweg.
- V3 lokal mit dem bestehenden Installerkern und separatem optionalen
  Transformerpaket installiert; DLL/Assets per SHA-256 geprueft und bestehende
  Konfiguration erhalten. Backup vor Austausch, alte Datumsbackups erst nach
  erfolgreicher Pruefung reduziert. Nicht gepusht oder veroeffentlicht.
  Die reale Unity-Spielabnahme bleibt getrennt; weiterhin nur ebene freie
  Flaechen, keine Terrain-IK und keine Multiplayer-Freigabe.

## 350Z-Transformer: lokale Blender-Studie

- Eigenstaendige Roboterform mit segmentierter Karosserie, artikulierten Armen
  und Beinen, schmaler Taille und gefaltetem Rueckenpaket. Studien bis V18 sind
  Blender-Prototypen; ein erster optionaler Laufzeitimport ist unten beschrieben.
  Blender-Studien allein aendern die Mod nicht; lokale Laufzeitintegration siehe unten.
- Verbindliche Gestaltungsregel: jede Scheibe bleibt ein starres, ungeteiltes
  Bauteil. Front- und Heckscheibe, Seitenscheiben und Spiegel behalten ihre
  Quell-Dreiecke, UVs und Normalen; auch Fassungen/Halterungen mitfuehren.
- Spieler soll beim Umbau auf der Schulterbruecke hinter dem Kopf mitfahren
  und bei der Rueckverwandlung wieder auf dem Fahrersitz sitzen. Den bestehenden
  Player aus VehicleCabinDriver weiterverwenden: Inventar, Ausruestung, Gesundheit
  und Netzwerkidentitaet erhalten; Sitzanker/Posenziele animieren.
- Blender zeigt dafuer eine generische Referenzfigur, nicht den echten Avatar.
  Gelenkgehaeuse, Huelsen, Rippen und ein mitfahrender Sitztraeger sind modelliert.
- Studie 6: Knie klappen nach oben; Beinpanzer und Hinterraeder erhalten
  Bodenfreiheit. 664 sichtbare Meshes in 529 Vor-/Rueckwaerts-Zeitpunkten
  inklusive halber Frames gegen die Bodenebene geprueft (ground-clearance.json).
- Studie 7: sieben durchgehende Neonleisten mit Halterungen. Drei folgen den
  Kofferraumleisten aus VehicleShowroom; vier Unterbodenroehren sind neu
  modellierte Hardware zu den horizontalen Lichtpositionen aus VehicleNeon.
  Die seitlichen Leisten fahren beim Umbau auf ihren Haltern nach aussen.
- Eingezogene Arme/Kopf vorn, Beine hinten; vier teleskopierende Gliedmassen.
  Panzertraeger nutzen feste ineinanderschiebende Huelsen oder feste Klappglieder.
  Lange Verbindungen laufen unter dem Fahrerraum, Scheibentraeger seitlich.
  Die Figur zieht beim Anheben die Beine an und nimmt anschliessend die Sitzpose ein.
- Bewegungswege sind als gepruefte bzw. nachzubessernde Schiebekurven in
  tools/transformer_carrier_paths.json und tools/transformer_neon_paths.json
  reproduzierbar. Pruefungen unterscheiden Boden, Fahrzeug/Neon/Figur,
  Mechanik/Figur sowie Packstellung. Griffkontakt der vereinfachten Haende
  ist explizit ausgewiesen; sonstige Kontakte werden nicht verschwiegen.
- Weiter offen: verbleibende Karosseriekontakte in Zwischenstellungen und
  Bauraum-/Montagekontakte der inneren Mechanik. Unity-Integration folgt nach
  der weiteren Kollisions- und Bauraumarbeit.
- Studie 8 korrigiert das Durchschwenken der Haubenhaelften durch beide
  Scheinwerfer: erst seitlich vor den Leuchten schliessen, danach nach hinten
  an die Brust anlegen. Anfangs werden die Hauben angehoben; beim Hochfahren
  nutzen sie einen vorderen Korridor. Zum Schliessen passieren sie die vordere
  Neonroehre von oben. Rueckweg in exakt umgekehrter Reihenfolge.
  Haube/Leuchten-Test: 1057 Viertelframes inklusive Rueckverwandlung,
  keine neuen Flaechenschnitte bei 0,01 mm Toleranz; vorhandener Kontakt der
  Quellgeometrie separat erfasst. Vorschauen einzeln mit zwei CPU-Threads.
- Studie 9 ist eine eigene statische Design-Endpose: sechs verlustfrei geschnittene
  Haubensegmente, acht Tuerschuppen mit Original-UVs, ausgeformter Brust-/Bauchkern,
  kraeftigere Gliedmassen, Gelenkgehaeuse, neues Gesicht und einzelne Fusszehen.
  Vier Raeder, ganze Scheiben/Leuchten, sieben ungeteilte Neonroehren und dieselbe
  Referenzfigur bleiben erhalten. Neon sitzt in Schutzkanaelen; Sitz hinter dem Kopf.
  Reproduzierbar mit tools/build_transformer_design.py; eigene Blender-Datei,
  vier Ansichten und optionaler Kameraumlauf in artifacts/transformer-study-v9.
  Der Kameraumlauf zeigt KEINE Transformation. Studie-8-Animation gilt fuer diese
  neue Form nicht; Faltwege, innere Kontakte und Packung im Auto bleiben offen.
- Studie 10 korrigiert die Endpose nach Designfeedback: originale Frontleuchten
  frei vor den Schultern, Rueckleuchten nach hinten gerichtet; Lichtemission
  nach den vorhandenen lightRole-Feldern des Fahrzeugmodells. Vordere Raeder
  nach aussen/oben geneigt als Schulterpolster, neuer Kopf mit schmalen Optiken
  und mehrteiliger Metallmaske. Unterbodenflaechen verlustfrei in 30 starre
  Segmente aufgeteilt und um die Beine gefaltet; hintere Gelenke, Kolben und
  Fersenanlenkung ergaenzt. Ganze Scheiben, vier Raeder und sieben Neonroehren
  bleiben erhalten. Erzeugen mit tools/build_transformer_design.py --revision 10
  im Blender-Python-Aufruf; Ausgabe artifacts/transformer-study-v10.
  Sechs Ansichten und optionaler Kameraumlauf, weiterhin keine Transformation.
  Statische Boden-, Fahrer- und Neonpruefungen; weitere innere Karosseriekontakte
  sind dokumentiert und offen, ebenso Packstellung und die neuen Faltbewegungen.
- Studie 11 ersetzt Kopf/Hals und Haende anhand der neuen Bildreferenzen:
  ueberlappende Schaedelplatten, tiefe Augensockel mit kleinen gruenen Optiken,
  freiliegende Wangen-/Kiefermechanik und verzahnte Metallkiefer. Beide Haende
  besitzen vier dreigliedrige Finger und einen gegenueberliegenden Daumen,
  Knochellager, Zugstangen und gekruemmte Greifspitzen mit unterschiedlicher
  Fingerstellung. Karosserie, Leuchten, Schulterraeder, Neon und Referenzfigur
  behalten die Studie-10-Geometrie und -Pose. Aufruf --revision 11; Ausgabe
  artifacts/transformer-study-v11, inklusive Kopfprofil und Handnahaufnahme.
  Statische Flaechenpruefung der neuen Teile gegen Karosserie, Neon und Figur;
  innere Selbstkontakte, Gesichts-/Fingeranimation und Packstellung sind offen.
- Studie 12 korrigiert die vertauschte Haendigkeit aus Studie 11: Daumen und
  Zeige-/Kleinfingerfolge passen nun zur nach vorn gerichteten Handrueckseite.
  Finger und Greifspitzen beugen zur Handflaeche nach hinten. Keine negative
  Objektskalierung; Fingerachsen halten den Panzer auf der Handrueckenseite.
  Kopf und uebrige Meshes bleiben unveraendert. Aufruf
  --revision 12; artifacts/transformer-study-v12 mit Nahansichten beider Haende.
- Studie 13 dreht die Handflaechen zur Beinseite, korrigiert die Daumen-/Fingerfolge
  fuer nach vorn gerichtete Daumen und fuehrt die Handgelenke unter der
  Tuerpanzerung heraus. Schmalere Taille, verjuengter Brustkorb und schlankere
  innere Beinkerne; offener gruener Bauchreaktor mit Halteringen und Streben.
  Kuehl-/Hydraulikleitungen besitzen Anschluesse, Rippen und Haltebaender.
  Die zwei breiten hinteren Unterbodenstreifen werden verlustfrei in vier
  Hueftsegmente gefaltet. Originalauspuffe, sichtbarer Waermetauscher und neue
  Rohrboegen ersetzen die flache untere Rueckenansicht; Spoiler hoeher gefaltet.
  Sieben durchgehende Neonroehren bleiben erhalten; hintere Roehre neu positioniert.
  Aufruf --revision 13; artifacts/transformer-study-v13 mit zwoelf Ansichten,
  Reaktor-/Heckdetails und Kameraumlauf. Weiterhin eine statische Designstudie.
  Boden, Fahrer, Kopf/Haende, Haube/Leuchten und Neon ohne Befund in den jeweiligen
  statischen Flaechenpruefungen. Neue Leitungen/Reaktor gegen Karosserie, Neon und
  Figur geprueft; enge Endanschluesse am jeweiligen Auspuff separat ausgewiesen.
  Sonstige Karosserie-/Mechanikkontakte bleiben dokumentiert und offen;
  Selbstkontakte, Schlauchverformung, Packstellung und Transformation ungeprueft.
- Studie 14 setzt die Kopfvorlage als eigene 3D-Geometrie um: geschlossener
  schwarz-gruener Helm, schraege Brauen mit je fuenf roten optischen Linsen,
  seitliche Lueftungen und eine spitze silberne Maske mit Atemgitter.
  Aufruf --revision 14; artifacts/transformer-study-v14 mit frontaler Kopfansicht,
  Profil, Gesamtansichten und Kameraumlauf. Nur Kopf-Meshes ersetzt; uebrige
  Meshes inklusive Hals, Haende, Karosserie, Neon und Figur gegen Studie 13
  auf unveraenderte Geometrie, UVs und Welttransformationen verglichen.
  Kopf-/Handflaechen gegen Fahrzeug, Neon und Figur ohne Schnittbefund;
  innere Kopfkontakte, Gesichtsanimation und Packstellung weiterhin offen.
- Studie 15 ersetzt die dreieckig facettierten Kopfflaechen durch 38 geformte
  Platten mit Quad-Ringen, gerundeten Ecken und schmalen Metallraendern.
  Vertiefte schwarze Augengehaeuse mit gewoelbten Linsen; Stirnschlitz zwischen
  separaten Platten, versenkte Wabengitter und kleine Schlaefenleitungen.
  Lack/Metall erhalten getrennte Rauheit, Mikrostruktur und Bearbeitungsspuren.
  Aufruf --revision 15; artifacts/transformer-study-v15. Normale Ansichten
  behalten die bisherige Beleuchtung und Figur fuer den direkten Vergleich.
  tools/render_transformer_head_portrait.py erzeugt zusaetzliche Cycles-Portraets
  mit gerichteter Studiobeleuchtung; nur dort wird die Referenzfigur ausgeblendet.
  Die gespeicherte Blender-Datei wird durch diese Portraets nicht veraendert.
  1383 Meshes ausserhalb des Kopfes gegen Studie 14 unveraendert; Kopf-/Hand-
  Flaechenpruefung gegen Fahrzeug, Neon und Figur ohne Befund. Innere Kopfkontakte,
  Packstellung und Transformation weiterhin offen; noch kein Film-Asset-Finish.
- Studie 16 entfernt Stuhl, Lehne und Griffbuegel. Dieselbe Referenzfigur sitzt
  direkt am Nacken auf flachen Hals-Panzerauflagen; Oberschenkel umschliessen den
  Hals, Unterschenkel haengen nach vorn. Gliedmassenlaengen bleiben konstant.
  Der ganze Originalspoiler klappt an das obere Rueckenmodul; seine Original-
  Fuesse sitzen in sichtbaren Gelenklagern mit Anlenkung am Rueckenrahmen.
  Die hintere Unterboden-Neonroehre samt Schutzkanal steht nun senkrecht am
  Ruecken oberhalb der Auspuffmechanik. Sieben ganze Neonroehren bleiben erhalten.
  Aufruf --revision 16; artifacts/transformer-study-v16 mit Nacken-/Profilansicht
  der Figur und Nahaufnahme der Rueckenbefestigungen. Kopf aus Studie 15 behalten.
  Gezielte statische Fahrer-, Kopf-, Neon-, Boden- und Befestigungspruefungen
  bestanden; beabsichtigter Kontakt der Spoilerfuesse mit Gelenklagern separat
  erfasst. Alle 336 Kopf-Meshes und die Original-Fahrzeuggeometrie erhalten.
  Weiterhin statische Endpose, noch keine neue Transformationsanimation.
- Studie 17 beginnt die Umsetzung mit einer Armature und 157 Bones fuer Koerper,
  Finger, Fahrzeugmodule, Neon und Referenzfahrer. Originale Fahrzeug-Meshes/UVs,
  ganze Scheiben, vier Raeder, Kopf, Haende und sieben Neonroehren bleiben erhalten.
  Innere Armmechanik wird schmaler; die acht Tuerplatten bekommen in der
  Funktions-Grundpose 20cm Abstand nach vorn. Dadurch werden Ueberschneidungen
  der zuvor nur statischen Armkonstruktion bearbeitet. Acht starre Plattenhalter
  werden durch verschachtelte Huelsen mit festen Laengen, Gelenklagern und
  sichtbaren Endanschluessen ersetzt. Spiegel folgen ihren vorderen Tuerplatten.
  Der Test entriegelt die Ellenbogenplatten, beugt jeweils einen Arm, bewegt
  dessen Finger und kehrt in die Grundpose zurueck. Schulterhub und Fahrertransfer
  sind noch nicht Bestandteil dieses Clips; keine Auto-Verwandlung vortaeuschen.
  tools/build_transformer_rig.py erzeugt artifacts/transformer-study-v17,
  tools/check_transformer_rig.py prueft Gelenke, Traeger, Boden und Flaechenkontakte,
  tools/render_transformer_rig.py rendert den Testclip sequenziell, wahlweise mit
  CPU-Cycles oder --engine eevee. Nie mehrere Blender-Prozesse gleichzeitig.
  car-module-targets.json rekonstruiert die 59 urspruenglichen Fahrzeuggruppen aus
  den spaeteren Teilstuecken. Diese Zielmatrizen sind noch keine geprueften Faltwege.
  Gelenk-/Traeger-/Bodenpruefung und gezielte Flaechenpruefung bestehen an 417
  Zeitpunkten einschliesslich halber Frames. Keine neuen geprueften Kontakte;
  bestehende Kontakte werden getrennt beruecksichtigt. Huelsen behalten mindestens
  30 Prozent Ueberdeckung, Gelenke ihre Laenge; der Zyklus kehrt exakt zurueck.
  Vollstaendige mechanische Eigenkollisionen und eingeschlossene Volumen sind
  weiterhin offen. Flaecheninhalt und Grenzen aller 59 Auto-Quellgruppen stimmen.
- Studie 18 setzt auf ausdruecklichen Nutzerwunsch eine filmische Kompaktierung
  statt vollstaendig physikalischer Packung um. Zusaetzliche innere Robotermechanik
  darf sich komprimieren und in geschlossener Autostellung ausgeblendet sein.
  Originalkarosserie, ganze Scheiben, Leuchten, vier Raeder, sieben Neonroehren
  und dieselbe Referenzfigur bleiben sichtbar und formstabil. Kopf und Haende
  entfalten sich aus der kompakten Mechanik; die detaillierte V17-Endpose bleibt.
  Der neue Ablauf verbindet Auto, Umbau, Nackenpose und Rueckverwandlung.
  Neue mitgefuehrte Teleskophalter ersetzen statische Plattenstreben; auch ihre
  innere Kompression ist ein bewusster Animationstrick. Neonhalter ziehen ein,
  der Fahrer folgt nach dem Oeffnen der Kabine der tatsaechlichen Nackenauflage.
  tools/build_transformer_cinematic.py erzeugt die Ausgangsanimation,
  tools/refine_transformer_cinematic.py wendet die reproduzierbaren Ausweichwege
  aus tools/transformer_cinematic_clearance.json an. Der optionale Fitter
  tools/fit_transformer_cinematic.py arbeitet auf der mit --no-fit gespeicherten
  Zwischenfassung; anschliessend erneut verfeinern und unabhaengig pruefen.
  tools/check_transformer_cinematic.py prueft Quellgeometrie/UVs, feste Aussenmasse,
  Fahrer, Boden, Gelenk-/Traegeranschluesse, Haube/Leuchten und Neon-Flaechenkontakte.
  tools/render_transformer_cinematic.py --animation --cinematic rendert die
  Vorschau mit Kamerabewegung; ohne --cinematic ist die Kamera zur Kontrolle fest.
  Ausgabe artifacts/transformer-study-v18; Blender weiterhin einzeln mit zwei
  Threads. 529 Zeitpunkte einschliesslich halber Frames bestehen die genannten
  Pruefungen; Quellgeometrie/UVs erhalten, Auto-Rueckkehr exakt. Das temporaere
  Ausblenden innerer Mechanik gilt im Render. Im Blender-Viewport bleibt sie
  zur Kontrolle sichtbar, da zwei gleichzeitig getriebene Sichtbarkeitsflags
  in Blender 2.80 inkonsistent ausgewertet wurden. Verdeckungsqualitaet und
  sonstige Mechanik-/Karosseriekontakte brauchen
  weitere visuelle Pruefung; die Studie ist noch kein Unity-/Spielprototyp.
- Naechste Umsetzungsschritte: Sichtbare Uebergaenge der Kompaktierung und weitere
  Karosserie-/Mechanikkontakte verfeinern; ganze Scheiben, Leuchten und Neon erhalten.
  Danach Stand-/Lauf-/Drehbewegungen, Mesh-/Materialoptimierung und ein isolierter
  Unity-Prototyp mit Kamera, Kollisionskoerpern und Moduswechsel.
  VehicleCabinDriver fuer denselben bestehenden Player erweitern; Inventar und
  Ausruestung behalten, Nacken-/Fahrerpose und Rueckkehr animieren. Erst nach
  lokalen Spieltests in die installierte Mod uebernehmen.
- Pruefen: Karosseriepfade, ungeteilte Scheiben, Fahrerfreiraum in jeder Phase,
  Verdeckung der kompakten Mechanik im Auto, Bodenkontakt und rueckwaerts identische
  Fahrzeugpose. Flaechenschnittpruefungen ersetzen keine Volumen-/Physikpruefung.
- Vor Spieleinsatz: Sitz-/Bein-IK, Kamerafuehrung, Ein-/Ausstieg, Notausstieg,
  Tod/Despawn und Wiederherstellung der Animator-/Visual-Einstellungen testen.


## Transformer: erster lokaler Integrationsstand

- Quellenabgleich: lokales und installiertes `body.json` stimmen per SHA-256
  ueberein. Aktuelle Raeder/Bremsen samt Tuning und gewaehlte Auspuffteile werden
  direkt vom gespawnten Auto uebernommen; die V18-Studienauswahl ersetzt sie nicht.
- `tools/export_transformer_runtime.py` erzeugt ein optionales, an das konkrete
  Fahrzeug gebundenes binaeres Asset. Originalmaterialien, Leuchtflaechen,
  Scheibentoenung und getrennte Neon-Einstellungen werden weiterverwendet.
  Die Referenzfigur wird durch IK-Ziele fuer denselben lebenden Player ersetzt.
- `TransformerAsset`, `TransformerVisual`, `VehicleTransformer` und die
  gesonderte Steuerungsanbindung implementieren Verwandlung, Standmodus,
  Nackenpose, Kamera und Rueckkehr. Originales Fahrzeughandling bleibt erhalten.
  Gespeicherte Renderer-/Physik-/Fahrerzustaende werden bei Rueckkehr und Abbruch
  wiederhergestellt. Standardmaessig ausgeschaltet; lokal als Teststand installiert
  und aktiviert. Nutzer bestaetigt die Optik im Spiel anhand von Front-/Rueckansicht.
- Reaktor nach Spielbild: Plasma-Emission fuer Valheim auf 15 Prozent reduziert,
  damit der Kern die umgebende Mechanik nicht ueberstrahlt. Korrektur gebaut;
  erneuter optischer Spielvergleich steht aus.
- Gehen/Drehen V1: getrennte Vorbereitung der Kniepanzerung, Zweigelenk-IK,
  gleitend gefuehrte Wadenpanzerung und gegenlaeufige Fussschritte beim Drehen.
  W/S gehen langsam vor/zurueck (0,85 m/s), A/D drehen (32 Grad/s).
  F10/F7 fordert erst Bremsen, Absetzen der Fuesse und Schliessen der Panzerung
  an; danach beginnt die unveraenderte Rueckverwandlung. Reaktor-Emission reduziert.
  Der gepruefte lokale Teststand ist mit Backup und SHA-256-Vergleich installiert;
  grundlegendes Gehen wurde inzwischen vom Nutzer im Spiel bestaetigt.
- Gang V2 nach Nutzerfeedback: schrittweise Beschleunigung ueber etwa drei
  Sekunden auf 2,1 m/s; Schrittauslenkung von 0,22 auf 0,48 m vergroessert und
  weiterhin geschwindigkeitsabhaengig. Rueckwaerts bis 1,4 m/s, staerkeres Bremsen
  vor Richtungswechsel/Rueckkehr. Arme schwingen bis 20 Grad gegenlaeufig zur
  Fussposition statt um eine Viertelphase versetzt; Ellbogen beugen sich mit.
  Eigener Geometrie- und Controller-Audit bestanden; lokal mit Backup und
  SHA-256-Vergleich installiert. Die Spielabnahme dieser Variante folgt.
- Die Freigabe im lokalen Asset ist an gepruefte Quell-Hashes gebunden:
  3379 geometrische Posen fuer Oeffnen, Teilschritte, Vor-/Rueckwaertsgehen,
  Pivot- und Kurvenbewegungen; zusaetzlich Bewegungswerte des echten Managed-
  Controllers. Sieben Neonroehren, Leuchten, Scheiben und vier Raeder bleiben starr.
  Bestehende Ueberlappungen in der geschlossenen Standpose sind separat erfasst:
  Beim Ausfahren duerfen nur diese Montagekontakte auseinanderlaufen, ohne
  hoehere gemessene Querschnittstiefe; beim Gehen gilt der Dreiecksvergleich.
- Weiter offen: Spielabnahme fuer Fussruhe, Hindernis-Sweeps, Kamera, Fahrer,
  getunte Radvarianten und Performance. Unebenheiten, Stufen, Wasser und Abgruende
  werden konservativ gesperrt. Innere Leitungen/Mechanik sind nicht vollstaendig
  auf Selbstkontakt geprueft. Keine allgemeine Kollisionsfreiheitsgarantie.
- Managed-Import-/Zustands-/Regressionstests ersetzen keinen Valheim-Test.
  Live-Avatar und Roboterform sind auf den Nutzerbildern sichtbar; Materialwirkung
  wird nachkorrigiert. Kamera, Notausstieg, vollstaendige Rueckkehr und Performance
  brauchen weiterhin gezielte Spieltests. Keine Idee aus `scratch_3.txt` wurde als Auftrag
  uebernommen. Anleitung und genaue Grenzen: `docs/TRANSFORMER-PROTOTYPE.md`.

## Dauerhafte Fahrzeuge (0.30.0)

- 350Z/H2 werden mit Position, vollständiger Ausrichtung, Flugzustand und
  bisheriger Fahrt gespeichert. Fahrzeugdaten und Stauraum liegen im selben
  nativen Welt-ZDO; auch leere Fahrzeuge bleiben erhalten. Keine externe
  Inventarkopie oder weltübergreifende Fahrzeugdatei.
- Wiederherstellung beim Laden der jeweiligen Umgebung; entfernte Fahrzeuge
  zählen weiterhin zum Limit von vier Autos. Bestehende Autos werden bei
  entladenem Boden festgehalten, ohne dafür entfernte Terrainjobs anzufordern.
- Weltwechsel, Plugin-Cleanup und Ladeabbruch löschen keine Fahrzeugmarkierung.
  Ein Hook vor Game.Shutdown sichert die letzte Pose vor Abbau der nativen Views;
  PrepareSave erfasst den aktuellen Stand auch vor normalen Weltspeicherungen.
- Versteckter Stauraum wird bereits beim nativen Laden verborgen und gegen
  Verschleiß, Schaden und Hammerentfernung geschützt. Nur „Fahrzeug entfernen“
  im Modmenü löscht das Auto; belegter Stauraum bleibt als normale Kiste zurück.
  F8 dient nur noch zum Abbrechen eines laufenden Ladevorgangs.
- Alte Rückgabekisten haben keine Fahrzeugdaten und werden nicht umgewandelt.
  Einstellungen bleiben wie bisher pro Modell in der Modkonfiguration.
- Automatische Speicher-/Streamingtests prüfen leere/belegte Autos, Wiederanbinden
  ohne Inventarkopie, Flugpose, fremden Weltkontext, Entladen und explizites Löschen.
  Build und vollständiger Prüflauf bestanden; zusätzlich 48 Stauraum-, 17 Welt-
  koordinations- und 32 Streamingprüfungen bestanden. Lokalen Stand bei geschlossenem
  Spiel mit Backup und SHA-256-Abgleich installiert; bestehende Assets erhalten.
  Nutzer bestätigt den Weltwechsel mit erhaltenem Fahrzeug im Spiel.
  Weitere native Spieltests offen: beide Modelle, vier Fahrzeuge, Welten A/B, Neustart,
  Entfernen/erneutes Speichern, entfernte Zonen, Flug und Welt-Backup-Wiederherstellung.
- Release-Build 0.30.0: vollständiger automatischer Prüflauf bestanden, inklusive
  74 Python-Prüfungen und aller C#-Suiten. Installer mit 90 Payload-Dateien und
  27 Radiotiteln; aktuelle DLL, alle Payload-Hashes, eingebettetes Payload/Changelog,
  gemeinsame Versionsmetadaten und EXE-Prüfsumme verifiziert.
- Version 0.30.0 auf GitHub veröffentlicht. Annotierter Tag v0.30.0 zeigt auf
  Release-Commit a8fd0fe. Beide Draft-Assets heruntergeladen und per SHA-256
  mit lokalen Dateien verglichen; öffentliche Release-Seite, Downloads und
  latest-API geprüft. Die echte Update-Assembly des veröffentlichten Installers
  0.29.0 erkennt 0.30.0 und akzeptiert das EXE-/Prüfsummenpaar. Der vollständige
  Update-Klickablauf auf der echten Installation bleibt der nächste Nutzertest.

## Installer-Updates (Release 0.29.0)

- Installer fragt beim Oeffnen asynchron den neuesten oeffentlichen stabilen
  GitHub-Release ab; gemeinsame Major.Minor.Patch-Version numerisch vergleichen.
  Neue Dateien ohne r-Zusatz; alte Release-Dateien bleiben lesbar, deren Revision
  beeinflusst die Update-Reihenfolge nicht. Mod und Installer beide 0.29.0.
  Ohne Internet bleibt der eingebettete Stand installierbar, manueller Neuversuch.
- Changelog im eigenen Fenster: neue Release-Notizen bei verfuegbarem Update,
  sonst eingebettete CHANGELOG.md. Schliessen per Schaltflaeche, Escape und Enter.
- Update-Schaltflaeche laedt den neuen Installer, prueft Dateigroesse, SHA-256
  und vorhandene GitHub-Digests. Fortschritt/Abbruch, Zeitlimits und festgelegte
  GitHub-Downloadziele; keine Anwendung ungepruefter oder unvollstaendiger EXEs.
- Neuer Installer uebernimmt den gewaehlten valheim.exe-Pfad und setzt die
  Installation mit seinen eigenen Spiel-/Pfad-/Backup-Pruefungen fort. Ohne
  ausgewaehltes Spiel oeffnet sich die neue Version zur normalen Auswahl.
- Bereits veroeffentlichte r6-EXEs erhalten keine Funktion nachtraeglich: einmal
  den neuen Installer beziehen. Version 0.29.0 ist auf GitHub veroeffentlicht.
- Vollstaendiger automatischer Prueflauf und 50 gezielte Update-Pruefungen bestanden;
  echte GitHub-Abfrage erkennt jetzt 0.29.0 ohne r-Zusatz. Offline- und simulierte Update-
  Oberflaeche gerendert. Uebergebener Spielpfad samt automatischer Installation
  mit der fertigen EXE im isolierten kuenstlichen Spielordner erfolgreich geprueft.
- Release 0.29.0: 90 Payload-Dateien, 27 Radiotitel, aktuelle DLL, eingebettetes
  Payload/Changelog und EXE-Pruefsumme geprueft; kein Transformer im Paket.
  Echter Wechsel zwischen veroeffentlichten Versionen, Windows SmartScreen/UAC,
  Offline-/Proxyumgebungen und DPI separat testen.
- Annotierter Tag v0.29.0 zeigt auf den Release-Commit b972c23. EXE und SHA-256
  erst als Draft hochgeladen, beide erneut heruntergeladen und per SHA-256 mit
  lokalen Dateien verglichen. Oeffentliche Release-Seite, beide Downloads und
  latest-API nach Veroeffentlichung geprueft. README-Link danach aktualisiert.

## Weltladen bei langen Fahrzeugfahrten (lokale Korrektur)

- Fehlerursache im lokal vorhandenen Spielcode nachvollzogen: Player.LateUpdate
  setzt ZNet.SetReferencePosition. Beim Einsteigen deaktiviert die Mod Player;
  bislang blieb der Lade-Mittelpunkt dadurch am Einstiegsort stehen.
- Die Mod fuehrt den nativen Lade-Mittelpunkt jetzt mit dem lokalen Fahrer mit:
  beim Einsteigen, vor der Fahrzeugphysik, nach der Kabinenpose und beim Ausstieg.
  Gilt fuer 350Z/H2, Fahrt/Flug und auch bei offenen Menues oder Eingabesperren.
- Nutzer bestaetigt groessere Reichweite, meldet aber viele Stopps und dauernden
  Ladehinweis. Spiellog zeigt haeufige Hold/Resume-Paare an fast gleicher Position.
  Die erste Sperre war zu streng: IsAreaReady wartet auf benachbarte ZDO-Objekte,
  IsZoneLoaded zusaetzlich auf Dekorationen. Beide sind kein reiner Bodentest.
- Zweite Korrektur: pro gefahrenem Fahrzeug nur aktive Terrain-MeshCollider mit
  vorhandenem Kollisionsmesh unter der Karosserie und 0,08 s voraus pruefen
  (maximal 8 m). Der native Lade-Mittelpunkt folgt weiter dem tatsaechlichen Fahrer.
  Fertige Boden-Collider geben die Fahrt frei, waehrend Weltobjekte weiter laden.
- Gelandedaten bis zwei Sekunden voraus (maximal 112 m) ueber die nicht blockierende
  HeightmapBuilder.IsTerrainReady-Abfrage vorbestellen: hoechstens vier verschiedene
  Jobs pro Viertelsekunde, mit Breite/Skalierung des echten Zonen-Prefabs. Native
  Builder-Threads berechnen die Daten; ZoneSystem erzeugt die Weltobjekte weiter.
  Kein eigener Task fuer Unity-Objekte, kein RequestTerrainSync und kein Busy-Wait.
- Nur unmittelbar fehlender Boden haelt das Fahrzeug. Unterbrechungen bis 0,25 s
  erhalten den Schwung; laengere starten aus dem Stand. Bildschirmhinweis erst
  nach 0,75 s und einmal je Wartephase. Ausstieg/Despawn geben die Sperre frei
  und erhalten den vorherigen Kinematic-Zustand.
- Automatische Adaptertests fuer entfernte Ladepositionen, Flughoehe, Vor-/Rueck-
  und Seitwaertsbewegung, diagonale Zonengrenzen, verzoegerte Dekoration ohne
  Fehlstopps, fehlende/inaktive Collider, nicht blockierende und begrenzte
  Vorbestellungen, kurze/lange Wartephasen, Wiederanlauf und Aufraeumen.
  Fahrphysik selbst bleibt unveraendert.
- Build und gezielte Streaming-/Handling-/Flug-/Kabinen-/Steuerungs-/Ladetests
  bestanden. Lokaler Teststand mit geprueftem Backup und SHA-256-Abgleich installiert;
  bestehende Konfiguration erhalten, experimentelles Transformer-Payload entfernt.
- Spielrueckmeldung zur zweiten Korrektur: weiter verbessert; verbleibende Ruckler
  beim Flug in unentdeckte Gebiete. Letztes Spiellog vom 15.10.2026 enthaelt keine
  Schutz-Holds/Resumes, aber 522 native Standort-Erzeugungen, davon 94 ueber 30 ms
  und 27 ueber 60 ms; groesster geloggter Einzelvorgang rund 446 ms (WoodVillage1).
  Das belegt native Generierungsspitzen, ist jedoch keine vollstaendige Frame-
  oder CPU/GPU-Profilierung. Ein zusaetzlicher Einfluss des Prefetch ist nicht
  separat gemessen; keine weitere Laufzeitaenderung allein aus dieser Vermutung.
- Spieltest offen: mehrere Kilometer in neue und bereits erkundete Gebiete fahren
  und mit Nitro fliegen, danach landen/aussteigen; 350Z und H2, Wasser/Berge,
  langsames Nachladen, Menues/Pause. Adaptertests ersetzen kein Unity-Streaming.
- Transformer bleibt ausschliesslich im lokalen Branch wip/350z-transformer.

## Innenseiten der Fahrzeugflächen (lokale Korrektur)

- Nutzer hat den lokalen Gesamtstand einschließlich der Bremssattelkorrektur
  im Spiel optisch bestätigt; die folgenden Prüfpunkte dokumentieren den Umfang.
- Bremssattel: schwarze Ring-Decals lagen vor bereits geschlossenen roten
  Gehäuseflächen. Decals entfernt; Naben-/Schrauben-Atlasrechtecke durch
  Metallflächen ersetzt. Nahansichten mit/ohne Felge und Spielbild bestätigt.
- Drittes Nutzerfeedback: glatte Haubenunterseite und Teile vor der Frontscheibe.
  Donor-Armaturenbrett und Verkleidung ragten messbar durch die originale Scheibe.
  Anpassung hinter Scheibe/Dichtung einschließlich angrenzender Dreiecke;
  Prüfung mit Eckpunkten, Kantenmitten und Flächenmittelpunkten.
- Haube: eigene konturierte Graphit-Innenhaut, Verstärkungsprofile, vier matte
  Einsätze und Befestigungen. An STYLE07 statt flacher Serienunterseite angepasst;
  äußeres Carbon, Lüftungsöffnungen und Haubengelenk erhalten. Keine Behauptung
  einer originalgetreuen Nissan-/NFS-Innenkonstruktion. Freigang bei allen drei
  Motoroptiken und externe Ansichten prüfen; Materialwirkung im Spiel bestätigen.
- Zweites Nutzerfeedback: Rückseiten allein schließen offene Türkanten,
  Kofferraumrand und abgeschnittene A-Säulen nicht. Innenraum-Lader blendete
  ganze originale Türverkleidungen und BASE-Innenschale aus; Innenraumimport
  entfernte sämtliche Dreiecke über 1 m Höhe.
- Originale Türkanten bleiben sichtbar. Hintere Innenschale aus BASE separat
  exportiert, bei allen vier Ausbauten sichtbar; altes Cockpit bleibt verborgen.
  Volle A-Säulen und Dachhimmel importiert, Dachhimmel an NFS-Dachunterseite
  angepasst. Blender-Vorschau prüft offene Türen, Heckrand und Dach von beiden
  Seiten; native Beleuchtung und Kameranähe bleiben im Spiel zu bestätigen.
- Nutzerbilder zeigen Durchblick durch Dach, offene Türen/Heckklappe und
  Radinnenseiten. Importierte Flächen erhalten Rückseiten mit eigenen nach
  innen gerichteten Normalen; Scheiben behalten ihre Tönungssteuerung.
- Rückseiten bleiben im selben Mesh und folgen Tür-/Klappengelenken sowie
  Felgengröße und Radbewegungen. Audio-Membranen behalten ihre Vertexzuordnung.
- Automatisch: Dreiecksrichtung, Normalen, UV/Farben, unveränderte Quelldaten
  und große Indexpuffer prüfen. Im Spiel Dach von unten, Türen/Heckklappe offen
  und geschlossen, Räder innen, Scheibentönung und Beleuchtung prüfen; auch H2.
- Rückseiten ergänzen vorhandene Flächen; weitere fehlende modellierte Bauteile
  müssen bei Bedarf gesondert ergänzt werden.

## Release 0.28.0 / Setup r6

- Öffentlicher Funktionsstand seit 0.22.3: Hordenmodus mit Minimap und Pakten,
  Audio-/Schild-/Bosskorrekturen und Fahrzeug-Stauraum. Setup mit kompletter Musik.
- Nutzerfeedback bestätigt das Grundspielprinzip des Hordenmodus. Die jüngste
  Boss-Schadensbegrenzung und Stauraum-Speicherung/Bedienung bleiben gezielte
  Spieltests; automatisierte Adapterprüfungen ersetzen diese nicht.
- Setup-Code unverändert; vollständige automatische Prüfungen bestanden,
  90 Payload-Dateien, aktuelle DLL, 27 Radiotitel und eingebetteter Payload
  per SHA-256 geprüft. Echter Wizard-/DPI-/Spielstarttest
  auf sauberer und bestehender Installation bleibt separat zu prüfen.

## Fahrzeug-Stauraum in 0.28.0

- 350Z Serie / Ausbau 1 / 2 / 3: 24 / 18 / 12 / 6 Plätze; H2 fest 32.
  Lokale native Prefabs geprüft: Holzkiste 5×2, verstärkte Kiste 6×4,
  Schwarzmetallkiste 8×4. Kapazität folgt Gehäuse, nicht Subwoofer-Marke.
- Im Stand aussteigen, ans Heck gehen, F9 → Fahrzeug → Stauraum öffnen.
  Normales Valheim-Inventar; jede Fahrzeuginstanz hat eigenen Inhalt.
- Verkleinern prüft alle 350Z und sperrt bei zu vielen Stapeln/offenem Inventar.
  Umsortieren behält Iteminstanzen mit Metadaten. Externe Konfigurationswechsel
  dürfen eine belegte Kapazität ebenfalls nicht unterschreiten.
- Native persistente Kiste speichert im Weltstand. Beim Entfernen/Weltwechsel
  bleibt gefüllter Stauraum als normale Kiste zurück; neues Auto beginnt leer.
  Beim Flug liegt der Wiederherstellungsort am Boden darunter. Ausgeladene
  Kisten bleiben im ZDO, keine zweite Inventarkopie und kein Ersatz beim Unload.
- Automatisch: Größen, Grenzwerte, verlustfreies Umpacken, Sperre bei Zugriff,
  Positionsfortschreibung, Freigabe, leere Kiste und Unload/Reload geprüft.
- Spieltest: DE/EN und Controller, Drag/Drop/Split/Take-all, volle Kiste bei
  Ausbauwechsel/Standard-System, mehrere Autos, entfernte Zonen, F8, Weltwechsel
  und Save/Reload. Rückgabekiste auf Hängen/Bauwerken/Wasser sowie Flug prüfen.

## Boss-Schadensspitzen und Einstiegshinweis in 0.27.2

- Nur Moder: Schaden um 45 % gesenkt, maximal 180 Schaden je akzeptiertem
  Treffer (225 in Zornphase) nach allen Multiplikatoren. Höchstens ein Treffer
  pro Sekunde aus ihren Atem-/Geschosssalven. Andere Gegner bleiben wie bestätigt.
- Im bereiten 350Z unter der Minimap: „Stelle dich der Horde“ und
  „Numpad 9 · Kampf beginnen“, inklusive englischer Übersetzung. Folgt Map und
  Biomtext; verschwindet während Modus/Menüs/großer Karte sowie beim Aussteigen.
- Automatisch: Salven, Caps, normale Gegner, Trefferintervalle und Übersetzung
  geprüft. Im Spiel Bossbalance und Hinweisposition bei verschiedenen HUD-Skalen prüfen.

## Horden-Feinschliff in 0.27.1

- Blaue Farbpulse bei Blitztreffern entfernt; Blitzbögen und Eisfärbung bleiben.
- Walkürenpakt in Welle 5 aus dem Angebot entfernt. Drei verschiedene Angebote
  und mindestens zwei neue gegenüber der vorherigen Welle bleiben erhalten.
- Gegnerischer Angriffsschaden im Hordenmodus für den nächsten Test +20 %;
  Pakt-/Zornphasen-Aufschläge kommen multiplikativ hinzu. Unfallschaden unverändert.
- Spieltest: neue Überlebensbalance und Blitzoptik beurteilen.

## Elementarmunition und Drachenfinale in 0.27.0

- Keine Wartezeit zwischen Wellenende und Paktauswahl. Drei aus sechs Angeboten,
  mindestens zwei davon unterscheiden sich von der letzten Welle.
- Eis friert normale Hordengegner 1,2 s ein (danach 2,4 s Immunität), färbt sie
  weißblau und verlangsamt Bosse um 35 %. Nachladezeit dafür +20 %.
- Kettenblitz springt bis zu dreimal, maximal 8 m je Sprung, mit sinkendem Schaden
  und 0,4 s Auslöseabstand; Deckung und fremde/zahme Gegner werden berücksichtigt.
  Direkte Kugeltreffer verursachen dafür 20 % weniger Schaden.
- Walkürenpakt: Automatik für die Welle, dafür +25 % gegnerischer Schaden.
- Finale: Moder + 2 Drakes + 4 Fulinge. Native 7.500 Basis-HP, Flug/Bodenwechsel,
  Zornphase ab halben HP. Horden-Automatik auf 60 m erweitert. Die lokalen
  Dragon/Hatchling-Prefabs und nativen Boss-/Flugfelder wurden geprüft.
- Keine Freischaltung des normalen Moder-Fortschritts, keine Bossbeute;
  Geschosse werden begrenzt/aufgeräumt, dauerhafte Eishindernisse unterdrückt.
- Automatisierte Zustands- und Adaptertests ersetzen keinen Spieltest:
  Frostoptik/Animationen, Blitzsichtbarkeit, Flug-/Landeverhalten auf normalem
  Gelände, Zornphase, Geschossbereinigung, Paktauswahl und Balance prüfen.

## Gemischte Wellen und Götterpakte in 0.26.0

- Neue Wellen: 15 / 20 / 18 / 14 / 13 Gegner, insgesamt 80 geplante Spawns.
  Waldgegner, Skelette, Draugr, Wölfe, Fulinge und Berserker; zum Abschluss
  ein markierter Troll als Waldtyrann mit Lebensbalken. Keine neuen Boss-Assets.
- Vor jeder Welle Odin / Thor / Eiserner Wagen oder ohne Pakt wählen.
  Vorteil und Nachteil gelten genau eine Welle. Keine dauerhaften Upgrades.
- Abschluss ab aufgerundeten 85 % tatsächlicher Kills und allen besiegten
  Elitegegnern; übrige kleine Gegner werden ohne Punkte entfernt.
- Optionaler 25-Sekunden-Jagdauftrag in Wellen 2–4: +500 Punkte / +250 HP.
  Neuer Bestwert für die geänderten Regeln; alter Bestwert bleibt erhalten.
- Automatisiert: Spawnlimit, Paktauswahl, Wellenquoten, Pflichtgegner,
  Bonusfrist, Buff-Ablauf, Nachladen, Überfahren und Explosionsschaden geprüft.
- Im Spiel noch testen: gemischte KI auf normalem Gelände, native Resistenzen,
  Schamanen-Heilung, Balance der Berserker, DE/EN-Paktauswahl mit Maus/Controller,
  Bossmarkierung und HP sowie Odin/Thor nach Entladen und Wiederladen einer Zone.
  Erst danach Zahlen nachjustieren und weitere Pakte/Gegnertypen ergänzen.

## Gegner-Radar in 0.25.5

- Lebende Hordengegner als rote 9-Pixel-Punkte mit dunkler Kontur auf der kleinen
  Minimap. Lokales UI-Overlay, keine gespeicherten/geteilten Pins, keine Änderung
  von Kartenfiltern oder Erkundung. Große Karte bleibt unverändert.
- Positionen in LateUpdate aus lebenden eigenen Charakteren, bei entladenen
  Instanzen aus deren vorhandenem ZDO. Tote/fehlende Gegner ausblenden;
  maximal 100 wiederverwendete UI-Punkte. Tod, Reset, Sieg/Niederlage und
  Weltwechsel entsorgen Overlay, Sprite und Textur. HUD-Ausblenden beachten.
- Projektion entspricht der lokal geprüften Minimap-Implementierung:
  Welt X/Z, Pixel-/Texturgröße und aktuelle UV-Ansicht. Randabstand verhindert
  überstehende Punkte. 14 Prüfungen für Richtungen, Bewegung/Zoom, verschiedene
  Kartengrößen, Ränder und ungültige Werte; bestehende Horde/Wettertests bestanden.
- Im Spiel Anzeige bei 720p/1080p, 90 Gegnern, Minimap-Zoom, Kartenöffnung/-schließen,
  Gegnersterben und Wellenwechsel prüfen. Native UI-Maskierung/Lesbarkeit ist
  nicht Teil der automatischen Projektionstests.

## Nacht und Gewitter in 0.25.4

- Start blendet über acht Sekunden zur Mitternacht und zum nativen ThunderStorm-
  Wetter. Zeitwechsel mit weicher Beschleunigung, Himmel/Nebel/Regen/Gewitter
  über EnvMans bestehenden Wetterübergang. Keine Änderung der ZNet-Weltzeit.
- Sieg, Niederlage, Numpad 0, Ausstieg und Fahrzeugentfernung wechseln zu Mittag
  und Clear. Der Übergang läuft auch nach Freigabe des Fahrzeugs weiter. Klarer
  Tag bleibt für diese Weltsitzung; Welt verlassen/Mehrspieler/Plugin entladen
  stellt vorherige lokale Zeit-/Wetter-Overrides und Übergangsdauer wieder her.
- 17 Adapterprüfungen: Start, Abend/Mitternacht/Sonnenaufgang/Mittag, Halten,
  früher Abbruch, wiederholtes Cleanup, schneller Neustart, ungültiges Delta,
  vorherige Overrides, neue Welt, fehlende Wetterpresets und Dispose. Weitere
  Horden-/Leichenprüfungen bestanden. Im Spiel native Wettereffekte und
  Hörbarkeit des Turrets bei Gewitter prüfen; automatischer Test rendert nicht.

## Schild über Scheiben und Turret-Priorität in 0.25.3

- Nutzerbilder zeigen fehlende Waben über Seiten-/Heckscheiben. Codebefund:
  Glas und Schild teilten Renderqueue 3000. Schild erhält eigenes Material
  mit Queue 3100, ohne ZWrite und mit beibehaltenem Tiefentest. Pickup-Glow
  bleibt unverändert. Im Spiel rund um das Auto bei klaren/dunklen Scheiben
  prüfen; Wände und Karosserie müssen verdeckte Schildteile weiter verdecken.
- Nutzer hört Turret bei vielen gleichzeitigen Gegnerangriffen nicht mehr.
  Schüsse bisher Standardpriorität 128. Im besetzten Fahrzeug jetzt 16,
  Aktionen 32, Nachhall 80, Servo 160; Radio weiterhin 0. Nach Ausstieg
  normale Priorität für geparkte Geschütze. Automatik und manueller Betrieb
  verwenden dieselbe Regel; keine globalen SFX-Pegeländerungen.
- Audio-Adapter prüft Prioritätswechsel ohne Wiedergabeneustart zusätzlich
  zum bestehenden Dauerschuss-/Quellenlimit. Tatsächliches Verdrängen oder
  akustisches Überdecken durch Gegnergeräusche bleibt im Spiel zu prüfen.

## Schildkuppel und Kampfaudio in 0.25.2

- Nutzer bestätigt verbesserte Schildoptik. Jetzt zusammenhängendes Hexagon-
  Raster auf einer größeren Halbkugel statt einzelner Wabenplatten. Sichtbare
  Grundhelligkeit bei jedem positiven Ladestand, langsames Schimmern und
  hellerer Treffer-/Aufladeimpuls. Bei 1/150/300 Schild, Tag/Nacht und allen
  Kamerawinkeln prüfen; Turret und Motorhaube sollen frei erkennbar bleiben.
- Pickup heißt Explosivmunition / Explosive ammunition; Horde-Reset und
  Bereits-aktiv-Meldung ohne „Test“. Übersetzungsprüfungen bestanden.
- Nutzer meldet Radioaussetzer beim Schießen. Codebefund: 3 s Nachhall pro
  Schuss als unbeschränkte OneShots, Radio-Priorität 128 statt nativem Musikwert 0.
  Vermutete Ursache: Verdrängung durch zu viele Audiostimmen; kein Laufzeitbeweis.
  Radio im besetzten Fahrzeug jetzt Priorität 0, außen wieder 128. Schüsse
  begrenzt auf vier Quellen, Nachhall auf zwei mit 40 % Pegel. Die bestehenden
  0,34-s-Schussanfänge bleiben bei 450 Schuss/min vollständig hörbar.
- Adaptertest mit 1.000 Schüssen prüft Quellenlimit, vollständige Transienten,
  Pegeländerung und Ausklingen. Radio-/Turret-/Horden-/Sprachtests bestanden.
  Radio im Stand/Fahren, manuelles und automatisches Dauerfeuer, Explosivmunition,
  Pause sowie Fahrzeugwechsel akustisch im Spiel prüfen. Keine globalen Mixer-
  oder Audio-Geräteeinstellungen geändert.

## Hordenmodus-Feinschliff in 0.25.1

- Nutzer bestätigt das Spielprinzip. Start weiterhin ausschließlich im
  gelandeten 350Z mit Turret; H2 und Start zu Fuß im Spiel gegenprüfen.
- Eigene Ragdolls separat über den nativen Setup-Callback erfassen: keine
  gespeicherte Beute, nicht persistent, nach drei aktiven Sekunden entfernen,
  bei Massenkills maximal zwölf. Abbruch/Sieg/Niederlage bereinigen sie sofort.
  Tests prüfen geladene/entladene Leichen, Limit, Reset, verspäteten Callback
  und Weltwechsel; normale Weltleichen bleiben unberührt. Loot-Sperre und
  Marker beim Wiederladen eines eigenen Gegners erneut setzen.
- Größere umrandete HUD-Schrift ohne graue Standardboxen, schmale HP-/Schild-
  Balken, fünf Wellenmarkierungen, Combo-Zeitleiste, farbige Boni und schwebende
  Pickup-Texte. Layout aus tatsächlichen C#-Zeichenbefehlen in DE/EN bei
  720p/1080p gerendert, mit Ersatzschrift ohne Spielszene; native Schrift,
  Lesbarkeit bei Sonne/Nacht und Überschneidungen mit Spiel-HUD noch prüfen.
- Drei Schildringe ersetzt durch eine transparente Wabenhülle in einem Mesh,
  schwaches wanderndes Schimmern und kurzer Impuls bei Schildtreffer/Aufladung.
  Keine zusätzlichen Lichtquellen oder Collider. Bei leerem Schild aus;
  reine HP-Treffer erzeugen keinen neuen Schildblitz. Material, Silhouette
  und Sicht vom Fahrersitz brauchen den Spieltest.

## Fahrzeug-Hordenmodus in 0.25.0

- Nutzer bestätigt den gesamten Fünf-Wellen-Ablauf. Ausbau zum Arcade-Modus:
  Start im gelandeten 350Z mit geladenem Turret, 1.000 HP/300 Schild, nur Gelände.
  Native Gegnerwaffen treffen IDestructible am Auto; Fahrertreffer werden im
  Modus umgeleitet. Schild absorbiert zuerst. Baum-/Bergbau-Zusatzschaden einer
  Waffe wird nicht als zusätzlicher Autoschaden gezählt. Gegnertreffer im Spiel
  gezielt mit Grauling, Steinwurf, Brutling und Troll bestätigen; reine Gift-AOEs,
  die nativ keine Objekte treffen, treffen die Karosserie ebenfalls nicht.
- Harte seitliche/frontale Hinderniskontakte ab 8 m/s kosten HP/Schild; maximal
  200 pro Einschlag, kurze Sperre gegen Kontaktwiederholungen. Gegner, kleine
  Vegetation, normale Bodenkontakte und bloßes Entlangschrammen ausgenommen.
- Orange: 30 Explosivschüsse mit frischem Magazin; Violett: 15 s Autoturret;
  Blau: +150 Schild; Grün: +250 HP. Vier lokale Runen mit Lichtkegel und Label,
  Ablauf nach 45 aktiven Sekunden bzw. großer Entfernung, Neuplatzierung auf
  freiem trockenem Boden. Durchfahren auch bei hohem Tempo prüfen; volle
  HP/Schild dürfen den entsprechenden Pickup nicht verbrauchen.
- HUD: HP/Schildbalken, Welle/Restgegner, Punkte/Combo, Munition und Auto-Timer;
  Schildringe am Auto mit Trefferblitz. 720p/1080p, Tag/Nacht und DE/EN im Spiel
  prüfen. Menü/Pause/Fokusverlust halten Timer und eigenen Schaden an.
- Fünf schnelle Abschüsse erhöhen den Multiplikator bis x5; nach fünf Sekunden
  ohne Kill zurücksetzen. Sieg mit Zeit-/Überlebensbonus, Gold/Silber/Bronze,
  Ergebnisanzeige und lokalem BestScore. Gold: mindestens 750 HP und höchstens
  300 Sekunden; Silber: mindestens 400 HP, sonst Bronze. Ausgelassene oder
  ungeladen verschwundene Gegner verhindern Rekordwertung; keine Punkte durch
  Entfernen/Abbrechen. Letzten Tod vor Abschlusswertung berücksichtigen.
- Niederlage beendet den Durchlauf und lässt das Auto für einen neuen Versuch
  stehen. Numpad 0, Ausstieg, Tod/Teleport, Fahrzeugentfernung und Weltwechsel
  räumen Gegner, Pickups, Schadenadapter und temporäre Boni auf. Fahrmodus und
  Turret-Einstellungen werden nicht überschrieben. Fremde geparkte Turrets
  bleiben während des Durchlaufs inaktiv. Wiederherstellung im Spiel prüfen.
- Vorhandene funktionierende Testwelt ist der erste Arena-Kandidat. Kein neuer
  Seed validiert oder angelegt; eine große ebene trockene Fläche ist entscheidend.
  Lichtpunkte werden dynamisch geprüft. Bestehende Explosivkrater bleiben aktiv.
- Neue Logik-/Adaptertests prüfen HP/Schild, Boni, Punkte, native Trefferweitergabe,
  Crash-Ausnahmen und Freigabe nach Modusende. Unity-Treffer, sichtbare Runen,
  Balance und tatsächliche Frametimes müssen noch im Spiel bestätigt werden.

## Wald-Wellen in 0.24.0

- Nutzer bestätigt: 30 Graulinge laufen problemlos. Jetzt fünf Wellen testen:
  60 Graulinge; 90 Graulinge; 50 Grauzwerge/8 Brutlinge/2 Schamanen;
  40 Grauzwerge/10 Brutlinge/1 Troll; 30 Grauzwerge/3 Trolle.
- Numpad 9 startet, Numpad 0 entfernt alle Testgegner und den ausstehenden
  Ablauf. Nach der letzten besiegten Kreatur acht Sekunden Pause; nach Welle 5
  Abschlussmeldung. DE/EN-Ankündigungen sowie Wellen-, Restgegner- und Pausenanzeige.
  Der Restzähler enthält noch ausstehende Spawns; HUD-Ausblenden beachten.
- Gruppen von vorn und schräg seitlich, 30–45 m entfernt; Trolle 45–60 m
  voraus mit an der Prefab-Kapsel bemessener Platzprüfung. Gruppen richten sich
  während der Fahrt neu aus. Native KI, Wege, Heilung, Angriff und Platzbedarf
  aller neuen Gegner sowie Lesbarkeit bei 720p/1080p im Spiel prüfen.
- Ein Durchlauf gleichzeitig, maximal 100 lebende Testgegner, höchstens ein
  Spawn bzw. drei Platzprüfungen pro Frame. Kein Platz: einzelne Slots auslassen;
  bei komplett fehlgeschlagener Welle abbrechen. Fehlende Prefabs im Log melden.
- Menü/Chat/Inventar, Fokusverlust und Pause halten den Ablauf samt Countdown an.
  Abbruch während Spawn/Kampf/Pause, Tod, Teleport, Weltwechsel, Sieg und Neustart
  prüfen. Keine Beute oder gespeicherten Gegner; tote Gegner vor Folgewelle entfernen.
- Logiktests prüfen exakte Gegnerzahlen, Reihenfolge, letzte Überlebende,
  Zeitsteuerung, Obergrenze, Platzfehler, Abbruch und Neustart. DE/EN geprüft;
  tatsächliche Frametimes und Wirkung des Troll-Finales benötigen den Spieltest.

## Testhorde für Aufnahmen in 0.23.0

- Numpad 9: bis zu 30 Graulinge ohne Sterne, in einem Bogen 30–45 m vor
  Spieler bzw. Fahrzeug. Numpad 0 entfernt ausschließlich diese Testhorde.
  Num Lock einschalten. Keine Beute; Gegner werden nicht gespeichert.
- DE: „Der Wald erwacht – eine Horde greift an!“; EN: „The forest awakens –
  a horde attacks!“ Die Sprache folgt F9 → Einstellungen → Sprache.
- Einzelspieler auf offener Wiese: zu Fuß, im 350Z/H2 und mit Turret testen;
  Anlaufen, Überfahren, Beschuss und Frametimes beobachten. Der Nutzer bestätigt
  inzwischen ausreichende Leistung für 30 Graulinge; Ausbau folgt in 0.24.0.
- Tasten wiederholen/halten, laufende Welle zurücksetzen und nach Besiegen neu
  starten. Menü/Chat/Inventar, Pause, Alt-Tab, Tod, Teleport und Weltwechsel
  prüfen; kein verspäteter Spawn. Geistermodus verhindert natives Anvisieren.
- Wasser, Hänge, Wald, Felsen und Gebäude: belegte Stellen auslassen, bei zu
  wenig Platz kleinere/keine Horde melden. Startpunkt bleibt während der Welle
  fest; neue Gegner halten mindestens 18 m Abstand zum Spieler. Weiter als
  65 m vom Start entfernt endet der noch ausstehende Spawn.
- Automatische Tests prüfen Begrenzung, Zeitsteuerung, Abbruch und Übersetzung;
  native KI, Terrain/Kollisionen und tatsächliche Rechnerlast bleiben Spieltests.

## Fadenkreuz im Fahrzeug in 0.22.3

- Das native Valheim-Fadenkreuz wird beim Sitzen im 350Z und H2 unabhängig
  vom Geschütz ausgeblendet. Die eigene Geschütz-Zielmarkierung bleibt erhalten.
- Im Spiel ohne Geschütz, mit ein-/ausgeschaltetem Geschütz, im Flug und nach
  Ausstieg/Fahrzeugentfernung prüfen; zu Fuß muss das normale Fadenkreuz zurückkehren.

## Rust-Menülogo in 0.22.2

- Drehung des Rust-Logos erfolgt jetzt vor Menüskalierung und Positionierung;
  der Sticker bleibt dadurch am Turret-Eintrag.
- Der Nutzer bestätigt den Fix nach Installation von 0.22.2 im Spiel.
- Im Spiel bei Standardposition und verschobenem Menü, großer und kleiner
  Auflösung sowie DE/EN prüfen: Logo innerhalb der Turret-Zeile, Anzeige ohne Logo.
- Drei animierte Ingame-Vorschauen zeigen Front, Heck und Flugmodus in der README.

## Kleinerer Radio-Installer (Setup-Revision 3)

- 27 Songs als Ogg Vorbis mit Zielbitrate 192 kbit/s, lokale WAV-Master erhalten.
- Lokaler Setup-Vergleich: 746,68 MB (r2) auf 148,30 MB (r3), rund 80 % kleiner.
- Vollständiges Zurückdekodieren, Stereo/Abtastrate/Dauer und Prüfsummen werden
  automatisch geprüft. Songwechsel, Titelreihenfolge, Pause/Fortsetzen und
  Klangvergleich mit WAV im Spiel prüfen; insbesondere Becken, Gesang und Bass.
- WAV-zu-OGG-Update entfernt die bisher vom Installer verwalteten WAV-Dateien;
  persönliche Dateien bleiben erhalten. Spielstände und Radioeinstellungen
  bleiben vom Paketwechsel unberührt.

## Controller und Nitro in 0.22.1

- Installation und Spielstart mit dem vorherigen Wizard wurden vom Nutzer bestätigt.
- Flugmodus mit Controller starten, landen, aussteigen und Fahrzeug entfernen:
  keine dauerhafte Vibration. Auch Menü, Weltwechsel und erneuten Einstieg prüfen;
  normale Valheim-Vibration nach dem Aussteigen muss weiter funktionieren.
- Fahrertür vor Einstieg/Ausstieg blockieren: zentrale Spielmeldung in DE/EN.
- Nitro am Boden/im Flug prüfen: sichtbarer Randfilter und sanfter Sichtfeldwechsel,
  Fahrzeug bleibt scharf. Loslassen, Pause, Menü, Alt-Tab und Ausstieg stellen
  das Sichtfeld zurück. Effekt aus/Stärke null bleibt ohne Bildänderung.
- Automatische Logik-/Adaptertests ersetzen diese Unity-/Controller-Prüfung nicht.

## Windows-Installer und öffentlicher Release

- Neuer EXE-Wizard: Neuinstallation, Update, Deinstallation und optionales
  Entfernen des selbst installierten BepInEx. Seit Setup-Revision 2 wird Musik
  immer mitinstalliert; Logo im Wizard und in der README, LICENSE mit Close/Escape.
- Automatische Tests verwenden temporäre Spielverzeichnisse. Den echten Wizard
  auf sauberer und bereits modifizierter Valheim-Installation manuell prüfen:
  Dateiauswahl, Berechtigungen, Skalierung, Spielstart und Fahrzeugspawn.
- Paketinhalt zusätzlich in einem isolierten Verzeichnis installiert,
  aktualisiert und mit BepInEx deinstalliert. Das Layout von Setup-Revision 4
  wurde zusätzlich per WinForms-Rendering geprüft; weitere DPI-Prüfungen offen.
- Windows-EXE und SHA-256 werden lokal in `artifacts/installer/` erzeugt.
  Erster öffentlicher Release: `v0.22.1`, Setup-Revision 4. Der Build selbst
  veröffentlicht nichts; EXE und Prüfsumme werden als Release-Assets hochgeladen.
- Zwei klar beschriftete Blender-Vorschauen des 350Z stehen in der README.
  Durch echte Ingame-Screenshots ergänzen/ersetzen, sobald passende Aufnahmen
  vorliegen; ein kurzer Gameplay-Clip kann später als GIF eingebunden werden.
- Entwickleranleitung: [CONTRIBUTING](../CONTRIBUTING.md),
  Versionsgeschichte: [CHANGELOG](../CHANGELOG.md).

## Turbo und kleine Bäume in 0.22.0

- Bei ca. 1–3 km/h kleine Buchen/Tannen/Büsche vorwärts/rückwärts umfahren;
  nach Stillstand am Stamm weiter Gas geben. Seitlicher Kontakt, Rollen ohne
  Gas, Bremse/Handbremse, Parken, Menü und Flug dürfen keine künstlichen Treffer
  erzeugen. Gegner bleiben unter 9 km/h ohne Kollisionsschaden; Wände schützen.
- 350Z: Teil-/Volllast, hohe Drehzahl ohne Last, abruptes und graduelles
  Gaswegnehmen, Schaltvorgänge und erneutes Gasgeben hören. Blow-off richtet
  sich nach vorherigem Ladedruck und darf ohne Last nicht losgehen.
- Alle elf Zifferblätter: −30/0/+30, kurzer Unterdruck-Flattereffekt, digitale
  Geschwindigkeit in KM/H/MPH, 720p/1080p, H2 ohne Turboinstrument prüfen.
- Unter Last Menü/Pause/Alt-Tab, Ausstieg, Rückwärtsgang und Flug: keine alten
  oder künstlich ausgelösten Blow-offs. Menü-Klangwahl bleibt hörbar.
- Originale NFS-Trigger-/Druckkurve und Semantik der fünf Turbo-Substreams
  bleiben offen. 1/2/3-Zuordnung sowie Druck/Gain/Flattern sind Annäherungen;
  tatsächlichen NFS-Laufzeitverlauf später mit Hör-/Telemetriedaten vergleichen.
- `tools/render_audio_preview.py` erzeugt aus kompiliertem Soundmodell eine
  Motorvorschau und `350Z-Turbovergleich.wav`: 0–5 s Teilgas, 5–10 s Vollgas,
  10–15 s hohe Drehzahl ohne Last. Das sind Offline-Renderings, keine Spielaufnahme.

## Menü, Antrieb und Auspuff in 0.21.0

- Controller: alle Kategorien mit Stick/Steuerkreuz erreichen, rechts/A in den
  Inhalt, links am Zeilenanfang zurück; LB Seitenleiste, RB Inhalt. Regler und
  Auswahlfelder mit A bearbeiten, A/B beendet Bearbeitung. Laser-Untermenü,
  Songliste, deaktivierte Controls, Mauswechsel und Wiederöffnen prüfen.
- Ohne Auto bzw. außerhalb 5 m nur Garage/Einstellungen; Spawn, Entfernen und
  Annähern aktualisieren die sichtbaren Kategorien. 720p/1080p: Auspuff-Eintrag,
  Rust-Logo, „by BCKWDZ“, Fokusrahmen und DE/EN ohne Überschneidung prüfen.
- 350Z und H2: Anfahren vorwärts/rückwärts auf derselben Steigung vergleichen;
  Rollen entgegen der gewählten Fahrtrichtung über 1,25 m/s muss zunächst
  abbremsen; darunter greift der Anfahrschub auch mit Controller-Teilgas.
  Gelände/Straße/Drift, Controller-Teilgas, Burnout und Bremsen prüfen.
- Normale Munition: ungerüsteter 40-HP-Gegner etwa drei Treffer; Sterne,
  Resistenzen und größere Gegner bleiben relevant. Explosivmunition gegen
  große Bäume, gefällte Stämme und mehrere Felsabschnitte testen; native
  Zerstörung/Beute, Deckung, Spieler, zahme Tiere und Bauwerke prüfen.
- Auspuff: Klangwechsel bei laufendem und abgestelltem Motor, Lautstärke null,
  gespeicherte Einstellungen pro Modell, mehrere Fahrzeuge; Nitrofarbe am
  Boden/im Flug, Purge, Schubflammen, Licht und Wechsel zwischen Autos prüfen.
- Alle zehn 350Z-Blenden wechseln: genau eine Variante sichtbar, zwei bzw.
  vier Nitroflammen an den Rohröffnungen, keine alten Flammen nach Wechsel;
  STYLE08 entspricht dem bisherigen Modell. H2-Endrohre bleiben modellfest.
- Automatische Tests prüfen Logik und Adapter mit Test-Doubles. Fahrgefühl,
  Unity-Kollisionen, Hörwirkung und tatsächliches Menülayout bleiben Spieltests.

## Controller und Sprache in 0.20.0

- 0.20.5: Hinweise unten rechts auf Xbox/PS5, Mauswechsel, DE/EN, 720p/1080p,
  HUD-/Hinweisskalierung prüfen: Symbole, keine abgeschnittenen Zeilen, Tacho
  darüber, Wechsel Boden/Flug/Menü und Wiederherstellung nach Aussteigen,
  Inventar, Pause, Weltwechsel sowie HUD-Ausblenden.
- 0.20.4: Kamera bei offenem Menü zu Fuß, im Fahrzeug und im Flug testen:
  rechter Stick, RMB-Ziehen, keine Bewegung bei Menüklick/Regler/Titelleiste;
  Cursor nach Loslassen, Schließen, Pause und Alt-Tab sowie Hindernisse prüfen.
- 0.20.1: Schnellleiste im Fahrzeug und Modmenü per Steuerkreuz, Zahlentasten
  und Klick prüfen: keine Auswahl/Benutzung, Anzeige bleibt aktuell; nach dem
  Aussteigen bei geschlossenem Menü wieder normal bedienbar.
- Xbox und PS5 per USB/Bluetooth prüfen: analoge Trigger, Stick-Totzone,
  Kameraempfindlichkeit, Y-Invertierung, Wechsel zur Maus, Hotplug und Steam Input.
- Zu Fuß L3 ohne gleichzeitige native Aktion; B/Kreis nahe am Auto nur Einsteigen.
  Beim Fahren X/Quadrat Nitro, A/Kreuz Handbremse, LB/L1 manuelles Geschütz.
- Sämtliche Menükategorien, deaktivierte Einträge, Regler, Farbfelder, Laser-Untermenü,
  lange Songliste mit automatischem Scrollen und Maus-Fensterverschiebung testen.
  A/Kreuz bestätigt, B/Kreis geht zurück; Menü darf keine Fahraktion auslösen.
- Gehaltene Pedale/Nitro bei Menü, Pause/Fokusverlust und Controller-Wechsel:
  zuerst loslassen. Kein Durchreichen an native Bewegung/Angriff beim Fahren.
- 1–4 bzw. Steuerkreuz hoch/runter: Boden/Flug, H2 ohne Drift; F7/B/Kreis
  im Flug landen und erst dann aussteigen. Blockierte Tür meldet Fehler.
- Flug-Turret automatisch auch bei ausgeschalteter Park-Automatik; manueller
  Schuss ausschließlich am Boden. Sichtprüfung, Reichweite und Deckung erhalten.
- DE/EN sofort im Menü/HUD/Meldungen; Werte und Konfigurations-IDs unverändert.
  Controller-Anzeige erzwingen, größere Hinweise bei kleinen Auflösungen und
  Songtitel-Einblendung auch beim automatischen Titelwechsel prüfen.

## Scheinwerfer-Show in 0.19.0

- F9 → Lichter auf 350Z/H2: Show-off aktivieren, aussteigen, zweimal physisch
  links/rechts aus Fahrersicht plus Pause; Kegel und Lampenflächen synchron.
- Farbe/Stärke, Stärke null, Reset, Aktivieren neben bereits gefahrenem Auto,
  Einsteigen während Blitz/Pause, zwei Autos mit unabhängigen Einstellungen.
- Ohne Motor-Weiterlaufen, manueller Lichtschalter, Brems-/Rückfahrlicht,
  Fokusverlust/Pause und Cleanup prüfen. Neuer Menüpunkt bei kleiner Auflösung.

## Metallic-Lack in 0.18.4

- 350Z und H2 frisch spawnen: Metallic-Reflexionen bei Sonne/Schatten/Nacht,
  Vinylfarben, Carbonhaube und Kunststoff vergleichen. Lackshader im Log prüfen.

## Nitro-Bildeffekte in 0.17.0

- 350Z am Boden/im Flug und H2 im Flug: W + LMB/Strg, Loslassen und leerer
  Tank. Purge/Schweben ohne Boost ohne Filter/Spuren. Rückleuchten aus/an.
- F9 → Nitro: beide Schalter einzeln, Stärke 0/70/100 %. Menü, Pause, Chat,
  Inventar, Alt-Tab, Ausstieg, Tod, Weltwechsel, Fahrzeugwechsel: keine Reste.
- DX11/Vulkan, HDR, Auflösungswechsel, Kameraorbit/-zoom und Hindernisse prüfen:
  kein kopfstehendes/dunkles Bild, Fahrzeug/HUD lesbar, keine Rücklichtspuren
  durch Gebäude. Länge/Position der Spuren an beiden Autos visuell abstimmen.
- GPU-Frametimes bei 1080p/1440p/4K messen. Sechs Bildlagen plus halbe Auflösung
  für die GPU-Abtasttextur; keine CPU-Bildrücklesung. Bei Stärke 0/ausgeblendeten
  Effekten ist der Kamera-Callback deaktiviert. Kein Unity-Laufzeittest erfolgt.

## Flug-Auspuff und NFS-Bildeffekte in 0.16.1

- 350Z im Flug: W + LMB/Strg aktiviert die vier vorhandenen Auspuffflammen
  und den normalen Nitro-Sound zusätzlich zu den Rad-Düsen. Tank leer,
  Loslassen, Menü, Fokusverlust und Landen beenden Boost-Effekte korrekt.
- Schweben ohne Boost bleibt ohne Auspuff-Nitro; kein Purge/Reifenrauch
  oder Schubknallen im Flug. Boden-Nitro und Purge erneut vergleichen.
- NFSU2 Motion Blur, Light Trails und Light Glow sind getrennt belegt
  (REFERENCES). Radiales Verwischen um den Fluchtpunkt plus kurz auslaufende
  Lichtspuren ist eine plausible NFVU2-Nachbildung; noch keine verifizierte
  Rekonstruktion des NFS-Shaders oder seiner Nitro-Steuerung.
- Originale Filter-Shader inzwischen disassembliert: gewichtete Mischung
  von 1–4 Textur-Abtastungen, separate Helligkeits- und Farbverarbeitung.
  Nitro-Zuordnung und Rücklichtspur bleiben offen. Wegen gepacktem/geschütztem
  EXE-Code als nächsten Nachweis Laufzeit-/Grafikdaten des Originals verwenden:
  Texturen, UVs, Gewichte, Blendmodus und Spur-Geometrie vergleichen.
- Für eine spätere Nachbildung: Fahrzeug/HUD lesbar halten, Stärke regelbar
  und abschaltbar, nur bei tatsächlichem Boost, weich ausblenden. Grafiklast
  messen; keine CPU-Bildrücklesung pro Frame. Gegen Original-NFS vergleichen.

## Hintergrund-Spawn in 0.16.0

- 350Z mit Innenraum und H2 jeweils erstmals und mehrfach spawnen: Ladeanzeige
  animiert, Welt/Spieler bewegen sich weiter, vollständiges Auto erscheint.
  Im Log „Vehicle data ready“ und „Vehicle spawn completed“ vergleichen;
  Frametimes im Spiel messen, besonders Textur-Uploads/Shader beim ersten Sichtkontakt.
- F6 mehrfach, F8 und Abbrechen in verschiedenen Ladeabschnitten; danach
  erneut spawnen. Keine Geister-Collider, halben Autos, Sounds oder Meshreste.
- Bei schon vorhandenen Autos darf F8 während Laden nur den Ladevorgang
  abbrechen. Deren Radio, Turret und Physik müssen normal weiterlaufen.
- Während Laden bewegen, F9 öffnen, Alt-Tab, Tod, Teleport und Welt verlassen.
  Kein verspäteter Spawn in der nächsten Welt; kein Einstieg in Teilressourcen.
- Innenraum, bewegliche Türen, Scheibentönung, Lenkrad, HUD, Motor/Nitro/Turret-
  Audio, Beleuchtung und H2-Spinner nach Aktivierung prüfen. Vier-Auto-Grenze.
- Automatische Lade-/Cachetests ersetzen keine Unity-Laufzeitprüfung. Einzelne
  unteilbare Unity-Aufrufe und GC können weiterhin kurze Spitzen verursachen.

## Nitro und Reifen in 0.15.2

- Im Flug Menü schließen, Tasten loslassen, W + LMB bzw. W + linke Strg:
  „NITRO AKTIV“, sinkender Tank und Tempo deutlich über 65 km/h prüfen.
  Falls weiterhin ohne Wirkung, `Flight nitro:` im Log auswerten: native/Legacy-
  Mauswerte, Strg, Freigabe, Phase, Gas, Tank und Geschwindigkeit. Ohne diese
  Laufzeitdaten ist die Ursache des gemeldeten Ausfalls noch nicht bestätigt.
- Flammen dürfen weder über den Reifen hinausragen noch darunter frei schweben.
  Neutral, voller Schub, Neigung und H2 vergleichen.
- Original-Reifenprofil bei Tageslicht/Nacht und nach Felgengrößenänderung prüfen.

## Flugkorrekturen und HUD in 0.15.1

- Steigen/Sinken in großer Höhe und nahe Boden/Decken prüfen; nach schnellem
  Sinken muss die Bodenabbremsung vor dem Aufsetzen greifen. Auto-Landung separat.
- Bei allen vier Rädern zeigt die fertige Felgenseite nach oben; Flammen nur
  unterhalb. W/S sowie A/D, Loslassen und Nitro: Neigung/Flammenrichtung folgt.
- 350Z/H2: W + LMB im Flug, leerer Tank, Nachladen, Menü, Fokusverlust,
  Flug-/Bodenwechsel und Landung. Kein Nitro-Verbrauch durch bloßes Schweben.
- HUD: alle elf Designs und 65–140 % Skalierung, KM/H und MPH, Stillstand,
  Rückwärtsfahrt und hohes Flugtempo. Beide Zeiger deutlich, Zahlen unverdeckt.

## Flugmodus in 0.15.0

- 350Z und H2: im Sitz F9 → Fahrzeug → Fliegen; vier Räder klappen gleichmäßig
  mit Bremsen/Spinnern nach unten, vier Düsen folgen der äußeren Felgenseite.
  Sturz, Radgröße, Radversatz und Tieferlegung kombinieren; keine Mesh-Überlappung.
- W/S vor/zurück, A/D drehen, beide Shift-Tasten hoch, Leertaste runter;
  gegenläufige Tasten heben sich auf. Loslassen bremst und hält die Position.
- Bodenfreiheit, Hänge, Felsen, Hausdächer, Decken und hohe Starts testen.
  Schweberegelung und Kollisionen müssen in Unity bestätigt/abgestimmt werden.
- Menü, Chat, Fokusverlust, Pause: keine alten Eingaben oder ungewollter
  Sinkflug durch die bisherige Bremsbelegung der Leertaste.
- Bodenmodus in großer Höhe auswählen: kontrollierte Landung, Räder ausklappen,
  anschließend Anfahren/Bremsen/Drift/Nitro. Landung durch Fliegen abbrechen.
- Aussteigen erst am Boden; F8, Tod und Weltwechsel während des Fluges prüfen.
  Mehrere Autos behalten eigene Zustände; kein Flugzustand nach neuem Spawn.
- Vier Flammen, Soundübergänge, Lautstärke 0 und fehlende optionale Audiodateien.
  Partikel und Sound im Spiel prüfen; automatische Tests ersetzen dies nicht.

## Felgen in 0.14.3

- Bei Tageslicht um den 350Z gehen: Chromrand soll helle/dunkle Umgebung
  spiegeln, der Stern gedeckter golden wirken. Nacht, Wald und Neon vergleichen.
- Logeintrag `350Z wheel metal shader` kontrollieren; Standard bevorzugt,
  vorhandener Fahrzeugshader als Fallback. Kein künstliches Leuchten im Dunkeln.
- Mehrere Fahrzeuge sowie Radgrößen/Sturz prüfen. Die Blender-Vorschau bildet
  Materialparameter ab, ist aber kein Nachweis für das Unity-Rendering.

## Scheibentönung in 0.14.2

- 0/50/100 % bei Tageslicht und Nacht: keine rote Eigenleuchtung, neutral dunkle
  Scheiben bei 100 %. Heckscheibe mit Rücklicht und Kofferraum-Neon vergleichen.
- Linkes offenes Fenster, Ausstieg, weitere 350Z und Neustart: Tönung bleibt
  gespeichert und Fensterzustand funktioniert weiter.
- Lokale Lava-Laternen-Materialien besitzen bestätigte rote Emission; die Mod
  übernimmt nun nur deren Shaderreferenz und erstellt ein neutrales Material.

## Fahrerhaltung und Scheiben in 0.14.1

0.14.0 funktioniert laut Nutzer grundsätzlich. Gemeldet wurden ein durch das
Dach ragender Kopf, zu weit vorne liegende Hände und umgekehrtes sichtbares Lenken.

- Kopf mit beiden Charaktermodellen, Haaren/Helmen, Tieferlegung und auf Hängen
  unter dem Dach prüfen; tiefere Sitzposition darf nicht im Sitzpolster verschwinden.
- Rechts/links lenken: Rad und rechte Hand müssen zusammen in die erwartete
  Richtung drehen. Handgelenk hinter dem Kranz, Finger am Kranz, keine Überstreckung.
- Linker Arm am offenen Fenster; bei Türöffnung und Ausstieg nach innen.
  Alle drei Türmechaniken sowie schnelles Öffnen/Schließen im F9-Menü prüfen.
- Tönung 0/50/100 %, Neustart und mehrere 350Z: geschlossene Scheiben reagieren,
  linkes Fenster beim Sitzen bleibt frei und nach Ausstieg wieder sichtbar.
- F8, Fehler und Weltwechsel stellen Fenster, Körperhaltung und Steuerung wieder her.

## Innenraum und Fahrer in 0.14.0

- F6/F7: Cockpit, Sitze, Türverkleidungen, Fenster und Lenkraddrehung bei
  Tageslicht und Nacht prüfen. Türverkleidungen müssen bei allen drei Türstilen mitgehen.
- Einstieg von beiden Seiten, vorne/hinten, mit blockiertem Weg und am Hang.
  Kein Durchlaufen von Wänden; Fahrfreigabe erst nach geschlossener Tür.
- Beide Charaktermodelle mit/ohne Rüstung, Helm, Waffen und Umhang: Hüfte im
  Sitz, Kopf unter dem Dach, Hände am Lenkrad, Füße an Pedalen. Visualskala 85 %.
  Exakte Hand-/Fußhaltung und Textur-/Glaswirkung benötigen den Unity-Spieltest.
- Ausstieg im Stand und während der Fahrt, blockierte Fahrertür, Hindernis
  erscheint während des Ausstiegs, wiederholtes F7: keine festhängende Steuerung.
- F9/Pause/Alt-Tab während Annäherung, offener Tür und Sitzübergang; F8, Tod,
  Weltwechsel/Plugin-Abbau: Renderer, Ausrüstung, Animator, Collider, Körper
  und Kamera müssen wiederhergestellt werden.
- Mehrere Autos, H2-Wechsel, fehlender/ungültiger Innenraum-Payload und
  fehlender transparenter Fenstershader: ursprünglicher Mod bleibt nutzbar.
- Native Stuhlpose und eigene prozedurale Übergänge weiter verfeinern;
  cockpitinterne Anzeigen sind statisch, keine Instrumentensimulation.

## Verschiebbares Menü in 0.13.1

- F9: Titelleiste links gedrückt ziehen; Text und freie Headerfläche probieren.
- Alle Bildschirmränder, 720p/1080p, kleinere Fenster und Auflösungswechsel
  prüfen: Menü vollständig erreichbar, Maus folgt ohne Sprung bei Skalierung.
- Kategorie/Regler/Schließen klicken: kein unbeabsichtigtes Verschieben.
- Während Ziehen F9, Fokusverlust und Loslassen außerhalb des Fensters prüfen;
  beim Wiederöffnen kein festgehaltener Cursor. Position nach Neustart prüfen.

## Tieferlegung, Menü und Parkautomatik in 0.13.0

- 350Z/H2: 0/6/12 cm auf ebener Fläche, Fahrmoduswechsel, F6-Neuspawn und
  Konfigurationsneustart prüfen. Bestehende Sturz-/Versatz-/Felgenregler weiterhin
  kombinierbar; Radfreigang und Bodenkontakt bei starken Werten beobachten.
- Mit teilweisem/vollen Lenkeinschlag beider Richtungen aussteigen, lange warten,
  F9 neben dem Wagen öffnen und wiedereinsteigen: kein Zurückschnappen beim
  Ausstieg, sanftes Weiterlenken beim Einstieg; Bremsen/Burnout/Nitro stoppen.
- Neues Menü bei 720p/1080p und kleineren Fenstern durch alle Kategorien prüfen:
  Icons, deutsche Originalüberschriften, Cursor, Scrolllisten, Schließen,
  Kategorien über ↑↓ und Soundlautstärke 0/50/100 Prozent.
- Drei Original-Frontend-Samples im NFS-Spiel gegenhören; native Rollen sind
  noch nicht verifiziert. Import-Indizes bleiben über CLI konfigurierbar.
- Turret einmal fahren/aktivieren, aussteigen: ruhiger Laserschwenk nach beiden
  Seiten. Gegner vorne/seitlich bei 5/29/31 m und hinter dem Auto prüfen.
- Fels/Gebäude/eigenes Auto/Spieler/zahmes Tier vor dem Lauf: kein automatischer
  Schuss durch Deckung. Zielverlust, Tod, Zähmung, mehrere Gegner und Nachladen
  prüfen. Ein anderer feindlicher Ersttreffer wird erst nach Zielwechsel beschossen.
- Motorweiterlauf aus, anderer 350Z/H2 gefahren, mehrere geparkte Turrets:
  jeweils eigene Suchbewegung, Schüsse, Nachladen und räumliche Sounds.
- F9, Inventar, Pause, Alt-Tab, Spielertod, Wiederbelebung und RMB beim Einstieg:
  Sperren und erneute Zielerfassung prüfen. F8/Weltwechsel entfernt alles.
- Explosivmunition/Laserfarbe und Stärke in beiden Modi prüfen. Suchsektor
  ±90° orientiert sich an Rust; Scanbewegung und 30-m-Reichweite sind eigene
  Mod-Regeln. Automatisierte Tests ersetzen keine Unity-Physik/Spielansicht.

## Hummer und Garage in 0.12.0

- F9 ohne Auto → Garage: 350Z und gelben H2 nacheinander spawnen; beide bleiben
  bestehen, kein Überlappen. Enge Stellen/Hänge müssen einen anderen Platz
  oder eine verständliche Meldung ergeben. Maximal vier Autos.
- Zwischen beiden Autos laufen: F7/F9 treffen das nähere; Fahrt, Ausstieg,
  Kamera und Spielersteuerung prüfen. F8 entfernt genau eines, Weltwechsel alle.
- H2: Beschleunigen, Bremsen, Rückwärtsfahrt, Gelände, Federweg und Seitenneigung.
  Eigener Hummer-Sound, vier Gänge, Radio und gemeinsame Musikdämpfung prüfen.
- Spinner: verzögertes Hochlaufen, Nachlauf nach Stopp, Rückwärtsfahrt und
  Sturz/Radversatz/22–28-Zoll-Regler. Bremssättel bleiben fest.
- Gelb, Chrom, Radpassform, Heckschrift, rote Rück-/Bremslichter und weiße
  Rückfahrlichter im Spiel prüfen. Vorschau benutzt angenäherte Federstellung.
- H2-/350Z-Einstellungen getrennt speichern; beim Weiterlaufen behalten beide
  ihre Radio- und Motorzustände. Zwei gleichzeitig laufende Radios sind hörbar.
- Regression: 350Z-Ausbau/Subwoofer, Türanimationen, Drift, Nitro, Turret,
  Lasereinstellungen und Explosivmunition mit H2 daneben testen.
- H2-Motorraum/Kofferraum, Türen, Nitro und Turret sind noch nicht eingerichtet.

## Laser und Subwoofer in 0.11.0

- Laser bei Tag/Nacht prüfen: Standardstärke 200 Prozent, Bereich 25–400,
  Farbvorlagen und freie RGB-Werte, Speichern, Ausschalten, Wandabschattung.
- Musik mit Bass bei offener Heckklappe: Membran schwingt, Rand/Gehäuse bleiben
  fest; vier Subwoofer-Modelle und alle Slotgrößen/Ausbauten durchwechseln.
- Pause, Titelwechsel, Radio aus, Lautstärke null, Fokusverlust, Ein-/Ausstieg
  und Weiterlaufen prüfen; bei fehlender/ladender Musik keine Dauerbewegung.
- Anlage mehrfach umbauen und Fahrzeug neu spawnen: keine fremden Meshänderungen
  und keine zurückbleibenden animierten Meshkopien.
- Signaltests prüfen Bass/Treble, Stereo und Stillstand. DSP-Callback, sichtbarer
  Hub und Shaderhelligkeit benötigen zusätzlich den Unity-Spieltest.

## Laser und Explosivmunition in 0.10.0

- Zielvisier oberhalb des Autos bei Zoom, Rundumblick, steilem Blick nach unten,
  Hanglage und Kamerahindernissen prüfen; orange bei verdecktem/noch nachlaufendem Lauf.
- Lasermodell, roter Strahl und Punkt mit Rückstoß sowie Menüschalter prüfen.
- Normal/Explosiv im Menü wechseln, Nachladen und Maustastensperre beibehalten.
- Explosionen an Gegnern/Deckung, Lautstärke und viele Treffer hintereinander testen.
- Boden direkt treffen: kleine Senke bei jedem Treffer, weitere Vertiefung,
  Grasentfernung, Kachelgrenzen, Kollision und Erhalt nach Welt-Neuladen prüfen.
- Fels-/Wand-/Gegnertreffer dürfen den Boden darunter nicht verändern.
- Terrain-API lokal geprüft; Adaptertests ersetzen keinen Unity-Spieltest.
- Keine Löcher/Tunnel unter überhängendem Terrain: Valheim verändert ein Höhenfeld.

## Zielmarkierung und Montage in 0.9.1

- Fadenkreuz auf dem tatsächlichen Auftreffpunkt bei freiem Blick, Hindernissen,
  Kameradrehung, Zoom, Kurven und Neigegrenzen prüfen; keine feste Zusatzmarkierung.
- Tiefer/hinten versetzte Stützen auf dem Dach im Spiel von allen Seiten prüfen.
- Munitionsanzeige links unten mit ein-/ausgeschaltetem Tacho prüfen.

## Dach-Turret in 0.9.0

- F9 → Turret: Aktivierung neben/im Auto, gespeicherter Zustand, Entfernen und
  erneutes Spawnen; Dachposition und Materialien bei Tag/Nacht prüfen.
- Kamera rundum und in der Höhe drehen; Neigegrenzen, Nachlauf, Zielpunkt
  und orange Markierung bei vom eigenen Fahrzeug blockiertem Lauf prüfen.
- RMB: Einzelschüsse, Dauerfeuer, 30 Schuss, 2,5 s automatisches Nachladen,
  Schuss-/Nachlade-/Servosounds und separate Lautstärke prüfen.
- Gegner vor/hinter Fels oder Gebäude: nur erster Festkörper zählt; keine
  Schäden an Spielern, gezähmten Tieren oder Bauwerken. Eigenes Auto blockiert.
- Menü, Pause, Alt-Tab, Ausstieg und Wiedereinstieg mit gehaltener Maustaste:
  kein Schuss bis nach Loslassen. Nitro links gleichzeitig prüfen.
- Automatische Prüfungen und Blender-Vorschau ersetzen keinen Unity-Spieltest.
  Original sind Meshes, Texturen, Gelenkpunkte und Samples; Waffenmontage,
  Rückstoß, Mündungsfeuer, Treffer und Nachladen sind eigene Anpassungen.
  Keine automatische Zielsuche, kein vollständiges Rust-Animationssystem.

## Tachos, Kamera und Drift in 0.8.0

- Alle elf Designs, Farbe, Größen, KM/H/MPH und gespeicherte Auswahl prüfen.
- Drehzahl/Gang mit Sound und NOS-Füllung mit Boost/Nachladen vergleichen.
- HUD bei verschiedenen Auflösungen, geöffnetem Menü, Ausstieg und F6/F8 prüfen.
- Dynamische Kamera: Kurven, Drift, Rückwärtsfahrt, Mausblick, Stand, Zoom,
  Mittelklick, Menü/Alt-Tab und Hindernisse testen; Nachzieh-Regler vergleichen.
- Drift auf freier ebener Fläche: Gas, Gegenlenken, Handbremse, S-Bremse,
  Rauch bei Bodenkontakt, Burnout-Abbruch, Nitro und Aus-/Wiedereinstieg.
- Wieder zurück auf Gelände/Straße wechseln; bisheriges Fahrgefühl erhalten.
- Grip und Kameranachlauf benötigen einen Spieltest; keine Original-NFS-Physik
  oder vollständige GTA-Kamerareproduktion behauptet.

## Außenmusik in 0.7.2

- Bei laufendem Radio draußen linke/rechte Tür und Heckklappe einzeln sowie
  gemeinsam öffnen: stufenlos lauter/klarer, räumlich zur jeweiligen Öffnung.
- Beim Schließen wieder gedämpft, kein Titelsprung; Motorhaube ohne Einfluss.
- Alle Türmechaniken, Wiedereinstieg, Pause, Lautstärke 0, Weiterlaufen aus,
  Fokusverlust und Entfernung vom Auto im Spiel hören.
- Eine räumliche Quelle bildet bei mehreren Öffnungen deren gewichtete Mitte;
  keine getrennte Ausbreitungs- oder Wandabschattungssimulation.

## Rückleuchten-Korrektur in 0.7.1

Die oberen STYLE04-Einsätze waren im Import ausgelassen und sind wieder
enthalten. Beide Seiten mit offenem/geschlossenem Kofferraum und aktivem
Neon im Spiel prüfen: kein Durchblick durch die Leuchten; untere orange
Streifen sowie Rück-/Brems-/Rückfahrlicht weiterhin korrekt.
Originalgeometrie und externe Vorschau geprüft; Unity-Spieltest offen.

## Kofferraumausbau, Türen und Motorraum in 0.7.0

Umgesetzt: vier originale Kofferraumgehäuse, Auswahl pro Einbauplatz,
Standard-Ausbau mit vier Subwoofern, eigenes Neon mit Farbe/Pulsmodus,
drei Motoroptiken und acht Original-Animationsspuren mit Scharnierpunkten.
Normale/Suicide-/Scherentüren, Haube und Heckklappe sind einzeln bedienbar.
Externe Vorschauen aller Türmechaniken, Gehäuse 1–3 und Motoroptiken geprüft.

- Im Spiel F9 → Ausbau: alle Einbauplätze, Subwoofer, Verstärker, LCDs und
  Abdeckungen wechseln; beim Gehäusewechsel darf nichts doppelt erscheinen.
- Standard-System, gespeicherte Konfiguration und F6/F8-Wiederholung prüfen.
- Kofferraum-Neon unabhängig vom Unterboden einstellen: sieben Farben,
  statisch/pulsierend, Ausstieg/Weiterlaufen, offener/geschlossener Kofferraum.
- Beide Türen einzeln, Haube und Heckklappe öffnen/schließen; laufende Animation
  umkehren und Türmechanik während geöffneter Türen wechseln.
- Spiegel/Innenverkleidungen und Spoiler müssen mitgehen, Schweller bleiben fest.
- Anfahren und Einsteigen schließen alles; auf Hängen keine bleibende Fehlstellung.
- Die Showroom-Animationen haben keine zusätzlichen Tür-Kollisionskörper.
- Innenraum bleibt ein einfaches Originalmodell; detaillierte Sitze/Cockpit,
  transparente Scheiben und feinere Materialwirkung sind spätere Arbeiten.
- LCDs sind statisch texturiert. Neon ist ein visueller Puls, keine Beat-Analyse.

## Überfahren und Weiterlaufen nach Ausstieg in 0.6.0

Umgesetzt: geschwindigkeitsabhängiger nativer Treffer auf feindliche Kreaturen
und ausgewählte kleine Bäume/Büsche; Motorleerlauf, Neon und gedämpftes
räumliches Radio nach dem Aussteigen, gemeinsam schaltbar unter F9 → Fahrzeug.

- Kleine Buchen/Tannen und Büsche vorwärts/rückwärts ab etwa 9 km/h anfahren:
  native Zerstörung, Holz und Effekte; besonders dicht stehende Vegetation prüfen.
- Gegner bei verschiedenen Geschwindigkeiten treffen: Schaden, Rückstoß,
  Tod/Beute und wiederholte Anfahrt; keine Mehrfachtreffer durch mehrere Collider.
- Große Bäume, Felsen, Gebäude, zahme Tiere und friedliche NPCs bleiben ausgenommen.
- Parkplatz: nach frischem Spawn aus, nach Fahrt Leerlauf und aktiviertes Neon an;
  Menüschalter draußen aus/an, gespeicherte Einstellung, Aus-/Wiedereinstieg prüfen.
- Radio darf beim Aussteigen nicht neu starten; draußen leiser/dumpfer, mit
  Entfernung ausblenden. Manuelle Pause, Titelende/-wechsel, Lautstärke 0 prüfen.
- Valheim-Musik draußen wiederherstellen, Fokusverlust/Spielmenü pausieren,
  F8 und Weltwechsel müssen sämtliche Quellen/Effekte entfernen.
- Tatsächliche Physikkontakte, native Schadens-RPCs, Hörwirkung und Menülayout
  benötigen den Spieltest; automatisierte Tests verwenden hierfür Test-Doubles.

## Anfahren, Rückwärtsfahrt und Burnout in 0.5.0

Umgesetzt: +20 Prozent Anfahrmoment mit weichem Auslauf, kürzerer erster
Sound-Gang, synthetisches Rückwärts-Getriebeheulen, gedimmtes Rückfahrlicht
und W+S-Burnout mit originalem Reifensound und Rauchtextur.

- Anfahren auf derselben Strecke mit 0.4.1 vergleichen, besonders auf Hängen.
- W+S im Stand in beiden Fahrmodi prüfen: Frontachse hält, Hinterräder drehen.
- Loslassen von S/W, Leertaste, F9, Alt-Tab, Aussteigen und Entfernen prüfen:
  keine hängenbleibenden Effekte oder Gripänderungen.
- Bodenkontaktverlust und seitliches Rutschen sollen den Burnout abbrechen.
- Rückwärts-Heulen mit Geschwindigkeit/Stillstand und Lautstärke prüfen.
- Rückfahrlicht bei Nacht prüfen; Original-Sample-Zuordnung weiter recherchieren.
- Rauchposition mit Radversatz/Sturz sowie Shaderwirkung im Spiel prüfen.

## Menüposition und Scheinwerfer in 0.4.1

Menü links oben mit 16 Pixel Rand und Skalierung für kleinere Fenster.
Scheinwerferfarbe aus 14 NFSU2-Vorlagen, Stärke 0–300 Prozent, gespeicherte
Werte und Zurücksetzen unter F9 → Licht umgesetzt.

- Menüposition bei verschiedenen Auflösungen und freier Sicht aufs Auto prüfen.
- Front-Lichtkegel und Leuchtenflächen bei Nacht mit allen Farben vergleichen.
- Stärke 0/100/300 Prozent, Aus-/Einstieg und gespeicherte Werte prüfen.
- Rück-/Brems-/Rückfahr- und Kennzeichenlicht müssen unabhängig bleiben.

## Räder-Menü in 0.4.0

Fünf Regler umgesetzt: Sturz und Radversatz je Achse, gemeinsame Felgengröße.
Optische Live-Anpassung mit gespeicherten Werten und Zurücksetzen;
Reifenaußendurchmesser und bestätigtes Fahrwerk bleiben erhalten.

- Im Spiel beide Achsen unabhängig einstellen und nach Neustart prüfen.
- Bei Lenkeinschlag und drehenden Rädern auf stabilen Sturz ohne Taumeln achten.
- Bremssättel müssen mitlenken/kippen, aber weiterhin nicht mitrotieren.
- Extremwerte auf Durchdringung von Kotflügel/Karosserie prüfen; Regler sind optisch.

## Heckschriftzug in 0.3.1

Spiegelverkehrten 350Z-Schriftzug gezielt über seine UV-Insel korrigiert.
Externe Nahaufnahme geprüft; Ausrichtung im Spiel noch bestätigen.

## Nitro, Purge und Felgen in 0.3.0

Umgesetzt: gehaltene linke Maustaste für Boost mit grünen Auspuffflammen,
Purge im Stand an Front/Haube, sechs Sekunden Tank, Nachladung beim Fahren,
F9-Nitro-Einstellungen und Chromring mit goldbronzenem Felgenstern.
Originale NFS-Texturen und Nitrobank-Sounds sind lokal importiert.
Der Spieltest dieser Version steht noch aus.

- Boost/Purge-Wechsel, Tank, Bremsen, Rückwärtsfahrt und Sprünge im Spiel prüfen.
- Menü schließen per Mausklick, Alt-Tab, Ein-/Ausstieg: keine unbeabsichtigte Aktivierung.
- Grüne Flammen an allen vier Endrohren und Purge-Düsen bei Tag/Nacht prüfen.
- Chrom-/Goldwirkung mit Valheims Beleuchtung prüfen; externe Vorschau ist fertig.
- Native Nitro-/Purge-Loop-Zuordnung durch Hörvergleich mit NFS bestätigen.
- Exakte NFS-Lastwechsel-/Tuningbedingungen und Sputter-Substream identifizieren.
  Optionaler Flammenstoß ist eine eigene, zunächst ausgeschaltete Annäherung;
  ohne verifizierten Knall-Sample bleibt er visuell.

## Rückmeldung aus dem Spieltest von 0.2.0

Der Nutzer bestätigt die grundsätzliche Funktion der umgesetzten Menü-, Radio-,
Licht-, Neon- und Soundfunktionen. Folgende Nacharbeiten wurden dabei gemeldet:

- Fehlende Leuchtenflächen in der Heckstoßstange und die Streifen unter den
  oberen Rückleuchten ergänzen; Funktionen anhand der Originalteile unterscheiden.
- Weiße Rückfahrleuchten an den richtigen Stellen ergänzen; beim Vorwärtsbremsen
  mit S dürfen sie noch nicht angehen.
- Die optisch zu schmalen Reifen verbreitern; Durchmesser und Fahrwerk beibehalten.
- Größere, sportliche Bremsscheiben/-sättel aus verfügbaren NFS-Teilen prüfen.
- Radio mit verifiziert zugeordneten Künstler- und Songnamen statt Nummern versehen.

In 0.2.1 umgesetzt: Heckstreifen und Stoßstangeneinsätze, weiße Rückfahrleuchten,
28-cm-Reifen, STYLE03-Sportbremsen und 27 echte Radiotitel. Der Nutzer bestätigt
auch die Funktion von 0.2.1 in Valheim. Gezielte Grenzfalltests bleiben unten
als weitere Qualitätsprüfung aufgeführt.
Die orangefarbenen Streifen sind passiv; Blinkersteuerung bleibt eine spätere Aufgabe.

## Weitere gezielte Spieltests

- F9-Menü, Cursor/Rückkehr zur Kamera, Ein-/Ausstieg und Fahrzeugentfernen prüfen.
- Radio: rasches Vor/Zurück, Pause, Titelauswahl, Fokuswechsel, Ausstieg/Fortsetzen.
- Die 27 zugeordneten Radiotitel stichprobenweise beim Hören im Spiel prüfen.
- Licht und Kennzeichen bei Nacht, Bremslicht bei ausgeschaltetem Hauptlicht prüfen.
- Neon auf Ebene, Hängen und Stufen sowie bei Sprüngen prüfen; Shader/Farbintensität abstimmen.
- Frühere akustische Schaltungen und Turbo auf derselben kurzen Strecke hören.
- Originale Turbo-Substream-Rollen, Rachel-Tuning und NFS-Auslöseverhalten abgleichen.
- Türen/Kofferraum: neue 0.7.0-Animationen und Innenflächen im Spiel prüfen.

## Sound-Feinschliff

- Die gelegentlich seltsamen Übergänge im Spiel reproduzieren und eingrenzen.
- Drehzahl, Lastwechsel, Schleifen und Schaltübergänge abstimmen.
- Leerlauf, Rückwärtsfahren, Aussteigen, Menü-Pause und Lautstärke prüfen.
- Rachels tatsächliche Sound-Zuordnung anhand der NFS-Konfiguration bestimmen.

## Rachels Fahrzeug

- Darstellung von 0.1.9 vom Nutzer in Valheim bestätigt: Widebody 02, Spoiler 02,
  Carbonhaube, Originalfelgen und Drachenvinyl sind aus `DDAY_PLAYER_CAR` importiert.
- Lackreflexionen und Felgentönung mit dem Originalspiel vergleichen.
- Radfreigang bei Lenkeinschlag und Einfedern in beiden Fahrmodi prüfen.
- Scheibentransparenz, Leuchtenabdeckungen, detaillierten Innenraum und Fahrersitz ergänzen;
  angenäherten Kennzeicheneinbau im Spiel prüfen.
- Alle offenen Darstellungsschritte abschließen, bevor die vollständige
  Rachel-Rekonstruktion als fertig bezeichnet wird.

## Fahrgefühl

- Der Geländemodus von 0.1.4 ist vom Nutzer als gut funktionierend bestätigt.
- Künftige Änderungen auf derselben Strecke vergleichen und einzeln prüfen.
- Kollisionen, Schwerpunkt und Straßenmodus weiter abstimmen.

## Mögliche Bridge

- Machbarkeit einer Verbindung zwischen laufendem NFSU2 und Valheim untersuchen.
- Zuerst NFS-Version und auslesbare Fahrzeugzustände verifizieren.
- Steuerung, Position, Zeitabgleich sowie Gelände-/Kollisionsübertragung getrennt erproben.
- Eine funktionierende Bridge ist noch nicht nachgewiesen und kein Bestandteil von 0.1.5.
