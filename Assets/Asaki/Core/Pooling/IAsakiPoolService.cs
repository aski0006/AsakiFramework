using Cysharp.Threading.Tasks;
using Asaki.Core.Context;
using UnityEngine;
using System.Threading;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// [Asaki Native] 对象池服务契约
    /// <para>核心策略：Async-First Prewarm + Sync Spawn</para>
    /// </summary>
    public interface IAsakiPoolService : IAsakiService
    {
        /// <summary>
        /// [异步预热] 核心 API。
        /// <para>1. 通过资源系统加载 Prefab (Ref Count +1)。</para>
        /// <para>2. 分帧实例化对象填充池子。</para>
        /// <para>必须在 Spawn 前调用，否则 Spawn 会失败。</para>
        /// </summary>
        /// <param name="key">资源地址/Key</param>
        /// <param name="count">目标池内数量</param>
        /// <param name="itemsPerFrame">分帧生成速率 (防卡顿)</param>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask PrewarmAsync(string key, int count, int itemsPerFrame = 5, CancellationToken cancellationToken = default);

        /// <summary>
        /// [同步生成] 获取对象。
        /// <para>前提：Key 必须已预热 (Prewarmed)。</para>
        /// </summary>
        /// <param name="position">可选位置 (null则保持原样或默认)</param>
        /// <param name="rotation">可选旋转 (null则保持原样或默认)</param>
        /// <param name="parent">父节点 (null则为场景根或保持池结构)</param>
        GameObject Spawn(
            string key,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null
        );

        /// <summary>
        /// [回收] 将对象归还给池。
        /// </summary>
        /// <param name="go">要回收的对象</param>
        /// <param name="key">所属的池 Key (V5.1暂需显式传递)</param>
        void Despawn(GameObject go, string key);

        /// <summary>
        /// [释放池]
        /// <para>1. 销毁池中所有闲置对象。</para>
        /// <para>2. 释放资源句柄 (Ref Count -1)，允许底层卸载资源。</para>
        /// </summary>
        void ReleasePool(string key);
    }
}
