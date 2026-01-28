using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Context; // [新增] 用于获取服务
using Asaki.Core.Pooling;
using Asaki.Core.UI;
using Asaki.Unity.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// [移除] 旧的静态引用
// using AsakiSmartPool = Asaki.Core.Pooling.AsakiSmartPool;

namespace Asaki.Unity.Services.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class AsakiUIWindow : MonoBehaviour, IAsakiWindow, IAsakiPoolable
    {
        [Header("Focus Management")]
        [SerializeField]
        private Selectable _firstFocusObject;
        private GameObject _previousFocus;

        // 持有抽象句柄 (非池化模式下使用)
        public IUIResourceHandle ResHandle { get; set; }

        public CanvasGroup CanvasGroup { get; private set; }

        public string PoolKey { get; set; }

        // 标记是否是池化对象 (由 UIManager 在 Spawn 后赋值)
        public bool IsPooled { get; set; }

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
        }

        // ====================================================
        // IAsakiPoolable 生命周期
        // ====================================================

        public virtual void OnSpawn()
        {
            // [Pooling] 取出时重置基础状态
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1;
                CanvasGroup.blocksRaycasts = true;
            }
            gameObject.SetActive(true);
        }

        public virtual void OnDespawn()
        {
            // [Pooling] 回收时逻辑
            gameObject.SetActive(false);

            // 注意：不要在这里 Dispose ResHandle。
            // 如果是池化对象，ResHandle 由 PoolService/UIManager 持有。
            // 如果是非池化对象，Destroy 时会由 CloseInternal 处理。
        }

        // ====================================================
        // 核心流程控制 (Template Method)
        // ====================================================

        public async Task OnOpenAsync(object args, CancellationToken token)
        {
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
            OnRefresh(args);

            // 3. [异步] 入场动画
            await PlayEntryAnimation(token);

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
        }

        public async Task OnCloseAsync(CancellationToken token)
        {
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
            await PlayExitAnimation(token);

            // 2. 销毁/回收逻辑
            CloseInternal();
        }

        // ====================================================
        // 子类扩展点
        // ====================================================

        protected virtual void OnRefresh(object args) { }

        protected virtual Task PlayEntryAnimation(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected virtual Task PlayExitAnimation(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        // ====================================================
        // 内部逻辑与辅助
        // ====================================================

        // 提供给 Button Click Event 的同步入口
        public void Close()
        {
            HandleCloseAsync().FireAndForget();
        }

        private async Task HandleCloseAsync()
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
                    poolService.Despawn(gameObject, PoolKey);
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
    }
}
