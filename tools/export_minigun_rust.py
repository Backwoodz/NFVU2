"""Export the installed Rust Minigun's original deployed model and audio.

Inputs are read-only local UnityFS slices. Output stays ignored in .local/minigun.
Geometry uses original skin bind matrices and bone transforms, split along rigid
barrel weights. No Rust game code, bundles or invented sound assets are copied.
"""
import argparse
import base64
import hashlib
import io
import json
from pathlib import Path
import wave

import numpy as np

from project import LOCAL
from rust_bundle import load_bundles
from UnityPy.helpers.MeshHelper import MeshHandler
from export_rust_turret import matrix, xyz, mono_wav


PREFAB = 'assets/prefabs/weapons/minigun/minigun.entity.prefab'
SOUNDS = {
    'minigun-motor-start': 'spin_start.wav',
    'minigun-motor-loop-pitched': 'spin_loop.wav',
    'minigun-motor-loop-unpitched': 'spin_unpitched_loop.wav',
    'minigun-motor-release': 'spin_release.wav',
    'minigun-motor-stop': 'spin_stop.wav',
    'minigun-gunshot-body-start': 'fire_start.wav',
    'minigun-gunshot-body-loop': 'fire_loop.wav',
    'minigun-gunshot-body-tail': 'fire_tail.wav',
    'minigun-gunshot-mech-loop': 'mech_loop.wav',
    'minigun-gunshot-mech-tail': 'mech_tail.wav',
    'minigun-gunshot-LFE-start': 'lfe_start.wav',
    'minigun-gunshot-LFE-loop': 'lfe_loop.wav',
    'minigun-gunshot-LFE-tail': 'lfe_tail.wav',
    'minigun-reload': 'reload.wav',
    'minigun-deploy': 'deploy.wav',
}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def object_source(obj):
    return dict(serialized_file=obj.assets_file.name, path_id=obj.path_id,
                object_type=obj.type.name, serialized_object_sha256=digest(obj.get_raw_data()))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, default=LOCAL / 'minigun')
    parser.add_argument('--texture-size', type=int, choices=[256, 512, 1024], default=1024)
    args = parser.parse_args()
    out = args.output_dir
    out.mkdir(parents=True, exist_ok=True)
    print('Loading original Rust object indexes; payloads are read on demand', flush=True)
    env = load_bundles(['assetscenes.bundle', 'content.bundle', 'audio.bundle',
                        *[f'textures.{i}.bundle' for i in range(5)]])

    def resolve(file, ptr):
        if not ptr['m_PathID']:
            return None
        target = (file if ptr['m_FileID'] == 0 else
                  env.cabs[file.externals[ptr['m_FileID'] - 1].path.split('/')[-1].lower()])
        return target.objects[ptr['m_PathID']]

    scene = env.files['BuildPlayer-AssetScene-prefabs']
    root = next(obj for obj in scene.objects.values()
                if obj.type.name == 'GameObject' and obj.peek_name() == PREFAB)
    rows = []
    bone_world = {}

    def walk(go, parent):
        data = go.parse_as_dict()
        components = [(resolve(go.assets_file, item['component'])) for item in data['m_Component']]
        transform_obj = next(item for item in components if item.type.name == 'Transform')
        transform = transform_obj.parse_as_dict()
        world = parent @ matrix(transform)
        bone_world[(transform_obj.assets_file.name, transform_obj.path_id)] = (data['m_Name'], world)
        rows.append((data['m_Name'], components, world))
        for ptr in transform['m_Children']:
            child = resolve(go.assets_file, ptr)
            walk(resolve(child.assets_file, child.parse_as_dict()['m_GameObject']), world)

    walk(root, np.eye(4))
    selected = next(row for row in rows if row[0] == 'Minigun_LOD0')
    renderer = next(obj for obj in selected[1] if obj.type.name == 'SkinnedMeshRenderer')
    renderer_data = renderer.parse_as_dict()
    mesh_obj = resolve(renderer.assets_file, renderer_data['m_Mesh'])
    mesh = mesh_obj.parse_as_object()
    mesh_dict = mesh_obj.parse_as_dict()
    handler = MeshHandler(mesh)
    handler.process()
    vertices = np.asarray(handler.m_Vertices, dtype=float)
    normals = np.asarray(handler.m_Normals, dtype=float)[:, :3]
    bone_indices = np.asarray(handler.m_BoneIndices, dtype=int)
    if handler.m_BoneWeights is None and bone_indices.ndim == 2 and bone_indices.shape[1] == 1:
        # Unity's optimized rigid skin stream stores one integer bone index and
        # omits redundant blend weights: exactly one influence has unit weight.
        bone_weights = np.ones_like(bone_indices, dtype=float)
        weight_encoding = 'one bone index per vertex; implicit unit influence (blend-weight channel absent)'
    else:
        bone_weights = np.asarray(handler.m_BoneWeights, dtype=float)
        weight_encoding = 'explicit per-vertex bone indices and blend weights'
    if bone_weights.shape != bone_indices.shape or not np.allclose(np.sum(bone_weights, axis=1), 1):
        raise ValueError('Unsupported or non-normalized original skin weights')
    bones = [resolve(renderer.assets_file, ptr) for ptr in renderer_data['m_Bones']]
    bone_rows = [bone_world[(obj.assets_file.name, obj.path_id)] for obj in bones]
    barrel_bone = next(i for i, (name, _) in enumerate(bone_rows) if name == 'w_minigun_barrel_joint')
    bind_poses = [np.asarray([[p[f'e{row}{column}'] for column in range(4)]
                             for row in range(4)], dtype=float) for p in mesh_dict['m_BindPose']]
    posed_vertices = np.zeros_like(vertices)
    posed_normals = np.zeros_like(normals)
    homogeneous = np.column_stack([vertices, np.ones(len(vertices))])
    barrel_weight = np.zeros(len(vertices))
    for channel in range(bone_indices.shape[1]):
        for bone_index, (_, world) in enumerate(bone_rows):
            mask = (bone_indices[:, channel] == bone_index) & (bone_weights[:, channel] > 0)
            if not np.any(mask):
                continue
            skin = world @ bind_poses[bone_index]
            weight = bone_weights[mask, channel, None]
            posed_vertices[mask] += (homogeneous[mask] @ skin.T)[:, :3] * weight
            posed_normals[mask] += (normals[mask] @ np.linalg.inv(skin[:3, :3])) * weight
            if bone_index == barrel_bone:
                barrel_weight[mask] += bone_weights[mask, channel]
    posed_normals /= np.maximum(np.linalg.norm(posed_normals, axis=1)[:, None], 1e-12)
    barrel_flags = barrel_weight > .5
    if not np.any(barrel_flags) or not np.any(~barrel_flags):
        raise ValueError('Original mesh has no separable moving barrel bone')
    textures, materials, parts, material_sources = [], [], [], []
    for index, ptr in enumerate(renderer_data['m_Materials']):
        obj = resolve(renderer.assets_file, ptr)
        data = obj.parse_as_dict()
        envs = dict(data['m_SavedProperties']['m_TexEnvs'])
        tex = resolve(obj.assets_file, envs['_MainTex']['m_Texture'])
        if tex is None:
            raise ValueError('Original Minigun albedo missing')
        image = tex.parse_as_object().image.convert('RGBA')
        original_size = list(image.size)
        image.thumbnail((args.texture_size, args.texture_size))
        name = f'texture_{index}'
        image.save(out / (name + '.png'))
        textures.append(dict(name=name, width=image.width, height=image.height,
                             rgba=base64.b64encode(image.tobytes()).decode()))
        materials.append(dict(name=data['m_Name'], color=[1, 1, 1, 1],
                              metallic=.2, smoothness=.35, texture=index))
        material_sources.append(dict(material=object_source(obj), texture=object_source(tex),
                                     original_texture_size=original_size,
                                     output_texture_size=list(image.size)))
    for material_index, triangles in enumerate(handler.get_triangles()):
        triangles = np.asarray(triangles, dtype=int)
        flags = barrel_flags[triangles]
        if np.any(np.any(flags, axis=1) != np.all(flags, axis=1)):
            raise ValueError('A triangle crosses rigid barrel/body bones; do not guess its animation')
        for role, selected_triangles in [('body', triangles[~np.all(flags, axis=1)]),
                                         ('barrels', triangles[np.all(flags, axis=1)])]:
            if not len(selected_triangles):
                continue
            used, compact = np.unique(selected_triangles, return_inverse=True)
            parts.append(dict(name=f'{role}|Minigun_LOD0_{material_index}', group='body', lightRole='',
                              vertices=[xyz(v) for v in posed_vertices[used]],
                              normals=[xyz(v) for v in posed_normals[used]],
                              uv=[dict(u=float(handler.m_UV0[i][0]), v=float(handler.m_UV0[i][1])) for i in used],
                              indices=compact.reshape(-1).astype(int).tolist(), material=material_index))
    model = dict(schemaVersion=2, preset='RUST_MINIGUN_DEPLOYED', parts=parts,
                 materials=materials, textures=textures)
    (out / 'minigun.json').write_text(json.dumps(model, separators=(',', ':')), encoding='utf-8')
    pivot = bone_rows[barrel_bone][1][:3, 3]
    muzzle = next(row[2][:3, 3] for row in rows if row[0] == 'muzzle')
    rig = dict(schemaVersion=1, barrelPivot=xyz(pivot), muzzle=xyz(muzzle),
               barrelAxis=dict(x=0, y=0, z=1), forward='+Z', up='+Y', units='metres')
    (out / 'rig.json').write_text(json.dumps(rig, indent=2), encoding='utf-8')
    print('Exported original body/barrel geometry and textures', flush=True)
    found = []
    for file in env.files.values():
        if not hasattr(file, 'objects'):
            continue
        for obj in file.objects.values():
            if obj.type.name != 'AudioClip':
                continue
            name = obj.peek_name()
            if name not in SOUNDS:
                continue
            samples = obj.parse_as_object().samples
            if len(samples) != 1:
                raise ValueError('Ambiguous original audio clip: ' + name)
            original_wav = next(iter(samples.values()))
            output_wav = mono_wav(original_wav)
            with wave.open(io.BytesIO(output_wav), 'rb') as audio:
                duration = audio.getnframes() / audio.getframerate()
                if duration > 15:
                    raise ValueError('Native Minigun sound exceeds agreed runtime bound: ' + name)
                audio_metadata = dict(sample_rate=audio.getframerate(), channels=audio.getnchannels(),
                                      bits_per_sample=audio.getsampwidth() * 8, samples=audio.getnframes(),
                                      duration_seconds=duration)
            (out / SOUNDS[name]).write_bytes(output_wav)
            found.append(dict(file=SOUNDS[name], original_name=name, **object_source(obj),
                              decoded_original_wav_sha256=digest(original_wav),
                              sha256=digest(output_wav), **audio_metadata))
            print('Original audio:', name, '->', SOUNDS[name], round(duration, 3), 's', flush=True)
    if {item['original_name'] for item in found} != set(SOUNDS):
        raise ValueError('Missing required original Minigun sounds')
    all_vertices = np.vstack([posed_vertices])
    provenance = dict(
        schema_version=1, source_prefab=PREFAB, prefab=object_source(root),
        mesh=object_source(mesh_obj), skin_renderer=object_source(renderer),
        bone_names=[name for name, _ in bone_rows], barrel_bone_index=barrel_bone,
        skin_weight_encoding=weight_encoding,
        body_vertex_count=int(np.count_nonzero(~barrel_flags)),
        barrel_vertex_count=int(np.count_nonzero(barrel_flags)),
        bounds_min=xyz(np.min(all_vertices, axis=0)), bounds_max=xyz(np.max(all_vertices, axis=0)),
        rig=rig, materials=material_sources, sounds=sorted(found, key=lambda item: item['file']),
        files=[dict(file=name, sha256=digest((out / name).read_bytes()))
               for name in ['minigun.json', 'rig.json']],
        operations=['original deployed LOD0 skin evaluated at original prefab rest pose',
                    'rigid barrel triangles separated by original bone weights',
                    'all vertices retained in gun-root coordinates; no pivot subtraction',
                    'native metres, +Z forward, +Y up, no scale/axis change',
                    'original albedo resized for local runtime; diffuse-only material adaptation',
                    'decoded original audio downmixed to PCM16 mono; native rate/timing retained'],
        provenance='Read-only local Rust installation; original meshes, rig pivots, diffuse texture and named audio clips. Licensing/redistribution rights have not been independently verified.',
        limitations='Runtime shoulder mounting, aiming, muzzle flash, damage and barrel spin control are NFVU2 adaptations. Rust scripts, bundles and full weapon animation system are not copied. Visual/audio in-game review pending.')
    (out / 'provenance.json').write_text(json.dumps(provenance, indent=2), encoding='utf-8')
    native_settings = []
    for name, components, _ in rows:
        for obj in components:
            if obj.type.name != 'MonoBehaviour':
                continue
            data = obj.parse_as_dict()
            selected = {key: value for key, value in data.items()
                        if any(token in key.lower() for token in ['spin', 'repeatdelay', 'reloadtime'])}
            if selected:
                native_settings.append(dict(node=name, source=object_source(obj), values=selected))
    (out / 'native_settings.json').write_text(json.dumps(native_settings, indent=2), encoding='utf-8')
    print('Export complete:', len(parts), 'mesh sections,', len(found), 'original sounds', flush=True)


if __name__ == '__main__':
    main()
