using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling.Interfaces
{
    /// <summary>
    /// 对象池基础接口
    /// </summary>
    public interface IAsakiPoolBase : IDisposable
    {
        /// <summary>池的唯一标识符</summary>
        string Key { get; }

        /// <summary>池配置</summary>
        AsakiPoolConfig Config { get; }

        /// <summary>统计信息</summary>
        IAsakiPoolStatistics Statistics { get; }

        /// <summary>对象类型</summary>
        Type ObjectType { get; }

        /// <summary>清空池中所有对象</summary>
        void Clear();

        /// <summary>收缩池到指定大小</summary>
        void Shrink(int targetSize);
    }

    /// <summary>
    /// 泛型对象池接口
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IAsakiPool<T> : IAsakiPoolBase
        where T : class
    {
        /// <summary>
        /// 异步预热池
        /// </summary>
        /// <param name="count">预热数量</param>
        /// <param name="itemsPerFrame">每帧创建数量</param>
        /// <param name="token">取消令牌</param>
        UniTask PrewarmAsync(
            int count,
            int itemsPerFrame = 5,
            CancellationToken token = default(CancellationToken)
        );

        /// <summary>
        /// 异步获取对象
        /// </summary>
        UniTask<T> GetAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// 同步获取对象
        /// </summary>
        T Get();

        /// <summary>
        /// 归还对象到池
        /// </summary>
        bool Return(T item);
    }
}
