using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD00 wall lettering / P1 task016 and P2 task006.
    public sealed class RangeIronFistVictoryPlayModeTests
    {
        [UnityTest] public IEnumerator ModeOneFourCharactersAreReadableFromFiringEye() => Verify("ZeroingRangeScene","ZeroingRange");
        [UnityTest] public IEnumerator ModeTwoFourCharactersAreReadableFromFiringEye() => Verify("MovingTargetRangeScene","MovingTargetRange");

        IEnumerator Verify(string sceneName, string prefix)
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
            var mode=UnityEngine.Object.FindObjectOfType<ZeroingRangeXRModeController>();mode.SetVrModeForTests(false);
            yield return null; yield return null;
            var group=UnityEngine.Object.FindObjectsOfType<SceneTestId>(true).Single(t=>t.Id==prefix+".Visual.Slogan.DataWins");
            Assert.That(group.name,Is.EqualTo("Slogan_铁拳胜利"));
            var letters=group.GetComponentsInChildren<MeshRenderer>(true).OrderBy(r=>r.bounds.center.x).ToArray();
            Assert.That(letters.Select(r=>r.name),Is.EqualTo(new[]{"Character_铁","Character_拳","Character_胜","Character_利"}));
            var wall=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true))
                .Single(r=>r.name=="Wall_铁拳胜利_Backboard");
            Assert.That(wall.sharedMaterial.name,Is.EqualTo("RangeIronFistVictoryWall"));
            Assert.That(wall.sharedMaterial.color.g,Is.GreaterThan(.6f));
            var eye=UnityEngine.Object.FindObjectsOfType<SceneTestId>(true).Single(t=>t.Id==prefix+".Camera.NoVR").GetComponent<Camera>();
            Physics.SyncTransforms();
            for(int i=0;i<4;i++)
            {
                Assert.That(letters[i].enabled&&letters[i].gameObject.activeInHierarchy,Is.True);
                Assert.That(letters[i].sharedMaterial.mainTexture.name,Is.EqualTo("ZeroingSloganIronFistVictory"));
                Assert.That(letters[i].bounds.size.y,Is.EqualTo(letters[0].bounds.size.y).Within(.001f));
                Assert.That(letters[i].bounds.center.y,Is.EqualTo(letters[0].bounds.center.y).Within(.001f));
                Assert.That(letters[i].bounds.center.z,Is.LessThan(wall.bounds.min.z));
                // Exact UVs are verified in EditMode before Unity creates non-readable static batches.
            }
            RenderAndCheck(eye,letters,sceneName+"-desktop");
            var reference=UnityEngine.Object.FindObjectsOfType<SceneTestId>(true).Single(t=>t.Id==prefix+".FiringStation.ProneHeadReference").transform;
            var verificationEye=new GameObject("SloganStereoVerification");var camera=verificationEye.AddComponent<Camera>();camera.CopyFrom(eye);
            camera.stereoTargetEye=StereoTargetEyeMask.None;
            try
            {
                foreach(var offset in new[]{-.032f,.032f})
                {
                    camera.transform.SetPositionAndRotation(reference.position+reference.right*offset,reference.rotation);
                    RenderAndCheck(camera,letters,sceneName+(offset<0?"-left-eye":"-right-eye"));
                }
            }
            finally { UnityEngine.Object.Destroy(verificationEye); }
        }
        static void RenderAndCheck(Camera eye, MeshRenderer[] letters,string label)
        {
            const int width=1920,height=1080;
            var previousTarget=eye.targetTexture;var previousActive=RenderTexture.active;var previousMask=eye.cullingMask;
            var target=RenderTexture.GetTemporary(width,height,24);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                eye.cullingMask&=~(1<<5);eye.targetTexture=target;eye.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();var pixels=image.GetPixels32();
                var trace="eye="+eye.transform.position+"\n";
                foreach(var letter in letters)
                {
                    Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(eye),letter.bounds),Is.True);
                    var delta=letter.bounds.center-eye.transform.position;
                    Assert.That(Physics.Raycast(eye.transform.position,delta.normalized,delta.magnitude-.03f,~(1<<2),QueryTriggerInteraction.Ignore),Is.False,"Wall text is occluded: "+letter.name);
                    var min=eye.WorldToViewportPoint(letter.bounds.min);var max=eye.WorldToViewportPoint(letter.bounds.max);
                    int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(min.x,max.x)*width),0,width-1),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(min.x,max.x)*width),0,width-1);
                    int y0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(min.y,max.y)*height),0,height-1),y1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(min.y,max.y)*height),0,height-1);
                    int red=0;
                    for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                    {var c=pixels[y*width+x];if(c.r>60&&c.r>c.g*1.8f&&c.r>c.b*1.8f)red++;}
                    Assert.That(red,Is.GreaterThan(120),"Rendered letter has insufficient visible pixels: "+letter.name);
                    trace+=letter.name+" redPixels="+red+" bounds="+letter.bounds+"\n";
                }
                const string directory="Logs/grenade-slogan-repair/evidence";Directory.CreateDirectory(directory);
                File.WriteAllBytes(directory+"/"+label+".png",image.EncodeToPNG());File.WriteAllText(directory+"/"+label+".txt",trace);
            }
            finally {eye.targetTexture=previousTarget;eye.cullingMask=previousMask;RenderTexture.active=previousActive;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(image);}
        }
    }

    // BDD27 heavy blast / task015: imported sound, real AudioSource and full tail.
    public sealed class GrenadeExplosionAudioPlayModeTests
    {
        GameObject explosion;
        AudioListener listener;
        [UnitySetUp] public IEnumerator SetUpAudioScene()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            foreach(var fixture in UnityEngine.Object.FindObjectsOfType<CombatSceneFixture>())fixture.enabled=false;
            var mode=UnityEngine.Object.FindObjectOfType<ZeroingRangeXRModeController>();mode.SetVrModeForTests(false);
            yield return null;yield return null;
            listener=UnityEngine.Object.FindObjectsOfType<AudioListener>(true).Single(a=>a.isActiveAndEnabled);
        }
        [UnityTest] public IEnumerator ImportedHeavyBlastPlaysBeyondVisualLifetimeAndFinishes()
        {
            explosion=new GameObject("HeavyBlastVerification");explosion.transform.position=listener.transform.position+listener.transform.forward*18f;
            explosion.AddComponent<CombatExplosionVfx>();
            var source=explosion.GetComponent<AudioSource>();
            Assert.That(source.clip,Is.Not.Null);Assert.That(source.clip.name,Is.EqualTo("GrenadeExplosion_Heavy"));
            Assert.That(source.clip.length,Is.GreaterThan(2f));Assert.That(source.volume,Is.EqualTo(1f));
            Assert.That(source.spatialBlend,Is.EqualTo(1f));Assert.That(source.rolloffMode,Is.EqualTo(AudioRolloffMode.Linear));
            Assert.That(source.minDistance,Is.GreaterThanOrEqualTo(12f));Assert.That(source.maxDistance,Is.GreaterThanOrEqualTo(100f));
            var samples=new float[source.clip.samples*source.clip.channels];Assert.That(source.clip.GetData(samples,0),Is.True);
            Assert.That(samples.Max(v=>Mathf.Abs(v)),Is.InRange(.97f,.99f));
            var attack=samples.Take((int)(source.clip.frequency*.2f)).ToArray();
            Assert.That(Mathf.Sqrt(attack.Average(v=>v*v)),Is.GreaterThan(.25f),"The blast must have a strong audible attack, not low-level procedural noise");
            yield return new WaitForSecondsRealtime(.1f);Assert.That(source.isPlaying,Is.True);
            var output=new float[1024];var ownOutput=new float[1024];float outputRms=0,sourceRms=0;
            var until=Time.realtimeSinceStartup+.4f;
            while(Time.realtimeSinceStartup<until)
            {
                AudioListener.GetOutputData(output,0);source.GetOutputData(ownOutput,0);
                outputRms=Mathf.Max(outputRms,Mathf.Sqrt(output.Average(v=>v*v)));
                sourceRms=Mathf.Max(sourceRms,Mathf.Sqrt(ownOutput.Average(v=>v*v)));
                yield return null;
            }
            File.WriteAllText("Logs/grenade-slogan-repair/audio-playback.txt","clip="+source.clip.name+" length="+source.clip.length+
                " listenerDistance="+Vector3.Distance(listener.transform.position,explosion.transform.position)+" outputRms="+outputRms+
                " sourceRms="+sourceRms+" sourceTime="+source.time+" virtual="+source.isVirtual+" dspTime="+AudioSettings.dspTime+
                " listenerVolume="+AudioListener.volume+" paused="+AudioListener.pause+
                " listeners="+string.Join(";",UnityEngine.Object.FindObjectsOfType<AudioListener>().Select(l=>l.name+":"+l.enabled))+
                " volume="+source.volume+" minDistance="+source.minDistance+" maxDistance="+source.maxDistance+"\n");
            Assert.That(UnityEngine.Object.FindObjectsOfType<AudioListener>().Count(l=>l.isActiveAndEnabled),Is.EqualTo(1),"Only the production scene's actual listener may contribute to the output test");
            Assert.That(outputRms,Is.GreaterThan(.01f),"The real Unity listener must receive a strong blast at normal 18m throw distance");
            yield return new WaitForSeconds(.8f);
            Assert.That(explosion,Is.Not.Null,"Do not delete the audio when the 1.2s visual ends");Assert.That(source.isPlaying,Is.True);
            yield return new WaitForSeconds(source.clip.length);
            Assert.That(explosion==null,Is.True,"Finished blast objects must be reclaimed");
        }
        [UnityTest] public IEnumerator DisablingExplosionStopsItsSoundImmediately()
        {
            explosion=new GameObject("CancelledBlastVerification");explosion.transform.position=listener.transform.position+listener.transform.forward*18f;
            explosion.AddComponent<CombatExplosionVfx>();
            var source=explosion.GetComponent<AudioSource>();yield return new WaitForSeconds(.1f);
            Assert.That(source.isPlaying,Is.True);explosion.SetActive(false);Assert.That(source.isPlaying,Is.False);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(explosion!=null)UnityEngine.Object.Destroy(explosion);
            yield return null;
        }
    }
}
