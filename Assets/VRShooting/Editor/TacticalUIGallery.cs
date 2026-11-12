using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SimulatedShooting.Scene;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using SessionState = VRShooting.Common.SessionState;
using VRShooting.Unity;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace VRShooting.Editor
{
    /// <summary>Isolated presentation inspection; never saves or replaces a user's scene.</summary>
    public static class TacticalUIGallery
    {
        public static void CaptureBatch() => Debug.Log(Capture());

        public static string Capture()
        {
            var folder = Environment.GetEnvironmentVariable("RUNTIME_GALLERY_OUT");
            if (string.IsNullOrWhiteSpace(folder)) folder = "docs/codex-reports/evidence/ui-art-completion";
            var captureStates = Environment.GetEnvironmentVariable("RUNTIME_GALLERY_STATES") == "1";
            var modeOneOnly = Environment.GetEnvironmentVariable("RUNTIME_GALLERY_MODE1") == "1";
            var modeOneNames = new Dictionary<string, string> {
                { "Screen_MainMenu", "01-mode-select" },
                { "Screen_ZeroingBriefing", "02-briefing" },
                { "Screen_ZeroingHud", "03-hud-round1" },
                { "Screen_ZeroingImpactAnalysis", "04-analysis-pass" },
                { "Screen_ZeroingFinalRating", "06-final-rating" }
            };
            Directory.CreateDirectory(folder);
            var scene = EditorSceneManager.NewPreviewScene();
            var objects = new List<GameObject>();
            RenderTexture target = null;
            RenderTexture dronePreview = null;
            try
            {
                var cameraRoot = new GameObject("GalleryCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraRoot, scene);
                var camera = cameraRoot.GetComponent<Camera>();
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(7, 15, 25, 255);
                camera.orthographic = true;
                camera.orthographicSize = 540;
                camera.transform.position = new Vector3(960, 540, -1000);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 2000;
                target = new RenderTexture(1920, 1080, 24);
                camera.targetTexture = target;
                var light = new GameObject("GalleryLight", typeof(Light));
                SceneManager.MoveGameObjectToScene(light, scene);
                light.GetComponent<Light>().type = LightType.Directional;
                // RuntimeInitializeOnLoadMethod does not run in this edit-mode preview.
                // Supply the actual loader so the menu reflects normal runtime availability.
                var services = ApplicationServices.CreateDefault(combatScenes: new UnityCombatSceneLoader());
                var main = Create<MainMenuUI>(scene, objects);
                main.Initialize(services);
                var range = Create<ZeroingRangeUI>(scene, objects);
                range.Initialize(services);
                var moving = Create<MovingTargetRangeUI>(scene, objects);
                moving.Initialize(new MovingTargetUICommandAdapter(services));
                FillRangePreview(range, moving);
                var combat = Create<P3CombatUIRoot>(scene, objects);
                combat.BuildIfNeeded();
                // A still of the actual trench scene supplies useful image content to the
                // monitor; it is a preview texture, not footage from a running mission.
                if (captureStates) dronePreview = RenderTrenchPreview();
                FillCombatPreview(combat, dronePreview);
                foreach (var root in objects)
                {
                    var adapter = root.GetComponent<TrainingUICanvasAdapter>();
                    if (adapter != null) adapter.enabled = false;
                    var scaler = root.GetComponent<CanvasScaler>();
                    if (scaler != null) scaler.enabled = false;
                    var canvas = root.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = camera;
                    var rect = (RectTransform)root.transform;
                    rect.sizeDelta = new Vector2(1920, 1080);
                    rect.pivot = new Vector2(.5f, .5f);
                    rect.position = new Vector3(960, 540, 0);
                    rect.localScale = Vector3.one;
                    foreach (var group in root.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        if (child.name == "LargePanelRoot" || child.name == "MinimalHudRoot") child.gameObject.SetActive(true);
                    root.SetActive(false);
                }
                var names = new List<string>();
                foreach (var root in objects)
                {
                    root.SetActive(true);
                    // The P1 pickup page is an actual full-size runtime panel, not a Screen_ node.
                    // MovingTargetSettings, conversely, is a zero-size test ID alias.
                    var pages = root.GetComponentsInChildren<RectTransform>(true).Where(t =>
                        (t.name.StartsWith("Screen_") || t.name == "Panel_Training_AwaitingWeaponPickup") && t.rect.width > 100).ToArray();
                    foreach (var page in pages) page.gameObject.SetActive(false);
                    foreach (var page in pages)
                    {
                        page.gameObject.SetActive(true);
                        var name = page.name == "Panel_Training_AwaitingWeaponPickup"
                            ? "Screen_ZeroingAwaitingWeaponPickup" : page.name;
                        if (modeOneOnly && !modeOneNames.TryGetValue(name, out name))
                        {
                            page.gameObject.SetActive(false);
                            continue;
                        }
                        CaptureFrame(page, name, folder, camera, target, names);
                        if (modeOneOnly && page.name == "Screen_MainMenu")
                            CaptureControlsState(page, "07-controls", folder, camera, target, names);
                        if (captureStates) CaptureExtraStates(page, folder, camera, target, names);
                        if (modeOneOnly && page.name == "Screen_ZeroingImpactAnalysis")
                            CaptureSpreadState(page, folder, camera, target, names);
                        page.gameObject.SetActive(false);
                    }
                    root.SetActive(false);
                }
                return string.Join(", ", names);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (dronePreview != null) { dronePreview.Release(); UnityEngine.Object.DestroyImmediate(dronePreview); }
            }
        }

        static RenderTexture RenderTrenchPreview()
        {
            var scene = SceneManager.GetSceneByPath(UnityCombatSceneLoader.TrenchScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(UnityCombatSceneLoader.TrenchScenePath, OpenSceneMode.Additive);
            var cameraScene = EditorSceneManager.NewPreviewScene();
            RenderTexture texture = null;
            try
            {
                var bindings = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<CombatSceneBindings>(true)).FirstOrDefault();
                if (bindings == null || bindings.PlayerSpawn == null)
                    throw new InvalidOperationException("Trench preview scene has no player spawn binding.");
                var target = bindings.PlayerSpawn.position;
                var cameraRoot = new GameObject("GalleryTrenchPreviewCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraRoot, cameraScene);
                var camera = cameraRoot.GetComponent<Camera>();
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.cullingMask = ~(1 << 5); // The feed contains scene geometry, never gallery UI.
                camera.fieldOfView = 60;
                camera.farClipPlane = 500;
                camera.transform.position = target + new Vector3(-15, 18, -20);
                camera.transform.LookAt(target + new Vector3(6, 0, 0));
                texture = new RenderTexture(1280, 720, 24);
                camera.targetTexture = texture;
                camera.Render();
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
                throw;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(cameraScene);
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void CaptureFrame(RectTransform page, string name, string folder, Camera camera,
            RenderTexture target, List<string> names)
        {
            foreach (var text in page.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
                names.Add(name);
                Debug.Log("GalleryState " + name + ": " + string.Join(" | ",
                    page.GetComponentsInChildren<TMP_Text>().Select(t => t.name + "=" + t.text)) +
                    " | buttons: " + string.Join(", ", page.GetComponentsInChildren<Button>()
                        .Select(b => b.name + "=" + b.interactable)));
            }
            finally
            {
                RenderTexture.active = old;
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static void CaptureSpreadState(RectTransform page, string folder, Camera camera,
            RenderTexture target, List<string> names)
        {
            var points = new[] { new Vector2(-20,-20), new Vector2(20,-20),
                new Vector2(-20,20), new Vector2(20,20) };
            var shots = points.Select((point, index) => new ZeroingShotDto {
                RoundIndex=1, ShotIndex=index+1, ImpactPointCm=point,
                InsideTenRing=ZeroingRules.IsInsideTenRing(point)
            }).ToArray();
            var analysis = ZeroingRules.BuildRoundAnalysis("preview", 1, shots, false,
                Vector2.zero, new Vector2(12,8), new SightAdjustmentDto { RearSightClicks=1, FrontSightDegrees=5 });
            if (!analysis.SpreadRejected) throw new InvalidOperationException("Expected a spread-rejected preview.");
            typeof(TrainingUIRoot).GetMethod("RenderImpactAnalysis", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(page.GetComponentInParent<TrainingUIRoot>(), new object[] { analysis });
            foreach (var action in new[] { "HorizontalMinus", "HorizontalPlus", "VerticalMinus", "VerticalPlus", "ApplyAdjustment" })
                if (Find(page, "Button_ZeroingImpactAnalysis_" + action).GetComponent<Button>().interactable)
                    throw new InvalidOperationException("Spread preview adjustment must be disabled: " + action);
            RequireActive(page, "Button_ZeroingImpactAnalysis_Help", false);
            RequireActive(page, "Button_ZeroingImpactAnalysis_Answer", false);
            CaptureFrame(page, "05-analysis-spread", folder, camera, target, names);
        }

        static void CaptureControlsState(RectTransform page, string name, string folder, Camera camera,
            RenderTexture target, List<string> names)
        {
            var panel = Find(page, "Panel_MainMenu_Controls").gameObject;
            RequireActive(page, panel.name, false);
            try
            {
                Click(page, "Button_MainMenu_Controls");
                RequireActive(page, panel.name, true);
                if (!panel.activeInHierarchy)
                    throw new InvalidOperationException("Gallery controls panel has an inactive parent.");
                CaptureFrame(page, name, folder, camera, target, names);
            }
            finally
            {
                if (panel.activeSelf) Click(page, "Button_MainMenu_Controls_Back");
                RequireActive(page, panel.name, false);
            }
        }

        static void CaptureExtraStates(RectTransform page, string folder, Camera camera,
            RenderTexture target, List<string> names)
        {
            if (page.name == "Screen_MainMenu")
            {
                CaptureControlsState(page, "Screen_MainMenu_ControlsOpen", folder, camera, target, names);
            }
            else if (page.name == "Screen_ZeroingImpactAnalysis")
            {
                Click(page, "Button_ZeroingImpactAnalysis_Help");
                RequireActive(page, "Button_ZeroingImpactAnalysis_Answer", false);
                CaptureFrame(page, "Screen_ZeroingImpactAnalysis_HelpOpen", folder, camera, target, names);
                Click(page, "Button_ZeroingImpactAnalysis_Help");
                Click(page, "Button_ZeroingImpactAnalysis_Answer");
                RequireActive(page, "Panel_ZeroingImpactAnalysis_Answer", true);
                RequireActive(page, "Button_ZeroingImpactAnalysis_Help", false);
                CaptureFrame(page, "Screen_ZeroingImpactAnalysis_AnswerOpen", folder, camera, target, names);
                Click(page, "Button_ZeroingImpactAnalysis_Answer");
                RequireActive(page, "Panel_ZeroingImpactAnalysis_Answer", false);
            }
            else if (page.name == "Screen_MovingTargetResults")
            {
                Click(page, "Button_MovingTargetResults_Help");
                RequireActive(page, "Panel_MovingTargetResults_Help", true);
                CaptureFrame(page, "Screen_MovingTargetResults_HelpOpen", folder, camera, target, names);
                Click(page, "Button_MovingTargetResults_Help");
                RequireActive(page, "Panel_MovingTargetResults_Help", false);
            }
        }

        static Transform Find(RectTransform page, string name)
        {
            var item = page.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == name);
            if (item == null) throw new InvalidOperationException("Gallery control not found: " + name);
            return item;
        }

        static void Click(RectTransform page, string name)
        {
            var button = Find(page, name).GetComponent<Button>();
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                throw new InvalidOperationException("Gallery button is not available: " + name);
            button.onClick.Invoke();
        }

        static void RequireActive(RectTransform page, string name, bool expected)
        {
            if (Find(page, name).gameObject.activeSelf != expected)
                throw new InvalidOperationException("Unexpected gallery state: " + name);
        }
        static T Create<T>(Scene scene, List<GameObject> objects) where T : Component
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.SetActive(false);
            SceneManager.MoveGameObjectToScene(go, scene);
            var component = go.AddComponent<T>();
            var flag = typeof(TrainingUIRoot).GetField("buildOnAwake", BindingFlags.NonPublic | BindingFlags.Instance);
            if (component is TrainingUIRoot) flag.SetValue(component, false);
            objects.Add(go);
            return component;
        }

        // Explicit editor-only samples for visual QA. No sample data enters runtime services or HUDs.
        static void FillRangePreview(ZeroingRangeUI zeroing, MovingTargetRangeUI moving)
        {
            // Use the production rule to populate all derived answer/grade fields consistently.
            ZeroingRoundAnalysisDto Round(int round, Vector2 average, Vector2 fixedOffset,
                Vector2 correction, bool applied, SightAdjustmentDto sight)
            {
                var offsets = new[] { new Vector2(-3,-2), new Vector2(3,-2), new Vector2(0,4), new Vector2(10,10) };
                var shots = Enumerable.Range(1, ZeroingRules.ShotsPerRound).Select(shot => {
                    var point = average + offsets[shot - 1];
                    return new ZeroingShotDto { RoundIndex=round, ShotIndex=shot,
                        ImpactPointCm=point, InsideTenRing=ZeroingRules.IsInsideTenRing(point) };
                }).ToArray();
                return ZeroingRules.BuildRoundAnalysis("preview", round, shots, applied, correction, fixedOffset, sight);
            }
            var initialSight = new SightAdjustmentDto { RearSightClicks=1, FrontSightDegrees=5 };
            var analysis = Round(1, new Vector2(12,8), new Vector2(12,8), Vector2.zero, false, initialSight);
            var rounds = new[] {
                Round(1, new Vector2(12,8), new Vector2(8,6.08f), new Vector2(-4,-1.92f), true,
                    ZeroingRules.ApplyCorrection(initialSight, new Vector2(-4,-1.92f))),
                Round(2, new Vector2(8,6.08f), new Vector2(4,4.8f), new Vector2(-4,-1.28f), true,
                    ZeroingRules.ApplyCorrection(initialSight, new Vector2(-8,-3.2f))),
                Round(3, new Vector2(4,4.8f), new Vector2(2,2.88f), new Vector2(-2,-1.92f), true,
                    ZeroingRules.ApplyCorrection(initialSight, new Vector2(-10,-5.12f)))
            };
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(TrainingUIRoot).GetMethod("RenderImpactAnalysis",flags).Invoke(zeroing,new object[]{analysis});
            typeof(TrainingUIRoot).GetMethod("RenderFinalRating",flags).Invoke(zeroing,new object[]{new ZeroingResultDto {
                SessionId="preview",Grade=ZeroingRules.ComputeFinalGrade(ZeroingRules.ResolvePassedRoundIndex(rounds)),
                PassedRoundIndex=ZeroingRules.ResolvePassedRoundIndex(rounds),Rounds=rounds}});
            HudTextLineDto Line(string key, string label, string value) => new HudTextLineDto { Key=key, Label=label, Value=value };
            typeof(TrainingUIRoot).GetMethod("RenderHud",flags).Invoke(zeroing,new object[]{new HudDto {
                SessionId="preview",Mode=TrainingMode.Zeroing100m,HudType=HudType.Zeroing,CanShoot=true,
                Ammo=new AmmoDto {CurrentMagazine=4,MagazineCapacity=4,ReserveAmmo=8},
                TextLines=new[] {Line("round","轮次","1/3"),Line("distance","距离","100m"),
                    Line("ammo","弹数","4/4"),Line("stability","稳定度","82%"),
                    Line("impactRecord","弹着记录","待记录 4 发"),
                    Line("shoulder","肩侧","右肩"),Line("rearSight","觇孔","向前 1 格"),
                    Line("frontSight","准心柱","顺时针 5°")},
                Prompts=new[] {new HudPromptDto {Text="稳定据枪"}}
            }});
            var plot = typeof(TrainingUIRoot).GetField("hudTargetPlot",flags).GetValue(zeroing) as TacticalTargetPlot;
            plot?.SetImpacts(Array.Empty<Vector2>());
            typeof(MovingTargetRangeUI).GetMethod("RenderResult",flags).Invoke(moving,new object[]{new MovingTargetResultViewModel {
                Summary="评级：优秀\n命中 7 / 10  ·  命中率 70%\n目标速度 4 m/s  ·  训练完成",
                Sequences=string.Join("\n",Enumerable.Range(1,10).Select(i=>"第 "+i+" 发   |   "+(i%3==0?"未命中":"命中")+"   |   两发起射 / 连射记录"))
            }});
            typeof(MovingTargetRangeUI).GetMethod("RenderHud",flags).Invoke(moving,new object[]{new MovingTargetHudViewModel {
                FireMode="两发起射 / 长按连射",Ammo="弹药 6 / 10",Hits="命中 3",Progress="进度 4 / 10",Speed="速度 4 m/s",Direction="方向：向右",FireState="逐发连射",Prompt="目标移动 · 允许射击",CanShoot=true,Countdown=""
            }});
        }

        static void FillCombatPreview(P3CombatUIRoot combat, Texture dronePreview)
        {
            var urbanFloors = LoadUrbanPreviewFloors();
            SquadStatusDto Squad(float playerHealth) => new SquadStatusDto { Members = new[] {
                new SquadMemberDto { MemberId="preview.player", Role=SquadMemberRole.Player, Health=playerHealth },
                new SquadMemberDto { MemberId="preview.two", Role=SquadMemberRole.TeammateTwo, Health=5, State=SquadMemberState.Following },
                new SquadMemberDto { MemberId="preview.three", Role=SquadMemberRole.TeammateThree, Health=5, State=SquadMemberState.Following }
            }};
            MiniMapDto Map(string id) => new MiniMapDto { MapId=id, Visible=true, Markers=new[] {
                new MapMarkerDto {MarkerId="player",Type=MarkerType.Player,NormalizedPosition=new Vector2(.21f,.24f)},
                new MapMarkerDto {MarkerId="teammate",Type=MarkerType.Teammate,NormalizedPosition=new Vector2(.21f,.14f)},
                new MapMarkerDto {MarkerId="estimate",Type=MarkerType.EnemyEstimate,NormalizedPosition=new Vector2(.56f,.62f)},
                new MapMarkerDto {MarkerId="killed",Type=MarkerType.EnemyKilled,NormalizedPosition=new Vector2(.37f,.43f)}
            }, Areas=Array.Empty<MapAreaDto>() };
            var trenchMap = Map("trench-a");
            combat.TrenchMapView.Apply(new[]{CombatMapCatalog.Trench}, "trench-a");
            combat.UrbanMapView.Apply(new[]{CombatMapCatalog.Urban}, "urban-a");
            var trenchSquad = Squad(8);
            var urbanSquad = Squad(4);
            combat.TrenchBriefingView.Apply(new TrenchBriefingDto {MapId="trench-a",PlannedSquad=Squad(10),ProjectedMap=trenchMap,EnemyEstimateMin=5,EnemyEstimateMax=7}, CombatMapCatalog.Trench);
            // The optional texture is a static preview of the current trench scene, not mission footage.
            combat.DroneReconView.Apply(new DroneReconSnapshotDto {
                SessionId="preview",Phase=DroneReconPhase.DroneRecon,ViewMode=DroneReconViewMode.DroneFeed,
                FeedAvailable=dronePreview!=null,ReconElapsedSeconds=8.4f,TelemetryValid=true,
                HeightAboveGroundMeters=12.3f,SpeedMetersPerSecond=2.5f
            }, dronePreview, false);
            var trenchPlayer = new PlayerStatusDto {Health=8,MaxHealth=CombatConfigDto.ForMode(TrainingMode.Trench).PlayerHealth,IsAlive=true};
            var urbanPlayer = new PlayerStatusDto {Health=8,MaxHealth=CombatConfigDto.ForMode(TrainingMode.Urban).PlayerHealth,IsAlive=true};
            var ammo = CombatConfigDto.Default.InitialAmmo;
            combat.TrenchHudView.Apply(new TrenchSessionDto {SessionId="preview",MapId="trench-a",State=SessionState.Running,EnemyKilled=2,EnemyTotal=5,SearchProgress01=.48f,Player=trenchPlayer,Ammo=ammo,Squad=trenchSquad,MiniMap=trenchMap},default);
            combat.TrenchResultsView.Apply(new TrenchResultDto {SessionId="preview",MapName="堑壕训练场",Victory=true,EnemyKilled=5,EnemyTotal=5,SearchProgress01=1,RemainingAmmo=57,ElapsedSeconds=246,Squad=trenchSquad,ResultMap=trenchMap});
            combat.UrbanStreetView.Apply(new UrbanSessionDto {SessionId="preview",MapId="urban-a",Phase=UrbanPhase.Street,State=SessionState.Running,Player=urbanPlayer,Ammo=ammo,Squad=urbanSquad,MiniMap=Map("urban-a"),StreetEnemyTotal=0},default);
            combat.UrbanBuildingView.Apply(new UrbanSessionDto {SessionId="preview",MapId="urban-a",Phase=UrbanPhase.Building,CurrentFloorId=urbanFloors[0].FloorId,Floors=urbanFloors,State=SessionState.Running,Player=urbanPlayer,Ammo=ammo,Squad=urbanSquad,MiniMap=Map("urban-1f"),RoomsTotal=3,RoomsSearched=1},default);
            combat.UrbanResultsView.Apply(new UrbanResultDto {SessionId="preview",Victory=true,StreetCleared=true,RoomsSearched=3,RoomsTotal=3,BuildingSearchProgress01=1,EnemyKilled=6,EnemyTotal=6,RemainingAmmo=82,Squad=urbanSquad,FloorMaps=new[]{Map("urban-1f"),Map("urban-2f"),Map("urban-3f")}});
        }

        static FloorDto[] LoadUrbanPreviewFloors()
        {
            var scene = SceneManager.GetSceneByPath(UnityCombatSceneLoader.ScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(UnityCombatSceneLoader.ScenePath, OpenSceneMode.Additive);
            try
            {
                var binding = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<CombatSceneBindings>(true)).First();
                return CombatSceneDefinitionBuilder.Build(binding, TrainingMode.Urban).Floors.ToArray();
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
