using System;
using System.Collections.Generic;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.P3.TestSupport
{
    public sealed class FakeCombatClock : ICombatClock
    {
        public double Now { get; private set; }
        public long Tick { get; private set; }
        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || double.IsInfinity(Now + seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            Now += seconds; Tick++;
        }
    }
    public sealed class FakeCombatRandom : ICombatRandom
    {
        uint state;
        public FakeCombatRandom(RandomSeed seed) { Reset(seed); }
        public void Reset(RandomSeed seed) { state = unchecked((uint)seed.Value); if (state == 0) state = 0x6d2b79f5; }
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (int)(minInclusive + (long)(state % (ulong)((long)maxExclusive - minInclusive)));
        }
    }
    public sealed class FakeCombatWorld : ICombatWorldInputPort, ICombatNavigationPort, ICombatSceneView
    {
        readonly List<CombatInputDto> inputs = new List<CombatInputDto>();
        readonly List<CombatNavigationRequestDto> navigation = new List<CombatNavigationRequestDto>();
        public IReadOnlyList<CombatInputDto> Inputs => inputs.AsReadOnly();
        public IReadOnlyList<CombatNavigationRequestDto> NavigationRequests => navigation.AsReadOnly();
        public CombatVisualSnapshotDto Visual { get; private set; }
        public ErrorCode NextError { get; set; }
        public ServiceResult<Unit> Submit(CombatInputDto input) { inputs.Add(input); return Response(); }
        public ServiceResult<Unit> Move(CombatNavigationRequestDto request) { navigation.Add(request); return Response(); }
        public void Apply(CombatVisualSnapshotDto snapshot) { Visual = snapshot; }
        ServiceResult<Unit> Response()
        {
            var error = NextError; NextError = ErrorCode.None;
            return error == ErrorCode.None ? ServiceResult<Unit>.Ok(Unit.Value) : ServiceResult<Unit>.Fail(error);
        }
    }
    public sealed class FakeCombatSummaryStore : ICombatSummaryStore
    {
        readonly Dictionary<TrainingMode, CombatSummaryDto> summaries = new Dictionary<TrainingMode, CombatSummaryDto>();
        public ErrorCode NextError { get; set; }
        public ServiceResult<Unit> SaveLatest(CombatSummaryDto summary)
        {
            var error = NextError; NextError = ErrorCode.None;
            if (error != ErrorCode.None) return ServiceResult<Unit>.Fail(error);
            if (!IsCombat(summary.Mode) || string.IsNullOrWhiteSpace(summary.SessionId) || string.IsNullOrWhiteSpace(summary.MapId)) return ServiceResult<Unit>.Fail(ErrorCode.InvalidInput);
            summaries[summary.Mode] = summary;
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        public ServiceResult<CombatSummaryDto> GetLatest(TrainingMode mode)
        {
            if (!IsCombat(mode)) return ServiceResult<CombatSummaryDto>.Fail(ErrorCode.InvalidInput);
            return summaries.TryGetValue(mode, out var summary) ? ServiceResult<CombatSummaryDto>.Ok(summary) : ServiceResult<CombatSummaryDto>.Fail(ErrorCode.NotFound);
        }
        static bool IsCombat(TrainingMode mode) => mode == TrainingMode.Trench || mode == TrainingMode.Urban;
    }
}
