using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public interface IWeaponControlService
    {
        ServiceResult<WeaponControlStateDto> StartSession(string sessionId, string weaponId, TrainingMode mode);
        ServiceResult<WeaponControlStateDto> GetState(string sessionId);
        ServiceResult<WeaponShotResultDto> Fire(WeaponFireInputDto input);
        ServiceResult<WeaponControlStateDto> Reload(string sessionId);
        ServiceResult<WeaponControlStateDto> SetShoulder(string sessionId, ShoulderSide shoulderSide);
        ServiceResult<WeaponControlStateDto> ToggleShoulder(string sessionId);
        ServiceResult<WeaponControlStateDto> SetAimMode(string sessionId, WeaponAimMode aimMode);
        ServiceResult<WeaponControlStateDto> SetGripState(string sessionId, bool twoHandGripActive, float stability01);
    }
}
