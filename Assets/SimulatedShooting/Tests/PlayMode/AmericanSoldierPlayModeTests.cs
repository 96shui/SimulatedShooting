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
        [UnityTest] public IEnumerator Bdd23_Task019_EnemyDiesWithRelaxedArms() => CheckDeath("DetailedCharacterD3");
        [UnityTest] public IEnumerator Bdd23_Task019_TeammateDiesWithRelaxedArms() => CheckDeath("DetailedCharacterD4");

        IEnumerator CheckDeath(string model)
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            yield return null;
            var fixture=Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled=false;
            var actor=fixture.Actors.First(a=>a.VisualRoot.Find(model)!=null);
            var animation=actor.SoldierAnimation;
            var hips=animation.Animator.GetComponentsInChildren<Transform>().Single(t=>t.name=="Hips");
            var feet=actor.transform.position;
            var cameraObject=new GameObject("Task019_DeathEvidenceCamera");
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            // Isolate the real actor for pose inspection; surrounding bunker walls occlude this angle.
            foreach(var part in actor.GetComponentsInChildren<Transform>(true))part.gameObject.layer=31;
            camera.cullingMask=1<<31;camera.fieldOfView=40;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.19f,.22f);
            camera.transform.position=feet+new Vector3(2.4f,1.1f,3.0f);
            camera.transform.LookAt(feet+Vector3.up*.85f);
            try
            {
                PresentationEvidence.Capture(camera,model+"-standing");
                actor.ApplyDead(true);
                for(int frame=0;frame<70;frame++)
                {
                    yield return new WaitForSeconds(.03f);
                    if(frame>=6)
                    {
                        var up=(animation.Chest.position-hips.position).normalized;
                        Assert.That(Vector3.Dot(animation.LeftHand.position-animation.Chest.position,up),Is.LessThan(.15f),model+" left arm raised");
                        Assert.That(Vector3.Dot(animation.RightHand.position-animation.Chest.position,up),Is.LessThan(.15f),model+" right arm raised");
                    }
                    if(frame==5||frame==17||frame==34||frame==69)PresentationEvidence.Capture(camera,model+"-death-"+frame);
                }
                Assert.That(animation.Chest.position.y-feet.y,Is.LessThan(.8f));
                Assert.That(Vector3.Distance(actor.transform.position,feet),Is.LessThan(.01f));
                actor.ApplyDead(false);
                yield return new WaitForSeconds(.2f);
                Assert.That(animation.Chest.position.y-feet.y,Is.GreaterThan(1f));
            }
            finally {Object.Destroy(cameraObject);}
        }
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
            Assert.That(actor.VisualRoot.Find("DetailedCharacterD4"), Is.Not.Null);
            var enemy = fixture.Actors.First(a => !a.EntityId.StartsWith("teammate"));
            Assert.That(enemy.VisualRoot.Find("DetailedCharacterD3"), Is.Not.Null);
            var start = actor.transform.position;
            actor.PlayShot("task014-shot");
            yield return null;
            yield return new WaitForSeconds(.15f);
            Assert.That(animation.Animator.GetCurrentAnimatorStateInfo(0).IsName("Shot"), Is.True);
            actor.PlayHit("task014-hit"); actor.PlayHit("task014-hit");
            Assert.That(actor.HitFeedbackCount, Is.EqualTo(1));
            actor.ApplyDead(true);
            // BDD23 task019: the torso collapses with bent arms instead of raising both hands.
            var hips = animation.Animator.GetComponentsInChildren<Transform>().Single(t => t.name == "Hips");
            for (int frame = 0; frame < 30; frame++)
            {
                yield return new WaitForSeconds(.03f);
                if (frame < 6) continue; // The transition preserves the current hit/grip pose.
                var torsoUp = (animation.Chest.position - hips.position).normalized;
                Assert.That(Vector3.Dot(animation.LeftHand.position - animation.Chest.position, torsoUp), Is.LessThan(.15f), "Left arm must stay below the head");
                Assert.That(Vector3.Dot(animation.RightHand.position - animation.Chest.position, torsoUp), Is.LessThan(.15f), "Right arm must stay below the head");
            }
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
            var leg = animation.Animator.GetComponentsInChildren<Transform>().First(t => t.name == "L_knee");
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

        [UnityTest]
        public IEnumerator Bdd23_EnemyShotHasSoundAndCorpseHidesAfterFiveSeconds()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            yield return null;
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            var enemy = fixture.Actors.First(a => !a.EntityId.StartsWith("teammate"));
            Assert.That(enemy.ShotClip, Is.Not.Null);
            Assert.That(enemy.HideCorpseAfterDelay, Is.True);
            Assert.That(enemy.Audio.spatialBlend, Is.EqualTo(1f));
            enemy.PlayShot("bdd23-audible-shot");
            enemy.PlayShot("bdd23-audible-shot");
            Assert.That(enemy.ShotAudioFeedbackCount, Is.EqualTo(1));

            enemy.ApplyDead(true);
            Assert.That(enemy.VisualRoot.gameObject.activeSelf, Is.True);
            yield return new WaitForSeconds(5.1f);
            Assert.That(enemy.VisualRoot.gameObject.activeSelf, Is.False);
            Assert.That(enemy.IsDead, Is.True, "Hiding the corpse must not revive the enemy.");

            enemy.ApplyDead(false);
            Assert.That(enemy.VisualRoot.gameObject.activeSelf, Is.True);
        }
    }

    internal static class PresentationEvidence
    {
        internal static void Capture(Camera camera,string name)
        {
            var output=System.IO.Path.Combine(Application.dataPath,"../Temp/GoalRepair/images");
            System.IO.Directory.CreateDirectory(output);
            var target=new RenderTexture(1280,960,24);
            var texture=new Texture2D(1280,960,TextureFormat.RGB24,false);
            var outputCamera=new GameObject("PresentationEvidenceCamera").AddComponent<Camera>();
            outputCamera.CopyFrom(camera);outputCamera.enabled=false;
            outputCamera.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
            outputCamera.ResetWorldToCameraMatrix();outputCamera.ResetProjectionMatrix();outputCamera.aspect=1280f/960f;
            var previousActive=RenderTexture.active;
            try
            {
                outputCamera.stereoTargetEye=StereoTargetEyeMask.None;outputCamera.targetTexture=target;outputCamera.Render();
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,960),0,0);texture.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,name+".png"),texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previousActive;Object.Destroy(outputCamera.gameObject);
                target.Release();Object.Destroy(target);Object.Destroy(texture);
            }
        }
    }
}
