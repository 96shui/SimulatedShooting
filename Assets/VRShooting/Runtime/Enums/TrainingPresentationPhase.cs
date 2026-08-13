namespace VRShooting.Common
{
    /// <summary>
    /// P1/P2 共用展示阶段。参见 docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md。
    /// </summary>
    public enum TrainingPresentationPhase
    {
        ModeEntry = 0,
        AwaitingStartConfirmation,
        AwaitingWeaponPickup,
        LiveFire,
        RoundReview,
        SessionResults,
        Exiting
    }
}
