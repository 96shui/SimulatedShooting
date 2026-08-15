using System;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    public enum MovingTargetTravelDirection
    {
        Stationary,
        RightToLeft,
        LeftToRight
    }

    [Serializable]
    public readonly struct MovingTargetVisualState
    {
        public MovingTargetVisualState(
            float routeProgress01,
            MovingTargetTravelDirection direction,
            bool endpointHold,
            bool canShoot,
            float speedMetresPerSecond)
        {
            RouteProgress01 = Mathf.Clamp01(routeProgress01);
            Direction = direction;
            EndpointHold = endpointHold;
            CanShoot = canShoot;
            SpeedMetresPerSecond = Mathf.Max(0f, speedMetresPerSecond);
        }

        public float RouteProgress01 { get; }
        public MovingTargetTravelDirection Direction { get; }
        public bool EndpointHold { get; }
        public bool CanShoot { get; }
        public float SpeedMetresPerSecond { get; }
    }

    [DisallowMultipleComponent]
    public sealed class MovingTargetVisualDriver : MonoBehaviour
    {
        [SerializeField] MovingTargetRouteBinding routeBinding;
        [SerializeField] GameObject endpointHoldIndicator;
        [SerializeField] GameObject shootingAllowedIndicator;
        [SerializeField] Quaternion baseVisualRotation = Quaternion.identity;

        public MovingTargetVisualState CurrentState { get; private set; }
        public MovingTargetRouteBinding RouteBinding => routeBinding;

        public void Configure(
            MovingTargetRouteBinding binding,
            GameObject holdIndicator,
            GameObject canShootIndicator)
        {
            routeBinding = binding;
            endpointHoldIndicator = holdIndicator;
            shootingAllowedIndicator = canShootIndicator;
            if (routeBinding != null && routeBinding.TargetVisualRoot != null)
                baseVisualRotation = routeBinding.TargetVisualRoot.localRotation;
            Apply(new MovingTargetVisualState(0f, MovingTargetTravelDirection.Stationary, true, false, 0f));
        }

        public void Apply(MovingTargetVisualState state)
        {
            CurrentState = state;
            if (routeBinding != null)
            {
                routeBinding.ApplyNormalizedProgress(state.RouteProgress01);
                if (routeBinding.TargetVisualRoot != null)
                {
                    routeBinding.TargetVisualRoot.localRotation = state.Direction == MovingTargetTravelDirection.LeftToRight
                        ? baseVisualRotation * Quaternion.Euler(0f, 180f, 0f)
                        : baseVisualRotation;
                }
            }
            if (endpointHoldIndicator != null)
                endpointHoldIndicator.SetActive(state.EndpointHold);
            if (shootingAllowedIndicator != null)
                shootingAllowedIndicator.SetActive(state.CanShoot);
        }
    }
}
