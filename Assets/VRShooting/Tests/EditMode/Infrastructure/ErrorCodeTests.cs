using NUnit.Framework;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Infrastructure
{
    /// <summary>
    /// ErrorCode 契约测试。追溯 docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    [TestFixture]
    public class Infrastructure_ErrorCodeTests
    {
        [Test]
        public void Infrastructure_ErrorCode_None_IsZero()
        {
            Assert.AreEqual(0, (int)ErrorCode.None);
        }

        [Test]
        public void Infrastructure_ErrorCode_ErrorBranches_AreDistinctFromNone()
        {
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.InvalidState);
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.InvalidInput);
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.NotFound);
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.Busy);
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.Cooldown);
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.ResourceUnavailable);
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.PersistenceFailed);
            Assert.AreNotEqual(ErrorCode.None, ErrorCode.TestOnlyFailure);
        }

        [Test]
        public void Infrastructure_ErrorCode_FailResult_UsesRequestedCode()
        {
            var result = ServiceResult<object>.Fail(ErrorCode.NotFound, "missing weapon");

            Assert.AreEqual(ErrorCode.NotFound, result.ErrorCode);
            Assert.IsFalse(result.Success);
        }
    }
}
