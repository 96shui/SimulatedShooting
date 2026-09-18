using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    public class AmericanSoldierPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bdd23_Task014_ShotDeathResetAndNavigationDriveSkinnedSoldiers()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            yield return null;
            var binding = Object.FindObjectOfType<CombatSceneBindings>();
            binding.GetComponent<ZeroingRangeXRModeController>().SetVrModeForTests(false);
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled = false;
            var actor = fixture.Actors.First(a => a.EntityId == "teammate-2");
            var animation = actor.SoldierAnimation;
            var start = actor.transform.position;
            actor.PlayShot("task014-shot");
            yield return null;
            yield return new WaitForSeconds(.15f);
            Assert.That(animation.Animator.GetCurrentAnimatorStateInfo(0).IsName("Shot"), Is.True);
            actor.PlayHit("task014-hit"); actor.PlayHit("task014-hit");
            Assert.That(actor.HitFeedbackCount, Is.EqualTo(1));
            actor.ApplyDead(true);
            yield return new WaitForSeconds(1.3f);
            var time = animation.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            actor.ApplyDead(true);
            yield return null;
            Assert.That(animation.Animator.GetCurrentAnimatorStateInfo(0).IsName("Death"), Is.True);
            Assert.That(animation.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThanOrEqualTo(time));
            Assert.That(actor.HitCollider.enabled, Is.False);
            Assert.That(actor.Agent.enabled, Is.False);
            Assert.That(Vector3.Distance(actor.transform.position, start), Is.LessThan(.01f));
            Assert.That(animation.Chest.position.y - actor.transform.position.y, Is.LessThan(.8f), "Corpse must actually fall");
            Assert.That(actor.GetComponentInChildren<SkinnedMeshRenderer>().enabled, Is.True);
            actor.ApplyDead(false);
            yield return new WaitForSeconds(.2f);
            Assert.That(actor.HitCollider.enabled, Is.True);
            Assert.That(animation.Chest.position.y - actor.transform.position.y, Is.GreaterThan(1f), "Revive must restore an upright torso");
            actor.MoveTo(binding.PlayerSpawn.position, actor.transform.rotation);
            yield return new WaitForSeconds(.5f);
            Assert.That(animation.Animator.GetFloat("Speed"), Is.GreaterThan(.1f));
            Assert.That(Vector3.Distance(actor.transform.position, start), Is.GreaterThan(.1f));
            var leg = animation.Animator.GetComponentsInChildren<Transform>().First(t => t.name == "mott_var01:LeftLeg");
            var rotation = leg.localRotation;
            // Compare across a stride: two isolated samples can coincide in a looping gait.
            float maxAngle = 0;
            float deadline = Time.time + .7f;
            while (Time.time < deadline)
            {
                yield return null;
                maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(rotation, leg.localRotation));
            }
            Assert.That(maxAngle, Is.GreaterThan(1f), "Navigation must animate the leg during a stride");
        }
    }
}
