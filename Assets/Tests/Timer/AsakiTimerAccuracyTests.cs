// File: Assets/Tests/Timer/AsakiTimerAccuracyTests.cs

using System;
using System.Collections;
using Asaki.Core.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Asaki.Tests.Timer
{
    /// <summary>
    /// 计时功能准确性测试
    /// 测试计时的精度、暂停、恢复、进度回调等
    /// </summary>
    [TestFixture]
    [Category("Timer")]
    [Category("Unit")]
    public class AsakiTimerAccuracyTests : AsakiTimerTestBase
    {
        #region 计时精度测试

        [UnityTest]
        [Description("计时器应在指定时间后完成")]
        public IEnumerator Timer_CompletesAfterDuration()
        {
            // Arrange
            float duration = 1.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(duration, tracker.GetCompleteAction());

            // Act - 模拟时间流逝
            yield return WaitForSeconds(duration + 0.1f);

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "计时器应在指定时间后完成");
            Assert.AreEqual(0, TimerService.GetActiveTimerCount(), "完成后应无活跃计时器");
        }

        [UnityTest]
        [Description("计时器进度应线性增长")]
        public IEnumerator Timer_Progress_IncreasesLinearly()
        {
            // Arrange
            float duration = 2.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration,
                tracker.GetCompleteAction(),
                tracker.GetUpdateAction()
            );

            // Act & Assert - 检查多个时间点的进度
            Tick(0.5f);
            AssertFloatEquals(0.25f, tracker.LastProgress, "0.5秒后进度应为25%");

            Tick(0.5f);
            AssertFloatEquals(0.5f, tracker.LastProgress, "1秒后进度应为50%");

            Tick(0.5f);
            AssertFloatEquals(0.75f, tracker.LastProgress, "1.5秒后进度应为75%");

            Tick(0.5f);
            AssertFloatEquals(1.0f, tracker.LastProgress, "2秒后进度应为100%");

            yield return null;
        }

        [UnityTest]
        [Description("Update回调应在每Tick触发")]
        public IEnumerator Timer_UpdateCallback_TriggersEveryTick()
        {
            // Arrange
            float duration = 1.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration,
                tracker.GetCompleteAction(),
                tracker.GetUpdateAction()
            );

            // Act
            const int tickCount = 10;
            for (int i = 0; i < tickCount; i++)
            {
                Tick(duration / tickCount);
            }

            // Assert
            Assert.AreEqual(tickCount, tracker.UpdateCount, $"应有{tickCount}次Update回调");
            yield return null;
        }

        #endregion

        #region 暂停与恢复测试

        [UnityTest]
        [Description("暂停后计时器应停止计时")]
        public IEnumerator Timer_Pause_StopsCounting()
        {
            // Arrange
            float duration = 2.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration,
                tracker.GetCompleteAction(),
                tracker.GetUpdateAction()
            );

            // Act - 先运行一半时间
            Tick(1.0f);
            float progressBeforePause = tracker.LastProgress;

            // 暂停
            TimerService.Pause(handle, true);

            // 再运行一段时间（应该不计时）
            Tick(1.0f);

            // Assert
            AssertFloatEquals(progressBeforePause, tracker.LastProgress, "暂停后进度不应变化");
            Assert.IsFalse(tracker.WasCompleted, "暂停时不应完成");

            yield return null;
        }

        [UnityTest]
        [Description("恢复后计时器应继续计时")]
        public IEnumerator Timer_Resume_ContinuesCounting()
        {
            // Arrange
            float duration = 2.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration,
                tracker.GetCompleteAction(),
                tracker.GetUpdateAction()
            );

            // Act - 运行、暂停、再恢复
            Tick(0.5f);
            TimerService.Pause(handle, true);
            Tick(1.0f); // 暂停期间的时间
            TimerService.Pause(handle, false); // 恢复
            Tick(1.5f); // 继续运行直到完成

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "恢复后应能完成");
            Assert.AreEqual(0, TimerService.GetActiveTimerCount());

            yield return null;
        }

        [UnityTest]
        [Description("多次暂停恢复应正常工作")]
        public IEnumerator Timer_MultiplePauseResume_WorksCorrectly()
        {
            // Arrange
            float duration = 1.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(duration, tracker.GetCompleteAction());

            // Act - 多次暂停和恢复
            for (int i = 0; i < 3; i++)
            {
                Tick(0.2f);
                TimerService.Pause(handle, true);
                Tick(0.5f); // 暂停期间
                TimerService.Pause(handle, false);
            }

            // 继续运行直到完成
            Tick(0.5f);

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "多次暂停恢复后应完成");

            yield return null;
        }

        #endregion

        #region 循环计时器测试

        [UnityTest]
        [Description("循环计时器应在完成后重新开始")]
        public IEnumerator Timer_Looped_RestartsAfterComplete()
        {
            // Arrange
            float duration = 1.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration,
                tracker.GetCompleteAction(),
                isLooped: true
            );

            // Act - 运行超过一个周期
            yield return WaitForSeconds(2.5f);

            // Assert
            Assert.AreEqual(2, tracker.CompleteCount, "应有2次完成回调");
            Assert.AreEqual(1, TimerService.GetActiveTimerCount(), "循环计时器应保持活跃");

            // 取消循环计时器
            TimerService.Cancel(handle);
        }

        [UnityTest]
        [Description("循环计时器的进度应在0-1之间循环")]
        public IEnumerator Timer_Looped_ProgressLoops()
        {
            // Arrange
            float duration = 1.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration,
                () => { },
                tracker.GetUpdateAction(),
                isLooped: true
            );

            // Act - 运行1.5个周期
            Tick(0.5f);
            AssertFloatEquals(0.5f, tracker.LastProgress, "0.5秒进度50%");

            Tick(0.5f); // 完成第一个周期
            Tick(0.3f); // 进入第二个周期

            // Assert - 进度应该在0.3左右（第二个周期的30%）
            Assert.That(tracker.LastProgress, Is.LessThan(0.5f), "循环后进度应重新开始");

            yield return null;
        }

        #endregion

        #region 全局暂停测试

        [UnityTest]
        [Description("全局暂停应暂停所有计时器")]
        public IEnumerator GlobalPause_PausesAllTimers()
        {
            // Arrange
            var tracker1 = CreateCallbackTracker();
            var tracker2 = CreateCallbackTracker();
            AsakiTimerHandle handle1 = TimerService.Register(2.0f, tracker1.GetCompleteAction());
            AsakiTimerHandle handle2 = TimerService.Register(2.0f, tracker2.GetCompleteAction());

            // Act
            Tick(0.5f);
            TimerService.PauseAll();
            Tick(2.0f); // 全局暂停期间

            // Assert
            Assert.IsFalse(tracker1.WasCompleted, "计时器1不应完成");
            Assert.IsFalse(tracker2.WasCompleted, "计时器2不应完成");

            yield return null;
        }

        [UnityTest]
        [Description("全局恢复应恢复所有计时器")]
        public IEnumerator GlobalResume_ResumesAllTimers()
        {
            // Arrange
            var tracker1 = CreateCallbackTracker();
            var tracker2 = CreateCallbackTracker();
            AsakiTimerHandle handle1 = TimerService.Register(1.0f, tracker1.GetCompleteAction());
            AsakiTimerHandle handle2 = TimerService.Register(1.0f, tracker2.GetCompleteAction());

            // Act
            Tick(0.3f);
            TimerService.PauseAll();
            Tick(1.0f);
            TimerService.ResumeAll();
            Tick(0.8f); // 继续运行

            // Assert
            Assert.IsTrue(tracker1.WasCompleted, "计时器1应完成");
            Assert.IsTrue(tracker2.WasCompleted, "计时器2应完成");

            yield return null;
        }

        #endregion

        #region 标签批量控制测试

        [UnityTest]
        [Description("按标签暂停应只影响该标签的计时器")]
        public IEnumerator PauseByTag_OnlyAffectsTaggedTimers()
        {
            // Arrange
            string tag = "SkillCooldown";
            var tracker1 = CreateCallbackTracker();
            var tracker2 = CreateCallbackTracker();

            AsakiTimerHandle taggedHandle = TimerService.Register(
                2.0f,
                tracker1.GetCompleteAction(),
                tag: tag
            );
            AsakiTimerHandle untaggedHandle = TimerService.Register(
                1.0f,
                tracker2.GetCompleteAction()
            );

            // Act
            Tick(0.5f);
            TimerService.PauseAllByTag(tag, true);
            Tick(1.0f); // 暂停期间

            // Assert
            Assert.IsFalse(tracker1.WasCompleted, "标签计时器应暂停");
            Assert.IsTrue(tracker2.WasCompleted, "无标签计时器应完成");

            yield return null;
        }

        [UnityTest]
        [Description("按标签恢复应只恢复该标签的计时器")]
        public IEnumerator ResumeByTag_OnlyResumesTaggedTimers()
        {
            // Arrange
            string tag = "Buff";
            CallbackTracker tracker1 = CreateCallbackTracker();
            CallbackTracker tracker2 = CreateCallbackTracker();

            // 两个计时器都是1.0f，便于比较
            AsakiTimerHandle taggedHandle = TimerService.Register(
                1.0f,
                tracker1.GetCompleteAction(),
                tag: tag
            );
            AsakiTimerHandle untaggedHandle = TimerService.Register(
                1.0f,
                tracker2.GetCompleteAction()
            );

            // Act - 先运行一点进度
            Tick(0.2f);
            // 全局暂停
            TimerService.PauseAll();
            Tick(0.5f); // 暂停期间
            // 只恢复标签计时器
            TimerService.PauseAllByTag(tag, false);
            // 继续运行足够时间让标签计时器完成
            Tick(1.0f);

            // Assert
            Assert.IsTrue(tracker1.WasCompleted, "标签计时器应完成");
            Assert.IsFalse(tracker2.WasCompleted, "无标签计时器应保持暂停");

            yield return null;
        }

        #endregion

        #region 取消与重置测试

        [UnityTest]
        [Description("取消后计时器不应再触发回调")]
        public IEnumerator Cancel_TimerStopsFiringCallbacks()
        {
            // Arrange
            float duration = 2.0f;
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration,
                tracker.GetCompleteAction(),
                tracker.GetUpdateAction()
            );

            // Act
            Tick(0.5f);
            int updateCountBeforeCancel = tracker.UpdateCount;
            TimerService.Cancel(handle);
            Tick(2.0f); // 继续运行时间

            // Assert
            Assert.AreEqual(updateCountBeforeCancel, tracker.UpdateCount, "取消后不应有Update回调");
            Assert.IsFalse(tracker.WasCompleted, "取消后不应触发完成回调");
            Assert.AreEqual(0, TimerService.GetActiveTimerCount());

            yield return null;
        }

        [UnityTest]
        [Description("取消后重新创建计时器应正常工作")]
        public IEnumerator CancelAndRecreate_WorksCorrectly()
        {
            // Arrange
            var tracker1 = CreateCallbackTracker();
            var tracker2 = CreateCallbackTracker();

            // Act - 创建、取消、再创建
            AsakiTimerHandle handle1 = TimerService.Register(1.0f, tracker1.GetCompleteAction());
            Tick(0.3f);
            TimerService.Cancel(handle1);

            AsakiTimerHandle handle2 = TimerService.Register(1.0f, tracker2.GetCompleteAction());
            yield return WaitForSeconds(1.2f);

            // Assert
            Assert.IsFalse(tracker1.WasCompleted, "第一个计时器不应完成");
            Assert.IsTrue(tracker2.WasCompleted, "第二个计时器应完成");

            yield return null;
        }

        #endregion
    }
}
