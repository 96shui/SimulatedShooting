using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Common;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    // Reproducible native renders of the saved assets; does not save or re-author poses/scenes.
    public static class RangeAssistanceEvidence
    {
        const string Output = "docs/codex-reports/evidence/range-assist-shoulder-20261022";

        [MenuItem("Tools/Simulated Shooting/Capture Training Assistance Evidence")]
        public static void Capture()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before capturing evidence.");
            var restoreSetup = setup.Any(s => s.isLoaded && s.isActive);
            if (!restoreSetup) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Output);
            try
            {
                CaptureShoulder(ShoulderSide.Right);
                CaptureShoulder(ShoulderSide.Left);
            }
            catch (Exception exception) { Debug.LogException(exception); throw; }
            finally
            {
                if (restoreSetup) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        static void CaptureShoulder(ShoulderSide side)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var meshes = new List<Mesh>();
            try
            {
                var root = new GameObject("SavedShoulderPoseEvidence");
                var rifle = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab"), scene);
                rifle.transform.SetParent(root.transform, false);
                rifle.transform.localPosition = Vector3.zero; rifle.transform.localRotation = Quaternion.identity;
                var grab = rifle.GetComponent<TrainingRifleGrabInteractable>();
                grab.Configure(rifle.GetComponent<WeaponPrefabBinding>());
                foreach (var handSide in new[] { VirtualHandSide.Left, VirtualHandSide.Right })
                {
                    var controller = new GameObject(handSide + " Controller").transform; controller.SetParent(root.transform, false);
                    var hand = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_" + handSide), controller, false)
                        .GetComponent<VRControllerHandVisual>();
                    hand.BindWeapon(grab); hand.BeginGripPosePreview();
                }
                grab.SetShoulder(side);
                // Bake only these evidence instances, preserving source meshes/materials and exact pose.
                foreach (var hand in root.GetComponentsInChildren<VRControllerHandVisual>())
                    foreach (var skin in hand.ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh = new Mesh(); skin.BakeMesh(mesh, false); meshes.Add(mesh);
                        var baked = new GameObject("EvidenceBakedPose", typeof(MeshFilter), typeof(MeshRenderer));
                        baked.transform.SetParent(skin.transform, false);
                        baked.GetComponent<MeshFilter>().sharedMesh = mesh;
                        baked.GetComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                        skin.enabled = false;
                    }
                foreach (var node in root.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 30;
                var camera = new GameObject("ShoulderEvidenceCamera").AddComponent<Camera>();
                camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.18f, .22f, .27f); camera.nearClipPlane = .01f;
                var light = new GameObject("ShoulderEvidenceLight").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.5f;
                var sign = side == ShoulderSide.Left ? -1f : 1f;
                light.transform.rotation = Quaternion.Euler(40, -35 * sign, 0);
                camera.fieldOfView = 60;
                camera.transform.position = new Vector3(-.10f * sign, .095f, -.53f);
                camera.transform.LookAt(new Vector3(0, 0, 4));
                Save(camera, side.ToString().ToLowerInvariant() + "-forward");
                camera.fieldOfView = 56;
                camera.transform.position = new Vector3(.85f * sign, .25f, -.15f);
                camera.transform.LookAt(new Vector3(0, -.08f, .18f));
                Save(camera, side.ToString().ToLowerInvariant() + "-side");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var mesh in meshes) if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        static void Save(Camera camera, string name)
        {
            var target = RenderTexture.GetTemporary(1600, 900, 24);
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = camera.targetTexture; var active = RenderTexture.active; var aspect = camera.aspect;
            try
            {
                camera.targetTexture = target; camera.aspect = 1600f / 900f; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG());
                Debug.Log("[RangeAssistanceEvidence] " + name);
            }
            finally
            {
                camera.targetTexture = previous; camera.aspect = aspect; RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
