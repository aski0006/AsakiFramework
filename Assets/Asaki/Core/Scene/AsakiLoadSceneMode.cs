namespace Asaki.Core.Scene
{
    public enum AsakiLoadSceneMode
    {
        Single,
        Additive,
    }

    public enum AsakiSceneActivation
    {
        Immediate, // 加载完立即激活
        ManualConfirm, // 等待手动确认 (用于 "按任意键继续")
    }
}
