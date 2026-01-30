using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Command
{
    /// <summary>
    /// Command 对象池管理器 - 现在委托给统一的 AsakiArchitecturePoolManager
    /// </summary>
    internal static class AsakiCommandPoolManager
    {
        public static async UniTask<TCommand> RentAsync<TCommand>(CancellationToken token = default)
            where TCommand : class, new()
        {
            return await AsakiArchitecturePoolManager.RentAsync<TCommand>(token);
        }

        public static TCommand Rent<TCommand>()
            where TCommand : class, new()
        {
            return AsakiArchitecturePoolManager.Rent<TCommand>();
        }

        public static bool Return<TCommand>(TCommand cmd)
            where TCommand : class
        {
            return AsakiArchitecturePoolManager.Return(cmd);
        }

        public static void ClearAll()
        {
            // 委托给全局清理(通常不需要单独清理 Command 池)
            AsakiArchitecturePoolManager.ClearAll();
        }
    }
}
