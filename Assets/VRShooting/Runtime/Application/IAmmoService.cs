using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public interface IAmmoService
    {
        ServiceResult<AmmoDto> GetAmmo(string sessionId);
        ServiceResult<AmmoDto> ConsumeAmmo(string sessionId, int amount);
        ServiceResult<AmmoDto> StartReload(string sessionId);
        ServiceResult<AmmoDto> CompleteReload(string sessionId);
    }
}
