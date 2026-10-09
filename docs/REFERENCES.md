# Technische Quellen

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
