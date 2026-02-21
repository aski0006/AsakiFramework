using Asaki.Core.FrameworkSettings;
using Asaki.Core.Pooling;
using NUnit.Framework;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// AsakiPoolGlobalConfig 全局配置类单元测试
    /// 测试全局配置的默认值、JSON序列化和重置功能
    /// </summary>
    [TestFixture]
    public class AsakiPoolGlobalConfigTests
    {
        [SetUp]
        public void SetUp()
        {
            AsakiPoolGlobalConfig.Instance.ResetToDefaults();
        }

        [Test]
        [Category("Unit")]
        [Description("测试全局配置默认值")]
        public void GlobalConfig_HasCorrectDefaultValues()
        {
            var config = new AsakiPoolGlobalConfig();

            Assert.AreEqual(10, config.DefaultInitialSize);
            Assert.AreEqual(100, config.DefaultMaxSize);
            Assert.IsTrue(config.DefaultEnableValidation);
            Assert.IsTrue(config.DefaultEnableCollectionCheck);
            Assert.IsFalse(config.DefaultAllowSyncCreation);
            Assert.AreEqual(0f, config.DefaultOperationTimeout);
            Assert.AreEqual(5, config.DefaultPrewarmItemsPerFrame);
            Assert.AreEqual(16, config.DefaultPoolCapacity);

            Assert.IsTrue(config.DefaultEnableAutoShrink);
            Assert.AreEqual(30f, config.DefaultCheckInterval);
            Assert.AreEqual(60f, config.DefaultIdleTimeout);
            Assert.AreEqual(5, config.DefaultKeepMinSize);
            Assert.AreEqual(0.5f, config.DefaultShrinkRatio);

            Assert.AreEqual(32, config.EventPoolDefaultThreshold);
            Assert.AreEqual(64, config.EventPoolMaxSize);

            Assert.AreEqual(32, config.StringBuilderPoolInitialCapacity);
            Assert.AreEqual(64 * 1024, config.StringBuilderMaxRetainCapacity);
            Assert.AreEqual(1024, config.StringBuilderInitialCapacity);

            Assert.AreEqual(256, config.LogCommandPoolMaxSize);

            Assert.AreEqual(0, config.ArchitecturePoolInitialSize);
            Assert.AreEqual(128, config.ArchitecturePoolMaxSize);
            Assert.IsTrue(config.ArchitecturePoolEnableValidation);
            Assert.IsFalse(config.ArchitecturePoolEnableCollectionCheck);
            Assert.IsTrue(config.ArchitecturePoolAllowSyncCreation);

            Assert.AreEqual(16, config.AudioPoolDefaultInitialSize);
            Assert.AreEqual(100, config.AudioPoolDefaultMaxSize);
            Assert.AreEqual(32, config.AudioPoolDefaultActiveAgentCapacity);

            Assert.AreEqual(1024, config.LightWeightPoolDefaultMaxSize);
        }

        [Test]
        [Category("Unit")]
        [Description("测试JSON序列化和反序列化")]
        public void GlobalConfig_SerializeAndDeserialize_RoundTrip()
        {
            var original = new AsakiPoolGlobalConfig
            {
                DefaultInitialSize = 20,
                DefaultMaxSize = 200,
                EventPoolMaxSize = 128,
                LogCommandPoolMaxSize = 512,
            };

            string json = original.ToJson();
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);

            var deserialized = AsakiPoolGlobalConfig.FromJson(json);
            Assert.AreEqual(20, deserialized.DefaultInitialSize);
            Assert.AreEqual(200, deserialized.DefaultMaxSize);
            Assert.AreEqual(128, deserialized.EventPoolMaxSize);
            Assert.AreEqual(512, deserialized.LogCommandPoolMaxSize);
        }

        [Test]
        [Category("Unit")]
        [Description("测试空JSON返回默认配置")]
        public void GlobalConfig_FromEmptyJson_ReturnsDefaultConfig()
        {
            var config = AsakiPoolGlobalConfig.FromJson("");
            Assert.AreEqual(10, config.DefaultInitialSize);

            config = AsakiPoolGlobalConfig.FromJson(null);
            Assert.AreEqual(10, config.DefaultInitialSize);
        }

        [Test]
        [Category("Unit")]
        [Description("测试重置为默认值")]
        public void GlobalConfig_ResetToDefaults_RestoresAllValues()
        {
            var config = new AsakiPoolGlobalConfig
            {
                DefaultInitialSize = 999,
                DefaultMaxSize = 9999,
            };

            config.ResetToDefaults();

            Assert.AreEqual(10, config.DefaultInitialSize);
            Assert.AreEqual(100, config.DefaultMaxSize);
        }

        [Test]
        [Category("Unit")]
        [Description("测试单例实例")]
        public void GlobalConfig_Instance_ReturnsSameInstance()
        {
            var instance1 = AsakiPoolGlobalConfig.Instance;
            var instance2 = AsakiPoolGlobalConfig.Instance;

            Assert.AreSame(instance1, instance2);
        }
    }

    /// <summary>
    /// AsakiPoolConfig 配置类单元测试
    /// 测试配置对象的默认值、工厂方法和属性设置
    /// </summary>
    [TestFixture]
    public class AsakiPoolConfigTests
    {
        [SetUp]
        public void SetUp()
        {
            AsakiPoolGlobalConfig.Instance.ResetToDefaults();
        }

        [Test]
        [Category("Unit")]
        [Description("测试默认配置从全局配置获取初始值")]
        public void DefaultConfig_GetsValuesFromGlobalConfig()
        {
            var globalConfig = AsakiPoolGlobalConfig.Instance;
            globalConfig.DefaultInitialSize = 15;
            globalConfig.DefaultMaxSize = 150;

            var config = new AsakiPoolConfig();

            Assert.AreEqual(15, config.InitialSize);
            Assert.AreEqual(150, config.MaxSize);
        }

        [Test]
        [Category("Unit")]
        [Description("测试默认配置的初始值")]
        public void DefaultConfig_HasCorrectInitialValues()
        {
            var config = AsakiPoolConfig.Default;

            Assert.AreEqual(10, config.InitialSize, "默认初始大小应为10");
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
            var config = AsakiPoolConfig.ForGameObject(initialSize: 10, maxSize: 100);

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
            var globalConfig = AsakiPoolGlobalConfig.Instance;
            globalConfig.DefaultInitialSize = 25;
            globalConfig.DefaultMaxSize = 250;

            var config = AsakiPoolConfig.ForGameObject();

            Assert.AreEqual(25, config.InitialSize, "默认初始大小应从全局配置获取");
            Assert.AreEqual(250, config.MaxSize, "默认最大大小应从全局配置获取");
        }

        [Test]
        [Category("Unit")]
        [Description("测试轻量级对象配置工厂方法")]
        public void ForLightWeightObject_CreatesCorrectConfig()
        {
            var config = AsakiPoolConfig.ForLightWeightObject(maxSize: 1024);

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
            var globalConfig = AsakiPoolGlobalConfig.Instance;
            globalConfig.LightWeightPoolDefaultMaxSize = 2048;

            var config = AsakiPoolConfig.ForLightWeightObject();

            Assert.AreEqual(2048, config.MaxSize, "默认最大大小应从全局配置获取");
        }

        [Test]
        [Category("Unit")]
        [Description("测试自定义配置属性设置")]
        public void CustomConfig_AllowsPropertyModification()
        {
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
            var config = new AsakiPoolConfig();

            config.ShrinkRatio = 0f;
            Assert.AreEqual(0f, config.ShrinkRatio);

            config.ShrinkRatio = 1f;
            Assert.AreEqual(1f, config.ShrinkRatio);

            config.ShrinkRatio = 0.5f;
            Assert.AreEqual(0.5f, config.ShrinkRatio);
        }

        [Test]
        [Category("Unit")]
        [Description("测试配置对象的独立性")]
        public void ConfigInstances_AreIndependent()
        {
            var config1 = AsakiPoolConfig.ForGameObject(initialSize: 10, maxSize: 100);
            var config2 = AsakiPoolConfig.ForGameObject(initialSize: 20, maxSize: 200);

            Assert.AreNotSame(config1, config2, "不同配置应是不同实例");
            Assert.AreEqual(10, config1.InitialSize);
            Assert.AreEqual(20, config2.InitialSize);

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
            var config = AsakiPoolConfig.ForGameObject(initialSize, maxSize);

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
            var config = AsakiPoolConfig.ForLightWeightObject(maxSize);

            Assert.AreEqual(maxSize, config.MaxSize);
        }
    }
}
