using UnityEngine;
using UnityEngine.UI;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// 将 float 属性绑定到 Slider。
    /// </summary>
    public class AsakiSliderObserver : AsakiObserverBase<float, Slider>
    {
        public AsakiSliderObserver(Slider slider)
            : base(slider)
        {
            _lastValue = float.NaN;
        }

        protected override bool ShouldUpdate(float value)
        {
            return !Mathf.Approximately(value, _lastValue);
        }

        protected override void ApplyValue(float value)
        {
            _target.value = value;
        }

        protected override float GetDefaultValue()
        {
            return float.NaN;
        }
    }
}
