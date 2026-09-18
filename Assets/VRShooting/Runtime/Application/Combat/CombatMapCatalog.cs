using VRShooting.Common;

namespace VRShooting.Application.Combat
{
    // Map selection metadata, before acquiring a scene lease. Actual map data is validated at load.
    public static class CombatMapCatalog
    {
        public static TrenchMapDto Trench => new TrenchMapDto { MapId=P3ContractIds.TrenchMap,
            DisplayName="堑壕训练场", Difficulty=DifficultyLevel.Low, MinEnemyCount=3, MaxEnemyCount=5 };
        public static UrbanMapDto Urban => new UrbanMapDto { MapId=P3ContractIds.UrbanMap,
            DisplayName="城镇训练场", StreetEnemyMin=1, StreetEnemyMax=2, BuildingEnemyMin=3, BuildingEnemyMax=6,
            BuildingEntranceId=P3ContractIds.UrbanEntrance,
            Floors=new[]{new FloorDto{FloorId="floor-1",DisplayName="1F"},new FloorDto{FloorId="floor-2",DisplayName="2F"},new FloorDto{FloorId="floor-3",DisplayName="3F"}} };
    }
}
