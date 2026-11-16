using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using VRShooting.Common;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD36: actual Physics queries, not injected visibility flags.
    public sealed class Round7EnemySightPhysicsTests
    {
        GameObject root, enemy, player;
        Vector3 at = new Vector3(300, 20, 300);
        [SetUp] public void Setup()
        {
            root = new GameObject("Round7Physics");
            enemy = Box("Self", at + Vector3.up, new Vector3(.8f, 2, .8f));
            player = Box("Player", at + new Vector3(0, 1, 6), new Vector3(.8f, 2, .8f));
        }
        GameObject Box(string name, Vector3 position, Vector3 size)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(root.transform); obj.transform.position = position; obj.transform.localScale = size; return obj;
        }
        CombatEnemySight Sample()
        {
            Physics.SyncTransforms();
            return CombatEnemySight.Sample(at + Vector3.up * 1.2f, at + Vector3.forward * 6, enemy.transform, player.transform, null, CombatConfigDto.Default);
        }
        [Test] public void Bdd36_SelfAndPlayerCollidersAreIgnoredOnOrdinaryLayer()
        {
            // Origin overlaps Self, endpoint is inside Player; both are layer 0.
            var gear = Box("SelfGear", at + new Vector3(0, 1.2f, .8f), Vector3.one * .5f);
            gear.transform.SetParent(enemy.transform, true);
            Assert.That(Sample().Visible, Is.True);
        }
        [Test] public void Bdd36_HeadCanSeeOverChestAndFootOcclusion()
        {
            var cover = Box("LowCover", at + new Vector3(0, .6f, 3), new Vector3(2, 1.3f, .3f));
            var sight = Sample();
            Assert.That(sight.HeadBlocker, Is.Null);
            Assert.That(sight.ChestBlocker, Is.EqualTo(cover.GetComponent<Collider>()));
            Assert.That(sight.FootBlocker, Is.EqualTo(cover.GetComponent<Collider>()));
            Assert.That(sight.Visible, Is.True);
        }
        [Test] public void Bdd36_AllThreeSamplesBlockedByWallIncludingLayerTwo()
        {
            var wall = Box("Wall", at + new Vector3(0, 1, 3), new Vector3(3, 3, .3f));
            wall.layer = 2;
            var sight = Sample();
            Assert.That(sight.InCircle, Is.True);
            Assert.That(sight.Visible, Is.False);
            Assert.That(sight.HeadBlocker, Is.EqualTo(wall.GetComponent<Collider>()));
        }
        [Test] public void Bdd36_OpenDoorImmediatelyDisablesLeafAndResetRestoresCollider()
        {
            var doorRoot = new GameObject("Door"); doorRoot.transform.SetParent(root.transform);
            var door = doorRoot.AddComponent<CombatDoorView>();
            var hinge = new GameObject("Hinge"); hinge.transform.SetParent(doorRoot.transform); door.Hinge = hinge.transform;
            var leaf = Box("DoorLeaf", at + new Vector3(0, 1, 3), new Vector3(3, 3, .3f)); leaf.transform.SetParent(hinge.transform);
            door.ResetView(); Assert.That(Sample().Visible, Is.False);
            door.Apply("open", true); Assert.That(leaf.GetComponent<Collider>().enabled, Is.False); Assert.That(Sample().Visible, Is.True);
            door.Apply("close", false); Assert.That(leaf.GetComponent<Collider>().enabled, Is.True); Assert.That(Sample().Visible, Is.False);
            door.Apply("reopen", true); door.ResetView(); Assert.That(leaf.GetComponent<Collider>().enabled, Is.True);
        }
        [Test] public void Bdd36_TurnSpeedUsesInjectedValue()
        {
            var actor = enemy.AddComponent<CombatActorView>();
            actor.TurnTowards(enemy.transform.position + Vector3.right, .1f, 90);
            Assert.That(Vector3.Angle(Vector3.forward, enemy.transform.forward), Is.EqualTo(9).Within(.01));
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);
    }
}
