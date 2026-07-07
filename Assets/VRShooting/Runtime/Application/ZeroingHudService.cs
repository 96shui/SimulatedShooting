using System;
using System.Collections.Generic;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public sealed class ZeroingHudService : IHUDService
    {
        const int TotalRounds = 3;
        const int ShotsPerRound = 3;
        const string DistanceLabel = "100m";

        readonly IGameEventBus eventBus;
        readonly ITrainingSessionService trainingSessions;
        readonly IAmmoService ammo;
        readonly IWeaponControlService weaponControl;
        readonly List<WeaponShotResultDto> impactRecords = new List<WeaponShotResultDto>();
        int currentRound = 1;

        public ZeroingHudService(
            IGameEventBus eventBus,
            ITrainingSessionService trainingSessions,
            IAmmoService ammo,
            IWeaponControlService weaponControl)
        {
            this.eventBus = eventBus;
            this.trainingSessions = trainingSessions;
            this.ammo = ammo;
            this.weaponControl = weaponControl;

            eventBus.Subscribe<SessionStartedEvent>(OnSessionStarted);
            eventBus.Subscribe<AmmoChangedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<ReloadStartedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<ReloadCompletedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<ShoulderChangedEvent>(evt => PublishIfCurrent(evt.SessionId));
            eventBus.Subscribe<WeaponStateChangedEvent>(evt => PublishIfCurrent(evt.State.SessionId));
            eventBus.Subscribe<WeaponShotResultEvent>(OnShotResult);
            eventBus.Subscribe<ZeroingRoundStartedEvent>(OnZeroingRoundStarted);
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

            var ammoDto = ResolveAmmo(session);
            var weaponState = ResolveWeaponState(session.SessionId);
            var canShoot = weaponState.HasValue ? weaponState.Value.CanShoot : ammoDto.CurrentMagazine > 0 && !ammoDto.IsReloading;
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
                TextLines = BuildTextLines(ammoDto, shoulder, stability01, canShoot),
                Prompts = BuildPrompts(ammoDto, stability01, canShoot),
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

            impactRecords.Clear();
            currentRound = 1;
            PublishIfCurrent(evt.Session.SessionId);
        }

        void OnZeroingRoundStarted(ZeroingRoundStartedEvent evt)
        {
            currentRound = evt.Session.CurrentRound;
            impactRecords.Clear();
            PublishIfCurrent(evt.SessionId);
        }

        void OnShotResult(WeaponShotResultEvent evt)
        {
            if (!evt.Result.IsValidShot)
            {
                PublishIfCurrent(evt.Result.SessionId);
                return;
            }

            impactRecords.Add(evt.Result);
            PublishIfCurrent(evt.Result.SessionId);
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

        IReadOnlyList<HudTextLineDto> BuildTextLines(
            AmmoDto ammoDto,
            ShoulderSide shoulder,
            float stability01,
            bool canShoot)
        {
            return new[]
            {
                Line("round", "轮次", currentRound + "/" + TotalRounds, HudSeverity.Normal),
                Line("distance", "距离", DistanceLabel, HudSeverity.Info),
                Line("ammo", "弹数", FormatAmmo(ammoDto), ammoDto.CurrentMagazine == 0 ? HudSeverity.Warning : HudSeverity.Normal),
                Line("stability", "稳定度", Math.Round(Clamp01(stability01) * 100f) + "%", stability01 >= 0.7f ? HudSeverity.Success : HudSeverity.Warning),
                Line("impactRecord", "弹着记录", FormatImpactRecord(), HudSeverity.Normal),
                Line("shoulder", "肩侧", shoulder == ShoulderSide.Left ? "左肩" : "右肩", HudSeverity.Info),
                Line("shootState", "射击状态", canShoot ? "可射击" : "禁止射击", canShoot ? HudSeverity.Success : HudSeverity.Warning)
            };
        }

        IReadOnlyList<HudPromptDto> BuildPrompts(AmmoDto ammoDto, float stability01, bool canShoot)
        {
            var text = "稳定据枪";
            var enabled = true;

            if (ammoDto.IsReloading)
            {
                text = "换弹中";
                enabled = false;
            }
            else if (!canShoot)
            {
                text = "禁止射击：弹数为0";
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

        string FormatImpactRecord()
        {
            if (impactRecords.Count == 0)
            {
                return "待记录 3 发";
            }

            var lines = new List<string> { $"已记录 {Math.Min(impactRecords.Count, ShotsPerRound)}/{ShotsPerRound}" };
            for (var i = 0; i < impactRecords.Count && i < ShotsPerRound; i++)
            {
                lines.Add($"#{i + 1:00} {(impactRecords[i].Hit ? "命中" : "未命中")}");
            }

            return string.Join("\n", lines);
        }

        static string FormatAmmo(AmmoDto ammoDto)
        {
            var capacity = ammoDto.MagazineCapacity > 0 ? ammoDto.MagazineCapacity : ShotsPerRound;
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
