"""Regression checks against the user's ignored original assets."""
import sys
import unittest
from pathlib import Path
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from project import LOCAL
from nfs_formats import TexturePack,bin_hash,jdlz
from export_geometry import parse
from export_rachel import wheel_source_radius


class AssetImportTests(unittest.TestCase):
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
