using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Queries
{
    /// <summary>
    /// Query 对象池管理器 - 现在委托给统一的 AsakiArchitecturePoolManager
    /// </summary>
    internal static class QueryPoolManager
    {
        public static async UniTask<TQuery> RentAsync<TQuery>(CancellationToken token = default)
            where TQuery : class, new()
        {
            return await AsakiArchitecturePoolManager.RentAsync<TQuery>(token);
        }

        public static TQuery Rent<TQuery>()
            where TQuery : class, new()
        {
            return AsakiArchitecturePoolManager.Rent<TQuery>();
        }

        public static bool Return<TQuery>(TQuery query)
            where TQuery : class
        {
            return AsakiArchitecturePoolManager.Return(query);
        }

        /// <summary>
        /// 清空所有池（场景切换时调用）
        /// </summary>
        public static void ClearAll()
        {
            // 委托给全局清理
            AsakiArchitecturePoolManager.ClearAll();
        }
    }
}
