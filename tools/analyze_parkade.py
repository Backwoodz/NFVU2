"""Reproduce Parkade Drift 1 measurements from the locally owned USB discs.

Only the parser is source-controlled. Original files, hashes, decoded maps and
measured source coordinates are written below ignored .local/parkade-research.
Uses the existing ISO9660, chunk and texture readers; never modifies the discs.
"""
import hashlib
import json
import math
import statistics
import struct
import zipfile
from pathlib import Path

from project import LOCAL, disc_root
from inspect_discs import Disc
from extract_assets import DiscFile
from export_geometry import chunks, leaves, objects, unpad
from nfs_formats import TexturePack, bin_hash


OUTPUT = LOCAL / 'parkade-research'
FILES = {
    'LANGUAGES/LABELS.BIN', 'LANGUAGES/ENGLISH.BIN',
    'TRACKS/ROUTESL4RC/PATHS4301.BIN',
    'TRACKS/ROUTESL4RC/ROUTES4301F.BIN',
    'TRACKS/ROUTESL4RC/TRACKPOSMARKERSALL.BIN',
    'TRACKS/STREAML4RC.BUN', 'TRACKS/TRACKMAP4301.BIN',
}


def extract_usb():
    found, manifest = {}, []
    for image in disc_root().glob('CD*/*.bin'):
        disc = Disc(image)
        for record in disc.walk():
            if record['name'] != 'COMPRESS.ZIP':
                continue
            with zipfile.ZipFile(DiscFile(disc, record)) as archive:
                for entry in archive.infolist():
                    name = entry.filename.replace('\\', '/')
                    if name.upper() not in FILES:
                        continue
                    target = (OUTPUT / name).resolve()
                    if not target.is_relative_to(OUTPUT.resolve()):
                        raise ValueError('Unsafe archive path')
                    data = archive.read(entry)  # ZIP reader also verifies CRC.
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.write_bytes(data)
                    assert hashlib.sha256(target.read_bytes()).digest() == hashlib.sha256(data).digest()
                    found[name.upper()] = target
                    manifest.append(dict(disc=disc.label, name=name,
                                         size=len(data), zip_crc32=f'{entry.CRC:08x}',
                                         sha256=hashlib.sha256(data).hexdigest()))
    if set(found) != FILES:
        raise ValueError(f'Missing USB source files: {sorted(FILES - set(found))}')
    (OUTPUT / 'usb_manifest.json').write_text(json.dumps(manifest, indent=2))
    return found


def language(path):
    data = path.read_bytes()
    result = {}
    for tag, payload in leaves(data, 0, len(data)):
        if tag != 0x39000:
            continue
        _, count, table, strings = struct.unpack_from('<4I', payload)
        if count > 100000 or table + count * 8 > len(payload):
            raise ValueError('Invalid language table')
        for index in range(count):
            key, offset = struct.unpack_from('<II', payload, table + index * 8)
            start = strings + offset
            if start >= len(payload):
                raise ValueError('Invalid language string')
            result[key] = payload[start:].split(b'\0', 1)[0].decode('ascii', 'replace')
    return result


def route_points(path):
    """Return authored nav runs in run-ID order, not naive physical file order."""
    data = path.read_bytes()
    payloads = [b for tag, b in leaves(data, 0, len(data)) if tag == 0x34148]
    if len(payloads) != 1 or len(payloads[0]) % 24:
        raise ValueError('Expected one 24-byte navigation table')
    runs = {}
    for offset in range(0, len(payloads[0]), 24):
        x, y, run = struct.unpack_from('<2fH', payloads[0], offset)
        if not all(math.isfinite(v) and abs(v) < 10000 for v in (x, y)):
            raise ValueError('Invalid route coordinate')
        runs.setdefault(run, []).append((x, y))
    if set(runs) != {0, 1, 2, 3}:
        raise ValueError('Unexpected Parkade 1 authored run IDs')
    result = []
    for run in sorted(runs):
        points = runs[run]
        if result and math.dist(result[-1], points[0]) > 1:
            raise ValueError('Authored run join exceeds one metre')
        for point in points:
            if not result or math.dist(result[-1], point) > .8:
                result.append(point)
    if math.dist(result[-1], result[0]) > 1:
        raise ValueError('Route is not closed')
    result.pop()  # Last authored point duplicates first within half a metre.
    return result


def floor_triangles(path):
    """Read measured ROAD triangles, including each object's affine placement."""
    data = path.read_bytes()
    result = []
    for begin, end in objects(data):
        table = dict(leaves(data, begin, end))
        header = unpad(table.get(0x134011, b''))
        if b'TRN_VL_ROAD01_CH' not in header[:180]:
            continue
        vertex_bytes = unpad(table.get(0x134b01, b''))
        index_bytes = unpad(table.get(0x134b03, b''), True)
        if not vertex_bytes or len(vertex_bytes) % 24 or len(index_bytes) % 2:
            raise ValueError('Invalid road geometry')
        matrix = struct.unpack_from('<16f', header, 0x40)
        vertices = []
        for offset in range(0, len(vertex_bytes), 24):
            x, y, z = struct.unpack_from('<3f', vertex_bytes, offset)
            vertices.append(tuple(x * matrix[i] + y * matrix[4+i] + z * matrix[8+i] + matrix[12+i]
                                  for i in range(3)))
        indices = struct.unpack('<' + 'H' * (len(index_bytes)//2), index_bytes)
        for offset in range(0, len(indices)-2, 3):
            ids = indices[offset:offset+3]
            if max(ids) >= len(vertices):
                raise ValueError('Road index exceeds vertex buffer')
            a, b, c = [vertices[index] for index in ids]
            denominator = (b[1]-c[1])*(a[0]-c[0]) + (c[0]-b[0])*(a[1]-c[1])
            if abs(denominator) > 1e-6:
                result.append((a, b, c, denominator))
    return result


def height_at(triangles, point):
    x, y = point
    heights = []
    for a, b, c, denominator in triangles:
        if not (min(a[0], b[0], c[0])-.001 <= x <= max(a[0], b[0], c[0])+.001
                and min(a[1], b[1], c[1])-.001 <= y <= max(a[1], b[1], c[1])+.001):
            continue
        u = ((b[1]-c[1])*(x-c[0]) + (c[0]-b[0])*(y-c[1])) / denominator
        v = ((c[1]-a[1])*(x-c[0]) + (a[0]-c[0])*(y-c[1])) / denominator
        if u >= -.001 and v >= -.001 and u+v <= 1.001:
            heights.append(u*a[2] + v*b[2] + (1-u-v)*c[2])
    if not heights:
        raise ValueError(f'No source road surface at {point}')
    return max(heights)


def start_grid(path):
    data = path.read_bytes()
    payloads = [b for tag, b in leaves(data, 0, len(data)) if tag == 0x34146]
    if len(payloads) != 1 or (len(payloads[0])-8) % 48:
        raise ValueError('Invalid start-grid records')
    for offset in range(8, len(payloads[0]), 48):
        if struct.unpack_from('<I', payloads[0], offset+36)[0] == 4301:
            return struct.unpack_from('<3f', payloads[0], offset+16)
    raise ValueError('No start grid for event 4301')


def lateral_extents(path):
    """Preserve plausible lateral extent fields without claiming wall positions."""
    data = path.read_bytes()
    found = [(a, b) for tag, a, b in chunks(data)
             if tag == 0x34121]
    if len(found) != 1:
        raise ValueError('Expected one route table')
    begin, end = found[0]
    at = begin + 8
    result = []
    while at < end:
        count, duplicate = struct.unpack_from('<2H', data, at+44)
        if not 1 <= count <= 4096 or duplicate != count or at+128+56*count > end:
            raise ValueError('Invalid route-point record')
        for index in range(count):
            offset = at+128+56*index
            x, y = struct.unpack_from('<2f', data, offset+4)
            left, right = struct.unpack_from('<2H', data, offset+24)
            result.append(dict(x=x, y=y, raw_left=left, raw_right=right,
                               candidate_left_metres=left/256,
                               candidate_right_metres=right/256))
        at += 140+56*count
        if at == end+8:
            at = end
    if at != end:
        raise ValueError('Route points exceed leaf')
    return result


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    files = extract_usb()
    labels = language(files['LANGUAGES/LABELS.BIN'])
    english = language(files['LANGUAGES/ENGLISH.BIN'])
    key = bin_hash('TRACK_NAME_4301')
    assert key == 0x5463523B and labels[key] == 'TRACK_NAME_4301' and english[key] == 'Parkade Drift 1'
    points = route_points(files['TRACKS/ROUTESL4RC/PATHS4301.BIN'])
    triangles = floor_triangles(files['TRACKS/STREAML4RC.BUN'])
    grid = start_grid(files['TRACKS/ROUTESL4RC/TRACKPOSMARKERSALL.BIN'])
    candidates = []
    for index, a in enumerate(points):
        b = points[(index+1) % len(points)]
        dx, dy = b[0]-a[0], b[1]-a[1]
        t = max(0, min(1, ((grid[0]-a[0])*dx + (grid[1]-a[1])*dy) / (dx*dx+dy*dy)))
        point = (a[0]+t*dx, a[1]+t*dy)
        candidates.append((math.dist(point, grid[:2]), index, point))
    _, index, origin = min(candidates)
    points = [origin] + points[index+1:] + points[:index+1] + [origin]
    heights = [height_at(triangles, point) for point in points]
    extents = lateral_extents(files['TRACKS/ROUTESL4RC/ROUTES4301F.BIN'])
    (OUTPUT / 'parkade1-lateral-extents.json').write_text(json.dumps(dict(
        status='Plausible AI-line lateral extents, not verified physical course boundaries',
        candidate_total_width_median=statistics.median(
            p['candidate_left_metres']+p['candidate_right_metres'] for p in extents),
        points=extents), indent=2))
    layout = dict(event_id=4301, name=english[key], units='metres, unchanged 1:1 plan',
                  source_origin=[*origin, heights[0]], source_grid=grid,
                  start_definition='Projection of front-grid coordinate onto authored nav line; exact original start trigger unknown',
                  length=sum(math.dist(a, b) for a, b in zip(points, points[1:])),
                  source_height_bounds=[min(heights), max(heights)],
                  centerline=[dict(x=round(p[0]-origin[0], 6), z=round(p[1]-origin[1], 6),
                                   height=round(h-heights[0], 6), source_height=round(h, 6))
                              for p, h in zip(points, heights)])
    (OUTPUT / 'parkade1-layout.json').write_text(json.dumps(layout, indent=2))
    pack = TexturePack(files['TRACKS/TRACKMAP4301.BIN'])
    keys = [*pack.records, *pack.slots]
    if len(keys) != 1:
        raise ValueError('Expected one original track-map image')
    pack.decode(keys[0])[1].save(OUTPUT / 'map4301.png')
    print(f'Parkade Drift 1: event 4301, {layout["length"]:.3f} m, '
          f'{max(heights)-min(heights):.3f} m elevation change; USB hashes verified.')


if __name__ == '__main__':
    main()
