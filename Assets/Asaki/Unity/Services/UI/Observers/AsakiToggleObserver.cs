using Asaki.Core.Reactive;
using UnityEngine.UI;

namespace Asaki.Unity.Services.UI.Observers
{
    public class AsakiToggleObserver : IAsakiObserver<bool>
    {
        private readonly Toggle _toggle;

        public AsakiToggleObserver(Toggle toggle)
        {
            _toggle = toggle;
        }

        public void OnValueChange(bool value)
        {
            if (_toggle != null && _toggle.isOn != value)
            {
                _toggle.isOn = value;
            }
        }
    }
}
