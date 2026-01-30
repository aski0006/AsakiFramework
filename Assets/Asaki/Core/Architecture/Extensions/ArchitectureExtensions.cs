using System.Threading;
using Asaki.Core.Architecture.Command;
using Asaki.Core.Architecture.Queries;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Extensions
{
    /// <summary>
    /// 架构扩展方法 - 提供便捷的池化 Command 和 Query 执行
    /// </summary>
    public static class ArchitectureExtensions
    {
        /// <summary>
        /// 池化执行 Command(自动租借和归还)
        /// </summary>
        public static void ExecutePooledCommand<TCommand>(this IAsakiArchitecture architecture)
            where TCommand : class, IAsakiCommand, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            try
            {
                cmd.Create(architecture);
                cmd.Execute();
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        /// <summary>
        /// 池化执行异步 Command
        /// </summary>
        public static async UniTask ExecutePooledCommandAsync<TCommand>(
            this IAsakiArchitecture architecture,
            CancellationToken token = default
        )
            where TCommand : class, IAsakiCommandAsync, new()
        {
            TCommand cmd = await AsakiCommandPoolManager.RentAsync<TCommand>(token);
            try
            {
                cmd.Create(architecture);
                await cmd.ExecuteAsync();
            }
            finally
            {
                AsakiCommandPoolManager.Return(cmd);
            }
        }

        /// <summary>
        /// 池化执行 Query
        /// </summary>
        public static TResult QueryPooled<TQuery, TResult>(this IAsakiArchitecture architecture)
            where TQuery : class, IAsakiQuery<TResult>, new()
        {
            TQuery query = QueryPoolManager.Rent<TQuery>();
            try
            {
                query.Create(architecture);
                return query.Query();
            }
            finally
            {
                QueryPoolManager.Return(query);
            }
        }

        /// <summary>
        /// 池化执行异步 Query
        /// </summary>
        public static async UniTask<TResult> QueryPooledAsync<TQuery, TResult>(
            this IAsakiArchitecture architecture,
            CancellationToken token = default
        )
            where TQuery : class, IAsakiQueryAsync<TResult>, new()
        {
            TQuery query = await QueryPoolManager.RentAsync<TQuery>(token);
            try
            {
                query.Create(architecture);
                return await query.QueryAsync(token);
            }
            finally
            {
                QueryPoolManager.Return(query);
            }
        }
    }
}
