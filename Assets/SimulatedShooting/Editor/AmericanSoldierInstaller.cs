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
    public static class AmericanSoldierInstaller
    {
        public const string Folder = "Assets/SimulatedShooting/Art/Characters/AmericanSoldier";
        const string ModelPath = Folder + "/army3.fbx";
        const string SourcePath = "Assets/SimulatedShooting/Art/Characters/CC0TacticalSWAT/Swat.fbx";
        static readonly string[] TargetBones = { "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot", "RightUpLeg", "RightLeg", "RightFoot" };
        static readonly string[] SourceBones = { "Body", "Abdomen", "Torso", "Chest", "Neck", "Head", "Shoulder.L", "UpperArm.L", "LowerArm.L", "Wrist.L", "Shoulder.R", "UpperArm.R", "LowerArm.R", "Wrist.R", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R" };

        [MenuItem("Tools/Simulated Shooting/Scene 3/Install American Soldiers")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing character assets.");
            foreach (var name in new[] { "Embedded", "Materials", "Animations" })
                if (!AssetDatabase.IsValidFolder(Folder + "/" + name)) AssetDatabase.CreateFolder(Folder, name);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.ExtractTextures(Folder + "/Embedded");
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.isReadable = false;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
            ConfigureTextures();
            var controller = BakeController();
            Upgrade("Enemy", false, controller);
            Upgrade("Teammate", true, controller);
            AssetDatabase.SaveAssets();
            Debug.Log("AmericanSoldier: both faction prefabs installed; embedded textures and CC0 animations bound.");
        }

        static void ConfigureTextures()
        {
            foreach (var file in Directory.GetFiles(Folder + "/Embedded", "*.jpg"))
            {
                var path = file.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = path.Contains("body_n") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
        }

        static Material Material(string name, string texture, Color tint, bool normal = false)
        {
            var path = Folder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", tint);
            material.SetTexture("_BaseMap", texture == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Embedded/mott_var01_" + texture + ".tex.jpg"));
            material.SetFloat("_Smoothness", .15f);
            if (normal)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Embedded/mott_var01_body_n.tex.jpg"));
                material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale", .5f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static AnimatorController BakeController()
        {
            var animations = new Dictionary<string, AnimationClip>();
            var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath));
            var target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            try
            {
                foreach (var a in source.GetComponentsInChildren<Animator>()) a.enabled = false;
                foreach (var a in target.GetComponentsInChildren<Animator>()) a.enabled = false;
                var src = SourceBones.Select(n => Find(source.transform, n)).ToArray();
                var dst = TargetBones.Select(n => Find(target.transform, "mott_var01:" + n)).ToArray();
                var sourceRest = src.Select(t => t.rotation).ToArray();
                var targetRest = dst.Select(t => t.rotation).ToArray();
                var sourceHip = src[0].position;
                var targetHip = dst[0].position;
                float ratio = targetHip.y / sourceHip.y;
                var clips = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
                var names = new[] { "Idle", "Walk", "Run", "Shot", "Hit", "Death" };
                var sourceNames = new[] { "Idle_Gun_Pointing", "Walk", "Run", "Gun_Shoot", "HitRecieve", "Death" };
                for (int index = 0; index < names.Length; index++)
                {
                    var original = clips.Single(c => c.name.EndsWith("|" + sourceNames[index]));
                    var clip = new AnimationClip { name = names[index], frameRate = 30 };
                    var curves = dst.Select(t => Enumerable.Range(0, 4).Select(_ => new AnimationCurve()).ToArray()).ToArray();
                    var position = Enumerable.Range(0, 3).Select(_ => new AnimationCurve()).ToArray();
                    int frames = Mathf.CeilToInt(original.length * 30);
                    for (int frame = 0; frame <= frames; frame++)
                    {
                        float time = original.length * frame / frames;
                        original.SampleAnimation(source, time);
                        for (int bone = 0; bone < dst.Length; bone++)
                            dst[bone].rotation = src[bone].rotation * Quaternion.Inverse(sourceRest[bone]) * targetRest[bone];
                        var offset = (src[0].position - sourceHip) * ratio;
                        // Navigation owns XZ movement. Death preserves the authored collapse displacement.
                        if (names[index] != "Death") { offset.x = 0; offset.z = 0; }
                        dst[0].position = targetHip + offset;
                        for (int bone = 0; bone < dst.Length; bone++)
                        {
                            var q = dst[bone].localRotation;
                            for (int component = 0; component < 4; component++) curves[bone][component].AddKey(time, q[component]);
                        }
                        for (int component = 0; component < 3; component++) position[component].AddKey(time, dst[0].localPosition[component]);
                    }
                    for (int bone = 0; bone < dst.Length; bone++)
                        for (int component = 0; component < 4; component++)
                            clip.SetCurve(AnimationUtility.CalculateTransformPath(dst[bone], target.transform), typeof(Transform), "localRotation." + "xyzw"[component], curves[bone][component]);
                    for (int component = 0; component < 3; component++)
                        clip.SetCurve(AnimationUtility.CalculateTransformPath(dst[0], target.transform), typeof(Transform), "localPosition." + "xyz"[component], position[component]);
                    clip.EnsureQuaternionContinuity();
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = index < 3;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    var path = Folder + "/Animations/" + clip.name + ".anim";
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (existing == null) { AssetDatabase.CreateAsset(clip, path); existing = clip; }
                    else { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); }
                    animations.Add(names[index], existing);
                }
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(target); }
            var controllerPath = Folder + "/Animations/Soldier.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            controller.parameters = new[] { new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float }, new AnimatorControllerParameter { name = "Dead", type = AnimatorControllerParameterType.Bool } };
            var locomotion = machine.AddState("Locomotion"); machine.defaultState = locomotion;
            var blend = AssetDatabase.LoadAllAssetsAtPath(controllerPath).OfType<BlendTree>().FirstOrDefault(b => b.name == "Movement");
            if (blend == null) { blend = new BlendTree { name = "Movement" }; AssetDatabase.AddObjectToAsset(blend, controller); }
            blend.children = new ChildMotion[0];
            blend.blendType = BlendTreeType.Simple1D; blend.blendParameter = "Speed"; blend.useAutomaticThresholds = false;
            blend.AddChild(animations["Idle"], 0); blend.AddChild(animations["Walk"], 1.4f); blend.AddChild(animations["Run"], 3.5f);
            locomotion.motion = blend;
            foreach (var name in new[] { "Shot", "Hit", "Death" })
            {
                var state = machine.AddState(name); state.motion = animations[name];
                if (name == "Death") continue;
                var transition = state.AddTransition(locomotion);
                transition.hasExitTime = true; transition.exitTime = .85f; transition.duration = .1f;
                transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void Upgrade(string role, bool friendly, RuntimeAnimatorController controller)
        {
            var path = "Assets/SimulatedShooting/Prefabs/Combat/Actor_" + role + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var actor = root.GetComponent<CombatActorView>();
                // Keep existing service anchors and collision. Replace only visual geometry.
                var flash = actor.MuzzleFlash;
                flash.transform.SetParent(actor.VisualRoot, true);
                actor.Muzzle.SetParent(actor.VisualRoot, true);
                foreach (var t in actor.VisualRoot.GetComponentsInChildren<Transform>(true).Where(t => new[] { "AmericanSoldier", "CC0_SoldierModel", "CC0_TacticalSWAT", "Model_QBZ191_Enemy", "TrainingForceArmband" }.Contains(t.name)).ToArray())
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                foreach (var renderer in actor.VisualRoot.GetComponentsInChildren<Renderer>(true)) renderer.enabled = renderer.gameObject == flash;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), actor.VisualRoot);
                model.name = "AmericanSoldier";
                model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * .95f;
                var uniform = Material(role + "Uniform", "body_diffuse", friendly ? new Color(.52f, .72f, .65f) : Color.white, true);
                var legs = Material(role + "Trousers", "legs_diffuse", friendly ? new Color(.48f, .64f, .59f) : Color.white);
                var face = Material("Face", "eyerefmask", Color.white);
                var hair = Material("Hair", "hair_a", Color.white);
                var hands = Material("Hands", "hands_diffuse", Color.white);
                var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                skin.sharedMaterials = new[] { legs, legs, uniform, face, face, hands, hair };
                skin.localBounds = new Bounds(new Vector3(0, .8f, 0), new Vector3(3.5f, 2.5f, 3.5f));
                var animator = model.GetComponent<Animator>();
                animator.avatar = null; // Baked transform clips already target this exact rig; no runtime retarget required.
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var driver = root.GetComponent<CombatSoldierAnimation>() ?? root.AddComponent<CombatSoldierAnimation>();
                driver.Actor = actor; driver.Animator = animator; driver.Friendly = friendly; actor.SoldierAnimation = driver;
                driver.LeftUpperArm = Find(model.transform, "mott_var01:LeftArm");
                driver.LeftForearm = Find(model.transform, "mott_var01:LeftForeArm");
                driver.LeftHand = Find(model.transform, "mott_var01:LeftHand");
                driver.RightUpperArm = Find(model.transform, "mott_var01:RightArm");
                driver.RightForearm = Find(model.transform, "mott_var01:RightForeArm");
                driver.RightHand = Find(model.transform, "mott_var01:RightHand");
                driver.Chest = Find(model.transform, "mott_var01:Spine2");
                controller.animationClips.First(c => c.name == "Idle").SampleAnimation(model, 0);
                CombatSoldierAnimation.SolveArm(driver.RightUpperArm, driver.RightForearm, driver.RightHand, actor.VisualRoot.TransformPoint(new Vector3(.14f, 1.30f, .25f)), actor.VisualRoot.TransformPoint(new Vector3(.48f, 1.08f, .12f)));
                CombatSoldierAnimation.SolveArm(driver.LeftUpperArm, driver.LeftForearm, driver.LeftHand, actor.VisualRoot.TransformPoint(new Vector3(.12f, 1.27f, .43f)), actor.VisualRoot.TransformPoint(new Vector3(-.40f, 1.08f, .28f)));
                PoseHand(driver.RightHand, false, actor.VisualRoot);
                PoseHand(driver.LeftHand, true, actor.VisualRoot);
                var rifle = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Weapons/QBC-191/source/QBZ-191.obj"), actor.VisualRoot);
                rifle.name = "Model_QBZ191_Enemy";
                rifle.transform.localPosition = new Vector3(.14f, 1.30f, .25f);
                rifle.transform.localRotation = Quaternion.identity;
                rifle.transform.SetParent(driver.RightHand, true); driver.Rifle = rifle.transform;
                var gunMaterials = new[] { AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Prefabs/Weapons/QBC-191/materials/QBZ191_Body_URP.mat"), AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Prefabs/Weapons/QBC-191/materials/QBZ191_Magazine_URP.mat") };
                foreach (var renderer in rifle.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = gunMaterials;
                actor.Muzzle.SetParent(rifle.transform, false); actor.Muzzle.localPosition = new Vector3(0, .025f, .45f); actor.Muzzle.localRotation = Quaternion.identity;
                flash.transform.SetParent(actor.Muzzle, false); flash.transform.localPosition = Vector3.zero;
                var mark = Material(role + "Identification", null, friendly ? new Color(.04f, .35f, .95f) : new Color(.55f, .15f, .08f));
                var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder); band.name = "FactionArmband";
                Object.DestroyImmediate(band.GetComponent<Collider>());
                band.transform.position = Vector3.Lerp(driver.LeftUpperArm.position, driver.LeftForearm.position, .38f);
                band.transform.rotation = Quaternion.FromToRotation(Vector3.up, driver.LeftForearm.position - driver.LeftUpperArm.position);
                band.transform.localScale = new Vector3(.115f, .035f, .115f);
                band.transform.SetParent(driver.LeftUpperArm, true); band.GetComponent<Renderer>().sharedMaterial = mark;
                if (friendly)
                {
                    AddMark("FriendlyChestPatch", driver.Chest, actor.VisualRoot, new Vector3(-.10f, 1.39f, .15f), new Vector3(.10f, .055f, .012f), mark);
                    AddMark("FriendlyBackPatch", driver.Chest, actor.VisualRoot, new Vector3(0, 1.43f, -.16f), new Vector3(.22f, .07f, .012f), mark);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void AddMark(string name, Transform bone, Transform visual, Vector3 position, Vector3 scale, Material material)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube); mark.name = name;
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            mark.transform.SetParent(visual, false); mark.transform.localPosition = position; mark.transform.localScale = scale;
            mark.transform.SetParent(bone, true); mark.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        static void PoseHand(Transform hand, bool left, Transform visual)
        {
            string prefix = "mott_var01:" + (left ? "Left" : "Right") + "Hand";
            var middle = Find(hand, prefix + "Middle1");
            var index = Find(hand, prefix + "Index1");
            var pinky = Find(hand, prefix + "Pinky1");
            var forward = (middle.position - hand.position).normalized;
            var normal = Vector3.Cross(index.position - hand.position, pinky.position - hand.position).normalized;
            if (left) normal = -normal;
            var desiredForward = left ? visual.forward : -visual.up;
            var desiredNormal = left ? visual.up : -visual.right;
            hand.rotation = Quaternion.LookRotation(desiredForward, desiredNormal) * Quaternion.Inverse(Quaternion.LookRotation(forward, normal)) * hand.rotation;
            foreach (var finger in new[] { "Index", "Middle", "Ring", "Pinky", "Thumb" })
            {
                for (int joint = 1; joint <= 3; joint++)
                {
                    var bone = Find(hand, prefix + finger + joint);
                    if (bone.childCount == 0) continue;
                    var direction = bone.GetChild(0).position - bone.position;
                    var axis = Vector3.Cross(direction, desiredNormal).normalized;
                    float angle = finger == "Thumb" ? 15 : (joint == 2 ? 55 : 35);
                    if (!left && finger == "Index") angle *= .35f;
                    bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
                }
            }
        }

        public static string Capture(string pose = "Idle", float time = 0, bool side = false)
        {
            var objects = new List<GameObject>();
            var old = RenderTexture.active;
            var oldSun = RenderSettings.sun;
            var oldAmbientMode = RenderSettings.ambientMode;
            var oldAmbientLight = RenderSettings.ambientLight;
            RenderTexture rt = null; Texture2D pixels = null;
            try
            {
                int index = 0;
                foreach (var role in new[] { "Enemy", "Teammate" })
                {
                    var actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Combat/Actor_" + role + ".prefab")); objects.Add(actor);
                    actor.transform.position = new Vector3(1000 + index++ * 1.5f, 0, 1000);
                    var driver = actor.GetComponent<CombatSoldierAnimation>();
                    driver.Animator.enabled = false;
                    if (pose != "Idle") driver.Animator.runtimeAnimatorController.animationClips.First(c => c.name == pose).SampleAnimation(driver.Animator.gameObject, time);
                    foreach (var t in actor.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                    actor.GetComponent<CombatActorView>().MuzzleFlash.SetActive(false);
                }
                var cameraObject = new GameObject("SoldierPreviewCamera"); objects.Add(cameraObject);
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = side ? new Vector3(1004.7f, 1.6f, 1002.7f) : new Vector3(1001.6f, 1.5f, 1004.6f);
                camera.transform.LookAt(new Vector3(1000.75f, 1f, 1000));
                camera.fieldOfView = 35; camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.18f,.20f,.22f);
                var lightObject = new GameObject("SoldierPreviewLight"); objects.Add(lightObject);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.6f; light.cullingMask = 1 << 31;
                light.transform.rotation = Quaternion.Euler(35, 160, 0);
                RenderSettings.sun = light;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.45f, .45f, .45f);
                rt = new RenderTexture(1200, 900, 24); camera.targetTexture = rt; camera.Render();
                RenderTexture.active = rt; pixels = new Texture2D(1200,900,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,1200,900),0,0); pixels.Apply();
                var folder = "docs/codex-reports/evidence/american-soldier"; Directory.CreateDirectory(folder);
                var path = folder + "/" + pose + (side ? "-side" : "") + ".png"; File.WriteAllBytes(path,pixels.EncodeToPNG()); return path;
            }
            finally
            {
                RenderTexture.active = old;
                RenderSettings.sun = oldSun;
                RenderSettings.ambientMode = oldAmbientMode;
                RenderSettings.ambientLight = oldAmbientLight;
                foreach (var obj in objects) Object.DestroyImmediate(obj);
                if (rt != null) Object.DestroyImmediate(rt);
                if (pixels != null) Object.DestroyImmediate(pixels);
            }
        }
    }
}
