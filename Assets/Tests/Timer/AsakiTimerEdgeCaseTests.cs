// File: Assets/Tests/Timer/AsakiTimerEdgeCaseTests.cs

using System;
using System.Collections;
using Asaki.Core.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Timer
{
    /// <summary>
    /// 边界值和异常情况测试
    /// 测试极端参数、边界条件、错误处理等
    /// </summary>
    [TestFixture]
    [Category("Timer")]
    [Category("Unit")]
    public class AsakiTimerEdgeCaseTests : AsakiTimerTestBase
    {
        #region 边界值测试

        [Test]
        [Description("极短时长计时器应能正常工作")]
        public void Register_VeryShortDuration_Works()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(0.001f, tracker.GetCompleteAction());
            Tick(0.01f);

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "极短时长计时器应完成");
        }

        [Test]
        [Description("极长时长计时器应能正常工作")]
        public void Register_VeryLongDuration_Works()
        {
            // Arrange
            const float longDuration = 999999f;

            // Act
            AsakiTimerHandle handle = TimerService.Register(longDuration, () => { });

            // Assert
            AssertValidHandle(handle);
            Assert.AreEqual(1, TimerService.GetActiveTimerCount());
        }

        [Test]
        [Description("零值时长应处理为立即完成")]
        public void Register_ZeroDuration_CompletesImmediately()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(0f, tracker.GetCompleteAction());
            Tick(0.001f);

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "零时长计时器应立即完成");
        }

        [Test]
        [Description("负值时长应被处理")]
        public void Register_NegativeDuration_Handled()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(-1.0f, tracker.GetCompleteAction());
            Tick(0.001f);

            // Assert - 根据实现，负时长可能被处理为立即完成或无效
            AssertValidHandle(handle);
        }

        [Test]
        [Description("Float.MaxValue时长应能创建")]
        public void Register_MaxFloatDuration_CreatesTimer()
        {
            // Act
            AsakiTimerHandle handle = TimerService.Register(float.MaxValue, () => { });

            // Assert
            AssertValidHandle(handle);
        }

        [Test]
        [Description("Float.Epsilon时长应能正常工作")]
        public void Register_EpsilonDuration_Works()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(
                float.Epsilon,
                tracker.GetCompleteAction()
            );
            Tick(0.001f);

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "Epsilon时长计时器应完成");
        }

        #endregion

        #region 空值和无效参数测试

        [Test]
        [Description("空标签应被处理为空字符串")]
        public void Register_NullTag_Handled()
        {
            // Act
            AsakiTimerHandle handle = TimerService.Register(1.0f, () => { }, tag: null);

            // Assert
            AssertValidHandle(handle);
        }

        [Test]
        [Description("空回调应能正常工作")]
        public void Register_NullCallbacks_Works()
        {
            // Act
            AsakiTimerHandle handle = TimerService.Register(1.0f, onComplete: null, onUpdate: null);

            // Assert
            AssertValidHandle(handle);
            Tick(1.0f); // 不应抛出异常
        }

        [Test]
        [Description("只有Update回调应能正常工作")]
        public void Register_OnlyUpdateCallback_Works()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(
                0.5f,
                onComplete: null,
                onUpdate: tracker.GetUpdateAction()
            );

            Tick(0.5f);

            // Assert
            Assert.Greater(tracker.UpdateCount, 0, "应有Update回调");
        }

        #endregion

        #region 无效句柄操作测试

        [Test]
        [Description("对无效句柄调用Cancel不应抛出异常")]
        public void Cancel_InvalidHandle_DoesNotThrow()
        {
            // Arrange
            AsakiTimerHandle invalidHandle = AsakiTimerHandle.Invalid;

            // Act & Assert
            Assert.DoesNotThrow(
                () => TimerService.Cancel(invalidHandle),
                "取消无效句柄不应抛出异常"
            );
        }

        [Test]
        [Description("对已取消的句柄再次取消不应抛出异常")]
        public void Cancel_AlreadyCancelledHandle_DoesNotThrow()
        {
            // Arrange
            AsakiTimerHandle handle = TimerService.Register(1.0f, () => { });
            TimerService.Cancel(handle);

            // Act & Assert
            Assert.DoesNotThrow(() => TimerService.Cancel(handle), "重复取消不应抛出异常");
        }

        [Test]
        [Description("对已完成的句柄调用Cancel不应抛出异常")]
        public void Cancel_CompletedHandle_DoesNotThrow()
        {
            // Arrange
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(0.1f, tracker.GetCompleteAction());
            Tick(0.2f);
            Assert.IsTrue(tracker.WasCompleted);

            // Act & Assert
            Assert.DoesNotThrow(() => TimerService.Cancel(handle), "取消已完成句柄不应抛出异常");
        }

        [Test]
        [Description("对无效句柄调用Pause不应抛出异常")]
        public void Pause_InvalidHandle_DoesNotThrow()
        {
            // Arrange
            AsakiTimerHandle invalidHandle = AsakiTimerHandle.Invalid;

            // Act & Assert
            Assert.DoesNotThrow(
                () => TimerService.Pause(invalidHandle, true),
                "暂停无效句柄不应抛出异常"
            );
        }

        [Test]
        [Description("对已取消的句柄调用Pause不应抛出异常")]
        public void Pause_CancelledHandle_DoesNotThrow()
        {
            // Arrange
            AsakiTimerHandle handle = TimerService.Register(1.0f, () => { });
            TimerService.Cancel(handle);

            // Act & Assert
            Assert.DoesNotThrow(
                () => TimerService.Pause(handle, true),
                "暂停已取消句柄不应抛出异常"
            );
        }

        #endregion

        #region 特殊字符和边界标签测试

        [Test]
        [Description("特殊字符标签应能正常工作")]
        public void Register_SpecialCharacterTags_Works()
        {
            // Arrange
            string[] specialTags =
            {
                "",
                " ",
                "Tag With Space",
                "Tag\tWith\tTab",
                "Tag\nWith\nNewline",
                "!@#$%^&*()",
            };

            foreach (var tag in specialTags)
            {
                // Act
                AsakiTimerHandle handle = TimerService.Register(1.0f, () => { }, tag: tag);

                // Assert
                AssertValidHandle(handle);
            }
        }

        [Test]
        [Description("超长标签应能正常工作")]
        public void Register_VeryLongTag_Works()
        {
            // Arrange
            string longTag = new string('A', 1000);

            // Act
            AsakiTimerHandle handle = TimerService.Register(1.0f, () => { }, tag: longTag);

            // Assert
            AssertValidHandle(handle);
        }

        [Test]
        [Description("Unicode标签应能正常工作")]
        public void Register_UnicodeTag_Works()
        {
            // Arrange
            string[] unicodeTags = { "中文标签", "日本語", "한국어", "🎮", "🔥Fire🔥" };

            foreach (var tag in unicodeTags)
            {
                // Act
                AsakiTimerHandle handle = TimerService.Register(1.0f, () => { }, tag: tag);

                // Assert
                AssertValidHandle(handle);
            }
        }

        #endregion

        #region Dispose后操作测试

        [Test]
        [Description("Dispose后所有操作应安全处理")]
        public void Operations_AfterDispose_AreSafe()
        {
            // Arrange
            AsakiTimerHandle handle = TimerService.Register(10.0f, () => { });
            TimerService.Dispose();

            // Act & Assert
            Assert.DoesNotThrow(() => TimerService.Cancel(handle), "Dispose后Cancel应安全");
            Assert.DoesNotThrow(() => TimerService.Pause(handle, true), "Dispose后Pause应安全");
            Assert.DoesNotThrow(() => TimerService.CancelAll(), "Dispose后CancelAll应安全");
            Assert.DoesNotThrow(() => TimerService.PauseAll(), "Dispose后PauseAll应安全");
            Assert.DoesNotThrow(() => TimerService.ResumeAll(), "Dispose后ResumeAll应安全");
            Assert.DoesNotThrow(() => TimerService.Tick(0.1f), "Dispose后Tick应安全");
        }

        [Test]
        [Description("Dispose后GetActiveTimerCount应返回0")]
        public void GetActiveTimerCount_AfterDispose_ReturnsZero()
        {
            // Arrange
            TimerService.Register(10.0f, () => { });
            TimerService.Dispose();

            // Act
            int count = TimerService.GetActiveTimerCount();

            // Assert
            Assert.AreEqual(0, count, "Dispose后活跃计数应为0");
        }

        #endregion

        #region 极端Tick测试

        [Test]
        [Description("极大DeltaTime应正确处理")]
        public void Tick_VeryLargeDeltaTime_HandlesCorrectly()
        {
            // Arrange
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(1.0f, tracker.GetCompleteAction());

            // Act
            Tick(1000f); // 极大时间步进

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "极大DeltaTime应使计时器完成");
        }

        [Test]
        [Description("零DeltaTime不应导致问题")]
        public void Tick_ZeroDeltaTime_DoesNotCauseIssues()
        {
            // Arrange
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                1.0f,
                tracker.GetCompleteAction(),
                tracker.GetUpdateAction()
            );

            // Act
            for (int i = 0; i < 100; i++)
            {
                Tick(0f);
            }

            // Assert
            Assert.AreEqual(0, tracker.UpdateCount, "零DeltaTime不应触发Update");
            Assert.IsFalse(tracker.WasCompleted, "零DeltaTime不应使计时器完成");
        }

        [Test]
        [Description("负DeltaTime应被处理")]
        public void Tick_NegativeDeltaTime_Handled()
        {
            // Arrange
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(1.0f, tracker.GetCompleteAction());

            // Act & Assert
            Assert.DoesNotThrow(() => Tick(-0.1f), "负DeltaTime不应抛出异常");
        }

        #endregion

        #region 循环计时器边界测试

        [UnityTest]
        [Description("极短周期的循环计时器应能正常工作")]
        public IEnumerator LoopedTimer_VeryShortPeriod_Works()
        {
            // Arrange
            int completeCount = 0;
            AsakiTimerHandle handle = TimerService.Register(
                0.01f,
                () => completeCount++,
                isLooped: true
            );

            // Act
            yield return WaitForSeconds(0.1f);

            // Assert
            Assert.Greater(completeCount, 5, "短周期循环计时器应触发多次");

            // Cleanup
            TimerService.Cancel(handle);
        }

        [Test]
        [Description("循环计时器在极大DeltaTime下应正确处理")]
        public void LoopedTimer_VeryLargeDeltaTime_HandlesCorrectly()
        {
            // Arrange
            int completeCount = 0;
            AsakiTimerHandle handle = TimerService.Register(
                0.1f,
                () => completeCount++,
                isLooped: true
            );

            // Act
            Tick(10f); // 极大时间步进

            // Assert - 不应无限循环导致死锁
            Assert.Greater(completeCount, 0, "循环计时器应至少完成一次");

            // Cleanup
            TimerService.Cancel(handle);
        }

        #endregion

        #region 回调异常边界测试

        [UnityTest]
        [Description("回调中抛出异常不应影响服务状态")]
        public IEnumerator CallbackException_ServiceRemainsFunctional()
        {
            // Arrange
            bool secondCallbackCalled = false;

            LogAssert.ignoreFailingMessages = true;

            AsakiTimerHandle handle1 = TimerService.Register(
                0.1f,
                () =>
                {
                    throw new InvalidOperationException("Test exception");
                }
            );

            AsakiTimerHandle handle2 = TimerService.Register(
                0.2f,
                () =>
                {
                    secondCallbackCalled = true;
                }
            );

            Tick(0.5f);

            // Act
            yield return WaitForSeconds(0.3f);

            // Assert
            Assert.IsTrue(secondCallbackCalled, "服务应继续正常工作");
        }

        [UnityTest]
        [Description("递归调用Register不应导致栈溢出")]
        public IEnumerator RecursiveRegister_DoesNotStackOverflow()
        {
            // Arrange
            int depth = 0;
            const int maxDepth = 100;

            System.Action createTimer = null;
            createTimer = () =>
            {
                if (depth < maxDepth)
                {
                    depth++;
                    TimerService.Register(0.001f, createTimer);
                }
            };

            // Act
            createTimer();
            // 使用更小的 tickInterval 以确保每次 Tick 都能处理新创建的计时器
            // 需要至少 maxDepth * duration 的时间来完成所有递归
            yield return WaitForSeconds(2.0f, tickInterval: 0.001f);

            // Assert
            Assert.AreEqual(maxDepth, depth, "应能递归创建计时器");
        }

        #endregion
    }
}
