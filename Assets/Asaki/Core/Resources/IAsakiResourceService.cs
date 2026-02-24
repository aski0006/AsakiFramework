using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Resources
{
    /// <summary>
    /// 资源句柄
    /// <para>封装已加载资源的引用，实现IDisposable模式支持using语句自动释放。</para>
    /// <para>支持隐式转换，可直接当作资源实例使用。</para>
    /// </summary>
    /// <typeparam name="T">资源类型，必须为class约束</typeparam>
    /// <example>
    /// <code>
    /// // 使用using语句自动释放
    /// using (var handle = await resourceService.LoadAsync&lt;GameObject&gt;("Prefabs/Player"))
    /// {
    ///     Instantiate(handle.Asset);
    /// } // 自动调用Release
    ///
    /// // 隐式转换
    /// using (var handle = await resourceService.LoadAsync&lt;GameObject&gt;("Prefabs/Enemy"))
    /// {
    ///     Instantiate(handle); // 隐式转换为GameObject
    /// }
    /// </code>
    /// </example>
    public class ResHandle<T> : IDisposable
        where T : class
    {
        private readonly IAsakiResourceService _service;

        /// <summary>
        /// 资源定位地址
        /// </summary>
        public readonly string Location;

        /// <summary>
        /// 加载的资源实例
        /// </summary>
        public readonly T Asset;

        /// <summary>
        /// 句柄是否有效（资源非空）
        /// </summary>
        public bool IsValid => Asset != null;

        /// <summary>
        /// 创建资源句柄实例
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="asset">资源实例</param>
        /// <param name="service">资源服务引用，用于释放时回调</param>
        public ResHandle(string location, T asset, IAsakiResourceService service)
        {
            Location = location;
            Asset = asset;
            _service = service;
        }

        /// <summary>
        /// 释放资源引用
        /// <para>减少引用计数，当计数归零时卸载资源。</para>
        /// </summary>
        public void Dispose()
        {
            if (IsValid)
            {
                _service?.Release(Location, typeof(T));
            }
        }

        /// <summary>
        /// 隐式转换为资源实例
        /// </summary>
        /// <param name="handle">资源句柄</param>
        /// <returns>资源实例，若句柄无效则返回null</returns>
        public static implicit operator T(ResHandle<T> handle)
        {
            return handle.Asset;
        }
    }

    /// <summary>
    /// Asaki资源服务接口
    /// <para>定义资源加载、释放的核心契约，是框架资源管理的主要入口。</para>
    /// <para>支持单资源加载、批量加载、进度回调和取消操作。</para>
    /// </summary>
    /// <remarks>
    /// <para>核心功能：</para>
    /// <list type="bullet">
    /// <item><description>异步加载：所有加载方法均为异步，基于UniTask实现</description></item>
    /// <item><description>引用计数：自动管理资源生命周期，支持多引用共享</description></item>
    /// <item><description>类型安全：泛型方法确保类型正确，避免运行时错误</description></item>
    /// <item><description>进度反馈：支持单个和批量加载的进度回调</description></item>
    /// </list>
    /// <para>使用建议：</para>
    /// <list type="bullet">
    /// <item><description>使用using语句确保资源正确释放</item></item>
    /// <item><description>批量操作优先使用Batch方法，性能更优</item></item>
    /// <item><description>长时间加载场景建议设置合理的超时时间</item></item>
    /// </list>
    /// </remarks>
    public interface IAsakiResourceService : IAsakiModule
    {
        /// <summary>
        /// 异步加载资源（带进度回调）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源定位地址</param>
        /// <param name="onProgress">进度回调，参数范围0.0~1.0，可为null</param>
        /// <param name="token">取消令牌</param>
        /// <returns>资源句柄，使用完毕后应调用Dispose释放</returns>
        /// <exception cref="ArgumentNullException">location为空时抛出</exception>
        /// <exception cref="InvalidOperationException">资源不存在时抛出</exception>
        /// <exception cref="InvalidCastException">类型不匹配时抛出</exception>
        /// <exception cref="TimeoutException">加载超时时抛出</exception>
        /// <exception cref="OperationCanceledException">加载被取消时抛出</exception>
        UniTask<ResHandle<T>> LoadAsync<T>(
            string location,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class;

        /// <summary>
        /// 异步加载资源（无进度回调）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源定位地址</param>
        /// <param name="token">取消令牌</param>
        /// <returns>资源句柄</returns>
        UniTask<ResHandle<T>> LoadAsync<T>(string location, CancellationToken token)
            where T : class;

        /// <summary>
        /// 异步加载资源（非泛型版本）
        /// <para>用于运行时类型不确定的场景，如配置驱动的资源加载。</para>
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="type">资源类型</param>
        /// <param name="onProgress">进度回调，可为null</param>
        /// <param name="token">取消令牌</param>
        /// <returns>资源句柄（Object类型）</returns>
        UniTask<ResHandle<UnityEngine.Object>> LoadAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        );

        /// <summary>
        /// 释放资源引用
        /// <para>减少引用计数，当计数归零时触发实际卸载。</para>
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="type">资源类型，必须与加载时类型一致</param>
        void Release(string location, Type type);

        /// <summary>
        /// 批量异步加载资源（带进度回调）
        /// <para>并行加载多个资源，提供整体进度反馈。</para>
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="locations">资源定位地址集合</param>
        /// <param name="onProgress">整体进度回调，范围0.0~1.0</param>
        /// <param name="token">取消令牌</param>
        /// <returns>资源句柄列表，顺序与输入地址顺序一致</returns>
        UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class;

        /// <summary>
        /// 批量异步加载资源（无进度回调）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="locations">资源定位地址集合</param>
        /// <param name="token">取消令牌</param>
        /// <returns>资源句柄列表</returns>
        UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            CancellationToken token
        )
            where T : class;

        /// <summary>
        /// 批量释放资源
        /// <para>与LoadBatchAsync&lt;T&gt;对应，确保使用正确的类型释放资源。</para>
        /// </summary>
        /// <typeparam name="T">资源类型，必须与加载时类型一致</typeparam>
        /// <param name="locations">资源定位地址集合</param>
        void ReleaseBatch<T>(IEnumerable<string> locations)
            where T : class;

        /// <summary>
        /// 卸载未使用的资源
        /// <para>触发垃圾回收，释放所有引用计数为0的资源。</para>
        /// <para>建议在场景切换或内存压力大时调用。</para>
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>卸载完成的异步任务</returns>
        UniTask UnloadUnusedAssets(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// 设置加载超时时间
        /// <para>影响依赖资源加载的超时检测，不影响主资源加载。</para>
        /// </summary>
        /// <param name="timeoutSeconds">超时秒数，最小值为1秒</param>
        void SetTimeoutSeconds(int timeoutSeconds);
    }
}
