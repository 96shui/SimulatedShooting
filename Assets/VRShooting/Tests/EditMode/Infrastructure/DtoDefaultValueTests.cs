using NUnit.Framework;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Infrastructure
{
    /// <summary>
    /// P1 DTO 与枚举默认值测试。追溯 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    [TestFixture]
    public class Infrastructure_DtoDefaultValueTests
    {
        [Test]
        public void Infrastructure_AmmoDto_Default_DoesNotUseNullStringsOrInvalidFlags()
        {
            var ammo = AmmoDto.Empty;

            Assert.AreEqual(0, ammo.CurrentMagazine);
            Assert.AreEqual(0, ammo.ReserveAmmo);
            Assert.AreEqual(0, ammo.MagazineCapacity);
            Assert.IsFalse(ammo.IsReloading);
        }

        [Test]
        public void Infrastructure_EnumDefaults_AreSafeForUiBinding()
        {
            Assert.AreEqual(TrainingMode.None, default(TrainingMode));
            Assert.AreEqual(SessionState.NotStarted, default(SessionState));
            Assert.AreEqual(ResultGrade.None, default(ResultGrade));
            Assert.AreEqual(PlayerPosture.Standing, default(PlayerPosture));
            Assert.AreEqual(ShoulderSide.Left, default(ShoulderSide));
            Assert.AreEqual(TrainingPresentationPhase.ModeEntry, default(TrainingPresentationPhase));
            Assert.AreEqual(TrainingPostureMode.ProneFixed, default(TrainingPostureMode));
            Assert.AreEqual(TargetMovePhase.WaitingCountdown, default(TargetMovePhase));
            Assert.AreEqual(WeaponFireSequencePhase.Idle, default(WeaponFireSequencePhase));
            Assert.AreEqual(WeaponFireMode.SingleShot, default(WeaponFireMode));
        }

        [Test]
        public void Infrastructure_RandomSeed_Default_IsUnfixedZero()
        {
            var seed = default(RandomSeed);

            Assert.AreEqual(0, seed.Value);
            Assert.IsFalse(seed.IsFixed);
        }

        [Test]
        public void Infrastructure_RandomSeed_Fixed_PreservesValue()
        {
            var seed = RandomSeed.Fixed(12345);

            Assert.AreEqual(12345, seed.Value);
            Assert.IsTrue(seed.IsFixed);
        }
    }
}
