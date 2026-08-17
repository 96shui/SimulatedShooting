using VRShooting.Input;
using VRShooting.Application.Weapons;

namespace VRShooting.Application
{
    /// <summary>
    /// 应用服务组合根。P1 射校与 P2 移动靶共用。
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
            IZeroingService zeroing,
            IMovingTargetService movingTarget,
            ITrainingPresentationService presentation)
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
            MovingTarget = movingTarget;
            Presentation = presentation;
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

        public IMovingTargetService MovingTarget { get; }

        public ITrainingPresentationService Presentation { get; }

        public static ApplicationServices CreateDefault(IXRTrainingInput trainingInput = null)
        {
            var eventBus = new GameEventBus();
            var router = new UIRouter(eventBus);
            var trainingSessions = new TrainingSessionService(eventBus);
            var input = trainingInput ?? new InputSystemXRTrainingInput();
            var weaponControl = new WeaponControlService(eventBus);
            var zeroing = new ZeroingService(eventBus, trainingSessions, weaponControl);
            var movingTarget = new MovingTargetService(eventBus, trainingSessions);
            var presentation = new TrainingPresentationService(eventBus, trainingSessions, weaponControl, zeroing);
            var zeroingHud = new ZeroingHudService(eventBus, trainingSessions, zeroing, weaponControl, weaponControl);
            var movingTargetHud = new MovingTargetHudService(eventBus, trainingSessions, movingTarget, weaponControl);
            var hud = new TrainingHudService(trainingSessions, zeroingHud, movingTargetHud);
            return new ApplicationServices(
                eventBus,
                router,
                trainingSessions,
                input,
                weaponControl,
                weaponControl,
                weaponControl,
                hud,
                zeroing,
                movingTarget,
                presentation);
        }
    }
}
