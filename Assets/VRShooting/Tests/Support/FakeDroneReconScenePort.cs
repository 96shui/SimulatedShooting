using System;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.P3.TestSupport
{
    /// <summary>Scene facts remain explicit: loading or activating does not complete an opening.</summary>
    public sealed class FakeDroneReconScenePort : IDroneReconScenePort
    {
        public DroneReconStepCommandDto Current {get;private set;}
        public bool Locked {get;private set;}=true;
        public bool Suspended {get;private set;}
        public event Action<DroneReconSceneFactDto> FactReceived;
        public ServiceResult<Unit> Prepare(DroneReconStepCommandDto value)=>Ok();
        public ServiceResult<Unit> PlayStep(DroneReconStepCommandDto value){Current=value;return Ok();}
        public ServiceResult<Unit> SetCharacterActionsLocked(string id,bool value){Locked=value;return Ok();}
        public ServiceResult<Unit> SetSuspended(string id,bool value){Suspended=value;return Ok();}
        public ServiceResult<Unit> RestorePlayerView(DroneReconStepCommandDto value){Current=value;return Ok();}
        public ServiceResult<Unit> StopAndReset(string id,string sequence){Locked=true;return Ok();}
        public void CompleteCurrentStep()
        {
            if(Suspended)return;
            Report(DroneReconFactKind.FeedState);Report(DroneReconFactKind.StepCompleted);
        }
        void Report(DroneReconFactKind kind)=>FactReceived?.Invoke(new DroneReconSceneFactDto {
            SessionId=Current.SessionId,SequenceId=Current.SequenceId,StepId=Current.StepId,Phase=Current.Phase,
            Kind=kind,FeedAvailable=true,FeedBindingId=Current.FeedBindingId });
        static ServiceResult<Unit> Ok()=>ServiceResult<Unit>.Ok(Unit.Value);
    }
}
