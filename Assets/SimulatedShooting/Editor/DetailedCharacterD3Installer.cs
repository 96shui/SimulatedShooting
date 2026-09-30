using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    // Replaces only the P3 enemy's presentation, preserving its combat service bindings.
    public static class DetailedCharacterD3Installer
    {
        public const string Folder = "Assets/SimulatedShooting/Art/Characters/DetailedCharacterD3";
        const string ModelPath = Folder + "/D3_MIXAMO.fbx";
        const string SourceAnimationPath = "Assets/SimulatedShooting/Art/Characters/CC0TacticalSWAT/Swat.fbx";
        const string EnemyPath = "Assets/SimulatedShooting/Prefabs/Combat/Actor_Enemy.prefab";

        static readonly string[] SourceBones = { "Body", "Abdomen", "Torso", "Chest", "Neck", "Head", "Shoulder.L", "UpperArm.L", "LowerArm.L", "Wrist.L", "Shoulder.R", "UpperArm.R", "LowerArm.R", "Wrist.R", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R" };
        static readonly string[] TargetBones = { "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "L_shoulder", "L_arm", "L_fore_arm", "L_hand", "R_shoulder", "R_arm", "R_fore_arm", "R_hand", "L_up_leg", "L_knee", "L_foot", "R_up_leg", "R_knee", "R_foot" };
        static readonly string[] ClipNames = { "Idle", "Walk", "Run", "Shot", "Hit", "Death" };
        static readonly string[] SourceClipNames = { "Idle_Gun_Pointing", "Walk", "Run", "Gun_Shoot", "HitRecieve", "Death" };

        [MenuItem("Tools/Simulated Shooting/Scene 3/Install D3 Enemy")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing the D3 enemy.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null) throw new FileNotFoundException(ModelPath);
            ConfigureTextures();
            var controller = BakeController();
            UpgradeEnemy(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("D3 enemy installed from Detailed Characters; combat service bindings unchanged.");
        }

        [MenuItem("Tools/Simulated Shooting/Scene 3/Remove D3 External Weapons")]
        public static void RemoveExternalWeapons()
        {
            var root = PrefabUtility.LoadPrefabContents(EnemyPath);
            try
            {
                var actor = root.GetComponent<CombatActorView>();
                var model = actor.VisualRoot.Find("DetailedCharacterD3");
                if (model == null) throw new InvalidOperationException("D3 enemy model not found.");
                HideBackCylinders(model);
                var flamethrower = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "D3_Flamethrower");
                if (flamethrower != null) Object.DestroyImmediate(flamethrower.gameObject);
                var rifle = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Model_QBZ191_Enemy");
                if (rifle == null) throw new InvalidOperationException("Enemy muzzle carrier not found.");
                foreach (var renderer in rifle.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void HideBackCylinders(Transform model)
        {
            var cylinders = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == "D3_Baloons");
            if (cylinders == null) throw new InvalidOperationException("D3 back cylinder mesh not found.");
            cylinders.enabled = false;
        }

        static void ConfigureTextures()
        {
            foreach (var file in Directory.GetFiles(Folder + "/Textures"))
            {
                var path = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = file.ToLowerInvariant().Contains("normal") || file.ToLowerInvariant().Contains("nmap") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
        }

        static Material Material(string name)
        {
            var path = Folder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            string color = name + "_color.jpg";
            string normal = name + "_normal.jpg";
            switch (name)
            {
                case "D3_Suit": color = "D3_set2_dirt_color.jpg"; normal = "D3_set_normal.jpg"; break;
                case "D3_Belt": color = "D3_D4_Belt_color.jpg"; normal = "D3_D4_Belt_normal.jpg"; break;
                case "D3_Back_Sling": color = "D3_BackPack_color.jpg"; normal = "D3_BackPack_normal.jpg"; break;
                case "D3_Mask_Glass": color = "D3_Mask_Glass_color.jpg"; normal = "D3_Mask_Glass_normal.jpg"; break;
                case "D3_Eyelashes": color = "D3_Head_color.jpg"; normal = null; break;
                case "D3_Cornea": color = null; normal = null; break;
            }
            material.SetColor("_BaseColor", name == "D3_Cornea" ? new Color(.18f, .14f, .12f) : Color.white);
            material.SetTexture("_BaseMap", color == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + color));
            var normalMap = normal == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + normal);
            material.SetTexture("_BumpMap", normalMap);
            if (normalMap != null) material.EnableKeyword("_NORMALMAP");
            else material.DisableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", name.Contains("Glass") ? .65f : .2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static AnimatorController BakeController()
        {
            if (!AssetDatabase.IsValidFolder(Folder + "/Animations")) AssetDatabase.CreateFolder(Folder, "Animations");
            if (!AssetDatabase.IsValidFolder(Folder + "/Materials")) AssetDatabase.CreateFolder(Folder, "Materials");
            var clips = new Dictionary<string, AnimationClip>();
            var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SourceAnimationPath));
            var target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            try
            {
                foreach (var animator in source.GetComponentsInChildren<Animator>()) animator.enabled = false;
                foreach (var animator in target.GetComponentsInChildren<Animator>()) animator.enabled = false;
                var src = SourceBones.Select(name => Find(source.transform, name)).ToArray();
                var dst = TargetBones.Select(name => Find(target.transform, name)).ToArray();
                var sourceRest = src.Select(t => t.rotation).ToArray();
                var targetRest = dst.Select(t => t.rotation).ToArray();
                var sourceHip = src[0].position;
                var targetHip = dst[0].position;
                float ratio = targetHip.y / sourceHip.y;
                var imported = AssetDatabase.LoadAllAssetsAtPath(SourceAnimationPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
                for (int index = 0; index < ClipNames.Length; index++)
                {
                    var original = imported.Single(c => c.name.EndsWith("|" + SourceClipNames[index]));
                    var clip = new AnimationClip { name = ClipNames[index], frameRate = 30 };
                    var rotations = dst.Select(_ => Enumerable.Range(0, 4).Select(__ => new AnimationCurve()).ToArray()).ToArray();
                    var positions = Enumerable.Range(0, 3).Select(_ => new AnimationCurve()).ToArray();
                    int frames = Mathf.Max(1, Mathf.CeilToInt(original.length * 30));
                    for (int frame = 0; frame <= frames; frame++)
                    {
                        float time = original.length * frame / frames;
                        original.SampleAnimation(source, time);
                        for (int bone = 0; bone < dst.Length; bone++)
                            dst[bone].rotation = src[bone].rotation * Quaternion.Inverse(sourceRest[bone]) * targetRest[bone];
                        var offset = (src[0].position - sourceHip) * ratio;
                        if (ClipNames[index] != "Death") { offset.x = 0; offset.z = 0; }
                        dst[0].position = targetHip + offset;
                        for (int bone = 0; bone < dst.Length; bone++)
                        {
                            var rotation = dst[bone].localRotation;
                            for (int component = 0; component < 4; component++) rotations[bone][component].AddKey(time, rotation[component]);
                        }
                        for (int component = 0; component < 3; component++) positions[component].AddKey(time, dst[0].localPosition[component]);
                    }
                    for (int bone = 0; bone < dst.Length; bone++)
                        for (int component = 0; component < 4; component++)
                            clip.SetCurve(AnimationUtility.CalculateTransformPath(dst[bone], target.transform), typeof(Transform), "localRotation." + "xyzw"[component], rotations[bone][component]);
                    for (int component = 0; component < 3; component++)
                        clip.SetCurve(AnimationUtility.CalculateTransformPath(dst[0], target.transform), typeof(Transform), "localPosition." + "xyz"[component], positions[component]);
                    clip.EnsureQuaternionContinuity();
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = index < 3;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    var path = Folder + "/Animations/" + clip.name + ".anim";
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (existing == null) { AssetDatabase.CreateAsset(clip, path); existing = clip; }
                    else { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); }
                    clips.Add(ClipNames[index], existing);
                }
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(target); }
            var controllerPath = Folder + "/Animations/D3Enemy.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            controller.parameters = new[] { new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float }, new AnimatorControllerParameter { name = "Dead", type = AnimatorControllerParameterType.Bool } };
            var locomotion = machine.AddState("Locomotion"); machine.defaultState = locomotion;
            var blend = AssetDatabase.LoadAllAssetsAtPath(controllerPath).OfType<BlendTree>().FirstOrDefault(b => b.name == "Movement");
            if (blend == null) { blend = new BlendTree { name = "Movement" }; AssetDatabase.AddObjectToAsset(blend, controller); }
            blend.children = new ChildMotion[0];
            blend.blendType = BlendTreeType.Simple1D; blend.blendParameter = "Speed"; blend.useAutomaticThresholds = false;
            blend.AddChild(clips["Idle"], 0); blend.AddChild(clips["Walk"], 1.4f); blend.AddChild(clips["Run"], 3.5f);
            locomotion.motion = blend;
            foreach (var name in new[] { "Shot", "Hit", "Death" })
            {
                var state = machine.AddState(name); state.motion = clips[name];
                if (name == "Death") continue;
                var transition = state.AddTransition(locomotion);
                transition.hasExitTime = true; transition.exitTime = .85f; transition.duration = .1f;
                transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void UpgradeEnemy(RuntimeAnimatorController controller)
        {
            var root = PrefabUtility.LoadPrefabContents(EnemyPath);
            try
            {
                var actor = root.GetComponent<CombatActorView>();
                if (actor == null || actor.VisualRoot == null || actor.Muzzle == null || actor.HitCollider == null)
                    throw new InvalidOperationException("Enemy service bindings are incomplete.");
                var visual = actor.VisualRoot;
                visual.localPosition = Vector3.up * .25f;
                var rifle = visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Model_QBZ191_Enemy");
                if (rifle == null) throw new InvalidOperationException("Existing enemy rifle not found.");
                rifle.SetParent(visual, true);
                foreach (var old in new[] { visual.Find("AmericanSoldier"), visual.Find("DetailedCharacterD1"), visual.Find("DetailedCharacterD3") })
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                foreach (var child in visual.GetComponentsInChildren<Transform>(true).Where(t => t.name == "FactionArmband" || t.name == "FriendlyChestPatch" || t.name == "FriendlyBackPatch").ToArray())
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), visual);
                model.name = "DetailedCharacterD3";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * .95f;
                foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var names = renderer.sharedMaterials.Select(m => m == null ? "D3_Suit" : m.name).ToArray();
                    renderer.sharedMaterials = names.Select(Material).ToArray();
                }
                HideBackCylinders(model.transform);
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.avatar = null; // Clips are baked for this exact skeleton.
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var driver = root.GetComponent<CombatSoldierAnimation>();
                if (driver == null) driver = root.AddComponent<CombatSoldierAnimation>();
                driver.Actor = actor; driver.Animator = animator; driver.Friendly = false; actor.SoldierAnimation = driver;
                driver.LeftUpperArm = Find(model.transform, "L_arm");
                driver.LeftForearm = Find(model.transform, "L_fore_arm");
                driver.LeftHand = Find(model.transform, "L_hand");
                driver.RightUpperArm = Find(model.transform, "R_arm");
                driver.RightForearm = Find(model.transform, "R_fore_arm");
                driver.RightHand = Find(model.transform, "R_hand");
                driver.Chest = Find(model.transform, "Spine2");
                controller.animationClips.First(c => c.name == "Idle").SampleAnimation(model, 0);
                rifle.position = visual.TransformPoint(new Vector3(.14f, 1.30f, .25f));
                rifle.rotation = visual.rotation;
                CombatSoldierAnimation.SolveArm(driver.RightUpperArm, driver.RightForearm, driver.RightHand,
                    visual.TransformPoint(new Vector3(.14f, 1.30f, .25f)), visual.TransformPoint(new Vector3(.48f, 1.08f, .12f)));
                CombatSoldierAnimation.SolveArm(driver.LeftUpperArm, driver.LeftForearm, driver.LeftHand,
                    visual.TransformPoint(new Vector3(.12f, 1.27f, .43f)), visual.TransformPoint(new Vector3(-.40f, 1.08f, .28f)));
                rifle.SetParent(driver.RightHand, true);
                foreach (var renderer in rifle.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                driver.Rifle = rifle;
                var mark = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Characters/AmericanSoldier/Materials/EnemyIdentification.mat");
                var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                band.name = "FactionArmband";
                Object.DestroyImmediate(band.GetComponent<Collider>());
                band.transform.position = Vector3.Lerp(driver.LeftUpperArm.position, driver.LeftForearm.position, .38f);
                band.transform.rotation = Quaternion.FromToRotation(Vector3.up, driver.LeftForearm.position - driver.LeftUpperArm.position);
                band.transform.localScale = new Vector3(.115f, .035f, .115f);
                band.transform.SetParent(driver.LeftUpperArm, true);
                band.GetComponent<Renderer>().sharedMaterial = mark;
                AddPatch("EnemyChestPatch", driver.Chest, visual, new Vector3(-.10f, 1.39f, .15f), new Vector3(.10f, .055f, .012f), mark);
                AddPatch("EnemyBackPatch", driver.Chest, visual, new Vector3(0, 1.43f, -.16f), new Vector3(.22f, .07f, .012f), mark);
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void AddPatch(string name, Transform bone, Transform visual, Vector3 position, Vector3 scale, Material material)
        {
            var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = name;
            Object.DestroyImmediate(patch.GetComponent<Collider>());
            patch.transform.SetParent(visual, false);
            patch.transform.localPosition = position;
            patch.transform.localScale = scale;
            patch.transform.SetParent(bone, true);
            patch.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    }
}

