using Asaki.Plungin.ComboSystem;
using NUnit.Framework;
using UnityEngine;

namespace Asaki.Tests.ComboSystem
{
    /// <summary>
    /// 连招重置策略单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("ComboSystem")]
    public class ComboResetStrategyTests
    {
        private ComboContext _context;

        [SetUp]
        public void Setup()
        {
            _context = new ComboContext { ComboCount = 10, ComboTimer = 5f };
        }

        #region ResetToZeroStrategy 测试

        [Test]
        [Description("测试：ResetToZero策略总是返回0")]
        public void ResetToZeroStrategy_AlwaysReturnsZero()
        {
            // Arrange
            var strategy = new ResetToZeroStrategy();

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(0, result, "ResetToZero策略应始终返回0");
        }

        [Test]
        [Description("测试：ResetToZero策略总是应该重置")]
        public void ResetToZeroStrategy_ShouldReset_ReturnsTrue()
        {
            // Arrange
            var strategy = new ResetToZeroStrategy();

            // Act & Assert
            Assert.IsTrue(strategy.ShouldReset(_context), "ResetToZero策略应始终返回true");
        }

        #endregion

        #region KeepCountStrategy 测试

        [Test]
        [Description("测试：KeepCount策略保持当前计数")]
        public void KeepCountStrategy_KeepsCurrentCount()
        {
            // Arrange
            var strategy = new KeepCountStrategy();

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(10, result, "KeepCount策略应保持当前计数");
        }

        [Test]
        [Description("测试：KeepCount策略不应该触发重置")]
        public void KeepCountStrategy_ShouldReset_ReturnsFalse()
        {
            // Arrange
            var strategy = new KeepCountStrategy();

            // Act & Assert
            Assert.IsFalse(strategy.ShouldReset(_context), "KeepCount策略应返回false");
        }

        #endregion

        #region DecayCountStrategy 测试

        [Test]
        [Description("测试：Decay策略减少固定值")]
        public void DecayCountStrategy_ReducesByFixedAmount()
        {
            // Arrange
            var strategy = new DecayCountStrategy { DecayAmount = 2, MinCount = 0 };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(8, result, "应减少固定值");
        }

        [Test]
        [Description("测试：Decay策略不会低于最小值")]
        public void DecayCountStrategy_RespectsMinCount()
        {
            // Arrange
            var strategy = new DecayCountStrategy { DecayAmount = 5, MinCount = 3 };

            // Act
            int result = strategy.CalculateResetCount(5, _context);

            // Assert
            Assert.AreEqual(3, result, "不应低于最小值");
        }

        [Test]
        [Description("测试：Decay策略当前计数为0时返回0")]
        public void DecayCountStrategy_ZeroCount_ReturnsZero()
        {
            // Arrange
            var strategy = new DecayCountStrategy { DecayAmount = 2, MinCount = 0 };

            // Act
            int result = strategy.CalculateResetCount(0, _context);

            // Assert
            Assert.AreEqual(0, result, "0计数应保持为0");
        }

        #endregion

        #region PercentageDecayStrategy 测试

        [Test]
        [Description("测试：PercentageDecay策略按百分比减少")]
        public void PercentageDecayStrategy_ReducesByPercentage()
        {
            // Arrange
            var strategy = new PercentageDecayStrategy { DecayPercent = 0.5f, MinCount = 0 };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(5, result, "应按50%减少");
        }

        [Test]
        [Description("测试：PercentageDecay策略四舍五入")]
        public void PercentageDecayStrategy_RoundsCorrectly()
        {
            // Arrange
            var strategy = new PercentageDecayStrategy { DecayPercent = 0.33f, MinCount = 0 };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert (10 * 0.67 = 6.7, 四舍五入为7)
            Assert.AreEqual(7, result, "应正确四舍五入");
        }

        [Test]
        [Description("测试：PercentageDecay策略不会低于最小值")]
        public void PercentageDecayStrategy_RespectsMinCount()
        {
            // Arrange
            var strategy = new PercentageDecayStrategy { DecayPercent = 0.9f, MinCount = 2 };

            // Act
            int result = strategy.CalculateResetCount(5, _context);

            // Assert
            Assert.AreEqual(2, result, "不应低于最小值");
        }

        #endregion

        #region SetToSpecificStrategy 测试

        [Test]
        [Description("测试：SetToSpecific策略设置为特定值")]
        public void SetToSpecificStrategy_SetsToTargetCount()
        {
            // Arrange
            var strategy = new SetToSpecificStrategy { TargetCount = 5 };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(5, result, "应设置为特定值");
        }

        [Test]
        [Description("测试：SetToSpecific策略可以设置为0")]
        public void SetToSpecificStrategy_CanSetToZero()
        {
            // Arrange
            var strategy = new SetToSpecificStrategy { TargetCount = 0 };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(0, result, "应能设置为0");
        }

        #endregion

        #region CustomResetStrategy 测试

        [Test]
        [Description("测试：CustomReset策略使用自定义函数")]
        public void CustomResetStrategy_UsesCustomFunction()
        {
            // Arrange
            var strategy = new CustomResetStrategy
            {
                ResetFunction = (count, ctx) => count * 2,
                ShouldResetFunction = ctx => true,
            };

            // Act
            int result = strategy.CalculateResetCount(5, _context);

            // Assert
            Assert.AreEqual(10, result, "应使用自定义函数计算");
        }

        [Test]
        [Description("测试：CustomReset策略使用自定义条件")]
        public void CustomResetStrategy_UsesCustomCondition()
        {
            // Arrange
            var strategy = new CustomResetStrategy
            {
                ShouldResetFunction = ctx => ctx.ComboCount > 5,
            };

            // Act & Assert
            Assert.IsTrue(strategy.ShouldReset(_context), "条件满足时应返回true");

            _context.ComboCount = 3;
            Assert.IsFalse(strategy.ShouldReset(_context), "条件不满足时应返回false");
        }

        [Test]
        [Description("测试：CustomReset策略函数为null时默认返回0")]
        public void CustomResetStrategy_NullFunction_ReturnsZero()
        {
            // Arrange
            var strategy = new CustomResetStrategy { ResetFunction = null };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(0, result, "函数为null时应返回0");
        }

        #endregion

        #region ConditionalResetStrategy 测试

        [Test]
        [Description("测试：ConditionalReset策略条件为真时使用TrueStrategy")]
        public void ConditionalResetStrategy_ConditionTrue_UsesTrueStrategy()
        {
            // Arrange
            var strategy = new ConditionalResetStrategy
            {
                Condition = ctx => ctx.ComboCount > 5,
                TrueStrategy = new ResetToZeroStrategy(),
                FalseStrategy = new KeepCountStrategy(),
            };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(0, result, "条件为真时应使用TrueStrategy");
        }

        [Test]
        [Description("测试：ConditionalReset策略条件为假时使用FalseStrategy")]
        public void ConditionalResetStrategy_ConditionFalse_UsesFalseStrategy()
        {
            // Arrange
            var strategy = new ConditionalResetStrategy
            {
                Condition = ctx => ctx.ComboCount > 15,
                TrueStrategy = new ResetToZeroStrategy(),
                FalseStrategy = new KeepCountStrategy(),
            };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(10, result, "条件为假时应使用FalseStrategy");
        }

        #endregion

        #region CompositeResetStrategy 测试

        [Test]
        [Description("测试：Composite策略Sequential模式链式执行")]
        public void CompositeResetStrategy_Sequential_ChainsExecution()
        {
            // Arrange
            var strategy = new CompositeResetStrategy
            {
                Mode = CompositeMode.Sequential,
                Strategies = new System.Collections.Generic.List<IComboResetStrategy>
                {
                    new DecayCountStrategy { DecayAmount = 2, MinCount = 0 }, // 10 -> 8
                    new DecayCountStrategy { DecayAmount = 3, MinCount = 0 }, // 8 -> 5
                },
            };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(5, result, "Sequential模式应链式执行");
        }

        [Test]
        [Description("测试：Composite策略Minimum模式取最小值")]
        public void CompositeResetStrategy_Minimum_ReturnsMin()
        {
            // Arrange
            var strategy = new CompositeResetStrategy
            {
                Mode = CompositeMode.Minimum,
                Strategies = new System.Collections.Generic.List<IComboResetStrategy>
                {
                    new SetToSpecificStrategy { TargetCount = 8 },
                    new SetToSpecificStrategy { TargetCount = 3 },
                    new SetToSpecificStrategy { TargetCount = 5 },
                },
            };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(3, result, "Minimum模式应返回最小值");
        }

        [Test]
        [Description("测试：Composite策略Maximum模式取最大值")]
        public void CompositeResetStrategy_Maximum_ReturnsMax()
        {
            // Arrange
            var strategy = new CompositeResetStrategy
            {
                Mode = CompositeMode.Maximum,
                Strategies = new System.Collections.Generic.List<IComboResetStrategy>
                {
                    new SetToSpecificStrategy { TargetCount = 3 },
                    new SetToSpecificStrategy { TargetCount = 8 },
                    new SetToSpecificStrategy { TargetCount = 5 },
                },
            };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(8, result, "Maximum模式应返回最大值");
        }

        [Test]
        [Description("测试：Composite策略Average模式取平均值")]
        public void CompositeResetStrategy_Average_ReturnsAverage()
        {
            // Arrange
            var strategy = new CompositeResetStrategy
            {
                Mode = CompositeMode.Average,
                Strategies = new System.Collections.Generic.List<IComboResetStrategy>
                {
                    new SetToSpecificStrategy { TargetCount = 4 },
                    new SetToSpecificStrategy { TargetCount = 6 },
                    new SetToSpecificStrategy { TargetCount = 8 },
                },
            };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert ((4 + 6 + 8) / 3 = 6)
            Assert.AreEqual(6, result, "Average模式应返回平均值（四舍五入）");
        }

        [Test]
        [Description("测试：Composite策略空列表返回原值")]
        public void CompositeResetStrategy_EmptyList_ReturnsOriginal()
        {
            // Arrange
            var strategy = new CompositeResetStrategy
            {
                Mode = CompositeMode.Sequential,
                Strategies = new System.Collections.Generic.List<IComboResetStrategy>(),
            };

            // Act
            int result = strategy.CalculateResetCount(10, _context);

            // Assert
            Assert.AreEqual(10, result, "空列表应返回原值");
        }

        [Test]
        [Description("测试：Composite策略任一子策略ShouldReset为true则返回true")]
        public void CompositeResetStrategy_ShouldReset_AnyTrueReturnsTrue()
        {
            // Arrange
            var strategy = new CompositeResetStrategy
            {
                Strategies = new System.Collections.Generic.List<IComboResetStrategy>
                {
                    new KeepCountStrategy(), // ShouldReset = false
                    new ResetToZeroStrategy(), // ShouldReset = true
                },
            };

            // Act & Assert
            Assert.IsTrue(strategy.ShouldReset(_context), "任一策略返回true则应返回true");
        }

        #endregion

        #region ComboContext 测试

        [Test]
        [Description("测试：ComboContext可以存储和获取数据")]
        public void ComboContext_CanStoreAndRetrieveData()
        {
            // Arrange
            var context = new ComboContext();

            // Act
            context.SetData("testKey", 42);
            context.SetData("anotherKey", "testValue");

            // Assert
            Assert.AreEqual(42, context.GetData<int>("testKey"));
            Assert.AreEqual("testValue", context.GetData<string>("anotherKey"));
        }

        [Test]
        [Description("测试：ComboContext获取不存在的键返回默认值")]
        public void ComboContext_MissingKey_ReturnsDefault()
        {
            // Arrange
            var context = new ComboContext();

            // Act
            int result = context.GetData<int>("nonExistent");

            // Assert
            Assert.AreEqual(0, result, "不存在的键应返回默认值");
        }

        [Test]
        [Description("测试：ComboContext可以存储InterruptReason")]
        public void ComboContext_CanStoreInterruptReason()
        {
            // Arrange
            var context = new ComboContext();

            // Act
            context.InterruptReason = Asaki.Plungin.ComboSystem.InterruptReason.Damaged;

            // Assert
            Assert.AreEqual(
                Asaki.Plungin.ComboSystem.InterruptReason.Damaged,
                context.InterruptReason
            );
        }

        #endregion
    }
}
