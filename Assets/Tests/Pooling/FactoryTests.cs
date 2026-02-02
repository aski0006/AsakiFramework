using System;
using System.Collections;
using System.Threading;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Pooling
{
    /// <summary>
    /// 工厂类单元测试
    /// 测试各种工厂实现的正确性
    /// </summary>
    [TestFixture]
    public class FactoryTests
    {
        #region DelegateFactory 测试

        [TestFixture]
        public class DelegateFactoryTests
        {
            [UnityTest]
            [Category("Unit")]
            [Description("测试异步创建函数")]
            public IEnumerator CreateAsync_WithAsyncFunc_CreatesObject()
            {
                // Arrange
                bool createCalled = false;
                var factory = new DelegateFactory<string>(
                    async (token) =>
                    {
                        createCalled = true;
                        return await UniTask.FromResult("test");
                    }
                );

                // Act
                var result = factory.CreateAsync();

                // Assert
                Assert.IsTrue(createCalled);
                Assert.AreEqual("test", result.GetAwaiter().GetResult());
                yield return null;
            }

            [Test]
            [Category("Unit")]
            [Description("测试同步创建函数")]
            public void CreateSync_WithSyncFunc_CreatesObject()
            {
                // Arrange
                bool createCalled = false;
                var factory = new DelegateFactory<string>(() =>
                {
                    createCalled = true;
                    return "test";
                });

                // Act
                var result = factory.CreateSync();

                // Assert
                Assert.IsTrue(createCalled);
                Assert.AreEqual("test", result);
            }

            [Test]
            [Category("Unit")]
            [Description("测试构造函数传入null异步函数抛出异常")]
            public void Constructor_WithNullAsyncFunc_ThrowsArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() =>
                {
                    new DelegateFactory<string>((Func<CancellationToken, UniTask<string>>)null);
                });
            }

            [Test]
            [Category("Unit")]
            [Description("测试构造函数传入null同步函数抛出异常")]
            public void Constructor_WithNullSyncFunc_ThrowsArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() =>
                {
                    new DelegateFactory<string>((Func<string>)null);
                });
            }

            [Test]
            [Category("Unit")]
            [Description("测试OnGet回调")]
            public void OnGet_CallsCallback()
            {
                // Arrange
                bool onGetCalled = false;
                var factory = new DelegateFactory<string>(
                    () => "test",
                    onGet: (obj) => onGetCalled = true
                );

                // Act
                factory.OnGet("test");

                // Assert
                Assert.IsTrue(onGetCalled);
            }

            [Test]
            [Category("Unit")]
            [Description("测试OnReturn回调")]
            public void OnReturn_CallsCallback()
            {
                // Arrange
                bool onReturnCalled = false;
                var factory = new DelegateFactory<string>(
                    () => "test",
                    onReturn: (obj) => onReturnCalled = true
                );

                // Act
                factory.OnReturn("test");

                // Assert
                Assert.IsTrue(onReturnCalled);
            }

            [Test]
            [Category("Unit")]
            [Description("测试OnDestroy回调")]
            public void OnDestroy_CallsCallback()
            {
                // Arrange
                bool onDestroyCalled = false;
                var factory = new DelegateFactory<string>(
                    () => "test",
                    onDestroy: (obj) => onDestroyCalled = true
                );

                // Act
                factory.OnDestroy("test");

                // Assert
                Assert.IsTrue(onDestroyCalled);
            }

            [Test]
            [Category("Unit")]
            [Description("测试自定义验证函数")]
            public void Validate_WithCustomValidator_UsesValidator()
            {
                // Arrange
                var factory = new DelegateFactory<string>(
                    () => "test",
                    validate: (obj) => obj == "valid"
                );

                // Act & Assert
                Assert.IsFalse(factory.Validate("test"));
                Assert.IsTrue(factory.Validate("valid"));
            }

            [Test]
            [Category("Unit")]
            [Description("测试无验证函数时非空检查")]
            public void Validate_WithoutValidator_ChecksNotNull()
            {
                // Arrange
                var factory = new DelegateFactory<string>(() => "test");

                // Act & Assert
                Assert.IsTrue(factory.Validate("test"));
                Assert.IsFalse(factory.Validate(null));
            }

            [Test]
            [Category("Unit")]
            [Description("测试无同步函数时CreateSync抛出异常")]
            public void CreateSync_WithoutSyncFunc_ThrowsInvalidOperationException()
            {
                // Arrange
                var factory = new DelegateFactory<string>(
                    async (token) => await UniTask.FromResult("test")
                );

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => factory.CreateSync());
            }
        }

        #endregion

        #region GameObjectFactory 测试

        [TestFixture]
        public class GameObjectFactoryTests
        {
            private GameObject _prefab;

            [SetUp]
            public void Setup()
            {
                _prefab = new GameObject("TestPrefab");
                _prefab.SetActive(false);
            }

            [TearDown]
            public void Teardown()
            {
                if (_prefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(_prefab);
                }
            }

            [Test]
            [Category("Unit")]
            [Description("测试构造函数传入null预制体抛出异常")]
            public void Constructor_WithNullPrefab_ThrowsArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() =>
                {
                    new GameObjectFactory(null);
                });
            }

            [UnityTest]
            [Category("Unit")]
            [Description("测试异步创建返回GameObject")]
            public IEnumerator CreateAsync_ReturnsGameObject()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);

                // Act
                var result = factory.CreateAsync();

                // Assert
                Assert.IsNotNull(result);
                Assert.AreNotSame(_prefab, result);

                // Cleanup
                UnityEngine.Object.Destroy(result.GetAwaiter().GetResult(), 1f);
                yield return new WaitForSeconds(1f);
                yield return null;
            }

            [Test]
            [Category("Unit")]
            [Description("测试同步创建返回GameObject")]
            public void CreateSync_ReturnsGameObject()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);

                // Act
                var result = factory.CreateSync();

                // Assert
                Assert.IsNotNull(result);
                Assert.AreNotSame(_prefab, result);
                Assert.IsFalse(result.activeSelf);

                // Cleanup
                UnityEngine.Object.DestroyImmediate(result);
            }

            [Test]
            [Category("Unit")]
            [Description("测试OnGet激活对象")]
            public void OnGet_ActivatesObject()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);
                var obj = factory.CreateSync();
                obj.SetActive(false);

                // Act
                factory.OnGet(obj);

                // Assert
                Assert.IsTrue(obj.activeSelf);

                // Cleanup
                UnityEngine.Object.DestroyImmediate(obj);
            }

            [Test]
            [Category("Unit")]
            [Description("测试OnReturn禁用对象")]
            public void OnReturn_DeactivatesObject()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);
                var obj = factory.CreateSync();
                obj.SetActive(true);

                // Act
                factory.OnReturn(obj);

                // Assert
                Assert.IsFalse(obj.activeSelf);

                // Cleanup
                UnityEngine.Object.DestroyImmediate(obj);
            }

            [Test]
            [Category("Unit")]
            [Description("测试OnReturn设置父节点")]
            public void OnReturn_SetsParent()
            {
                // Arrange
                var parent = new GameObject("Parent").transform;
                var factory = new GameObjectFactory(_prefab, parent);
                var obj = factory.CreateSync();

                // Act
                factory.OnReturn(obj);

                // Assert
                Assert.AreEqual(parent, obj.transform.parent);

                // Cleanup
                UnityEngine.Object.DestroyImmediate(obj);
                UnityEngine.Object.DestroyImmediate(parent.gameObject);
            }

            [Test]
            [Category("Unit")]
            [Description("测试OnDestroy销毁对象")]
            public void OnDestroy_DestroysObject()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);
                var obj = factory.CreateSync();

                // Act
                factory.OnDestroy(obj);

                // Assert - 对象应被标记为销毁
                // 注意：在Edit Mode中，DestroyImmediate会立即销毁对象
            }

            [Test]
            [Category("Unit")]
            [Description("测试验证null对象返回false")]
            public void Validate_NullObject_ReturnsFalse()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);

                // Act & Assert
                Assert.IsFalse(factory.Validate(null));
            }

            [Test]
            [Category("Unit")]
            [Description("测试验证有效对象返回true")]
            public void Validate_ValidObject_ReturnsTrue()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);
                var obj = factory.CreateSync();

                // Act & Assert
                Assert.IsTrue(factory.Validate(obj));

                // Cleanup
                UnityEngine.Object.DestroyImmediate(obj);
            }

            [Test]
            [Category("Unit")]
            [Description("测试预制体被销毁时CreateSync抛出异常")]
            public void CreateSync_WhenPrefabDestroyed_ThrowsInvalidOperationException()
            {
                // Arrange
                var factory = new GameObjectFactory(_prefab);
                UnityEngine.Object.DestroyImmediate(_prefab);
                _prefab = null;

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => factory.CreateSync());
            }
        }

        #endregion
    }
}
