"""Check sampled poses recorded from the actual managed gait controller."""
from pathlib import Path
import json,hashlib
source=Path(__file__).with_name('check_transformer_locomotion.py').read_text()
exec(source[:source.index('\n# BEGIN_DOMAIN_REVIEW')])
trace=ROOT/'artifacts/tests/transformer/runtime-gait-preview.json'
for i,p in enumerate(json.loads(trace.read_text())):
    apply_pose(p)
    check('runtime/'+str(i),p['amount']==0)
    if (findings or ground or ik_errors) and '--fail-fast' in sys.argv:break
result=report(TRACE_BINDINGS)
result['scope']='Actual managed controller at60Hz observing50Hz ideal flat-ground root transforms; full posture and actual planted foot outputs replayed. No Unity physics.'
(OUT/'locomotion-v3-trace.json').write_text(json.dumps(result,indent=2))
print('V3 RUNTIME TRACE',result['passed'],tested,'poses',flush=True)
