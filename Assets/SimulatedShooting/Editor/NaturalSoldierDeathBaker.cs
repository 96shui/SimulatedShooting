using System;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    // Keeps the source collapse/footwork and authors relaxed, bent arms instead of its overhead gesture.
    public static class NaturalSoldierDeathBaker
    {
        const string SourcePath = "Assets/SimulatedShooting/Art/Characters/CC0TacticalSWAT/Swat.fbx";
        static readonly string[] SourceBones = { "Body", "Abdomen", "Torso", "Chest", "Neck", "Head", "Shoulder.L", "UpperArm.L", "LowerArm.L", "Wrist.L", "Shoulder.R", "UpperArm.R", "LowerArm.R", "Wrist.R", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R" };
        static readonly string[] TargetBones = { "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "L_shoulder", "L_arm", "L_fore_arm", "L_hand", "R_shoulder", "R_arm", "R_fore_arm", "R_hand", "L_up_leg", "L_knee", "L_foot", "R_up_leg", "R_knee", "R_foot" };

        [MenuItem("Tools/Simulated Shooting/Scene 3/Bake Natural Soldier Death")]
        public static void BakeAll()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before baking animations.");
            Bake("DetailedCharacterD3", "D3_MIXAMO");
            Bake("DetailedCharacterD4", "D4_MIXAMO");
            AssetDatabase.SaveAssets();
        }

        public static void ApplyRelaxedArms(Transform[] bones, Quaternion[] standingRotations, float time)
        {
            var chest = bones[3];
            var rotation = chest.rotation * Quaternion.Inverse(standingRotations[3]);
            var relax = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.08f, .55f, time));
            var right = chest.position + rotation * Vector3.Lerp(new Vector3(.14f, -.08f, .25f), new Vector3(.22f, -.38f, .10f), relax);
            var left = chest.position + rotation * Vector3.Lerp(new Vector3(.12f, -.11f, .43f), new Vector3(-.23f, -.34f, .10f), relax);
            CombatSoldierAnimation.SolveArm(bones[11], bones[12], bones[13], right,
                chest.position + rotation * new Vector3(.42f, -.22f, -.04f));
            CombatSoldierAnimation.SolveArm(bones[7], bones[8], bones[9], left,
                chest.position + rotation * new Vector3(-.42f, -.23f, -.04f));
            // Relax the wrists with the arms so the held rifle settles beside the body.
            bones[13].rotation = rotation * Quaternion.AngleAxis(70f * relax, Vector3.right) * standingRotations[13];
            bones[9].rotation = rotation * Quaternion.AngleAxis(50f * relax, Vector3.right) * standingRotations[9];
        }

        static void Bake(string folderName, string modelName)
        {
            var folder = "Assets/SimulatedShooting/Art/Characters/" + folderName;
            var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath));
            var target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + modelName + ".fbx"));
            try
            {
                foreach (var animator in source.GetComponentsInChildren<Animator>()) animator.enabled = false;
                foreach (var animator in target.GetComponentsInChildren<Animator>()) animator.enabled = false;
                var src = SourceBones.Select(name => Find(source, name)).ToArray();
                var dst = TargetBones.Select(name => Find(target, name)).ToArray();
                var srcRest = src.Select(t => t.rotation).ToArray();
                var dstRest = dst.Select(t => t.rotation).ToArray();
                var srcHip = src[0].position; var dstHip = dst[0].position;
                var ratio = dstHip.y / srcHip.y;
                var original = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>()
                    .Single(c => !c.name.StartsWith("__preview") && c.name.EndsWith("|Death"));
                var clip = new AnimationClip { name = "Death", frameRate = 30 };
                var curves = dst.Select(_ => Enumerable.Range(0, 4).Select(__ => new AnimationCurve()).ToArray()).ToArray();
                var positions = Enumerable.Range(0, 3).Select(_ => new AnimationCurve()).ToArray();
                var frames = Mathf.CeilToInt(original.length * 30);
                for (int frame = 0; frame <= frames; frame++)
                {
                    var time = original.length * frame / frames;
                    original.SampleAnimation(source, time);
                    for (int bone = 0; bone < dst.Length; bone++)
                        dst[bone].rotation = src[bone].rotation * Quaternion.Inverse(srcRest[bone]) * dstRest[bone];
                    dst[0].position = dstHip + (src[0].position - srcHip) * ratio;
                    ApplyRelaxedArms(dst, dstRest, time);
                    for (int bone = 0; bone < dst.Length; bone++)
                        for (int component = 0; component < 4; component++) curves[bone][component].AddKey(time, dst[bone].localRotation[component]);
                    for (int component = 0; component < 3; component++) positions[component].AddKey(time, dst[0].localPosition[component]);
                }
                for (int bone = 0; bone < dst.Length; bone++)
                    for (int component = 0; component < 4; component++)
                        clip.SetCurve(AnimationUtility.CalculateTransformPath(dst[bone], target.transform), typeof(Transform), "localRotation." + "xyzw"[component], curves[bone][component]);
                for (int component = 0; component < 3; component++)
                    clip.SetCurve(AnimationUtility.CalculateTransformPath(dst[0], target.transform), typeof(Transform), "localPosition." + "xyz"[component], positions[component]);
                clip.EnsureQuaternionContinuity();
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/Animations/Death.anim");
                EditorUtility.CopySerialized(clip, existing);
                Object.DestroyImmediate(clip);
                EditorUtility.SetDirty(existing);
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(target); }
        }

        static Transform Find(GameObject model, string name) => model.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
    }
}
