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
        /// 租借 Query 对象
        /// </summary>
        public static TQuery Rent<TQuery>()
            where TQuery : class, new()
        {
            return AsakiArchitecturePoolManager.Rent<TQuery>();
        }

        /// <summary>
        /// 异步租借 Query 对象
        /// </summary>
        public static async UniTask<TQuery> RentAsync<TQuery>(CancellationToken token = default)
            where TQuery : class, new()
        {
            return await AsakiArchitecturePoolManager.RentAsync<TQuery>(token);
        }

        /// <summary>
        /// 归还 Query 对象到池
        /// </summary>
        public static bool Return<TQuery>(TQuery query)
            where TQuery : class
        {
            return AsakiArchitecturePoolManager.Return(query);
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
