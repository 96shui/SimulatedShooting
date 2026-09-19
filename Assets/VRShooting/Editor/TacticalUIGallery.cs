using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            const string folder = "docs/codex-reports/evidence/ui-art-completion";
            Directory.CreateDirectory(folder);
            var scene = EditorSceneManager.NewPreviewScene();
            var objects = new List<GameObject>();
            RenderTexture target = null;
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
                var services = ApplicationServices.CreateDefault();
                var main = Create<MainMenuUI>(scene, objects);
                main.Initialize(services);
                var range = Create<ZeroingRangeUI>(scene, objects);
                range.Initialize(services);
                var moving = Create<MovingTargetRangeUI>(scene, objects);
                moving.Initialize(new MovingTargetUICommandAdapter(services));
                FillRangePreview(range, moving);
                var combat = Create<P3CombatUIRoot>(scene, objects);
                combat.BuildIfNeeded();
                FillCombatPreview(combat);
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
                    var pages = root.GetComponentsInChildren<RectTransform>(true).Where(t => t.name.StartsWith("Screen_") && t.rect.width > 100).ToArray();
                    foreach (var page in pages) page.gameObject.SetActive(false);
                    foreach (var page in pages)
                    {
                        page.gameObject.SetActive(true);
                        foreach (var text in page.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                        Canvas.ForceUpdateCanvases();
                        camera.Render();
                        var old = RenderTexture.active;
                        RenderTexture.active = target;
                        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                        texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                        texture.Apply();
                        File.WriteAllBytes(folder + "/" + page.name + ".png", texture.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(texture);
                        RenderTexture.active = old;
                        names.Add(page.name);
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
            }
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
            var rounds = Enumerable.Range(1, 3).Select(round => new ZeroingRoundAnalysisDto {
                SessionId="preview", RoundIndex=round, PassedTenRing=round==3, AdjustmentApplied=true,
                AverageOffsetCm=round==3?new Vector2(0,1):new Vector2(10-round*2,4-round*2), Shots=Enumerable.Range(1,3).Select(shot=>new ZeroingShotDto {
                    RoundIndex=round,ShotIndex=shot,ImpactPointCm=round==3?new Vector2(shot-2,shot*.5f):new Vector2(8-round*2+shot,6-round*2-shot)
                }).ToArray()
            }).ToArray();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(TrainingUIRoot).GetMethod("RenderImpactAnalysis",flags).Invoke(zeroing,new object[]{rounds[0]});
            typeof(TrainingUIRoot).GetMethod("RenderFinalRating",flags).Invoke(zeroing,new object[]{new ZeroingResultDto {SessionId="preview",Grade=ResultGrade.Pass,PassedRoundIndex=3,Rounds=rounds}});
            typeof(MovingTargetRangeUI).GetMethod("RenderResult",flags).Invoke(moving,new object[]{new MovingTargetResultViewModel {
                Summary="评级：良好\n命中 7 / 10  ·  命中率 70%\n目标速度 4 m/s  ·  训练完成",
                Sequences=string.Join("\n",Enumerable.Range(1,10).Select(i=>"第 "+i+" 发   |   "+(i%3==0?"未命中":"命中")+"   |   两发起射 / 连射记录"))
            }});
            typeof(MovingTargetRangeUI).GetMethod("RenderHud",flags).Invoke(moving,new object[]{new MovingTargetHudViewModel {
                FireMode="两发起射 / 长按连射",Ammo="弹药 6 / 10",Hits="命中 3",Progress="进度 4 / 10",Speed="速度 4 m/s",Direction="方向：向右",FireState="逐发连射",Prompt="目标移动 · 允许射击",CanShoot=true,Countdown=""
            }});
        }

        static void FillCombatPreview(P3CombatUIRoot combat)
        {
            var squad = new SquadStatusDto { Members = new[] {
                new SquadMemberDto { MemberId="preview.player", Role=SquadMemberRole.Player, Health=82 },
                new SquadMemberDto { MemberId="preview.two", Role=SquadMemberRole.TeammateTwo, Health=100, State=SquadMemberState.Following },
                new SquadMemberDto { MemberId="preview.three", Role=SquadMemberRole.TeammateThree, Health=64, State=SquadMemberState.Following }
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
            combat.TrenchBriefingView.Apply(new TrenchBriefingDto {MapId="trench-a",PlannedSquad=squad,ProjectedMap=trenchMap,EnemyEstimateMin=3,EnemyEstimateMax=5}, CombatMapCatalog.Trench);
            var player = new PlayerStatusDto {Health=82,IsAlive=true};
            var ammo = CombatConfigDto.Default.InitialAmmo;
            combat.TrenchHudView.Apply(new TrenchSessionDto {SessionId="preview",MapId="trench-a",State=SessionState.Running,EnemyKilled=2,EnemyTotal=5,SearchProgress01=.48f,Player=player,Ammo=ammo,Squad=squad,MiniMap=trenchMap},default);
            combat.TrenchResultsView.Apply(new TrenchResultDto {SessionId="preview",MapName="堑壕训练场",Victory=true,EnemyKilled=5,EnemyTotal=5,SearchProgress01=1,RemainingAmmo=97,ElapsedSeconds=246,Squad=squad,ResultMap=trenchMap});
            combat.UrbanStreetView.Apply(new UrbanSessionDto {SessionId="preview",MapId="urban-a",Phase=UrbanPhase.Street,State=SessionState.Running,Player=player,Ammo=ammo,Squad=squad,MiniMap=Map("urban-a"),StreetEnemyTotal=2},default);
            combat.UrbanBuildingView.Apply(new UrbanSessionDto {SessionId="preview",MapId="urban-a",Phase=UrbanPhase.Building,CurrentFloorId="urban-a.floor-1",State=SessionState.Running,Player=player,Ammo=ammo,Squad=squad,MiniMap=Map("urban-1f"),RoomsTotal=3,RoomsSearched=1},default);
            combat.UrbanResultsView.Apply(new UrbanResultDto {SessionId="preview",Victory=true,StreetCleared=true,RoomsSearched=3,RoomsTotal=3,BuildingSearchProgress01=1,EnemyKilled=6,EnemyTotal=6,RemainingAmmo=82,Squad=squad,FloorMaps=new[]{Map("urban-1f"),Map("urban-2f"),Map("urban-3f")}});
        }
    }
}
