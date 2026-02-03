// File: Assets/Tests/Timer/AsakiTimerEventTests.cs

using System;
using System.Collections;
using System.Collections.Generic;
using Asaki.Core.Time;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Timer
{
    /// <summary>
    /// 计时结束事件触发测试
    /// 测试回调触发时机、顺序、异常处理等
    /// </summary>
    [TestFixture]
    [Category("Timer")]
    [Category("Unit")]
    public class AsakiTimerEventTests : AsakiTimerTestBase
    {
        #region 完成回调测试

        [UnityTest]
        [Description("计时器完成时应触发回调")]
        public IEnumerator CompleteCallback_TriggersWhenTimerFinishes()
        {
            // Arrange
            bool wasCalled = false;
            AsakiTimerHandle handle = TimerService.Register(0.5f, () => wasCalled = true);

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.IsTrue(wasCalled, "完成回调应被触发");
        }

        [UnityTest]
        [Description("完成回调应在正确的时间触发")]
        public IEnumerator CompleteCallback_TriggersAtCorrectTime()
        {
            // Arrange
            float callbackTime = -1f;
            float elapsedTime = 0f;
            const float duration = 1.0f;
            const float tickInterval = 0.1f;

            AsakiTimerHandle handle = TimerService.Register(
                duration,
                () =>
                {
                    // 回调触发时，记录的是更新后的 elapsedTime
                    callbackTime = elapsedTime + tickInterval;
                }
            );

            // Act - 逐步推进时间
            while (elapsedTime < duration + 0.2f)
            {
                Tick(tickInterval);
                elapsedTime += tickInterval;
                yield return null;
            }

            // Assert
            AssertFloatEquals(duration, callbackTime, $"回调应在{duration}秒时触发");
        }

        [Test]
        [Description("取消的计时器不应触发完成回调")]
        public void CompleteCallback_DoesNotTriggerWhenCancelled()
        {
            // Arrange
            bool wasCalled = false;
            AsakiTimerHandle handle = TimerService.Register(1.0f, () => wasCalled = true);

            // Act
            Tick(0.5f);
            TimerService.Cancel(handle);
            Tick(1.0f); // 继续运行超过原定时时间

            // Assert
            Assert.IsFalse(wasCalled, "取消后不应触发完成回调");
        }

        [UnityTest]
        [Description("暂停的计时器不应触发完成回调")]
        public IEnumerator CompleteCallback_DoesNotTriggerWhenPaused()
        {
            // Arrange
            bool wasCalled = false;
            AsakiTimerHandle handle = TimerService.Register(0.5f, () => wasCalled = true);

            // Act
            Tick(0.3f);
            TimerService.Pause(handle, true);
            yield return WaitForSeconds(1.0f); // 暂停期间

            // Assert
            Assert.IsFalse(wasCalled, "暂停时不应触发完成回调");
        }

        #endregion

        #region Update回调测试

        [UnityTest]
        [Description("Update回调应在计时期间持续触发")]
        public IEnumerator UpdateCallback_TriggersDuringTimerDuration()
        {
            // Arrange
            var progressValues = new List<float>();
            AsakiTimerHandle handle = TimerService.Register(
                1.0f,
                () => { },
                progress => progressValues.Add(progress)
            );

            // Act
            yield return WaitForSeconds(1.2f);

            // Assert
            Assert.Greater(progressValues.Count, 5, "应有多次Update回调");
            // 第一次Tick后进度 = tickInterval / duration = 0.1 / 1.0 = 0.1
            AssertFloatEquals(0.1f, progressValues[0], "第一次进度应接近0.1");
            AssertFloatEquals(1f, progressValues[progressValues.Count - 1], "最后一次进度应接近1");
        }

        [UnityTest]
        [Description("Update回调的进度应单调递增")]
        public IEnumerator UpdateCallback_ProgressIncreasesMonotonically()
        {
            // Arrange
            var progressValues = new List<float>();
            AsakiTimerHandle handle = TimerService.Register(
                1.0f,
                () => { },
                progress => progressValues.Add(progress)
            );

            // Act
            yield return WaitForSeconds(1.2f);

            // Assert
            for (int i = 1; i < progressValues.Count; i++)
            {
                Assert.GreaterOrEqual(
                    progressValues[i],
                    progressValues[i - 1],
                    $"进度应单调递增，但在索引{i}处违反"
                );
            }
        }

        [UnityTest]
        [Description("暂停时不应触发Update回调")]
        public IEnumerator UpdateCallback_DoesNotTriggerWhenPaused()
        {
            // Arrange
            int updateCount = 0;
            AsakiTimerHandle handle = TimerService.Register(
                2.0f,
                () => { },
                progress => updateCount++
            );

            // Act
            Tick(0.5f);
            int countBeforePause = updateCount;
            TimerService.Pause(handle, true);
            yield return WaitForSeconds(0.5f);

            // Assert
            Assert.AreEqual(countBeforePause, updateCount, "暂停时不应有Update回调");
        }

        #endregion

        #region 回调顺序测试

        [UnityTest]
        [Description("完成回调应在最后一次Update之后触发")]
        public IEnumerator CompleteCallback_TriggersAfterFinalUpdate()
        {
            // Arrange
            float lastUpdateProgress = -1f;
            float completeProgress = -1f;
            bool completeTriggered = false;

            AsakiTimerHandle handle = TimerService.Register(
                0.5f,
                () =>
                {
                    completeProgress = lastUpdateProgress;
                    completeTriggered = true;
                },
                progress =>
                {
                    if (!completeTriggered)
                    {
                        lastUpdateProgress = progress;
                    }
                }
            );

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            AssertFloatEquals(1f, completeProgress, "完成回调应在进度为1时触发");
        }

        #endregion

        #region 多计时器回调测试

        [UnityTest]
        [Description("多个计时器的回调应独立触发")]
        public IEnumerator MultipleTimers_CallbacksTriggerIndependently()
        {
            // Arrange
            var tracker1 = CreateCallbackTracker();
            var tracker2 = CreateCallbackTracker();

            AsakiTimerHandle handle1 = TimerService.Register(0.5f, tracker1.GetCompleteAction());
            AsakiTimerHandle handle2 = TimerService.Register(1.0f, tracker2.GetCompleteAction());

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.IsTrue(tracker1.WasCompleted, "计时器1应先完成");
            Assert.IsFalse(tracker2.WasCompleted, "计时器2不应完成");

            yield return WaitForSeconds(0.5f);

            Assert.IsTrue(tracker2.WasCompleted, "计时器2后完成");
        }

        [UnityTest]
        [Description("同时完成的计时器都应触发回调")]
        public IEnumerator SimultaneousTimers_AllCallbacksTrigger()
        {
            // Arrange
            int completeCount = 0;
            const int timerCount = 5;

            for (int i = 0; i < timerCount; i++)
            {
                TimerService.Register(0.5f, () => completeCount++);
            }

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.AreEqual(timerCount, completeCount, $"应有{timerCount}个完成回调");
        }

        #endregion

        #region 异常处理测试

        [UnityTest]
        [Description("回调异常不应影响其他计时器")]
        public IEnumerator CallbackException_DoesNotAffectOtherTimers()
        {
            // Arrange
            bool secondTimerCompleted = false;
            LogAssert.ignoreFailingMessages = true;

            AsakiTimerHandle handle1 = TimerService.Register(
                0.5f,
                () =>
                {
                    throw new Exception("Test exception");
                }
            );

            AsakiTimerHandle handle2 = TimerService.Register(
                0.5f,
                () =>
                {
                    secondTimerCompleted = true;
                }
            );

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.IsTrue(secondTimerCompleted, "第二个计时器应正常完成");
        }

        [UnityTest]
        [Description("Update回调异常不应阻止计时器完成")]
        public IEnumerator UpdateException_DoesNotPreventCompletion()
        {
            // Arrange
            bool wasCompleted = false;
            int updateCount = 0;
            LogAssert.ignoreFailingMessages = true;

            AsakiTimerHandle handle = TimerService.Register(
                0.5f,
                () => wasCompleted = true,
                progress =>
                {
                    updateCount++;
                    if (updateCount == 2)
                    {
                        throw new Exception("Update exception");
                    }
                }
            );

            // Act
            yield return WaitForSeconds(0.6f);

            // Assert
            Assert.IsTrue(wasCompleted, "计时器仍应完成");
        }

        #endregion

        #region 循环计时器回调测试

        [UnityTest]
        [Description("循环计时器应在每次循环时触发完成回调")]
        public IEnumerator LoopedTimer_CompleteCallbackTriggersEachLoop()
        {
            // Arrange
            int completeCount = 0;
            AsakiTimerHandle handle = TimerService.Register(
                0.3f,
                () => completeCount++,
                isLooped: true
            );

            // Act
            yield return WaitForSeconds(1.0f);

            // Assert
            Assert.GreaterOrEqual(completeCount, 2, "循环计时器应触发多次完成回调");

            // Cleanup
            TimerService.Cancel(handle);
        }

        [UnityTest]
        [Description("循环计时器的Update回调应在整个过程中持续触发")]
        public IEnumerator LoopedTimer_UpdateCallbackTriggersContinuously()
        {
            // Arrange
            int updateCount = 0;
            AsakiTimerHandle handle = TimerService.Register(
                0.3f,
                () => { },
                progress => updateCount++,
                isLooped: true
            );

            // Act
            yield return WaitForSeconds(1.2f);

            // Assert
            Assert.GreaterOrEqual(updateCount, 10, "循环计时器应有多次Update回调");

            // Cleanup
            TimerService.Cancel(handle);
        }

        #endregion
    }
}
