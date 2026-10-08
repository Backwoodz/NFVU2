import json,sys,unittest,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL
from showroom_assets import animations

class ShowroomAssets(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.model=json.loads((LOCAL/'model/body.json').read_text())
        cls.tracks={a['name']:a for a in animations(LOCAL/'nfs-assets/CARS/350Z/PARTS_ANIMATIONS.bin',.04)}

    def test_all_original_tracks_close_at_identity(self):
        self.assertEqual(len(self.tracks),8)
        for track in self.tracks.values():
            f=track['frames'][0]
            self.assertAlmostEqual(f['rotation']['w'],1,places=6)
            self.assertLess(sum(abs(x) for x in f['offset'].values()),.00001)
            self.assertGreater(len(track['frames']),20)

    def test_hinges_and_scissor_translation_are_original(self):
        left=self.tracks['LEFT_DOOR_REG'];suicide=self.tracks['LEFT_DOOR_SUICIDE']
        self.assertGreater(left['pivot']['z'],.6)
        self.assertLess(suicide['pivot']['z'],-.6)
        self.assertLess(left['frames'][-1]['rotation']['y'],0)
        self.assertGreater(suicide['frames'][-1]['rotation']['y'],0)
        for side,sign in [('LEFT',1),('RIGHT',-1)]:
            scissor=self.tracks[side+'_DOOR_SCISSOR']
            self.assertGreater(scissor['frames'][-1]['offset']['x']*sign,.06)
            self.assertGreater(scissor['frames'][-1]['rotation']['x'],.59)

    def test_independent_original_catalog_and_default_system(self):
        m=self.model
        self.assertEqual([len(l['sockets']) for l in m['trunkLayouts']],[0,2,4,6])
        self.assertEqual(len(m['audioComponents']),39)
        for size in ('SMALL','MEDIUM','LARGE'):
            choices=[c for c in m['audioComponents'] if c['size']==size]
            self.assertEqual(len(choices),13)
            self.assertEqual(choices[1]['kind'],'speaker')
            self.assertEqual(sum(c['kind']=='lcd' for c in choices),4)
            for c in choices:self.assertTrue(any(p.get('option')==c['id'] for p in m['parts']))

    def test_moving_groups_include_linings_and_mirrors_but_not_sills(self):
        p=self.model['parts']
        for side,role in [('LEFT','leftDoor'),('RIGHT','rightDoor')]:
            selected=[v for v in p if v.get('movingPart')==role]
            self.assertTrue(any('DOOR_PANEL_'+side in v['name'] for v in selected))
            self.assertTrue(any(side+'_SIDE_MIRROR' in v['name'] for v in selected))
            self.assertFalse(any('SILL' in v['name'] for v in selected))
        self.assertTrue(all(v['movingPart']=='trunk' for v in p if v['name'].startswith('SPOILER_STYLE02')))
        self.assertTrue(all(v.get('movingPart','')=='' for v in p if v.get('optionGroup')=='trunk'))

    def test_engine_variants_and_audio_use_real_textures(self):
        m=self.model
        for option in ('0','1','2'):
            pieces=[p for p in m['parts'] if p.get('optionGroup')=='engine' and p['option']==option]
            self.assertGreater(len(pieces),1)
            self.assertTrue(any(m['materials'][p['material']]['texture']>=0 for p in pieces))
        audio=[p for p in m['parts'] if p['group']=='audio']
        self.assertTrue(all(m['materials'][p['material']]['texture']>=0 for p in audio))

if __name__=='__main__':unittest.main()
