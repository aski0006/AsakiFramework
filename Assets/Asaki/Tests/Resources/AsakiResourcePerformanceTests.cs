// File: Assets/Asaki/Tests/Resources/AsakiResourcePerformanceTests.cs
// AsakiResourceService 性能测试

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Asaki.Core.Resources;
using Asaki.Tests.Resources.Mocks;
using Asaki.Unity.Services.Resources;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Asaki.Tests.Resources
{
    /// <summary>
    /// AsakiResourceService 性能测试
    /// 测试资源加载速度、内存占用和GC表现
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class AsakiResourcePerformanceTests
    {
        private MockAsakiResStrategy _mockStrategy;
        private MockAsakiAsyncService _mockAsyncService;
        private MockAsakiResDependencyLookup _mockDependencyLookup;
        private AsakiResourceService _resourceService;

        // 性能阈值
        private const float SINGLE_LOAD_TIME_BUDGET_MS = 100f; // 单资源加载时间预算
        private const float BATCH_LOAD_TIME_BUDGET_MS = 500f; // 批量加载时间预算
        private const long MAX_GC_ALLOCATION_BYTES = 1024 * 1024; // 最大GC分配1MB

        [SetUp]
        public void Setup()
        {
            _mockStrategy = new MockAsakiResStrategy();
            _mockAsyncService = new MockAsakiAsyncService();
            _mockStrategy.LoadDelayMs = 0; // 性能测试无延迟
            _mockDependencyLookup = new MockAsakiResDependencyLookup();
            _resourceService = new AsakiResourceService(
                _mockStrategy,
                _mockAsyncService,
                _mockDependencyLookup
            );
        }

        [TearDown]
        public void Teardown()
        {
            _resourceService?.OnDispose();
            _resourceService = null;
            _mockStrategy = null;
            _mockAsyncService = null;
            _mockDependencyLookup = null;
        }

        #region 加载性能测试

        [UnityTest]
        [Category("Performance")]
        [Description("测试单资源加载速度")]
        public IEnumerator LoadPerformance_SingleAsset_MeetsTimeBudget()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("perf/test", asset);

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            yield return _resourceService
                .LoadAsync<GameObject>("perf/test", CancellationToken.None)
                .ToCoroutine();
            stopwatch.Stop();

            // 清理
            UnityEngine.Object.DestroyImmediate(asset);

            // Assert
            float elapsedMs = stopwatch.ElapsedMilliseconds;
            Debug.Log($"Single asset load time: {elapsedMs}ms");
            Assert.Less(
                elapsedMs,
                SINGLE_LOAD_TIME_BUDGET_MS,
                $"Single asset load took {elapsedMs}ms, exceeding budget of {SINGLE_LOAD_TIME_BUDGET_MS}ms"
            );
        }

        [UnityTest]
        [Category("Performance")]
        [Description("测试批量加载速度")]
        public IEnumerator LoadPerformance_BatchAssets_MeetsTimeBudget()
        {
            // Arrange
            const int assetCount = 10;
            var locations = new List<string>();

            for (int i = 0; i < assetCount; i++)
            {
                var asset = new GameObject($"TestAsset_{i}");
                var location = $"perf/asset_{i}";
                _mockStrategy.RegisterAsset(location, asset);
                locations.Add(location);
            }

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            yield return _resourceService
                .LoadBatchAsync<GameObject>(locations, CancellationToken.None)
                .ToCoroutine();
            stopwatch.Stop();

            // 清理
            foreach (var location in locations)
            {
                if (_mockStrategy.LoadedAssets.Contains(location))
                {
                    // 清理资源
                }
            }

            // Assert
            float elapsedMs = stopwatch.ElapsedMilliseconds;
            Debug.Log($"Batch load ({assetCount} assets) time: {elapsedMs}ms");
            Assert.Less(
                elapsedMs,
                BATCH_LOAD_TIME_BUDGET_MS,
                $"Batch load took {elapsedMs}ms, exceeding budget of {BATCH_LOAD_TIME_BUDGET_MS}ms"
            );
        }

        [UnityTest]
        [Category("Performance")]
        [Description("测试并发加载性能")]
        public IEnumerator LoadPerformance_ConcurrentLoads_MeetsTimeBudget()
        {
            // Arrange
            const int concurrentCount = 5;
            var tasks = new List<UniTask<ResHandle<GameObject>>>();

            for (int i = 0; i < concurrentCount; i++)
            {
                var asset = new GameObject($"ConcurrentAsset_{i}");
                _mockStrategy.RegisterAsset($"concurrent/asset_{i}", asset);
            }

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            for (int i = 0; i < concurrentCount; i++)
            {
                tasks.Add(
                    _resourceService.LoadAsync<GameObject>(
                        $"concurrent/asset_{i}",
                        CancellationToken.None
                    )
                );
            }
            yield return UniTask.WhenAll(tasks).ToCoroutine();
            stopwatch.Stop();

            // Assert
            float elapsedMs = stopwatch.ElapsedMilliseconds;
            Debug.Log($"Concurrent load ({concurrentCount} assets) time: {elapsedMs}ms");
            Assert.Less(
                elapsedMs,
                BATCH_LOAD_TIME_BUDGET_MS,
                $"Concurrent load took {elapsedMs}ms, exceeding budget of {BATCH_LOAD_TIME_BUDGET_MS}ms"
            );
        }

        #endregion

        #region 内存测试

        [UnityTest]
        [Category("Performance")]
        [Description("测试加载和释放无内存泄漏")]
        public IEnumerator Memory_LoadAndRelease_NoLeak()
        {
            // Arrange
            long memoryBefore = GC.GetTotalMemory(true);

            // Act - 多次加载和释放
            for (int i = 0; i < 10; i++)
            {
                var asset = new GameObject($"LeakTestAsset_{i}");
                _mockStrategy.RegisterAsset($"leaktest/asset_{i}", asset);

                yield return _resourceService
                    .LoadAsync<GameObject>($"leaktest/asset_{i}", CancellationToken.None)
                    .ToCoroutine();

                _resourceService.Release($"leaktest/asset_{i}", typeof(GameObject));

                UnityEngine.Object.DestroyImmediate(asset);
            }

            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memoryAfter = GC.GetTotalMemory(true);
            long memoryDiff = memoryAfter - memoryBefore;

            // Assert
            Debug.Log($"Memory difference after load/release cycle: {memoryDiff} bytes");
            Assert.Less(
                memoryDiff,
                MAX_GC_ALLOCATION_BYTES,
                $"Memory leak detected: {memoryDiff} bytes"
            );
        }

        [UnityTest]
        [Category("Performance")]
        [Description("测试批量加载内存峰值")]
        public IEnumerator Memory_BatchLoad_PeakMemoryWithinLimit()
        {
            // Arrange
            const int assetCount = 20;
            long memoryBefore = GC.GetTotalMemory(true);

            var locations = new List<string>();
            for (int i = 0; i < assetCount; i++)
            {
                var asset = new GameObject($"BatchAsset_{i}");
                var location = $"batch/asset_{i}";
                _mockStrategy.RegisterAsset(location, asset);
                locations.Add(location);
            }

            // Act
            yield return _resourceService
                .LoadBatchAsync<GameObject>(locations, CancellationToken.None)
                .ToCoroutine();

            long memoryDuring = GC.GetTotalMemory(false);

            // 释放所有资源
            foreach (var location in locations)
            {
                _resourceService.Release(location, typeof(GameObject));
            }

            GC.Collect();
            long memoryAfter = GC.GetTotalMemory(true);

            // Assert
            long peakMemory = memoryDuring - memoryBefore;
            Debug.Log($"Peak memory during batch load: {peakMemory} bytes");
            Assert.Less(
                peakMemory,
                MAX_GC_ALLOCATION_BYTES * 5,
                $"Peak memory {peakMemory} bytes exceeds limit"
            );
        }

        [UnityTest]
        [Category("Performance")]
        [Description("测试缓存命中率")]
        public IEnumerator Memory_CacheEfficiency_HighHitRate()
        {
            // Arrange
            var asset = new GameObject("CachedAsset");
            _mockStrategy.RegisterAsset("cache/test", asset);

            int cacheHits = 0;
            int totalRequests = 10;

            // Act - 第一次加载后，后续都应该命中缓存
            for (int i = 0; i < totalRequests; i++)
            {
                int loadCountBefore = _mockStrategy.LoadedAssets.Count;

                yield return _resourceService
                    .LoadAsync<GameObject>("cache/test", CancellationToken.None)
                    .ToCoroutine();

                int loadCountAfter = _mockStrategy.LoadedAssets.Count;

                if (i > 0 && loadCountAfter == loadCountBefore)
                {
                    cacheHits++;
                }
            }

            // Assert
            float hitRate = (float)cacheHits / (totalRequests - 1);
            Debug.Log($"Cache hit rate: {hitRate:P}");
            Assert.GreaterOrEqual(hitRate, 0.99f, $"Cache hit rate {hitRate:P} is too low");
        }

        #endregion

        #region GC测试

        [UnityTest]
        [Category("Performance")]
        [Description("测试异步加载GC分配")]
        public IEnumerator GC_Allocation_LoadAsync_MinimalAllocation()
        {
            // Arrange
            var asset = new GameObject("GCAsset");
            _mockStrategy.RegisterAsset("gc/test", asset);

            // 预热
            yield return _resourceService
                .LoadAsync<GameObject>("gc/test", CancellationToken.None)
                .ToCoroutine();
            _resourceService.Release("gc/test", typeof(GameObject));

            GC.Collect();
            long memoryBefore = GC.GetTotalMemory(true);

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>("gc/test", CancellationToken.None)
                .ToCoroutine();

            long memoryAfter = GC.GetTotalMemory(false);
            long allocation = memoryAfter - memoryBefore;

            // Assert
            Debug.Log($"GC allocation for LoadAsync: {allocation} bytes");
            Assert.Less(
                allocation,
                100 * 1024, // 100KB阈值
                $"GC allocation {allocation} bytes is too high"
            );
        }

        [UnityTest]
        [Category("Performance")]
        [Description("测试释放无GC分配")]
        public IEnumerator GC_Allocation_Release_NoAllocation()
        {
            // Arrange
            var asset = new GameObject("GCReleaseAsset");
            _mockStrategy.RegisterAsset("gc/release", asset);

            yield return _resourceService
                .LoadAsync<GameObject>("gc/release", CancellationToken.None)
                .ToCoroutine();

            GC.Collect();
            long memoryBefore = GC.GetTotalMemory(true);

            // Act
            _resourceService.Release("gc/release", typeof(GameObject));

            long memoryAfter = GC.GetTotalMemory(false);
            long allocation = memoryAfter - memoryBefore;

            // Assert
            Debug.Log($"GC allocation for Release: {allocation} bytes");
            Assert.Less(
                allocation,
                10 * 1024, // 10KB阈值
                $"GC allocation {allocation} bytes for release is too high"
            );
        }

        [UnityTest]
        [Category("Performance")]
        [Description("测试高频加载稳定性")]
        public IEnumerator GC_Pressure_HighFrequencyLoading_Stable()
        {
            // Arrange
            const int iterations = 100;
            var stopwatch = new Stopwatch();

            for (int i = 0; i < 5; i++)
            {
                var asset = new GameObject($"StressAsset_{i}");
                _mockStrategy.RegisterAsset($"stress/asset_{i}", asset);
            }

            // Act
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                var location = $"stress/asset_{i % 5}";
                yield return _resourceService
                    .LoadAsync<GameObject>(location, CancellationToken.None)
                    .ToCoroutine();
            }
            stopwatch.Stop();

            // Assert
            float avgTime = (float)stopwatch.ElapsedMilliseconds / iterations;
            Debug.Log($"Average load time under pressure: {avgTime}ms");
            Assert.Less(avgTime, 25f, $"Average load time {avgTime}ms is too high under pressure");
        }

        #endregion

        #region 压力测试

        [UnityTest]
        [Category("Performance")]
        [Description("测试大量资源加载性能")]
        public IEnumerator Stress_LargeNumberOfAssets_HandlesCorrectly()
        {
            // Arrange
            const int assetCount = 50;
            var locations = new List<string>();

            for (int i = 0; i < assetCount; i++)
            {
                var asset = new GameObject($"StressAsset_{i}");
                var location = $"stress/asset_{i}";
                _mockStrategy.RegisterAsset(location, asset);
                locations.Add(location);
            }

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            yield return _resourceService
                .LoadBatchAsync<GameObject>(locations, CancellationToken.None)
                .ToCoroutine();
            stopwatch.Stop();

            // Assert
            float elapsedMs = stopwatch.ElapsedMilliseconds;
            float avgTimePerAsset = elapsedMs / assetCount;
            Debug.Log(
                $"Stress test: {assetCount} assets in {elapsedMs}ms, avg {avgTimePerAsset:F2}ms/asset"
            );
            Assert.Less(
                avgTimePerAsset,
                20f,
                $"Average time per asset {avgTimePerAsset:F2}ms is too high"
            );
        }

        [UnityTest]
        [Category("Performance")]
        [Description("测试快速连续加载同一资源")]
        public IEnumerator Stress_RapidSameAssetLoading_HandlesCorrectly()
        {
            // Arrange
            const int rapidRequests = 20;
            var asset = new GameObject("RapidAsset");
            _mockStrategy.RegisterAsset("rapid/test", asset);

            var tasks = new List<UniTask<ResHandle<GameObject>>>();

            // Act - 快速发起多个相同资源的加载请求
            for (int i = 0; i < rapidRequests; i++)
            {
                tasks.Add(
                    _resourceService.LoadAsync<GameObject>("rapid/test", CancellationToken.None)
                );
            }

            yield return UniTask.WhenAll(tasks).ToCoroutine();

            // Assert - 策略应该只被调用一次
            Assert.AreEqual(
                1,
                _mockStrategy.LoadedAssets.Count,
                "Strategy should only load the asset once"
            );
        }

        #endregion
    }
}
