using Asaki.Core.MVVM;
using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    public class AsakiInputFieldObserver : IAsakiObserver<string>
    {
        private readonly TMP_InputField _input;

        public AsakiInputFieldObserver(TMP_InputField input)
        {
            _input = input;
        }

        public void OnValueChange(string value)
        {
            if (_input == null)
                return;

            if (_input.text != value)
            {
                _input.text = value ?? "";
            }
        }
    }
}
