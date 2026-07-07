using System.Collections.Generic;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public interface IWeaponService
    {
        ServiceResult<IReadOnlyList<WeaponDefinitionDto>> GetWeapons();
        ServiceResult<WeaponDefinitionDto> GetWeapon(string weaponId);
        ServiceResult<WeaponDefinitionDto> GetEquippedWeapon();
        ServiceResult<WeaponDefinitionDto> SelectPreview(string weaponId);
        ServiceResult<WeaponDefinitionDto> Equip(string weaponId, TrainingMode? mode);
    }
}
