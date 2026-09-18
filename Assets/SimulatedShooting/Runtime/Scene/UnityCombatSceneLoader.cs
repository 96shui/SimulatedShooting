using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;

namespace SimulatedShooting.Scene
{
    public sealed class UnityCombatSceneLoader : ICombatSceneLoader
    {
        public const string ScenePath="Assets/Scenes/CombatScene.unity";
        readonly Func<bool> vrAvailability;
        // Tests may substitute display availability; tracking and interaction still use the real XR rig.
        public UnityCombatSceneLoader(Func<bool> vrAvailability = null){this.vrAvailability=vrAvailability;}
        // Unity async scene creation is serialized; cancelled acquisitions still return their own lease.
        static readonly SemaphoreSlim loads=new SemaphoreSlim(1,1);
        public static bool PreparingProductionScene {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()=>ApplicationServices.CombatSceneLoaderFactory=()=>new UnityCombatSceneLoader();
        public async Task<ServiceResult<ICombatSceneLease>> LoadAsync(TrainingMode mode,CancellationToken cancellation)
        {
            // The registered factory can outlive Play Mode when domain reload is disabled.
            // Runtime scene APIs are unavailable in Edit Mode; fail without touching the scene.
            if(!Application.isPlaying)return ServiceResult<ICombatSceneLease>.Fail(ErrorCode.ResourceUnavailable,"Combat scenes require Play Mode.");
            await loads.WaitAsync(cancellation);
            UnityEngine.SceneManagement.Scene loaded=default;
            try
            {
                PreparingProductionScene=true;
                var before=new HashSet<int>();
                for(int i=0;i<SceneManager.sceneCount;i++)before.Add(SceneManager.GetSceneAt(i).handle);
                var op=SceneManager.LoadSceneAsync(ScenePath,LoadSceneMode.Additive);
                if(op==null)return ServiceResult<ICombatSceneLease>.Fail(ErrorCode.ResourceUnavailable);
                while(!op.isDone)await Task.Yield();
                for(int i=0;i<SceneManager.sceneCount;i++)
                { var s=SceneManager.GetSceneAt(i);if(s.path==ScenePath&&!before.Contains(s.handle))loaded=s; }
                if(!loaded.IsValid())return ServiceResult<ICombatSceneLease>.Fail(ErrorCode.ResourceUnavailable);
                if(cancellation.IsCancellationRequested)
                {
                    var unload=SceneManager.UnloadSceneAsync(loaded);
                    if(unload!=null)while(!unload.isDone)await Task.Yield();
                    loaded=default;
                    return ServiceResult<ICombatSceneLease>.Fail(ErrorCode.InvalidState);
                }
                var binding=loaded.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
                var runtime=binding.gameObject.AddComponent<CombatSceneRuntime>();
                runtime.Prepare(binding,mode,vrAvailability?.Invoke());
                return ServiceResult<ICombatSceneLease>.Ok(new Lease(loaded,runtime));
            }
            catch(Exception exception)
            {
                if(loaded.IsValid()&&loaded.isLoaded)SceneManager.UnloadSceneAsync(loaded);
                Debug.LogError("P3 scene load: "+exception.Message);
                return ServiceResult<ICombatSceneLease>.Fail(ErrorCode.ResourceUnavailable,exception.Message);
            }
            finally {PreparingProductionScene=false;loads.Release();}
        }
        sealed class Lease:ICombatSceneLease
        {
            readonly UnityEngine.SceneManagement.Scene scene;
            CombatSceneRuntime runtime;
            public Lease(UnityEngine.SceneManagement.Scene scene,CombatSceneRuntime runtime){this.scene=scene;this.runtime=runtime;}
            public CombatSceneDefinitionDto Definition=>runtime.Definition;
            public ICombatClock Clock=>runtime.Clock;
            public ICombatRandom Random {get;}=new SeededCombatRandom();
            public ICombatNavigationPort Navigation=>runtime;
            public ServiceResult<Unit> Activate(ICombatCoreService core,ICombatWorldInputPort world,ICombatStateService state,IHUDService hud,ISquadCommandService squad,ICombatTickPort tick,string sessionId)
                =>runtime.Activate(core,world,state,tick,sessionId);
            public void Deactivate(){if(runtime!=null)runtime.Deactivate();}
            public void Dispose()
            {
                if(runtime==null)return;
                runtime.Deactivate();runtime.RestoreMainScene();runtime=null;
                if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
