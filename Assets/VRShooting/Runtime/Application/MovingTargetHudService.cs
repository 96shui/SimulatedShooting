using System;
using System.Collections.Generic;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 移动靶最小 HUD 聚合。追溯 docs/BDD/screens/09-移动靶HUD.feature.md。
    /// </summary>
    public sealed class MovingTargetHudService : IHUDService
    {
        readonly IGameEventBus eventBus;
        readonly ITrainingSessionService trainingSessions;
        readonly IMovingTargetService movingTarget;
        readonly IAmmoService ammo;

        public MovingTargetHudService(
            IGameEventBus eventBus,
            ITrainingSessionService trainingSessions,
            IMovingTargetService movingTarget,
            IAmmoService ammo)
        {
            this.eventBus = eventBus;
            this.trainingSessions = trainingSessions;
            this.movingTarget = movingTarget;
            this.ammo = ammo;

            eventBus.Subscribe<SessionStartedEvent>(evt => PublishIfCurrent(evt.Session.SessionId));
            eventBus.Subscribe<AmmoChangedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<MovingTargetStateChangedEvent>(evt => PublishIfCurrent(evt.Session.SessionId));
            eventBus.Subscribe<MovingTargetShotRecordedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<MovingTargetFireSequenceCompletedEvent>(evt => PublishIfCurrent(evt.SessionId));
        }

        public event Action<HudDto> HudUpdated;

        public ServiceResult<HudDto> GetHud(string sessionId)
        {
            var session = trainingSessions.Current;
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = session.SessionId;
            }

            if (string.IsNullOrEmpty(session.SessionId) || session.SessionId != sessionId)
            {
                return ServiceResult<HudDto>.Fail(ErrorCode.NotFound, "session not found", HudDto.Empty);
            }

            if (session.Mode != TrainingMode.MovingTarget)
            {
                return ServiceResult<HudDto>.Fail(ErrorCode.InvalidInput, "hud only supports moving target mode", HudDto.Empty);
            }

            var moving = ResolveSession(sessionId);
            var ammoDto = ResolveAmmo(session, moving);
            var canShoot = moving.CanShoot && ammoDto.CurrentMagazine > 0 && !ammoDto.IsReloading;
            var fireSequence = BuildFireSequence(sessionId, moving);

            var dto = new HudDto
            {
                SessionId = session.SessionId,
                Mode = session.Mode,
                HudType = HudType.MovingTarget,
                Ammo = ammoDto,
                Player = session.Player,
                MiniMap = MiniMapDto.Hidden,
                TextLines = BuildTextLines(moving, ammoDto, canShoot),
                Prompts = BuildPrompts(moving, canShoot),
                CanShoot = canShoot,
                FireSequence = fireSequence
            };

            return ServiceResult<HudDto>.Ok(dto);
        }

        void PublishIfCurrent(string sessionId)
        {
            var result = GetHud(sessionId);
            if (!result.Success)
            {
                return;
            }

            HudUpdated?.Invoke(result.Data);
            eventBus.Publish(new HudUpdatedEvent { Hud = result.Data });
        }

        MovingTargetSessionDto ResolveSession(string sessionId)
        {
            var result = movingTarget.GetSession(sessionId);
            return result.Success ? result.Data : MovingTargetSessionDto.Empty;
        }

        AmmoDto ResolveAmmo(TrainingSessionDto session, MovingTargetSessionDto moving)
        {
            var result = ammo.GetAmmo(session.SessionId);
            if (result.Success)
            {
                return result.Data;
            }

            var remaining = Math.Max(0, MovingTargetRules.TotalAmmo - moving.ShotsFired);
            return new AmmoDto
            {
                CurrentMagazine = remaining,
                ReserveAmmo = 0,
                MagazineCapacity = MovingTargetRules.TotalAmmo,
                IsReloading = false
            };
        }

        static WeaponFireSequenceStateDto BuildFireSequence(string sessionId, MovingTargetSessionDto moving)
        {
            return new WeaponFireSequenceStateDto
            {
                SessionId = sessionId,
                SequenceId = string.Empty,
                FireMode = WeaponFireMode.InitialTwoThenAutomatic,
                Phase = moving.FirePhase,
                ShotsFired = moving.ShotsFired,
                TriggerHeld = moving.FirePhase == WeaponFireSequencePhase.ContinuousFire
                              || moving.FirePhase == WeaponFireSequencePhase.InitialTwoShots,
                TriggerArmedForNewSequence = moving.FirePhase == WeaponFireSequencePhase.Idle
                                             || moving.FirePhase == WeaponFireSequencePhase.Stopped,
                StopReason = moving.FirePhase == WeaponFireSequencePhase.Stopped
                    ? WeaponFireStopReason.TriggerReleased
                    : (WeaponFireStopReason?)null
            };
        }

        static IReadOnlyList<HudTextLineDto> BuildTextLines(
            MovingTargetSessionDto moving,
            AmmoDto ammoDto,
            bool canShoot)
        {
            var capacity = ammoDto.MagazineCapacity > 0 ? ammoDto.MagazineCapacity : MovingTargetRules.TotalAmmo;
            return new[]
            {
                Line("ammo", "弹药", ammoDto.CurrentMagazine + "/" + capacity, ammoDto.CurrentMagazine == 0 ? HudSeverity.Warning : HudSeverity.Normal),
                Line("fireMode", "射击模式", "两发起射 / 长按连射", HudSeverity.Info),
                Line("firePhase", "连射状态", FormatFirePhase(moving.FirePhase), HudSeverity.Normal),
                Line("hits", "命中", moving.HitCount + "/" + MovingTargetRules.TotalAmmo, HudSeverity.Normal),
                Line("speed", "速度", FormatSpeed(moving.SpeedMetersPerSecond), HudSeverity.Info),
                Line("direction", "方向", string.IsNullOrEmpty(moving.DirectionLabel) ? "等待" : moving.DirectionLabel, HudSeverity.Info),
                Line("countdown", "倒计时", FormatCountdown(moving), HudSeverity.Warning),
                Line("shootState", "射击状态", canShoot ? "可射击" : "禁止射击", canShoot ? HudSeverity.Success : HudSeverity.Warning)
            };
        }

        static IReadOnlyList<HudPromptDto> BuildPrompts(MovingTargetSessionDto moving, bool canShoot)
        {
            var text = "可射击";
            if (moving.Phase == TargetMovePhase.WaitingCountdown)
            {
                text = "等待开始";
            }
            else if (moving.Phase == TargetMovePhase.LeftEndpointHold)
            {
                text = "端点停留禁射";
            }
            else if (moving.Phase == TargetMovePhase.Completed)
            {
                text = "训练结束";
            }
            else if (!canShoot)
            {
                text = "禁止射击";
            }

            return new[]
            {
                new HudPromptDto
                {
                    PromptId = "moving-target-center",
                    Text = text,
                    IsInteractive = false,
                    IsEnabled = canShoot
                }
            };
        }

        static string FormatFirePhase(WeaponFireSequencePhase phase)
        {
            switch (phase)
            {
                case WeaponFireSequencePhase.InitialTwoShots:
                    return "两发起射";
                case WeaponFireSequencePhase.ContinuousFire:
                    return "长按连射";
                case WeaponFireSequencePhase.Stopped:
                    return "已停止";
                default:
                    return "待扣动";
            }
        }

        static string FormatSpeed(float speed)
        {
            if (speed <= 0f)
            {
                return MovingTargetRules.DefaultSpeedMetersPerSecond + "m/s";
            }

            return speed + "m/s";
        }

        static string FormatCountdown(MovingTargetSessionDto moving)
        {
            if (moving.Phase != TargetMovePhase.WaitingCountdown)
            {
                return "0";
            }

            return Math.Ceiling(moving.CountdownSecondsRemaining).ToString("0");
        }

        static HudTextLineDto Line(string key, string label, string value, HudSeverity severity)
        {
            return new HudTextLineDto
            {
                Key = key,
                Label = label,
                Value = value,
                Severity = severity
            };
        }
    }
}
