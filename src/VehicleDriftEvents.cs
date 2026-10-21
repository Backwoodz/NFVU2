using System;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    const string ParkadeRecordKey="nfvu2_drift_parkade1_best_v1";
    readonly DriftRaceRun driftRace=new DriftRaceRun();
    ParkadeVenue parkadeVenue;
    DriftEventPad driftEventPad;
    DriftEventPanel driftEventPanel;
    DriftCircuitMap parkadeMiniMap;
    ParkadeStartLine parkadeStartLine;
    ParkadeRaceStarter parkadeRaceStarter;
    ParkadeStartSpectators parkadeStartSpectators;
    ParkadeNeonSign parkadeNeonSign;
    ParkadeFestival parkadeFestival;
    ParkadeServiceYards parkadeServiceYards;
    Minimap driftEventMap;
    Minimap.PinData eventPadPin,parkadePin;
    ZDOMan driftEventWorld;
    Game driftEventGame;
    VehicleRuntime driftEventVehicle;
    bool driftPadLatched,driftRaceUi,driftRaceHeld,driftRaceWasKinematic,driftEventFailed;
    float driftTransferStarted,driftRecordRetry;
    int driftRecordSaved;
    bool driftPreparationAtStart;
    int driftPreparationProgress;
    float driftPreparationLog;
    bool driftPauseRequested;
    int driftRaceUiOpenedFrame=-1;
    bool DriftRaceOwnsVehicle=>driftRace.UsesRaceTrack;
    bool DriftRaceHoldsCurrent=>driftRaceHeld&&current==driftEventVehicle;
    static DriftScoreRoute ParkadeScoreRoute=>new DriftScoreRoute(ParkadeLayout.TotalLength,ParkadeLayout.StartX,ParkadeLayout.StartZ,
        ParkadeLayout.StartTangentX,ParkadeLayout.StartTangentZ,ParkadeLayout.StartHalfWidth,ParkadeLayout.Project);

    // Runs once for the selected car, after staged construction and UI gating.
    bool TickDriftEvents(MonoBehaviour local) {
        if(local==null||!DriftSessionRules.Active||Multiplayer()||Game.instance==null||Game.instance.IsShuttingDown()||ZDOMan.instance==null) {
            ClearDriftEvents();return false;
        }
        try {
            if(driftEventWorld!=ZDOMan.instance||driftEventGame!=Game.instance) {
                ClearDriftEvents();driftEventWorld=ZDOMan.instance;driftEventGame=Game.instance;
                string stored=null;if(ZoneSystem.instance!=null)ZoneSystem.instance.GetGlobalKey(ParkadeRecordKey,out stored);
                DriftLapRecord.Decode(stored,out var best,out var time);driftRace.LoadRecord(best,time);
            }
            if(driftEventFailed)return false;
            if(parkadeVenue==null)parkadeVenue=new ParkadeVenue(message=>Logger.LogWarning(message));
            parkadeVenue.Tick();SaveParkadeRecord();
            if(parkadeMiniMap==null)parkadeMiniMap=new DriftCircuitMap(message=>Logger.LogWarning(message),ParkadeMapDrawing.Pixels,ParkadeMapDrawing.Size,ParkadeMapDrawing.MinX,ParkadeMapDrawing.MinZ,ParkadeMapDrawing.Span);
            parkadeMiniMap.Tick(Minimap.instance,driftEventWorld,true);
            if(parkadeStartLine==null)parkadeStartLine=new ParkadeStartLine(message=>Logger.LogWarning(message));
            parkadeStartLine.Tick();UpdateDriftEventPins();
            var viewer=body!=null?body.position:local.transform.position;
            if(parkadeStartSpectators==null)parkadeStartSpectators=new ParkadeStartSpectators(message=>Logger.LogWarning(message));
            parkadeStartSpectators.Tick(viewer,driftEventWorld);
            if(parkadeNeonSign==null)parkadeNeonSign=new ParkadeNeonSign(message=>Logger.LogWarning(message));
            parkadeNeonSign.Tick(viewer,driftEventWorld);
            if(parkadeFestival==null)parkadeFestival=new ParkadeFestival(System.IO.Path.GetDirectoryName(typeof(Plugin).Assembly.Location),message=>Logger.LogWarning(message));
            parkadeFestival.Tick(viewer,driftEventWorld);
            if(parkadeServiceYards==null)parkadeServiceYards=new ParkadeServiceYards(message=>Logger.LogWarning(message));
            parkadeServiceYards.Tick(viewer,driftEventWorld);
            if(driftEventPad==null)driftEventPad=new DriftEventPad();
            driftEventPad.Tick(driftRace.Phase==DriftRacePhase.Practice||driftRace.Phase==DriftRacePhase.Preview);
            bool validCar=driving&&car!=null&&body!=null&&!IsHummer&&!Flying&&!Transforming&&current!=null;
            if(validCar&&DriftPauseArea&&(driftRace.Phase==DriftRacePhase.Practice||driftRace.Phase==DriftRacePhase.Racing)
                &&(driftPauseRequested||!audioBlocked&&DriftRaceMenuShortcutDown()))PauseDriftEvent();
            if(driftRace.Phase==DriftRacePhase.Practice) {
                if(validCar&&!audioBlocked&&!menuOpen&&DriverInputReady) {
                    float dx=body.position.x-DriftEventPad.X,dz=body.position.z-DriftEventPad.Z,d=dx*dx+dz*dz;
                    if(d>36)driftPadLatched=false;
                    if(!driftPadLatched&&d<DriftEventPad.Radius*DriftEventPad.Radius) {
                        driftPadLatched=true;driftRace.Preview();HoldDriftRaceCar();OpenDriftEventUI();
                    }
                }
            }
            if(driftRace.Phase!=DriftRacePhase.Practice&&(!validCar||current!=driftEventVehicle||local is Player p&&(p.IsDead()||p.IsTeleporting()))) {
                ReleaseDriftRaceHold();driftRace.Practice();CloseDriftEventUI();return false;
            }
            if(driftRace.Phase==DriftRacePhase.Loading) {
                if(ZNet.instance!=null)ZNet.instance.SetReferencePosition(body.position);
                int progress=parkadeVenue.CompletedOperations;
                if(progress>driftPreparationProgress){driftPreparationProgress=progress;driftTransferStarted=Time.unscaledTime;}
                if(parkadeVenue.ConstructionComplete&&!driftPreparationAtStart) {
                    MoveToParkadeStart();driftPreparationAtStart=true;
                } else if(parkadeVenue.TryPreparationPoint(body.position,out var preparation)) {
                    if(!MoveDriftCarTo(preparation,body.rotation,false))throw new InvalidOperationException("Parkade preparation requires a held car");
                    driftPreparationAtStart=false;
                }
                if(driftPreparationAtStart&&parkadeVenue.RaceReady&&DriftTransferGroundReady()) {
                    if(parkadeRaceStarter==null)parkadeRaceStarter=new ParkadeRaceStarter(message=>Logger.LogWarning(message));
                    if(parkadeRaceStarter.EnsureReady(driftEventWorld))driftRace.Ready();
                    else if(parkadeRaceStarter.Failed)throw new InvalidOperationException("Greydwarf race starter could not be prepared");
                }
                if(driftRace.Phase==DriftRacePhase.Loading&&Time.unscaledTime-driftTransferStarted>120) {
                    Logger.LogWarning("Parkade preparation stalled: "+parkadeVenue.PendingStatus());
                    ShowVehicleWarning("Parkade wird noch nicht bereit. Rueckkehr zum Parkplatz.");ReturnFromDriftRace();
                } else if(driftRace.Phase==DriftRacePhase.Loading&&Time.unscaledTime>=driftPreparationLog) {
                    driftPreparationLog=Time.unscaledTime+10;
                    Logger.LogInfo("Parkade preparation "+progress+"/"+parkadeVenue.PlannedOperations+": "+parkadeVenue.PendingStatus());
                }
            } else if(driftRace.Phase==DriftRacePhase.Returning) {
                if(ZNet.instance!=null&&body!=null)ZNet.instance.SetReferencePosition(body.position);
                if(DriftTransferGroundReady()){ReleaseDriftRaceHold();driftRace.Practice();CloseDriftEventUI();}
            } else if(driftRace.Phase==DriftRacePhase.Countdown) {
                TickDriftRaceCountdown(!audioBlocked&&Application.isFocused,Time.deltaTime);
            } else if(driftRace.Phase==DriftRacePhase.Racing) {
                if(audioBlocked||menuOpen||!DriverInputReady||!Application.isFocused||Time.timeScale<=0)driftRace.Score.Pause();
                else {
                    driftRace.TickLaunch(Time.deltaTime);
                    int contacts=0;
                    if(DriftPhysicsActive)for(int i=0;i<4;i++){if(current.driftPhysics.Grounded(i))contacts++;}
                    else if(wheels!=null)foreach(var wheel in wheels)if(wheel!=null&&wheel.isGrounded)contacts++;
                    var pos=body.position;var velocity=body.linearVelocity;var forward=body.rotation*Vector3.forward;
                    bool grounded=contacts>=2&&Math.Abs(pos.y-ParkadeLayout.GroundHeightAt(pos.x,pos.z))<3;
                    driftRace.Step(new DriftLapSample(pos.x,pos.z,velocity.x,velocity.z,forward.x,forward.z,Time.deltaTime,grounded));
                    if(driftRace.Phase==DriftRacePhase.Finished){SaveParkadeRecord();HoldDriftRaceCar();OpenDriftEventUI();}
                }
            }
            if(parkadeRaceStarter!=null) {
                if(driftRace.Phase==DriftRacePhase.Countdown||driftRace.Phase==DriftRacePhase.Racing||driftRace.Phase==DriftRacePhase.Paused||driftRace.Phase==DriftRacePhase.Finished)
                    parkadeRaceStarter.Tick(driftRace.Countdown,driftRace.LaunchElapsed,driftRace.Phase!=DriftRacePhase.Countdown);
                else parkadeRaceStarter.Clear();
            }
            if(driftRaceUi) {
                if(!menuOpen)SetVehicleMenu(true);
                Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            }
            if(driftEventPanel==null)driftEventPanel=new DriftEventPanel(()=>RunDriftEventAction(AcceptDriftEvent),()=>RunDriftEventAction(CancelDriftEvent),
                System.IO.Path.GetDirectoryName(Info.Location),message=>Logger.LogWarning(message),()=>RunDriftEventAction(RestartDriftEvent));
            driftEventPanel.Tick(Hud.instance,driftRace,parkadeVenue.CompletedOperations,parkadeVenue.PlannedOperations,uiLanguage.Value=="DE");
            if(driftRaceUi&&!audioBlocked&&Application.isFocused) {
                if(DriftEventMenuNavigation.IsPause(driftRace.Phase)&&Time.frameCount>driftRaceUiOpenedFrame&&DriftRaceMenuShortcutDown())AcceptDriftEvent();
                else TickDriftEventInput();
            }
            return driftRaceUi||driftRaceHeld;
        } catch(Exception error) {
            FailDriftEvent(error);return false;
        }
    }
    // Native button callbacks run outside Update's exception boundary.
    void TickDriftRaceCountdown(bool advance,float deltaTime) {
        if(driftRace.Phase!=DriftRacePhase.Countdown)return;
        if(advance)driftRace.TickCountdown(deltaTime);
        if(driftRace.Phase!=DriftRacePhase.Racing)return;
        // Commit the low start pose before releasing the actual car body.
        parkadeRaceStarter?.Tick(0,0,true);
        ReleaseDriftRaceHold();CloseDriftEventUI();controlsGate.Allow(false);
    }
    void RunDriftEventAction(Action action){try{action();}catch(Exception error){FailDriftEvent(error);}}
    void FailDriftEvent(Exception error) {
        ReleaseDriftRaceHold();CloseDriftEventUI();driftRace.Practice();driftEventFailed=true;
        driftEventPanel?.Clear();driftEventPanel=null;
        parkadeRaceStarter?.Clear();
        Logger.LogWarning("Drift event stopped: "+error.GetBaseException().Message);
        ShowVehicleWarning("Rennen konnte nicht gestartet werden. Siehe BepInEx-Log.");
    }
    void AcceptDriftEvent() {
        if(!driftRaceUi||audioBlocked||!Application.isFocused)return;
        if(DriftEventMenuNavigation.IsPause(driftRace.Phase)){ResumeDriftEvent();return;}
        if(driftRace.Phase!=DriftRacePhase.Preview&&driftRace.Phase!=DriftRacePhase.Finished)return;
        StartDriftEventRace();
    }
    void PauseDriftEvent() {
        driftPauseRequested=false;
        if(driftRace.Phase!=DriftRacePhase.Racing&&driftRace.Phase!=DriftRacePhase.Practice)return;
        if(driftRace.Phase==DriftRacePhase.Practice)driftLapScore?.Pause();
        driftRace.Pause();HoldDriftRaceCar();OpenDriftEventUI();
    }
    void ResumeDriftEvent() {
        if(!DriftEventMenuNavigation.IsPause(driftRace.Phase))return;
        ReleaseDriftRaceHold();driftRace.Resume();CloseDriftEventUI();
    }
    void RestartDriftEvent() {
        if(!driftRaceUi||!DriftEventMenuNavigation.IsPause(driftRace.Phase)||audioBlocked||!Application.isFocused)return;
        StartDriftEventRace();
    }
    void StartDriftEventRace() {
        driftEventVehicle=current;driftRace.Start(ParkadeScoreRoute);HoldDriftRaceCar();
        parkadeRaceStarter?.Clear();
        MoveToParkadeStart();driftPreparationAtStart=true;driftPreparationProgress=0;driftPreparationLog=Time.unscaledTime+10;
        driftTransferStarted=Time.unscaledTime;ClearDriftCircuitHud();
    }
    void MoveToParkadeStart() {
        var s=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength-5);
        var position=new Vector3(s.X,ParkadeLayout.GroundHeightAt(s.X,s.Z)+.35f,s.Z);
        var rotation=Quaternion.LookRotation(new Vector3(s.TangentX,0,s.TangentZ));
        if(!MoveDriftCarTo(position,rotation,false))throw new InvalidOperationException("Race transfer requires a held 350Z");
    }
    void CancelDriftEvent() {
        if(!driftRaceUi)return;
        if(driftRace.Phase==DriftRacePhase.Loading||driftRace.Phase==DriftRacePhase.Returning)return;
        if(driftRace.Phase==DriftRacePhase.Preview){ReleaseDriftRaceHold();driftRace.Practice();CloseDriftEventUI();return;}
        if(driftRace.Phase!=DriftRacePhase.Returning)ReturnFromDriftRace();
    }
    void ReturnFromDriftRace() {
        parkadeRaceStarter?.Clear();
        driftRace.Return();HoldDriftRaceCar();
        if(body!=null&&car!=null&&MoveDriftCarTo(DriftParkingPosition,DriftParkingRotation,false)) {
            driftTransferStarted=Time.unscaledTime;OpenDriftEventUI();ClearDriftCircuitHud();
        } else {ReleaseDriftRaceHold();driftRace.Practice();CloseDriftEventUI();}
    }
    void HoldDriftRaceCar() {
        if(driftRaceHeld||body==null)return;
        driftEventVehicle=current;ReleaseDriftPhysics();
        current.worldStreaming?.Dispose();current.worldStreaming=null;
        driftRaceWasKinematic=body.isKinematic;
        if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        body.isKinematic=true;driftRaceHeld=true;BlockDrivingInput();
    }
    void ReleaseDriftRaceHold() {
        if(!driftRaceHeld)return;
        var selected=current;
        try {
            if(driftEventVehicle!=null)current=driftEventVehicle;
            current.worldStreaming?.Dispose();current.worldStreaming=null;
            if(body!=null){body.isKinematic=driftRaceWasKinematic;if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.WakeUp();}}
        } finally {current=selected;driftRaceHeld=false;}
    }
    bool DriftTransferGroundReady() {
        if(body==null||ZNetScene.instance==null||ZoneSystem.instance==null||!ZNetScene.instance.IsAreaReady(body.position))return false;
        return ZoneSystem.instance.GetGroundHeight(body.position,out var y)&&Math.Abs(y-body.position.y)<2;
    }
    void OpenDriftEventUI(){CloseVehicleMenu();SetVehicleMenu(true);driftRaceUi=true;driftRaceUiOpenedFrame=Time.frameCount;driftEventPanel?.ResetNavigation();}
    void CloseDriftEventUI(){driftRaceUi=false;driftPauseRequested=false;CloseVehicleMenu();}
    void SaveParkadeRecord() {
        if(driftRace.RecordRevision==driftRecordSaved||Time.unscaledTime<driftRecordRetry||ZoneSystem.instance==null||ZRoutedRpc.instance==null)return;
        try{ZoneSystem.instance.SetGlobalKey(ParkadeRecordKey+" "+DriftLapRecord.Encode(driftRace.BestScore,driftRace.BestTime));driftRecordSaved=driftRace.RecordRevision;}
        catch(Exception error){driftRecordRetry=Time.unscaledTime+5;Logger.LogWarning("Parkade record save deferred: "+error.Message);}
    }
    void UpdateDriftEventPins() {
        if(driftEventMap==Minimap.instance)return;
        RemoveDriftEventPins();driftEventMap=Minimap.instance;
        if(driftEventMap==null)return;
        eventPadPin=driftEventMap.AddPin(new Vector3(DriftEventPad.X,32,DriftEventPad.Z),Minimap.PinType.Icon3,"Parkade Drift 1 | 3 Runden",false,false);
        parkadePin=driftEventMap.AddPin(new Vector3(ParkadeLayout.StartX,40,ParkadeLayout.StartZ),Minimap.PinType.Icon3,"Parkade Drift 1 | Start",false,false);
    }
    void RemoveDriftEventPins() {
        if(driftEventMap!=null){if(eventPadPin!=null)driftEventMap.RemovePin(eventPadPin);if(parkadePin!=null)driftEventMap.RemovePin(parkadePin);}
        driftEventMap=null;eventPadPin=parkadePin=null;
    }
    void ClearDriftEvents() {
        ReleaseDriftRaceHold();if(driftRaceUi)CloseDriftEventUI();
        driftEventPanel?.Clear();driftEventPanel=null;driftEventPad?.Clear();driftEventPad=null;
        parkadeRaceStarter?.Clear();parkadeRaceStarter=null;
        parkadeStartSpectators?.Clear();parkadeStartSpectators=null;parkadeNeonSign?.Clear();parkadeNeonSign=null;
        parkadeFestival?.Clear();parkadeFestival=null;
        parkadeServiceYards?.Clear();parkadeServiceYards=null;
        parkadeMiniMap?.Clear();parkadeMiniMap=null;parkadeStartLine?.Clear();parkadeStartLine=null;RemoveDriftEventPins();
        parkadeVenue?.Clear();parkadeVenue=null;driftRace.Reset();driftEventWorld=null;driftEventGame=null;driftEventVehicle=null;
        driftPadLatched=driftEventFailed=false;driftRecordSaved=0;driftRecordRetry=0;
        driftPauseRequested=false;driftRaceUiOpenedFrame=-1;
        driftPreparationAtStart=false;driftPreparationProgress=0;driftPreparationLog=0;
    }
}
}
