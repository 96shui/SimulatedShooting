using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class ZeroingRangeSessionBootstrap : MonoBehaviour
    {
        [SerializeField] private bool autoStartSessionOnAwake = true;
        [SerializeField] private int sessionSeed = 100;

        ApplicationServices services;
        bool ownsServices;

        public ApplicationServices Services => services;

        public bool HasActiveSession => services != null && services.TrainingSessions.HasActiveSession;

        public string ActiveSessionId => HasActiveSession ? services.TrainingSessions.Current.SessionId : string.Empty;

        void Awake()
        {
            EnsureServices();
            if (autoStartSessionOnAwake)
            {
                EnsureZeroingSession();
            }
        }

        void OnDestroy()
        {
            if (!ownsServices || services == null)
            {
                return;
            }

            services = null;
            ownsServices = false;
        }

        public ApplicationServices EnsureServices()
        {
            if (services != null)
            {
                return services;
            }

            if (GameMain.Instance != null)
            {
                services = GameMain.Instance.Services;
                P1PersistentUIHost.EnsureExists();
                return services;
            }

            var gameMainObject = new GameObject("GameMain");
            gameMainObject.AddComponent<GameMain>();
            services = GameMain.Instance.Services;
            P1PersistentUIHost.EnsureExists();
            ownsServices = false;
            return services;
        }

        public bool EnsureZeroingSession()
        {
            var resolved = EnsureServices();
            if (resolved.TrainingSessions.HasActiveSession)
            {
                EnsureWeaponSession(resolved.TrainingSessions.Current.SessionId);
                return true;
            }

            var zeroing = resolved.Zeroing.StartSession(RandomSeed.Fixed(sessionSeed), WeaponControlService.TrainingRifleId);
            if (!zeroing.Success)
            {
                Debug.LogError($"[{nameof(ZeroingRangeSessionBootstrap)}] failed to start zeroing session: {zeroing.Message}", this);
                return false;
            }

            return EnsureWeaponSession(zeroing.Data.SessionId);
        }

        public void ConfigureForTests(ApplicationServices testServices, bool startSession = true)
        {
            services = testServices;
            ownsServices = false;
            if (startSession)
            {
                EnsureZeroingSession();
            }
        }

        static bool EnsureWeaponSession(ApplicationServices resolved, string sessionId)
        {
            var training = resolved.TrainingSessions.Current;
            var weapon = resolved.WeaponControl.StartSession(
                sessionId,
                training.WeaponId,
                training.Mode);
            if (!weapon.Success)
            {
                Debug.LogError($"[{nameof(ZeroingRangeSessionBootstrap)}] failed to start weapon session: {weapon.Message}");
                return false;
            }

            return true;
        }

        bool EnsureWeaponSession(string sessionId)
        {
            return EnsureWeaponSession(services, sessionId);
        }
    }
}
