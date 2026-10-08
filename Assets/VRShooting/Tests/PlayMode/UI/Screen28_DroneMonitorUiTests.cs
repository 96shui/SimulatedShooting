using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Common;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    // BDD28/task018: player takeoff view, live video binding, telemetry absence and recoverable error.
    public sealed class Screen28_DroneMonitorUiTests
    {
        [UnityTest]
        public IEnumerator ReconfiguringMonitorReleasesOldMaterialAndKeepsOneCommandListener()
        {
            var root=new GameObject("DroneMonitorLifecycleFixture",typeof(RectTransform));
            var ui=root.AddComponent<P3CombatUIRoot>();ui.BuildIfNeeded();ui.Show(ScreenId.TrenchDroneRecon);
            var view=ui.DroneReconView;
            TMP_Text Text(string name)=>root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name==name);
            GameObject Node(string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).gameObject;
            var back=Node("Button_TrenchDroneRecon_Back").GetComponent<Button>();
            var retry=Node("Button_TrenchDroneRecon_Retry").GetComponent<Button>();
            var oldFilter=view.Feed.material;
            var backs=0;var retries=0;
            view.BackRequested+=()=>backs++;view.RetryRequested+=()=>retries++;
            try
            {
                for(var i=0;i<2;i++)view.Configure(view.Feed,Node("Panel_TrenchDroneRecon_Monitor"),
                    Node("Hud_DroneRecon_Reticle"),Text("Text_TrenchDroneRecon_Phase"),
                    Text("Text_TrenchDroneRecon_FeedStatus"),Text("Text_TrenchDroneRecon_Elapsed"),
                    Text("Text_TrenchDroneRecon_Telemetry"),Text("Text_TrenchDroneRecon_Error"),back,retry);
                back.onClick.Invoke();retry.onClick.Invoke();
                Assert.That(backs,Is.EqualTo(1));Assert.That(retries,Is.EqualTo(1));
                Assert.That(view.Feed.material,Is.Not.SameAs(oldFilter));
                yield return null;
                Assert.That(oldFilter==null,Is.True,"Reconfiguration must release the previous filter material.");
                Object.Destroy(view);yield return null;
                back.onClick.Invoke();retry.onClick.Invoke();
                Assert.That(backs,Is.EqualTo(1));Assert.That(retries,Is.EqualTo(1));
                Assert.That(ui.GetComponentsInChildren<RawImage>(true).Single().texture,Is.Null);
            }
            finally {Object.Destroy(root);}
            yield return null;
        }
        [UnityTest]
        public IEnumerator MonitorUsesOnlyCurrentLiveTextureAndHidesDuringTakeoff()
        {
            var root=new GameObject("DroneMonitorFixture",typeof(RectTransform));
            var ui=root.AddComponent<P3CombatUIRoot>();ui.BuildIfNeeded();ui.Show(ScreenId.TrenchDroneRecon);
            var view=ui.DroneReconView;var texture=new Texture2D(16,16);
            try
            {
                view.Apply(new DroneReconSnapshotDto {Phase=DroneReconPhase.PlayerTakeoff,
                    ViewMode=DroneReconViewMode.Player,CharacterActionsLocked=true},null,false);
                Assert.That(view.Feed.gameObject.activeInHierarchy,Is.False);
                view.Apply(new DroneReconSnapshotDto {Phase=DroneReconPhase.DroneRecon,
                    ViewMode=DroneReconViewMode.DroneFeed,FeedAvailable=true,CharacterActionsLocked=true,
                    ReconElapsedSeconds=12.5f},texture,false);
                Assert.That(view.Feed.gameObject.activeInHierarchy,Is.True);
                Assert.That(view.Feed.texture,Is.SameAs(texture));
                var texts=root.GetComponentsInChildren<TMP_Text>(true);
                Assert.That(texts.Single(t=>t.name=="Text_TrenchDroneRecon_FeedStatus").text,Does.StartWith("LIVE"));
                Assert.That(texts.Single(t=>t.name=="Text_TrenchDroneRecon_Telemetry").text,Does.Contain("--"));
                Assert.That(texts.Single(t=>t.name=="Text_TrenchDroneRecon_Elapsed").text,Does.Contain("12"));
                Assert.That(view.Feed.material.shader.name,Is.EqualTo("VRShooting/UI/DroneMonitor"));
                Assert.That(root.GetComponentsInChildren<Transform>(true).All(t=>t.gameObject.layer==5),Is.True,
                    "The capture camera must exclude all monitor graphics to avoid video feedback.");
                view.Apply(new DroneReconSnapshotDto {Phase=DroneReconPhase.Error,
                    ViewMode=DroneReconViewMode.Player,CharacterActionsLocked=true,
                    ErrorCode=VRShooting.Contracts.ErrorCode.ResourceUnavailable},null,false);
                Assert.That(view.Feed.texture,Is.Null);
                Assert.That(view.Feed.gameObject.activeInHierarchy,Is.False);
                Assert.That(root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Button_TrenchDroneRecon_Retry")
                    .gameObject.activeInHierarchy,Is.True);
            }
            finally {Object.Destroy(root);Object.Destroy(texture);}
            yield return null;
        }
    }
}
