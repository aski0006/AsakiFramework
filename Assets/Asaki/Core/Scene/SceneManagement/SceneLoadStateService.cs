using System;
using Asaki.Core.Logging;

namespace Asaki.Core.Scene.SceneManagement
{
    /// <summary>
    /// 场景加载状态服务
    /// 用于跨场景传递场景加载参数
    /// </summary>
    /// <remarks>
    /// [已废弃] 请使用 IAsakiSceneManagerService.CurrentPayload 替代。
    /// 此静态服务在下一版本中将移除。
    /// </remarks>
    [Obsolete(
        "Use IAsakiSceneManagerService.CurrentPayload instead. This class will be removed in a future version."
    )]
    public static class SceneLoadStateService
    {
        private static SceneLoadPayload _currentPayload;
        private static bool _hasPayload;

        /// <summary>
        /// 是否有待处理的场景加载参数
        /// </summary>
        public static bool HasPayload => _hasPayload;

        /// <summary>
        /// 获取当前场景加载参数（获取后自动清空）
        /// </summary>
        public static SceneLoadPayload GetPayload()
        {
            if (!_hasPayload)
            {
                ALog.Warn("[SceneLoadStateService] No payload available");
                return null;
            }

            var payload = _currentPayload;
            ClearPayload();
            return payload;
        }

        /// <summary>
        /// 设置场景加载参数
        /// </summary>
        public static void SetPayload(SceneLoadPayload payload)
        {
            _currentPayload = payload;
            _hasPayload = payload != null;

            if (_hasPayload)
            {
                ALog.Info(
                    $"[SceneLoadStateService] Payload set for scene: {payload.TargetSceneName}"
                );
            }
        }

        /// <summary>
        /// 清空场景加载参数
        /// </summary>
        public static void ClearPayload()
        {
            _currentPayload = null;
            _hasPayload = false;
        }

        /// <summary>
        /// 查看当前参数（不清空）
        /// </summary>
        public static SceneLoadPayload PeekPayload()
        {
            return _hasPayload ? _currentPayload : null;
        }
    }
}
