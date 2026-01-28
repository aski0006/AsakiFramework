using Asaki.Core.MVVM;
using UnityEngine;

namespace Asaki.Unity.Services.UI.Observers
{
    public class AsakiActiveObserver : IAsakiObserver<bool>
    {
        private readonly GameObject _target;

        // 可选：是否反转逻辑 (如 IsDead -> ShowGameOver, 此时不需要反转；IsAlive -> HideGrave，需要反转)
        private readonly bool _invert;

        public AsakiActiveObserver(GameObject target, bool invert = false)
        {
            _target = target;
            _invert = invert;
        }

        public void OnValueChange(bool value)
        {
            if (_target == null)
                return;
            bool activeState = _invert ? !value : value;

            if (_target.activeSelf != activeState)
            {
                _target.SetActive(activeState);
            }
        }
    }
}
