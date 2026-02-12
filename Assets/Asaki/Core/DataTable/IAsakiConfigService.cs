using System;
using System.Collections.Generic;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.DataTable
{
    public interface IAsakiConfigService : IAsakiModule
    {
        UniTask LoadAllAsync();
        T Get<T>(int id)
            where T : class, IAsakiDataTable, new();
        IReadOnlyList<T> GetAll<T>()
            where T : class, IAsakiDataTable, new();
        IAsyncEnumerable<T> GetAllStreamAsync<T>()
            where T : class, IAsakiDataTable, new();
        UniTask ReloadAsync<T>()
            where T : class, IAsakiDataTable, new();
        T Find<T>(Predicate<T> predicate)
            where T : class, IAsakiDataTable, new();
        IReadOnlyList<T> Where<T>(Func<T, bool> predicate)
            where T : class, IAsakiDataTable, new();
        bool Exists<T>(Predicate<T> predicate)
            where T : class, IAsakiDataTable, new();
        IReadOnlyList<T> GetBatch<T>(IEnumerable<int> ids)
            where T : class, IAsakiDataTable, new();
        int GetCount<T>()
            where T : class, IAsakiDataTable, new();
        bool IsLoaded<T>()
            where T : class, IAsakiDataTable, new();
        bool IsLoaded(Type type);
        string GetSourcePath<T>()
            where T : class, IAsakiDataTable, new();
        DateTime GetLastModifiedTime<T>()
            where T : class, IAsakiDataTable, new();
        UniTask<T> GetAsync<T>(int id)
            where T : class, IAsakiDataTable, new();
        UniTask PreloadAsync<T>()
            where T : class, IAsakiDataTable, new();
        UniTask PreloadAsync(Type type);
        UniTask PreloadBatchAsync(params Type[] configTypes);
        void Unload<T>()
            where T : class, IAsakiDataTable, new();
        void Unload(Type configType);
        AsakiConfigLoadInfo GetLoadInfo<T>()
            where T : class, IAsakiDataTable, new();
    }
}
