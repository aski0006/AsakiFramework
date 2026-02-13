using System.Collections.Generic;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Core.UI;
using UnityEngine;

namespace Asaki.Unity.Services.UI
{
    /// <summary>
    /// UI资源管理器，负责延迟释放资源和资源复用。
    /// </summary>
    public class UIResourceManager
    {
        private class PendingReleaseInfo
        {
            public string AssetPath;
            public AsakiUIResourceHandleAdapter Handle;
            public float RemainingSeconds;
        }

        private readonly Dictionary<string, PendingReleaseInfo> _pendingReleaseHandles =
            new Dictionary<string, PendingReleaseInfo>();

        private readonly float _defaultReleaseDelay;

        public UIResourceManager(float defaultReleaseDelay = 0f)
        {
            _defaultReleaseDelay = defaultReleaseDelay;
        }

        /// <summary>
        /// 尝试获取可复用的待释放资源句柄。
        /// </summary>
        public bool TryGetReusableHandle(string assetPath, out AsakiUIResourceHandleAdapter handle)
        {
            handle = default;
            if (_pendingReleaseHandles.TryGetValue(assetPath, out var info))
            {
                handle = info.Handle;
                _pendingReleaseHandles.Remove(assetPath);
                ALog.Trace($"[AsakiUI] Reuse deferred resources: {assetPath}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将资源加入延迟释放队列。
        /// </summary>
        public void ScheduleRelease(
            string assetPath,
            AsakiUIResourceHandleAdapter handle,
            float? delaySeconds = null
        )
        {
            float delay = delaySeconds ?? _defaultReleaseDelay;

            if (delay > 0)
            {
                if (_pendingReleaseHandles.TryGetValue(assetPath, out var existingInfo))
                {
                    existingInfo.Handle.Dispose();
                    _pendingReleaseHandles.Remove(assetPath);
                }

                _pendingReleaseHandles[assetPath] = new PendingReleaseInfo
                {
                    AssetPath = assetPath,
                    Handle = handle,
                    RemainingSeconds = delay,
                };

                ALog.Trace(
                    $"[AsakiUI] The resource enters the delayed release queue: {assetPath}, delay: {delay}s"
                );
            }
            else
            {
                handle.Dispose();
            }
        }

        /// <summary>
        /// 处理延迟释放的资源，每帧调用。
        /// </summary>
        public void ProcessDelayedRelease(float deltaTime)
        {
            if (_pendingReleaseHandles.Count == 0)
                return;

            var keysToRemove = new List<string>();

            foreach (var kvp in _pendingReleaseHandles)
            {
                var info = kvp.Value;
                info.RemainingSeconds -= deltaTime;

                if (info.RemainingSeconds <= 0)
                {
                    ALog.Trace(
                        $"[AsakiUI] Delay the expiration of the released resource: {info.AssetPath}"
                    );
                    info.Handle.Dispose();
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                _pendingReleaseHandles.Remove(key);
            }
        }

        /// <summary>
        /// 立即释放所有待释放资源。
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var kvp in _pendingReleaseHandles)
            {
                kvp.Value.Handle.Dispose();
            }
            _pendingReleaseHandles.Clear();
        }
    }
}
