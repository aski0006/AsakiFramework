using System;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// ALog 到 Unity 控制台的桥接接口。
    /// 用于解决 Asaki.Core 与 Asaki.Unity 之间的循环依赖问题。
    /// </summary>
    /// <remarks>
    /// 实现方式：
    /// 1. Asaki.Core 定义此接口
    /// 2. Asaki.Unity 提供实现并在初始化时注册
    /// 3. ALog 在输出日志时调用已注册的桥接器
    /// </remarks>
    public interface IALogUnityBridge
    {
        /// <summary>
        /// 将日志转发到 Unity 控制台
        /// </summary>
        void ForwardToUnityConsole(
            AsakiLogLevel level,
            string message,
            string payload,
            string callerPath,
            int callerLine,
            Exception exception = null
        );
    }

    /// <summary>
    /// ALog 桥接器管理类
    /// </summary>
    public static class ALogBridgeManager
    {
        private static IALogUnityBridge _bridge;

        /// <summary>
        /// 注册 Unity 控制台桥接器
        /// </summary>
        public static void RegisterBridge(IALogUnityBridge bridge)
        {
            _bridge = bridge;
        }

        /// <summary>
        /// 注销桥接器
        /// </summary>
        public static void UnregisterBridge()
        {
            _bridge = null;
        }

        /// <summary>
        /// 获取当前注册的桥接器
        /// </summary>
        internal static IALogUnityBridge GetBridge()
        {
            return _bridge;
        }

        /// <summary>
        /// 检查是否有桥接器已注册
        /// </summary>
        public static bool HasBridge => _bridge != null;
    }
}
