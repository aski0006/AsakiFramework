using System.Text;
using Asaki.Unity.Utils;
using UnityEngine.UI;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// [性能组件] 专门用于将 int 属性绑定到 Text 组件。
    /// 避免了 "Prefix" + value + "Suffix" 拼接产生的垃圾。
    /// </summary>
    public class AsakiIntTextObserver : AsakiObserverBase<int, Text>
    {
        private readonly string _prefix;
        private readonly string _suffix;

        public AsakiIntTextObserver(Text targetText, string prefix = "", string suffix = "")
            : base(targetText)
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

                _target.text = AsakiStringBuilderPool.GetStringAndRelease(sb);
            }
            finally
            {
                if (sb.Length > 0)
                    AsakiStringBuilderPool.Return(sb);
            }
        }
    }
}
