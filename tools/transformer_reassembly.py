"""Exterior segmentation for study 04; stock cockpit replaced by robot chassis.

This variant deliberately omits the simplified donor interior in BOTH modes.
No exterior panel fades away, scales down, or gains a duplicate during motion.
"""
from collections import defaultdict
from transformer_parts import selected as old_selected, partition as old_partition, split, area, vertex


def rigid_glazing_group(part):
    """Route glazing and its support WITHOUT cutting any original triangle.

    The quarter-window slices contain two separate panes. Assign whole source
    triangles to left/right; reject geometry bridging the vehicle centreline.
    """
    name = part['name']
    if name.startswith('350Z_BASE_A_slice7_') or name.startswith('350Z_BASE_A_slice8_'):
        return 'windscreen'
    if name.startswith('350Z_BASE_A_slice9_') or name.startswith('350Z_BASE_A_slice10_'):
        return 'quarterglass_pair'
    if part.get('movingPart') == 'trunk' and not name.startswith('SPOILER'):
        return 'hatch'
    if 'KITW02_DOOR_' in name and '_A_slice4_' in name:
        return 'window_L' if '_LEFT_' in name else 'window_R'
    if 'SIDE_MIRROR_' in name:
        return 'mirror_L' if '_LEFT_' in name else 'mirror_R'
    return None


def selected(part):
    if not old_selected(part):
        return False
    name = part['name']
    if part.get('optionGroup') in ('engine', 'trunk'):
        return False
    if name == 'NFVU2_350Z_RearInnerShell':
        return False
    if name.startswith('350Z_BASE') and any('slice%d_' % i in name for i in range(2, 7)):
        return False
    return True


def partition(part):
    rigid = rigid_glazing_group(part)
    if rigid:
        groups, audit = defaultdict(list), []
        ids = part['indices']
        for offset in range(0, len(ids), 3):
            tri = [vertex(part, i) for i in ids[offset:offset+3][::-1]]
            group = rigid
            if rigid == 'quarterglass_pair':
                xs = [v[0] for v in tri]
                if min(xs) < -1e-6 and max(xs) > 1e-6:
                    raise ValueError('A quarter pane crosses the centreline: '+part['name'])
                group = 'quarterglass_L' if sum(xs) >= 0 else 'quarterglass_R'
            groups[group].append(tri)
            size = area(tri)
            audit.append((size, size))
        return groups, audit
    old, audit = old_partition(part)
    groups = defaultdict(list)
    name = part['name']
    for role, tris in old.items():
        planes = []
        if role == 'chest' or role in ('roof', 'hatch', 'bumper_center', 'engine'):
            planes = [(0, 0)]
        if role.startswith(('arm_', 'shin_')):
            planes = [(1, -.75), (1, 0), (1, .70)] if role.startswith('shin_') else [(1, 0)]
        if role.startswith('shin_'):
            planes += [(0, -.60), (0, .60)]
        if role == 'engine':
            planes += [(1, 1.30)]
        if role.startswith('shoulder_') and 'HEADLIGHT' not in name:
            planes = [(1, 1.25)]
        if role.startswith('calf_') and 'KITW02_BODY' in name:
            planes = [(1, -1.60), (0, -.65), (0, .65)]
        for tri in tris:
            polys = [tri]
            for axis, level in planes:
                polys = [piece for poly in polys for piece in split(poly, axis, level)]
            subtotal = 0.
            for poly in polys:
                for j in range(1, len(poly)-1):
                    fragment = [poly[0], poly[j], poly[j+1]]
                    size = area(fragment)
                    if size < 1e-14:
                        continue
                    subtotal += size
                    x, y, z = [sum(v[a] for v in fragment)/3 for a in range(3)]
                    side = 'L' if x >= 0 else 'R'
                    target = role
                    if role == 'chest': target = 'chest_'+side
                    elif role.startswith('arm_'): target = ('door_front_' if y >= 0 else 'door_rear_')+side
                    elif role.startswith('shoulder_'):
                        target = ('lamp_' if 'HEADLIGHT' in name else 'fender_front_' if y >= 1.25 else 'fender_back_')+side
                    elif role.startswith('calf_'):
                        if 'BRAKELIGHT' in name: target = 'taillamp_'+side
                        elif name.startswith('EXHAUST'): target = 'exhaust_'+side
                        elif name.startswith('350Z_BASE'): target = 'aerial'
                        else: target = ('floor2_' if y >= -1.60 and abs(x) < .65 else 'rear_arch_' if y >= -1.60 else 'rear_corner_')+side
                    elif role.startswith('shin_'):
                        index = sum(y > p for p in (-.75, 0, .70))
                        target = ('floor%d_' % (index-1) if abs(x) < .60 else 'sill%d_' % index)+side
                    elif role == 'roof':
                        if name.startswith('350Z_BASE'):
                            target = 'windscreen_'+side if any('slice%d_' % i in name for i in (7, 8)) else 'quarterglass_'+side
                        else: target = 'roof_'+side
                    elif role == 'hatch': target = 'hatch_'+side
                    elif role == 'engine': target = ('apron_front_' if y >= 1.30 else 'apron_back_')+side
                    elif role.startswith('bumper_') and role != 'bumper_center': target = 'toe_'+side
                    elif role == 'bumper_center': target = 'shinface_'+side
                    elif role.startswith('foot_'): target = 'hipguard_'+side
                    elif role.startswith('cargo_'): target = 'belt_'+side
                    groups[target].append(fragment)
            assert abs(subtotal-area(tri)) < 1e-9, (name, role)
    return groups, audit


def sources(model, prepare):
    bounds = {}
    for part in model['parts']:
        if not selected(part) or part['group'] in ('wheel', 'brake'):
            continue
        prepared, _ = prepare(part)
        groups, _ = partition(prepared)
        for role, tris in groups.items():
            points = [v for tri in tris for v in tri]
            low, high = bounds.setdefault(role, ([float('inf')]*3, [-float('inf')]*3))
            for a in range(3):
                low[a] = min(low[a], min(v[a] for v in points))
                high[a] = max(high[a], max(v[a] for v in points))
    result = {role: tuple((a+b)/2 for a, b in zip(*bound)) for role, bound in bounds.items()}
    for side, sign in [('L', 1), ('R', -1)]:
        for axle, y in [('front', 1.375), ('rear', -1.29)]:
            result[axle+'_wheel_'+side] = (sign*.8, y, .21)
    return result
