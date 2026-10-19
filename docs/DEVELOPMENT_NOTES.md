# Detailed gameplay notes

Historical implementation notes retained from the former README (German).
For player setup use [README](../README.md); for development use
[CONTRIBUTING](../CONTRIBUTING.md). Version-specific statements describe their
original release; current limitations and pending in-game checks are in
[ROADMAP](ROADMAP.md).

## Bedienung

F6 oder der Spawnknopf startet einen Ladevorgang mit Anzeige des aktuellen
Abschnitts. Dateilesen, JSON-Auswertung und Sounddekodierung laufen im Hintergrund;
Unity-Texturen, Meshes und Audioclips werden schrittweise erstellt. Das fertige
Fahrzeug erscheint anschließend an einem erneut geprüften freien Platz vor dem
Spieler. Währenddessen kann man weiterlaufen; weitere Spawns und Einsteigen
warten auf den Abschluss. F8 oder „Abbrechen“ verwirft den Ladevorgang, ohne ein
bereits vorhandenes Auto zu entfernen. Tod, Teleport und Weltwechsel brechen ab.

Unveränderte Modell- und Sounddaten werden für weitere Spawns derselben Welt
zwischengespeichert; Unity-Ressourcen bleiben pro Auto unabhängig. Geänderte
Dateigröße oder Änderungszeit erzwingen neues Einlesen. Der Cache benötigt
zusätzlichen Arbeitsspeicher und wird beim Verlassen der Welt freigegeben.
Das Arbeitsbudget für den Aufbau beträgt ungefähr 3 ms pro Frame; einzelne
Unity-Aufrufe sind nicht unterbrechbar und können länger dauern. Das Log nennt
Gesamtdauer und längsten Hauptthread-Schritt. Ruckelfreiheit ist noch nicht im
Spiel gemessen. Die Fahrwerksabstimmung bleibt unverändert.

F9 → Fahrzeug → **Fliegen** aktiviert im besetzten Auto den Flugmodus. Alle vier
Räder klappen in etwa 1,2 Sekunden um 90° mit der Felgen-Sichtseite nach oben, das Auto hebt sanft
ab und schwebt. W/S fliegt vorwärts/rückwärts, A/D dreht das Fahrzeug, Shift
steigt und Leertaste sinkt. Nach dem Loslassen bremst es ab und hält seine
Position. Die Maus steuert weiter die Kamera. Menü und Fokusverlust sperren
Flugeingaben; die Stabilisierung bleibt aktiv.

Vier blaue Düsen verwenden die originale NFS-Nitro-Flammentextur und einen
tiefer abgestimmten lokalen Nitro-/Gasloop. Größe und Sound reagieren auf den
Schub. Bewegung, Flugregelung und Klappanimation sind NFVU2-Anpassungen.
Schweben verbraucht keinen Nitrotank. W + linke Maustaste aktiviert Flug-Nitro
mit zusätzlichem Vorwärtsschub bis etwa 158 km/h und sechs Sekunden Tank.
Nach dem Loslassen lädt dieser nach zwei Sekunden Pause bei Bewegung wieder auf.
Beim 350Z teilen Boden und Flug denselben Tank; der H2 hat Nitro nur im Flug.
Beim tatsächlichen Flug-Boost laufen am 350Z zusätzlich die normalen grünen
Auspuffflammen und der Nitro-Sound. Reines Schweben aktiviert diese nicht.
Purge und Burnout bleiben in der Luft gesperrt. Der H2 verwendet ebenfalls den 350Z-FIREB-Atlas.
Shift steigt mit bis zu 12 m/s, Leertaste sinkt mit bis zu 16 m/s; nahe am
Boden wird abgebremst. Die Räder neigen sich mit Vorwärts-/Rückwärts- und
Lenkeingaben; die unterhalb ansetzenden Flammen zeigen entgegen der Schubrichtung.
Der Tacho zeigt die Geschwindigkeit digital in KM/H/MPH; das kleine linke
Instrument ist seit 0.22.0 wieder die originale Turbo-/Unterdruckanzeige.
Die große originale NFS-Skala bleibt der Drehzahlmesser mit verstärktem Zeiger.

Seit 0.15.2 liest Nitro zusätzlich Valheims `ZInput.GetMouseButton`; im Flug
funktioniert alternativ W + linke Strg. Nach dem Schließen des Menüs die
Boosttaste erst kurz loslassen. Eine Statuszeile zeigt Aktivierung, Tank und
Loslass-Sperre; begrenzte Diagnosezeilen protokollieren den Eingabeweg.
Die Original-Reifentextur war bereits importiert, wurde aber stark abgedunkelt.
Der 350Z nutzt nun ein frisches Gummimaterial mit weniger Farbmultiplikation.
Der 128×128-Felgenatlas enthält Profil und Seitenwand; es sind keine HD-Reifen.

Zum Landen in F9 einen Bodenmodus wählen: kontrollierter Sinkflug, danach
Räder ausklappen und Rückkehr zu Gelände/Straße/Drift. W/S und A/D erlauben
während des Anflugs eine Korrektur; „Fliegen“ bricht die Landung ab. F7 und der
Aussteigen-Knopf sind erst nach der Landung verfügbar. F8 entfernt das Fahrzeug
auch im Flug und stellt den Spieler wieder her. Der Flugzustand gehört zum
einzelnen Fahrzeug und wird nicht als Startmodus gespeichert.

Seit 0.13.1 lässt sich das F9-Menü mit gedrückter linker Maustaste an der
Titelleiste verschieben. Die Position bleibt auch nach Neustart gespeichert;
bei kleinerer Auflösung wird das gesamte Menü wieder in den Bildschirm gerückt.
Schließen und Fokusverlust beenden das Ziehen. Kategorien links sind verfügbar,
sobald ein Fahrzeug gespawnt und in Reichweite ist; ohne Auto bleibt die Garage.

Neu in 0.13.0: F9 hat eine seitliche Kategorienleiste mit originalen NFSU2-Icons,
ausgewählten deutschen Menübezeichnungen und originalen Frontend-Sounds.
Die Anordnung, Aktionen und Zuordnung der drei Sounds sind NFVU2-Anpassungen;
es wird kein vollständiger NFS-Menüablauf emuliert. Pfeil hoch/runter wechselt
die Kategorie. `Menu.SoundVolume` regelt die Navigationslautstärke (0 = stumm).
Import: `python tools/export_menu.py`; ohne Medien bleibt das Menü bedienbar.

F9 → Räder ergänzt **Tieferlegung von 0 bis 12 cm** in 0,5-cm-Schritten,
getrennt für 350Z/H2 gespeichert. Sie hebt die Radaufhängungspunkte relativ
zum Fahrzeug an: Der Aufbau setzt sich tiefer, die Bodenfreiheit sinkt.
Federweg, Federn, Dämpfer und bisherige Fahrmodi behalten ihre Abstimmung.
Zurücksetzen stellt auch die Serienhöhe wieder her. Beim Aussteigen bleibt
der tatsächliche vordere Lenkwinkel erhalten; beim Wiedereinstieg läuft die
Lenkung wieder weich zum Eingabewert.

F9 → Turret → **Nach dem Aussteigen automatisch bewachen** ist standardmäßig
an, sobald der Turret selbst eingeschaltet und das Auto einmal gefahren wurde.
Der Turret sucht mit aktivem Laserschalter sichtbar im vorderen 180°-Sektor
(±90° zur Fahrzeugfront) und erfasst feindliche Kreaturen bis 30 m.
Er richtet den Lauf aus, schießt bei freier Schusslinie und lädt automatisch
nach. Spieler, zahme Tiere, eigene Karosserie und Deckung blockieren die Linie;
Spieler, zahme und friedliche Kreaturen werden nicht als Ziele ausgewählt.
Die Automatik ist unabhängig vom weiterlaufenden Motor/Radio und arbeitet
auch an anderen geparkten 350Z in der Garage. Menüs, Fokusverlust und Tod des
lokalen Spielers pausieren sie. Beim Einstieg übernimmt die Maus; eine
gehaltene Feuertaste muss vor dem ersten manuellen Schuss losgelassen werden.
Munition und Lasereinstellungen gelten auch für die Automatik. Suchbewegung,
30-m-Reichweite und Ziel-/Feuerlogik sind NFVU2-Anpassungen. Der Suchsektor
orientiert sich an der [offiziellen Rust-Wiki](https://wiki.facepunch.com/rust/Item/autoturret).

| Taste | Funktion |
|---|---|
| F6 | In der Garage ausgewähltes Fahrzeug zusätzlich erstellen |
| F7 | Ein-/aussteigen |
| W / S | Gas / bremsen und rückwärts fahren |
| W + S halten | Im Stand Burnout; während der Fahrt zunächst bremsen |
| A / D | Lenken |
| Leertaste | Bremsen; im Driftmodus Hinterrad-Handbremse |
| Maus / Mausrad | Umschauen / Kameraabstand |
| Mittlere Maustaste | Kamera zentrieren |
| F9 | Fahrzeugmenü einschließlich Fahrmodus sowie Anzeige/Kamera |
| Linke Maustaste halten | Bei Vorwärtsfahrt mit Gas: Nitro; im Stand: Purge |
| H halten | Hupe |
| F8 | Aktuelles bzw. nächstes Fahrzeug innerhalb von 5 m entfernen |


F9 öffnet im Auto oder in dessen Nähe das Menü links oben. Es bremst das Auto und gibt die
Maus frei; Radio und Motor bleiben hörbar. Chat, Inventar, Spielmenü und
Fokusverlust pausieren Audio weiterhin. Beim Einstieg schaltet sich das Fahrlicht
ein, beim Ausstieg aus. Neon ist zunächst aus; Farbe und Pulsmodus sind gespeichert.
F9 → Anzeige wählt elf Original-Tachodesigns, Farbe, Größe (65–140 Prozent),
KM/H oder MPH sowie Ein-/Ausblenden. Der Tacho rechts unten zeigt tatsächliche
Fahrgeschwindigkeit, Drehzahl/Gang des akustischen Getriebes und den echten
Mod-Nitrotank einschließlich Verbrauch/Nachladung. Die originalen Grafiken
werden lokal in `hud/hud.json` erzeugt; Anordnung, Zeigerkalibrierung und
NOS-Füllung sind für Valheim angepasst. Fehlen sie, erscheint eine Textanzeige.
F9 → Anzeige steuert auch die dynamische Verfolgerkamera und ihr Nachziehen.
Sie folgt Kurven verzögert und orientiert sich teilweise an der Fahrtrichtung.
Mausblick bleibt kurz frei; nach rund 2,2 Sekunden ohne Mausbewegung zieht die
Kamera während der Fahrt wieder nach. Im Stand bleibt die gewählte Ansicht.
Mittlere Maustaste zentriert; Abschalten stellt das bisherige Kameraverhalten her.

F9 → Fahrzeug bietet Gelände, Straße und Drift. Drift ist eine zusätzliche
Mod-Abstimmung mit Heckantrieb, geringerem Seitengrip hinten, mehr Lenkwinkel,
schnellerem Gegenlenken und 20 Prozent mehr Drehmoment gegenüber Straße.
Leertaste zieht nur die Hinterrad-Handbremse und unterbricht dabei den Antrieb;
S bremst wie bisher zunächst vorwärts und legt erst im Stand rückwärts ein.
Rauch und Reifensound reagieren auf tatsächlichen Schlupf/Bodenkontakt.
Menü, Fokusverlust und Ausstieg bremsen weiterhin alle vier Räder. Gelände
und Straße behalten ihre bisherige Abstimmung; der Wechsel stellt sie wieder her.
Kamera und Driftgefühl müssen auf Valheim-Untergrund im Spiel abgestimmt werden.

Unter „Licht“ stehen 14 Vorlagen aus den originalen NFSU2-Sprachdateien zur Wahl:
Serie, Xenon 4000K, Rot, Blau, Grün, Violett, Gelb, Weiß extrahell, Xenon 8000K
und extrahelle Varianten der fünf Farben. Der Stärke-Regler reicht von 0 bis
300 Prozent und betrifft Lichtkegel und Emission der Frontscheinwerfer.
Die Einstellungen bleiben gespeichert; „Scheinwerfer zurücksetzen“ stellt
Serie/100 Prozent wieder her. RGB-Werte und Helligkeitsverhältnisse sind für
Valheim angenähert. Rück-, Brems-, Rückfahr- und Kennzeichenlicht bleiben unabhängig.
Die weißen Rückfahrleuchten in der Stoßstange reagieren auf die Rückwärtswahl:
S bremst bei Vorwärtsfahrt zunächst, erst nahe Stillstand wird rückwärts eingelegt.
Orange Heckstreifen sind sichtbar, haben bislang aber keine Blinkersteuerung.
Seit 0.5.0 ist das Rückfahrlicht deutlich gedimmt (Lichtintensität 0,45,
Reichweite 4 m, Emission 0,7). Beim Anfahren stehen vorwärts bis rund 11 km/h
20 Prozent mehr Drehmoment zur Verfügung; der Zuschlag klingt bis rund 22 km/h
aus. Der erste akustische Gang ist etwa neun Prozent kürzer.
W+S startet nahe Stillstand einen Burnout: Frontbremse hält, Hinterräder drehen.
Rauch und originales NFS-Reifenquietschen reagieren auf drehende Reifen mit
Bodenkontakt. Loslassen, Leertaste, Menü, Fokusverlust oder Aussteigen beendet
den Burnout; der normale Grip wird wiederhergestellt. Nitro ist dabei gesperrt.
Das geschwindigkeitsabhängige Rückwärts-Getriebeheulen ist synthetisch erzeugt;
ein eindeutig zugeordneter NFS-Rückwärtsgang-Sample wurde nicht gefunden.
F9 → Türen öffnet linke/rechte Tür, Haube und Heckklappe im Stand einzeln.
Beim Losfahren schließen sie automatisch. Die Türmechanik lässt sich zwischen
Normal, Suicide und Scherentüren wechseln; der Wechsel erfolgt nach dem Schließen.
Motoroptik Serie/Style 1/Style 2 betrifft ausschließlich das Modell.
F9 → Ausbau bietet Serien-Kofferraum oder drei Gehäuse mit 2/4/6 Einbauplätzen.
Je Platz sind Abdeckung, vier Subwooferdesigns, vier Verstärker und vier LCDs
wählbar. Standard ist Ausbau 2 mit vier Subwoofern. Teile und Einbaupunkte
stammen aus den lokalen NFS-Dateien; die passende Plattengröße ist nach der
Geometrie gewählt. LCDs verwenden statische Originaltexturen, keine Videowiedergabe.
Kofferraum-Neon hat einen eigenen Schalter, sieben Farben und Pulsmodus.
Die Leuchtstreifen, Lichtwirkung und Pulsfrequenz sind Mod-Anpassungen.
Bei weiterlaufendem Radio wird die Musik draußen mit zunehmender Tür- oder
Heckklappenöffnung lauter und weniger gedämpft. Der Kofferraum hat den stärksten
Effekt; beim Schließen wird es wieder leiser und dumpfer. Die Klangquelle folgt
der offenen Seite beziehungsweise dem Heck, bei mehreren Öffnungen deren Mitte.
Die Motorhaube beeinflusst die Musik nicht. Radiolautstärke, Pause und der
Schalter „Beim Aussteigen weiterlaufen lassen“ gelten weiterhin.
Der optionale Innenraumimport in 0.14.0 ergänzt Cockpit, Sitze, Pedale, Schalthebel
und Türverkleidungen aus der lokal bereitgestellten BeamNG-Mod. Das Lenkrad folgt
der Lenkung. Ohne Import bleibt das vereinfachte NFS-Basismodell erhalten.
Unter F9 → Räder lassen sich Sturz vorne/hinten (−15 bis +10 Grad),
Radversatz vorne/hinten (−10 bis +20 cm je Rad) und die gemeinsame Felgengröße
(16 bis 22 Zoll, ausgehend vom nominalen NFS-20-Zoll-Modell) einstellen.
Negativer Sturz neigt die Oberkante nach innen; positiver Versatz schiebt die
Räder nach außen. Die Reifenflanke passt sich an die Felge an, der äußere
Reifendurchmesser bleibt bei 70 cm. Diese Einstellungen sind optisch;
Radstand und Radkollision bleiben für diese optischen Regler unverändert.
Der zusätzliche Tieferlegungsregler versetzt die Aufhängungspunkte. Bremsen folgen Sturz
und Versatz, behalten aber ihre Größe. Änderungen erscheinen direkt, bleiben
gespeichert und können gemeinsam zurückgesetzt werden.
Nitro wirkt bei Vorwärtsfahrt mit Gas und Bodenkontakt; im Stand entstehen
Purge-Strahlen an Stoßstange und Haube. Ein voller Tank reicht sechs Sekunden,
lädt zwei Sekunden nach dem Loslassen während der Fahrt in zehn Sekunden nach.
F9 → Nitro schaltet die Funktion und optionale Flammen beim Gaswegnehmen.
Letztere sind zunaechst aus; ein optionaler Knall folgt jetzt demselben Flammenereignis.
Originale Feuer-/Rauchtexturen und Nitrobank-Aufnahmen sind importiert;
Boost, Nachladung, Partikelbewegung und Purge-Düsenpositionen sind Mod-Anpassungen.
Die beiden Nitrobank-Loops sind vorläufig Boost/Purge zugeordnet. Ob und ab
welcher Tuningstufe NFS automatisch Lastwechsel-Flammen auslöst, bleibt offen.
Das Kennzeichen verwendet die feste Originaltextur „UNDRGND 2“ auf einer eigenen
kleinen Schildfläche. Freier NFS-Kennzeichentext ist nicht nachgewiesen.
Die aktuelle 350Z-Audiomischung verwendet weiterhin die lokal importierten
NFSU2-Aufnahmen. Eigene vordere Motor- und hintere Auspuffquellen teilen sich
die dekodierten Motorclips, erhalten unterschiedliche Frequenzfilter und
starten synchron. Beim Drehen der Kamera vor das Auto werden Motor und Turbo
betont, von hinten der Auspuff; der Wechsel ist geglaettet. Die Aufnahmen
enthalten bereits einen Gesamtklang, die Trennung ist eine NFVU2-Abstimmung.
Blowoff wird beim deutlichen Schliessen des Gases aus gespeichertem Ladedruck
ausgeloest, nicht bei kleinen Gaskorrekturen. Nach der ersten Reduktion wurde
der Mix auf Wunsch um 2.3 dB angehoben (weiterhin rund 5.7 dB unter Vollpegel),
Schalt-Releases zusaetzlich reduziert; eine eigene Huellkurve blendet die lange
Aufnahme innerhalb von 550 ms aus. Exakte NFS-Ausloese- und Kameralogik sind
nicht rekonstruiert. Hummer-Sound und Fahrzeughandling bleiben unveraendert.

Neon-Pulsverlauf, Lichtstärken, Kennzeichenposition und Turbo-Auslösung sind
NFVU2-Anpassungen; eine vollständige originale Rachel-Rekonstruktion bleibt offen.

Die optionalen Lastwechsel-Aufnahmen liegen separat unter audio/exhaust.
Gemeinsam abgelegte, explizit benannte Backfire-Events wurden aus lokal
vorhandenen Spieldaten offline gerendert, gekuerzt und als Mono-PCM vorbereitet.
Die genaue Quelle ist im ignorierten Medienmanifest dokumentiert; eine
350Z-spezifische Zuordnung ist nicht belegt. NFVU2 steuert Timing, Abklingen,
Pitch und Lautstaerke selbst. Drei leise Pops beim deutlichen Gaswegnehmen,
bei aktivem Flammenereignis ein staerkerer Knall und zwei versetzte leise Pops.
Keine neuen Flammen oder Grafikressourcen und keine Aenderung am Handling.

Die erste Spielabnahme bestaetigte den Flammenknall, aber keinen wahrnehmbaren
Burble beim weiteren Ausrollen. Der reine Zwei-/Drei-Pop-Ansatz endete bereits
nach weniger als einer halben Sekunde. Eine nun getrennt vorbereitete,
fortlaufende Auspufftextur aus dem lokal installierten Online-2-Playtest bleibt
waehrend der Schubphase hoerbar. Drehzahl steuert Pitch und Pegel; erneutes
Gas beendet die Ebene, niedrige Drehzahl blendet sie aus. FMOD-Event und
Parameter sind belegt; exakte interne Ausloeseschwellen sind nicht rekonstruiert.
Flammenknall und Blowoff bleiben auf der bisherigen Abstimmung.
