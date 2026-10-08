# NFVU2

**Need for Valheim: Underground 2** — ein privater Fahrzeug-Mod-Prototyp für Valheim.

Aktueller Stand: **0.7.2**, gebaut und automatisiert geprüft; Spieltest noch offen.
Die vorherige Version 0.2.1 ist vom Nutzer in Valheim bestätigt. Ein aus eigenen NFS-Underground-2-Dateien ausgelesener
Nissan 350Z lässt sich in einer lokalen Valheim-Einzelspielerwelt spawnen und fahren.
Mauskamera, Gelände-/Straßenmodus und Fahrzeug-Sound sind im Spiel bestätigt.
Neu: Fahrzeugmenü, lokales NFS-Radio, automatische Beleuchtung, Originalkennzeichen,
farbiges/pulsierendes Neon sowie frühere akustische Gangwechsel und Turbo.
Die grundsätzliche Funktion von 0.2.0 wurde vom Nutzer in Valheim bestätigt.
0.2.1 ergänzt fehlende Heckleuchtenflächen, weiße Rückfahrleuchten, 28 cm breite
Reifen und größere Sportbremsen sowie echte Künstler-/Songnamen im Radio.
0.3.0 ergänzt Nitro per linker Maustaste, grüne Auspuffflammen, Purge im Stand
und getrennte Chromring-/Goldstern-Materialien für die Felgen.
0.3.1 korrigiert den spiegelverkehrten 350Z-Schriftzug am Heck.
0.4.0 ergänzt fünf Live-Regler für Sturz, Radversatz und Felgengröße unter F9 → Räder.
0.4.1 platziert das Menü links oben und ergänzt Scheinwerferfarben und -stärke.
0.5.0 ergänzt kräftigeres Anfahren, einen etwas kürzeren ersten Sound-Gang,
Rückwärtsgang-Heulen, schwächeres Rückfahrlicht und W+S-Burnouts mit Rauch.
0.6.0 ergänzt Aufprallschaden an kleinen Bäumen, Büschen und Gegnern.
F9 → Fahrzeug → „Beim Aussteigen weiterlaufen lassen“ hält Motor, aktiviertes
Neon und gedämpfte, räumliche Musik nach der Fahrt an; standardmäßig eingeschaltet.
0.7.0 ergänzt originale Kofferraumausbauten mit einzeln wählbaren Subwoofern,
Verstärkern und LCDs sowie eigenem Neon. Standard: vier Subwoofer in Ausbau 2.
Türen, Haube und Heckklappe öffnen mit den originalen Animationsdaten;
normale Türen, Suicide Doors und NFS-Scherentüren sind wählbar.
Drei originale Motoroptiken und die vereinfachten Innenflächen sind enthalten.
0.7.1 ergänzt die zuvor ausgelassenen oberen Rückleuchten-Einsätze und schließt
damit die offenen Sichtlinien vom Heck zum Kofferraum-Neon.
0.7.2 lässt die Außenmusik mit offenen Türen oder offener Heckklappe lauter
und klarer werden; die räumliche Klangquelle wandert zur jeweiligen Öffnung.

Die Darstellung verwendet jetzt die Originalvoreinstellung `DDAY_PLAYER_CAR`:
Widebody-Kit 02, Carbonhaube 07, Spoiler 02, Racing-Hart-Felgen 07,
Leuchten und Doppel-Endrohre sowie das originale Drachenvinyl mit Farbpalette.
Lack und Felgentönung sind für Valheim angenähert. Scheiben sind vorerst dunkel
und undurchsichtig, transparente Leuchtenabdeckungen fehlen. Die Darstellung
von 0.1.9 wurde nach der Textur-Upload-Korrektur vom Nutzer in Valheim bestätigt.
Die Räder sind optisch auf das Widebody abgestimmt; die Vorschau verwendet eine
angenäherte eingefederte Stellung. Die tatsächliche Federstellung bestimmt Unity.
Der sichtbare Reifendurchmesser beträgt 70 cm, die Breite seit 0.2.1 28 cm.
Die Sportbremsen verwenden STYLE03 mit roten Sätteln und 38-cm-Scheiben;
Reifenbreite und Bremsengröße sind visuelle Anpassungen für diesen Mod.
Seit 0.1.8 misst der Import die
Reifenoberfläche statt der größeren quadratischen Fläche hinter der Felge.
Die Fahrphysik nutzt Unity WheelCollider. Eine Bridge zu einem parallel laufenden
NFS-Prozess ist eine mögliche spätere Forschungsaufgabe und noch nicht implementiert.

## Bedienung

| Taste | Funktion |
|---|---|
| F6 | Fahrzeug erstellen |
| F7 | Ein-/aussteigen |
| W / S | Gas / bremsen und rückwärts fahren |
| W + S halten | Im Stand Burnout; während der Fahrt zunächst bremsen |
| A / D | Lenken |
| Leertaste | Bremsen |
| Maus / Mausrad | Umschauen / Kameraabstand |
| Mittlere Maustaste | Kamera zentrieren |
| F9 | Fahrzeugmenü: Fahrmodus, Radio, Licht, Neon, Nitro, Räder, Türen und Ausbau |
| Linke Maustaste halten | Bei Vorwärtsfahrt mit Gas: Nitro; im Stand: Purge |
| H halten | Hupe |
| F8 | Fahrzeug entfernen |

## Projektstruktur

- `src/`: C#-Mod, einschließlich Kamera, Antrieb und Sound.
- `tools/`: Extraktion, Konvertierung, Bau und Tests.
- `tests/`: Prüfungen für Modelldaten, Antrieb und Audio.
- `docs/`: Spielanleitung, Quellen, Änderungsverlauf und nächste Aufgaben.
- `.local/`: lokale Spieldateien, erzeugte Modelle/Sounds und externe Werkzeuge; ignoriert.
- `artifacts/`: fertige Mod, Testprogramme und lokale Pakete; ignoriert.
- `config.local.json`: Installationspfade des jeweiligen Rechners; ignoriert.

## Voraussetzungen

- Windows, Python 3.10 oder neuer und ein .NET SDK ab Version 7.
- Valheim mit BepInEx; entwickelt gegen Valheim 1.0.17 / Unity 6000.0.75f1
  und BepInExPack Valheim 5.4.2351.
- Eigene NFS Underground 2-CD-Abbilder im bisher getesteten MODE1/2352-BIN-Format.
- Für Audioaufbereitung: NumPy und [vgmstream](https://github.com/vgmstream/vgmstream/releases/tag/r2117).

`config.example.json` nach `config.local.json` kopieren und die Pfade anpassen.
Die Mod referenziert die installierten Unity-/Valheim-/BepInEx-Bibliotheken.
Diese Bibliotheken und die Originalspielinhalte werden nicht mitversioniert.

## Lokale Daten erzeugen

Alle folgenden Befehle im Projektverzeichnis ausführen:

```powershell
python -m pip install -r requirements.txt
python tools/extract_assets.py
python tools/export_geometry.py
python tools/export_rachel.py
python tools/extract_audio.py
python tools/decode_audio.py
python tools/prepare_audio.py
python tools/extract_audio.py --turbo-only
python tools/decode_audio.py --turbo-only
python tools/prepare_audio.py --turbo-only
python tools/prepare_effects_audio.py
python tools/extract_music.py
```

Den offiziellen vgmstream-Decoder vorher lokal entpacken und seinen Pfad in
`config.local.json` eintragen. Der Import liest ausschließlich benötigte Daten
aus den CD-Abbildern; er führt keinen NFS-Installer aus.

Das Radio lädt lokale Stereo-WAV/OGG-Dateien aus dem Unterordner `radio/`
neben der DLL. Der Import erzeugt `.local/radio/tracks.json` mit 27 verifizierten
EA-Trax-Songs und echten Künstler-/Titelnamen. Die Zuordnung in
`tools/music_titles.json` wird durch MPF- und Audio-Prüfsummen abgesichert;
weitere Containerstreams werden nicht als Songs angeboten. Bestehende
Importe können mit `python tools/extract_music.py --refresh-titles` aktualisiert
werden. Turbo-Samples liegen separat
unter `.local/audio-turbo/`, Nitro-/Purge-Sounds unter `.local/audio-effects/`;
Build, Paket und Installer übernehmen die Medienordner.
Die 19 kurzen Motor-/Fahrgeräusche bleiben in der DLL eingebettet.

## Bauen und prüfen

```powershell
python tools/build.py
python tools/test.py --skip-build
python tools/package.py
```

Die Bauprogramme suchen das vorhandene .NET SDK und dessen Referenzbibliotheken.
Sie verwenden den mitgelieferten C#-Compiler direkt und benötigen keinen
NuGet-Download. `src/NFVU2.csproj` dient zusätzlich als Projektdatei für Editoren;
dafür `ValheimDir` beziehungsweise die Umgebungsvariable `NFVU2_VALHEIM` setzen.

Prüfungen umfassen Modell-/Lichtzustände, Antrieb, Audio/Turbo, Radioauswahl, Nitro-/Eingabezustände,
Originalasset-Verarbeitung und Installation mit Rücksicherung.
Hinzu kommen Kofferraumkatalog, Animationsimport, Bewegungszustände und
Validierung von Scharnier-/Einbaupunkten. Externe Blender-Vorschauen prüfen
die Geometrie und Posen, nicht die Darstellung im Spiel.
Die Antriebsprüfungen verwenden kontrollierte API-Antworten. Sie ersetzen keinen
Unity-Fahrtest; auch die Audio-Prüfungen spielen nichts in Valheim ab.

Die Antriebsprüfungen funktionieren ohne Spieldateien:

```powershell
python tools/test.py --handling-only
```

Nach den vollständigen Tests kann eine externe Soundvorschau erzeugt werden:

```powershell
python tools/render_audio_preview.py
```

## Installieren

Valheim regulär schließen und `python tools/install.py` ausführen. Das Werkzeug
prüft den laufenden Prozess, sichert die installierten Dateien in `artifacts/backups/`
und verifiziert alle kopierten Dateien mit SHA-256.

Alternativ den Inhalt von `artifacts/mod/` einschließlich `radio/` und
`audio/turbo/` und `audio/effects/` nach `<Valheim>/BepInEx/plugins/Rachel350Z/` kopieren.
Der bestehende Plugin-Name und die
Konfigurationskennung bleiben für die bisherige Installation erhalten.

Eine lokale Einzelspieler-Testwelt öffnen. Das Fahrzeug wird nicht gespeichert
und unterstützt derzeit keine Mehrspielersitzungen. Audio-Lautstärke und
Kameraeinstellungen stehen in `BepInEx/config/local.rachel350z.prototype.cfg`.

F9 öffnet im Auto oder in dessen Nähe das Menü links oben. Es bremst das Auto und gibt die
Maus frei; Radio und Motor bleiben hörbar. Chat, Inventar, Spielmenü und
Fokusverlust pausieren Audio weiterhin. Beim Einstieg schaltet sich das Fahrlicht
ein, beim Ausstieg aus. Neon ist zunächst aus; Farbe und Pulsmodus sind gespeichert.
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
Der Innenraum ist das vereinfachte Original-Basismodell mit Türinnenflächen;
ein detailliertes Cockpit und transparente Scheiben bleiben offen.
Unter F9 → Räder lassen sich Sturz vorne/hinten (−15 bis +10 Grad),
Radversatz vorne/hinten (−10 bis +20 cm je Rad) und die gemeinsame Felgengröße
(16 bis 22 Zoll, ausgehend vom nominalen NFS-20-Zoll-Modell) einstellen.
Negativer Sturz neigt die Oberkante nach innen; positiver Versatz schiebt die
Räder nach außen. Die Reifenflanke passt sich an die Felge an, der äußere
Reifendurchmesser bleibt bei 70 cm. Diese Einstellungen sind optisch;
Radstand, WheelCollider und Fahrwerk bleiben unverändert. Bremsen folgen Sturz
und Versatz, behalten aber ihre Größe. Änderungen erscheinen direkt, bleiben
gespeichert und können gemeinsam zurückgesetzt werden.
Nitro wirkt bei Vorwärtsfahrt mit Gas und Bodenkontakt; im Stand entstehen
Purge-Strahlen an Stoßstange und Haube. Ein voller Tank reicht sechs Sekunden,
lädt zwei Sekunden nach dem Loslassen während der Fahrt in zehn Sekunden nach.
F9 → Nitro schaltet die Funktion und optionale Flammen beim Gaswegnehmen.
Letztere sind zunächst aus; ein eindeutig zugeordneter Knall-Sample fehlt noch.
Originale Feuer-/Rauchtexturen und Nitrobank-Aufnahmen sind importiert;
Boost, Nachladung, Partikelbewegung und Purge-Düsenpositionen sind Mod-Anpassungen.
Die beiden Nitrobank-Loops sind vorläufig Boost/Purge zugeordnet. Ob und ab
welcher Tuningstufe NFS automatisch Lastwechsel-Flammen auslöst, bleibt offen.
Das Kennzeichen verwendet die feste Originaltextur „UNDRGND 2“ auf einer eigenen
kleinen Schildfläche. Freier NFS-Kennzeichentext ist nicht nachgewiesen.
Neon-Pulsverlauf, Lichtstärken, Kennzeichenposition und Turbo-Auslösung sind
NFVU2-Anpassungen; eine vollständige originale Rachel-Rekonstruktion bleibt offen.

## Weitere Informationen

- [Nächste Aufgaben](docs/ROADMAP.md)
- [Versionsgeschichte](docs/CHANGELOG.md)
- [Spielanleitung](docs/Spielanleitung.txt)
- [Audioquellen und Verarbeitung](docs/Audioquellen.txt)
- [Technische Quellen](docs/REFERENCES.md)

Das Repository enthält den selbst erstellten Mod-Code und die Werkzeuge zur
lokalen Aufbereitung. CD-Abbilder, extrahierte Modelle/Sounds, Spielbibliotheken
und erzeugte Mod-Pakete bleiben außerhalb der Git-Historie.
