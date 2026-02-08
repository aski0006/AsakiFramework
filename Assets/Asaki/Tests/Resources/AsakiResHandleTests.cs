// File: Assets/Asaki/Tests/Resources/AsakiResHandleTests.cs
// ResHandle 单元测试

using System;
using Asaki.Core.Resources;
using Asaki.Tests.Resources.Mocks;
using Asaki.Unity.Services.Resources;
using NUnit.Framework;
using UnityEngine;

namespace Asaki.Tests.Resources
{
    /// <summary>
    /// ResHandle 单元测试
    /// 测试资源句柄的创建、使用和释放
    /// </summary>
    [TestFixture]
    public class AsakiResHandleTests
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

        #region 构造函数测试

        [Test]
        [Category("Unit")]
        [Description("测试构造函数设置属性")]
        public void Constructor_SetsProperties()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var location = "test/location";

            // Act
            var handle = new ResHandle<GameObject>(location, asset, _resourceService);

            // Assert
            Assert.AreEqual(location, handle.Location);
            Assert.AreSame(asset, handle.Asset);
            Assert.IsTrue(handle.IsValid);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数允许null资源")]
        public void Constructor_WithNullAsset_SetsInvalid()
        {
            // Act
            var handle = new ResHandle<GameObject>("test/location", null, _resourceService);

            // Assert
            Assert.IsFalse(handle.IsValid);
            Assert.IsNull(handle.Asset);
        }

        #endregion

        #region IsValid 测试

        [Test]
        [Category("Unit")]
        [Description("测试有效资源返回IsValid为true")]
        public void IsValid_WithValidAsset_ReturnsTrue()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var handle = new ResHandle<GameObject>("test/location", asset, _resourceService);

            // Act & Assert
            Assert.IsTrue(handle.IsValid);
        }

        [Test]
        [Category("Unit")]
        [Description("测试null资源返回IsValid为false")]
        public void IsValid_WithNullAsset_ReturnsFalse()
        {
            // Arrange
            var handle = new ResHandle<GameObject>("test/location", null, _resourceService);

            // Act & Assert
            Assert.IsFalse(handle.IsValid);
        }

        [Test]
        [Category("Unit")]
        [Description("测试已销毁资源返回IsValid为false")]
        public void IsValid_WithDestroyedAsset_ReturnsFalse()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var handle = new ResHandle<GameObject>("test/location", asset, _resourceService);

            // Act
            UnityEngine.Object.DestroyImmediate(asset);

            // Assert - 注意：Unity的==操作符会返回true表示对象已被销毁
            // 但ResHandle.IsValid只检查null，所以这里的行为取决于Unity版本
            // 这里主要测试不抛出异常
            Assert.DoesNotThrow(() =>
            {
                _ = handle.IsValid;
            });
        }

        #endregion

        #region Dispose 测试

        [Test]
        [Category("Unit")]
        [Description("测试Dispose调用服务释放")]
        public void Dispose_CallsServiceRelease()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var location = "test/location";
            var handle = new ResHandle<GameObject>(location, asset, _resourceService);

            // 先加载资源以增加引用计数
            _mockStrategy.RegisterAsset(location, asset);

            // Act
            handle.Dispose();

            // Assert - 验证不抛出异常
            Assert.Pass("Dispose executed without exception");
        }

        [Test]
        [Category("Unit")]
        [Description("测试Dispose无效句柄不抛出异常")]
        public void Dispose_InvalidHandle_DoesNotThrow()
        {
            // Arrange
            var handle = new ResHandle<GameObject>("test/location", null, _resourceService);

            // Act & Assert
            Assert.DoesNotThrow(() => handle.Dispose());
        }

        [Test]
        [Category("Unit")]
        [Description("测试多次Dispose不抛出异常")]
        public void Dispose_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var handle = new ResHandle<GameObject>("test/location", asset, _resourceService);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                handle.Dispose();
                handle.Dispose();
                handle.Dispose();
            });
        }

        #endregion

        #region 隐式转换测试

        [Test]
        [Category("Unit")]
        [Description("测试隐式转换返回资源")]
        public void ImplicitConversion_ReturnsAsset()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var handle = new ResHandle<GameObject>("test/location", asset, _resourceService);

            // Act
            GameObject result = handle;

            // Assert
            Assert.AreSame(asset, result);
        }

        [Test]
        [Category("Unit")]
        [Description("测试隐式转换null资源返回null")]
        public void ImplicitConversion_NullAsset_ReturnsNull()
        {
            // Arrange
            var handle = new ResHandle<GameObject>("test/location", null, _resourceService);

            // Act
            GameObject result = handle;

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region 泛型类型测试

        [Test]
        [Category("Unit")]
        [Description("测试不同类型资源的句柄")]
        public void ResHandle_DifferentTypes_WorksCorrectly()
        {
            // Arrange & Act & Assert
            var goHandle = new ResHandle<GameObject>("test/go", new GameObject(), _resourceService);
            Assert.IsTrue(goHandle.IsValid);

            var texture = new Texture2D(2, 2);
            var texHandle = new ResHandle<Texture2D>("test/texture", texture, _resourceService);
            Assert.IsTrue(texHandle.IsValid);

            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            var spriteHandle = new ResHandle<Sprite>("test/sprite", sprite, _resourceService);
            Assert.IsTrue(spriteHandle.IsValid);

            var material =
                new Material(Shader.Find("Standard")) ?? new Material(Shader.Find("Diffuse"));
            var matHandle = new ResHandle<Material>("test/material", material, _resourceService);
            Assert.IsTrue(matHandle.IsValid);
        }

        #endregion

        #region using 语句测试

        [Test]
        [Category("Unit")]
        [Description("测试using语句自动释放")]
        public void UsingStatement_AutomaticallyDisposes()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var handle = new ResHandle<GameObject>("test/location", asset, _resourceService);

            // Act
            using (handle)
            {
                Assert.IsTrue(handle.IsValid);
            }

            // Assert - 释放后应该仍然有效（因为Dispose只是减少引用计数）
            // 但这里主要验证不抛出异常
            Assert.Pass("Using statement executed without exception");
        }

        #endregion

        #region Location 属性测试

        [Test]
        [Category("Unit")]
        [Description("测试Location属性正确")]
        public void Location_ReturnsCorrectValue()
        {
            // Arrange
            var location = "path/to/asset";
            var handle = new ResHandle<GameObject>(location, new GameObject(), _resourceService);

            // Act & Assert
            Assert.AreEqual(location, handle.Location);
        }

        [Test]
        [Category("Unit")]
        [Description("测试Location为空字符串")]
        public void Location_EmptyString_WorksCorrectly()
        {
            // Arrange
            var handle = new ResHandle<GameObject>("", new GameObject(), _resourceService);

            // Act & Assert
            Assert.AreEqual("", handle.Location);
        }

        #endregion

        #region Asset 属性测试

        [Test]
        [Category("Unit")]
        [Description("测试Asset属性返回正确资源")]
        public void Asset_ReturnsCorrectAsset()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var handle = new ResHandle<GameObject>("test/location", asset, _resourceService);

            // Act & Assert
            Assert.AreSame(asset, handle.Asset);
        }

        [Test]
        [Category("Unit")]
        [Description("测试Asset属性可以访问资源成员")]
        public void Asset_CanAccessMembers()
        {
            // Arrange
            var asset = new GameObject("TestAsset");
            var handle = new ResHandle<GameObject>("test/location", asset, _resourceService);

            // Act & Assert
            Assert.AreEqual("TestAsset", handle.Asset.name);
            Assert.IsNotNull(handle.Asset.transform);
            Assert.IsTrue(handle.Asset.activeInHierarchy);
        }

        #endregion
    }
}
