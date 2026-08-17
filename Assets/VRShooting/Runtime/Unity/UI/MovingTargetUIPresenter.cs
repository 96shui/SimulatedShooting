using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Formats Task 5 DTOs and forwards view commands through a replaceable UI port.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovingTargetUIPresenter : MonoBehaviour
    {
        MovingTargetRangeUI view;
        IMovingTargetUIPort port;
        IReadOnlyList<float> availableSpeeds = Array.Empty<float>();
        TrainingPresentationDto presentation;
        bool hasPresentation;
        bool commandInFlight;
        bool subscribed;
        float selectedSpeed = 4f;

        public bool IsInitialized => port != null;

        public float SelectedSpeed => selectedSpeed;

        public string LastError { get; private set; } = string.Empty;

        public int HudRenderCount { get; private set; }

        public int ResultRenderCount { get; private set; }

        public void Initialize(MovingTargetRangeUI targetView, IMovingTargetUIPort uiPort)
        {
            Unsubscribe();
            port?.Dispose();
            availableSpeeds = Array.Empty<float>();
            presentation = default;
            hasPresentation = false;
            commandInFlight = false;
            selectedSpeed = 4f;
            LastError = string.Empty;
            HudRenderCount = 0;
            ResultRenderCount = 0;
            view = targetView ?? throw new ArgumentNullException(nameof(targetView));
            port = uiPort ?? throw new ArgumentNullException(nameof(uiPort));
            Subscribe();
            LoadInitialState();
        }

        void OnDestroy()
        {
            Unsubscribe();
            port?.Dispose();
            port = null;
        }

        void Subscribe()
        {
            if (subscribed || port == null || view == null)
            {
                return;
            }

            port.PresentationChanged += OnPresentationChanged;
            port.HudUpdated += OnHudUpdated;
            port.ResultUpdated += OnResultUpdated;
            foreach (var pair in view.SpeedButtons)
            {
                var speed = pair.Key;
                pair.Value.onClick.AddListener(() => SelectSpeed(speed));
            }
            view.StartButton.onClick.AddListener(RequestStart);
            view.SettingsBackButton.onClick.AddListener(RequestExit);
            view.RetryButton.onClick.AddListener(RequestRetry);
            view.ResultsBackButton.onClick.AddListener(RequestExit);
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (port != null)
            {
                port.PresentationChanged -= OnPresentationChanged;
                port.HudUpdated -= OnHudUpdated;
                port.ResultUpdated -= OnResultUpdated;
            }
            if (view != null)
            {
                foreach (var button in view.SpeedButtons.Values)
                {
                    button.onClick.RemoveAllListeners();
                }
                view.StartButton.onClick.RemoveListener(RequestStart);
                view.SettingsBackButton.onClick.RemoveListener(RequestExit);
                view.RetryButton.onClick.RemoveListener(RequestRetry);
                view.ResultsBackButton.onClick.RemoveListener(RequestExit);
            }
            subscribed = false;
        }

        void LoadInitialState()
        {
            LastError = string.Empty;
            var speeds = port.GetAvailableSpeeds();
            if (!speeds.Success || speeds.Data == null || speeds.Data.Count == 0)
            {
                SetError(speeds.Message ?? "没有可用速度");
                return;
            }

            availableSpeeds = speeds.Data;
            selectedSpeed = ContainsSpeed(availableSpeeds, 4f) ? 4f : availableSpeeds[0];
            view.RenderSettings(selectedSpeed, availableSpeeds);

            var current = port.GetPresentation();
            if (!current.Success)
            {
                SetError(current.Message);
                return;
            }

            ApplyPresentation(current.Data);
        }

        void SelectSpeed(float speed)
        {
            if (commandInFlight || !ContainsSpeed(availableSpeeds, speed)
                || (hasPresentation && presentation.Phase != TrainingPresentationPhase.AwaitingStartConfirmation
                    && presentation.Phase != TrainingPresentationPhase.ModeEntry))
            {
                return;
            }

            selectedSpeed = speed;
            SetError(string.Empty);
            view.RenderSettings(selectedSpeed, availableSpeeds);
        }

        void RequestStart()
        {
            if (commandInFlight || !ContainsSpeed(availableSpeeds, selectedSpeed)
                || (hasPresentation && presentation.Phase != TrainingPresentationPhase.AwaitingStartConfirmation
                    && presentation.Phase != TrainingPresentationPhase.ModeEntry))
            {
                return;
            }

            BeginCommand();
            var result = port.Start(new MovingTargetSettingsDto { SpeedMetersPerSecond = selectedSpeed });
            CompleteCommand(result);
        }

        void RequestRetry()
        {
            if (commandInFlight || !hasPresentation
                || presentation.Phase != TrainingPresentationPhase.SessionResults)
            {
                return;
            }

            BeginCommand();
            var result = port.Retry(presentation.SessionId);
            if (result.Success)
            {
                view.ClearHud();
                view.RenderSettings(selectedSpeed, availableSpeeds);
            }
            CompleteCommand(result);
        }

        void RequestExit()
        {
            if (commandInFlight || !hasPresentation
                || presentation.Phase == TrainingPresentationPhase.Exiting)
            {
                return;
            }

            BeginCommand();
            CompleteCommand(port.Exit(presentation.SessionId));
        }

        void BeginCommand()
        {
            commandInFlight = true;
            SetError(string.Empty);
            view.SetBusy(true);
        }

        void CompleteCommand(ServiceResult<TrainingPresentationDto> result)
        {
            commandInFlight = false;
            if (!result.Success)
            {
                SetError(result.Message);
                if (hasPresentation)
                {
                    view.RenderPresentation(presentation, false);
                }
                return;
            }

            ApplyPresentation(result.Data);
        }

        void OnPresentationChanged(TrainingPresentationDto dto)
        {
            ApplyPresentation(dto);
        }

        void ApplyPresentation(TrainingPresentationDto dto)
        {
            if (dto.Mode != TrainingMode.MovingTarget)
            {
                return;
            }

            presentation = dto;
            hasPresentation = true;
            view.RenderPresentation(dto, commandInFlight);
            if (dto.MinimalHudVisible && !string.IsNullOrEmpty(dto.SessionId))
            {
                var hud = port.GetHud(dto.SessionId);
                if (hud.Success)
                {
                    OnHudUpdated(hud.Data);
                }
            }
            if (dto.ActiveScreen == ScreenId.MovingTargetResults && !string.IsNullOrEmpty(dto.SessionId))
            {
                view.ClearHud();
                var result = port.GetResult(dto.SessionId);
                if (result.Success)
                {
                    OnResultUpdated(result.Data);
                }
            }
        }

        void OnHudUpdated(HudDto dto)
        {
            if (dto.HudType != HudType.MovingTarget
                || (hasPresentation && !string.IsNullOrEmpty(presentation.SessionId)
                    && presentation.SessionId != dto.SessionId))
            {
                return;
            }

            HudRenderCount++;
            view.RenderHud(FormatHud(dto));
        }

        void OnResultUpdated(MovingTargetResultDto dto)
        {
            if (hasPresentation && !string.IsNullOrEmpty(presentation.SessionId)
                && presentation.SessionId != dto.SessionId)
            {
                return;
            }

            ResultRenderCount++;
            view.RenderResult(FormatResult(dto));
        }

        void SetError(string message)
        {
            LastError = message ?? string.Empty;
            view?.RenderError(LastError);
        }

        public static MovingTargetHudViewModel FormatHud(HudDto dto)
        {
            var ammo = FindLine(dto, "ammo");
            var mode = FindLine(dto, "fireMode");
            var hits = FindLine(dto, "hits");
            var progress = FindLine(dto, "progress");
            var speed = FindLine(dto, "speed");
            var direction = FindLine(dto, "direction");
            var countdown = FindLine(dto, "countdown");
            var fireSequence = dto.FireSequence;
            var fireState = fireSequence.HasValue
                ? FormatFireSequence(fireSequence.Value)
                : ValueOrFallback(FindLine(dto, "firePhase"), "待扣动");
            var prompt = dto.Prompts != null && dto.Prompts.Count > 0
                ? dto.Prompts[0].Text
                : (dto.CanShoot ? "可射击" : "禁止射击");
            var countdownText = countdown.Value;
            if (countdownText == "0" && dto.CanShoot)
            {
                countdownText = "开始";
            }

            return new MovingTargetHudViewModel
            {
                FireMode = ValueOrFallback(mode, "两发起射 / 长按连射"),
                Ammo = "弹药 " + ValueOrFallback(ammo, "--"),
                Hits = "命中 " + ValueOrFallback(hits, "--"),
                Progress = "进度 " + ValueOrFallback(progress, "--"),
                Speed = "速度 " + ValueOrFallback(speed, "--"),
                Direction = "方向 " + NormalizeDirection(ValueOrFallback(direction, "--")),
                Countdown = countdownText ?? string.Empty,
                FireState = fireState,
                Prompt = prompt ?? string.Empty,
                CanShoot = dto.CanShoot
            };
        }

        public static MovingTargetResultViewModel FormatResult(MovingTargetResultDto dto)
        {
            var summary = string.Format(CultureInfo.InvariantCulture,
                "评级：{0}\n总射击：{1}    命中：{2}    命中率：{3:0.#}%\n速度：{4:0.#} m/s    用时：{5:0.0} 秒",
                FormatGrade(dto.Grade), dto.TotalShotsFired, dto.HitCount, dto.HitRate01 * 100f,
                dto.SpeedMetersPerSecond, dto.ElapsedSeconds);
            var sequenceText = new StringBuilder();
            var sequences = dto.FireSequences ?? Array.Empty<FireSequenceRecordDto>();
            if (sequences.Count == 0)
            {
                sequenceText.Append("无射击序列");
            }
            for (var sequenceIndex = 0; sequenceIndex < sequences.Count; sequenceIndex++)
            {
                var sequence = sequences[sequenceIndex];
                sequenceText.Append("序列 ").Append(sequenceIndex + 1).Append("：")
                    .Append(sequence.EnteredContinuousFire ? "进入连射" : "快速两发")
                    .Append("  发数 ").Append(sequence.ShotCount)
                    .Append("  命中 ").Append(sequence.HitCount)
                    .Append("  停止：").Append(FormatStopReason(sequence.StopReason)).AppendLine();
                var shots = sequence.Shots ?? Array.Empty<MovingTargetShotRecordDto>();
                for (var shotIndex = 0; shotIndex < shots.Count; shotIndex++)
                {
                    var shot = shots[shotIndex];
                    sequenceText.Append("  #").Append(shot.ShotIndexInSequence)
                        .Append(shot.Hit ? " 命中" : " 未命中")
                        .Append("  路线 ").Append((shot.RouteProgress01 * 100f).ToString("0.#", CultureInfo.InvariantCulture))
                        .AppendLine("%");
                }
            }

            return new MovingTargetResultViewModel { Summary = summary, Sequences = sequenceText.ToString().TrimEnd() };
        }

        static string FormatFireSequence(WeaponFireSequenceStateDto dto)
        {
            switch (dto.Phase)
            {
                case WeaponFireSequencePhase.InitialTwoShots:
                    return "两发起射";
                case WeaponFireSequencePhase.ContinuousFire:
                    return "长按连射";
                case WeaponFireSequencePhase.Stopped:
                    return dto.StopReason.HasValue ? "已停止 · " + FormatStopReason(dto.StopReason.Value) : "已停止";
                default:
                    return "待扣动";
            }
        }

        static string FormatStopReason(WeaponFireStopReason reason)
        {
            switch (reason)
            {
                case WeaponFireStopReason.AmmoDepleted:
                    return "弹药耗尽";
                case WeaponFireStopReason.ShootingBecameForbidden:
                    return "进入禁射";
                case WeaponFireStopReason.TrainingCompleted:
                    return "训练完成";
                case WeaponFireStopReason.WeaponBecameInvalid:
                    return "武器或跟踪失效";
                default:
                    return "释放扳机";
            }
        }

        static string FormatGrade(ResultGrade grade)
        {
            switch (grade)
            {
                case ResultGrade.Excellent:
                    return "优秀";
                case ResultGrade.Good:
                    return "良好";
                case ResultGrade.Pass:
                    return "及格";
                default:
                    return "不及格";
            }
        }

        static HudTextLineDto FindLine(HudDto dto, string key)
        {
            if (dto.TextLines != null)
            {
                for (var i = 0; i < dto.TextLines.Count; i++)
                {
                    if (dto.TextLines[i].Key == key)
                    {
                        return dto.TextLines[i];
                    }
                }
            }
            return default;
        }

        static string ValueOrFallback(HudTextLineDto line, string fallback)
        {
            return string.IsNullOrEmpty(line.Value) ? fallback : line.Value;
        }

        static string NormalizeDirection(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("→", "至");
        }

        static bool ContainsSpeed(IReadOnlyList<float> speeds, float value)
        {
            if (speeds == null)
            {
                return false;
            }
            for (var i = 0; i < speeds.Count; i++)
            {
                if (Math.Abs(speeds[i] - value) < 0.001f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
