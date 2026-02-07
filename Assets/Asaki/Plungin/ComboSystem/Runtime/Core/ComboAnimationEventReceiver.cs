using UnityEngine;

namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 动画事件接收器 - 附加在Animator所在GameObject
    /// </summary>
    public class ComboAnimationEventReceiver : MonoBehaviour
    {
        private AsakiComboController _controller;

        public void Initialize(AsakiComboController controller)
        {
            _controller = controller;
        }

        // 由Animation Event调用
        void OnComboEvent(string eventName)
        {
            _controller?.OnAnimationEvent(eventName);
        }

        // 预定义事件
        void OnStartupEnd() => _controller?.OnAnimationEvent("StartupEnd");
        void OnActiveStart() => _controller?.OnAnimationEvent("ActiveStart");
        void OnActiveEnd() => _controller?.OnAnimationEvent("ActiveEnd");
        void OnRecoveryEnd() => _controller?.OnAnimationEvent("RecoveryEnd");
        void OnComboWindowOpen() => _controller?.OnAnimationEvent("ComboWindowOpen");
    }
}
