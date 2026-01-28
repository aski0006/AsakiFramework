#if !UNITY_2022_2_OR_NEWER
using System.Threading;
using UnityEngine;

namespace Asaki.Unity.Bridge
{
    /// <summary>
    /// [内部组件] 用于在 Unity 2021 及更早版本中模拟 destroyCancellationToken。
    /// <para>开发者不应手动添加此组件，它由 AsakiFlow 自动管理。</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class AsakiLifecycleTracker : MonoBehaviour
    {
        private CancellationTokenSource _cts;
        private bool _isDisposed;

        public CancellationToken Token
        {
            get
            {
                if (_isDisposed)
                    return CancellationToken.None;
                if (_cts == null)
                    _cts = new CancellationTokenSource();
                return _cts.Token;
            }
        }

        private void OnDestroy()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _isDisposed = true;
        }
    }
}
#endif
