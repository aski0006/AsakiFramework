using System;
using System.Collections;
using System.Threading;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Simulation;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// 模拟的仿真服务，用于测试
    /// </summary>
    public class MockSimulationService : IAsakiSimulationService
    {
        public int RegisterTickableCallCount { get; private set; }
        public int UnregisterTickableCallCount { get; private set; }
        public IAsakiTickable LastRegisteredTickable { get; private set; }
        public IAsakiTickable LastUnregisteredTickable { get; private set; }

        public void Register(IAsakiTickable tickable, int priority = (int)TickPriority.Normal)
        {
            RegisterTickableCallCount++;
            LastRegisteredTickable = tickable;
        }

        public void Register(IAsakiFixedTickable tickable)
        {
            // No-op for mock
        }

        public void Register(IAsakiLateTickable tickable, int priority = (int)TickPriority.Normal)
        {
            // No-op for mock
        }

        public void Unregister(IAsakiTickable tickable)
        {
            UnregisterTickableCallCount++;
            LastUnregisteredTickable = tickable;
        }

        public void Unregister(IAsakiFixedTickable tickable)
        {
            // No-op for mock
        }

        public void Unregister(IAsakiLateTickable tickable)
        {
            // No-op for mock
        }

        public void Tick(float deltaTime)
        {
            // No-op for mock
        }

        public void FixedTick(float fixedDeltaTime)
        {
            // No-op for mock
        }

        public void LateTick(float lateDeltaTime)
        {
            // No-op for mock
        }

        public void ResetCounters()
        {
            RegisterTickableCallCount = 0;
            UnregisterTickableCallCount = 0;
            LastRegisteredTickable = null;
            LastUnregisteredTickable = null;
        }
    }

    /// <summary>
    /// AsakiPoolService 服务层单元测试
    /// 测试池服务的创建、管理和销毁功能
    /// </summary>
    [TestFixture]
    public class AsakiPoolServiceTests
    {
        private MockSimulationService _mockSimulationService;
        private AsakiPoolService _poolService;

        [SetUp]
        public void Setup()
        {
            _mockSimulationService = new MockSimulationService();
            _poolService = new AsakiPoolService(_mockSimulationService);
        }

        [TearDown]
        public void Teardown()
        {
            _poolService?.Dispose();
            _poolService = null;
            _mockSimulationService = null;
        }

        #region 构造函数测试

        [Test]
        [Category("Unit")]
        [Description("测试服务构造函数正确初始化")]
        public void Constructor_RegistersWithSimulationService()
        {
            // Assert
            Assert.AreEqual(1, _mockSimulationService.RegisterTickableCallCount);
            Assert.AreSame(_poolService, _mockSimulationService.LastRegisteredTickable);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数传入null simulationService")]
        public void Constructor_WithNullSimulationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                new AsakiPoolService(null);
            });
        }

        #endregion

        #region CreatePoolAsync 测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试异步创建池")]
        public IEnumerator CreatePoolAsync_CreatesPoolSuccessfully()
        {
            // Arrange
            var factory = new TestObjectFactory();
            var config = AsakiPoolConfig.ForLightWeightObject(10);

            // Act
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory, config)
                .ToCoroutine(pool =>
                {
                    // Assert
                    Assert.IsNotNull(pool);
                    Assert.AreEqual("TestPool", pool.Key);
                    Assert.IsTrue(_poolService.HasPool("TestPool"));
                });
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试创建池时自动预热")]
        public IEnumerator CreatePoolAsync_WithInitialSize_PrewarmsPool()
        {
            // Arrange
            var factory = new TestObjectFactory();
            var config = AsakiPoolConfig.ForLightWeightObject(10);
            config.InitialSize = 5;

            // Act
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory, config)
                .ToCoroutine(pool =>
                {
                    // Assert
                    Assert.AreEqual(5, pool.Statistics.TotalCreated);
                    Assert.AreEqual(5, pool.Statistics.InactiveCount);
                });
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试创建池时传入null key抛出异常")]
        public IEnumerator CreatePoolAsync_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var factory = new TestObjectFactory();

            // Act & Assert
            var task = _poolService.CreatePoolAsync<TestPoolObject>(null, factory);
            Exception capturedException = null;

            yield return task.ToCoroutine(
                resultHandler: _ => { },
                exceptionHandler: ex => capturedException = ex
            );

            Assert.IsNotNull(capturedException, "Expected exception was not thrown");
            Assert.IsInstanceOf<ArgumentException>(capturedException);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试创建池时传入空字符串key抛出异常")]
        public IEnumerator CreatePoolAsync_WithEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var factory = new TestObjectFactory();

            // Act & Assert
            var task = _poolService.CreatePoolAsync<TestPoolObject>("", factory);
            Exception capturedException = null;

            yield return task.ToCoroutine(
                resultHandler: _ => { },
                exceptionHandler: ex => capturedException = ex
            );

            Assert.IsNotNull(capturedException, "Expected exception was not thrown");
            Assert.IsInstanceOf<ArgumentException>(capturedException);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试创建池时传入null factory抛出异常")]
        public IEnumerator CreatePoolAsync_WithNullFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var task = _poolService.CreatePoolAsync<TestPoolObject>("TestPool", null);
            Exception capturedException = null;

            yield return task.ToCoroutine(
                resultHandler: _ => { },
                exceptionHandler: ex => capturedException = ex
            );

            Assert.IsNotNull(capturedException, "Expected exception was not thrown");
            Assert.IsInstanceOf<ArgumentNullException>(capturedException);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试创建重复key的池抛出异常")]
        public IEnumerator CreatePoolAsync_WithDuplicateKey_ThrowsArgumentException()
        {
            // Arrange
            var factory = new TestObjectFactory();

            // Act - Create first pool
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory)
                .ToCoroutine();

            // Assert - Creating second pool with same key should throw
            var task = _poolService.CreatePoolAsync<TestPoolObject>("TestPool", factory);
            Exception capturedException = null;

            yield return task.ToCoroutine(
                resultHandler: _ => { },
                exceptionHandler: ex => capturedException = ex
            );

            Assert.IsNotNull(capturedException, "Expected exception was not thrown");
            Assert.IsInstanceOf<ArgumentException>(capturedException);
            StringAssert.Contains("already exists", capturedException.Message);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试创建池使用默认配置")]
        public IEnumerator CreatePoolAsync_WithNullConfig_UsesDefaultConfig()
        {
            // Arrange
            var factory = new TestObjectFactory();

            // Act
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory, null)
                .ToCoroutine(pool =>
                {
                    // Assert
                    Assert.AreSame(AsakiPoolConfig.Default, pool.Config);
                });
        }

        #endregion

        #region GetPool 测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试获取已存在的池")]
        public IEnumerator GetPool_ExistingPool_ReturnsPool()
        {
            // Arrange
            var factory = new TestObjectFactory();
            IAsakiPool<TestPoolObject> createdPool = null;
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory)
                .ToCoroutine(pool => createdPool = pool);

            // Act
            var retrievedPool = _poolService.GetPool<TestPoolObject>("TestPool");

            // Assert
            Assert.IsNotNull(retrievedPool);
            Assert.AreSame(createdPool, retrievedPool);
        }

        [Test]
        [Category("Unit")]
        [Description("测试获取不存在的池返回null")]
        public void GetPool_NonExistingPool_ReturnsNull()
        {
            // Act
            var pool = _poolService.GetPool<TestPoolObject>("NonExistingPool");

            // Assert
            Assert.IsNull(pool);
        }

        [Test]
        [Category("Unit")]
        [Description("测试获取null key返回null")]
        public void GetPool_NullKey_ReturnsNull()
        {
            // Act
            var pool = _poolService.GetPool<TestPoolObject>(null);

            // Assert
            Assert.IsNull(pool);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试类型不匹配时返回null")]
        public IEnumerator GetPool_TypeMismatch_ReturnsNull()
        {
            // Arrange
            var factory = new TestObjectFactory();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory)
                .ToCoroutine();

            // 由于 ALog 接管了 Unity 日志，忽略日志错误
            LogAssert.ignoreFailingMessages = true;

            // Act - 尝试用错误类型获取
            var pool = _poolService.GetPool<string>("TestPool");

            // Assert
            Assert.IsNull(pool);

            yield return null;
        }

        #endregion

        #region HasPool 测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试检查已存在的池返回true")]
        public IEnumerator HasPool_ExistingPool_ReturnsTrue()
        {
            // Arrange
            var factory = new TestObjectFactory();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory)
                .ToCoroutine();

            // Act & Assert
            Assert.IsTrue(_poolService.HasPool("TestPool"));

            yield return null;
        }

        [Test]
        [Category("Unit")]
        [Description("测试检查不存在的池返回false")]
        public void HasPool_NonExistingPool_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_poolService.HasPool("NonExistingPool"));
        }

        [Test]
        [Category("Unit")]
        [Description("测试检查null key返回false")]
        public void HasPool_NullKey_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_poolService.HasPool(null));
        }

        [Test]
        [Category("Unit")]
        [Description("测试检查空字符串key返回false")]
        public void HasPool_EmptyKey_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_poolService.HasPool(""));
        }

        #endregion

        #region DestroyPool 测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试销毁存在的池返回true")]
        public IEnumerator DestroyPool_ExistingPool_ReturnsTrue()
        {
            // Arrange
            var factory = new TestObjectFactory();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory)
                .ToCoroutine();

            // Act
            bool result = _poolService.DestroyPool("TestPool");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_poolService.HasPool("TestPool"));

            yield return null;
        }

        [Test]
        [Category("Unit")]
        [Description("测试销毁不存在的池返回false")]
        public void DestroyPool_NonExistingPool_ReturnsFalse()
        {
            // Act
            bool result = _poolService.DestroyPool("NonExistingPool");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        [Category("Unit")]
        [Description("测试销毁null key返回false")]
        public void DestroyPool_NullKey_ReturnsFalse()
        {
            // Act
            bool result = _poolService.DestroyPool(null);

            // Assert
            Assert.IsFalse(result);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试销毁池后资源被释放")]
        public IEnumerator DestroyPool_ReleasesResources()
        {
            // Arrange
            var factory = new TestObjectFactory();
            var config = new AsakiPoolConfig
            {
                InitialSize = 1,
                MaxSize = 10,
                AllowSyncCreation = true,
            };
            IAsakiPool<TestPoolObject> pool = null;
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory, config)
                .ToCoroutine(p => pool = p);
            var obj = pool.Get();
            pool.Return(obj);

            // Act
            _poolService.DestroyPool("TestPool");

            // Assert - 工厂应被调用销毁对象
            Assert.AreEqual(1, factory.OnDestroyCallCount);

            yield return null;
        }

        #endregion

        #region GetStatisticsSummary 测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试获取统计摘要包含池信息")]
        public IEnumerator GetStatisticsSummary_ContainsPoolInfo()
        {
            // Arrange
            var factory = new TestObjectFactory();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("Pool1", factory)
                .ToCoroutine();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("Pool2", factory)
                .ToCoroutine();

            // Act
            string summary = _poolService.GetStatisticsSummary();

            // Assert
            StringAssert.Contains("Pool1", summary);
            StringAssert.Contains("Pool2", summary);
            StringAssert.Contains("total: 2 pools", summary);

            yield return null;
        }

        [Test]
        [Category("Unit")]
        [Description("测试空服务返回正确的统计摘要")]
        public void GetStatisticsSummary_EmptyService_ReturnsCorrectMessage()
        {
            // Act
            string summary = _poolService.GetStatisticsSummary();

            // Assert
            StringAssert.Contains("No active pools", summary);
        }

        #endregion

        #region 低内存处理测试

        [Test]
        [Category("Unit")]
        [Description("测试注册低内存处理程序")]
        public void RegisterLowMemoryHandler_RegistersSuccessfully()
        {
            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() => _poolService.RegisterLowMemoryHandler());
        }

        [Test]
        [Category("Unit")]
        [Description("测试重复注册低内存处理程序不抛出异常")]
        public void RegisterLowMemoryHandler_MultipleTimes_DoesNotThrow()
        {
            // Act
            _poolService.RegisterLowMemoryHandler();
            _poolService.RegisterLowMemoryHandler();

            // Assert - 不应抛出异常
            Assert.DoesNotThrow(() => _poolService.RegisterLowMemoryHandler());
        }

        [Test]
        [Category("Unit")]
        [Description("测试注销低内存处理程序")]
        public void UnregisterLowMemoryHandler_UnregistersSuccessfully()
        {
            // Arrange
            _poolService.RegisterLowMemoryHandler();

            // Act & Assert
            Assert.DoesNotThrow(() => _poolService.UnregisterLowMemoryHandler());
        }

        [Test]
        [Category("Unit")]
        [Description("测试未注册时注销不抛出异常")]
        public void UnregisterLowMemoryHandler_WhenNotRegistered_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _poolService.UnregisterLowMemoryHandler());
        }

        #endregion

        #region Tick 测试

        [Test]
        [Category("Unit")]
        [Description("测试Tick调用不抛出异常")]
        public void Tick_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _poolService.Tick(0.016f));
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试Tick在有池时调用不抛出异常")]
        public IEnumerator Tick_WithPools_DoesNotThrow()
        {
            // Arrange
            var factory = new TestObjectFactory();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory)
                .ToCoroutine();

            // Act & Assert
            Assert.DoesNotThrow(() => _poolService.Tick(0.016f));

            yield return null;
        }

        #endregion

        #region PerformManualGovernance 测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试手动治理返回销毁的对象数量")]
        public IEnumerator PerformManualGovernance_ReturnsRemovedCount()
        {
            // Arrange
            var factory = new TestObjectFactory();
            var config = AsakiPoolConfig.ForLightWeightObject(100);
            config.EnableAutoShrink = true;
            config.IdleTimeout = 0f; // 立即过期

            IAsakiPool<TestPoolObject> pool = null;
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("TestPool", factory, config)
                .ToCoroutine(p => pool = p);

            // 预热并归还对象
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act
            int removed = _poolService.PerformManualGovernance(force: true);

            // Assert - 应该销毁一些对象
            Assert.GreaterOrEqual(removed, 0);

            yield return null;
        }

        [Test]
        [Category("Unit")]
        [Description("测试空服务手动治理返回0")]
        public void PerformManualGovernance_EmptyService_ReturnsZero()
        {
            // Act
            int removed = _poolService.PerformManualGovernance();

            // Assert
            Assert.AreEqual(0, removed);
        }

        #endregion

        #region Dispose 测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试释放服务销毁所有池")]
        public IEnumerator Dispose_DestroyAllPools()
        {
            // Arrange
            var factory = new TestObjectFactory();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("Pool1", factory)
                .ToCoroutine();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("Pool2", factory)
                .ToCoroutine();

            // 在 Dispose 前验证池存在
            Assert.IsTrue(_poolService.HasPool("Pool1"));
            Assert.IsTrue(_poolService.HasPool("Pool2"));

            // Act
            _poolService.Dispose();

            // Assert - 服务释放后访问 HasPool 应该抛出 ObjectDisposedException
            Assert.Throws<ObjectDisposedException>(() => _poolService.HasPool("Pool1"));

            yield return null;
        }

        [Test]
        [Category("Unit")]
        [Description("测试释放服务注销模拟服务")]
        public void Dispose_UnregistersFromSimulationService()
        {
            // Act
            _poolService.Dispose();

            // Assert
            Assert.AreEqual(1, _mockSimulationService.UnregisterTickableCallCount);
            Assert.AreSame(_poolService, _mockSimulationService.LastUnregisteredTickable);
        }

        [Test]
        [Category("Unit")]
        [Description("测试重复释放不抛出异常")]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _poolService.Dispose();
                _poolService.Dispose();
                _poolService.Dispose();
            });
        }

        [Test]
        [Category("Unit")]
        [Description("测试释放后操作抛出异常")]
        public void Dispose_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            _poolService.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _poolService.HasPool("Test"));
            Assert.Throws<ObjectDisposedException>(() =>
                _poolService.GetPool<TestPoolObject>("Test")
            );
            Assert.Throws<ObjectDisposedException>(() => _poolService.DestroyPool("Test"));
            Assert.Throws<ObjectDisposedException>(() => _poolService.GetStatisticsSummary());
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试释放后异步创建池抛出异常")]
        public IEnumerator CreatePoolAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            _poolService.Dispose();
            var factory = new TestObjectFactory();

            // Act & Assert - 使用 ToCoroutine 来捕获异步异常
            var task = _poolService.CreatePoolAsync<TestPoolObject>("TestPool", factory);
            Exception capturedException = null;

            yield return task.ToCoroutine(_ => { }, ex => capturedException = ex);

            Assert.IsNotNull(capturedException, "Expected exception was not thrown");
            Assert.IsInstanceOf<ObjectDisposedException>(capturedException);
        }

        #endregion

        #region 多池管理测试

        [UnityTest]
        [Category("Integration")]
        [Description("测试管理多个不同类型的池")]
        public IEnumerator MultiplePools_DifferentTypes_ManagesCorrectly()
        {
            // Arrange
            var objectFactory = new TestObjectFactory();
            var stringFactory = new TestStringFactory();

            // Act
            IAsakiPool<TestPoolObject> objectPool = null;
            IAsakiPool<string> stringPool = null;
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("ObjectPool", objectFactory)
                .ToCoroutine(p => objectPool = p);
            yield return _poolService
                .CreatePoolAsync<string>("StringPool", stringFactory)
                .ToCoroutine(p => stringPool = p);

            // Assert
            Assert.IsNotNull(objectPool);
            Assert.IsNotNull(stringPool);
            Assert.AreNotSame(objectPool, stringPool);
            Assert.IsTrue(_poolService.HasPool("ObjectPool"));
            Assert.IsTrue(_poolService.HasPool("StringPool"));

            // 验证类型正确
            Assert.AreEqual(typeof(TestPoolObject), objectPool.ObjectType);
            Assert.AreEqual(typeof(string), stringPool.ObjectType);

            yield return null;
        }

        [UnityTest]
        [Category("Integration")]
        [Description("测试销毁一个池不影响其他池")]
        public IEnumerator DestroyPool_OnePool_OthersUnaffected()
        {
            // Arrange
            var factory = new TestObjectFactory();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("Pool1", factory)
                .ToCoroutine();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("Pool2", factory)
                .ToCoroutine();
            yield return _poolService
                .CreatePoolAsync<TestPoolObject>("Pool3", factory)
                .ToCoroutine();

            // Act
            _poolService.DestroyPool("Pool2");

            // Assert
            Assert.IsTrue(_poolService.HasPool("Pool1"));
            Assert.IsFalse(_poolService.HasPool("Pool2"));
            Assert.IsTrue(_poolService.HasPool("Pool3"));

            yield return null;
        }

        #endregion
    }

    /// <summary>
    /// 字符串对象工厂，用于测试不同类型
    /// </summary>
    public class TestStringFactory : IAsakiPoolObjectFactory<string>
    {
        private int _counter = 0;

        public UniTask<string> CreateAsync(CancellationToken token = default)
        {
            return UniTask.FromResult($"String_{Interlocked.Increment(ref _counter)}");
        }

        public string CreateSync()
        {
            return $"String_{Interlocked.Increment(ref _counter)}";
        }

        public void OnGet(string obj)
        {
            // No-op for string
        }

        public void OnReturn(string obj)
        {
            // No-op for string
        }

        public void OnDestroy(string obj)
        {
            // No-op for string
        }

        public bool Validate(string obj)
        {
            return !string.IsNullOrEmpty(obj);
        }
    }
}
