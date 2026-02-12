using Asaki.Core.FSM;

namespace Asaki.Plugin.ComboSystem.States
{
    /// <summary>
    /// 中断状态
    /// </summary>
    public class ComboInterruptedState : AsakiState<AsakiComboController>
    {
        public override void OnEnter()
        {
            Context.DeactivateHitBoxes();
        }

        public override void OnUpdate(float deltaTime)
        {
            // 中断状态直接返回Idle
            Machine.ChangeState<ComboIdleState>();
        }
    }
}
