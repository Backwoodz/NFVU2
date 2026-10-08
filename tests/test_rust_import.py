import io,struct,sys,tempfile,unittest,wave
from pathlib import Path
BASE=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(BASE/'tools'))

class RustImportTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not (BASE/'.local/tools/unitypy/UnityPy').is_dir():raise unittest.SkipTest('Optional local UnityPy import tools not installed')
        import rust_bundle,export_rust_turret
        cls.bundle=rust_bundle;cls.export=export_rust_turret
    def test_readonly_slice_bounds_and_seek(self):
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'source';path.write_bytes(b'headPAYLOADtail')
            with self.bundle.Slice(path,4,7) as src:
                self.assertEqual(src.read(99),b'PAYLOAD')
                src.seek(-3,2);self.assertEqual(src.read(),b'OAD')
                with self.assertRaises(ValueError):src.seek(-1)
                with self.assertRaises(ValueError):src.seek(8)
                with self.assertRaises(ValueError):src.seek(0,3)
            self.assertEqual(path.read_bytes(),b'headPAYLOADtail')
    def test_bundle_directory_and_corrupt_block(self):
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'test.bundle'
            def fixture(blockflags=0):
                table=bytes(16)+struct.pack('>IIIH',1,7,7,blockflags)+struct.pack('>IQQI',1,0,7,0)+b'asset\0'
                packed=self.bundle.lz4.block.compress(table,store_size=False)
                prefix=b'UnityFS\0'+struct.pack('>I',8)+b'5.x.x\0'+b'6000.3.15\0'
                header_end=(len(prefix)+20+15)&~15;begin=(header_end+len(packed)+15)&~15
                header=prefix+struct.pack('>QIII',begin+7,len(packed),len(table),0x243)
                return header.ljust(header_end,b'\0')+packed+bytes(begin-header_end-len(packed))+b'PAYLOAD'
            path.write_bytes(fixture());nodes=self.bundle.nodes(path)
            self.assertEqual(len(nodes),1)
            name,start,size,flags=nodes[0];self.assertEqual(name,'asset')
            with self.bundle.Slice(path,start,size) as src:self.assertEqual(src.read(),b'PAYLOAD')
            path.write_bytes(fixture(2))
            with self.assertRaisesRegex(ValueError,'Compressed payload'):self.bundle.nodes(path)
    def test_mono_downmix_keeps_timing_and_prevents_overflow(self):
        src=io.BytesIO()
        with wave.open(src,'wb') as w:
            w.setnchannels(2);w.setsampwidth(2);w.setframerate(44100)
            w.writeframes(struct.pack('<hhhhhh',32767,32767,-32768,-32768,1000,-1000))
        with wave.open(io.BytesIO(self.export.mono_wav(src.getvalue())),'rb') as w:
            self.assertEqual((w.getnframes(),w.getnchannels(),w.getframerate()),(3,1,44100))
            self.assertEqual(struct.unpack('<hhh',w.readframes(3)),(32767,-32768,0))
    def test_only_runtime_turret_payload_is_copied(self):
        from mod_files import sync_optional_assets
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source=root/'local/turret';source.mkdir(parents=True)
            for name in ('turret.json','shot1.wav','provenance.json','texture_0.png','content.bundle'):(source/name).write_bytes(b'local')
            sync_optional_assets(root/'local',root/'out')
            self.assertEqual({p.name for p in (root/'out/turret').iterdir()},{'turret.json','shot1.wav','provenance.json'})

if __name__=='__main__':unittest.main()
