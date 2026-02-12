using Asaki.Core.FSM;

namespace Asaki.Plugin.ComboSystem.States
{
    /// <summary>
    /// 待机状态
    /// </summary>
    public class ComboIdleState : AsakiState<AsakiComboController>
    {
        public override void OnEnter()
        {
            Context.DeactivateHitBoxes();
        }

        public override void OnUpdate(float deltaTime)
        {
            // 检查是否有缓冲的输入
            if (Context.TryConsumeBufferedInput(out var inputTypeId))
            {
                Context.ProcessAttackInput(inputTypeId);
            }
        }
    }
}
