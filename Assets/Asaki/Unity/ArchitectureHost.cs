using System;
using Asaki.Core.Architecture;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Unity
{
    /// <summary>
    /// Architecture的MonoBehaviour包装器，用于实现跨场景持久化。
    /// 继承AsakiMono并实现IAsakiGlobalService，使Architecture能够作为全局服务存在。
    /// </summary>
    /// <typeparam name="T">Architecture类型，必须继承自AsakiArchitecture并具有无参构造函数</typeparam>
    /// <remarks>
    /// <para>【使用方式】</para>
    /// <para>1. 创建一个继承自ArchitectureHost的MonoBehaviour脚本</para>
    /// <para>2. 将其挂载到一个GameObject上，该GameObject会在场景切换时自动持久化</para>
    /// <para>3. Architecture会在框架初始化时自动创建并注入</para>
    /// </remarks>
    public class ArchitectureHost<T> : AsakiMono, IAsakiGlobalService
        where T : AsakiArchitecture, new()
    {
        /// <summary>
        /// Architecture实例，提供对Model和System的访问
        /// </summary>
        public T Architecture { get; private set; }

        /// <summary>
        /// 获取Architecture是否已完成初始化
        /// </summary>
        public bool IsArchitectureInitialized { get; private set; }

        /// <summary>
        /// Architecture类型信息，用于调试和日志输出
        /// </summary>
        private readonly Type _architectureType = typeof(T);

        /// <summary>
        /// 线程安全锁，用于保护Architecture实例的创建和销毁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 标记是否已被销毁，防止重复销毁操作
        /// </summary>
        private bool _isDestroyed;

        /// <summary>
        /// 在Awake阶段创建Architecture实例并设置跨场景持久化
        /// </summary>
        protected override void OnAwake()
        {
            lock (_lock)
            {
                if (_isDestroyed)
                {
                    ALog.Warn(
                        $"[ArchitectureHost] Attempted to initialize after destruction: {_architectureType.Name}"
                    );
                    return;
                }

                try
                {
                    // 创建Architecture实例
                    Architecture = new T();
                    IsArchitectureInitialized = false;

                    // 设置跨场景持久化
                    DontDestroyOnLoad(gameObject);

                    ALog.Info(
                        $"[ArchitectureHost] Created Architecture instance: {_architectureType.Name}"
                    );
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[ArchitectureHost] Failed to create Architecture instance: {_architectureType.Name}, Error: {ex}"
                    );
                }
            }
        }

        /// <summary>
        /// 在引导程序初始化阶段调用，触发Architecture的Inject方法
        /// </summary>
        /// <remarks>
        /// 此方法由AsakiBootstrapper统一调用，确保所有全局服务按正确顺序初始化。
        /// 如果Architecture已经初始化，则跳过重复初始化。
        /// </remarks>
        public void OnBootstrapInit()
        {
            lock (_lock)
            {
                if (_isDestroyed)
                {
                    ALog.Warn(
                        $"[ArchitectureHost] Attempted to initialize after destruction: {_architectureType.Name}"
                    );
                    return;
                }

                if (IsArchitectureInitialized)
                {
                    ALog.Warn(
                        $"[ArchitectureHost] Architecture already initialized: {_architectureType.Name}"
                    );
                    return;
                }

                if (Architecture == null)
                {
                    ALog.Error(
                        $"[ArchitectureHost] Architecture instance is null during bootstrap init: {_architectureType.Name}"
                    );
                    return;
                }

                try
                {
                    // 触发Architecture的Inject，使用全局Resolver
                    Architecture.Inject(AsakiGlobalResolver.Instance);
                    IsArchitectureInitialized = true;

                    ALog.Info(
                        $"[ArchitectureHost] Architecture initialized successfully: {_architectureType.Name}"
                    );
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[ArchitectureHost] Failed to initialize Architecture: {_architectureType.Name}, Error: {ex}"
                    );
                    IsArchitectureInitialized = false;
                }
            }
        }

        /// <summary>
        /// 清理资源，销毁Architecture实例
        /// </summary>
        /// <remarks>
        /// 在MonoBehaviour销毁时自动调用，确保Architecture正确释放资源。
        /// 此方法在锁内执行，保证线程安全。
        /// </remarks>
        protected override void Cleanup()
        {
            lock (_lock)
            {
                if (_isDestroyed)
                {
                    return;
                }

                _isDestroyed = true;

                if (Architecture != null)
                {
                    try
                    {
                        Architecture.Dispose();
                        ALog.Info(
                            $"[ArchitectureHost] Architecture disposed: {_architectureType.Name}"
                        );
                    }
                    catch (Exception ex)
                    {
                        ALog.Error(
                            $"[ArchitectureHost] Error disposing Architecture: {_architectureType.Name}, Error: {ex}"
                        );
                    }
                    finally
                    {
                        Architecture = null;
                        IsArchitectureInitialized = false;
                    }
                }
            }
        }

        /// <summary>
        /// 获取Architecture实例的字符串表示，用于调试
        /// </summary>
        /// <returns>包含Architecture类型和初始化状态的字符串</returns>
        public override string ToString()
        {
            return $"[ArchitectureHost<{_architectureType.Name}>: Initialized={IsArchitectureInitialized}]";
        }
    }
}
