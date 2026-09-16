using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    [InitializeOnLoad]
    static class CombatTaiwanStreetArtAutoRun
    {
        static readonly string Marker = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/apply-combat-taiwan-reference-art"));

        static CombatTaiwanStreetArtAutoRun()
        {
            if (File.Exists(Marker)) EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }
            if (Enumerable.Range(0,EditorSceneManager.sceneCount).Any(i=>EditorSceneManager.GetSceneAt(i).isDirty))
            {
                Debug.LogError("Taiwan reference art pass paused because the open scene has unsaved changes.");
                return;
            }
            File.Delete(Marker);
            CombatTaiwanStreetArt.Apply();
        }
    }

    // Local art pass: retain the saved buildings, routes, doors and training actors; replace only the visual shell.
    public static class CombatTaiwanStreetArt
    {
        const string Art = "Assets/SimulatedShooting/Art/Combat/TaiwanStreet/";
        static Material tile, mintTile, steel, dark, glass, white, red, yellow, green, blue, plaster, mud, water, wood;
        static Material warmConcrete, terracotta, sandstone, ribbonGlass;
        static TMP_FontAsset font;
        static int meshIndex;
        static readonly string[] Shops = { "永安早餐店", "順發機車行", "幸福便當", "新生五金行", "日常茶飲", "安和藥局" };
        static readonly string[] BladeShops = { "百貨批發", "光陽機車", "公益彩券", "台豐鐘錶", "老林紅茶", "斯朵利髮型" };

        [MenuItem("Tools/Simulated Shooting/Scene 3/Taiwan Street And Trench Art")]
        public static void Apply()
        {
            if (!Application.isBatchMode && Enumerable.Range(0,EditorSceneManager.sceneCount).Any(i=>EditorSceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save scene edits first.");
            var scene = EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath,OpenSceneMode.Single);
            var b = Object.FindObjectOfType<CombatSceneBindings>();
            var old = b.GeometryRoot.Find("TaiwanStreetArt");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = Child(b.GeometryRoot,"TaiwanStreetArt",Vector3.zero);
            meshIndex = 0;
            SetupMaterials();
            SetupFont();
            SetupRendering(b);
            var houses = b.GeometryRoot.Find("Town_PerimeterBuildings");
            int index = 0;
            foreach (Transform house in houses) DressHouse(root,house,index++);
            HideIntrudingEarth(houses);
            DressMainBuilding(root,b.GeometryRoot);
            DressStreet(root,b.GeometryRoot);
            DressTrench(root,b.GeometryRoot);
            // Remove the generic ruined-city treatment from the urban area, retain trench earthwork.
            foreach (var renderer in b.GeometryRoot.GetComponentsInChildren<Renderer>())
                if ((renderer.name == "BrokenMasonry" || renderer.name == "ScorchedPlaster" || renderer.name == "ShellSpall" ||
                     renderer.name == "WindowBoard" || renderer.name == "BrokenWindowFrame" || renderer.name == "ExposedRebar") &&
                    !renderer.transform.IsChildOf(root) && renderer.transform.position.z > 44)
                    renderer.enabled = false;
            foreach (var renderer in b.GeometryRoot.GetComponentsInChildren<Renderer>().Where(r=>r.name=="RoofParapet")) renderer.enabled=true;
            SetLighting();
            Object.FindObjectOfType<CombatSceneFixture>().InspectionFont=AssetDatabase.LoadAssetAtPath<Font>(Art+"NotoSansTC.ttf");
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            // Existing repair entry point bakes the complete district without reconstructing buildings.
            CombatSceneWalkabilityRepair.Apply();
            Capture();
        }

        public static void FinishInteriorClearance()
        {
            var scene=EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath,OpenSceneMode.Single);
            HideIntrudingEarth(Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot.Find("Town_PerimeterBuildings"));
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Capture();
        }

        static void HideIntrudingEarth(Transform houses)
        {
            foreach (Transform house in houses)
            {
                var inside=house.Find("Interior/Floor").GetComponent<Renderer>().bounds;
                inside.center+=Vector3.up*1.6f;
                inside.size=new Vector3(inside.size.x-.6f,2.8f,inside.size.z-.6f);
                foreach(var renderer in Object.FindObjectsOfType<MeshRenderer>().Where(r=>r.name=="Sandbag_Imported" || r.name=="EarthBank"))
                    if(inside.Intersects(renderer.bounds)) renderer.enabled=false;
            }
        }

        static void SetupMaterials()
        {
            tile = Material("IvoryTiles",new Color(.92f,.91f,.86f),.25f);
            tile.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"WeatheredFacadeTiles.png"));
            mintTile = Material("MintTiles",new Color(.64f,.78f,.72f),.23f);
            mintTile.SetTexture("_BaseMap",tile.GetTexture("_BaseMap"));
            steel = Material("GalvanizedMetal",new Color(.48f,.52f,.53f),.48f,.72f);
            dark = Material("RubberAndGrilles",new Color(.035f,.045f,.048f),.17f);
            glass = Material("BlueGreyGlass",new Color(.035f,.075f,.085f),.35f);
            white = Material("IvoryPaint",new Color(.84f,.83f,.74f),.2f);
            red = Material("SignRed",new Color(.55f,.035f,.035f),.28f);
            yellow = Material("RoadYellow",new Color(.88f,.63f,.08f),.1f);
            green = Material("SignGreen",new Color(.045f,.25f,.17f),.25f);
            blue = Material("ScooterBlue",new Color(.045f,.16f,.25f),.6f,.28f);
            plaster = Material("WeatheredPlaster",new Color(.66f,.65f,.58f),.08f);
            plaster.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SimulatedShooting/Art/Textures/ConcreteFloor01/concrete_floor_01_diff_2k.jpg"));
            warmConcrete = Material("TaiwanWarmConcrete",new Color(.76f,.72f,.64f),.12f);
            warmConcrete.SetTexture("_BaseMap",null);
            terracotta = Material("TaiwanTerracottaCladding",new Color(.48f,.27f,.2f),.2f);
            sandstone = Material("TaiwanSandstoneFrame",new Color(.8f,.74f,.64f),.16f);
            ribbonGlass = Material("TaiwanRibbonGlass",new Color(.18f,.39f,.42f),.62f,.08f);
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/Earth.mat");
            mud = AssetDatabase.LoadAssetAtPath<Material>(Art+"WetEarth.mat");
            if (mud==null) { mud=new Material(source); AssetDatabase.CreateAsset(mud,Art+"WetEarth.mat"); }
            mud.SetColor("_BaseColor",new Color(.65f,.59f,.46f)); mud.SetFloat("_Smoothness",.32f);
            water = Material("DrainWater",new Color(.075f,.09f,.065f),.92f,.25f);
            wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/WeatheredWood/WeatheredWood.mat");
            foreach(var m in new[]{tile,mintTile,plaster,mud,warmConcrete,terracotta,sandstone,ribbonGlass}) EditorUtility.SetDirty(m);
        }

        static void SetupFont()
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art+"TaiwanSignsFont.asset");
            bool create=font==null;
            if(create)
            {
                font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Art+"NotoSansTC.ttf"),72,8,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
                font.name="TaiwanSignsFont";AssetDatabase.CreateAsset(font,Art+"TaiwanSignsFont.asset");
                foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture,font);
                AssetDatabase.AddObjectToAsset(font.material,font);
            }
            font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
            font.ClearFontAssetData();
            string copy=string.Join("",Shops)+string.Join("",BladeShops)+"廣慶百貨行民生路新生里街機車停車區營業中手作豆漿飯糰蛋餅專業維修換油輪胎冷氣水電五金工具外帶歡迎光臨茶咖啡藥水塔0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ /.-_";
            copy+=string.Join("",Object.FindObjectsOfType<TextMeshPro>().Select(t=>t.text));
            if(!font.TryAddCharacters(copy,out var missing)) throw new InvalidOperationException("Missing Traditional Chinese glyphs: "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            foreach(var texture in font.atlasTextures) EditorUtility.SetDirty(texture);
            EditorUtility.SetDirty(font.material);EditorUtility.SetDirty(font);
        }

        static void DressHouse(Transform root,Transform house,int index)
        {
            var facade = Child(root,"HouseDetails_"+house.name,house.position);
            facade.rotation=house.rotation;
            var floor=house.Find("Interior/Floor");
            float width=floor.localScale.x, depth=floor.localScale.z;
            float front=-depth/2;
            var walls=house.Find("Interior").GetComponentsInChildren<MeshRenderer>();
            var facadeMaterial=index%3==0?warmConcrete:index%3==1?terracotta:sandstone;
            foreach(var r in walls.Where(r=>r.name.EndsWith("Wall")||r.name.StartsWith("Door")))
                Surface(r,facadeMaterial);
            foreach(Transform part in house.Cast<Transform>().ToArray())
            {
                if(part.name=="BrokenParapet"||part.name=="Chimney"||part.name=="BrokenShutter"||part.name=="FacadeRubble")
                    foreach(var r in part.GetComponentsInChildren<Renderer>()) r.enabled=false;
                if(part.name=="DarkWindow")
                {
                    foreach(var r in part.GetComponentsInChildren<Renderer>()) r.enabled=false;
                    ModernWindow(facade,part.localPosition,part.localPosition.z>0?180:0);
                }
            }
            float height=house.Find("Interior/RearWall").localScale.y;
            float doorX=house.Find("Interior/WalkEntrance").localPosition.x;
            ModernFacade(facade,width,height,front,index);
            var canopy=Box(facade,"ArcadeCanopy",new Vector3(0,2.95f,front-1.15f),new Vector3(width+.6f,.18f,2.6f),plaster,true);
            for(float x=-width/2+.25f;x<=width/2;x+=width/3)
                if(Mathf.Abs(x-doorX)>1.4f)
                    Box(facade,"ArcadeColumn",new Vector3(x,1.4f,front-2.25f),new Vector3(.25f,2.8f,.25f),plaster,true);
            var colour=index%3==0?red:index%3==1?green:blue;
            Sign(facade,Shops[index],new Vector3(0,3.55f,front-2.46f),new Vector2(width-.2f,.95f),colour,.66f);
            Sign(facade,index==1?"專業維修  換油  輪胎":index==0?"手作豆漿  飯糰  蛋餅":"歡迎光臨  營業中",new Vector3(0,3.02f,front-2.47f),new Vector2(width-.2f,.25f),white,.17f,dark);
            Sign(facade,"營業中",new Vector3(doorX+1.55f,1.55f,front-.21f),new Vector2(.9f,.38f),green,.19f);
            BladeSign(facade,BladeShops[index],new Vector3(-width/2-.45f,4.65f,front-2.15f),index%2==0?blue:red);
            Box(facade,"StripedShopAwning",new Vector3(0,2.72f,front-1.45f),new Vector3(width-.7f,.1f,2.15f),colour);
            for(int i=0;i<Mathf.FloorToInt(width/.7f);i++)
                Box(facade,"AwningStripe",new Vector3(-width/2+.55f+i*.7f,2.785f,front-1.45f),new Vector3(.28f,.015f,2.15f),white);
            // Raised shutters beside the real entrance; never cover its clear opening.
            for(float x=1;x<width/2-1;x+=2.6f)
            {
                Box(facade,"ShopWindow",new Vector3(x,1.3f,front-.17f),new Vector3(2.25f,2.3f,.08f),glass);
                for(int i=0;i<5;i++) Box(facade,"RollerShutter",new Vector3(x,2.4f-i*.085f,front-.25f),new Vector3(2.32f,.055f,.08f),steel);
                Box(facade,"ShopMullion",new Vector3(x,1.25f,front-.24f),new Vector3(.055f,2.3f,.06f),steel);
            }
            for(int f=1;f<Mathf.RoundToInt(height/3.2f);f++)
            {
                AirConditioner(facade,new Vector3(width/2-1.05f,f*3.2f+1.1f,front-.38f));
                Pipe(facade,"RainDrain",new Vector3(width/2-.3f,.2f,front-.23f),new Vector3(width/2-.3f,height+.1f,front-.23f),.045f,steel);
            }
            Tank(facade,new Vector3(width*.2f,height+.2f,depth*.18f));
            Tank(facade,new Vector3(-width*.26f,height+.2f,depth*.18f),.8f);
            // Roof equipment, cables and pipes reinforce scale without inventing extra walkable floors.
            Box(facade,"RoofUtilityRoom",new Vector3(0,height+.65f,depth*.3f),new Vector3(3,1.3f,2),plaster);
            Box(facade,"CorrugatedRoof",new Vector3(0,height+1.35f,depth*.3f),new Vector3(3.4f,.1f,2.4f),steel);
            for(int i=0;i<9;i++) Box(facade,"RoofRib",new Vector3(-1.6f+i*.4f,height+1.43f,depth*.3f),new Vector3(.04f,.045f,2.4f),steel);
            // Furnish the previously empty ground-floor room, away from entrance and dummy.
            var furniture=Child(facade,"ShopInterior",Vector3.zero);
            Box(furniture,"Counter",new Vector3(width*.25f,.45f,-depth*.1f),new Vector3(2.6f,.9f,.7f),white,true);
            Box(furniture,"CounterTop",new Vector3(width*.25f,.94f,-depth*.1f),new Vector3(2.72f,.08f,.8f),steel);
            for(int i=0;i<3;i++)
            {
                Box(furniture,"Shelf",new Vector3(width/2-.5f,.6f+i*.6f,1),new Vector3(.7f,.06f,3),steel);
                for(int j=0;j<5;j++) Box(furniture,"ShopStock",new Vector3(width/2-.5f,.78f+i*.6f,-.1f+j*.5f),new Vector3(.4f,.3f,.32f),j%2==0?white:green);
            }
            Combine(facade,"House"+index);
        }

        static void DressMainBuilding(Transform root,Transform geometry)
        {
            var details=Child(root,"MainApartmentDetails",Vector3.zero);
            foreach(var r in geometry.GetComponentsInChildren<MeshRenderer>().Where(r=>r.transform.parent==geometry &&
                new[]{"RearWall","EastWall","FrontWall","FrontHeader","WestWall","WestEndWall"}.Contains(r.name))) Surface(r,warmConcrete);
            ModernRibbon(details,new Vector3(31,5.35f,63.68f),15.5f,1.25f,0);
            ModernRibbon(details,new Vector3(31,8.95f,63.68f),15.5f,1.25f,0);
            for(int x=24;x<39;x+=5)
            {
                ModernWindow(details,new Vector3(x,1.85f,63.68f),0);
                for(int f=0;f<3;f++) AirConditioner(details,new Vector3(x+1.65f,f*3.6f+1.15f,63.4f));
            }
            for(int i=0;i<4;i++) Tank(details,new Vector3(23+i*4,10.95f,79));
            Sign(details,"廣慶百貨行",new Vector3(31,3.05f,63.35f),new Vector2(12,.8f),green,.6f);
            BladeSign(details,"台豐鐘錶",new Vector3(39.15f,5.1f,62.95f),red);
            Box(details,"ShopAwning",new Vector3(31,2.65f,62.9f),new Vector3(17,.13f,2),green);
            for(int i=0;i<35;i++) Box(details,"AwningStripe",new Vector3(22.6f+i*.48f,2.73f,62.9f),new Vector3(.23f,.015f,2),white);
            foreach(float x in new[]{16.3f,39.7f}) Pipe(details,"DrainPipe",new Vector3(x,.1f,63.65f),new Vector3(x,10.8f,63.65f),.05f,steel);
            Combine(details,"MainApartment");
        }

        static void Window(Transform parent,Vector3 p,float yaw)
        {
            var root=Child(parent,"TaiwanSecurityWindow",p);
            root.localRotation=Quaternion.Euler(0,yaw,0);
            root.localPosition += root.localRotation * Vector3.back * .24f;
            Box(root,"Glass",new Vector3(0,0,-.17f),new Vector3(1.2f,1.45f,.06f),glass);
            for(int i=-1;i<=1;i++) Box(root,"WindowFrame",new Vector3(i*.6f,0,-.1f),new Vector3(.045f,1.5f,.09f),steel);
            foreach(int sign in new[]{-1,1}) Box(root,"WindowFrame",new Vector3(0,sign*.75f,-.1f),new Vector3(1.3f,.045f,.09f),steel);
            for(int i=0;i<7;i++) Box(root,"SecurityBar",new Vector3(-.57f+i*.19f,0,-.23f),new Vector3(.02f,1.5f,.025f),steel);
            for(int i=0;i<3;i++) Box(root,"SecurityBrace",new Vector3(0,-.6f+i*.6f,-.24f),new Vector3(1.26f,.025f,.025f),steel);
            Box(root,"WindowHood",new Vector3(0,.82f,-.15f),new Vector3(1.5f,.055f,.6f),steel);
        }

        static void ModernFacade(Transform parent,float width,float height,float front,int variant)
        {
            var floors=Mathf.Max(1,Mathf.RoundToInt(height/3.2f));
            var frame=variant%2==0?terracotta:sandstone;
            for(int floor=1;floor<floors;floor++)
                ModernRibbon(parent,new Vector3(0,floor*3.2f+1.55f,front-.38f),width-1.2f,1.35f,0,frame);
            foreach(float x in new[]{-width/2+.28f,width/2-.28f})
                Box(parent,"ModernFacadePier",new Vector3(x,height*.5f,front-.24f),new Vector3(.46f,height,.2f),frame);
            Box(parent,"ModernRoofCap",new Vector3(0,height+.16f,front-.18f),new Vector3(width+.15f,.32f,.32f),terracotta);
        }

        static void ModernRibbon(Transform parent,Vector3 p,float width,float height,float yaw,Material frame=null)
        {
            frame=frame==null?sandstone:frame;
            var root=Child(parent,"ModernRibbonWindow",p);
            root.localRotation=Quaternion.Euler(0,yaw,0);
            Box(root,"TintedGlass",Vector3.zero,new Vector3(width,height,.07f),ribbonGlass);
            foreach(float y in new[]{-height/2,height/2}) Box(root,"RibbonFrame",new Vector3(0,y, -.06f),new Vector3(width+.18f,.1f,.14f),frame);
            var panes=Mathf.Max(2,Mathf.RoundToInt(width/2.2f));
            for(int i=0;i<=panes;i++)
            {
                var x=-width/2+i*width/panes;
                Box(root,"RibbonMullion",new Vector3(x,0,-.06f),new Vector3(.075f,height,.14f),steel);
            }
        }

        static void ModernWindow(Transform parent,Vector3 p,float yaw)
        {
            var root=Child(parent,"ModernWindow",p);
            root.localRotation=Quaternion.Euler(0,yaw,0);
            root.localPosition+=root.localRotation*Vector3.back*.18f;
            Box(root,"TintedGlass",Vector3.zero,new Vector3(1.7f,1.55f,.07f),ribbonGlass);
            foreach(float x in new[]{-.85f,0,.85f}) Box(root,"WindowFrame",new Vector3(x,0,-.06f),new Vector3(.07f,1.62f,.14f),steel);
            foreach(float y in new[]{-.78f,.78f}) Box(root,"WindowFrame",new Vector3(0,y,-.06f),new Vector3(1.78f,.08f,.14f),sandstone);
        }

        static void AirConditioner(Transform parent,Vector3 p)
        {
            var root=Child(parent,"AirConditioner",p);
            Box(root,"Case",Vector3.zero,new Vector3(.85f,.55f,.42f),white);
            var fan=Shape(root,"FanGrille",new Vector3(-.15f,0,-.225f),new Vector3(.39f,.018f,.39f),dark,PrimitiveType.Cylinder);
            fan.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<8;i++) Box(root,"FanWire",new Vector3(-.32f+i*.05f,0,-.25f),new Vector3(.01f,.37f,.018f),steel);
            for(int i=0;i<5;i++) Box(root,"Vent",new Vector3(.28f,-.16f+i*.08f,-.22f),new Vector3(.18f,.018f,.012f),dark);
            foreach(float x in new[]{-.32f,.32f}) Box(root,"MountBracket",new Vector3(x,-.33f,0),new Vector3(.045f,.055f,.62f),steel);
            Pipe(root,"Conduit",new Vector3(.45f,0,0),new Vector3(.65f,-1.1f,0),.024f,white);
        }

        static void Tank(Transform parent,Vector3 p,float scale=1)
        {
            var root=Child(parent,"RoofWaterTank",p); root.localScale=Vector3.one*scale;
            Shape(root,"TankBody",new Vector3(0,.85f,0),new Vector3(1.25f,.72f,1.25f),steel,PrimitiveType.Cylinder);
            Shape(root,"TankCap",new Vector3(0,1.57f,0),new Vector3(1.22f,.12f,1.22f),steel,PrimitiveType.Sphere);
            for(int i=0;i<4;i++) Shape(root,"TankBand",new Vector3(0,.28f+i*.37f,0),new Vector3(1.28f,.025f,1.28f),white,PrimitiveType.Cylinder);
            foreach(float x in new[]{-.4f,.4f}) foreach(float z in new[]{-.4f,.4f}) Box(root,"TankLeg",new Vector3(x,.1f,z),new Vector3(.08f,.3f,.08f),steel);
            Pipe(root,"TankPipe",new Vector3(.65f,.3f,0),new Vector3(.65f,-.05f,1),.045f,steel);
        }

        static void DressStreet(Transform root,Transform geometry)
        {
            var street=Child(root,"StreetFurniture",Vector3.zero);
            var asphalt=Material("WeatheredAsphalt",new Color(.34f,.36f,.36f),.16f);
            asphalt.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SimulatedShooting/Art/Textures/ConcreteFloor01/concrete_floor_01_diff_2k.jpg"));
            Surface(geometry.Find("Street").GetComponent<MeshRenderer>(),asphalt);
            var ground=geometry.Find("Town_ExteriorCollision/TownGround"); Surface(ground.GetComponent<MeshRenderer>(),plaster);
            for(int side=-1;side<=1;side+=2) Box(street,"DoubleYellowLine",new Vector3(24,.014f,54+side*.15f),new Vector3(57,.012f,.09f),yellow);
            for(int i=0;i<11;i++) Box(street,"Crosswalk",new Vector3(42,.017f,48.4f+i*.7f),new Vector3(3,.014f,.4f),white);
            for(int i=0;i<9;i++)
            {
                float x=-1+i*1.1f;
                Box(street,"ScooterBayLine",new Vector3(x,.014f,60),new Vector3(.05f,.012f,2),white);
                if(i<8) Scooter(street,new Vector3(x+.5f,.03f,60),i%2==0?165:175,i%3);
            }
            Box(street,"ScooterBayEnd",new Vector3(3.4f,.015f,59),new Vector3(8.8f,.012f,.05f),white);
            for(int i=0;i<4;i++) Scooter(street,new Vector3(42f+i*1.4f,.03f,60),i%2==0?165:175,i%3);
            foreach(float x in new[]{0f,48f})
            {
                var pole=Child(street,"UtilityPole",new Vector3(x,0,60.9f));
                Shape(pole,"Pole",new Vector3(0,3.7f,0),new Vector3(.23f,3.7f,.23f),plaster,PrimitiveType.Cylinder);
                Box(pole,"Crossarm",new Vector3(0,7,0),new Vector3(1.5f,.09f,.1f),steel);
                for(int j=-1;j<=1;j++) Shape(pole,"Insulator",new Vector3(j*.6f,7.15f,0),new Vector3(.14f,.12f,.14f),white,PrimitiveType.Cylinder);
                Sign(pole,"民生路",new Vector3(.85f,3.1f,-.14f),new Vector2(1.4f,.45f),green,.3f);
                Sign(pole,"新生里",new Vector3(.85f,2.84f,-.14f),new Vector2(1.4f,.17f),green,.095f);
            }
            for(int wire=-1;wire<=1;wire++)
                for(int i=0;i<16;i++)
                {
                    float a=i/16f, c=(i+1)/16f;
                    Pipe(street,"OverheadCable",new Vector3(48*a,7.2f-1.2f*Mathf.Sin(a*Mathf.PI),60.9f+wire*.6f),
                        new Vector3(48*c,7.2f-1.2f*Mathf.Sin(c*Mathf.PI),60.9f+wire*.6f),.012f,dark);
                }
            for(int i=0;i<17;i++)
            {
                float x=-3+i*3.3f;
                Box(street,"CurbPaint",new Vector3(x,.09f,61.52f),new Vector3(3.1f,.08f,.06f),i%3==0?white:red);
                Box(street,"DrainGrate",new Vector3(x,.01f,61.2f),new Vector3(.75f,.02f,.35f),dark);
                for(int j=0;j<7;j++) Box(street,"DrainSlat",new Vector3(x-.3f+j*.1f,.025f,61.2f),new Vector3(.035f,.02f,.34f),steel);
            }
            foreach(var p in new[]{new Vector3(2,.018f,51),new Vector3(33,.018f,57)})
            {
                Shape(street,"ManholeCover",p,new Vector3(.7f,.009f,.7f),steel,PrimitiveType.Cylinder);
                for(int i=-2;i<=2;i++) Box(street,"ManholeTread",p+new Vector3(i*.1f,.013f,0),new Vector3(.025f,.015f,.48f),dark);
            }
            Combine(street,"StreetRealAssets");
        }

        static void Scooter(Transform parent,Vector3 p,float yaw,int colour)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"ScopiaScooter/scooter2.obj");
            if(source==null) throw new InvalidOperationException("Import licensed Scopia scooter2.obj before applying street art.");
            var root=Child(parent,"ParkedScooter",p);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source,root);
            model.name="Scopia_Scooter";
            var renderers=model.GetComponentsInChildren<MeshRenderer>();
            var bounds=renderers[0].bounds;
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            float scale=1.9f/bounds.size.z;
            model.transform.localScale=Vector3.one*scale;
            model.transform.localPosition=new Vector3(-(bounds.center.x-root.position.x)*scale,-(bounds.min.y-root.position.y)*scale,-(bounds.center.z-root.position.z)*scale);
            foreach(var r in renderers)
            {
                r.sharedMaterials=r.sharedMaterials.Select(original=>
                {
                    string id=original.name;
                    var color=Color.white;
                    var lines=File.ReadAllLines(Art+"ScopiaScooter/scooter2.mtl");
                    for(int i=0;i<lines.Length;i++)
                        if(lines[i]=="newmtl "+id)
                            for(int j=i+1;j<lines.Length && !lines[j].StartsWith("newmtl ");j++)
                                if(lines[j].StartsWith("Kd "))
                                {
                                    var rgb=lines[j].Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
                                    color=new Color(float.Parse(rgb[1],System.Globalization.CultureInfo.InvariantCulture),float.Parse(rgb[2],System.Globalization.CultureInfo.InvariantCulture),float.Parse(rgb[3],System.Globalization.CultureInfo.InvariantCulture));
                                }
                    float metallic=0,smoothness=.25f;
                    if(id=="firstcolor") { color=colour==0?new Color(.055f,.07f,.19f):colour==1?new Color(.32f,.025f,.018f):new Color(.72f,.7f,.65f);smoothness=.65f;metallic=.25f; }
                    if(id.Contains("chrome") || id.Contains("metal") || id.Contains("mirror")) { metallic=.8f;smoothness=.72f; }
                    if(id.Contains("tires") || id.Contains("seat")) smoothness=.12f;
                    var mat=Material("Scopia_"+id+(id=="firstcolor"?colour.ToString():""),color,smoothness,metallic);
                    mat.SetTexture("_BaseMap",id=="textured"?AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"ScopiaScooter/scooter-texture-map.png"):null);
                    return mat;
                }).ToArray();
            }
            var c=root.gameObject.AddComponent<BoxCollider>();
            c.center=new Vector3(0,bounds.size.y*scale*.5f,0);c.size=bounds.size*scale;
            root.localRotation=Quaternion.Euler(0,yaw,0);
        }

        [MenuItem("Tools/Simulated Shooting/Scene 3/Import Real Scooters And Traditional Text")]
        public static void ApplyRealScooters()
        {
            if(!Application.isBatchMode && Enumerable.Range(0,EditorSceneManager.sceneCount).Any(i=>EditorSceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save scene edits first.");
            var scene=EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath,OpenSceneMode.Single);
            var b=Object.FindObjectOfType<CombatSceneBindings>();
            SetupMaterials();SetupFont();
            var root=b.GeometryRoot.Find("TaiwanStreetArt");
            Object.DestroyImmediate(root.Find("StreetFurniture").gameObject);
            meshIndex=1000;DressStreet(root,b.GeometryRoot);
            Object.FindObjectOfType<CombatSceneFixture>().InspectionFont=AssetDatabase.LoadAssetAtPath<Font>(Art+"NotoSansTC.ttf");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            CombatSceneWalkabilityRepair.Apply();Capture();
        }

        static void DressTrench(Transform root,Transform geometry)
        {
            var trench=Child(root,"TrenchWeathering",Vector3.zero);
            var floors=geometry.GetComponentsInChildren<Transform>().Where(t=>t.name=="TrenchFloor").ToArray();
            foreach(var floor in floors)
            {
                Surface(floor.GetComponent<MeshRenderer>(),mud);
                var cell=Child(trench,"DrainageCell",floor.position+Vector3.up*.2f);
                // Water stays below the existing raised timber walkway and does not create a new walking surface.
                Shape(cell,"ShallowDrainWater",new Vector3(.8f,.006f,0),new Vector3(.3f,.008f,Mathf.Min(3.6f,floor.localScale.z-.2f)),water,PrimitiveType.Cube);
                foreach(int side in new[]{-1,1})
                    for(int i=0;i<4;i++)
                    {
                        var stone=Shape(cell,"SoilClod",new Vector3(side*(1.48f+.08f*(i%2)),.035f,-1.3f+i*.8f),
                            new Vector3(.26f,.09f,.2f),mud,PrimitiveType.Sphere);
                        stone.localRotation=Quaternion.Euler(0,i*73,11);
                    }
                Combine(cell,"TrenchCell"+Array.IndexOf(floors,floor));
            }
            foreach(var wall in geometry.GetComponentsInChildren<Transform>().Where(t=>t.name=="EarthRevetment"))
            {
                Surface(wall.GetComponent<MeshRenderer>(),mud);
                var nearest=floors.OrderBy(f=>(f.position-wall.position).sqrMagnitude).First();
                var inward=nearest.position-wall.position;
                inward=wall.localScale.x>wall.localScale.z?new Vector3(0,0,Mathf.Sign(inward.z)):new Vector3(Mathf.Sign(inward.x),0,0);
                var along=new Vector3(inward.z,0,-inward.x);
                for(int i=-1;i<=1;i++)
                {
                    var p=wall.position+inward*.42f+along*i*1.5f;
                    for(int row=0;row<3;row++)
                        Shape(trench,"RevetmentBolt",new Vector3(p.x,.45f+row*.65f,p.z),new Vector3(.055f,.055f,.055f),steel,PrimitiveType.Sphere);
                }
                Pipe(trench,"FieldCable",wall.position+inward*.43f+along*1.8f+Vector3.up*.55f,
                    wall.position+inward*.43f-along*1.8f+Vector3.up*.55f,.012f,dark);
            }
            Combine(trench,"TrenchHardware",false);
        }

        static void SetLighting()
        {
            var sky=AssetDatabase.LoadAssetAtPath<Material>(Art+"TaiwanDaylight.mat");
            if(sky==null) { sky=new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(sky,Art+"TaiwanDaylight.mat"); }
            sky.shader=Shader.Find("Skybox/Panoramic");
            sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"CloudySky.hdr"));
            sky.SetFloat("_Exposure",.8f); sky.SetFloat("_Mapping",1); sky.SetFloat("_Rotation",35);
            sky.SetColor("_Tint",Color.gray);
            RenderSettings.skybox=sky; EditorUtility.SetDirty(sky);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.52f,.6f,.68f);
            RenderSettings.ambientEquatorColor=new Color(.33f,.38f,.4f);
            RenderSettings.ambientGroundColor=new Color(.18f,.17f,.14f);
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.66f,.72f,.75f);
            RenderSettings.fogStartDistance=85; RenderSettings.fogEndDistance=210;
            if(RenderSettings.sun!=null)
            {
                RenderSettings.sun.transform.rotation=Quaternion.Euler(48,-36,0);
                RenderSettings.sun.intensity=1.15f; RenderSettings.sun.color=new Color(1,.96f,.89f);
                RenderSettings.sun.shadowStrength=.78f; RenderSettings.sun.shadows=LightShadows.Soft;
            }
            DynamicGI.UpdateEnvironment();
        }

        static void SetupRendering(CombatSceneBindings bindings)
        {
            var profile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Art+"CombatQuality.asset");
            if(profile==null)
            {
                profile=Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Project Configuration/Quality URP Config.asset"));
                profile.name="CombatQuality";AssetDatabase.CreateAsset(profile,Art+"CombatQuality.asset");
            }
            var serialized=new SerializedObject(profile);
            serialized.FindProperty("m_MainLightShadowsSupported").boolValue=true;
            serialized.FindProperty("m_MainLightShadowmapResolution").intValue=2048;
            serialized.FindProperty("m_ShadowDistance").floatValue=90;
            serialized.FindProperty("m_SoftShadowsSupported").boolValue=true;
            serialized.FindProperty("m_ShadowCascadeCount").intValue=4;
            serialized.FindProperty("m_MSAA").intValue=4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var controller=bindings.GetComponent<CombatSceneRenderProfile>();
            if(controller==null) controller=bindings.gameObject.AddComponent<CombatSceneRenderProfile>();
            controller.enabled=false;controller.Profile=profile;controller.enabled=true;
        }

        static void Sign(Transform parent,string text,Vector3 p,Vector2 size,Material backing,float height,Material ink=null)
        {
            Box(parent,"ShopSignBoard",p,new Vector3(size.x,size.y,.08f),backing);
            var go=Child(parent,"TraditionalSign",p+Vector3.back*.048f);
            var label=go.gameObject.AddComponent<TextMeshPro>();
            label.font=font; label.text=text; label.fontSize=height*10; label.alignment=TextAlignmentOptions.Center;
            label.color=ink==null?new Color(.97f,.96f,.88f):ink.color;
            label.fontStyle=FontStyles.Bold;
            label.rectTransform.sizeDelta=size; label.enableWordWrapping=false;
            label.overflowMode=TextOverflowModes.Overflow;
            label.ForceMeshUpdate();
        }

        static void BladeSign(Transform parent,string text,Vector3 p,Material backing)
        {
            var blade=Child(parent,"VerticalShopSign",p);
            blade.localRotation=Quaternion.Euler(0,90,0);
            Sign(blade,string.Join("\n",text.ToCharArray()),Vector3.zero,new Vector2(.78f,2.9f),backing,.22f);
        }

        static Material Material(string name,Color colour,float smoothness,float metallic=0)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Art+name+".mat");
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,Art+name+".mat"); }
            material.SetColor("_BaseColor",colour); material.SetFloat("_Smoothness",smoothness); material.SetFloat("_Metallic",metallic);
            material.enableInstancing=true; EditorUtility.SetDirty(material); return material;
        }

        static Transform Child(Transform parent,string name,Vector3 p)
        {
            var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=p; return t;
        }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool collider=false)
        {
            var t=Shape(parent,name,p,size,material,PrimitiveType.Cube);
            if(collider) t.gameObject.AddComponent<BoxCollider>();
            return t;
        }
        static Transform Shape(Transform parent,string name,Vector3 p,Vector3 size,Material material,PrimitiveType type)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=p; go.transform.localScale=size; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial=material; go.isStatic=true; return go.transform;
        }
        static void Pipe(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material material)
        {
            var t=Shape(parent,name,(a+b)/2,new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2),material,PrimitiveType.Cylinder);
            t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        static void Surface(MeshRenderer renderer,Material material)
        {
            var filter=renderer.GetComponent<MeshFilter>();
            var mesh=Object.Instantiate(filter.sharedMesh); mesh.name="WorldScaleSurface";
            var vertices=mesh.vertices; var normals=mesh.normals; var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var p=renderer.transform.TransformPoint(vertices[i]); var n=renderer.transform.TransformDirection(normals[i]);
                uv[i]=(Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y))/2;
            }
            mesh.uv=uv; mesh.RecalculateTangents(); filter.sharedMesh=SaveMesh(mesh,"Surface"+meshIndex++);
            renderer.sharedMaterial=material;
        }
        static Mesh SaveMesh(Mesh mesh,string name)
        {
            var path=Art+name+".asset"; var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); return saved;
        }
        static void Combine(Transform parent,string name,bool recursive=true)
        {
            var renderers=(recursive?parent.GetComponentsInChildren<MeshRenderer>():parent.Cast<Transform>().Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r!=null).ToArray())
                .Where(r=>r.GetComponent<TextMeshPro>()==null && !r.GetComponentsInParent<Transform>().Any(t=>t.name=="ParkedScooter") && r.enabled && r.GetComponent<MeshFilter>()!=null).ToArray();
            int index=0;
            foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
            {
                var mesh=new Mesh { name=name+"_"+index,indexFormat=IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Select(r=>new CombineInstance { mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix }).ToArray());
                var go=Child(parent,"Batched_"+name+"_"+index,Vector3.zero).gameObject;
                go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,name+"_"+index++);
                go.AddComponent<MeshRenderer>().sharedMaterial=group.Key; go.isStatic=true;
                foreach(var r in group)
                {
                    var source=r.gameObject;
                    Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);
                    if(source.transform.childCount==0 && source.GetComponents<Component>().Length==1 && source.name!="ShallowDrainWater")
                        Object.DestroyImmediate(source);
                }
            }
        }

        public static void Capture()
        {
            Directory.CreateDirectory("Logs/Scene3/Taiwan");
            foreach(var text in Object.FindObjectsOfType<TextMeshPro>()) text.ForceMeshUpdate();
            var previous=QualitySettings.renderPipeline;
            var shadows=QualitySettings.shadows;
            var distance=QualitySettings.shadowDistance;
            try
            {
                QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Art+"CombatQuality.asset");
                QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowDistance=90;
                Shot("scooter",new Vector3(1.7f,1.2f,57.4f),new Vector3(1.7f,.65f,60));
                var house=Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot.Find("Town_PerimeterBuildings").GetChild(0);
                var dummy=house.Find("Interior/TrainingDummy");
                Shot("person",dummy.position+dummy.forward*2.4f+Vector3.up*1.2f,dummy.position+Vector3.up*.95f);
                Shot("street",new Vector3(15,1.7f,48),new Vector3(2,2,61));
                Shot("district",new Vector3(-24,18,31),new Vector3(20,4,70));
                Shot("shop",new Vector3(9,1.7f,51),new Vector3(8,2,42));
                Shot("trench",new Vector3(.2f,1.65f,-6),new Vector3(0,1.4f,10));
            }
            finally
            {
                QualitySettings.renderPipeline=previous;QualitySettings.shadows=shadows;QualitySettings.shadowDistance=distance;
            }
        }
        static void Shot(string name,Vector3 from,Vector3 at)
        {
            var go=new GameObject("ArtCapture"); var camera=go.AddComponent<Camera>();
            camera.transform.position=from;camera.transform.LookAt(at);camera.fieldOfView=65;camera.farClipPlane=250;
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();
            File.WriteAllBytes("Logs/Scene3/Taiwan/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
        }
    }
}
