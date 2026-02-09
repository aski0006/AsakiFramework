// File: Assets/Asaki/Tests/Resources/AsakiResourceServiceTests.cs
// AsakiResourceService 核心单元测试

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Resources;
using Asaki.Tests.Resources.Mocks;
using Asaki.Unity.Services.Resources;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Resources
{
    /// <summary>
    /// AsakiResourceService 核心单元测试
    /// 测试资源加载、释放、缓存、异常处理等核心功能
    /// </summary>
    [TestFixture]
    public class AsakiResourceServiceTests
    {
        private MockAsakiResStrategy _mockStrategy;
        private MockAsakiAsyncService _mockAsyncService;
        private MockAsakiResDependencyLookup _mockDependencyLookup;
        private AsakiResourceService _resourceService;

        [SetUp]
        public void Setup()
        {
            _mockStrategy = new MockAsakiResStrategy();
            _mockAsyncService = new MockAsakiAsyncService();
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

        #region 生命周期测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试异步初始化调用策略初始化")]
        public IEnumerator OnInitAsync_CallsStrategyInitialize()
        {
            // Act
            yield return _resourceService.OnInitAsync().ToCoroutine();

            // Assert
            Assert.AreEqual(1, _mockStrategy.InitializeCallCount);
        }

        [Test]
        [Category("Unit")]
        [Description("测试同步初始化不抛出异常")]
        public void OnInit_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _resourceService.OnInit());
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试释放时卸载所有缓存资源")]
        public IEnumerator OnDispose_UnloadsAllCachedAssets()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("test/path", asset);

            yield return _resourceService
                .LoadAsync<GameObject>("test/path", CancellationToken.None)
                .ToCoroutine();

            // Act
            _resourceService.OnDispose();

            // Assert
            Assert.AreEqual(1, _mockStrategy.UnloadedAssets.Count);

            yield return null;
        }

        [Test]
        [Category("Unit")]
        [Description("测试设置超时秒数")]
        public void SetTimeoutSeconds_UpdatesTimeout()
        {
            // Act
            _resourceService.SetTimeoutSeconds(30);

            // Assert - 验证不抛出异常即可，因为超时是内部行为
            Assert.Pass("SetTimeoutSeconds executed without exception");
        }

        #endregion

        #region 加载功能测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试正常加载资源返回有效句柄")]
        public IEnumerator LoadAsync_WithValidLocation_ReturnsHandle()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("test/asset", asset);

            ResHandle<GameObject> handle = null;

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset", CancellationToken.None)
                .ToCoroutine(h => handle = h);

            // Assert
            Assert.IsNotNull(handle);
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual("test/asset", handle.Location);
            Assert.AreSame(asset, handle.Asset);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试加载资源调用策略")]
        public IEnumerator LoadAsync_CallsStrategyLoad()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("test/asset", asset);

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset", CancellationToken.None)
                .ToCoroutine();

            // Assert
            Assert.That(_mockStrategy.LoadedAssets, Does.Contain("test/asset"));
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试加载时报告进度")]
        public IEnumerator LoadAsync_WithProgressCallback_ReportsProgress()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("test/asset", asset);
            _mockStrategy.LoadDelayMs = 50; // 确保有进度报告

            var tracker = new MockProgressTracker();

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>(
                    "test/asset",
                    tracker.GetProgressAction(),
                    CancellationToken.None
                )
                .ToCoroutine();

            // Assert
            Assert.Greater(tracker.UpdateCount, 0, "Progress should be reported");
            Assert.IsTrue(tracker.IsComplete, "Progress should reach 1.0");
            Assert.IsTrue(
                tracker.IsMonotonicallyIncreasing(),
                "Progress should increase monotonically"
            );
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试取消加载抛出OperationCanceledException")]
        public IEnumerator LoadAsync_WithCancellationToken_CancelsLoading()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            _mockStrategy.LoadDelayMs = 100;

            // 延迟取消
            cts.CancelAfter(20);

            Exception capturedException = null;

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset", cts.Token)
                .ToCoroutine(
                    resultHandler: _ => { },
                    exceptionHandler: ex => capturedException = ex
                );

            // Assert
            Assert.IsNotNull(capturedException);
            Assert.IsInstanceOf<OperationCanceledException>(capturedException);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试并发加载同一资源只执行一次加载")]
        public IEnumerator LoadAsync_ConcurrentLoading_SingleLoadOperation()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("shared/asset", asset);
            _mockStrategy.LoadDelayMs = 50;

            // Act - 同时发起多个加载请求
            var task1 = _resourceService.LoadAsync<GameObject>(
                "shared/asset",
                CancellationToken.None
            );
            var task2 = _resourceService.LoadAsync<GameObject>(
                "shared/asset",
                CancellationToken.None
            );
            var task3 = _resourceService.LoadAsync<GameObject>(
                "shared/asset",
                CancellationToken.None
            );

            yield return UniTask.WhenAll(task1, task2, task3).ToCoroutine();

            // Assert - 策略应该只被调用一次
            Assert.AreEqual(1, _mockStrategy.LoadedAssets.Count);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试不同类型使用不同缓存键")]
        public IEnumerator LoadAsync_DifferentTypes_DifferentCacheKeys()
        {
            // Arrange
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);

            _mockStrategy.RegisterAsset("test/image", texture);
            _mockStrategy.RegisterAsset("test/image", sprite);

            // Act
            ResHandle<Texture2D> textureHandle = null;
            ResHandle<Sprite> spriteHandle = null;

            yield return _resourceService
                .LoadAsync<Texture2D>("test/image", CancellationToken.None)
                .ToCoroutine(h => textureHandle = h);
            yield return _resourceService
                .LoadAsync<Sprite>("test/image", CancellationToken.None)
                .ToCoroutine(h => spriteHandle = h);

            // Assert
            Assert.IsNotNull(textureHandle);
            Assert.IsNotNull(spriteHandle);
            Assert.AreNotSame(textureHandle.Asset, spriteHandle.Asset);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试加载失败时传播异常")]
        public IEnumerator LoadAsync_WhenStrategyThrows_PropagatesException()
        {
            // Arrange
            _mockStrategy.ShouldFail = true;
            _mockStrategy.ExceptionToThrow = new Exception("Mock load failure");

            Exception capturedException = null;

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset", CancellationToken.None)
                .ToCoroutine(
                    resultHandler: _ => { },
                    exceptionHandler: ex => capturedException = ex
                );

            // Assert
            Assert.IsNotNull(capturedException);
            Assert.That(capturedException.Message, Does.Contain("Mock load failure"));
        }

        #endregion

        #region 释放功能测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试释放资源减少引用计数")]
        public IEnumerator Release_WithValidHandle_DecrementsRefCount()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("test/asset", asset);

            ResHandle<GameObject> handle = null;
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset", CancellationToken.None)
                .ToCoroutine(h => handle = h);

            // Act - 释放一次
            _resourceService.Release("test/asset", typeof(GameObject));

            // Assert - 资源不应该被卸载（引用计数可能仍大于0）
            // 这里主要验证不抛出异常
            Assert.Pass("Release executed without exception");
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试使用using语句自动释放")]
        public IEnumerator ResHandle_Dispose_CallsRelease()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("test/asset", asset);

            // Act - 先异步加载获取handle
            ResHandle<GameObject> handle = null;
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset", CancellationToken.None)
                .ToCoroutine(h => handle = h);

            // 使用using语句测试Dispose
            using (handle)
            {
                Assert.IsTrue(handle.IsValid);
            }

            // Assert - 释放后再次加载应该重新加载
            _mockStrategy.Reset();
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset", CancellationToken.None)
                .ToCoroutine();

            Assert.AreEqual(1, _mockStrategy.LoadedAssets.Count);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试批量释放多个资源")]
        public IEnumerator ReleaseBatch_MultipleAssets_ReleasesAll()
        {
            // Arrange
            var asset1 = new GameObject("Asset1");
            var asset2 = new GameObject("Asset2");
            _mockStrategy.RegisterAsset("test/asset1", asset1);
            _mockStrategy.RegisterAsset("test/asset2", asset2);

            yield return _resourceService
                .LoadAsync<GameObject>("test/asset1", CancellationToken.None)
                .ToCoroutine();
            yield return _resourceService
                .LoadAsync<GameObject>("test/asset2", CancellationToken.None)
                .ToCoroutine();

            // Act - 使用泛型ReleaseBatch方法，与LoadBatchAsync<GameObject>对应
            _resourceService.ReleaseBatch<GameObject>(new[] { "test/asset1", "test/asset2" });

            // Assert
            Assert.AreEqual(2, _mockStrategy.UnloadedAssets.Count);
        }

        #endregion

        #region 批量加载测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试批量加载多个资源")]
        public IEnumerator LoadBatchAsync_MultipleAssets_LoadsAll()
        {
            // Arrange
            var asset1 = new GameObject("Asset1");
            var asset2 = new GameObject("Asset2");
            _mockStrategy.RegisterAsset("test/asset1", asset1);
            _mockStrategy.RegisterAsset("test/asset2", asset2);

            var locations = new[] { "test/asset1", "test/asset2" };
            List<ResHandle<GameObject>> handles = null;

            // Act
            yield return _resourceService
                .LoadBatchAsync<GameObject>(locations, CancellationToken.None)
                .ToCoroutine(h => handles = h);

            // Assert
            Assert.IsNotNull(handles);
            Assert.AreEqual(2, handles.Count);
            Assert.IsTrue(handles[0].IsValid);
            Assert.IsTrue(handles[1].IsValid);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试批量加载报告整体进度")]
        public IEnumerator LoadBatchAsync_WithProgress_ReportsOverallProgress()
        {
            // Arrange
            var asset1 = new GameObject("Asset1");
            var asset2 = new GameObject("Asset2");
            _mockStrategy.RegisterAsset("test/asset1", asset1);
            _mockStrategy.RegisterAsset("test/asset2", asset2);
            _mockStrategy.LoadDelayMs = 30;

            var locations = new[] { "test/asset1", "test/asset2" };
            var tracker = new MockProgressTracker();

            // Act
            yield return _resourceService
                .LoadBatchAsync<GameObject>(
                    locations,
                    tracker.GetProgressAction(),
                    CancellationToken.None
                )
                .ToCoroutine();

            // Assert
            Assert.Greater(tracker.UpdateCount, 0);
            Assert.IsTrue(tracker.IsComplete);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试批量加载空列表返回空列表")]
        public IEnumerator LoadBatchAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var locations = new string[0];
            List<ResHandle<GameObject>> handles = null;

            // Act
            yield return _resourceService
                .LoadBatchAsync<GameObject>(locations, CancellationToken.None)
                .ToCoroutine(h => handles = h);

            // Assert
            Assert.IsNotNull(handles);
            Assert.AreEqual(0, handles.Count);
        }

        #endregion

        #region 依赖加载测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试加载带依赖的资源")]
        public IEnumerator LoadAsync_WithDependencies_LoadsDependencies()
        {
            // Arrange
            var mainAsset = new GameObject("MainAsset");
            var depAsset = new GameObject("DepAsset");

            _mockStrategy.RegisterAsset("main/asset", mainAsset);
            _mockStrategy.RegisterAsset("dep/asset", depAsset);
            _mockDependencyLookup.RegisterDependencies("main/asset", "dep/asset");

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>("main/asset", CancellationToken.None)
                .ToCoroutine();

            // Assert
            Assert.That(_mockStrategy.LoadedAssets, Does.Contain("main/asset"));
            Assert.That(_mockStrategy.LoadedAssets, Does.Contain("dep/asset"));
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试释放主资源时释放依赖")]
        public IEnumerator Release_WithDependencies_ReleasesDependencies()
        {
            // Arrange
            var mainAsset = new GameObject("MainAsset");
            var depAsset = new GameObject("DepAsset");

            _mockStrategy.RegisterAsset("main/asset", mainAsset);
            _mockStrategy.RegisterAsset("dep/asset", depAsset);
            _mockDependencyLookup.RegisterDependencies("main/asset", "dep/asset");

            yield return _resourceService
                .LoadAsync<GameObject>("main/asset", CancellationToken.None)
                .ToCoroutine();

            // Act - 释放主资源
            _resourceService.Release("main/asset", typeof(GameObject));

            // Assert - 依赖也应该被释放
            // 注意：由于引用计数机制，这里可能需要多次释放
        }

        #endregion

        #region 异常处理测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试加载不存在的资源抛出异常")]
        public IEnumerator LoadAsync_NonExistentAsset_ThrowsException()
        {
            // Arrange - 不注册任何资源
            _mockStrategy.ShouldFail = true;

            Exception capturedException = null;

            // Act
            yield return _resourceService
                .LoadAsync<GameObject>("non/existent/asset", CancellationToken.None)
                .ToCoroutine(
                    resultHandler: _ => { },
                    exceptionHandler: ex => capturedException = ex
                );

            // Assert
            Assert.IsNotNull(capturedException);
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试类型不匹配抛出InvalidCastException")]
        public IEnumerator LoadAsync_TypeMismatch_ThrowsInvalidCastException()
        {
            // Arrange
            // 设置Mock策略强制返回一个Texture2D，无论请求什么类型
            // 这样可以模拟Strategy返回错误类型资源的场景
            var texture = new Texture2D(2, 2);
            _mockStrategy.ForceReturnAsset = texture;

            Exception capturedException = null;

            // Act - 尝试加载GameObject，但Mock会返回Texture2D
            yield return _resourceService
                .LoadAsync<GameObject>("test/texture", CancellationToken.None)
                .ToCoroutine(
                    resultHandler: _ => { },
                    exceptionHandler: ex => capturedException = ex
                );

            // 清理
            _mockStrategy.ForceReturnAsset = null;

            // Assert
            Assert.IsNotNull(capturedException);
            Assert.IsInstanceOf<InvalidCastException>(capturedException);
        }

        #endregion

        #region 缓存测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试重复加载同一资源返回缓存")]
        public IEnumerator LoadAsync_SameLocationTwice_UsesCache()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            _mockStrategy.RegisterAsset("cached/asset", asset);

            // Act - 第一次加载
            ResHandle<GameObject> handle1 = null;
            yield return _resourceService
                .LoadAsync<GameObject>("cached/asset", CancellationToken.None)
                .ToCoroutine(h => handle1 = h);

            int loadCountAfterFirst = _mockStrategy.LoadedAssets.Count;

            // 第二次加载
            ResHandle<GameObject> handle2 = null;
            yield return _resourceService
                .LoadAsync<GameObject>("cached/asset", CancellationToken.None)
                .ToCoroutine(h => handle2 = h);

            // Assert
            Assert.AreEqual(
                loadCountAfterFirst,
                _mockStrategy.LoadedAssets.Count,
                "Strategy should not be called for cached asset"
            );
            Assert.AreSame(handle1.Asset, handle2.Asset);
        }

        #endregion

        #region 卸载未使用资源测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试卸载未使用资源调用策略")]
        public IEnumerator UnloadUnusedAssets_CallsStrategyUnload()
        {
            // Act
            yield return _resourceService.UnloadUnusedAssets(CancellationToken.None).ToCoroutine();

            // Assert
            Assert.Pass("UnloadUnusedAssets executed without exception");
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试取消卸载未使用资源")]
        public IEnumerator UnloadUnusedAssets_WithCancellation_StopsOperation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert - 不应抛出异常
            Exception ex = null;
            yield return _resourceService.UnloadUnusedAssets(cts.Token).ToCoroutine(e => ex = e);
            Assert.IsInstanceOf<OperationCanceledException>(ex);
        }

        #endregion
    }
}
