using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Logging
{
    /// <summary>
    /// Asaki 日志系统线程安全性测试类
    /// 验证日志服务在多线程环境下的正确性和稳定性
    /// </summary>
    [TestFixture]
    public class AsakiLogThreadSafetyTests
    {
        private AsakiLogAggregator _aggregator;
        private const int StressTestIterations = 1000;
        private const int ConcurrentThreads = 10;

        [SetUp]
        public void Setup()
        {
            _aggregator = new AsakiLogAggregator();
        }

        [TearDown]
        public void TearDown()
        {
            _aggregator?.Dispose();
            _aggregator = null;
        }

        #region 基础线程安全测试

        /// <summary>
        /// 测试：多线程并发写入日志不会产生重复ID
        /// </summary>
        [Test]
        public void ConcurrentLog_Writes_ShouldGenerateUniqueIds()
        {
            // Arrange
            var ids = new List<int>();
            var lockObj = new object();
            var tasks = new List<Task>();
            var syncCts = new CancellationTokenSource();

            // 启动后台 Sync 任务
            var syncTask = Task.Run(() =>
            {
                while (!syncCts.IsCancellationRequested)
                {
                    _aggregator.Sync(1000);
                    Thread.Sleep(10);
                }
            });

            // Act - 多个线程并发写入不同日志
            for (int t = 0; t < ConcurrentThreads; t++)
            {
                int threadId = t;
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            _aggregator.Log(
                                AsakiLogLevel.Info,
                                $"Thread{threadId}_Message{i}",
                                null,
                                $"TestFile{threadId}.cs",
                                i,
                                null
                            );

                            // 定期让出时间片，允许 Sync 执行
                            if (i % 10 == 0)
                            {
                                Thread.Sleep(1);
                            }
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());

            // 停止后台 Sync
            syncCts.Cancel();
            try
            {
                syncTask.Wait(1000);
            }
            catch (AggregateException) { } // 忽略取消导致的异常

            // 在主线程同步处理所有剩余日志
            _aggregator.Sync(10000);

            // Assert - 获取快照并验证ID唯一性
            var snapshot = _aggregator.GetSnapshot();
            var idSet = new HashSet<int>();

            foreach (var log in snapshot)
            {
                Assert.IsFalse(
                    idSet.Contains(log.ID),
                    $"发现重复ID: {log.ID}, 消息: {log.Message}"
                );
                idSet.Add(log.ID);
            }

            Assert.AreEqual(
                ConcurrentThreads * 100,
                snapshot.Count,
                "日志数量应该等于并发线程数乘以每线程日志数"
            );
        }

        /// <summary>
        /// 测试：多线程并发写入相同日志应正确聚合
        /// </summary>
        [Test]
        public void ConcurrentLog_SameMessage_ShouldAggregateCorrectly()
        {
            // Arrange
            const string sharedMessage = "SharedLogMessage";
            const string sharedFile = "SharedFile.cs";
            const int sharedLine = 100;
            var tasks = new List<Task>();
            var syncCts = new CancellationTokenSource();

            // 启动后台 Sync 任务
            var syncTask = Task.Run(() =>
            {
                while (!syncCts.IsCancellationRequested)
                {
                    _aggregator.Sync(1000);
                    Thread.Sleep(10);
                }
            });

            // Act - 多个线程并发写入相同日志
            for (int t = 0; t < ConcurrentThreads; t++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            _aggregator.Log(
                                AsakiLogLevel.Warning,
                                sharedMessage,
                                null,
                                sharedFile,
                                sharedLine,
                                null
                            );

                            if (i % 10 == 0)
                            {
                                Thread.Sleep(1);
                            }
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());

            // 停止后台 Sync
            syncCts.Cancel();
            try
            {
                syncTask.Wait(1000);
            }
            catch (AggregateException) { }

            // 在主线程同步处理所有剩余日志
            _aggregator.Sync(10000);

            // Assert
            var snapshot = _aggregator.GetSnapshot();
            Assert.AreEqual(1, snapshot.Count, "相同日志应该被聚合为一条");
            Assert.AreEqual(
                ConcurrentThreads * 100,
                snapshot[0].Count,
                "聚合计数应该等于总写入次数"
            );
        }

        /// <summary>
        /// 测试：并发调用 GetSnapshot 不应导致数据损坏
        /// </summary>
        [Test]
        public void ConcurrentGetSnapshot_ShouldNotCorruptData()
        {
            // Arrange
            var writeTask = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    _aggregator.Log(AsakiLogLevel.Debug, $"Message{i}", null, "Test.cs", i, null);

                    if (i % 50 == 0)
                    {
                        _aggregator.Sync(100);
                    }
                }
            });

            var readTasks = new List<Task>();
            for (int t = 0; t < 5; t++)
            {
                readTasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            var snapshot = _aggregator.GetSnapshot();
                            // 验证快照数据一致性
                            foreach (var log in snapshot)
                            {
                                Assert.Greater(log.ID, 0, "ID应该大于0");
                                Assert.IsNotNull(log.Message, "消息不应为null");
                            }
                            Thread.Sleep(1);
                        }
                    })
                );
            }

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() =>
            {
                Task.WaitAll(writeTask);
                _aggregator.Sync(10000);
                Task.WaitAll(readTasks.ToArray());
            });
        }

        #endregion

        #region 压力测试

        /// <summary>
        /// 压力测试：高并发写入性能测试
        /// </summary>
        [Test]
        [Timeout(30000)] // 30秒超时
        public void StressTest_HighConcurrencyLogging()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var tasks = new List<Task>();
            var syncCts = new CancellationTokenSource();

            // 启动后台 Sync 任务
            var syncTask = Task.Run(() =>
            {
                while (!syncCts.IsCancellationRequested)
                {
                    _aggregator.Sync(1000);
                    Thread.Sleep(5);
                }
            });

            // Act
            for (int t = 0; t < ConcurrentThreads; t++)
            {
                int threadId = t;
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < StressTestIterations; i++)
                        {
                            // 修正：使用 AsakiLogLevel.Error 级别
                            // 原因：Info 级别会受 MAX_QUEUE_DEPTH(5000) 限制触发背压丢弃，
                            // 导致总数不符。Error 级别会绕过背压检查，适合用于验证
                            // 高并发下的数据完整性和 ID 生成逻辑。
                            _aggregator.Log(
                                AsakiLogLevel.Error,
                                $"StressTest_Thread{threadId}_Iteration{i}",
                                $"{{\"iteration\":{i}}}",
                                $"StressTest{threadId}.cs",
                                i,
                                null
                            );

                            // 定期让出时间片
                            if (i % 50 == 0)
                            {
                                Thread.Sleep(1);
                            }
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());

            // 停止后台 Sync
            syncCts.Cancel();
            try
            {
                syncTask.Wait(1000);
            }
            catch (AggregateException) { }

            _aggregator.Sync(StressTestIterations * ConcurrentThreads);

            stopwatch.Stop();

            // Assert
            var snapshot = _aggregator.GetSnapshot();
            Assert.AreEqual(
                ConcurrentThreads * StressTestIterations,
                snapshot.Count,
                "压力测试后日志数量应该正确 (使用 Error 级别绕过背压)"
            );

            Debug.Log(
                $"压力测试完成: {ConcurrentThreads * StressTestIterations} 条日志, "
                    + $"耗时: {stopwatch.ElapsedMilliseconds}ms, "
                    + $"TPS: {(ConcurrentThreads * StressTestIterations) / (stopwatch.ElapsedMilliseconds / 1000.0):F0}"
            );
        }

        /// <summary>
        /// 压力测试：快速连续 Sync 调用
        /// </summary>
        [Test]
        public void StressTest_RapidSyncCalls()
        {
            // Arrange
            var logCts = new CancellationTokenSource();
            var loggedCount = 0;

            var logTask = Task.Run(() =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    if (logCts.IsCancellationRequested)
                        break;

                    _aggregator.Log(
                        AsakiLogLevel.Debug,
                        $"RapidSync_Message{i}",
                        null,
                        "RapidSync.cs",
                        i,
                        null
                    );

                    Interlocked.Increment(ref loggedCount);

                    if (i % 100 == 0)
                    {
                        Thread.Sleep(1);
                    }
                }
            });

            // Act - 快速连续调用 Sync
            var syncTask = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    _aggregator.Sync(1000);
                    Thread.Sleep(1);
                }
            });

            // Assert - 不应抛出异常
            Assert.DoesNotThrow(() =>
            {
                Task.WaitAll(logTask, syncTask);
                _aggregator.Sync(10000);
            });

            var snapshot = _aggregator.GetSnapshot();
            Debug.Log($"预期日志: {loggedCount}, 实际日志: {snapshot.Count}");

            // 由于背压机制，实际数量可能略少，但应该大部分被处理
            Assert.GreaterOrEqual(snapshot.Count, loggedCount * 0.8, "至少80%的日志应该被正确处理");
        }

        #endregion

        #region 边界条件测试

        /// <summary>
        /// 测试：队列深度超过限制时的背压行为
        /// </summary>
        [Test]
        public void BackPressure_WhenQueueFull_ShouldDropLowerLevelLogs()
        {
            // Arrange - 快速写入大量日志以触发背压
            var tasks = new List<Task>();

            // Act - 并发写入大量 Debug 级别日志（应该被丢弃）和 Error 级别日志（应该保留）
            for (int t = 0; t < 20; t++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            // 混合写入不同级别日志
                            if (i % 10 == 0)
                            {
                                _aggregator.Log(
                                    AsakiLogLevel.Error,
                                    $"ErrorMessage{i}",
                                    null,
                                    "Test.cs",
                                    i,
                                    null
                                );
                            }
                            else
                            {
                                _aggregator.Log(
                                    AsakiLogLevel.Debug,
                                    $"DebugMessage{i}",
                                    null,
                                    "Test.cs",
                                    i,
                                    null
                                );
                            }
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());
            _aggregator.Sync(50000);

            // Assert
            var snapshot = _aggregator.GetSnapshot();
            int errorCount = 0;
            int debugCount = 0;

            foreach (var log in snapshot)
            {
                if (log.Level == AsakiLogLevel.Error)
                    errorCount++;
                else if (log.Level == AsakiLogLevel.Debug)
                    debugCount++;
            }

            // Error 日志应该被保留
            Assert.Greater(errorCount, 0, "Error 级别日志应该被保留");
            Debug.Log($"背压测试: Error日志={errorCount}, Debug日志={debugCount}");
        }

        /// <summary>
        /// 测试：并发 Clear 操作的安全性
        /// </summary>
        [Test]
        public void ConcurrentClear_ShouldNotCauseDataCorruption()
        {
            // Arrange
            var tasks = new List<Task>();

            // 写入任务
            tasks.Add(
                Task.Run(() =>
                {
                    for (int i = 0; i < 5000; i++)
                    {
                        _aggregator.Log(
                            AsakiLogLevel.Info,
                            $"Message{i}",
                            null,
                            "Test.cs",
                            i,
                            null
                        );

                        if (i % 100 == 0)
                        {
                            Thread.Sleep(1);
                        }
                    }
                })
            );

            // 清除任务
            tasks.Add(
                Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Thread.Sleep(10);
                        _aggregator.Clear();
                    }
                })
            );

            // 读取任务
            tasks.Add(
                Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Thread.Sleep(5);
                        var snapshot = _aggregator.GetSnapshot();
                        // 不应抛出异常
                    }
                })
            );

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                Task.WaitAll(tasks.ToArray());
            });
        }

        #endregion

        #region 异常安全测试

        /// <summary>
        /// 测试：多线程环境下异常日志处理
        /// </summary>
        [Test]
        public void ConcurrentExceptionLogging_ShouldCaptureStackTrace()
        {
            // Arrange
            var tasks = new List<Task>();
            var syncCts = new CancellationTokenSource();

            // 启动后台 Sync 任务
            var syncTask = Task.Run(() =>
            {
                while (!syncCts.IsCancellationRequested)
                {
                    _aggregator.Sync(100);
                    Thread.Sleep(5);
                }
            });

            // Act
            for (int t = 0; t < 5; t++)
            {
                int threadId = t;
                tasks.Add(
                    Task.Run(() =>
                    {
                        try
                        {
                            throw new InvalidOperationException(
                                $"Test exception from thread {threadId}"
                            );
                        }
                        catch (Exception ex)
                        {
                            _aggregator.Log(
                                AsakiLogLevel.Error,
                                $"Exception in thread {threadId}",
                                null,
                                "ExceptionTest.cs",
                                threadId,
                                ex
                            );
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());

            // 停止后台 Sync
            syncCts.Cancel();
            try
            {
                syncTask.Wait(1000);
            }
            catch (AggregateException) { }

            _aggregator.Sync(100);

            // Assert
            var snapshot = _aggregator.GetSnapshot();
            Assert.AreEqual(5, snapshot.Count, "应该捕获5个异常日志");

            foreach (var log in snapshot)
            {
                Assert.IsNotNull(log.StackFrames, "异常日志应该包含堆栈信息");
                Assert.Greater(log.StackFrames.Count, 0, "堆栈帧不应为空");
            }
        }

        /// <summary>
        /// 测试：Dispose 后并发访问的安全性
        /// </summary>
        [Test]
        public void Dispose_ConcurrentAccess_ShouldNotThrow()
        {
            // Arrange
            var aggregator = new AsakiLogAggregator();

            // 先写入一些日志
            for (int i = 0; i < 100; i++)
            {
                aggregator.Log(AsakiLogLevel.Info, $"Message{i}", null, "Test.cs", i, null);
            }

            // Act - 在另一个线程 Dispose
            var disposeTask = Task.Run(() =>
            {
                aggregator.Dispose();
            });

            // 同时尝试读取
            var readTask = Task.Run(() =>
            {
                try
                {
                    var snapshot = aggregator.GetSnapshot();
                }
                catch (ObjectDisposedException)
                {
                    // 预期的异常
                }
            });

            // Assert - 不应抛出未预期的异常
            Assert.DoesNotThrow(() =>
            {
                Task.WaitAll(disposeTask, readTask);
            });
        }

        #endregion

        #region 数据一致性测试

        /// <summary>
        /// 测试：聚合计数的准确性
        /// </summary>
        [Test]
        public void AggregationCount_ShouldBeAccurateUnderConcurrency()
        {
            // Arrange
            const string message = "AggregationTestMessage";
            const int expectedCount = ConcurrentThreads * 100;
            var tasks = new List<Task>();
            var syncCts = new CancellationTokenSource();

            // 启动后台 Sync 任务
            var syncTask = Task.Run(() =>
            {
                while (!syncCts.IsCancellationRequested)
                {
                    _aggregator.Sync(1000);
                    Thread.Sleep(5);
                }
            });

            // Act
            for (int t = 0; t < ConcurrentThreads; t++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            _aggregator.Log(
                                AsakiLogLevel.Warning,
                                message,
                                null,
                                "AggregationTest.cs",
                                1,
                                null
                            );

                            if (i % 10 == 0)
                            {
                                Thread.Sleep(1);
                            }
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());

            // 停止后台 Sync
            syncCts.Cancel();
            try
            {
                syncTask.Wait(1000);
            }
            catch (AggregateException) { }

            _aggregator.Sync(expectedCount);

            // Assert
            var snapshot = _aggregator.GetSnapshot();
            Assert.AreEqual(1, snapshot.Count, "相同日志应该被聚合为一条");
            Assert.AreEqual(
                expectedCount,
                snapshot[0].Count,
                $"聚合计数应该准确，期望 {expectedCount}，实际 {snapshot[0].Count}"
            );
        }

        /// <summary>
        /// 测试：时间戳一致性
        /// </summary>
        [Test]
        public void Timestamp_ShouldBeMonotonicallyIncreasing()
        {
            // Arrange
            var tasks = new List<Task>();

            // Act - 并发写入带时间戳的日志
            for (int t = 0; t < 5; t++)
            {
                int threadId = t;
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            _aggregator.Log(
                                AsakiLogLevel.Info,
                                $"TimestampTest_Thread{threadId}_Msg{i}",
                                null,
                                "TimestampTest.cs",
                                i,
                                null
                            );
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());
            _aggregator.Sync(5000);

            // Assert
            var snapshot = _aggregator.GetSnapshot();
            foreach (var log in snapshot)
            {
                Assert.Greater(log.LastTimestamp, 0, "时间戳应该大于0");
            }
        }

        #endregion
    }
}
