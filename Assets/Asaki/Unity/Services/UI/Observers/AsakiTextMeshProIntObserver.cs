using System.Text;
using Asaki.Core.MVVM;
using Asaki.Unity.Utils;
using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    public class AsakiTextMeshProIntObserver : IAsakiObserver<int>
    {
        private readonly TextMeshPro _target;
        private readonly string _prefix;
        private readonly string _suffix;
        private int _lastValue = int.MinValue;

        public AsakiTextMeshProIntObserver(
            TextMeshPro target,
            string prefix = "",
            string suffix = ""
        )
        {
            _target = target;
            _prefix = prefix;
            _suffix = suffix;
        }

        public void OnValueChange(int value)
        {
            if (!_target)
                return;
            if (value == _lastValue)
                return; // 脏检查
            _lastValue = value;
            StringBuilder sb = AsakiStringBuilderPool.Rent();
            if (!string.IsNullOrEmpty(_prefix))
                sb.Append(_prefix);
            sb.Append(value);
            if (!string.IsNullOrEmpty(_suffix))
                sb.Append(_suffix);
            _target.SetText(sb);
            AsakiStringBuilderPool.Return(sb);
        }
    }
}
