using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.P3.TestSupport
{
    // Application lifecycle substitute only; never registered by the production composition root.
    public sealed class CombatApplicationFixture : ICombatSceneLoader, ICombatSummaryStore
    {
        public readonly List<TaskCompletionSource<ServiceResult<ICombatSceneLease>>> Loads = new List<TaskCompletionSource<ServiceResult<ICombatSceneLease>>>();
        public readonly List<CancellationToken> Tokens = new List<CancellationToken>();
        public readonly List<CombatSummaryDto> Saved = new List<CombatSummaryDto>();
        public bool SaveFails;
        public Action DuringSave;
        public Task<ServiceResult<ICombatSceneLease>> LoadAsync(TrainingMode mode, CancellationToken cancellation)
        {
            var completion = new TaskCompletionSource<ServiceResult<ICombatSceneLease>>();
            Loads.Add(completion); Tokens.Add(cancellation); return completion.Task;
        }
        public void Complete(int index, Lease lease) => Loads[index].SetResult(ServiceResult<ICombatSceneLease>.Ok(lease));
        public ServiceResult<Unit> SaveLatest(CombatSummaryDto summary)
        {
            DuringSave?.Invoke();
            if (SaveFails) return ServiceResult<Unit>.Fail(ErrorCode.PersistenceFailed);
            Saved.Add(summary); return ServiceResult<Unit>.Ok(Unit.Value);
        }
        public ServiceResult<CombatSummaryDto> GetLatest(TrainingMode mode) => ServiceResult<CombatSummaryDto>.Fail(ErrorCode.NotFound);
        public sealed class Lease : ICombatSceneLease
        {
            public Lease(TrainingMode mode) { Definition = mode == TrainingMode.Trench ? P3Fixtures.TrenchDefinition : P3Fixtures.UrbanDefinition; }
            public CombatSceneDefinitionDto Definition { get; set; }
            public FakeCombatClock TestClock { get; } = new FakeCombatClock();
            public ICombatClock Clock => TestClock;
            public ICombatRandom Random { get; } = new FakeCombatRandom(RandomSeed.Fixed(1));
            public ICombatNavigationPort Navigation { get; } = new FakeCombatWorld();
            public int Activations, Deactivations, Disposals;
            public bool FailActivation;
            public Action DuringActivation, DuringDeactivation;
            public ICombatCoreService Core;
            public ICombatWorldInputPort World;
            public string SessionId;
            public ServiceResult<Unit> Activate(ICombatCoreService core, ICombatWorldInputPort world, ICombatStateService state, IHUDService hud, ISquadCommandService squad, ICombatTickPort tick, string sessionId)
            {
                Activations++; Core = core; World = world; SessionId = sessionId;
                DuringActivation?.Invoke();
                return FailActivation ? ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable) : ServiceResult<Unit>.Ok(Unit.Value);
            }
            public void Deactivate() { Deactivations++; DuringDeactivation?.Invoke(); }
            public void Dispose() { Disposals++; }
        }
    }
}
