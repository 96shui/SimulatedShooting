using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Tests.EditMode
{
    // BDD00 / P1 task016 / P2 task006: inspect saved geometry before runtime static batching.
    public sealed class RangeIronFistAuthoringTests
    {
        [TestCase("ZeroingRange")] [TestCase("MovingTargetRange")]
        public void SavedWallCharactersUseTheFourCompleteCorrectAtlasSlices(string prefix)
        {
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/"+prefix+"Scene.unity");var owned=!scene.isLoaded;
            if(owned)scene=EditorSceneManager.OpenScene("Assets/Scenes/"+prefix+"Scene.unity",OpenSceneMode.Additive);
            try
            {
                var group=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SceneTestId>(true))
                    .Single(t=>t.Id==prefix+".Visual.Slogan.DataWins");
                Assert.That(group.name,Is.EqualTo("Slogan_铁拳胜利"));
                var letters=group.GetComponentsInChildren<MeshRenderer>(true).OrderBy(r=>r.transform.position.x).ToArray();
                Assert.That(letters.Select(l=>l.name),Is.EqualTo(new[]{"Character_铁","Character_拳","Character_胜","Character_利"}));
                for(int i=0;i<4;i++)
                {
                    var uv=letters[i].GetComponent<MeshFilter>().sharedMesh.uv;
                    Assert.That(uv.Min(v=>v.x),Is.EqualTo(i*.25f).Within(.0001f));Assert.That(uv.Max(v=>v.x),Is.EqualTo((i+1)*.25f).Within(.0001f));
                    Assert.That(uv.Min(v=>v.y),Is.Zero);Assert.That(uv.Max(v=>v.y),Is.EqualTo(1f));
                    Assert.That(letters[i].sharedMaterial.mainTexture.name,Is.EqualTo("ZeroingSloganIronFistVictory"));
                }
            }
            finally {if(owned)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
