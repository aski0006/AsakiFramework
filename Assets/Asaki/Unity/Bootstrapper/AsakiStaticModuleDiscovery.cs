using System;
using System.Collections.Generic;
using Asaki.Core.Context;

namespace Asaki.Unity.Bootstrapper
{
    /// <summary>
    /// [Asaki V5 Native] 静态模块发现服务
    /// <para>配合 Roslyn 生成器使用，实现"零反射"的模块注册。</para>
    /// <para>生成器会为每个模块生成注册代码，在游戏启动前自动调用 Register。</para>
    /// </summary>
    public class AsakiStaticModuleDiscovery : IAsakiModuleDiscovery
    {
        // 静态缓存，所有生成的代码都会往这里塞数据
        private static readonly HashSet<Type> _registry = new HashSet<Type>();

        /// <summary>
        /// 供 Roslyn 生成的代码调用
        /// </summary>
        public static void Register(Type moduleType)
        {
            if (moduleType != null && !_registry.Contains(moduleType))
            {
                _registry.Add(moduleType);
            }
        }

        public IEnumerable<Type> GetModuleTypes()
        {
            // 直接返回缓存，0 消耗
            return _registry;
        }

        /// <summary>
        /// 仅用于调试，清理静态状态
        /// </summary>
        public static void Reset()
        {
            _registry.Clear();
        }
    }
}
