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
            ITrainingSessionService trainingSessions)
        {
            EventBus = eventBus;
            Router = router;
            TrainingSessions = trainingSessions;
        }

        public IGameEventBus EventBus { get; }

        public IUIRouter Router { get; }

        public ITrainingSessionService TrainingSessions { get; }

        public static ApplicationServices CreateDefault()
        {
            var eventBus = new GameEventBus();
            var router = new UIRouter(eventBus);
            var trainingSessions = new TrainingSessionService(eventBus);
            return new ApplicationServices(eventBus, router, trainingSessions);
        }
    }
}
