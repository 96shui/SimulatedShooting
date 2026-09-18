using UnityEngine;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.UI
{
    // Production presenter: commands are owned by the application coordinator, not standalone UI fixtures.
    public sealed class P3LiveUIController : MonoBehaviour
    {
        CombatApplicationCoordinator app;
        P3CombatUIRoot view;
        ScreenId lastScreen = ScreenId.MainMenu;
        float nextRefresh;
        string renderedResultSession="";
        public P3CombatUIRoot View => view;
        public void Initialize(CombatApplicationCoordinator application)
        {
            app=application;
            var root=new GameObject("P3CombatUI",typeof(RectTransform));
            root.transform.SetParent(transform,false);
            view=root.AddComponent<P3CombatUIRoot>();
            view.BuildIfNeeded();
            view.TrenchMapView.Apply(new[]{CombatMapCatalog.Trench},P3ContractIds.TrenchMap);
            view.UrbanMapView.Apply(new[]{CombatMapCatalog.Urban},P3ContractIds.UrbanMap);
            view.TrenchMapView.SelectRequested+=SelectTrench;
            view.UrbanMapView.SelectRequested+=SelectUrban;
            view.TrenchMapView.BackRequested+=Return;
            view.UrbanMapView.BackRequested+=Return;
            view.TrenchBriefingView.StartRequested+=StartMission;
            view.TrenchBriefingView.BackRequested+=Back;
            view.TrenchBriefingView.ViewMapRequested+=ToggleMap;
            view.TrenchResultsView.RetryRequested+=Retry;
            view.UrbanResultsView.RetryRequested+=Retry;
            view.TrenchResultsView.BackToMainMenuRequested+=Return;
            view.UrbanResultsView.BackToMainMenuRequested+=Return;
            view.UrbanStreetView.EnterBuildingRequested+=Enter;
            view.UrbanBuildingView.ExitBuildingRequested+=Exit;
            view.UrbanBuildingView.OpenRoomDoorRequested+=OpenDoor;
            view.UrbanBuildingView.MarkRoomSearchedRequested+=CheckRoom;
            app.Changed+=Render;
            Render(app.Snapshot);
        }
        async void SelectTrench() { await app.SelectMapAsync(view.TrenchMapView.SelectedMapId,RandomSeed.Fixed(20260916)); }
        async void SelectUrban() { await app.SelectMapAsync(view.UrbanMapView.SelectedMapId,RandomSeed.Fixed(20260916)); }
        void StartMission()=>app.Start();
        void Back()=>app.BackToMaps();
        void Return()=>app.ReturnToMainMenu();
        void Retry()=>app.Retry();
        void ToggleMap()=>view.TrenchBriefingView.SetMapFocused(!view.TrenchBriefingView.MapFocused);
        void Enter() { var m=app.Mission; if(m?.Urban!=null) Error(m.Urban.EnterBuilding(m.SessionId,m.Definition.EntranceId).ErrorCode); }
        void Exit() { var m=app.Mission; if(m?.Urban!=null) Error(m.Urban.ExitBuilding(m.SessionId,m.Definition.EntranceId).ErrorCode); }
        void OpenDoor(string id) { var m=app.Mission; if(m?.Urban!=null) Error(m.Urban.OpenRoomDoor(m.SessionId,id).ErrorCode); }
        void CheckRoom(string id) { var m=app.Mission; if(m?.Urban!=null) Error(m.Urban.MarkRoomSearched(m.SessionId,id).ErrorCode); }
        void Error(ErrorCode code)
        {
            if(code==ErrorCode.None)return;
            view.TrenchMapView.ShowError(code,""); view.UrbanMapView.ShowError(code,"");
            view.TrenchBriefingView.ShowError(code,""); view.TrenchResultsView.ShowError(code,"");
            view.UrbanResultsView.ShowError(code,""); view.UrbanBuildingView.ShowError(code,"");
        }
        void Render(CombatApplicationSnapshotDto state)
        {
            if(view==null)return;
            view.gameObject.SetActive(state.Mode.HasValue);
            if(!state.Mode.HasValue)return;
            if(lastScreen!=state.Screen)
            {
                view.Show(state.Screen); lastScreen=state.Screen;
                nextRefresh=0;
                view.GetComponent<TrainingUICanvasAdapter>()?.ForcePlacementForTests();
            }
            var m=app.Mission;
            if(m!=null && state.Screen==ScreenId.TrenchBriefing)
            {
                var brief=m.Trench.GetBriefing(m.Definition.MapId,P3ContractIds.TrainingWeapon,RandomSeed.Fixed(20260916));
                var map=m.Trench.SelectMap(m.Definition.MapId);
                if(brief.Success&&map.Success)view.TrenchBriefingView.Apply(brief.Data,map.Data);
            }
            view.TrenchMapView.SetBusy(state.Busy); view.UrbanMapView.SetBusy(state.Busy);
            view.TrenchBriefingView.SetBusy(state.Busy);
            if(Time.unscaledTime>=nextRefresh && (!state.Summary.HasValue || renderedResultSession!=state.SessionId))
            {
                nextRefresh=Time.unscaledTime+.1f;
                if(m!=null && m.SessionId.Length>0)
                {
                    if(state.Summary.HasValue)renderedResultSession=state.SessionId;
                    var hud=m.Hud.GetHud(m.SessionId);
                    if(m.Trench!=null)
                    {
                        var session=m.Trench.GetSession(m.SessionId);
                        if(session.Success&&hud.Success)view.TrenchHudView.Apply(session.Data,hud.Data);
                        if(state.Screen==ScreenId.TrenchResults) { var result=m.Trench.GetResult(m.SessionId);if(result.Success)view.TrenchResultsView.Apply(result.Data); }
                    }
                    else
                    {
                        var session=m.Urban.GetSession(m.SessionId);
                        if(session.Success&&hud.Success)
                        { view.UrbanStreetView.Apply(session.Data,hud.Data);view.UrbanBuildingView.Apply(session.Data,hud.Data); }
                        if(state.Screen==ScreenId.UrbanResults) { var result=m.Urban.GetResult(m.SessionId);if(result.Success)view.UrbanResultsView.Apply(result.Data); }
                    }
                }
            }
            Error(state.Error);
        }
        void LateUpdate()
        {
            if(app==null||view==null||!view.gameObject.activeInHierarchy)return;
            if(Time.unscaledTime>=nextRefresh)Render(app.Snapshot);
            var s=app.Snapshot.Screen;
            if(s!=ScreenId.TrenchHud&&s!=ScreenId.UrbanStreetHud&&s!=ScreenId.UrbanBuildingHud)return;
            var adapter=view.GetComponent<TrainingUICanvasAdapter>();
            if(adapter!=null&&adapter.IsVrMode&&adapter.ActiveVrCamera!=null)
            {
                // HUD stays readable while walking; never changes the HMD transform.
                var eye=adapter.ActiveVrCamera.transform;
                view.transform.SetPositionAndRotation(eye.position+eye.forward*1.4f,eye.rotation);
            }
        }
        void OnDestroy() { if(app!=null)app.Changed-=Render; }
    }
}
