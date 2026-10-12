"""NFVU2-authored inner finish fitted to the original STYLE07 carbon hood.

The outer skin and hinge stay original. This is a visual adaptation, not a
claim to reproduce Nissan's factory reinforcement or NFS's hidden structure.
"""
import numpy as np
from cabin_fit import roof_heights


def dress_hood(parts, materials):
    outer = next(p for p in parts if p['name'].startswith('350Z_STYLE07_HOOD_')
                 and materials[p['material']]['name'].startswith('d6d6080a_'))
    under = next(p for p in parts if 'KIT00_HOOD_UNDER_' in p['name'])
    points = np.array([[v[k] for k in 'xyz'] for v in outer['vertices']])
    normals = np.array([[v[k] for k in 'xyz'] for v in outer['normals']])
    triangles = points[np.array(outer['indices']).reshape(-1, 3)]

    def material(name, color, metallic, smoothness):
        index = len(materials)
        materials.append(dict(name=name, color=[*color, 1.], metallic=metallic,
                              smoothness=smoothness, texture=-1, bodyPaint=False))
        return index

    skin = material('hood_inner_graphite', [.065, .072, .08], .35, .3)
    frame = material('hood_inner_reinforcement', [.10, .115, .13], .3, .2)
    felt = material('hood_inner_insulation', [.023, .027, .03], 0., .08)
    clips = material('hood_inner_fasteners', [.22, .23, .24], .8, .42)
    # The old stock underside sat far below this vented hood's actual surface.
    # Fit the inner skin to STYLE07, preserving the original vent openings.
    under['vertices'] = [dict(zip('xyz', map(float, v))) for v in points-normals*.006]
    under['normals'] = [dict(zip('xyz', map(float, -v))) for v in normals]
    under['uv'] = [dict(v) for v in outer['uv']]
    under['indices'] = np.array(outer['indices']).reshape(-1, 3)[:, ::-1].flatten().tolist()
    under['material'] = skin

    def mesh(name, mat):
        part = dict(name=name, group='body', movingPart='hood', material=mat,
                    vertices=[], normals=[], uv=[], indices=[])
        parts.append(part)
        return part

    def surface(xz, depth):
        p = np.column_stack([xz[:, 0], np.zeros(len(xz)), xz[:, 1]])
        y = roof_heights(p, triangles)
        if not np.all(np.isfinite(y)):
            raise ValueError('Hood detail leaves original skin: '+str(xz[~np.isfinite(y)]))
        p[:, 1] = y-depth
        return p

    def face(part, vertices):
        if len(vertices) == 4:
            face(part, vertices[[0, 1, 2]])
            face(part, vertices[[0, 2, 3]])
            return
        normal = np.cross(vertices[1]-vertices[0], vertices[2]-vertices[0])
        length = np.linalg.norm(normal)
        if length < 1e-10: return
        normal /= length
        start = len(part['vertices'])
        for v in vertices:
            part['vertices'].append(dict(zip('xyz', map(float, v))))
            part['normals'].append(dict(zip('xyz', map(float, normal))))
            part['uv'].append(dict(u=float(v[0]), v=float(v[2])))
        part['indices'].extend([start, start+1, start+2])

    def strip(part, path, width=.03, depth=.017):
        for a, b in zip(path, path[1:]):
            a, b = np.array(a), np.array(b)
            direction = b-a; length = np.linalg.norm(direction)
            side = np.array([-direction[1], direction[0]])/length*width/2
            divisions = max(1, int(np.ceil(length/.018)))
            for j in range(divisions):
                p, q = a+direction*j/divisions, a+direction*(j+1)/divisions
                outline = np.array([p-side, q-side, q+side, p+side])
                # Profile has a broad base and a narrower raised crown.
                crown = surface(np.array([p-side*.55, q-side*.55, q+side*.55, p+side*.55]), depth)
                base = surface(outline, .010)
                face(part, crown)
                for k in (0, 1, 2, 3):
                    n = (k+1)%4
                    face(part, np.array([base[k], base[n], crown[n], crown[k]]))

    ribs = mesh('NFVU2_HoodInnerFrame', frame)
    # Stay between the vent banks; do not bridge or plug the original louvers.
    perimeter = [(-.41, .98), (-.425, 1.38), (-.30, 1.82), (0, 1.9),
                 (.30, 1.82), (.425, 1.38), (.41, .98), (0, .96), (-.41, .98)]
    strip(ribs, perimeter, .028, .016)
    strip(ribs, [(0, .97), (0, 1.9)], .034)
    strip(ribs, [(-.42, 1.4), (0, 1.4), (.42, 1.4)], .034)
    strip(ribs, [(-.30, 1.8), (0, 1.85), (.30, 1.8)], .03)

    pads = mesh('NFVU2_HoodInnerPads', felt)
    fasteners = mesh('NFVU2_HoodInnerClips', clips)
    for side in (-1, 1):
        for x0, x1, z0, z1 in ((.075, .375, 1.02, 1.32), (.075, .275, 1.48, 1.75)):
            # Small cells follow the hood curvature; the pads stay inside its skin.
            nx, nz = 8, 9
            for x in range(nx):
                for z in range(nz):
                    xa, xb = x0+(x1-x0)*x/nx, x0+(x1-x0)*(x+1)/nx
                    za, zb = z0+(z1-z0)*z/nz, z0+(z1-z0)*(z+1)/nz
                    face(pads, surface(np.array([(side*xa, za), (side*xb, za), (side*xb, zb), (side*xa, zb)]), .012))
            for x, z in ((x0+.02, z0+.02), (x1-.02, z1-.02)):
                angles = np.linspace(0, 2*np.pi, 13)[:-1]
                rim = surface(np.column_stack([side*x+.007*np.cos(angles), z+.007*np.sin(angles)]), .015)
                center = surface(np.array([[side*x, z]]), .016)[0]
                for k in range(12): face(fasteners, np.array([center, rim[k], rim[(k+1)%12]]))

    # The tuned engine covers have different high spots. Locally flatten the
    # profiles where necessary rather than pushing them into a closed engine bay.
    engine = np.concatenate([
        np.array([[v[k] for k in 'xyz'] for v in p['vertices']])[np.array(p['indices']).reshape(-1, 3)]
        for p in parts if p.get('optionGroup') == 'engine'])
    for part in (ribs, pads, fasteners):
        vertices = np.array([[v[k] for k in 'xyz'] for v in part['vertices']])
        top = roof_heights(vertices, engine)
        covered = np.isfinite(top)
        vertices[covered, 1] = np.maximum(vertices[covered, 1], top[covered]+.006)
        ids = np.array(part['indices']).reshape(-1, 3)
        # Preserve the triangle winding while updating its geometric normals.
        part['vertices'] = []; part['normals'] = []; part['uv'] = []; part['indices'] = []
        for tri in ids: face(part, vertices[tri])
