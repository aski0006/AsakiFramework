using System.Text;
using Asaki.Core.MVVM;
using Asaki.Unity.Utils;
using TMPro;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [TMP专用] 零GC Int 绑定器。
    /// 利用 TextMeshPro 的 SetText(StringBuilder) 实现极致性能。
    /// </summary>
    public class AsakiTMPTextIntObserver : IAsakiObserver<int>
    {
        private readonly TMP_Text _target;
        private readonly string _prefix;
        private readonly string _suffix;
        private int _lastValue = int.MinValue;

        public AsakiTMPTextIntObserver(TMP_Text target, string prefix = "", string suffix = "")
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

            // 1. 借出 Builder
            StringBuilder sb = AsakiStringBuilderPool.Rent();

            // 2. 拼接
            if (!string.IsNullOrEmpty(_prefix))
                sb.Append(_prefix);
            sb.Append(value);
            if (!string.IsNullOrEmpty(_suffix))
                sb.Append(_suffix);

            // 3. 直接喂给 TextMeshPro (这是 TextMeshPro 独有的 ZeroGC API)
            _target.SetText(sb);

            // 4. 归还
            AsakiStringBuilderPool.Return(sb);
        }
    }
}
