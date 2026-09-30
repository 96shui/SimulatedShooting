using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Unity;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

// Temporary editor helper for capturing each live mode-one screen.
internal static class ModeOneScreenshotAdvance
{
    private static bool gripReady;

    [MenuItem("Tools/Mode One Screenshot/Render UI Gallery _F10")]
    private static void RenderUiGallery()
    {
        Debug.Log(VRShooting.Editor.TacticalUIGallery.Capture());
    }

    [MenuItem("Tools/Mode One Screenshot/Advance _F9")]
    private static void Advance()
    {
        if (!EditorApplication.isPlaying || GameMain.Instance?.Services == null)
        {
            Debug.LogWarning("Start Play mode before advancing mode-one screenshots.");
            return;
        }

        var services = GameMain.Instance.Services;
        Debug.Log("Mode-one capture advance: router=" + services.Router.Current
            + ", phase=" + services.Presentation.Get(string.Empty).Data.Phase);
        switch (services.Router.Current)
        {
            case ScreenId.MainMenu:
                gripReady = false;
                Click("Button_MainMenu_OpenZeroing");
                break;
            case ScreenId.ZeroingBriefing:
                if (SceneManager.GetActiveScene().name == "ZeroingRangeScene")
                {
                    gripReady = SetGrip(services);
                }
                else
                {
                    Click("Button_ZeroingBriefing_Start");
                }
                break;
            case ScreenId.ZeroingHud:
                if (!gripReady)
                {
                    gripReady = SetGrip(services);
                }
                else
                {
                    CompletePassingRound(services);
                }
                break;
            case ScreenId.ZeroingImpactAnalysis:
                Click("Button_ZeroingImpactAnalysis_HorizontalPlus");
                Click("Button_ZeroingImpactAnalysis_VerticalMinus");
                Click("Button_ZeroingImpactAnalysis_ApplyAdjustment");
                Click("Button_ZeroingImpactAnalysis_NextRound");
                break;
            default:
                Debug.Log("Mode-one screenshot sequence reached " + services.Router.Current);
                break;
        }
    }

    private static void CompletePassingRound(ApplicationServices services)
    {
        var sessionId = services.TrainingSessions.Current.SessionId;
        var offset = services.Zeroing.GetSession(sessionId).Data.FixedImpactOffsetCm;
        var aim = new Vector2(1f, 1f) - offset;
        for (var i = 0; i < 3; i++)
        {
            var shot = services.Zeroing.RecordShot(sessionId, new ShotInputDto
            {
                AimDirection = new Vector3(aim.x, aim.y, ZeroingRules.DistanceMeters),
                WeaponPosition = Vector3.zero,
                WeaponStability = 0.95f,
                FireTime = Time.timeAsDouble + i * 0.1
            });
            if (!shot.Success)
            {
                Debug.LogError(shot.Message);
                return;
            }
        }
        if (services.Router.Current == ScreenId.ZeroingHud)
        {
            services.Router.Open(ScreenId.ZeroingImpactAnalysis, new NavigationArgs
            {
                Mode = TrainingMode.Zeroing100m,
                SessionId = sessionId,
                ReturnToScreen = ScreenId.ZeroingHud.ToString()
            });
        }
    }

    private static bool SetGrip(ApplicationServices services)
    {
        var sessionId = services.TrainingSessions.Current.SessionId;
        var rear = services.WeaponControl.SetGripState(new WeaponGripStateInputDto
        {
            SessionId = sessionId,
            HoldState = WeaponHoldState.RearHandHeld,
            RearHandTracked = true,
            FrontHandTracked = false,
            Stability01 = 0.45f
        });
        if (!rear.Success)
        {
            Debug.LogError(rear.Message);
            return false;
        }
        var grip = services.WeaponControl.SetGripState(new WeaponGripStateInputDto
        {
            SessionId = sessionId,
            HoldState = WeaponHoldState.TwoHandHeld,
            RearHandTracked = true,
            FrontHandTracked = true,
            Stability01 = 0.95f
        });
        if (!grip.Success)
        {
            Debug.LogError(grip.Message);
            return false;
        }
        var pickup = services.Presentation.HandleWeaponPickup(new TrainingWeaponPickupEvent
        {
            SessionId = sessionId,
            WeaponId = services.TrainingSessions.Current.WeaponId,
            PreviousState = WeaponHoldState.OnRack,
            CurrentState = WeaponHoldState.RearHandHeld
        });
        if (!pickup.Success && pickup.ErrorCode != ErrorCode.InvalidState)
        {
            Debug.LogError(pickup.Message);
        }
        return true;
    }

    private static void Click(string testId)
    {
        foreach (var item in Object.FindObjectsOfType<UITestId>(true))
        {
            if (item.Id != testId) continue;
            var button = item.GetComponent<Button>();
            if (button == null) break;
            button.onClick.Invoke();
            return;
        }
        Debug.LogError("Screenshot button missing: " + testId);
    }
}
