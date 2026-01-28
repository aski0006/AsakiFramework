using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Resources;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Extensions
{
    /// <summary>
    /// [资源流扩展] AsakiResFlow
    /// 将 Resources 的加载操作直接绑定到 MonoBehaviour 的生命周期。
    /// </summary>
    public static class AsakiResFlowExtensions
    {
        // =========================================================
        // 1. 生命周期感知的加载 (Lifecycle-Aware Loading)
        // =========================================================

        /// <summary>
        /// 加载资源，并自动绑定到 context 的生命周期。
        /// <para>如果 context (MonoBehaviour) 在加载完成前被销毁，任务将抛出 OperationCanceledException。</para>
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="service">资源服务</param>
        /// <param name="location">资源地址</param>
        /// <param name="context">上下文 (通常是 this)</param>
        public static UniTask<ResHandle<T>> LoadAsync<T>(
            this IAsakiResourceService service,
            string location,
            MonoBehaviour context
        )
            where T : class
        {
            // 自动获取 AsakiFlow 提供的生命周期 Token
            CancellationToken token = context.GetToken();
            return service.LoadAsync<T>(location, token);
        }

        /// <summary>
        /// 批量加载资源，并自动绑定到 context 的生命周期。
        /// </summary>
        public static UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            this IAsakiResourceService service,
            IEnumerable<string> locations,
            MonoBehaviour context
        )
            where T : class
        {
            CancellationToken token = context.GetToken();
            return service.LoadBatchAsync<T>(locations, token);
        }

        public static UniTask<ResHandle<T>> LoadAsync<T>(
            this IAsakiResourceService service,
            string location,
            Action<float> onProgress, // 新增参数
            MonoBehaviour context
        )
            where T : class
        {
            CancellationToken token = context.GetToken();
            return service.LoadAsync<T>(location, onProgress, token);
        }

        public static UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            this IAsakiResourceService service,
            IEnumerable<string> locations,
            Action<float> onProgress, // 新增参数
            MonoBehaviour context
        )
            where T : class
        {
            CancellationToken token = context.GetToken();
            return service.LoadBatchAsync<T>(locations, onProgress, token);
        }
    }
}
