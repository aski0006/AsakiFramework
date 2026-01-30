using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Command
{
    /// <summary>
    /// Command 对象池管理器
    /// 统一使用 AsakiArchitecturePoolManager 实现
    /// </summary>
    internal static class AsakiCommandPoolManager
    {
        /// <summary>
        /// 租借 Command 对象
        /// </summary>
        public static TCommand Rent<TCommand>()
            where TCommand : class, new()
        {
            return AsakiArchitecturePoolManager.Rent<TCommand>();
        }

        /// <summary>
        /// 异步租借 Command 对象
        /// </summary>
        public static async UniTask<TCommand> RentAsync<TCommand>(CancellationToken token = default)
            where TCommand : class, new()
        {
            return await AsakiArchitecturePoolManager.RentAsync<TCommand>(token);
        }

        /// <summary>
        /// 归还 Command 对象到池
        /// </summary>
        public static bool Return<TCommand>(TCommand cmd)
            where TCommand : class
        {
            return AsakiArchitecturePoolManager.Return(cmd);
        }

        /// <summary>
        /// 清空所有 Command 池
        /// </summary>
        public static void ClearAll()
        {
            AsakiArchitecturePoolManager.ClearAll();
        }
    }
}
