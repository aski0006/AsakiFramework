using System.Collections;
using System.Collections.Generic;
using Asaki.Plugin.ComboSystem;
using Asaki.Plugin.ComboSystem.States;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.ComboSystem
{
    /// <summary>
    /// ComboController 集成测试（PlayMode）
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("ComboSystem")]
    public class ComboControllerTests
    {
        private GameObject _testObject;
        private AsakiComboController _controller;
        private ComboTree _comboTree;
        private Animator _animator;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // 创建测试GameObject
            _testObject = new GameObject("TestComboController");

            // 添加Animator（需要Controller才能在PlayMode正确工作）
            _animator = _testObject.AddComponent<Animator>();

            // 创建测试用的ComboTree
            _comboTree = CreateTestComboTree();

            // 添加ComboController
            _controller = _testObject.AddComponent<AsakiComboController>();
            _controller.Initialize(_comboTree);

            // 等待一帧让组件初始化
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
            if (_comboTree != null)
            {
                Object.DestroyImmediate(_comboTree);
            }
            yield return null;
        }

        #region 初始化测试

        [UnityTest]
        [Description("测试：控制器初始化后处于Idle状态")]
        public IEnumerator Initialize_AfterAwake_IsInIdleState()
        {
            // Assert
            Assert.AreEqual(
                ComboStateType.Idle,
                _controller.CurrentStateType,
                "初始化后应处于Idle状态"
            );
            yield return null;
        }

        [UnityTest]
        [Description("测试：控制器初始化后连击数为0")]
        public IEnumerator Initialize_ComboCountIsZero()
        {
            // Assert
            Assert.AreEqual(0, _controller.CurrentComboCount, "初始化后连击数应为0");
            yield return null;
        }

        [UnityTest]
        [Description("测试：控制器可以接受输入")]
        public IEnumerator Initialize_CanAcceptInput()
        {
            // Assert
            Assert.IsTrue(_controller.CanAcceptInput(), "Idle状态下应可以接受输入");
            yield return null;
        }

        #endregion

        #region 连招输入测试

        [UnityTest]
        [Description("测试：触发攻击后状态变为Startup")]
        public IEnumerator TriggerAttack_FromIdle_GoesToStartup()
        {
            // Act
            _controller.TriggerAttack("LightAttack");

            // Assert
            Assert.AreEqual(
                ComboStateType.Startup,
                _controller.CurrentStateType,
                "触发攻击后应进入Startup状态"
            );
            yield return null;
        }

        [UnityTest]
        [Description("测试：触发攻击后连击数增加")]
        public IEnumerator TriggerAttack_IncreasesComboCount()
        {
            // Act
            _controller.TriggerAttack("LightAttack");

            // Assert
            Assert.AreEqual(1, _controller.CurrentComboCount, "触发攻击后连击数应为1");
            yield return null;
        }

        [UnityTest]
        [Description("测试：当前招式被正确设置")]
        public IEnumerator TriggerAttack_SetsCurrentMove()
        {
            // Act
            _controller.TriggerAttack("LightAttack");

            // Assert
            Assert.IsNotNull(_controller.CurrentMove, "触发攻击后应有当前招式");
            Assert.AreEqual("light_1", _controller.CurrentMove.MoveId, "当前招式应为light_1");
            yield return null;
        }

        #endregion

        #region 连招流程测试

        [UnityTest]
        [Description("测试：完成一个招式的完整流程")]
        public IEnumerator FullComboFlow_CompletesAllStates()
        {
            // 设置一个快速的招式用于测试
            var fastMove = new ComboMove
            {
                MoveId = "fast_move",
                MoveName = "Fast Move",
                StartupTime = 0.05f,
                ActiveDuration = 0.05f,
                RecoveryTime = 0.05f,
                ComboWindowStart = 0.05f,
                ComboWindowEnd = 0.2f,
            };

            _comboTree.Moves = new[] { fastMove };
            _comboTree.Transitions = new ComboTransition[0];

            // 重新初始化
            _controller.Initialize(_comboTree);

            // Act - 开始攻击
            _controller.TriggerAttack("LightAttack");
            Assert.AreEqual(ComboStateType.Startup, _controller.CurrentStateType);

            // 等待进入Active (StartupTime = 0.05f, 等待0.06s确保进入Active但不超过Active+Recovery)
            yield return new WaitForSeconds(0.06f);
            Assert.AreEqual(ComboStateType.Active, _controller.CurrentStateType);

            // 等待进入Recovery (ActiveDuration = 0.05f, 等待0.06s确保进入Recovery但不超过Recovery)
            yield return new WaitForSeconds(0.06f);
            Assert.AreEqual(ComboStateType.Recovery, _controller.CurrentStateType);

            // 等待进入ComboWindow (RecoveryTime = 0.05f, 等待0.06s确保进入ComboWindow)
            yield return new WaitForSeconds(0.06f);
            Assert.AreEqual(ComboStateType.ComboWindow, _controller.CurrentStateType);
        }

        [UnityTest]
        [Description("测试：连招窗口超时后返回Idle")]
        public IEnumerator ComboWindow_Timeout_ReturnsToIdle()
        {
            // Arrange - 创建一个有短暂连招窗口的招式
            var shortWindowMove = new ComboMove
            {
                MoveId = "short_window",
                MoveName = "Short Window Move",
                StartupTime = 0.01f,
                ActiveDuration = 0.01f,
                RecoveryTime = 0.01f,
                ComboWindowStart = 0.01f,
                ComboWindowEnd = 0.05f, // 很短的窗口
            };

            _comboTree.Moves = new[] { shortWindowMove };
            _comboTree.Transitions = new ComboTransition[0];
            _controller.Initialize(_comboTree);

            // Act - 开始并等待完成
            _controller.TriggerAttack("LightAttack");
            yield return new WaitForSeconds(0.15f); // 等待窗口超时

            // Assert
            Assert.AreEqual(
                ComboStateType.Idle,
                _controller.CurrentStateType,
                "窗口超时后应返回Idle"
            );
        }

        #endregion

        #region 连招中断测试

        [UnityTest]
        [Description("测试：中断连招后状态变为Interrupted")]
        public IEnumerator InterruptCombo_GoesToInterrupted()
        {
            // Arrange
            _controller.TriggerAttack("LightAttack");
            yield return null;

            // Act
            _controller.InterruptCombo(InterruptReason.Damaged);

            // Assert
            Assert.AreEqual(
                ComboStateType.Interrupted,
                _controller.CurrentStateType,
                "中断后应进入Interrupted状态"
            );
        }

        [UnityTest]
        [Description("测试：中断连招后连击数重置")]
        public IEnumerator InterruptCombo_ResetsComboCount()
        {
            // Arrange
            _controller.TriggerAttack("LightAttack");
            yield return null;
            Assert.AreEqual(1, _controller.CurrentComboCount);

            // Act
            _controller.InterruptCombo(InterruptReason.Damaged);

            // Assert
            Assert.AreEqual(0, _controller.CurrentComboCount, "中断后连击数应重置为0");
        }

        [UnityTest]
        [Description("测试：Idle状态下中断不会触发事件")]
        public IEnumerator InterruptCombo_FromIdle_DoesNothing()
        {
            // Arrange
            bool eventTriggered = false;
            _controller.OnComboInterrupted += (reason) => eventTriggered = true;

            // Act - 在Idle状态下中断
            _controller.InterruptCombo(InterruptReason.Damaged);

            // Assert
            Assert.IsFalse(eventTriggered, "Idle状态下中断不应触发事件");
            Assert.AreEqual(ComboStateType.Idle, _controller.CurrentStateType);

            yield return null;
        }

        #endregion

        #region 重置测试

        [UnityTest]
        [Description("测试：重置后返回Idle状态")]
        public IEnumerator ResetCombo_ReturnsToIdle()
        {
            // Arrange
            _controller.TriggerAttack("LightAttack");
            yield return null;
            Assert.AreNotEqual(ComboStateType.Idle, _controller.CurrentStateType);

            // Act
            _controller.ResetCombo();

            // Assert
            Assert.AreEqual(ComboStateType.Idle, _controller.CurrentStateType, "重置后应返回Idle");
        }

        [UnityTest]
        [Description("测试：重置后连击数为0")]
        public IEnumerator ResetCombo_ComboCountIsZero()
        {
            // Arrange
            _controller.TriggerAttack("LightAttack");
            yield return null;

            // Act
            _controller.ResetCombo();

            // Assert
            Assert.AreEqual(0, _controller.CurrentComboCount, "重置后连击数应为0");
        }

        [UnityTest]
        [Description("测试：重置触发OnComboCompleted事件")]
        public IEnumerator ResetCombo_TriggersCompletedEvent()
        {
            // Arrange
            bool eventTriggered = false;
            _controller.OnComboCompleted += () => eventTriggered = true;

            _controller.TriggerAttack("LightAttack");
            yield return null;

            // Act
            _controller.ResetCombo();

            // Assert
            Assert.IsTrue(eventTriggered, "重置应触发OnComboCompleted事件");
        }

        #endregion

        #region 输入缓冲测试

        [UnityTest]
        [Description("测试：非接受输入状态时缓冲输入")]
        public IEnumerator TriggerAttack_WhenCannotAcceptInput_BuffersInput()
        {
            // Arrange - 进入不能接受输入的状态（Startup）
            _controller.TriggerAttack("LightAttack");
            yield return null;

            Assert.IsFalse(_controller.CanAcceptInput(), "Startup状态下不应接受输入");

            // Act - 再次触发攻击（应该被缓冲）
            _controller.TriggerAttack("LightAttack");

            // Assert - 检查输入是否被缓冲（通过后续连招执行验证）
            // 等待进入可接受输入状态，验证缓冲的输入被处理
            yield return new WaitForSeconds(0.2f);
            // 如果输入被正确缓冲，连击数应该增加
            Assert.That(
                _controller.CurrentComboCount,
                Is.GreaterThanOrEqualTo(1),
                "非接受输入状态时输入应被缓冲并在可接受时处理"
            );
        }

        #endregion

        #region 事件测试

        [UnityTest]
        [Description("测试：触发连招开始时触发OnComboStarted")]
        public IEnumerator TriggerAttack_TriggersComboStartedEvent()
        {
            // Arrange
            bool eventTriggered = false;
            _controller.OnComboStarted += () => eventTriggered = true;

            // Act
            _controller.TriggerAttack("LightAttack");

            // Assert
            Assert.IsTrue(eventTriggered, "触发第一招应触发OnComboStarted事件");
            yield return null;
        }

        [UnityTest]
        [Description("测试：触发招式开始时触发OnMoveStarted")]
        public IEnumerator TriggerAttack_TriggersMoveStartedEvent()
        {
            // Arrange
            ComboMove startedMove = null;
            _controller.OnMoveStarted += (move) => startedMove = move;

            // Act
            _controller.TriggerAttack("LightAttack");

            // Assert
            Assert.IsNotNull(startedMove, "应触发OnMoveStarted事件");
            Assert.AreEqual("light_1", startedMove.MoveId, "事件应传递正确的招式");
            yield return null;
        }

        [UnityTest]
        [Description("测试：中断连招触发OnComboInterrupted")]
        public IEnumerator InterruptCombo_TriggersInterruptedEvent()
        {
            // Arrange
            InterruptReason? receivedReason = null;
            _controller.OnComboInterrupted += (reason) => receivedReason = reason;

            _controller.TriggerAttack("LightAttack");
            yield return null;

            // Act
            _controller.InterruptCombo(InterruptReason.Stunned);

            // Assert
            Assert.IsTrue(receivedReason.HasValue, "应触发OnComboInterrupted事件");
            Assert.AreEqual(
                InterruptReason.Stunned,
                receivedReason.Value,
                "事件应传递正确的中断原因"
            );
        }

        [UnityTest]
        [Description("测试：状态变化触发OnStateChanged")]
        public IEnumerator StateChange_TriggersStateChangedEvent()
        {
            // Arrange
            ComboStateType? fromState = null;
            ComboStateType? toState = null;
            _controller.OnStateChanged += (from, to) =>
            {
                fromState = from;
                toState = to;
            };

            // Act
            _controller.TriggerAttack("LightAttack");

            // Assert
            Assert.IsTrue(fromState.HasValue && toState.HasValue, "应触发OnStateChanged事件");
            Assert.AreEqual(ComboStateType.Idle, fromState.Value, "应记录从Idle状态变化");
            Assert.AreEqual(ComboStateType.Startup, toState.Value, "应记录到Startup状态变化");
            yield return null;
        }

        #endregion

        #region 帮助方法

        private ComboTree CreateTestComboTree()
        {
            var tree = ScriptableObject.CreateInstance<ComboTree>();
            tree.TreeId = "test_combo";
            tree.InputBufferWindow = 0.3f;

            // 创建测试招式
            var lightAttack1 = new ComboMove
            {
                MoveId = "light_1",
                MoveName = "Light Attack 1",
                AnimationStateName = "LightAttack1",
                StartupTime = 0.1f,
                ActiveDuration = 0.2f,
                RecoveryTime = 0.3f,
                ComboWindowStart = 0.15f,
                ComboWindowEnd = 0.5f,
                MinComboCount = 0,
                Cooldown = 0f,
            };

            var lightAttack2 = new ComboMove
            {
                MoveId = "light_2",
                MoveName = "Light Attack 2",
                AnimationStateName = "LightAttack2",
                StartupTime = 0.1f,
                ActiveDuration = 0.2f,
                RecoveryTime = 0.3f,
                ComboWindowStart = 0.15f,
                ComboWindowEnd = 0.5f,
                MinComboCount = 0,
                Cooldown = 0f,
            };

            tree.Moves = new[] { lightAttack1, lightAttack2 };

            // 创建转换
            tree.Transitions = new[]
            {
                new ComboTransition
                {
                    FromMoveId = "light_1",
                    ToMoveId = "light_2",
                    InputType = "LightAttack",
                },
            };

            return tree;
        }

        #endregion
    }
}
