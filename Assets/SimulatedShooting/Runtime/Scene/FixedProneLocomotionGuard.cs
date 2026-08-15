using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class FixedProneLocomotionGuard : MonoBehaviour
    {
        [SerializeField] Transform playerRootAnchor;
        [SerializeField] GameObject xrOrigin;
        [SerializeField] GameObject locomotionRoot;

        public Transform PlayerRootAnchor => playerRootAnchor;
        public GameObject XrOrigin => xrOrigin;
        public GameObject LocomotionRoot => locomotionRoot;
        public bool ArtificialLocomotionDisabled =>
            playerRootAnchor != null &&
            (locomotionRoot == null || !locomotionRoot.activeSelf) &&
            (xrOrigin == null || xrOrigin.GetComponentsInChildren<LocomotionProvider>(true)
                .All(provider => provider == null || !provider.enabled));

        public void Configure(Transform fixedPlayerRoot, GameObject origin, GameObject locomotion)
        {
            playerRootAnchor = fixedPlayerRoot;
            xrOrigin = origin;
            locomotionRoot = locomotion;
            EnforcePolicy();
        }

        [ContextMenu("Enforce Fixed Prone Locomotion Policy")]
        public void EnforcePolicy()
        {
            if (locomotionRoot != null)
                locomotionRoot.SetActive(false);

            if (xrOrigin == null)
                return;

            foreach (var provider in xrOrigin.GetComponentsInChildren<LocomotionProvider>(true))
                provider.enabled = false;
        }

        public bool TryApplyArtificialMotionForTests(Vector3 translation, float yawDegrees)
        {
            EnforcePolicy();
            return false;
        }

        void Awake()
        {
            EnforcePolicy();
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
                EnforcePolicy();
        }
    }
}
