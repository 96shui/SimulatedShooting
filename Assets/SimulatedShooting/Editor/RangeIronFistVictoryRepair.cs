using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    // Incremental authoring fix: preserve the scene, firing station and route; repair only the sign.
    public static class RangeIronFistVictoryRepair
    {
        const string TexturePath="Assets/SimulatedShooting/Art/Environment/MovingTargetRange/ZeroingSloganIronFistVictory.png";
        const string TextMaterialPath="Assets/SimulatedShooting/Art/Materials/ZeroingSloganIronFistVictory.mat";
        const string WallMaterialPath="Assets/SimulatedShooting/Art/Materials/RangeIronFistVictoryWall.mat";

        public static void RepairExistingScenes()
        {
            var original=SceneManager.GetActiveScene();
            foreach(var prefix in new[]{"ZeroingRange","MovingTargetRange"})
            {
                var scene=SceneManager.GetSceneByPath("Assets/Scenes/"+prefix+"Scene.unity");var owned=!scene.isLoaded;
                if(owned)scene=EditorSceneManager.OpenScene("Assets/Scenes/"+prefix+"Scene.unity",OpenSceneMode.Additive);
                try {PatchScene(scene,prefix);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                finally {if(owned)EditorSceneManager.CloseScene(scene,true);}
            }
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            AssetDatabase.SaveAssets();
        }

        public static void PatchScene(UnityEngine.SceneManagement.Scene scene,string prefix)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if(texture==null)throw new InvalidOperationException("Missing 铁拳胜利 texture");
            var material=AssetDatabase.LoadAssetAtPath<Material>(TextMaterialPath);
            if(material==null)
            {
                var shader=Shader.Find("SimulatedShooting/World Space Text Occluded");
                if(shader==null)throw new InvalidOperationException("Missing depth-tested world text shader");
                material=new Material(shader){name="ZeroingSloganIronFistVictory"};AssetDatabase.CreateAsset(material,TextMaterialPath);
            }
            material.mainTexture=texture;material.color=new Color(.82f,.04f,.02f,1);
            material.SetFloat("_ZTest",(float)CompareFunction.LessEqual);EditorUtility.SetDirty(material);
            var wallMaterial=AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
            if(wallMaterial==null)
            {
                var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Materials/MovingTargetRidgeWall.mat");
                if(template==null)throw new InvalidOperationException("Missing concrete wall template");
                wallMaterial=new Material(template){name="RangeIronFistVictoryWall"};AssetDatabase.CreateAsset(wallMaterial,WallMaterialPath);
            }
            wallMaterial.color=new Color(.94f,.91f,.82f,1);EditorUtility.SetDirty(wallMaterial);
            var nodes=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var root=nodes.Single(t=>t.name==prefix);
            var group=nodes.Select(t=>t.GetComponent<SceneTestId>()).FirstOrDefault(t=>t!=null&&t.Id==prefix+".Visual.Slogan.DataWins");
            if(group==null)
            {
                var node=new GameObject("Slogan_铁拳胜利");node.transform.SetParent(root.Find("Environment")??root,false);
                node.transform.position=new Vector3(0,9.46f,110.7f);
                group=node.AddComponent<SceneTestId>();group.Id=prefix+".Visual.Slogan.DataWins";
                for(int i=0;i<4;i++)
                {
                    var glyph=new GameObject("Character_"+"铁拳胜利"[i],typeof(MeshFilter),typeof(MeshRenderer));
                    glyph.transform.SetParent(group.transform,false);glyph.transform.localPosition=new Vector3((i-1.5f)*11f,0,0);
                    var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-3.5f,-3.5f,0),new Vector3(3.5f,-3.5f,0),new Vector3(-3.5f,3.5f,0),new Vector3(3.5f,3.5f,0)};
                    mesh.triangles=new[]{0,2,1,1,2,3};mesh.colors=Enumerable.Repeat(Color.white,4).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
                    glyph.GetComponent<MeshFilter>().sharedMesh=mesh;
                }
            }
            group.name="Slogan_铁拳胜利";group.gameObject.SetActive(true);
            var letters=group.GetComponentsInChildren<MeshRenderer>(true).OrderBy(r=>r.bounds.center.x).ToArray();
            if(letters.Length!=4)throw new InvalidOperationException("Expected four sign characters in "+scene.name);
            for(int i=0;i<4;i++)
            {
                letters[i].name="Character_"+"铁拳胜利"[i];letters[i].gameObject.layer=6;letters[i].gameObject.SetActive(true);
                var filter=letters[i].GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
                mesh.name=letters[i].name+"_Mesh";
                mesh.uv=new[]{new Vector2(i*.25f,0),new Vector2((i+1)*.25f,0),new Vector2(i*.25f,1),new Vector2((i+1)*.25f,1)};
                filter.sharedMesh=mesh;letters[i].sharedMaterial=material;letters[i].enabled=true;letters[i].forceRenderingOff=false;
                letters[i].shadowCastingMode=ShadowCastingMode.Off;letters[i].receiveShadows=false;
            }
            var wall=nodes.FirstOrDefault(t=>t.name=="Wall_铁拳胜利_Backboard"||t.name=="Billboard_数据致胜_Backboard");
            if(wall==null)
            {
                var bounds=letters[0].bounds;foreach(var letter in letters.Skip(1))bounds.Encapsulate(letter.bounds);
                var node=GameObject.CreatePrimitive(PrimitiveType.Cube);node.transform.SetParent(group.transform.parent,false);
                node.transform.position=bounds.center+Vector3.forward*.2f;node.transform.localScale=new Vector3(bounds.size.x+5,bounds.size.y+3.8f,.3f);wall=node.transform;
            }
            wall.name="Wall_铁拳胜利_Backboard";wall.GetComponent<MeshRenderer>().sharedMaterial=wallMaterial;wall.gameObject.SetActive(true);
            foreach(var post in nodes.Where(n=>n.name.StartsWith("Billboard_数据致胜_Post_",StringComparison.Ordinal)))
                post.name=post.name.Replace("Billboard_数据致胜","Wall_铁拳胜利");
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
