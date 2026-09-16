using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimulatedShooting.Editor
{
    public static class CombatTrenchDetailUpdater
    {
        const string Art = "Assets/SimulatedShooting/Art/Combat/";

        [MenuItem("Tools/Simulated Shooting/Scene 3/Update Trench Details")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var geometry = Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot;
            var parts = geometry.GetComponentsInChildren<Transform>();
            var apron = parts.Single(t => t.name == "StairBase");
            apron.position = new Vector3(apron.position.x, -.11f, apron.position.z);
            var exit = parts.Where(t => t.name == "TrenchFloor").OrderByDescending(t => t.position.z).First();
            exit.position = new Vector3(exit.position.x, exit.position.y, 43);
            exit.localScale = new Vector3(exit.localScale.x, exit.localScale.y, 2);

            var old = GameObject.Find("VisualPolish_TrenchDetails");
            if (old != null) Object.DestroyImmediate(old);
            foreach (var sphere in Object.FindObjectsOfType<Transform>().Where(t => t.name == "RoundedSandbag").ToArray())
                Object.DestroyImmediate(sphere.gameObject);
            parts = geometry.GetComponentsInChildren<Transform>();
            var root = new GameObject("VisualPolish_TrenchDetails").transform;
            var sackMaterial = TexturedMaterial("Sandbag/Sandbag", "Sandbag/sandbag_diffuse.png", "Sandbag/sandbag_normal.png");
            var wood = TexturedMaterial("WeatheredWood/WeatheredWood", "WeatheredWood/wood_planks_diff_1k.jpg", "WeatheredWood/wood_planks_nor_gl_1k.jpg");
            wood.SetTextureScale("_BaseMap", new Vector2(1, .1f));
            wood.SetTextureScale("_BumpMap", new Vector2(1, .1f));
            var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "Sandbag/sandbag_model.obj");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = true;
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Sandbag/sandbag_model.obj").GetComponentInChildren<MeshFilter>().sharedMesh;
            var mesh = Object.Instantiate(source);
            mesh.name = "Sandbag_Normalized";
            var bounds = mesh.bounds;
            mesh.vertices = mesh.vertices.Select(v => {
                v -= bounds.center;
                return new Vector3(v.z / bounds.size.z, v.y / bounds.size.y, -v.x / bounds.size.x);
            }).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            var meshPath = Art + "Sandbag/Sandbag_Normalized.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
            else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); }

            foreach (var bag in parts.Where(t => t.name == "Sandbag"))
            {
                bag.GetComponent<Renderer>().enabled = false;
                var visual = new GameObject("Sandbag_Imported");
                visual.transform.SetParent(root);
                visual.transform.SetPositionAndRotation(bag.position, bag.rotation);
                visual.transform.localScale = new Vector3(1.4f, .34f, .65f);
                visual.AddComponent<MeshFilter>().sharedMesh = saved;
                visual.AddComponent<MeshRenderer>().sharedMaterial = sackMaterial;
                visual.isStatic = true;
            }
            foreach (var post in parts.Where(t => t.name == "TimberPost"))
                post.GetComponent<Renderer>().sharedMaterial = wood;
            foreach (var board in parts.Where(t => t.name == "Duckboard"))
            {
                board.GetComponent<Renderer>().enabled = false;
                for (int i = 0; i < 13; i++)
                    Plank(root, "Duckboard_Slat", board.position + new Vector3(0, .016f, (i - 6) * .29f),
                        new Vector3(1.1f, .06f, .265f), Quaternion.identity, wood);
            }
            var floors = parts.Where(t => t.name == "TrenchFloor").ToArray();
            foreach (var wall in parts.Where(t => t.name == "EarthRevetment"))
            {
                var nearest = floors.OrderBy(t => (t.position - wall.position).sqrMagnitude).First();
                var inward = nearest.position - wall.position;
                inward.y = 0;
                inward = wall.localScale.x > wall.localScale.z
                    ? new Vector3(0, 0, Mathf.Sign(inward.z)) : new Vector3(Mathf.Sign(inward.x), 0, 0);
                for (int row = 0; row < 7; row++)
                    Plank(root, "Revetment_Plank", new Vector3(wall.position.x, .25f + row * .28f, wall.position.z) + inward * .36f,
                        new Vector3(3.88f, .23f, .065f), Quaternion.Euler(0, inward.x != 0 ? 90 : 0, 0), wood);
            }
            EditorSceneManager.MarkSceneDirty(geometry.gameObject.scene);
            EditorSceneManager.SaveScene(geometry.gameObject.scene);
            AssetDatabase.SaveAssets();
            CombatSceneBuilder.RebuildNavigation();
            CombatSceneBuilder.Validate();
            CombatSceneBuilder.Capture();
            Debug.Log("Trench details updated; saved scene layout and actor prefabs preserved.");
        }

        static Material TexturedMaterial(string name, string diffuse, string normal)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + normal);
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
            var path = Art + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + diffuse));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", .08f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Plank(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = name;
            plank.transform.SetParent(parent);
            plank.transform.SetPositionAndRotation(position, rotation);
            plank.transform.localScale = scale;
            Object.DestroyImmediate(plank.GetComponent<Collider>());
            plank.GetComponent<Renderer>().sharedMaterial = material;
            plank.isStatic = true;
        }
    }
}
