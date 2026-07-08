using System;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public interface IHUDService
    {
        event Action<HudDto> HudUpdated;

        ServiceResult<HudDto> GetHud(string sessionId);
    }
}
