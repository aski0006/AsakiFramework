using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// 将 string 属性绑定到 TMP_InputField。
    /// </summary>
    public class AsakiInputFieldObserver : AsakiObserverBase<string, TMP_InputField>
    {
        public AsakiInputFieldObserver(TMP_InputField input)
            : base(input) { }

        protected override void ApplyValue(string value)
        {
            if (_target.text != value)
            {
                _target.text = value ?? "";
            }
        }

        protected override string GetDefaultValue()
        {
            return null;
        }
    }
}
