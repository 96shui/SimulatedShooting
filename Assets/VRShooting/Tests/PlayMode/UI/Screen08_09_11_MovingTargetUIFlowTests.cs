using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
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
    public sealed class Screen08_09_11_MovingTargetUIFlowTests
    {
        GameObject root;
        MovingTargetRangeUI ui;
        FakeMovingTargetUIPort fake;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("Task005_BlankSceneUI", typeof(RectTransform));
            ui = root.AddComponent<MovingTargetRangeUI>();
            fake = new FakeMovingTargetUIPort();
            ui.Initialize(fake);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
            if (TrainingUIHost.Instance != null)
            {
                UnityEngine.Object.Destroy(TrainingUIHost.Instance.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Screen08_ThreeSpeedsDefaultToFourAndForwardOnlySelectedSpeed()
        {
            Assert.That(FindButton("Button_MovingTargetSetup_Speed3"), Is.Not.Null);
            Assert.That(FindButton("Button_MovingTargetSetup_Speed4"), Is.Not.Null);
            Assert.That(FindButton("Button_MovingTargetSetup_Speed5"), Is.Not.Null);
            Assert.That(FindText("Text_MovingTargetSetup_SelectedSpeed").text, Does.Contain("4 m/s"));

            FindButton("Button_MovingTargetSetup_Speed3").onClick.Invoke();
            Assert.That(ui.Presenter.SelectedSpeed, Is.EqualTo(3f));
            FindButton("Button_MovingTargetSetup_Speed5").onClick.Invoke();
            Assert.That(ui.Presenter.SelectedSpeed, Is.EqualTo(5f));
            FindButton("Button_MovingTargetSetup_Start").onClick.Invoke();
            yield return null;

            Assert.That(fake.StartCount, Is.EqualTo(1));
            Assert.That(fake.LastSettings.SpeedMetersPerSecond, Is.EqualTo(5f));
            Assert.That(FindById("Screen_MovingTargetSetup"), Is.Not.Null);
            Assert.That(FindById("Screen_MovingTargetSettings"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Screen08_StartSuccessWaitsForPickupAndLiveDtoAtomicallyShowsMinimalHud()
        {
            FindButton("Button_MovingTargetSetup_Start").onClick.Invoke();
            FindButton("Button_MovingTargetSetup_Start").onClick.Invoke();
            yield return null;

            Assert.That(fake.StartCount, Is.EqualTo(1), "Awaiting-pickup state must reject duplicate start commands.");
            Assert.That(ui.LargePanelRoot.activeSelf, Is.True);
            Assert.That(ui.MinimalHudRoot.activeSelf, Is.False);
            Assert.That(FindText("Text_MovingTargetSetup_Status").text, Does.Contain("拿起"));

            fake.PublishPresentation(Presentation(TrainingPresentationPhase.LiveFire, ScreenId.MovingTargetHud, false, true));
            yield return null;

            Assert.That(ui.LargePanelRoot.activeSelf, Is.False);
            Assert.That(ui.MinimalHudRoot.activeSelf, Is.True);
            Assert.That(FindById("Screen_MovingTargetHud").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Screen00_SceneAnchorsBindLargePanelAndMinimalHudAsIndependentWorldCanvases()
        {
            var largeAnchor = new GameObject("LargeUiAnchor").transform;
            var minimalAnchor = new GameObject("MinimalHudAnchor").transform;

            Assert.That(ui.BindToSceneAnchors(largeAnchor, minimalAnchor), Is.True);
            yield return null;

            Assert.That(ui.LargePanelRoot.transform.parent, Is.EqualTo(largeAnchor));
            Assert.That(ui.MinimalHudRoot.transform.parent, Is.EqualTo(minimalAnchor));
            Assert.That(ui.LargePanelRoot.transform.parent, Is.Not.EqualTo(ui.MinimalHudRoot.transform.parent));
            Assert.That(ui.LargePanelRoot.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(ui.MinimalHudRoot.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));

            UnityEngine.Object.Destroy(largeAnchor.gameObject);
            UnityEngine.Object.Destroy(minimalAnchor.gameObject);
        }

        [UnityTest]
        public IEnumerator Screen08_MissingSpeedConfigurationCannotStartAndShowsServiceError()
        {
            var unavailable = new FakeMovingTargetUIPort
            {
                AvailableSpeeds = Array.Empty<float>()
            };
            fake = unavailable;
            ui.Initialize(unavailable);

            FindButton("Button_MovingTargetSetup_Start").onClick.Invoke();
            yield return null;

            Assert.That(unavailable.StartCount, Is.Zero);
            Assert.That(ui.Presenter.LastError, Does.Contain("没有可用速度"));
            Assert.That(FindText("Text_MovingTargetSetup_Error").text, Does.Contain("没有可用速度"));
        }

        [UnityTest]
        public IEnumerator Screen09_HudConsumesCountdownPerShotContinuousAndForbiddenSnapshotsInOrder()
        {
            FindButton("Button_MovingTargetSetup_Start").onClick.Invoke();
            fake.PublishPresentation(Presentation(TrainingPresentationPhase.LiveFire, ScreenId.MovingTargetHud, false, true));

            fake.PublishHud(Hud("3", "10/10", "0/10", "0%", WeaponFireSequencePhase.Idle, null, false, "等待开始"));
            Assert.That(FindText("Hud_MovingTarget_Countdown").text, Is.EqualTo("3"));
            Assert.That(FindText("Hud_MovingTarget_Ammo").text, Does.Contain("10/10"));

            fake.PublishHud(Hud("0", "9/10", "1/10", "20%", WeaponFireSequencePhase.InitialTwoShots, null, true, "可射击"));
            Assert.That(FindText("Hud_MovingTarget_Ammo").text, Does.Contain("9/10"));
            Assert.That(FindText("Hud_MovingTarget_FireSequence").text, Is.EqualTo("两发起射"));

            fake.PublishHud(Hud("0", "8/10", "1/10", "25%", WeaponFireSequencePhase.InitialTwoShots, null, true, "可射击"));
            Assert.That(FindText("Hud_MovingTarget_Ammo").text, Does.Contain("8/10"));
            fake.PublishHud(Hud("0", "7/10", "2/10", "30%", WeaponFireSequencePhase.ContinuousFire, null, true, "可射击"));
            Assert.That(FindText("Hud_MovingTarget_Ammo").text, Does.Contain("7/10"));
            Assert.That(FindText("Hud_MovingTarget_FireSequence").text, Is.EqualTo("长按连射"));

            fake.PublishHud(Hud("0", "7/10", "2/10", "50%", WeaponFireSequencePhase.Stopped,
                WeaponFireStopReason.ShootingBecameForbidden, false, "端点停留禁射"));
            Assert.That(FindText("Hud_MovingTarget_NoFirePrompt").text, Is.EqualTo("端点停留禁射"));
            Assert.That(FindText("Hud_MovingTarget_FireSequence").text, Does.Contain("进入禁射"));

            Assert.That(StopState(WeaponFireStopReason.TriggerReleased), Does.Contain("释放扳机"));
            Assert.That(StopState(WeaponFireStopReason.AmmoDepleted), Does.Contain("弹药耗尽"));
            Assert.That(StopState(WeaponFireStopReason.TrainingCompleted), Does.Contain("训练完成"));
            Assert.That(StopState(WeaponFireStopReason.WeaponBecameInvalid), Does.Contain("武器或跟踪失效"));

            fake.PublishHud(Hud("0", "7/10", "2/10", "55%", WeaponFireSequencePhase.Idle, null, true, "可射击"));
            Assert.That(FindText("Hud_MovingTarget_FireSequence").text, Is.EqualTo("待扣动"),
                "Recovery must not fabricate a continuing fire sequence before re-arming.");
            Assert.That(ui.Presenter.HudRenderCount, Is.GreaterThanOrEqualTo(6));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Screen11_CompletionShowsVariableSequencesAndRetrySendsOneCommand()
        {
            FindButton("Button_MovingTargetSetup_Start").onClick.Invoke();
            var result = MovingTargetFakeSequences.CreateResult(hitCount: 4, speedMetersPerSecond: 4f);
            fake.SetResult(result);
            fake.PublishPresentation(Presentation(TrainingPresentationPhase.SessionResults, ScreenId.MovingTargetResults, true, false));
            yield return null;

            Assert.That(ui.LargePanelRoot.activeSelf, Is.True);
            Assert.That(ui.MinimalHudRoot.activeSelf, Is.False);
            Assert.That(FindText("Text_MovingTargetResults_Summary").text, Does.Contain("总射击：8"));
            Assert.That(FindText("Text_MovingTargetResults_Summary").text, Does.Contain("命中：4"));
            Assert.That(FindText("Text_MovingTargetResults_Sequences").text, Does.Contain("快速两发"));
            Assert.That(FindText("Text_MovingTargetResults_Sequences").text, Does.Contain("进入连射"));
            Assert.That(FindText("Text_MovingTargetResults_Sequences").text, Does.Contain("#6"));
            Assert.That(FindText("Hud_MovingTarget_Ammo").text, Is.EqualTo("弹药 --"));

            FindButton("Button_MovingTargetResults_Retry").onClick.Invoke();
            FindButton("Button_MovingTargetResults_Retry").onClick.Invoke();
            yield return null;

            Assert.That(fake.RetryCount, Is.EqualTo(1));
            Assert.That(FindById("Screen_MovingTargetSetup").activeSelf, Is.True);
            Assert.That(ui.Presenter.SelectedSpeed, Is.EqualTo(4f), "Retry preserves the selected speed.");
        }

        [UnityTest]
        public IEnumerator Screen11_BackToModeSelectionSendsOnlyOneExitCommand()
        {
            FindButton("Button_MovingTargetSetup_Start").onClick.Invoke();
            fake.PublishPresentation(Presentation(TrainingPresentationPhase.SessionResults, ScreenId.MovingTargetResults, true, false));

            FindButton("Button_MovingTargetResults_BackToModeSelection").onClick.Invoke();
            FindButton("Button_MovingTargetResults_BackToModeSelection").onClick.Invoke();
            yield return null;

            Assert.That(fake.ExitCount, Is.EqualTo(1));
        }

        [TestCase(ResultGrade.Fail, "不及格", 0, "0%")]
        [TestCase(ResultGrade.Pass, "及格", 3, "37.5%")]
        [TestCase(ResultGrade.Good, "良好", 4, "50%")]
        [TestCase(ResultGrade.Excellent, "优秀", 8, "100%")]
        public void Screen11_GradeAndEmptySequenceFormattingIsDtoDriven(
            ResultGrade grade, string expected, int hitCount, string expectedRate)
        {
            var formatted = MovingTargetUIPresenter.FormatResult(new MovingTargetResultDto
            {
                SessionId = "result",
                Grade = grade,
                TotalShotsFired = 8,
                HitCount = hitCount,
                HitRate01 = hitCount / 8f,
                FireSequences = Array.Empty<FireSequenceRecordDto>()
            });
            Assert.That(formatted.Summary, Does.Contain(expected));
            Assert.That(formatted.Summary, Does.Contain(expectedRate));
            Assert.That(formatted.Sequences, Is.EqualTo("无射击序列"));
        }

        [UnityTest]
        public IEnumerator Destroy_UnsubscribesAndDisposesFakePort()
        {
            UnityEngine.Object.Destroy(root);
            root = null;
            yield return null;

            Assert.That(fake.DisposeCount, Is.EqualTo(1));
            Assert.That(fake.PresentationSubscriberCount, Is.EqualTo(0));
            Assert.That(fake.HudSubscriberCount, Is.EqualTo(0));
            Assert.That(fake.ResultSubscriberCount, Is.EqualTo(0));
        }

        GameObject FindById(string id)
        {
            var ids = root.GetComponentsInChildren<UITestId>(true);
            for (var index = 0; index < ids.Length; index++)
            {
                if (ids[index].Id == id)
                {
                    return ids[index].gameObject;
                }
            }
            return null;
        }

        Button FindButton(string id)
        {
            var go = FindById(id);
            Assert.That(go, Is.Not.Null, id);
            return go.GetComponent<Button>();
        }

        TMP_Text FindText(string id)
        {
            var go = FindById(id);
            Assert.That(go, Is.Not.Null, id);
            return go.GetComponent<TMP_Text>();
        }

        static TrainingPresentationDto Presentation(TrainingPresentationPhase phase, ScreenId screen, bool large, bool minimal)
        {
            return new TrainingPresentationDto
            {
                SessionId = phase == TrainingPresentationPhase.AwaitingStartConfirmation ? string.Empty : "fake-moving-target",
                Mode = TrainingMode.MovingTarget,
                Phase = phase,
                Posture = TrainingPostureMode.ProneFixed,
                ActiveScreen = screen,
                LargePanelVisible = large,
                MinimalHudVisible = minimal,
                ShootingAllowed = minimal,
                ArtificialLocomotionAllowed = false,
                AwaitingWeaponPickup = phase == TrainingPresentationPhase.AwaitingWeaponPickup,
                FiringStationId = "moving-target-prone-station"
            };
        }

        static HudDto Hud(string countdown, string ammo, string hits, string progress,
            WeaponFireSequencePhase phase, WeaponFireStopReason? reason, bool canShoot, string prompt)
        {
            return new HudDto
            {
                SessionId = "fake-moving-target",
                Mode = TrainingMode.MovingTarget,
                HudType = HudType.MovingTarget,
                CanShoot = canShoot,
                TextLines = new[]
                {
                    Line("ammo", ammo), Line("fireMode", "两发起射 / 长按连射"), Line("hits", hits),
                    Line("progress", progress), Line("speed", "4m/s"), Line("direction", "右→左"), Line("countdown", countdown)
                },
                Prompts = new[] { new HudPromptDto { PromptId = "moving", Text = prompt, IsEnabled = canShoot } },
                FireSequence = new WeaponFireSequenceStateDto
                {
                    SessionId = "fake-moving-target",
                    Phase = phase,
                    StopReason = reason,
                    FireMode = WeaponFireMode.InitialTwoThenAutomatic
                }
            };
        }

        static HudTextLineDto Line(string key, string value)
        {
            return new HudTextLineDto { Key = key, Value = value };
        }

        static string StopState(WeaponFireStopReason reason)
        {
            return MovingTargetUIPresenter.FormatHud(
                Hud("0", "7/10", "2/10", "50%", WeaponFireSequencePhase.Stopped,
                    reason, false, "停止")).FireState;
        }

        sealed class FakeMovingTargetUIPort : IMovingTargetUIPort
        {
            Action<TrainingPresentationDto> presentationChanged;
            Action<HudDto> hudUpdated;
            Action<MovingTargetResultDto> resultUpdated;
            TrainingPresentationDto current = Presentation(
                TrainingPresentationPhase.AwaitingStartConfirmation, ScreenId.MovingTargetSettings, true, false);
            HudDto hud;
            MovingTargetResultDto result;

            public event Action<TrainingPresentationDto> PresentationChanged
            {
                add { presentationChanged += value; PresentationSubscriberCount++; }
                remove { presentationChanged -= value; PresentationSubscriberCount--; }
            }

            public event Action<HudDto> HudUpdated
            {
                add { hudUpdated += value; HudSubscriberCount++; }
                remove { hudUpdated -= value; HudSubscriberCount--; }
            }

            public event Action<MovingTargetResultDto> ResultUpdated
            {
                add { resultUpdated += value; ResultSubscriberCount++; }
                remove { resultUpdated -= value; ResultSubscriberCount--; }
            }

            public int StartCount { get; private set; }
            public int RetryCount { get; private set; }
            public int ExitCount { get; private set; }
            public int DisposeCount { get; private set; }
            public int PresentationSubscriberCount { get; private set; }
            public int HudSubscriberCount { get; private set; }
            public int ResultSubscriberCount { get; private set; }
            public MovingTargetSettingsDto LastSettings { get; private set; }
            public IReadOnlyList<float> AvailableSpeeds { get; set; } = new[] { 3f, 4f, 5f };

            public ServiceResult<IReadOnlyList<float>> GetAvailableSpeeds()
            {
                return AvailableSpeeds.Count == 0
                    ? ServiceResult<IReadOnlyList<float>>.Fail(
                        ErrorCode.InvalidState, "没有可用速度", AvailableSpeeds)
                    : ServiceResult<IReadOnlyList<float>>.Ok(AvailableSpeeds);
            }

            public ServiceResult<TrainingPresentationDto> GetPresentation()
            {
                return ServiceResult<TrainingPresentationDto>.Ok(current);
            }

            public ServiceResult<HudDto> GetHud(string sessionId)
            {
                return string.IsNullOrEmpty(hud.SessionId)
                    ? ServiceResult<HudDto>.Fail(ErrorCode.NotFound, "hud not ready", HudDto.Empty)
                    : ServiceResult<HudDto>.Ok(hud);
            }

            public ServiceResult<MovingTargetResultDto> GetResult(string sessionId)
            {
                return string.IsNullOrEmpty(result.SessionId)
                    ? ServiceResult<MovingTargetResultDto>.Fail(ErrorCode.NotFound, "result not ready", MovingTargetResultDto.Empty)
                    : ServiceResult<MovingTargetResultDto>.Ok(result);
            }

            public ServiceResult<TrainingPresentationDto> Start(MovingTargetSettingsDto settings)
            {
                StartCount++;
                LastSettings = settings;
                current = Presentation(TrainingPresentationPhase.AwaitingWeaponPickup, ScreenId.MovingTargetSettings, true, false);
                return ServiceResult<TrainingPresentationDto>.Ok(current);
            }

            public ServiceResult<TrainingPresentationDto> Retry(string sessionId)
            {
                RetryCount++;
                current = Presentation(TrainingPresentationPhase.AwaitingStartConfirmation, ScreenId.MovingTargetSettings, true, false);
                return ServiceResult<TrainingPresentationDto>.Ok(current);
            }

            public ServiceResult<TrainingPresentationDto> Exit(string sessionId)
            {
                ExitCount++;
                current = Presentation(TrainingPresentationPhase.Exiting, ScreenId.MainMenu, false, false);
                return ServiceResult<TrainingPresentationDto>.Ok(current);
            }

            public void PublishPresentation(TrainingPresentationDto dto)
            {
                current = dto;
                presentationChanged?.Invoke(dto);
            }

            public void PublishHud(HudDto dto)
            {
                hud = dto;
                hudUpdated?.Invoke(dto);
            }

            public void SetResult(MovingTargetResultDto dto)
            {
                result = dto;
                resultUpdated?.Invoke(dto);
            }

            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
