# Local asset pipeline

Detailed import notes retained in German from the former README.
Start with [CONTRIBUTING](../CONTRIBUTING.md) for prerequisites and build steps.
All source assets, tools, exports and binary packages stay ignored locally.

## Lokale Daten erzeugen

Alle folgenden Befehle im Projektverzeichnis ausführen:

```powershell
python -m pip install -r requirements.txt
python tools/extract_assets.py
python tools/export_geometry.py
python tools/export_rachel.py
python tools/export_hummer.py
python tools/prepare_hummer_audio.py
python tools/export_hud.py
python tools/export_menu.py
python tools/import_rust_logo.py --download
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
werden. `python tools/prepare_radio.py --ffmpeg path/to/ffmpeg.exe` erzeugt
anschließend kleinere Ogg-Vorbis-Kopien unter `.local/radio-ogg/` (Zielbitrate
192 kbit/s). WAV-Master bleiben unverändert; Songs werden vollständig
zurückdekodiert und auf Stereo, Abtastrate und Dauer geprüft. Der Build nutzt
die komprimierte Playlist nach Prüfsummenabgleich automatisch. Nach erneutem
Import oder Titeländerungen die Ogg-Vorbereitung wiederholen.
Turbo-Samples liegen separat
unter `.local/audio-turbo/`, Nitro-/Purge-Sounds unter `.local/audio-effects/`;
Build, Paket und Installer übernehmen die Medienordner.
Die 19 kurzen Motor-/Fahrgeräusche bleiben in der DLL eingebettet.
Der H2-Import verwendet dieselben lokalen CD-Abbilder und den vorhandenen
Geometrie-/Texturleser. Er erzeugt `.local/hummer/h2.json` und acht originale
Motorloops unter `.local/audio-hummer/`. Build und Installer übernehmen diese
als `hummer/` und `audio/hummer/` neben der unverändert benannten DLL.
`tools/render_hummer.py` erstellt mit Blender eine lokale Vorschau.

### Optionale Lastwechsel- und Auspuffaufnahmen

`tools/prepare_exhaust_audio.py` verarbeitet drei lokal bereitgestellte,
offline gerenderte Event-WAVs. Beispiel mit Dateien unter `.local/`:

```powershell
python tools/prepare_exhaust_audio.py --overrun .local/exhaust-source/pop_a.wav .local/exhaust-source/pop_b.wav --backfire .local/exhaust-source/bang.wav --source-description "Originaltitel, Bank und genaue Eventnamen"
```

Ausgabe: `.local/audio-exhaust/{overrun_01.wav,overrun_02.wav,backfire.wav}`
und ein Manifest mit Herkunft, Quell-/Ausgabehashes und Verarbeitungsschritten.
Optional ergaenzen `--loop .local/exhaust-source/coast.wav` und
`--loop-source-description "Originaltitel, Event und Renderparameter"`
eine weitere Datei `overrun_loop.wav`. Die Schleife verwendet rund drei
Sekunden ohne Event-Anlauf, eine 35-ms-Kreuzblende an der Naht und einen
begrenzten Arbeitspegel (RMS-Ziel 0.085, Peak maximal 0.75). Die kurzen Pops
werden weiterhin nicht hoch normalisiert. Ein reiner Pop-Reimport erhaelt
eine vorhandene Schleife samt ihrer getrennten Herkunftsangabe.
Der Build kopiert nur die vorbereiteten Medien nach `audio/exhaust/`.
Originalbanken, Renderwerkzeuge und native Bibliotheken bleiben lokal und
werden vom Mod nicht benoetigt. Die Dateien sind optional; fehlerhafte oder
fehlende Clips duerfen den normalen Motorsound nicht abschalten.

Der aktuelle lokale Import verwendet gemeinsam abgelegte, ausdruecklich als
Backfire bezeichnete Events aus CarX Drift Racing Online. Eine spezifische
350Z-Zuordnung und originales Burble-Timing sind nicht belegt. Mono-Downmix,
Beschnitt auf 360/240 ms und kurze Fades entfernen Vorlauf und lange Halltails.
Clipping in der Eingabe wird abgelehnt; leise Aufnahmen werden nicht pauschal
hoch normalisiert. `ExhaustOverrunModel` erzeugt die kurze Knatterfolge selbst.
Der zusaetzliche Dauerklang stammt aus dem lokal installierten Online-2-Playtest:
FMOD-Bank `car_parts`, Event `car_system_antilag`. Es laesst sich offline als
fortlaufender Klang rendern und deklariert unter anderem RPM, Load, HP,
Motorart und Anti-Lag-Stufe. Metadaten enthalten Bedingungsbausteine fuer
Gasstellung, Drehzahl, Geschwindigkeit und Timer. Die Original-Schwellwerte,
Motorzuordnung und vollstaendige Steuerlogik sind damit nicht rekonstruiert.
NFVU2 startet die Ebene nach Lastaufbau bei geschlossenem Gas, traegt sie
waehrend des Ausrollens mit drehzahlabhaengiger Tonhoehe/Lautstaerke weiter
und blendet bei erneutem Gas beziehungsweise niedriger Drehzahl aus.
Die Originalaufnahmen bleiben Originalaufnahmen; neutrale Dateinamen aendern
ihre Herkunft nicht. Alle Medien und Herkunftsmanifeste bleiben Git-ignoriert.

### Optionaler 350Z-Innenraum und Fahrer

Die lokal bereitgestellte ZIP „Nissan 350Z FIX UKBEAMNG“ wird unter
`.local/interior-source/beamng-350z/` entpackt; dort muss
`vehicles/Nis350Z/Nis350Z.dae` liegen. Nur Modelle, Texturen und Materialdaten
werden verarbeitet, keine BeamNG-Skripte ausgeführt.

```powershell
python tools/export_interior.py
python tools/build.py
python tools/test.py --skip-build
```

Der Import erzeugt den optionalen Mod-Payload `interior/interior.json`:
62 Teilmeshes, rund 41.000 Dreiecke und 15 eingebettete Texturen. Zwei fehlende
Quelltexturen erhalten einfache dunkle Ersatzmaterialien. Dach und Kofferraum
bleiben aus dem NFS-Modell; überlappende Innenflächen werden ersetzt.
`tools/render_interior.py` erzeugt mit Blender lokale Kontrollansichten.

F7 im Stand: Der Charakter geht zur linken Fahrertür, die Tür öffnet, er nimmt
sichtbar Platz und die Tür schließt. Erst dann werden Gas, Nitro und Turret
freigegeben. F7 zum Aussteigen benötigt Stillstand und einen freien Platz
neben der Fahrertür. F8 entfernt das Fahrzeug und stellt den Spieler sofort
wieder her. Menüs/Fokusverlust pausieren den Übergang. H2 bleibt unverändert.

Seit 0.14.1 sitzt der Fahrer tiefer; die gemessene Kopfposition begrenzt seine
Höhe unter dem Dach. Die rechte Hand lenkt, der linke Arm ruht am offenen
Fahrerfenster. Beim Öffnen der Tür wird der linke Arm eingezogen. Die Drehrichtung
des sichtbaren Lenkrads ist korrigiert; die rechte Hand folgt derselben Drehung.
F9 → Türen bietet eine gespeicherte Scheibentönung von 0 % (klar) bis 100 %
(dunkel) für die geschlossenen 350Z-Scheiben. Im Stand lassen sich die Türen
auch vom Sitz aus bedienen, nach Abschluss des Ein-/Ausstiegs. Der Menüaufruf
pausiert beim Sitzen die Türbewegung nicht; beim Losfahren schließen die Türen.

0.14.2 entfernt die geerbte rote Eigenleuchtung einer möglichen Lava-Laternen-
Glasvorlage. Das Fenstermaterial übernimmt nur den geladenen Shader und nutzt
neutrale Grauwerte ohne Eigenleuchtung.

Der sichtbare Fahrer verwendet Valheims vorhandene Stuhl-Sitzanimation,
einen eigenen Bewegungsablauf und prozedurale Ausrichtung der Gliedmaßen.
Das Charaktervisual wird im Sitz auf 85 % skaliert; Spielfigur, Inventar und
Ausrüstung bleiben dieselben. Die Animation ist keine original aus BeamNG
übernommene Einsteigeanimation. Pose, Kleidung, Fenstershader, Türstile und
Kollisionen müssen im Spiel geprüft werden.

Quelle: [Modland — Nissan 350Z FIX UKBEAMNG](https://www.modland.net/beamng.drive-mods/cars/nissan-350z-fix-ukbeamng.html).
Der Originalautor ist auf der Downloadseite unbekannt; eine Freigabe zur
Weiterveröffentlichung der Assets ist nicht belegt. Archiv, extrahierte Dateien
und generierter Innenraum bleiben ignorierte lokale Dateien.

### Optionaler Rust-Import

In der ignorierten `config.local.json` den Schlüssel `rust_dir` auf die eigene
Rust-Installation setzen. Der Import liest deren Bundles unverändert und legt
nur die ausgewählten Modelle, Texturen und acht Sounds in `.local/turret/` ab:

```powershell
python -m pip install --target .local/tools/unitypy UnityPy==1.25.4 numpy
python tools/export_rust_turret.py
python tools/build.py
python tools/test.py --skip-build
python tools/install.py
```

Rust wird zum Import benötigt, nicht während der Fahrt. Ohne importierte Assets
bleibt der Turret-Menüpunkt mit Erklärung deaktiviert. Der Reader unterstützt
die lokal geprüften UnityFS-Bundles mit unkomprimierten Nutzdaten; andere
Bundlevarianten werden abgelehnt. Originaldateien, Werkzeuge, Exporte und
lokale Pakete bleiben ignoriert und gehören nicht ins Git-Repository.
