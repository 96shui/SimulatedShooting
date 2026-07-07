using VRShooting.Input;
using VRShooting.Application.Weapons;

namespace VRShooting.Application
{
    /// <summary>
    /// P1 应用服务组合根（task002）。
    /// </summary>
    public sealed class ApplicationServices
    {
        ApplicationServices(
            IGameEventBus eventBus,
            IUIRouter router,
            ITrainingSessionService trainingSessions,
            IXRTrainingInput trainingInput,
            IWeaponControlService weaponControl,
            IWeaponService weapons,
            IAmmoService ammo,
            IHUDService hud,
            IZeroingService zeroing)
        {
            EventBus = eventBus;
            Router = router;
            TrainingSessions = trainingSessions;
            TrainingInput = trainingInput;
            WeaponControl = weaponControl;
            Weapons = weapons;
            Ammo = ammo;
            Hud = hud;
            Zeroing = zeroing;
        }

        public IGameEventBus EventBus { get; }

        public IUIRouter Router { get; }

        public ITrainingSessionService TrainingSessions { get; }

        public IXRTrainingInput TrainingInput { get; }

        public IWeaponControlService WeaponControl { get; }

        public IWeaponService Weapons { get; }

        public IAmmoService Ammo { get; }

        public IHUDService Hud { get; }

        public IZeroingService Zeroing { get; }

        public static ApplicationServices CreateDefault()
        {
            var eventBus = new GameEventBus();
            var router = new UIRouter(eventBus);
            var trainingSessions = new TrainingSessionService(eventBus);
            var trainingInput = new InputSystemXRTrainingInput();
            var weaponControl = new WeaponControlService(eventBus);
            var zeroing = new ZeroingService(eventBus, trainingSessions, weaponControl);
            var hud = new ZeroingHudService(eventBus, trainingSessions, weaponControl, weaponControl);
            return new ApplicationServices(
                eventBus,
                router,
                trainingSessions,
                trainingInput,
                weaponControl,
                weaponControl,
                weaponControl,
                hud,
                zeroing);
        }
    }
}
