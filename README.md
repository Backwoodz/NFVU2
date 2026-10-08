# NFVU2

**Need for Valheim: Underground 2** — ein privater Fahrzeug-Mod-Prototyp für Valheim.

Aktueller Stand: **0.3.0**, gebaut und automatisiert geprüft; Spieltest noch offen.
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
| A / D | Lenken |
| Leertaste | Bremsen |
| Maus / Mausrad | Umschauen / Kameraabstand |
| Mittlere Maustaste | Kamera zentrieren |
| F9 | Fahrzeugmenü: Fahrmodus, Radio, Licht, Neon und Nitro |
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

F9 öffnet im Auto oder in dessen Nähe das Menü. Es bremst das Auto und gibt die
Maus frei; Radio und Motor bleiben hörbar. Chat, Inventar, Spielmenü und
Fokusverlust pausieren Audio weiterhin. Beim Einstieg schaltet sich das Fahrlicht
ein, beim Ausstieg aus. Neon ist zunächst aus; Farbe und Pulsmodus sind gespeichert.
Die weißen Rückfahrleuchten in der Stoßstange reagieren auf die Rückwärtswahl:
S bremst bei Vorwärtsfahrt zunächst, erst nahe Stillstand wird rückwärts eingelegt.
Orange Heckstreifen sind sichtbar, haben bislang aber keine Blinkersteuerung.
Türen und Kofferraum sind vorbereitete, noch deaktivierte Menüeinträge.
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
