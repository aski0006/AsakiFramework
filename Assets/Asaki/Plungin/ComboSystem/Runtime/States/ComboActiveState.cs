using Asaki.Core.FSM;

namespace Asaki.Plungin.ComboSystem.States
{
    /// <summary>
    /// 攻击判定状态
    /// </summary>
    public class ComboActiveState : AsakiState<AsakiComboController>
    {
        private float _timer;
        private ComboMove _move;

        public void SetMove(ComboMove move) => _move = move;

        public override void OnEnter()
        {
            _timer = 0f;
            Context.ActivateHitBoxes();
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            // 检查是否进入后摇
            if (_timer >= _move.ActiveDuration)
            {
                var recoveryState = Machine.GetState<ComboRecoveryState>();
                recoveryState.SetMove(_move);
                Machine.ChangeState<ComboRecoveryState>();
            }
        }

        public override void OnExit()
        {
            Context.DeactivateHitBoxes();
        }
    }
}
