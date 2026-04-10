using System.Threading;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.UI;
using Asaki.Unity;
using Asaki.Unity.Bootstrapper;
using Asaki.Unity.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Asaki.Unity.Services.UI
{
    /// <summary>
    /// Asaki UI窗口基类，提供完整的窗口生命周期管理、动画支持和对象池集成。
    /// </summary>
    /// <remarks>
    /// <para>AsakiUIWindow是所有UI窗口的基类，提供以下核心功能：</para>
    /// <list type="bullet">
    /// <item><description>窗口生命周期：OnOpenAsync → OnRefresh → PlayEntryAnimation → Activate → OnCloseAsync → PlayExitAnimation</description></item>
    /// <item><description>对象池集成：自动处理池化对象的取出和归还</description></item>
    /// <item><description>焦点管理：自动处理UI焦点切换</description></item>
    /// <item><description>层级管理：支持窗口栈的覆盖和恢复行为</description></item>
    /// <item><description>框架初始化感知：继承AsakiMono的初始化机制</description></item>
    /// </list>
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class AsakiUIWindow : AsakiMono, IAsakiWindow, IAsakiPoolable
    {
        [Header("Focus Management")]
        [SerializeField]
        private Selectable _firstFocusObject;
        private GameObject _previousFocus;

        /// <summary>
        /// 获取或设置资源句柄（非池化模式下使用）
        /// </summary>
        public IAsakiUIResourceHandle ResHandle { get; set; }

        /// <summary>
        /// 获取CanvasGroup组件
        /// </summary>
        public CanvasGroup CanvasGroup { get; private set; }

        /// <summary>
        /// 获取或设置对象池键
        /// </summary>
        public string PoolKey { get; set; }

        /// <summary>
        /// 标记是否是池化对象（由UIManager在Spawn后赋值）
        /// </summary>
        public bool IsPooled { get; set; }

        /// <summary>
        /// 获取窗口是否已打开
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 获取窗口是否正在关闭
        /// </summary>
        public bool IsClosing { get; private set; }

        /// <summary>
        /// Awake阶段初始化
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();
            CanvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 框架就绪后的初始化
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
            // UI窗口的OnStart通常不需要额外操作
            // 因为窗口通过OnOpenAsync进行初始化
        }

        // ====================================================
        // IAsakiPoolable 生命周期
        // ====================================================

        /// <summary>
        /// 从对象池取出时调用
        /// </summary>
        public virtual void OnSpawn()
        {
            // [Pooling] 取出时重置基础状态
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1;
                CanvasGroup.blocksRaycasts = true;
            }
            gameObject.SetActive(true);
            IsClosing = false;
        }

        /// <summary>
        /// 归还到对象池时调用
        /// </summary>
        public virtual void OnDespawn()
        {
            // [Pooling] 回收时逻辑
            gameObject.SetActive(false);
            IsOpen = false;

            // 注意：不要在这里 Dispose ResHandle。
            // 如果是池化对象，ResHandle 由 PoolService/UIManager 持有。
            // 如果是非池化对象，Destroy 时会由 CloseInternal 处理。
        }

        // ====================================================
        // 核心流程控制 (Template Method)
        // ====================================================

        /// <summary>
        /// 异步打开窗口
        /// </summary>
        /// <param name="args">打开参数</param>
        /// <param name="token">取消令牌</param>
        public async UniTask OnOpenAsync(object args, CancellationToken token)
        {
            if (IsOpen)
            {
                ALog.Warn($"[{GetType().Name}] Window is already open.");
                return;
            }

            IsOpen = true;
            IsClosing = false;

            if (EventSystem.current != null)
            {
                _previousFocus = EventSystem.current.currentSelectedGameObject;
            }

            // 1. 基础状态设置
            gameObject.SetActive(true);
            // 暂时阻挡交互，防止动画过程中误触
            if (CanvasGroup != null)
                CanvasGroup.blocksRaycasts = false;

            // 2. [同步] 业务逻辑回调 (刷新数据)
            try
            {
                OnRefresh(args);
            }
            catch (System.Exception ex)
            {
                ALog.Error($"[{GetType().Name}] Error in OnRefresh: {ex}");
            }

            // 3. [异步] 入场动画
            try
            {
                await PlayEntryAnimation(token);
            }
            catch (System.Exception ex)
            {
                ALog.Error($"[{GetType().Name}] Error in PlayEntryAnimation: {ex}");
            }

            // 4. 动画结束，开启交互
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1;
                CanvasGroup.blocksRaycasts = true;
            }

            if (_firstFocusObject != null && EventSystem.current != null)
            {
                // 清除当前选择，防止 UGUI 状态残留
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(_firstFocusObject.gameObject);
            }

            // 5. 激活组件，触发 OnActivated 生命周期
            Activate();
        }

        /// <summary>
        /// 异步关闭窗口
        /// </summary>
        /// <param name="token">取消令牌</param>
        public virtual async UniTask OnCloseAsync(CancellationToken token)
        {
            if (!IsOpen || IsClosing)
                return;

            IsClosing = true;

            // 禁止交互
            if (CanvasGroup != null)
                CanvasGroup.blocksRaycasts = false;

            if (EventSystem.current != null && _previousFocus != null)
            {
                // 简单的有效性检查：上一个对象还在场景里且激活
                if (_previousFocus.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(_previousFocus);
                }
            }
            _previousFocus = null; // 清理引用

            // 1. [异步] 离场动画
            try
            {
                await PlayExitAnimation(token);
            }
            catch (System.Exception ex)
            {
                ALog.Error($"[{GetType().Name}] Error in PlayExitAnimation: {ex}");
            }

            // 2. 停用组件，触发 OnDeactivated 生命周期
            Deactivate();

            // 3. 销毁/回收逻辑
            CloseInternal();

            IsOpen = false;
            IsClosing = false;
        }

        // ====================================================
        // 子类扩展点
        // ====================================================

        /// <summary>
        /// 窗口刷新回调，子类重写以更新UI数据
        /// </summary>
        /// <param name="args">打开参数</param>
        protected virtual void OnRefresh(object args) { }

        protected virtual void OnRefresh<TArg>(TArg args)
        {
            OnRefresh((object)args);
        }

        /// <summary>
        /// 播放入场动画，子类重写以实现自定义动画
        /// </summary>
        /// <param name="token">取消令牌</param>
        protected virtual UniTask PlayEntryAnimation(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 播放离场动画，子类重写以实现自定义动画
        /// </summary>
        /// <param name="token">取消令牌</param>
        protected virtual UniTask PlayExitAnimation(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        // ====================================================
        // 内部逻辑与辅助
        // ====================================================

        /// <summary>
        /// 提供给 Button Click Event 的同步入口
        /// </summary>
        public void Close()
        {
            HandleCloseAsync().Forget();
        }

        private async UniTask HandleCloseAsync()
        {
            // 确保 Close 也走完整的动画流程
            await OnCloseAsync(CancellationToken.None);
        }

        private void CloseInternal()
        {
            if (IsPooled && !string.IsNullOrEmpty(PoolKey))
            {
                // [分支 A: 池化对象] -> 归还进池子

                // V5.1 最佳实践：通过 Context 获取服务实例
                if (AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService poolService))
                {
                    var pool = poolService.GetPool<GameObject>(PoolKey);
                    if (pool != null)
                        pool.Return(gameObject);
                    else
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    // 极端情况：服务已销毁 (比如游戏退出时)，直接销毁物体
                    Destroy(gameObject);
                }
            }
            else
            {
                // [分支 B: 普通对象] -> 销毁

                // 1. 释放句柄 (RefCount -1)
                ResHandle?.Dispose();
                ResHandle = null;

                // 2. 销毁 GameObject
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 立即释放资源（用于服务销毁时，跳过动画）
        /// </summary>
        internal void DisposeImmediately()
        {
            // 清理资源引用
            _previousFocus = null;

            // 如果是池化对象，归还到对象池
            if (IsPooled && !string.IsNullOrEmpty(PoolKey))
            {
                if (AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService poolService))
                {
                    var pool = poolService.GetPool<GameObject>(PoolKey);
                    if (pool != null)
                    {
                        pool.Return(gameObject);
                        return;
                    }
                }
            }
            else
            {
                // 非池化对象，释放资源句柄
                ResHandle?.Dispose();
                ResHandle = null;
            }

            // 销毁 GameObject
            Destroy(gameObject);
        }

        // 栈管理行为：被覆盖时
        public virtual void OnCover()
        {
            if (CanvasGroup != null)
                CanvasGroup.blocksRaycasts = false;
        }

        // 栈管理行为：恢复显示时
        public virtual void OnReveal()
        {
            if (CanvasGroup)
            {
                CanvasGroup.blocksRaycasts = true;
                gameObject.SetActive(true);
            }
        }

        // ====================================================
        // 生命周期覆盖
        // ====================================================

        /// <summary>
        /// 组件被激活时的回调
        /// </summary>
        protected override void OnActivated()
        {
            base.OnActivated();
            // UI窗口特有的激活逻辑可以在这里实现
        }

        /// <summary>
        /// 组件被停用时的回调
        /// </summary>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            // UI窗口特有的停用逻辑可以在这里实现
        }

        /// <summary>
        /// 资源清理
        /// </summary>
        protected override void Cleanup()
        {
            base.Cleanup();

            // 如果窗口仍然打开，确保正确关闭
            if (IsOpen && !IsClosing)
            {
                _previousFocus = null;
                ResHandle?.Dispose();
                ResHandle = null;
            }
        }

        // ====================================================
        // 静态工厂方法
        // ====================================================

        /// <summary>
        /// 创建UI窗口实例（支持对象池）
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="prefab">窗口预制体</param>
        /// <param name="parent">父变换</param>
        /// <param name="usePool">是否使用对象池</param>
        /// <returns>窗口实例</returns>
        public static T Create<T>(T prefab, Transform parent, bool usePool = false)
            where T : AsakiUIWindow
        {
            if (prefab == null)
            {
                ALog.Error($"[{typeof(T).Name}] Cannot create window - prefab is null.");
                return null;
            }

            T instance;

            if (usePool && AsakiContext.TryGet<IAsakiPoolService>(out var poolService))
            {
                string poolKey = $"UIWindow_{typeof(T).Name}";
                var pool = poolService.GetPool<GameObject>(poolKey);

                if (pool != null)
                {
                    var go = pool.Get();
                    instance = go.GetComponent<T>();
                    if (instance == null)
                    {
                        ALog.Error($"[{typeof(T).Name}] Pooled object missing component.");
                        pool.Return(go);
                        return null;
                    }
                    instance.PoolKey = poolKey;
                    instance.IsPooled = true;
                }
                else
                {
                    // 池子不存在，直接实例化
                    instance = Instantiate(prefab, parent);
                    instance.IsPooled = false;
                }
            }
            else
            {
                instance = Instantiate(prefab, parent);
                instance.IsPooled = false;
            }

            // 确保实例被正确初始化
            if (instance != null && AsakiBootstrapper.IsReady)
            {
                // 如果框架已就绪，手动触发初始化
                AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately(instance);
            }

            return instance;
        }
    }
}
