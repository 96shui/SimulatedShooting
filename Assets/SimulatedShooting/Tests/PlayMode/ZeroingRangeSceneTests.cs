using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRShooting.Common;
using VRShooting.Input;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class ZeroingRangeSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("ZeroingRangeScene", LoadSceneMode.Single);
            Physics.SyncTransforms();
            EnsureBootstrap();
        }

        static void EnsureBootstrap()
        {
            if (Object.FindObjectOfType<ZeroingRangeSessionBootstrap>() != null)
            {
                return;
            }

            var root = GameObject.Find("ZeroingRange") ?? new GameObject("ZeroingRange");
            if (root.GetComponent<ZeroingRangeSessionBootstrap>() == null)
            {
                root.AddComponent<ZeroingRangeSessionBootstrap>();
            }
        }

        [Test]
        public void Task004_SceneContainsStableTrainingAnchorsForVrAndNoVr()
        {
            Assert.That(Find("ZeroingRange.ShootingPosition"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Target.Primary"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>(), Is.Not.Null);
        }

        [Test]
        public void Task003_ZeroingRangeUsesTheSharedProneFiringStationContract()
        {
            var requiredIds = new[]
            {
                "ZeroingRange.FiringStation.Root",
                "ZeroingRange.FiringStation.PlayerRoot",
                "ZeroingRange.FiringStation.ProneHeadReference",
                "ZeroingRange.FiringStation.AimForward",
                "ZeroingRange.FiringStation.LargeUiAnchor",
                "ZeroingRange.FiringStation.MinimalHudAnchor",
                "ZeroingRange.FiringStation.WeaponRackAnchor"
            };

            foreach (var id in requiredIds)
                Assert.That(Find(id), Is.Not.Null, $"Missing task003 test ID: {id}");

            var bindings = Find("ZeroingRange.FiringStation.Root").GetComponent<TrainingRangeSceneBindings>();
            Assert.That(bindings.ValidateBindings(out var error), Is.True, error);
        }

        [Test]
        public void Task003_ZeroingRangeDisablesArtificialLocomotionWithoutLockingHeadReference()
        {
            var station = Find("ZeroingRange.FiringStation.Root");
            var bindings = station.GetComponent<TrainingRangeSceneBindings>();
            var guard = station.GetComponent<FixedProneLocomotionGuard>();
            var playerPose = (bindings.PlayerRootAnchor.position, bindings.PlayerRootAnchor.rotation);
            var headLocalPosition = bindings.ProneHeadReference.localPosition;

            Assert.That(guard.ArtificialLocomotionDisabled, Is.True);
            Assert.That(guard.TryApplyArtificialMotionForTests(Vector3.forward, 30f), Is.False);
            bindings.ProneHeadReference.localPosition += Vector3.right * 0.03f;

            Assert.That(bindings.PlayerRootAnchor.position, Is.EqualTo(playerPose.position));
            Assert.That(Quaternion.Angle(bindings.PlayerRootAnchor.rotation, playerPose.rotation), Is.LessThan(0.001f));
            Assert.That(bindings.ProneHeadReference.localPosition, Is.Not.EqualTo(headLocalPosition));
        }

        [Test]
        public void Task004_XrOriginUsesFloorTrackingWithoutArtificialEyeHeight()
        {
            var xrOriginObject = Find("ZeroingRange.Origin.VR");
            var xrOrigin = xrOriginObject.GetComponent<XROrigin>();
            Assert.That(xrOrigin, Is.Not.Null);
            Assert.That(xrOrigin.RequestedTrackingOriginMode,
                Is.EqualTo(XROrigin.TrackingOriginMode.Floor));
            Assert.That(xrOrigin.CameraYOffset, Is.EqualTo(0f).Within(0.001f));
            Assert.That(xrOrigin.CameraFloorOffsetObject, Is.Not.Null);
            Assert.That(xrOrigin.CameraFloorOffsetObject.transform.localPosition.y,
                Is.EqualTo(0f).Within(0.001f));

            var hmdCamera = xrOriginObject.GetComponentInChildren<Camera>(true);
            Assert.That(hmdCamera, Is.Not.Null);
        }

        [Test]
        public void Task004_TrainingRifleStartsWithinComfortableRightHandReach()
        {
            var xrOrigin = Find("ZeroingRange.Origin.VR").transform;
            var weaponSpawn = Find("ZeroingRange.FiringStation.WeaponRackAnchor").transform;
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle").transform;
            var rearGrip = Find("ZeroingRange.Weapon.Grip.RearHand").transform;

            var localSpawn = xrOrigin.InverseTransformPoint(weaponSpawn.position);
            var horizontalReach = new Vector2(localSpawn.x, localSpawn.z).magnitude;
            Assert.That(horizontalReach, Is.InRange(0.5f, 0.8f));
            Assert.That(localSpawn.y, Is.InRange(0.3f, 0.5f));
            Assert.That(Vector3.Distance(weapon.position, weaponSpawn.position), Is.LessThan(1f));

            var localRearGrip = xrOrigin.InverseTransformPoint(rearGrip.position);
            Assert.That(localRearGrip.z, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(localRearGrip.x), Is.LessThan(1.5f));
            Assert.That(localRearGrip.y, Is.InRange(0.2f, 1.3f));
        }

        [Test]
        public void Task004_TargetHasSpecifiedDimensionsAtOneHundredMetres()
        {
            var shootingPosition = Find("ZeroingRange.ShootingPosition").transform;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var backer = Find("ZeroingRange.Target.Backer").GetComponent<Renderer>();
            var face = Find("ZeroingRange.Target.Face").GetComponent<Renderer>();
            var tenRing = Find("ZeroingRange.Target.TenRing").GetComponent<Renderer>();

            Assert.That(Vector3.Distance(shootingPosition.position, target.position), Is.EqualTo(100f).Within(0.001f));
            Assert.That(backer.bounds.size.x, Is.EqualTo(1.4f).Within(0.001f));
            Assert.That(backer.bounds.size.y, Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(face.bounds.size.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(face.bounds.size.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(tenRing.bounds.size.x, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(tenRing.bounds.size.y, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void Task004_DistanceMarkersShowZeroThroughOneHundredMetres()
        {
            // BDD 05 / task004: the 100m firing lane must provide clear distance context.
            foreach (var distance in new[] { 0, 25, 50, 75, 100 })
            {
                foreach (var side in new[] { "Left", "Right" })
                {
                    var label = Find($"ZeroingRange.Environment.DistanceLabel.{distance}m.{side}");
                    Assert.That(label, Is.Not.Null, $"Missing {distance} m marker on the {side.ToLowerInvariant()} side");
                    var text = label.GetComponent<TextMesh>();
                    Assert.That(text.text, Is.EqualTo($"{distance} m"));
                    Assert.That(text.anchor, Is.EqualTo(TextAnchor.MiddleCenter));
                    Assert.That(text.alignment, Is.EqualTo(TextAlignment.Center));
                    Assert.That(text.fontStyle, Is.EqualTo(FontStyle.Bold));
                    Assert.That(text.characterSize, Is.EqualTo(0.035f).Within(0.001f));
                    Assert.That(text.color, Is.EqualTo(Color.white));
                    Assert.That(Quaternion.Angle(label.transform.localRotation, Quaternion.identity), Is.LessThan(0.001f));
                    var material = label.GetComponent<MeshRenderer>().sharedMaterial;
                    Assert.That(material.shader.name, Is.EqualTo("SimulatedShooting/World Space Text Occluded"));
                    Assert.That(material.GetFloat("_ZTest"), Is.EqualTo((float)CompareFunction.LessEqual));
                }
            }
        }

        [Test]
        public void Task004_TargetCanBeHitFromShootingPosition()
        {
            var origin = Find("ZeroingRange.ShootingPosition").transform.position;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var direction = (target.position - origin).normalized;

            Assert.That(Physics.Raycast(origin, direction, out var hit, 101f), Is.True);
            Assert.That(hit.transform == target || hit.transform.IsChildOf(target), Is.True);
        }

        [Test]
        public void Task004_FirstPersonCompositionFramesTargetThroughRangeLane()
        {
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            var target = Find("ZeroingRange.Target.Primary").transform;
            var viewportPoint = camera.WorldToViewportPoint(target.position);

            Assert.That(viewportPoint.z, Is.GreaterThan(0f));
            Assert.That(viewportPoint.x, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(viewportPoint.y, Is.InRange(0.35f, 0.65f));
            Assert.That(Find("ZeroingRange.Environment.GrassGround"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Environment.ForestedHill"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Reference"), Is.Not.Null);
        }

        [Test]
        public void Task012_RaycastHitReturnsStableImpactCoordinates()
        {
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var origin = Find("ZeroingRange.ShootingPosition").transform.position;
            var direction = (surface.TargetCenter.position - origin).normalized;

            Assert.That(surface.TryRecordRay(new Ray(origin, direction), 101f, out var impact), Is.True);
            Assert.That(impact.OffsetCm.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(impact.OffsetCm.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(impact.InsideTenRing, Is.True);
            Assert.That(surface.TenRingRadiusCm, Is.EqualTo(5f).Within(0.001f));
            Assert.That(Find("ZeroingRange.Target.Center"), Is.Not.Null);
            Assert.That(surface.Impacts.Count, Is.EqualTo(1));
            Assert.That(surface.ImpactMarkerRoot.childCount, Is.EqualTo(1));
        }

        [Test]
        public void Task012_ImpactCoordinatesUseCentimetresAndMatchVisualMarker()
        {
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var worldPoint = surface.TargetCenter.TransformPoint(new Vector3(0.12f, -0.08f, 0f));

            Assert.That(surface.TryRecordWorldPoint(worldPoint, out var impact), Is.True);
            Assert.That(impact.OffsetCm.x, Is.EqualTo(12f).Within(0.01f));
            Assert.That(impact.OffsetCm.y, Is.EqualTo(-8f).Within(0.01f));
            Assert.That(impact.InsideTenRing, Is.False);
            Assert.That(surface.ImpactMarkerRoot.GetChild(0).position.x,
                Is.EqualTo(impact.WorldPoint.x).Within(0.001f));
            Assert.That(surface.ImpactMarkerRoot.GetChild(0).position.y,
                Is.EqualTo(impact.WorldPoint.y).Within(0.001f));
        }

        [Test]
        public void Task012_ImpactOutsideFiftyCentimetreFaceIsRejected()
        {
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var outsideFace = surface.TargetCenter.TransformPoint(new Vector3(0.251f, 0f, 0f));

            Assert.That(surface.TryRecordWorldPoint(outsideFace, out _), Is.False);
            Assert.That(surface.Impacts, Is.Empty);
            Assert.That(surface.ImpactMarkerRoot.childCount, Is.Zero);
        }

        [Test]
        public void Task016_VisualPolishKeepsTargetAndShootingBayRecognisable()
        {
            Assert.That(Find("ZeroingRange.Environment.GrassGround"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Environment.ForestedHill"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Visual.RangeGate"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Visual.TargetBayPanels"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Visual.WeaponCrate.Left"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Lighting.Sun"), Is.Not.Null);
        }

        [Test]
        public void Task016_ForestedHillSitsBehindTargetAndUsesTreeBillboards()
        {
            var mountain = Find("ZeroingRange.Environment.ForestedHill");
            var target = Find("ZeroingRange.Target.Primary");
            var hillsideTrees = mountain.transform.Find("HillsideTrees");
            var trees = hillsideTrees.GetComponentsInChildren<Renderer>(true);

            Assert.That(mountain.GetComponentInChildren<MeshCollider>(), Is.Not.Null);
            Assert.That(mountain.GetComponentInChildren<Renderer>().bounds.size.x, Is.GreaterThanOrEqualTo(300f));
            Assert.That(hillsideTrees, Is.Not.Null);
            Assert.That(trees.Length, Is.EqualTo(84));
            Assert.That(trees.All(renderer => renderer.sharedMaterial.name == "RangeTreeBillboard"), Is.True);
            Assert.That(trees.All(renderer => renderer.shadowCastingMode == ShadowCastingMode.Off), Is.True);
            Assert.That(hillsideTrees.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(trees.Min(renderer => renderer.bounds.min.z),
                Is.GreaterThan(target.transform.position.z));
        }

        [Test]
        public void Task016_DistantHillRemainsClearFromTheFiringPosition()
        {
            Assert.That(RenderSettings.fog, Is.True);
            Assert.That(RenderSettings.fogMode, Is.EqualTo(FogMode.Linear));
            Assert.That(RenderSettings.fogStartDistance, Is.GreaterThanOrEqualTo(180f));
            Assert.That(RenderSettings.fogEndDistance, Is.GreaterThanOrEqualTo(320f));
            Assert.That(RenderSettings.fogColor.maxColorComponent, Is.LessThanOrEqualTo(0.55f));
        }

        [Test]
        public void Task016_GrassGroundUsesTiledPbrSurface()
        {
            var ground = Find("ZeroingRange.Environment.GrassGround");
            var groundMaterial = ground.GetComponent<Renderer>().sharedMaterial;

            Assert.That(ground.GetComponent<MeshCollider>(), Is.Not.Null);
            Assert.That(groundMaterial.name, Is.EqualTo("ZeroingGreenGrassGround"));
            Assert.That(groundMaterial.mainTexture, Is.Not.Null);
            Assert.That(groundMaterial.mainTexture.name, Is.EqualTo("ZeroingGrassSandBlend"));
            Assert.That(groundMaterial.GetTexture("_BumpMap"), Is.Not.Null);
            Assert.That(groundMaterial.color.g, Is.GreaterThan(groundMaterial.color.r));
            Assert.That(groundMaterial.color.g, Is.GreaterThan(groundMaterial.color.b));
        }

        [Test]
        public void Task016_SandPatchesBreakUpGrassWithoutReplacingTheGreenRange()
        {
            var renderers = GameObject.Find("ZeroingRange").GetComponentsInChildren<Renderer>(true);
            var sandPatches = renderers
                .Where(renderer => renderer.gameObject.name.StartsWith("SandPatch_"))
                .ToArray();

            Assert.That(sandPatches.Length, Is.EqualTo(48));
            Assert.That(sandPatches.All(renderer => renderer.sharedMaterial.name == "ZeroingGreenVariation"), Is.True);
            Assert.That(sandPatches.All(renderer => renderer.sharedMaterial.mainTexture != null), Is.True);
            Assert.That(sandPatches.All(renderer => renderer.sharedMaterial.color.r > renderer.sharedMaterial.color.g), Is.True);
            Assert.That(sandPatches.All(renderer => renderer.sharedMaterial.color.g > renderer.sharedMaterial.color.b), Is.True);
        }

        [Test]
        public void Task016_P2BasedRangeKeepsWideHillAndTargetBayGeometry()
        {
            var hill = Find("ZeroingRange.Environment.ForestedHill");
            var targetBays = Find("ZeroingRange.Visual.TargetBayPanels");
            var ridge = Find("ZeroingRange.Visual.RidgeWall");

            Assert.That(hill.GetComponentsInChildren<Renderer>(true)
                .Any(renderer => renderer.sharedMaterial.name == "ZeroingGreenHills"), Is.True);
            Assert.That(targetBays.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(24));
            Assert.That(targetBays.GetComponentsInChildren<Renderer>(true)
                .Max(renderer => renderer.bounds.max.x) - targetBays.GetComponentsInChildren<Renderer>(true)
                .Min(renderer => renderer.bounds.min.x), Is.GreaterThanOrEqualTo(175f));
            Assert.That(ridge.GetComponentsInChildren<Renderer>(true)
                .Max(renderer => renderer.bounds.max.x) - ridge.GetComponentsInChildren<Renderer>(true)
                .Min(renderer => renderer.bounds.min.x), Is.GreaterThanOrEqualTo(260f));
        }

        [Test]
        public void Task016_P2MovingTargetObjectsAreInactiveInP1()
        {
            var transforms = GameObject.Find("ZeroingRange").GetComponentsInChildren<Transform>(true);
            var route = transforms.Single(transform => transform.name == "MovingTargetRoute_40m");
            var movingTarget = transforms.Single(transform => transform.name == "Target_Moving_SideProfile");

            Assert.That(route.gameObject.activeSelf, Is.False);
            Assert.That(movingTarget.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Task016_LegacyPrimitiveTreeCrownsAreAbsent()
        {
            var crownCount = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Count(child => child.name.StartsWith("Crown_"));

            Assert.That(crownCount, Is.Zero);
        }

        [Test]
        public void Task016_WeaponCrateAppearsInLowerLeftWithoutBlockingTarget()
        {
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            var crate = Find("ZeroingRange.Visual.WeaponCrate.Left").transform;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var crateViewportPoint = camera.WorldToViewportPoint(crate.position + Vector3.up * 0.3f);
            var targetDirection = (target.position - camera.transform.position).normalized;

            Assert.That(crateViewportPoint.z, Is.GreaterThan(0f));
            Assert.That(crateViewportPoint.x, Is.InRange(0f, 0.45f));
            Assert.That(crateViewportPoint.y, Is.InRange(0f, 0.45f));
            Assert.That(Physics.Raycast(camera.transform.position, targetDirection, out var hit, 101f), Is.True);
            Assert.That(hit.transform == target || hit.transform.IsChildOf(target), Is.True);
        }

        [Test]
        public void Task016_SceneStaysWithinPrototypeRenderingBudget()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            var materials = renderers.SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            var activeLights = roots.SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .Where(light => light.isActiveAndEnabled)
                .ToArray();
            var hasEnabledPostProcessing = roots
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Any(component => component != null && component.enabled && component.GetType().Name == "Volume");

            Assert.That(renderers.Length, Is.LessThanOrEqualTo(1024));
            Assert.That(materials.Length, Is.LessThanOrEqualTo(32),
                $"Unexpected materials: {string.Join(", ", materials.Select(material => material.name))}");
            Assert.That(activeLights.Length, Is.EqualTo(1));
            Assert.That(activeLights[0].type, Is.EqualTo(LightType.Directional));
            Assert.That(hasEnabledPostProcessing, Is.False);
        }

        [Test]
        public void Task016_NoVrCameraHasClearTargetSightlineAndVrSafeSettings()
        {
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            var target = Find("ZeroingRange.Target.Primary").transform;
            var hudAnchor = Find("ZeroingRange.HudAnchor");
            var direction = (target.position - camera.transform.position).normalized;

            Assert.That(camera.allowHDR, Is.False);
            Assert.That(camera.allowMSAA, Is.True);
            Assert.That(camera.useOcclusionCulling, Is.True);
            Assert.That(hudAnchor, Is.Not.Null);
            Assert.That(Physics.Raycast(camera.transform.position, direction, out var hit, 101f), Is.True);
            Assert.That(hit.transform == target || hit.transform.IsChildOf(target), Is.True);
        }

        [Test]
        public void Task005_FirstPersonTrainingWeaponHasRequiredVisibleBindings()
        {
            var playerRoot = Find("ZeroingRange.Weapon.PlayerRoot");
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle");
            Assert.That(playerRoot, Is.Not.Null);
            Assert.That(weapon, Is.Not.Null);

            var controller = playerRoot.GetComponent<FirstPersonTrainingWeaponController>();
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(binding, Is.Not.Null);
            Assert.That(binding.HasRequiredBinding, Is.True);
            Assert.That(controller.HasVrPoseSources, Is.True);
            Assert.That(Find("ZeroingRange.Weapon.Grip.RearHand"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Grip.FrontHand"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Muzzle"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.AimLine"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.TracerRoot"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR.HeadPose"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR.RearHandPose"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR.FrontHandPose"), Is.Not.Null);
        }

        [Test]
        public void Task005_FeedbackUsesLicensedAudioAndProjectileVisualAssets()
        {
            var playerRoot = Find("ZeroingRange.Weapon.PlayerRoot");
            var controller = playerRoot.GetComponent<FirstPersonTrainingWeaponController>();
            var feedback = playerRoot.GetComponent<WeaponFeedbackController>();

            Assert.That(feedback, Is.Not.Null);
            Assert.That(controller.FeedbackController, Is.EqualTo(feedback));
            Assert.That(feedback.HasRequiredAudio, Is.True);
            Assert.That(feedback.HasProjectileVisualPrefab, Is.True);
            Assert.That(feedback.RifleShotClip.name, Does.Contain("rifle-sks-single-shot"));
            Assert.That(feedback.PickupClip.name, Does.Contain("weapon-pickup-mechanical"));
            Assert.That(feedback.BulletFlybyClip.name, Does.Contain("bullet-flyby"));
            Assert.That(Find("ZeroingRange.Weapon.Feedback"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.MuzzleFlash"), Is.Not.Null);

            Assert.That(playerRoot.GetComponent<PlayerFootstepAudio>(), Is.Null,
                "Fixed-prone P1 must not retain artificial footstep behavior.");
            Assert.That(Find("ZeroingRange.Player.Footsteps"), Is.Null);
        }

        [Test]
        public void Task005_TrainingRifleUsesLicensedQbz191Visual()
        {
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle");
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            var model = binding.RecoilRoot.Find("Model_QBZ191");

            Assert.That(model, Is.Not.Null, "The procedural blockout should be replaced by the QBZ-191 model");
            Assert.That(Find("ZeroingRange.Weapon.Visual.QBZ191"), Is.EqualTo(model.gameObject));

            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var triangleCount = filters.Sum(filter =>
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    return 0L;
                }

                return Enumerable.Range(0, mesh.subMeshCount)
                    .Sum(subMesh => (long)mesh.GetIndexCount(subMesh) / 3L);
            });

            Assert.That(filters.Length, Is.GreaterThan(0));
            Assert.That(renderers.Length, Is.GreaterThan(0));
            Assert.That(triangleCount, Is.GreaterThan(35000), "The first-person model lost its expected visual detail");
            Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
                .All(material => material != null && material.mainTexture != null), Is.True);
            var magazineRenderer = renderers.FirstOrDefault(renderer => renderer.sharedMaterials
                .Any(material => material != null && material.name.Contains("QBZ191_Magazine")));
            Assert.That(magazineRenderer, Is.Not.Null, "The QBZ-191 magazine renderer is missing");
            var magazineMesh = magazineRenderer.GetComponent<MeshFilter>()?.sharedMesh;
            Assert.That(magazineMesh, Is.Not.Null, "The QBZ-191 magazine mesh is missing");
            var magazineMaterialIndex = Enumerable.Range(0, magazineRenderer.sharedMaterials.Length)
                .First(index => magazineRenderer.sharedMaterials[index] != null &&
                                magazineRenderer.sharedMaterials[index].name.Contains("QBZ191_Magazine"));
            var magazineBounds = magazineMesh.GetSubMesh(
                Mathf.Min(magazineMaterialIndex, magazineMesh.subMeshCount - 1)).bounds;
            Assert.That(magazineBounds.size.y, Is.GreaterThan(0.18f));
            Assert.That(magazineBounds.center.y, Is.InRange(-0.105f, -0.093f),
                "The source magazine mesh has drifted away from its seated receiver position");
            Assert.That(magazineBounds.max.y, Is.InRange(-0.01f, 0f),
                "The magazine top should sit just inside the receiver instead of floating below it");
            Assert.That(Vector3.Distance(binding.RearHandGrip.localPosition,
                new Vector3(0.006f, -0.10f, -0.135f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(binding.FrontHandGrip.localPosition,
                new Vector3(0.006f, -0.015f, 0.18f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(binding.MagazinePoint.localPosition,
                new Vector3(-0.008f, -0.136f, 0.042f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(binding.RearHandGrip.position, binding.FrontHandGrip.position),
                Is.InRange(0.30f, 0.35f));
            Assert.That(binding.RecoilRoot.Find("Receiver"), Is.Null, "Legacy blockout geometry is still present");
        }

        [Test]
        public void Task005_NoVrFirstPersonWeaponFiresVisibleTracerAndTargetImpact()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var initialImpacts = surface.Impacts.Count;

            Assert.That(controller.InitializeForTests(), Is.True);
            var fired = controller.FireOnceForTests();
            Physics.SyncTransforms();

            Assert.That(fired, Is.True);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(2));
            Assert.That(controller.LastShotWasValid, Is.True);
            Assert.That(controller.TracerCount, Is.EqualTo(1));
            Assert.That(surface.Impacts.Count, Is.EqualTo(initialImpacts + 1),
                $"Shot hit={controller.LastShotHit}, object='{controller.LastHitObjectId}', " +
                $"muzzle={controller.LastShotMuzzlePosition}, hitPoint={controller.LastShotHitPoint}, " +
                $"aim={controller.CurrentAimDirection}");
            Assert.That(Find("ZeroingRange.Weapon.Tracer"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Tracer").GetComponent<BallisticTracerVisual>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Task005_ValidShotPlaysOneFeedbackStackAndArrivesAtRecordedImpact()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var feedback = controller.FeedbackController;

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            Assert.That(feedback.ValidShotFeedbackCount, Is.EqualTo(1));
            Assert.That(feedback.ImpactFeedbackCount, Is.Zero);
            var tracer = Find("ZeroingRange.Weapon.Tracer").GetComponent<BallisticTracerVisual>();
            Assert.That(tracer.HasProjectileVisual, Is.True);

            yield return new WaitForSeconds(0.24f);

            Assert.That(feedback.ImpactFeedbackCount, Is.EqualTo(1));
            Assert.That(Find("ZeroingRange.Target.ImpactFeedback"), Is.Not.Null);
        }

        [Test]
        public void Task005_InvalidShotProducesNoAudioTracerOrImpactFeedback()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var feedback = controller.FeedbackController;

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.CurrentHoldState, Is.EqualTo(WeaponHoldState.OnRack));
            Assert.That(controller.FireCurrentStateForTests(), Is.False);
            Assert.That(controller.TracerCount, Is.Zero);
            Assert.That(feedback.ValidShotFeedbackCount, Is.Zero);
            Assert.That(feedback.ImpactFeedbackCount, Is.Zero);
        }

        [Test]
        public void Task003_FixedProneSceneHasNoFootstepComponent()
        {
            Assert.That(Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<PlayerFootstepAudio>(), Is.Null);
            Assert.That(Find("ZeroingRange.Player.Footsteps"), Is.Null);
        }

        [Test]
        public void Task005_AdsViewAlignsToGunLineAndFrontHandChangesAimDirection()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            Assert.That(controller.InitializeForTests(), Is.True);
            var before = controller.CurrentAimDirection;

            controller.AdjustFrontHandForTests(new Vector2(0.12f, 0.08f));
            var after = controller.CurrentAimDirection;
            controller.SetAimModeForTests(WeaponAimMode.AimDownSights);

            Assert.That(Vector3.Angle(before, after), Is.GreaterThan(1f));
            Assert.That(controller.CurrentAimMode, Is.EqualTo(WeaponAimMode.AimDownSights));
            Assert.That(Vector3.Angle(camera.transform.forward, controller.CurrentAimDirection), Is.LessThan(0.5f));
        }

        [Test]
        public void Task005_ShoulderSwitchUpdatesNoVrWeaponState()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            controller.ToggleShoulderForTests();

            Assert.That(controller.CurrentMagazine, Is.EqualTo(2));
            Assert.That(controller.CurrentShoulder, Is.EqualTo(ShoulderSide.Left));
        }

        [Test]
        public void Task005_ReloadRestoresMagazineAfterSpendingAmmo()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(0));
            Assert.That(controller.ReloadOnceForTests(), Is.True);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(3));
        }

        [Test]
        public void Task005_006_FireUpdatesSharedZeroingSession()
        {
            var bootstrap = Object.FindObjectOfType<ZeroingRangeSessionBootstrap>();
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.HasActiveSession, Is.True);
            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);

            var zeroing = bootstrap.Services.Zeroing.GetSession(bootstrap.ActiveSessionId);
            Assert.That(zeroing.Success, Is.True, zeroing.Message);
            Assert.That(zeroing.Data.ShotsRemainingInRound, Is.EqualTo(2));
            Assert.That(zeroing.Data.CurrentRound, Is.EqualTo(1));
        }

        [Test]
        public void Task013_RiflePrefabHasDirectGrabPhysicsAndStableRecoilHierarchy()
        {
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle");
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            var grab = weapon.GetComponent<TrainingRifleGrabInteractable>();
            var body = weapon.GetComponent<Rigidbody>();

            Assert.That(grab, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(weapon.GetComponents<BoxCollider>().Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(grab.selectMode, Is.EqualTo(InteractableSelectMode.Multiple));
            Assert.That(binding.RearHandGrip.IsChildOf(binding.RecoilRoot), Is.False);
            Assert.That(binding.FrontHandGrip.IsChildOf(binding.RecoilRoot), Is.False);
            Assert.That(Find("ZeroingRange.Weapon.PickupPrompt"), Is.Not.Null);
        }

        [Test]
        public void Task013_VirtualHandsUseRiggedMeshesAndGripOnlyTheVisualLayer()
        {
            var rightHand = Find("ZeroingRange.Origin.VR.VirtualHand.Right");
            var leftHand = Find("ZeroingRange.Origin.VR.VirtualHand.Left");
            Assert.That(rightHand, Is.Not.Null);
            Assert.That(leftHand, Is.Not.Null);
            Assert.That(rightHand.GetComponent<MeshFilter>(), Is.Null, "The legacy cube hand is still present");
            Assert.That(leftHand.GetComponent<MeshFilter>(), Is.Null, "The legacy cube hand is still present");

            var rightVisual = rightHand.GetComponent<VRControllerHandVisual>();
            var leftVisual = leftHand.GetComponent<VRControllerHandVisual>();
            Assert.That(rightVisual, Is.Not.Null);
            Assert.That(leftVisual, Is.Not.Null);
            Assert.That(rightVisual.HasRenderableHand, Is.True);
            Assert.That(leftVisual.HasRenderableHand, Is.True);
            Assert.That(rightVisual.ModelRoot.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Not.Null);
            Assert.That(leftVisual.ModelRoot.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Not.Null);

            var trackedController = rightHand.transform.parent;
            var trackedPosition = trackedController.position;
            var trackedRotation = trackedController.rotation;
            rightVisual.SetGripForTests(true);

            Assert.That(rightVisual.GripPose01, Is.EqualTo(1f).Within(0.001f));
            Assert.That(trackedController.position, Is.EqualTo(trackedPosition));
            Assert.That(Quaternion.Angle(trackedController.rotation, trackedRotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(
                rightHand.transform.position,
                Find("ZeroingRange.Weapon.Grip.RearHand").transform.position), Is.LessThan(0.06f));
        }

        [Test]
        public void Task013_VirtualHandGripPoseUsesUprightRearGripAndPalmUpFrontSupport()
        {
            // BDD: 05-100m射击HUD / 虚拟手使用手部网格并在持枪时切换握持姿势
            var rightVisual = Find("ZeroingRange.Origin.VR.VirtualHand.Right")
                .GetComponent<VRControllerHandVisual>();
            var leftVisual = Find("ZeroingRange.Origin.VR.VirtualHand.Left")
                .GetComponent<VRControllerHandVisual>();
            var rearGrip = Find("ZeroingRange.Weapon.Grip.RearHand").transform;
            var frontGrip = Find("ZeroingRange.Weapon.Grip.FrontHand").transform;

            rightVisual.SetGripForTests(true);
            leftVisual.SetGripForTests(true);

            Assert.That(Vector3.Dot(rightVisual.transform.forward, rearGrip.up), Is.GreaterThan(0.95f),
                "The rear-hand wrist axis should be upright along the pistol grip");
            Assert.That(Vector3.Dot(-leftVisual.transform.up, frontGrip.up), Is.GreaterThan(0.95f),
                "The front-hand palm normal should face upward beneath the handguard");
            Assert.That(Vector3.Distance(leftVisual.transform.position, frontGrip.position), Is.LessThan(0.06f));
        }

        [Test]
        public void Task013_RifleGripUsesStraightTriggerFingerAndOpenFrontHandWrap()
        {
            // BDD: 05-100m射击HUD / 虚拟手使用手部网格并在持枪时切换握持姿势
            var rightVisual = Find("ZeroingRange.Origin.VR.VirtualHand.Right")
                .GetComponent<VRControllerHandVisual>();
            var leftVisual = Find("ZeroingRange.Origin.VR.VirtualHand.Left")
                .GetComponent<VRControllerHandVisual>();

            Transform Joint(VRControllerHandVisual visual, string finger, string segment) =>
                visual.ModelRoot.GetComponentsInChildren<Transform>(true).First(transform =>
                    transform.name.IndexOf(finger, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    transform.name.EndsWith(segment, System.StringComparison.OrdinalIgnoreCase));

            var rightIndexProximal = Joint(rightVisual, "Index", "Proximal");
            var rightIndexIntermediate = Joint(rightVisual, "Index", "Intermediate");
            var rightMiddleIntermediate = Joint(rightVisual, "Middle", "Intermediate");
            var leftIndexIntermediate = Joint(leftVisual, "Index", "Intermediate");
            var leftIndexDistal = Joint(leftVisual, "Index", "Distal");
            var leftThumbProximal = Joint(leftVisual, "Thumb", "Proximal");

            rightVisual.SetGripForTests(false);
            leftVisual.SetGripForTests(false);
            var rightIndexProximalOpen = rightIndexProximal.localRotation;
            var rightIndexIntermediateOpen = rightIndexIntermediate.localRotation;
            var rightMiddleIntermediateOpen = rightMiddleIntermediate.localRotation;
            var leftIndexIntermediateOpen = leftIndexIntermediate.localRotation;
            var leftIndexDistalOpen = leftIndexDistal.localRotation;
            var leftThumbProximalOpen = leftThumbProximal.localRotation;

            rightVisual.SetGripForTests(true);
            leftVisual.SetGripForTests(true);

            Assert.That(Quaternion.Angle(rightIndexProximalOpen, rightIndexProximal.localRotation),
                Is.InRange(65f, 95f), "The trigger finger should turn toward the trigger at its base joint");
            Assert.That(Quaternion.Angle(rightIndexIntermediateOpen, rightIndexIntermediate.localRotation),
                Is.LessThan(20f), "The trigger finger should remain relatively straight");
            Assert.That(Quaternion.Angle(rightMiddleIntermediateOpen, rightMiddleIntermediate.localRotation),
                Is.InRange(55f, 80f), "The lower fingers should naturally wrap the rear grip");
            Assert.That(Quaternion.Angle(leftIndexIntermediateOpen, leftIndexIntermediate.localRotation),
                Is.InRange(20f, 40f), "The support fingers should only bend gently upward");
            Assert.That(Quaternion.Angle(leftIndexDistalOpen, leftIndexDistal.localRotation),
                Is.LessThan(20f), "The support fingertips should not hook through the handguard");
            Assert.That(Quaternion.Angle(leftThumbProximalOpen, leftThumbProximal.localRotation),
                Is.InRange(5f, 25f), "The support thumb should keep an open web with the index finger");
        }

        [UnityTest]
        public IEnumerator Task013_VirtualHandGripPoseDeformsTheSkinnedMesh()
        {
            var rightVisual = Find("ZeroingRange.Origin.VR.VirtualHand.Right")
                .GetComponent<VRControllerHandVisual>();
            var renderer = rightVisual.ModelRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(rightVisual.FingerBoneCount, Is.GreaterThanOrEqualTo(15));

            var openMesh = new Mesh();
            renderer.BakeMesh(openMesh);
            var openVertices = openMesh.vertices;

            rightVisual.SetGripForTests(true);
            yield return null;

            var gripMesh = new Mesh();
            renderer.BakeMesh(gripMesh);
            var gripVertices = gripMesh.vertices;
            Assert.That(gripVertices.Length, Is.EqualTo(openVertices.Length));

            var maximumVertexDisplacement = 0f;
            for (var index = 0; index < openVertices.Length; index++)
            {
                maximumVertexDisplacement = Mathf.Max(
                    maximumVertexDisplacement,
                    Vector3.Distance(openVertices[index], gripVertices[index]));
            }

            Object.Destroy(openMesh);
            Object.Destroy(gripMesh);
            Assert.That(maximumVertexDisplacement, Is.GreaterThan(0.003f),
                "The grip state changed, but the rendered hand mesh did not deform");
        }

        [Test]
        public void Task013_NoVrModeStartsOnRackAndCannotShootBeforeGrip()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.CurrentHoldState, Is.EqualTo(WeaponHoldState.OnRack));
            Assert.That(controller.CanShoot, Is.False);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease()
        {
            var mode = ResolveOrCreateXRModeController();
            Assert.That(mode, Is.Not.Null, "Task013 XR mode controller is missing");
            Assert.That(mode.NoVrCamera, Is.Not.Null, "Task013 no-VR camera binding is missing");
            Assert.That(mode.XrOrigin, Is.Not.Null, "Task013 XR Origin binding is missing");
            mode.SetVrModeForTests(true);

            var grab = Find("ZeroingRange.Weapon.TrainingRifle").GetComponent<TrainingRifleGrabInteractable>();
            var right = Find("ZeroingRange.Origin.VR.RightDirectInteractor").GetComponent<XRDirectInteractor>();
            var left = Find("ZeroingRange.Origin.VR.LeftDirectInteractor").GetComponent<XRDirectInteractor>();
            var manager = mode.XrOrigin.GetComponentInChildren<XRInteractionManager>(true);

            // This test drives selection through the manager, so hardware actions must stay on the
            // deterministic no-device path while the interaction manager processes strength values.
            right.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            left.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            yield return null;

            Assert.That(grab.RearGrabRadius, Is.EqualTo(0.10f).Within(0.001f));
            Assert.That(grab.FrontGrabRadius, Is.EqualTo(0.12f).Within(0.001f));
            right.transform.position = grab.RearAttach.position;
            left.transform.position = grab.FrontAttach.position;
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)right), Is.True);
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)left), Is.False, "front hand cannot become the primary grab");

            manager.SelectEnter((IXRSelectInteractor)right, (IXRSelectInteractable)grab);
            yield return null;
            Assert.That(grab.HoldState, Is.EqualTo(WeaponHoldState.RearHandHeld));

            left.transform.position = grab.FrontAttach.position;
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)left), Is.True);
            manager.SelectEnter((IXRSelectInteractor)left, (IXRSelectInteractable)grab);
            yield return null;
            Assert.That(grab.HoldState, Is.EqualTo(WeaponHoldState.TwoHandHeld));

            manager.SelectExit((IXRSelectInteractor)right, (IXRSelectInteractable)grab);
            yield return null;
            Assert.That(grab.HoldState, Is.EqualTo(WeaponHoldState.Dropped));
            Assert.That(grab.FrontHandSelected, Is.False);
        }

        [UnityTest]
        public IEnumerator Task013_RuntimeModeKeepsExactlyOneCameraAndAudioListener()
        {
            var mode = ResolveOrCreateXRModeController();
            Assert.That(mode, Is.Not.Null, "Task013 XR mode controller is missing");
            Assert.That(mode.NoVrCamera, Is.Not.Null, "Task013 no-VR camera binding is missing");
            Assert.That(mode.XrOrigin, Is.Not.Null, "Task013 XR Origin binding is missing");

            mode.SetVrModeForTests(false);
            yield return null;
            Assert.That(mode.NoVrCamera.isActiveAndEnabled, Is.True);
            Assert.That(mode.XrOrigin.GetComponentsInChildren<Camera>(true)
                .Count(camera => camera != null && camera.isActiveAndEnabled), Is.Zero);
            Assert.That(CountPlayerAudioListeners(mode), Is.EqualTo(1));

            mode.SetVrModeForTests(true);
            yield return null;
            Assert.That(mode.NoVrCamera.isActiveAndEnabled, Is.False);
            Assert.That(mode.XrOrigin.GetComponentsInChildren<Camera>(true)
                .Count(camera => camera != null && camera.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(CountPlayerAudioListeners(mode), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Task013_ShotProducesLocalRecoilAndTwoHandHapticsThenSettles()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var binding = Find("ZeroingRange.Weapon.TrainingRifle").GetComponent<WeaponPrefabBinding>();
            var haptics = new ManualWeaponHapticOutput();
            controller.ConfigureHaptics(haptics);
            Assert.That(controller.InitializeForTests(), Is.True);
            var initialPosition = binding.RecoilRoot.localPosition;
            var initialRotation = binding.RecoilRoot.localRotation;

            Assert.That(controller.FireOnceForTests(), Is.True);
            var sampleElapsed = 0f;
            var maximumAngle = 0f;
            var maximumDistance = 0f;
            while (sampleElapsed < 0.10f)
            {
                yield return null;
                sampleElapsed += Time.deltaTime;
                maximumAngle = Mathf.Max(
                    maximumAngle,
                    Quaternion.Angle(initialRotation, binding.RecoilRoot.localRotation));
                maximumDistance = Mathf.Max(
                    maximumDistance,
                    Vector3.Distance(initialPosition, binding.RecoilRoot.localPosition));
            }

            Assert.That(haptics.ImpulseCount, Is.EqualTo(1));
            Assert.That(haptics.LastFrontHandHeld, Is.True);
            Assert.That(maximumAngle, Is.InRange(2.2f, 4.5f));
            Assert.That(maximumDistance, Is.InRange(0.02f, 0.05f));

            yield return new WaitForSeconds(0.35f);
            Assert.That(Quaternion.Angle(initialRotation, binding.RecoilRoot.localRotation), Is.LessThan(0.1f));
            Assert.That(Vector3.Distance(initialPosition, binding.RecoilRoot.localPosition), Is.LessThan(0.001f));
        }

        private static GameObject Find(string id)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
        }

        private static int CountPlayerAudioListeners(ZeroingRangeXRModeController mode)
        {
            var noVrCount = mode.NoVrCamera.GetComponents<AudioListener>()
                .Count(listener => listener != null && listener.isActiveAndEnabled);
            var vrCount = mode.XrOrigin.GetComponentsInChildren<AudioListener>(true)
                .Count(listener => listener != null && listener.isActiveAndEnabled);
            return noVrCount + vrCount;
        }

        private static ZeroingRangeXRModeController ResolveOrCreateXRModeController()
        {
            var existing = Object.FindObjectsOfType<ZeroingRangeXRModeController>(true).FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            var noVrObject = Find("ZeroingRange.Camera.NoVR");
            var xrOrigin = Find("ZeroingRange.Origin.VR");
            if (noVrObject == null || xrOrigin == null)
            {
                return null;
            }

            var mode = noVrObject.transform.parent.gameObject.AddComponent<ZeroingRangeXRModeController>();
            mode.Configure(xrOrigin, noVrObject.GetComponent<Camera>());
            return mode;
        }
    }
}
