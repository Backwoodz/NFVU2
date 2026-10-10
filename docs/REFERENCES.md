# Technische Quellen

## NFSU2-Nitro-Bildfilter und Lichtspuren, Untersuchung 2026-10-09

- [yugecin: NFSU2-Grafikoptionen](https://yugecin.github.io/nfsu2-re/docs.html)
  ordnet Motion Blur, Light Trails und Light Glow den separaten Schaltern
  `g_MotionBlurEnable`, `g_LightStreaksEnable`, `g_LightGlowEnable` zu.
  Dieselben Namen wurden in der lokalen `SPEED2.EXE` gefunden, bei Datei-
  Offsets `0x3a37e4`, `0x3a37cc`, `0x3a37b8`. Das belegt getrennte Optionen,
  noch keinen exakten Algorithmus oder ausschließliche Nitro-Aktivierung.
- Lokale EXE: SHA-256
  `2cbd698fee8338a13d1891bc17d27e6641b4b9dd40580de818594b9140222899`.
  Aus deren PE-Ressourcen wurden ausschließlich zur lokalen Untersuchung
  `IDI_FILTER_FX` (14376 Byte), `IDI_OVERBRIGHT_FX` (5692 Byte) und
  `IDI_SCREENFILTER_FX` (16116 Byte) gelesen. Enthaltene Namen zeigen
  vier Filtertexturen mit Gewichten, einen Overbright- sowie einen separaten
  Farb-/Bildfilter. Das ist ein Nachweis der D3D9-Filterbausteine, keine
  vollständige Analyse der Draw-Aufrufe oder des Nitro-Filterkerns.
- Fortgesetzte Untersuchung: 20 eingebettete Vertex-/Pixelshader dieser drei
  Ressourcen mit Microsofts lokalem D3DX9-Disassembler ausgewertet, ohne
  die Spiel-EXE auszuführen. `IDI_FILTER_FX` enthält vier Varianten mit
  jeweils 1, 2, 3 oder 4 Abtastungen: komponentenweise gewichtete Summe
  `output = sum(texture_i(uv_i) * weight_i)`. Die zugehörigen Vertexshader
  reichen Position und vier UV-Sätze unverändert weiter. Radiale Verschiebung,
  Fluchtpunkt, Gewichtswerte und eventuell vorherige Frames sind darin nicht
  festgelegt; dafür müssten gebundene Texturen, Vertexdaten und Draw-Aufrufe
  des laufenden Spiels untersucht werden. Vier Eingänge bedeuten weder vier
  vorherige Frames noch den Nachweis, dass Nitro diesen Filter verwendet.
- `IDI_OVERBRIGHT_FX` liest eine Textur, zieht einen einstellbaren Offset
  ab, verstärkt und begrenzt RGB, gewichtet es nach Helligkeit und quadriert
  das Ergebnis. Dieser Shader isoliert/verstärkt helle Anteile; er berechnet
  selbst keine räumliche Unschärfe. Die untersuchten `SCREENFILTER`-Varianten
  enthalten Farb-/Gain-/Rampmap-Verarbeitung. Aus dem Namen „Screenfilter“
  allein lässt sich deshalb kein Nitro-Tunnelblick ableiten.
- Grenze der statischen Codeanalyse: Die lokale EXE hat einen zusätzlichen
  Einstieg bei VA `0x93309e`; ihre `.text`-Sektion besitzt nahezu maximale
  Byte-Entropie (ca. 7,999 Bit/Byte) und liefert keinen plausiblen normalen
  x86-Code. Das spricht für eine gepackte/geschützte Codeabbildung. Direkte
  Aufrufer der lesbaren Effektnamen lassen sich daraus nicht zuverlässig
  rekonstruieren. Ein bestimmtes Schutzverfahren ist damit nicht identifiziert.
  Die nächsten belastbaren Belege wären eine lesbare Laufzeitabbildung und
  ein Grafikmitschnitt derselben Fahrsituation mit/ohne Nitro sowie mit einzeln
  geschalteten Motion Blur, Light Trails und Light Glow. Dabei insbesondere
  Texturhistorie, UVs, Filtergewichte, Blendzustände und Spur-Geometrie prüfen.
- [NFSU2_CustomShaders: eEffect.h, Revision 1e850a4](https://github.com/ArchieGoldmill/NFSU2_CustomShaders/blob/1e850a41661181cef4e65effb6ba75ac8bc7251f/eEffect.h)
  dokumentiert dazu passende Effektarten und Shaderparameter wie
  `FILTERWEIGHTS`, `SCREENOFFSET`, `MOTIONALPHA`. Diese Parameternamen stehen
  auch in der lokalen EXE; `MOTIONALPHA` wurde als Zeichenfolge in keiner
  PE-Ressource nachgewiesen. Das schließt eine andere Verwendung nicht aus.
  Fremde Mod-Shaders sind kein Beweis
  für die unveränderte originale Nitro-Implementierung.
- Lokales `GLOBAL/GlobalB.lzc`, dekomprimiert: Originaltextur
  `SFX_FLARE_STREAK_A`, 128 × 128, erfolgreich mit dem vorhandenen Decoder
  ausgelesen und visuell geprüft. Sie zeigt einen kleinen hellen Flare auf
  dunklem Grund. Die konkrete Zuordnung zur Rücklichtspur und deren
  Lebensdauer/Blendmodus sind damit noch nicht nachgewiesen. Originaldaten
  und extrahierte Effekte bleiben unter `.local/effects-research/`.
- Ableitung für eine mögliche NFVU2-Nachbildung: separater radialer Bildfilter
  (Verwischen zum/vom Fluchtpunkt), kurz auslaufende Lichtspuren an den
  Rückleuchten und weiches Leuchten. Exakte NFS-Samplepositionen, Gewichte,
  Bewegungs-/Nitro-Kopplung und zeitliche Historie bleiben Forschungsfragen.
  In 0.17.0 als eigene Nachbildung implementiert: sechs radial versetzte
  Bildlagen mit weicher Fahrzeug-/Zentrumsmaske sowie kurze rote 3D-Lichtbänder.
  UV-Versatz, Gewichtung, Hüllkurve, Spurtextur und Zeitwerte sind NFVU2-Werte.
- Ergänzend das vollständige [yugecin-Repository, Revision 192090c](https://github.com/yugecin/nfsu2-re/tree/192090c564159b6a25e8dda06a8701477f7b5f1b)
  geprüft: `docs/docs.txt` führt die drei Schalter auf, `docs/dumps/hashes.txt`
  die Filterparameternamen. Keine vollständige Nitro-Filter-/Lichtspurlogik
  gefunden. Daraus werden keine unverifizierten NFS-Originalwerte übernommen.
- Unity [OnRenderImage](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html)
  und [GL.LoadOrtho](https://docs.unity3d.com/ScriptReference/GL.LoadOrtho.html):
  Kamera-Bildfilter im Built-in-Renderer. `UI/Default` ist im lokal installierten
  Valheim `unity_builtin_extra` nachgewiesen. NFVU2 verwendet eine eigene
  Materialinstanz und UV-Geometrie, verteilt keine Spielshader. Ein SRP-Wechsel
  deaktiviert den Bildschirmfilter; die Welt-Lichtspuren bleiben unabhängig.
  Die lokale D3D11-Variante zusätzlich disassembliert: `_MainTex_ST`-Transformation
  im Vertexshader, Alpha-Vormultiplikation im Pixelshader, Blend One/InvSrcAlpha.
  Die radiale Verschiebung benötigt deshalb keine Mesh-Änderung pro Bildlage.

## Weitere Quellen

- Lokale `GLOBAL/HUD_CustomTextures_00.bin` bis `_10.bin`: elf originale
  TACH_FILL_CUSTOM-Flächen, 3RDPERSON_7500LINES_CUSTOM-Skalen und
  3RDPERSON_RPMDIAL_CUSTOM-Zeiger. Padding wird für das neue Layout beschnitten.
  `GLOBAL/InGameRace.bun`: TACH_NOS_ALPHA und TACH_NOS_BACKING; die Alphavariante
  vermeidet den schwarzen Hintergrund der separaten TACH_NOS-Farbtextur.
  Import über den vorhandenen TexturePack-Decoder; Livewerte, Farbwahl,
  Positionierung und Füllmasken sind eigene NFVU2-Anpassungen.
- Lokale `CARS/350Z/GEOMETRY.BIN`: KIT00–03_TRUNK_AUDIO, KIT00/STYLE01/STYLE02
  ENGINE und ENGINE_UNLIT sowie Türverkleidungen und Haubenunterseiten.
  `CARS/AUDIO/GEOMETRY.BIN` und `CARS/TEXTURES.BIN`: Originalkomponenten und
  AUDIO_SKIN; Platzierung über MARKER_AUDIO_COMP_0..5. Baugrößen nach Passform.
- Lokale `CARS/350Z/PARTS_ANIMATIONS.bin`: ELF32-Animationskanäle und 21
  Pivotmatrizen. Acht Tracks importiert; Quellenpose und seitliche Translation
  bleiben erhalten, Wiedergabezeit ist für den Mod normiert.
- [PartAnimatorulator](https://github.com/TVasya/PartAnimatorulator): Referenz
  für Pivotreihenfolge, Quaternion-/Translationsdaten und ELF-Animationsblöcke.
  Unabhängiger, lesender Import in `tools/showroom_assets.py`; keine Verteilung
  des Werkzeugs oder der Originaldaten.
- Lokal installierte `assembly_valheim.dll` (Valheim 1.0.17): Signaturen/IL von
  `Character.Damage`, `Destructible.Damage`, `HitData.SetAttacker` und
  `BaseAI.IsEnemy` für native Fahrzeugtreffer geprüft; keine Spielbibliothek im Paket.
- [Jötunn-Prefabliste](https://valheim-modding.github.io/Jotunn/data/prefabs/prefab-list.html):
  `Beech_small1/2`, `FirTree_small/_dead` und `Bush01`, `Bush01_heath`,
  `Bush01_deepnorth`, `Bush02_en` besitzen `Destructible`; große Bäume und
  gepflanzte Setzlinge werden für Fahrzeugtreffer ausdrücklich nicht freigegeben.
- Lokale Originaldateien `LANGUAGES/English.bin`,
  `LANGUAGES/German.bin` und `LANGUAGES/Labels.bin`: zusammenhängende Liste
  der 14 Scheinwerfer-Vorlagen von STOCK/SERIE bis BRIGHT_YELLOW/GELB EXTRAHELL.
  Die englische Xenon-Zeichenfolge beginnt bei 0xABD7; der Labels-Block enthält
  STOCK, XENON, RED/BLUE/GREEN/PURPLE/YELLOW und die entsprechenden BRIGHT-Namen.
  Es werden nur Namen übernommen; RGB und Helligkeitsfaktoren sind angenähert.
- [OpenUG2-Dateiformate](https://github.com/whoismept/OpenUG2/blob/main/docs/FORMATS.md): Hintergrund für die NFSU2-Modellextraktion.
- [OpenUG2-Parser](https://github.com/whoismept/OpenUG2/blob/main/src/nfsu2.h):
  Referenz für UG2-Materialbereiche, Anbausockel und komprimierte Texturen.
- [Nikki-PresetRide](https://github.com/SpeedReflect/Nikki/blob/master/Nikki/Support.Underground2/Class/PresetRide.cs):
  Feldreihenfolge der `DDAY_PLAYER_CAR`-Voreinstellung (0x338 Byte).
- [EA-Huffman-Format](https://problemkaputt.de/psx-spx.htm#cdromfilecompressioneamethodshuffman):
  Spezifikation für die lokale Vinyl-Decodierung.
- [vgmstream r2117](https://github.com/vgmstream/vgmstream/releases/tag/r2117): unveränderter externer Audio-Decoder.
- [vgmstream-GIN-Parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/gin.c): GIN-Header und Audiocodec.
- [NFSU2CarSoundTuner](https://github.com/nlgxzef/NFSU2CarSoundTuner): rekonstruierte Motor-/Turbo-Tabellen; kein Beleg einer vollständigen Trigger-Zustandsmaschine.
- [Unlimiter CarEffect](https://github.com/nlgxzef/NFSU2Unlimiter/blob/master/CarEffect.h): getrennte Nitro-, Exhaust-Blowoff- und NOS-Blowoff-Effektarten.
- [350Z-Sweetener-Tabelle](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/SweetnerDataSet/27.ini): Sputter-/Schaltlautstärken und Bank; genaue Lastwechsel-/Upgradebedingungen bleiben offen.
- [vgmstream-ABK-Parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_abk.c): Originalbank-Substreams und Loopmetadaten.
- [XtraTrax-Importer](https://github.com/RadeonChan/XtraTrax/blob/main/patcher.py): SDAT.VIV/BIG4 und MPF/MUS-Musikcontainer.
- [vgmstream-MPF/MUS-Parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_map_mpf_mus.c): Substream-Decodierung.
- [NFSU2TraxRenamer](https://github.com/nlgxzef/NFSU2TraxRenamer): originale 27 EA-Trax-Titel; konkrete Quellrevisionen und Dateiprüfsummen stehen in `tools/music_titles.json`.
- [Nissan 350Z Werkstatthandbuch 2004, LT-109](https://manuals.plus/m/9e82ecaf8253036632c55385423e37081480f44596e1ec36936b69de0ec59c91.pdf): Rückfahr- und Blinkerleuchten in der hinteren Stoßstange, Brems-/Rücklicht oben; Originaldokument auf einem Spiegelserver.
- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/): Mod-Lader.
- [Unity WheelCollider](https://docs.unity3d.com/6000.0/Documentation/Manual/WheelColliderTutorial.html): Fahrzeugphysik.
- [Unity AudioSource](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource.html): räumliche Audiowiedergabe.
- [Unity Texture2D.SetPixelData](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D.SetPixelData.html)
  und [Apply](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D.Apply.html):
  einzelne Texturstufe hochladen und Mipmap-Kette erzeugen.

Externe Bibliotheken und Werkzeuge behalten ihre jeweiligen Lizenzen. Sie werden
hier nicht als Quellcode oder Binärdateien mitgeliefert. Die Originalspielinhalte
werden aus den lokalen Dateien des Nutzers aufbereitet.

Rust-Import in 0.9.0: [UnityPy 1.25.4](https://github.com/K0lb3/UnityPy) zum
Lesen der lokal installierten Unity-Objekte, Meshkanäle, Texturen und Audioclips.
Quellprefabs: `assets/prefabs/npc/autoturret/autoturret_deployed.prefab` und
`assets/prefabs/weapons/ak47u/ak47u.worldmodel.prefab`. Der Import übernimmt
LOD0 und Haupttexturen, keine Rust-Assemblies. WAVs werden für räumliche
Wiedergabe nach Mono gemischt. Die konkrete Assetliste und Anpassungen stehen
zusätzlich in der ignorierten `.local/turret/provenance.json`.

0.10.0 ergänzt `assets/prefabs/weapon mods/lasersight/lasersight.entity.prefab`
und den Clip `explosive-ammo-close`. Montagesitz und Strahldarstellung sind
eigene Anpassungen. Valheims Terrain-API (`TerrainComp.DoOperation`,
`Heightmap.GetAndCreateTerrainCompiler`) wurde in der lokal installierten
Assembly mit ILSpyCmd 8.2 gelesen. Die Mod nutzt deren Speichern, Gras- und
Mesh-Neuberechnung; dekompilierte Spielquellen bleiben ausschließlich in `.local/`.

Subwoofer in 0.11.0: [Unity OnAudioFilterRead](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnAudioFilterRead.html)
für die Analyse der interleavten Samples auf dem Audiothread. Der Callback
ändert keine Samples und ruft keine Unity-API auf; Basshüllkurve und
Membranverformung sind eigene NFVU2-Implementierungen.
# Rust-Logo in 0.21.0

- [Offizieller Facepunch-Rust-Pressebereich](https://facepunch.com/games/rust)
  und das dort verlinkte [Logoarchiv](https://facepunch.com/s/rust-logos.zip).
  `tools/import_rust_logo.py` importiert `Rust_Branding-1080.png`, entfernt
  ausschließlich transparenten Außenrand und verkleinert proportional.
  Originalfarben und vollständige Wort-/Bildmarke bleiben erhalten; die
  schräge Platzierung im Menü ist NFVU2-Gestaltung. © Facepunch Studios Limited.
  Download, Pixel und Prüfsummen bleiben in ignorierten lokalen Dateien.

## Auspuff und Klang in 0.21.0

- Lokales `CARS/EXHAUST/GEOMETRY.BIN`: `STYLE01_LEVEL1_A` bis
  `STYLE10_LEVEL1_A`. Endrohröffnungen werden aus zusammenhängenden originalen
  dunklen Mündungsflächen bestimmt; Montage verwendet die vorhandenen Sockets.
- [NFSU2 Car Sound Tuner, Erklärung der Originaldaten](https://github.com/nlgxzef/NFSU2CarSoundTuner#explanation):
  NFSU2 besitzt Soundzuordnungen für Leistungsstufen Stock/Street/Pro/Extreme.
  Das belegt keine frei wählbaren Auspuff-Soundpresets im Originalmenü.
  NFVU2 verwendet weiterhin seine bereits importierten Aufnahmen; „Tief“ und
  „Sport“ verändern Tonhöhe und Last-/Schubmischung als eigene Abstimmungen.

## Turbo und Druckanzeige in 0.22.0

- [350Z-Zuordnung](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarTypeMapping/350Z.ini)
  und [CarDataMapping 02](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarDataMapping/02.ini):
  Stock/Street/Pro/Extreme verwenden TurboDataSet 6/6/7/8.
- [TurboDataSet 08](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/TurboDataSet/08.ini):
  `TURBO_MED_2_MB.abk`, `TurboChargeVol=8000`, `LowBoostBovVol=24500`,
  `HighBoostBovVol=16500`, `ChargeTime=0.5`. Das sind rekonstruierte originale
  Datentabellen des Community-Werkzeugs, keine offizielle Engine-Dokumentation.
  Die Tabellen unterscheiden Low-/High-Boost-Blow-off; die genaue originale
  Trigger-, Druck- und Lautstärkekurve konnte damit nicht belegt werden.
- [vgmstream ABK-Parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_abk.c)
  und lokale Dekodierung: fünf Substreams, davon 1 und 5 geloopt. Die verwendete
  Rollenzuordnung 1=Aufladen, 2=Low, 3=High ist weiterhin eine NFVU2-Annahme.
  Der gemeinsame Importfaktor 0.8 erhält die relativen Samplepegel; die frühere
  unabhängige RMS-Normalisierung jedes Clips entfällt. Die Division der obigen
  Werte durch 32768 ist unsere Unity-Gain-Umrechnung, keine belegte Mixer-Einheit.
- Lokale HUD-Texturen `3RDPERSON_TURBOFILL_CUSTOM_XX` und zugehörige
  `TURBOLINES`: originale Grafik für alle elf Designs. Die Skala zeigt
  -30/0/+30; die Einheit ist nicht nachgewiesen und wird nicht als PSI bezeichnet.
  Lastmodell, Druckzuordnung, Unterdruck, Nadel-Flattern und dynamische Gains
  sind eigene NFVU2-Abstimmungen. Sound und Anzeige verwenden denselben Zustand;
  die Fahrzeugphysik bleibt davon unabhängig.

# Menü und Parkautomatik in 0.13.0

- Lokale NFSU2-Dateien: `FRONTEND/FrontB.lzc`,
  `LANGUAGES/Labels.bin`, `LANGUAGES/German.bin` und
  `SOUND/GLOBAL/FE_COMMON_MB.abk`. Export erfasst SHA-256, Texturschlüssel,
  Sprachschlüssel und Audio-Substreams in `.local/menu/provenance.json`.
- Icons/Titel sind Originaldaten; Layout, Menüaktionen und Soundrollen sind
  eigene Anpassungen. Die nativen Soundrollen sind noch nicht nachgewiesen.
- [Facepunch: Auto Turret](https://wiki.facepunch.com/rust/Item/autoturret):
  180° frontal für die Zielsuche. Mod verwendet ±90° relativ zur Fahrzeugfront;
  Scanbewegung, 30-m-Reichweite, Valheim-Gegnerfilter und Feuerlogik sind eigene
  Regeln, keine vollständige Reproduktion der Rust-Steuerung.

# Hummer H2 in 0.12.0

- Lokale NFSU2-CDs: `CARS/HUMMER/GEOMETRY.BIN`, `TEXTURES.BIN`,
  `CARS/WHEELS/GEOMETRY_SPINNER.BIN`, gemeinsame Reifen-/Bremsentexturen.
  `SPINNER_STYLE01_26_34_A` und separates `SPINNER_STYLE01_26_34_SPIN_A`.
- Rad-Locators aus HUMMER in `GLOBAL/GlobalB.lzc`, bei 0x120 + n*0x30
  relativ zum Modellnamen. Import prüft Achsabstand/Spurweite vor Verwendung.
- [CarTypeMapping/HUMMER](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarTypeMapping/HUMMER.ini),
  [CarDataMapping/14](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarDataMapping/14.ini),
  [EngineData/38](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/EngineData/38.ini):
  Serien-H2 verwendet `GIN_Hummer.gin`, `UseDualGinsu=0`, 1000–4782 U/min.
  Lokale GIN-Grain-Tabelle: Aufnahmebereich ca. 1266.9–4782.7 U/min.
  Unterhalb der Aufnahmegrenze werden deren Körner auf Ziel-RPM resampelt.
- Gelber Lack, Spinner-Nachlauf, Reifenbreite, akustische Schaltzeiten,
  Fahrphysik und Lichtprojektion sind NFVU2-Anpassungen. Keine vollständige
  Übernahme des originalen NFS-Fahrwerks oder Audiosystems behauptet.
