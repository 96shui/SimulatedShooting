using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// UI 页面路由。参见 docs/接口文档/01-页面导航与UI事件.md。
    /// </summary>
    public interface IUIRouter
    {
        ScreenId Current { get; }

        TrainingMode? SelectedMode { get; }

        ServiceResult<ScreenId> Open(ScreenId screen, NavigationArgs args = default);

        ServiceResult<ScreenId> Back();

        ServiceResult<ScreenId> HandleUIEvent(UIEventId eventId, ScreenId sourceScreen, NavigationArgs args = default);

        bool IsTransitioning { get; }
    }
}
