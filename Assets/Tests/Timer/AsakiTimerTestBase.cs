// File: Assets/Tests/Timer/AsakiTimerTestBase.cs

using System;
using System.Collections;
using Asaki.Core.Time;
using Asaki.Unity.Services.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Timer
{
    /// <summary>
    /// Timer 服务测试基类
    /// 提供通用的测试辅助方法和设置
    /// </summary>
    public abstract class AsakiTimerTestBase
    {
        protected IAsakiTimerService TimerService { get; private set; }
        protected const float FLOAT_TOLERANCE = 0.05f;
        protected const int DEFAULT_CAPACITY = 64;

        [SetUp]
        public virtual void Setup()
        {
            TimerService = new AsakiTimerService(DEFAULT_CAPACITY);
        }

        [TearDown]
        public virtual void Teardown()
        {
            TimerService?.Dispose();
            TimerService = null;
        }

        /// <summary>
        /// 模拟 Tick 更新
        /// </summary>
        protected void Tick(float deltaTime)
        {
            TimerService.Tick(deltaTime);
        }

        /// <summary>
        /// 等待指定时间（通过多次 Tick 模拟）
        /// </summary>
        protected IEnumerator WaitForSeconds(float seconds, float tickInterval = 0.1f)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                float dt = Mathf.Min(tickInterval, seconds - elapsed);
                Tick(dt);
                elapsed += dt;
                yield return null;
            }
        }

        /// <summary>
        /// 创建测试用的回调追踪器
        /// </summary>
        protected CallbackTracker CreateCallbackTracker()
        {
            return new CallbackTracker();
        }

        /// <summary>
        /// 验证 TimerHandle 是否有效
        /// </summary>
        protected void AssertValidHandle(AsakiTimerHandle handle)
        {
            Assert.AreNotEqual(0, handle.Id, "Timer handle ID should not be zero");
            Assert.AreNotEqual(
                AsakiTimerHandle.Invalid,
                handle,
                "Timer handle should not be invalid"
            );
        }

        /// <summary>
        /// 验证 TimerHandle 是否无效
        /// </summary>
        protected void AssertInvalidHandle(AsakiTimerHandle handle)
        {
            Assert.AreEqual(0, handle.Id, "Timer handle ID should be zero for invalid handle");
        }

        /// <summary>
        /// 断言浮点数相等（带容差）
        /// </summary>
        protected void AssertFloatEquals(float expected, float actual, string message = null)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(FLOAT_TOLERANCE), message);
        }
    }

    /// <summary>
    /// 回调追踪器 - 用于验证回调是否被正确触发
    /// </summary>
    public class CallbackTracker
    {
        public int CompleteCount { get; private set; }
        public int UpdateCount { get; private set; }
        public float LastProgress { get; private set; }
        public bool WasCompleted => CompleteCount > 0;

        public void OnComplete()
        {
            CompleteCount++;
        }

        public void OnUpdate(float progress)
        {
            UpdateCount++;
            LastProgress = progress;
        }

        public void Reset()
        {
            CompleteCount = 0;
            UpdateCount = 0;
            LastProgress = 0f;
        }

        public Action GetCompleteAction() => OnComplete;

        public Action<float> GetUpdateAction() => OnUpdate;
    }

    /// <summary>
    /// 多回调追踪器 - 用于并发测试
    /// </summary>
    public class MultiCallbackTracker
    {
        private readonly System.Collections.Generic.Dictionary<int, int> _completeCounts = new();
        private readonly System.Collections.Generic.Dictionary<int, int> _updateCounts = new();

        public Action GetCompleteAction(int id)
        {
            return () =>
            {
                if (!_completeCounts.ContainsKey(id))
                    _completeCounts[id] = 0;
                _completeCounts[id]++;
            };
        }

        public Action<float> GetUpdateAction(int id)
        {
            return progress =>
            {
                if (!_updateCounts.ContainsKey(id))
                    _updateCounts[id] = 0;
                _updateCounts[id]++;
            };
        }

        public int GetCompleteCount(int id)
        {
            return _completeCounts.TryGetValue(id, out var count) ? count : 0;
        }

        public int GetUpdateCount(int id)
        {
            return _updateCounts.TryGetValue(id, out var count) ? count : 0;
        }

        public void Reset()
        {
            _completeCounts.Clear();
            _updateCounts.Clear();
        }
    }
}
