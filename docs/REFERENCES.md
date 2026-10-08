# Technische Quellen

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
