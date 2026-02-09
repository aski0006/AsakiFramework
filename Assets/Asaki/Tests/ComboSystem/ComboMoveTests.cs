using Asaki.Plungin.ComboSystem;
using NUnit.Framework;
using UnityEngine;

namespace Asaki.Tests.ComboSystem
{
    /// <summary>
    /// ComboMove 及相关数据单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("ComboSystem")]
    public class ComboMoveTests
    {
        #region ComboMove 基础测试

        [Test]
        [Description("测试：ComboMove默认属性值")]
        public void ComboMove_DefaultValues_AreCorrect()
        {
            // Arrange
            var move = new ComboMove();

            // Assert
            Assert.AreEqual(1f, move.AnimationSpeed, "默认动画速度应为1");
            Assert.AreEqual(0, move.MinComboCount, "默认最小连击数应为0");
            Assert.AreEqual(0, move.MaxComboCount, "默认最大连击数应为0");
            Assert.AreEqual(0f, move.Cooldown, "默认冷却时间应为0");
            Assert.AreEqual(-999f, move.LastUsedTime, "默认上次使用时间应为-999");
        }

        [Test]
        [Description("测试：ComboMove可以设置属性")]
        public void ComboMove_Properties_CanBeSet()
        {
            // Arrange
            var move = new ComboMove
            {
                MoveId = "test_move",
                MoveName = "Test Move",
                AnimationStateName = "TestAnimation",
                AnimationSpeed = 1.5f,
                StartupTime = 0.2f,
                ActiveDuration = 0.3f,
                RecoveryTime = 0.4f,
                MinComboCount = 1,
                MaxComboCount = 5,
                Cooldown = 2f,
            };

            // Assert
            Assert.AreEqual("test_move", move.MoveId);
            Assert.AreEqual("Test Move", move.MoveName);
            Assert.AreEqual("TestAnimation", move.AnimationStateName);
            Assert.AreEqual(1.5f, move.AnimationSpeed);
            Assert.AreEqual(0.2f, move.StartupTime);
            Assert.AreEqual(0.3f, move.ActiveDuration);
            Assert.AreEqual(0.4f, move.RecoveryTime);
            Assert.AreEqual(1, move.MinComboCount);
            Assert.AreEqual(5, move.MaxComboCount);
            Assert.AreEqual(2f, move.Cooldown);
        }

        #endregion

        #region 冷却测试

        [Test]
        [Description("测试：新招式不在冷却中")]
        public void IsOnCooldown_NewMove_NotOnCooldown()
        {
            // Arrange
            var move = new ComboMove { MoveId = "test", Cooldown = 2f };

            // Act
            bool onCooldown = move.IsOnCooldown(Time.time);

            // Assert
            Assert.IsFalse(onCooldown, "新招式不应在冷却中");
        }

        [Test]
        [Description("测试：设置LastUsedTime后招式在冷却中")]
        public void IsOnCooldown_AfterUse_IsOnCooldown()
        {
            // Arrange
            var move = new ComboMove
            {
                MoveId = "test",
                Cooldown = 10f,
                LastUsedTime = Time.time,
            };

            // Act
            bool onCooldown = move.IsOnCooldown(Time.time);

            // Assert
            Assert.IsTrue(onCooldown, "刚使用的招式应在冷却中");
        }

        [Test]
        [Description("测试：冷却时间过后招式可用")]
        public void IsOnCooldown_AfterCooldownExpired_NotOnCooldown()
        {
            // Arrange
            var move = new ComboMove
            {
                MoveId = "test",
                Cooldown = 1f,
                LastUsedTime = Time.time - 2f, // 2秒前使用
            };

            // Act
            bool onCooldown = move.IsOnCooldown(Time.time);

            // Assert
            Assert.IsFalse(onCooldown, "冷却过期后招式应可用");
        }

        [Test]
        [Description("测试：无冷却的招式总是可用")]
        public void IsOnCooldown_NoCooldown_AlwaysAvailable()
        {
            // Arrange
            var move = new ComboMove
            {
                MoveId = "test",
                Cooldown = 0f,
                LastUsedTime = Time.time,
            };

            // Act
            bool onCooldown = move.IsOnCooldown(Time.time);

            // Assert
            Assert.IsFalse(onCooldown, "无冷却的招式应始终可用");
        }

        #endregion

        #region HitBoxDefinition 测试

        [Test]
        [Description("测试：HitBoxDefinition可以设置属性")]
        public void HitBoxDefinition_Properties_CanBeSet()
        {
            // Arrange
            var hitBox = new HitBoxDefinition
            {
                HitBoxId = "hand_r",
                Shape = HitBoxShape.Sphere,
                Offset = new Vector3(0, 1, 0),
                Size = new Vector3(1, 1, 1),
                Radius = 0.5f,
                Height = 2f,
                BoneName = "Hand_R",
            };

            // Assert
            Assert.AreEqual("hand_r", hitBox.HitBoxId);
            Assert.AreEqual(HitBoxShape.Sphere, hitBox.Shape);
            Assert.AreEqual(new Vector3(0, 1, 0), hitBox.Offset);
            Assert.AreEqual(0.5f, hitBox.Radius);
            Assert.AreEqual(2f, hitBox.Height);
            Assert.AreEqual("Hand_R", hitBox.BoneName);
        }

        [Test]
        [Description("测试：Box形状的HitBox")]
        public void HitBoxDefinition_BoxShape()
        {
            // Arrange
            var hitBox = new HitBoxDefinition
            {
                HitBoxId = "sword",
                Shape = HitBoxShape.Box,
                Size = new Vector3(0.5f, 2f, 0.2f),
            };

            // Assert
            Assert.AreEqual(HitBoxShape.Box, hitBox.Shape);
            Assert.AreEqual(new Vector3(0.5f, 2f, 0.2f), hitBox.Size);
        }

        [Test]
        [Description("测试：Capsule形状的HitBox")]
        public void HitBoxDefinition_CapsuleShape()
        {
            // Arrange
            var hitBox = new HitBoxDefinition
            {
                HitBoxId = "punch",
                Shape = HitBoxShape.Capsule,
                Radius = 0.3f,
                Height = 1.5f,
            };

            // Assert
            Assert.AreEqual(HitBoxShape.Capsule, hitBox.Shape);
            Assert.AreEqual(0.3f, hitBox.Radius);
            Assert.AreEqual(1.5f, hitBox.Height);
        }

        #endregion

        #region TransitionCondition 测试

        [Test]
        [Description("测试：TransitionCondition可以设置属性")]
        public void TransitionCondition_Properties_CanBeSet()
        {
            // Arrange
            var condition = new TransitionCondition
            {
                Type = ConditionType.ComboCount,
                Parameter = "min_count",
                Value = 5f,
            };

            // Assert
            Assert.AreEqual(ConditionType.ComboCount, condition.Type);
            Assert.AreEqual("min_count", condition.Parameter);
            Assert.AreEqual(5f, condition.Value);
        }

        [Test]
        [Description("测试：所有ConditionType枚举值")]
        public void ConditionType_AllValues()
        {
            // 验证所有枚举值存在
            var types = new[]
            {
                ConditionType.ComboCount,
                ConditionType.TimeWindow,
                ConditionType.HealthPercent,
                ConditionType.StaminaCost,
                ConditionType.Custom,
            };

            Assert.AreEqual(5, types.Length, "应有5种条件类型");
        }

        #endregion

        #region 枚举测试

        [Test]
        [Description("测试：ComboStateType枚举值")]
        public void ComboStateType_AllValues()
        {
            // Arrange & Act
            var states = new[]
            {
                ComboStateType.Idle,
                ComboStateType.Startup,
                ComboStateType.Active,
                ComboStateType.Recovery,
                ComboStateType.ComboWindow,
                ComboStateType.Interrupted,
            };

            // Assert
            Assert.AreEqual(6, states.Length, "应有6种连招状态");
            Assert.AreEqual(0, (int)ComboStateType.Idle);
            Assert.AreEqual(1, (int)ComboStateType.Startup);
            Assert.AreEqual(2, (int)ComboStateType.Active);
        }

        [Test]
        [Description("测试：InterruptReason枚举值")]
        public void InterruptReason_AllValues()
        {
            // Arrange & Act
            var reasons = new[]
            {
                InterruptReason.Damaged,
                InterruptReason.Stunned,
                InterruptReason.KnockedDown,
                InterruptReason.Forced,
                InterruptReason.UserCancel,
            };

            // Assert
            Assert.AreEqual(5, reasons.Length, "应有5种中断原因");
        }

        [Test]
        [Description("测试：HitBoxShape枚举值")]
        public void HitBoxShape_AllValues()
        {
            // Arrange & Act
            var shapes = new[] { HitBoxShape.Box, HitBoxShape.Sphere, HitBoxShape.Capsule };

            // Assert
            Assert.AreEqual(3, shapes.Length, "应有3种判定框形状");
        }

        [Test]
        [Description("测试：ResetComboMode枚举值")]
        public void ResetComboMode_AllValues()
        {
            // Arrange & Act
            var modes = new[]
            {
                ResetComboMode.ResetToZero,
                ResetComboMode.KeepCount,
                ResetComboMode.Decay,
                ResetComboMode.PercentageDecay,
                ResetComboMode.SetToSpecific,
                ResetComboMode.CustomFunction,
            };

            // Assert
            Assert.AreEqual(6, modes.Length, "应有6种重置模式");
        }

        [Test]
        [Description("测试：CompositeMode枚举值")]
        public void CompositeMode_AllValues()
        {
            // Arrange & Act
            var modes = new[]
            {
                CompositeMode.Sequential,
                CompositeMode.Minimum,
                CompositeMode.Maximum,
                CompositeMode.Average,
            };

            // Assert
            Assert.AreEqual(4, modes.Length, "应有4种组合模式");
        }

        #endregion

        #region 复杂招式测试

        [Test]
        [Description("测试：创建复杂的ComboMove配置")]
        public void ComboMove_ComplexConfiguration()
        {
            // Arrange
            var move = new ComboMove
            {
                MoveId = "combo_finisher",
                MoveName = "Combo Finisher",
                AnimationStateName = "Finisher",
                AnimationSpeed = 1.2f,
                StartupTime = 0.15f,
                ActiveDuration = 0.25f,
                RecoveryTime = 0.5f,
                ComboWindowStart = 0.3f,
                ComboWindowEnd = 0.6f,
                HitBoxes = new[]
                {
                    new HitBoxDefinition
                    {
                        HitBoxId = "sword_blade",
                        Shape = HitBoxShape.Box,
                        Offset = new Vector3(0, 0, 1),
                        Size = new Vector3(0.3f, 0.1f, 1.5f),
                        BoneName = "Weapon",
                    },
                    new HitBoxDefinition
                    {
                        HitBoxId = "sword_tip",
                        Shape = HitBoxShape.Sphere,
                        Offset = new Vector3(0, 0, 2),
                        Radius = 0.2f,
                        BoneName = "WeaponTip",
                    },
                },
                MinComboCount = 3,
                MaxComboCount = 10,
                Cooldown = 5f,
            };

            // Assert
            Assert.AreEqual(2, move.HitBoxes.Length, "应有2个判定框");
            Assert.AreEqual("sword_blade", move.HitBoxes[0].HitBoxId);
            Assert.AreEqual("sword_tip", move.HitBoxes[1].HitBoxId);
            Assert.AreEqual(HitBoxShape.Box, move.HitBoxes[0].Shape);
            Assert.AreEqual(HitBoxShape.Sphere, move.HitBoxes[1].Shape);
        }

        [Test]
        [Description("测试：没有判定框的招式")]
        public void ComboMove_NoHitBoxes()
        {
            // Arrange
            var move = new ComboMove
            {
                MoveId = "dodge",
                MoveName = "Dodge",
                HitBoxes = null,
            };

            // Assert
            Assert.IsNull(move.HitBoxes);
        }

        [Test]
        [Description("测试：空判定框数组")]
        public void ComboMove_EmptyHitBoxes()
        {
            // Arrange
            var move = new ComboMove
            {
                MoveId = "taunt",
                MoveName = "Taunt",
                HitBoxes = new HitBoxDefinition[0],
            };

            // Assert
            Assert.IsNotNull(move.HitBoxes);
            Assert.AreEqual(0, move.HitBoxes.Length);
        }

        #endregion

        #region 转换测试

        [Test]
        [Description("测试：ComboTransition默认ResetGroup")]
        public void ComboTransition_DefaultResetGroup()
        {
            // Arrange
            var transition = new ComboTransition();

            // Assert
            Assert.AreEqual("default", transition.ResetGroup, "默认ResetGroup应为'default'");
        }

        [Test]
        [Description("测试：带条件的转换")]
        public void ComboTransition_WithConditions()
        {
            // Arrange
            var transition = new ComboTransition
            {
                FromMoveId = "move_1",
                ToMoveId = "move_2",
                InputType = "LightAttack",
                Conditions = new[]
                {
                    new TransitionCondition { Type = ConditionType.ComboCount, Value = 3f },
                    new TransitionCondition { Type = ConditionType.HealthPercent, Value = 0.5f },
                },
                ResetGroup = "special",
            };

            // Assert
            Assert.AreEqual(2, transition.Conditions.Length);
            Assert.AreEqual("special", transition.ResetGroup);
            Assert.IsTrue(transition.IsValid());
        }

        #endregion
    }
}
