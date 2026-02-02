using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Unity.Bootstrapper;
using UnityEngine;

namespace Asaki.Unity
{
    /// <summary>
    /// Asaki框架MonoBehaviour基类，提供统一的生命周期管理、组件缓存和实用工具方法。
    /// </summary>
    /// <remarks>
    /// <para>AsakiMono是Asaki框架中所有MonoBehaviour脚本的推荐基类，提供以下核心功能：</para>
    /// <list type="bullet">
    /// <item><description>框架就绪感知：通过<see cref="OnAsakiFrameworkReadyEvent"/>确保框架初始化完成后才执行业务逻辑</description></item>
    /// <item><description>生命周期扩展：提供OnAwake、OnStart等模板方法，分离Unity回调与业务逻辑</description></item>
    /// <item><description>组件缓存：自动缓存GetComponent结果，避免重复调用带来的性能开销</description></item>
    /// <item><description>安全执行：提供异常捕获机制，防止单个组件错误影响整个应用</description></item>
    /// </list>
    /// <para>使用示例：</para>
    /// <code>
    /// public class PlayerController : AsakiMono
    /// {
    ///     [SerializeField]
    ///     private float moveSpeed = 5f;
    ///
    ///     private Rigidbody _rb;
    ///
    ///     protected override void OnAwake()
    ///     {
    ///         // 使用缓存获取组件
    ///         _rb = GetCachedComponent&lt;Rigidbody&gt;();
    ///     }
    ///
    ///     protected override void OnUpdate()
    ///     {
    ///         // 业务逻辑只在框架就绪后执行
    ///         float horizontal = Input.GetAxis("Horizontal");
    ///         _rb.velocity = new Vector3(horizontal * moveSpeed, _rb.velocity.y, 0);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    /// <seealso cref="MonoBehaviour"/>
    /// <seealso cref="IAsakiHandler{OnAsakiFrameworkReadyEvent}"/>
    public abstract class AsakiMono : MonoBehaviour, IAsakiHandler<OnAsakiFrameworkReadyEvent>
    {
        /// <summary>
        /// 获取组件是否已被激活（框架已就绪且组件已启动）。
        /// </summary>
        /// <value>
        /// 如果框架已就绪且<see cref="OnStart"/>已被调用，则返回<c>true</c>；否则返回<c>false</c>。
        /// </value>
        /// <remarks>
        /// 此属性用于控制Update、FixedUpdate和LateUpdate的执行。
        /// 在框架就绪前，这些循环方法不会调用对应的OnXXX方法。
        /// </remarks>
        protected bool IsActivated { get; private set; }

        /// <summary>
        /// Unity Awake生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>此方法在脚本实例被加载时调用，执行以下操作：</para>
        /// <list type="number">
        /// <item><description>将<see cref="IsActivated"/>设置为<c>false</c></description></item>
        /// <item><description>调用<see cref="OnAwake"/>模板方法，供子类实现初始化逻辑</description></item>
        /// </list>
        /// <para>注意：此时框架可能尚未就绪，不应访问Asaki服务。</para>
        /// </remarks>
        /// <seealso cref="OnAwake"/>
        protected virtual void Awake()
        {
            IsActivated = false;
            OnAwake();
        }

        /// <summary>
        /// Unity OnEnable生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>此方法在组件启用时调用，执行以下操作：</para>
        /// <list type="number">
        /// <item><description>向Asaki事件总线注册此组件（<see cref="AsakiRegister"/>）</description></item>
        /// <item><description>调用<see cref="EnableComponent"/>模板方法</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="OnDisable"/>
        /// <seealso cref="EnableComponent"/>
        protected virtual void OnEnable()
        {
            this.AsakiRegister();
            EnableComponent();
        }

        /// <summary>
        /// 组件启用时的模板方法，供子类重写以执行启用逻辑。
        /// </summary>
        /// <remarks>
        /// <para>此方法在<see cref="OnEnable"/>中被调用，适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>注册事件监听</description></item>
        /// <item><description>启动协程或定时器</description></item>
        /// <item><description>恢复暂停的逻辑</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void EnableComponent()
        /// {
        ///     InputManager.OnMove += HandleMove;
        /// }
        /// </code>
        /// </example>
        protected virtual void EnableComponent() { }

        /// <summary>
        /// Unity OnDisable生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>此方法在组件禁用时调用，执行以下操作：</para>
        /// <list type="number">
        /// <item><description>从Asaki事件总线注销此组件（<see cref="AsakiUnregister"/>）</description></item>
        /// <item><description>调用<see cref="DisableComponent"/>模板方法</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="OnEnable"/>
        /// <seealso cref="DisableComponent"/>
        protected virtual void OnDisable()
        {
            this.AsakiUnregister();
            DisableComponent();
        }

        /// <summary>
        /// 组件禁用时的模板方法，供子类重写以执行禁用逻辑。
        /// </summary>
        /// <remarks>
        /// <para>此方法在<see cref="OnDisable"/>中被调用，适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>注销事件监听（防止内存泄漏）</description></item>
        /// <item><description>停止协程或定时器</description></item>
        /// <item><description>暂停正在进行的逻辑</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void DisableComponent()
        /// {
        ///     InputManager.OnMove -= HandleMove;
        /// }
        /// </code>
        /// </example>
        protected virtual void DisableComponent() { }

        /// <summary>
        /// Awake阶段的模板方法，供子类重写以执行早期初始化。
        /// </summary>
        /// <remarks>
        /// <para>此方法在<see cref="Awake"/>中被调用，适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>获取和缓存组件引用</description></item>
        /// <item><description>初始化内部数据结构</description></item>
        /// <item><description>读取序列化字段的默认值</description></item>
        /// </list>
        /// <para>注意：此时不应访问Asaki服务，因为框架可能尚未就绪。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnAwake()
        /// {
        ///     _rb = GetCachedComponent&lt;Rigidbody&gt;();
        ///     _collider = GetCachedComponent&lt;Collider&gt;();
        /// }
        /// </code>
        /// </example>
        protected virtual void OnAwake() { }

        /// <summary>
        /// 框架就绪后的启动模板方法，供子类重写以执行业务逻辑初始化。
        /// </summary>
        /// <remarks>
        /// <para>此方法在Asaki框架初始化完成后被调用，适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>访问Asaki服务（如配置、资源、网络等）</description></item>
        /// <item><description>订阅框架事件</description></item>
        /// <item><description>执行业务逻辑初始化</description></item>
        /// </list>
        /// <para>此方法被调用时，<see cref="IsActivated"/>已被设置为<c>true</c>。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnStart()
        /// {
        ///     var config = AsakiContext.Get&lt;IAsakiConfigService&gt;();
        ///     moveSpeed = config.GetFloat("Player.MoveSpeed", 5f);
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="OnEvent"/>
        protected virtual void OnStart() { }

        /// <summary>
        /// 处理Asaki框架就绪事件。
        /// </summary>
        /// <param name="e">框架就绪事件数据</param>
        /// <remarks>
        /// <para>此方法由Asaki事件总线调用，当框架初始化完成时触发。</para>
        /// <para>执行流程：</para>
        /// <list type="number">
        /// <item><description>将<see cref="IsActivated"/>设置为<c>true</c></description></item>
        /// <item><description>调用<see cref="OnStart"/>模板方法</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="OnStart"/>
        public void OnEvent(OnAsakiFrameworkReadyEvent e)
        {
            IsActivated = true;
            OnStart();
        }

        /// <summary>
        /// Unity Update生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>每帧调用一次，但仅在<see cref="IsActivated"/>为<c>true</c>时才会调用<see cref="OnUpdate"/>。</para>
        /// <para>使用<see cref="OnUpdate"/>替代此方法以实现业务逻辑。</para>
        /// </remarks>
        /// <seealso cref="OnUpdate"/>
        protected virtual void Update()
        {
            if (!IsActivated)
            {
                return;
            }

            OnUpdate();
        }

        /// <summary>
        /// Unity FixedUpdate生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>以固定时间间隔调用，适用于物理相关逻辑。</para>
        /// <para>仅在<see cref="IsActivated"/>为<c>true</c>时才会调用<see cref="OnFixedUpdate"/>。</para>
        /// </remarks>
        /// <seealso cref="OnFixedUpdate"/>
        protected virtual void FixedUpdate()
        {
            if (!IsActivated)
            {
                return;
            }

            OnFixedUpdate();
        }

        /// <summary>
        /// Unity LateUpdate生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>每帧在所有Update调用完成后调用，适用于摄像机跟随等逻辑。</para>
        /// <para>仅在<see cref="IsActivated"/>为<c>true</c>时才会调用<see cref="OnLateUpdate"/>。</para>
        /// </remarks>
        /// <seealso cref="OnLateUpdate"/>
        protected virtual void LateUpdate()
        {
            if (!IsActivated)
            {
                return;
            }
            OnLateUpdate();
        }

        /// <summary>
        /// Update阶段的业务逻辑模板方法。
        /// </summary>
        /// <remarks>
        /// <para>在<see cref="Update"/>中被调用，每帧执行一次。</para>
        /// <para>适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>处理输入</description></item>
        /// <item><description>更新UI</description></item>
        /// <item><description>执行游戏逻辑</description></item>
        /// </list>
        /// </remarks>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// FixedUpdate阶段的业务逻辑模板方法。
        /// </summary>
        /// <remarks>
        /// <para>在<see cref="FixedUpdate"/>中被调用，以固定时间间隔执行。</para>
        /// <para>适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>物理计算</description></item>
        /// <item><description>刚体控制</description></item>
        /// <item><description>需要固定时间步长的逻辑</description></item>
        /// </list>
        /// </remarks>
        protected virtual void OnFixedUpdate() { }

        /// <summary>
        /// LateUpdate阶段的业务逻辑模板方法。
        /// </summary>
        /// <remarks>
        /// <para>在<see cref="LateUpdate"/>中被调用，每帧在所有Update之后执行。</para>
        /// <para>适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>摄像机跟随</description></item>
        /// <item><description>在对象移动后执行的逻辑</description></item>
        /// <item><description>确保在其他更新完成后执行的操作</description></item>
        /// </list>
        /// </remarks>
        protected virtual void OnLateUpdate() { }

        #region Lifecycle Extensions

        /// <summary>
        /// Unity OnDestroy生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>在组件被销毁时调用，执行资源清理。</para>
        /// <para>调用<see cref="Cleanup"/>模板方法供子类执行自定义清理逻辑。</para>
        /// </remarks>
        /// <seealso cref="Cleanup"/>
        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        /// <summary>
        /// 资源清理模板方法，供子类重写以执行销毁逻辑。
        /// </summary>
        /// <remarks>
        /// <para>在<see cref="OnDestroy"/>中被调用，适用于以下场景：</para>
        /// <list type="bullet">
        /// <item><description>释放托管资源</description></item>
        /// <item><description>清除组件缓存</description></item>
        /// <item><description>取消订阅事件</description></item>
        /// <item><description>停止协程和定时器</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void Cleanup()
        /// {
        ///     ClearComponentCache();
        ///     _cancellationTokenSource?.Cancel();
        ///     _cancellationTokenSource?.Dispose();
        /// }
        /// </code>
        /// </example>
        protected virtual void Cleanup() { }

        /// <summary>
        /// Unity OnApplicationPause生命周期方法。
        /// </summary>
        /// <param name="pauseStatus">如果应用被暂停则为<c>true</c>，如果恢复则为<c>false</c></param>
        /// <remarks>
        /// <para>在应用暂停或恢复时调用。</para>
        /// <para>适用于移动端游戏，处理应用进入后台或返回前台时的逻辑。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnApplicationPause(bool pauseStatus)
        /// {
        ///     if (pauseStatus)
        ///     {
        ///         // 应用进入后台，保存游戏状态
        ///         SaveGameState();
        ///     }
        ///     else
        ///     {
        ///         // 应用返回前台
        ///         ResumeGame();
        ///     }
        /// }
        /// </code>
        /// </example>
        protected virtual void OnApplicationPause(bool pauseStatus) { }

        /// <summary>
        /// Unity OnApplicationFocus生命周期方法。
        /// </summary>
        /// <param name="hasFocus">如果应用获得焦点则为<c>true</c>，如果失去焦点则为<c>false</c></param>
        /// <remarks>
        /// <para>在应用获得或失去焦点时调用。</para>
        /// <para>适用于处理窗口焦点变化时的逻辑，如暂停游戏、静音等。</para>
        /// </remarks>
        protected virtual void OnApplicationFocus(bool hasFocus) { }

        /// <summary>
        /// Unity OnApplicationQuit生命周期方法。
        /// </summary>
        /// <remarks>
        /// <para>在应用退出时调用。</para>
        /// <para>适用于保存数据、释放资源等退出前的清理工作。</para>
        /// </remarks>
        protected virtual void OnApplicationQuit() { }

        #endregion

        #region Component Cache

        /// <summary>
        /// 组件缓存字典，用于存储已获取的组件引用。
        /// </summary>
        /// <remarks>
        /// <para>使用<see cref="Type"/>作为键，<see cref="Component"/>作为值。</para>
        /// <para>通过<see cref="GetCachedComponent{T}"/>和<see cref="ClearComponentCache"/>管理。</para>
        /// </remarks>
        private readonly Dictionary<Type, Component> _componentCache = new();

        /// <summary>
        /// 获取指定类型的组件，并缓存结果以避免重复调用GetComponent。
        /// </summary>
        /// <typeparam name="T">要获取的组件类型，必须继承自<see cref="Component"/></typeparam>
        /// <returns>找到的组件实例，如果未找到则返回<c>null</c></returns>
        /// <remarks>
        /// <para>此方法会缓存第一次获取的组件引用，后续调用直接返回缓存值。</para>
        /// <para>适用于需要频繁访问的组件，可显著提升性能。</para>
        /// <para>如果组件可能在运行时被添加或移除，请使用<see cref="GetComponent{T}"/>或调用<see cref="ClearComponentCache"/>。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// private Rigidbody _rb;
        ///
        /// protected override void OnAwake()
        /// {
        ///     _rb = GetCachedComponent&lt;Rigidbody&gt;();
        ///     if (_rb == null)
        ///     {
        ///         LogError("Rigidbody component not found!");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="GetCachedComponentInChildren{T}"/>
        /// <seealso cref="ClearComponentCache"/>
        protected T GetCachedComponent<T>()
            where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var component = GetComponent<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }

        /// <summary>
        /// 在子对象中获取指定类型的组件，并缓存结果。
        /// </summary>
        /// <typeparam name="T">要获取的组件类型，必须继承自<see cref="Component"/></typeparam>
        /// <returns>找到的组件实例，如果未找到则返回<c>null</c></returns>
        /// <remarks>
        /// <para>使用<see cref="GetComponentInChildren{T}"/>搜索组件，包括自身和所有子对象。</para>
        /// <para>结果会被缓存，后续调用直接返回缓存值。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// private SpriteRenderer _spriteRenderer;
        ///
        /// protected override void OnAwake()
        /// {
        ///     // 在子对象中查找SpriteRenderer
        ///     _spriteRenderer = GetCachedComponentInChildren&lt;SpriteRenderer&gt;();
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="GetCachedComponent{T}"/>
        /// <seealso cref="ClearComponentCache"/>
        protected T GetCachedComponentInChildren<T>()
            where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
            {
                return cached as T;
            }

            var component = GetComponentInChildren<T>();
            if (component != null)
            {
                _componentCache[type] = component;
            }

            return component;
        }

        /// <summary>
        /// 清除组件缓存。
        /// </summary>
        /// <remarks>
        /// <para>清除所有已缓存的组件引用。</para>
        /// <para>在以下场景调用：</para>
        /// <list type="bullet">
        /// <item><description>动态添加或移除组件后</description></item>
        /// <item><description>对象池回收对象时</description></item>
        /// <item><description>需要强制重新获取组件时</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void Cleanup()
        /// {
        ///     ClearComponentCache();
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="GetCachedComponent{T}"/>
        /// <seealso cref="GetCachedComponentInChildren{T}"/>
        protected void ClearComponentCache()
        {
            _componentCache.Clear();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 安全执行操作，捕获并记录任何异常。
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="context">可选的上下文信息，用于错误日志</param>
        /// <remarks>
        /// <para>此方法包装操作在try-catch块中，防止单个操作错误影响其他逻辑。</para>
        /// <para>异常会被记录到日志，包含类名、上下文信息和堆栈跟踪。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnUpdate()
        /// {
        ///     SafeExecute(() =>
        ///     {
        ///         // 可能抛出异常的操作
        ///         var data = ParseComplexData(input);
        ///         ApplyData(data);
        ///     }, "DataProcessing");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="SafeExecute{T}"/>
        protected void SafeExecute(Action action, string context = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                ALog.Error(
                    $"[{GetType().Name}] {(context != null ? $"[{context}] " : "")}Error: {e.Message}\n{e.StackTrace}",
                    e
                );
            }
        }

        /// <summary>
        /// 安全执行带返回值的操作，捕获并记录任何异常。
        /// </summary>
        /// <typeparam name="T">返回值的类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">发生异常时返回的默认值</param>
        /// <param name="context">可选的上下文信息，用于错误日志</param>
        /// <returns>函数的执行结果，如果发生异常则返回<paramref name="defaultValue"/></returns>
        /// <remarks>
        /// <para>此方法与<see cref="SafeExecute"/>类似，但支持返回值。</para>
        /// <para>当发生异常时，返回指定的默认值而不是抛出异常。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnStart()
        /// {
        ///     var configValue = SafeExecute(() =>
        ///     {
        ///         return ConfigService.GetInt("Player.MaxHealth");
        ///     }, defaultValue: 100, "ConfigLoading");
        ///
        ///     _maxHealth = configValue;
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="SafeExecute"/>
        protected T SafeExecute<T>(Func<T> func, T defaultValue = default, string context = null)
        {
            try
            {
                return func != null ? func.Invoke() : defaultValue;
            }
            catch (Exception e)
            {
                ALog.Error(
                    $"[{GetType().Name}] {(context != null ? $"[{context}] " : "")}Error: {e.Message}\n{e.StackTrace}",
                    e
                );
                return defaultValue;
            }
        }

        /// <summary>
        /// 检查Unity对象是否为空或已被销毁。
        /// </summary>
        /// <param name="obj">要检查的Unity对象</param>
        /// <returns>如果对象为null或已被销毁则返回<c>true</c>；否则返回<c>false</c></returns>
        /// <remarks>
        /// <para>Unity的重载==操作符在对象被销毁后会返回true，但对象引用本身不为null。</para>
        /// <para>此方法同时检查null和销毁状态，是判断Unity对象有效性的可靠方式。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// [SerializeField]
        /// private GameObject targetObject;
        ///
        /// protected override void OnUpdate()
        /// {
        ///     if (IsNullOrDestroyed(targetObject))
        ///     {
        ///         LogWarning("Target object is missing!");
        ///         return;
        ///     }
        ///
        ///     // 安全使用targetObject
        ///     targetObject.transform.position = transform.position;
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="DestroySafely"/>
        /// <seealso cref="DestroyImmediateSafely"/>
        protected bool IsNullOrDestroyed(UnityEngine.Object obj)
        {
            return obj == null || obj.Equals(null);
        }

        /// <summary>
        /// 安全销毁Unity对象。
        /// </summary>
        /// <param name="obj">要销毁的对象</param>
        /// <param name="delay">延迟销毁的时间（秒），0表示立即销毁</param>
        /// <remarks>
        /// <para>此方法会先检查对象是否有效，避免对null或已销毁对象调用Destroy。</para>
        /// <para>支持延迟销毁，适用于需要延迟清理的场景。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnStart()
        /// {
        ///     // 创建临时特效，3秒后自动销毁
        ///     var effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        ///     DestroySafely(effect, 3f);
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="IsNullOrDestroyed"/>
        /// <seealso cref="DestroyImmediateSafely"/>
        protected void DestroySafely(UnityEngine.Object obj, float delay = 0f)
        {
            if (IsNullOrDestroyed(obj))
            {
                return;
            }

            if (delay > 0)
            {
                Destroy(obj, delay);
            }
            else
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// 安全立即销毁Unity对象。
        /// </summary>
        /// <param name="obj">要销毁的对象</param>
        /// <remarks>
        /// <para>使用<see cref="DestroyImmediate"/>立即销毁对象，不等待帧结束。</para>
        /// <para>注意：在运行时通常应使用<see cref="DestroySafely"/>，DestroyImmediate主要用于编辑器代码。</para>
        /// </remarks>
        /// <seealso cref="IsNullOrDestroyed"/>
        /// <seealso cref="DestroySafely"/>
        protected void DestroyImmediateSafely(UnityEngine.Object obj)
        {
            if (!IsNullOrDestroyed(obj))
            {
                DestroyImmediate(obj);
            }
        }

        #endregion

        #region Activation Control

        /// <summary>
        /// 激活组件，允许Update等循环方法执行业务逻辑。
        /// </summary>
        /// <remarks>
        /// <para>将<see cref="IsActivated"/>设置为<c>true</c>，并调用<see cref="OnActivated"/>。</para>
        /// <para>如果组件已经是激活状态，则不做任何操作。</para>
        /// </remarks>
        /// <seealso cref="Deactivate"/>
        /// <seealso cref="OnActivated"/>
        protected void Activate()
        {
            if (IsActivated)
                return;

            IsActivated = true;
            OnActivated();
        }

        /// <summary>
        /// 停用组件，阻止Update等循环方法执行业务逻辑。
        /// </summary>
        /// <remarks>
        /// <para>将<see cref="IsActivated"/>设置为<c>false</c>，并调用<see cref="OnDeactivated"/>。</para>
        /// <para>如果组件已经是停用状态，则不做任何操作。</para>
        /// </remarks>
        /// <seealso cref="Activate"/>
        /// <seealso cref="OnDeactivated"/>
        protected void Deactivate()
        {
            if (!IsActivated)
                return;

            IsActivated = false;
            OnDeactivated();
        }

        /// <summary>
        /// 组件被激活时的回调方法。
        /// </summary>
        /// <remarks>
        /// <para>在<see cref="Activate"/>中被调用，供子类实现激活逻辑。</para>
        /// </remarks>
        /// <seealso cref="Activate"/>
        /// <seealso cref="OnDeactivated"/>
        protected virtual void OnActivated() { }

        /// <summary>
        /// 组件被停用时的回调方法。
        /// </summary>
        /// <remarks>
        /// <para>在<see cref="Deactivate"/>中被调用，供子类实现停用逻辑。</para>
        /// </remarks>
        /// <seealso cref="Deactivate"/>
        /// <seealso cref="OnActivated"/>
        protected virtual void OnDeactivated() { }

        #endregion
    }
}
