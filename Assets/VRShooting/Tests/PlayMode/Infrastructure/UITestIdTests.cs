using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRShooting.Unity;

namespace VRShooting.Tests.PlayMode.Infrastructure
{
    /// <summary>
    /// UITestId PlayMode 程序集占位与组件测试。追溯 docs/接口文档/11-Unity场景与Prefab约定.md。
    /// </summary>
    [TestFixture]
    public class Infrastructure_UITestIdTests
    {
        [UnityTest]
        public IEnumerator Infrastructure_UITestId_DefaultId_IsEmptyStringNotNull()
        {
            var go = new GameObject("Test_UITestId");
            var testId = go.AddComponent<UITestId>();

            yield return null;

            Assert.IsNotNull(testId.Id);
            Assert.AreEqual(string.Empty, testId.Id);

            Object.Destroy(go);
        }
    }
}
