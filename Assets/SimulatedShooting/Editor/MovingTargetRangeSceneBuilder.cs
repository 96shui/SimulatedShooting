using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimulatedShooting.Editor
{
    public static class MovingTargetRangeSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/MovingTargetRangeScene.unity";
        const string MaterialFolder = "Assets/SimulatedShooting/Art/Materials";
        const string SloganListenTexturePath =
            "Assets/SimulatedShooting/Art/Environment/MovingTargetRange/MovingTargetSloganListen.png";
        const string SloganWinTexturePath =
            "Assets/SimulatedShooting/Art/Environment/MovingTargetRange/MovingTargetSloganWin.png";
        const string SloganDisciplineTexturePath =
            "Assets/SimulatedShooting/Art/Environment/MovingTargetRange/MovingTargetSloganDiscipline.png";
        const string SloganDataWinsTexturePath =
            "Assets/SimulatedShooting/Art/Environment/MovingTargetRange/MovingTargetSloganDataWins.png";
        const string DistanceTextMaterialPath =
            "Assets/SimulatedShooting/Art/Materials/RangeDistanceText.mat";
        const string MudDiffusePath =
            "Assets/SimulatedShooting/Art/Textures/BrownMud/brown_mud_2k.blend/textures/brown_mud_diff_2k.jpg";
        const string MudNormalPath =
            "Assets/SimulatedShooting/Art/Textures/BrownMud/brown_mud_2k.blend/textures/brown_mud_nor_gl_2k.exr";
        const string ConcreteDiffusePath =
            "Assets/SimulatedShooting/Art/Textures/ConcreteFloor01/concrete_floor_01_diff_2k.jpg";
        const string ConcreteNormalPath =
            "Assets/SimulatedShooting/Art/Textures/ConcreteFloor01/concrete_floor_01_nor_dx_2k.jpg";
        const string WorldSpaceTextShaderName = "SimulatedShooting/World Space Text Occluded";
        const string XrOriginPrefabPath =
            "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";


        [MenuItem("Tools/Simulated Shooting/Build Moving Target Range Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/SimulatedShooting/Art");
            EnsureFolder(MaterialFolder);
            ConfigureSceneTexture(SloganListenTexturePath, true);
            ConfigureSceneTexture(SloganWinTexturePath, true);
            ConfigureSceneTexture(SloganDisciplineTexturePath, true);
            ConfigureSceneTexture(SloganDataWinsTexturePath, true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MovingTargetRange").transform;
            AddTestId(root.gameObject, "MovingTargetRange.Root");

            var ground = GetTiledMaterial("MovingTargetSandyGround", MudDiffusePath, MudNormalPath,
                new Color(1.22f, 1.08f, 0.88f), new Vector2(55f, 22f), 0.02f);
            var concrete = GetTiledMaterial("MovingTargetWeatheredConcrete", ConcreteDiffusePath, ConcreteNormalPath,
                new Color(0.92f, 0.90f, 0.82f), new Vector2(22f, 2f), 0.03f);
            var ridgeConcrete = GetMaterial("MovingTargetRidgeWall", new Color(0.72f, 0.70f, 0.62f), 0.02f);
            var earth = GetTiledMaterial("MovingTargetLoessBerm", MudDiffusePath, MudNormalPath,
                new Color(0.94f, 0.78f, 0.57f), new Vector2(25f, 6f), 0.01f);
            var sandPatch = GetMaterial("MovingTargetSandPatch", new Color(0.34f, 0.28f, 0.20f), 0.01f);
            var dryGrass = GetMaterial("MovingTargetDryGrass", new Color(0.38f, 0.35f, 0.15f), 0.01f);
            if (dryGrass.HasProperty("_Cull"))
                dryGrass.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(dryGrass);
            var darkPanel = GetMaterial("MovingTargetBayDark", new Color(0.09f, 0.09f, 0.075f), 0.01f);
            var route = GetMaterial("MovingTargetRoute", new Color(0.13f, 0.14f, 0.13f), 0.12f);
            var marker = GetMaterial("MovingTargetMarker", new Color(0.87f, 0.84f, 0.65f), 0.05f);
            var movingTarget = GetMaterial("MovingTargetSilhouette", new Color(0.035f, 0.04f, 0.035f), 0.08f);
            var rangeMarker = GetMaterial("RangeMarker", new Color(0.70f, 0.58f, 0.18f), 0.05f);
            var darkMetal = GetMaterial("RangeDarkMetal", new Color(0.035f, 0.04f, 0.035f), 0.30f);
            var fixedTarget = GetMaterial("TargetDark", new Color(0.035f, 0.04f, 0.035f), 0.05f);
            var targetBoard = GetMaterial("TargetBoard", new Color(0.76f, 0.74f, 0.65f), 0.05f);
            var tenRing = GetMaterial("TargetTenRing", new Color(0.88f, 0.86f, 0.75f), 0.05f);
            var impactMarker = GetMaterial("TargetImpactMarker", new Color(0.55f, 0.03f, 0.02f), 0.05f);
            var sloganListen = GetSloganMaterial("MovingTargetSloganListen", SloganListenTexturePath);
            var sloganWin = GetSloganMaterial("MovingTargetSloganWin", SloganWinTexturePath);
            var sloganDiscipline = GetSloganMaterial("MovingTargetSloganDiscipline", SloganDisciplineTexturePath);
            var sloganDataWins = GetSloganMaterial("MovingTargetSloganDataWins", SloganDataWinsTexturePath);

            CreateEnvironment(root, ground, concrete, ridgeConcrete, earth, sandPatch, dryGrass, darkPanel,
                route, rangeMarker, darkMetal, sloganListen, sloganWin, sloganDiscipline, sloganDataWins);
            CreateRouteAndTarget(root, route, marker, movingTarget);
            CreateFixedTargets(root, fixedTarget, targetBoard, tenRing, impactMarker, concrete);
            CreatePlayerAnchors(root, concrete);
            CreateLighting(root);
            ProneRangeSceneMigration.PatchOpenMovingTargetRangeScene();
            MarkRenderersStatic(root.Find("Environment"));
            MarkRenderersStatic(root.Find("MovingTargetRoute_40m"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[MovingTargetRangeSceneBuilder] Built MovingTargetRangeScene with a 100m range and 40m route.");
        }

        [MenuItem("Tools/Simulated Shooting/Open Moving Target Range Scene")]
        public static void OpenScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void CreateEnvironment(Transform root, Material ground, Material concrete, Material ridgeConcrete,
            Material earth, Material sandPatch, Material dryGrass, Material darkPanel, Material route,
            Material marker, Material markerPost, Material sloganListen, Material sloganWin,
            Material sloganDiscipline, Material sloganDataWins)
        {
            var environment = CreateAnchor("Environment", root, Vector3.zero);
            AddTestId(environment.gameObject, "MovingTargetRange.Environment.Root");
            var sandyGround = CreateSandyGround(environment, ground);
            AddTestId(sandyGround, "MovingTargetRange.Visual.SandyGround");
            CreateCube("FiringPad", environment, new Vector3(0f, 0.02f, 0f), new Vector3(12f, 0.15f, 5f), concrete, true);
            CreateCube("TargetTrackBed", environment, new Vector3(0f, 0.03f, 100f), new Vector3(44f, 0.12f, 1.1f), route, false);
            CreateGroundDetail(environment, sandPatch, dryGrass);
            CreateThreeDimensionalRange(environment, earth, concrete, ridgeConcrete, darkPanel, dryGrass);

            foreach (var distance in new[] { 25f, 50f, 75f, 100f })
            {
                CreateDistanceMarker(environment, -6.2f, distance, marker, markerPost);
                CreateDistanceMarker(environment, 6.2f, distance, marker, markerPost);
            }

            var listen = CreateSloganGroup("Slogan_听党指挥", "听党指挥", environment,
                new Vector3(-78f, 25.5f, 143.7f), sloganListen, new[]
                {
                    new Rect(0.091f, 0.351f, 0.165f, 0.319f), new Rect(0.308f, 0.351f, 0.172f, 0.319f),
                    new Rect(0.527f, 0.351f, 0.175f, 0.319f), new Rect(0.744f, 0.351f, 0.172f, 0.319f)
                });
            AddTestId(listen, "MovingTargetRange.Visual.Slogan.ListenToParty");
            var win = CreateSloganGroup("Slogan_能打胜仗", "能打胜仗", environment,
                new Vector3(0f, 25.5f, 143.7f), sloganWin, new[]
                {
                    new Rect(0.091f, 0.328f, 0.171f, 0.374f), new Rect(0.307f, 0.328f, 0.170f, 0.374f),
                    new Rect(0.519f, 0.328f, 0.177f, 0.374f), new Rect(0.740f, 0.328f, 0.173f, 0.374f)
                });
            AddTestId(win, "MovingTargetRange.Visual.Slogan.WinBattles");
            var discipline = CreateSloganGroup("Slogan_作风优良", "作风优良", environment,
                new Vector3(78f, 25.5f, 143.7f), sloganDiscipline, new[]
                {
                    new Rect(0.091f, 0.319f, 0.181f, 0.362f), new Rect(0.312f, 0.319f, 0.181f, 0.362f),
                    new Rect(0.533f, 0.319f, 0.180f, 0.362f), new Rect(0.761f, 0.319f, 0.169f, 0.362f)
                });
            AddTestId(discipline, "MovingTargetRange.Visual.Slogan.ExcellentConduct");

            var dataWins = CreateSloganGroup("Slogan_数据致胜", "数据致胜", environment,
                new Vector3(0f, 15.5f, 135.5f), sloganDataWins, new[]
                {
                    new Rect(0.130f, 0.374f, 0.136f, 0.243f), new Rect(0.331f, 0.374f, 0.136f, 0.243f),
                    new Rect(0.535f, 0.374f, 0.137f, 0.243f), new Rect(0.735f, 0.374f, 0.138f, 0.243f)
                }, 11f, new Vector2(7f, 7f));
            dataWins.transform.Find("Character_数").localPosition = new Vector3(-30f, -7f, -24.8f);
            foreach (var renderer in dataWins.GetComponentsInChildren<Renderer>(true))
                renderer.transform.localPosition = new Vector3(renderer.transform.localPosition.x, -7f, -24.8f);
            AddTestId(dataWins, "MovingTargetRange.Visual.Slogan.DataWins");

            var day = CreateAnchor("Lighting_Day", environment, Vector3.zero);
            AddTestId(day.gameObject, "MovingTargetRange.Lighting.Day");
            var night = CreateAnchor("Lighting_Night", environment, Vector3.zero);
            AddTestId(night.gameObject, "MovingTargetRange.Lighting.Night");
            night.gameObject.SetActive(false);
        }

        static GameObject CreateSandyGround(Transform environment, Material material)
        {
            const int xSegments = 64;
            const int zSegments = 36;
            const float width = 400f;
            const float depth = 136f;
            var vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[xSegments * zSegments * 6];

            for (var zIndex = 0; zIndex <= zSegments; zIndex++)
            {
                var z = zIndex * depth / zSegments - 10f;
                for (var xIndex = 0; xIndex <= xSegments; xIndex++)
                {
                    var x = xIndex * width / xSegments - width * 0.5f;
                    var index = zIndex * (xSegments + 1) + xIndex;
                    var undulation = z < 5f ? 0f :
                        Mathf.Sin(x * 0.21f + z * 0.09f) * 0.025f +
                        Mathf.Sin(x * 0.47f - z * 0.13f) * 0.012f;
                    vertices[index] = new Vector3(x, undulation, z);
                    uv[index] = new Vector2((float)xIndex / xSegments, (float)zIndex / zSegments);
                }
            }

            var triangleIndex = 0;
            for (var zIndex = 0; zIndex < zSegments; zIndex++)
            {
                for (var xIndex = 0; xIndex < xSegments; xIndex++)
                {
                    var lowerLeft = zIndex * (xSegments + 1) + xIndex;
                    var upperLeft = lowerLeft + xSegments + 1;
                    triangles[triangleIndex++] = lowerLeft;
                    triangles[triangleIndex++] = upperLeft;
                    triangles[triangleIndex++] = lowerLeft + 1;
                    triangles[triangleIndex++] = lowerLeft + 1;
                    triangles[triangleIndex++] = upperLeft;
                    triangles[triangleIndex++] = upperLeft + 1;
                }
            }

            var mesh = new Mesh { name = "MovingTargetSandyGroundMesh" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var ground = new GameObject("SandyTrainingGround", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            ground.transform.SetParent(environment, false);
            ground.GetComponent<MeshFilter>().sharedMesh = mesh;
            ground.GetComponent<MeshRenderer>().sharedMaterial = material;
            ground.GetComponent<MeshCollider>().sharedMesh = mesh;
            return ground;
        }

        static void CreateGroundDetail(Transform environment, Material sandPatch, Material dryGrass)
        {
            var patches = CreateAnchor("SandSoilVariation", environment, Vector3.zero);
            var random = new System.Random(20260811);
            for (var index = 0; index < 48; index++)
            {
                var radius = 1.4f + (float)random.NextDouble() * 3.0f;
                var patch = CreateIrregularGroundPatch($"SandPatch_{index:00}", patches,
                    new Vector3(-105f + (float)random.NextDouble() * 210f, 0.006f,
                        4f + (float)random.NextDouble() * 91f),
                    radius, radius * (0.55f + (float)random.NextDouble() * 0.3f),
                    (float)random.NextDouble() * 180f, sandPatch, index + 17);
                patch.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            var grass = CreateAnchor("SparseBrokenGrass", environment, Vector3.zero);
            AddTestId(grass.gameObject, "MovingTargetRange.Visual.SparseGrass");
            var grassMesh = CreateGrassClumpMesh();
            for (var index = 0; index < 180; index++)
            {
                var x = -110f + (float)random.NextDouble() * 220f;
                var z = 5f + (float)random.NextDouble() * 90f;
                var clump = new GameObject($"DryGrassClump_{index:000}", typeof(MeshFilter), typeof(MeshRenderer));
                clump.transform.SetParent(grass, false);
                clump.transform.localPosition = new Vector3(x, 0.018f, z);
                clump.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 180f, 0f);
                var scale = 0.65f + (float)random.NextDouble() * 0.8f;
                clump.transform.localScale = new Vector3(scale, scale * (0.8f + (float)random.NextDouble() * 0.45f), scale);
                clump.GetComponent<MeshFilter>().sharedMesh = grassMesh;
                var renderer = clump.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = dryGrass;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static GameObject CreateIrregularGroundPatch(string name, Transform parent, Vector3 position,
            float radiusX, float radiusZ, float rotation, Material material, int seed)
        {
            const int edgeCount = 10;
            var vertices = new Vector3[edgeCount + 1];
            var triangles = new int[edgeCount * 3];
            var random = new System.Random(seed);
            for (var index = 0; index < edgeCount; index++)
            {
                var angle = index * Mathf.PI * 2f / edgeCount;
                var variation = 0.72f + (float)random.NextDouble() * 0.28f;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radiusX * variation, 0f,
                    Mathf.Sin(angle) * radiusZ * variation);
                triangles[index * 3] = 0;
                triangles[index * 3 + 1] = index == edgeCount - 1 ? 1 : index + 2;
                triangles[index * 3 + 2] = index + 1;
            }
            var mesh = new Mesh { name = $"{name}_Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var patch = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            patch.transform.SetParent(parent, false);
            patch.transform.localPosition = position;
            patch.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
            patch.GetComponent<MeshFilter>().sharedMesh = mesh;
            patch.GetComponent<MeshRenderer>().sharedMaterial = material;
            return patch;
        }

        static Mesh CreateGrassClumpMesh()
        {
            var vertices = new[]
            {
                new Vector3(-0.12f, 0f, 0f), new Vector3(0.12f, 0f, 0f),
                new Vector3(-0.025f, 0.42f, 0f), new Vector3(0.025f, 0.42f, 0f),
                new Vector3(0f, 0f, -0.12f), new Vector3(0f, 0f, 0.12f),
                new Vector3(0f, 0.36f, -0.025f), new Vector3(0f, 0.36f, 0.025f),
                new Vector3(-0.08f, 0f, -0.08f), new Vector3(0.08f, 0f, 0.08f),
                new Vector3(-0.018f, 0.30f, -0.018f), new Vector3(0.018f, 0.30f, 0.018f)
            };
            var triangles = new[]
            {
                0, 2, 1, 1, 2, 3,
                4, 6, 5, 5, 6, 7,
                8, 10, 9, 9, 10, 11
            };
            var mesh = new Mesh { name = "MovingTargetDryGrassClump", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void CreateThreeDimensionalRange(Transform environment, Material earth, Material concrete,
            Material ridgeConcrete, Material darkPanel, Material dryGrass)
        {
            var berm = CreateAnchor("ThreeDimensionalBerm", environment, Vector3.zero);
            AddTestId(berm.gameObject, "MovingTargetRange.Visual.Berm3D");
            CreateTerracedBerm("MainLoessSlope", berm, 340f, 106f, 148f, earth);
            CreateCube("Terrace_Lower", berm, new Vector3(0f, 4.0f, 114.5f),
                new Vector3(330f, 0.35f, 1.8f), earth, true);
            CreateCube("Terrace_Middle", berm, new Vector3(0f, 8.1f, 123.2f),
                new Vector3(330f, 0.35f, 1.8f), earth, true);
            CreateCube("Terrace_Upper", berm, new Vector3(0f, 12.3f, 132.0f),
                new Vector3(330f, 0.35f, 1.8f), earth, true);

            var bays = CreateAnchor("TargetBayPanels", environment, Vector3.zero);
            AddTestId(bays.gameObject, "MovingTargetRange.Visual.TargetBayPanels");
            for (var index = 0; index < 56; index++)
            {
                var x = (index - 27.5f) * 3.25f;
                CreateCube($"TargetBay_{index:00}", bays, new Vector3(x, 1.15f, 105.6f),
                    new Vector3(3.05f, 2.3f, 0.45f), index % 2 == 0 ? concrete : darkPanel, true);
            }
            foreach (var moundX in new[] { -72f, -46f, -19f, 24f, 51f, 78f })
            {
                CreateSphere($"BackstopSoilMound_{moundX:0}", bays,
                    new Vector3(moundX, 0.2f, 104.8f), new Vector3(2.6f, 0.55f, 1.15f), earth);
            }

            var scrub = CreateAnchor("DryBermScrub", berm, Vector3.zero);
            var scrubMesh = CreateGrassClumpMesh();
            var scrubRandom = new System.Random(20260812);
            for (var index = 0; index < 84; index++)
            {
                var x = -155f + (float)scrubRandom.NextDouble() * 310f;
                var z = 108f + (float)scrubRandom.NextDouble() * 37f;
                var plant = new GameObject($"BermScrub_{index:000}", typeof(MeshFilter), typeof(MeshRenderer));
                plant.transform.SetParent(scrub, false);
                plant.transform.localPosition = new Vector3(x, GetTerracedBermHeight(z) + 0.03f, z);
                plant.transform.localRotation = Quaternion.Euler(0f, (float)scrubRandom.NextDouble() * 180f, 0f);
                var scale = 1.1f + (float)scrubRandom.NextDouble() * 1.3f;
                plant.transform.localScale = new Vector3(scale, scale * 1.2f, scale);
                plant.GetComponent<MeshFilter>().sharedMesh = scrubMesh;
                plant.GetComponent<MeshRenderer>().sharedMaterial = dryGrass;
            }

            var ridge = CreateAnchor("RidgeWall", environment, Vector3.zero);
            AddTestId(ridge.gameObject, "MovingTargetRange.Visual.RidgeWall");
            CreateCube("RidgeWall_Base", ridge, new Vector3(0f, 25.5f, 146f),
                new Vector3(270f, 9f, 1.4f), ridgeConcrete, true);
            for (var index = 0; index < 68; index++)
            {
                CreateCube($"Battlement_{index:00}", ridge,
                    new Vector3(-134f + index * 4f, 30.6f, 146f),
                    new Vector3(2.2f, 1.2f, 1.45f), ridgeConcrete, true);
            }
            CreateCube("RidgeTower_Left", ridge, new Vector3(-138f, 26f, 146f),
                new Vector3(5f, 10f, 4f), ridgeConcrete, true);
            CreateCube("RidgeTower_Right", ridge, new Vector3(138f, 26f, 146f),
                new Vector3(5f, 10f, 4f), ridgeConcrete, true);
        }

        static GameObject CreateTerracedBerm(string name, Transform parent, float width, float frontZ,
            float backZ, Material material)
        {
            var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            gameObject.transform.SetParent(parent, false);
            const int xSegments = 32;
            const int zSegments = 28;
            var vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[xSegments * zSegments * 6];

            for (var zIndex = 0; zIndex <= zSegments; zIndex++)
            {
                var normalizedZ = (float)zIndex / zSegments;
                var z = Mathf.Lerp(frontZ, backZ, normalizedZ);
                var baseHeight = GetTerracedBermHeight(z);
                for (var xIndex = 0; xIndex <= xSegments; xIndex++)
                {
                    var normalizedX = (float)xIndex / xSegments;
                    var x = Mathf.Lerp(-width * 0.5f, width * 0.5f, normalizedX);
                    var edgeDrop = Mathf.SmoothStep(0f, 1.3f,
                        Mathf.InverseLerp(width * 0.5f, width * 0.38f, Mathf.Abs(x)));
                    var naturalVariation = Mathf.Sin(x * 0.18f + z * 0.31f) * 0.12f +
                                           Mathf.Sin(x * 0.47f - z * 0.14f) * 0.06f;
                    var index = zIndex * (xSegments + 1) + xIndex;
                    vertices[index] = new Vector3(x, baseHeight * edgeDrop + naturalVariation, z);
                    uv[index] = new Vector2(normalizedX, normalizedZ);
                }
            }

            var triangleIndex = 0;
            for (var zIndex = 0; zIndex < zSegments; zIndex++)
            {
                for (var xIndex = 0; xIndex < xSegments; xIndex++)
                {
                    var lowerLeft = zIndex * (xSegments + 1) + xIndex;
                    var upperLeft = lowerLeft + xSegments + 1;
                    triangles[triangleIndex++] = lowerLeft;
                    triangles[triangleIndex++] = upperLeft;
                    triangles[triangleIndex++] = lowerLeft + 1;
                    triangles[triangleIndex++] = lowerLeft + 1;
                    triangles[triangleIndex++] = upperLeft;
                    triangles[triangleIndex++] = upperLeft + 1;
                }
            }

            var mesh = new Mesh { name = "MovingTargetLoessBermMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
            return gameObject;
        }

        static float GetTerracedBermHeight(float z)
        {
            var points = new[]
            {
                new Vector2(106f, 0.6f), new Vector2(113.8f, 3.8f), new Vector2(116.5f, 4.1f),
                new Vector2(122.8f, 7.9f), new Vector2(125.5f, 8.2f),
                new Vector2(131.8f, 12f), new Vector2(135f, 12.4f), new Vector2(148f, 18f)
            };
            for (var index = 1; index < points.Length; index++)
            {
                if (z <= points[index].x)
                    return Mathf.Lerp(points[index - 1].y, points[index].y,
                        Mathf.InverseLerp(points[index - 1].x, points[index].x, z));
            }
            return points[points.Length - 1].y;
        }

        static void CreateRouteAndTarget(Transform root, Material routeMaterial, Material markerMaterial,
            Material targetMaterial)
        {
            var route = CreateAnchor("MovingTargetRoute_40m", root, new Vector3(0f, 0f, 100f));
            AddTestId(route.gameObject, "MovingTargetRange.Route.Root");
            CreateCube("RouteRail", route, new Vector3(0f, 0.22f, 0f), new Vector3(40f, 0.08f, 0.12f), routeMaterial, false);

            var right = CreateAnchor("Anchor_MovingTarget_Right", route, new Vector3(20f, 0f, 0f));
            AddTestId(right.gameObject, "MovingTargetRange.Route.RightEndpoint");
            CreateEndpointMarker(right, "Right", markerMaterial);

            var left = CreateAnchor("Anchor_MovingTarget_Left", route, new Vector3(-20f, 0f, 0f));
            AddTestId(left.gameObject, "MovingTargetRange.Route.LeftEndpoint");
            CreateEndpointMarker(left, "Left", markerMaterial);

            var target = CreateAnchor("Target_Moving_SideProfile", root, right.position);
            AddTestId(target.gameObject, "MovingTargetRange.Target");
            CreateRunnerSilhouette(target, targetMaterial);

            var hitSurfaceObject = new GameObject("TargetHitSurface");
            hitSurfaceObject.transform.SetParent(target, false);
            hitSurfaceObject.transform.localPosition = new Vector3(0f, 1.05f, -0.02f);
            var hitSurface = hitSurfaceObject.AddComponent<BoxCollider>();
            hitSurface.size = new Vector3(0.9f, 1.9f, 0.08f);
            AddTestId(hitSurfaceObject, "MovingTargetRange.Target.HitSurface");

            var center = CreateAnchor("TargetCenter", target, new Vector3(0f, 1.05f, -0.07f));
            AddTestId(center.gameObject, "MovingTargetRange.Target.Center");
            var feedback = CreateAnchor("ImpactFeedback", target, Vector3.zero);
            AddTestId(feedback.gameObject, "MovingTargetRange.Target.ImpactFeedback");

            var bindingObject = new GameObject("MovingTargetRouteBinding");
            bindingObject.transform.SetParent(route, false);
            AddTestId(bindingObject, "MovingTargetRange.Target.Binding");
            var binding = bindingObject.AddComponent<MovingTargetRouteBinding>();
            binding.Configure(right, left, target, hitSurface, center, feedback);
        }

        static void CreateEndpointMarker(Transform endpoint, string suffix, Material material)
        {
            CreateCube($"EndpointPost_{suffix}", endpoint, new Vector3(0f, 1.25f, 0.45f),
                new Vector3(0.10f, 2.5f, 0.10f), material, false);
            CreateCube($"EndpointFlag_{suffix}", endpoint, new Vector3(suffix == "Right" ? -0.35f : 0.35f, 2.05f, 0.45f),
                new Vector3(0.7f, 0.42f, 0.04f), material, false);
        }

        static void CreateRunnerSilhouette(Transform parent, Material material)
        {
            CreateCapsule("Runner_Torso", parent, new Vector3(0f, 1.25f, 0f), new Vector3(0.24f, 0.42f, 0.11f),
                material, Quaternion.Euler(0f, 0f, -12f));
            CreateSphere("Runner_Head", parent, new Vector3(-0.10f, 1.82f, 0f), new Vector3(0.20f, 0.20f, 0.12f), material);
            CreateCapsule("Runner_ArmForward", parent, new Vector3(-0.30f, 1.32f, 0f), new Vector3(0.10f, 0.34f, 0.08f),
                material, Quaternion.Euler(0f, 0f, 62f));
            CreateCapsule("Runner_ArmRear", parent, new Vector3(0.28f, 1.25f, 0f), new Vector3(0.10f, 0.32f, 0.08f),
                material, Quaternion.Euler(0f, 0f, -58f));
            CreateCapsule("Runner_LegForward", parent, new Vector3(-0.22f, 0.62f, 0f), new Vector3(0.13f, 0.48f, 0.10f),
                material, Quaternion.Euler(0f, 0f, -34f));
            CreateCapsule("Runner_LegRear", parent, new Vector3(0.25f, 0.58f, 0f), new Vector3(0.13f, 0.48f, 0.10f),
                material, Quaternion.Euler(0f, 0f, 38f));
        }

        static void CreateFixedTargets(Transform root, Material targetMaterial, Material boardMaterial,
            Material ringMaterial, Material impactMarkerMaterial, Material supportMaterial)
        {
            var fixedTargets = CreateAnchor("FixedTargets_100m", root, Vector3.zero);
            AddTestId(fixedTargets.gameObject, "MovingTargetRange.FixedTargets.Root");
            var targetDefinitions = new[]
            {
                ("Left_Far", "MovingTargetRange.FixedTarget.Left.Far", -7f),
                ("Left_Near", "MovingTargetRange.FixedTarget.Left.Near", -3.5f),
                ("Center", "MovingTargetRange.FixedTarget.Center", 0f),
                ("Right_Near", "MovingTargetRange.FixedTarget.Right.Near", 3.5f),
                ("Right_Far", "MovingTargetRange.FixedTarget.Right.Far", 7f)
            };

            foreach (var definition in targetDefinitions)
            {
                CreateFixedTarget(fixedTargets, definition.Item1, definition.Item2,
                    new Vector3(definition.Item3, 1.5f, 101.2f), targetMaterial, boardMaterial,
                    ringMaterial, impactMarkerMaterial, supportMaterial);
            }
        }

        static void CreateFixedTarget(Transform parent, string suffix, string testId, Vector3 position,
            Material targetMaterial, Material boardMaterial, Material ringMaterial,
            Material impactMarkerMaterial, Material supportMaterial)
        {
            var target = CreateAnchor($"Target_Fixed_{suffix}_100m", parent, position);
            AddTestId(target.gameObject, testId);

            CreateCube("TargetBacker", target, new Vector3(0f, 0f, 0.03f),
                new Vector3(1.4f, 1.6f, 0.05f), targetMaterial, true);
            var face = CreateCube("TargetFace_50cm", target, Vector3.zero,
                new Vector3(0.5f, 0.5f, 0.02f), boardMaterial, true);

            var targetCenter = CreateAnchor("TargetCenter", target, new Vector3(0f, 0f, -0.012f));
            var impactMarkers = CreateAnchor("ImpactMarkers", target, Vector3.zero);
            var impactSurface = face.AddComponent<TargetImpactSurface>();
            impactSurface.Configure(face.GetComponent<Collider>(), targetCenter, impactMarkers, impactMarkerMaterial);

            CreateCube("TargetSilhouette_Torso", target, new Vector3(0f, -0.06f, -0.013f),
                new Vector3(0.36f, 0.38f, 0.008f), targetMaterial, false);
            CreateCylinder("TargetSilhouette_Head", target, new Vector3(0f, 0.17f, -0.014f),
                new Vector3(0.15f, 0.003f, 0.15f), targetMaterial,
                Quaternion.Euler(90f, 0f, 0f), false);
            CreateCylinder("TenRing_10cm", target, new Vector3(0f, 0f, -0.019f),
                new Vector3(0.1f, 0.0025f, 0.1f), ringMaterial,
                Quaternion.Euler(90f, 0f, 0f), false);
            CreateCube("TargetPost_Left", target, new Vector3(-0.18f, -0.95f, 0.08f),
                new Vector3(0.05f, 1.4f, 0.05f), supportMaterial, true);
            CreateCube("TargetPost_Right", target, new Vector3(0.18f, -0.95f, 0.08f),
                new Vector3(0.05f, 1.4f, 0.05f), supportMaterial, true);
        }

        static void CreateDistanceMarker(Transform parent, float x, float z, Material marker, Material post)
        {
            CreateCube($"DistancePost_{z:0}m_{x:0.0}", parent, new Vector3(x, 0.42f, z),
                new Vector3(0.05f, 0.8f, 0.05f), post, true);
            CreateCube($"DistanceBoard_{z:0}m_{x:0.0}", parent, new Vector3(x, 0.83f, z),
                new Vector3(0.45f, 0.55f, 0.04f), marker, true);

            var side = x < 0f ? "Left" : "Right";
            var label = new GameObject($"DistanceLabel_{z:0}m_{side}");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(x, 0.83f, z - 0.025f);
            label.transform.localRotation = Quaternion.identity;
            AddTestId(label, $"MovingTargetRange.Environment.DistanceLabel.{z:0}m.{side}");

            var text = label.AddComponent<TextMesh>();
            text.text = $"{z:0} m";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.fontStyle = FontStyle.Bold;
            text.characterSize = 0.035f;
            text.color = Color.white;
            var font = text.font ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.font = font;
            text.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(DistanceTextMaterialPath);
        }

        static void CreatePlayerAnchors(Transform root, Material concrete)
        {
            var anchors = CreateAnchor("TrainingAnchors", root, Vector3.zero);
            var shootingPosition = CreateAnchor("ShootingPosition", anchors, new Vector3(0f, 1.5f, 0f));
            AddTestId(shootingPosition.gameObject, "MovingTargetRange.ShootingPosition");
            var playerSpawn = CreateAnchor("PlayerSpawn", anchors, Vector3.zero);
            AddTestId(playerSpawn.gameObject, "MovingTargetRange.PlayerSpawn");
            var hudAnchor = CreateAnchor("HudAnchor", anchors, new Vector3(0f, 1.45f, 1.4f));
            AddTestId(hudAnchor.gameObject, "MovingTargetRange.Hud.Anchor");

            var weaponHooks = CreateAnchor("WeaponIntegrationAnchors", anchors, Vector3.zero);
            var muzzle = CreateAnchor("MuzzleHook", weaponHooks, new Vector3(0.35f, 1.2f, 0.7f));
            AddTestId(muzzle.gameObject, "MovingTargetRange.Weapon.Muzzle");
            var hitRay = CreateAnchor("HitRayHook", weaponHooks, muzzle.localPosition + Vector3.forward);
            AddTestId(hitRay.gameObject, "MovingTargetRange.Weapon.HitRay");
            var lowLight = CreateAnchor("LowLightOpticHook", weaponHooks, new Vector3(0f, 1.5f, 0.5f));
            AddTestId(lowLight.gameObject, "MovingTargetRange.Optic.LowLight");

            var cameraObject = new GameObject("Camera_NoVR", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(anchors, false);
            cameraObject.transform.position = shootingPosition.position;
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.05f, 100f) - shootingPosition.position);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 54f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            AddTestId(cameraObject, "MovingTargetRange.Camera.NoVR");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
            var xrOrigin = prefab == null
                ? new GameObject("XR Origin (VR)")
                : (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            xrOrigin.name = "XR Origin (VR)";
            xrOrigin.transform.SetParent(anchors, false);
            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;
            P1XrFloorOriginUpgrader.ConfigureFloorOrigin(xrOrigin);
            AddTestId(xrOrigin, "MovingTargetRange.Origin.VR");

            var modeController = anchors.gameObject.AddComponent<ZeroingRangeXRModeController>();
            modeController.Configure(xrOrigin, camera);

            CreateCube("ShootingPositionMarker", anchors, new Vector3(0f, 0.08f, 0f),
                new Vector3(1.4f, 0.03f, 1.0f), concrete, false);
        }

        static void CreateLighting(Transform root)
        {
            var lighting = CreateAnchor("Lighting", root, Vector3.zero);
            var sunObject = new GameObject("Sun_Day", typeof(Light));
            sunObject.transform.SetParent(lighting, false);
            sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.shadows = LightShadows.Soft;
            AddTestId(sunObject, "MovingTargetRange.Lighting.Sun");

            RenderSettings.skybox = GetDryClearSkyMaterial();

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.56f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.39f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.17f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.60f, 0.61f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 220f;
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool keepCollider)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, position, scale, material,
                Quaternion.identity, keepCollider);
        }

        static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material)
        {
            return CreatePrimitive(PrimitiveType.Sphere, name, parent, position, scale, material,
                Quaternion.identity, false);
        }

        static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Quaternion rotation, bool keepCollider)
        {
            return CreatePrimitive(PrimitiveType.Cylinder, name, parent, position, scale, material,
                rotation, keepCollider);
        }

        static GameObject CreateCapsule(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Quaternion rotation)
        {
            return CreatePrimitive(PrimitiveType.Capsule, name, parent, position, scale, material, rotation, false);
        }

        static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, Material material, Quaternion rotation, bool keepCollider)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                Object.DestroyImmediate(instance.GetComponent<Collider>());
            return instance;
        }

        static GameObject CreateCroppedQuad(string name, Transform parent, Vector3 position, Vector2 size,
            Material material, Rect uvRect)
        {
            var halfSize = size * 0.5f;
            var mesh = new Mesh { name = $"{name}_Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-halfSize.x, -halfSize.y, 0f), new Vector3(halfSize.x, -halfSize.y, 0f),
                new Vector3(-halfSize.x, halfSize.y, 0f), new Vector3(halfSize.x, halfSize.y, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(uvRect.xMin, uvRect.yMin), new Vector2(uvRect.xMax, uvRect.yMin),
                new Vector2(uvRect.xMin, uvRect.yMax), new Vector2(uvRect.xMax, uvRect.yMax)
            };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var quad = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = position;
            quad.transform.localRotation = Quaternion.identity;
            quad.GetComponent<MeshFilter>().sharedMesh = mesh;
            quad.GetComponent<MeshRenderer>().sharedMaterial = material;
            return quad;
        }

        static GameObject CreateSloganGroup(string name, string characters, Transform parent, Vector3 position,
            Material material, Rect[] characterUvRects, float characterSpacing = 18f,
            Vector2? characterSize = null)
        {
            if (characters.Length != characterUvRects.Length)
                throw new System.ArgumentException($"Slogan character and UV counts do not match for {name}.");

            var group = CreateAnchor(name, parent, position);
            var resolvedCharacterSize = characterSize ?? new Vector2(5.4f, 5.4f);
            for (var index = 0; index < characters.Length; index++)
            {
                CreateCroppedQuad($"Character_{characters[index]}", group,
                    new Vector3((index - 1.5f) * characterSpacing, 0f, 0f), resolvedCharacterSize, material,
                    characterUvRects[index]);
            }
            return group.gameObject;
        }

        static Transform CreateAnchor(string name, Transform parent, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }

        static void AddTestId(GameObject gameObject, string id)
        {
            gameObject.AddComponent<SceneTestId>().Id = id;
        }

        static Material GetMaterial(string name, Color color, float smoothness)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GetTiledMaterial(string name, string diffusePath, string normalPath, Color tint,
            Vector2 tiling, float smoothness)
        {
            var material = GetMaterial(name, tint, smoothness);
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (diffuse == null || normal == null)
                throw new System.InvalidOperationException($"Environment textures were not found for {name}.");

            material.mainTexture = diffuse;
            material.mainTextureScale = tiling;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", diffuse);
                material.SetTextureScale("_BaseMap", tiling);
            }
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GetDryClearSkyMaterial()
        {
            const string name = "MovingTargetDryClearSky";
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                    throw new System.InvalidOperationException("Procedural skybox shader was not found.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_SkyTint", new Color(0.26f, 0.53f, 0.78f));
            material.SetColor("_GroundColor", new Color(0.47f, 0.43f, 0.35f));
            material.SetFloat("_SunSize", 0.025f);
            material.SetFloat("_SunSizeConvergence", 5f);
            material.SetFloat("_AtmosphereThickness", 0.78f);
            material.SetFloat("_Exposure", 1.08f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GetSloganMaterial(string name, string texturePath)
        {
            var shader = Shader.Find(WorldSpaceTextShaderName);
            if (shader == null)
                throw new System.InvalidOperationException($"World-space decal shader was not found: {WorldSpaceTextShaderName}");
            var material = GetTextureMaterial(name, shader, texturePath, true);
            material.color = new Color(0.82f, 0.025f, 0.015f, 1f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", material.color);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GetTextureMaterial(string name, Shader shader, string texturePath, bool transparent)
        {
            if (shader == null)
                throw new System.InvalidOperationException($"Shader was not found for material {name}.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new System.InvalidOperationException($"Texture was not imported: {texturePath}");

            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.mainTexture = texture;
            material.color = Color.white;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);
            if (transparent && material.HasProperty("_ZTest"))
                material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ConfigureSceneTexture(string path, bool alpha)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Scene texture importer was not found: {path}");

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        static void MarkRenderersStatic(Transform root)
        {
            var flags = StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.ReflectionProbeStatic;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var separator = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
        }

        static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(scene => scene.path != ScenePath))
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
