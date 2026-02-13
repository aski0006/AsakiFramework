using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Unity.Logging;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Debugging
{
    /// <summary>
    /// ALog Unity 控制台桥接器的编辑器菜单和快捷控制
    /// </summary>
    public static class ALogUnityBridgeToggle
    {
        private const string MENU_PATH = "Asaki/ALog Output to Unity Console";
        private const string PREFS_KEY = "Asaki_ALog_OutputToUnityConsole";

        /// <summary>
        /// 在 Asaki 菜单中添加切换项
        /// </summary>
        [MenuItem(MENU_PATH, false, 10)]
        public static void ToggleUnityConsoleOutput()
        {
            bool current = GetEnabledState();
            SetEnabledState(!current);
            Menu.SetChecked(MENU_PATH, !current);
        }

        [MenuItem(MENU_PATH, true)]
        public static bool ToggleUnityConsoleOutputValidate()
        {
            Menu.SetChecked(MENU_PATH, GetEnabledState());
            return true;
        }

        /// <summary>
        /// 初始化菜单状态
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // 延迟初始化，等待配置系统就绪
            EditorApplication.delayCall += () =>
            {
                bool enabled = GetEnabledState();
                ALogUnityBridge.SetEnabled(enabled);
            };
        }

        /// <summary>
        /// 获取当前启用状态（优先从配置读取）
        /// </summary>
        private static bool GetEnabledState()
        {
            // 尝试从配置读取
            if (AsakiContext.TryGet(out AsakiFrameworkSetting config) && config.LogConfig != null)
            {
                return config.LogConfig.OutputToUnityConsole;
            }
            // 回退到 EditorPrefs
            return EditorPrefs.GetBool(PREFS_KEY, true);
        }

        /// <summary>
        /// 设置启用状态
        /// </summary>
        private static void SetEnabledState(bool enabled)
        {
            // 更新运行时状态
            ALogUnityBridge.SetEnabled(enabled);

            // 更新配置
            if (AsakiContext.TryGet(out AsakiFrameworkSetting config) && config.LogConfig != null)
            {
                config.LogConfig.OutputToUnityConsole = enabled;
            }

            // 持久化到 EditorPrefs
            EditorPrefs.SetBool(PREFS_KEY, enabled);

            Debug.Log($"[ALog] Unity Console Output: {(enabled ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 快捷工具栏按钮：打开 LogDashboard
        /// </summary>
        [MenuItem("Asaki/Window/Open Log Dashboard %&L", false, 9)]
        public static void OpenDashboardShortcut()
        {
            AsakiLogDashboard.ShowWindow();
        }
    }
}
