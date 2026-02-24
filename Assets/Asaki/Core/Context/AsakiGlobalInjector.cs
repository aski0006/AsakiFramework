using System;
using System.Collections.Generic;
using Asaki.Core.Logging;

namespace Asaki.Core.Context
{
    /// <summary>
    /// 分布式注入器接口，支持优先级排序
    /// </summary>
    public interface IAsakiInjector
    {
        /// <summary>
        /// 注入优先级（数值越小越早执行，默认 1000）
        /// </summary>
        int Priority => 1000;

        /// <summary>
        /// 尝试为目标对象注入其声明的所有依赖项
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="resolver">依赖解析器</param>
        /// <param name="injectedTypes">已注入类型集合，用于追踪和冲突检测</param>
        void Inject(object target, IAsakiResolver resolver = null, HashSet<Type> injectedTypes = null);
    }

    /// <summary>
    /// 注入器注册表，管理所有程序集注入器的注册和执行
    /// </summary>
    public static class AsakiGlobalInjector
    {
        private static readonly List<InjectorEntry> _injectors = new List<InjectorEntry>();
        private static bool _isSorted = false;

        private struct InjectorEntry
        {
            public IAsakiInjector Injector;
            public int Priority;
        }

        /// <summary>
        /// 注册程序集注入器（由生成的代码在 RuntimeInitializeOnLoadMethod 中调用）
        /// </summary>
        /// <param name="injector">注入器实例</param>
        /// <param name="priority">可选优先级，默认为注入器自身定义的优先级</param>
        public static void Register(IAsakiInjector injector, int? priority = null)
        {
            if (injector == null)
            {
                ALog.Warn("[AsakiGlobalInjector] Null injector passed to Register, ignoring.");
                return;
            }

            // 检查重复注册
            foreach (var entry in _injectors)
            {
                if (ReferenceEquals(entry.Injector, injector))
                {
                    ALog.Warn(
                        $"[AsakiGlobalInjector] Injector {injector.GetType().Name} already registered, ignoring duplicate."
                    );
                    return;
                }
            }

            _injectors.Add(
                new InjectorEntry { Injector = injector, Priority = priority ?? injector.Priority }
            );
            _isSorted = false;

            ALog.Info(
                $"[AsakiGlobalInjector] Registered injector: {injector.GetType().Name} (Priority: {priority ?? injector.Priority})"
            );
        }

        /// <summary>
        /// 对目标对象执行全量注入，按优先级顺序调用所有注入器
        /// 支持类型追踪和冲突检测
        /// </summary>
        public static void Inject(object target, IAsakiResolver resolver = null)
        {
            if (target == null)
                return;

            // 确保注入器按优先级排序
            if (!_isSorted)
            {
                SortInjectors();
            }

            var injectedTypes = new HashSet<Type>();
            var injectorNames = new Dictionary<Type, string>(); // 记录每个类型是由哪个注入器处理的

            int injectedCount = 0;
            foreach (var entry in _injectors)
            {
                try
                {
                    int beforeCount = injectedTypes.Count;
                    entry.Injector.Inject(target, resolver, injectedTypes);
                    injectedCount++;

                    // 检测是否有新类型被注入
                    if (injectedTypes.Count > beforeCount)
                    {
                        // 记录新注入的类型
                        foreach (var type in injectedTypes)
                        {
                            if (!injectorNames.ContainsKey(type))
                            {
                                injectorNames[type] = entry.Injector.GetType().Name;
                            }
                            else
                            {
                                // 冲突检测：同一类型被多个注入器处理
                                ALog.Warn(
                                    $"[AsakiGlobalInjector] Conflict detected: Type {type.Name} was already injected by {injectorNames[type]}, now also by {entry.Injector.GetType().Name}"
                                );
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiGlobalInjector] Injector {entry.Injector.GetType().Name} failed to inject {target.GetType().Name}: {ex}"
                    );
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ALog.Info(
                $"[AsakiGlobalInjector] Injected {injectedCount} injectors into {target.GetType().Name}, types: {injectedTypes.Count}"
            );
#endif
        }

        private static void SortInjectors()
        {
            _injectors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _isSorted = true;

            ALog.Info($"[AsakiGlobalInjector] Sorted {_injectors.Count} injectors by priority");
        }
    }
}
