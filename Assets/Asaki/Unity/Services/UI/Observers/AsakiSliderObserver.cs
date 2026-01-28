using Asaki.Core.MVVM;
using UnityEngine.UI;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [Slider专用] 将 float 属性绑定到进度条。
    /// </summary>
    public class AsakiSliderObserver : IAsakiObserver<float>
    {
        private readonly Slider _slider;

        public AsakiSliderObserver(Slider slider)
        {
            _slider = slider;
        }

        public void OnValueChange(float value)
        {
            if (_slider == null)
                return;
            if (!UnityEngine.Mathf.Approximately(_slider.value, value))
            {
                _slider.value = value;
            }
        }
    }
}
