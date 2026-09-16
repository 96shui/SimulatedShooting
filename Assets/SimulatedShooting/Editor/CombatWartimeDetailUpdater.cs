using System;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    // Additive art pass on the saved scene. Existing collision and navigation remain authoritative.
    public static class CombatWartimeDetailUpdater
    {
        const string Art = "Assets/SimulatedShooting/Art/Combat/";
        static Transform root;
        static System.Random random;
        static Material concrete, brick, soot, wood, earth, steel;
        static Mesh chip, scar;

        [MenuItem("Tools/Simulated Shooting/Scene 3/Apply Wartime Details")]
        public static void Apply()
        {
            if (!Application.isBatchMode && Enumerable.Range(0, EditorSceneManager.sceneCount)
                .Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scene edits before applying the art pass.");
            var scene = EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath, OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var geometry = bindings.GeometryRoot;
            var old = geometry.Find("VisualPolish_Wartime");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            root = new GameObject("VisualPolish_Wartime").transform;
            root.SetParent(geometry, false);
            random = new System.Random(913);
            concrete = Tint("WarPlaster", "Concrete", new Color(.8f, .76f, .65f));
            brick = Tint("WarBrick", "Concrete", new Color(.31f, .19f, .12f));
            soot = Tint("WarSoot", "Concrete", new Color(.075f, .069f, .058f));
            earth = AssetDatabase.LoadAssetAtPath<Material>(Art + "Earth.mat");
            wood = Tint("WarTimber", "WeatheredWood/WeatheredWood", new Color(.48f, .45f, .38f));
            steel = AssetDatabase.LoadAssetAtPath<Material>(Art + "Steel.mat");
            chip = MakeChip();
            scar = MakeScar();
            random = new System.Random(913);
            var parts = geometry.GetComponentsInChildren<Transform>(true);
            foreach (var sign in parts.Where(t => t.name.StartsWith("Sign_")).ToArray())
                Object.DestroyImmediate(sign.gameObject);
            foreach (var stripe in parts.Where(t => t != null && t.name == "RoadStripe"))
                stripe.GetComponent<Renderer>().enabled = false;

            // Ragged plaster and soot sit in front of the retained brick shell, away from doorways.
            for (int floor = 0; floor < 3; floor++)
            {
                float y = floor * 3.6f;
                for (int i = 0; i < 5; i++)
                {
                    float x = 23 + i * 3.5f;
                    Detail("ScorchedPlaster", new Vector3(x, y + Range(.5f, 2.5f), 63.815f),
                        new Vector3(Range(1.5f, 2.8f), Range(.7f, 1.9f), 1), concrete, scar);
                }
                for (int i = 0; i < 7; i++)
                    Detail("ShellSpall", new Vector3(40.185f, y + Range(.6f, 2.6f), Range(65, 87)),
                        new Vector3(Range(1, 2.7f), Range(.5f, 1.7f), 1), concrete, scar, new Vector3(0, -90, 0));
            }
            foreach (var window in parts.Where(t => t != null && t.name == "WindowRecess"))
            {
                window.GetComponent<Renderer>().sharedMaterial = soot;
                var p = window.position + Vector3.back * .12f;
                for (int side = -1; side <= 1; side += 2)
                    Detail("BrokenWindowFrame", p + new Vector3(side * .91f, Range(-.12f, .12f), 0),
                        new Vector3(.065f, Range(.85f, 1.4f), .12f), wood);
                Detail("BrokenWindowFrame", p + Vector3.up * .67f, new Vector3(1.9f, .07f, .12f), wood);
                if (random.Next(2) == 0)
                    Detail("WindowBoard", p, new Vector3(1.95f, .17f, .12f), wood, null, new Vector3(0, 0, Range(-28, 28)));
                for (int streak = 0; streak < 5; streak++)
                    Detail("ScorchedPlaster", p + new Vector3((streak - 2) * .24f, .75f, .025f),
                        new Vector3(.3f, Range(.6f, 1.2f), 1), soot, scar);
            }
            foreach (var parapet in parts.Where(t => t != null && t.name == "RoofParapet"))
                parapet.GetComponent<Renderer>().enabled = false;
            for (int i = 0; i < 43; i++)
            {
                float x = 16.4f + i * .55f;
                int rows = i > 13 && i < 20 ? 0 : random.Next(1, 4);
                for (int row = 0; row < rows; row++)
                    Detail("BrokenMasonry", new Vector3(x + (row % 2) * .18f, 10.98f + row * .19f, 64),
                        new Vector3(.52f, .18f, .36f), row % 2 == 0 ? brick : concrete, null, new Vector3(0, Range(-5, 5), Range(-4, 4)));
                if (i % 4 == 0)
                    Detail("ExposedRebar", new Vector3(x, 11.18f, 64.1f), new Vector3(.025f, .72f, .025f), steel, null, new Vector3(0, 0, Range(-25, 25)));
            }
            // Rubble is restricted to the facade margins; the entrance and stair route stay clear.
            for (int i = 0; i < 150; i++)
                Detail("BrokenMasonry", new Vector3(Range(23.2f, 40.8f), Range(.13f, .26f), Range(62.3f, 63.6f)),
                    new Vector3(Range(.13f, .55f), Range(.12f, .38f), Range(.15f, .5f)), i % 3 == 0 ? brick : concrete,
                    chip, new Vector3(Range(-30, 30), Range(0, 360), Range(-20, 20)));

            var floors = parts.Where(t => t != null && t.name == "TrenchFloor").ToArray();
            foreach (var wall in parts.Where(t => t != null && t.name == "EarthRevetment"))
            {
                var nearest = floors.OrderBy(t => (t.position - wall.position).sqrMagnitude).First();
                var inward = nearest.position - wall.position;
                inward = wall.localScale.x > wall.localScale.z
                    ? new Vector3(0, 0, Mathf.Sign(inward.z)) : new Vector3(Mathf.Sign(inward.x), 0, 0);
                var along = new Vector3(inward.z, 0, -inward.x);
                for (int i = -1; i <= 1; i++)
                {
                    var position = wall.position - inward * .8f + along * (i * 1.35f);
                    position.y = 2f;
                    Detail("EarthBank", position, new Vector3(2.1f, Range(1.3f, 1.9f), 2.1f), earth, chip);
                    var brace = wall.position + inward * .43f + along * i * 1.3f;
                    Detail("RevetmentBrace", brace, new Vector3(.11f, 1.95f, .12f), wood, null,
                        inward.x == 0 ? new Vector3(0, 0, i * 8) : new Vector3(i * 8, 0, 0));
                }
            }
            var trenchDetails = GameObject.Find("VisualPolish_TrenchDetails");
            int plankIndex = 0;
            foreach (var plank in trenchDetails.GetComponentsInChildren<Transform>().Where(t => t.name == "Revetment_Plank"))
            {
                plank.GetComponent<Renderer>().sharedMaterial = wood;
                if (plankIndex++ % 17 != 3) continue;
                plank.GetComponent<Renderer>().enabled = false;
                Detail("SplinteredRevetment", plank.position, new Vector3(2.7f, .18f, .065f), wood, null,
                    plank.eulerAngles + new Vector3(0, 0, 4));
            }
            AssetDatabase.SaveAssets();
            Physics.SyncTransforms();
            CombatSceneBuilder.Validate();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            CombatSceneBuilder.Capture();
            Debug.Log("Wartime details saved; route colliders and scene bindings preserved.");
        }

        static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        static Material Tint(string name, string source, Color color)
        {
            var path = Art + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(Art + source + ".mat"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .05f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Mesh MakeChip()
        {
            var path = Art + "WarChip.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "Irregular masonry" };
            var vertices = new Vector3[10];
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                vertices[i] = new Vector3(Mathf.Cos(angle) * Range(.38f, .55f), Mathf.Sin(angle) * Range(.38f, .55f), Range(-.12f, .12f));
            }
            vertices[8] = new Vector3(.08f, -.05f, -.5f);
            vertices[9] = new Vector3(-.06f, .1f, .5f);
            var triangles = new int[48];
            for (int i = 0; i < 8; i++)
            {
                int n = (i + 1) % 8;
                int k = i * 6;
                triangles[k] = i; triangles[k + 1] = 8; triangles[k + 2] = n;
                triangles[k + 3] = i; triangles[k + 4] = n; triangles[k + 5] = 9;
            }
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.uv = vertices.Select(v => new Vector2(v.x + .5f, v.y + .5f)).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Mesh MakeScar()
        {
            var path = Art + "WarScar.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "Jagged plaster edge" };
            var vertices = new Vector3[25];
            var triangles = new int[72];
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12;
                float radius = .35f + .14f * Mathf.PerlinNoise(i * 1.7f, 9.13f);
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % 24 + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.uv = vertices.Select(v => new Vector2(v.x + .5f, v.y + .5f)).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void Detail(string name, Vector3 position, Vector3 scale, Material material, Mesh mesh = null, Vector3 rotation = default)
        {
            var go = mesh == null ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject(name);
            go.name = name; go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation)); go.transform.localScale = scale;
            if (mesh != null) { go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>(); }
            else Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (mesh == scar) go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = true;
        }
    }
}
