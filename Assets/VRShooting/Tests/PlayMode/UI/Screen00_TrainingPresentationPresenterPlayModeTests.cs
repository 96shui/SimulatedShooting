using System;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    [TestFixture]
    public sealed class Screen00_TrainingPresentationPresenterPlayModeTests
    {
        GameObject root;
        FakePresentationService service;
        TrainingPresentationView view;
        TrainingPresentationPresenter presenter;
        GameObject largeRoot;
        GameObject minimalHudRoot;
        GameObject briefing;
        GameObject impactAnalysis;
        GameObject finalRating;
        GameObject zeroingHud;
        TextMeshProUGUI pickupPrompt;
        TextMeshProUGUI firingStationState;
        Button nextRoundButton;
        Button retryButton;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("Task002_BlankPlayModeScene");
            new GameObject("EventSystem", typeof(EventSystem)).transform.SetParent(root.transform);

            var canvasObject = new GameObject("Training_SharedWorldSpaceUI", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            largeRoot = CreateUiObject("LargePanelRoot", canvasObject.transform, "Training.Shared.LargePanelRoot");
            StretchToParent(largeRoot.GetComponent<RectTransform>());
            largeRoot.AddComponent<CanvasGroup>();
            minimalHudRoot = CreateUiObject("MinimalHudRoot", canvasObject.transform, "Training.Shared.MinimalHudRoot");
            StretchToParent(minimalHudRoot.GetComponent<RectTransform>());
            minimalHudRoot.AddComponent<CanvasGroup>();

            briefing = CreateUiObject("Screen_ZeroingBriefing", largeRoot.transform, "Screen_ZeroingBriefing");
            impactAnalysis = CreateUiObject("Screen_ZeroingImpactAnalysis", largeRoot.transform, "Screen_ZeroingImpactAnalysis");
            finalRating = CreateUiObject("Screen_ZeroingFinalRating", largeRoot.transform, "Screen_ZeroingFinalRating");
            zeroingHud = CreateUiObject("Screen_ZeroingHud", minimalHudRoot.transform, "Screen_ZeroingHud");
            StretchToParent(zeroingHud.GetComponent<RectTransform>());
            var cornerHud = CreateUiObject("Hud_Zeroing_Ammo", zeroingHud.transform, "Hud_Zeroing_Ammo");
            var cornerRect = cornerHud.GetComponent<RectTransform>();
            cornerRect.anchorMin = new Vector2(1f, 1f);
            cornerRect.anchorMax = new Vector2(1f, 1f);
            cornerRect.pivot = new Vector2(1f, 1f);
            cornerRect.anchoredPosition = new Vector2(-60f, -60f);
            cornerRect.sizeDelta = new Vector2(330f, 85f);
            cornerHud.AddComponent<Image>();

            pickupPrompt = CreateText("Text_TrainingShared_PickupPrompt", briefing.transform, "Training.Shared.PickupPrompt");
            firingStationState = CreateText("Text_TrainingShared_FiringStationState", briefing.transform, "Training.Shared.FiringStationState");
            nextRoundButton = CreateButton("Button_ZeroingImpactAnalysis_NextRound", impactAnalysis.transform);
            retryButton = CreateButton("Button_ZeroingFinalRating_Retry", finalRating.transform);

            view = canvasObject.AddComponent<TrainingPresentationView>();
            view.Configure(
                largeRoot,
                minimalHudRoot,
                pickupPrompt,
                firingStationState,
                null,
                nextRoundButton,
                retryButton,
                new[]
                {
                    new TrainingPresentationPanelBinding(ScreenId.ZeroingBriefing, briefing),
                    new TrainingPresentationPanelBinding(ScreenId.ZeroingImpactAnalysis, impactAnalysis),
                    new TrainingPresentationPanelBinding(ScreenId.ZeroingFinalRating, finalRating),
                    new TrainingPresentationPanelBinding(ScreenId.ZeroingHud, zeroingHud)
                });

            service = new FakePresentationService();
            presenter = canvasObject.AddComponent<TrainingPresentationPresenter>();
            presenter.Initialize(view, service);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Screen00_FakeSnapshotsAtomicallyDriveLargePanelMinimalHudAndCurrentScreen()
        {
            service.Publish(Snapshot(TrainingPresentationPhase.AwaitingStartConfirmation, ScreenId.ZeroingBriefing, true, false));
            yield return null;
            AssertVisible(large: true, minimal: false, briefing);

            service.Publish(Snapshot(
                TrainingPresentationPhase.AwaitingWeaponPickup,
                ScreenId.ZeroingBriefing,
                true,
                false,
                awaitingPickup: true,
                reason: "AwaitingWeaponPickup"));
            yield return null;
            AssertVisible(large: true, minimal: false, briefing);
            Assert.That(pickupPrompt.text, Does.Contain("取枪"));
            Assert.That(firingStationState.text, Does.Contain("ZeroingRange.FiringStation.Root"));

            service.Publish(Snapshot(TrainingPresentationPhase.LiveFire, ScreenId.ZeroingHud, false, true));
            yield return null;
            AssertVisible(large: false, minimal: true, zeroingHud);

            service.Publish(Snapshot(TrainingPresentationPhase.RoundReview, ScreenId.ZeroingImpactAnalysis, true, false));
            yield return null;
            AssertVisible(large: true, minimal: false, impactAnalysis);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(nextRoundButton.gameObject));

            service.Publish(Snapshot(TrainingPresentationPhase.SessionResults, ScreenId.ZeroingFinalRating, true, false));
            yield return null;
            AssertVisible(large: true, minimal: false, finalRating);
        }

        [UnityTest]
        public IEnumerator Screen00_NextRoundClickSendsOneCommandAndWaitsForReturnedSnapshotToHide()
        {
            service.Publish(Snapshot(TrainingPresentationPhase.RoundReview, ScreenId.ZeroingImpactAnalysis, true, false));
            service.NextRoundResult = Snapshot(TrainingPresentationPhase.LiveFire, ScreenId.ZeroingHud, false, true);
            yield return null;

            nextRoundButton.onClick.Invoke();
            nextRoundButton.onClick.Invoke();
            yield return null;

            Assert.That(service.NextRoundCalls, Is.EqualTo(1));
            AssertVisible(large: true, minimal: false, impactAnalysis);
            Assert.That(nextRoundButton.interactable, Is.False);

            service.Publish(service.NextRoundResult);
            yield return null;
            AssertVisible(large: false, minimal: true, zeroingHud);
        }

        [UnityTest]
        public IEnumerator Screen00_RepeatedAndOutOfOrderEventsDoNotReapplyOrFlashOlderPanel()
        {
            var results = Snapshot(TrainingPresentationPhase.SessionResults, ScreenId.ZeroingFinalRating, true, false);
            service.Publish(results);
            yield return null;
            var applied = presenter.AppliedSnapshotCount;

            service.Publish(results);
            yield return null;
            Assert.That(presenter.AppliedSnapshotCount, Is.EqualTo(applied));

            service.PublishEventOnly(Snapshot(TrainingPresentationPhase.RoundReview, ScreenId.ZeroingImpactAnalysis, true, false));
            yield return null;
            Assert.That(presenter.AppliedSnapshotCount, Is.EqualTo(applied));
            AssertVisible(large: true, minimal: false, finalRating);
        }

        [UnityTest]
        public IEnumerator Screen00_DisableEnableAndDestroyKeepOneSubscriptionAndSelfHealFromQuery()
        {
            service.Publish(Snapshot(TrainingPresentationPhase.AwaitingWeaponPickup, ScreenId.ZeroingBriefing, true, false, true));
            yield return null;
            Assert.That(service.SubscriberCount, Is.EqualTo(1));

            presenter.enabled = false;
            yield return null;
            Assert.That(service.SubscriberCount, Is.Zero);

            service.SetCurrent(Snapshot(TrainingPresentationPhase.LiveFire, ScreenId.ZeroingHud, false, true));
            presenter.enabled = true;
            yield return null;
            Assert.That(service.SubscriberCount, Is.EqualTo(1));
            AssertVisible(large: false, minimal: true, zeroingHud);

            UnityEngine.Object.Destroy(presenter);
            yield return null;
            Assert.That(service.SubscriberCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Screen00_AllSharedNodesHaveStableTestIdsAndHudKeepsCenterAimReserveClear()
        {
            service.Publish(Snapshot(TrainingPresentationPhase.LiveFire, ScreenId.ZeroingHud, false, true));
            yield return null;

            Assert.That(FindById("Training.Shared.LargePanelRoot"), Is.EqualTo(largeRoot));
            Assert.That(FindById("Training.Shared.MinimalHudRoot"), Is.EqualTo(minimalHudRoot));
            Assert.That(FindById("Training.Shared.PickupPrompt"), Is.EqualTo(pickupPrompt.gameObject));
            Assert.That(FindById("Training.Shared.FiringStationState"), Is.EqualTo(firingStationState.gameObject));
            Assert.That(FindById("Button_ZeroingImpactAnalysis_NextRound"), Is.EqualTo(nextRoundButton.gameObject));
            Assert.That(FindById("Button_ZeroingFinalRating_Retry"), Is.EqualTo(retryButton.gameObject));
            Assert.That(view.HasCenterAimObstruction(), Is.False);

            var obstruction = CreateUiObject("Task002_TestCenterObstruction", zeroingHud.transform, "Task002.Test.CenterObstruction");
            obstruction.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 200f);
            obstruction.AddComponent<Image>();
            Assert.That(view.HasCenterAimObstruction(), Is.True);
        }

        [UnityTest]
        public IEnumerator Screen00_AnchorBinderReportsMissingAnchorAndSupportsDefaultFakeAnchors()
        {
            var binderObject = new GameObject("AnchorBinder");
            binderObject.transform.SetParent(root.transform, false);
            var binder = binderObject.AddComponent<TrainingUIAnchorBinder>();
            binder.Configure(largeRoot.transform, minimalHudRoot.transform);

            LogAssert.Expect(LogType.Error, "[TrainingUIAnchorBinder] Missing LargePanel anchor.");
            Assert.That(binder.Bind(null, null), Is.False);
            Assert.That(binder.LastError, Does.Contain("LargePanel"));

            var anchors = TrainingUIAnchorPair.CreateDefaultFake(root.transform);
            Assert.That(binder.Bind(anchors.LargePanel, anchors.MinimalHud), Is.True);
            Assert.That(largeRoot.transform.parent, Is.EqualTo(anchors.LargePanel.AnchorTransform));
            Assert.That(minimalHudRoot.transform.parent, Is.EqualTo(anchors.MinimalHud.AnchorTransform));
            yield return null;
        }

        void AssertVisible(bool large, bool minimal, GameObject activePanel)
        {
            Assert.That(largeRoot.activeSelf, Is.EqualTo(large));
            Assert.That(minimalHudRoot.activeSelf, Is.EqualTo(minimal));
            Assert.That(activePanel.activeSelf, Is.True);
        }

        GameObject FindById(string id)
        {
            var ids = root.GetComponentsInChildren<UITestId>(true);
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i].Id == id)
                {
                    return ids[i].gameObject;
                }
            }

            return null;
        }

        static GameObject CreateUiObject(string name, Transform parent, string testId)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var id = go.AddComponent<UITestId>();
            id.SetId(testId);
            return go;
        }

        static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string testId)
        {
            var go = CreateUiObject(name, parent, testId);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.raycastTarget = false;
            return text;
        }

        static Button CreateButton(string name, Transform parent)
        {
            var go = CreateUiObject(name, parent, name);
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }

        static TrainingPresentationDto Snapshot(
            TrainingPresentationPhase phase,
            ScreenId screen,
            bool large,
            bool minimal,
            bool awaitingPickup = false,
            string reason = "")
        {
            return new TrainingPresentationDto
            {
                SessionId = phase == TrainingPresentationPhase.AwaitingStartConfirmation ? string.Empty : "task002-session",
                Mode = TrainingMode.Zeroing100m,
                Phase = phase,
                Posture = TrainingPostureMode.ProneFixed,
                ActiveScreen = screen,
                LargePanelVisible = large,
                MinimalHudVisible = minimal,
                ShootingAllowed = phase == TrainingPresentationPhase.LiveFire && !awaitingPickup,
                ArtificialLocomotionAllowed = false,
                AwaitingWeaponPickup = awaitingPickup,
                FiringStationId = "ZeroingRange.FiringStation.Root",
                VisibilityReason = reason
            };
        }

        sealed class FakePresentationService : ITrainingPresentationService
        {
            Action<TrainingPresentationDto> presentationChanged;

            public TrainingPresentationDto Current { get; private set; }
            public TrainingPresentationDto NextRoundResult { get; set; }
            public int NextRoundCalls { get; private set; }
            public int SubscriberCount { get; private set; }

            public event Action<TrainingPresentationDto> PresentationChanged
            {
                add
                {
                    presentationChanged += value;
                    SubscriberCount++;
                }
                remove
                {
                    presentationChanged -= value;
                    SubscriberCount--;
                }
            }

            public void SetCurrent(TrainingPresentationDto dto)
            {
                Current = dto;
            }

            public void Publish(TrainingPresentationDto dto)
            {
                Current = dto;
                presentationChanged?.Invoke(dto);
            }

            public void PublishEventOnly(TrainingPresentationDto dto)
            {
                presentationChanged?.Invoke(dto);
            }

            public ServiceResult<TrainingPresentationDto> Enter(TrainingMode mode) => ServiceResult<TrainingPresentationDto>.Ok(Current);
            public ServiceResult<TrainingPresentationDto> Get(string sessionId) => ServiceResult<TrainingPresentationDto>.Ok(Current);
            public ServiceResult<TrainingPresentationDto> ConfirmStart(string sessionId) => ServiceResult<TrainingPresentationDto>.Ok(Current);
            public ServiceResult<TrainingPresentationDto> HandleWeaponPickup(VRShooting.Application.Events.TrainingWeaponPickupEvent pickup) => ServiceResult<TrainingPresentationDto>.Ok(Current);

            public ServiceResult<TrainingPresentationDto> ContinueNextRound(string sessionId)
            {
                NextRoundCalls++;
                return ServiceResult<TrainingPresentationDto>.Ok(Current);
            }

            public ServiceResult<TrainingPresentationDto> NotifyMovingTargetCountdownElapsed(string sessionId) => ServiceResult<TrainingPresentationDto>.Ok(Current);
            public ServiceResult<TrainingPresentationDto> NotifyTrainingCompleted(string sessionId) => ServiceResult<TrainingPresentationDto>.Ok(Current);
            public ServiceResult<TrainingPresentationDto> Retry(string sessionId) => ServiceResult<TrainingPresentationDto>.Ok(Current);
            public ServiceResult<TrainingPresentationDto> Exit(string sessionId) => ServiceResult<TrainingPresentationDto>.Ok(Current);
            public ServiceResult<TrainingLocomotionPolicyDto> GetLocomotionPolicy(string sessionId) => ServiceResult<TrainingLocomotionPolicyDto>.Fail(ErrorCode.NotFound);
        }
    }
}
