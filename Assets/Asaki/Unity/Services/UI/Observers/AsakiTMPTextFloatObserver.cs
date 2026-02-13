using System.Text;
using Asaki.Unity.Utils;
using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [TMP专用] 零GC float 绑定器。
    /// 利用 TextMeshPro 的 SetText(StringBuilder) 实现极致性能。
    /// </summary>
    public class AsakiTMPTextFloatObserver : AsakiObserverBase<float, TMP_Text>
    {
        private readonly string _format;
        private readonly string _prefix;
        private readonly string _suffix;

        public AsakiTMPTextFloatObserver(
            TMP_Text target,
            string format = "F1",
            string prefix = "",
            string suffix = ""
        )
            : base(target)
        {
            _format = format;
            _prefix = prefix;
            _suffix = suffix;
            _lastValue = float.NaN;
        }

        protected override bool ShouldUpdate(float value)
        {
            return !UnityEngine.Mathf.Approximately(value, _lastValue);
        }

        protected override void ApplyValue(float value)
        {
            StringBuilder sb = AsakiStringBuilderPool.Rent();
            try
            {
                if (!string.IsNullOrEmpty(_prefix))
                    sb.Append(_prefix);
                sb.Append(value.ToString(_format));
                if (!string.IsNullOrEmpty(_suffix))
                    sb.Append(_suffix);

                _target.SetText(sb);
            }
            finally
            {
                AsakiStringBuilderPool.Return(sb);
            }
        }

        protected override float GetDefaultValue()
        {
            return float.NaN;
        }
    }
}
