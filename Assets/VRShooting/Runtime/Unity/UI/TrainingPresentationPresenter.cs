using System;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Subscribes to presentation snapshots, queries the canonical current state, and forwards commands.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrainingPresentationPresenter : MonoBehaviour
    {
        TrainingPresentationView view;
        ITrainingPresentationService service;
        bool subscribed;
        bool hasSnapshot;
        bool commandInFlight;
        TrainingPresentationDto commandOrigin;

        public TrainingPresentationDto Current { get; private set; }

        public string LastError { get; private set; } = string.Empty;

        public int AppliedSnapshotCount { get; private set; }

        public event Action<TrainingPresentationDto> SnapshotApplied;

        public void Initialize(TrainingPresentationView presentationView, ITrainingPresentationService presentationService)
        {
            Unsubscribe();
            view = presentationView;
            service = presentationService;
            LastError = string.Empty;
            if (isActiveAndEnabled)
            {
                Subscribe();
                Refresh();
            }
        }

        public void Refresh()
        {
            if (service == null)
            {
                return;
            }

            var result = service.Get(hasSnapshot ? Current.SessionId : string.Empty);
            if (result.Success)
            {
                ApplySnapshot(result.Data);
            }
            else if (result.ErrorCode != ErrorCode.NotFound)
            {
                LastError = result.Message;
            }
        }

        void OnEnable()
        {
            if (service != null)
            {
                Subscribe();
                Refresh();
            }
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (subscribed || service == null)
            {
                return;
            }

            service.PresentationChanged += OnPresentationChanged;
            if (view != null)
            {
                if (view.NextRoundButton != null)
                {
                    view.NextRoundButton.onClick.AddListener(RequestNextRound);
                }

                if (view.RetryButton != null)
                {
                    view.RetryButton.onClick.AddListener(RequestRetry);
                }
            }

            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (service != null)
            {
                service.PresentationChanged -= OnPresentationChanged;
            }

            if (view != null)
            {
                if (view.NextRoundButton != null)
                {
                    view.NextRoundButton.onClick.RemoveListener(RequestNextRound);
                }

                if (view.RetryButton != null)
                {
                    view.RetryButton.onClick.RemoveListener(RequestRetry);
                }
            }

            subscribed = false;
        }

        void OnPresentationChanged(TrainingPresentationDto eventSnapshot)
        {
            if (service == null)
            {
                return;
            }

            var canonical = service.Get(eventSnapshot.SessionId);
            if (!canonical.Success)
            {
                return;
            }

            ApplySnapshot(canonical.Data);
        }

        void RequestNextRound()
        {
            if (service == null || commandInFlight || !hasSnapshot
                || Current.Phase != TrainingPresentationPhase.RoundReview)
            {
                return;
            }

            BeginCommand();
            var result = service.ContinueNextRound(Current.SessionId);
            CompleteCommand(result.Success, result.Message, result.Data);
        }

        void RequestRetry()
        {
            if (service == null || commandInFlight || !hasSnapshot
                || Current.Phase != TrainingPresentationPhase.SessionResults)
            {
                return;
            }

            BeginCommand();
            var result = service.Retry(Current.SessionId);
            CompleteCommand(result.Success, result.Message, result.Data);
        }

        void BeginCommand()
        {
            commandInFlight = true;
            commandOrigin = Current;
            LastError = string.Empty;
            view?.SetCommandInFlight(true);
        }

        void CompleteCommand(bool success, string message, TrainingPresentationDto result)
        {
            if (!success)
            {
                commandInFlight = false;
                LastError = message ?? string.Empty;
                view?.SetCommandInFlight(false);
                return;
            }

            ApplySnapshot(result);
        }

        void ApplySnapshot(TrainingPresentationDto snapshot)
        {
            if (hasSnapshot && SnapshotsEqual(Current, snapshot))
            {
                return;
            }

            Current = snapshot;
            hasSnapshot = true;
            AppliedSnapshotCount++;

            if (commandInFlight
                && (commandOrigin.SessionId != snapshot.SessionId || commandOrigin.Phase != snapshot.Phase))
            {
                commandInFlight = false;
            }

            view?.Apply(snapshot);
            if (commandInFlight)
            {
                view?.SetCommandInFlight(true);
            }

            SnapshotApplied?.Invoke(snapshot);
        }

        static bool SnapshotsEqual(TrainingPresentationDto left, TrainingPresentationDto right)
        {
            return left.SessionId == right.SessionId
                   && left.Mode == right.Mode
                   && left.Phase == right.Phase
                   && left.Posture == right.Posture
                   && left.ActiveScreen == right.ActiveScreen
                   && left.LargePanelVisible == right.LargePanelVisible
                   && left.MinimalHudVisible == right.MinimalHudVisible
                   && left.ShootingAllowed == right.ShootingAllowed
                   && left.ArtificialLocomotionAllowed == right.ArtificialLocomotionAllowed
                   && left.AwaitingWeaponPickup == right.AwaitingWeaponPickup
                   && left.FiringStationId == right.FiringStationId
                   && left.VisibilityReason == right.VisibilityReason;
        }
    }
}
