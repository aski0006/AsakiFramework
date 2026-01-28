using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Services.Configuration
{
    public static class AsakiConfigRegistry
    {
        private static readonly List<Func<AsakiConfigService, string, string, UniTask?>> _loaders =
            new List<Func<AsakiConfigService, string, string, UniTask?>>();

        public static void RegisterLoader(Func<AsakiConfigService, string, string, UniTask?> loader)
        {
            if (!_loaders.Contains(loader))
            {
                _loaders.Add(loader);
            }
        }

        public static UniTask? GetLoader(AsakiConfigService service, string configName, string path)
        {
            foreach (var loader in _loaders)
            {
                var task = loader(service, configName, path);
                if (task.HasValue)
                {
                    return task.Value;
                }
            }
            return null;
        }

        public static void Clear()
        {
            _loaders.Clear();
        }
    }
}
