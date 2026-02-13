// File: Assets/Tests/UI/AsakiUIDelayedReleaseTests.cs

using System.Collections;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Core.Simulation;
using Asaki.Core.UI;
using Asaki.Unity.Extensions;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.UI
{
    /// <summary>
    /// UI延迟释放功能集成测试
    /// 测试资源在窗口关闭后延迟释放的机制
    /// </summary>
    [TestFixture]
    [Category("UI")]
    [Category("Integration")]
    public class AsakiUIDelayedReleaseTests
    {
        private MockSimulationService _mockSimulationService;
        private MockResourceService _mockResourceService;
        private MockPoolService _mockPoolService;
        private AsakiUIManageService _uiService;
        private AsakiUIConfig _config;

        [SetUp]
        public void Setup()
        {
            // 清理上下文，确保测试隔离
            AsakiContext.ClearAll();

            _mockSimulationService = new MockSimulationService();
            _mockResourceService = new MockResourceService();
            _mockPoolService = new MockPoolService();

            // 注册模拟服务到上下文
            AsakiContext.Register<IAsakiSimulationService>(_mockSimulationService);
            AsakiContext.Register<IAsakiResourceService>(_mockResourceService);
            AsakiContext.Register<IAsakiPoolService>(_mockPoolService);

            // AsakiUIConfig 是可序列化POCO类，不是ScriptableObject
            _config = new AsakiUIConfig
            {
                ResourceReleaseDelaySeconds = 2f, // 2秒延迟
            };
        }

        [TearDown]
        public void Teardown()
        {
            _uiService?.OnDispose();
            _uiService = null;
            _config = null;
            _mockPoolService = null;
            _mockResourceService = null;
            _mockSimulationService = null;

            // 清理上下文
            AsakiContext.ClearAll();
        }

        #region 延迟释放核心测试

        [UnityTest]
        [Description("关闭窗口后资源应进入延迟释放队列")]
        public IEnumerator CloseWindow_ResourceEntersDelayedReleaseQueue()
        {
            // Arrange
            SetupUIService();
            var mockWindow = CreateMockWindow("UI/TestWindow", false);
            ((IAsakiUIService)_uiService).Close(mockWindow);

            // Act - 模拟一帧
            yield return null;

            // Assert - 此时资源还未释放
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "延迟期间不应释放资源");

            // 模拟足够时间
            _mockSimulationService.SimulateTicks(2.1f);

            // Assert - 资源应该被释放了
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "延迟时间到后应释放资源");
        }

        [UnityTest]
        [Description("快速重新打开窗口应复用延迟释放的资源")]
        public IEnumerator ReopenWindowQuickly_ReusesDelayedResource()
        {
            // Arrange
            SetupUIService();
            const string assetPath = "UI/TestWindow";
            var mockWindow = CreateMockWindow(assetPath, false);

            // Act - 关闭窗口并等待异步操作完成
            yield return CloseWindowAsync(mockWindow).ToCoroutine();

            // Assert - 此时资源不应被释放（延迟中）
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "应无释放");

            // Assert - 检查资源是否未释放（仍在延迟队列中）
            Assert.AreEqual(
                0,
                _mockResourceService.ReleaseCallCount,
                "资源应仍在延迟队列中，未释放"
            );
            bool canReuse = _mockResourceService.HasAsset(assetPath);
            Assert.IsTrue(canReuse, "资源应存在且可被复用");
        }

        [UnityTest]
        [Description("延迟时间到期后资源应被释放")]
        public IEnumerator DelayTimeExpired_ResourceReleased()
        {
            // Arrange
            SetupUIService();
            var mockWindow = CreateMockWindow("UI/TestWindow", false);

            // Act - 关闭窗口并等待异步操作完成
            yield return CloseWindowAsync(mockWindow).ToCoroutine();

            // Assert - 初始未释放
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "初始应无释放");

            // Act - 模拟时间流逝（超过2秒延迟时间）
            _mockSimulationService.SimulateTicks(2.5f);

            // Assert - 资源应被释放
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "延迟时间到后应释放");
        }

        [UnityTest]
        [Description("服务销毁时应立即释放所有延迟释放的资源")]
        public IEnumerator ServiceDispose_ReleasesAllDelayedResources()
        {
            // Arrange
            SetupUIService();
            var window1 = CreateMockWindow("UI/Window1", false);
            var window2 = CreateMockWindow("UI/Window2", false);

            // Act - 关闭窗口并等待异步操作完成
            yield return CloseWindowAsync(window1).ToCoroutine();
            yield return CloseWindowAsync(window2).ToCoroutine();

            // Assert - 尚未释放
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "延迟期间应无释放");

            // Act - 销毁服务
            _uiService.OnDispose();

            // Assert - 所有资源应被立即释放
            Assert.AreEqual(2, _mockResourceService.ReleaseCallCount, "销毁时应释放所有资源");
        }

        #endregion

        #region 配置相关测试

        [UnityTest]
        [Description("ResourceReleaseDelaySeconds为0时应立即释放")]
        public IEnumerator ZeroDelay_ResourceReleasedImmediately()
        {
            // Arrange
            _config.ResourceReleaseDelaySeconds = 0f;
            SetupUIService();
            var mockWindow = CreateMockWindow("UI/TestWindow", false);

            // Act - 关闭窗口并等待异步操作完成
            yield return CloseWindowAsync(mockWindow).ToCoroutine();

            // Assert - 立即释放
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "0延迟应立即释放");
        }

        [UnityTest]
        [Description("不同的延迟时间应正确生效")]
        public IEnumerator DifferentDelayTimes_WorkCorrectly()
        {
            // Arrange - 10秒延迟
            _config.ResourceReleaseDelaySeconds = 10f;
            SetupUIService();
            var mockWindow = CreateMockWindow("UI/TestWindow", false);

            // Act - 关闭窗口并等待异步操作完成
            yield return CloseWindowAsync(mockWindow).ToCoroutine();

            // Act - 模拟5秒（不到10秒延迟）
            _mockSimulationService.SimulateTicks(5f);

            // Assert - 仍不应释放
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "5秒不应释放（延迟10秒）");

            // Act - 再模拟6秒（共11秒，超过10秒延迟）
            _mockSimulationService.SimulateTicks(6f);

            // Assert - 应该释放了
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "11秒应释放");
        }

        #endregion

        #region 池化对象测试

        [UnityTest]
        [Description("池化对象不应进入延迟释放队列")]
        public IEnumerator PooledWindow_DoesNotEnterDelayedRelease()
        {
            // Arrange
            SetupUIService();
            var mockWindow = CreateMockWindow("UI/PooledWindow", true);

            // Act - 关闭池化窗口并等待异步操作完成
            yield return CloseWindowAsync(mockWindow).ToCoroutine();

            // Assert - 池化对象不应触发资源释放
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "池化对象不应触发延迟释放");
        }

        #endregion

        #region 重复释放保护测试

        [UnityTest]
        [Description("快速关闭同一窗口多次不应重复释放资源")]
        public IEnumerator CloseSameWindowMultipleTimes_DoesNotReleaseMultipleTimes()
        {
            // Arrange
            SetupUIService();
            const string assetPath = "UI/TestWindow";
            var mockWindow = CreateMockWindow(assetPath, false);

            // Act - 第一次关闭
            yield return CloseWindowAsync(mockWindow).ToCoroutine();
            Assert.AreEqual(
                0,
                _mockResourceService.ReleaseCallCount,
                "第一次关闭后应无释放（延迟中）"
            );

            // Act - 模拟快速再次关闭同一资源（模拟复用后快速关闭）
            // 创建一个新窗口但使用相同资源路径
            var mockWindow2 = CreateMockWindow(assetPath, false);
            yield return CloseWindowAsync(mockWindow2).ToCoroutine();

            // Assert - 此时应该已经释放了旧的句柄（因为同一资源路径），但只应释放一次
            Assert.AreEqual(
                1,
                _mockResourceService.ReleaseCallCount,
                "快速关闭多次应只释放一次旧资源"
            );

            // Act - 等待延迟时间
            _mockSimulationService.SimulateTicks(2.5f);

            // Assert - 总释放次数应为2（旧的1次 + 新的1次）
            Assert.AreEqual(2, _mockResourceService.ReleaseCallCount, "延迟到期后应释放新资源");
        }

        [UnityTest]
        [Description("同一资源快速重开重关不应导致重复释放")]
        public IEnumerator RapidOpenCloseSameResource_NoDuplicateRelease()
        {
            // Arrange
            SetupUIService();
            const string assetPath = "UI/TestWindow";
            var mockWindow1 = CreateMockWindow(assetPath, false);

            // Act - 第一次关闭（进入延迟队列）
            yield return CloseWindowAsync(mockWindow1).ToCoroutine();
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "第一次关闭后应无释放");

            // Act - 快速创建并关闭第二个窗口（相同资源）
            var mockWindow2 = CreateMockWindow(assetPath, false);
            yield return CloseWindowAsync(mockWindow2).ToCoroutine();

            // Act - 再快速创建并关闭第三个窗口（相同资源）
            var mockWindow3 = CreateMockWindow(assetPath, false);
            yield return CloseWindowAsync(mockWindow3).ToCoroutine();

            // Assert - 由于每次关闭都会释放旧的并添加新的，应该只有2次释放（window1和window2的）
            Assert.AreEqual(
                2,
                _mockResourceService.ReleaseCallCount,
                "三次快速关闭应只释放两次旧资源"
            );

            // Act - 等待延迟时间
            _mockSimulationService.SimulateTicks(2.5f);

            // Assert - 最后应该总共3次释放
            Assert.AreEqual(
                3,
                _mockResourceService.ReleaseCallCount,
                "延迟到期后应释放最后一个资源"
            );
        }

        [UnityTest]
        [Description("不同资源的窗口独立释放")]
        public IEnumerator DifferentResources_ReleaseIndependently()
        {
            // Arrange
            SetupUIService();
            var windowA = CreateMockWindow("UI/WindowA", false);
            var windowB = CreateMockWindow("UI/WindowB", false);

            // Act - 关闭窗口A
            yield return CloseWindowAsync(windowA).ToCoroutine();
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "关闭A后应无释放");

            // Act - 关闭窗口B
            yield return CloseWindowAsync(windowB).ToCoroutine();
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "关闭B后应无释放");

            // Act - 等待延迟时间
            _mockSimulationService.SimulateTicks(2.5f);

            // Assert - 两个资源都应该被释放
            Assert.AreEqual(2, _mockResourceService.ReleaseCallCount, "两个不同资源应分别释放");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 异步关闭窗口并等待完成
        /// </summary>
        private async UniTask CloseWindowAsync(IAsakiWindow window)
        {
            ((IAsakiUIService)_uiService).Close(window);
            // 等待一帧让 Unity 处理
            await UniTask.Yield();
            // 手动驱动 Tick 处理 _pendingDestroyQueue 并执行 HandleCloseAsync
            _mockSimulationService.Tick(0.016f);
            // 再等待一帧让 HandleCloseAsync 中的异步操作完成
            await UniTask.Yield();
        }

        private void SetupUIService()
        {
            _uiService = new AsakiUIManageService(
                _config,
                new Vector2(1920, 1080),
                0.5f,
                null, // eventService
                _mockResourceService,
                _mockPoolService
            );
            _uiService.OnInit();
            _uiService.OnInitAsync().Forget();
        }

        private MockAsakiWindow CreateMockWindow(string assetPath, bool isPooled)
        {
            var go = new GameObject("MockWindow");
            var window = go.AddComponent<MockAsakiWindow>();
            window.Setup(assetPath, isPooled, _mockResourceService);
            return window;
        }

        #endregion
    }
}
