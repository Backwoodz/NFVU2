---
name: tiktok
description: Schneide NFVU2-Spielaufnahmen mit FFmpeg in Hochkantclips mit dem freigegebenen Zoom, verschwommenem Hintergrund und Farbkorrektur. Verwenden bei /tiktok, $tiktok oder weiteren NFVU2-Gameplay-Clips für TikTok.
---

# NFVU2 TikTok-Clips

Erstelle lokale Videoexporte im vom Nutzer freigegebenen Stil. Alle Projektpfade
beziehen sich auf das Repository-Stammverzeichnis. Keine eigene Schnitt-App bauen.

## Auftrag und lokale Dateien

- Übernimm Quelldatei und Start-/Endzeit aus dem Auftrag; Dauer = Ende minus Start.
  Wenn das Video unklar ist, frage nach der Aufnahme und bereite währenddessen
  eine Bildübersicht vor. Für noch nicht festgelegte Szenen dienen Bilder mit
  Zeitmarken als Auswahlhilfe. Der Befehl allein autorisiert keine Veröffentlichung.
- Lies `.local/tiktok/NOTES.md`, die passende `.local/tiktok/settings*.json` und
  vorhandene lokale Helfer, wenn diese existieren. Absolute Quell-/Exportpfade
  stehen nur dort. Fehlende Originale nicht durch einen alten Export ersetzen.
- Bisherige Quellen liegen lokal im Videos/Desktop-Ordner. Fertige MP4-Dateien
  gehören in dessen Unterordner `NFVU2-Exports`, außerhalb des Git-Repositorys.
  Nutze den vorhandenen `export_directory` oder ermittle den Videos-Ordner des
  aktuellen Nutzers. Erzeuge Bilder, Logs, Tools und Konfigurationen unter
  `.local/tiktok/`; keine persönlichen absoluten Pfade in getrackte Dateien schreiben.
- Originale unverändert lassen. Vorhandene Exporte nicht überschreiben oder löschen.
  Frühere Driving-Testvarianten hat der Nutzer selbst gelöscht; nicht wiederherstellen.

## Freigegebener Standard

Ausgangspunkt sind Aufnahmen mit **3440 × 1440, 60 FPS, HEVC Main 10,
HDR PQ (smpte2084), BT.2020 und full range**. Prüfe jede neue Quelle mit
FFprobe oder `ffmpeg -hide_banner -i`; SDR braucht kein HDR-Tonemapping.
Andere Auflösungen brauchen angepasste Geometrie, keine blinde Wiederverwendung.

| Bestandteil | Freigegebene Einstellung |
| --- | --- |
| Ausgabe | 1080 × 1920, SAR 1:1, H.264, yuv420p, SDR BT.709 limited range |
| Bildrate | Ursprüngliche Bildrate erhalten; bisher 60 FPS, keine Zeitlupe |
| Scharfer Quellausschnitt | 1720 × 1280, x=860, y=160 |
| Vordergrund | 1080 × 804, x=0, y=558; keine Streckung |
| Hintergrund | Derselbe Clip synchron, auf 9:16 vergrößert/beschnitten |
| Hintergrundeffekt | Zunächst 270 × 480, Gaussian Blur Sigma 6, Sättigung 0,65, Helligkeit −0,04; danach 1080 × 1920 |
| Gameplay-Farbkorrektur | Kontrast 1,14; Sättigung 1,18; Helligkeit +0,016; Gamma 1,10 |
| Titel | Englischer szenenbezogener Text, weiß, schwarzer Rand 4 px, Arial Bold 54 px; x=(w-text_w)/2, y=464 |
| Video-Encoding | libx264, preset fast, CRF 20, fps_mode passthrough, movflags +faststart |
| Ton | Aufgezeichneten Spielton inkl. Radio erhalten; AAC Stereo 48 kHz, Zielbitrate 192 kbit/s; keine Lautstärkeänderung |

Die Farbkorrektur gilt nur für das scharfe Gameplay; Hintergrund bleibt weich
und zurückhaltend. Das Motiv darf nicht abgeschnitten werden: Auto einschließlich
Flugdüsen, offenen Türen/Kofferraum und gegebenenfalls tanzender Figur prüfen.
Der Ausschnitt ist ein Ausgangspunkt; passe ihn bei anderen Kamerapositionen an.
Ein fester Ausschnitt reicht bisher. Einige HUD-/Menüfragmente an den Seiten
sind akzeptiert; für deren Entfernung nicht das Auto oder die Figur beschneiden.

HDR-Tonemapping vor Farbkorrektur (für die oben beschriebene HDR-Quelle):

```text
zscale=rangein=full:transfer=linear:npl=100,format=gbrpf32le,zscale=primaries=bt709,tonemap=tonemap=mobius:desat=0,zscale=transfer=bt709:matrix=bt709:range=limited,format=yuv420p
```

Der vorhandene Renderer skaliert vor dem Tonemapping auf 2580 × 1080 (75 %).
In dieser Zwischenauflösung lautet der Crop 1290 × 960 bei x=645, y=120.
Danach auf 1080 × 804 skalieren und `eq=contrast=1.14:saturation=1.18:brightness=0.016:gamma=1.10` anwenden.
Vorder-/Hintergrund aus demselben Videozweig teilen, überlagern, dann Titel hinzufügen.

## Export und Prüfung

Vorhandenes FFmpeg liegt derzeit unter
`.local/tools/audio-encoder/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe`.
Wenn es fehlt, zuerst ein installiertes FFmpeg suchen; notwendige Tools lokal
unter `.local/` halten. Bei fehlenden Helfern diesen Ablauf direkt mit FFmpeg
rekonstruieren; keine alten benutzerspezifischen Pfade erfinden.

- Extrahiere zuerst repräsentative Einzelbilder inklusive Start/Ende und
  Kameraschwenks. Prüfe Geometrie und Helligkeit. Bei neuem Stil zuerst kurz
  rendern; bei weiteren Clips im freigegebenen Stil direkt den gewünschten Abschnitt.
- Lege je Aufnahme eine eigene ignorierte Settings-Datei an. Das vorhandene
  Skript aktiviert die akzeptierte Farbkorrektur **nur mit `--color-correction`**:

```powershell
python .local/tiktok/render_preview.py --settings settings-flying.json --start 39 --duration 32 --color-correction --output Flying-39-71-zoom-color.mp4
```

- Dateinamen nennen Motiv, Quellzeitraum und Variante. Bei Kollision neue Revision
  wählen. Übernimm passende Titel; keine Details des Spiels als eigene Implementierung
  ausgeben. Nicht für jeden neuen Clip zusätzliche Vergleichsvideos exportieren.
- Prüfe die fertige Datei vollständig auf Dekodierfehler, Auflösung, Bildrate,
  Farbcodierung, Tonspur und Dauer. AAC-Paketgrenzen können die MP4-Dauer um
  wenige Millisekunden verlängern. Prüfe Layout-Einzelbild und Stichproben über
  den ganzen Clip visuell. Eine technische Tonspurprüfung ist keine Hörprüfung.
- Aktualisiere `.local/tiktok/NOTES.md` mit Abschnitt, Settings, Dateiname und
  Prüfergebnis. Liefere einen klickbaren Dateipfad und den eindeutigen Dateinamen.

## Bisherige Clips

| Quelle | Gewählter Zeitraum | Titel | Export |
| --- | --- | --- | --- |
| Driving.mp4 | 00:24–01:12 | Driving a 350z in Valheim | Driving-24-72-zoom-color.mp4 |
| Driving and turret.mp4 | 00:18–00:56 | 350z with a turret in Valheim | Driving-turret-18-56-zoom-color.mp4 |
| Subwoofer and dance.mp4 | 00:07–00:45 | Dancing with a 350z in Valheim | Dancing-07-45-zoom-color.mp4 |
| Flying and turret.mp4 | 00:39–01:11 | Flying a 350z in Valheim | Flying-39-71-zoom-color.mp4 |

Diese Tabelle dokumentiert den bisherigen Stand, kein Auftrag zum erneuten Rendern.
