using System.Text;
using Asaki.Core.MVVM;
using Asaki.Unity.Utils;
using TMPro;
using UnityEngine;

namespace Asaki.Unity.Services.UI.Observers
{
    public class AsakiTextMeshProFloatObserver : IAsakiObserver<float>
    {
        private readonly TextMeshPro _target;
        private readonly string _format;
        private readonly string _prefix;
        private readonly string _suffix;
        private float _lastValue = float.MinValue;

        public AsakiTextMeshProFloatObserver(
            TextMeshPro target,
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
            if (Mathf.Approximately(value, _lastValue))
                return; // 脏检查
            _lastValue = value;

            // 1. 借出 Builder
            StringBuilder sb = AsakiStringBuilderPool.Rent();

            // 2. 拼接
            if (!string.IsNullOrEmpty(_prefix))
                sb.Append(_prefix);
            sb.Append(value.ToString(_format));
            if (!string.IsNullOrEmpty(_suffix))
                sb.Append(_suffix);

            _target.SetText(sb);
        }
    }
}
