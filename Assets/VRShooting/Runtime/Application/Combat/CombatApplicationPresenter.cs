using System;
using VRShooting.Common;

namespace VRShooting.Application.Combat
{
    /// <summary>Lifecycle/status rendering only. UI actions call the coordinator, HUD reads mission DTOs.</summary>
    public sealed class CombatApplicationPresenter : IDisposable
    {
        readonly CombatApplicationCoordinator application;
        readonly ICombatApplicationView view;
        bool disposed;
        public CombatApplicationPresenter(CombatApplicationCoordinator application, ICombatApplicationView view)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            application.Changed += OnChanged;
            view.Render(application.Snapshot);
        }
        void OnChanged(CombatApplicationSnapshotDto snapshot) { if (!disposed) view.Render(snapshot); }
        public void Dispose() { if (disposed) return; disposed = true; application.Changed -= OnChanged; }
    }
}
