using System;
using System.Collections.Generic;
using Asaki.Core.Logging;
using Asaki.Core.Time;
using UnityEngine;

namespace Asaki.Unity.Services.Time
{
    /// <summary>
    /// [Asaki Native] 高性能定时器服务 (V5.0)
    /// <para>1. 零分配 (Zero-Alloc): 基于 Struct 和 List 复用。</para>
    /// <para>2. O(1) 移除: 使用 Swap-Removal 算法。</para>
    /// <para>3. 资源安全: 实现 IDisposable 防止委托引用导致的内存泄漏。</para>
    /// </summary>
    public class AsakiTimerService : IAsakiTimerService
    {
        // 内部数据结构：Struct 布局优化
        private struct TimerData
        {
            public int Id;
            public ulong Version; // 版本号，防止 ID 复用冲突

            public float Duration;
            public float Elapsed;

            // 状态标记位 (使用 bool 或位域)
            public bool IsLooped;
            public bool UseUnscaledTime;
            public bool IsPaused;
            public bool IsCancelled; // 标记软删除

            // 引用类型 (GC 根节点)
            public Action OnComplete;
            public Action<float> OnUpdate;
        }

        private readonly List<TimerData> _timers;
        private int _idCounter = 0;
        private bool _isDisposed = false;

        // 构造函数
        public AsakiTimerService(int initialCapacity = 64)
        {
            _timers = new List<TimerData>(initialCapacity);
        }

        // =========================================================
        // IDisposable 资源管理 (核心)
        // =========================================================

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            // [关键] 清理所有定时器数据
            // 这会释放所有 Action 委托对外部对象的引用，允许 GC 回收这些对象
            _timers.Clear();

            // 如果有对象池，在这里归还
            // _timerPool.Clear();

            ALog.Info("[AsakiTimer] Service Disposed & Memory Released.");
        }

        // =========================================================
        // IAsakiTickable 驱动逻辑
        // =========================================================

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
                return;

            float unscaledDt = UnityEngine.Time.unscaledDeltaTime;
            int count = _timers.Count;

            // 倒序遍历：支持在遍历过程中安全的删除元素
            for (int i = count - 1; i >= 0; i--)
            {
                // [注意] List<Struct> 的索引访问是值拷贝
                // 修改必须遵循：读出 -> 修改 -> 写回
                TimerData t = _timers[i];

                // 1. 检查取消/暂停状态
                if (t.IsCancelled)
                {
                    RemoveAtSwap(i);
                    continue;
                }

                if (t.IsPaused)
                    continue;

                // 2. 时间步进
                float dt = t.UseUnscaledTime ? unscaledDt : deltaTime;
                t.Elapsed += dt;

                // 3. 执行 Update 回调 (如果存在)
                if (t.OnUpdate != null)
                {
                    // 保护性计算进度
                    float progress = t.Duration <= 0 ? 1f : Mathf.Clamp01(t.Elapsed / t.Duration);
                    try
                    {
                        t.OnUpdate(progress);
                    }
                    catch (Exception ex)
                    {
                        ALog.Error($"[AsakiTimer] Update Callback Exception", ex); // 即使回调报错，定时器逻辑不能崩
                    }
                }

                // 4. 检查完成
                if (t.Elapsed >= t.Duration)
                {
                    // 执行完成回调
                    try
                    {
                        t.OnComplete?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ALog.Error("[AsakiTimer] Complete Callback Exception", ex); // 即使回调报错，定时器逻辑不能崩
                    }

                    // 再次获取数据 (防止回调中修改了定时器状态，虽然 Struct 拷贝避免了这个问题，但逻辑上要注意)
                    // 在 Struct 模式下，回调里的修改通常是通过 Handle 走的，会修改 List 里的源数据
                    // 所以这里我们只需要决定是 循环 还是 移除

                    // 检查是否在回调中被取消了
                    if (_timers[i].IsCancelled)
                    {
                        RemoveAtSwap(i);
                        continue;
                    }

                    if (t.IsLooped)
                    {
                        // 循环：扣除周期，保留溢出时间以保持节奏
                        t.Elapsed %= t.Duration;
                        // 如果 Duration 极小可能导致死循环，建议加个最小值保护
                        if (t.Duration < 0.0001f)
                            t.Elapsed = 0;

                        _timers[i] = t; // 写回状态
                    }
                    else
                    {
                        // 完成且不循环：移除
                        RemoveAtSwap(i);
                    }
                }
                else
                {
                    _timers[i] = t; // 写回状态 (Elapsed 更新)
                }
            }
        }

        // =========================================================
        // 接口实现
        // =========================================================

        public AsakiTimerHandle Register(
            float duration,
            Action onComplete,
            Action<float> onUpdate = null,
            bool isLooped = false,
            bool useUnscaledTime = false
        )
        {
            if (_isDisposed)
                return default(AsakiTimerHandle);

            _idCounter++;
            // 简单处理 ID 溢出 (实际项目中 21亿次很难达到，或者使用 long)
            if (_idCounter < 0)
                _idCounter = 1;

            // 在 V5.0 中，我们利用 struct 的 Create 模式
            TimerData timer = new TimerData
            {
                Id = _idCounter,
                Version = 1, // 简化版，实际可配合 Slot Map 算法
                Duration = duration,
                OnComplete = onComplete,
                OnUpdate = onUpdate,
                IsLooped = isLooped,
                UseUnscaledTime = useUnscaledTime,
                Elapsed = 0,
                IsPaused = false,
                IsCancelled = false,
            };

            _timers.Add(timer);

            return new AsakiTimerHandle(timer.Id, timer.Version);
        }

        public void Cancel(AsakiTimerHandle handle)
        {
            if (_isDisposed)
                return;

            int index = FindIndex(handle);
            if (index != -1)
            {
                // 标记删除 (Soft Delete)，在 Tick 中统一清理
                // 这样避免了 RemoveAtSwap 破坏当前 Tick 循环的索引顺序 (虽然倒序遍历不怕，但软删除更稳健)
                // 也可以直接移除：
                RemoveAtSwap(index);
            }
        }

        public void Pause(AsakiTimerHandle handle, bool isPaused)
        {
            if (_isDisposed)
                return;

            int index = FindIndex(handle);
            if (index != -1)
            {
                TimerData t = _timers[index];
                t.IsPaused = isPaused;
                _timers[index] = t; // 写回
            }
        }

        // =========================================================
        // 内部工具
        // =========================================================

        private int FindIndex(AsakiTimerHandle handle)
        {
            // 线性查找 O(N)。
            // 在 Timer 数量 < 200 时，比 Dictionary 更快且 0 GC。
            for (int i = 0; i < _timers.Count; i++)
            {
                // 只要 ID 匹配即可 (Version 校验留作扩展)
                if (_timers[i].Id == handle.Id)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// O(1) 移除算法
        /// 将最后一个元素移动到要删除的位置，然后移除最后一个。
        /// </summary>
        private void RemoveAtSwap(int index)
        {
            int lastIndex = _timers.Count - 1;
            if (index < lastIndex)
            {
                _timers[index] = _timers[lastIndex];
            }
            _timers.RemoveAt(lastIndex);
        }
    }
}
