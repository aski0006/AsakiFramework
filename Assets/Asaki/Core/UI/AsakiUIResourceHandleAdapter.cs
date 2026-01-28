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

        public AsakiUIResourceHandleAdapter(ResHandle<GameObject> handle)
        {
            _handle = handle;
        }

        public bool IsValid => _handle is { IsValid: true };

        // 获取原始资源 (仅 Unity 层可见)
        public GameObject Asset => _handle?.Asset;

        public void Dispose()
        {
            _handle?.Dispose();
            _handle = null;
        }
    }
}
