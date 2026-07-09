namespace VRShooting.Common
{
    /// <summary>
    /// UI 事件 ID。参见 docs/接口文档/01-页面导航与UI事件.md。
    /// </summary>
    public enum UIEventId
    {
        MainMenu_OpenZeroing = 0,
        MainMenu_OpenMovingTarget,
        MainMenu_OpenTrench,
        MainMenu_OpenUrban,
        MainMenu_OpenArmory,
        MainMenu_OpenSettings,
        Mode_SelectZeroing,
        Mode_SelectMovingTarget,
        Mode_SelectTrench,
        Mode_SelectUrban,
        Mode_Confirm,
        Zeroing_Start,
        Zeroing_ApplyAdjustment,
        Zeroing_NextRound,
        Zeroing_BackToMainMenu,
        Zeroing_Retry,
        Zeroing_BackToModeSelection,
        Common_Back,
        Common_Retry,
        Common_Apply,
        Common_ResetDefault
    }
}
