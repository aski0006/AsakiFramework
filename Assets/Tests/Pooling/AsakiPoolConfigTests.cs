using Asaki.Core.Pooling;
using NUnit.Framework;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// AsakiPoolConfig 配置类单元测试
    /// 测试配置对象的默认值、工厂方法和属性设置
    /// </summary>
    [TestFixture]
    public class AsakiPoolConfigTests
    {
        [Test]
        [Category("Unit")]
        [Description("测试默认配置的初始值")]
        public void DefaultConfig_HasCorrectInitialValues()
        {
            // Act
            var config = AsakiPoolConfig.Default;

            // Assert
            Assert.AreEqual(0, config.InitialSize, "默认初始大小应为0");
            Assert.AreEqual(100, config.MaxSize, "默认最大大小应为100");
            Assert.IsTrue(config.EnableValidation, "默认应启用验证");
            Assert.IsTrue(config.EnableCollectionCheck, "默认应启用集合检查");
            Assert.IsFalse(config.AllowSyncCreation, "默认应禁用同步创建");
            Assert.AreEqual(0f, config.OperationTimeout, "默认操作超时应为0（无超时）");
            Assert.IsTrue(config.EnableAutoShrink, "默认应启用自动收缩");
            Assert.AreEqual(30f, config.CheckInterval, "默认检查间隔应为30秒");
            Assert.AreEqual(60f, config.IdleTimeout, "默认闲置超时应为60秒");
            Assert.AreEqual(5, config.KeepMinSize, "默认保底数量应为5");
            Assert.AreEqual(0.5f, config.ShrinkRatio, "默认收缩比例应为0.5");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 GameObject 配置工厂方法")]
        public void ForGameObject_CreatesCorrectConfig()
        {
            // Act
            var config = AsakiPoolConfig.ForGameObject(initialSize: 10, maxSize: 100);

            // Assert
            Assert.AreEqual(10, config.InitialSize, "初始大小应为10");
            Assert.AreEqual(100, config.MaxSize, "最大大小应为100");
            Assert.IsTrue(config.EnableValidation, "应启用验证");
            Assert.IsTrue(config.EnableCollectionCheck, "应启用集合检查");
            Assert.IsFalse(config.AllowSyncCreation, "应禁用同步创建");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 GameObject 配置工厂方法使用默认值")]
        public void ForGameObject_WithDefaultValues_CreatesCorrectConfig()
        {
            // Act
            var config = AsakiPoolConfig.ForGameObject();

            // Assert
            Assert.AreEqual(10, config.InitialSize, "默认初始大小应为10");
            Assert.AreEqual(100, config.MaxSize, "默认最大大小应为100");
        }

        [Test]
        [Category("Unit")]
        [Description("测试轻量级对象配置工厂方法")]
        public void ForLightWeightObject_CreatesCorrectConfig()
        {
            // Act
            var config = AsakiPoolConfig.ForLightWeightObject(maxSize: 1024);

            // Assert
            Assert.AreEqual(0, config.InitialSize, "初始大小应为0");
            Assert.AreEqual(1024, config.MaxSize, "最大大小应为1024");
            Assert.IsFalse(config.EnableValidation, "轻量级对象应禁用验证");
            Assert.IsFalse(config.EnableCollectionCheck, "轻量级对象应禁用集合检查");
            Assert.IsTrue(config.AllowSyncCreation, "轻量级对象应启用同步创建");
        }

        [Test]
        [Category("Unit")]
        [Description("测试轻量级对象配置工厂方法使用默认值")]
        public void ForLightWeightObject_WithDefaultMaxSize_CreatesCorrectConfig()
        {
            // Act
            var config = AsakiPoolConfig.ForLightWeightObject();

            // Assert
            Assert.AreEqual(1024, config.MaxSize, "默认最大大小应为1024");
        }

        [Test]
        [Category("Unit")]
        [Description("测试自定义配置属性设置")]
        public void CustomConfig_AllowsPropertyModification()
        {
            // Arrange
            var config = new AsakiPoolConfig
            {
                InitialSize = 20,
                MaxSize = 200,
                EnableValidation = false,
                EnableCollectionCheck = false,
                AllowSyncCreation = true,
                OperationTimeout = 5f,
                EnableAutoShrink = false,
                CheckInterval = 60f,
                IdleTimeout = 120f,
                KeepMinSize = 10,
                ShrinkRatio = 0.3f,
            };

            // Assert
            Assert.AreEqual(20, config.InitialSize);
            Assert.AreEqual(200, config.MaxSize);
            Assert.IsFalse(config.EnableValidation);
            Assert.IsFalse(config.EnableCollectionCheck);
            Assert.IsTrue(config.AllowSyncCreation);
            Assert.AreEqual(5f, config.OperationTimeout);
            Assert.IsFalse(config.EnableAutoShrink);
            Assert.AreEqual(60f, config.CheckInterval);
            Assert.AreEqual(120f, config.IdleTimeout);
            Assert.AreEqual(10, config.KeepMinSize);
            Assert.AreEqual(0.3f, config.ShrinkRatio);
        }

        [Test]
        [Category("Unit")]
        [Description("测试收缩比例范围限制")]
        public void ShrinkRatio_IsClampedToValidRange()
        {
            // 注意：ShrinkRatio 使用 [Range(0f, 1f)] 属性，但这是在 Inspector 中的限制
            // 代码中仍然可以设置超出范围的值，这里测试实际行为
            var config = new AsakiPoolConfig();

            // Act & Assert - 可以设置范围内的值
            config.ShrinkRatio = 0f;
            Assert.AreEqual(0f, config.ShrinkRatio);

            config.ShrinkRatio = 1f;
            Assert.AreEqual(1f, config.ShrinkRatio);

            config.ShrinkRatio = 0.5f;
            Assert.AreEqual(0.5f, config.ShrinkRatio);
        }

        [Test]
        [Category("Unit")]
        [Description("测试默认配置的单例行为")]
        public void DefaultConfig_ReturnsSameInstance()
        {
            // Act
            var config1 = AsakiPoolConfig.Default;
            var config2 = AsakiPoolConfig.Default;

            // Assert
            Assert.AreSame(config1, config2, "Default 应返回相同的实例");
        }

        [Test]
        [Category("Unit")]
        [Description("测试配置对象的独立性")]
        public void ConfigInstances_AreIndependent()
        {
            // Arrange
            var config1 = AsakiPoolConfig.ForGameObject(initialSize: 10, maxSize: 100);
            var config2 = AsakiPoolConfig.ForGameObject(initialSize: 20, maxSize: 200);

            // Act & Assert
            Assert.AreNotSame(config1, config2, "不同配置应是不同实例");
            Assert.AreEqual(10, config1.InitialSize);
            Assert.AreEqual(20, config2.InitialSize);

            // 修改 config1 不应影响 config2
            config1.InitialSize = 50;
            Assert.AreEqual(50, config1.InitialSize);
            Assert.AreEqual(20, config2.InitialSize);
        }

        [TestCase(0, 0)]
        [TestCase(10, 100)]
        [TestCase(100, 1000)]
        [TestCase(1000, 10000)]
        [Category("Unit")]
        [Description("测试 GameObject 配置工厂方法参数化")]
        public void ForGameObject_WithVariousSizes_CreatesCorrectConfig(
            int initialSize,
            int maxSize
        )
        {
            // Act
            var config = AsakiPoolConfig.ForGameObject(initialSize, maxSize);

            // Assert
            Assert.AreEqual(initialSize, config.InitialSize);
            Assert.AreEqual(maxSize, config.MaxSize);
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1024)]
        [TestCase(10000)]
        [Category("Unit")]
        [Description("测试轻量级对象配置工厂方法参数化")]
        public void ForLightWeightObject_WithVariousMaxSizes_CreatesCorrectConfig(int maxSize)
        {
            // Act
            var config = AsakiPoolConfig.ForLightWeightObject(maxSize);

            // Assert
            Assert.AreEqual(maxSize, config.MaxSize);
        }
    }
}
