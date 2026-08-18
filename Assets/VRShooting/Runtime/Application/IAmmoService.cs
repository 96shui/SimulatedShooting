using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public interface IAmmoService
    {
        ServiceResult<AmmoDto> GetAmmo(string sessionId);
        ServiceResult<AmmoDto> ConsumeAmmo(string sessionId, int amount);
        ServiceResult<AmmoReservationDto> ReserveAmmo(string sessionId, int amount, string reservationId);
        ServiceResult<AmmoDto> ConsumeReservedAmmo(string sessionId, string reservationId, int amount);
        ServiceResult<AmmoDto> ReleaseAmmoReservation(string sessionId, string reservationId);
        ServiceResult<AmmoDto> StartReload(string sessionId);
        ServiceResult<AmmoDto> CompleteReload(string sessionId);
    }
}
