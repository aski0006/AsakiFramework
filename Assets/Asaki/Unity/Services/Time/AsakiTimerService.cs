using System;
using System.Collections.Generic;
using Asaki.Core.Logging;
using Asaki.Core.Time;
using UnityEngine;

namespace Asaki.Unity.Services.Time
{
    /// <summary>
    /// [Asaki Native] 高性能定时器服务 (V6.1)
    /// <para>1. 零分配 (Zero-Alloc): 基于 Struct 和 List 复用。</para>
    /// <para>2. O(1) 移除: 使用 Swap-Removal 算法。</para>
    /// <para>3. 资源安全: 实现 IDisposable 防止委托引用导致的内存泄漏。</para>
    /// <para>4. 全局管理: 支持标签分组、批量操作。</para>
    /// <para>5. 延迟删除: 使用软删除避免 Tick 中修改列表。</para>
    /// </summary>
    public class AsakiTimerService : IAsakiTimerService
    {
        // 内部数据结构：Struct 布局优化
        private struct TimerData
        {
            public int Id;
            public ulong Version;
            public string Tag;

            public float Duration;
            public float Elapsed;

            public bool IsLooped;
            public bool UseUnscaledTime;
            public bool IsPaused;
            public bool IsCancelled; // 软删除标记

            public Action OnComplete;
            public Action<float> OnUpdate;
        }

        private readonly List<TimerData> _timers;
        private readonly Dictionary<string, List<int>> _taggedTimers;
        private readonly List<int> _pendingRemoveIndices; // 延迟删除列表
        private int _idCounter = 0;
        private bool _isDisposed = false;
        private bool _isTicking = false; // 标记是否正在 Tick 中

#if UNITY_EDITOR
        private float _globalTimeScale = 1f;
#endif

        public AsakiTimerService(int initialCapacity = 64)
        {
            _timers = new List<TimerData>(initialCapacity);
            _taggedTimers = new Dictionary<string, List<int>>();
            _pendingRemoveIndices = new List<int>(initialCapacity);
        }

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            _timers.Clear();
            _taggedTimers.Clear();
            _pendingRemoveIndices.Clear();

            ALog.Info("[AsakiTimer] Service Disposed & Memory Released.");
        }

        #endregion

        #region IAsakiTickable

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }
            if (Mathf.Approximately(deltaTime, 0f))
            {
                return;
            }
            _isTicking = true;

#if UNITY_EDITOR
            deltaTime *= _globalTimeScale;
#endif

            float unscaledDt = UnityEngine.Time.unscaledDeltaTime;
#if UNITY_EDITOR
            unscaledDt *= _globalTimeScale;
#endif

            _pendingRemoveIndices.Clear();

            // 使用 while 循环，允许处理回调中新注册的计时器
            int i = _timers.Count - 1;
            while (i >= 0)
            {
                // 如果索引超出范围（可能因为回调添加了新计时器），跳过
                if (i >= _timers.Count)
                {
                    i--;
                    continue;
                }

                TimerData t = _timers[i];

                // 跳过已标记取消的计时器
                if (t.IsCancelled)
                {
                    _pendingRemoveIndices.Add(i);
                    i--;
                    continue;
                }

                // 暂停检查
                if (t.IsPaused)
                {
                    i--;
                    continue;
                }

                float dt = t.UseUnscaledTime ? unscaledDt : deltaTime;
                t.Elapsed += dt;

                // 先保存 Elapsed 更新到列表
                _timers[i] = t;

                if (t.OnUpdate != null)
                {
                    float progress = t.Duration <= 0 ? 1f : Mathf.Clamp01(t.Elapsed / t.Duration);
                    try
                    {
                        t.OnUpdate(progress);
                    }
                    catch (Exception ex)
                    {
                        ALog.Error($"[AsakiTimer] Update Callback Exception", ex);
                    }
                }

                // 重新获取数据，因为 Update 回调可能已修改它
                t = _timers[i];

                // 处理计时器完成（支持一帧内多次触发循环计时器）
                if (t.Elapsed >= t.Duration)
                {
                    while (t.Elapsed >= t.Duration && !t.IsCancelled)
                    {
                        try
                        {
                            t.OnComplete?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            ALog.Error("[AsakiTimer] Complete Callback Exception", ex);
                        }

                        // 重新获取数据，因为 Complete 回调可能已修改它
                        t = _timers[i];

                        if (t.IsCancelled)
                        {
                            break;
                        }

                        if (t.IsLooped)
                        {
                            // 循环：扣除周期，保留溢出时间以保持节奏
                            t.Elapsed -= t.Duration;
                            // 如果 Duration 极小可能导致死循环，加个最小值保护
                            if (t.Duration < 0.0001f)
                            {
                                t.Elapsed = 0;
                                _timers[i] = t;
                                break;
                            }
                            _timers[i] = t;
                        }
                        else
                        {
                            // 标记为待删除
                            t.IsCancelled = true;
                            _timers[i] = t;
                            _pendingRemoveIndices.Add(i);
                            break;
                        }
                    }
                }
                else
                {
                    // 未完成，保存状态
                    _timers[i] = t;
                }

                i--;
            }

            _isTicking = false;

            // 执行延迟删除
            ProcessPendingRemovals();
        }

        /// <summary>
        /// 处理延迟删除的计时器
        /// </summary>
        private void ProcessPendingRemovals()
        {
            if (_pendingRemoveIndices.Count == 0)
                return;

            // 按索引降序排序，确保从后向前删除
            _pendingRemoveIndices.Sort((a, b) => b.CompareTo(a));

            int lastIndex = -1;
            foreach (int index in _pendingRemoveIndices)
            {
                // 跳过重复索引
                if (index == lastIndex)
                    continue;

                RemoveAtSwap(index);
                lastIndex = index;
            }

            _pendingRemoveIndices.Clear();
        }

        #endregion

        #region 核心接口实现

        public AsakiTimerHandle Register(
            float duration,
            Action onComplete,
            Action<float> onUpdate = null,
            bool isLooped = false,
            bool useUnscaledTime = false,
            string tag = "")
        {
            if (_isDisposed)
                return default(AsakiTimerHandle);

            _idCounter++;
            // 简单处理 ID 溢出 (实际项目中 21亿次很难达到，或者使用 long)
            if (_idCounter < 0)
                _idCounter = 1;

            ulong version = (ulong)UnityEngine.Random.Range(1, int.MaxValue);

            TimerData timer = new TimerData
            {
                Id = _idCounter,
                Version = version,
                Tag = tag ?? "",
                Duration = duration,
                OnComplete = onComplete,
                OnUpdate = onUpdate,
                IsLooped = isLooped,
                UseUnscaledTime = useUnscaledTime,
                Elapsed = 0,
                IsPaused = false,
                IsCancelled = false,
            };

            int index = _timers.Count;
            _timers.Add(timer);

            if (!string.IsNullOrEmpty(tag))
            {
                if (!_taggedTimers.TryGetValue(tag, out var list))
                {
                    list = new List<int>();
                    _taggedTimers[tag] = list;
                }
                list.Add(index);
            }

            return new AsakiTimerHandle(timer.Id, timer.Version);
        }

        public void Cancel(AsakiTimerHandle handle)
        {
            if (_isDisposed)
                return;

            int index = FindIndex(handle);
            if (index != -1)
            {
                // 软删除：标记为取消
                TimerData t = _timers[index];
                if (!t.IsCancelled)
                {
                    t.IsCancelled = true;
                    _timers[index] = t;

                    // 如果不在 Tick 中，立即执行删除
                    if (!_isTicking)
                    {
                        RemoveAtSwap(index);
                    }
                }
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
                _timers[index] = t;
            }
        }

        #endregion

        #region 全局管理功能

        public void CancelAllByTag(string tag)
        {
            if (_isDisposed || string.IsNullOrEmpty(tag))
                return;

            if (_taggedTimers.TryGetValue(tag, out var indices))
            {
                foreach (int index in indices)
                {
                    if (index < _timers.Count)
                    {
                        TimerData t = _timers[index];
                        if (!t.IsCancelled)
                        {
                            t.IsCancelled = true;
                            _timers[index] = t;
                        }
                    }
                }
                _taggedTimers.Remove(tag);

                // 如果不在 Tick 中，立即执行删除
                if (!_isTicking)
                {
                    ProcessPendingRemovals();
                }
            }
        }

        public void PauseAllByTag(string tag, bool isPaused)
        {
            if (_isDisposed || string.IsNullOrEmpty(tag))
                return;

            if (_taggedTimers.TryGetValue(tag, out var indices))
            {
                foreach (int index in indices)
                {
                    if (index < _timers.Count)
                    {
                        TimerData t = _timers[index];
                        t.IsPaused = isPaused;
                        _timers[index] = t;
                    }
                }
            }
        }

        public void CancelAll()
        {
            if (_isDisposed)
                return;

            for (int i = 0; i < _timers.Count; i++)
            {
                TimerData t = _timers[i];
                t.IsCancelled = true;
                _timers[i] = t;
            }
            _taggedTimers.Clear();

            // 如果不在 Tick 中，立即执行删除
            if (!_isTicking)
            {
                ProcessPendingRemovals();
            }
        }

        public void PauseAll()
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                TimerData t = _timers[i];
                t.IsPaused = true;
                _timers[i] = t;
            }
        }

        public void ResumeAll()
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                TimerData t = _timers[i];
                t.IsPaused = false;
                _timers[i] = t;
            }
        }

        public int GetActiveTimerCount()
        {
            return _isDisposed ? 0 : _timers.Count;
        }

        public int GetTimerCountByTag(string tag)
        {
            if (_isDisposed || string.IsNullOrEmpty(tag))
                return 0;

            return _taggedTimers.TryGetValue(tag, out var list) ? list.Count : 0;
        }

        #endregion

        #region 内部工具

        private int FindIndex(AsakiTimerHandle handle)
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].Id == handle.Id && _timers[i].Version == handle.Version)
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
            if (index < 0 || index >= _timers.Count)
                return;

            TimerData removed = _timers[index];

            int lastIndex = _timers.Count - 1;
            TimerData movedTimer = default;
            if (index < lastIndex)
            {
                movedTimer = _timers[lastIndex];
                _timers[index] = movedTimer;
            }
            _timers.RemoveAt(lastIndex);

            // 更新被删除计时器的标签索引
            if (!string.IsNullOrEmpty(removed.Tag) && _taggedTimers.TryGetValue(removed.Tag, out var removedList))
            {
                removedList.Remove(index);
                if (removedList.Count == 0)
                {
                    _taggedTimers.Remove(removed.Tag);
                }
            }

            // 更新被移动计时器的标签索引（如果发生了移动）
            if (index < lastIndex && !string.IsNullOrEmpty(movedTimer.Tag))
            {
                if (_taggedTimers.TryGetValue(movedTimer.Tag, out var movedList))
                {
                    // 找到旧索引并更新为新索引
                    int oldIndexPos = movedList.IndexOf(lastIndex);
                    if (oldIndexPos != -1)
                    {
                        movedList[oldIndexPos] = index;
                    }
                }
            }
        }

        #endregion

        #region UNITY_EDITOR 专用功能

#if UNITY_EDITOR

        public List<AsakiTimerDebugInfo> GetAllTimerDebugInfos()
        {
            List<AsakiTimerDebugInfo> infos = new List<AsakiTimerDebugInfo>(_timers.Count);

            for (int i = 0; i < _timers.Count; i++)
            {
                TimerData t = _timers[i];
                infos.Add(new AsakiTimerDebugInfo
                {
                    Id = t.Id,
                    Version = t.Version,
                    Tag = t.Tag,
                    Duration = t.Duration,
                    Elapsed = t.Elapsed,
                    Progress = t.Duration <= 0 ? 1f : Mathf.Clamp01(t.Elapsed / t.Duration),
                    IsPaused = t.IsPaused,
                    IsLooped = t.IsLooped,
                    UseUnscaledTime = t.UseUnscaledTime,
                    HasCompleteCallback = t.OnComplete != null,
                    HasUpdateCallback = t.OnUpdate != null,
                    CallbackTargetType = t.OnComplete?.Target?.GetType().Name ?? "None"
                });
            }

            return infos;
        }

        public void ForceComplete(AsakiTimerHandle handle)
        {
            if (_isDisposed)
                return;

            int index = FindIndex(handle);
            if (index != -1)
            {
                TimerData t = _timers[index];
                t.Elapsed = t.Duration;
                _timers[index] = t;
            }
        }

        public void SetGlobalTimeScale(float scale)
        {
            _globalTimeScale = Mathf.Clamp(scale, 0f, 10f);
        }

        public float GetGlobalTimeScale()
        {
            return _globalTimeScale;
        }

#endif

        #endregion
    }
}
