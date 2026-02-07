using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Asaki.Plungin.ComboSystem;

namespace Asaki.Tests.ComboSystem
{
    /// <summary>
    /// ComboTree 单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("ComboSystem")]
    public class ComboTreeTests
    {
        private ComboTree _comboTree;

        [SetUp]
        public void Setup()
        {
            // 创建测试用的ComboTree ScriptableObject
            _comboTree = ScriptableObject.CreateInstance<ComboTree>();
            _comboTree.TreeId = "test_tree";
            _comboTree.Description = "Test Combo Tree";
            _comboTree.InputBufferWindow = 0.3f;
            _comboTree.MaxComboDuration = 10f;
            _comboTree.MaxComboLength = 10;
        }

        [TearDown]
        public void Teardown()
        {
            if (_comboTree != null)
            {
                Object.DestroyImmediate(_comboTree);
            }
        }

        #region 基础数据测试

        [Test]
        [Description("测试：ComboTree默认属性值")]
        public void DefaultValues_AreCorrect()
        {
            // Assert
            Assert.AreEqual("test_tree", _comboTree.TreeId);
            Assert.AreEqual(0.3f, _comboTree.InputBufferWindow);
            Assert.AreEqual(10f, _comboTree.MaxComboDuration);
            Assert.AreEqual(10, _comboTree.MaxComboLength);
        }

        #endregion

        #region 招式查找测试

        [Test]
        [Description("测试：添加招式后可以通过ID查找")]
        public void GetMove_AfterAddingMove_CanRetrieve()
        {
            // Arrange
            var move = CreateTestMove("move_1", "Test Move");
            _comboTree.Moves = new[] { move };

            // 触发OnEnable来重建查找表
            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act
            var retrieved = _comboTree.GetMove("move_1");

            // Assert
            Assert.IsNotNull(retrieved, "应该能找到招式");
            Assert.AreEqual("move_1", retrieved.MoveId);
            Assert.AreEqual("Test Move", retrieved.MoveName);
        }

        [Test]
        [Description("测试：查找不存在的招式返回null")]
        public void GetMove_NonExistentMove_ReturnsNull()
        {
            // Arrange
            _comboTree.Moves = new ComboMove[0];
            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act
            var retrieved = _comboTree.GetMove("non_existent");

            // Assert
            Assert.IsNull(retrieved, "不存在的招式应返回null");
        }

        [Test]
        [Description("测试：可以添加多个招式")]
        public void GetMove_MultipleMoves_CanRetrieveAll()
        {
            // Arrange
            var move1 = CreateTestMove("move_1", "Move 1");
            var move2 = CreateTestMove("move_2", "Move 2");
            var move3 = CreateTestMove("move_3", "Move 3");
            _comboTree.Moves = new[] { move1, move2, move3 };

            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act & Assert
            Assert.AreEqual("Move 1", _comboTree.GetMove("move_1").MoveName);
            Assert.AreEqual("Move 2", _comboTree.GetMove("move_2").MoveName);
            Assert.AreEqual("Move 3", _comboTree.GetMove("move_3").MoveName);
        }

        #endregion

        #region 转换查找测试

        [Test]
        [Description("测试：可以从一个招式查找到下一个招式")]
        public void FindNextMove_WithValidTransition_ReturnsNextMove()
        {
            // Arrange
            var move1 = CreateTestMove("move_1", "Move 1");
            var move2 = CreateTestMove("move_2", "Move 2");
            
            _comboTree.Moves = new[] { move1, move2 };
            _comboTree.Transitions = new[]
            {
                new ComboTransition
                {
                    FromMoveId = "move_1",
                    ToMoveId = "move_2",
                    InputType = "LightAttack"
                }
            };

            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act
            var nextMove = _comboTree.FindNextMove("move_1", "LightAttack");

            // Assert
            Assert.IsNotNull(nextMove, "应该能找到下一个招式");
            Assert.AreEqual("move_2", nextMove.MoveId);
        }

        [Test]
        [Description("测试：不匹配的输入类型返回null")]
        public void FindNextMove_WrongInputType_ReturnsNull()
        {
            // Arrange
            var move1 = CreateTestMove("move_1", "Move 1");
            var move2 = CreateTestMove("move_2", "Move 2");
            
            _comboTree.Moves = new[] { move1, move2 };
            _comboTree.Transitions = new[]
            {
                new ComboTransition
                {
                    FromMoveId = "move_1",
                    ToMoveId = "move_2",
                    InputType = "LightAttack"
                }
            };

            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act
            var nextMove = _comboTree.FindNextMove("move_1", "HeavyAttack");

            // Assert
            Assert.IsNull(nextMove, "不匹配的输入类型应返回null");
        }

        [Test]
        [Description("测试：多个可能的转换按顺序返回第一个")]
        public void FindNextMove_MultipleTransitions_ReturnsFirstMatch()
        {
            // Arrange
            var move1 = CreateTestMove("move_1", "Move 1");
            var move2 = CreateTestMove("move_2", "Move 2");
            var move3 = CreateTestMove("move_3", "Move 3");
            
            _comboTree.Moves = new[] { move1, move2, move3 };
            _comboTree.Transitions = new[]
            {
                new ComboTransition
                {
                    FromMoveId = "move_1",
                    ToMoveId = "move_2",
                    InputType = "LightAttack"
                },
                new ComboTransition
                {
                    FromMoveId = "move_1",
                    ToMoveId = "move_3",
                    InputType = "LightAttack"
                }
            };

            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act
            var nextMove = _comboTree.FindNextMove("move_1", "LightAttack");

            // Assert
            Assert.IsNotNull(nextMove);
            Assert.AreEqual("move_2", nextMove.MoveId, "应该返回第一个匹配的转换");
        }

        #endregion

        #region 可用转换测试

        [Test]
        [Description("测试：获取所有可用转换")]
        public void GetAvailableTransitions_ReturnsValidTransitions()
        {
            // Arrange
            var move1 = CreateTestMove("move_1", "Move 1");
            var move2 = CreateTestMove("move_2", "Move 2");
            var move3 = CreateTestMove("move_3", "Move 3");
            
            _comboTree.Moves = new[] { move1, move2, move3 };
            _comboTree.Transitions = new[]
            {
                new ComboTransition
                {
                    FromMoveId = "move_1",
                    ToMoveId = "move_2",
                    InputType = "LightAttack"
                },
                new ComboTransition
                {
                    FromMoveId = "move_1",
                    ToMoveId = "move_3",
                    InputType = "HeavyAttack"
                }
            };

            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act
            var transitions = _comboTree.GetAvailableTransitions("move_1");

            // Assert
            Assert.AreEqual(2, transitions.Count, "应该返回两个转换");
        }

        [Test]
        [Description("测试：无可用转换时返回空列表")]
        public void GetAvailableTransitions_NoTransitions_ReturnsEmptyList()
        {
            // Arrange
            _comboTree.Moves = new ComboMove[0];
            _comboTree.Transitions = new ComboTransition[0];

            var method = typeof(ComboTree).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            method?.Invoke(_comboTree, null);

            // Act
            var transitions = _comboTree.GetAvailableTransitions("move_1");

            // Assert
            Assert.IsNotNull(transitions, "应返回空列表而非null");
            Assert.AreEqual(0, transitions.Count, "应返回空列表");
        }

        #endregion

        #region 边界条件测试

        [Test]
        [Description("测试：无效转换（空FromMoveId）")]
        public void Transition_InvalidFromMoveId_IsNotValid()
        {
            // Arrange
            var transition = new ComboTransition
            {
                FromMoveId = "",
                ToMoveId = "move_2",
                InputType = "LightAttack"
            };

            // Assert
            Assert.IsFalse(transition.IsValid(), "空FromMoveId应为无效");
        }

        [Test]
        [Description("测试：无效转换（空ToMoveId）")]
        public void Transition_InvalidToMoveId_IsNotValid()
        {
            // Arrange
            var transition = new ComboTransition
            {
                FromMoveId = "move_1",
                ToMoveId = "",
                InputType = "LightAttack"
            };

            // Assert
            Assert.IsFalse(transition.IsValid(), "空ToMoveId应为无效");
        }

        [Test]
        [Description("测试：有效转换")]
        public void Transition_ValidData_IsValid()
        {
            // Arrange
            var transition = new ComboTransition
            {
                FromMoveId = "move_1",
                ToMoveId = "move_2",
                InputType = "LightAttack"
            };

            // Assert
            Assert.IsTrue(transition.IsValid(), "有效数据应为有效");
        }

        #endregion

        #region 帮助方法

        private ComboMove CreateTestMove(string moveId, string moveName)
        {
            return new ComboMove
            {
                MoveId = moveId,
                MoveName = moveName,
                AnimationStateName = $"Anim_{moveId}",
                StartupTime = 0.1f,
                ActiveDuration = 0.2f,
                RecoveryTime = 0.3f
            };
        }

        #endregion
    }
}
