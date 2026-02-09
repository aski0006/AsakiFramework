using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Queries
{
    /// <summary>
    /// Query 对象池管理器
    /// 统一使用 AsakiArchitecturePoolManager 实现
    /// </summary>
    internal static class QueryPoolManager
    {
        /// <summary>
        /// 租借 Query 对象（用于预热场景）
        /// </summary>
        public static TQuery Rent<TQuery>()
            where TQuery : class, new()
        {
            return AsakiArchitecturePoolManager.Rent<TQuery>();
        }

        /// <summary>
        /// 异步租借 Query 对象（用于预热场景）
        /// </summary>
        public static async UniTask<TQuery> RentAsync<TQuery>(CancellationToken token = default)
            where TQuery : class, new()
        {
            return await AsakiArchitecturePoolManager.RentAsync<TQuery>(token);
        }

        /// <summary>
        /// 尝试租借 Query 对象，如果池不存在则返回false
        /// 用于运行时Query，池不存在时使用new创建
        /// </summary>
        public static bool TryRent<TQuery>(out TQuery query)
            where TQuery : class, new()
        {
            return AsakiArchitecturePoolManager.TryRent(out query);
        }

        /// <summary>
        /// 异步尝试租借 Query 对象
        /// </summary>
        public static async UniTask<(bool success, TQuery query)> TryRentAsync<TQuery>(
            CancellationToken token = default
        )
            where TQuery : class, new()
        {
            return await AsakiArchitecturePoolManager.TryRentAsync<TQuery>(token);
        }

        /// <summary>
        /// 归还 Query 对象到池（用于预热场景）
        /// </summary>
        public static bool Return<TQuery>(TQuery query)
            where TQuery : class
        {
            return AsakiArchitecturePoolManager.Return(query);
        }

        /// <summary>
        /// 尝试归还 Query 对象到池，如果池不存在则直接丢弃
        /// 用于运行时Query，池不存在时由GC回收
        /// </summary>
        public static void TryReturn<TQuery>(TQuery query)
            where TQuery : class
        {
            AsakiArchitecturePoolManager.TryReturn(query);
        }

        /// <summary>
        /// 清空所有 Query 池
        /// </summary>
        public static void ClearAll()
        {
            AsakiArchitecturePoolManager.ClearAll();
        }
    }
}
