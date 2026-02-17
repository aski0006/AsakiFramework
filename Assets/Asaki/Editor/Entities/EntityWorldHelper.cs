using System;
using System.Collections.Generic;
using System.Reflection;
using Asaki.Core.Architecture;
using Asaki.Core.Architecture.Entities;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Entities
{
    /// <summary>
    /// EntityWorld获取工具类 - 用于编辑器窗口获取EntityWorld实例
    /// </summary>
    public static class EntityWorldHelper
    {
        private static FieldInfo _pureCSharpServicesField;
        private static bool _reflectionInitialized;

        private static List<IAsakiArchitecture> _cachedArchitectures;
        private static double _cacheTime;
        private const double CacheDuration = 1.0;

        private static void InitializeReflection()
        {
            if (_reflectionInitialized)
                return;

            _pureCSharpServicesField = typeof(AsakiSceneContext).GetField(
                "_pureCSharpServices",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            _reflectionInitialized = _pureCSharpServicesField != null;
        }

        /// <summary>
        /// 尝试获取第一个可用的EntityWorld实例
        /// </summary>
        /// <param name="world">输出的EntityWorld实例</param>
        /// <returns>是否成功获取</returns>
        public static bool TryGetEntityWorld(out IEntityWorld world)
        {
            world = null;

            var architectures = GetArchitectures();
            foreach (var arch in architectures)
            {
                try
                {
                    world = arch.GetEntityWorld();
                    if (world != null)
                        return true;
                }
                catch (KeyNotFoundException)
                {
                    // This Architecture doesn't have EntityModel registered, skip it
                    continue;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取所有可用的EntityWorld实例
        /// </summary>
        /// <returns>EntityWorld实例列表</returns>
        public static List<IEntityWorld> GetAllEntityWorlds()
        {
            var worlds = new List<IEntityWorld>();
            var architectures = GetArchitectures();

            foreach (var arch in architectures)
            {
                try
                {
                    var world = arch.GetEntityWorld();
                    if (world != null && !worlds.Contains(world))
                        worlds.Add(world);
                }
                catch (KeyNotFoundException)
                {
                    // This Architecture doesn't have EntityModel registered, skip it
                    continue;
                }
            }

            return worlds;
        }

        /// <summary>
        /// 获取所有IAsakiArchitecture实例（带缓存）
        /// </summary>
        /// <returns>Architecture实例列表</returns>
        public static List<IAsakiArchitecture> GetArchitectures()
        {
            InitializeReflection();

            if (
                _cachedArchitectures != null
                && (EditorApplication.timeSinceStartup - _cacheTime) < CacheDuration
            )
            {
                return _cachedArchitectures;
            }

            _cachedArchitectures = FindArchitectures();
            _cacheTime = EditorApplication.timeSinceStartup;
            return _cachedArchitectures;
        }

        /// <summary>
        /// 强制刷新缓存
        /// </summary>
        public static void RefreshCache()
        {
            _cachedArchitectures = null;
            _cacheTime = 0;
        }

        private static List<IAsakiArchitecture> FindArchitectures()
        {
            var architectures = new List<IAsakiArchitecture>();

            if (_pureCSharpServicesField == null)
                return architectures;

            var sceneContexts = Resources.FindObjectsOfTypeAll<AsakiSceneContext>();

            foreach (var context in sceneContexts)
            {
                if (context == null)
                    continue;

                try
                {
                    if (
                        _pureCSharpServicesField.GetValue(context)
                        is IList<IAsakiSceneService> services
                    )
                    {
                        foreach (var service in services)
                        {
                            if (service is IAsakiArchitecture arch)
                            {
                                architectures.Add(arch);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[EntityWorldHelper] Failed to get services from context: {ex.Message}"
                    );
                }
            }

            return architectures;
        }
    }
}
