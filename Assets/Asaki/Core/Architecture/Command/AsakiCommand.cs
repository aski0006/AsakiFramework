using System;
using System.Threading;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Command
{
    public abstract class AsakiCommand : IAsakiCommand
    {
        protected IAsakiServiceProvider ServiceProvider { get; private set; }

        public void Create(IAsakiServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            OnCreate();
        }

        protected virtual void OnCreate() { }

        public abstract void Execute();

        protected T GetSystem<T>() where T : class, IAsakiSystem
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetSystem<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected T GetModel<T>() where T : class, IAsakiModel
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetModel<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected void Log(string message)
        {
            ALog.Info($"[{GetType().Name}] {message}");
        }

        protected void LogWarning(string message)
        {
            ALog.Warn($"[{GetType().Name}] {message}");
        }

        protected void LogError(string message)
        {
            ALog.Error($"[{GetType().Name}] {message}");
        }
    }

    public abstract class AsakiCommand<TResult> : IAsakiCommand<TResult>
    {
        protected IAsakiServiceProvider ServiceProvider { get; private set; }

        public void Create(IAsakiServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            OnCreate();
        }

        protected virtual void OnCreate() { }

        public abstract TResult Execute();

        protected T GetSystem<T>() where T : class, IAsakiSystem
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetSystem<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected T GetModel<T>() where T : class, IAsakiModel
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetModel<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected void Log(string message)
        {
            ALog.Info($"[{GetType().Name}] {message}");
        }

        protected void LogWarning(string message)
        {
            ALog.Warn($"[{GetType().Name}] {message}");
        }

        protected void LogError(string message)
        {
            ALog.Error($"[{GetType().Name}] {message}");
        }
    }

    public abstract class AsakiCommandAsync : IAsakiCommandAsync
    {
        protected IAsakiServiceProvider ServiceProvider { get; private set; }

        public void Create(IAsakiServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            OnCreate();
        }

        protected virtual void OnCreate() { }

        public abstract UniTask ExecuteAsync();

        protected T GetSystem<T>() where T : class, IAsakiSystem
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetSystem<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected T GetModel<T>() where T : class, IAsakiModel
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetModel<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected void Log(string message)
        {
            ALog.Info($"[{GetType().Name}] {message}");
        }

        protected void LogWarning(string message)
        {
            ALog.Warn($"[{GetType().Name}] {message}");
        }

        protected void LogError(string message)
        {
            ALog.Error($"[{GetType().Name}] {message}");
        }
    }

    public abstract class AsakiCommandAsync<TResult> : IAsakiCommandAsync<TResult>
    {
        protected IAsakiServiceProvider ServiceProvider { get; private set; }

        public void Create(IAsakiServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            OnCreate();
        }

        protected virtual void OnCreate() { }

        public abstract UniTask<TResult> ExecuteAsync(
            CancellationToken token = default(CancellationToken)
        );

        protected T GetSystem<T>() where T : class, IAsakiSystem
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetSystem<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected T GetModel<T>() where T : class, IAsakiModel
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetModel<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        protected void Log(string message)
        {
            ALog.Info($"[{GetType().Name}] {message}");
        }

        protected void LogWarning(string message)
        {
            ALog.Warn($"[{GetType().Name}] {message}");
        }

        protected void LogError(string message)
        {
            ALog.Error($"[{GetType().Name}] {message}");
        }
    }

    public abstract class AsakiUndoCommand : AsakiCommand, IAsakiUndoCommand
    {
        public virtual bool CanUndo => true;

        public abstract void Undo();

        public virtual void Redo()
        {
            // 默认 Redo = 重新执行
            Execute();
        }
    }

    public abstract class AsakiUndoCommand<TResult>
        : AsakiCommand<TResult>,
            IAsakiUndoCommand<TResult>
    {
        public virtual bool CanUndo => true;

        public abstract void Undo();

        public virtual void Redo()
        {
            Execute();
        }
    }
}
