using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling.Interfaces
{
    /// <summary>
    /// 对象池服务接口
    /// 提供池的创建、管理和销毁功能
    /// </summary>
    public interface IAsakiPoolService : IAsakiService, IDisposable
    {
        /// <summary>
        /// 创建对象池
        /// </summary>
        UniTask<IAsakiPool<T>> CreatePoolAsync<T>(
            string key,
            IAsakiPoolObjectFactory<T> factory,
            AsakiPoolConfig config = null,
            CancellationToken token = default(CancellationToken)
        )
            where T : class;

        /// <summary>
        /// 获取指定类型的池
        /// </summary>
        IAsakiPool<T> GetPool<T>(string key)
            where T : class;

        /// <summary>
        /// 检查池是否存在
        /// </summary>
        bool HasPool(string key);

        /// <summary>
        /// 销毁指定池
        /// </summary>
        bool DestroyPool(string key);

        /// <summary>
        /// 获取统计信息摘要
        /// </summary>
        string GetStatisticsSummary();
        IEnumerable<string> GetAllPoolKeys();
    }
}
