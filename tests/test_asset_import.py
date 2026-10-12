"""Regression checks against the user's ignored original assets."""
import sys
import json
import unittest
from pathlib import Path
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL
from nfs_formats import TexturePack,bin_hash,jdlz
from export_geometry import parse
from export_rachel import wheel_source_radius,wheel_geometry,wheel_finish_sections,exhaust_mouths,sport_brake_geometry,rear_lens_sections


class AssetImportTests(unittest.TestCase):
    def test_chrome_ring_split_preserves_all_original_wheel_triangles(self):
        wheel=next(p for p in parse(LOCAL/'nfs-assets/CARS/WHEELS/GEOMETRY_RACINGHART.BIN') if p['key']==bin_hash('RACINGHART_STYLE07_20_26_A'))
        source=next(s for s in wheel['slices'] if s['materialKey']==0xfd102a92)
        split=dict((finish,s) for s,finish in wheel_finish_sections(wheel,source))
        self.assertEqual(len(split['wheel_chrome']['indices'])//3,120)
        self.assertEqual(len(split['wheel_gold']['indices'])//3,210)
        triangles=lambda s:{tuple(s['indices'][i:i+3]) for i in range(0,len(s['indices']),3)}
        self.assertEqual(triangles(source),triangles(split['wheel_chrome'])|triangles(split['wheel_gold']))
        self.assertFalse(triangles(split['wheel_chrome'])&triangles(split['wheel_gold']))
        self.assertGreater(min(np.hypot(wheel['vertices'][i][0],wheel['vertices'][i][2]) for i in split['wheel_chrome']['indices']),.2319)

    def test_exhaust_emitters_use_actual_two_tip_centres(self):
        part=next(p for p in parse(LOCAL/'nfs-assets/CARS/EXHAUST/GEOMETRY.BIN') if p['key']==bin_hash('EXHAUST_STYLE08_LEVEL1_A'))
        mouths=exhaust_mouths(part)
        self.assertEqual(len(mouths),2)
        self.assertAlmostEqual(mouths[0][0],-.00868424,places=6)
        self.assertAlmostEqual(mouths[0][1],-.052866,places=5)
        self.assertAlmostEqual(mouths[1][1],.061012,places=5)
        self.assertAlmostEqual(mouths[0][2],mouths[1][2],places=6)

    def test_effect_pixel_alignment_preserves_leading_real_0x11_bytes(self):
        pack=TexturePack(LOCAL/'nfs-assets/GLOBAL/InGameCommon.bun')
        self.assertEqual(pack.pixel_base,46080)
        self.assertEqual(pack.data[pack.pixel_base:pack.pixel_base+4],b'\x11\x11\x11\x7f')
        for name in ('SFX_FIREB','SFX_SMOKE_B_CS'):
            decoded,image,_=pack.decode(bin_hash(name))
            self.assertEqual(decoded,name)
            self.assertEqual(image.size,(128,128))
        _,smoke,_=pack.decode(bin_hash('SFX_SMOKE_B_CS'))
        alpha=np.array(smoke)[:,:,3]
        self.assertEqual(alpha[0,0],0)
        self.assertGreater(alpha.max(),100)

    def test_exhaust_catalog_uses_original_geometry_and_matching_mouths(self):
        model=json.loads((LOCAL/'model/body.json').read_text())
        source=parse(LOCAL/'nfs-assets/CARS/EXHAUST/GEOMETRY.BIN')
        variants=model['exhaustVariants']
        self.assertEqual([v['style'] for v in variants],list(range(1,11)))
        for style,openings in enumerate((1,1,2,2,1,1,1,2,2,1),1):
            part=next(p for p in source if p['name']==f'EXHAUST_STYLE{style:02}_LEVEL1_A')
            self.assertEqual(len(exhaust_mouths(part)),openings)
            meshes=[p for p in model['parts'] if p.get('optionGroup')=='exhaust' and p['option']==str(style)]
            self.assertEqual(sum(len(p['indices']) for p in meshes),2*sum(len(s['indices']) for s in part['slices']))
            emitters=variants[style-1]['emitters']
            self.assertEqual(len(emitters),openings*2)
            self.assertTrue(all(e['direction']['z']<-.9 and e['position']['z']<-1.9 for e in emitters))
            self.assertEqual(sum(e['position']['x']<0 for e in emitters),openings)
        self.assertEqual(variants[7]['emitters'],[e for e in model['effectEmitters'] if e['role']=='exhaust'])

    def test_wide_tyre_keeps_diameter_outer_face_and_correct_normals(self):
        wheel=next(p for p in parse(LOCAL/'nfs-assets/CARS/WHEELS/GEOMETRY_RACINGHART.BIN') if p['key']==bin_hash('RACINGHART_STYLE07_20_26_A'))
        vertices,normals=wheel_geometry(wheel)
        ids={i for s in wheel['slices'] if s['materialKey']==0x78743ca1 for i in s['indices']}
        self.assertAlmostEqual(max(vertices[i]['x'] for i in ids)-min(vertices[i]['x'] for i in ids),.28,places=6)
        self.assertAlmostEqual(max(np.hypot(vertices[i]['y'],vertices[i]['z']) for i in ids),.35,places=6)
        self.assertAlmostEqual(max(vertices[i]['x'] for i in ids),.165*.35/wheel_source_radius(wheel),places=6)
        width=max(wheel['vertices'][i][1] for i in ids)-min(wheel['vertices'][i][1] for i in ids)
        radial=.35/wheel_source_radius(wheel);axial=.28/width
        for source,normal in zip(wheel['normals'],normals):
            tangent=np.cross(source,[0,0,1])
            transformed=np.array([-tangent[1]*axial,tangent[2]*radial,tangent[0]*radial])
            vector=np.array([normal['x'],normal['y'],normal['z']])
            self.assertAlmostEqual(np.linalg.norm(vector),1,places=5)
            self.assertAlmostEqual(np.dot(vector,transformed),0,places=5)

    def test_original_sport_brake_has_round_rotor_and_retains_caliper(self):
        source=next(p for p in parse(LOCAL/'nfs-assets/CARS/BRAKES/GEOMETRY.BIN') if p['name']=='BRAKE_STYLE03_A')
        brake=sport_brake_geometry(source)
        rotor=next(s for s in brake['slices'] if s['materialKey']==0xb1aae6bb)
        self.assertEqual(rotor['textureKey'],bin_hash('BRAKE_STYLE03'))
        self.assertEqual(len(rotor['indices'])//3,128)
        self.assertAlmostEqual(max(np.hypot(brake['vertices'][i][0],brake['vertices'][i][2]) for i in rotor['indices']),.18,places=6)
        original_caliper=next(s for s in source['slices'] if s['materialKey']==0xd6640dff)
        painted=next(s for s in brake['slices'] if s['materialKey']==0xd6640dff)
        metal=next(s for s in brake['slices'] if s['materialKey']==0xf0a43b67 and s['textureKey']==0)
        triangles=lambda ids:[tuple(ids[i:i+3]) for i in range(0,len(ids),3)]
        # All original cast faces survive, with only the central hat recoloured.
        self.assertCountEqual(triangles(original_caliper['indices']),
            triangles(painted['indices'])+[t for t in triangles(metal['indices']) if max(t)<len(source['vertices'])])
        self.assertEqual(brake['vertices'][:len(source['vertices'])],source['vertices'])
        details=next(s for s in brake['slices'] if s['materialKey']==0x1fbd2378)
        self.assertFalse(set(range(60,68)).intersection(details['indices']))
        # Both closed boss faces beneath the removed ring decals stay red.
        self.assertTrue(set(range(265,271)).issubset(painted['indices']))
        self.assertTrue(set(range(283,289)).issubset(painted['indices']))
        for tri in triangles(metal['indices']):
            if max(tri)<len(source['vertices']):continue
            a,b,c=np.array([brake['vertices'][i] for i in tri])
            normal=np.cross(b-a,c-a)
            self.assertGreater(np.linalg.norm(normal),1e-7)
            self.assertLess(normal[1],0)  # Solid detail faces point out of wheel.
        # The complete scaled assembly fits between the widened wheel's faces.
        ids={i for s in brake['slices'] for i in s['indices']}
        axial=[(-brake['vertices'][i][1]+.04)*(.19/.18) for i in ids]
        self.assertGreater(min(axial),-.105)
        self.assertLess(max(axial),.175)

    def test_rear_lens_restore_does_not_turn_amber_trim_into_reverse_lamps(self):
        parts=parse(LOCAL/'nfs-assets/CARS/350Z/GEOMETRY.BIN')
        tail=next(p for p in parts if p['key']==bin_hash('350Z_STYLE04_BRAKELIGHT_A'))
        sections=[entry for s in tail['slices'] if s['materialKey']==0x33a26cb6 for entry in rear_lens_sections(tail,'350Z_STYLE04_BRAKELIGHT',s)]
        self.assertEqual([role for _,role in sections],['rear_amber_trim','rear_lens_detail']*2)
        self.assertEqual([len(s['indices'])//3 for s,_ in sections],[5,6,5,6])
        body=next(p for p in parts if p['key']==bin_hash('350Z_KITW02_BODY_A'))
        sections=[entry for s in body['slices'] if s['materialKey']==0x33a26cb6 for entry in rear_lens_sections(body,'350Z_KITW02_BODY',s)]
        self.assertEqual(sum(role=='reverse' for _,role in sections),2)
        self.assertEqual(sum(role=='rear_amber_trim' for _,role in sections),2)
        self.assertEqual(sum(len(s['indices'])//3 for s,_ in sections),12)
        self.assertTrue(all(body['vertices'][i][0]<-1.8 for s,_ in sections for i in s['indices']))

    def test_upper_rear_lens_inserts_have_no_missing_or_duplicated_triangles(self):
        tail=next(p for p in parse(LOCAL/'nfs-assets/CARS/350Z/GEOMETRY.BIN') if p['key']==bin_hash('350Z_STYLE04_BRAKELIGHT_A'))
        for source in tail['slices']:
            if source['materialKey']!=0x33a26cb6:continue
            sections=rear_lens_sections(tail,'350Z_STYLE04_BRAKELIGHT',source)
            triangles=lambda ids:[tuple(ids[i:i+3]) for i in range(0,len(ids),3)]
            self.assertCountEqual(triangles(source['indices']),[t for s,_ in sections for t in triangles(s['indices'])])
            upper=next(s for s,role in sections if role=='rear_lens_detail')
            self.assertGreater(min(tail['vertices'][i][2] for i in upper['indices']),.62)
            self.assertEqual(upper['textureKey'],source['textureKey'])

    def test_original_plate_and_neon_are_decodable(self):
        global_pack=TexturePack(LOCAL/'global-decompressed.bin')
        name,plate,_=global_pack.decode(bin_hash('LICENSE_PLATE'))
        self.assertEqual(name,'LICENSE_PLATE')
        self.assertEqual(plate.size,(128,64))
        pack=TexturePack(LOCAL/'nfs-assets/CARS/350Z/TEXTURES.BIN')
        name,neon,_=pack.decode(bin_hash('350Z_NEON'))
        self.assertEqual(name,'350Z_NEON')
        self.assertEqual(neon.size,(128,64))
        pixels=np.array(neon.convert('RGBA'))
        self.assertGreater(pixels[:,:,:3].max(),100)
        self.assertLess(pixels[:,:,:3].min(),5)

    def test_rear_plate_socket_orientation_and_front_light_locators(self):
        parts=parse(LOCAL/'nfs-assets/CARS/350Z/GEOMETRY.BIN')
        body=next(p for p in parts if p['key']==bin_hash('350Z_KITW02_BODY_A'))
        socket=body['sockets'][0xc805f7e0]
        self.assertAlmostEqual(socket[12],-2.119565,places=5)
        self.assertAlmostEqual(socket[14],.393378,places=5)
        self.assertLess(socket[8],-.95) # plate normal points out the rear
        lamp=next(p for p in parts if p['key']==bin_hash('350Z_STYLE02_HEADLIGHT_LEFT_A'))
        left,right=[lamp['sockets'][key] for key in (0x9db90133,0xd09091c6)]
        self.assertGreater(left[12],1.7)
        self.assertGreater(left[13],.6)
        self.assertLess(right[13],-.6)

    def test_radius_ignores_square_wheel_backing(self):
        parts=parse(LOCAL/'nfs-assets/CARS/WHEELS/GEOMETRY_RACINGHART.BIN')
        wheel=next(p for p in parts if p['key']==bin_hash('RACINGHART_STYLE07_20_26_A'))
        # Independent dimensions of the shipped 26-inch tyre and its atlas
        # backing. Using the latter shrinks the visible tyre by about 28%.
        self.assertAlmostEqual(wheel_source_radius(wheel),.330328,places=5)
        self.assertTrue(any(np.hypot(v[0],v[2])>.45 for v in wheel['vertices']))

    def test_original_vinyl_mask_uses_palette_not_index_brightness(self):
        pack=TexturePack(LOCAL/'nfs-assets/CARS/350Z/VINYLS.BIN')
        _,mask,_=pack.decode(bin_hash('350Z_ART_F_6_MASK'))
        # In the shipped atlas, index 9 covers the solid design; its palette
        # brightness is 255. Treating that index itself as alpha erases it.
        self.assertEqual(mask.getpalette('RGBA')[9*4:9*4+4],[255]*4)
        pixels=np.array(mask.convert('RGBA'))
        self.assertGreater(np.count_nonzero(pixels[:,:,0]>240),24000)
        self.assertGreater(np.count_nonzero(pixels[:,:,0]==0),220000)

    def test_original_spoiler_socket_is_on_trunk(self):
        parts=parse(LOCAL/'nfs-assets/CARS/350Z/GEOMETRY.BIN')
        base=next(p for p in parts if p['key']==bin_hash('350Z_BASE_A'))
        matrix=base['sockets'][0xc93b73fd]
        self.assertAlmostEqual(matrix[12],-1.93327,places=4)
        self.assertAlmostEqual(matrix[14],.787908,places=4)

    def test_jdlz_invalid_back_reference_rejected(self):
        # One back-reference before any literal has been decoded.
        stream=b'JDLZ'+bytes([2,16,0,0])+(3).to_bytes(4,'little')+(20).to_bytes(4,'little')+bytes([1,1,0,0])
        with self.assertRaises(ValueError):jdlz(stream)


if __name__=='__main__':unittest.main()
