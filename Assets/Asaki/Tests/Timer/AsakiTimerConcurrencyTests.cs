// File: Assets/Tests/Timer/AsakiTimerConcurrencyTests.cs

using System;
using System.Collections;
using System.Collections.Generic;
using Asaki.Core.Time;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Timer
{
    /// <summary>
    /// 多计时器并发管理测试
    /// 测试大量计时器的创建、管理、性能等
    /// </summary>
    [TestFixture]
    [Category("Timer")]
    [Category("Integration")]
    public class AsakiTimerConcurrencyTests : AsakiTimerTestBase
    {
        #region 大量计时器测试

        [Test]
        [Description("应能同时管理大量计时器")]
        public void Register_ManyTimers_ManagesCorrectly()
        {
            // Arrange
            const int count = 500;

            // Act
            for (int i = 0; i < count; i++)
            {
                TimerService.Register(10.0f, () => { });
            }

            // Assert
            Assert.AreEqual(count, TimerService.GetActiveTimerCount(), $"应能管理{count}个计时器");
        }

        [UnityTest]
        [Description("大量计时器应能正常完成")]
        [Timeout(10000)]
        public IEnumerator ManyTimers_CompleteCorrectly()
        {
            // Arrange
            const int count = 100;
            int completedCount = 0;

            for (int i = 0; i < count; i++)
            {
                TimerService.Register(0.5f, () => completedCount++);
            }

            // Act
            yield return WaitForSeconds(0.7f);

            // Assert
            Assert.AreEqual(count, completedCount, $"应有{count}个计时器完成");
            Assert.AreEqual(0, TimerService.GetActiveTimerCount());
        }

        [Test]
        [Description("大量计时器的取消应正常工作")]
        public void Cancel_ManyTimers_WorksCorrectly()
        {
            // Arrange
            const int count = 100;
            var handles = new List<AsakiTimerHandle>();

            for (int i = 0; i < count; i++)
            {
                handles.Add(TimerService.Register(10.0f, () => { }));
            }

            // Act
            foreach (var handle in handles)
            {
                TimerService.Cancel(handle);
            }

            // Assert
            Assert.AreEqual(0, TimerService.GetActiveTimerCount(), "所有计时器应被取消");
        }

        #endregion

        #region 混合操作测试

        [UnityTest]
        [Description("同时进行创建、暂停、取消操作应正常工作")]
        public IEnumerator MixedOperations_WorksCorrectly()
        {
            // Arrange
            var handles = new List<AsakiTimerHandle>();
            var tracker = new MultiCallbackTracker();

            // Act - 创建一批计时器
            for (int i = 0; i < 10; i++)
            {
                handles.Add(
                    TimerService.Register(
                        1.0f,
                        tracker.GetCompleteAction(i),
                        tag: i % 2 == 0 ? "Even" : "Odd"
                    )
                );
            }

            // 混合操作
            Tick(0.3f);
            TimerService.Pause(handles[0], true); // 暂停单个
            TimerService.PauseAllByTag("Even", true); // 暂停标签组
            TimerService.Cancel(handles[2]); // 取消单个

            yield return WaitForSeconds(0.5f);

            TimerService.PauseAllByTag("Even", false); // 恢复标签组

            yield return WaitForSeconds(0.5f);

            // Assert
            Assert.IsFalse(tracker.GetCompleteCount(0) > 0, "暂停的计时器不应完成");
            Assert.IsTrue(tracker.GetCompleteCount(1) > 0, "Odd标签计时器应完成");
            Assert.IsFalse(tracker.GetCompleteCount(2) > 0, "取消的计时器不应完成");
        }

        [UnityTest]
        [Description("在Tick过程中创建新计时器应正常工作")]
        public IEnumerator CreateDuringTick_WorksCorrectly()
        {
            // Arrange
            bool firstCompleted = false;
            bool secondCompleted = false;

            AsakiTimerHandle handle1 = TimerService.Register(
                0.3f,
                () =>
                {
                    firstCompleted = true;
                    // 在回调中创建新计时器
                    TimerService.Register(0.3f, () => secondCompleted = true);
                }
            );

            // Act
            yield return WaitForSeconds(0.4f);

            // Assert - 第一个应完成，第二个刚创建
            Assert.IsTrue(firstCompleted, "第一个计时器应完成");
            Assert.AreEqual(1, TimerService.GetActiveTimerCount(), "应有1个新计时器");

            yield return WaitForSeconds(0.4f);

            Assert.IsTrue(secondCompleted, "第二个计时器应完成");
        }

        #endregion

        #region 标签组并发测试

        [Test]
        [Description("多个标签组应独立管理")]
        public void MultipleTags_ManageIndependently()
        {
            // Arrange
            string[] tags = { "Skill", "Buff", "Debuff", "Cooldown" };
            int timersPerTag = 25;

            // Act
            foreach (var tag in tags)
            {
                for (int i = 0; i < timersPerTag; i++)
                {
                    TimerService.Register(10.0f, () => { }, tag: tag);
                }
            }

            // Assert
            foreach (var tag in tags)
            {
                Assert.AreEqual(
                    timersPerTag,
                    TimerService.GetTimerCountByTag(tag),
                    $"{tag}标签应有{timersPerTag}个计时器"
                );
            }
            Assert.AreEqual(tags.Length * timersPerTag, TimerService.GetActiveTimerCount());
        }

        [UnityTest]
        [Description("取消整个标签组不应影响其他标签")]
        public IEnumerator CancelTagGroup_DoesNotAffectOthers()
        {
            // Arrange
            var tracker1 = CreateCallbackTracker();
            var tracker2 = CreateCallbackTracker();

            for (int i = 0; i < 5; i++)
            {
                TimerService.Register(0.5f, tracker1.GetCompleteAction(), tag: "Group1");
                TimerService.Register(0.5f, tracker2.GetCompleteAction(), tag: "Group2");
            }

            // Act
            TimerService.CancelAllByTag("Group1");
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.AreEqual(0, tracker1.CompleteCount, "Group1的计时器应被取消");
            Assert.AreEqual(5, tracker2.CompleteCount, "Group2的计时器应正常完成");
        }

        #endregion

        #region 性能测试

        [Test]
        [Description("Tick处理大量计时器应在合理时间内完成")]
        [Category("Performance")]
        public void Tick_LargeNumber_PerformsWell()
        {
            // Arrange
            const int count = 1000;
            for (int i = 0; i < count; i++)
            {
                TimerService.Register(10.0f, () => { }, tag: $"Tag{i % 10}");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                Tick(0.016f); // 模拟60fps
            }

            stopwatch.Stop();

            // Assert - 100帧应在1秒内完成
            Assert.Less(
                stopwatch.ElapsedMilliseconds,
                1000,
                $"处理{count}个计时器的100帧应在1秒内完成"
            );
        }

        [Test]
        [Description("创建大量计时器应在合理时间内完成")]
        [Category("Performance")]
        public void Register_LargeNumber_PerformsWell()
        {
            // Arrange
            const int count = 10000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < count; i++)
            {
                TimerService.Register(10.0f, () => { });
            }

            stopwatch.Stop();

            // Assert - 创建10000个计时器应在500ms内完成
            Assert.Less(stopwatch.ElapsedMilliseconds, 500, $"创建{count}个计时器应在500ms内完成");
            Assert.AreEqual(count, TimerService.GetActiveTimerCount());
        }

        #endregion

        #region 边界并发测试

        [UnityTest]
        [Description("所有计时器同时完成应正常工作")]
        public IEnumerator AllTimersCompleteSimultaneously_WorksCorrectly()
        {
            // Arrange
            const int count = 50;
            int completedCount = 0;

            for (int i = 0; i < count; i++)
            {
                TimerService.Register(0.5f, () => completedCount++);
            }

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.AreEqual(count, completedCount, $"应有{count}个计时器完成");
        }

        [UnityTest]
        [Description("在计时器回调中取消其他计时器应正常工作")]
        public IEnumerator CancelOthersInCallback_WorksCorrectly()
        {
            // Arrange
            var handles = new List<AsakiTimerHandle>();
            bool firstCompleted = false;

#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool othersCancelled = true;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值

            // 创建多个计时器
            for (int i = 0; i < 5; i++)
            {
                handles.Add(TimerService.Register(0.5f, () => { }));
            }

            // 第一个计时器完成后取消其他
            TimerService.Cancel(handles[0]);
            handles[0] = TimerService.Register(
                0.3f,
                () =>
                {
                    firstCompleted = true;
                    for (int i = 1; i < handles.Count; i++)
                    {
                        TimerService.Cancel(handles[i]);
                    }
                }
            );

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.IsTrue(firstCompleted, "第一个计时器应完成");
            Assert.AreEqual(0, TimerService.GetActiveTimerCount(), "其他计时器应被取消");
        }

        #endregion
    }
}
