# Versionsgeschichte

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
