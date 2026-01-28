using System.Collections.Generic;

namespace Asaki.Core.Simulation
{
    /// <summary>
    /// Unity仿真时间管理器实现
    /// 支持优先级排序与延迟更新控制
    /// </summary>
    public class AsakiSimulationService : IAsakiSimulationService
    {
        // =========================================================
        // 内部包装结构
        // =========================================================

        private struct TickableWrapper
        {
            public IAsakiTickable Tickable;
            public int Priority;
        }

        private struct LateTickableWrapper
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

        // 脏标记控制排序触发频率
        private bool _isTickDirty = false;
        private bool _isLateTickDirty = false;

        // =========================================================
        // 注册与注销
        // =========================================================

        public void Register(IAsakiTickable tickable, int priority = (int)TickPriority.Normal)
        {
            if (tickable == null)
                return;

            // 查重 (O(N)，启动阶段可接受)
            for (int i = 0; i < _tickables.Count; i++)
            {
                if (_tickables[i].Tickable == tickable)
                    return;
            }

            _tickables.Add(new TickableWrapper { Tickable = tickable, Priority = priority });
            _isTickDirty = true;
        }

        public void Register(IAsakiFixedTickable tickable)
        {
            if (tickable == null)
                return;
            if (!_fixedTickables.Contains(tickable))
            {
                _fixedTickables.Add(tickable);
            }
        }

        public void Register(IAsakiLateTickable tickable, int priority = (int)TickPriority.Normal)
        {
            if (tickable == null)
                return;

            // 查重
            for (int i = 0; i < _lateTickables.Count; i++)
            {
                if (_lateTickables[i].Tickable == tickable)
                    return;
            }

            _lateTickables.Add(
                new LateTickableWrapper { Tickable = tickable, Priority = priority }
            );
            _isLateTickDirty = true;
        }

        public void Unregister(IAsakiTickable tickable)
        {
            if (tickable == null)
                return;

            for (int i = 0; i < _tickables.Count; i++)
            {
                if (_tickables[i].Tickable == tickable)
                {
                    _tickables.RemoveAt(i);
                    return;
                }
            }
        }

        public void Unregister(IAsakiFixedTickable tickable)
        {
            if (tickable == null)
                return;
            _fixedTickables.Remove(tickable);
        }

        public void Unregister(IAsakiLateTickable tickable)
        {
            if (tickable == null)
                return;

            for (int i = 0; i < _lateTickables.Count; i++)
            {
                if (_lateTickables[i].Tickable == tickable)
                {
                    _lateTickables.RemoveAt(i);
                    return;
                }
            }
        }

        // =========================================================
        // 驱动方法
        // =========================================================

        public void Tick(float deltaTime)
        {
            // 1. 排序（稳定排序保证同优先级按注册顺序）
            if (_isTickDirty)
            {
                _tickables.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _isTickDirty = false;
            }

            // 2. 正序遍历执行
            for (int i = 0; i < _tickables.Count; i++)
            {
                TickableWrapper wrapper = _tickables[i];
                if (wrapper.Tickable != null)
                {
                    wrapper.Tickable.Tick(deltaTime);
                }
            }
        }

        public void FixedTick(float fixedDeltaTime)
        {
            // FixedUpdate 通常不支持优先级（物理计算顺序敏感）
            for (int i = 0; i < _fixedTickables.Count; i++)
            {
                IAsakiFixedTickable tickable = _fixedTickables[i];
                if (tickable != null)
                {
                    tickable.FixedTick(fixedDeltaTime);
                }
            }
        }

        public void LateTick(float lateDeltaTime)
        {
            // 1. 排序
            if (_isLateTickDirty)
            {
                _lateTickables.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _isLateTickDirty = false;
            }

            // 2. 正序遍历执行（优先级高的先执行）
            for (int i = 0; i < _lateTickables.Count; i++)
            {
                LateTickableWrapper wrapper = _lateTickables[i];
                if (wrapper.Tickable != null)
                {
                    wrapper.Tickable.LateTick(lateDeltaTime);
                }
            }
        }
    }
}
