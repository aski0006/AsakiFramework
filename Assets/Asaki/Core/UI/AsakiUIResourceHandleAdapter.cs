using Asaki.Core.Resources;
using UnityEngine;

namespace Asaki.Core.UI
{
    /// <summary>
    /// 结构体适配器 (ZeroGC)，将 ResHandle<GameObject> 适配给 IAsakiUIResourceHandle
    /// </summary>
    public struct AsakiUIResourceHandleAdapter : IAsakiUIResourceHandle
    {
        private ResHandle<GameObject> _handle;
        private bool _isDisposed;

        public AsakiUIResourceHandleAdapter(ResHandle<GameObject> handle)
        {
            _handle = handle;
            _isDisposed = false;
        }

        /// <summary>
        /// 是否有效（句柄有效且未被释放）
        /// </summary>
        public bool IsValid => _handle is { IsValid: true } && !_isDisposed;

        /// <summary>
        /// 句柄是否仍持有资源（即使被标记为待释放）
        /// </summary>
        public bool HasResource => _handle is { IsValid: true } && !_isDisposed;

        // 获取原始资源 (仅 Unity 层可见)
        public GameObject Asset => _handle?.Asset;

        /// <summary>
        /// 资源位置标识
        /// </summary>
        public string Location => _handle?.Location;

        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed => _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _handle?.Dispose();
            _handle = null;
            _isDisposed = true;
        }
    }
}
