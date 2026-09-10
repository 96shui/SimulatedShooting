using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace SimulatedShooting.Editor
{
    public static class CombatSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/CombatScene.unity";
        const string Art = "Assets/SimulatedShooting/Art/Combat";
        const string Prefabs = "Assets/SimulatedShooting/Prefabs/Combat";
        static Material earth, concrete, wood, sand, dark, metal, accent;
        static Transform geometry;
        static CombatSceneBindings bindings;
        static readonly List<CombatScenePoint> points = new List<CombatScenePoint>();
        static readonly List<CombatDoorView> doors = new List<CombatDoorView>();

        [MenuItem("Tools/Simulated Shooting/Scene 3/Create Combined Scene (First Time Only)")]
        public static void Build()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("CombatScene already exists. Preserve saved edits; use Rebuild Navigation or Validate.");
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            bindings = new GameObject("CombatScene").AddComponent<CombatSceneBindings>();
            Id(bindings.gameObject, "CombatScene.Root");
            geometry = Child("Environment", bindings.transform, Vector3.zero);
            bindings.GeometryRoot = geometry;
            points.Clear(); doors.Clear();
            earth = Material("Earth", new Color(.43f,.36f,.23f), "Assets/SimulatedShooting/Art/Textures/BrownMud/brown_mud_2k.blend/textures/brown_mud_diff_2k.jpg");
            concrete = Material("Concrete", new Color(.66f,.65f,.58f), "Assets/SimulatedShooting/Art/Textures/ConcreteFloor01/concrete_floor_01_diff_2k.jpg");
            wood = Material("Timber", new Color(.25f,.18f,.105f));
            sand = Material("Sandbags", new Color(.49f,.46f,.31f));
            dark = Material("Asphalt", new Color(.12f,.14f,.13f));
            metal = Material("Steel", new Color(.19f,.23f,.22f));
            accent = Material("Markings", new Color(.76f,.65f,.36f));
            BuildTrench();
            BuildTown();
            SetupAnchors();
            bindings.Points = points.ToArray(); bindings.Doors = doors.ToArray();
            bindings.Maps = new[]
            {
                Plan("trench-a", new Vector2(-4,-12), new Vector2(30,46), 0),
                Plan("urban-a", new Vector2(-6,42), new Vector2(54,92), 0),
                Plan("urban-1f", new Vector2(4,62), new Vector2(42,90), 1),
                Plan("urban-2f", new Vector2(4,62), new Vector2(42,90), 2),
                Plan("urban-3f", new Vector2(4,62), new Vector2(42,90), 3)
            };
            var clip = HitSound();
            bindings.EnemyPrefab = ActorPrefab("Enemy", new Color(.37f,.30f,.24f), clip);
            bindings.TeammatePrefab = ActorPrefab("Teammate", new Color(.22f,.32f,.23f), clip);
            Bake(bindings);
            SetupInspection();
            SetupLighting();
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            CompletePresentation();
            PolishSavedScene();
            Validate();
            Capture();
            Debug.Log("Scene 3 saved: " + ScenePath);
        }

        static void BuildTrench()
        {
            var cells = new HashSet<Vector2Int>();
            for (int z=-2; z<=4; z++) cells.Add(new Vector2Int(0,z));
            for (int x=0; x<=3; x++) cells.Add(new Vector2Int(x,4));
            for (int z=4; z<=8; z++) cells.Add(new Vector2Int(3,z));
            for (int x=3; x<=6; x++) cells.Add(new Vector2Int(x,8));
            for (int z=8; z<=11; z++) cells.Add(new Vector2Int(6,z));
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var cell in cells)
            {
                var c = new Vector3(cell.x * 4, 0, cell.y * 4);
                Box("TrenchFloor", c + Vector3.down * .2f, new Vector3(4,.4f,4), earth);
                Box("Duckboard", c + new Vector3(0,.015f,0), new Vector3(1.1f,.03f,3.85f), wood);
                foreach (var d in dirs)
                {
                    if (cells.Contains(cell+d) || (cell == new Vector2Int(6,11) && d == Vector2Int.up)) continue;
                    var normal = new Vector3(d.x,0,d.y);
                    var w = c + normal * 2.2f + Vector3.up * 1.1f;
                    var along = new Vector3(d.y,0,d.x);
                    Box("EarthRevetment", w, new Vector3(d.x == 0 ? 4.4f : .65f,2.2f,d.x == 0 ? .65f : 4.4f), earth);
                    for (int i=-1; i<=1; i++)
                    {
                        Box("TimberPost", w - normal*.4f + along * i*1.65f, new Vector3(.14f,2.1f,.14f), wood);
                        for (int row=0; row<2; row++)
                        {
                            var b = Box("Sandbag", w + Vector3.up*(1.2f + row*.23f) + along*(i*1.35f + row*.15f), new Vector3(1.28f,.26f,.65f), sand);
                            if (d.x != 0) b.localRotation = Quaternion.Euler(0,90,0);
                        }
                    }
                }
            }
            var route = new[] { new Vector3(0,0,-4), new Vector3(0,0,12), new Vector3(0,0,16), new Vector3(12,0,16), new Vector3(12,0,28), new Vector3(12,0,32), new Vector3(24,0,32), new Vector3(24,0,44) };
            for (int i=0;i<route.Length;i++) Point("trench-node-"+(i+1), "trench-a", CombatPointKind.SearchNode, route[i]);
            for (int i=0;i<5;i++)
            {
                var location = route[i+2] + new Vector3(.8f,0,.8f);
                Point("trench-spawn-"+(i+1), "trench-a", CombatPointKind.EnemySpawn, location);
                Point("trench-estimate-"+(i+1), "trench-a", CombatPointKind.EstimateArea, route[i+2]);
            }
            foreach (int i in new[] {2,3,5,6}) Point("trench-corner-"+i,"trench-a",CombatPointKind.Corner,route[i]);
            Sign("TRENCH / 01", new Vector3(0,2.8f,-9.5f), 180);
            Sign("TOWN  >", new Vector3(24,2.7f,42), 180);
        }

        static void BuildTown()
        {
            Box("Street",new Vector3(24,-.2f,55),new Vector3(60,.4f,22),dark);
            Box("Sidewalk",new Vector3(24,-.05f,63),new Vector3(60,.1f,3),concrete);
            for(int x=-2;x<52;x+=6) Box("RoadStripe",new Vector3(x,.01f,53),new Vector3(3,.02f,.14f),accent);
            // One enterable building. Five rooms in total: 2 + 2 + 1.
            for(int f=0; f<3; f++)
            {
                float y=f*3.6f;
                Box("Floor_"+(f+1),new Vector3(28,y-.15f,76),new Vector3(24,.3f,24),concrete);
                Box("RearWall",new Vector3(28,y+1.65f,88),new Vector3(24,3.3f,.35f),concrete);
                Box("EastWall",new Vector3(40,y+1.65f,76),new Vector3(.35f,3.3f,24),concrete);
                Box("FrontWall",new Vector3(31,y+1.65f,64),new Vector3(18,3.3f,.35f),concrete);
                Box("FrontHeader",new Vector3(19,y+2.95f,64),new Vector3(6,.7f,.35f),concrete);
                Box("WestWall",new Vector3(16,y+1.65f,74),new Vector3(.35f,3.3f,16),concrete);
                Box("WestEndWall",new Vector3(16,y+1.65f,87),new Vector3(.35f,3.3f,2),concrete);
                int count=f<2?2:1;
                for(int room=0;room<count;room++)
                {
                    float z=f<2?70+room*12:76;
                    float length=f<2?12:24;
                    string roomId="room-"+(f+1)+(room+1);
                    float start=z-length/2;
                    Box("DoorWallLower",new Vector3(22,y+1.65f,start+1.5f),new Vector3(.3f,3.3f,3),concrete);
                    Box("DoorWallUpper",new Vector3(22,y+1.65f,start+5+(length-5)/2),new Vector3(.3f,3.3f,length-5),concrete);
                    Box("DoorLintel",new Vector3(22,y+2.9f,start+4),new Vector3(.3f,.8f,2),concrete);
                    if(room==1) Box("RoomDivider",new Vector3(31,y+1.65f,start),new Vector3(18,3.3f,.3f),concrete);
                    var door=MakeDoor(roomId,new Vector3(22,y,start+3));
                    var point=Point(roomId,"urban-a",CombatPointKind.Room,new Vector3(25,y,start+4),"floor-"+(f+1),roomId,new Vector3(7,2.7f,4));
                    point.Door=door;
                    Point("building-spawn-"+(f*2+room+1),"urban-a",CombatPointKind.EnemySpawn,new Vector3(30,y,z),"floor-"+(f+1),roomId);
                    Box("RoomCover",new Vector3(34,y+.6f,z+1),new Vector3(2,1.2f,.8f),wood);
                    Sign(roomId.ToUpper(),new Vector3(21.75f,y+2.45f,start+4),90);
                }
                Point("floor-"+(f+1),"urban-a",CombatPointKind.Floor,new Vector3(19,y,84),"floor-"+(f+1));
                // Floor edge rail on external landing, leaving stair access open.
                Box("Landing",new Vector3(13,y-.15f,84),new Vector3(6,.3f,4),concrete);
                if(f>0) Box("LandingRail",new Vector3(10,y+.55f,85),new Vector3(.12f,1.1f,2),metal);
                var light=Child("InteriorLight",geometry,new Vector3(28,y+3,76)).gameObject.AddComponent<Light>();
                light.type=LightType.Point; light.range=18; light.intensity=1.6f; light.shadows=LightShadows.None;
                Sign("0"+(f+1),new Vector3(19,y+2.5f,87.7f),180);
            }
            Point("building-spawn-6","urban-a",CombatPointKind.EnemySpawn,new Vector3(19,7.2f,72),"floor-3");
            // Broad exterior switchback stair, continuous collider underneath visible treads.
            Box("StairBase",new Vector3(10,-.15f,74),new Vector3(12,.3f,24),concrete);
            Stairs(new Vector3(13,0,66),new Vector3(13,3.6f,82));
            Box("SwitchbackLanding",new Vector3(9.5f,3.45f,84),new Vector3(13,.3f,4),concrete);
            Stairs(new Vector3(7,3.6f,82),new Vector3(7,7.2f,66));
            Box("TopLanding",new Vector3(10,7.05f,64),new Vector3(14,.3f,4),concrete);
            Box("TopWalkway",new Vector3(13,7.05f,74),new Vector3(3,.3f,20),concrete);
            for(int i=0;i<2;i++) Point("street-spawn-"+(i+1),"urban-a",CombatPointKind.EnemySpawn,new Vector3(6+i*32,0,57));
            Point("building-entrance","urban-a",CombatPointKind.Entrance,new Vector3(19,0,64),"floor-1",null,new Vector3(5,2.7f,5));
            Point("urban-route-street","urban-a",CombatPointKind.Route,new Vector3(24,0,53));
            Point("urban-route-stair-base","urban-a",CombatPointKind.Route,new Vector3(13,0,66));
            Point("urban-route-switchback","urban-a",CombatPointKind.Route,new Vector3(7,3.6f,84));
            Point("urban-route-top","urban-a",CombatPointKind.Route,new Vector3(7,7.2f,64));
            // Street limits and low cover keep the playable footprint compact.
            Box("WestBoundary",new Vector3(-6,1.3f,55),new Vector3(.5f,2.6f,22),concrete);
            Box("EastBoundary",new Vector3(54,1.3f,55),new Vector3(.5f,2.6f,22),concrete);
            for(int i=0;i<4;i++) Box("StreetBarricade",new Vector3(i*14, .65f,47),new Vector3(3,1.3f,.7f),concrete);
            for(int x=24;x<39;x+=5)
                for(int f=0;f<3;f++)
                {
                    Box("WindowRecess",new Vector3(x,f*3.6f+1.85f,63.79f),new Vector3(2,1.5f,.08f),dark);
                    Box("WindowSill",new Vector3(x,f*3.6f+1.03f,63.65f),new Vector3(2.3f,.16f,.35f),concrete);
                }
            Sign("TRAINING BLOCK / 03",new Vector3(30,10.6f,63.7f),180);
        }

        static void Stairs(Vector3 from,Vector3 to)
        {
            var delta=to-from;
            float horizontal=Mathf.Abs(delta.z);
            var ramp=Box("StairRamp",(from+to)*.5f-Vector3.up*.15f,new Vector3(3,.3f,delta.magnitude),concrete);
            ramp.localRotation=Quaternion.Euler(-Mathf.Atan2(delta.y,delta.z)*Mathf.Rad2Deg,0,0);
            // Rotation for the return flight must preserve an upward facing top surface.
            if(delta.z<0) ramp.localRotation=Quaternion.Euler(Mathf.Atan2(delta.y,horizontal)*Mathf.Rad2Deg,0,0);
            ramp.GetComponent<Renderer>().enabled=false;
            for(int i=0;i<24;i++)
            {
                var p=Vector3.Lerp(from,to,(i+.5f)/24f);
                var step=Box("StairTread",p-Vector3.up*.075f,new Vector3(3,.15f,horizontal/24+.02f),concrete);
                UnityEngine.Object.DestroyImmediate(step.GetComponent<Collider>());
            }
            for(int side=-1;side<=1;side+=2)
            {
                var rail=Box("StairHandrail",(from+to)*.5f+new Vector3(side*1.5f,1.05f,0),new Vector3(.1f,.1f,delta.magnitude),metal);
                rail.rotation=ramp.rotation;
                for(int i=0;i<=8;i++) Box("RailPost",Vector3.Lerp(from,to,i/8f)+new Vector3(side*1.5f,.5f,0),new Vector3(.08f,1,.08f),metal);
            }
        }

        static CombatDoorView MakeDoor(string roomId,Vector3 at)
        {
            var root=Child("Door_"+roomId,bindings.transform,at);
            var view=root.gameObject.AddComponent<CombatDoorView>(); view.RoomId=roomId;
            view.Hinge=Child("Hinge",root,at);
            var leaf=Box("DoorLeaf",at+new Vector3(0,1.15f,1),new Vector3(.12f,2.3f,1.95f),wood);
            leaf.SetParent(view.Hinge,true);
            var obstacle=Child("DoorObstacle",root,at+new Vector3(0,1,1)).gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape=NavMeshObstacleShape.Box; obstacle.size=new Vector3(.5f,2,2); obstacle.carving=true;
            view.Obstacle=obstacle;
            Id(root.gameObject,"CombatScene.Door."+roomId);
            doors.Add(view); return view;
        }

        static void SetupAnchors()
        {
            bindings.PlayerSpawn=Anchor("PlayerSpawn",new Vector3(0,0,-4));
            bindings.TrenchEntry=Anchor("TrenchScene",new Vector3(0,0,-4));
            bindings.UrbanEntry=Anchor("UrbanScene",new Vector3(24,0,48));
            bindings.TeammateSpawns=new[] { Anchor("TeammateTwo",new Vector3(0,0,-6)),Anchor("TeammateThree",new Vector3(0,0,-8)) };
            bindings.TeammateSpawns[1].rotation=Quaternion.Euler(0,180,0);
            bindings.WeaponAnchor=Anchor("Weapon",new Vector3(.9f,1,-3));
            bindings.BriefingAnchor=Anchor("Briefing",new Vector3(0,1.5f,-1));
            bindings.HudAnchor=Anchor("Hud",new Vector3(0,1.6f,-2));
            bindings.ResultsAnchor=Anchor("Results",new Vector3(0,1.5f,-1));
            bindings.ProjectionAnchor=Anchor("Projection",new Vector3(-1,1.3f,-3));
            bindings.Drone=Anchor("Drone",new Vector3(-1,2,-2));
            var body=Box("DroneBody",bindings.Drone.position,new Vector3(.45f,.15f,.3f),metal); body.SetParent(bindings.Drone,true);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                var rotor=Box("Rotor",bindings.Drone.position+new Vector3(x*.35f,.03f,z*.3f),new Vector3(.4f,.025f,.08f),dark); rotor.SetParent(bindings.Drone,true);
            }
            var weapon=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab");
            if(weapon!=null)
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(weapon);
                instance.transform.SetParent(bindings.WeaponAnchor,false);
                // Preserve the prefab's binding data; this scene fixture does not start weapon gameplay.
                foreach(var behaviour in instance.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled=false;
                foreach(var rigidbody in instance.GetComponentsInChildren<Rigidbody>()) rigidbody.isKinematic=true;
            }
        }

        static CombatActorView ActorPrefab(string name,Color color,AudioClip hit)
        {
            var root=new GameObject("Actor_"+name);
            var view=root.AddComponent<CombatActorView>();
            var uniform=Material(name+"Uniform",color);
            view.VisualRoot=Child("Visual",root.transform,Vector3.zero);
            Action<string,Vector3,Vector3,Material,PrimitiveType> part=(label,pos,scale,mat,shape)=>
            {
                var obj=GameObject.CreatePrimitive(shape); obj.name=label; obj.transform.SetParent(view.VisualRoot,false);
                obj.transform.localPosition=pos; obj.transform.localScale=scale; obj.GetComponent<Renderer>().sharedMaterial=mat;
                UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            };
            part("Torso",new Vector3(0,1.15f,0),new Vector3(.55f,.65f,.3f),uniform,PrimitiveType.Cube);
            part("Vest",new Vector3(0,1.16f,.07f),new Vector3(.59f,.46f,.31f),metal,PrimitiveType.Cube);
            part("Head",new Vector3(0,1.63f,0),new Vector3(.25f,.3f,.25f),sand,PrimitiveType.Sphere);
            part("Helmet",new Vector3(0,1.77f,0),new Vector3(.34f,.19f,.35f),uniform,PrimitiveType.Sphere);
            foreach(int side in new[]{-1,1})
            {
                part("Leg",new Vector3(side*.16f,.46f,0),new Vector3(.23f,.75f,.25f),uniform,PrimitiveType.Cube);
                part("Boot",new Vector3(side*.16f,.1f,.07f),new Vector3(.25f,.2f,.38f),dark,PrimitiveType.Cube);
                part("Arm",new Vector3(side*.34f,1.18f,.19f),new Vector3(.18f,.22f,.55f),uniform,PrimitiveType.Cube);
            }
            part("Rifle",new Vector3(.15f,1.15f,.52f),new Vector3(.09f,.14f,.85f),dark,PrimitiveType.Cube);
            view.Muzzle=Child("Muzzle",view.VisualRoot,new Vector3(.15f,1.15f,.95f));
            view.PerceptionOrigin=Child("PerceptionOrigin",root.transform,new Vector3(0,1.65f,.3f));
            view.MuzzleFlash=GameObject.CreatePrimitive(PrimitiveType.Sphere); view.MuzzleFlash.name="MuzzleFlash";
            view.MuzzleFlash.transform.SetParent(view.Muzzle,false); view.MuzzleFlash.transform.localScale=new Vector3(.12f,.12f,.25f);
            view.MuzzleFlash.GetComponent<Renderer>().sharedMaterial=accent;
            UnityEngine.Object.DestroyImmediate(view.MuzzleFlash.GetComponent<Collider>()); view.MuzzleFlash.SetActive(false);
            var collider=root.AddComponent<CapsuleCollider>(); collider.center=new Vector3(0,.9f,0); collider.height=1.8f; collider.radius=.3f; view.HitCollider=collider;
            view.Agent=root.AddComponent<NavMeshAgent>(); view.Agent.radius=.3f; view.Agent.height=1.8f; view.Agent.speed=3; view.Agent.stoppingDistance=.15f;
            view.Audio=root.AddComponent<AudioSource>(); view.Audio.spatialBlend=1; view.Audio.playOnAwake=false; view.Audio.maxDistance=25; view.HitClip=hit;
            var saved=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/"+root.name+".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<CombatActorView>();
        }

        static void SetupInspection()
        {
            var player=Child("Player_Inspection",bindings.transform,bindings.PlayerSpawn.position).gameObject;
            var controller=player.AddComponent<CharacterController>(); controller.height=1.8f; controller.center=new Vector3(0,.9f,0); controller.radius=.28f; controller.stepOffset=.3f;
            var camera=Child("Camera_NoVR",player.transform,player.transform.position+Vector3.up*1.65f).gameObject.AddComponent<Camera>();
            camera.nearClipPlane=.05f; camera.farClipPlane=180; camera.gameObject.AddComponent<AudioListener>();
            var walker=player.AddComponent<CombatSceneWalker>(); walker.View=camera;
            var fixture=Child("SceneInspectionFixture",bindings.transform,Vector3.zero).gameObject.AddComponent<CombatSceneFixture>();
            fixture.Bindings=bindings; fixture.Walker=walker;
        }

        static void SetupLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.67f,.73f,.77f);
            RenderSettings.ambientEquatorColor=new Color(.4f,.43f,.4f);
            RenderSettings.ambientGroundColor=new Color(.22f,.19f,.15f);
            var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.3f; sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-32,0); RenderSettings.sun=sun;
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.64f,.7f,.74f); RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=90; RenderSettings.fogEndDistance=190;
        }

        [MenuItem("Tools/Simulated Shooting/Scene 3/Rebuild Navigation In Open Scene")]
        public static void RebuildNavigation()
        {
            var b=UnityEngine.Object.FindObjectOfType<CombatSceneBindings>();
            if(b==null) throw new InvalidOperationException("Open CombatScene first.");
            Bake(b); EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
            EditorSceneManager.SaveScene(b.gameObject.scene); AssetDatabase.SaveAssets();
        }

        static void Bake(CombatSceneBindings b)
        {
            var sources=new List<NavMeshBuildSource>();
            foreach(var collider in b.GeometryRoot.GetComponentsInChildren<BoxCollider>())
            {
                if(!collider.enabled || collider.isTrigger) continue;
                sources.Add(new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box, transform=collider.transform.localToWorldMatrix*Matrix4x4.Translate(collider.center), size=collider.size, area=0 });
            }
            var settings=NavMesh.GetSettingsByID(0); settings.agentRadius=.3f; settings.agentHeight=1.8f; settings.agentClimb=.35f; settings.agentSlope=45;
            settings.overrideVoxelSize=true; settings.voxelSize=.1f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(24,4,40),new Vector3(90,25,120)),Vector3.zero,Quaternion.identity);
            if(data==null) throw new InvalidOperationException("NavMesh bake failed.");
            var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(Art+"/CombatNavigation.asset");
            if(existing==null) { AssetDatabase.CreateAsset(data,Art+"/CombatNavigation.asset"); b.NavigationData=data; }
            else { EditorUtility.CopySerialized(data,existing); UnityEngine.Object.DestroyImmediate(data); b.NavigationData=existing; EditorUtility.SetDirty(existing); }
        }

        [MenuItem("Tools/Simulated Shooting/Scene 3/Validate Open Scene")]
        public static void Validate()
        {
            var b=UnityEngine.Object.FindObjectOfType<CombatSceneBindings>();
            var errors=b.ValidateBindings();
            foreach(var door in b.Doors) door.Obstacle.enabled=false;
            var nav=NavMesh.AddNavMeshData(b.NavigationData);
            try
            {
                foreach(var p in b.Points.Where(p=>p.RequiresNavigation))
                {
                    if(!NavMesh.SamplePosition(p.transform.position,out var hit,.6f,NavMesh.AllAreas)) { errors.Add("Off NavMesh: "+p.Id); continue; }
                    var path=new NavMeshPath();
                    if(!NavMesh.CalculatePath(b.PlayerSpawn.position,hit.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete) errors.Add("Unreachable: "+p.Id);
                    foreach(var c in Physics.OverlapCapsule(p.transform.position+Vector3.up*.35f,p.transform.position+Vector3.up*1.45f,.25f,~0,QueryTriggerInteraction.Ignore))
                        if(c.transform.IsChildOf(b.GeometryRoot)) errors.Add("Point in solid geometry: "+p.Id+" / "+c.name);
                }
                foreach(var root in b.gameObject.scene.GetRootGameObjects())
                    foreach(var t in root.GetComponentsInChildren<Transform>(true))
                        if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) errors.Add("Missing script: "+t.name);
            }
            finally { nav.Remove(); foreach(var door in b.Doors) door.Obstacle.enabled=true; }
            Directory.CreateDirectory("Logs/Scene3");
            File.WriteAllText("Logs/Scene3/bindings-validation.txt",errors.Count==0?"PASS: bindings, navigation, collision clearance, missing scripts":string.Join("\n",errors));
            if(errors.Count>0) throw new InvalidOperationException(string.Join("\n",errors));
        }

        public static void VerifySavedScene()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Capture();
            Validate();
        }

        [MenuItem("Tools/Simulated Shooting/Scene 3/Capture Open Scene")]
        public static void Capture()
        {
            Directory.CreateDirectory("Logs/Scene3");
            Shot("overview",new Vector3(85,85,-25),new Vector3(18,0,40));
            Shot("trench",new Vector3(0,1.7f,-5),new Vector3(0,1.5f,14));
            Shot("town",new Vector3(0,9,42),new Vector3(25,4,75));
        }

        static void Shot(string name,Vector3 from,Vector3 at)
        {
            var go=new GameObject("Capture"); var camera=go.AddComponent<Camera>(); camera.transform.position=from; camera.transform.LookAt(at); camera.farClipPlane=250;
            var rt=new RenderTexture(1440,900,24); var previous=RenderTexture.active; camera.targetTexture=rt;
            camera.Render(); RenderTexture.active=rt; var texture=new Texture2D(1440,900,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1440,900),0,0); texture.Apply();
            File.WriteAllBytes("Logs/Scene3/"+name+".png",texture.EncodeToPNG()); RenderTexture.active=previous; camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
        }

        public static void PolishSavedScene()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            bindings=UnityEngine.Object.FindObjectOfType<CombatSceneBindings>(); geometry=bindings.GeometryRoot;
            earth=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Earth.mat");
            concrete=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Concrete.mat");
            if(bindings.transform.Find("EnvironmentBackdrop")==null)
            {
                var backdrop=Child("EnvironmentBackdrop",bindings.transform,Vector3.zero);
                var ground=Box("Landscape",new Vector3(24,-.65f,40),new Vector3(300,.4f,300),earth);
                ground.SetParent(backdrop,true); UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
                for(int i=0;i<12;i++)
                {
                    var hill=GameObject.CreatePrimitive(PrimitiveType.Sphere); hill.name="DistantEarth"; hill.transform.SetParent(backdrop);
                    var angle=i*Mathf.PI/6; hill.transform.position=new Vector3(24+Mathf.Cos(angle)*110,-4,40+Mathf.Sin(angle)*110);
                    hill.transform.localScale=new Vector3(45,16+(i%3)*5,40); hill.GetComponent<Renderer>().sharedMaterial=earth;
                    UnityEngine.Object.DestroyImmediate(hill.GetComponent<Collider>());
                }
            }
            if(geometry.Find("BuildingRoof")==null) Box("BuildingRoof",new Vector3(28,10.8f,76),new Vector3(24,.3f,24),concrete);
            foreach(var text in geometry.GetComponentsInChildren<TextMesh>())
                if(Mathf.Abs(Mathf.DeltaAngle(text.transform.eulerAngles.y,180))<1) text.transform.rotation=Quaternion.identity;
            // Larger texture scale prevents the single building slab from stretching one tile across 24 metres.
            concrete.SetTextureScale("_BaseMap",new Vector2(4,4));
            concrete.SetColor("_BaseColor",new Color(.85f,.86f,.82f));
            EditorUtility.SetDirty(concrete);
            RenderSettings.fogStartDistance=160; RenderSettings.fogEndDistance=280;
            Bake(bindings);
            EditorSceneManager.SaveScene(bindings.gameObject.scene); AssetDatabase.SaveAssets();
            Capture(); Validate();
        }

        static CombatMapBinding Plan(string id,Vector2 min,Vector2 max,int floor)
        {
            const int size=256; var texture=new Texture2D(size,size,TextureFormat.RGB24,false);
            var pixels=Enumerable.Repeat(new Color(.07f,.1f,.09f),size*size).ToArray();
            Physics.SyncTransforms();
            float height=floor>0?(floor-1)*3.6f+2.5f:id.StartsWith("trench")?4:14;
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                var origin=new Vector3(Mathf.Lerp(min.x,max.x,x/(float)(size-1)),height,Mathf.Lerp(min.y,max.y,y/(float)(size-1)));
                if(Physics.Raycast(origin,Vector3.down,out var hit,height+1,~0,QueryTriggerInteraction.Ignore))
                    pixels[y*size+x]=hit.point.y>height-2.2f?new Color(.55f,.55f,.43f):new Color(.24f,.31f,.28f);
            }
            foreach(var p in points.Where(p=>p.RegionId==(id.StartsWith("trench")?"trench-a":"urban-a") && (floor==0 || p.FloorId=="floor-"+floor)))
            {
                int x=Mathf.RoundToInt((p.transform.position.x-min.x)/(max.x-min.x)*(size-1));
                int y=Mathf.RoundToInt((p.transform.position.z-min.y)/(max.y-min.y)*(size-1));
                int radius=p.Kind==CombatPointKind.EstimateArea?9:4;
                var color=p.Kind==CombatPointKind.EstimateArea?new Color(.7f,.2f,.15f):new Color(.3f,.7f,.62f);
                if(p.Kind==CombatPointKind.EnemySpawn) continue; // Plans expose estimate areas, never exact spawn offsets.
                for(int dx=-radius;dx<=radius;dx++) for(int dy=-radius;dy<=radius;dy++) if(x+dx>=0 && x+dx<size && y+dy>=0 && y+dy<size) pixels[(y+dy)*size+x+dx]=color;
            }
            texture.SetPixels(pixels); texture.Apply(); var path=Art+"/"+id+".png"; File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            return new CombatMapBinding { Id=id,Min=min,Max=max,Plan=AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
        }

        // Adds only this delivery's missing presentation assets to the saved scene; does not rebuild geometry.
        public static void CompletePresentation()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            bindings=UnityEngine.Object.FindObjectOfType<CombatSceneBindings>(); geometry=bindings.GeometryRoot;
            points.Clear(); points.AddRange(bindings.Points);
            for(int i=0;i<bindings.Maps.Length;i++)
            {
                var map=bindings.Maps[i]; bindings.Maps[i]=Plan(map.Id,map.Min,map.Max,i<2?0:i-1);
            }
            if(bindings.transform.Find("XR Origin (VR)")==null)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab");
                var origin=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                origin.name="XR Origin (VR)"; origin.transform.SetParent(bindings.transform); origin.transform.position=bindings.PlayerSpawn.position;
                P1XrFloorOriginUpgrader.ConfigureFloorOrigin(origin);
                // This scene delivers tracking and input anchors. Locomotion policy remains owned by gameplay.
                foreach(var behaviour in origin.GetComponentsInChildren<MonoBehaviour>(true))
                    if(behaviour.GetType().Namespace!=null && behaviour.GetType().Namespace.Contains("Locomotion")) behaviour.enabled=false;
                Id(origin,"CombatScene.Origin.VR");
                var mode=bindings.gameObject.AddComponent<ZeroingRangeXRModeController>();
                mode.Configure(origin,UnityEngine.Object.FindObjectOfType<CombatSceneWalker>().View);
                var serialized=new SerializedObject(mode); serialized.FindProperty("autoDetectVrDisplayInEditor").boolValue=true; serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            if(bindings.ProjectionAnchor.childCount==0)
            {
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name="TrenchProjection";
                quad.transform.SetParent(bindings.ProjectionAnchor,false); quad.transform.localRotation=Quaternion.Euler(45,180,0); quad.transform.localScale=new Vector3(1.3f,1.3f,1);
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetTexture("_BaseMap",bindings.Maps[0].Plan);
                AssetDatabase.CreateAsset(mat,Art+"/Projection.mat"); quad.GetComponent<Renderer>().sharedMaterial=mat;
            }
            EditorSceneManager.SaveScene(bindings.gameObject.scene); AssetDatabase.SaveAssets();
            Capture(); Validate();
        }

        static AudioClip HitSound()
        {
            var path=Art+"/Hit.wav";
            using(var file=new BinaryWriter(File.Create(path)))
            {
                const int count=4800; const int rate=24000;
                file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); file.Write(36+count*2); file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); file.Write(16); file.Write((short)1); file.Write((short)1); file.Write(rate); file.Write(rate*2); file.Write((short)2); file.Write((short)16); file.Write(System.Text.Encoding.ASCII.GetBytes("data")); file.Write(count*2);
                var random=new System.Random(3);
                for(int i=0;i<count;i++) file.Write((short)((random.NextDouble()*2-1)*Math.Exp(-i/650.0)*18000));
            }
            AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static CombatScenePoint Point(string id,string region,CombatPointKind kind,Vector3 position,string floor=null,string room=null,Vector3? volume=null)
        {
            var root=Child("Point_"+id,bindings.transform,position); var p=root.gameObject.AddComponent<CombatScenePoint>();
            p.Id=id; p.RegionId=region; p.Kind=kind; p.FloorId=floor; p.RoomId=room;
            p.Volume=root.gameObject.AddComponent<BoxCollider>(); p.Volume.isTrigger=true; p.Volume.center=new Vector3(0,1.35f,0); p.Volume.size=volume??new Vector3(2,2.7f,2);
            Id(root.gameObject,"CombatScene.Point."+id); points.Add(p); return p;
        }
        static Transform Anchor(string label,Vector3 position) { var t=Child("Anchor_"+label,bindings.transform,position); Id(t.gameObject,"CombatScene.Anchor."+label); return t; }
        static Transform Child(string name,Transform parent,Vector3 position) { var t=new GameObject(name).transform; t.SetParent(parent); t.position=position; return t; }
        static void Id(GameObject go,string id) { go.AddComponent<SceneTestId>().Id=id; }
        static Transform Box(string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(geometry); go.transform.position=position; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=material; return go.transform;
        }
        static Material Material(string name,Color color,string texture=null)
        {
            var path=Art+"/"+name+".mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat!=null) return mat;
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",.12f);
            if(texture!=null) mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
            AssetDatabase.CreateAsset(mat,path); return mat;
        }
        static void Sign(string text,Vector3 position,float yaw)
        {
            var go=Child("Sign_"+text,geometry,position); go.rotation=Quaternion.Euler(0,yaw,0);
            var label=go.gameObject.AddComponent<TextMesh>(); label.text=text; label.characterSize=.13f; label.fontSize=48; label.anchor=TextAnchor.MiddleCenter; label.color=new Color(.9f,.85f,.65f);
        }
    }
}
