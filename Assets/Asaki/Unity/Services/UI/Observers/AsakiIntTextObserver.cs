using System.Text;
using Asaki.Core.MVVM;
using Asaki.Unity.Utils;
using UnityEngine.UI;

// 如果使用 TextMeshPro，请引用 TMPro

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [性能组件] 专门用于将 int 属性绑定到 Text 组件。
    /// 避免了 "Prefix" + value + "Suffix" 拼接产生的垃圾。
    /// </summary>
    public class AsakiIntTextObserver : IAsakiObserver<int>
    {
        private readonly Text _targetText;
        private readonly string _prefix;
        private readonly string _suffix;

        private int _lastValue = int.MinValue;

        public AsakiIntTextObserver(Text targetText, string prefix = "", string suffix = "")
        {
            _targetText = targetText;
            _prefix = prefix;
            _suffix = suffix;
        }

        public void OnValueChange(int value)
        {
            if (_targetText == null)
                return; // 防止 UI 被销毁后报错

            // 脏检查：如果 UI 上已经是这个数字，就不动它 (节省 Canvas Rebuild)
            if (value == _lastValue)
                return;
            _lastValue = value;

            // 1. 从池中借出 StringBuilder
            StringBuilder sb = AsakiStringBuilderPool.Rent();

            // 2. 拼接 (Append(int) 是无 GC 的)
            if (!string.IsNullOrEmpty(_prefix))
                sb.Append(_prefix);
            sb.Append(value);
            if (!string.IsNullOrEmpty(_suffix))
                sb.Append(_suffix);

            // 3. 赋值并归还
            // 注意：对于 Legacy Text，ToString() 依然会产生一个 String 对象的 GC，
            // 但这已经是原生组件的极限。如果是 TextMeshPro，可以使用 _tmp.SetText(sb); 达到真·ZeroGC。
            _targetText.text = AsakiStringBuilderPool.GetStringAndRelease(sb);
        }
    }
}
