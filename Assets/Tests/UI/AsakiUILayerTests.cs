// File: Assets/Tests/UI/AsakiUILayerTests.cs

using Asaki.Core.UI;
using NUnit.Framework;

namespace Asaki.Tests.UI
{
    /// <summary>
    /// UI层级枚举单元测试
    /// 测试层级顺序和值
    /// </summary>
    [TestFixture]
    [Category("UI")]
    [Category("Unit")]
    public class AsakiUILayerTests
    {
        #region 层级值测试

        [Test]
        [Description("Scene层级应为0")]
        public void UILayer_Scene_IsZero()
        {
            Assert.AreEqual(0, (int)AsakiUILayer.Scene, "Scene层级应为0");
        }

        [Test]
        [Description("Normal层级应为1")]
        public void UILayer_Normal_IsOne()
        {
            Assert.AreEqual(1, (int)AsakiUILayer.Normal, "Normal层级应为1");
        }

        [Test]
        [Description("Popup层级应为2")]
        public void UILayer_Popup_IsTwo()
        {
            Assert.AreEqual(2, (int)AsakiUILayer.Popup, "Popup层级应为2");
        }

        [Test]
        [Description("System层级应为3")]
        public void UILayer_System_IsThree()
        {
            Assert.AreEqual(3, (int)AsakiUILayer.System, "System层级应为3");
        }

        [Test]
        [Description("Hidden层级应为4")]
        public void UILayer_Hidden_IsFour()
        {
            Assert.AreEqual(4, (int)AsakiUILayer.Hidden, "Hidden层级应为4");
        }

        #endregion

        #region 层级顺序测试

        [Test]
        [Description("层级顺序应符合预期")]
        public void UILayer_Order_IsCorrect()
        {
            // Arrange & Act & Assert
            Assert.Less(AsakiUILayer.Scene, AsakiUILayer.Normal, "Scene应在Normal之下");
            Assert.Less(AsakiUILayer.Normal, AsakiUILayer.Popup, "Normal应在Popup之下");
            Assert.Less(AsakiUILayer.Popup, AsakiUILayer.System, "Popup应在System之下");
            Assert.Less(AsakiUILayer.System, AsakiUILayer.Hidden, "System应在Hidden之下");
        }

        [Test]
        [Description("层级值是连续的")]
        public void UILayer_Values_AreConsecutive()
        {
            // Arrange
            var values = (AsakiUILayer[])System.Enum.GetValues(typeof(AsakiUILayer));

            // Act & Assert
            for (int i = 0; i < values.Length; i++)
            {
                Assert.AreEqual(i, (int)values[i], $"层级{i}的值应为{i}");
            }
        }

        #endregion

        #region 枚举转换测试

        [Test]
        [Description("整数可正确转换为层级")]
        public void UILayer_FromInt_ConvertsCorrectly()
        {
            Assert.AreEqual(AsakiUILayer.Scene, (AsakiUILayer)0);
            Assert.AreEqual(AsakiUILayer.Normal, (AsakiUILayer)1);
            Assert.AreEqual(AsakiUILayer.Popup, (AsakiUILayer)2);
            Assert.AreEqual(AsakiUILayer.System, (AsakiUILayer)3);
            Assert.AreEqual(AsakiUILayer.Hidden, (AsakiUILayer)4);
        }

        [Test]
        [Description("层级可正确转换为整数")]
        public void UILayer_ToInt_ConvertsCorrectly()
        {
            Assert.AreEqual(0, (int)AsakiUILayer.Scene);
            Assert.AreEqual(1, (int)AsakiUILayer.Normal);
            Assert.AreEqual(2, (int)AsakiUILayer.Popup);
            Assert.AreEqual(3, (int)AsakiUILayer.System);
            Assert.AreEqual(4, (int)AsakiUILayer.Hidden);
        }

        #endregion

        #region 字符串转换测试

        [Test]
        [Description("层级名称应正确")]
        public void UILayer_Names_AreCorrect()
        {
            Assert.AreEqual("Scene", AsakiUILayer.Scene.ToString());
            Assert.AreEqual("Normal", AsakiUILayer.Normal.ToString());
            Assert.AreEqual("Popup", AsakiUILayer.Popup.ToString());
            Assert.AreEqual("System", AsakiUILayer.System.ToString());
            Assert.AreEqual("Hidden", AsakiUILayer.Hidden.ToString());
        }

        [Test]
        [Description("Parse应正确解析层级名称")]
        public void UILayer_Parse_WorksCorrectly()
        {
            Assert.AreEqual(AsakiUILayer.Scene, System.Enum.Parse<AsakiUILayer>("Scene"));
            Assert.AreEqual(AsakiUILayer.Normal, System.Enum.Parse<AsakiUILayer>("Normal"));
            Assert.AreEqual(AsakiUILayer.Popup, System.Enum.Parse<AsakiUILayer>("Popup"));
            Assert.AreEqual(AsakiUILayer.System, System.Enum.Parse<AsakiUILayer>("System"));
            Assert.AreEqual(AsakiUILayer.Hidden, System.Enum.Parse<AsakiUILayer>("Hidden"));
        }

        #endregion

        #region 位运算测试（如果用于层级掩码）

        [Test]
        [Description("层级可用于位运算")]
        public void UILayer_BitwiseOperations_WorkCorrectly()
        {
            // Arrange
            int sceneMask = 1 << (int)AsakiUILayer.Scene;
            int normalMask = 1 << (int)AsakiUILayer.Normal;
            int popupMask = 1 << (int)AsakiUILayer.Popup;

            // Act
            int combinedMask = sceneMask | normalMask | popupMask;

            // Assert
            Assert.AreNotEqual(0, combinedMask & sceneMask, "应包含Scene");
            Assert.AreNotEqual(0, combinedMask & normalMask, "应包含Normal");
            Assert.AreNotEqual(0, combinedMask & popupMask, "应包含Popup");
            Assert.AreEqual(0, combinedMask & (1 << (int)AsakiUILayer.System), "不应包含System");
        }

        #endregion
    }
}
