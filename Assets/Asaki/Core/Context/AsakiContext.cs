using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.Context
{
    /// <summary>
    /// [极速微内核] 服务容器 (V5.1 Lock - Free Edition)
    /// <para>架构策略：Copy - On - Write (写时复制) + Snapshot Swap (快照交换)</para>
    /// <para>性能特征：</para>
    /// <list type="bullet">
    /// <item>读操作 (Get): O(1), 无锁 (Zero - Lock), 仅一次引用解引用。</item>
    /// <item>写操作 (Register): O(n), 有锁, 触发内存分配 (仅在启动时发生)。</item>
    /// </list>
    /// </summary>
    public static class AsakiContext
    {
        // ========================================================================
        // 核心存储 (Snapshot)
        // ========================================================================

        // 使用 volatile 确保多线程下的可见性。
        // _services 永远指向一个"只读"的字典实例。每次写入都会创建一个新的字典并替换它。
        private static volatile Dictionary<Type, IAsakiService> _services = new Dictionary<
            Type,
            IAsakiService
        >(64);

        // 写操作专用锁 (读操作不使用任何锁)
        private static readonly object _writeLock = new object();

        // 架构状态机
        private static volatile bool _isFrozen;

        // 框架就绪标志
        private static volatile bool _isReady;

        /// <summary>
        /// 框架是否已就绪
        /// </summary>
        public static bool IsReady => _isReady;

        /// <summary>
        /// 标记框架为就绪状态
        /// </summary>
        public static void SetReady()
        {
            _isReady = true;
        }

        // ========================================================================
        // 极速读取 API (Hot Path)
        // ========================================================================

        /// <summary>
        /// 获取服务实例。
        /// <para>性能：主程热路径专用，无锁，开销等同于原生 Dictionary 查找。</para>
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了 IAsakiService 接口的类类型。</typeparam>
        /// <returns>请求的服务实例。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的服务未找到时抛出。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>()
            where T : class, IAsakiService
        {
            // 直接访问 volatile 引用，无锁
            if (_services.TryGetValue(typeof(T), out IAsakiService service))
            {
                // 强转开销极低 (Unsafe.As 在这里也可以，但 standard cast 更安全且足够快)
                return (T)service;
            }
            throw new KeyNotFoundException($"[AsakiContext] Service not found: {typeof(T).Name}");
        }

        /// <summary>
        /// 尝试获取服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了 IAsakiService 接口的类类型。</typeparam>
        /// <param name="service">如果找到服务，将返回的服务实例赋值给此参数；否则为 null。</param>
        /// <returns>如果找到服务则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<T>(out T service)
            where T : class, IAsakiService
        {
            if (_services.TryGetValue(typeof(T), out IAsakiService s))
            {
                service = (T)s;
                return true;
            }
            service = null;
            return false;
        }

        // ========================================================================
        // 写入 API (Cold Path - Copy On Write)
        // ========================================================================

        /// <summary>
        /// [启动期] 注册服务。
        /// <para>注意：这是一个 O(n) 操作，仅应在游戏初始化阶段调用。</para>
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了 IAsakiService 接口的类类型。</typeparam>
        /// <param name="service">要注册的服务实例。</param>
        public static void Register<T>(T service)
            where T : class, IAsakiService
        {
            RegisterInternal(typeof(T), service, false);
        }

        /// <summary>
        /// [启动期] 注册服务。
        /// <para>注意：这是一个 O(n) 操作，仅应在游戏初始化阶段调用。</para>
        /// </summary>
        /// <param name="type">服务类型。</param>
        /// <param name="service">要注册的服务实例，必须是实现了 <paramref name="type"/> 所表示类型的实例。</param>
        public static void Register(Type type, IAsakiService service)
        {
            RegisterInternal(type, service, false);
        }

        /// <summary>
        /// [热更新] 运行时替换现有服务。
        /// <para>允许在 Freeze 后执行，用于修复 Bug 或热切模块。</para>
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了 IAsakiService 接口的类类型。</typeparam>
        /// <param name="service">要替换的服务实例。</param>
        public static void Replace<T>(T service)
            where T : class, IAsakiService
        {
            RegisterInternal(typeof(T), service, true);
        }

        /// <summary>
        /// 内部注册方法，用于注册或替换服务。
        /// </summary>
        /// <param name="type">服务类型。</param>
        /// <param name="service">要注册或替换的服务实例，必须是实现了 <paramref name="type"/> 所表示类型的实例。</param>
        /// <param name="isReplacement">指示是否为替换操作，如果为 true，则即使容器冻结也允许操作。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="service"/> 为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当 <paramref name="service"/> 的类型未实现 <paramref name="type"/> 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当容器已冻结且不是替换操作，或者在非替换操作时服务已注册时抛出。</exception>
        private static void RegisterInternal(Type type, IAsakiService service, bool isReplacement)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (!type.IsAssignableFrom(service.GetType()))
                throw new ArgumentException(
                    $"Service {service.GetType().Name} does not implement {type.Name}"
                );

            // 1. 获取写锁 (阻塞其他写入者，但不阻塞读取者)
            lock (_writeLock)
            {
                // 2. 状态检查
                if (_isFrozen && !isReplacement)
                {
                    throw new InvalidOperationException(
                        $"[AsakiContext] Container is Frozen! Cannot register new service '{type.Name}' at runtime. "
                            + "Use 'Replace()' if you intend to hot - fix."
                    );
                }

                // 3. 检查重复 (仅针对非替换模式)
                if (!isReplacement && _services.ContainsKey(type))
                {
                    throw new InvalidOperationException(
                        $"[AsakiContext] Service '{type.Name}' is already registered."
                    );
                }

                // 4. 写时复制 (Copy - On - Write)
                // 创建一个新字典，大小扩容一点防止频繁 Resize
                var newServices = new Dictionary<Type, IAsakiService>(_services);

                // 执行写入/覆盖
                newServices[type] = service;

                // 5. 原子交换 (Atomic Swap)
                // 将引用指向新字典。此时所有新的 Get<T> 调用都会看到新数据。
                // 旧字典会被 GC 回收 (只要没有读取者持有它)。
                _services = newServices;
            }
        }

        /// <summary>
        /// 获取或注册 (懒加载)。
        /// <para>包含双重检查锁定 (Double - Check)，线程安全。</para>
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了 IAsakiService 接口的类类型。</typeparam>
        /// <param name="factory">用于创建服务实例的工厂方法。</param>
        /// <returns>已存在的服务实例或新创建并注册的服务实例。</returns>
        public static T GetOrRegister<T>(Func<T> factory)
            where T : class, IAsakiService
        {
            // 1. 快速检查 (无锁)
            if (TryGet(out T existing))
                return existing;

            lock (_writeLock)
            {
                // 2. 二次检查 (有锁)
                if (TryGet(out existing))
                    return existing;

                // 3. 执行工厂创建 (注意：工厂应无副作用)
                T instance = factory();

                // 4. 注册 (复用内部逻辑)
                RegisterInternal(typeof(T), instance, false);

                return instance;
            }
        }

        // ========================================================================
        // 架构控制 API
        // ========================================================================

        /// <summary>
        /// [架构] 冻结容器。
        /// <para>应在初始化完毕 (Bootstrapper 结束) 后调用。</para>
        /// <para>冻结后禁止 Register，防止业务逻辑随处注册服务导致架构腐化。</para>
        /// </summary>
        public static void Freeze()
        {
            lock (_writeLock)
            {
                _isFrozen = true;
            }
        }

        /// <summary>
        /// [生命周期] 清空并销毁所有服务。
        /// </summary>
        public static void ClearAll()
        {
            Dictionary<Type, IAsakiService> oldSnapshot;

            lock (_writeLock)
            {
                // 1. 获取当前快照的引用
                oldSnapshot = _services;

                // 2. 立即置空 (后续读取将失败或返回空)
                _services = new Dictionary<Type, IAsakiService>();
                _isFrozen = false;
                _isReady = false;
            }

            // 3. 在锁外执行 Dispose，防止 Dispose 逻辑死锁
            foreach (var kvp in oldSnapshot)
            {
                IAsakiService service = kvp.Value;
                try
                {
                    // 优先调用模块销毁
                    if (service is IAsakiModule module)
                    {
                        module.OnDispose();
                    }
                    // 其次调用通用销毁 (需去重，如果既是Module又是Disposable)
                    else if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    // 记录异常但不中断清理流程
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    ALog.Error($"[AsakiContext] Error disposing service {kvp.Key.Name}: {ex}");
#endif
                }
            }
        }

        public static void Reset()
        {
            lock (_writeLock)
            {
                _services = new Dictionary<Type, IAsakiService>();
                _isFrozen = false;
                _isReady = false;
            }
        }
    }
}
