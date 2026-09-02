using VRShooting.Application.Events;
using VRShooting.Application.Weapons;

namespace VRShooting.Application
{
    /// <summary>
    /// Keeps completed-session data available on the results screen, then releases it
    /// when presentation navigation leaves that session.
    /// </summary>
    public sealed class TrainingSessionResourceCoordinator
    {
        readonly ZeroingService zeroing;
        readonly MovingTargetService movingTarget;
        readonly WeaponAutomaticFireService automaticFire;
        readonly WeaponControlService weaponControl;
        string retainedSessionId = string.Empty;

        public TrainingSessionResourceCoordinator(
            IGameEventBus eventBus,
            ZeroingService zeroing,
            MovingTargetService movingTarget,
            WeaponAutomaticFireService automaticFire,
            WeaponControlService weaponControl)
        {
            this.zeroing = zeroing;
            this.movingTarget = movingTarget;
            this.automaticFire = automaticFire;
            this.weaponControl = weaponControl;

            eventBus.Subscribe<TrainingPresentationChangedEvent>(OnPresentationChanged);
        }

        void OnPresentationChanged(TrainingPresentationChangedEvent evt)
        {
            var sessionId = evt.Presentation.SessionId;
            if (!string.IsNullOrEmpty(sessionId))
            {
                ReleaseIfReplacedBy(sessionId);
                retainedSessionId = sessionId;
                return;
            }

            ReleaseRetainedSession();
        }

        void ReleaseIfReplacedBy(string sessionId)
        {
            if (string.IsNullOrEmpty(retainedSessionId) || retainedSessionId == sessionId)
            {
                return;
            }

            Release(retainedSessionId);
        }

        void ReleaseRetainedSession()
        {
            if (string.IsNullOrEmpty(retainedSessionId))
            {
                return;
            }

            Release(retainedSessionId);
            retainedSessionId = string.Empty;
        }

        void Release(string sessionId)
        {
            automaticFire.ReleaseSession(sessionId);
            movingTarget.ReleaseSession(sessionId);
            zeroing.ReleaseSession(sessionId);
            weaponControl.ReleaseSession(sessionId);
        }
    }
}
