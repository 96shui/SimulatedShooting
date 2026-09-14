using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Common;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.PlayMode.Infrastructure
{
    // BDD 22 空白场景独立消费与卸载；BDD 15/19/20 HUD、房门状态。
    public sealed class Screen22_P3FixturePlayModeTests
    {
        [UnityTest]
        public IEnumerator Screen22_BlankSceneConsumesUiAndSceneFixturesThenUnsubscribes()
        {
            var scene = SceneManager.CreateScene("P3Task001FixtureOnly");
            var root = new GameObject("FixtureProbe");
            SceneManager.MoveGameObjectToScene(root, scene);
            var textObject = new GameObject("FixtureText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(root.transform);
            var actor = new GameObject("FixtureActor");
            actor.transform.SetParent(root.transform);
            var door = new GameObject("FixtureDoor");
            door.transform.SetParent(root.transform);
            using (var fake = new FakeUrbanService())
            using (var visuals = new FixtureFeed<CombatVisualSnapshotDto>(x => x.SessionId, x => x.Revision))
            {
                fake.Bind(P3Fixtures.UrbanSession("fixture", 1));
                visuals.Bind(new CombatVisualSnapshotDto { SessionId = "fixture", Revision = 1 });
                var probe = root.AddComponent<P3FixtureProbe>();
                probe.Attach(fake, visuals, textObject.GetComponent<Text>(), actor.transform, door);
                fake.Publish(P3Fixtures.UrbanSession("fixture", 2, UrbanPhase.Building));
                visuals.Publish(new CombatVisualSnapshotDto
                {
                    SessionId = "fixture", Revision = 2,
                    Entities = new[] { new CombatEntityVisualDto { EntityId = "enemy", Position = new Vector3(1, 2, 3), State = CombatEntityState.Dead, CorpseVisible = true } },
                    Doors = new[] { new CombatDoorVisualDto { RoomId = "room", Open = true } }
                });
                yield return null;
                Assert.That(textObject.GetComponent<Text>().text, Is.EqualTo("Building:30/120"));
                Assert.That(actor.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(door.activeSelf, Is.False);
                Assert.That(fake.SubscriberCount, Is.EqualTo(1));
                Assert.That(visuals.SubscriberCount, Is.EqualTo(1));
                yield return SceneManager.UnloadSceneAsync(scene);
                Assert.That(fake.SubscriberCount, Is.Zero);
                Assert.That(visuals.SubscriberCount, Is.Zero);
                Assert.DoesNotThrow(() => fake.Publish(P3Fixtures.UrbanSession("fixture", 3)));
                LogAssert.NoUnexpectedReceived();
            }
        }
    }

    public sealed class P3FixtureProbe : MonoBehaviour
    {
        FakeUrbanService service;
        FixtureFeed<CombatVisualSnapshotDto> visuals;
        Text label;
        Transform actor;
        GameObject door;
        public void Attach(FakeUrbanService source, FixtureFeed<CombatVisualSnapshotDto> frames, Text text, Transform target, GameObject doorObject)
        {
            service = source; visuals = frames; label = text; actor = target; door = doorObject;
            service.SessionChanged += Render;
            visuals.Published += RenderScene;
        }
        void Render(UrbanSessionDto dto) => label.text = $"{dto.Phase}:{dto.Ammo.CurrentMagazine}/{dto.Ammo.ReserveAmmo}";
        void RenderScene(CombatVisualSnapshotDto dto)
        {
            foreach (var entity in dto.Entities) actor.position = entity.Position;
            foreach (var state in dto.Doors) door.SetActive(!state.Open);
        }
        void OnDestroy()
        {
            if (service != null) service.SessionChanged -= Render;
            if (visuals != null) visuals.Published -= RenderScene;
        }
    }
}
