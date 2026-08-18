using System;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// P1 单发与 P2 两发起射/长按连射调度。参见 docs/接口文档/06-武器与弹药服务.md。
    /// </summary>
    public interface IWeaponAutomaticFireService
    {
        ServiceResult<WeaponFireSequenceStateDto> StartSession(
            string sessionId,
            WeaponFireMode fireMode,
            WeaponAutoFireConfigDto config);

        ServiceResult<WeaponFireSequenceStateDto> GetState(string sessionId);

        ServiceResult<WeaponFireSequenceStateDto> UpdateTrigger(WeaponTriggerStateInputDto input);

        ServiceResult<WeaponFireSequenceStateDto> Tick(
            string sessionId,
            float deltaTime,
            WeaponFireInputDto latestFireSnapshot);

        ServiceResult<WeaponFireSequenceStateDto> Cancel(
            string sessionId,
            WeaponFireStopReason reason);

        event Action<WeaponFireSequenceStateDto> FireSequenceChanged;
    }
}
