using System.Text;
using Asaki.Unity.Utils;
using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [TMP专用] 零GC Int 绑定器。
    /// 利用 TextMeshPro 的 SetText(StringBuilder) 实现极致性能。
    /// </summary>
    public class AsakiTMPTextIntObserver : AsakiObserverBase<int, TMP_Text>
    {
        private readonly string _prefix;
        private readonly string _suffix;

        public AsakiTMPTextIntObserver(TMP_Text target, string prefix = "", string suffix = "")
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
