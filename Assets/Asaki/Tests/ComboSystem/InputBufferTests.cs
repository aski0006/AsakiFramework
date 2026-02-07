using System.Collections;
using Asaki.Plungin.ComboSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.ComboSystem
{
    /// <summary>
    /// InputBuffer 单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("ComboSystem")]
    public class InputBufferTests
    {
        private InputBuffer _inputBuffer;
        private const float BUFFER_DURATION = 0.3f;

        [SetUp]
        public void Setup()
        {
            _inputBuffer = new InputBuffer(BUFFER_DURATION);
        }

        [TearDown]
        public void Teardown()
        {
            _inputBuffer = null;
        }

        #region 基础功能测试

        [Test]
        [Description("测试：新创建的缓冲应该是空的")]
        public void Constructor_WhenCreated_IsEmpty()
        {
            // Act
            bool hasInput = _inputBuffer.TryGetInput(out string inputTypeId);

            // Assert
            Assert.IsFalse(hasInput, "新创建的InputBuffer应该为空");
            Assert.IsNull(inputTypeId, "空缓冲返回的inputTypeId应为null");
        }

        [Test]
        [Description("测试：压入输入后可以获取")]
        public void PushInput_AfterPush_CanGetInput()
        {
            // Arrange
            const string testInput = "LightAttack";

            // Act
            _inputBuffer.PushInput(testInput);
            bool hasInput = _inputBuffer.TryGetInput(out string inputTypeId);

            // Assert
            Assert.IsTrue(hasInput, "压入输入后应该能获取到");
            Assert.AreEqual(testInput, inputTypeId, "获取的输入类型应与压入的相同");
        }

        [Test]
        [Description("测试：获取输入后应该从缓冲中移除")]
        public void TryGetInput_AfterGet_RemovesFromBuffer()
        {
            // Arrange
            _inputBuffer.PushInput("LightAttack");

            // Act
            _inputBuffer.TryGetInput(out string _);
            bool hasInput = _inputBuffer.TryGetInput(out string _);

            // Assert
            Assert.IsFalse(hasInput, "获取输入后应该被移除");
        }

        [Test]
        [Description("测试：可以压入多个输入并按FIFO顺序获取")]
        public void PushInput_MultipleInputs_RespectsFIFO()
        {
            // Arrange
            const string input1 = "LightAttack";
            const string input2 = "HeavyAttack";
            const string input3 = "Skill1";

            // Act
            _inputBuffer.PushInput(input1);
            _inputBuffer.PushInput(input2);
            _inputBuffer.PushInput(input3);

            // Assert
            _inputBuffer.TryGetInput(out string result1);
            _inputBuffer.TryGetInput(out string result2);
            _inputBuffer.TryGetInput(out string result3);

            Assert.AreEqual(input1, result1, "应按FIFO顺序返回第一个输入");
            Assert.AreEqual(input2, result2, "应按FIFO顺序返回第二个输入");
            Assert.AreEqual(input3, result3, "应按FIFO顺序返回第三个输入");
        }

        #endregion

        #region 清理测试

        [Test]
        [Description("测试：清空缓冲后应该为空")]
        public void Clear_AfterClear_IsEmpty()
        {
            // Arrange
            _inputBuffer.PushInput("LightAttack");
            _inputBuffer.PushInput("HeavyAttack");

            // Act
            _inputBuffer.Clear();

            // Assert
            bool hasInput = _inputBuffer.TryGetInput(out string _);
            Assert.IsFalse(hasInput, "清空后缓冲应为空");
        }

        #endregion

        #region 过期清理测试（需要运行时）

        [UnityTest]
        [Description("测试：过期输入应该被自动清理")]
        [Category("PlayMode")]
        public IEnumerator TryGetInput_ExpiredInput_IsRemoved()
        {
            // Arrange - 使用一个很短的缓冲时间
            var shortBuffer = new InputBuffer(0.05f);
            shortBuffer.PushInput("LightAttack");

            // Act - 等待超过缓冲时间
            yield return new WaitForSeconds(0.1f);

            // Assert
            bool hasInput = shortBuffer.TryGetInput(out string _);
            Assert.IsFalse(hasInput, "过期输入应该被自动清理");
        }

        [UnityTest]
        [Description("测试：混合有效和过期输入时只返回有效输入")]
        [Category("PlayMode")]
        public IEnumerator TryGetInput_MixedValidAndExpired_ReturnsOnlyValid()
        {
            // Arrange
            var shortBuffer = new InputBuffer(0.05f);
            shortBuffer.PushInput("OldInput");

            yield return new WaitForSeconds(0.06f);

            shortBuffer.PushInput("NewInput");

            // Act
            bool hasInput = shortBuffer.TryGetInput(out string inputTypeId);

            // Assert
            Assert.IsTrue(hasInput, "应该返回有效输入");
            Assert.AreEqual("NewInput", inputTypeId, "应该返回最新的有效输入");

            // 再次获取应该为空
            bool hasMore = shortBuffer.TryGetInput(out string _);
            Assert.IsFalse(hasMore, "不应该有旧输入残留");
        }

        #endregion

        #region 边界条件测试

        [Test]
        [Description("测试：空字符串输入")]
        public void PushInput_EmptyString_CanBeRetrieved()
        {
            // Arrange
            const string emptyInput = "";

            // Act
            _inputBuffer.PushInput(emptyInput);
            bool hasInput = _inputBuffer.TryGetInput(out string inputTypeId);

            // Assert
            Assert.IsTrue(hasInput, "空字符串也应该被缓冲");
            Assert.AreEqual(emptyInput, inputTypeId, "应该返回空字符串");
        }

        [Test]
        [Description("测试：null输入")]
        public void PushInput_NullInput_CanBeRetrieved()
        {
            // Arrange
            string nullInput = null;

            // Act
            _inputBuffer.PushInput(nullInput);
            bool hasInput = _inputBuffer.TryGetInput(out string inputTypeId);

            // Assert
            Assert.IsTrue(hasInput, "null也应该被缓冲");
            Assert.IsNull(inputTypeId, "应该返回null");
        }

        [Test]
        [Description("测试：大量输入 - 性能测试：1000个输入")]
        [Category("Performance")]
        public void PushInput_LargeNumberOfInputs_HandlesCorrectly()
        {
            // Arrange
            const int count = 1000;

            // Act
            for (int i = 0; i < count; i++)
            {
                _inputBuffer.PushInput($"Input{i}");
            }

            // Assert - 验证所有输入都能按顺序获取
            for (int i = 0; i < count; i++)
            {
                bool hasInput = _inputBuffer.TryGetInput(out string inputTypeId);
                Assert.IsTrue(hasInput, $"第{i}个输入应该能被获取");
                Assert.AreEqual($"Input{i}", inputTypeId, $"第{i}个输入应该匹配");
            }

            // 验证缓冲已空
            bool hasMore = _inputBuffer.TryGetInput(out string _);
            Assert.IsFalse(hasMore, "所有输入获取完毕后应为空");
        }

        #endregion
    }
}
