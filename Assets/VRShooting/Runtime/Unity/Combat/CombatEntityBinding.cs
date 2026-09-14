using UnityEngine;

namespace VRShooting.Unity.Combat
{
    /// <summary>Scene-owned stable ID. The service owns alive/dead state.</summary>
    public sealed class CombatEntityBinding : MonoBehaviour
    {
        [SerializeField] string entityId;
        public string EntityId { get => entityId; set => entityId = value; }
    }
}
