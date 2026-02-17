using System;
using System.Threading;
using Asaki.Core.Architecture;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Queries
{
    public abstract class AsakiQuery<TResult> : IAsakiQuery<TResult>
    {
        protected IAsakiServiceProvider ServiceProvider { get; private set; }

        public void Create(IAsakiServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            OnCreate();
        }

        protected virtual void OnCreate() { }

        public abstract TResult Query();

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

    public abstract class AsakiQueryAsync<TResult> : IAsakiQueryAsync<TResult>
    {
        protected IAsakiServiceProvider ServiceProvider { get; private set; }

        public void Create(IAsakiServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            OnCreate();
        }

        public abstract UniTask<TResult> QueryAsync(
            CancellationToken token = default(CancellationToken)
        );

        protected virtual void OnCreate() { }

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
}
