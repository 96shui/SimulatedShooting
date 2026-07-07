using System.Collections.Generic;

namespace VRShooting.Common
{
    public readonly struct WeaponDefinitionDto
    {
        public string WeaponId { get; init; }
        public string DisplayName { get; init; }
        public WeaponType Type { get; init; }
        public int MagazineCapacity { get; init; }
        public int MaxReserveAmmo { get; init; }
        public RecoilLevel Recoil { get; init; }
        public IReadOnlyList<TrainingMode> ApplicableModes { get; init; }
    }
}
