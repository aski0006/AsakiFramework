// File: Assets/Asaki/Tests/Resources/AsakiResKitFactoryTests.cs
// AsakiResKitFactory 工厂类单元测试

using System;
using Asaki.Core.Resources;
using Asaki.Tests.Network;
using Asaki.Tests.Resources.Mocks;
using Asaki.Unity.Services.Resources;
using NUnit.Framework;
using MockEventService = Asaki.Tests.Resources.Mocks.MockEventService;

namespace Asaki.Tests.Resources
{
    /// <summary>
    /// AsakiResKitFactory 工厂类单元测试
    /// 测试不同模式下的服务创建
    /// </summary>
    [TestFixture]
    public class AsakiResKitFactoryTests
    {
        private MockAsyncService _mockAsyncService;
        private MockEventService _mockEventService;

        [SetUp]
        public void Setup()
        {
            _mockAsyncService = new MockAsyncService();
            _mockEventService = new MockEventService();
        }

        [TearDown]
        public void Teardown()
        {
            // 清理自定义策略注册
            AsakiResKitFactory.RegisterCustom(null, null);
            _mockAsyncService = null;
            _mockEventService = null;
        }

        #region Resources 模式测试

        [Test]
        [Category("Unit")]
        [Description("测试Resources模式创建服务")]
        public void Create_WithResourcesMode_ReturnsService()
        {
            // Act
            var service = AsakiResKitFactory.Create(
                AsakiResKitMode.Resources,
                _mockAsyncService,
                _mockEventService
            );

            // Assert
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<AsakiResourceService>(service);
        }

        [Test]
        [Category("Unit")]
        [Description("测试Resources模式创建的服务实现了接口")]
        public void Create_WithResourcesMode_ImplementsInterface()
        {
            // Act
            var service = AsakiResKitFactory.Create(
                AsakiResKitMode.Resources,
                _mockAsyncService,
                _mockEventService
            );

            // Assert
            Assert.IsInstanceOf<IAsakiResourceService>(service);
        }

        #endregion

        #region Addressables 模式测试

        [Test]
        [Category("Unit")]
        [Description("测试Addressables模式行为")]
        public void Create_WithAddressablesMode_HandlesCorrectly()
        {
            // 根据是否定义了ASAKI_USE_ADDRESSABLES宏，验证相应行为
#if ASAKI_USE_ADDRESSABLES
            // Act
            var service = AsakiResKitFactory.Create(
                AsakiResKitMode.Addressables,
                _mockAsyncService,
                _mockEventService
            );

            // Assert
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<AsakiResourceService>(service);
            Assert.IsInstanceOf<IAsakiResourceService>(service);
#else
            // Act & Assert - 未定义宏时应抛出异常
            Assert.Throws<NotSupportedException>(() =>
            {
                AsakiResKitFactory.Create(
                    AsakiResKitMode.Addressables,
                    _mockAsyncService,
                    _mockEventService
                );
            });
#endif
        }

        #endregion

        #region Custom 模式测试

        [Test]
        [Category("Unit")]
        [Description("测试Custom模式使用已注册的策略")]
        public void Create_WithCustomMode_UsesRegisteredStrategy()
        {
            // Arrange
            var customStrategy = new MockAsakiResStrategy();
            var customLookup = new MockAsakiResDependencyLookup();

            AsakiResKitFactory.RegisterCustom(() => customStrategy, () => customLookup);

            // Act
            var service = AsakiResKitFactory.Create(
                AsakiResKitMode.Custom,
                _mockAsyncService,
                _mockEventService
            );

            // Assert
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<AsakiResourceService>(service);
        }

        [Test]
        [Category("Unit")]
        [Description("测试Custom模式未注册策略时抛出异常")]
        public void Create_WithCustomMode_NoStrategyRegistered_ThrowsInvalidOperationException()
        {
            // Arrange - 确保没有注册自定义策略
            AsakiResKitFactory.RegisterCustom(null, null);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                AsakiResKitFactory.Create(
                    AsakiResKitMode.Custom,
                    _mockAsyncService,
                    _mockEventService
                );
            });

            StringAssert.Contains("Custom mode", ex.Message);
        }

        [Test]
        [Category("Unit")]
        [Description("测试Custom模式使用默认Lookup当未提供时")]
        public void Create_WithCustomMode_NoLookup_UsesDefaultLookup()
        {
            // Arrange
            var customStrategy = new MockAsakiResStrategy();

            AsakiResKitFactory.RegisterCustom(() => customStrategy, null);

            // Act
            var service = AsakiResKitFactory.Create(
                AsakiResKitMode.Custom,
                _mockAsyncService,
                _mockEventService
            );

            // Assert - 服务应该成功创建
            Assert.IsNotNull(service);
        }

        [Test]
        [Category("Unit")]
        [Description("测试注册自定义策略")]
        public void RegisterCustom_SetsCustomStrategy()
        {
            // Arrange
            var customStrategy = new MockAsakiResStrategy();
            bool strategyCreated = false;

            AsakiResKitFactory.RegisterCustom(() =>
            {
                strategyCreated = true;
                return customStrategy;
            });

            // Act
            AsakiResKitFactory.Create(AsakiResKitMode.Custom, _mockAsyncService, _mockEventService);

            // Assert
            Assert.IsTrue(strategyCreated);
        }

        #endregion

        #region 异常处理测试

        [Test]
        [Category("Unit")]
        [Description("测试传入null异步服务抛出ArgumentNullException")]
        public void Create_WithNullAsyncService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                AsakiResKitFactory.Create(AsakiResKitMode.Resources, null, _mockEventService);
            });

            StringAssert.Contains("asyncService", ex.ParamName);
        }

        [Test]
        [Category("Unit")]
        [Description("测试无效模式抛出ArgumentOutOfRangeException")]
        public void Create_WithInvalidMode_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                AsakiResKitFactory.Create(
                    (AsakiResKitMode)999,
                    _mockAsyncService,
                    _mockEventService
                );
            });
        }

        #endregion

        #region 多模式切换测试

        [Test]
        [Category("Unit")]
        [Description("测试可以创建多个不同模式的服务")]
        public void Create_MultipleModes_CreatesDifferentServices()
        {
            // Act
            var resourcesService = AsakiResKitFactory.Create(
                AsakiResKitMode.Resources,
                _mockAsyncService,
                _mockEventService
            );

            // Assert
            Assert.IsNotNull(resourcesService);

            // 清理并注册自定义策略
            var customStrategy = new MockAsakiResStrategy();
            AsakiResKitFactory.RegisterCustom(() => customStrategy);

            var customService = AsakiResKitFactory.Create(
                AsakiResKitMode.Custom,
                _mockAsyncService,
                _mockEventService
            );

            Assert.IsNotNull(customService);
            Assert.AreNotSame(resourcesService, customService);
        }

        [Test]
        [Category("Unit")]
        [Description("测试重复注册自定义策略覆盖之前的")]
        public void RegisterCustom_OverwritesPreviousStrategy()
        {
            // Arrange
            var firstStrategy = new MockAsakiResStrategy();
            var secondStrategy = new MockAsakiResStrategy();
            IAsakiResStrategy capturedStrategy = null;

            AsakiResKitFactory.RegisterCustom(() => firstStrategy);
            AsakiResKitFactory.RegisterCustom(() =>
            {
                capturedStrategy = secondStrategy;
                return secondStrategy;
            });

            // Act
            AsakiResKitFactory.Create(AsakiResKitMode.Custom, _mockAsyncService, _mockEventService);

            // Assert
            Assert.AreSame(secondStrategy, capturedStrategy);
        }

        #endregion
    }
}
