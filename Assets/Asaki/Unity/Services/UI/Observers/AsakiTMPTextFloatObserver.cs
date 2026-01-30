using System.Text;
using Asaki.Core.Reactive;
using Asaki.Unity.Utils;
using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    public class AsakiTMPTextFloatObserver : IAsakiObserver<float>
    {
        private readonly TMP_Text _target;
        private readonly string _format; // 例如 "F1", "0.00"
        private readonly string _prefix;
        private readonly string _suffix;
        private float _lastValue = float.NaN; // 使用 NaN 确保第一次一定更新

        public AsakiTMPTextFloatObserver(
            TMP_Text target,
            string format = "F1",
            string prefix = "",
            string suffix = ""
        )
        {
            _target = target;
            _format = format;
            _prefix = prefix;
            _suffix = suffix;
        }

        public void OnValueChange(float value)
        {
            if (!_target)
                return;
            if (UnityEngine.Mathf.Approximately(_lastValue, value))
                return;
            _lastValue = value;

            StringBuilder sb = AsakiStringBuilderPool.Rent();
            if (!string.IsNullOrEmpty(_prefix))
                sb.Append(_prefix);

            sb.Append(value.ToString(_format));

            if (!string.IsNullOrEmpty(_suffix))
                sb.Append(_suffix);

            _target.SetText(sb);
            AsakiStringBuilderPool.Return(sb);
        }
    }
}
