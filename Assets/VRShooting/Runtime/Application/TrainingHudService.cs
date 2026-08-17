using System;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 按训练模式分发 HUD。P1 射校与 P2 移动靶共用同一 IHUDService 入口。
    /// </summary>
    public sealed class TrainingHudService : IHUDService
    {
        readonly ITrainingSessionService trainingSessions;
        readonly ZeroingHudService zeroingHud;
        readonly MovingTargetHudService movingTargetHud;

        public TrainingHudService(
            ITrainingSessionService trainingSessions,
            ZeroingHudService zeroingHud,
            MovingTargetHudService movingTargetHud)
        {
            this.trainingSessions = trainingSessions;
            this.zeroingHud = zeroingHud;
            this.movingTargetHud = movingTargetHud;

            zeroingHud.HudUpdated += Forward;
            movingTargetHud.HudUpdated += Forward;
        }

        public event Action<HudDto> HudUpdated;

        public ServiceResult<HudDto> GetHud(string sessionId)
        {
            var session = trainingSessions.Current;
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = session.SessionId;
            }

            if (session.Mode == TrainingMode.MovingTarget)
            {
                return movingTargetHud.GetHud(sessionId);
            }

            return zeroingHud.GetHud(sessionId);
        }

        void Forward(HudDto dto)
        {
            HudUpdated?.Invoke(dto);
        }
    }
}
