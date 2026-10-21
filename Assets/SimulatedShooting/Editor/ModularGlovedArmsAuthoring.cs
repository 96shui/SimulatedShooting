using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    // One selected arms variant. Extract independent meshes so the all-variants FBX is unnecessary.
    public static class ModularGlovedArmsAuthoring
    {
        const string Source = "Assets/AAAModularFPSArmsPack/Prefabs/SK_SOF_ArmsB.prefab";
        const string Dest = "Assets/SimulatedShooting/Art/Characters/ModularGlovedArms";

        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before replacing hand assets.");
            System.IO.Directory.CreateDirectory(Dest);
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            if(source==null)throw new InvalidOperationException("Original pack was intentionally removed after selective extraction. Restore the selected source to a staging project before rebuilding; use PatchSceneHands for existing scenes.");
            var renderer = source.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            var materials = renderer.sharedMaterials.Select(m => CopyMaterial(m.name.StartsWith("M_Arms")
                ? AssetDatabase.LoadAssetAtPath<Material>("Assets/AAAModularFPSArmsPack/Materials/M_Arms.mat") : m)).ToArray();
            var summary = new List<string>();
            for (int side = 0; side < 2; side++) summary.Add(BuildHand(renderer, materials, side == 0));
            AssetDatabase.SaveAssets();
            return string.Join("\n", summary);
        }

        public static void PatchSceneHands(UnityEngine.SceneManagement.Scene scene)
        {
            foreach(var hand in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VRControllerHandVisual>(true)))
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Combat/Hands/VirtualHand_"+hand.HandSide+".prefab").GetComponent<VRControllerHandVisual>();
                var old=hand.ModelRoot;
                var id=old!=null?old.GetComponent<SceneTestId>():null;
                var testId=id!=null?id.Id:null;
                var name=old!=null?old.name:source.ModelRoot.name;
                if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var model=UnityEngine.Object.Instantiate(source.ModelRoot.gameObject,hand.transform,false);
                model.name=name;
                if(!string.IsNullOrEmpty(testId))
                    (model.GetComponent<SceneTestId>()??model.AddComponent<SceneTestId>()).Id=testId;
                var so=new SerializedObject(hand);
                so.FindProperty("modelRoot").objectReferenceValue=model.transform;
                so.FindProperty("useCustomFingerPose").boolValue=true;
                var sourceSo=new SerializedObject(source);
                so.CopyFromSerializedProperty(sourceSo.FindProperty("customFingerPose"));
                so.CopyFromSerializedProperty(sourceSo.FindProperty("gripPositionOffset"));
                so.CopyFromSerializedProperty(sourceSo.FindProperty("gripRotationOffset"));
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        static Material CopyMaterial(Material original)
        {
            var path = Dest + "/" + original.name + ".mat";
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = original.name };
            mat.SetColor("_BaseColor", original.color);
            mat.SetFloat("_Smoothness", .35f);
            foreach (var prop in original.GetTexturePropertyNames())
            {
                var tex = original.GetTexture(prop);
                if (tex == null) continue;
                var sourcePath = AssetDatabase.GetAssetPath(tex);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(sourcePath) + ".png";
                var targetPath = Dest + "/" + fileName;
                if (!System.IO.File.Exists(targetPath))
                {
                    System.IO.File.Copy("TempAssetResearch/ModularArmsBackup/Textures/" + fileName, targetPath);
                    AssetDatabase.ImportAsset(targetPath);
                }
                var importer = (TextureImporter)AssetImporter.GetAtPath(targetPath);
                importer.textureType = ((TextureImporter)AssetImporter.GetAtPath(sourcePath)).textureType;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.isReadable = false;
                importer.SaveAndReimport();
                mat.SetTexture(prop == "_MainTex" ? "_BaseMap" : prop, AssetDatabase.LoadAssetAtPath<Texture>(targetPath));
            }
            if(mat.GetTexture("_BumpMap")!=null)mat.EnableKeyword("_NORMALMAP");
            if(mat.GetTexture("_OcclusionMap")!=null)mat.EnableKeyword("_OCCLUSIONMAP");
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { EditorUtility.CopySerialized(mat, existing); UnityEngine.Object.DestroyImmediate(mat); mat=existing; }
            else AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static string BuildHand(SkinnedMeshRenderer src, Material[] mats, bool left)
        {
            var side = left ? "Left" : "Right";
            var suffix = left ? "_l" : "_r";
            var prefabPath = "Assets/Resources/Combat/Hands/VirtualHand_" + side + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var visual = prefab.GetComponent<VRControllerHandVisual>();
                var model = visual.ModelRoot;
                var oldWrist = model.GetComponentsInChildren<Transform>().First(t => t.name.EndsWith("Wrist") || t.name == "hand" + suffix);
                var wristPosition = model.InverseTransformPoint(oldWrist.position);
                foreach (Transform child in model.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                var wrist = src.bones.First(b => b.name == "hand" + suffix);
                var middle = src.bones.First(b => b.name == "middle_01" + suffix);
                var index = src.bones.First(b => b.name == "index_01" + suffix);
                var pinky = src.bones.First(b => b.name == "pinky_01" + suffix);
                var forward = (middle.position - wrist.position).normalized;
                var lateral = (index.position - pinky.position).normalized * (left ? 1 : -1);
                var up = Vector3.Cross(forward, lateral).normalized;
                var orientation = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
                Func<Vector3, Vector3> point = p => wristPosition + orientation * (p - wrist.position);
                var mesh = src.sharedMesh;
                var positions = mesh.vertices.Select(v => point(src.transform.TransformPoint(v))).ToArray();
                var eligible = mesh.vertices.Select(v => src.transform.TransformPoint(v)).Select(v =>
                    (left ? v.x < 0 : v.x > 0) && Vector3.Dot(v - wrist.position, forward) > -.27f).ToArray();
                var triangles = new List<int[]>();
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var kept = new List<int>(); var tris = mesh.GetTriangles(sub);
                    for (int t = 0; t < tris.Length; t += 3)
                        if (eligible[tris[t]] && eligible[tris[t+1]] && eligible[tris[t+2]])
                        { kept.Add(tris[t]); kept.Add(tris[t+1]); kept.Add(tris[t+2]); }
                    triangles.Add(kept.ToArray());
                }
                var selected = triangles.SelectMany(t => t).Distinct().OrderBy(i => i).ToArray();
                var remap = selected.Select((id,i) => new {id,i}).ToDictionary(x => x.id, x => x.i);
                var weights = selected.Select(i => mesh.boneWeights[i]).ToArray();
                var used = new HashSet<int>();
                foreach (var w in weights)
                {
                    if(w.weight0>0)used.Add(w.boneIndex0); if(w.weight1>0)used.Add(w.boneIndex1);
                    if(w.weight2>0)used.Add(w.boneIndex2); if(w.weight3>0)used.Add(w.boneIndex3);
                }
                // Retain ancestor transforms only for selected weighted bones, no body/head/other arm.
                var bones = new Dictionary<Transform, Transform>();
                Func<Transform, Transform> clone = null;
                clone = old =>
                {
                    if (bones.ContainsKey(old)) return bones[old];
                    var parent = old.parent != null && old.parent != src.transform.root ? clone(old.parent) : model;
                    var bone = new GameObject(Rename(old.name, left)).transform;
                    bone.SetParent(parent, false);
                    bone.position = model.TransformPoint(point(old.position));
                    // Rebase joints to the existing hand system's +X curl axis, including reload curls.
                    bone.rotation = model.rotation;
                    bones.Add(old, bone); return bone;
                };
                var usedIndices = used.OrderBy(i => i).ToArray();
                var boneMap = usedIndices.Select((id,i) => new{id,i}).ToDictionary(x=>x.id,x=>x.i);
                var outputBones = usedIndices.Select(i => clone(src.bones[i])).ToArray();
                foreach (var b in bones.Values.Where(b => b.name.EndsWith("Distal")).ToArray())
                {
                    var tip = new GameObject(b.name.Replace("Distal", "Tip")).transform;
                    tip.SetParent(b, false); tip.localPosition = Vector3.right * .015f;
                }
                for (int i=0;i<weights.Length;i++)
                {
                    var w=weights[i];
                    w.boneIndex0=w.weight0>0?boneMap[w.boneIndex0]:0;
                    w.boneIndex1=w.weight1>0?boneMap[w.boneIndex1]:0;
                    w.boneIndex2=w.weight2>0?boneMap[w.boneIndex2]:0;
                    w.boneIndex3=w.weight3>0?boneMap[w.boneIndex3]:0;weights[i]=w;
                }
                var output = new Mesh { name = "ModularGlovedArm_"+side };
                output.vertices = selected.Select(i=>positions[i]).ToArray();
                output.uv = selected.Select(i=>mesh.uv[i]).ToArray();
                output.boneWeights = weights;
                output.bindposes = outputBones.Select(b=>b.worldToLocalMatrix * model.localToWorldMatrix).ToArray();
                output.subMeshCount = triangles.Count;
                for(int i=0;i<triangles.Count;i++)output.SetTriangles(triangles[i].Select(v=>remap[v]).ToArray(),i);
                output.RecalculateNormals(); output.RecalculateTangents(); output.RecalculateBounds();
                var meshPath=Dest+"/"+output.name+".asset";
                var oldMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(oldMesh!=null){EditorUtility.CopySerialized(output,oldMesh);UnityEngine.Object.DestroyImmediate(output);output=oldMesh;}
                else AssetDatabase.CreateAsset(output, meshPath);
                var node = new GameObject("GlovedArm_"+side).transform;node.SetParent(model,false);
                var skin = node.gameObject.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh=output;skin.bones=outputBones;skin.rootBone=model;
                skin.sharedMaterials=mats;skin.localBounds=output.bounds;skin.updateWhenOffscreen=true;
                var serialized = new SerializedObject(visual);
                serialized.FindProperty("useCustomFingerPose").boolValue=true;
                foreach(var finger in new[]{"thumb","index","middle","ring","little"})
                {
                    var canonical = char.ToUpper(finger[0])+finger.Substring(1);
                    var pose=serialized.FindProperty("customFingerPose").FindPropertyRelative(finger);
                    var stages=new[]{"Proximal","Intermediate","Distal"};
                    var angles= finger=="thumb" ? new[]{20f,25f,15f} : (!left&&finger=="index"?new[]{45f,18f,8f}:new[]{left?28f:55f,left?40f:65f,left?15f:30f});
                    for(int i=0;i<3;i++)
                    {
                        var bone=bones.Values.FirstOrDefault(b=>b.name.Contains(canonical)&&b.name.EndsWith(stages[i]));
                        if(bone==null)continue;
                        var axis=bone.InverseTransformDirection(model.right);
                        pose.FindPropertyRelative(stages[i].ToLowerInvariant()).vector3Value=Quaternion.AngleAxis(angles[i],axis).eulerAngles;
                    }
                    pose.FindPropertyRelative("metacarpal").vector3Value=Vector3.zero;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
                return side+": "+output.vertexCount+" vertices, "+output.triangles.Length/3+" triangles, "+outputBones.Length+" bones";
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        static string Rename(string name, bool left)
        {
            var parts=name.Split('_');
            var finger=parts[0]=="pinky"?"little":parts[0];
            if(parts.Length==3 && new[]{"thumb","index","middle","ring","little"}.Contains(finger))
                return (left?"L_":"R_")+char.ToUpper(finger[0])+finger.Substring(1)+(parts[1]=="01"?"Proximal":parts[1]=="02"?"Intermediate":"Distal");
            return name;
        }
    }
}
