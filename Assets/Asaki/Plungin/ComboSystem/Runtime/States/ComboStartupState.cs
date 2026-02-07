using Asaki.Core.FSM;

namespace Asaki.Plungin.ComboSystem.States
{
    /// <summary>
    /// 前摇状态
    /// </summary>
    public class ComboStartupState : AsakiState<AsakiComboController>
    {
        private float _timer;
        private ComboMove _move;

        public void SetMove(ComboMove move) => _move = move;

        public override void OnEnter()
        {
            _timer = 0f;
            Context.StartMove(_move);
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            // 检查是否进入判定阶段
            if (_timer >= _move.StartupTime)
            {
                var activeState = Machine.GetState<ComboActiveState>();
                activeState.SetMove(_move);
                Machine.ChangeState<ComboActiveState>();
            }
        }

        /// <summary>
        /// 从状态机缓存获取状态实例
        /// </summary>
    }
}
