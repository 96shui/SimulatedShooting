using System;
using System.Threading;
using System.Threading.Tasks;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>BDD14/17/18/21. Unity-main-thread lifecycle owner, with cancellation-safe exclusive scene leases.</summary>
    public sealed class CombatApplicationCoordinator : IDisposable
    {
        readonly IUIRouter router;
        readonly ICombatSceneLoader loader;
        readonly ICombatSummaryStore store;
        CancellationTokenSource loading;
        ICombatSceneLease scene;
        TrainingMode? mode;
        RandomSeed seed;
        long generation;
        bool busy, publishing, disposingScene, saving, disposed;
        ErrorCode error;
        ScreenId screen = ScreenId.MainMenu;
        public CombatMission Mission { get; private set; }
        public CombatApplicationSnapshotDto Snapshot => new CombatApplicationSnapshotDto { Screen = screen, Mode = mode,
            Busy = busy, Error = error, SessionId = Mission?.SessionId, Summary = Mission?.Summary };
        public event Action<CombatApplicationSnapshotDto> Changed;
        public CombatApplicationCoordinator(IUIRouter router, ICombatSceneLoader loader, ICombatSummaryStore store)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.loader = loader ?? new UnavailableSceneLoader();
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }
        public ServiceResult<Unit> OpenMode(TrainingMode selected)
        {
            var guard = Guard(true); if (!guard.Success) return guard;
            if (selected != TrainingMode.Trench && selected != TrainingMode.Urban) return Fail(ErrorCode.InvalidInput);
            // Leaving a completed result via another mode must obey the same save boundary as Return.
            var saved = SaveResult(); if (!saved.Success) return Reject(saved.ErrorCode);
            CancelLoad(); CleanupScene(); mode = selected; error = ErrorCode.None;
            return Navigate(MapScreen);
        }
        public async Task<ServiceResult<Unit>> SelectMapAsync(string mapId, RandomSeed randomSeed)
        {
            var guard = Guard(); if (!guard.Success) return guard;
            if (!mode.HasValue || screen != MapScreen || Mission != null) return Fail(ErrorCode.InvalidState);
            if (mapId != (mode == TrainingMode.Trench ? P3ContractIds.TrenchMap : P3ContractIds.UrbanMap)) return Fail(ErrorCode.NotFound);
            var selected = mode.Value;
            var operation = ++generation;
            var cancellation = new CancellationTokenSource(); loading = cancellation;
            busy = true; error = ErrorCode.None; Publish();
            ICombatSceneLease loaded = null;
            try
            {
                var result = await loader.LoadAsync(selected, cancellation.Token);
                loaded = result.Data;
                if (disposed || operation != generation) return Fail(ErrorCode.InvalidState);
                // Cancellation/switching is allowed while awaiting a load, not during synchronous activation.
                loading = null;
                if (!result.Success || loaded == null) return Reject(result.Success ? ErrorCode.ResourceUnavailable : result.ErrorCode);
                var definition = loaded.Definition;
                var valid = definition.Validate();
                if (!valid.Success || definition.Mode != selected || definition.MapId != mapId)
                    return Reject(valid.Success ? ErrorCode.InvalidInput : valid.ErrorCode);
                scene = loaded; loaded = null; seed = randomSeed;
                CreateMission();
                if (selected == TrainingMode.Trench) return Navigate(ScreenId.TrenchBriefing);
                return StartInternal();
            }
            catch (OperationCanceledException)
            { return operation != generation || disposed ? Fail(ErrorCode.InvalidState) : Reject(ErrorCode.ResourceUnavailable); }
            catch (Exception)
            {
                if (operation != generation || disposed) return Fail(ErrorCode.InvalidState);
                CleanupScene(); return Reject(ErrorCode.ResourceUnavailable);
            }
            finally
            {
                loaded?.Dispose();
                cancellation.Dispose();
                if (operation == generation && !disposed) { loading = null; busy = false; Publish(); }
            }
        }
        public ServiceResult<Unit> Start()
        {
            var guard = Guard(); if (!guard.Success) return guard;
            if (Mission?.SessionId.Length > 0) return Fail(ErrorCode.Busy);
            if (screen != ScreenId.TrenchBriefing || Mission == null) return Fail(ErrorCode.InvalidState);
            busy = true; Publish();
            try { return StartInternal(); }
            finally { busy = false; Publish(); }
        }
        ServiceResult<Unit> StartInternal()
        {
            try
            {
                var started = Mission.Start(seed);
                if (!started.Success) { CleanupScene(); Navigate(MapScreen); return Reject(started.ErrorCode); }
                var attached = scene.Activate(Mission.Core, Mission.World, Mission.State, Mission.Hud, Mission.Squad, Mission, Mission.SessionId);
                if (!attached.Success) { CleanupScene(); Navigate(MapScreen); return Reject(attached.ErrorCode); }
                return Navigate(mode == TrainingMode.Trench ? ScreenId.TrenchHud : ScreenId.UrbanStreetHud);
            }
            catch (Exception) { CleanupScene(); Navigate(MapScreen); return Reject(ErrorCode.ResourceUnavailable); }
        }
        public ServiceResult<Unit> Retry()
        {
            var guard = Guard(); if (!guard.Success) return guard;
            if (Mission == null || !Mission.Summary.HasValue || scene == null) return Fail(ErrorCode.InvalidState);
            busy = true; error = ErrorCode.None; Publish();
            try
            {
                CleanupMission(); CreateMission();
                return mode == TrainingMode.Trench ? Navigate(ScreenId.TrenchBriefing) : StartInternal();
            }
            catch (Exception) { CleanupScene(); Navigate(MapScreen); return Reject(ErrorCode.ResourceUnavailable); }
            finally { busy = false; Publish(); }
        }
        public ServiceResult<Unit> BackToMaps()
        {
            var guard = Guard(true); if (!guard.Success) return guard;
            if (!mode.HasValue) return Fail(ErrorCode.InvalidState);
            var saved = SaveResult(); if (!saved.Success) return Reject(saved.ErrorCode);
            CancelLoad(); CleanupScene(); error = ErrorCode.None; return Navigate(MapScreen);
        }
        public ServiceResult<Unit> ReturnToMainMenu()
        {
            var guard = Guard(true); if (!guard.Success) return guard;
            var saved = SaveResult(); if (!saved.Success) return Reject(saved.ErrorCode);
            CancelLoad(); CleanupScene(); mode = null; error = ErrorCode.None; return Navigate(ScreenId.MainMenu);
        }
        ServiceResult<Unit> SaveResult()
        {
            if (Mission == null || !Mission.Summary.HasValue) return Ok();
            saving = true;
            try { return store.SaveLatest(Mission.Summary.Value); }
            catch (Exception) { return Fail(ErrorCode.PersistenceFailed); }
            finally { saving = false; }
        }
        public ServiceResult<Unit> Advance()
        {
            var guard = Guard(); if (!guard.Success) return guard;
            return Mission == null ? Fail(ErrorCode.InvalidState) : Mission.Advance();
        }
        void CreateMission() { Mission = new CombatMission(scene); Mission.Changed += OnMission; }
        void OnMission()
        {
            if (disposed || busy || Mission == null) return;
            if (Mission.Summary.HasValue) Navigate(mode == TrainingMode.Trench ? ScreenId.TrenchResults : ScreenId.UrbanResults);
            else if (Mission.Urban != null && Mission.SessionId.Length > 0)
            {
                var state = Mission.Urban.GetSession(Mission.SessionId);
                if (state.Success && state.Data.Phase != UrbanPhase.Results)
                    Navigate(state.Data.Phase == UrbanPhase.Building ? ScreenId.UrbanBuildingHud : ScreenId.UrbanStreetHud);
            }
            else Publish();
        }
        void CleanupMission()
        {
            var wasDisposing = disposingScene; disposingScene = true;
            try
            {
                var old = Mission; Mission = null;
                if (old != null) { old.Changed -= OnMission; old.Dispose(); }
                scene?.Deactivate();
            }
            finally { disposingScene = wasDisposing; }
        }
        void CleanupScene()
        {
            var old = scene; disposingScene = true;
            try { CleanupMission(); }
            finally
            {
                scene = null;
                try { old?.Dispose(); }
                finally { disposingScene = false; }
            }
        }
        void CancelLoad()
        {
            generation++; var old = loading; loading = null; busy = false;
            // The async operation owns CTS disposal so an uncancellable loader can still inspect its token.
            old?.Cancel();
        }
        ScreenId MapScreen => mode == TrainingMode.Trench ? ScreenId.TrenchMapSelection : ScreenId.UrbanMapSelection;
        ServiceResult<Unit> Navigate(ScreenId next)
        {
            // Router subscribers must not reenter lifecycle while a page transition is being published.
            publishing = true;
            ServiceResult<ScreenId> routed;
            try { routed = router.Open(next, new NavigationArgs { Mode = mode, SessionId = Mission?.SessionId }); }
            finally { publishing = false; }
            if (!routed.Success) return Reject(routed.ErrorCode);
            screen = next; Publish(); return Ok();
        }
        void Publish()
        {
            if (disposed || publishing) return;
            publishing = true; try { Changed?.Invoke(Snapshot); } finally { publishing = false; }
        }
        ServiceResult<Unit> Guard(bool allowLoading = false) => disposed ? Fail(ErrorCode.InvalidState)
            : publishing || disposingScene || saving || (busy && !(allowLoading && loading != null)) ? Fail(ErrorCode.Busy) : Ok();
        ServiceResult<Unit> Reject(ErrorCode code) { error = code; Publish(); return Fail(code); }
        static ServiceResult<Unit> Ok() => ServiceResult<Unit>.Ok(Unit.Value);
        static ServiceResult<Unit> Fail(ErrorCode code) => ServiceResult<Unit>.Fail(code, "Combat application: " + code);
        public void Dispose()
        {
            if (disposed) return; disposed = true; Changed = null; CancelLoad(); CleanupScene();
        }
        sealed class UnavailableSceneLoader : ICombatSceneLoader
        {
            public Task<ServiceResult<ICombatSceneLease>> LoadAsync(TrainingMode mode, CancellationToken cancellation)
                => Task.FromResult(ServiceResult<ICombatSceneLease>.Fail(ErrorCode.ResourceUnavailable, "P3 production scene loader is not registered"));
        }
    }
}
