using System;
using System.Collections.Generic;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Configuration
{
    public interface IAsakiConfigService : IAsakiModule
    {
        UniTask LoadAllAsync();
        T Get<T>(int id)
            where T : class, IAsakiConfig, new();
        IReadOnlyList<T> GetAll<T>()
            where T : class, IAsakiConfig, new();
        IAsyncEnumerable<T> GetAllStreamAsync<T>()
            where T : class, IAsakiConfig, new();
        UniTask ReloadAsync<T>()
            where T : class, IAsakiConfig, new();
        T Find<T>(Predicate<T> predicate)
            where T : class, IAsakiConfig, new();
        IReadOnlyList<T> Where<T>(Func<T, bool> predicate)
            where T : class, IAsakiConfig, new();
        bool Exists<T>(Predicate<T> predicate)
            where T : class, IAsakiConfig, new();
        IReadOnlyList<T> GetBatch<T>(IEnumerable<int> ids)
            where T : class, IAsakiConfig, new();
        int GetCount<T>()
            where T : class, IAsakiConfig, new();
        bool IsLoaded<T>()
            where T : class, IAsakiConfig, new();
        bool IsLoaded(Type type);
        string GetSourcePath<T>()
            where T : class, IAsakiConfig, new();
        DateTime GetLastModifiedTime<T>()
            where T : class, IAsakiConfig, new();
        UniTask<T> GetAsync<T>(int id)
            where T : class, IAsakiConfig, new();
        UniTask PreloadAsync<T>()
            where T : class, IAsakiConfig, new();
        UniTask PreloadAsync(Type type);
        UniTask PreloadBatchAsync(params Type[] configTypes);
        void Unload<T>()
            where T : class, IAsakiConfig, new();
        void Unload(Type configType);
        AsakiConfigLoadInfo GetLoadInfo<T>()
            where T : class, IAsakiConfig, new();
    }
}
