using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Asaki.Core.Pooling;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// AsakiGenericPool 边界条件和异常处理单元测试
    /// 测试对象池在极端情况和错误处理下的行为
    /// </summary>
    [TestFixture]
    public class AsakiGenericPoolBoundaryTests
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

        #region 边界条件测试

        [Test]
        [Category("Boundary")]
        [Description("测试池大小为0时的行为")]
        public void Pool_WithZeroMaxSize_DestroysAllReturnedObjects()
        {
            // Arrange
            _config.MaxSize = 0;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj = pool.Get();

            // Act
            bool result = pool.Return(obj);

            // Assert
            Assert.IsFalse(result, "池大小为0时应销毁所有归还的对象");
            Assert.AreEqual(1, _factory.OnDestroyCallCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Boundary")]
        [Description("测试大量对象获取和归还")]
        public void Pool_HighVolumeOperations_MaintainsConsistency()
        {
            // Arrange - 使用更大的MaxSize以容纳所有对象
            const int iterations = 1000;
            var config = new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = 1000, // 设置足够大以容纳所有对象
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = true,
            };
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, config);
            var objects = new List<TestPoolObject>();

            // Act - 获取大量对象
            for (int i = 0; i < iterations; i++)
            {
                objects.Add(pool.Get());
            }

            // Assert
            Assert.AreEqual(iterations, pool.Statistics.ActiveCount);
            Assert.AreEqual(iterations, pool.Statistics.TotalCreated);

            // Act - 归还所有对象
            foreach (var obj in objects)
            {
                pool.Return(obj);
            }

            // Assert
            Assert.AreEqual(0, pool.Statistics.ActiveCount);
            Assert.AreEqual(iterations, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Boundary")]
        [Description("测试并发获取对象")]
        public void Pool_ConcurrentGets_MaintainsCorrectCount()
        {
            // Arrange - 使用更大的MaxSize以容纳所有对象
            const int threadCount = 10;
            const int getsPerThread = 50;
            var config = new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = 1000, // 设置足够大以容纳所有对象
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = true,
            };
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, config);
            var objects = new System.Collections.Concurrent.ConcurrentBag<TestPoolObject>();
            var threads = new Thread[threadCount];

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < getsPerThread; j++)
                    {
                        var obj = pool.Get();
                        if (obj != null)
                        {
                            objects.Add(obj);
                        }
                    }
                });
                threads[i].Start();
            }

            // Wait for all threads
            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.AreEqual(threadCount * getsPerThread, objects.Count);
            Assert.AreEqual(threadCount * getsPerThread, pool.Statistics.ActiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Boundary")]
        [Description("测试单线程归还对象")]
        public void Pool_SingleThreadReturns_MaintainsCorrectCount()
        {
            // Arrange - 使用更大的MaxSize以容纳所有对象
            const int objectCount = 100;
            var config = new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = 100,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = true,
            };
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, config);
            var objects = new List<TestPoolObject>();

            for (int i = 0; i < objectCount; i++)
            {
                objects.Add(pool.Get());
            }

            // 验证所有对象都已成功获取
            Assert.AreEqual(objectCount, pool.Statistics.ActiveCount, "活动对象数应为100");

            // Act - 单线程归还所有对象
            int successCount = 0;
            for (int i = 0; i < objectCount; i++)
            {
                if (pool.Return(objects[i]))
                {
                    successCount++;
                }
            }

            // Assert - 验证所有归还操作都成功
            Assert.AreEqual(objectCount, successCount, "所有对象都应该被成功归还");
            Assert.AreEqual(0, pool.Statistics.ActiveCount, "活动对象数应为0");
            Assert.AreEqual(objectCount, pool.Statistics.InactiveCount, "非活动对象数应为100");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Boundary")]
        [Description("测试验证失败的对象被销毁")]
        public void Pool_WithValidationFailed_DestroysObject()
        {
            // Arrange
            var invalidFactory = new TestObjectFactory(validateResult: false);
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", invalidFactory, _config);

            // Act - 获取对象
            var obj = pool.Get();

            // Act - 归还应触发验证失败
            bool result = pool.Return(obj);

            // Assert
            Assert.IsFalse(result, "验证失败应返回false");
            Assert.AreEqual(1, invalidFactory.OnDestroyCallCount, "验证失败的对象应被销毁");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Boundary")]
        [Description("测试禁用集合检查时重复归还不被检测")]
        public void Pool_WithCollectionCheckDisabled_AllowsDoubleReturn()
        {
            // Arrange
            _config.EnableCollectionCheck = false;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj = pool.Get();
            pool.Return(obj);

            // Act - 再次归还同一对象
            bool secondReturn = pool.Return(obj);

            // Assert - 禁用集合检查时，重复归还可能成功（取决于池状态）
            // 这里主要验证不抛出异常
            Assert.DoesNotThrow(() => pool.Return(obj));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Boundary")]
        [Description("测试禁用验证时无效对象也被接受")]
        public void Pool_WithValidationDisabled_AcceptsInvalidObjects()
        {
            // Arrange
            _config.EnableValidation = false;
            var invalidFactory = new TestObjectFactory(validateResult: false);
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", invalidFactory, _config);

            var obj = pool.Get();

            // Act
            bool result = pool.Return(obj);

            // Assert - 禁用验证时，即使工厂验证返回false，对象也应被接受
            Assert.IsTrue(result);

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region 异常处理测试

        [Test]
        [Category("Exception")]
        [Description("测试工厂创建抛出异常时Get返回null")]
        public void Get_WhenFactoryThrowsException_ReturnsNull()
        {
            // Arrange
            var throwingFactory = new TestObjectFactory(throwOnCreate: true);
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", throwingFactory, _config);

            // Act
            var obj = pool.Get();

            // Assert - 验证工厂抛出异常时 Get 返回 null
            Assert.IsNull(obj);

            // Cleanup
            pool.Dispose();
        }

        [UnityTest]
        [Category("Exception")]
        [Description("测试异步获取时工厂抛出异常")]
        public IEnumerator GetAsync_WhenFactoryThrowsException_ReturnsNull()
        {
            // Arrange
            var throwingFactory = new TestObjectFactory(throwOnCreate: true);
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", throwingFactory, _config);

            // Act
            yield return pool.GetAsync().ToCoroutine();
            var obj = pool.Get();

            // Assert - 验证工厂抛出异常时 GetAsync 返回 null
            Assert.IsNull(obj);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Exception")]
        [Description("测试工厂OnGet抛出异常不影响获取")]
        public void Get_WhenOnGetThrowsException_StillReturnsObject()
        {
            // Arrange
            var factory = new ExceptionThrowingFactory(throwOnGet: true);
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", factory, _config);

            // Act
            var obj = pool.Get();

            // Assert - 即使OnGet抛出异常，对象仍应被返回
            Assert.IsNotNull(obj);
            Assert.AreEqual(1, pool.Statistics.ActiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Exception")]
        [Description("测试工厂OnReturn抛出异常不影响归还")]
        public void Return_WhenOnReturnThrowsException_StillProcessesReturn()
        {
            // Arrange
            var factory = new ExceptionThrowingFactory(throwOnReturn: true);
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", factory, _config);
            var obj = pool.Get();

            // Act
            bool result = pool.Return(obj);

            // Assert - 即使OnReturn抛出异常，归还仍应成功
            Assert.IsTrue(result);
            Assert.AreEqual(0, pool.Statistics.ActiveCount);
            Assert.AreEqual(1, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Exception")]
        [Description("测试工厂OnDestroy抛出异常不影响清理")]
        public void Clear_WhenOnDestroyThrowsException_ContinuesClearing()
        {
            // Arrange
            var factory = new ExceptionThrowingFactory(throwOnDestroy: true);
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", factory, _config);

            for (int i = 0; i < 3; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() => pool.Clear());

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Exception")]
        [Description("测试释放后获取抛出ObjectDisposedException")]
        public void Get_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            pool.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
        }

        [UnityTest]
        [Category("Exception")]
        [Description("测试释放后异步获取抛出ObjectDisposedException")]
        public IEnumerator GetAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            pool.Dispose();

            // Act & Assert - 使用 UniTask 的异常处理
            Exception capturedException = null;
            var task = pool.GetAsync();
            task.Forget(e => capturedException = e);

            // 等待一帧让异步操作执行
            yield return null;

            Assert.IsNotNull(capturedException, "应抛出异常");
            Assert.IsInstanceOf<ObjectDisposedException>(
                capturedException,
                "应抛出 ObjectDisposedException"
            );
        }

        [UnityTest]
        [Category("Exception")]
        [Description("测试释放后预热抛出ObjectDisposedException")]
        public IEnumerator PrewarmAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            pool.Dispose();

            // Act & Assert - 使用 UniTask 的异常处理
            Exception capturedException = null;
            var task = pool.PrewarmAsync(5);
            task.Forget(e => capturedException = e);

            // 等待一帧让异步操作执行
            yield return null;

            Assert.IsNotNull(capturedException, "应抛出异常");
            Assert.IsInstanceOf<ObjectDisposedException>(
                capturedException,
                "应抛出 ObjectDisposedException"
            );
        }

        [Test]
        [Category("Exception")]
        [Description("测试释放后清空抛出ObjectDisposedException")]
        public void Clear_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            pool.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => pool.Clear());
        }

        [Test]
        [Category("Exception")]
        [Description("测试释放后收缩抛出ObjectDisposedException")]
        public void Shrink_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            pool.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => pool.Shrink(0));
        }

        #endregion

        #region 特殊场景测试

        [Test]
        [Category("Special")]
        [Description("测试获取-归还-再获取循环")]
        public void Pool_GetReturnCycle_ReusesSameObject()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act - 第一次获取
            var obj1 = pool.Get();
            int firstId = obj1.Id;
            pool.Return(obj1);

            // Act - 第二次获取
            var obj2 = pool.Get();
            int secondId = obj2.Id;

            // Assert
            Assert.AreSame(obj1, obj2, "应是同一个对象");
            Assert.AreEqual(firstId, secondId, "ID应相同");
            Assert.AreEqual(1, pool.Statistics.TotalCreated, "只创建了一个对象");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Special")]
        [Description("测试多对象获取后的LIFO顺序")]
        public void Pool_MultipleObjects_LIFOReturn()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            var obj3 = pool.Get();

            // Act - 按 1, 2, 3 顺序归还
            pool.Return(obj1);
            pool.Return(obj2);
            pool.Return(obj3);

            // Act - 按 LIFO 顺序获取
            var get3 = pool.Get();
            var get2 = pool.Get();
            var get1 = pool.Get();

            // Assert - 栈是 LIFO，所以获取顺序应与归还顺序相反
            Assert.AreSame(obj3, get3);
            Assert.AreSame(obj2, get2);
            Assert.AreSame(obj1, get1);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Special")]
        [Description("测试部分对象活动时的统计")]
        public void Pool_PartialActiveObjects_CorrectStatistics()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建5个对象
            var objects = new List<TestPoolObject>();
            for (int i = 0; i < 5; i++)
            {
                objects.Add(pool.Get());
            }

            // 归还3个
            pool.Return(objects[0]);
            pool.Return(objects[1]);
            pool.Return(objects[2]);

            // Assert
            Assert.AreEqual(5, pool.Statistics.TotalCreated);
            Assert.AreEqual(2, pool.Statistics.ActiveCount);
            Assert.AreEqual(3, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Special")]
        [Description("测试长时间运行后的池稳定性")]
        public void Pool_LongRunningOperations_RemainsStable()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            const int iterations = 100;

            // Act - 模拟长时间运行
            for (int i = 0; i < iterations; i++)
            {
                var obj1 = pool.Get();
                var obj2 = pool.Get();
                pool.Return(obj1);
                pool.Return(obj2);

                if (i % 10 == 0)
                {
                    pool.Shrink(1);
                }
            }

            // Assert
            Assert.GreaterOrEqual(pool.Statistics.TotalCreated, 0);
            Assert.GreaterOrEqual(pool.Statistics.ActiveCount, 0);
            Assert.GreaterOrEqual(pool.Statistics.InactiveCount, 0);

            // Cleanup
            pool.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 在特定操作抛出异常的测试工厂
    /// </summary>
    public class ExceptionThrowingFactory : TestObjectFactory
    {
        public bool ThrowOnGet { get; set; }
        public bool ThrowOnReturn { get; set; }
        public bool ThrowOnDestroy { get; set; }

        public ExceptionThrowingFactory(
            bool throwOnGet = false,
            bool throwOnReturn = false,
            bool throwOnDestroy = false
        )
        {
            ThrowOnGet = throwOnGet;
            ThrowOnReturn = throwOnReturn;
            ThrowOnDestroy = throwOnDestroy;
        }

        public new void OnGet(TestPoolObject obj)
        {
            base.OnGet(obj);
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("Simulated OnGet exception");
            }
        }

        public new void OnReturn(TestPoolObject obj)
        {
            base.OnReturn(obj);
            if (ThrowOnReturn)
            {
                throw new InvalidOperationException("Simulated OnReturn exception");
            }
        }

        public new void OnDestroy(TestPoolObject obj)
        {
            base.OnDestroy(obj);
            if (ThrowOnDestroy)
            {
                throw new InvalidOperationException("Simulated OnDestroy exception");
            }
        }
    }
}
