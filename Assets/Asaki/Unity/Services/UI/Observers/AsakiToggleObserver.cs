using UnityEngine.UI;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// 将 bool 属性绑定到 Toggle。
    /// </summary>
    public class AsakiToggleObserver : AsakiObserverBase<bool, Toggle>
    {
        public AsakiToggleObserver(Toggle toggle)
            : base(toggle) { }

        protected override void ApplyValue(bool value)
        {
            if (_target.isOn != value)
            {
                _target.isOn = value;
            }
        }
    }
}
