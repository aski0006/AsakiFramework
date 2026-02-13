using UnityEngine;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// 将 bool 属性绑定到 GameObject 的激活状态。
    /// </summary>
    public class AsakiActiveObserver : AsakiObserverBase<bool, GameObject>
    {
        private readonly bool _invert;

        public AsakiActiveObserver(GameObject target, bool invert = false)
            : base(target)
        {
            _invert = invert;
        }

        protected override void ApplyValue(bool value)
        {
            bool activeState = _invert ? !value : value;
            if (_target.activeSelf != activeState)
            {
                _target.SetActive(activeState);
            }
        }
    }
}
