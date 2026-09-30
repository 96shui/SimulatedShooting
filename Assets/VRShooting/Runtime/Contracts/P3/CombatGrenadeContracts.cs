using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Contracts;
using VRShooting.Common;

namespace VRShooting.Common
{
    public readonly struct GrenadeThrowPlanDto
    {
        readonly string sessionId, grenadeId, throwerId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public string GrenadeId { get => grenadeId ?? string.Empty; init => grenadeId = value ?? string.Empty; }
        public string ThrowerId { get => throwerId ?? string.Empty; init => throwerId = value ?? string.Empty; }
        public Vector3 Origin { get; init; }
        public Vector3 Target { get; init; }
        public double ThrowTime { get; init; }
        public float WindupSeconds { get; init; }
        public float FlightSeconds { get; init; }
        public double ExplosionTime { get; init; }
        public float BlastRadius { get; init; }
        public float ThrowSpeed { get; init; }
    }

    public static class GrenadeTrajectory
    {
        public static Vector3 Position(Vector3 origin, Vector3 target, float fraction)
        {
            var t = Mathf.Clamp01(fraction);
            return Vector3.Lerp(origin, target, t) + Vector3.up * (4 * t * (1 - t) * Mathf.Clamp(Vector3.Distance(origin,target) * .22f,.5f,3f));
        }
        public static Vector3 Position(GrenadeThrowPlanDto plan, double now)
            => Position(plan.Origin, plan.Target, (float)((now-plan.ThrowTime-plan.WindupSeconds)/Math.Max(.01,plan.FlightSeconds)));
    }

    public readonly struct GrenadeExplosionDto
    {
        readonly string sessionId, grenadeId, throwerId;
        readonly IReadOnlyList<string> targets;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public string GrenadeId { get => grenadeId ?? string.Empty; init => grenadeId = value ?? string.Empty; }
        public string ThrowerId { get => throwerId ?? string.Empty; init => throwerId = value ?? string.Empty; }
        public Vector3 Position { get; init; }
        public IReadOnlyList<string> Targets { get => targets ?? Array.Empty<string>(); init => targets = ContractCollection.Copy(value); }
        public double Time { get; init; }
    }
}

namespace VRShooting.Application
{
    public interface ICombatGrenadeExplosionPort
    {
        ServiceResult<IReadOnlyList<string>> ApplyGrenadeExplosion(string sessionId, string grenadeId, string throwerId, Vector3 position, float radius, IReadOnlyList<string> exposedTargets);
    }

    public interface ICombatGrenadeTacticService : IDisposable
    {
        event Action<GrenadeThrowPlanDto> GrenadeThrown;
        event Action<GrenadeExplosionDto> GrenadeExploded;
        event Action GrenadeCancelled;
        GrenadeThrowPlanDto? Current { get; }
        float Elapsed { get; }
        Vector3 ProjectilePosition { get; }
        void Configure(ICombatGrenadeWorld world);
        void Cancel();
        void RequestFirstTeammateThrow();
        ServiceResult<Unit> Start(string sessionId);
        ServiceResult<Unit> Advance();
    }

    public interface ICombatGrenadeWorld
    {
        bool TryGetThrowPose(string memberId, out Vector3 feet, out Vector3 release);
        bool Sweep(Vector3 from, Vector3 to, out Vector3 impact);
        bool IsExposed(Vector3 blast, Vector3 enemyFeet);
    }
}
