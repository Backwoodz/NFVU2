# Native Materialien fuer die Driftstrecke

Lokale Recherche am 18.10.2026. Grundlage sind die installierten Valheim-
Assemblies und das native Pieces-Bundle; keine FlatGrass-Quellen oder -APIs.
Die folgenden Zahlen sind aus Prefab-Transformhierarchien und aktivierten
BoxCollidern abgeleitet, keine optisch geschaetzten Meshabmessungen.
Spieltests mit Fahrzeugkontakt und Beleuchtung stehen noch aus.

## Fahrbahn

Empfohlen ist **Paved-Terrain auf der vorhandenen flachen Heightmap**.
`TerrainModifier.PaintType` enthaelt `Dirt`, `Cultivate`, `Paved`, `Reset`,
`ClearVegetation` und `DeepSnow`. `Paved` schreibt den blauen Kanal der nativen
Terrainmaske. Der vorhandene Terrain-Collider bleibt die Fahrflaeche; ein
Farbanstrich erzeugt keine neue Fahrphysik oder Grip-Einstellung.

`TerrainComp.DoOperation` speichert reine Maloperationen mit `Save(true)`,
fordert `Heightmap.Poke(1, true)` an und setzt Gras ueber
`ClutterSystem.ResetGrass` zurueck. Die native Heightmap-Regeneration ersetzt
bei PaintOnly nur die Materialmaske und baut den Collider nicht neu auf.
Das ist auf ebenem Grund eine geeignete Basis fuer eine durchgehende Fahrbahn
ohne hunderte einzelne Bauteil-Collider und deren Uebergaenge.

Fuer reine Fahrbahnfarbe muessen `m_level`, `m_raise` und `m_smooth` ausdruecklich
false sein. `m_paintCleared=true`, `m_paintType=Paved` und
`m_paintHeightCheck=false` erlauben die Fahrbahngestaltung ohne Hoehenbearbeitung.
Die Operation nur an vorhandenen, initialisierten TerrainComp-Instanzen im
lokalen Besitz ausfuehren und den gespeicherten Zustand respektieren.

Das native `paved_road_v2` ist **keine fertige Strassenplatte**: Es ist ein
kurzlebiges Terrainwerkzeug mit `m_smooth=true` (Radius 3 m) und Paved-Malradius
2,2 m. Unveraendertes Instanziieren ist fuer einen exakt flachen Bauplan deshalb
ungeeignet. `src/TurretTerrain.cs` zeigt bereits den vorhandenen NFVU2-Zugang
zu `TerrainComp.DoOperation`; dessen Krater-Settings duerfen nicht uebernommen
werden.

Paved-Randkonturen folgen dem nativen Terrainraster. Sehr feine Kurvenkanten
und Fahrbahnbreite muessen gerendert geprueft werden. Die native Heightmap-
Klasse verwendet standardmaessig `m_scale=1`; nicht jede Weltinstanz ist damit
allein durch die Klassenvorgabe vermessen.

## Bestaetigte Bauteile

Masse in Metern, XYZ bei unveraenderter Prefab-Ausrichtung/Skalierung.
Der Ursprung ist die Mitte der angegebenen Steinbauteile.

| Prefab | Physische Colliderhuelle XYZ | Verwendung |
| --- | --- | --- |
| `stone_floor` | 4 x 1 x 4 | Podest oder Boxengassenplattform; Oberkante Ursprung +0,5 m |
| `stone_floor_2x2` | 2 x 1 x 2 | Kleinere Plattformen, spaetere Bauteil-Vergleichsflaeche |
| `stone_wall_2x1` | 2 x 1 x 1 | Niedrige feste Streckenbegrenzung, leicht eingegraben |
| `stone_wall_4x2` | 4 x 2 x 1 | Zuschauer-/Aussenmauer; zwei BoxCollider zusammen |
| `wood_beam` | 2 x 0,4 x 0,4 | Kleine Holzrahmen; Collider-X minimal asymmetrisch |
| `wood_pole` | 0,4 x 1 x 0,4 | Kurzer Pfosten |
| `wood_pole2` | 0,4 x 2 x 0,4 | Tor-/Bannerpfosten, gegebenenfalls gestapelt |
| `sign` | 1 x 0,55 x 0,089 | Beschriftung an einem gegruendeten Holzpfosten |
| `wood_fence` | ca. 2,69 x 2,08 x 0,291 | Zaun hinter dem Auslauf, native Hoehenkontur unregelmaessig |
| `piece_banner01`, `piece_banner02` | Stange 0,2 x 0,2 x 1,41 | Haengende Tordeko, Stange entlang lokal Z |
| `piece_groundtorch` | ca. 0,233 x 1,476 x 0,237 | Kleine Lichtakzente ausserhalb des Fahrkorridors |
| `piece_groundtorch_wood` | ca. 0,119 x 1,414 x 0,125 | Holzlichtakzente ausserhalb des Fahrkorridors |

Weitere Banner `piece_banner03` bis `piece_banner11` sind im lokalen Manifest
vorhanden; ihre Einzelhierarchien wurden hier nicht vermessen.

Die sichtbare Stoffflaeche von `piece_banner01` ist erheblich groesser als der
Stangen-Collider: Das native `default`-Mesh hat lokalen AABB-Mittelpunkt
Y=-1,43917346 m und Y-Halbausdehnung 1,48703647 m. Die Stoff-Unterkante liegt
damit **2,9262 m unter dem Bannerursprung**, die Oberkante 0,0479 m darueber.
Bei Banner-Offset 1,6 m wuerde die Stoffflaeche deshalb rund 1,33 m im Boden
verschwinden. Diese Meshmessung gilt fuer `piece_banner01`; der Stoff von
`piece_banner02` wurde nicht separat vermessen.

Eine auf Hoehe 32 um 0,3 m versenkte `stone_wall_2x1` hat Ursprung 32,2 m,
Unterkante 31,7 m und Oberkante 32,7 m. Das ist ein brauchbarer Ausgangswert
fuer eine niedrige Mauer, noch keine bestaetigte Fahrzeug-Kontaktabnahme.
Bei Kurven muessen die Ecken zwischen gedrehten Segmenten und der Auslauf
mit der tatsaechlichen Fahrzeughuelle beurteilt werden.

## Native Nebenwirkungen

- Stein-/Holzteile besitzen `Piece`, persistente `ZNetView`-Daten und
  `WearNTear`. Die Originalprefabs koennen ueber die native Weltspeicherung
  bestehen bleiben. NFVU2 braucht zusaetzlich eine eigene wiederholsichere
  Zuordnung seiner erzeugten Bauteile, damit Neuladen keine Duplikate erzeugt.
- `WearNTear.m_noSupportWear=true` aktiviert entgegen einer moeglichen
  Fehlinterpretation des Namens die Stuetzwertpruefung: Ohne Support setzt
  `UpdateWear` Zerfallsschaden. Bodennahe Mauern in den Boden setzen; schwebende
  Torbalken brauchen native Stuetzung. Gemeinsame Prefabs nicht veraendern.
- Beide Fackelarten haben einen 20-m-PlayerBase-Trigger. Das ist ein nativer
  Einflussbereich und kein 20-m-Fahrhindernis. Ihre festen Collider bleiben
  trotzdem Hindernisse fuer das Auto.
- `piece_groundtorch` hat max. 6 Brennstoffeinheiten mit 20000 s je Einheit;
  die Holzfackel max. 4 mit 10000 s je Einheit. `Fireplace` speichert Brennstoff
  im ZDO. Automatischer Spawn garantiert daher keine unbegrenzte Beleuchtung.
  Fuer den ersten Fahrtest ist Tageslicht ausreichend.
- Kleine Fackel- und Dekoteile erst hinter einer freien Auslaufzone einsetzen.
  Ein nativer Licht-/Bauteiltest muss deren Sichtwirkung, Kontakt und Verhalten
  nach Speichern/Neuladen bestaetigen.
- Ein `wood_pole2` bei Bodenhoehe 32 mit Ursprung Y=32,9 gruendet 0,1 m im
  Boden. Ein `sign` bei Y=33,4 mit gleicher Ausrichtung und 0,20 m Versatz
  entlang der lokalen Z-Achse hat etwa 4 cm Colliderueberlappung zum Pfosten.
  Das erlaubt die native Stuetzpruefung ohne pauschales Abschalten von
  WearNTear. Die Schildvorderseite muss im Spiel visuell geprueft werden.
- `Sign.SetText(string)` ist public und verwendet die native Zugangspruefung,
  ClaimOwnership, Text-/Autor-/Anzeigename-ZDOs und die native Aktualisierung
  mit Sichtrechten. Dies ist der vorgesehene Initialisierungspfad. Ein direkter
  Schreibzugriff nur auf `ZDOVars.s_text` laesst Autorenfelder uninitialisiert.
- Das einfache Starttor verwendet pro Seite zwei normale `wood_pole2` bei
  Hoehenoffsets 0,9 m und 2,85 m. Der untere Pfosten gruendet 0,1 m im Terrain;
  die beiden Pfosten ueberlappen vertikal um 0,05 m. `piece_banner01` sitzt bei
  Offset 3,6 m: Sein Stangen-Collider ueberlappt den oberen Pfosten und seine
  gemessene Stoff-Unterkante bleibt rund **0,67 m ueber dem Boden**. Diese
  Anordnung verwendet native Stuetzung ohne WearNTear-Aenderung. Ein
  durchgehender breiter Holzbalken benoetigt eine eigene native
  Stabilitaetspruefung und gehoert nicht zur minimalen Materialfreigabe.

## Nachvollziehbarkeit

Der lokale `SoftRef/manifest_extended` weist alle genannten Prefabs unter
`Assets/GameElements/Pieces/` dem Bundle `c4210710` zu. Die ignorierten
Hilfsdateien `.local/inspect_drift_pieces.py`, `.local/drift-pieces.json` und
`.local/drift_piece_bounds.py` lesen die echte Hierarchie und transformieren
Collider-Ecken in den Prefabraum. `.local/inspect_banner_mesh.py` und
`.local/drift-banner-mesh.txt` belegen zusaetzlich die echte Banner-Mesh-AABB.
Die native Referenz liegt lokal in
`.local/decompiled/game/TerrainModifier.cs`, `TerrainOp.cs`, `TerrainComp.cs`,
`Heightmap.cs`, `WearNTear.cs`, `Fireplace.cs`, `Piece.cs` und `Sign.cs`.
Originalbundles und extrahierte Daten werden nicht versioniert.
