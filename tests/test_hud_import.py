import sys,unittest,base64
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from export_hud import export

class HudImportTests(unittest.TestCase):
    def test_original_catalog_alpha_and_canvas(self):
        data=export()
        self.assertEqual(len(data['designs']),11)
        self.assertEqual(len({d['fill']['rgba'] for d in data['designs']}),11)
        for i,d in enumerate(data['designs']):
            self.assertEqual(d['fill']['width'],70 if i==10 else 140)
            for part in ('fill','lines','needle'):
                t=d[part];pixels=base64.b64decode(t['rgba'])
                self.assertEqual(len(pixels),t['width']*t['height']*4)
                self.assertEqual(min(pixels[3::4]),0)
                self.assertGreater(max(pixels[3::4]),128)
        self.assertEqual(data['nitro']['name'],'TACH_NOS_ALPHA')
        pixels=base64.b64decode(data['nitro']['rgba'])
        self.assertEqual(pixels[3],0) # no opaque black rectangle behind the arc
