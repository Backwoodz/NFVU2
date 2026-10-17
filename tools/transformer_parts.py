"""Lossless surface partition for the standalone 350Z transformation study.

Coordinates here follow the existing Blender previews: X transverse, Y forward,
Z up. Source game geometry is never modified. Cut faces retain interpolated UVs
and normals. This is an exterior blockout, not a collision-certified mechanism.
"""
import math
import copy
from collections import defaultdict

OPTIONS = {'engine': '0', 'trunk': '0', 'exhaust': '8'}


def prepare_study_part(part):
    """Author clearance on INNER trims; never change exterior surfaces.

    The low-poly source's upper door lining hooks into the fixed cabin, the
    dashboard lip blocks windshield extraction and the full-size hatch lining
    catches the boot rim. These adapted inner skins replace those surfaces in
    this robot variant. The original export on disk remains untouched.
    """
    name = part['name']
    door = 'KITW02_DOOR_PANEL_' in name
    dash = name.startswith('350Z_BASE_A_slice5_')
    hatch = 'KIT00_TRUNK_UNDER_' in name
    cabin = name.startswith('350Z_BASE_A_slice3_')
    cargo = 'KIT00_TRUNK_AUDIO_' in name
    rear_frame = name == 'NFVU2_350Z_RearInnerShell'
    if not (door or dash or hatch or cabin or cargo or rear_frame):
        return part, None
    result = copy.deepcopy(part)
    if door:
        triangles = []
        for i in range(0, len(part['indices']), 3):
            tri = [vertex(part, j) for j in part['indices'][i:i+3]]
            for poly in split(tri, 2, .74):
                if max(v[2] for v in poly) > .7400001:
                    continue
                triangles.extend([poly[0], poly[j], poly[j+1]] for j in range(1, len(poly)-1))
        verts = [v for tri in triangles for v in tri]
        result['vertices'] = [dict(x=v[0], y=v[2], z=v[1]) for v in verts]
        result['normals'] = [dict(x=v[3], y=v[5], z=v[4]) for v in verts]
        result['uv'] = [dict(u=v[6], v=v[7]) for v in verts]
        result['indices'] = list(range(len(verts)))
        reason = 'Upper inner door lining relieved above 0.74 m for window carrier and jamb clearance'
    elif rear_frame:
        for v in result['vertices']:
            v['x'] *= .78
            v['y'] = .186+(v['y']-.186)*.68
        for n in result['normals']:
            n['x'] /= .78
            n['y'] /= .68
        reason = 'Authored rear inner frame compacted inside moving roof/header envelope (22% narrower, 32% lower)'
    elif cabin:
        for v in result['vertices']:
            v['x'] *= .78
        for n in result['normals']:
            n['x'] /= .78
        reason = 'Inner cabin frame narrowed 22% for door release and folded window carriers; outer body unchanged'
    elif cargo:
        for v in result['vertices']:
            v['y'] -= .08
            v['x'] *= .78
        for n in result['normals']:
            n['x'] /= .78
        reason = 'Inactive boot insert lowered 0.08 m and narrowed 22% for hatch and rear-quarter clearance'
    elif dash:
        for v in result['vertices']:
            v['z'] -= .20
            v['y'] -= .03
        reason = 'Hidden dashboard lip set back 0.20 m and down 0.03 m from moving windshield frame'
    else:
        for v in result['vertices']:
            v['x'] *= .90
        for n in result['normals']:
            n['x'] /= .90
        reason = 'Inner hatch lining inset 10% across; longitudinal hinge overhang and exterior hatch unchanged'
    return result, dict(source_part=name, change=reason)


def selected(part):
    return part['group'] != 'audio' and (
        not part.get('optionGroup') or
        part.get('option') == OPTIONS.get(part['optionGroup']))


def lerp(a, b, t):
    return tuple(x + (y - x) * t for x, y in zip(a, b))


def split(poly, axis, level):
    """Partition a convex polygon, including its eight vertex attributes."""
    if all(v[axis] <= level + 1e-10 for v in poly):
        return [poly]
    if all(v[axis] >= level - 1e-10 for v in poly):
        return [poly]
    halves = [[], []]
    for a, b in zip(poly, poly[1:] + poly[:1]):
        da, db = a[axis] - level, b[axis] - level
        if da <= 0:
            halves[0].append(a)
        if da >= 0:
            halves[1].append(a)
        if da * db < 0:
            point = lerp(a, b, da / (da - db))
            halves[0].append(point)
            halves[1].append(point)
    return [p for p in halves if len(p) >= 3]


def area(tri):
    a, b, c = tri
    u = [b[i] - a[i] for i in range(3)]
    v = [c[i] - a[i] for i in range(3)]
    return .5 * math.sqrt(sum(x*x for x in (
        u[1]*v[2]-u[2]*v[1], u[2]*v[0]-u[0]*v[2],
        u[0]*v[1]-u[1]*v[0])))


def vertex(part, i):
    v, n, uv = part['vertices'][i], part['normals'][i], part['uv'][i]
    return (v['x'], v['z'], v['y'], n['x'], n['z'], n['y'], uv['u'], uv['v'])


def side(x):
    return 'L' if x >= 0 else 'R'


def route(part, center):
    name = part['name']
    if part['group'] in ('wheel', 'brake'):
        return 'wheel_source'
    x, y, z = center
    s = side(x)
    motion = part.get('movingPart')
    if motion in ('leftDoor', 'rightDoor'):
        if 'KITW02_DOOR_' in name and '_A_slice4_' in name:
            return 'window_' + ('L' if motion == 'leftDoor' else 'R')
        return 'arm_' + ('L' if motion == 'leftDoor' else 'R')
    if motion == 'hood':
        return 'chest'
    if name.startswith('SPOILER'):
        return 'spoiler'
    if motion == 'trunk':
        return 'hatch'
    if 'HEADLIGHT' in name:
        return 'shoulder_' + s
    if 'BRAKELIGHT' in name:
        return 'calf_' + s
    if name.startswith('EXHAUST'):
        return 'calf_' + s
    if 'DOOR_SILL' in name:
        return 'shin_' + s
    if 'ROOF' in name:
        return 'roof'
    if part.get('optionGroup') == 'engine':
        return 'engine'
    if part.get('optionGroup') == 'trunk':
        return 'cargo_' + s
    if 'license_plate' in name:
        return 'pelvis_rear'
    if 'KITW02_BODY' in name:
        if y > 1.65 and z < .52:
            return 'bumper_center' if abs(x) < .38 else 'bumper_' + s
        if y > .70:
            return 'engine' if abs(x) < .74 else 'shoulder_' + s
        if y < -1.75:
            if abs(x) < .24:
                return 'pelvis_rear'
            if z < .42:
                return 'foot_' + s
        if y < -.65:
            return 'cargo_' + s if abs(x) < .24 else 'calf_' + s
        return 'shin_' + s
    # BASE contains the windshield, quarter glass and simplified interior.
    if name.startswith('350Z_BASE'):
        if ('slice0_' in name or 'slice1_' in name):
            return 'calf_L'  # Rear antenna and its base follow the left quarter.
        if ('slice7_' in name or 'slice8_' in name):
            return 'roof'
        if ('slice9_' in name or 'slice10_' in name):
            return 'roof'
    if name.startswith('350Z_BASE') or 'RearInnerShell' in name:
        if y < -.65:
            return 'cargo_' + s
        if y > .4:
            return 'dashboard'
        return 'cabin_' + s
    raise ValueError('Unmapped source part: '+name)


def partition(part):
    """Return routed triangle fragments and per-source-triangle audit data."""
    planes = []
    if 'KITW02_BODY' in part['name']:
        planes = [(0, -.74), (0, -.38), (0, -.24), (0, 0), (0, .24), (0, .38), (0, .74),
                  (1, -1.75), (1, -.65), (1, .70), (1, 1.65),
                  (2, .42), (2, .52)]
    elif 'BRAKELIGHT' in part['name']:
        planes = [(0, 0)]
    elif (part['name'].startswith('350Z_BASE') or 'RearInnerShell' in part['name']
          or part.get('optionGroup') == 'trunk'):
        planes = [(0, 0), (1, -.65), (1, .4)]
    groups = defaultdict(list)
    audit = []
    ids = part['indices']
    for offset in range(0, len(ids), 3):
        # Conversion swaps Y/Z, so reverse winding exactly once.
        original = [vertex(part, i) for i in ids[offset:offset+3][::-1]]
        polys = [original]
        for axis, level in planes:
            polys = [piece for poly in polys for piece in split(poly, axis, level)]
        output_area = 0.
        for poly in polys:
            for j in range(1, len(poly)-1):
                tri = [poly[0], poly[j], poly[j+1]]
                size = area(tri)
                if size < 1e-14:
                    continue
                output_area += size
                center = tuple(sum(v[a] for v in tri)/3 for a in range(3))
                groups[route(part, center)].append(tri)
        audit.append((area(original), output_area))
    return groups, audit
