using System;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    public static class CombatTownExpansion
    {
        const string Art = "Assets/SimulatedShooting/Art/Combat/";

        [MenuItem("Tools/Simulated Shooting/Scene 3/Add Perimeter Buildings")]
        public static void Apply()
        {
            if (!Application.isBatchMode && Enumerable.Range(0, EditorSceneManager.sceneCount)
                .Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scene edits first.");
            var scene = EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath, OpenSceneMode.Single);
            var geometry = Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot;
            var old = geometry.Find("Town_PerimeterBuildings");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Town_PerimeterBuildings").transform;
            root.SetParent(geometry, false);
            Building(root, "South_RowHouse", new Vector3(8, 0, 39), 12, 8, 2, 180, 0);
            Building(root, "South_Workshop", new Vector3(40, 0, 37), 14, 12, 1, 180, 1);
            Building(root, "West_CornerHouse", new Vector3(-15, 0, 57), 12, 12, 3, 90, 2);
            Building(root, "NorthWest_Tenement", new Vector3(-10, 0, 78), 12, 16, 2, 0, 3);
            Building(root, "East_Apartments", new Vector3(51, 0, 78), 14, 18, 4, 0, 4);
            Building(root, "Rear_DamagedHouse", new Vector3(24, 0, 102), 18, 12, 3, 0, 5);
            Physics.SyncTransforms();
            CombatSceneBuilder.Validate();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            CombatSceneBuilder.Capture();
            CaptureDistrict();
        }

        static void CaptureDistrict()
        {
            var go = new GameObject("DistrictPreview");
            var camera = go.AddComponent<Camera>();
            camera.transform.position = new Vector3(-39, 31, 19);
            camera.transform.LookAt(new Vector3(20, 3, 69));
            camera.fieldOfView = 57;
            var target = new RenderTexture(1440, 900, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); image.Apply();
            System.IO.File.WriteAllBytes("Logs/Scene3/district.png", image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(target); Object.DestroyImmediate(go);
        }

        static void Building(Transform parent, string name, Vector3 position, float width, float depth, int floors, float yaw, int variant)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var brick = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Materials/FactoryBrick.mat");
            var plaster = AssetDatabase.LoadAssetAtPath<Material>(Art + "WarPlaster.mat");
            var dark = AssetDatabase.LoadAssetAtPath<Material>(Art + "WarSoot.mat");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Art + "WarTimber.mat");
            var concrete = AssetDatabase.LoadAssetAtPath<Material>(Art + "Concrete.mat");
            float height = floors * 3.2f;
            var shell = Box(root, "ClosedBuildingShell", new Vector3(0, height / 2, 0), new Vector3(width, height, depth), variant % 2 == 0 ? brick : plaster);
            shell.gameObject.AddComponent<BoxCollider>();
            Box(root, "Foundation", new Vector3(0, -.2f, 0), new Vector3(width + .3f, .8f, depth + .3f), concrete);
            for (int floor = 0; floor < floors; floor++)
            {
                float y = floor * 3.2f;
                Box(root, "FloorCornice", new Vector3(0, y + 3.1f, 0), new Vector3(width + .25f, .18f, depth + .25f), concrete);
                for (int side = -1; side <= 1; side += 2)
                    for (int column = 0; column < (int)(width / 3); column++)
                    {
                        var p = new Vector3(-width / 2 + 1.6f + column * 3, y + 1.8f, side * (depth / 2 + .035f));
                        Box(root, "DarkWindow", p, new Vector3(1.25f, 1.55f, .06f), dark);
                        Box(root, "WindowLintel", p + Vector3.up * .85f, new Vector3(1.5f, .12f, .16f), concrete);
                        Box(root, "WindowSill", p - Vector3.up * .85f, new Vector3(1.5f, .15f, .25f), concrete);
                        if ((column + floor + variant) % 3 == 0)
                        {
                            var board = Box(root, "BrokenShutter", p + Vector3.forward * side * .1f, new Vector3(1.4f, .19f, .12f), wood);
                            board.localRotation = Quaternion.Euler(0, 0, 23);
                        }
                    }
            }
            Box(root, "SealedDoor", new Vector3(-width * .23f, 1.15f, -depth / 2 - .05f), new Vector3(1.25f, 2.3f, .12f), wood);
            // Uneven roof walls and broken masonry distinguish the silhouettes without opening playable rooms.
            for (int i = 0; i < (int)width; i++)
            {
                if ((i + variant) % 7 < 2) continue;
                Box(root, "BrokenParapet", new Vector3(-width / 2 + i + .5f, height + .16f + (i % 3) * .08f, -depth / 2),
                    new Vector3(.95f, .32f + (i % 3) * .16f, .3f), brick);
            }
            Box(root, "Chimney", new Vector3(width * .28f, height + .65f, depth * .2f), new Vector3(.8f, 1.3f, .8f), brick);
            for (int i = 0; i < 14; i++)
            {
                var rubble = Box(root, "FacadeRubble", new Vector3(-width / 2 + .5f + i * (width - 1) / 14, .12f, -depth / 2 - .35f),
                    new Vector3(.35f + (i % 3) * .12f, .22f, .3f), concrete);
                rubble.localRotation = Quaternion.Euler(i * 13 % 30, i * 47, i * 11 % 25);
            }
        }

        static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.isStatic = true;
            return go.transform;
        }
    }
}
