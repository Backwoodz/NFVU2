# Nächste Aufgaben

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
