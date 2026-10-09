using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class EnemyDeathAudioTests
    {
        // BDD23 task019 death cry: production prefab, lifecycle, overlapping actors and spatial sound.
        [UnityTest]
        public IEnumerator DeathCryPlaysOncePerDeathStopsOnReviveAndKeepsIndependentVoices()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            yield return null;
            var fixture=Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled=false;
            var actors=fixture.Actors.Where(a=>a.DeathClip!=null).Take(2).ToArray();
            Assert.That(actors.Length,Is.EqualTo(2));
            foreach(var actor in actors)
            {
                actor.PlayHit("first-hit");
                Assert.That(actor.DeathAudioFeedbackCount,Is.Zero);
                actor.ApplyDead(true);
                var voice=actor.DeathVoiceSource;
                Assert.That(voice,Is.Not.Null);Assert.That(voice.isPlaying,Is.True);
                Assert.That(voice.clip,Is.SameAs(actor.DeathClip));
                Assert.That(voice.spatialBlend,Is.EqualTo(1));Assert.That(voice.loop,Is.False);
                Assert.That(voice.playOnAwake,Is.False);Assert.That(voice.dopplerLevel,Is.Zero);
                Assert.That(voice.transform.IsChildOf(actor.SoldierAnimation.Chest),Is.True);
                Assert.That(voice,Is.Not.SameAs(actor.Audio),"Voice cannot replace the rifle/impact audio source");
                actor.ApplyDead(true);
                Assert.That(actor.DeathAudioFeedbackCount,Is.EqualTo(1));
            }
            Assert.That(actors[0].DeathVoiceSource,Is.Not.SameAs(actors[1].DeathVoiceSource));
            actors[0].ApplyDead(false);
            Assert.That(actors[0].DeathVoiceSource.isPlaying,Is.False,"Reset must stop the old scream");
            Assert.That(actors[1].DeathVoiceSource.isPlaying,Is.True,"Resetting one actor cannot stop another voice");
            actors[0].ApplyDead(true);
            Assert.That(actors[0].DeathAudioFeedbackCount,Is.EqualTo(2));
            Assert.That(actors[0].DeathVoiceSource.isPlaying,Is.True);
            yield return new WaitForSeconds(actors[0].DeathClip.length+.15f);
            Assert.That(actors.All(a=>!a.DeathVoiceSource.isPlaying),Is.True,"Cry ends rather than looping");
            actors[0].ApplyDead(false);actors[0].ApplyDead(true);
            actors[0].gameObject.SetActive(false);
            Assert.That(actors[0].DeathVoiceSource.isPlaying,Is.False,"Exit/deactivation must stop the voice");
        }
    }
}
