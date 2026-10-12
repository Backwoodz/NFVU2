"""Import selected cabin meshes from the user's locally extracted BeamNG 350Z.

No BeamNG code is executed or packaged. Source files stay in .local/interior-source.
The source's original author/redistribution permission is unknown; this is a local import.
"""
import argparse
import base64
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

import numpy as np
from PIL import Image
from project import LOCAL
from cabin_fit import fit_headliner, roof_triangles, fit_windscreen, windscreen_triangles, REAR_LIMIT

NS = {'c': 'http://www.collada.org/2005/11/COLLADASchema'}
SELECTED = {
    'Nis350Z_interior', 'Nis350Z_dash', 'Nis350Z_seat_FL', 'Nis350Z_seat_FR',
    'Nis350Z_steer', 'Nis350Z_doorpanel_L', 'Nis350Z_doorpanel_R',
    'Nis350Z_gaspedal', 'Nis350Z_brakepedal', 'Nis350Z_clutchpedal',
    'Nis350Z_shifter_M_base', 'Nis350Z_shifter_M_boot', 'Nis350Z_shifter_M_knob',
    'Nis350Z_seatbeltdfs_L', 'Nis350Z_seatbeltdfs_R',
}
# BeamNG front=-Y, left=+X, up=Z. Fit the roof lining to NFS's 1.21 m roof.
FIT = np.array([[-1., 0, 0, 0], [0, 0, 1., -.13], [0, -1., 0, .06], [0, 0, 0, 1]])


def vectors(values, keys='xyz'):
    return [dict(zip(keys, map(float, row))) for row in np.round(values, 6)]


def scene_nodes(parent, matrix=None):
    matrix = np.eye(4) if matrix is None else matrix
    for node in parent.findall('c:node', NS):
        local = node.find('c:matrix', NS)
        transform = matrix @ (np.fromstring(local.text, sep=' ').reshape(4, 4) if local is not None else np.eye(4))
        yield node, transform
        yield from scene_nodes(node, transform)


def read_materials(path):
    # This mod's JSON contains trailing commas; don't strip // inside paths.
    text = path.read_text(encoding='utf-8-sig')
    text = re.sub(r',\s*([}\]])', r'\1', text)
    return {v.get('mapTo', k): v for k, v in json.loads(text).items()}


def geometry_parts(geometry, transform, name, material_id, roof=None, windscreen=None):
    mesh = geometry.find('c:mesh', NS)
    sources = {}
    for source in mesh.findall('c:source', NS):
        accessor = source.find('c:technique_common/c:accessor', NS)
        stride = int(accessor.get('stride', '1'))
        sources[source.get('id')] = np.fromstring(source.find('c:float_array', NS).text, sep=' ').reshape(-1, stride)
    vertices = {v.get('id'): v.find('c:input', NS).get('source')[1:] for v in mesh.findall('c:vertices', NS)}
    normal_matrix = np.linalg.inv(transform[:3, :3]).T
    for number, primitive in enumerate(list(mesh)):
        kind = primitive.tag.split('}')[-1]
        if kind not in ('triangles', 'polylist'): continue
        inputs = primitive.findall('c:input', NS)
        stride = max(int(i.get('offset')) for i in inputs) + 1
        raw = np.fromstring(primitive.find('c:p', NS).text, sep=' ', dtype=int).reshape(-1, stride)
        if kind == 'polylist':
            counts = np.fromstring(primitive.find('c:vcount', NS).text, sep=' ', dtype=int)
            triangles = []; start = 0
            for count in counts:
                for j in range(1, count - 1): triangles.extend([raw[start], raw[start+j], raw[start+j+1]])
                start += count
            raw = np.array(triangles)
        if not len(raw): continue
        unique, inverse = np.unique(raw, axis=0, return_inverse=True)
        attributes = {}
        for inp in inputs:
            semantic = inp.get('semantic'); source = inp.get('source')[1:]
            if semantic == 'VERTEX': source = vertices[source]
            if semantic == 'TEXCOORD' and inp.get('set', '0') != '0': continue
            attributes[semantic] = sources[source][unique[:, int(inp.get('offset'))]]
        positions = attributes['VERTEX'][:, :3]
        positions = (transform @ np.column_stack([positions, np.ones(len(positions))]).T).T[:, :3]
        normals = (normal_matrix @ attributes['NORMAL'][:, :3].T).T
        normals /= np.maximum(np.linalg.norm(normals, axis=1)[:, None], 1e-8)
        indices = inverse.reshape(-1, 3)
        if np.linalg.det(transform[:3, :3]) < 0: indices = indices[:, ::-1]
        original_positions = positions
        if name == 'Nis350Z_interior':
            # Retain full-height pillars and lining. Only the donor luggage bay
            # and roof behind the fixed NFS header conflict with our moving hatch.
            triangles = positions[indices]
            indices = indices[(triangles[:, :, 2].min(axis=1) > -.9) &
                              ((triangles[:, :, 1].max(axis=1) < 1.0) |
                               (triangles[:, :, 2].min(axis=1) > REAR_LIMIT))]
            if not len(indices): continue
            if roof is not None:
                positions = fit_headliner(positions, roof)
        if windscreen is not None and name in ('Nis350Z_interior', 'Nis350Z_dash'):
            positions = fit_windscreen(positions, windscreen)
            if name == 'Nis350Z_interior' and roof is not None:
                # Moving the upper seal rearward changes the roof point above it.
                positions = fit_headliner(positions, roof)
        if not np.array_equal(positions, original_positions):
            # Rebuild only normals adjacent to fitted faces; keep authored
            # shading on the untouched floor, trim and dashboard controls.
            changed = np.any(positions != original_positions, axis=1)
            affected = np.unique(indices[np.any(changed[indices], axis=1)])
            rebuilt = np.zeros_like(positions)
            faces = positions[indices]
            face_normals = np.cross(faces[:, 1]-faces[:, 0], faces[:, 2]-faces[:, 0])
            for corner in range(3): np.add.at(rebuilt, indices[:, corner], face_normals)
            length = np.linalg.norm(rebuilt, axis=1)
            affected = affected[length[affected] > 1e-8]
            normals[affected] = rebuilt[affected]/length[affected, None]
        used, compact = np.unique(indices, return_inverse=True)
        positions = positions[used]; normals = normals[used]
        texcoords = attributes.get('TEXCOORD', np.zeros((len(unique), 2)))[used, :2]
        indices = compact.reshape(-1, 3)
        if len(positions) > 65535: raise ValueError('Split oversized cabin mesh: '+name)
        part = dict(name=f'Cabin_{name}_{number}', group='body', material=material_id(primitive.get('material')),
                    vertices=vectors(positions), normals=vectors(normals),
                    uv=vectors(texcoords, 'uv'),
                    indices=indices.flatten().tolist())
        if name == 'Nis350Z_doorpanel_L': part['movingPart'] = 'rightDoor'  # NFS naming is mirrored.
        if name == 'Nis350Z_doorpanel_R': part['movingPart'] = 'leftDoor'
        yield part


def export(source, output, body_path=LOCAL/'model/body.json'):
    output.mkdir(parents=True, exist_ok=True)
    vehicle = source / 'vehicles/Nis350Z'
    root = ET.parse(vehicle / 'Nis350Z.dae').getroot()
    geometries = {g.get('id'): g for g in root.findall('c:library_geometries/c:geometry', NS)}
    definitions = read_materials(vehicle / 'main.materials.json')
    body = json.loads(body_path.read_text(encoding='utf-8'))
    roof = roof_triangles(body); windscreen = windscreen_triangles(body)
    materials = []; textures = []; material_ids = {}; texture_ids = {}; missing = []
    def material_id(symbol):
        name = symbol.removesuffix('-material')
        if name in material_ids: return material_ids[name]
        data = definitions.get(name, {})
        stage = data.get('Stages', [{}])[0]
        color = stage.get('baseColorFactor', stage.get('diffuseColor', [1, 1, 1, 1]))[:4]
        if len(color) != 4: color = [1, 1, 1, 1]
        color = [max(0, min(1, float(c))) for c in color]
        texture = -1
        filename = stage.get('baseColorMap', stage.get('colorMap', stage.get('diffuseMap', '')))
        path = (source / filename.lstrip('/')).resolve()
        if filename and path.is_relative_to(source.resolve()) and path.is_file():
            if filename not in texture_ids:
                im = Image.open(path).convert('RGBA'); im.thumbnail((512, 512), Image.Resampling.LANCZOS)
                # Opaque cabin surfaces; source opacity maps are separate BeamNG layers.
                im.putalpha(255)
                texname = 'cabin_'+str(len(textures)); im.save(output / (texname+'.png'))
                texture_ids[filename] = len(textures)
                textures.append(dict(name=texname, width=im.width, height=im.height,
                                     rgba=base64.b64encode(im.tobytes()).decode('ascii')))
            texture = texture_ids[filename]
        else:
            if filename: missing.append({'material': name, 'reference': filename})
            # BeamNG base-game materials have no usable diffuse in this archive.
            if name.startswith('etk') or 'carbonfibre' in name or filename.startswith('@'): color = [.055, .06, .065, 1]
        index = len(materials); material_ids[name] = index
        materials.append(dict(name='cabin_'+name, color=color, texture=texture,
                              metallic=max(0, min(1, float(stage.get('metallicFactor', .05)))),
                              smoothness=max(0, min(.8, 1-float(stage.get('roughnessFactor', .65))))))
        return index
    parts = []; found = set(); steering = None
    scene = root.find('c:library_visual_scenes/c:visual_scene', NS)
    for node, matrix in scene_nodes(scene):
        name = node.get('name')
        if name not in SELECTED: continue
        found.add(name)
        geometry = geometries[node.find('c:instance_geometry', NS).get('url')[1:]]
        parts.extend(geometry_parts(geometry, FIT @ matrix, name, material_id, roof, windscreen))
        if name == 'Nis350Z_steer':
            steering = {'pivot': vectors([(FIT @ matrix)[:3, 3]])[0],
                        'axis': vectors([(FIT @ matrix)[:3, 2]])[0]}
    if found != SELECTED: raise ValueError('Missing cabin parts: '+str(SELECTED-found))
    model = dict(schemaVersion=2, preset='LOCAL_BEAMNG_350Z_CABIN', parts=parts, materials=materials, textures=textures)
    target = output/'interior.json'; target.write_text(json.dumps(model, separators=(',', ':')), encoding='utf-8')
    if target.stat().st_size > 32*1024*1024: raise ValueError('Cabin exceeds runtime model size limit')
    report = dict(source='User supplied Nissan 350Z FIX UKBEAMNG archive', originalAuthor='Unknown',
                  sourceUrl='https://www.modland.net/beamng.drive-mods/cars/nissan-350z-fix-ukbeamng.html',
                  transform=FIT.tolist(), selected=sorted(found), parts=len(parts),
                  triangles=sum(len(p['indices'])//3 for p in parts), steering=steering,
                  substitutedMaterials=missing,
                  shellFit='Full A pillars and roof lining fitted below the NFS roof; dashboard/pillar fronts fitted behind NFS windshield and seal; donor rear bay/roof omitted at animated hatch')
    # Provenance/review data stays next to the source, outside the mod payload.
    (source.parent/'interior-import-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f'Cabin: {len(parts)} meshes, {report["triangles"]} triangles, {len(textures)} textures, {target.stat().st_size/1048576:.1f} MiB')
    print('Steering:', steering, 'Fallback materials:', missing)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, default=LOCAL/'interior-source/beamng-350z')
    parser.add_argument('--output', type=Path, default=LOCAL/'interior')
    parser.add_argument('--body', type=Path, default=LOCAL/'model/body.json')
    args = parser.parse_args(); export(args.source, args.output, args.body)
