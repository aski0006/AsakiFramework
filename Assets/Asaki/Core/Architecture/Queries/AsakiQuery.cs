using System.Threading;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Queries
{
    public abstract class AsakiQuery<TResult> : IAsakiQuery<TResult>
    {
        protected IAsakiArchitecture Architecture { get; private set; }

        public void Create(IAsakiArchitecture architecture)
        {
            Architecture = architecture;
            OnCreate();
        }

        protected virtual void OnCreate() { }

        public abstract TResult Query();

        protected T GetModel<T>()
            where T : class, IAsakiModel => Architecture.GetModel<T>();

        protected T GetSystem<T>()
            where T : class, IAsakiSystem => Architecture.GetSystem<T>();

        protected void Log(string message) => ALog.Info($"[{GetType().Name}] {message}");

        protected void LogWarning(string message) => ALog.Warn($"[{GetType().Name}] {message}");

        protected void LogError(string message) => ALog.Error($"[{GetType().Name}] {message}");
    }

    public abstract class AsakiQueryAsync<TResult> : IAsakiQueryAsync<TResult>
    {
        protected IAsakiArchitecture Architecture { get; private set; }

        public void Create(IAsakiArchitecture architecture)
        {
            Architecture = architecture;
            OnCreate();
        }

        public abstract UniTask<TResult> QueryAsync(CancellationToken token = default);

        protected virtual void OnCreate() { }

        protected T GetModel<T>()
            where T : class, IAsakiModel => Architecture.GetModel<T>();

        protected T GetSystem<T>()
            where T : class, IAsakiSystem => Architecture.GetSystem<T>();

        protected void Log(string message) => ALog.Info($"[{GetType().Name}] {message}");

        protected void LogWarning(string message) => ALog.Warn($"[{GetType().Name}] {message}");

        protected void LogError(string message) => ALog.Error($"[{GetType().Name}] {message}");
    }
}
