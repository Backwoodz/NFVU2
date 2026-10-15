"""Bind reviewed gait settings to an existing, unchanged V18 mesh payload."""
import hashlib,json
from project import BASE,LOCAL,OUTPUT

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def reviewed():
    path=OUTPUT/'transformer-runtime-review/locomotion-v2-check.json'
    if not path.exists():return None
    report=json.loads(path.read_text())
    if not report.get('passed') or report.get('quick') or report.get('samples',0)<3000:return None
    if report.get('newContacts') or report.get('belowGround'):return None
    expected={'tools/transformer_gait_v2.json','tools/preview_transformer_locomotion.py',
        'tools/check_transformer_locomotion.py','src/TransformerVisual.cs','src/TransformerGait.cs',
        'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'}
    if set(report.get('hashes',{}))!=expected:return None
    if any(not (BASE/name).is_file() or digest(BASE/name)!=sha for name,sha in report['hashes'].items()):return None
    trace_path=OUTPUT/'transformer-runtime-review/locomotion-v2-trace.json'
    if not trace_path.exists():return None
    trace=json.loads(trace_path.read_text())
    if not trace.get('passed') or trace.get('samples',0)<190 or trace.get('newContacts') or trace.get('belowGround'):return None
    trace_expected={'tools/check_transformer_gait_trace.py','tools/check_transformer_locomotion.py',
        'src/TransformerGait.cs','tests/transformer/GaitPreview.cs','artifacts/tests/transformer/runtime-gait-preview.json'}
    if set(trace.get('hashes',{}))!=trace_expected:return None
    if any(not (BASE/name).is_file() or digest(BASE/name)!=sha for name,sha in trace['hashes'].items()):return None
    return hashlib.sha256(path.read_bytes()+trace_path.read_bytes()).hexdigest()

def configure(manifest):
    path=BASE/'tools/transformer_gait_v2.json';definition=json.loads(path.read_text())
    if manifest['blendSha256']!=digest(BASE/'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'):
        raise ValueError('Gait was reviewed against a different source scene')
    indices={n['name']:i for i,n in enumerate(manifest['nodes'])}
    manifest['gaitReleases']=[dict(node=indices[name],offset=[p[0],p[2],p[1]]) for name,p in definition['releases'].items()]
    review=reviewed()
    manifest.update(gaitVersion=2,gaitDefinitionSha256=digest(path),gaitReviewSha256=review,walkingReady=bool(review),
        locomotion='Reviewed flat-ground walking and stepped turning; live Unity physics and tuned equipment still require a game test.' if review else 'Locomotion locked pending the current complete geometry review.')
    manifest.pop('gaitClearanceSha256',None)
    return manifest

def main():
    if not reviewed():raise SystemExit('Current complete locomotion review is missing or failed; asset remains unchanged.')
    path=LOCAL/'transformer/transformer.json';manifest=json.loads(path.read_text())
    if manifest['payloadSha256']!=digest(path.with_name('transformer.bin')):raise SystemExit('Payload hash differs')
    path.write_text(json.dumps(configure(manifest),indent=2),encoding='utf-8')
    print('Enabled reviewed locomotion in local asset; transformation binary unchanged.')

if __name__=='__main__':main()
