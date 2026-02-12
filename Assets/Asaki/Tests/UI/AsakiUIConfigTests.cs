// File: Assets/Tests/UI/AsakiUIConfigTests.cs

using System.Collections.Generic;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.UI;
using NUnit.Framework;
using UnityEngine;

namespace Asaki.Tests.UI
{
    /// <summary>
    /// UI配置单元测试
    /// 测试配置结构、查找表初始化、资源延迟释放配置等
    /// </summary>
    [TestFixture]
    [Category("UI")]
    [Category("Unit")]
    public class AsakiUIConfigTests
    {
        private AsakiUIConfig _config;

        [SetUp]
        public void Setup()
        {
            // AsakiUIConfig 是可序列化POCO类，不是ScriptableObject
            _config = new AsakiUIConfig();
        }

        [TearDown]
        public void Teardown()
        {
            _config = null;
        }

        #region 默认值测试

        [Test]
        [Description("参考分辨率默认应为1920x1080")]
        public void DefaultReferenceResolution_Is1920x1080()
        {
            // Assert
            Assert.AreEqual(
                new Vector2(1920, 1080),
                _config.ReferenceResolution,
                "默认参考分辨率应为1920x1080"
            );
        }

        [Test]
        [Description("MatchWidthOrHeight默认值应为0.5")]
        public void DefaultMatchWidthOrHeight_Is0_5()
        {
            // Assert
            Assert.AreEqual(0.5f, _config.MatchWidthOrHeight, "默认适配比例应为0.5");
        }

        [Test]
        [Description("ResourceReleaseDelaySeconds默认值应为5秒")]
        public void DefaultResourceReleaseDelaySeconds_Is5()
        {
            // Assert
            Assert.AreEqual(5f, _config.ResourceReleaseDelaySeconds, "默认资源释放延迟应为5秒");
        }

        [Test]
        [Description("UI列表初始应为空")]
        public void DefaultUIList_IsEmpty()
        {
            // Assert
            Assert.IsNotNull(_config.UIList, "UI列表不应为null");
            Assert.AreEqual(0, _config.UIList.Count, "UI列表初始应为空");
        }

        [Test]
        [Description("模板列表初始应为空")]
        public void DefaultTemplates_IsEmpty()
        {
            // Assert
            Assert.IsNotNull(_config.Templates, "模板列表不应为null");
            Assert.AreEqual(0, _config.Templates.Count, "模板列表初始应为空");
        }

        #endregion

        #region 查找表测试

        [Test]
        [Description("InitializeLookup应正确构建查找表")]
        public void InitializeLookup_BuildsLookupCorrectly()
        {
            // Arrange
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 1,
                    Name = "TestUI1",
                    AssetPath = "Path/1",
                }
            );
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 2,
                    Name = "TestUI2",
                    AssetPath = "Path/2",
                }
            );
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 3,
                    Name = "TestUI3",
                    AssetPath = "Path/3",
                }
            );

            // Act
            _config.InitializeLookup();

            // Assert
            Assert.IsTrue(_config.TryGet(1, out UIInfo info1), "应能找到ID=1");
            Assert.AreEqual("TestUI1", info1.Name);

            Assert.IsTrue(_config.TryGet(2, out UIInfo info2), "应能找到ID=2");
            Assert.AreEqual("TestUI2", info2.Name);

            Assert.IsTrue(_config.TryGet(3, out UIInfo info3), "应能找到ID=3");
            Assert.AreEqual("TestUI3", info3.Name);
        }

        [Test]
        [Description("重复调用InitializeLookup不会重复构建")]
        public void InitializeLookup_CalledTwice_DoesNotDuplicate()
        {
            // Arrange
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 1,
                    Name = "TestUI",
                    AssetPath = "Path/1",
                }
            );
            _config.InitializeLookup();

            // Act - 再次调用不应抛出异常
            Assert.DoesNotThrow(() => _config.InitializeLookup());

            // Assert
            Assert.IsTrue(_config.TryGet(1, out UIInfo info));
            Assert.AreEqual("TestUI", info.Name);
        }

        [Test]
        [Description("TryGet不存在的ID应返回false")]
        public void TryGet_NonExistentId_ReturnsFalse()
        {
            // Arrange
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 1,
                    Name = "TestUI",
                    AssetPath = "Path/1",
                }
            );
            _config.InitializeLookup();

            // Act & Assert
            Assert.IsFalse(_config.TryGet(999, out UIInfo info), "不存在的ID应返回false");
            Assert.AreEqual(default(UIInfo), info, "out参数应为默认值");
        }

        [Test]
        [Description("TryGet未初始化时应自动初始化")]
        public void TryGet_NotInitialized_AutoInitializes()
        {
            // Arrange
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 1,
                    Name = "TestUI",
                    AssetPath = "Path/1",
                }
            );
            // 不调用InitializeLookup

            // Act
            bool result = _config.TryGet(1, out UIInfo info);

            // Assert
            Assert.IsTrue(result, "应自动初始化并找到");
            Assert.AreEqual("TestUI", info.Name);
        }

        [Test]
        [Description("空列表调用InitializeLookup后TryGet返回false")]
        public void InitializeLookup_EmptyList_TryGetReturnsFalse()
        {
            // Act
            _config.InitializeLookup();

            // Assert
            Assert.IsFalse(_config.TryGet(1, out UIInfo info), "空列表应返回false");
        }

        #endregion

        #region UIInfo结构测试

        [Test]
        [Description("UIInfo所有字段应可正确存储")]
        public void UIInfo_AllFieldsStoreCorrectly()
        {
            // Arrange & Act
            var uiInfo = new UIInfo
            {
                ID = 100,
                Name = "TestWindow",
                AssetPath = "UI/Prefabs/TestWindow",
                Layer = AsakiUILayer.Popup,
                UsePool = true,
            };

            // Assert
            Assert.AreEqual(100, uiInfo.ID);
            Assert.AreEqual("TestWindow", uiInfo.Name);
            Assert.AreEqual("UI/Prefabs/TestWindow", uiInfo.AssetPath);
            Assert.AreEqual(AsakiUILayer.Popup, uiInfo.Layer);
            Assert.IsTrue(uiInfo.UsePool);
        }

        [Test]
        [Description("UIInfo默认值测试")]
        public void UIInfo_DefaultValues_AreDefault()
        {
            // Arrange
            UIInfo uiInfo = default;

            // Assert
            Assert.AreEqual(0, uiInfo.ID);
            Assert.IsNull(uiInfo.Name);
            Assert.IsNull(uiInfo.AssetPath);
            Assert.AreEqual(AsakiUILayer.Scene, uiInfo.Layer); // 枚举默认第一个值
            Assert.IsFalse(uiInfo.UsePool);
        }

        #endregion

        #region GetTemplate测试

        [Test]
        [Description("GetTemplate应返回匹配的预制体")]
        public void GetTemplate_ExistingType_ReturnsPrefab()
        {
            // Arrange
            var prefab = new GameObject("TestPrefab");
            _config.Templates.Add(
                new WidgetTemplate { Type = AsakiUIWidgetType.Button, Prefab = prefab }
            );

            // Act
            GameObject result = _config.GetTemplate(AsakiUIWidgetType.Button);

            // Assert
            Assert.AreSame(prefab, result, "应返回匹配的预制体");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(prefab);
        }

        [Test]
        [Description("GetTemplate不存在的类型应返回null")]
        public void GetTemplate_NonExistentType_ReturnsNull()
        {
            // Act
            GameObject result = _config.GetTemplate(AsakiUIWidgetType.Button);

            // Assert
            Assert.IsNull(result, "不存在的类型应返回null");
        }

        [Test]
        [Description("GetTemplate空列表应返回null")]
        public void GetTemplate_EmptyList_ReturnsNull()
        {
            // Act
            GameObject result = _config.GetTemplate(AsakiUIWidgetType.Text);

            // Assert
            Assert.IsNull(result, "空列表应返回null");
        }

        [Test]
        [Description("GetTemplate应返回第一个匹配的类型")]
        public void GetTemplate_MultipleSameType_ReturnsFirst()
        {
            // Arrange
            var prefab1 = new GameObject("Prefab1");
            var prefab2 = new GameObject("Prefab2");
            _config.Templates.Add(
                new WidgetTemplate { Type = AsakiUIWidgetType.Button, Prefab = prefab1 }
            );
            _config.Templates.Add(
                new WidgetTemplate { Type = AsakiUIWidgetType.Button, Prefab = prefab2 }
            );

            // Act
            GameObject result = _config.GetTemplate(AsakiUIWidgetType.Button);

            // Assert
            Assert.AreSame(prefab1, result, "应返回第一个匹配的预制体");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(prefab1);
            UnityEngine.Object.DestroyImmediate(prefab2);
        }

        #endregion

        #region 资源释放延迟配置测试

        [Test]
        [Description("ResourceReleaseDelaySeconds可设置为0")]
        public void ResourceReleaseDelaySeconds_CanBeZero()
        {
            // Arrange & Act
            _config.ResourceReleaseDelaySeconds = 0f;

            // Assert
            Assert.AreEqual(0f, _config.ResourceReleaseDelaySeconds, "可设置为0");
        }

        [Test]
        [Description("ResourceReleaseDelaySeconds可设置较大值")]
        public void ResourceReleaseDelaySeconds_CanBeLarge()
        {
            // Arrange & Act
            _config.ResourceReleaseDelaySeconds = 300f;

            // Assert
            Assert.AreEqual(300f, _config.ResourceReleaseDelaySeconds, "可设置较大值");
        }

        [Test]
        [Description("ResourceReleaseDelaySeconds可设置小数值")]
        public void ResourceReleaseDelaySeconds_CanBeFractional()
        {
            // Arrange & Act
            _config.ResourceReleaseDelaySeconds = 0.5f;

            // Assert
            Assert.AreEqual(0.5f, _config.ResourceReleaseDelaySeconds, "可设置小数值");
        }

        #endregion

        #region 边界条件测试

        [Test]
        [Description("大量UI的查找表性能测试")]
        public void InitializeLookup_LargeList_PerformsWell()
        {
            // Arrange
            const int count = 1000;
            for (int i = 0; i < count; i++)
            {
                _config.UIList.Add(
                    new UIInfo
                    {
                        ID = i,
                        Name = $"UI_{i}",
                        AssetPath = $"Path/{i}",
                    }
                );
            }

            // Act
            _config.InitializeLookup();

            // Assert - 验证随机几个
            Assert.IsTrue(_config.TryGet(0, out UIInfo info0));
            Assert.AreEqual("UI_0", info0.Name);

            Assert.IsTrue(_config.TryGet(500, out UIInfo info500));
            Assert.AreEqual("UI_500", info500.Name);

            Assert.IsTrue(_config.TryGet(999, out UIInfo info999));
            Assert.AreEqual("UI_999", info999.Name);
        }

        [Test]
        [Description("重复ID的UI应使用最后一个")]
        public void InitializeLookup_DuplicateIds_UsesLast()
        {
            // Arrange
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 1,
                    Name = "First",
                    AssetPath = "Path/1",
                }
            );
            _config.UIList.Add(
                new UIInfo
                {
                    ID = 1,
                    Name = "Second",
                    AssetPath = "Path/2",
                }
            );

            // Act
            _config.InitializeLookup();

            // Assert - Dictionary.TryAdd不会覆盖，所以应该是第一个
            Assert.IsTrue(_config.TryGet(1, out UIInfo info));
            Assert.AreEqual("First", info.Name, "应使用第一个（TryAdd不覆盖）");
        }

        #endregion
    }
}
