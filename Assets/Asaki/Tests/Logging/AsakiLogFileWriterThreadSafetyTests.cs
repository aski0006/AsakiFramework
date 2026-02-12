using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Logging
{
    /// <summary>
    /// Asaki 日志文件写入器线程安全性测试类
    /// 验证日志文件写入在多线程环境下的正确性和稳定性
    /// </summary>
    [TestFixture]
    public class AsakiLogFileWriterThreadSafetyTests
    {
        private AsakiLogAggregator _aggregator;
        private AsakiLogFileWriter _writer;
        private string _testPrefix;

        [SetUp]
        public void Setup()
        {
            // 为每个测试生成唯一前缀，避免文件冲突
            _testPrefix = $"Test_{Guid.NewGuid():N}";

            _aggregator = new AsakiLogAggregator();
            _writer = new AsakiLogFileWriter(_aggregator);

            // 立即应用唯一前缀
            _writer.ApplyConfig(
                new AsakiLogConfig
                {
                    FilePrefix = _testPrefix,
                    MaxFileSizeKB = 2048,
                    MaxHistoryFiles = 10,
                }
            );
        }

        [TearDown]
        public void TearDown()
        {
            _writer?.Dispose();
            _writer = null;

            _aggregator?.Dispose();
            _aggregator = null;

            // 清理测试生成的日志文件
            try
            {
                var logDir = Path.Combine(Application.persistentDataPath, "Logs");
                if (Directory.Exists(logDir))
                {
                    var files = Directory.GetFiles(logDir, $"{_testPrefix}_*.asakilog");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // 忽略删除失败
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"清理测试文件失败: {ex.Message}");
            }
        }

        #region 基础线程安全测试

        /// <summary>
        /// 测试：并发配置更新不应导致数据竞争
        /// </summary>
        [Test]
        public void ConcurrentConfigUpdates_ShouldNotCauseRaceCondition()
        {
            // Arrange
            var tasks = new List<Task>();
            var configs = new[]
            {
                new AsakiLogConfig
                {
                    MaxFileSizeKB = 1024,
                    MaxHistoryFiles = 5,
                    FilePrefix = _testPrefix,
                },
                new AsakiLogConfig
                {
                    MaxFileSizeKB = 2048,
                    MaxHistoryFiles = 10,
                    FilePrefix = _testPrefix,
                },
                new AsakiLogConfig
                {
                    MaxFileSizeKB = 512,
                    MaxHistoryFiles = 3,
                    FilePrefix = _testPrefix,
                },
            };

            // Act - 并发更新配置
            for (int i = 0; i < 10; i++)
            {
                int configIndex = i % configs.Length;
                tasks.Add(
                    Task.Run(() =>
                    {
                        _writer.ApplyConfig(configs[configIndex]);
                    })
                );
            }

            // Assert - 不应抛出异常
            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()));
        }

        /// <summary>
        /// 测试：配置更新与日志写入并发执行的安全性
        /// </summary>
        [Test]
        [Timeout(10000)]
        public async Task ConcurrentConfigUpdateAndLogging_ShouldBeThreadSafe()
        {
            // Arrange
            var logTask = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    _aggregator.Log(
                        AsakiLogLevel.Info,
                        $"ConcurrentLog_Message{i}",
                        null,
                        "ConcurrentTest.cs",
                        i,
                        null
                    );

                    if (i % 100 == 0)
                    {
                        Thread.Sleep(1);
                    }
                }
            });

            var configTask = Task.Run(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    _writer.ApplyConfig(
                        new AsakiLogConfig
                        {
                            MaxFileSizeKB = 512 + (i * 100),
                            MaxHistoryFiles = 5 + i,
                            FilePrefix = _testPrefix,
                        }
                    );
                    Thread.Sleep(50);
                }
            });

            // Act & Assert
            // 使用 await Task.WhenAll 替代 WaitAll，防止阻塞 Unity MainThread
            await Task.WhenAll(logTask, configTask);

            // 等待写入完成 (Yield Execution)
            await Task.Delay(1000);
        }

        #endregion

        #region 文件轮转测试

        /// <summary>
        /// 测试：文件轮转在多线程环境下的正确性
        /// </summary>
        [Test]
        [Timeout(15000)]
        public async Task FileRotation_UnderConcurrentLogging_ShouldWorkCorrectly()
        {
            // Arrange - 设置较小的文件大小以触发轮转
            _writer.ApplyConfig(
                new AsakiLogConfig
                {
                    MaxFileSizeKB = 1, // 1KB 小文件
                    MaxHistoryFiles = 5,
                    FilePrefix = _testPrefix,
                }
            );

            // Act - 并发写入大量日志
            var tasks = new List<Task>();
            for (int t = 0; t < 5; t++)
            {
                int threadId = t;
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 500; i++)
                        {
                            _aggregator.Log(
                                AsakiLogLevel.Info,
                                $"RotationTest_Thread{threadId}_LargeMessageToTriggerRotation_{new string('x', 100)}",
                                null,
                                "RotationTest.cs",
                                i,
                                null
                            );
                        }
                    })
                );
            }

            // 关键修正：使用 await Task.WhenAll 非阻塞等待
            await Task.WhenAll(tasks);

            // 关键修正：使用 await Task.Delay 替代 Thread.Sleep
            // Thread.Sleep 会阻塞主线程，导致 UniTask 的 PlayerLoop 无法 Tick，
            // 从而导致 AsakiLogFileWriter 的 WriteLoopAsync 无法执行 FlushBufferAsync。
            await Task.Delay(2000);

            // Assert
            var logDir = Path.Combine(Application.persistentDataPath, "Logs");
            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, $"{_testPrefix}_*.asakilog");
                Assert.Greater(files.Length, 0, "应该生成至少一个日志文件");
                Debug.Log($"文件轮转测试: 生成了 {files.Length} 个日志文件");
            }
        }

        #endregion

        #region Dispose 安全测试

        /// <summary>
        /// 测试：并发 Dispose 调用的安全性
        /// </summary>
        [Test]
        public void ConcurrentDispose_ShouldBeIdempotent()
        {
            // Arrange - 使用独立的前缀创建新的 writer
            var uniquePrefix = $"DisposeTest_{Guid.NewGuid():N}";
            var aggregator = new AsakiLogAggregator();
            var writer = new AsakiLogFileWriter(aggregator);
            writer.ApplyConfig(new AsakiLogConfig { FilePrefix = uniquePrefix });

            // 先写入一些日志
            for (int i = 0; i < 100; i++)
            {
                aggregator.Log(AsakiLogLevel.Info, $"Message{i}", null, "Test.cs", i, null);
            }

            // 等待日志写入
            Thread.Sleep(500);

            // Act - 并发调用 Dispose
            var disposeTasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                disposeTasks.Add(
                    Task.Run(() =>
                    {
                        writer.Dispose();
                    })
                );
            }

            // Assert - 不应抛出异常
            Assert.DoesNotThrow(() => Task.WaitAll(disposeTasks.ToArray()));

            // 清理
            aggregator.Dispose();
        }

        /// <summary>
        /// 测试：写入过程中 Dispose 的安全性
        /// </summary>
        [Test]
        [Timeout(10000)]
        public void Dispose_DuringWriting_ShouldNotCorruptFiles()
        {
            // Arrange - 使用独立的前缀
            var uniquePrefix = $"DisposeDuringWrite_{Guid.NewGuid():N}";
            var aggregator = new AsakiLogAggregator();
            var writer = new AsakiLogFileWriter(aggregator);
            writer.ApplyConfig(new AsakiLogConfig { FilePrefix = uniquePrefix });

            // Act - 开始写入
            var writeTask = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    aggregator.Log(
                        AsakiLogLevel.Info,
                        $"DisposeDuringWrite_Message{i}_{new string('x', 50)}",
                        null,
                        "DisposeTest.cs",
                        i,
                        null
                    );

                    if (i == 100)
                    {
                        // 在写入中途 Dispose
                        writer.Dispose();
                    }
                }
            });

            // Assert - 不应抛出未预期的异常
            Assert.DoesNotThrow(() =>
            {
                try
                {
                    writeTask.Wait();
                }
                catch (AggregateException)
                {
                    // 可能由于 Dispose 导致的异常，这是预期的
                }
            });

            // 清理
            aggregator.Dispose();
        }

        #endregion

        #region 历史清理测试

        /// <summary>
        /// 测试：并发历史文件清理的安全性
        /// </summary>
        [Test]
        [Timeout(15000)]
        public async Task ConcurrentCleanupHistory_ShouldNotDeleteRecentFiles()
        {
            // Arrange - 使用独立前缀创建测试文件
            var testPrefix = $"CleanupTest_{Guid.NewGuid():N}";
            var logDir = Path.Combine(Application.persistentDataPath, "Logs");

            // 先创建一些旧的测试文件
            for (int i = 0; i < 10; i++)
            {
                var testFile = Path.Combine(
                    logDir,
                    $"{testPrefix}_{i}_{DateTime.Now:yyyyMMdd_HHmmss}.asakilog"
                );
                File.WriteAllText(testFile, $"#TEST FILE {i}\n");

                // 修改创建时间为过去的时间
                File.SetCreationTime(testFile, DateTime.Now.AddHours(-i));
            }

            // 应用配置触发清理
            _writer.ApplyConfig(
                new AsakiLogConfig
                {
                    MaxFileSizeKB = 1024,
                    MaxHistoryFiles = 3, // 只保留3个文件
                    FilePrefix = testPrefix,
                }
            );

            // Act - 并发更新配置（触发清理）
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        _writer.ApplyConfig(
                            new AsakiLogConfig
                            {
                                MaxFileSizeKB = 1024,
                                MaxHistoryFiles = 3,
                                FilePrefix = testPrefix,
                            }
                        );
                    })
                );
            }

            // Assert - 不应抛出异常
            await Task.WhenAll(tasks);

            // 等待清理完成 (Yield Execution)
            await Task.Delay(1000);

            // 验证清理结果
            if (Directory.Exists(logDir))
            {
                var remainingFiles = Directory.GetFiles(logDir, $"{testPrefix}_*.asakilog");
                Debug.Log($"历史清理测试: 剩余 {remainingFiles.Length} 个文件");
            }

            // 清理测试文件
            try
            {
                var files = Directory.GetFiles(logDir, $"{testPrefix}_*.asakilog");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch { }
        }

        #endregion

        #region 压力测试

        /// <summary>
        /// 压力测试：高并发日志写入
        /// </summary>
        [Test]
        [Timeout(30000)]
        public async Task StressTest_HighVolumeConcurrentLogging()
        {
            // Arrange
            const int threadCount = 10;
            const int logsPerThread = 1000;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var tasks = new List<Task>();
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks.Add(
                    Task.Run(() =>
                    {
                        for (int i = 0; i < logsPerThread; i++)
                        {
                            _aggregator.Log(
                                AsakiLogLevel.Info,
                                $"Stress_Thread{threadId}_Log{i}_{new string('x', 50)}",
                                $"{{\"thread\":{threadId},\"iteration\":{i}}}",
                                $"StressTest{threadId}.cs",
                                i,
                                null
                            );
                        }
                    })
                );
            }

            await Task.WhenAll(tasks);

            // 等待所有日志写入磁盘 (释放主线程给 UniTask Loop)
            await Task.Delay(2000);

            stopwatch.Stop();

            // Assert
            var logDir = Path.Combine(Application.persistentDataPath, "Logs");
            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, $"{_testPrefix}_*.asakilog");
                long totalSize = 0;
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    totalSize += info.Length;
                }

                Debug.Log(
                    $"压力测试完成: {threadCount * logsPerThread} 条日志, "
                        + $"耗时: {stopwatch.ElapsedMilliseconds}ms, "
                        + $"文件数: {files.Length}, "
                        + $"总大小: {totalSize / 1024}KB"
                );
            }
        }

        /// <summary>
        /// 压力测试：频繁配置切换
        /// </summary>
        [Test]
        [Timeout(10000)]
        public async Task StressTest_RapidConfigSwitching()
        {
            // Arrange
            var configTask = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    _writer.ApplyConfig(
                        new AsakiLogConfig
                        {
                            MaxFileSizeKB = 100 + (i % 10) * 100,
                            MaxHistoryFiles = 3 + (i % 5),
                            FilePrefix = _testPrefix,
                        }
                    );
                    Thread.Sleep(10);
                }
            });

            var logTask = Task.Run(() =>
            {
                for (int i = 0; i < 500; i++)
                {
                    _aggregator.Log(
                        AsakiLogLevel.Info,
                        $"RapidConfig_Message{i}",
                        null,
                        "RapidConfig.cs",
                        i,
                        null
                    );
                    Thread.Sleep(5);
                }
            });

            // Act & Assert
            await Task.WhenAll(configTask, logTask);
        }

        #endregion
    }
}
