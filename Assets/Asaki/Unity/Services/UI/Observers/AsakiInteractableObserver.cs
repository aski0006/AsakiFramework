using Asaki.Core.Reactive;
using UnityEngine.UI;

namespace Asaki.Unity.Services.UI.Observers
{
    public class AsakiInteractableObserver : IAsakiObserver<bool>
    {
        private readonly Selectable _selectable; // Button, Toggle, Slider 都是 Selectable

        public AsakiInteractableObserver(Selectable selectable)
        {
            _selectable = selectable;
        }

        public void OnValueChange(bool value)
        {
            if (_selectable != null && _selectable.interactable != value)
            {
                _selectable.interactable = value;
            }
        }
    }
}
