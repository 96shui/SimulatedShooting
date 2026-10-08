using System;
using VRShooting.Application.Combat;
using VRShooting.Common;

namespace VRShooting.P3.TestSupport
{
    /// <summary>Completes explicit scene steps through the real opening state machine for combat-rule fixtures.</summary>
    public sealed class TrenchOpeningTestDriver : IDisposable
    {
        readonly FakeCombatClock clock;
        readonly FakeDroneReconScenePort scene=new FakeDroneReconScenePort();
        string previous;
        public DroneReconService Service {get;}
        public TrenchOpeningTestDriver(FakeCombatClock clock)
        {this.clock=clock;Service=new DroneReconService(clock,scene);}
        public void StartCombat(TrenchService trench,string id)
        {
            if(previous!=null)Service.Cancel(previous);
            var began=Service.Begin(id);
            if(!began.Success)throw new InvalidOperationException("Opening failed: "+began.ErrorCode);
            previous=id;
            for(var step=0;step<5;step++)
            {
                scene.CompleteCurrentStep();
                clock.Advance(scene.Current.Phase==DroneReconPhase.PlayerTakeoff?3:
                    scene.Current.Phase==DroneReconPhase.DroneLanding?2:.1);
                var result=Service.Advance(id);
                if(!result.Success)throw new InvalidOperationException("Opening step failed: "+result.ErrorCode);
            }
            var combat=trench.BeginCombat(id);
            if(!combat.Success)throw new InvalidOperationException("Combat gate rejected completed opening: "+combat.ErrorCode);
            if(!Service.ConfirmCombatStarted(id).Success)throw new InvalidOperationException("Scene unlock failed");
            if(!trench.CompleteCombatStart(id).Success)throw new InvalidOperationException("Core gate commit failed");
        }
        public void Dispose()=>Service.Dispose();
    }
}
