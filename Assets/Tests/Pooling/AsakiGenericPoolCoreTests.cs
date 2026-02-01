using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// 测试用的简单对象
    /// </summary>
    public class TestPoolObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int OnGetCallCount { get; set; }
        public int OnReturnCallCount { get; set; }
        public int OnDestroyCallCount { get; set; }

        public TestPoolObject()
        {
            Id = 0;
            Name = string.Empty;
            IsActive = false;
        }
    }

    /// <summary>
    /// 测试用的对象工厂
    /// </summary>
    public class TestObjectFactory : IAsakiPoolObjectFactory<TestPoolObject>
    {
        private int _counter = 0;
        private readonly bool _validateResult;
        private readonly bool _throwOnCreate;

        public int CreateCallCount { get; private set; }
        public int OnGetCallCount { get; private set; }
        public int OnReturnCallCount { get; private set; }
        public int OnDestroyCallCount { get; private set; }

        public TestObjectFactory(bool validateResult = true, bool throwOnCreate = false)
        {
            _validateResult = validateResult;
            _throwOnCreate = throwOnCreate;
        }

        public UniTask<TestPoolObject> CreateAsync(CancellationToken token = default)
        {
            if (_throwOnCreate)
            {
                throw new InvalidOperationException("Simulated creation failure");
            }

            CreateCallCount++;
            var obj = new TestPoolObject
            {
                Id = Interlocked.Increment(ref _counter), Name = $"Object_{_counter}",
            };
            return UniTask.FromResult(obj);
        }

        public TestPoolObject CreateSync()
        {
            if (_throwOnCreate)
            {
                throw new InvalidOperationException("Simulated creation failure");
            }

            CreateCallCount++;
            return new TestPoolObject
            {
                Id = Interlocked.Increment(ref _counter), Name = $"Object_{_counter}",
            };
        }

        public void OnGet(TestPoolObject obj)
        {
            OnGetCallCount++;
            if (obj != null)
            {
                obj.IsActive = true;
                obj.OnGetCallCount++;
            }
        }

        public void OnReturn(TestPoolObject obj)
        {
            OnReturnCallCount++;
            if (obj != null)
            {
                obj.IsActive = false;
                obj.OnReturnCallCount++;
            }
        }

        public void OnDestroy(TestPoolObject obj)
        {
            OnDestroyCallCount++;
            if (obj != null)
            {
                obj.OnDestroyCallCount++;
            }
        }

        public bool Validate(TestPoolObject obj)
        {
            return obj != null && _validateResult;
        }

        public void ResetCounters()
        {
            CreateCallCount = 0;
            OnGetCallCount = 0;
            OnReturnCallCount = 0;
            OnDestroyCallCount = 0;
        }
    }

    /// <summary>
    /// AsakiGenericPool 核心功能单元测试
    /// 测试对象池的基本操作：创建、获取、归还、销毁
    /// </summary>
    [TestFixture]
    public class AsakiGenericPoolCoreTests
    {
        private TestObjectFactory _factory;
        private AsakiPoolConfig _config;

        [SetUp]
        public void Setup()
        {
            _factory = new TestObjectFactory();
            _config = new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = 10,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = true,
            };
        }

        [TearDown]
        public void Teardown()
        {
            _factory = null;
            _config = null;
        }

        #region 构造函数测试

        [Test]
        [Category("Unit")]
        [Description("测试池构造函数正确初始化")]
        public void Constructor_InitializesCorrectly()
        {
            // Act
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Assert
            Assert.AreEqual("TestPool", pool.Key);
            Assert.AreSame(_config, pool.Config);
            Assert.AreEqual(typeof(TestPoolObject), pool.ObjectType);
            Assert.IsNotNull(pool.Statistics);
            Assert.AreEqual(0, pool.Statistics.TotalCreated);
            Assert.AreEqual(0, pool.Statistics.InactiveCount);
            Assert.AreEqual(0, pool.Statistics.ActiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试池构造函数使用默认配置")]
        public void Constructor_WithNullConfig_UsesDefaultConfig()
        {
            // Act
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, null);

            // Assert
            Assert.IsNotNull(pool.Config);
            Assert.AreSame(AsakiPoolConfig.Default, pool.Config);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数传入null key抛出异常")]
        public void Constructor_WithNullKey_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                new AsakiGenericPool<TestPoolObject>(null, _factory, _config);
            });
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数传入null factory抛出异常")]
        public void Constructor_WithNullFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                new AsakiGenericPool<TestPoolObject>("TestPool", null, _config);
            });
        }

        #endregion

        #region Get 同步获取测试

        [Test]
        [Category("Unit")]
        [Description("测试同步获取对象 - 池为空时创建新对象")]
        public void Get_WhenPoolEmpty_CreatesNewObject()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            var obj = pool.Get();

            // Assert
            Assert.IsNotNull(obj);
            Assert.AreEqual(1, obj.Id);
            Assert.IsTrue(obj.IsActive);
            Assert.AreEqual(1, _factory.CreateCallCount);
            Assert.AreEqual(1, _factory.OnGetCallCount);
            Assert.AreEqual(1, pool.Statistics.TotalCreated);
            Assert.AreEqual(1, pool.Statistics.ActiveCount);
            Assert.AreEqual(0, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试同步获取对象 - 从池中获取现有对象")]
        public void Get_WhenPoolHasObject_ReturnsPooledObject()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj1 = pool.Get();
            pool.Return(obj1);
            _factory.ResetCounters();

            // Act
            var obj2 = pool.Get();

            // Assert
            Assert.IsNotNull(obj2);
            Assert.AreSame(obj1, obj2, "应返回同一个对象");
            Assert.AreEqual(0, _factory.CreateCallCount, "不应创建新对象");
            Assert.AreEqual(1, _factory.OnGetCallCount, "应调用 OnGet");
            Assert.AreEqual(1, pool.Statistics.TotalCreated, "总创建数应保持1");
            Assert.AreEqual(1, pool.Statistics.ActiveCount);
            Assert.AreEqual(0, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试同步获取对象 - 禁用同步创建时返回null")]
        public void Get_WhenSyncCreationDisabledAndPoolEmpty_ReturnsNull()
        {
            // Arrange
            _config.AllowSyncCreation = false;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            var obj = pool.Get();

            // Assert
            Assert.IsNull(obj);
            Assert.AreEqual(0, _factory.CreateCallCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试同步获取对象 - 触发回调")]
        public void Get_TriggersOnGetCallback()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            var obj = pool.Get();

            // Assert
            Assert.AreEqual(1, obj.OnGetCallCount);
            Assert.IsTrue(obj.IsActive);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试多次获取创建多个对象")]
        public void Get_MultipleTimes_CreatesMultipleObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            var obj3 = pool.Get();

            // Assert
            Assert.AreNotSame(obj1, obj2);
            Assert.AreNotSame(obj2, obj3);
            Assert.AreEqual(3, _factory.CreateCallCount);
            Assert.AreEqual(3, pool.Statistics.TotalCreated);
            Assert.AreEqual(3, pool.Statistics.ActiveCount);

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region GetAsync 异步获取测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试异步获取对象 - 池为空时创建新对象")]
        public IEnumerator GetAsync_WhenPoolEmpty_CreatesNewObject()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            TestPoolObject obj = null;

            // Act
            yield return pool.GetAsync().ContinueWith(result => obj = result).ToCoroutine();

            // Assert
            Assert.IsNotNull(obj);
            Assert.AreEqual(1, _factory.CreateCallCount);
            Assert.AreEqual(1, pool.Statistics.ActiveCount);

            // Cleanup
            pool.Return(obj);
            pool.Dispose();
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试异步获取对象 - 从池中获取现有对象")]
        public IEnumerator GetAsync_WhenPoolHasObject_ReturnsPooledObject()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            TestPoolObject obj1 = null;
            yield return pool.GetAsync().ContinueWith(result => obj1 = result).ToCoroutine();
            pool.Return(obj1);
            _factory.ResetCounters();

            // Act
            TestPoolObject obj2 = null;
            yield return pool.GetAsync().ContinueWith(result => obj2 = result).ToCoroutine();

            // Assert
            Assert.AreSame(obj1, obj2);
            Assert.AreEqual(0, _factory.CreateCallCount);

            // Cleanup
            pool.Return(obj2);
            pool.Dispose();
        }

        #endregion

        #region Return 归还测试

        [Test]
        [Category("Unit")]
        [Description("测试归还对象到池")]
        public void Return_ValidObject_ReturnsTrue()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj = pool.Get();

            // Act
            bool result = pool.Return(obj);

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(obj.IsActive);
            Assert.AreEqual(1, _factory.OnReturnCallCount);
            Assert.AreEqual(0, pool.Statistics.ActiveCount);
            Assert.AreEqual(1, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试归还null对象返回false")]
        public void Return_NullObject_ReturnsFalse()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            bool result = pool.Return(null);

            // Assert
            Assert.IsFalse(result);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试归还非池对象返回false")]
        public void Return_ObjectNotFromPool_ReturnsFalse()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            TestPoolObject externalObj = new TestPoolObject
            {
                Id = 999
            };

            // Act
            bool result = pool.Return(externalObj);

            // Assert
            Assert.IsFalse(result);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试重复归还原对象返回false")]
        public void Return_SameObjectTwice_ReturnsFalseOnSecond()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj = pool.Get();
            pool.Return(obj);

            // Act
            bool secondReturn = pool.Return(obj);

            // Assert
            Assert.IsFalse(secondReturn);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试归还对象触发回调")]
        public void Return_TriggersOnReturnCallback()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj = pool.Get();

            // Act
            pool.Return(obj);

            // Assert
            Assert.AreEqual(1, obj.OnReturnCallCount);
            Assert.IsFalse(obj.IsActive);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试池满时归还对象会销毁对象")]
        public void Return_WhenPoolFull_DestroysObject()
        {
            // Arrange
            _config.MaxSize = 2;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建3个对象（都保持活动状态）
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            var obj3 = pool.Get();

            // 归还2个对象填满池
            pool.Return(obj1);
            pool.Return(obj2);

            _factory.ResetCounters();

            // Act - 此时池已满（2个非活动对象），归还第3个对象应销毁
            bool result = pool.Return(obj3);

            // Assert
            Assert.IsFalse(result, "池满时归还应返回false");
            Assert.AreEqual(1, _factory.OnDestroyCallCount, "应销毁对象");
            Assert.AreEqual(1, pool.Statistics.TotalDestroyed, "销毁计数应为1");

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region PrewarmAsync 预热测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试异步预热创建指定数量的对象")]
        public IEnumerator PrewarmAsync_CreatesSpecifiedObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            yield return pool.PrewarmAsync(5).ToCoroutine();

            // Assert
            Assert.AreEqual(5, _factory.CreateCallCount);
            Assert.AreEqual(5, pool.Statistics.TotalCreated);
            Assert.AreEqual(5, pool.Statistics.InactiveCount);
            Assert.AreEqual(0, pool.Statistics.ActiveCount);

            // Cleanup
            pool.Dispose();
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试预热0个对象不执行任何操作")]
        public IEnumerator PrewarmAsync_WithZeroCount_DoesNothing()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            yield return pool.PrewarmAsync(0).ToCoroutine();

            // Assert
            Assert.AreEqual(0, _factory.CreateCallCount);

            // Cleanup
            pool.Dispose();
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试预热负数对象不执行任何操作")]
        public IEnumerator PrewarmAsync_WithNegativeCount_DoesNothing()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            yield return pool.PrewarmAsync(-5).ToCoroutine();

            // Assert
            Assert.AreEqual(0, _factory.CreateCallCount);

            // Cleanup
            pool.Dispose();
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试预热后的对象可以被获取")]
        public IEnumerator PrewarmAsync_ObjectsCanBeRetrieved()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            yield return pool.PrewarmAsync(3).ToCoroutine();
            _factory.ResetCounters();

            // Act
            var obj = pool.Get();

            // Assert
            Assert.IsNotNull(obj);
            Assert.AreEqual(0, _factory.CreateCallCount, "不应创建新对象");
            Assert.AreEqual(
                2,
                pool.Statistics.InactiveCount,
                "预热3个，获取1个，应剩余2个非活动对象"
            );

            // Cleanup
            pool.Return(obj);
            pool.Dispose();
        }

        #endregion

        #region Clear 清空测试

        [Test]
        [Category("Unit")]
        [Description("测试清空池销毁所有非活动对象")]
        public void Clear_DestroysAllInactiveObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            pool.Return(obj1);
            pool.Return(obj2);
            _factory.ResetCounters();

            // Act
            pool.Clear();

            // Assert
            Assert.AreEqual(2, _factory.OnDestroyCallCount);
            Assert.AreEqual(2, pool.Statistics.TotalDestroyed);
            Assert.AreEqual(0, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试清空空池不抛出异常")]
        public void Clear_WhenPoolEmpty_DoesNotThrow()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act & Assert
            Assert.DoesNotThrow(() => pool.Clear());
            Assert.AreEqual(0, pool.Statistics.TotalDestroyed);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试清空后活动对象不受影响")]
        public void Clear_DoesNotDestroyActiveObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var activeObj = pool.Get();
            var inactiveObj = pool.Get();
            pool.Return(inactiveObj);
            _factory.ResetCounters();

            // Act
            pool.Clear();

            // Assert
            Assert.AreEqual(1, _factory.OnDestroyCallCount, "只应销毁非活动对象");
            Assert.IsNotNull(activeObj);
            Assert.AreEqual(1, pool.Statistics.ActiveCount);

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region Shrink 收缩测试

        [Test]
        [Category("Unit")]
        [Description("测试收缩到指定大小")]
        public void Shrink_ToTargetSize_RemovesExcessObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            // 同时获取5个对象，然后逐一归还，确保池中有5个不同的对象
            var objs = new TestPoolObject[5];
            for (int i = 0; i < 5; i++)
            {
                objs[i] = pool.Get();
            }
            for (int i = 0; i < 5; i++)
            {
                pool.Return(objs[i]);
            }
            _factory.ResetCounters();

            // Act
            pool.Shrink(2);

            // Assert
            Assert.AreEqual(3, _factory.OnDestroyCallCount, "应销毁3个对象");
            Assert.AreEqual(3, pool.Statistics.TotalDestroyed, "销毁计数应为3");
            Assert.AreEqual(2, pool.Statistics.InactiveCount, "应剩余2个非活动对象");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试收缩到0")]
        public void Shrink_ToZero_RemovesAllObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            // 同时获取5个对象，然后逐一归还，确保池中有5个不同的对象
            var objs = new TestPoolObject[5];
            for (int i = 0; i < 5; i++)
            {
                objs[i] = pool.Get();
            }
            for (int i = 0; i < 5; i++)
            {
                pool.Return(objs[i]);
            }

            // Act
            pool.Shrink(0);

            // Assert
            Assert.AreEqual(5, _factory.OnDestroyCallCount, "应销毁5个对象");
            Assert.AreEqual(5, pool.Statistics.TotalDestroyed, "销毁计数应为5");
            Assert.AreEqual(0, pool.Statistics.InactiveCount, "应剩余0个非活动对象");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试收缩到大于当前大小不执行操作")]
        public void Shrink_ToLargerSize_DoesNothing()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            // 同时获取3个对象，然后逐一归还，确保池中有3个不同的对象
            var objs = new TestPoolObject[3];
            for (int i = 0; i < 3; i++)
            {
                objs[i] = pool.Get();
            }
            for (int i = 0; i < 3; i++)
            {
                pool.Return(objs[i]);
            }
            _factory.ResetCounters();

            // Act
            pool.Shrink(10);

            // Assert
            Assert.AreEqual(0, _factory.OnDestroyCallCount, "不应销毁任何对象");
            Assert.AreEqual(3, pool.Statistics.InactiveCount, "应剩余3个非活动对象");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Unit")]
        [Description("测试收缩负数目标大小视为0")]
        public void Shrink_NegativeTargetSize_TreatsAsZero()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            // 同时获取3个对象，然后逐一归还，确保池中有3个不同的对象
            var objs = new TestPoolObject[3];
            for (int i = 0; i < 3; i++)
            {
                objs[i] = pool.Get();
            }
            for (int i = 0; i < 3; i++)
            {
                pool.Return(objs[i]);
            }

            // Act
            pool.Shrink(-1);

            // Assert
            Assert.AreEqual(3, _factory.OnDestroyCallCount, "应销毁3个对象");
            Assert.AreEqual(3, pool.Statistics.TotalDestroyed, "销毁计数应为3");
            Assert.AreEqual(0, pool.Statistics.InactiveCount, "应剩余0个非活动对象");

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region Dispose 释放测试

        [Test]
        [Category("Unit")]
        [Description("测试释放池清空所有对象")]
        public void Dispose_ClearsAllObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            pool.Return(obj1);

            // Act
            pool.Dispose();

            // Assert
            Assert.AreEqual(1, _factory.OnDestroyCallCount, "只销毁非活动对象");
        }

        [Test]
        [Category("Unit")]
        [Description("测试释放后操作抛出异常")]
        public void Dispose_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            pool.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
            Assert.Throws<ObjectDisposedException>(() => pool.Clear());
            Assert.Throws<ObjectDisposedException>(() => pool.Shrink(0));
        }

        [Test]
        [Category("Unit")]
        [Description("测试重复释放不抛出异常")]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                pool.Dispose();
                pool.Dispose();
                pool.Dispose();
            });
        }

        [Test]
        [Category("Unit")]
        [Description("测试释放后归还对象会被销毁")]
        public void Dispose_AfterDispose_ReturnDestroysObject()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj = pool.Get();
            pool.Dispose();
            _factory.ResetCounters();

            // Act
            bool result = pool.Return(obj);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, _factory.OnDestroyCallCount);
        }

        #endregion
    }
}
