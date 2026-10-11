using System;
using UnityEngine;
using VRShooting.Common;

namespace SimulatedShooting.Scene
{
    public sealed class CombatGrenadeProjectile : MonoBehaviour
    {
        GrenadeThrowPlanDto plan;
        float elapsed;
        Action expired;
        Func<Vector3> positionProvider;
        bool launched;
        Vector3 previousPosition;

        public static Bounds GetModelBounds(GameObject model)
        {
            var result=new Bounds();var initialized=false;
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var filter=renderer.GetComponent<MeshFilter>();
                var mesh=filter!=null?filter.sharedMesh:(renderer as SkinnedMeshRenderer)?.sharedMesh;
                if(mesh==null)continue;
                var local=mesh.bounds;
                for(var corner=0;corner<8;corner++)
                {
                    var point=renderer.transform.TransformPoint(local.center+Vector3.Scale(local.extents,
                        new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    if(!initialized){result=new Bounds(point,Vector3.zero);initialized=true;}
                    else result.Encapsulate(point);
                }
            }
            return result;
        }

        public static bool NormalizeModel(GameObject model,float height)
        {
            foreach(var child in model.GetComponentsInChildren<Transform>(true))
            {child.gameObject.SetActive(true);child.gameObject.layer=0;}
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var lod in model.GetComponentsInChildren<LODGroup>(true))lod.ForceLOD(0);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {renderer.enabled=true;renderer.forceRenderingOff=false;}
            var bounds=GetModelBounds(model);
            if(bounds.size.y<=.00001f)return false;
            var centre=model.transform.parent!=null?model.transform.parent.position:model.transform.position;
            model.transform.localScale*=height/bounds.size.y;
            model.transform.position+=centre-GetModelBounds(model).center;
            return true;
        }

        public void Launch(GrenadeThrowPlanDto throwPlan, Action onExpired, Func<Vector3> actualPosition)
        {
            plan = throwPlan; expired = onExpired; positionProvider = actualPosition; elapsed = 0; launched = true;
            transform.position = plan.Origin;
            previousPosition=plan.Origin;
            transform.rotation = Quaternion.LookRotation((plan.Target - plan.Origin).normalized);
        }

        void Update()
        {
            if (!launched) return;
            elapsed += Time.deltaTime;
            var duration = Mathf.Max(.08f, (float)(plan.ExplosionTime - plan.ThrowTime));
            var now = plan.ThrowTime + elapsed;
            var t = Mathf.Clamp01(elapsed / duration);
            transform.position = positionProvider?.Invoke() ?? GrenadeTrajectory.Position(plan, now);
            var movement=transform.position-previousPosition;
            if(movement.sqrMagnitude>.000001f)
                transform.Rotate(420f * Time.deltaTime,280f * Time.deltaTime,190f * Time.deltaTime,Space.Self);
            else if(elapsed>plan.WindupSeconds+.05f)
                // A tumbling model can have a taller vertical bound than its collision radius.
                // Settle upright once the authoritative position stops so it stays above ground.
                transform.rotation=Quaternion.identity;
            previousPosition=transform.position;
            // A service-driven projectile lives until GrenadeExploded/GrenadeCancelled.
            // Wall-clock time must not hide it while the combat clock is stationary.
            if (positionProvider == null && t >= 1f)
            {
                launched = false;
                expired?.Invoke();
            }
        }
    }
}
