using Asaki.Core.FSM;

namespace Asaki.Plungin.ComboSystem.States
{
    /// <summary>
    /// 连招窗口状态
    /// </summary>
    public class ComboWindowState : AsakiState<AsakiComboController>
    {
        private float _timer;
        private float _duration;

        public void SetDuration(float duration) => _duration = duration;

        public override void OnEnter()
        {
            _timer = 0f;
            Context.OpenComboWindow(_duration);
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            // 检查是否有缓冲的输入
            if (Context.TryConsumeBufferedInput(out var inputTypeId))
            {
                if (Context.TryContinueCombo(inputTypeId))
                    return;
            }

            // 窗口超时
            if (_timer >= _duration)
            {
                Context.ResetCombo();
            }
        }

        public override void OnExit()
        {
            Context.CloseComboWindow();
        }
    }
}
