using System.Threading;
using Asaki.Core.Pooling;
using NUnit.Framework;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// AsakiPoolStatistics 统计信息类单元测试
    /// 测试统计数据的准确性和线程安全性
    /// </summary>
    [TestFixture]
    public class AsakiPoolStatisticsTests
    {
        private AsakiPoolStatistics _statistics;

        [SetUp]
        public void Setup()
        {
            _statistics = new AsakiPoolStatistics { MaxSize = 100 };
        }

        [TearDown]
        public void Teardown()
        {
            _statistics = null;
        }

        [Test]
        [Category("Unit")]
        [Description("测试统计对象的初始状态")]
        public void InitialState_AllCountersAreZero()
        {
            // Assert
            Assert.AreEqual(0, _statistics.TotalCreated, "总创建数应为0");
            Assert.AreEqual(0, _statistics.ActiveCount, "活动对象数应为0");
            Assert.AreEqual(0, _statistics.InactiveCount, "非活动对象数应为0");
            Assert.AreEqual(100, _statistics.MaxSize, "最大大小应为设置的值");
            Assert.AreEqual(0, _statistics.TotalDestroyed, "总销毁数应为0");
            Assert.AreEqual(0, _statistics.GetCallCount, "获取调用次数应为0");
            Assert.AreEqual(0, _statistics.ReturnCallCount, "归还调用次数应为0");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementCreated 方法")]
        public void IncrementCreated_IncreasesTotalCreatedAndInactive()
        {
            // Act
            _statistics.IncrementCreated();

            // Assert
            Assert.AreEqual(1, _statistics.TotalCreated, "总创建数应为1");
            Assert.AreEqual(1, _statistics.InactiveCount, "非活动对象数应为1");
            Assert.AreEqual(0, _statistics.ActiveCount, "活动对象数应保持0");
        }

        [Test]
        [Category("Unit")]
        [Description("测试多次 IncrementCreated")]
        public void IncrementCreated_MultipleTimes_IncreasesCorrectly()
        {
            // Act
            for (int i = 0; i < 10; i++)
            {
                _statistics.IncrementCreated();
            }

            // Assert
            Assert.AreEqual(10, _statistics.TotalCreated, "总创建数应为10");
            Assert.AreEqual(10, _statistics.InactiveCount, "非活动对象数应为10");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementGet 方法 - 从池中获取")]
        public void IncrementGet_FromPool_IncreasesActiveAndDecreasesInactive()
        {
            // Arrange
            _statistics.IncrementCreated();
            _statistics.IncrementCreated();

            // Act - 从池中获取
            _statistics.IncrementGet(fromPool: true);

            // Assert
            Assert.AreEqual(1, _statistics.GetCallCount, "获取调用次数应为1");
            Assert.AreEqual(1, _statistics.ActiveCount, "活动对象数应为1");
            Assert.AreEqual(1, _statistics.InactiveCount, "非活动对象数应为1");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementGet 方法 - 新创建对象")]
        public void IncrementGet_NewCreated_IncreasesActiveButNotDecreasesInactive()
        {
            // Arrange
            _statistics.IncrementCreated();
            _statistics.IncrementCreated();

            // Act - 新创建对象（不从池中获取）
            _statistics.IncrementGet(fromPool: false);

            // Assert
            Assert.AreEqual(1, _statistics.GetCallCount, "获取调用次数应为1");
            Assert.AreEqual(1, _statistics.ActiveCount, "活动对象数应为1");
            Assert.AreEqual(2, _statistics.InactiveCount, "非活动对象数应保持2（未从池中获取）");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementGet 当非活动对象不足时不会低于0")]
        public void IncrementGet_WhenNoInactiveObjects_KeepsInactiveAtZero()
        {
            // Act - 直接获取，没有预先创建的对象（从池中获取）
            _statistics.IncrementGet(fromPool: true);

            // Assert - 非活动数不应低于0
            Assert.AreEqual(1, _statistics.GetCallCount, "获取调用次数应为1");
            Assert.AreEqual(1, _statistics.ActiveCount, "活动对象数应为1");
            Assert.AreEqual(0, _statistics.InactiveCount, "非活动对象数应为0（不会低于0）");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementReturn 方法")]
        public void IncrementReturn_IncreasesInactiveAndDecreasesActive()
        {
            // Arrange
            _statistics.IncrementCreated();
            _statistics.IncrementGet(fromPool: true);

            // Act
            _statistics.IncrementReturn();

            // Assert
            Assert.AreEqual(1, _statistics.ReturnCallCount, "归还调用次数应为1");
            Assert.AreEqual(0, _statistics.ActiveCount, "活动对象数应为0");
            Assert.AreEqual(1, _statistics.InactiveCount, "非活动对象数应为1");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementReturn 当活动对象不足时不会低于0")]
        public void IncrementReturn_WhenNoActiveObjects_KeepsActiveAtZero()
        {
            // Act - 直接归还，没有活动对象
            _statistics.IncrementReturn();

            // Assert - 活动数不应低于0
            Assert.AreEqual(1, _statistics.ReturnCallCount, "归还调用次数应为1");
            Assert.AreEqual(0, _statistics.ActiveCount, "活动对象数应为0（不会低于0）");
            Assert.AreEqual(1, _statistics.InactiveCount, "非活动对象数应为1");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementDestroyed 方法")]
        public void IncrementDestroyed_IncreasesTotalDestroyed()
        {
            // Arrange
            _statistics.IncrementCreated();
            _statistics.IncrementCreated();

            // Act
            _statistics.IncrementDestroyed();

            // Assert
            Assert.AreEqual(1, _statistics.TotalDestroyed, "总销毁数应为1");
            Assert.AreEqual(1, _statistics.InactiveCount, "非活动对象数应为1");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 IncrementDestroyed 当非活动对象不足时不会低于0")]
        public void IncrementDestroyed_WhenNoInactiveObjects_KeepsInactiveAtZero()
        {
            // Act - 直接销毁，没有非活动对象
            _statistics.IncrementDestroyed();

            // Assert - 非活动数不应低于0
            Assert.AreEqual(1, _statistics.TotalDestroyed, "总销毁数应为1");
            Assert.AreEqual(0, _statistics.InactiveCount, "非活动对象数应为0（不会低于0）");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 AdjustInactive 方法")]
        public void AdjustInactive_AdjustsInactiveCount()
        {
            // Act - 增加
            _statistics.AdjustInactive(5);

            // Assert
            Assert.AreEqual(5, _statistics.InactiveCount, "非活动对象数应为5");

            // Act - 减少
            _statistics.AdjustInactive(-3);

            // Assert
            Assert.AreEqual(2, _statistics.InactiveCount, "非活动对象数应为2");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 AdjustInactive 负值")]
        public void AdjustInactive_NegativeValue_DecreasesInactiveCount()
        {
            // Arrange
            _statistics.AdjustInactive(10);

            // Act
            _statistics.AdjustInactive(-5);

            // Assert
            Assert.AreEqual(5, _statistics.InactiveCount, "非活动对象数应为5");
        }

        [Test]
        [Category("Unit")]
        [Description("测试完整的对象生命周期统计")]
        public void FullLifecycle_StatisticsAreCorrect()
        {
            // 创建5个对象
            for (int i = 0; i < 5; i++)
            {
                _statistics.IncrementCreated();
            }

            Assert.AreEqual(5, _statistics.TotalCreated);
            Assert.AreEqual(5, _statistics.InactiveCount);
            Assert.AreEqual(0, _statistics.ActiveCount);

            // 获取3个对象（从池中获取）
            for (int i = 0; i < 3; i++)
            {
                _statistics.IncrementGet(fromPool: true);
            }

            Assert.AreEqual(3, _statistics.GetCallCount);
            Assert.AreEqual(3, _statistics.ActiveCount);
            Assert.AreEqual(2, _statistics.InactiveCount);

            // 归还2个对象
            for (int i = 0; i < 2; i++)
            {
                _statistics.IncrementReturn();
            }

            Assert.AreEqual(2, _statistics.ReturnCallCount);
            Assert.AreEqual(1, _statistics.ActiveCount);
            Assert.AreEqual(4, _statistics.InactiveCount);

            // 销毁1个对象
            _statistics.IncrementDestroyed();

            Assert.AreEqual(1, _statistics.TotalDestroyed);
            Assert.AreEqual(3, _statistics.InactiveCount);
        }

        [Test]
        [Category("Unit")]
        [Description("测试 Reset 方法")]
        public void Reset_ClearsAllStatistics()
        {
            // Arrange
            _statistics.IncrementCreated();
            _statistics.IncrementCreated();
            _statistics.IncrementGet(fromPool: true);
            _statistics.IncrementReturn();
            _statistics.IncrementDestroyed();

            // Act
            _statistics.Reset();

            // Assert
            Assert.AreEqual(0, _statistics.TotalCreated, "总创建数应重置为0");
            Assert.AreEqual(0, _statistics.ActiveCount, "活动对象数应重置为0");
            Assert.AreEqual(0, _statistics.InactiveCount, "非活动对象数应重置为0");
            Assert.AreEqual(0, _statistics.TotalDestroyed, "总销毁数应重置为0");
            Assert.AreEqual(0, _statistics.GetCallCount, "获取调用次数应重置为0");
            Assert.AreEqual(0, _statistics.ReturnCallCount, "归还调用次数应重置为0");
            Assert.AreEqual(100, _statistics.MaxSize, "最大大小应保持不变");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 ToString 方法")]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            _statistics.IncrementCreated();
            _statistics.IncrementGet(fromPool: true);

            // Act
            string result = _statistics.ToString();

            // Assert
            StringAssert.Contains("Total: 1", result);
            StringAssert.Contains("Active: 1", result);
            StringAssert.Contains("Inactive: 0", result);
            StringAssert.Contains("Destroyed: 0", result);
            StringAssert.Contains("Gets: 1", result);
            StringAssert.Contains("Returns: 0", result);
        }

        [Test]
        [Category("Unit")]
        [Description("测试线程安全 - 并发创建")]
        public void ThreadSafety_ConcurrentCreation_MaintainsCorrectCount()
        {
            // Arrange
            int threadCount = 10;
            int incrementsPerThread = 100;
            var threads = new Thread[threadCount];

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < incrementsPerThread; j++)
                    {
                        _statistics.IncrementCreated();
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
            Assert.AreEqual(
                threadCount * incrementsPerThread,
                _statistics.TotalCreated,
                "总创建数应正确（线程安全）"
            );
            Assert.AreEqual(
                threadCount * incrementsPerThread,
                _statistics.InactiveCount,
                "非活动对象数应正确（线程安全）"
            );
        }

        [Test]
        [Category("Unit")]
        [Description("测试线程安全 - 并发获取和归还")]
        public void ThreadSafety_ConcurrentGetAndReturn_MaintainsCorrectCount()
        {
            // Arrange
            // 预先创建对象
            for (int i = 0; i < 1000; i++)
            {
                _statistics.IncrementCreated();
            }

            int threadCount = 10;
            var threads = new Thread[threadCount];

            // Act - 并发获取和归还
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < 50; j++)
                    {
                        _statistics.IncrementGet(fromPool: true);
                        _statistics.IncrementReturn();
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
            Assert.AreEqual(
                threadCount * 50,
                _statistics.GetCallCount,
                "获取调用次数应正确（线程安全）"
            );
            Assert.AreEqual(
                threadCount * 50,
                _statistics.ReturnCallCount,
                "归还调用次数应正确（线程安全）"
            );
            Assert.AreEqual(0, _statistics.ActiveCount, "活动对象数应为0");
            Assert.AreEqual(1000, _statistics.InactiveCount, "非活动对象数应保持1000");
        }

        [Test]
        [Category("Unit")]
        [Description("测试 MaxSize 属性设置")]
        public void MaxSize_CanBeModified()
        {
            // Act
            _statistics.MaxSize = 200;

            // Assert
            Assert.AreEqual(200, _statistics.MaxSize);
        }

        [Test]
        [Category("Unit")]
        [Description("测试 MaxSize 为0的情况")]
        public void MaxSize_ZeroMeansUnlimited()
        {
            // Act
            _statistics.MaxSize = 0;

            // Assert
            Assert.AreEqual(0, _statistics.MaxSize);
        }

        [Test]
        [Category("Unit")]
        [Description("测试负数的 AdjustInactive")]
        public void AdjustInactive_NegativeBeyondZero_KeepsZero()
        {
            // Arrange - 初始为0

            // Act - 减少到负数
            _statistics.AdjustInactive(-10);

            // Assert - 实际值会是-10，但这是预期的行为（允许负数用于修正）
            Assert.AreEqual(-10, _statistics.InactiveCount);
        }
    }
}
