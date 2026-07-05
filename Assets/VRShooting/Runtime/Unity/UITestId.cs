using UnityEngine;

namespace VRShooting.Unity
{
    /// <summary>
    /// UI 稳定测试 ID 组件。参见 docs/接口文档/11-Unity场景与Prefab约定.md。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UITestId : MonoBehaviour
    {
        [SerializeField]
        string id = string.Empty;

        public string Id => id ?? string.Empty;
    }
}
