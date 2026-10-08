# Versionsgeschichte

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
