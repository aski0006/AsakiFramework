using System;
using Asaki.Core.Architecture.Command;
using Asaki.Core.Architecture.Queries;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture
{
    public interface IAsakiArchitecture : IAsakiSceneService, IAsakiServiceProvider, IDisposable
    {
        T GetSystem<T>()
            where T : class, IAsakiSystem;
        T GetModel<T>()
            where T : class, IAsakiModel;

        /// <summary>
        /// 获取实体世界（便捷方法）
        /// </summary>
        Entities.IEntityWorld GetEntityWorld();

        #region SendCommand 同步命令

        /// <summary>
        /// 执行同步命令（无返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        void SendCommand<TCommand>()
            where TCommand : class, IAsakiCommand, new();

        /// <summary>
        /// 执行同步命令（有返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns>命令执行结果</returns>
        TResult SendCommand<TCommand, TResult>()
            where TCommand : class, IAsakiCommand<TResult>, new();

        /// <summary>
        /// 执行同步命令（带配置委托，无返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <param name="configure">配置委托</param>
        void SendCommand<TCommand>(Action<TCommand> configure)
            where TCommand : class, IAsakiCommand, new();

        /// <summary>
        /// 执行同步命令（带配置委托，有返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="configure">配置委托</param>
        /// <returns>命令执行结果</returns>
        TResult SendCommand<TCommand, TResult>(Action<TCommand> configure)
            where TCommand : class, IAsakiCommand<TResult>, new();

        #endregion

        #region SendCommandAsync 异步命令

        /// <summary>
        /// 执行异步命令（无返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        UniTask SendCommandAsync<TCommand>()
            where TCommand : class, IAsakiCommandAsync, new();

        /// <summary>
        /// 执行异步命令（有返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns>命令执行结果</returns>
        UniTask<TResult> SendCommandAsync<TCommand, TResult>()
            where TCommand : class, IAsakiCommandAsync<TResult>, new();

        /// <summary>
        /// 执行异步命令（带配置委托，无返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <param name="configure">配置委托</param>
        UniTask SendCommandAsync<TCommand>(Action<TCommand> configure)
            where TCommand : class, IAsakiCommandAsync, new();

        /// <summary>
        /// 执行异步命令（带配置委托，有返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="configure">配置委托</param>
        /// <returns>命令执行结果</returns>
        UniTask<TResult> SendCommandAsync<TCommand, TResult>(Action<TCommand> configure)
            where TCommand : class, IAsakiCommandAsync<TResult>, new();

        #endregion

        #region SendQuery 同步查询

        /// <summary>
        /// 执行同步查询
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns>查询结果</returns>
        TResult SendQuery<TQuery, TResult>()
            where TQuery : class, IAsakiQuery<TResult>, new();

        /// <summary>
        /// 执行同步查询（带缓存）
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="cacheSeconds">缓存时长（秒）</param>
        /// <returns>查询结果</returns>
        TResult SendQuery<TQuery, TResult>(float cacheSeconds)
            where TQuery : class, IAsakiQuery<TResult>, new();

        /// <summary>
        /// 执行同步查询（带配置委托）
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="configure">配置委托</param>
        /// <param name="cacheSeconds">缓存时长（秒），默认不缓存</param>
        /// <returns>查询结果</returns>
        TResult SendQuery<TQuery, TResult>(Action<TQuery> configure, float cacheSeconds = 0f)
            where TQuery : class, IAsakiQuery<TResult>, new();

        #endregion

        #region SendQueryAsync 异步查询

        /// <summary>
        /// 执行异步查询
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns>查询结果</returns>
        UniTask<TResult> SendQueryAsync<TQuery, TResult>()
            where TQuery : class, IAsakiQueryAsync<TResult>, new();

        /// <summary>
        /// 执行异步查询（带缓存）
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="cacheSeconds">缓存时长（秒）</param>
        /// <returns>查询结果</returns>
        UniTask<TResult> SendQueryAsync<TQuery, TResult>(float cacheSeconds)
            where TQuery : class, IAsakiQueryAsync<TResult>, new();

        /// <summary>
        /// 执行异步查询（带配置委托）
        /// </summary>
        /// <typeparam name="TQuery">查询类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="configure">配置委托</param>
        /// <param name="cacheSeconds">缓存时长（秒），默认不缓存</param>
        /// <returns>查询结果</returns>
        UniTask<TResult> SendQueryAsync<TQuery, TResult>(
            Action<TQuery> configure,
            float cacheSeconds = 0f
        )
            where TQuery : class, IAsakiQueryAsync<TResult>, new();

        #endregion

        #region SendUndoCommand 撤销命令

        /// <summary>
        /// 执行可撤销命令
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        void SendUndoCommand<TCommand>()
            where TCommand : class, IAsakiUndoCommand, new();

        /// <summary>
        /// 执行可撤销命令（带配置委托）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <param name="configure">配置委托</param>
        void SendUndoCommand<TCommand>(Action<TCommand> configure)
            where TCommand : class, IAsakiUndoCommand, new();

        #endregion

        #region Undo/Redo 操作

        /// <summary>
        /// 撤销上一步操作
        /// </summary>
        void Undo();

        /// <summary>
        /// 重做上一步撤销的操作
        /// </summary>
        void Redo();

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// 是否可以重做
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// 撤销栈中的命令数量
        /// </summary>
        int UndoCount { get; }

        /// <summary>
        /// 重做栈中的命令数量
        /// </summary>
        int RedoCount { get; }

        #endregion
    }
}
