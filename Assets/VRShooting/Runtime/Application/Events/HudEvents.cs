using VRShooting.Common;

namespace VRShooting.Application.Events
{
    public readonly struct HudUpdatedEvent
    {
        public HudDto Hud { get; init; }
    }
}
