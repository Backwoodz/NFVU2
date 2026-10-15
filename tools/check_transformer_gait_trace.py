"""Check sampled poses recorded from the actual managed gait controller."""
from pathlib import Path
import json,hashlib
source=Path(__file__).with_name('check_transformer_locomotion.py').read_text()
exec(source[:source.index('\nfor i in range(129):')])
trace=ROOT/'artifacts/tests/transformer/runtime-gait-preview.json'
for i,p in enumerate(json.loads(trace.read_text())):
    gait(p['phase'],p['amount'],left_direction=p['left'],right_direction=p['right'],release=p['release'])
    check('runtime/'+str(i),p['amount']==0)
report=dict(passed=not findings and not ground,samples=tested,newContacts=findings,belowGround=ground,
    separatingMountContacts=deployment_contacts,
    hashes={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in (
        'tools/check_transformer_gait_trace.py','tools/check_transformer_locomotion.py',
        'src/TransformerGait.cs','tests/transformer/GaitPreview.cs','artifacts/tests/transformer/runtime-gait-preview.json')},
    scope='Actual managed controller at 60 Hz with an ideal flat-ground 50 Hz trajectory, sampled at 24 Hz; no Unity physics.')
(OUT/'locomotion-v2-trace.json').write_text(json.dumps(report,indent=2))
print('RUNTIME TRACE',report['passed'],tested,'poses',flush=True)
