using System;
using System.Collections.Generic;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Unity.UI;

namespace VRShooting.Unity.Bootstrap
{
    /// <summary>
    /// Composition-root adapter between the DTO-only Task 5 UI port and application services.
    /// </summary>
    public sealed class MovingTargetUICommandAdapter : IMovingTargetUIPort
    {
        readonly ApplicationServices services;
        readonly IDisposable completionSubscription;
        bool disposed;

        public MovingTargetUICommandAdapter(ApplicationServices applicationServices)
        {
            services = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
            services.Presentation.PresentationChanged += OnPresentationChanged;
            services.Hud.HudUpdated += OnHudUpdated;
            completionSubscription = services.EventBus.Subscribe<MovingTargetSessionCompletedEvent>(OnCompleted);
        }

        public event Action<TrainingPresentationDto> PresentationChanged;

        public event Action<HudDto> HudUpdated;

        public event Action<MovingTargetResultDto> ResultUpdated;

        public ServiceResult<IReadOnlyList<float>> GetAvailableSpeeds()
        {
            return services.MovingTarget.GetAvailableSpeeds();
        }

        public ServiceResult<TrainingPresentationDto> GetPresentation()
        {
            var result = services.Presentation.Get(string.Empty);
            if (result.Success || result.ErrorCode != ErrorCode.NotFound)
            {
                return result;
            }

            return services.Presentation.Enter(TrainingMode.MovingTarget);
        }

        public ServiceResult<HudDto> GetHud(string sessionId)
        {
            return services.Hud.GetHud(sessionId);
        }

        public ServiceResult<MovingTargetResultDto> GetResult(string sessionId)
        {
            return services.MovingTarget.CompleteSession(sessionId);
        }

        public ServiceResult<TrainingPresentationDto> Start(MovingTargetSettingsDto settings)
        {
            var started = services.MovingTarget.StartSession(settings, RandomSeed.Fixed(205));
            if (!started.Success)
            {
                return ServiceResult<TrainingPresentationDto>.Fail(
                    started.ErrorCode,
                    started.Message,
                    TrainingPresentationDto.Empty);
            }

            return services.Presentation.ConfirmStart(started.Data.SessionId);
        }

        public ServiceResult<TrainingPresentationDto> Retry(string sessionId)
        {
            return services.Presentation.Retry(sessionId);
        }

        public ServiceResult<TrainingPresentationDto> Exit(string sessionId)
        {
            var result = services.Presentation.Exit(sessionId);
            if (!result.Success)
            {
                return result;
            }

            services.Router.Open(ScreenId.MainMenu, new NavigationArgs
            {
                Mode = TrainingMode.MovingTarget,
                ReturnToScreen = ScreenId.MainMenu.ToString()
            });
            var gameState = GameMain.Instance?.GameState;
            if (gameState != null)
            {
                gameState.ChangeState(GameState.MainMenu);
            }

            return result;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            services.Presentation.PresentationChanged -= OnPresentationChanged;
            services.Hud.HudUpdated -= OnHudUpdated;
            completionSubscription?.Dispose();
        }

        void OnPresentationChanged(TrainingPresentationDto dto)
        {
            if (dto.Mode == TrainingMode.MovingTarget)
            {
                services.Router.Open(dto.ActiveScreen, new NavigationArgs
                {
                    Mode = TrainingMode.MovingTarget,
                    ReturnToScreen = ScreenId.MainMenu.ToString()
                });
                PresentationChanged?.Invoke(dto);
            }
        }

        void OnHudUpdated(HudDto dto)
        {
            if (dto.HudType == HudType.MovingTarget)
            {
                HudUpdated?.Invoke(dto);
            }
        }

        void OnCompleted(MovingTargetSessionCompletedEvent evt)
        {
            ResultUpdated?.Invoke(evt.Result);
        }
    }
}
