using System.Text;
using Asaki.Unity.Utils;
using TMPro;
using UnityEngine;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [TextMeshPro世界空间专用] 零GC float 绑定器。
    /// 用于世界空间中的 TextMeshPro 组件。
    /// </summary>
    public class AsakiTextMeshProFloatObserver : AsakiObserverBase<float, TextMeshPro>
    {
        private readonly string _format;
        private readonly string _prefix;
        private readonly string _suffix;

        public AsakiTextMeshProFloatObserver(
            TextMeshPro target,
            string format = "F1",
            string prefix = "",
            string suffix = ""
        )
            : base(target)
        {
            _format = format;
            _prefix = prefix;
            _suffix = suffix;
        }

        protected override bool ShouldUpdate(float value)
        {
            return !Mathf.Approximately(value, _lastValue);
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
    }
}
