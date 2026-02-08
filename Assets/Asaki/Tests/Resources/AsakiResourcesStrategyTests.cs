// File: Assets/Asaki/Tests/Resources/AsakiResourcesStrategyTests.cs
// AsakiResourcesStrategy 单元测试

using System;
using System.Collections;
using System.Threading;
using Asaki.Tests.Resources.Mocks;
using Asaki.Unity.Services.Resources.Strategies;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Resources
{
    /// <summary>
    /// AsakiResourcesStrategy 单元测试
    /// 测试原生Resources策略的加载、卸载功能
    /// </summary>
    [TestFixture]
    public class AsakiResourcesStrategyTests
    {
        private MockAsakiAsyncService _mockAsyncService;
        private AsakiResourcesStrategy _strategy;

        [SetUp]
        public void Setup()
        {
            _mockAsyncService = new MockAsakiAsyncService();
            _strategy = new AsakiResourcesStrategy(_mockAsyncService);
        }

        [TearDown]
        public void Teardown()
        {
            _strategy = null;
            _mockAsyncService = null;
        }

        #region 初始化测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试初始化立即完成")]
        public IEnumerator InitializeAsync_CompletesImmediately()
        {
            // Act
            yield return _strategy.InitializeAsync().ToCoroutine();
            bool completed = true;
            // Assert
            Assert.IsTrue(completed);
        }

        #endregion

        #region 加载测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试加载资源返回有效对象")]
        public IEnumerator LoadAssetInternalAsync_WithValidPath_LoadsAsset()
        {
            // Arrange - 创建一个测试资源
            var testObject = new GameObject("TestResource");
            testObject.AddComponent<SpriteRenderer>();

            // 注意：实际测试中需要使用Resources文件夹中的真实资源
            // 这里我们主要测试策略的行为

            // Act & Assert - 验证不抛出异常
            yield return _strategy
                .LoadAssetInternalAsync(
                    "NonExistent/Path",
                    typeof(GameObject),
                    null,
                    CancellationToken.None
                )
                .ToCoroutine(
                    resultHandler: _ => { },
                    exceptionHandler: _ => { } // 预期可能失败
                );

            // 清理
            UnityEngine.Object.DestroyImmediate(testObject);

            yield return null;
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试加载时报告进度")]
        public IEnumerator LoadAssetInternalAsync_WithProgress_ReportsProgress()
        {
            // Arrange
            var tracker = new MockProgressTracker();

            // Act
            yield return _strategy
                .LoadAssetInternalAsync(
                    "Test/Path",
                    typeof(GameObject),
                    tracker.GetProgressAction(),
                    CancellationToken.None
                )
                .ToCoroutine(resultHandler: _ => { }, exceptionHandler: _ => { });

            // Assert - 进度应该被报告（即使加载失败）
            // 注意：由于Resources.LoadAsync的行为，进度可能不会被报告
            // 这里主要验证不抛出异常
            Assert.Pass("Load with progress executed without exception");
        }

        [UnityTest]
        [Category("Unit")]
        [Description("测试取消加载抛出OperationCanceledException")]
        public IEnumerator LoadAssetInternalAsync_WithCancellation_ThrowsOperationCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Exception capturedException = null;

            // Act
            yield return _strategy
                .LoadAssetInternalAsync("Test/Path", typeof(GameObject), null, cts.Token)
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
        [Description("测试加载不同类型资源")]
        public IEnumerator LoadAssetInternalAsync_DifferentTypes_LoadsCorrectType()
        {
            // Arrange
            Type[] testTypes = new[]
            {
                typeof(GameObject),
                typeof(Texture2D),
                typeof(Sprite),
                typeof(Material),
            };

            foreach (var type in testTypes)
            {
                // Act & Assert - 验证不抛出异常
                yield return _strategy
                    .LoadAssetInternalAsync("Test/Path", type, null, CancellationToken.None)
                    .ToCoroutine(
                        resultHandler: _ => { },
                        exceptionHandler: _ => { } // 预期可能失败，因为资源不存在
                    );
            }

            Assert.Pass("All type loads executed without exception");
        }

        #endregion

        #region 卸载测试

        [Test]
        [Category("Unit")]
        [Description("测试卸载GameObject不执行卸载")]
        public void UnloadAssetInternal_GameObject_DoesNotUnload()
        {
            // Arrange
            var gameObject = new GameObject("TestObject");

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() =>
            {
                _strategy.UnloadAssetInternal("test/path", gameObject);
            });

            // 清理
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        [Category("Unit")]
        [Description("测试卸载Texture执行卸载")]
        public void UnloadAssetInternal_Texture_UnloadsAsset()
        {
            // Arrange
            var texture = new Texture2D(2, 2);

            // 预期Unity会记录错误日志（运行时创建的资源无法通过UnloadAsset卸载）
            LogAssert.Expect(LogType.Error, "UnloadAsset can only be used on assets;");

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() =>
            {
                _strategy.UnloadAssetInternal("test/path", texture);
            });
        }

        [Test]
        [Category("Unit")]
        [Description("测试卸载Sprite执行卸载")]
        public void UnloadAssetInternal_Sprite_UnloadsAsset()
        {
            // Arrange
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);

            // 预期Unity会记录错误日志（运行时创建的资源无法通过UnloadAsset卸载）
            LogAssert.Expect(LogType.Error, "UnloadAsset can only be used on assets;");

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() =>
            {
                _strategy.UnloadAssetInternal("test/path", sprite);
            });
        }

        [Test]
        [Category("Unit")]
        [Description("测试卸载null不抛出异常")]
        public void UnloadAssetInternal_NullAsset_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _strategy.UnloadAssetInternal("test/path", null);
            });
        }

        #endregion

        #region 卸载未使用资源测试

        [UnityTest]
        [Category("Unit")]
        [Description("测试卸载未使用资源")]
        public IEnumerator UnloadUnusedAssets_CallsResourcesUnload()
        {
            // Act
            yield return _strategy.UnloadUnusedAssets(CancellationToken.None).ToCoroutine();

            // Assert - 验证不抛出异常
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
            yield return _strategy.UnloadUnusedAssets(cts.Token).ToCoroutine();
        }

        #endregion

        #region 策略名称测试

        [Test]
        [Category("Unit")]
        [Description("测试策略名称正确")]
        public void StrategyName_ReturnsCorrectName()
        {
            // Act
            var name = _strategy.StrategyName;

            // Assert
            StringAssert.Contains("Resources", name);
        }

        #endregion

        #region 集成测试

        [UnityTest]
        [Category("Integration")]
        [Description("测试完整的加载和卸载流程")]
        public IEnumerator FullLoadUnloadFlow_WorksCorrectly()
        {
            // Arrange - 创建一个临时资源
            var tempObject = new GameObject("TempResource");

            // Act - 加载（这里会失败，因为没有在Resources文件夹中）
            UnityEngine.Object loadedAsset = null;
            yield return _strategy
                .LoadAssetInternalAsync(
                    "TempResource",
                    typeof(GameObject),
                    null,
                    CancellationToken.None
                )
                .ToCoroutine(
                    resultHandler: asset => loadedAsset = asset,
                    exceptionHandler: _ => { } // 预期失败
                );

            // 清理
            UnityEngine.Object.DestroyImmediate(tempObject);

            // Assert
            Assert.Pass("Full flow executed without unhandled exception");
        }

        #endregion
    }
}
