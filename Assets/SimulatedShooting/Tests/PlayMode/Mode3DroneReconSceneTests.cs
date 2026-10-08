using System.Collections;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD28, scene task017: actual trench bindings, camera output, ordered movement and cleanup.
    public sealed class Mode3DroneReconSceneTests
    {
        sealed class Tick : ICombatTickPort
        {public ServiceResult<Unit> Advance()=>ServiceResult<Unit>.Ok(Unit.Value);}
        static DroneReconStepCommandDto Step(DroneReconStepCommandDto source,DroneReconPhase phase)
            =>new DroneReconStepCommandDto {SessionId=source.SessionId,SequenceId=source.SequenceId,
                StepId=phase.ToString(),Phase=phase,RouteId=source.RouteId,FeedBindingId=source.FeedBindingId,
                DurationSeconds=2,FlightSpeedMetersPerSecond=source.FlightSpeedMetersPerSecond};
        [UnityTest]
        public IEnumerator PreparingSceneCompletesFullFlightAndRestoresPlayerViewBeforeCombat()
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            var load=new UnityCombatSceneLoader(()=>false).LoadAsync(TrainingMode.Trench,CancellationToken.None);
            while(!load.IsCompleted)yield return null;
            Assert.That(load.Result.Success,Is.True,load.Result.Message);
            var lease=load.Result.Data;
            TrenchService trench=null;
            try
            {
                var runtime=(CombatSceneRuntime)lease.Navigation;runtime.ManualStepping=true;
                Assert.That(runtime.DroneRecon.Video,Is.Null,"Inspecting the scene cannot start the feed or flight.");
                trench=new TrenchService(lease.Definition,lease.Clock,lease.Random,lease.Navigation);
                var preparation=trench.PrepareSession(lease.Definition.MapId,P3ContractIds.TrainingWeapon,RandomSeed.Fixed(42));
                Assert.That(preparation.Success,Is.True);
                var id=preparation.Data.SessionId;
                Assert.That(lease.Activate(trench.Combat,trench,trench,trench,trench.SquadCommands,
                    trench.GrenadeTactics,new Tick(),id).Success,Is.True);
                var adapter=runtime.DroneRecon;
                Assert.That(adapter.SetCharacterActionsLocked(id,true).Success,Is.True);
                var command=new DroneReconStepCommandDto {SessionId=id,SequenceId="scene-test",StepId="takeoff",
                    Phase=DroneReconPhase.PlayerTakeoff,RouteId="mode3.trench-recon",FeedBindingId="mode3.drone-feed",
                    DurationSeconds=3,FlightSpeedMetersPerSecond=5};
                Assert.That(adapter.Prepare(command).Success,Is.True);
                Assert.That(adapter.Route.Count,Is.GreaterThan(1));
                Assert.That(adapter.Video.IsCreated(),Is.True);
                Assert.That(adapter.CaptureCamera.targetTexture,Is.SameAs(adapter.Video));
                Assert.That(adapter.CaptureCamera.GetComponent<AudioListener>(),Is.Null);
                Assert.That(runtime.Actors.Values.All(a=>a.ActionsLocked),Is.True);
                var complete=0;
                string failure=null;
                adapter.FactReceived+=fact=>{
                    if(fact.Kind==DroneReconFactKind.StepCompleted)complete++;
                    if(fact.Kind==DroneReconFactKind.StepFailed)failure=fact.Reason;
                };
                var drone=adapter.CaptureCamera.transform.parent;
                var rotorSound=drone.GetComponent<AudioSource>();
                Assert.That(rotorSound,Is.Not.Null);
                Assert.That(rotorSound.clip,Is.Not.Null);
                Assert.That(rotorSound.clip.name,Is.EqualTo("DroneRotor_Mavic_Recorded"));
                Assert.That(rotorSound.loop,Is.True);
                Assert.That(rotorSound.volume,Is.GreaterThanOrEqualTo(.5f));
                Assert.That(rotorSound.rolloffMode,Is.EqualTo(AudioRolloffMode.Linear));
                Assert.That(rotorSound.maxDistance,Is.GreaterThanOrEqualTo(40));
                var launchPosition=drone.position;
                var playerPosition=runtime.PlayerCamera.transform.position;
                Assert.That(adapter.PlayStep(command).Success,Is.True);
                Assert.That(rotorSound.isPlaying,Is.True,"BDD28: rotor sound starts on takeoff.");
                for(var i=0;i<59;i++)adapter.AdvanceFlight(.05f);
                Assert.That(complete,Is.Zero);
                adapter.AdvanceFlight(.06f);
                Assert.That(complete,Is.EqualTo(1));
                Assert.That(runtime.PlayerCamera.enabled,Is.True,"Takeoff stays in the player's view.");
                Assert.That(trench.GetSession(id).Data.State,Is.EqualTo(SessionState.Preparing));
                command=Step(command,DroneReconPhase.DroneRecon);
                Assert.That(adapter.PlayStep(command).Success,Is.True);
                Assert.That(adapter.SetSuspended(id,true).Success,Is.True);
                var pausedPosition=drone.position;
                adapter.AdvanceFlight(1);
                Assert.That(drone.position,Is.EqualTo(pausedPosition));
                Assert.That(complete,Is.EqualTo(1));
                Assert.That(adapter.SetSuspended(id,false).Success,Is.True);
                var cameraRotation=adapter.CaptureCamera.transform.rotation;
                var cameraOffset=adapter.CaptureCamera.transform.position-drone.position;
                // Advance the complete adapter flight in isolation; the service owns the 20-second deadline.
                var routeLength=Vector3.Distance(drone.position,adapter.Route[0]);
                for(var i=1;i<adapter.Route.Count;i++)routeLength+=Vector3.Distance(adapter.Route[i-1],adapter.Route[i]);
                var maxFrames=Mathf.CeilToInt((routeLength/.5f+10)/.05f);
                foreach(var phase in new[]{DroneReconPhase.DroneRecon,DroneReconPhase.DroneReturn,DroneReconPhase.DroneLanding})
                {
                    if(phase!=DroneReconPhase.DroneRecon)
                    {
                        command=Step(command,phase);
                        Assert.That(adapter.PlayStep(command).Success,Is.True);
                    }
                    var beforeCompletion=complete;
                    for(var frame=0;frame<maxFrames&&adapter.IsFlying;frame++)
                    {
                        adapter.AdvanceFlight(.05f);
                        Assert.That(Quaternion.Angle(adapter.CaptureCamera.transform.rotation,cameraRotation),Is.LessThan(.01f));
                        Assert.That(Vector3.Distance(adapter.CaptureCamera.transform.position-drone.position,cameraOffset),Is.LessThan(.001f));
                        if(frame%100==0)yield return null;
                    }
                    Assert.That(failure,Is.Null,phase+": "+failure);
                    Assert.That(adapter.IsFlying,Is.False,"Flight must complete within its route-derived bound: "+phase);
                    Assert.That(complete,Is.EqualTo(beforeCompletion+1),"Exactly one completion fact: "+phase);
                    Assert.That(runtime.Actors.Values.All(a=>a.ActionsLocked),Is.True);
                    Assert.That(trench.GetSession(id).Data.State,Is.EqualTo(SessionState.Preparing));
                }
                Assert.That(Vector3.Distance(drone.position,launchPosition),Is.LessThan(.02f));
                Assert.That(rotorSound.isPlaying,Is.False,"BDD28: landing stops the rotor sound.");
                command=Step(command,DroneReconPhase.RestoringPlayerView);
                Assert.That(adapter.RestorePlayerView(command).Success,Is.True);
                Assert.That(complete,Is.EqualTo(5));
                Assert.That(adapter.CaptureCamera.enabled,Is.False);
                Assert.That(runtime.PlayerCamera.enabled,Is.True);
                Assert.That(runtime.PlayerCamera.transform.position,Is.EqualTo(playerPosition));
                Assert.That(runtime.Actors.Values.All(a=>a.ActionsLocked),Is.True,
                    "Scene completion cannot unlock actors without the application commit.");
                Assert.That(adapter.StopAndReset(id,"scene-test").Success,Is.True);
                Assert.That(adapter.Video,Is.Null);
                Assert.That(adapter.IsFlying,Is.False);
            }
            finally {trench?.Dispose();lease.Dispose();}
            yield return null;
        }
    }
}
