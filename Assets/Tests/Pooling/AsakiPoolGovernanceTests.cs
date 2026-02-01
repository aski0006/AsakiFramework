using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Pooling;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// AsakiGenericPool LRU收缩和治理功能单元测试
    /// 测试对象池的自动治理、LRU淘汰和内存管理功能
    /// </summary>
    [TestFixture]
    public class AsakiPoolGovernanceTests
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
                MaxSize = 100,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = true,
                EnableAutoShrink = true,
                CheckInterval = 1f,
                IdleTimeout = 2f,
                KeepMinSize = 2,
                ShrinkRatio = 0.5f,
            };
        }

        [TearDown]
        public void Teardown()
        {
            _factory = null;
            _config = null;
        }

        #region ShrinkByLRU 测试

        [Test]
        [Category("Governance")]
        [Description("测试LRU收缩销毁最久未使用的对象")]
        public void ShrinkByLRU_RemovesLeastRecentlyUsedObjects()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建10个对象并归还到池
            var objects = new List<TestPoolObject>();
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.Get();
                objects.Add(obj);
            }

            // 按顺序归还，使每个对象有不同的最后使用时间
            foreach (var obj in objects)
            {
                pool.Return(obj);
            }

            // Act - 强制收缩到2个对象
            int removed = pool.ShrinkByLRU(Time.time, force: true);

            // Assert
            Assert.AreEqual(8, removed, "应销毁8个对象，保留2个");
            Assert.AreEqual(2, pool.Statistics.InactiveCount);
            Assert.AreEqual(8, pool.Statistics.TotalDestroyed);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试LRU收缩保留至少KeepMinSize个对象")]
        public void ShrinkByLRU_KeepsAtLeastKeepMinSize()
        {
            // Arrange
            _config.KeepMinSize = 5;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建20个对象并归还
            for (int i = 0; i < 20; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act - 强制收缩
            int removed = pool.ShrinkByLRU(Time.time, force: true);

            // Assert
            Assert.AreEqual(15, removed, "应销毁15个对象");
            Assert.AreEqual(5, pool.Statistics.InactiveCount, "应保留5个对象");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试LRU收缩非强制模式按ShrinkRatio计算")]
        public void ShrinkByLRU_NonForceMode_UsesShrinkRatio()
        {
            // Arrange
            _config.ShrinkRatio = 0.3f; // 收缩30%
            _config.KeepMinSize = 0;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建10个对象并归还
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act - 非强制收缩（需要设置过期时间）
            float currentTime = Time.time + _config.IdleTimeout + 1f;
            int removed = pool.ShrinkByLRU(currentTime, force: false);

            // Assert - 应收缩约30%（3个对象）
            Assert.GreaterOrEqual(removed, 2);
            Assert.LessOrEqual(removed, 4);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试LRU收缩空池返回0")]
        public void ShrinkByLRU_EmptyPool_ReturnsZero()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // Act
            int removed = pool.ShrinkByLRU(Time.time, force: true);

            // Assert
            Assert.AreEqual(0, removed);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试LRU收缩更新LastGovernanceCheckTime")]
        public void ShrinkByLRU_UpdatesLastCheckTime()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);
            var obj = pool.Get();
            pool.Return(obj);

            float initialTime = pool.LastGovernanceCheckTime;
            float currentTime = Time.time + 1f;

            // Act
            pool.ShrinkByLRU(currentTime, force: true);

            // Assert
            Assert.AreEqual(currentTime, pool.LastGovernanceCheckTime);

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region PerformGovernance 测试

        [Test]
        [Category("Governance")]
        [Description("测试治理检查在间隔到达时执行")]
        public void PerformGovernance_WhenIntervalPassed_PerformsShrink()
        {
            // Arrange
            _config.CheckInterval = 0.1f;
            _config.IdleTimeout = 0.05f;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建对象并归还
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // 等待超过检查间隔
            Thread.Sleep(150);

            // Act
            bool performed = pool.PerformGovernance(Time.time);

            // Assert
            Assert.IsTrue(performed, "应执行了收缩操作");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试治理检查在间隔未到达时不执行")]
        public void PerformGovernance_WhenIntervalNotPassed_DoesNotPerformShrink()
        {
            // Arrange
            _config.CheckInterval = 60f; // 很长的间隔
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建对象并归还
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act - 立即执行治理检查
            bool performed = pool.PerformGovernance(Time.time);

            // Assert
            Assert.IsFalse(performed, "不应执行收缩操作");
            Assert.AreEqual(10, pool.Statistics.InactiveCount, "对象数应保持不变");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试禁用自动收缩时不执行治理")]
        public void PerformGovernance_WhenAutoShrinkDisabled_ReturnsFalse()
        {
            // Arrange
            _config.EnableAutoShrink = false;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建对象并归还
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act
            bool performed = pool.PerformGovernance(Time.time);

            // Assert
            Assert.IsFalse(performed);

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region 时间相关测试

        [Test]
        [Category("Governance")]
        [Description("测试对象在IdleTimeout内不被销毁")]
        public void ShrinkByLRU_ObjectsWithinIdleTimeout_NotDestroyed()
        {
            // Arrange
            _config.IdleTimeout = 10f; // 很长的闲置超时
            _config.KeepMinSize = 0;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建并归还对象
            for (int i = 0; i < 5; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act - 立即执行非强制收缩（对象仍在IdleTimeout内）
            int removed = pool.ShrinkByLRU(Time.time, force: false);

            // Assert
            Assert.AreEqual(0, removed, "对象不应被销毁");

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试超过IdleTimeout的对象被销毁")]
        public void ShrinkByLRU_ObjectsBeyondIdleTimeout_AreDestroyed()
        {
            // Arrange
            _config.IdleTimeout = 1f;
            _config.KeepMinSize = 0;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建并归还对象
            for (int i = 0; i < 5; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act - 等待超过IdleTimeout后执行非强制收缩
            float futureTime = Time.time + _config.IdleTimeout + 1f;
            int removed = pool.ShrinkByLRU(futureTime, force: false);

            // Assert
            Assert.AreEqual(5, removed, "所有对象都应被销毁");

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region 复杂场景测试

        [Test]
        [Category("Governance")]
        [Description("测试LRU收缩保留最近使用的对象")]
        public void ShrinkByLRU_PreservesMostRecentlyUsed()
        {
            // Arrange
            _config.KeepMinSize = 2;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建5个对象
            var objects = new List<TestPoolObject>();
            for (int i = 0; i < 5; i++)
            {
                objects.Add(pool.Get());
            }

            // 按顺序归还
            foreach (var obj in objects)
            {
                pool.Return(obj);
            }

            // Act - 强制收缩到2个对象
            pool.ShrinkByLRU(Time.time, force: true);

            // Assert - 获取剩余的对象，应该是最新归还的2个
            var remaining1 = pool.Get();
            var remaining2 = pool.Get();

            // 最新归还的是 objects[4] 和 objects[3]
            Assert.AreSame(objects[4], remaining1);
            Assert.AreSame(objects[3], remaining2);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试多次LRU收缩的累积效果")]
        public void ShrinkByLRU_MultipleTimes_AccumulatesCorrectly()
        {
            // Arrange
            _config.KeepMinSize = 0;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建10个对象并归还
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            // Act - 第一次收缩到5个
            int removed1 = pool.ShrinkByLRU(Time.time, force: true);
            Assert.AreEqual(5, removed1);
            Assert.AreEqual(5, pool.Statistics.InactiveCount);

            // 再添加5个对象
            for (int i = 0; i < 5; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }
            Assert.AreEqual(10, pool.Statistics.InactiveCount);

            // 第二次收缩到2个
            _config.KeepMinSize = 2;
            int removed2 = pool.ShrinkByLRU(Time.time, force: true);
            Assert.AreEqual(8, removed2);
            Assert.AreEqual(2, pool.Statistics.InactiveCount);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        [Category("Governance")]
        [Description("测试LRU收缩与活动对象的共存")]
        public void ShrinkByLRU_WithActiveObjects_OnlyShrinksInactive()
        {
            // Arrange
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建10个对象，5个活动，5个非活动
            var activeObjects = new List<TestPoolObject>();
            for (int i = 0; i < 5; i++)
            {
                activeObjects.Add(pool.Get());
            }

            for (int i = 0; i < 5; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            Assert.AreEqual(5, pool.Statistics.ActiveCount);
            Assert.AreEqual(5, pool.Statistics.InactiveCount);

            // Act - 强制收缩
            int removed = pool.ShrinkByLRU(Time.time, force: true);

            // Assert - 只应销毁非活动对象
            Assert.AreEqual(3, removed, "应销毁3个非活动对象，保留2个");
            Assert.AreEqual(5, pool.Statistics.ActiveCount, "活动对象应保持不变");
            Assert.AreEqual(2, pool.Statistics.InactiveCount);

            // 验证活动对象仍然有效
            foreach (var obj in activeObjects)
            {
                Assert.IsNotNull(obj);
            }

            // Cleanup
            pool.Dispose();
        }

        #endregion

        #region 性能测试

        [Test]
        [Category("Performance")]
        [Description("测试LRU收缩大池的性能")]
        [Timeout(5000)]
        public void ShrinkByLRU_LargePool_PerformsEfficiently()
        {
            // Arrange
            const int objectCount = 1000;
            _config.KeepMinSize = 100;
            var pool = new AsakiGenericPool<TestPoolObject>("TestPool", _factory, _config);

            // 创建大量对象
            for (int i = 0; i < objectCount; i++)
            {
                var obj = pool.Get();
                pool.Return(obj);
            }

            Assert.AreEqual(objectCount, pool.Statistics.InactiveCount);

            // Act
            var startTime = DateTime.Now;
            int removed = pool.ShrinkByLRU(Time.time, force: true);
            var elapsed = DateTime.Now - startTime;

            // Assert
            Assert.AreEqual(900, removed);
            Assert.AreEqual(100, pool.Statistics.InactiveCount);
            Assert.Less(elapsed.TotalMilliseconds, 1000, "应在1秒内完成");

            // Cleanup
            pool.Dispose();
        }

        #endregion
    }
}
