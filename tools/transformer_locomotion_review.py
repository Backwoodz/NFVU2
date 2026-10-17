"""Bind V4 walk/sprint behavior, hand clearance and geometry to V18."""
import hashlib,json
from project import BASE,LOCAL,OUTPUT

GAIT_VERSION=4
TRACE_FILE='artifacts/tests/transformer/runtime-gait-v4-preview.json'
DOMAIN_FILE='artifacts/tests/transformer/runtime-gait-v4-domain.json'
BEHAVIOR_FILE='artifacts/tests/transformer/runtime-gait-v4-behavior.json'
SPRINT_DEFINITION='tools/transformer_sprint_v4.json'
BEHAVIOR_SOURCES={'src/TransformerGait.cs','src/TransformerVisual.cs','src/VehicleTransformer.cs',
    'src/TransformerPlacement.cs','src/TransformerClearance.cs',
    'src/TransformerTerrainPhase.cs','src/Plugin.cs','src/VehicleControls.cs','src/VehicleTransformerControls.cs',
    'tests/transformer/Program.cs','tests/transformer/GaitPreview.cs',SPRINT_DEFINITION}
COMMON_BINDINGS=BEHAVIOR_SOURCES|{SPRINT_DEFINITION,
    'tools/transformer_gait_v3.json','tools/preview_transformer_locomotion.py',
    'tools/check_transformer_locomotion.py','tools/transformer_locomotion_review.py',
    'src/TransformerVisual.cs','src/TransformerGait.cs','src/VehicleTransformer.cs',
    'tests/transformer/Program.cs','tests/transformer/GaitPreview.cs',
    BEHAVIOR_FILE,DOMAIN_FILE,
    'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'}
DOMAIN_BINDINGS=COMMON_BINDINGS
TRACE_BINDINGS=COMMON_BINDINGS|{
    'tools/check_transformer_gait_trace.py',TRACE_FILE}
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
TERRAIN_CASES={'x+8','x-8','z+8','z-8','stepL+0.1','stepR+0.1','stepL+0.1-raised','stepR+0.1-raised'}
def terrain_reviewed(report):
    if not TERRAIN_CASES.issubset(set(report.get('terrainCases',[]))):return False
    if report.get('terrainContacts') or report.get('terrainBelowGround'):return False
    if report.get('terrainSamples',0)<356 or report.get('terrainDeploymentSamples',0)<64 or report.get('terrainWalkSamples',0)<256:return False
    if report.get('terrainPreparationSource')!='unchanged V18 support-parallel; existing129 flat release samples':return False
    if report.get('terrainStandingRelease')!=1 or report.get('terrainStandingSamples',0)<4:return False
    if report.get('terrainAlignmentSamples',0)<36:return False
    alignment=report.get('terrainAlignmentFractions',[])
    if len(alignment)!=9 or any(not isinstance(p,(int,float)) or abs(p-i/8)>1e-6 for i,p in enumerate(alignment)):return False
    alignment_counts=report.get('terrainAlignmentCaseSamples',{})
    if any(alignment_counts.get(case,0)<9 for case in ('x+8','x-8','z+8','z-8')):return False
    counts=report.get('terrainCaseSamples',{})
    if any(counts.get(case,0)<(57 if case[0] in 'xz' else 32) for case in TERRAIN_CASES):return False
    progress=report.get('terrainDeploymentProgress',[])
    if len(set(progress))<15 or any(not isinstance(p,(int,float)) or not 0<=p<=1 for p in progress):return False
    if any(not any(abs(p-required)<1e-6 for p in progress) for required in (.65,.82,1)):return False
    # Packed V18 hands already contact the legs in part of the original car
    # transformation. Only the exact same flat source frame is a comparison
    # baseline; complete robot poses retain the absolute25mm hand clearance.
    expected=[p for p in progress if p<1-1e-6]
    baseline_progress=report.get('terrainHandBaselineProgress',[])
    if len(baseline_progress)!=len(expected) or any(not isinstance(p,(int,float)) or not .65-1e-6<=p<1 for p in baseline_progress):return False
    if any(not any(abs(p-required)<1e-6 for p in baseline_progress) for required in expected):return False
    baseline_samples=report.get('terrainHandBaselineSamples',0)
    if baseline_samples!=report['terrainDeploymentSamples']-4 or baseline_samples<56:return False
    if report.get('terrainHandAbsoluteSamples')!=report['terrainSamples']-baseline_samples or report['terrainHandAbsoluteSamples']<296:return False
    if report.get('terrainHandBaselineSource')!='same-frame V18 flat-ground transformation':return False
    if report.get('terrainHandBaselineExcessContacts'):return False
    floor=report.get('terrainHandBaselineGapFloor');packed=report.get('terrainHandBaselinePackedMinPositiveGap')
    reduction=report.get('terrainHandBaselineMaximumGapReduction');shortfall=report.get('terrainHandBaselineMaximumGapShortfall')
    if not isinstance(floor,(int,float)) or not abs(floor-.0015)<1e-12:return False
    if not isinstance(packed,(int,float)) or not 0<packed<100:return False
    if not isinstance(reduction,(int,float)) or not 0<=reduction<100:return False
    if not isinstance(shortfall,(int,float)) or not 0<=shortfall<=1e-6:return False
    # The per-pair shortfall is decisive: an original volume overlap improving
    # to a tiny positive gap is valid, even below the1.5mm floor for new pairs.
    original=report.get('terrainHandBaselineOriginal',{})
    try:source_progress=[float(p) for p in original]
    except (TypeError,ValueError):return False
    if len(source_progress)!=len(expected) or any(not .65-1e-6<=p<1 for p in source_progress):return False
    if any(not any(abs(p-required)<1e-6 for p in source_progress) for required in expected):return False
    if any(not isinstance(record,dict) for record in original.values()):return False
    metrics={'reachError','upperLengthError','lowerLengthError','ankleError'};values=report.get('maximumTerrainIkErrors',{})
    if set(values)!=metrics or any(not isinstance(v,(int,float)) or not 0<=v<=.002 for v in values.values()):return False
    clearance=report.get('terrainHandMinimumClearance')
    return isinstance(clearance,(int,float)) and .025<=clearance<100
def valid_report(path,minimum,bindings,terrain_required=False):
    if not path.exists():return False
    report=json.loads(path.read_text())
    if not report.get('passed') or report.get('quick') or report.get('samples',0)<minimum:return False
    if report.get('newContacts') or report.get('belowGround') or report.get('ikErrors'):return False
    if terrain_required and not terrain_reviewed(report):return False
    if report.get('handContacts') or report.get('handCheckSides')!=['L','R']:return False
    if report.get('handCheckSamples')!=report['samples'] or report.get('handCheckMeshes',0)<254:return False
    baseline_samples=report.get('terrainHandBaselineSamples',0) if terrain_required else 0
    if report.get('handBaselineSamples')!=baseline_samples or report.get('handAbsoluteSamples')!=report['samples']-baseline_samples:return False
    if report.get('handBaselineExcessContacts'):return False
    if any(report.get(key,0)<=0 for key in ('handDistanceTests','handTriangleTests','handVolumeTests')):return False
    threshold=report.get('handClearanceThreshold');clearance=report.get('handMinimumClearance')
    if not isinstance(threshold,(int,float)) or not .025<=threshold<1:return False
    if not isinstance(clearance,(int,float)) or not threshold<=clearance<100:return False
    coverage=report.get('handFingerBoneCoverage',{})
    if set(coverage)!={'L','R'}:return False
    for side in ('L','R'):
        required={'hand.'+side}|{'digit%d%d.%s'%(digit,joint,side) for digit in range(4) for joint in range(3)}|{'thumb%d.%s'%(joint,side) for joint in range(3)}
        if not required.issubset(set(coverage[side])):return False
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
    behavior_path=BASE/BEHAVIOR_FILE
    if not behavior_path.exists():return None
    behavior=json.loads(behavior_path.read_text())
    if not behavior.get('passed') or behavior.get('gaitVersion')!=GAIT_VERSION or behavior.get('checks',0)<50 or not behavior.get('sprintTested'):return None
    if behavior.get('sprintFlightSamples',0)<=10 or behavior.get('sprintSupportSamples',0)<=30:return None
    if behavior.get('sprintAirClearanceSamples',0)<=5:return None
    air=behavior.get('maximumAirTime')
    if not isinstance(air,(int,float)) or not 0<air<.4:return None
    error=behavior.get('maximumPlantedError')
    if not isinstance(error,(int,float)) or not 0<=error<.004:return None
    sources=BEHAVIOR_SOURCES
    if set(behavior.get('sourceHashes',{}))!=sources:return None
    if any(not (BASE/name).is_file() or digest(BASE/name)!=sha for name,sha in behavior['sourceHashes'].items()):return None
    if behavior.get('traceSha256')!=digest(BASE/TRACE_FILE):return None
    if behavior.get('domainSha256')!=digest(BASE/DOMAIN_FILE):return None
    path=OUTPUT/'transformer-runtime-review/locomotion-v4-check.json'
    trace_path=OUTPUT/'transformer-runtime-review/locomotion-v4-trace.json'
    if not valid_report(path,5000,DOMAIN_BINDINGS,terrain_required=True):return None
    if not valid_report(trace_path,3600,TRACE_BINDINGS):return None
    return hashlib.sha256(path.read_bytes()+trace_path.read_bytes()).hexdigest()

def configure(manifest):
    path=BASE/'tools/transformer_gait_v3.json';definition=json.loads(path.read_text())
    if manifest['blendSha256']!=digest(BASE/'artifacts/transformer-study-v18/350z-transformer-cinematic.blend'):
        raise ValueError('Gait was reviewed against a different source scene')
    indices={n['name']:i for i,n in enumerate(manifest['nodes'])}
    manifest['gaitReleases']=[dict(node=indices[name],offset=[p[0],p[2],p[1]]) for name,p in definition['releases'].items()]
    review=reviewed()
    manifest.update(gaitVersion=GAIT_VERSION,gaitDefinitionSha256=hashlib.sha256(path.read_bytes()+(BASE/SPRINT_DEFINITION).read_bytes()).hexdigest(),gaitReviewSha256=review,walkingReady=bool(review),
        locomotion='Reviewed V4 walk/sprint transitions, contact/flight timing, hands and sampled geometry; Unity terrain, physics and equipment still require a game test.' if review else 'Locomotion locked pending the current complete V4 behavior, hand-clearance, geometry and IK review.')
    manifest.pop('gaitClearanceSha256',None)
    return manifest

def main():
    if not reviewed():raise SystemExit('Current complete V4 walk/sprint locomotion review is missing or failed; asset remains unchanged.')
    path=LOCAL/'transformer/transformer.json';manifest=json.loads(path.read_text())
    if manifest['payloadSha256']!=digest(path.with_name('transformer.bin')):raise SystemExit('Payload hash differs')
    path.write_text(json.dumps(configure(manifest),indent=2),encoding='utf-8')
    print('Enabled reviewed V4 walk/sprint locomotion in local asset; transformation binary unchanged.')
if __name__=='__main__':main()
