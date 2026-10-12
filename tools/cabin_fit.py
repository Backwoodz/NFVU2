"""Preserve the NFS structural shell and fit the optional cabin to its roof."""
import numpy as np

BASE_SHELL = '350Z_BASE_A_slice3_3'
REAR_SHELL = 'NFVU2_350Z_RearInnerShell'
REAR_LIMIT = -.72
ROOF_CLEARANCE = .018
WINDSCREEN_CLEARANCE = .02


def split_rear_shell(parts):
    """Keep rear body structure visible when the simplified cockpit is hidden.

    Partition the original triangles, rather than overlaying another copy.
    Without the optional cabin both partitions still form the original mesh.
    """
    original = next(p for p in parts if p['name'] == BASE_SHELL)
    rear, cabin = [], []
    for i in range(0, len(original['indices']), 3):
        tri = original['indices'][i:i+3]
        target = rear if all(original['vertices'][j]['z'] < REAR_LIMIT for j in tri) else cabin
        target.extend(tri)
    if not rear or not cabin:
        raise ValueError('Re-identify the 350Z rear inner shell')
    parts.append({**original, 'name': REAR_SHELL, 'indices': rear})
    original['indices'] = cabin


def roof_triangles(model):
    result = []
    for p in model['parts']:
        if 'KIT00_ROOF_' in p['name'] and model['materials'][p['material']].get('bodyPaint'):
            v = np.array([[q[k] for k in 'xyz'] for q in p['vertices']])
            result.extend(v[np.array(p['indices']).reshape(-1, 3)])
    if not result:
        raise ValueError('The fitted cabin requires the exported 350Z roof')
    return np.array(result)


def roof_heights(points, roof, margin=0.):
    """Top surface at X/Z; optionally extend its nearest facet just past the rim."""
    heights = np.full(len(points), -np.inf)
    nearest = np.full(len(points), np.inf)
    extended = np.full(len(points), np.nan)
    for a, b, c in roof:
        ab, ac = b-a, c-a
        determinant = ab[0]*ac[2]-ab[2]*ac[0]
        if abs(determinant) < 1e-10:
            continue
        d = points-a
        u = (d[:, 0]*ac[2]-d[:, 2]*ac[0])/determinant
        v = (ab[0]*d[:, 2]-ab[2]*d[:, 0])/determinant
        inside = (u >= -1e-6) & (v >= -1e-6) & (u+v <= 1+1e-6)
        plane = a[1]+u*ab[1]+v*ac[1]
        heights[inside] = np.maximum(heights[inside], plane[inside])
        if margin > 0:
            distance = np.full(len(points), np.inf)
            for start, end in ((a, b), (b, c), (c, a)):
                edge = (end-start)[[0, 2]]
                delta = (points-start)[:, [0, 2]]
                t = np.clip(delta@edge/np.dot(edge, edge), 0, 1)
                distance = np.minimum(distance, np.linalg.norm(delta-t[:, None]*edge, axis=1))
            closer = distance < nearest
            nearest[closer] = distance[closer]; extended[closer] = plane[closer]
    if margin > 0:
        border = ~np.isfinite(heights) & (nearest <= margin)
        heights[border] = extended[border]
    heights[~np.isfinite(heights)] = np.nan
    return heights


def fit_headliner(positions, roof):
    """Lower the lining where the donor roof protrudes through the NFS roof.

    Preserve the lower A pillars, floor and cockpit. The rear roof is excluded
    by the importer because the NFS hatch is a separately animated part.
    """
    result = positions.copy()
    height = roof_heights(positions, roof)
    fitted = (positions[:, 1] > .95) & np.isfinite(height)
    result[fitted, 1] = np.minimum(positions[fitted, 1], height[fitted]-ROOF_CLEARANCE)
    return result


def windscreen_triangles(model):
    # Include the seal so the donor dashboard cannot poke through its lower edge.
    parts = [p for p in model['parts'] if p['name'] in
             ('350Z_BASE_A_slice7_7', '350Z_BASE_A_slice8_8')]
    if len(parts) != 2:
        raise ValueError('Re-identify the 350Z windscreen and seal')
    return np.concatenate([np.array([[v[k] for k in 'xyz'] for v in p['vertices']])[
        np.array(p['indices']).reshape(-1, 3)] for p in parts])


def fit_windscreen(positions, windscreen):
    # Front is +Z. Query the actual curved screen in X/Y, then move only points
    # on/in front of it rearward. Seats, steering and door pivots stay unchanged.
    result = positions.copy()
    # Fit vertices just beyond the rim too: their connecting triangles can
    # cross the glass even though the endpoints themselves miss its footprint.
    front = roof_heights(positions[:, [0, 2, 1]], windscreen[:, :, [0, 2, 1]], margin=.05)
    covered = np.isfinite(front)
    result[covered, 2] = np.minimum(positions[covered, 2], front[covered]-WINDSCREEN_CLEARANCE)
    return result
