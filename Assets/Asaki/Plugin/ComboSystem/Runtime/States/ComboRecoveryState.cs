using Asaki.Core.FSM;

namespace Asaki.Plugin.ComboSystem.States
{
    /// <summary>
    /// 后摇状态
    /// </summary>
    public class ComboRecoveryState : AsakiState<AsakiComboController>
    {
        private float _timer;
        private ComboMove _move;

        public void SetMove(ComboMove move) => _move = move;

        public override void OnEnter()
        {
            _timer = 0f;
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            // 检查是否进入连招窗口
            if (_timer >= _move.RecoveryTime)
            {
                var windowState = Machine.GetState<ComboWindowState>();
                float windowDuration = _move.ComboWindowEnd - _move.ComboWindowStart;
                windowState.SetDuration(windowDuration);
                Machine.ChangeState<ComboWindowState>();
            }
        }
    }
}
