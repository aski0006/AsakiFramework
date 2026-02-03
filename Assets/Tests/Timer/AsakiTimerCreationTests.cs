// File: Assets/Tests/Timer/AsakiTimerCreationTests.cs

using System;
using System.Collections;
using Asaki.Core.Time;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Timer
{
    /// <summary>
    /// 计时器创建与初始化测试
    /// 测试计时器的注册、句柄生成、初始状态等
    /// </summary>
    [TestFixture]
    [Category("Timer")]
    [Category("Unit")]
    public class AsakiTimerCreationTests : AsakiTimerTestBase
    {
        #region 基础创建测试

        [Test]
        [Description("创建基础计时器应返回有效句柄")]
        public void Register_WithValidDuration_ReturnsValidHandle()
        {
            // Arrange
            float duration = 1.0f;
            Action onComplete = () => { };

            // Act
            AsakiTimerHandle handle = TimerService.Register(duration, onComplete);

            // Assert
            AssertValidHandle(handle);
            Assert.AreEqual(1, TimerService.GetActiveTimerCount(), "应有1个活跃计时器");
        }

        [Test]
        [Description("创建多个计时器应返回不同句柄")]
        public void Register_MultipleTimers_ReturnsUniqueHandles()
        {
            // Act
            AsakiTimerHandle handle1 = TimerService.Register(1.0f, () => { });
            AsakiTimerHandle handle2 = TimerService.Register(2.0f, () => { });
            AsakiTimerHandle handle3 = TimerService.Register(3.0f, () => { });

            // Assert
            Assert.AreNotEqual(handle1, handle2, "句柄1和句柄2应不同");
            Assert.AreNotEqual(handle2, handle3, "句柄2和句柄3应不同");
            Assert.AreNotEqual(handle1, handle3, "句柄1和句柄3应不同");
            Assert.AreEqual(3, TimerService.GetActiveTimerCount(), "应有3个活跃计时器");
        }

        [Test]
        [Description("创建带标签的计时器应正确记录标签")]
        public void Register_WithTag_CreatesTaggedTimer()
        {
            // Arrange
            string tag = "TestTag";

            // Act
            AsakiTimerHandle handle = TimerService.Register(
                duration: 1.0f,
                onComplete: () => { },
                tag: tag
            );

            // Assert
            AssertValidHandle(handle);
            Assert.AreEqual(1, TimerService.GetTimerCountByTag(tag), "标签计数应为1");
        }

        [Test]
        [Description("创建带所有参数的计时器应成功")]
        public void Register_WithAllParameters_ReturnsValidHandle()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(
                duration: 2.0f,
                onComplete: tracker.GetCompleteAction(),
                onUpdate: tracker.GetUpdateAction(),
                isLooped: true,
                useUnscaledTime: true,
                tag: "FullConfig"
            );

            // Assert
            AssertValidHandle(handle);
            Assert.AreEqual(1, TimerService.GetActiveTimerCount());
            Assert.AreEqual(1, TimerService.GetTimerCountByTag("FullConfig"));
        }

        #endregion

        #region 初始状态测试

        [UnityTest]
        [Description("新创建的计时器初始进度应为0")]
        public IEnumerator Register_NewTimer_HasZeroProgress()
        {
            // Arrange
            var tracker = CreateCallbackTracker();
            AsakiTimerHandle handle = TimerService.Register(
                duration: 2.0f,
                onComplete: () => { },
                onUpdate: tracker.GetUpdateAction()
            );
            // Assert
            Assert.AreEqual(0, tracker.UpdateCount, "新计时器不应立即触发Update");
            yield return null;
        }

        [Test]
        [Description("创建零时长计时器应立即完成")]
        public void Register_ZeroDuration_CompletesImmediately()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(0f, tracker.GetCompleteAction());

            // 立即执行Tick
            Tick(0.001f);

            // Assert
            Assert.IsTrue(tracker.WasCompleted, "零时长计时器应立即完成");
            Assert.AreEqual(0, TimerService.GetActiveTimerCount(), "完成后应无活跃计时器");
        }

        [Test]
        [Description("创建负时长计时器应处理为立即完成或无效")]
        public void Register_NegativeDuration_HandlesGracefully()
        {
            // Arrange
            var tracker = CreateCallbackTracker();

            // Act
            AsakiTimerHandle handle = TimerService.Register(-1.0f, tracker.GetCompleteAction());
            Tick(0.001f);

            // Assert - 根据实现，负时长应该被处理
            AssertValidHandle(handle);
        }

        #endregion

        #region 句柄唯一性测试

        [Test]
        [Description("大量创建计时器应保证ID唯一性")]
        public void Register_ManyTimers_MaintainsUniqueIds()
        {
            // Arrange
            const int count = 100;
            var handles = new System.Collections.Generic.HashSet<AsakiTimerHandle>();

            // Act
            for (int i = 0; i < count; i++)
            {
                AsakiTimerHandle handle = TimerService.Register(1.0f, () => { });
                handles.Add(handle);
            }

            // Assert
            Assert.AreEqual(count, handles.Count, $"应生成{count}个唯一句柄");
            Assert.AreEqual(count, TimerService.GetActiveTimerCount());
        }

        [Test]
        [Description("取消后创建新计时器应使用新ID")]
        public void Register_AfterCancel_UsesNewId()
        {
            // Arrange
            AsakiTimerHandle handle1 = TimerService.Register(1.0f, () => { });
            int firstId = handle1.Id;

            // Act
            TimerService.Cancel(handle1);
            AsakiTimerHandle handle2 = TimerService.Register(1.0f, () => { });

            // Assert
            Assert.AreNotEqual(firstId, handle2.Id, "新计时器应使用不同ID");
        }

        #endregion

        #region 服务状态测试

        [Test]
        [Description("Dispose后创建计时器应返回无效句柄")]
        public void Register_AfterDispose_ReturnsInvalidHandle()
        {
            // Arrange
            TimerService.Dispose();

            // Act
            AsakiTimerHandle handle = TimerService.Register(1.0f, () => { });

            // Assert
            AssertInvalidHandle(handle);
        }

        [Test]
        [Description("服务初始状态应为空")]
        public void Service_InitialState_IsEmpty()
        {
            // Assert
            Assert.AreEqual(0, TimerService.GetActiveTimerCount(), "初始活跃计数应为0");
        }

        #endregion

        #region 标签分组测试

        [Test]
        [Description("相同标签的计时器应正确分组")]
        public void Register_SameTag_GroupsCorrectly()
        {
            // Arrange
            string tag = "SkillCooldown";

            // Act
            TimerService.Register(1.0f, () => { }, tag: tag);
            TimerService.Register(2.0f, () => { }, tag: tag);
            TimerService.Register(3.0f, () => { }, tag: tag);

            // Assert
            Assert.AreEqual(3, TimerService.GetTimerCountByTag(tag), "标签组应包含3个计时器");
            Assert.AreEqual(3, TimerService.GetActiveTimerCount());
        }

        [Test]
        [Description("不同标签的计时器应独立计数")]
        public void Register_DifferentTags_CountsIndependently()
        {
            // Act
            TimerService.Register(1.0f, () => { }, tag: "TagA");
            TimerService.Register(2.0f, () => { }, tag: "TagA");
            TimerService.Register(3.0f, () => { }, tag: "TagB");
            TimerService.Register(4.0f, () => { }, tag: "TagC");

            // Assert
            Assert.AreEqual(2, TimerService.GetTimerCountByTag("TagA"));
            Assert.AreEqual(1, TimerService.GetTimerCountByTag("TagB"));
            Assert.AreEqual(1, TimerService.GetTimerCountByTag("TagC"));
            Assert.AreEqual(4, TimerService.GetActiveTimerCount());
        }

        [Test]
        [Description("无标签计时器应计入总数但不计入标签组")]
        public void Register_NoTag_CountsInTotalOnly()
        {
            // Act
            TimerService.Register(1.0f, () => { }); // 无标签
            TimerService.Register(2.0f, () => { }, tag: "Tagged");

            // Assert
            Assert.AreEqual(2, TimerService.GetActiveTimerCount());
            Assert.AreEqual(1, TimerService.GetTimerCountByTag("Tagged"));
            Assert.AreEqual(0, TimerService.GetTimerCountByTag(""));
        }

        #endregion
    }
}
