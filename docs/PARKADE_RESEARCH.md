# Parkade Drift 1: local source analysis

The local retail PC disc images were read directly from the configured removable
drive. They contain ISO9660 files in raw 2352-byte sectors; each disc's
`COMPRESS.ZIP` contains the original installed game files. The parser reads the
archives without changing the discs. Extracted files, CRC values, SHA-256 hashes,
decoded artwork and measured source coordinates remain in ignored
`.local/parkade-research/`.

Reproduce with `python tools/analyze_parkade.py`. The script checks ZIP CRCs,
verifies the extracted copies' SHA-256 hashes and rejects missing or structurally
unexpected source tables. It needs the user's disc configuration and the
existing texture reader's Pillow dependency. It is an offline data inspection,
not a test of the original game's race controller or Valheim physics.

## Identity and route

The relationship is established from two original language tables:

| File | Hash | Value |
| --- | --- | --- |
| `LANGUAGES/Labels.bin` | `5463523B` | `TRACK_NAME_4301` |
| `LANGUAGES/English.bin` | `5463523B` | `Parkade Drift 1` |

The hash also matches the existing game-format string hash function. The track's
original map is `TRACKS/TRACKMAP4301.BIN`; its navigation data is
`TRACKS/ROUTESL4RC/Paths4301.bin`. This is a measured match, independent of the
user's initial list of venues. The same tables actually contain six numbered
Parkade Drift names; only the first was studied for implementation.

The navigation leaf `0x34148` holds 43 records of 24 bytes. Each contains two
horizontal coordinates and an authored run ID. The physical file order is not
the complete driving order: run IDs 0, 1, 2 and 3 connect with sub-metre duplicated
endpoints. Sorting those runs, validating their joins, and removing duplicated
seam points produces a closed route of **503.381 metres**. This is the length of
the measured navigation polyline; the exact length of the game's smoothed
vehicle trajectory is not claimed.

The centreline footprint is about **116.40 by 111.92 metres** before adding road
width and scenery. Its defining features are a northern straight, a broad right
turn onto the eastern outer straight, a tight southern return, an inner upward
hairpin, a southwest bend, and the western return to start. The decoded original
track-map artwork was inspected as an independent visual cross-check.

## Starting position

`TrackPosMarkersAll.bin`, leaf `0x34146`, contains 48-byte records explicitly
referencing event 4301. The front grid and the row behind it establish an eastward
start direction. The adaptation projects the front grid position onto the
navigation line and uses that projection as its coordinate origin. The exact
original start/finish trigger has not been decoded; the projected position must
not be described as the original trigger.

The output `parkade1-layout.json` contains ordered horizontal coordinates,
closure, source heights, the grid reference, and this limitation. World placement
in Valheim is a separate translation; horizontal source scale is retained 1:1.

## Width and height evidence

`Routes4301F.bin`, leaf `0x34121`, contains four route runs with 36, 5, 8 and 7
points. Each point has two unsigned 16-bit fields which, divided by 256, are
consistent with asymmetric lateral clearances. Their summed median is
**18.678 metres**. Some individual values clamp to one metre and the paths do not
coincide exactly with the navigation line. These are useful scale evidence, not
a proven pair of physical wall boundaries. The diagnostic preserves those raw
fields in `parkade1-lateral-extents.json`; a uniform adaptation width remains a
design choice.

The actual `STREAML4RC.BUN` road meshes, including their authored affine
transforms, were also inspected. Barycentric interpolation of the
`TRN_VL_ROAD01_CH` triangles at all course points gives **21.913 to 29.013 metres**
of source elevation: approximately **7.10 metres of gradual height variation**.
The northern and western sections lie near the upper level; the outer eastern
straight descends and the inner return climbs. The implemented Valheim height
field retains these sampled relative heights along the measured navigation
line. Its upper/start level is raised to 40 metres, leaving the lowest road at
approximately 32.90 metres, above Valheim's water. The 18-metre-wide road blends
into the surrounding 32-metre terrain outside the venue. This reproduces the
sampled longitudinal grade, not the original road mesh's full crossfall or every
triangle. Native world terrain generation, driving and saved-world acceptance
remain pending.

No original building, wall or roof placement has been fully reconstructed.
Stone barriers, columns, roof framing, lighting and signs in Valheim are the
mod's own venue adaptation. The navigation, map and sampled road geometry alone
do not prove a faithful reconstruction of an enclosed multi-storey car park.
