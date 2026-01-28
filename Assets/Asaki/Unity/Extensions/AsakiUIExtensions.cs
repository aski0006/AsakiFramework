using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.MVVM;
using Asaki.Core.UI;
using Asaki.Generated;
using Asaki.Unity.Services.UI.Observers;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Asaki.Unity.Extensions
{
    public static class AsakiUIExtensions
    {
        // ===========================================
        // 1. 文本显示 (Display)
        // ===========================================

        public static IAsakiObserver<int> BindTo(
            this AsakiProperty<int> property,
            TextMeshPro text,
            string prefix = "",
            string suffix = ""
        )
        {
            AsakiTextMeshProIntObserver observer = new AsakiTextMeshProIntObserver(
                text,
                prefix,
                suffix
            );
            property.Bind(observer);
            return observer;
        }

        public static IAsakiObserver<float> BindTo(
            this AsakiProperty<float> property,
            TextMeshPro text,
            string format = "F1",
            string prefix = "",
            string suffix = ""
        )
        {
            AsakiTextMeshProFloatObserver observer = new AsakiTextMeshProFloatObserver(
                text,
                format,
                prefix,
                suffix
            );
            property.Bind(observer);
            return observer;
        }

        /// <summary>
        /// 绑定 int 到 TextMeshPro (ZeroGC)
        /// </summary>
        public static IAsakiObserver<int> BindTo(
            this AsakiProperty<int> property,
            TMP_Text text,
            string prefix = "",
            string suffix = ""
        )
        {
            AsakiTMPTextIntObserver observer = new AsakiTMPTextIntObserver(text, prefix, suffix);
            property.Bind(observer);
            return observer;
        }

        /// <summary>
        /// [新增] 绑定 float 到 TextMeshPro (支持格式化，如 "F2")
        /// </summary>
        public static IAsakiObserver<float> BindTo(
            this AsakiProperty<float> property,
            TMP_Text text,
            string format = "F1",
            string prefix = "",
            string suffix = ""
        )
        {
            AsakiTMPTextFloatObserver observer = new AsakiTMPTextFloatObserver(
                text,
                format,
                prefix,
                suffix
            );
            property.Bind(observer);
            return observer;
        }

        /// <summary>
        /// [新增] 绑定 string 到 TextMeshPro
        /// </summary>
        public static void BindTo(this AsakiProperty<string> property, TMP_Text text)
        {
            property.Subscribe(val =>
            {
                if (text != null)
                    text.text = val;
            });
        }

        /// <summary>
        /// Legacy Text 绑定 (保留兼容性)
        /// </summary>
        public static IAsakiObserver<int> BindTo(
            this AsakiProperty<int> property,
            Text text,
            string prefix = "",
            string suffix = ""
        )
        {
            AsakiIntTextObserver observer = new AsakiIntTextObserver(text, prefix, suffix);
            property.Bind(observer);
            return observer;
        }

        // ===========================================
        // 2. 输入控件 (Input)
        // ===========================================

        /// <summary>
        /// [双向绑定] 绑定 string 到 TMP_InputField
        /// </summary>
        public static IAsakiObserver<string> BindTwoWay(
            this AsakiProperty<string> property,
            TMP_InputField input
        )
        {
            AsakiInputFieldObserver observer = new AsakiInputFieldObserver(input);
            property.Bind(observer);

            // UI -> Data
            input.onValueChanged.AddListener(val => property.Value = val);
            return observer;
        }

        /// <summary>
        /// [双向绑定] 绑定 int 到 TMP_Dropdown (下拉菜单)
        /// </summary>
        public static void BindTwoWay(this AsakiProperty<int> property, TMP_Dropdown dropdown)
        {
            // Data -> UI (使用 Lambda 简化，Dropdown 频率不高)
            property.Subscribe(val =>
            {
                if (dropdown != null && dropdown.value != val)
                    dropdown.value = val;
            });

            // UI -> Data
            dropdown.onValueChanged.AddListener(val => property.Value = val);
        }

        // ===========================================
        // 3. 数值控件 (Slider/Toggle)
        // ===========================================

        public static IAsakiObserver<float> BindTo(
            this AsakiProperty<float> property,
            Slider slider
        )
        {
            AsakiSliderObserver observer = new AsakiSliderObserver(slider);
            property.Bind(observer);
            return observer;
        }

        public static IAsakiObserver<float> BindTwoWay(
            this AsakiProperty<float> property,
            Slider slider
        )
        {
            var observer = BindTo(property, slider);
            slider.onValueChanged.AddListener(val => property.Value = val);
            return observer;
        }

        public static IAsakiObserver<bool> BindTo(this AsakiProperty<bool> property, Toggle toggle)
        {
            AsakiToggleObserver observer = new AsakiToggleObserver(toggle);
            property.Bind(observer);
            return observer;
        }

        public static IAsakiObserver<bool> BindTwoWay(
            this AsakiProperty<bool> property,
            Toggle toggle
        )
        {
            var observer = BindTo(property, toggle);
            toggle.onValueChanged.AddListener(val => property.Value = val);
            return observer;
        }

        // ===========================================
        // 4. 状态与显隐 (State & Visibility)
        // ===========================================

        /// <summary>
        /// [新增] 绑定 bool 到 Button 的 interactable 属性
        /// 场景：金币不足时，按钮变灰
        /// </summary>
        public static IAsakiObserver<bool> BindInteractable(
            this AsakiProperty<bool> property,
            Selectable selectable
        )
        {
            AsakiInteractableObserver observer = new AsakiInteractableObserver(selectable);
            property.Bind(observer);
            return observer;
        }

        /// <summary>
        /// [新增] 绑定 bool 到 GameObject 的显隐 (SetActive)
        /// 场景：当 IsDead 为 true 时，显示 GameOverPanel
        /// </summary>
        /// <param name="invert">是否反转逻辑 (true: property为true时隐藏物体)</param>
        public static IAsakiObserver<bool> BindActive(
            this AsakiProperty<bool> property,
            GameObject target,
            bool invert = false
        )
        {
            AsakiActiveObserver observer = new AsakiActiveObserver(target, invert);
            property.Bind(observer);
            return observer;
        }

        /// <summary>
        /// [新增] 绑定 bool 到 CanvasGroup 的 alpha (1 或 0) 和 blocksRaycasts
        /// 场景：比 SetActive 更高性能的软隐藏
        /// </summary>
        public static void BindCanvasGroup(this AsakiProperty<bool> property, CanvasGroup group)
        {
            property.Subscribe(show =>
            {
                if (group == null)
                    return;
                group.alpha = show ? 1f : 0f;
                group.interactable = show;
                group.blocksRaycasts = show;
            });
        }

        public static UniTask<T> OpenAsync<T>(
            this IAsakiUIService service,
            WindowAssetId assetId,
            object args = null,
            CancellationToken token = default(CancellationToken)
        )
            where T : class, IAsakiWindow
        {
            // 核心逻辑：在这里进行枚举到 int 的强转
            return service.OpenAsync<T>((int)assetId, args, token);
        }
    }
}
