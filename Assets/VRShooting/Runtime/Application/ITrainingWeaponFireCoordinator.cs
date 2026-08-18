using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 把展示状态、移动靶路线、连射调度和逐发命中串成 P1/P2 应用流程。
    /// </summary>
    public interface ITrainingWeaponFireCoordinator
    {
        ServiceResult<WeaponFireSequenceStateDto> Tick(
            string sessionId,
            float deltaTime,
            WeaponTriggerStateInputDto trigger,
            WeaponFireInputDto latestFireSnapshot);
    }
}
