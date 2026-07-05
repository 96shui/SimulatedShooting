using System;

namespace VRShooting.Application
{
    /// <summary>
    /// 全局事件总线。参见 docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    public interface IGameEventBus
    {
        void Publish<TEvent>(TEvent evt);

        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
}
