using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Services.DataTable
{
    public static class AsakiConfigRegistry
    {
        // 键：Type，值：加载器（接受 service + path）
        private static readonly Dictionary<
            Type,
            Func<AsakiConfigService, string, UniTask?>
        > _loaders = new Dictionary<Type, Func<AsakiConfigService, string, UniTask?>>();

        public static void RegisterLoader(
            Type configType,
            Func<AsakiConfigService, string, UniTask?> loader
        )
        {
            _loaders[configType] = loader;
        }

        public static UniTask? GetLoader(AsakiConfigService service, Type configType, string path)
        {
            if (_loaders.TryGetValue(configType, out var loader))
            {
                return loader(service, path);
            }
            return null;
        }

        public static void Clear() => _loaders.Clear();
    }
}
