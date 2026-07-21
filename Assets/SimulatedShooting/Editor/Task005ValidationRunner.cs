using System;
using System.IO;
using System.Linq;
using UnityEditor;
using SimulatedShooting.Scene;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Application;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class Task005ValidationRunner
    {
        const string AutomationFlagPath = "Temp/task005-run-validation.flag";
        const string AutomationResultPath = "Temp/task005-validation-result.txt";

        [InitializeOnLoadMethod]
        static void RegisterAutomationHook()
        {
            EditorApplication.delayCall += RunPendingAutomation;
        }

        [MenuItem("Tools/Simulated Shooting/Run Task005 Validation")]
        public static void RunBuildAndValidation()
        {
            ZeroingRangeSceneBuilder.Build();
            RunAll();
        }

        public static void RunAll()
        {
            ValidateWeaponService();
            ValidateZeroingSceneWeapon();
            Debug.Log("TASK005_VALIDATION_PASS");
        }

        static void RunPendingAutomation()
        {
            if (!File.Exists(AutomationFlagPath))
            {
                return;
            }

            try
            {
                File.Delete(AutomationFlagPath);
                RunBuildAndValidation();
                File.WriteAllText(AutomationResultPath, $"TASK005_VALIDATION_PASS {DateTime.UtcNow:O}");
            }
            catch (Exception ex)
            {
                File.WriteAllText(AutomationResultPath, $"TASK005_VALIDATION_FAIL {DateTime.UtcNow:O}{Environment.NewLine}{ex}");
                Debug.LogException(ex);
            }
        }

        static void ValidateWeaponService()
        {
            var service = new WeaponControlService(new GameEventBus());
            var start = service.StartSession("task005-validation", WeaponControlService.TrainingRifleId, TrainingMode.Zeroing100m);
            Require(start.Success, "weapon service failed to start session");
            Require(start.Data.CurrentMagazine == 3, "training rifle magazine should start at 3");
            Require(service.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = "task005-validation",
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = 0.9f
            }).Success, "weapon service failed to enter two-hand held state");

            var fire = service.Fire(new WeaponFireInputDto
            {
                SessionId = "task005-validation",
                MuzzlePosition = Vector3.zero,
                WeaponPosition = Vector3.zero,
                RawAimDirection = Vector3.forward,
                AimDirection = Vector3.forward,
                Stability01 = 0.9f,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = true,
                HitPoint = Vector3.forward * 100f,
                HitObjectId = "ZeroingRange.Target.Face"
            });
            Require(fire.Success, "weapon service valid fire failed");
            Require(fire.Data.CurrentMagazine == 2, "weapon service did not consume ammo");
            Require(fire.Data.Hit, "weapon service did not preserve hit result");

            var aim = service.SetAimMode("task005-validation", WeaponAimMode.AimDownSights);
            Require(aim.Success && aim.Data.AimMode == WeaponAimMode.AimDownSights, "weapon service did not set ADS mode");

            var shoulder = service.ToggleShoulder("task005-validation");
            Require(shoulder.Success && shoulder.Data.ShoulderSide == ShoulderSide.Left, "weapon service did not toggle shoulder");
        }

        static void ValidateZeroingSceneWeapon()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ZeroingRangeScene.unity", OpenSceneMode.Single);
            Require(scene.IsValid(), "ZeroingRangeScene did not open");
            Physics.SyncTransforms();

            var playerRoot = Find("ZeroingRange.Weapon.PlayerRoot");
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle");
            var controller = playerRoot.GetComponent<FirstPersonTrainingWeaponController>();
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();

            Require(controller != null, "first-person weapon controller missing");
            Require(binding != null && binding.HasRequiredBinding, "training rifle binding incomplete");
            Require(surface != null, "target impact surface missing");
            Require(controller.HasVrPoseSources, "VR head/rear/front pose sources are not bound");
            Require(controller.InitializeForTests(), "first-person weapon controller did not initialize");

            var impactCount = surface.Impacts.Count;
            var fired = controller.FireOnceForTests();
            Physics.SyncTransforms();
            Require(fired, "controller failed to fire no-VR shot");
            Require(controller.CurrentMagazine == 2, "controller did not consume ammo");
            Require(controller.TracerCount == 1, "controller did not spawn visible tracer");
            Require(surface.Impacts.Count == impactCount + 1, "controller did not record target impact");

            var beforeAim = controller.CurrentAimDirection;
            controller.AdjustFrontHandForTests(new Vector2(0.12f, 0.08f));
            var afterAim = controller.CurrentAimDirection;
            Require(Vector3.Angle(beforeAim, afterAim) > 1f, "front hand adjustment did not change gun line");

            controller.SetAimModeForTests(WeaponAimMode.AimDownSights);
            Require(controller.CurrentAimMode == WeaponAimMode.AimDownSights, "controller did not enter ADS mode");
            Require(Vector3.Angle(camera.transform.forward, controller.CurrentAimDirection) < 0.5f,
                "ADS camera is not aligned to gun line");
        }

        static GameObject Find(string id)
        {
            var gameObject = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
            Require(gameObject != null, $"missing scene test id: {id}");
            return gameObject;
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"TASK005_VALIDATION_FAIL: {message}");
            }
        }
    }
}
