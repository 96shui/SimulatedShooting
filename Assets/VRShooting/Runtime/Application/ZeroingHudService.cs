using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public sealed class ZeroingHudService : IHUDService
    {
        const string DistanceLabel = "100m";

        readonly IGameEventBus eventBus;
        readonly ITrainingSessionService trainingSessions;
        readonly IZeroingService zeroing;
        readonly IAmmoService ammo;
        readonly IWeaponControlService weaponControl;
        readonly List<ZeroingShotDto> currentRoundShots = new List<ZeroingShotDto>();

        public ZeroingHudService(
            IGameEventBus eventBus,
            ITrainingSessionService trainingSessions,
            IZeroingService zeroing,
            IAmmoService ammo,
            IWeaponControlService weaponControl)
        {
            this.eventBus = eventBus;
            this.trainingSessions = trainingSessions;
            this.zeroing = zeroing;
            this.ammo = ammo;
            this.weaponControl = weaponControl;

            eventBus.Subscribe<SessionStartedEvent>(OnSessionStarted);
            eventBus.Subscribe<AmmoChangedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<ReloadStartedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<ReloadCompletedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<ShoulderChangedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<WeaponStateChangedEvent>(evt => PublishIfCurrent(evt.State.SessionId));
            eventBus.Subscribe<ZeroingShotRecordedEvent>(OnZeroingShotRecorded);
            eventBus.Subscribe<ZeroingRoundStartedEvent>(OnZeroingRoundStarted);
            eventBus.Subscribe<ZeroingRoundCompletedEvent>(evt => PublishIfCurrent(evt.SessionId));
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

            if (session.Mode != TrainingMode.Zeroing100m)
            {
                return ServiceResult<HudDto>.Fail(ErrorCode.InvalidInput, "hud only supports zeroing mode", HudDto.Empty);
            }

            var zeroingSession = ResolveZeroingSession(sessionId);
            var ammoDto = ResolveAmmo(session);
            var weaponState = ResolveWeaponState(session.SessionId);
            var canShoot = ResolveCanShoot(zeroingSession, ammoDto, weaponState);
            var shoulder = weaponState.HasValue ? weaponState.Value.ShoulderSide : session.Player.Shoulder;
            var stability01 = weaponState.HasValue ? weaponState.Value.Stability01 : 1f;

            var dto = new HudDto
            {
                SessionId = session.SessionId,
                Mode = session.Mode,
                HudType = HudType.Zeroing,
                Ammo = ammoDto,
                Player = new PlayerStatusDto
                {
                    Health = session.Player.Health,
                    IsAlive = session.Player.IsAlive,
                    Posture = session.Player.Posture,
                    Shoulder = shoulder,
                    CornerShootingAvailable = session.Player.CornerShootingAvailable
                },
                MiniMap = MiniMapDto.Hidden,
                TextLines = BuildTextLines(zeroingSession, ammoDto, shoulder, stability01, canShoot),
                Prompts = BuildPrompts(zeroingSession, ammoDto, stability01, canShoot),
                CanShoot = canShoot
            };

            return ServiceResult<HudDto>.Ok(dto);
        }

        void OnSessionStarted(SessionStartedEvent evt)
        {
            if (evt.Session.Mode != TrainingMode.Zeroing100m)
            {
                return;
            }

            currentRoundShots.Clear();
            PublishIfCurrent(evt.Session.SessionId);
        }

        void OnZeroingRoundStarted(ZeroingRoundStartedEvent evt)
        {
            currentRoundShots.Clear();
            PublishIfCurrent(evt.SessionId);
        }

        void OnZeroingShotRecorded(ZeroingShotRecordedEvent evt)
        {
            if (evt.Shot.RoundIndex > 0 && currentRoundShots.Count > 0 &&
                currentRoundShots[0].RoundIndex != evt.Shot.RoundIndex)
            {
                currentRoundShots.Clear();
            }

            currentRoundShots.Add(evt.Shot);
            PublishIfCurrent(evt.SessionId);
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

        ZeroingSessionDto ResolveZeroingSession(string sessionId)
        {
            var result = zeroing.GetSession(sessionId);
            return result.Success ? result.Data : ZeroingSessionDto.Empty;
        }

        AmmoDto ResolveAmmo(TrainingSessionDto session)
        {
            var result = ammo.GetAmmo(session.SessionId);
            return result.Success ? result.Data : session.Ammo;
        }

        WeaponControlStateDto? ResolveWeaponState(string sessionId)
        {
            var result = weaponControl.GetState(sessionId);
            return result.Success ? result.Data : (WeaponControlStateDto?)null;
        }

        static bool ResolveCanShoot(
            ZeroingSessionDto zeroingSession,
            AmmoDto ammoDto,
            WeaponControlStateDto? weaponState)
        {
            if (!zeroingSession.CanShoot)
            {
                return false;
            }

            if (ammoDto.IsReloading || ammoDto.CurrentMagazine <= 0)
            {
                return false;
            }

            return !weaponState.HasValue || weaponState.Value.CanShoot;
        }

        IReadOnlyList<HudTextLineDto> BuildTextLines(
            ZeroingSessionDto zeroingSession,
            AmmoDto ammoDto,
            ShoulderSide shoulder,
            float stability01,
            bool canShoot)
        {
            var maxRounds = zeroingSession.MaxRounds > 0 ? zeroingSession.MaxRounds : ZeroingRules.MaxRounds;
            var currentRound = zeroingSession.CurrentRound > 0 ? zeroingSession.CurrentRound : 1;

            return new[]
            {
                Line("round", "轮次", currentRound + "/" + maxRounds, HudSeverity.Normal),
                Line("distance", "距离", FormatDistance(zeroingSession), HudSeverity.Info),
                Line("ammo", "弹数", FormatAmmo(ammoDto), ammoDto.CurrentMagazine == 0 ? HudSeverity.Warning : HudSeverity.Normal),
                Line("stability", "稳定度", Math.Round(Clamp01(stability01) * 100f) + "%", stability01 >= 0.7f ? HudSeverity.Success : HudSeverity.Warning),
                Line("impactRecord", "弹着记录", FormatImpactRecord(currentRoundShots), HudSeverity.Normal),
                Line("shoulder", "肩侧", shoulder == ShoulderSide.Left ? "左肩" : "右肩", HudSeverity.Info),
                Line("shootState", "射击状态", canShoot ? "可射击" : "禁止射击", canShoot ? HudSeverity.Success : HudSeverity.Warning)
            };
        }

        IReadOnlyList<HudPromptDto> BuildPrompts(
            ZeroingSessionDto zeroingSession,
            AmmoDto ammoDto,
            float stability01,
            bool canShoot)
        {
            var text = "稳定据枪";
            var enabled = true;

            if (ammoDto.IsReloading)
            {
                text = "换弹中";
                enabled = false;
            }
            else if (!zeroingSession.CanShoot)
            {
                text = "本轮射击已完成";
                enabled = false;
            }
            else if (ammoDto.CurrentMagazine <= 0)
            {
                text = "禁止射击：弹数为0";
                enabled = false;
            }
            else if (!canShoot)
            {
                text = "禁止射击";
                enabled = false;
            }
            else if (stability01 < 0.7f)
            {
                text = "调整据枪";
            }

            return new[]
            {
                new HudPromptDto
                {
                    PromptId = "zeroing-center",
                    Text = text,
                    IsInteractive = false,
                    IsEnabled = enabled
                }
            };
        }

        static string FormatDistance(ZeroingSessionDto zeroingSession)
        {
            if (zeroingSession.DistanceMeters <= 0f)
            {
                return DistanceLabel;
            }

            return Mathf.Approximately(zeroingSession.DistanceMeters, 100f)
                ? DistanceLabel
                : zeroingSession.DistanceMeters + "m";
        }

        static string FormatImpactRecord(IReadOnlyList<ZeroingShotDto> shots)
        {
            if (shots == null || shots.Count == 0)
            {
                return "待记录 3 发";
            }

            var lines = new List<string> { $"已记录 {Math.Min(shots.Count, ZeroingRules.ShotsPerRound)}/{ZeroingRules.ShotsPerRound}" };
            for (var i = 0; i < shots.Count && i < ZeroingRules.ShotsPerRound; i++)
            {
                var shot = shots[i];
                var label = shot.InsideTenRing ? "10环" : "命中";
                lines.Add($"#{shot.ShotIndex:00} {label}");
            }

            return string.Join("\n", lines);
        }

        static string FormatAmmo(AmmoDto ammoDto)
        {
            var capacity = ammoDto.MagazineCapacity > 0 ? ammoDto.MagazineCapacity : ZeroingRules.ShotsPerRound;
            return ammoDto.CurrentMagazine + "/" + capacity;
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

        static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
