using Asaki.Core.Resources;
using UnityEngine;

namespace Asaki.Core.UI
{
    /// <summary>
    /// 结构体适配器 (ZeroGC)，将 ResHandle<GameObject> 适配给 IUIResourceHandle
    /// </summary>
    public struct AsakiUIResourceHandleAdapter : IUIResourceHandle
    {
        private ResHandle<GameObject> _handle;
        private bool _isMarkedForRelease;

        public AsakiUIResourceHandleAdapter(ResHandle<GameObject> handle)
        {
            _handle = handle;
            _isMarkedForRelease = false;
        }

        /// <summary>
        /// 是否有效（句柄有效且未被标记为待释放）
        /// </summary>
        public bool IsValid => _handle is { IsValid: true } && !_isMarkedForRelease;

        /// <summary>
        /// 句柄是否仍持有资源（即使被标记为待释放）
        /// </summary>
        public bool HasResource => _handle is { IsValid: true };

        // 获取原始资源 (仅 Unity 层可见)
        public GameObject Asset => _handle?.Asset;

        /// <summary>
        /// 资源位置标识
        /// </summary>
        public string Location => _handle?.Location;

        /// <summary>
        /// 是否已被标记为待释放
        /// </summary>
        public bool IsMarkedForRelease => _isMarkedForRelease;

        /// <summary>
        /// 标记为待释放（延迟释放机制使用）
        /// </summary>
        public void MarkForRelease()
        {
            _isMarkedForRelease = true;
        }

        /// <summary>
        /// 取消待释放标记（当资源被复用时调用）
        /// </summary>
        public void UnmarkForRelease()
        {
            _isMarkedForRelease = false;
        }

        public void Dispose()
        {
            _handle?.Dispose();
            _handle = null;
            _isMarkedForRelease = false;
        }
    }
}
