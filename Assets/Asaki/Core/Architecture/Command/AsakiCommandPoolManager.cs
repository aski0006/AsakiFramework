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
        /// 租借 Command 对象（用于预热场景）
        /// </summary>
        public static TCommand Rent<TCommand>()
            where TCommand : class, new()
        {
            return AsakiArchitecturePoolManager.Rent<TCommand>();
        }

        /// <summary>
        /// 异步租借 Command 对象（用于预热场景）
        /// </summary>
        public static async UniTask<TCommand> RentAsync<TCommand>(CancellationToken token = default)
            where TCommand : class, new()
        {
            return await AsakiArchitecturePoolManager.RentAsync<TCommand>(token);
        }

        /// <summary>
        /// 尝试租借 Command 对象，如果池不存在则返回false
        /// 用于运行时Command，池不存在时使用new创建
        /// </summary>
        public static bool TryRent<TCommand>(out TCommand cmd)
            where TCommand : class, new()
        {
            return AsakiArchitecturePoolManager.TryRent(out cmd);
        }

        /// <summary>
        /// 异步尝试租借 Command 对象
        /// </summary>
        public static async UniTask<(bool success, TCommand cmd)> TryRentAsync<TCommand>(
            CancellationToken token = default
        )
            where TCommand : class, new()
        {
            return await AsakiArchitecturePoolManager.TryRentAsync<TCommand>(token);
        }

        /// <summary>
        /// 归还 Command 对象到池（用于预热场景）
        /// </summary>
        public static bool Return<TCommand>(TCommand cmd)
            where TCommand : class
        {
            return AsakiArchitecturePoolManager.Return(cmd);
        }

        /// <summary>
        /// 尝试归还 Command 对象到池，如果池不存在则直接丢弃
        /// 用于运行时Command，池不存在时由GC回收
        /// </summary>
        public static void TryReturn<TCommand>(TCommand cmd)
            where TCommand : class
        {
            AsakiArchitecturePoolManager.TryReturn(cmd);
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
