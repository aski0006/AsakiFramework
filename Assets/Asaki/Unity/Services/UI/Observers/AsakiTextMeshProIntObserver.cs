using System.Text;
using Asaki.Unity.Utils;
using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [TextMeshPro世界空间专用] 零GC Int 绑定器。
    /// 用于世界空间中的 TextMeshPro 组件。
    /// </summary>
    public class AsakiTextMeshProIntObserver : AsakiObserverBase<int, TextMeshPro>
    {
        private readonly string _prefix;
        private readonly string _suffix;

        public AsakiTextMeshProIntObserver(
            TextMeshPro target,
            string prefix = "",
            string suffix = ""
        )
            : base(target)
        {
            _prefix = prefix;
            _suffix = suffix;
        }

        protected override void ApplyValue(int value)
        {
            StringBuilder sb = AsakiStringBuilderPool.Rent();
            try
            {
                if (!string.IsNullOrEmpty(_prefix))
                    sb.Append(_prefix);
                sb.Append(value);
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
