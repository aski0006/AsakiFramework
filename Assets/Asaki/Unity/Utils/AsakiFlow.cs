using System.Threading;
using Asaki.Core.Async;
using UnityEngine;

namespace Asaki.Unity.Utils
{
    /// <summary>
    /// [流控制核心] AsakiFlow
    /// 负责连接 Unity 生命周期与 Asaki 异步系统，消除 "Token Hell"。
    /// </summary>
    public static class AsakiFlow
    {
        // =========================================================
        // 1. 生命周期绑定 (Lifecycle Binding)
        // =========================================================

        /// <summary>
        /// 获取当前 MonoBehaviour 的生命周期 Token。
        /// <para>当 GameObject 被销毁时，此 Token 会自动取消。</para>
        /// <para>兼容性：自动适配 Unity 2022.2+ 原生 API，旧版本自动挂载追踪器。</para>
        /// </summary>
        public static CancellationToken GetToken(this MonoBehaviour self)
        {
            if (self == null)
                return CancellationToken.None;

#if UNITY_2022_2_OR_NEWER
            // Unity 2022.2+ 原生支持
            return self.destroyCancellationToken;
#else
            // Unity 2021.3 LTS 兼容方案
            var tracker = self.GetComponent<AsakiLifecycleTracker>();
            if (tracker == null)
            {
                // 懒加载：只有在请求 Token 时才添加追踪组件
                tracker = self.gameObject.AddComponent<AsakiLifecycleTracker>();

                // 隐藏组件，保持 Inspector 干净
                tracker.hideFlags = HideFlags.HideInInspector;
            }
            return tracker.Token;
#endif
        }

        // =========================================================
        // 2. 混合链接 (Hybrid Linking)
        // =========================================================

        /// <summary>
        /// [核心语法糖] 创建一个 "双重保险" 的 Token。
        /// <para>取消条件：Service 被重置 OR 目标 Component 被销毁。</para>
        /// <para>用法：await _routine.WaitSeconds(1f, _routine.Link(this));</para>
        /// </summary>
        /// <param name="service">异步服务实例</param>
        /// <param name="component">绑定的 Unity 组件</param>
        public static CancellationToken Link(
            this IAsakiAsyncService service,
            MonoBehaviour component
        )
        {
            CancellationToken componentToken = component.GetToken();

            // 利用 Service 现有的 CreateLinkedToken 方法
            // 它会自动连接 Service 自身的全局 CTS
            return service.CreateLinkedToken(componentToken);
        }

        /// <summary>
        /// 链接另一个外部 Token (通常用于 UI 窗口关闭按钮)
        /// </summary>
        public static CancellationToken Link(
            this IAsakiAsyncService service,
            CancellationToken externalToken
        )
        {
            return service.CreateLinkedToken(externalToken);
        }

        public static CancellationToken Link(
            this IAsakiAsyncService service,
            MonoBehaviour component,
            CancellationToken additionalToken
        )
        {
            CancellationToken componentToken = component.GetToken();
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                componentToken,
                additionalToken
            );
            return service.CreateLinkedToken(linkedSource.Token);
        }

        public static CancellationToken Link(
            this IAsakiAsyncService service,
            params CancellationToken[] tokens
        )
        {
            if (tokens == null || tokens.Length == 0)
                return service.CreateLinkedToken();

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                tokens
            );
            return service.CreateLinkedToken(linkedSource.Token);
        }

        public static CancellationToken Link(
            this IAsakiAsyncService service,
            MonoBehaviour component,
            params CancellationToken[] others
        )
        {
            CancellationToken componentToken = component.GetToken();

            // 构建新的数组包含 componentToken
            var allTokens = new CancellationToken[others.Length + 1];
            allTokens[0] = componentToken;
            System.Array.Copy(others, 0, allTokens, 1, others.Length);

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                allTokens
            );
            return service.CreateLinkedToken(linkedSource.Token);
        }
    }
}
