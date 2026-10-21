using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    public static class ApprovedForwardGripAuthoring
    {
        public static void Configure(VRControllerHandVisual hand)
        {
            var right=hand.HandSide==VirtualHandSide.Right;
            var so=new SerializedObject(hand);
            // The knuckle row must run down the pistol grip on the right, along the handguard on the left.
            so.FindProperty("gripPositionOffset").vector3Value=right?new Vector3(.049f,-.010f,-.032f):new Vector3(-.0478f,-.0516f,-.0082f);
            so.FindProperty("gripRotationOffset").vector3Value=right?new Vector3(-30,-15,-90):new Vector3(-20,65,180);
            so.FindProperty("useCustomFingerPose").boolValue=true;
            WriteContactPose(hand,so,right);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Authoring only: aim each anatomical segment around the actual contact shape.
        // The output remains the existing serialized pose; no runtime IK is introduced.
        static void WriteContactPose(VRControllerHandVisual hand,SerializedObject so,bool right)
        {
            var rootRotation=hand.transform.localRotation;
            hand.transform.rotation=Quaternion.Euler(so.FindProperty("gripRotationOffset").vector3Value);
            try
            {
                foreach(var finger in new[]{"thumb","index","middle","ring","little"})
                {
                    var prefix=(right?"R_":"L_")+char.ToUpper(finger[0])+finger.Substring(1);
                    var stages=new[]{"Proximal","Intermediate","Distal"};
                    var nodes=stages.Select(stage=>hand.ModelRoot.GetComponentsInChildren<Transform>(true).Single(t=>t.name==prefix+stage)).ToArray();
                    var originals=nodes.Select(t=>t.localRotation).ToArray();
                    var tipDirection=nodes[2].InverseTransformDirection(nodes[2].position-nodes[1].position).normalized;
                    var pose=so.FindProperty("customFingerPose").FindPropertyRelative(finger);
                    pose.FindPropertyRelative("metacarpal").vector3Value=Vector3.zero;
                    Vector3[] directions;
                    if(finger=="thumb") directions=right
                        ?new[]{new Vector3(-.95f,.2f,.24f),new Vector3(-.85f,.25f,.46f),new Vector3(.1f,.2f,.97f)}
                        :new[]{new Vector3(.8f,.5f,.2f),new Vector3(.5f,.2f,.84f),new Vector3(.1f,-.1f,.99f)};
                    // Index hooks inward through the guard, placing the distal pad against the trigger.
                    else if(right&&finger=="index")directions=new[]{new Vector3(-.25f,.03f,.967f),new Vector3(-.7f,.08f,.71f),new Vector3(-.6f,-.05f,.798f)};
                    else if(right&&finger=="middle")directions=new[]{new Vector3(-.65f,-.2f,.733f),new Vector3(-.97f,0,-.24f),new Vector3(-.17f,0,-.985f)};
                    else if(right&&finger=="ring")directions=new[]{new Vector3(-.83f,0,.56f),new Vector3(-.94f,0,-.34f),new Vector3(-.25f,0,-.968f)};
                    else if(right)directions=new[]{new Vector3(-.9f,0,.436f),new Vector3(-.93f,0,-.37f),new Vector3(-.3f,0,-.954f)};
                    else if(finger=="index")directions=new[]{new Vector3(.985f,.174f,0),Vector3.up,Vector3.up};
                    else directions=new[]{new Vector3(.866f,.5f,0),new Vector3(.087f,.996f,0),new Vector3(-.819f,.574f,0)};
                    for(int i=0;i<3;i++)
                    {
                        var current=i<2?nodes[i+1].position-nodes[i].position:nodes[i].TransformDirection(tipDirection);
                        nodes[i].rotation=Quaternion.FromToRotation(current,directions[i])*nodes[i].rotation;
                        var delta=Quaternion.Inverse(originals[i])*nodes[i].localRotation;
                        var e=delta.eulerAngles;
                        pose.FindPropertyRelative(stages[i].ToLowerInvariant()).vector3Value=new Vector3(Mathf.DeltaAngle(0,e.x),Mathf.DeltaAngle(0,e.y),Mathf.DeltaAngle(0,e.z));
                    }
                    for(int i=0;i<3;i++)nodes[i].localRotation=originals[i];
                }
            }
            finally{hand.transform.localRotation=rootRotation;}
        }

        public static void SaveAssetsAndScenes()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play before authoring poses.");
            foreach(var side in new[]{"Left","Right"})
            {
                var path="Assets/Resources/Combat/Hands/VirtualHand_"+side+".prefab";
                var p=PrefabUtility.LoadPrefabContents(path);
                try{Configure(p.GetComponent<VRControllerHandVisual>());PrefabUtility.SaveAsPrefabAsset(p,path);}
                finally{PrefabUtility.UnloadPrefabContents(p);}
            }
            foreach(var prefix in new[]{"ZeroingRange","MovingTargetRange"})
            {
                var path="Assets/Scenes/"+prefix+"Scene.unity";
                var scene=SceneManager.GetSceneByPath(path);var owned=!scene.isLoaded;
                if(owned)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try{ModularGlovedArmsAuthoring.PatchSceneHands(scene);EditorSceneManager.SaveScene(scene);}
                finally{if(owned)EditorSceneManager.CloseScene(scene,true);}
            }
            AssetDatabase.SaveAssets();
        }

        public static void CreatePreview(bool useSavedPose=false)
        {
            var previous=SceneManager.GetSceneByName("ForwardGripPreview");
            if(previous.isLoaded)EditorSceneManager.CloseScene(previous,true);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            scene.name="ForwardGripPreview";
            var gun=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab"),scene);
            gun.name="ForwardGripRifle";
            foreach(var side in new[]{"Left","Right"})
            {
                var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Combat/Hands/VirtualHand_"+side+".prefab"),scene);
                var h=g.GetComponent<VRControllerHandVisual>();if(!useSavedPose)Configure(h);
                var anchor=gun.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Grip_training-rifle_"+(side=="Left"?"FrontHand":"RearHand"));
                new SerializedObject(h).Update();
                var so=new SerializedObject(h);so.FindProperty("gripAnchor").objectReferenceValue=anchor;so.ApplyModifiedPropertiesWithoutUndo();
                h.BeginGripPosePreview();
                // Edit-mode skin rendering can retain the bind pose. Bake only the preview snapshot.
                foreach(var skin in g.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);
                    var baked=new GameObject("BakedPose",typeof(MeshFilter),typeof(MeshRenderer));
                    baked.transform.SetParent(skin.transform,false);baked.GetComponent<MeshFilter>().sharedMesh=mesh;
                    baked.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
                }
            }
            foreach(var r in scene.GetRootGameObjects())foreach(var t in r.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            var c=new GameObject("ForwardGripCamera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(c.gameObject,scene);
            c.cullingMask=1<<30;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.18f,.22f,.27f);
            c.nearClipPlane=.01f;c.fieldOfView=60;c.transform.position=new Vector3(-.10f,.095f,-.53f);c.transform.LookAt(new Vector3(0,0,4));
            var light=new GameObject("ForwardGripLight").AddComponent<Light>();SceneManager.MoveGameObjectToScene(light.gameObject,scene);
            light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(40,-35,0);
        }
    }
}
