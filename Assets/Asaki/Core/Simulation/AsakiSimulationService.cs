using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Asaki.Core.Simulation
{
    /// <summary>
    /// Unity仿真时间管理器实现
    /// 支持优先级排序与延迟更新控制
    /// </summary>
    public class AsakiSimulationService : IAsakiSimulationService, IDisposable
    {
        // =========================================================
        // 内部包装结构
        // =========================================================

        public struct TickableWrapper
        {
            public IAsakiTickable Tickable;
            public int Priority;
        }

        public struct LateTickableWrapper
        {
            public IAsakiLateTickable Tickable;
            public int Priority;
        }

        // =========================================================
        // 数据存储
        // =========================================================

        private readonly List<TickableWrapper> _tickables = new List<TickableWrapper>();
        private readonly List<IAsakiFixedTickable> _fixedTickables =
            new List<IAsakiFixedTickable>();
        private readonly List<LateTickableWrapper> _lateTickables = new List<LateTickableWrapper>();

        private readonly HashSet<IAsakiTickable> _tickableSet = new HashSet<IAsakiTickable>();
        private readonly HashSet<IAsakiFixedTickable> _fixedTickableSet =
            new HashSet<IAsakiFixedTickable>();
        private readonly HashSet<IAsakiLateTickable> _lateTickableSet =
            new HashSet<IAsakiLateTickable>();

        private bool _isTickDirty = false;
        private bool _isLateTickDirty = false;

        // =========================================================
        // 状态控制
        // =========================================================

        public bool IsPaused { get; set; } = false;

        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = value < 0f ? 0f : value;
        }
        private float _timeScale = 1f;

        public void Pause() => IsPaused = true;

        public void Resume() => IsPaused = false;

        // =========================================================
        // 统计信息
        // =========================================================

        public int TickableCount => _tickables.Count;
        public int FixedTickableCount => _fixedTickables.Count;
        public int LateTickableCount => _lateTickables.Count;

#if UNITY_EDITOR
        // =========================================================
        // 编辑器访问接口 (仅编辑器可用)
        // =========================================================

        /// <summary>
        /// 获取所有已注册的Tick对象（只读）
        /// </summary>
        public ReadOnlyCollection<TickableWrapper> GetTickables() => _tickables.AsReadOnly();

        /// <summary>
        /// 获取所有已注册的FixedTick对象（只读）
        /// </summary>
        public ReadOnlyCollection<IAsakiFixedTickable> GetFixedTickables() =>
            _fixedTickables.AsReadOnly();

        /// <summary>
        /// 获取所有已注册的LateTick对象（只读）
        /// </summary>
        public ReadOnlyCollection<LateTickableWrapper> GetLateTickables() =>
            _lateTickables.AsReadOnly();

        /// <summary>
        /// 获取Tick对象总数
        /// </summary>
        public int GetTotalTickableCount() =>
            _tickables.Count + _fixedTickables.Count + _lateTickables.Count;

        /// <summary>
        /// 获取Tick对象统计信息
        /// </summary>
        public (int tickCount, int fixedTickCount, int lateTickCount) GetTickableStats() =>
            (_tickables.Count, _fixedTickables.Count, _lateTickables.Count);
#endif

        // =========================================================
        // 注册与注销
        // =========================================================

        public void Register(IAsakiTickable tickable, int priority = (int)TickPriority.Normal)
        {
            if (tickable == null)
                return;

            if (!_tickableSet.Add(tickable))
                return;

            _tickables.Add(new TickableWrapper { Tickable = tickable, Priority = priority });
            _isTickDirty = true;
        }

        public void Register(IAsakiFixedTickable tickable)
        {
            if (tickable == null)
                return;

            if (!_fixedTickableSet.Add(tickable))
                return;

            _fixedTickables.Add(tickable);
        }

        public void Register(IAsakiLateTickable tickable, int priority = (int)TickPriority.Normal)
        {
            if (tickable == null)
                return;

            if (!_lateTickableSet.Add(tickable))
                return;

            _lateTickables.Add(
                new LateTickableWrapper { Tickable = tickable, Priority = priority }
            );
            _isLateTickDirty = true;
        }

        public void Unregister(IAsakiTickable tickable)
        {
            if (tickable == null)
                return;

            if (!_tickableSet.Remove(tickable))
                return;

            for (int i = 0; i < _tickables.Count; i++)
            {
                if (_tickables[i].Tickable == tickable)
                {
                    _tickables.RemoveAt(i);
                    _isTickDirty = true;
                    return;
                }
            }
        }

        public void Unregister(IAsakiFixedTickable tickable)
        {
            if (tickable == null)
                return;

            if (!_fixedTickableSet.Remove(tickable))
                return;

            _fixedTickables.Remove(tickable);
        }

        public void Unregister(IAsakiLateTickable tickable)
        {
            if (tickable == null)
                return;

            if (!_lateTickableSet.Remove(tickable))
                return;

            for (int i = 0; i < _lateTickables.Count; i++)
            {
                if (_lateTickables[i].Tickable == tickable)
                {
                    _lateTickables.RemoveAt(i);
                    _isLateTickDirty = true;
                    return;
                }
            }
        }

        // =========================================================
        // 驱动方法
        // =========================================================

        public void Tick(float deltaTime)
        {
            if (IsPaused)
                return;

            float scaledDelta = deltaTime * _timeScale;

            if (_isTickDirty)
            {
                _tickables.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _isTickDirty = false;
            }

            for (int i = 0; i < _tickables.Count; i++)
            {
                _tickables[i].Tickable.Tick(scaledDelta);
            }
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (IsPaused)
                return;

            float scaledDelta = fixedDeltaTime * _timeScale;

            for (int i = 0; i < _fixedTickables.Count; i++)
            {
                _fixedTickables[i].FixedTick(scaledDelta);
            }
        }

        public void LateTick(float lateDeltaTime)
        {
            if (IsPaused)
                return;

            float scaledDelta = lateDeltaTime * _timeScale;

            if (_isLateTickDirty)
            {
                _lateTickables.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _isLateTickDirty = false;
            }

            for (int i = 0; i < _lateTickables.Count; i++)
            {
                _lateTickables[i].Tickable.LateTick(scaledDelta);
            }
        }

        // =========================================================
        // IDisposable
        // =========================================================

        public void Dispose()
        {
            _tickables.Clear();
            _fixedTickables.Clear();
            _lateTickables.Clear();
            _tickableSet.Clear();
            _fixedTickableSet.Clear();
            _lateTickableSet.Clear();
            _isTickDirty = false;
            _isLateTickDirty = false;
            IsPaused = false;
            _timeScale = 1f;
        }
    }
}
