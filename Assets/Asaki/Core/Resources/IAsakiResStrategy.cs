using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Resources
{
    /// <summary>
    /// 资源加载策略接口
    /// <para>定义资源加载、卸载的核心行为抽象，支持多种底层实现方式。</para>
    /// <para>通过策略模式实现Resources、Addressables、AssetBundle等不同加载方式的无缝切换。</para>
    /// </summary>
    /// <remarks>
    /// <para>实现要求：</para>
    /// <list type="bullet">
    /// <item><description>必须实现线程安全的加载和卸载操作</description></item>
    /// <item><description>加载失败时应抛出明确的异常信息</description></item>
    /// <item><description>支持取消令牌以响应加载中断</description></item>
    /// </list>
    /// <para>内置实现：</para>
    /// <list type="bullet">
    /// <item><description>AsakiResourcesStrategy - Unity原生Resources加载</description></item>
    /// <item><description>AsakiAddressablesStrategy - Unity Addressables系统加载</description></item>
    /// </list>
    /// </remarks>
    public interface IAsakiResStrategy
    {
        /// <summary>
        /// 获取策略名称
        /// <para>用于日志输出和调试识别，建议返回格式："策略名 (描述)"。</para>
        /// </summary>
        /// <example>
        /// <code>"Resources (Native)"</code>
        /// <code>"Addressables (Pro)"</code>
        /// </example>
        string StrategyName { get; }

        /// <summary>
        /// 初始化策略
        /// <para>在资源服务启动时调用，用于执行策略所需的初始化操作。</para>
        /// <para>例如：Addressables需要初始化Catalog，自定义策略可能需要加载配置。</para>
        /// </summary>
        /// <returns>初始化完成的异步任务</returns>
        UniTask InitializeAsync();

        /// <summary>
        /// 加载资源
        /// <para>核心加载方法，由具体的策略实现决定底层加载方式。</para>
        /// </summary>
        /// <param name="location">资源定位地址
        /// <para>Resources模式：相对于Resources文件夹的路径，不含扩展名</para>
        /// <para>Addressables模式：Addressable地址或Label</para>
        /// </param>
        /// <param name="type">资源类型，用于正确加载子资源（如Sprite、Texture2D）</param>
        /// <param name="onProgress">进度回调，参数范围为0.0~1.0，可为null</param>
        /// <param name="token">取消令牌，用于中断加载操作</param>
        /// <returns>加载完成的资源对象</returns>
        /// <exception cref="ArgumentNullException">location为空时抛出</exception>
        /// <exception cref="InvalidOperationException">资源不存在时抛出</exception>
        /// <exception cref="OperationCanceledException">加载被取消时抛出</exception>
        UniTask<UnityEngine.Object> LoadAssetInternalAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        );

        /// <summary>
        /// 卸载单个资源
        /// <para>释放指定资源的内存占用，具体行为由策略实现决定。</para>
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="asset">要卸载的资源对象</param>
        /// <remarks>
        /// <para>注意事项：</para>
        /// <list type="bullet">
        /// <item><description>Resources模式：非GameObject资源调用Resources.UnloadAsset</description></item>
        /// <item><description>Addressables模式：调用Addressables.Release减少引用计数</description></item>
        /// <item><description>GameObject通常通过Instantiate创建，不应直接卸载</description></item>
        /// </list>
        /// </remarks>
        void UnloadAssetInternal(string location, UnityEngine.Object asset);

        /// <summary>
        /// 卸载未使用的资源
        /// <para>触发垃圾回收，释放所有引用计数为0的资源。</para>
        /// <para>通常在场景切换、内存压力大时调用。</para>
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>卸载完成的异步任务</returns>
        /// <remarks>
        /// <para>此操作会触发Resources.UnloadUnusedAssets()，可能导致帧卡顿。</para>
        /// <para>建议在Loading场景或过渡动画期间调用。</para>
        /// </remarks>
        UniTask UnloadUnusedAssets(CancellationToken token);
    }
}
