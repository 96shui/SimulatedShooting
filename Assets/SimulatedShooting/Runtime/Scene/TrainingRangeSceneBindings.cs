using System.Collections.Generic;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class TrainingRangeSceneBindings : MonoBehaviour
    {
        [SerializeField] Transform playerRootAnchor;
        [SerializeField] Transform proneHeadReference;
        [SerializeField] Transform aimForwardAnchor;
        [SerializeField] Transform largeUiAnchor;
        [SerializeField] Transform minimalHudAnchor;
        [SerializeField] Transform weaponRackAnchor;
        [SerializeField] Transform targetRootAnchor;

        public Transform PlayerRootAnchor => playerRootAnchor;
        public Transform ProneHeadReference => proneHeadReference;
        public Transform AimForwardAnchor => aimForwardAnchor;
        public Transform LargeUiAnchor => largeUiAnchor;
        public Transform MinimalHudAnchor => minimalHudAnchor;
        public Transform WeaponRackAnchor => weaponRackAnchor;
        public Transform TargetRootAnchor => targetRootAnchor;

        public IReadOnlyList<Transform> AllAnchors => new[]
        {
            playerRootAnchor,
            proneHeadReference,
            aimForwardAnchor,
            largeUiAnchor,
            minimalHudAnchor,
            weaponRackAnchor,
            targetRootAnchor
        };

        public void Configure(
            Transform playerRoot,
            Transform proneHead,
            Transform aimForward,
            Transform largeUi,
            Transform minimalHud,
            Transform weaponRack,
            Transform targetRoot)
        {
            playerRootAnchor = playerRoot;
            proneHeadReference = proneHead;
            aimForwardAnchor = aimForward;
            largeUiAnchor = largeUi;
            minimalHudAnchor = minimalHud;
            weaponRackAnchor = weaponRack;
            targetRootAnchor = targetRoot;
        }

        public bool ValidateBindings(out string error)
        {
            var names = new[]
            {
                nameof(PlayerRootAnchor),
                nameof(ProneHeadReference),
                nameof(AimForwardAnchor),
                nameof(LargeUiAnchor),
                nameof(MinimalHudAnchor),
                nameof(WeaponRackAnchor),
                nameof(TargetRootAnchor)
            };
            var anchors = AllAnchors;
            var errors = new List<string>();
            var unique = new HashSet<Transform>();

            for (var index = 0; index < anchors.Count; index++)
            {
                var anchor = anchors[index];
                if (anchor == null)
                {
                    errors.Add($"{names[index]} is missing");
                }
                else if (!unique.Add(anchor))
                {
                    errors.Add($"{names[index]} has a duplicate Transform binding ({anchor.name})");
                }
            }

            error = errors.Count == 0
                ? string.Empty
                : $"[{nameof(TrainingRangeSceneBindings)}:{name}] {string.Join("; ", errors)}";
            return errors.Count == 0;
        }

        [ContextMenu("Validate Training Range Bindings")]
        public void ValidateAndLog()
        {
            if (!ValidateBindings(out var error))
                Debug.LogError(error, this);
        }

        void Awake()
        {
            ValidateAndLog();
        }

        void OnValidate()
        {
            if (gameObject.scene.IsValid())
                ValidateAndLog();
        }
    }
}
