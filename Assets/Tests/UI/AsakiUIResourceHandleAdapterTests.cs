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
            Assert.IsFalse(adapter.IsMarkedForRelease, "初始不应标记为待释放");
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

        #region 标记释放测试

        [Test]
        [Description("MarkForRelease后IsMarkedForRelease应为true")]
        public void MarkForRelease_SetsIsMarkedForReleaseToTrue()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act
            adapter.MarkForRelease();

            // Assert
            Assert.IsTrue(adapter.IsMarkedForRelease, "应标记为待释放");
        }

        [Test]
        [Description("MarkForRelease后IsValid应为false")]
        public void MarkForRelease_IsValidBecomesFalse()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act
            adapter.MarkForRelease();

            // Assert
            Assert.IsFalse(adapter.IsValid, "标记后应无效");
            Assert.IsTrue(adapter.HasResource, "但资源仍应持有");
        }

        [Test]
        [Description("UnmarkForRelease可以取消待释放标记")]
        public void UnmarkForRelease_ClearsMarkedFlag()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            adapter.MarkForRelease();
            Assert.IsTrue(adapter.IsMarkedForRelease, "先确认已标记");

            // Act
            adapter.UnmarkForRelease();

            // Assert
            Assert.IsFalse(adapter.IsMarkedForRelease, "应取消待释放标记");
            Assert.IsTrue(adapter.IsValid, "取消后应恢复有效");
        }

        [Test]
        [Description("多次MarkForRelease状态应保持一致")]
        public void MarkForRelease_MultipleTimes_StateConsistent()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act
            adapter.MarkForRelease();
            adapter.MarkForRelease();
            adapter.MarkForRelease();

            // Assert
            Assert.IsTrue(adapter.IsMarkedForRelease, "应标记为待释放");
            Assert.IsFalse(adapter.IsValid, "应无效");
        }

        [Test]
        [Description("多次UnmarkForRelease状态应保持一致")]
        public void UnmarkForRelease_MultipleTimes_StateConsistent()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            adapter.MarkForRelease();

            // Act
            adapter.UnmarkForRelease();
            adapter.UnmarkForRelease();

            // Assert
            Assert.IsFalse(adapter.IsMarkedForRelease, "应取消待释放标记");
            Assert.IsTrue(adapter.IsValid, "应恢复有效");
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
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "应调用Release");
        }

        [Test]
        [Description("Dispose后IsMarkedForRelease应为false")]
        public void Dispose_ClearsMarkedFlag()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            adapter.MarkForRelease();
            Assert.IsTrue(adapter.IsMarkedForRelease);

            // Act
            adapter.Dispose();

            // Assert
            Assert.IsFalse(adapter.IsMarkedForRelease, "释放后应清除标记");
        }

        [Test]
        [Description("多次Dispose不应抛出异常")]
        public void Dispose_MultipleTimes_DoesNotThrow()
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
        }

        #endregion

        #region 延迟释放场景测试

        [Test]
        [Description("延迟释放流程：标记后取消，资源仍可用")]
        public void DelayReleaseScenario_MarkThenUnmark_ResourceStillAvailable()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act - 模拟延迟释放流程
            adapter.MarkForRelease(); // 关闭窗口，进入延迟释放队列
            Assert.IsFalse(adapter.IsValid, "标记后应无效（不可被新窗口使用）");

            adapter.UnmarkForRelease(); // 快速重新打开，复用资源

            // Assert
            Assert.IsTrue(adapter.IsValid, "取消标记后应恢复有效");
            Assert.IsTrue(adapter.HasResource, "资源仍应持有");
            Assert.AreSame(_testAsset, adapter.Asset, "资源引用应正确");
            Assert.AreEqual(0, _mockResourceService.ReleaseCallCount, "不应调用Release");
        }

        [Test]
        [Description("延迟释放流程：标记后Dispose，资源被释放")]
        public void DelayReleaseScenario_MarkThenDispose_ResourceReleased()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);

            // Act - 模拟延迟释放到期
            adapter.MarkForRelease(); // 标记待释放
            adapter.Dispose(); // 延迟时间到，真正释放

            // Assert
            Assert.IsFalse(adapter.IsValid, "应无效");
            Assert.IsFalse(adapter.HasResource, "不应持有资源");
            Assert.AreEqual(1, _mockResourceService.ReleaseCallCount, "应调用Release");
        }

        #endregion

        #region 边界条件测试

        [Test]
        [Description("对已标记的句柄再次标记状态不变")]
        public void MarkForRelease_AlreadyMarked_StateUnchanged()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            adapter.MarkForRelease();
            bool firstCheck = adapter.IsMarkedForRelease;

            // Act
            adapter.MarkForRelease();

            // Assert
            Assert.AreEqual(firstCheck, adapter.IsMarkedForRelease, "状态应不变");
        }

        [Test]
        [Description("对未标记的句柄取消标记状态不变")]
        public void UnmarkForRelease_NotMarked_StateUnchanged()
        {
            // Arrange
            var adapter = new AsakiUIResourceHandleAdapter(_resHandle);
            Assert.IsFalse(adapter.IsMarkedForRelease);

            // Act
            adapter.UnmarkForRelease();

            // Assert
            Assert.IsFalse(adapter.IsMarkedForRelease, "状态应不变");
            Assert.IsTrue(adapter.IsValid, "仍应有效");
        }

        #endregion
    }
}
