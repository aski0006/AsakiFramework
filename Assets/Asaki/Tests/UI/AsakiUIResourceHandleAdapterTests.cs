// File: Assets/Tests/UI/AsakiUIResourceHandleAdapterTests.cs

using System;
using Asaki.Core.Resources;
using Asaki.Core.UI;
using NUnit.Framework;
using UnityEngine;

namespace Asaki.Tests.UI
{
    /// <summary>
    /// UI资源句柄适配器单元测试
    /// 测试资源句柄的延迟释放功能
    /// </summary>
    [TestFixture]
    [Category("UI")]
    [Category("Unit")]
    public class AsakiUIResourceHandleAdapterTests
    {
        private MockResourceService _mockResourceService;
        private GameObject _testAsset;
        private ResHandle<GameObject> _resHandle;

        [SetUp]
        public void Setup()
        {
            _mockResourceService = new MockResourceService();
            _testAsset = new GameObject("TestUIAsset");
            _resHandle = new ResHandle<GameObject>("Test/Path/Asset", _testAsset, _mockResourceService);
        }

        [TearDown]
        public void Teardown()
        {
            if (_testAsset != null)
            {
                UnityEngine.Object.DestroyImmediate(_testAsset);
                _testAsset = null;
            }
            _resHandle?.Dispose();
            _resHandle = null;
            _mockResourceService = null;
        }

        #region 基础功能测试

        [Test]
        [Description("构造函数应正确初始化句柄")]
        public void Constructor_InitializesHandleCorrectly()
        {
            // Act
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Assert
            Assert.IsTrue(adapter.IsValid, "新创建的句柄应有效");
            Assert.IsTrue(adapter.HasResource, "新创建的句柄应持有资源");
            Assert.AreEqual("Test/Path/Asset", adapter.Location, "路径应正确");
            Assert.AreSame(_testAsset, adapter.Asset, "资源引用应正确");
            Assert.IsFalse(adapter.IsDisposed, "初始不应被释放");
        }

        [Test]
        [Description("使用null句柄构造时IsValid应为false")]
        public void Constructor_WithNullHandle_IsValidReturnsFalse()
        {
            // Act
            var adapter = new AsakiUIResourceHandleAdapter(null);

            // Assert
            Assert.IsFalse(adapter.IsValid, "null句柄应无效");
            Assert.IsFalse(adapter.HasResource, "null句柄不应持有资源");
            Assert.IsNull(adapter.Asset, "null句柄的资源应为null");
        }

        #endregion

        #region Dispose测试

        [Test]
        [Description("Dispose后资源应被释放")]
        public void Dispose_ReleasesResource()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act
            adapter.Dispose();

            // Assert
            Assert.IsFalse(adapter.IsValid, "释放后应无效");
            Assert.IsFalse(adapter.HasResource, "释放后不应持有资源");
            Assert.IsNull(adapter.Asset, "释放后资源应为null");
            Assert.IsTrue(adapter.IsDisposed, "应标记为已释放");
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "应调用Release");
        }

        [Test]
        [Description("多次Dispose不应抛出异常且只释放一次")]
        public void Dispose_MultipleTimes_DoesNotThrowAndReleasesOnce()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                adapter.Dispose();
                adapter.Dispose();
                adapter.Dispose();
            });

            // Assert - 只应释放一次
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "多次Dispose应只释放一次");
            Assert.IsTrue(adapter.IsDisposed, "应标记为已释放");
        }

        [Test]
        [Description("Dispose后IsDisposed应为true")]
        public void Dispose_SetsIsDisposedToTrue()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            Assert.IsFalse(adapter.IsDisposed);

            // Act
            adapter.Dispose();

            // Assert
            Assert.IsTrue(adapter.IsDisposed, "释放后应标记为已释放");
        }

        #endregion

        #region 重复释放保护测试

        [Test]
        [Description("重复Dispose不应重复减少引用计数")]
        public void MultipleDispose_ReferenceCountNotDecreasedMultipleTimes()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act
            adapter.Dispose();
            int countAfterFirstDispose = _mockResourceService.ReleaseCallCount;
            
            adapter.Dispose();
            adapter.Dispose();
            int countAfterMultipleDispose = _mockResourceService.ReleaseCallCount;

            // Assert
            Assert.AreEqual(1, countAfterFirstDispose, "第一次Dispose应释放资源");
            Assert.AreEqual(1, countAfterMultipleDispose, "多次Dispose不应重复释放");
        }

        [Test]
        [Description("已释放的句柄IsValid和HasResource都应为false")]
        public void DisposedHandle_IsValidAndHasResourceAreFalse()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            adapter.Dispose();

            // Assert
            Assert.IsFalse(adapter.IsValid, "已释放句柄应无效");
            Assert.IsFalse(adapter.HasResource, "已释放句柄不应持有资源");
            Assert.IsTrue(adapter.IsDisposed, "应标记为已释放");
        }

        #endregion

        #region 延迟释放场景测试

        [Test]
        [Description("延迟释放流程：Dispose后资源被释放")]
        public void DelayReleaseScenario_Dispose_ResourceReleased()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act - 模拟延迟释放到期
            adapter.Dispose();

            // Assert
            Assert.IsFalse(adapter.IsValid, "应无效");
            Assert.IsFalse(adapter.HasResource, "不应持有资源");
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "应调用Release");
        }

        [Test]
        [Description("延迟释放流程：复用后Dispose，资源被正确释放")]
        public void DelayReleaseScenario_ReuseThenDispose_ResourceReleased()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act - 模拟复用后释放
            // （复用逻辑由UI服务管理，这里只测试句柄本身行为）
            adapter.Dispose();

            // Assert
            Assert.IsFalse(adapter.IsValid, "应无效");
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "应调用Release一次");
        }

        #endregion

        #region 边界条件测试

        [Test]
        [Description("对已释放的句柄再次释放不会抛出异常")]
        public void Dispose_AlreadyDisposed_DoesNotThrow()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            adapter.Dispose();

            // Act & Assert
            Assert.DoesNotThrow(() => adapter.Dispose(), "对已释放句柄再次释放不应抛出异常");
        }

        [Test]
        [Description("null句柄Dispose不会抛出异常")]
        public void Dispose_NullHandle_DoesNotThrow()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(null);

            // Act & Assert
            Assert.DoesNotThrow(() => adapter.Dispose(), "null句柄Dispose不应抛出异常");
        }

        #endregion
    }
}
