import argparse,shutil,subprocess,sys
from project import BASE,LOCAL,OUTPUT,valheim_dir
from build import build
from build_support import compile_cs,test_refs,runtime_config

def main():
    suites=['model','mesh','handling','audio','radio','nitro','impact','transformerimpact','showroom','driving','turret','targeting','terrain','blast','hummer','menu','cabin','flight','loading','lighting','controls','hotbar','rumble','nitroview','horde','hordehull','hordecorpses','turretfireaudio','hordeweather','horderadar','hordeelements','storage','persistence','streaming','placement','transformer','discovery','vehiclemap','transformershake','transformermount','exhaustsmoke','transformeraudio','robotidleaudio','transformerturnaudio','transformerdeployaudio','transformerreactoraudio','transformerreturnaudio','transformerfootstepeffects','transformerfootstepgrass','transformerwater','transformerminigun','transformerground','transformerminigunheat','transformerminigunaudio']
    suites.append('transformercamera')
    suites.append('driftworld')
    suites.append('drifttrack')
    suites.append('driftbuild')
    suites.append('driftmap')
    suites.extend(('driftscore','driftatmosphere','driftsession'))
    suites.extend(('driftrace','driftarrival','parkade','tyresound','parkadestarter','parkadestartspectators'))
    suites.extend(('driftphysics','driftadapter'))
    suites.append('parkadefestival')
    parser=argparse.ArgumentParser();parser.add_argument('--skip-build',action='store_true');parser.add_argument('--handling-only',action='store_true')
    parser.add_argument('--suite',action='append',choices=suites,help='Run only these C# suites; repeat to combine.')
    args=parser.parse_args()
    if not args.handling_only and not args.skip_build:build()
    if not args.handling_only and not args.suite:
        subprocess.run([sys.executable,'-m','unittest','discover','-s',str(BASE/'tests'),'-p','test_*.py'],check=True)
    references,tfm,version=test_refs()
    selected=['handling'] if args.handling_only else args.suite or suites
    if not args.suite and 'transformer' in selected and not (LOCAL/'transformer/transformer.json').is_file():
        selected=[s for s in selected if s!='transformer']
        print('Optional transformer asset absent; export V18 to enable its payload checks.')
    for suite in selected:
        out=OUTPUT/'tests'/suite;out.mkdir(parents=True,exist_ok=True)
        sources=list((BASE/'tests'/suite).glob('*.cs'));refs=list(references);arguments=[]
        if suite=='handling':sources.extend(BASE/'src'/name for name in ('DrivingMath.cs','VehicleHandling.cs','VehicleDriftTuning.cs','HummerTuning.cs','VehicleRideHeight.cs'))
        elif suite=='driftbuild':sources.extend(BASE/'src'/name for name in ('DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftTrackPieces.cs','DriftTrackSignCorrection.cs','DriftTrackBuilder.cs','DriftCircuitLayout.cs','DriftCircuitPlan.cs','DriftCircuitPieces.cs','DriftCircuitLabels.cs'))
        elif suite=='driftmap':sources.extend(BASE/'src'/name for name in ('DriftCircuitMapDrawing.cs','DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftCircuitLayout.cs','DriftCircuitPlan.cs'))
        elif suite=='driftscore':sources.extend(BASE/'src'/name for name in ('DriftLapScore.cs','DriftCircuitScore.cs','DriftTrackLayout.cs','DriftCircuitLayout.cs'))
        elif suite=='driftrace':sources.extend(BASE/'src'/name for name in ('DriftRaceRun.cs','DriftLapScore.cs','DriftTrackLayout.cs','DriftCircuitLayout.cs','ParkadeLayout.cs','DriftEventMenuNavigation.cs'))
        elif suite=='driftarrival':
            sources.append(BASE/'src/DriftArrivalState.cs')
            dependency=out/'Mono.Cecil.dll';shutil.copy2(valheim_dir()/'BepInEx/core/Mono.Cecil.dll',dependency);refs.append(dependency)
            arguments=[str(valheim_dir()),str(OUTPUT/'mod/Rachel350Z.dll')]
        elif suite=='tyresound':sources.append(BASE/'src/TyreSoundModel.cs')
        elif suite=='parkadestarter':sources.extend(BASE/'src'/name for name in ('ParkadeStarterPose.cs','DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftTrackPieces.cs','ParkadeLayout.cs','ParkadePlan.cs'))
        elif suite=='parkadestartspectators':sources.extend(BASE/'src'/name for name in ('DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftTrackPieces.cs','ParkadeLayout.cs','ParkadePlan.cs','ParkadeStartSpectatorPlan.cs'))
        elif suite=='parkadefestival':sources.extend(BASE/'src'/name for name in ('DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftTrackPieces.cs','ParkadeLayout.cs','ParkadePlan.cs','ParkadeFestivalPlan.cs'))
        elif suite=='parkade':sources.extend(BASE/'src'/name for name in ('DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftTrackPieces.cs','ParkadeLayout.cs','ParkadePlan.cs','ParkadeVenue.cs'))
        elif suite=='driftatmosphere':sources.extend(BASE/'src'/name for name in ('DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftCircuitLayout.cs','DriftCircuitPlan.cs','DriftCircuitAtmospherePlan.cs'))
        elif suite=='driftsession':
            sources.extend(BASE/'src'/name for name in ('DriftWorldRules.cs','DriftSessionRules.cs','DriftSessionEnvironment.cs','DriftSessionCleanup.cs','DriftSessionPatches.cs','DriftSessionRaven.cs'))
            for name in ('0Harmony','Mono.Cecil'):
                dependency=out/(name+'.dll');shutil.copy2(valheim_dir()/'BepInEx/core'/(name+'.dll'),dependency);refs.append(dependency)
            arguments=[str(valheim_dir()),str(OUTPUT/'mod/Rachel350Z.dll')]
        elif suite=='driftphysics':sources.extend(sorted((BASE/'physics').glob('*.cs')))
        elif suite=='driftadapter':
            sources.extend(sorted((BASE/'physics').glob('*.cs')))
            sources.extend(BASE/'src'/name for name in ('DriftVehicleAdapter.cs','VehicleNitro.cs','NitroState.cs','DrivingMath.cs'))
        elif suite=='mesh':sources.append(BASE/'src/VehicleMesh.cs')
        elif suite=='placement':sources.append(BASE/'src/TransformerPlacement.cs')
        elif suite=='lighting':sources.extend(BASE/'src'/name for name in ('VehicleLightingState.cs','DrivingMath.cs'))
        elif suite=='flight':sources.extend(BASE/'src'/name for name in ('FlightState.cs','VehicleFlight.cs','NitroState.cs','DrivingMath.cs'))
        elif suite=='impact':sources.extend(BASE/'src'/name for name in ('VehicleImpactRules.cs','VehicleImpact.cs'))
        elif suite=='transformerimpact':sources.extend(BASE/'src'/name for name in ('VehicleImpactRules.cs','TransformerImpact.cs'))
        elif suite=='nitroview':sources.append(BASE/'src/NitroCameraView.cs')
        elif suite=='storage':sources.extend(BASE/'src'/name for name in ('VehicleStorageRules.cs','VehicleStorage.cs','VehiclePersistence.cs'))
        elif suite=='exhaustsmoke':sources.append(BASE/'src/ExhaustSmokeRules.cs')
        elif suite=='persistence':
            sources.append(BASE/'tests/storage/Doubles.cs')
            sources.extend(BASE/'src'/name for name in ('VehicleStorageRules.cs','VehicleStorage.cs','VehiclePersistence.cs','VehicleWorldPersistence.cs'))
        elif suite=='streaming':sources.append(BASE/'src/VehicleWorldStreaming.cs')
        elif suite=='discovery':sources.append(BASE/'src/VehicleDiscovery.cs')
        elif suite=='vehiclemap':sources.extend(BASE/'src'/name for name in ('VehicleMap.cs','VehicleMapPins.cs'))
        elif suite=='transformershake':sources.extend(BASE/'src'/name for name in ('TransformerGait.cs','TransformerStepShake.cs'))
        elif suite=='transformercamera':sources.extend(BASE/'src'/name for name in ('TransformerCinematicCamera.cs','VehicleOrbitCamera.Cinematic.cs','DrivingMath.cs','ChaseCameraState.cs'))
        elif suite=='transformerground':sources.extend(BASE/'src'/name for name in ('TransformerGroundProbe.cs','TransformerWaterRules.cs'))
        elif suite=='transformerwater':sources.append(BASE/'src/TransformerWaterRules.cs')
        elif suite=='transformerminigun':sources.extend(BASE/'src'/name for name in ('TransformerMinigunModel.cs','TransformerMinigunDamage.cs','TransformerMinigunFeedback.cs','TurretTargeting.cs','TurretState.cs'))
        elif suite=='transformerminigunheat':sources.append(BASE/'src/TransformerMinigunHeatModel.cs')
        elif suite=='transformerminigunaudio':
            sources.extend(BASE/'src'/name for name in ('TransformerMinigunAudio.cs','TransformerMinigunSoundLevels.cs','WavPcm16.cs'))
            arguments=[str(LOCAL/'minigun')]
        elif suite=='transformerfootstepgrass':sources.append(BASE/'src/TransformerFootstepGrass.cs')
        elif suite=='transformerfootstepeffects':sources.append(BASE/'src/TransformerFootstepEffectModel.cs')
        elif suite=='transformeraudio':sources.extend(BASE/'src'/name for name in ('TransformerGait.cs','TransformerFootstepModel.cs','TransformerFootstepAudio.cs','WavPcm16.cs'))
        elif suite=='transformerturnaudio':sources.extend(BASE/'src'/name for name in ('TransformerTurnSoundModel.cs','TransformerTurnAudio.cs','WavPcm16.cs'))
        elif suite=='transformerdeployaudio':sources.extend(BASE/'src'/name for name in ('TransformerDeploySoundModel.cs','TransformerDeployAudio.cs','WavPcm16.cs'))
        elif suite=='transformerreturnaudio':sources.extend(BASE/'src'/name for name in ('TransformerReturnSoundModel.cs','TransformerReturnAudio.cs','WavPcm16.cs'))
        elif suite=='transformerreactoraudio':sources.extend(BASE/'src'/name for name in ('TransformerReactorAudio.cs','WavPcm16.cs'))
        elif suite=='robotidleaudio':sources.extend(BASE/'src'/name for name in ('VehicleAudio.cs','TyreSoundModel.cs','VehicleEffectsAudio.cs','ExhaustOverrunModel.cs','VehicleSoundPerspective.cs','EngineSoundModel.cs','TurboSoundModel.cs','ReverseGearSound.cs','ExhaustSoundProfile.cs','DrivingMath.cs','WavPcm16.cs'))
        elif suite=='transformermount':sources.extend(BASE/'src'/name for name in ('TransformerMountMotion.cs','TransformerFlipShape.cs','TransformerDismountArc.cs','TransformerClimbLeg.cs'))
        elif suite=='horde':sources.extend(BASE/'src'/name for name in ('HordeWave.cs','HordeRun.cs','HordePact.cs','HordeBonus.cs','HordeElementRules.cs'))
        elif suite=='hordeweather':sources.append(BASE/'src/HordeWeather.cs')
        elif suite=='horderadar':sources.append(BASE/'src/HordeMapProjection.cs')
        elif suite=='turretfireaudio':sources.extend(BASE/'src'/name for name in ('TurretFireAudio.cs','TurretState.cs'))
        elif suite=='terrain':sources.extend(BASE/'src'/name for name in ('TurretTerrain.cs','TurretState.cs'))
        elif suite=='targeting':sources.extend(BASE/'src'/name for name in ('TurretTargeting.cs','TurretState.cs'))
        elif suite=='blast':sources.extend(BASE/'src'/name for name in ('TurretDamage.cs','TurretState.cs'))
        elif suite in ('hotbar','rumble','hordehull','hordecorpses','hordeelements','driftworld','drifttrack'):
            if suite=='driftworld':sources.extend(BASE/'src'/name for name in ('DriftWorldRules.cs','DriftWorldPatches.cs','DriftSeedFeedback.cs','DriftTrackLayout.cs','ParkadeLayout.cs'))
            elif suite=='drifttrack':sources.extend(BASE/'src'/name for name in ('DriftTrackLayout.cs','DriftTrackConstructionPlan.cs','DriftTrackPieces.cs','DriftTrackSignCorrection.cs','DriftCircuitLayout.cs','DriftCircuitPlan.cs','DriftCircuitPieces.cs','DriftCircuitLabels.cs'))
            elif suite=='hordeelements':sources.extend(BASE/'src'/name for name in ('HordeElementCombat.cs','HordeElementStatus.cs','HordeElementRules.cs','HordePact.cs'))
            elif suite=='hordehull':sources.extend(BASE/'src'/name for name in ('VehicleHordeHull.cs','VehicleImpactRules.cs'))
            elif suite=='hordecorpses':sources.extend(BASE/'src'/name for name in ('HordeCorpses.cs','HordeProjectiles.cs'))
            else:sources.append(BASE/'src'/('VehicleHotbarCapture.cs' if suite=='hotbar' else 'VehicleCameraShake.cs'))
            harmony=out/'0Harmony.dll';shutil.copy2(valheim_dir()/'BepInEx/core/0Harmony.dll',harmony);refs.append(harmony)
        else:
            plugin=out/'Rachel350Z.dll';shutil.copy2(OUTPUT/'mod/Rachel350Z.dll',plugin);refs.append(plugin)
            physics=out/'NFVU2.DriftPhysics.dll';shutil.copy2(OUTPUT/'mod/NFVU2.DriftPhysics.dll',physics);refs.append(physics)
        if suite=='model':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'model/body.json')]
        if suite=='loading':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(OUTPUT/'mod')]
        if suite=='audio':arguments=[str(out/'preview-state.csv'),str(OUTPUT/'mod/audio/effects'),str(OUTPUT/'mod/audio/exhaust')]
        if suite=='radio':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
        if suite=='menu':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'menu')]
        if suite=='driving':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'hud/hud.json')]
        if suite=='cabin':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'interior/interior.json')]
        if suite=='transformer':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'transformer'),str(LOCAL/'model/body.json')]
        if suite=='hummer':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'hummer/h2.json'),str(LOCAL/'audio-hummer')]
        if suite=='turret':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'turret')]
        if suite in ('driftworld','drifttrack'):
            # The installed BepInEx HarmonyX/MonoMod build targets the desktop
            # CLR/Mono. Run actual detours there, not on an unsupported .NET 9.
            from build_installer import framework
            # Reflection-based plugin checks resolve its own physics dependency
            # beside the test executable, without adding it to desktop CLR refs.
            shutil.copy2(OUTPUT/'mod/NFVU2.DriftPhysics.dll',out/'NFVU2.DriftPhysics.dll')
            root=framework()
            refs=[root/(name+'.dll') for name in ('mscorlib','System','System.Core')]+[harmony]
            facade=out/'netstandard.dll'
            shutil.copy2(valheim_dir()/'valheim_Data/Managed/netstandard.dll',facade)
            refs.append(facade)
            for name in ('MonoMod.RuntimeDetour','MonoMod.Utils','Mono.Cecil'):
                shutil.copy2(valheim_dir()/'BepInEx/core'/(name+'.dll'),out/(name+'.dll'))
            refs.append(out/'Mono.Cecil.dll')
            native=out/'assembly_valheim.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/assembly_valheim.dll',native)
            refs.append(('NativeValheim',native))
            for name in ('UnityEngine','UnityEngine.CoreModule','assembly_utils','Splatform'):
                shutil.copy2(valheim_dir()/'valheim_Data/Managed'/(name+'.dll'),out/(name+'.dll'))
            refs.append(('NativeUnity',out/'UnityEngine.CoreModule.dll'))
            refs.append(('NativeUtils',out/'assembly_utils.dll'))
            exe=out/(suite+'Tests.exe');compile_cs(exe,sources,refs)
            subprocess.run([str(exe),str(valheim_dir()),str(OUTPUT/'mod/Rachel350Z.dll')],check=True)
            continue
        dll=out/(suite+'Tests.dll');compile_cs(dll,sources,refs);runtime_config(dll,tfm,version)
        subprocess.run(['dotnet',str(dll),*arguments],check=True)
    print('Requested checks passed. These tests do not execute Unity physics or playback.')

if __name__=='__main__':main()
