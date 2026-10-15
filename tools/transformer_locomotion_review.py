"""Bind complete V3 controller/domain reviews to the unchanged V18 payload."""
import hashlib,json
from project import BASE,LOCAL,OUTPUT

COMMON_BINDINGS={
    'tools/transformer_gait_v3.json','tools/preview_transformer_locomotion.py',
    'tools/check_transformer_locomotion.py','tools/transformer_locomotion_review.py',
    'src/TransformerVisual.cs','src/TransformerGait.cs','src/VehicleTransformer.cs',
    'tests/transformer/Program.cs','tests/transformer/GaitPreview.cs',
    'artifacts/tests/transformer/runtime-gait-behavior.json',
    'artifacts/tests/transformer/runtime-gait-domain.json',
    'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'}
DOMAIN_BINDINGS=COMMON_BINDINGS
TRACE_BINDINGS=COMMON_BINDINGS|{
    'tools/check_transformer_gait_trace.py','artifacts/tests/transformer/runtime-gait-preview.json'}
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def valid_report(path,minimum,bindings):
    if not path.exists():return False
    report=json.loads(path.read_text())
    if not report.get('passed') or report.get('quick') or report.get('samples',0)<minimum:return False
    if report.get('newContacts') or report.get('belowGround') or report.get('ikErrors'):return False
    neutral=report.get('neutralPoseMaxError')
    if not isinstance(neutral,(int,float)) or not 0<=neutral<=2e-5:return False
    # Missing IK evidence is also a failed gate; old geometry-only reports must
    # never accidentally activate the V3 controller.
    metrics={side+'/'+key for side in ('L','R') for key in ('reachError','upperLengthError','lowerLengthError','ankleError')}
    values=report.get('maximumIkErrors',{})
    if set(values)!=metrics or any(not isinstance(v,(int,float)) or not 0<=v<=.002 for v in values.values()):return False
    if set(report.get('hashes',{}))!=bindings:return False
    return all((BASE/name).is_file() and digest(BASE/name)==sha for name,sha in report['hashes'].items())
def reviewed():
    behavior_path=OUTPUT/'tests/transformer/runtime-gait-behavior.json'
    if not behavior_path.exists():return None
    behavior=json.loads(behavior_path.read_text())
    if not behavior.get('passed') or behavior.get('gaitVersion')!=3 or behavior.get('checks',0)<41:return None
    error=behavior.get('maximumPlantedError')
    if not isinstance(error,(int,float)) or not 0<=error<.004:return None
    sources={'src/TransformerGait.cs','src/TransformerVisual.cs','src/VehicleTransformer.cs','tests/transformer/Program.cs','tests/transformer/GaitPreview.cs'}
    if set(behavior.get('sourceHashes',{}))!=sources:return None
    if any(not (BASE/name).is_file() or digest(BASE/name)!=sha for name,sha in behavior['sourceHashes'].items()):return None
    if behavior.get('traceSha256')!=digest(OUTPUT/'tests/transformer/runtime-gait-preview.json'):return None
    if behavior.get('domainSha256')!=digest(OUTPUT/'tests/transformer/runtime-gait-domain.json'):return None
    path=OUTPUT/'transformer-runtime-review/locomotion-v3-check.json'
    trace_path=OUTPUT/'transformer-runtime-review/locomotion-v3-trace.json'
    if not valid_report(path,4000,DOMAIN_BINDINGS):return None
    if not valid_report(trace_path,2000,TRACE_BINDINGS):return None
    return hashlib.sha256(path.read_bytes()+trace_path.read_bytes()).hexdigest()

def configure(manifest):
    path=BASE/'tools/transformer_gait_v3.json';definition=json.loads(path.read_text())
    if manifest['blendSha256']!=digest(BASE/'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'):
        raise ValueError('Gait was reviewed against a different source scene')
    indices={n['name']:i for i,n in enumerate(manifest['nodes'])}
    manifest['gaitReleases']=[dict(node=indices[name],offset=[p[0],p[2],p[1]]) for name,p in definition['releases'].items()]
    review=reviewed()
    manifest.update(gaitVersion=3,gaitDefinitionSha256=digest(path),gaitReviewSha256=review,walkingReady=bool(review),
        locomotion='Reviewed V3 planted flat-ground walking, acceleration, stop/restart, pivots and reversals; Unity terrain, physics and equipment still require a game test.' if review else 'Locomotion locked pending the current complete geometry and IK review.')
    manifest.pop('gaitClearanceSha256',None)
    return manifest

def main():
    if not reviewed():raise SystemExit('Current complete V3 locomotion review is missing or failed; asset remains unchanged.')
    path=LOCAL/'transformer/transformer.json';manifest=json.loads(path.read_text())
    if manifest['payloadSha256']!=digest(path.with_name('transformer.bin')):raise SystemExit('Payload hash differs')
    path.write_text(json.dumps(configure(manifest),indent=2),encoding='utf-8')
    print('Enabled reviewed V3 locomotion in local asset; transformation binary unchanged.')
if __name__=='__main__':main()
