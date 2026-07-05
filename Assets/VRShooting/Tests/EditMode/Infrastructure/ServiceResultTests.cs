using NUnit.Framework;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Infrastructure
{
    /// <summary>
    /// ServiceResult 契约测试。追溯 docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    [TestFixture]
    public class Infrastructure_ServiceResultTests
    {
        [Test]
        public void Infrastructure_ServiceResult_Ok_ReturnsSuccessWithData()
        {
            var result = ServiceResult<int>.Ok(42, "ready");

            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Data);
            Assert.AreEqual(ErrorCode.None, result.ErrorCode);
            Assert.AreEqual("ready", result.Message);
        }

        [Test]
        public void Infrastructure_ServiceResult_Fail_ReturnsFailureWithErrorCode()
        {
            var result = ServiceResult<string>.Fail(ErrorCode.InvalidState, "not running", "fallback");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("fallback", result.Data);
            Assert.AreEqual(ErrorCode.InvalidState, result.ErrorCode);
            Assert.AreEqual("not running", result.Message);
        }

        [Test]
        public void Infrastructure_ServiceResult_Fail_DefaultMessageIsEmptyString()
        {
            var result = ServiceResult<int>.Fail(ErrorCode.Busy);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(string.Empty, result.Message);
        }
    }
}
