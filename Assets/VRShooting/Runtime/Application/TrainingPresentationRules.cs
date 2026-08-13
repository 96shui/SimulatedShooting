using VRShooting.Common;

namespace VRShooting.Application
{
    /// <summary>
    /// 由阶段推导大型 UI / 最小 HUD / 是否允许射击。UI 不得自行组合这些布尔值。
    /// </summary>
    public static class TrainingPresentationRules
    {
        public const string ZeroingFiringStationId = "ZeroingRange.FiringStation.Root";
        public const string MovingTargetFiringStationId = "MovingTargetRange.FiringStation.Root";
        public const string TrainingRifleId = "training-rifle";
        public const float MovingTargetCountdownSeconds = 3f;

        public const string ReasonAwaitingStart = "AwaitingStartConfirmation";
        public const string ReasonPickupBeforeStart = "PickupBeforeStart";
        public const string ReasonAwaitingPickup = "AwaitingWeaponPickup";
        public const string ReasonLiveFire = "LiveFire";
        public const string ReasonP2Countdown = "MovingTargetCountdown";
        public const string ReasonAwaitingRearGrip = "AwaitingRearGrip";
        public const string ReasonRoundReview = "RoundReview";
        public const string ReasonSessionResults = "SessionResults";
        public const string ReasonExiting = "Exiting";

        public static bool IsProneFixedMode(TrainingMode mode)
        {
            return mode == TrainingMode.Zeroing100m || mode == TrainingMode.MovingTarget;
        }

        public static string FiringStationIdFor(TrainingMode mode)
        {
            return mode == TrainingMode.MovingTarget
                ? MovingTargetFiringStationId
                : ZeroingFiringStationId;
        }

        public static bool IsValidRearPickup(WeaponHoldState previous, WeaponHoldState current)
        {
            return (previous == WeaponHoldState.OnRack || previous == WeaponHoldState.Dropped)
                   && (current == WeaponHoldState.RearHandHeld || current == WeaponHoldState.TwoHandHeld);
        }

        public static bool IsWeaponHeld(WeaponHoldState hold)
        {
            return hold == WeaponHoldState.RearHandHeld || hold == WeaponHoldState.TwoHandHeld;
        }

        public static TrainingLocomotionPolicyDto CreateLocomotionPolicy(TrainingMode mode)
        {
            return new TrainingLocomotionPolicyDto
            {
                Mode = mode,
                Posture = TrainingPostureMode.ProneFixed,
                AllowContinuousMove = false,
                AllowTeleport = false,
                AllowArtificialTurn = false,
                AllowRoomScaleHeadAndHandTracking = true
            };
        }

        public static TrainingPresentationDto Project(
            string sessionId,
            TrainingMode mode,
            TrainingPresentationPhase phase,
            string firingStationId,
            bool weaponHeld,
            bool p2CountdownPending,
            string promptReason = null)
        {
            var awaitingReacquire = phase == TrainingPresentationPhase.LiveFire && !weaponHeld;
            var p2Countdown = mode == TrainingMode.MovingTarget
                              && phase == TrainingPresentationPhase.LiveFire
                              && p2CountdownPending;
            var shootingAllowed = phase == TrainingPresentationPhase.LiveFire
                                  && weaponHeld
                                  && !p2Countdown;
            var largePanelVisible = phase == TrainingPresentationPhase.ModeEntry
                                    || phase == TrainingPresentationPhase.AwaitingStartConfirmation
                                    || phase == TrainingPresentationPhase.AwaitingWeaponPickup
                                    || phase == TrainingPresentationPhase.RoundReview
                                    || phase == TrainingPresentationPhase.SessionResults;
            var minimalHudVisible = phase == TrainingPresentationPhase.LiveFire;
            var awaitingPickup = phase == TrainingPresentationPhase.AwaitingWeaponPickup || awaitingReacquire;

            return new TrainingPresentationDto
            {
                SessionId = sessionId ?? string.Empty,
                Mode = mode,
                Phase = phase,
                Posture = TrainingPostureMode.ProneFixed,
                ActiveScreen = ResolveActiveScreen(mode, phase),
                LargePanelVisible = largePanelVisible,
                MinimalHudVisible = minimalHudVisible,
                ShootingAllowed = shootingAllowed,
                ArtificialLocomotionAllowed = false,
                AwaitingWeaponPickup = awaitingPickup,
                FiringStationId = firingStationId ?? string.Empty,
                VisibilityReason = ResolveVisibilityReason(phase, awaitingReacquire, p2Countdown, promptReason)
            };
        }

        static ScreenId ResolveActiveScreen(TrainingMode mode, TrainingPresentationPhase phase)
        {
            switch (phase)
            {
                case TrainingPresentationPhase.LiveFire:
                    return mode == TrainingMode.MovingTarget
                        ? ScreenId.MovingTargetHud
                        : ScreenId.ZeroingHud;
                case TrainingPresentationPhase.RoundReview:
                    return ScreenId.ZeroingImpactAnalysis;
                case TrainingPresentationPhase.SessionResults:
                    return mode == TrainingMode.MovingTarget
                        ? ScreenId.MovingTargetResults
                        : ScreenId.ZeroingFinalRating;
                case TrainingPresentationPhase.Exiting:
                    return ScreenId.MainMenu;
                default:
                    return mode == TrainingMode.MovingTarget
                        ? ScreenId.MovingTargetSettings
                        : ScreenId.ZeroingBriefing;
            }
        }

        static string ResolveVisibilityReason(
            TrainingPresentationPhase phase,
            bool awaitingReacquire,
            bool p2Countdown,
            string promptReason)
        {
            if (!string.IsNullOrEmpty(promptReason))
            {
                return promptReason;
            }

            if (p2Countdown)
            {
                return ReasonP2Countdown;
            }

            if (awaitingReacquire)
            {
                return ReasonAwaitingRearGrip;
            }

            switch (phase)
            {
                case TrainingPresentationPhase.AwaitingWeaponPickup:
                    return ReasonAwaitingPickup;
                case TrainingPresentationPhase.LiveFire:
                    return ReasonLiveFire;
                case TrainingPresentationPhase.RoundReview:
                    return ReasonRoundReview;
                case TrainingPresentationPhase.SessionResults:
                    return ReasonSessionResults;
                case TrainingPresentationPhase.Exiting:
                    return ReasonExiting;
                default:
                    return ReasonAwaitingStart;
            }
        }
    }
}
